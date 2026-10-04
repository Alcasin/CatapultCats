using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatapultCats.Core;
using CatapultCats.Launch;
using CatapultCats.Physics;
using CatapultCats.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CatapultCats.Editor
{
    public static class R2DestructionSetup
    {
        private const string ScenePath = "Assets/_CatapultCats/Scenes/Gameplay.unity";
        private const string RootName = "R2_Prototype";
        private const string SquareSpritePath = "Assets/_CatapultCats/Art/Sprites/Environment/R2_PrototypeSquare.png";
        private const string MouseSpritePath = "Assets/_CatapultCats/Art/Sprites/Characters/R2_MousePrototype.png";
        private const string PhysicsFolder = "Assets/_CatapultCats/Physics";

        [MenuItem("CatapultCats/Apply R2 Destruction Prototype")]
        public static void Apply()
        {
            try
            {
                Scene scene = OpenScene();
                SlingshotController2D slingshot = FindOne<SlingshotController2D>(scene);
                Camera camera = FindOne<Camera>(scene);
                TrajectoryPreview2D trajectory = FindOne<TrajectoryPreview2D>(scene);
                if (slingshot == null || camera == null || trajectory == null)
                {
                    throw new InvalidOperationException("Accepted R1 launcher components are missing.");
                }

                EnsureFourDotTrajectory(trajectory);
                RemoveExistingRoot(scene);
                Sprite square = CreateSprite(SquareSpritePath, false);
                Sprite mouse = CreateSprite(MouseSpritePath, true);
                PhysicsMaterial2D woodMaterial = CreateMaterial("Wood", 0.55f, 0.02f);
                PhysicsMaterial2D glassMaterial = CreateMaterial("Glass", 0.12f, 0.03f);
                PhysicsMaterial2D heavyMaterial = CreateMaterial("Heavy", 0.6f, 0.01f);
                PhysicsMaterial2D catMaterial = CreateMaterial("Cat", 0.35f, 0.12f);
                slingshot.ProjectileCollider.sharedMaterial = catMaterial;

                CreateR2Prototype(scene, slingshot, square, mouse, woodMaterial, glassMaterial, heavyMaterial);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new InvalidOperationException("Could not save Gameplay scene.");
                }

                ValidateScene(scene);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CatapultCats R2] Apply and validation failed: " + exception.Message);
            }
        }

        [MenuItem("CatapultCats/Validate R2 Destruction Prototype")]
        public static void Validate()
        {
            try
            {
                ValidateScene(OpenScene());
            }
            catch (Exception exception)
            {
                Debug.LogError("[CatapultCats R2] Validation failed: " + exception.Message);
            }
        }

        private static Scene OpenScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new FileNotFoundException("Gameplay scene is missing.", ScenePath);
            }

            Scene active = SceneManager.GetActiveScene();
            return active.path == ScenePath && active.isLoaded
                ? active
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static T FindOne<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }

        private static void EnsureFourDotTrajectory(TrajectoryPreview2D preview)
        {
            var serialized = new SerializedObject(preview);
            SerializedProperty dots = serialized.FindProperty("dots");
            for (int index = dots.arraySize - 1; index >= 4; index--)
            {
                var renderer = dots.GetArrayElementAtIndex(index).objectReferenceValue as SpriteRenderer;
                if (renderer != null)
                {
                    Object.DestroyImmediate(renderer.gameObject);
                }
            }

            dots.arraySize = Mathf.Min(dots.arraySize, 4);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Transform solidSegment = preview.transform.Find("SolidSegment");
            if (solidSegment != null)
            {
                Object.DestroyImmediate(solidSegment.gameObject);
            }
        }

        private static void RemoveExistingRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects().Where(item => item.name == RootName).ToArray())
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateR2Prototype(
            Scene scene,
            SlingshotController2D slingshot,
            Sprite square,
            Sprite mouseSprite,
            PhysicsMaterial2D woodMaterial,
            PhysicsMaterial2D glassMaterial,
            PhysicsMaterial2D heavyMaterial)
        {
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            BreakablePiece2D leftWood = CreateBreakable(root.transform, "WoodSupportLeft", new Vector2(3.4f, -4.1f), new Vector2(0.32f, 1.6f), 1.2f, 5f, 3, square, new Color32(176, 108, 56, 255), woodMaterial);
            BreakablePiece2D rightWood = CreateBreakable(root.transform, "WoodSupportRight", new Vector2(5.6f, -4.1f), new Vector2(0.32f, 1.6f), 1.2f, 5f, 3, square, new Color32(176, 108, 56, 255), woodMaterial);
            BreakablePiece2D glass = CreateBreakable(root.transform, "GlassSupport", new Vector2(4.5f, -4.15f), new Vector2(0.24f, 1.5f), 0.55f, 1.5f, 4, square, new Color32(24, 184, 177, 255), glassMaterial);
            BreakablePiece2D topWood = CreateBreakable(root.transform, "WoodTopBeam", new Vector2(4.5f, -3.16f), new Vector2(2.6f, 0.28f), 1.4f, 5f, 3, square, new Color32(176, 108, 56, 255), woodMaterial);

            Rigidbody2D heavy = CreateHeavy(root.transform, square, heavyMaterial);
            MouseTarget2D mouseLeft = CreateMouse(root.transform, "MouseTarget_Left", new Vector2(3.95f, -4.58f), mouseSprite);
            MouseTarget2D mouseRight = CreateMouse(root.transform, "MouseTarget_Right", new Vector2(5.05f, -4.58f), mouseSprite);

            var resolverObject = new GameObject("PhysicsResolver");
            resolverObject.transform.SetParent(root.transform, false);
            var resolver = resolverObject.AddComponent<PhysicsResolver2D>();
            ConfigureResolver(resolver, slingshot.ProjectileBody, new[]
            {
                slingshot.ProjectileBody,
                heavy,
                topWood.GetComponent<Rigidbody2D>(),
                leftWood.GetComponent<Rigidbody2D>(),
                rightWood.GetComponent<Rigidbody2D>()
            });

            var attemptObject = new GameObject("AttemptController");
            attemptObject.transform.SetParent(root.transform, false);
            var attempt = attemptObject.AddComponent<AttemptController>();
            ConfigureAttempt(attempt, slingshot, resolver, new[] { mouseLeft, mouseRight });
        }

        private static BreakablePiece2D CreateBreakable(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float mass,
            float threshold,
            int debrisCount,
            Sprite sprite,
            Color color,
            PhysicsMaterial2D material)
        {
            GameObject piece = CreateVisualBody(parent, name, position, size, mass, sprite, color, material);
            Rigidbody2D body = piece.GetComponent<Rigidbody2D>();
            BoxCollider2D collider = piece.GetComponent<BoxCollider2D>();
            SpriteRenderer visual = piece.GetComponent<SpriteRenderer>();
            var debris = new Rigidbody2D[debrisCount];

            for (int index = 0; index < debrisCount; index++)
            {
                var fragment = new GameObject($"Debris_{index + 1}");
                fragment.transform.SetParent(piece.transform, false);
                fragment.transform.localPosition = new Vector3((index - (debrisCount - 1) * 0.5f) * size.x / debrisCount, 0f, 0f);
                fragment.transform.localScale = new Vector3(Mathf.Max(0.12f, size.x / debrisCount * 0.8f), Mathf.Max(0.12f, size.y * 0.35f), 1f);
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

            var breakable = piece.AddComponent<BreakablePiece2D>();
            var serialized = new SerializedObject(breakable);
            serialized.FindProperty("breakThreshold").floatValue = threshold;
            serialized.FindProperty("intactBody").objectReferenceValue = body;
            serialized.FindProperty("intactCollider").objectReferenceValue = collider;
            serialized.FindProperty("intactVisual").objectReferenceValue = visual;
            SetArray(serialized.FindProperty("debrisBodies"), debris);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return breakable;
        }

        private static GameObject CreateVisualBody(Transform parent, string name, Vector2 position, Vector2 size, float mass, Sprite sprite, Color color, PhysicsMaterial2D material)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            var visual = gameObject.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.color = color;
            visual.sortingOrder = 4;
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.sharedMaterial = material;
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = mass;
            body.gravityScale = 1f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return gameObject;
        }

        private static Rigidbody2D CreateHeavy(Transform parent, Sprite sprite, PhysicsMaterial2D material)
        {
            GameObject heavy = CreateVisualBody(parent, "HeavyBlock", new Vector2(4.5f, -2.62f), new Vector2(1.1f, 0.8f), 5f, sprite, new Color32(69, 84, 105, 255), material);
            return heavy.GetComponent<Rigidbody2D>();
        }

        private static MouseTarget2D CreateMouse(Transform parent, string name, Vector2 position, Sprite sprite)
        {
            var mouse = new GameObject(name);
            mouse.transform.SetParent(parent, false);
            mouse.transform.position = position;
            mouse.transform.localScale = Vector3.one * 0.6f;
            var visual = mouse.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.color = new Color32(205, 207, 214, 255);
            visual.sortingOrder = 6;
            var collider = mouse.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            var body = mouse.AddComponent<Rigidbody2D>();
            body.mass = 0.55f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var target = mouse.AddComponent<MouseTarget2D>();
            var serialized = new SerializedObject(target);
            serialized.FindProperty("defeatThreshold").floatValue = 3f;
            serialized.FindProperty("crushMassThreshold").floatValue = 4f;
            serialized.FindProperty("targetBody").objectReferenceValue = body;
            serialized.FindProperty("targetCollider").objectReferenceValue = collider;
            serialized.FindProperty("targetVisual").objectReferenceValue = visual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }

        private static void ConfigureResolver(PhysicsResolver2D resolver, Rigidbody2D projectile, IReadOnlyList<Rigidbody2D> criticalBodies)
        {
            var serialized = new SerializedObject(resolver);
            serialized.FindProperty("projectileBody").objectReferenceValue = projectile;
            SetArray(serialized.FindProperty("criticalBodies"), criticalBodies);
            serialized.FindProperty("minimumObservationTime").floatValue = 0.3f;
            serialized.FindProperty("linearSettleSpeed").floatValue = 0.15f;
            serialized.FindProperty("angularSettleSpeed").floatValue = 10f;
            serialized.FindProperty("settleHoldDuration").floatValue = 0.35f;
            serialized.FindProperty("maximumResolutionTime").floatValue = 5f;
            serialized.FindProperty("horizontalBounds").vector2Value = new Vector2(-11f, 11f);
            serialized.FindProperty("projectileMinimumY").floatValue = -6.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAttempt(AttemptController attempt, SlingshotController2D slingshot, PhysicsResolver2D resolver, IReadOnlyList<MouseTarget2D> targets)
        {
            var serialized = new SerializedObject(attempt);
            serialized.FindProperty("slingshot").objectReferenceValue = slingshot;
            serialized.FindProperty("resolver").objectReferenceValue = resolver;
            SetArray(serialized.FindProperty("targets"), targets);
            serialized.FindProperty("maximumShots").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static Sprite CreateSprite(string path, bool circle)
        {
            if (!File.Exists(path))
            {
                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                float center = (size - 1) * 0.5f;
                float radiusSquared = center * center;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool visible = !circle || (x - center) * (x - center) + (y - center) * (y - center) <= radiusSquared;
                        pixels[y * size + x] = visible
                            ? new Color32(255, 255, 255, 255)
                            : new Color32(0, 0, 0, 0);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static PhysicsMaterial2D CreateMaterial(string name, float friction, float bounciness)
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

        private static void ValidateScene(Scene scene)
        {
            var failures = new List<string>();
            SlingshotController2D slingshot = FindOne<SlingshotController2D>(scene);
            Camera camera = FindOne<Camera>(scene);
            TrajectoryPreview2D trajectory = FindOne<TrajectoryPreview2D>(scene);
            Require(slingshot != null, "R1: Slingshot is missing.", failures);
            Require(camera != null && camera.orthographic && Mathf.Abs(camera.orthographicSize - 5.4f) < 0.001f, "R1: Camera changed.", failures);
            if (slingshot != null)
            {
                Require(Vector2.Distance(slingshot.AnchorPosition, new Vector2(-6f, -2.4f)) < 0.001f, "R1: Anchor changed.", failures);
                Require(Mathf.Abs(slingshot.MaximumDragDistance - 1.8f) < 0.001f, "R1: Max drag changed.", failures);
                Require(Mathf.Abs(slingshot.MinimumLaunchDistance - 0.2f) < 0.001f, "R1: Minimum drag changed.", failures);
                Require(Mathf.Abs(slingshot.LaunchSpeedPerUnit - 7f) < 0.001f, "R1: Launch speed changed.", failures);
                Require(slingshot.ProjectileCollider is CircleCollider2D circle && Mathf.Abs(circle.radius - 0.38f) < 0.001f, "R1: Cat collider changed.", failures);
            }

            Require(trajectory != null && trajectory.DotCount == 4 && trajectory.HasCompleteDotReferences, "R1: Trajectory must contain exactly four referenced dots.", failures);
            Require(trajectory != null && trajectory.transform.Find("SolidSegment") == null, "R1: Solid trajectory segment must not exist.", failures);
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(item => item.name == RootName);
            Require(root != null, "R2: Exactly one prototype root is required.", failures);
            if (root != null)
            {
                MouseTarget2D[] mice = root.GetComponentsInChildren<MouseTarget2D>(true);
                BreakablePiece2D[] breakables = root.GetComponentsInChildren<BreakablePiece2D>(true);
                Rigidbody2D heavy = root.GetComponentsInChildren<Rigidbody2D>(true).FirstOrDefault(body => body.name == "HeavyBlock");
                AttemptController attempt = root.GetComponentInChildren<AttemptController>(true);
                PhysicsResolver2D resolver = root.GetComponentInChildren<PhysicsResolver2D>(true);
                Require(mice.Length == 2, "R2: Exactly two mice are required.", failures);
                Require(mice.All(mouse => Mathf.Abs(mouse.DefeatThreshold - 3f) < 0.001f), "R2: Mouse normal impulse threshold changed.", failures);
                Require(mice.All(mouse => Mathf.Abs(mouse.CrushMassThreshold - 4f) < 0.001f), "R2: Mouse crush mass threshold is invalid.", failures);
                Require(breakables.Length == 4, "R2: Four prototype breakables are required.", failures);
                Require(heavy != null && heavy.bodyType == RigidbodyType2D.Dynamic && heavy.mass >= 4f, "R2: Heavy block configuration is invalid.", failures);
                Require(heavy != null && heavy.GetComponent<BreakablePiece2D>() == null, "R2: Heavy block must not be breakable.", failures);
                Require(attempt != null && attempt.Slingshot == slingshot && attempt.Resolver == resolver && attempt.Targets.Length == 2 && attempt.MaximumShots == 3, "R2: AttemptController references are invalid.", failures);
                Require(resolver != null && resolver.ProjectileBody == slingshot?.ProjectileBody && resolver.CriticalBodies.Contains(heavy), "R2: Resolver references are invalid.", failures);
                Rigidbody2D[] debris = root.GetComponentsInChildren<Rigidbody2D>(true).Where(body => body.name.StartsWith("Debris_", StringComparison.Ordinal)).ToArray();
                Require(resolver == null || debris.All(body => !resolver.CriticalBodies.Contains(body)), "R2: Debris must not be monitored.", failures);
                Require(root.GetComponentsInChildren<Rigidbody>(true).Length == 0 && root.GetComponentsInChildren<Collider>(true).Length == 0, "R2: 3D physics components are forbidden.", failures);
                ValidateGeometry(root, camera, slingshot, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("[CatapultCats R2] Apply and validation succeeded. Destruction prototype is ready for tests and manual QA.");
                return;
            }

            foreach (string failure in failures)
            {
                Debug.LogError("[CatapultCats R2] " + failure);
            }

            Debug.LogError($"[CatapultCats R2] Apply and validation failed with {failures.Count} issue(s).");
        }

        private static void ValidateGeometry(
            GameObject root,
            Camera camera,
            SlingshotController2D slingshot,
            ICollection<string> failures)
        {
            if (camera == null || slingshot == null)
            {
                return;
            }

            float halfWidth = camera.orthographicSize * 16f / 9f;
            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true)
                .Where(item => !item.name.StartsWith("Debris_", StringComparison.Ordinal))
                .ToArray();
            float dragClearance = slingshot.MaximumDragDistance + 0.38f + 0.15f;
            foreach (Collider2D collider in colliders)
            {
                Bounds bounds = collider.bounds;
                Require(bounds.min.x >= camera.transform.position.x - halfWidth && bounds.max.x <= camera.transform.position.x + halfWidth, $"GEOMETRY: {collider.name} is outside camera width.", failures);
                Require(bounds.min.y >= -4.91f, $"GEOMETRY: {collider.name} begins inside PrototypeGround.", failures);
                Vector2 anchor = slingshot.AnchorPosition;
                var closest = new Vector2(
                    Mathf.Clamp(anchor.x, bounds.min.x, bounds.max.x),
                    Mathf.Clamp(anchor.y, bounds.min.y, bounds.max.y));
                Require(Vector2.Distance(anchor, closest) > dragClearance, $"GEOMETRY: {collider.name} intrudes into the legal drag region.", failures);
            }

            for (int first = 0; first < colliders.Length; first++)
            {
                for (int second = first + 1; second < colliders.Length; second++)
                {
                    Bounds a = colliders[first].bounds;
                    Bounds b = colliders[second].bounds;
                    float overlapX = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                    float overlapY = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
                    Require(
                        overlapX <= 0.02f || overlapY <= 0.02f,
                        $"GEOMETRY: {colliders[first].name} overlaps {colliders[second].name}.",
                        failures);
                }
            }
        }

        private static void Require(bool condition, string message, ICollection<string> failures)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
