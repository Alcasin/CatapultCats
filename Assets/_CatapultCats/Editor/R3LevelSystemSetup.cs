using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatapultCats.Core;
using CatapultCats.Launch;
using CatapultCats.Levels;
using CatapultCats.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CatapultCats.Editor
{
    public static class R3LevelSystemSetup
    {
        private const string GameplayScenePath = "Assets/_CatapultCats/Scenes/Gameplay.unity";
        private const string AuthoringScenePath = "Assets/_CatapultCats/Editor/LevelAuthoring.unity";
        private const string CatalogPath = "Assets/_CatapultCats/Data/PieceCatalog.asset";
        private const string SandboxPath = "Assets/_CatapultCats/Data/Levels/R3_Sandbox.asset";
        private const string SquareSpritePath = "Assets/_CatapultCats/Art/Sprites/Environment/R2_PrototypeSquare.png";
        private const string MouseSpritePath = "Assets/_CatapultCats/Art/Sprites/Characters/R2_MousePrototype.png";
        private const string PhysicsFolder = "Assets/_CatapultCats/Physics";
        private const string StructurePrefabFolder = "Assets/_CatapultCats/Prefabs/Structures";
        private const string GameplayPrefabFolder = "Assets/_CatapultCats/Prefabs/Gameplay";

        [MenuItem("CatapultCats/Apply R3 Level System")]
        public static void Apply()
        {
            try
            {
                EnsureFolders();
                Sprite square = RequireAsset<Sprite>(SquareSpritePath);
                Sprite mouse = RequireAsset<Sprite>(MouseSpritePath);
                PhysicsMaterial2D wood = RequireAsset<PhysicsMaterial2D>($"{PhysicsFolder}/Wood.physicsMaterial2D");
                PhysicsMaterial2D glass = RequireAsset<PhysicsMaterial2D>($"{PhysicsFolder}/Glass.physicsMaterial2D");
                PhysicsMaterial2D heavy = RequireAsset<PhysicsMaterial2D>($"{PhysicsFolder}/Heavy.physicsMaterial2D");
                PhysicsMaterial2D cat = RequireAsset<PhysicsMaterial2D>($"{PhysicsFolder}/Cat.physicsMaterial2D");
                PhysicsMaterial2D ramp = CreateOrUpdateMaterial("Ramp", 0.35f, 0.18f);

                Dictionary<LevelPieceType, GameObject> prefabs = CreatePrefabs(square, mouse, wood, glass, heavy, ramp);
                PieceCatalog catalog = CreateOrUpdateCatalog(prefabs);
                LevelDefinition sandbox = CreateOrUpdateSandbox();
                AssetDatabase.SaveAssets();

                catalog = RequireAsset<PieceCatalog>(CatalogPath);
                sandbox = RequireAsset<LevelDefinition>(SandboxPath);
                MigrateGameplay(catalog, sandbox, cat);
                CreateOrResetAuthoringScene();
                ConfigureBuildSettings();
                AssetDatabase.SaveAssets();

                IReadOnlyList<string> failures = ValidateArchitecture();
                if (failures.Count > 0)
                {
                    throw new InvalidOperationException(string.Join(" | ", failures));
                }

                Debug.Log("[CatapultCats R3] Apply and validation succeeded. Data-driven levels and Level Editor are ready for manual QA.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CatapultCats R3] Apply and validation failed: " + exception.Message);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_CatapultCats/Data/Levels");
            EnsureFolder(StructurePrefabFolder);
            EnsureFolder(GameplayPrefabFolder);
            EnsureFolder(PhysicsFolder);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static Dictionary<LevelPieceType, GameObject> CreatePrefabs(
            Sprite square,
            Sprite mouse,
            PhysicsMaterial2D wood,
            PhysicsMaterial2D glass,
            PhysicsMaterial2D heavy,
            PhysicsMaterial2D ramp)
        {
            return new Dictionary<LevelPieceType, GameObject>
            {
                [LevelPieceType.WoodBeam] = CreateBreakablePrefab(
                    LevelPieceType.WoodBeam, LevelValidation.GetPieceSize(LevelPieceType.WoodBeam), 1.4f, 5f, 3,
                    square, new Color32(176, 108, 56, 255), wood),
                [LevelPieceType.WoodBlock] = CreateBreakablePrefab(
                    LevelPieceType.WoodBlock, LevelValidation.GetPieceSize(LevelPieceType.WoodBlock), 1.2f, 5f, 3,
                    square, new Color32(176, 108, 56, 255), wood),
                [LevelPieceType.GlassBeam] = CreateBreakablePrefab(
                    LevelPieceType.GlassBeam, LevelValidation.GetPieceSize(LevelPieceType.GlassBeam), 0.7f, 1.5f, 4,
                    square, new Color32(24, 184, 177, 255), glass),
                [LevelPieceType.GlassBlock] = CreateBreakablePrefab(
                    LevelPieceType.GlassBlock, LevelValidation.GetPieceSize(LevelPieceType.GlassBlock), 0.55f, 1.5f, 4,
                    square, new Color32(24, 184, 177, 255), glass),
                [LevelPieceType.HeavyBlock] = CreateHeavyPrefab(square, heavy),
                [LevelPieceType.Ramp] = CreateRampPrefab(square, ramp),
                [LevelPieceType.Mouse] = CreateMousePrefab(mouse)
            };
        }

        private static GameObject CreateBreakablePrefab(
            LevelPieceType pieceType,
            Vector2 size,
            float mass,
            float threshold,
            int debrisCount,
            Sprite sprite,
            Color color,
            PhysicsMaterial2D material)
        {
            GameObject root = CreateVisualBody(pieceType.ToString(), size, mass, sprite, color, material);
            Rigidbody2D body = root.GetComponent<Rigidbody2D>();
            BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
            SpriteRenderer visual = root.GetComponent<SpriteRenderer>();
            var debris = new Rigidbody2D[debrisCount];
            for (int index = 0; index < debrisCount; index++)
            {
                var fragment = new GameObject($"Debris_{index + 1}");
                fragment.transform.SetParent(root.transform, false);
                fragment.transform.localPosition = new Vector3(
                    (index - (debrisCount - 1) * 0.5f) / debrisCount,
                    0f,
                    0f);
                fragment.transform.localScale = new Vector3(0.8f / debrisCount, 0.35f, 1f);
                var fragmentVisual = fragment.AddComponent<SpriteRenderer>();
                fragmentVisual.sprite = sprite;
                fragmentVisual.color = color;
                fragmentVisual.sortingOrder = 5;
                var fragmentCollider = fragment.AddComponent<BoxCollider2D>();
                fragmentCollider.sharedMaterial = material;
                debris[index] = fragment.AddComponent<Rigidbody2D>();
                debris[index].mass = Mathf.Max(0.1f, mass / debrisCount);
                debris[index].collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                fragment.SetActive(false);
            }

            var breakable = root.AddComponent<BreakablePiece2D>();
            var serialized = new SerializedObject(breakable);
            serialized.FindProperty("breakThreshold").floatValue = threshold;
            serialized.FindProperty("intactBody").objectReferenceValue = body;
            serialized.FindProperty("intactCollider").objectReferenceValue = collider;
            serialized.FindProperty("intactVisual").objectReferenceValue = visual;
            SetArray(serialized.FindProperty("debrisBodies"), debris);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, $"{StructurePrefabFolder}/{pieceType}.prefab");
        }

        private static GameObject CreateHeavyPrefab(Sprite sprite, PhysicsMaterial2D material)
        {
            GameObject root = CreateVisualBody(
                "HeavyBlock",
                LevelValidation.GetPieceSize(LevelPieceType.HeavyBlock),
                5f,
                sprite,
                new Color32(69, 84, 105, 255),
                material);
            return SavePrefab(root, $"{StructurePrefabFolder}/HeavyBlock.prefab");
        }

        private static GameObject CreateRampPrefab(Sprite sprite, PhysicsMaterial2D material)
        {
            var root = new GameObject("Ramp");
            Vector2 size = LevelValidation.GetPieceSize(LevelPieceType.Ramp);
            root.transform.localScale = new Vector3(size.x, size.y, 1f);
            var visual = root.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.color = new Color32(91, 123, 82, 255);
            visual.sortingOrder = 4;
            var collider = root.AddComponent<BoxCollider2D>();
            collider.edgeRadius = 0f;
            collider.sharedMaterial = material;
            return SavePrefab(root, $"{StructurePrefabFolder}/Ramp.prefab");
        }

        private static GameObject CreateMousePrefab(Sprite sprite)
        {
            var root = new GameObject("Mouse");
            root.transform.localScale = Vector3.one * 0.6f;
            var visual = root.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.color = new Color32(205, 207, 214, 255);
            visual.sortingOrder = 6;
            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            var body = root.AddComponent<Rigidbody2D>();
            body.mass = 0.55f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var target = root.AddComponent<MouseTarget2D>();
            var serialized = new SerializedObject(target);
            serialized.FindProperty("defeatThreshold").floatValue = 3f;
            serialized.FindProperty("crushMassThreshold").floatValue = 4f;
            serialized.FindProperty("targetBody").objectReferenceValue = body;
            serialized.FindProperty("targetCollider").objectReferenceValue = collider;
            serialized.FindProperty("targetVisual").objectReferenceValue = visual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, $"{GameplayPrefabFolder}/Mouse.prefab");
        }

        private static GameObject CreateVisualBody(
            string name,
            Vector2 size,
            float mass,
            Sprite sprite,
            Color color,
            PhysicsMaterial2D material)
        {
            var root = new GameObject(name);
            root.transform.localScale = new Vector3(size.x, size.y, 1f);
            var visual = root.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.color = color;
            visual.sortingOrder = 4;
            var collider = root.AddComponent<BoxCollider2D>();
            collider.edgeRadius = 0f;
            collider.sharedMaterial = material;
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = mass;
            body.gravityScale = 1f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return root;
        }

        private static GameObject SavePrefab(GameObject temporaryRoot, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporaryRoot, path);
            Object.DestroyImmediate(temporaryRoot);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Could not create prefab {path}.");
            }

            return prefab;
        }

        private static PieceCatalog CreateOrUpdateCatalog(IReadOnlyDictionary<LevelPieceType, GameObject> prefabs)
        {
            PieceCatalog catalog = AssetDatabase.LoadAssetAtPath<PieceCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PieceCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.ReplaceEntries(Enum.GetValues(typeof(LevelPieceType))
                .Cast<LevelPieceType>()
                .Select(pieceType => new PieceCatalog.Entry(pieceType, prefabs[pieceType])));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static LevelDefinition CreateOrUpdateSandbox()
        {
            LevelDefinition sandbox = AssetDatabase.LoadAssetAtPath<LevelDefinition>(SandboxPath);
            if (sandbox == null)
            {
                sandbox = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(sandbox, SandboxPath);
            }

            sandbox.Configure(
                "R3_Sandbox",
                3,
                LevelDefinition.DefaultSlingshotPosition,
                new[]
                {
                    new LevelPiecePlacement(LevelPieceType.WoodBlock, new Vector2(3.5f, -4.625f), 0f),
                    new LevelPiecePlacement(LevelPieceType.WoodBlock, new Vector2(5.5f, -4.625f), 0f),
                    new LevelPiecePlacement(LevelPieceType.GlassBlock, new Vector2(4.5f, -4.625f), 0f),
                    new LevelPiecePlacement(LevelPieceType.WoodBeam, new Vector2(4.5f, -4.25f), 0f),
                    new LevelPiecePlacement(LevelPieceType.HeavyBlock, new Vector2(4.5f, -3.75f), 0f),
                    new LevelPiecePlacement(LevelPieceType.Mouse, new Vector2(2.75f, -4.575f), 0f),
                    new LevelPiecePlacement(LevelPieceType.Mouse, new Vector2(6.25f, -4.575f), 0f)
                });
            EditorUtility.SetDirty(sandbox);
            return sandbox;
        }

        private static void MigrateGameplay(PieceCatalog catalog, LevelDefinition sandbox, PhysicsMaterial2D catMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            SlingshotController2D slingshot = FindOne<SlingshotController2D>(scene);
            if (slingshot == null)
            {
                throw new InvalidOperationException("Accepted R1 slingshot is missing from Gameplay.");
            }

            foreach (GameObject root in scene.GetRootGameObjects().Where(root => root.name == "R2_Prototype").ToArray())
            {
                Object.DestroyImmediate(root);
            }

            GameObject runtime = slingshot.transform.root.gameObject;
            runtime.name = "GameplayRuntime";
            ConfigureGround(runtime.transform);
            RemoveDirectChild(runtime.transform, "LevelRuntimeRoot");
            RemoveDirectChild(runtime.transform, "Systems");

            GameObject levelRootObject = CreateChild(runtime.transform, "LevelRuntimeRoot");
            GameObject systems = CreateChild(runtime.transform, "Systems");
            var resolver = CreateChild(systems.transform, "PhysicsResolver").AddComponent<PhysicsResolver2D>();
            var attempt = CreateChild(systems.transform, "AttemptController").AddComponent<AttemptController>();
            var loader = CreateChild(systems.transform, "LevelLoader").AddComponent<LevelLoader>();
            var bootstrap = CreateChild(systems.transform, "LevelBootstrap").AddComponent<LevelBootstrap>();

            ConfigureResolver(resolver, slingshot.ProjectileBody);
            ConfigureAttempt(attempt, slingshot, resolver);
            ConfigureLoader(loader, catalog, levelRootObject.transform, slingshot, attempt, resolver);
            ConfigureBootstrap(bootstrap, catalog, sandbox, loader);
            slingshot.ProjectileCollider.sharedMaterial = catMaterial;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, GameplayScenePath))
            {
                throw new InvalidOperationException("Could not save migrated Gameplay scene.");
            }
        }

        private static void CreateOrResetAuthoringScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(LevelEditorWindow.AuthoringRootName);
            if (!EditorSceneManager.SaveScene(scene, AuthoringScenePath))
            {
                throw new InvalidOperationException("Could not save LevelAuthoring scene.");
            }

            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
        }

        private static IReadOnlyList<string> ValidateArchitecture()
        {
            var failures = new List<string>();
            PieceCatalog catalog = AssetDatabase.LoadAssetAtPath<PieceCatalog>(CatalogPath);
            LevelDefinition sandbox = AssetDatabase.LoadAssetAtPath<LevelDefinition>(SandboxPath);
            if (catalog == null)
            {
                failures.Add($"Shared PieceCatalog is missing at {CatalogPath}.");
            }
            else
            {
                failures.AddRange(LevelEditorValidation.ValidateCatalogPrefabs(catalog));
            }

            if (sandbox == null)
            {
                failures.Add($"R3 sandbox LevelDefinition is missing at {SandboxPath}.");
            }
            else if (catalog != null)
            {
                failures.AddRange(LevelValidation.Validate(sandbox, catalog).Errors);
            }

            Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            LevelBootstrap bootstrap = FindOne<LevelBootstrap>(scene);
            LevelLoader loader = FindOne<LevelLoader>(scene);
            SlingshotController2D slingshot = FindOne<SlingshotController2D>(scene);
            if (scene.GetRootGameObjects().Any(root => root.name == "R2_Prototype"))
            {
                failures.Add("Gameplay still contains the obsolete R2_Prototype root.");
            }

            if (bootstrap == null || bootstrap.DefaultLevel != sandbox || bootstrap.PieceCatalog != catalog)
            {
                failures.Add("Gameplay LevelBootstrap references are invalid.");
            }

            if (loader == null || loader.Catalog != catalog || loader.RuntimeRoot == null)
            {
                failures.Add("Gameplay LevelLoader references are invalid.");
            }

            if (slingshot == null || Mathf.Abs(slingshot.MaximumDragDistance - 1.8f) > 0.001f ||
                Mathf.Abs(slingshot.MinimumLaunchDistance - 0.2f) > 0.001f ||
                Mathf.Abs(slingshot.LaunchSpeedPerUnit - 10.5f) > 0.001f)
            {
                failures.Add("Accepted slingshot tuning changed.");
            }

            Transform ground = slingshot == null
                ? null
                : slingshot.transform.root.Cast<Transform>().FirstOrDefault(child => child.name == "Ground");
            BoxCollider2D groundCollider = ground == null ? null : ground.GetComponent<BoxCollider2D>();
            if (groundCollider == null || Mathf.Abs(groundCollider.bounds.max.y - LevelValidation.GroundTopY) > 0.001f)
            {
                failures.Add($"Gameplay ground top must be {LevelValidation.GroundTopY:0.###}.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AuthoringScenePath) == null)
            {
                failures.Add("Editor-only LevelAuthoring scene is missing.");
            }

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length != 1 || !buildScenes[0].enabled || buildScenes[0].path != GameplayScenePath)
            {
                failures.Add("Build Settings must contain only enabled Gameplay scene.");
            }

            if (File.Exists("Assets/_CatapultCats/Editor/R1SlingshotSetup.cs") ||
                File.Exists("Assets/_CatapultCats/Editor/R2DestructionSetup.cs"))
            {
                failures.Add("Obsolete R1/R2 setup utilities still exist.");
            }

            return failures.Distinct().ToArray();
        }

        private static void ConfigureResolver(PhysicsResolver2D resolver, Rigidbody2D projectile)
        {
            var serialized = new SerializedObject(resolver);
            serialized.FindProperty("projectileBody").objectReferenceValue = projectile;
            serialized.FindProperty("minimumObservationTime").floatValue = 0.3f;
            serialized.FindProperty("linearSettleSpeed").floatValue = 0.15f;
            serialized.FindProperty("angularSettleSpeed").floatValue = 10f;
            serialized.FindProperty("settleHoldDuration").floatValue = 0.35f;
            serialized.FindProperty("maximumResolutionTime").floatValue = 5f;
            serialized.FindProperty("horizontalBounds").vector2Value = new Vector2(-11f, 11f);
            serialized.FindProperty("projectileMinimumY").floatValue = -6.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAttempt(AttemptController attempt, SlingshotController2D slingshot, PhysicsResolver2D resolver)
        {
            var serialized = new SerializedObject(attempt);
            serialized.FindProperty("slingshot").objectReferenceValue = slingshot;
            serialized.FindProperty("resolver").objectReferenceValue = resolver;
            serialized.FindProperty("maximumShots").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLoader(
            LevelLoader loader,
            PieceCatalog catalog,
            Transform runtimeRoot,
            SlingshotController2D slingshot,
            AttemptController attempt,
            PhysicsResolver2D resolver)
        {
            var serialized = new SerializedObject(loader);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("runtimeRoot").objectReferenceValue = runtimeRoot;
            serialized.FindProperty("slingshot").objectReferenceValue = slingshot;
            serialized.FindProperty("attemptController").objectReferenceValue = attempt;
            serialized.FindProperty("resolver").objectReferenceValue = resolver;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loader);
        }

        private static void ConfigureBootstrap(LevelBootstrap bootstrap, PieceCatalog catalog, LevelDefinition level, LevelLoader loader)
        {
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("pieceCatalog").objectReferenceValue = catalog;
            serialized.FindProperty("defaultLevel").objectReferenceValue = level;
            serialized.FindProperty("levelLoader").objectReferenceValue = loader;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
        }

        private static PhysicsMaterial2D CreateOrUpdateMaterial(string name, float friction, float bounciness)
        {
            string path = $"{PhysicsFolder}/{name}.physicsMaterial2D";
            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material == null)
            {
                material = new PhysicsMaterial2D(name);
                AssetDatabase.CreateAsset(material, path);
            }

            material.friction = friction;
            material.bounciness = bounciness;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException($"Required asset is missing: {path}");
            }

            return asset;
        }

        private static T FindOne<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void RemoveDirectChild(Transform parent, string name)
        {
            Transform child = parent.Cast<Transform>().FirstOrDefault(item => item.name == name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void ConfigureGround(Transform parent)
        {
            Transform ground = parent.Cast<Transform>()
                .FirstOrDefault(item => item.name == "Ground" || item.name == "PrototypeGround");
            if (ground == null)
            {
                throw new InvalidOperationException("Gameplay ground is missing.");
            }

            BoxCollider2D collider = ground.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                throw new InvalidOperationException("Gameplay ground has no BoxCollider2D.");
            }

            ground.name = "Ground";
            float halfHeight = collider.size.y * Mathf.Abs(ground.localScale.y) * 0.5f;
            float scaledOffset = collider.offset.y * ground.localScale.y;
            Vector3 position = ground.localPosition;
            position.y = LevelValidation.GroundTopY - scaledOffset - halfHeight;
            ground.localPosition = position;
        }

        private static void SetArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}
