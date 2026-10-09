using System;
using System.IO;
using System.Linq;
using CatapultCats.Audio;
using CatapultCats.Core;
using CatapultCats.Launch;
using CatapultCats.Levels;
using CatapultCats.Physics;
using CatapultCats.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatapultCats.Editor
{
    // Read-only scene preflight followed by an explicit, user-initiated Windows player build.
    public static class WindowsPortfolioBuild
    {
        private const string GameplayPath = "Assets/_CatapultCats/Scenes/Gameplay.unity";
        private static readonly string[] LevelNames = { "Level01_FirstShot", "Level02_Collapse", "Level03_GlassHouse",
            "Level04_Ricochet", "Level05_MouseFortress" };
        private static readonly string[] ClipFields = { "slingshotStretch", "catLaunch", "impactThump", "woodBreak",
            "glassBreak", "mouseDefeat", "levelWin", "levelFail" };
        private static readonly string[] ClipNames = { "SlingshotStretch", "CatLaunch", "ImpactThump", "WoodBreak",
            "GlassBreak", "MouseDefeat", "LevelWin", "LevelFail" };

        [MenuItem("CatapultCats/Build Windows Portfolio Demo")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
            {
                Debug.LogError("[CatapultCats R6] Build failed: exit Play Mode and wait for compilation/import or another build to finish.");
                return;
            }
            try
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                    throw new InvalidOperationException("Install Windows Build Support for Unity 6000.3.8f1 through Unity Hub, then retry.");
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayPath) == null)
                    throw new InvalidOperationException("Gameplay scene is missing.");
                Scene loaded = SceneManager.GetSceneByPath(GameplayPath);
                if (loaded.IsValid() && loaded.isLoaded && loaded.isDirty)
                    throw new InvalidOperationException("Save your accepted Gameplay edits before building; this command never saves or rebuilds the scene.");
#if !ENABLE_INPUT_SYSTEM
                throw new InvalidOperationException("Enable the Input System in Active Input Handling before building.");
#else
                Scene preview = EditorSceneManager.OpenPreviewScene(GameplayPath);
                try { ValidateScene(preview); }
                finally { EditorSceneManager.ClosePreviewScene(preview); }

                string executable = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Windows", "CatapultCats.exe"));
                Directory.CreateDirectory(Path.GetDirectoryName(executable));
                int oldWidth = PlayerSettings.defaultScreenWidth;
                int oldHeight = PlayerSettings.defaultScreenHeight;
                bool oldNative = PlayerSettings.defaultIsNativeResolution;
                FullScreenMode oldMode = PlayerSettings.fullScreenMode;
                BuildReport report;
                try
                {
                    // Release-only overrides. Restore project defaults even when the build fails.
                    // Existing R0 foundation checks and accepted gameplay settings stay unchanged.
                    PlayerSettings.defaultScreenWidth = 1280;
                    PlayerSettings.defaultScreenHeight = 720;
                    PlayerSettings.defaultIsNativeResolution = false;
                    PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                    report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = new[] { GameplayPath }, // Startup index 0; never include LevelAuthoring.
                        locationPathName = executable,
                        targetGroup = BuildTargetGroup.Standalone,
                        target = BuildTarget.StandaloneWindows64,
                        subtarget = (int)StandaloneBuildSubtarget.Player,
                        options = BuildOptions.None // Release player, no auto-run and no development/debug flags.
                    });
                }
                finally
                {
                    PlayerSettings.defaultScreenWidth = oldWidth;
                    PlayerSettings.defaultScreenHeight = oldHeight;
                    PlayerSettings.defaultIsNativeResolution = oldNative;
                    PlayerSettings.fullScreenMode = oldMode;
                }
                if (report == null || report.summary.result != BuildResult.Succeeded || !File.Exists(executable))
                    throw new InvalidOperationException("Windows build did not succeed: " +
                        (report == null ? "no BuildReport" : $"{report.summary.result}, {report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings") +
                        ". Do not use an older executable as evidence of success; inspect the Console.");
                Debug.Log($"[CatapultCats R6] Build succeeded: {executable}\n" +
                    $"Windows x64, Gameplay only, 1280x720 windowed defaults. {report.summary.totalWarnings} build warnings. " +
                    "Project defaults restored; standalone gameplay QA is still required.");
                EditorUtility.RevealInFinder(executable);
