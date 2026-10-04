using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatapultCats.Launch;
using CatapultCats.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CatapultCats.Editor
{
    public static class R1SlingshotSetup
    {
        private const string GameplayScenePath = "Assets/_CatapultCats/Scenes/Gameplay.unity";
        private const string PrototypeRootName = "R1_Prototype";
        private const string CatSpritePath = "Assets/_CatapultCats/Art/Sprites/Characters/R1_CatPrototype.png";
        private const string DotSpritePath = "Assets/_CatapultCats/Art/Sprites/UI/R1_TrajectoryDot.png";
        private const string LineMaterialPath = "Assets/_CatapultCats/Art/R1_PrototypeLine.mat";

        private static readonly Vector2 LaunchAnchorPosition = new Vector2(-6f, -2.4f);
        private static readonly Vector2 GroundPosition = new Vector2(0f, -5.1f);
        private static readonly Vector2 GroundSize = new Vector2(20f, 0.4f);

        [MenuItem("CatapultCats/Apply R1 Slingshot Prototype")]
        public static void Apply()
        {
            try
            {
                Scene scene = OpenGameplayScene();
                Camera mainCamera = FindMainCamera(scene);
                if (mainCamera == null)
                {
                    throw new InvalidOperationException("Gameplay must contain one Main Camera before R1 setup.");
                }

                Sprite catSprite = CreateOrLoadCircleSprite(CatSpritePath, 64, 80f, new Color32(242, 145, 54, 255));
                Sprite dotSprite = CreateOrLoadCircleSprite(DotSpritePath, 32, 32f, new Color32(255, 241, 166, 255));
                Material lineMaterial = CreateOrLoadLineMaterial();

                RemoveExistingPrototypeRoots(scene);
                CreatePrototype(scene, mainCamera, catSprite, dotSprite, lineMaterial);

                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, GameplayScenePath))
                {
                    throw new InvalidOperationException("Unity could not save the Gameplay scene.");
                }

                ValidateScene(scene);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CatapultCats R1] Apply and validation failed: " + exception.Message);
            }
        }

        [MenuItem("CatapultCats/Validate R1 Slingshot Prototype")]
        public static void Validate()
        {
            try
            {
                ValidateScene(OpenGameplayScene());
            }
            catch (Exception exception)
            {
                Debug.LogError("[CatapultCats R1] Validation failed: " + exception.Message);
            }
        }

        private static Scene OpenGameplayScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) == null)
            {
                throw new FileNotFoundException("Gameplay scene is missing.", GameplayScenePath);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.path == GameplayScenePath && activeScene.isLoaded
                ? activeScene
                : EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        }

        private static Camera FindMainCamera(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(camera => camera.CompareTag("MainCamera"));
        }

        private static void RemoveExistingPrototypeRoots(Scene scene)
        {
            GameObject[] existingRoots = scene.GetRootGameObjects()
                .Where(root => root.name == PrototypeRootName)
                .ToArray();

            foreach (GameObject root in existingRoots)
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreatePrototype(
            Scene scene,
            Camera mainCamera,
            Sprite catSprite,
            Sprite dotSprite,
            Material lineMaterial)
        {
            var prototypeRoot = new GameObject(PrototypeRootName);
            SceneManager.MoveGameObjectToScene(prototypeRoot, scene);

            GameObject slingshot = CreateChild(prototypeRoot.transform, "Slingshot");
            Transform launchAnchor = CreateChild(slingshot.transform, "LaunchAnchor").transform;
            launchAnchor.position = LaunchAnchorPosition;

            Transform leftBandAnchor = CreateChild(slingshot.transform, "LeftBandAnchor").transform;
            leftBandAnchor.position = new Vector3(-6.38f, -2.02f, 0f);
            Transform rightBandAnchor = CreateChild(slingshot.transform, "RightBandAnchor").transform;
            rightBandAnchor.position = new Vector3(-5.62f, -2.02f, 0f);

            CreateWoodenFrame(slingshot.transform, leftBandAnchor, rightBandAnchor, lineMaterial);
            LineRenderer leftBand = CreateLine(
                slingshot.transform,
                "LeftElastic",
                lineMaterial,
                new Color32(74, 43, 31, 255),
                0.075f,
                5);
            LineRenderer rightBand = CreateLine(
                slingshot.transform,
                "RightElastic",
                lineMaterial,
                new Color32(74, 43, 31, 255),
                0.075f,
                7);
            SetLinePositions(leftBand, leftBandAnchor.position, LaunchAnchorPosition);
            SetLinePositions(rightBand, rightBandAnchor.position, LaunchAnchorPosition);

            GameObject projectile = CreateChild(prototypeRoot.transform, "CatProjectile");
            projectile.transform.position = LaunchAnchorPosition;
            var spriteRenderer = projectile.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = catSprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 6;

            var projectileBody = projectile.AddComponent<Rigidbody2D>();
            projectileBody.bodyType = RigidbodyType2D.Kinematic;
            projectileBody.mass = 1f;
            projectileBody.gravityScale = 1f;
            projectileBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            projectileBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            projectileBody.linearVelocity = Vector2.zero;
            projectileBody.angularVelocity = 0f;

            var projectileCollider = projectile.AddComponent<CircleCollider2D>();
            projectileCollider.radius = 0.38f;

            var controller = slingshot.AddComponent<SlingshotController2D>();
            ConfigureController(controller, launchAnchor, projectileBody, projectileCollider, mainCamera);

            var slingshotVisual = slingshot.AddComponent<SlingshotVisual2D>();
            ConfigureSlingshotVisual(
                slingshotVisual,
                controller,
                leftBandAnchor,
                rightBandAnchor,
                leftBand,
                rightBand);

            GameObject trajectory = CreateChild(prototypeRoot.transform, "TrajectoryPreview");
            LineRenderer solidSegment = CreateLine(
                trajectory.transform,
                "SolidSegment",
                lineMaterial,
                new Color32(255, 241, 166, 255),
                0.055f,
                3);
            solidSegment.enabled = false;

            Transform dotsRoot = CreateChild(trajectory.transform, "Dots").transform;
            var dots = new SpriteRenderer[5];
            for (int index = 0; index < dots.Length; index++)
            {
                GameObject dotObject = CreateChild(dotsRoot, $"Dot_{index + 1}");
                dotObject.transform.localScale = Vector3.one * 0.13f;
                dots[index] = dotObject.AddComponent<SpriteRenderer>();
                dots[index].sprite = dotSprite;
                dots[index].sortingOrder = 3;
                dots[index].enabled = false;
            }

            var trajectoryPreview = trajectory.AddComponent<TrajectoryPreview2D>();
            ConfigureTrajectoryPreview(trajectoryPreview, controller, projectileBody, solidSegment, dots);

            GameObject ground = CreateChild(prototypeRoot.transform, "PrototypeGround");
            ground.transform.position = GroundPosition;
            var groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = GroundSize;

            LineRenderer groundVisual = CreateLine(
                ground.transform,
                "GroundVisual",
                lineMaterial,
                new Color32(91, 181, 80, 255),
                GroundSize.y,
                1);
            SetLinePositions(
                groundVisual,
                new Vector2(-GroundSize.x * 0.5f, GroundPosition.y),
                new Vector2(GroundSize.x * 0.5f, GroundPosition.y));
        }

        private static void CreateWoodenFrame(
            Transform parent,
            Transform leftBandAnchor,
            Transform rightBandAnchor,
            Material material)
        {
            var woodColor = new Color32(137, 77, 39, 255);
            LineRenderer post = CreateLine(parent, "WoodPost", material, woodColor, 0.28f, 2);
            SetLinePositions(post, new Vector2(-6f, -4.7f), new Vector2(-6f, -2.55f));

            LineRenderer leftArm = CreateLine(parent, "WoodLeftArm", material, woodColor, 0.24f, 2);
            SetLinePositions(leftArm, new Vector2(-6f, -2.8f), leftBandAnchor.position);

            LineRenderer rightArm = CreateLine(parent, "WoodRightArm", material, woodColor, 0.24f, 2);
            SetLinePositions(rightArm, new Vector2(-6f, -2.8f), rightBandAnchor.position);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static LineRenderer CreateLine(
            Transform parent,
            string name,
            Material material,
            Color color,
            float width,
            int sortingOrder)
        {
            GameObject lineObject = CreateChild(parent, name);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 6;
            line.numCornerVertices = 3;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private static void SetLinePositions(LineRenderer line, Vector2 start, Vector2 end)
        {
            line.SetPosition(0, new Vector3(start.x, start.y, 0f));
            line.SetPosition(1, new Vector3(end.x, end.y, 0f));
        }

        private static void ConfigureController(
            SlingshotController2D controller,
            Transform launchAnchor,
            Rigidbody2D projectileBody,
            Collider2D projectileCollider,
            Camera mainCamera)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("launchAnchor").objectReferenceValue = launchAnchor;
            serialized.FindProperty("projectileBody").objectReferenceValue = projectileBody;
            serialized.FindProperty("projectileCollider").objectReferenceValue = projectileCollider;
            serialized.FindProperty("inputCamera").objectReferenceValue = mainCamera;
            serialized.FindProperty("maxDragDistance").floatValue = 1.8f;
            serialized.FindProperty("minimumLaunchDistance").floatValue = 0.2f;
            serialized.FindProperty("launchSpeedPerUnit").floatValue = 7f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSlingshotVisual(
            SlingshotVisual2D visual,
            SlingshotController2D controller,
            Transform leftBandAnchor,
            Transform rightBandAnchor,
            LineRenderer leftBand,
            LineRenderer rightBand)
        {
            var serialized = new SerializedObject(visual);
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.FindProperty("leftBandAnchor").objectReferenceValue = leftBandAnchor;
            serialized.FindProperty("rightBandAnchor").objectReferenceValue = rightBandAnchor;
            serialized.FindProperty("leftBand").objectReferenceValue = leftBand;
            serialized.FindProperty("rightBand").objectReferenceValue = rightBand;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTrajectoryPreview(
            TrajectoryPreview2D preview,
            SlingshotController2D controller,
            Rigidbody2D projectileBody,
            LineRenderer solidSegment,
            IReadOnlyList<SpriteRenderer> dots)
        {
            var serialized = new SerializedObject(preview);
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.FindProperty("projectileBody").objectReferenceValue = projectileBody;
            serialized.FindProperty("solidSegment").objectReferenceValue = solidSegment;

            SerializedProperty dotsProperty = serialized.FindProperty("dots");
            dotsProperty.arraySize = dots.Count;
            for (int index = 0; index < dots.Count; index++)
            {
                dotsProperty.GetArrayElementAtIndex(index).objectReferenceValue = dots[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite CreateOrLoadCircleSprite(string assetPath, int size, float pixelsPerUnit, Color32 color)
        {
            if (!File.Exists(assetPath))
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                float center = (size - 1) * 0.5f;
                float radiusSquared = (center - 0.5f) * (center - 0.5f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float offsetX = x - center;
                        float offsetY = y - center;
                        pixels[y * size + x] = offsetX * offsetX + offsetY * offsetY <= radiusSquared
                            ? color
                            : new Color32(0, 0, 0, 0);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not import prototype sprite '{assetPath}'.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Material CreateOrLoadLineMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("Built-in Sprites/Default shader is unavailable.");
            }

            material = new Material(shader)
            {
                name = "R1 Prototype Line"
            };
            AssetDatabase.CreateAsset(material, LineMaterialPath);
            return material;
        }

        private static bool ValidateScene(Scene scene)
        {
            var failures = new List<string>();
            GameObject[] roots = scene.GetRootGameObjects();
            Camera[] cameras = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
            Require(cameras.Length == 1, "SCENE: Gameplay must contain exactly one Camera.", failures);

            Camera camera = cameras.Length == 1 ? cameras[0] : null;
            if (camera != null)
            {
                Require(camera.CompareTag("MainCamera"), "SCENE: Camera must use the MainCamera tag.", failures);
                Require(camera.orthographic, "SCENE: Main Camera must remain orthographic.", failures);
                Require(Mathf.Abs(camera.orthographicSize - 5.4f) < 0.001f, "SCENE: Camera size must remain 5.4.", failures);
            }

            GameObject[] prototypeRoots = roots.Where(root => root.name == PrototypeRootName).ToArray();
            Require(prototypeRoots.Length == 1, "SCENE: Exactly one R1_Prototype root must exist.", failures);
            if (prototypeRoots.Length != 1)
            {
                return FinishValidation(failures);
            }

            GameObject prototype = prototypeRoots[0];
            Transform[] transforms = prototype.GetComponentsInChildren<Transform>(true);
            Transform[] projectiles = transforms.Where(item => item.name == "CatProjectile").ToArray();
            Require(projectiles.Length == 1, "PROJECTILE: Exactly one CatProjectile must exist.", failures);

            Rigidbody2D projectileBody = projectiles.Length == 1 ? projectiles[0].GetComponent<Rigidbody2D>() : null;
            CircleCollider2D projectileCollider = projectiles.Length == 1 ? projectiles[0].GetComponent<CircleCollider2D>() : null;
            Require(projectileBody != null, "PROJECTILE: Rigidbody2D is missing.", failures);
            Require(projectileCollider != null, "PROJECTILE: CircleCollider2D is missing.", failures);
            Require(prototype.GetComponentsInChildren<Rigidbody>(true).Length == 0, "PROJECTILE: 3D Rigidbody is forbidden.", failures);
            Require(prototype.GetComponentsInChildren<Collider>(true).Length == 0, "PROJECTILE: 3D Collider is forbidden.", failures);

            if (projectileBody != null)
            {
                Require(projectileBody.bodyType == RigidbodyType2D.Kinematic, "PROJECTILE: Initial body type must be Kinematic.", failures);
                Require(projectileBody.collisionDetectionMode == CollisionDetectionMode2D.Continuous, "PROJECTILE: Collision detection must be Continuous.", failures);
                Require(projectileBody.interpolation == RigidbodyInterpolation2D.Interpolate, "PROJECTILE: Interpolation must be Interpolate.", failures);
            }

            if (projectileCollider != null)
            {
                Require(Mathf.Abs(projectileCollider.radius - 0.38f) < 0.001f, "PROJECTILE: Collider radius must be 0.38.", failures);
            }

            SlingshotController2D[] controllers = prototype.GetComponentsInChildren<SlingshotController2D>(true);
            Require(controllers.Length == 1, "SLINGSHOT: Exactly one controller must exist.", failures);
            SlingshotController2D controller = controllers.Length == 1 ? controllers[0] : null;
            if (controller != null)
            {
                Require(controller.LaunchAnchor != null, "SLINGSHOT: LaunchAnchor reference is missing.", failures);
                Require(controller.ProjectileBody == projectileBody, "SLINGSHOT: Projectile Rigidbody2D reference is invalid.", failures);
                Require(controller.ProjectileCollider == projectileCollider, "SLINGSHOT: Projectile Collider2D reference is invalid.", failures);
                Require(controller.InputCamera == camera, "SLINGSHOT: Camera reference is invalid.", failures);
            }

            SlingshotVisual2D[] visuals = prototype.GetComponentsInChildren<SlingshotVisual2D>(true);
            Require(visuals.Length == 1, "SLINGSHOT: Exactly one presentation component must exist.", failures);
            if (visuals.Length == 1)
            {
                Require(visuals[0].Controller == controller, "SLINGSHOT: Presentation controller reference is invalid.", failures);
                Require(visuals[0].LeftBandAnchor != null, "SLINGSHOT: LeftBandAnchor is missing.", failures);
                Require(visuals[0].RightBandAnchor != null, "SLINGSHOT: RightBandAnchor is missing.", failures);
                Require(visuals[0].LeftBand != null && visuals[0].RightBand != null, "SLINGSHOT: Elastic line references are missing.", failures);
            }

            TrajectoryPreview2D[] previews = prototype.GetComponentsInChildren<TrajectoryPreview2D>(true);
            Require(previews.Length == 1, "TRAJECTORY: Exactly one preview component must exist.", failures);
            if (previews.Length == 1)
            {
                Require(previews[0].Controller == controller, "TRAJECTORY: Controller reference is invalid.", failures);
                Require(previews[0].ProjectileBody == projectileBody, "TRAJECTORY: Rigidbody2D reference is invalid.", failures);
                Require(previews[0].SolidSegment != null, "TRAJECTORY: Solid segment is missing.", failures);
                Require(previews[0].DotCount == 5, "TRAJECTORY: Exactly five dots are required.", failures);
                Require(previews[0].HasCompleteDotReferences, "TRAJECTORY: One or more dot references are missing.", failures);
            }

            Transform[] groundTransforms = transforms.Where(item => item.name == "PrototypeGround").ToArray();
            BoxCollider2D ground = groundTransforms.Length == 1 ? groundTransforms[0].GetComponent<BoxCollider2D>() : null;
            Require(ground != null, "GEOMETRY: PrototypeGround BoxCollider2D is missing.", failures);

            if (controller != null && projectileCollider != null && camera != null)
            {
                ValidateDragVisibility(controller, projectileCollider, camera, failures);
                if (ground != null)
                {
                    ValidateGroundClearance(controller, projectileCollider, ground, failures);
                }
            }

            ValidateScope(prototype, failures);
            return FinishValidation(failures);
        }

        private static void ValidateDragVisibility(
            SlingshotController2D controller,
            CircleCollider2D projectileCollider,
            Camera camera,
            ICollection<string> failures)
        {
            const float margin = 0.1f;
            float reach = controller.MaximumDragDistance + projectileCollider.radius + margin;
            Vector2 anchor = controller.AnchorPosition;
            Vector2 cameraCenter = camera.transform.position;
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * (16f / 9f);

            bool inside = anchor.x - reach >= cameraCenter.x - halfWidth &&
                anchor.x + reach <= cameraCenter.x + halfWidth &&
                anchor.y - reach >= cameraCenter.y - halfHeight &&
                anchor.y + reach <= cameraCenter.y + halfHeight;
            Require(inside, "GEOMETRY: Full drag disk plus projectile and margin must remain inside the 16:9 camera view.", failures);
        }

        private static void ValidateGroundClearance(
            SlingshotController2D controller,
            CircleCollider2D projectileCollider,
            BoxCollider2D ground,
            ICollection<string> failures)
        {
            const float safetyMargin = 0.15f;
            float requiredClearance = controller.MaximumDragDistance + projectileCollider.radius + safetyMargin;
            Vector2 anchor = controller.AnchorPosition;
            Bounds bounds = ground.bounds;
            var closestPoint = new Vector2(
                Mathf.Clamp(anchor.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(anchor.y, bounds.min.y, bounds.max.y));

            Require(
                Vector2.Distance(anchor, closestPoint) > requiredClearance,
                "GEOMETRY: Legal drag disk plus projectile and safety margin overlaps PrototypeGround.",
                failures);
        }

        private static void ValidateScope(GameObject prototype, ICollection<string> failures)
        {
            string[] forbiddenAssetNames =
            {
                "MouseTarget",
                "BreakablePiece",
                "LevelDefinition",
                "PieceCatalog",
                "LevelLoader",
                "LevelEditorWindow"
            };

            string[] assets = Directory.GetFiles("Assets/_CatapultCats", "*", SearchOption.AllDirectories);
            foreach (string forbiddenName in forbiddenAssetNames)
            {
                Require(
                    assets.All(path => !string.Equals(Path.GetFileNameWithoutExtension(path), forbiddenName, StringComparison.OrdinalIgnoreCase)),
                    $"SCOPE: Forbidden R2/R3 asset exists: {forbiddenName}.",
                    failures);
            }

            bool hasHud = prototype.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0);
            Require(!hasHud, "SCOPE: HUD content is outside R1.", failures);

            string[] levelAssets = Directory.GetFiles("Assets/_CatapultCats/Data/Levels", "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Require(levelAssets.Length == 0, "SCOPE: Level data is outside R1.", failures);
        }

        private static bool FinishValidation(IReadOnlyCollection<string> failures)
        {
            if (failures.Count == 0)
            {
                Debug.Log("[CatapultCats R1] Apply and validation succeeded. Slingshot prototype is ready for tests and manual QA.");
                return true;
            }

            foreach (string failure in failures)
            {
                Debug.LogError("[CatapultCats R1] " + failure);
            }

            Debug.LogError($"[CatapultCats R1] Apply and validation failed with {failures.Count} issue(s).");
            return false;
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
