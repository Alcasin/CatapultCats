using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CatapultCats.Audio;
using CatapultCats.Core;
using CatapultCats.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CatapultCats.Editor
{
    public static class R5AudioSetup
    {
        private const string GameplayPath = "Assets/_CatapultCats/Scenes/Gameplay.unity";
        private const string AudioFolder = "Assets/_CatapultCats/Audio/SFX/";
        private static readonly string[] ClipNames = { "SlingshotStretch", "CatLaunch", "ImpactThump", "WoodBreak",
            "GlassBreak", "MouseDefeat", "LevelWin", "LevelFail" };
        private static readonly string[] ClipFields = { "slingshotStretch", "catLaunch", "impactThump", "woodBreak",
            "glassBreak", "mouseDefeat", "levelWin", "levelFail" };

        [MenuItem("CatapultCats/Tune R5 Stretch Audio")]
        public static void TuneStretch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatapultCats R5 Stretch] Tune failed: exit Play Mode first.");
                return;
            }
            try
            {
                // Tune the installed system only. Never call Apply or create components/assets.
                Scene scene = SceneManager.GetSceneByPath(GameplayPath);
                if (!scene.IsValid() || !scene.isLoaded)
                    scene = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Additive);
                if (scene.isDirty)
                    throw new InvalidOperationException("Save your accepted Gameplay edits first, then rerun Tune R5 Stretch Audio.");
                GameplayAudio manager = Single<GameplayAudio>(scene);
                if (!manager.enabled || !manager.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Existing GameplayAudio must be enabled and active; its state was not changed.");
                var serialized = new SerializedObject(manager);
                if (serialized.FindProperty("slingshotStretch").objectReferenceValue != LoadClip("SlingshotStretch"))
                    throw new InvalidOperationException("Existing stretch clip reference is unexpected; no audio wiring was overwritten.");
                var flow = serialized.FindProperty("flow").objectReferenceValue as LevelFlowController;
                if (flow == null || flow.gameObject.scene != scene || flow.Attempt == null ||
                    flow.Attempt.Slingshot == null || !flow.Attempt.Slingshot.enabled ||
                    !flow.Attempt.Slingshot.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Existing drag-event source wiring is incomplete; no gameplay references were changed.");
                SerializedProperty volume = serialized.FindProperty("stretchVolume");
                if (Mathf.Approximately(volume.floatValue, 0.40f))
                {
                    Debug.Log("[CatapultCats R5 Stretch] Already 0.40; stretch wiring validated, no scene changes.");
                    return;
                }

                // Preserve every existing component value except this one serialized scalar.
                Func<Component, string> protectedJson = component =>
                {
                    string json = EditorJsonUtility.ToJson(component);
                    return component == manager
                        ? Regex.Replace(json, @"""stretchVolume""\s*:\s*[-+0-9.eE]+", "\"stretchVolume\":0")
                        : json;
                };
                Dictionary<Component, string> before = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .SelectMany(transform => transform.GetComponents<Component>())
                    .Where(component => component != null)
                    .ToDictionary(component => component, component => protectedJson(component));
                float oldVolume = volume.floatValue;
                volume.floatValue = 0.40f;
                serialized.ApplyModifiedProperties();
                serialized.Update();
                if (!Mathf.Approximately(serialized.FindProperty("stretchVolume").floatValue, 0.40f))
                    throw new InvalidOperationException("Serialized stretch volume did not update.");
                foreach (var item in before)
                    if (item.Key == null || protectedJson(item.Key) != item.Value)
                        throw new InvalidOperationException("Unexpected non-stretch component change; Gameplay was not saved.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, GameplayPath))
                    throw new InvalidOperationException("Could not save the stretch tuning to Gameplay.");
                Debug.Log($"[CatapultCats R5 Stretch] Tune succeeded: saved stretch volume {oldVolume:0.00} -> 0.40. " +
                    "Existing clip/event wiring validated. Other clips, volumes, pool, headroom and presentation unchanged.");
            }
            catch (Exception exception) { Debug.LogError("[CatapultCats R5 Stretch] Tune failed: " + exception); }
        }

        [MenuItem("CatapultCats/Apply R5 Audio")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatapultCats R5] Apply failed: exit Play Mode first.");
                return;
            }
            try
            {
                // Preflight assets before any scene mutation. No generator/setup commands are invoked.
                AudioClip[] clips = ClipNames.Select(LoadClip).ToArray();
                Scene scene = SceneManager.GetSceneByPath(GameplayPath);
                if (!scene.IsValid() || !scene.isLoaded)
                    scene = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Additive);
                if (scene.isDirty)
                    throw new InvalidOperationException("Save your accepted Gameplay edits first, then rerun Apply R5 Audio.");
                LevelFlowController flow = Single<LevelFlowController>(scene);
                AttemptController attempt = Single<AttemptController>(scene);
                LevelBootstrap bootstrap = Single<LevelBootstrap>(scene);
                LevelLoader loader = bootstrap.Loader;
                if (flow.Attempt != attempt || loader == null || attempt.Slingshot == null ||
                    attempt.Slingshot.ProjectileBody == null || attempt.Slingshot.InputCamera == null ||
                    loader.RuntimeRoot == null || !flow.gameObject.activeInHierarchy || !flow.enabled ||
                    flow.Sequence == null || flow.Sequence.Count != 5 || flow.Sequence.Levels.Any(level => level == null))
                    throw new InvalidOperationException("Existing five-level gameplay wiring is incomplete; no scene rebuild will be attempted.");
                AudioListener[] listeners = All<AudioListener>(scene);
                if (listeners.Length > 1 || (listeners.Length == 1 &&
                    (!listeners[0].enabled || !listeners[0].gameObject.activeInHierarchy)))
                    throw new InvalidOperationException("Gameplay must have at most one listener, and any existing listener must be active. No listeners were removed or enabled.");
                GameplayAudio[] existing = All<GameplayAudio>(scene);
                GameObject[] namedRoots = scene.GetRootGameObjects().Where(root => root.name == "R5Audio").ToArray();
                if (namedRoots.Length > 1 || existing.Length > 1 || (existing.Length == 1 &&
                    (existing[0].gameObject.name != "R5Audio" || existing[0].transform.parent != null)))
                    throw new InvalidOperationException("Unexpected/duplicate audio manager; no authored object will be overwritten.");
                GameObject audioObject = existing.Length == 1 ? existing[0].gameObject
                    : namedRoots.SingleOrDefault();
                if (audioObject != null && (!audioObject.activeInHierarchy ||
                    audioObject.GetComponents<AudioSource>().Length > GameplayAudio.VoiceCount ||
                    audioObject.GetComponents<AudioSource>().Any(source => !source.enabled) ||
                    audioObject.GetComponents<Component>().Any(component => component == null ||
                        !(component is Transform || component is GameplayAudio || component is AudioSource))))
                    throw new InvalidOperationException("R5Audio must be an active, audio-only root with at most six enabled pool sources.");
                if (existing.Length == 1 && !existing[0].enabled)
                    throw new InvalidOperationException("Existing R5Audio is disabled; its authored active state was preserved.");

                var originalComponents = new Dictionary<Component, string>();
                var originalObjects = new Dictionary<GameObject, string>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        GameObject go = transform.gameObject;
                        originalObjects.Add(go, Identity(go));
                        foreach (Component component in go.GetComponents<Component>())
                            if (component != null && !(go == audioObject &&
                                (component is GameplayAudio || component is AudioSource)))
                                originalComponents.Add(component, EditorJsonUtility.ToJson(component));
                    }
                var levels = AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_CatapultCats/Data/Levels" })
                    .Select(guid => AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                    .ToDictionary(level => level, level => EditorJsonUtility.ToJson(level));
                string sequence = EditorJsonUtility.ToJson(flow.Sequence);

                if (audioObject == null)
                {
                    Scene previousActive = SceneManager.GetActiveScene();
                    try
                    {
                        SceneManager.SetActiveScene(scene);
                        audioObject = new GameObject("R5Audio");
                    }
                    finally { if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive); }
                }
                GameplayAudio manager = audioObject.GetComponent<GameplayAudio>();
                if (manager == null) manager = audioObject.AddComponent<GameplayAudio>();
                var voices = audioObject.GetComponents<AudioSource>().ToList();
                while (voices.Count < GameplayAudio.VoiceCount) voices.Add(audioObject.AddComponent<AudioSource>());
                foreach (AudioSource voice in voices)
                {
                    voice.playOnAwake = false;
                    voice.loop = false;
                    voice.spatialBlend = 0f;
                    voice.volume = 0f;
                    voice.pitch = 1f;
                    voice.dopplerLevel = 0f;
                    voice.reverbZoneMix = 0f;
                    voice.bypassReverbZones = true;
                }
                var serialized = new SerializedObject(manager);
                serialized.FindProperty("flow").objectReferenceValue = flow;
                serialized.FindProperty("loader").objectReferenceValue = loader;
                SerializedProperty pool = serialized.FindProperty("voices");
                pool.arraySize = voices.Count;
                for (int i = 0; i < voices.Count; i++) pool.GetArrayElementAtIndex(i).objectReferenceValue = voices[i];
                for (int i = 0; i < clips.Length; i++) serialized.FindProperty(ClipFields[i]).objectReferenceValue = clips[i];
                // Volumes are defaults only on first creation; preserve later Inspector sound tuning.
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (listeners.Length == 0) attempt.Slingshot.InputCamera.gameObject.AddComponent<AudioListener>();

                foreach (var item in originalObjects)
                    if (item.Key == null || Identity(item.Key) != item.Value)
                        throw new InvalidOperationException("Existing object identity/active state changed.");
                foreach (var item in originalComponents)
                    if (item.Key == null || EditorJsonUtility.ToJson(item.Key) != item.Value)
                        throw new InvalidOperationException("Existing component/transform changed: " + item.Key);
                foreach (var item in levels)
                    if (EditorJsonUtility.ToJson(item.Key) != item.Value)
                        throw new InvalidOperationException("Authored LevelDefinition changed.");
                if (EditorJsonUtility.ToJson(flow.Sequence) != sequence)
                    throw new InvalidOperationException("Five-level sequence changed.");
                Validate(scene, manager, clips, flow, loader);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, GameplayPath))
                    throw new InvalidOperationException("Could not save Gameplay.");
                Debug.Log("[CatapultCats R5] Apply succeeded: eight serialized clips, one manager, six 2D voices, " +
                    "one AudioListener; existing presentation, transforms, active states, five-level sequence and authored levels preserved.");
            }
            catch (Exception exception) { Debug.LogError("[CatapultCats R5] Apply failed: " + exception); }
        }

        private static AudioClip LoadClip(string name)
        {
            string path = AudioFolder + name + ".wav";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (clip == null || importer == null || clip.channels != 1 || clip.frequency != 44100 ||
                clip.length <= 0f || clip.length > 1f ||
                importer.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad ||
                importer.defaultSampleSettings.compressionFormat != AudioCompressionFormat.PCM)
                throw new InvalidOperationException("Missing/invalid mono PCM audio asset: " + path);
            return clip;
        }

        private static void Validate(Scene scene, GameplayAudio manager, AudioClip[] clips,
            LevelFlowController flow, LevelLoader loader)
        {
            if (All<GameplayAudio>(scene).Length != 1 || All<AudioListener>(scene).Length != 1)
                throw new InvalidOperationException("Audio manager/listener must be unique.");
            AudioSource[] sources = manager.GetComponents<AudioSource>();
            if (sources.Length != GameplayAudio.VoiceCount || sources.Any(source => source.loop || source.playOnAwake ||
                source.spatialBlend != 0f || !source.enabled))
                throw new InvalidOperationException("Audio pool configuration failed.");
            var serialized = new SerializedObject(manager);
            if (serialized.FindProperty("flow").objectReferenceValue != flow ||
                serialized.FindProperty("loader").objectReferenceValue != loader)
                throw new InvalidOperationException("Audio gameplay references failed.");
            for (int i = 0; i < clips.Length; i++)
                if (serialized.FindProperty(ClipFields[i]).objectReferenceValue != clips[i])
                    throw new InvalidOperationException("Serialized clip reference failed: " + ClipNames[i]);
            SerializedProperty pool = serialized.FindProperty("voices");
            if (pool.arraySize != GameplayAudio.VoiceCount) throw new InvalidOperationException("Invalid serialized voice count.");
            for (int i = 0; i < sources.Length; i++)
                if (pool.GetArrayElementAtIndex(i).objectReferenceValue != sources[i])
                    throw new InvalidOperationException("Serialized audio source reference failed.");
        }

        private static string Identity(GameObject go) => go.name + "|" + go.activeSelf + "|" + go.layer + "|" + go.tag;
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