#endif
            }
            catch (Exception exception) { Debug.LogError("[CatapultCats R6] Build failed: " + exception); }
        }

        private static void ValidateScene(Scene scene)
        {
            foreach (Transform transform in All<Transform>(scene))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + transform.name);
            LevelBootstrap bootstrap = Single<LevelBootstrap>(scene);
            LevelFlowController flow = Single<LevelFlowController>(scene);
            LevelLoader loader = Single<LevelLoader>(scene);
            AttemptController attempt = Single<AttemptController>(scene);
            SlingshotController2D sling = Single<SlingshotController2D>(scene);
            PhysicsResolver2D resolver = Single<PhysicsResolver2D>(scene);
            GameplayHud hud = Single<GameplayHud>(scene);
            GameplayAudio audio = Single<GameplayAudio>(scene);
            foreach (Behaviour component in new Behaviour[] { bootstrap, flow, loader, attempt, sling, resolver, hud, audio })
                if (!component.enabled || !component.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Required gameplay component is inactive: " + component.GetType().Name);
            if (bootstrap.GetComponent<LevelFlowController>() != flow || bootstrap.Loader != loader ||
                flow.Attempt != attempt || attempt.Slingshot != sling || attempt.Resolver != resolver ||
                loader.RuntimeRoot == null || loader.RuntimeRoot.gameObject.scene != scene || loader.RuntimeRoot.childCount != 0 ||
                sling.LaunchAnchor == null || sling.ProjectileBody == null || sling.ProjectileCollider == null ||
                sling.ProjectileBody.gameObject != sling.ProjectileCollider.gameObject ||
                resolver.ProjectileBody != sling.ProjectileBody)
                throw new InvalidOperationException("Gameplay startup/attempt/loader/projectile wiring is incomplete, or runtime authoring objects were saved into LevelRuntimeRoot.");
            RequireReference(flow, "loader", loader);
            RequireReference(loader, "slingshot", sling);
            RequireReference(loader, "attemptController", attempt);
            RequireReference(loader, "resolver", resolver);
            RequireReference(hud, "flow", flow);
            RequireReference(audio, "flow", flow);
            RequireReference(audio, "loader", loader);
            RequireReference(hud, "catIcon");
            RequireReference(hud, "panelSprite");

            PieceCatalog catalog = loader.Catalog;
            if (catalog == null || bootstrap.PieceCatalog != catalog)
                throw new InvalidOperationException("PieceCatalog references are inconsistent.");
            var prefabErrors = LevelEditorValidation.ValidateCatalogPrefabs(catalog);
            if (prefabErrors.Count != 0) throw new InvalidOperationException(string.Join("; ", prefabErrors));
            LevelSequence sequence = flow.Sequence;
            if (sequence == null || sequence.Count != LevelNames.Length || sequence.Levels.Distinct().Count() != LevelNames.Length)
                throw new InvalidOperationException("The sequence must contain the five unique authored levels.");
            for (int index = 0; index < LevelNames.Length; index++)
            {
                LevelDefinition expected = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                    "Assets/_CatapultCats/Data/Levels/" + LevelNames[index] + ".asset");
                if (expected == null || sequence.GetLevel(index) != expected || expected.LevelId != LevelNames[index])
                    throw new InvalidOperationException("Missing/wrong campaign reference at index " + index + ": " + LevelNames[index]);
                LevelValidationResult validation = LevelValidation.Validate(expected, catalog);
                if (!validation.IsValid) throw new InvalidOperationException(expected.LevelId + ": " + string.Join("; ", validation.Errors));
            }

            Camera camera = sling.InputCamera;
            if (camera == null || camera.gameObject.scene != scene || !camera.enabled || !camera.gameObject.activeInHierarchy ||
                !camera.orthographic || camera.GetComponent<GameplayCameraFraming>() == null)
                throw new InvalidOperationException("Accepted 2D camera/framing references are incomplete.");
            AudioListener listener = Single<AudioListener>(scene);
            if (!listener.enabled || listener.gameObject != camera.gameObject)
                throw new InvalidOperationException("Exactly one active camera AudioListener is required.");
            var serializedAudio = new SerializedObject(audio);
            SerializedProperty voices = serializedAudio.FindProperty("voices");
            AudioSource[] sources = audio.GetComponents<AudioSource>();
            if (voices.arraySize != GameplayAudio.VoiceCount || sources.Length != GameplayAudio.VoiceCount ||
                sources.Any(source => !source.enabled || source.loop || source.playOnAwake || source.spatialBlend != 0f))
                throw new InvalidOperationException("Existing six-voice 2D audio pool is incomplete.");
            for (int index = 0; index < sources.Length; index++)
                if (voices.GetArrayElementAtIndex(index).objectReferenceValue != sources[index])
                    throw new InvalidOperationException("Serialized audio pool references are inconsistent.");
            for (int index = 0; index < ClipNames.Length; index++)
            {
                string path = "Assets/_CatapultCats/Audio/SFX/" + ClipNames[index] + ".wav";
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null || clip.channels != 1 || clip.frequency != 44100 || clip.length <= 0f || clip.length > 1f)
                    throw new InvalidOperationException("Missing/invalid accepted sound: " + path);
                RequireReference(audio, ClipFields[index], clip);
            }
            // HUD creates its Canvas, GraphicRaycaster and InputSystemUIInputModule at runtime.
            // No editor-only UI objects or playtest SessionState are required by the player.
        }

        private static void RequireReference(Component component, string field, UnityEngine.Object expected = null)
        {
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            if (property == null || property.objectReferenceValue == null ||
                (expected != null && property.objectReferenceValue != expected))
                throw new InvalidOperationException(component.GetType().Name + "." + field + " is missing/inconsistent.");
        }
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static T Single<T>(Scene scene) where T : Component
        {
            T[] components = All<T>(scene);
            if (components.Length != 1) throw new InvalidOperationException("Expected exactly one " + typeof(T).Name);
            return components[0];
        }
    }
}
