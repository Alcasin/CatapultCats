using System;
using System.Collections.Generic;
using System.Linq;
using CatapultCats.Core;
using CatapultCats.Launch;
using CatapultCats.Levels;
using CatapultCats.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CatapultCats.Editor
{
    // Initial setup only. Existing R4 presentation is an authored baseline, never a rebuild target.
    public static class R4PresentationSetup
    {
        private const string SequencePath = "Assets/_CatapultCats/Data/PlayableLevels.asset";
        private const string GameplayPath = "Assets/_CatapultCats/Scenes/Gameplay.unity";
        private static readonly string[] LevelNames =
        {
            "Level01_FirstShot", "Level02_Collapse", "Level03_GlassHouse", "Level04_Ricochet", "Level05_MouseFortress"
        };

        [MenuItem("CatapultCats/Apply R4 Presentation + Flow")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatapultCats R4] Apply failed: exit Play Mode before applying setup.");
                return;
            }
            try
            {
                // Guard before save prompts, generated art, prefab writes, or hierarchy replacement.
                if (HasAuthoredPresentation())
                {
                    Debug.LogWarning("[CatapultCats R4] Apply skipped: Gameplay already contains authored R4 presentation. " +
                        "Its scene, sprites, prefabs, and environment were not changed. Do not reapply the generator; use normal Gameplay.");
                    return;
                }
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[CatapultCats R4] Apply failed: scene-save prompt cancelled; setup was not applied.");
                    return;
                }
                LevelDefinition[] levels = LevelNames.Select(name => Require<LevelDefinition>(
                    $"Assets/_CatapultCats/Data/Levels/{name}.asset")).ToArray();
                Dictionary<string, string> authoredLevels = CaptureAuthoredLevels(levels);
                PieceCatalog catalog = Require<PieceCatalog>(LevelEditorWindow.CatalogPath);
                IReadOnlyList<string> existingPrefabErrors = LevelEditorValidation.ValidateCatalogPrefabs(catalog);
                if (existingPrefabErrors.Count > 0)
                    throw new InvalidOperationException(string.Join("; ", existingPrefabErrors));
                foreach (LevelDefinition level in levels)
                {
                    LevelValidationResult validation = LevelValidation.Validate(level, catalog);
                    if (!validation.IsValid) throw new InvalidOperationException(level.LevelId + ": " + string.Join("; ", validation.Errors));
                }

                // Resolve and check the older R3 scene before creating presentation or changing prefabs.
                Scene scene = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
                LevelBootstrap bootstrap = Find<LevelBootstrap>(scene);
                SlingshotController2D slingshot = Find<SlingshotController2D>(scene);
                AttemptController attempt = Find<AttemptController>(scene);
                ValidateGameplayReferences(scene, bootstrap, slingshot, attempt, catalog);
                LevelFlowController existingFlow = Find<LevelFlowController>(scene);
                if (existingFlow != null && existingFlow.gameObject != bootstrap.gameObject)
                    throw new InvalidOperationException("The R4 flow manager must be on LevelBootstrap, not another object.");

                EnsureFolder(R4SpriteArt.Folder);
                var sprites = new Dictionary<string, Sprite>();
                foreach (string name in new[] { "Cat", "Mouse", "WoodBeam", "WoodBlock", "GlassBeam", "GlassBlock",
                    "HeavyBlock", "Ramp", "Cloud", "Bush", "Dot", "Spark", "Panel", "Square" })
                    sprites[name] = R4SpriteArt.Create(name);

                LevelSequence sequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(SequencePath);
                if (sequence == null)
                {
                    sequence = ScriptableObject.CreateInstance<LevelSequence>();
                    sequence.Configure(levels);
                    AssetDatabase.CreateAsset(sequence, SequencePath);
                }
                // Keep an existing Inspector-edited order on subsequent Apply runs.
                if (sequence.Count == 0 || sequence.Levels.Any(level => level == null) ||
                    sequence.Levels.Distinct().Count() != sequence.Count)
                    throw new InvalidOperationException("PlayableLevels must contain a nonempty list of unique LevelDefinitions.");
                foreach (LevelDefinition level in sequence.Levels)
                {
                    LevelValidationResult validation = LevelValidation.Validate(level, catalog);
                    if (!validation.IsValid) throw new InvalidOperationException(level.LevelId + ": " + string.Join("; ", validation.Errors));
                }
                UpdatePrefabs(catalog, sprites);
                AssetDatabase.SaveAssets();

                LevelFlowController flow = GetOrAdd<LevelFlowController>(bootstrap.gameObject);
                Configure(flow, serialized =>
                {
                    serialized.FindProperty("sequence").objectReferenceValue = sequence;
                    serialized.FindProperty("loader").objectReferenceValue = bootstrap.Loader;
                    serialized.FindProperty("attempt").objectReferenceValue = attempt;
                });

                Transform runtime = slingshot.transform.root;
                RemoveNamedChildren(runtime, "R4Presentation");
                var root = new GameObject("R4Presentation");
                root.transform.SetParent(runtime, false);
                GameplayHud hud = root.AddComponent<GameplayHud>();
                Configure(hud, serialized =>
                {
                    serialized.FindProperty("flow").objectReferenceValue = flow;
                    serialized.FindProperty("catIcon").objectReferenceValue = sprites["Cat"];
                    serialized.FindProperty("panelSprite").objectReferenceValue = sprites["Panel"];
                });
                CreateBackyard(root.transform, sprites);
                StyleSlingshot(slingshot, sprites);
                Camera camera = slingshot.InputCamera;
                camera.backgroundColor = new Color32(180, 222, 233, 255);
                GetOrAdd<GameplayCameraFraming>(camera.gameObject);
                Transform ground = runtime.Find("Ground");
                if (ground != null)
                {
                    foreach (SpriteRenderer visual in ground.GetComponentsInChildren<SpriteRenderer>()) visual.enabled = false;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                ValidateScene(scene);
                VerifyAuthoredLevels(authoredLevels);
                if (!EditorSceneManager.SaveScene(scene, GameplayPath)) throw new InvalidOperationException("Could not save Gameplay.");
                AssetDatabase.SaveAssets();
                // Reacquire everything after reopening: also verify that wiring actually survived serialization.
                ValidateScene(EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single));
                VerifyAuthoredLevels(authoredLevels);
                Debug.Log("[CatapultCats R4] Apply succeeded: saved Gameplay validated; one cat trail, HUD/flow wiring, and all five authored levels preserved.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CatapultCats R4] Apply failed: " + exception);
            }
        }

        private static bool HasAuthoredPresentation()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene loaded = SceneManager.GetSceneAt(index);
                if (loaded.isLoaded && loaded.path == GameplayPath && ContainsPresentation(loaded)) return true;
            }

            // Read the saved scene without replacing the user's current scene or saving any changes.
            Scene preview = EditorSceneManager.OpenPreviewScene(GameplayPath);
            try { return ContainsPresentation(preview); }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static bool ContainsPresentation(Scene scene) => scene.GetRootGameObjects().Any(root =>
            root.GetComponentsInChildren<GameplayHud>(true).Length > 0 ||
            root.GetComponentsInChildren<Transform>(true).Any(child => child.name == "R4Presentation"));

        [MenuItem("CatapultCats/Validate R4 Presentation + Flow")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                ValidateScene(EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single));
                Debug.Log("[CatapultCats R4] Presentation/flow validation passed.");
            }
            catch (Exception exception) { Debug.LogError("[CatapultCats R4] " + exception.Message); }
        }

        private static void UpdatePrefabs(PieceCatalog catalog, IReadOnlyDictionary<string, Sprite> sprites)
        {
            foreach (PieceCatalog.Entry entry in catalog.Entries)
            {
                string path = AssetDatabase.GetAssetPath(entry.Prefab);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Vector2 size = LevelValidation.GetPieceSize(entry.PieceType);
                    Sprite sprite = sprites[entry.PieceType.ToString()];
                    Transform visualTransform = root.transform.Find("Visual");
                    SpriteRenderer visual = visualTransform == null ? null : visualTransform.GetComponent<SpriteRenderer>();
                    if (visual == null) throw new InvalidOperationException(entry.PieceType + " has no fixed-size Visual child. Apply R3 first.");
                    SetVisual(visual, sprite, entry.PieceType == LevelPieceType.Mouse ? Vector2.one * 0.72f : size);
                    foreach (Transform child in root.transform)
                    {
                        if (!child.name.StartsWith("Debris_", StringComparison.Ordinal)) continue;
                        SpriteRenderer fragment = child.GetComponent<SpriteRenderer>();
                        if (fragment == null) continue;
                        // Keep authored debris physics/scale. Sized drawing changes only the SpriteRenderer.
                        fragment.sprite = sprite;
                        fragment.drawMode = SpriteDrawMode.Sliced;
                        fragment.size = Vector2.one;
                        fragment.color = Color.white;
                    }
                    if (root.GetComponent<CatapultCats.Physics.BreakablePiece2D>() != null)
                    {
                        BreakBurst2D burst = GetOrAdd<BreakBurst2D>(root);
                        Configure(burst, serialized =>
                        {
                            serialized.FindProperty("spark").objectReferenceValue = sprites["Spark"];
                            serialized.FindProperty("tint").colorValue = entry.PieceType == LevelPieceType.WoodBeam ||
                                entry.PieceType == LevelPieceType.WoodBlock
                                    ? new Color32(246, 198, 119, 255) : new Color32(206, 255, 249, 255);
                        });
                    }
                    if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                        throw new InvalidOperationException("Could not save presentation prefab: " + path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        private static void StyleSlingshot(SlingshotController2D slingshot, IReadOnlyDictionary<string, Sprite> sprites)
        {
            GameObject cat = slingshot.ProjectileBody.gameObject;
            SpriteRenderer oldCat = cat.GetComponent<SpriteRenderer>();
            if (oldCat != null) oldCat.enabled = false;
            RemoveNamedChildren(cat.transform, "R4CatVisual");
            Draw("R4CatVisual", cat.transform, sprites["Cat"], Vector2.zero, Vector2.one * 0.84f, Color.white, 7);
            TrailRenderer trail = GetOrAdd<TrailRenderer>(cat);
            trail.enabled = true;
            trail.autodestruct = false;
            trail.sharedMaterial = Require<Material>("Assets/_CatapultCats/Art/R1_PrototypeLine.mat");
            trail.time = 0.14f;
            trail.minVertexDistance = 0.05f;
            trail.startWidth = 0.075f;
            trail.endWidth = 0f;
            trail.startColor = new Color(1f, 0.94f, 0.76f, 0.5f);
            trail.endColor = new Color(1f, 0.94f, 0.76f, 0f);
            trail.numCapVertices = 4;
            trail.sortingOrder = 5;
            trail.emitting = false;
            trail.Clear();
            CatFlightVisual2D flight = GetOrAdd<CatFlightVisual2D>(cat);
            Configure(flight, serialized =>
            {
                serialized.FindProperty("controller").objectReferenceValue = slingshot;
                serialized.FindProperty("trail").objectReferenceValue = trail;
            });
            foreach (LineRenderer line in slingshot.GetComponentsInChildren<LineRenderer>(true))
            {
                bool band = line.name.Contains("Elastic");
                line.startColor = line.endColor = band ? new Color32(82, 60, 56, 255) : new Color32(171, 110, 64, 255);
                line.numCapVertices = 6;
                line.numCornerVertices = 4;
            }
            TrajectoryPreview2D preview = slingshot.transform.root.GetComponentInChildren<TrajectoryPreview2D>(true);
            if (preview == null) throw new InvalidOperationException("Trajectory preview is missing.");
            SpriteRenderer[] dots = preview.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.name.StartsWith("Dot_", StringComparison.Ordinal))
                .OrderBy(renderer => renderer.name, StringComparer.Ordinal).ToArray();
            if (dots.Length < 4) throw new InvalidOperationException("Four trajectory dots are required.");
            for (int i = 0; i < dots.Length; i++)
            {
                if (i >= 4) { Object.DestroyImmediate(dots[i].gameObject); continue; }
                dots[i].sprite = sprites["Dot"];
                dots[i].color = new Color(1f, 1f, 1f, 1f - i * 0.12f);
                dots[i].enabled = false;
            }
            // Remove only the renderer; never destroy the preview component or its dot references.
            foreach (LineRenderer line in preview.GetComponentsInChildren<LineRenderer>(true)) Object.DestroyImmediate(line);
            Configure(preview, serialized =>
            {
                SerializedProperty references = serialized.FindProperty("dots");
                references.arraySize = 4;
                for (int i = 0; i < 4; i++) references.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
            });
        }

        private static void CreateBackyard(Transform parent, IReadOnlyDictionary<string, Sprite> sprites)
        {
            var decor = new GameObject("Backyard (decoration only)");
            decor.transform.SetParent(parent, false);
            Sprite square = sprites["Square"];
            Draw("Warm horizon", decor.transform, square, new Vector2(0f, -3.8f), new Vector2(60f, 1.9f),
                new Color32(219, 237, 207, 255), -60);
            Draw("Lawn", decor.transform, square, new Vector2(0f, -4.55f), new Vector2(60f, 0.65f),
                new Color32(173, 211, 116, 255), -55);
            for (int i = 0; i < 7; i++)
                Draw("Bush " + i, decor.transform, sprites["Bush"], new Vector2(-12f + i * 4f, -4.05f),
                    new Vector2(3.7f, 1.4f), new Color(1f, 1f, 1f, 0.55f), -58);
            for (int i = 0; i < 4; i++)
                Draw("Cloud " + i, decor.transform, sprites["Cloud"], new Vector2(-8f + i * 5.2f, 2.1f + (i % 2) * 1.3f),
                    new Vector2(2.4f + (i % 2) * 0.7f, 1.35f), new Color(1f, 1f, 1f, 0.85f), -65);
            Draw("Soil", decor.transform, square, new Vector2(0f, LevelValidation.GroundTopY - 3f),
                new Vector2(60f, 6f), new Color32(175, 127, 92, 255), -40);
            Draw("Grass edge", decor.transform, square, new Vector2(0f, LevelValidation.GroundTopY - 0.045f),
                new Vector2(60f, 0.09f), new Color32(85, 145, 82, 255), -39);
            for (int i = 0; i < 15; i++)
            {
                Draw("Lawn tuft " + i, decor.transform, sprites["Bush"],
                    new Vector2(-10f + i * 1.45f, LevelValidation.GroundTopY + 0.015f),
                    new Vector2(0.21f, 0.08f), new Color32(99, 154, 79, 255), -38);
            }
        }

        private static void Draw(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color tint, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            SetVisual(renderer, sprite, size);
            renderer.color = tint;
        }

        private static void SetVisual(SpriteRenderer visual, Sprite sprite, Vector2 size)
        {
            visual.sprite = sprite;
            visual.color = Color.white;
            visual.drawMode = SpriteDrawMode.Simple;
            Vector2 native = sprite.bounds.size;
            visual.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        }

        private static void ValidateScene(Scene scene)
        {
            LevelBootstrap bootstrap = Find<LevelBootstrap>(scene);
            LevelFlowController flow = Find<LevelFlowController>(scene);
            GameplayHud hud = Find<GameplayHud>(scene);
            SlingshotController2D slingshot = Find<SlingshotController2D>(scene);
            AttemptController attempt = Find<AttemptController>(scene);
            if (bootstrap == null || flow == null || hud == null || flow.Sequence == null || flow.Sequence.Count == 0)
                throw new InvalidOperationException("R4 scene/sequence references are incomplete.");
            ValidateGameplayReferences(scene, bootstrap, slingshot, attempt, bootstrap.PieceCatalog);
            if (flow.Sequence.Levels.Any(level => level == null) ||
                flow.Sequence.Levels.Distinct().Count() != flow.Sequence.Count)
                throw new InvalidOperationException("PlayableLevels must contain unique, non-null levels.");
            if (flow.gameObject != bootstrap.gameObject || flow.Attempt != attempt ||
                hud.transform.parent != slingshot.transform.root || hud.name != "R4Presentation")
                throw new InvalidOperationException("R4 HUD/loader/attempt wiring is incomplete.");
            VerifyReference(flow, "loader", bootstrap.Loader);
            VerifyReference(hud, "flow", flow);
            VerifyReference(hud, "catIcon", Require<Sprite>(R4SpriteArt.Folder + "/Cat.asset"));
            VerifyReference(hud, "panelSprite", Require<Sprite>(R4SpriteArt.Folder + "/Panel.asset"));
            if (flow.Sequence != Require<LevelSequence>(SequencePath))
                throw new InvalidOperationException("Flow must reference the saved PlayableLevels asset.");
            foreach (string name in LevelNames)
            {
                LevelDefinition authored = Require<LevelDefinition>($"Assets/_CatapultCats/Data/Levels/{name}.asset");
                if (!flow.Sequence.Levels.Contains(authored))
                    throw new InvalidOperationException("PlayableLevels is missing authored level " + name);
            }

            GameObject cat = slingshot.ProjectileBody.gameObject;
            TrailRenderer trail = Find<TrailRenderer>(scene);
            CatFlightVisual2D flight = Find<CatFlightVisual2D>(scene);
            if (trail == null || trail.gameObject != cat || !trail.enabled || trail.autodestruct ||
                trail.sharedMaterial == null || flight == null || flight.gameObject != cat ||
                flight.Controller != slingshot || flight.Trail != trail)
                throw new InvalidOperationException("Exactly one configured cat TrailRenderer and valid CatFlightVisual2D references are required.");
            GameplayCameraFraming framing = Find<GameplayCameraFraming>(scene);
            if (framing == null || framing.gameObject != slingshot.InputCamera.gameObject)
                throw new InvalidOperationException("R4 camera framing component is missing.");
            foreach (LevelDefinition level in flow.Sequence.Levels)
            {
                LevelValidationResult result = LevelValidation.Validate(level, bootstrap.PieceCatalog);
                if (!result.IsValid) throw new InvalidOperationException(string.Join("; ", result.Errors));
            }
            IReadOnlyList<string> prefabErrors = LevelEditorValidation.ValidateCatalogPrefabs(bootstrap.PieceCatalog);
            if (prefabErrors.Count > 0) throw new InvalidOperationException(string.Join("; ", prefabErrors));
            TrajectoryPreview2D preview = Find<TrajectoryPreview2D>(scene);
            if (preview == null || preview.DotCount != 4 || !preview.HasCompleteDotReferences ||
                preview.GetComponentsInChildren<LineRenderer>(true).Length != 0)
                throw new InvalidOperationException("Trajectory must have exactly four dots and no solid line.");
            if (preview.Controller != slingshot || preview.ProjectileBody != slingshot.ProjectileBody)
                throw new InvalidOperationException("Trajectory scene references are incomplete.");
            if (EditorBuildSettings.scenes.Length != 1 || !EditorBuildSettings.scenes[0].enabled ||
                EditorBuildSettings.scenes[0].path != GameplayPath)
                throw new InvalidOperationException("Build Settings must keep the single Gameplay scene.");
        }

        private static void ValidateGameplayReferences(Scene scene, LevelBootstrap bootstrap,
            SlingshotController2D slingshot, AttemptController attempt, PieceCatalog catalog)
        {
            LevelLoader loader = Find<LevelLoader>(scene);
            CatapultCats.Physics.PhysicsResolver2D resolver = Find<CatapultCats.Physics.PhysicsResolver2D>(scene);
            if (bootstrap == null || slingshot == null || attempt == null || catalog == null || loader == null ||
                resolver == null || bootstrap.Loader != loader || bootstrap.PieceCatalog != catalog ||
                loader.Catalog != catalog || loader.RuntimeRoot == null || loader.RuntimeRoot.gameObject.scene != scene ||
                slingshot.ProjectileBody == null || slingshot.ProjectileBody.gameObject.scene != scene ||
                slingshot.InputCamera == null || slingshot.InputCamera.gameObject.scene != scene ||
                attempt.Slingshot != slingshot || attempt.Resolver != resolver)
                throw new InvalidOperationException("Gameplay R3 references are incomplete or point outside Gameplay.");
            if (loader.RuntimeRoot == slingshot.transform.root ||
                slingshot.ProjectileBody.transform.IsChildOf(loader.RuntimeRoot) ||
                bootstrap.transform.IsChildOf(loader.RuntimeRoot))
                throw new InvalidOperationException("LevelRuntimeRoot must not contain persistent gameplay/presentation objects.");
            VerifyReference(loader, "slingshot", slingshot);
            VerifyReference(loader, "attemptController", attempt);
            VerifyReference(loader, "resolver", resolver);
            VerifyTuning(slingshot);
        }

        private static void VerifyReference(Object target, string propertyName, Object expected)
        {
            if (target == null || expected == null)
                throw new InvalidOperationException("Missing required reference " + propertyName);
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            if (property == null || property.objectReferenceValue != expected)
                throw new InvalidOperationException(target.name + ": invalid reference " + propertyName);
        }

        private static Dictionary<string, string> CaptureAuthoredLevels(IEnumerable<LevelDefinition> levels)
        {
            return levels.ToDictionary(AssetDatabase.GetAssetPath,
                level => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(level)) + "\n" + EditorJsonUtility.ToJson(level));
        }

        private static void VerifyAuthoredLevels(IReadOnlyDictionary<string, string> snapshots)
        {
            foreach (KeyValuePair<string, string> snapshot in snapshots)
            {
                LevelDefinition level = Require<LevelDefinition>(snapshot.Key);
                string current = AssetDatabase.AssetPathToGUID(snapshot.Key) + "\n" + EditorJsonUtility.ToJson(level);
                if (current != snapshot.Value)
                    throw new InvalidOperationException("R4 setup changed an authored LevelDefinition: " + snapshot.Key);
            }
        }

        private static void VerifyTuning(SlingshotController2D slingshot)
        {
            if (slingshot == null || !Mathf.Approximately(slingshot.MaximumDragDistance, 1.8f) ||
                !Mathf.Approximately(slingshot.MinimumLaunchDistance, 0.2f) || !Mathf.Approximately(slingshot.LaunchSpeedPerUnit, 10.5f))
                throw new InvalidOperationException("Accepted slingshot tuning must remain 1.8 / 0.2 / 10.5.");
        }

        private static void Configure(Object target, Action<SerializedObject> write)
        {
            if (target == null) throw new InvalidOperationException("Cannot configure a missing Unity object.");
            var serialized = new SerializedObject(target);
            write(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            if (obj == null) throw new InvalidOperationException("Cannot add " + typeof(T).Name + " to a missing object.");
            if (obj.GetComponents<T>().Length > 1)
                throw new InvalidOperationException(obj.name + " has duplicate " + typeof(T).Name + " components.");
            T component = obj.GetComponent<T>();
            // ?? checks only the managed wrapper and misses Unity Editor's fake-null components.
            if ((Object)component == null) component = Undo.AddComponent<T>(obj);
            if ((Object)component == null)
                throw new InvalidOperationException("Could not add " + typeof(T).Name + " to " + obj.name);
            return component;
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            T[] components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            if (components.Length > 1)
                throw new InvalidOperationException("Gameplay has duplicate " + typeof(T).Name + " components.");
            return components.Length == 0 ? null : components[0];
        }

        private static T Require<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if ((Object)asset == null) throw new InvalidOperationException("Missing asset: " + path);
            return asset;
        }

        private static void RemoveNamedChildren(Transform parent, string name)
        {
            // Only R4-owned decoration/UI roots are replaced, including incomplete previous Apply runs.
            foreach (Transform child in parent.Cast<Transform>().Where(child => child.name == name).ToArray())
                Object.DestroyImmediate(child.gameObject);
        }
        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
