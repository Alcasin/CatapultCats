using System;
using System.Collections.Generic;
using System.Linq;
using CatapultCats.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CatapultCats.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        internal const string CatalogPath = "Assets/_CatapultCats/Data/PieceCatalog.asset";
        internal const string SandboxPath = "Assets/_CatapultCats/Data/Levels/R3_Sandbox.asset";
        internal const string AuthoringScenePath = "Assets/_CatapultCats/Editor/LevelAuthoring.unity";
        internal const string GameplayScenePath = "Assets/_CatapultCats/Scenes/Gameplay.unity";
        internal const string AuthoringRootName = "AuthoringRoot";

        [SerializeField] private LevelDefinition selectedLevel;
        [SerializeField] private LevelDefinition loadedLevel;
        [SerializeField] private string selectedLevelPath;
        [SerializeField] private string loadedLevelPath;
        private PieceCatalog catalog;
        [SerializeField] private string levelId = "NewLevel";
        [SerializeField] private int catCount = LevelDefinition.DefaultCatCount;
        [SerializeField] private Vector2 slingshotPosition = new Vector2(-6f, -2.4f);
        [SerializeField] private bool gridSnap = true;
        [SerializeField] private float gridSize = 0.25f;
        [SerializeField] private float rotationStep = 15f;
        private LevelPieceType? placementType;
        private Vector2 scroll;
        [SerializeField] private string statusMessage;
        [SerializeField] private MessageType statusType = MessageType.Info;

        [MenuItem("CatapultCats/Level Editor")]
        public static void Open()
        {
            GetWindow<LevelEditorWindow>("CatapultCats Level Editor");
        }

        private void OnEnable()
        {
            catalog = AssetDatabase.LoadAssetAtPath<PieceCatalog>(CatalogPath);
            RehydrateLevelReferences();

            SceneView.duringSceneGui += DuringSceneGui;
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
            Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawCurrentLevelSection();
            DrawLevelSettingsSection();
            DrawPaletteSection();
            DrawAuthoringSection();
            DrawToolsSection();
            DrawStatusSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCurrentLevelSection()
        {
            EditorGUILayout.LabelField("CURRENT LEVEL", EditorStyles.boldLabel);
            LevelDefinition selected = (LevelDefinition)EditorGUILayout.ObjectField(
                "Level Definition",
                selectedLevel,
                typeof(LevelDefinition),
                false);
            if (selected != selectedLevel)
            {
                selectedLevel = selected;
                selectedLevelPath = GetAssetPath(selectedLevel);
                if (IsSelectedLevelLoaded())
                {
                    SetStatus(MessageType.Info, $"Loaded: {loadedLevel.LevelId}");
                }
                else if (selectedLevel != null)
                {
                    SetStatus(MessageType.Info, $"Selected: {selectedLevel.LevelId}. Click Load to begin authoring.");
                }
                else
                {
                    SetStatus(MessageType.Info, "Choose or create a level, then Load it to begin authoring.");
                }
            }

            EditorGUILayout.LabelField(
                "Loaded Level",
                loadedLevel == null ? "None" : loadedLevel.LevelId);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Level"))
                {
                    CreateNewLevel();
                }

                if (GUILayout.Button("Load"))
                {
                    LoadCurrentLevel();
                }

                if (GUILayout.Button("Save"))
                {
                    SaveCurrentLevel();
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawLevelSettingsSection()
        {
            EditorGUILayout.LabelField("LEVEL SETTINGS", EditorStyles.boldLabel);
            levelId = EditorGUILayout.TextField("Level Id", levelId);
            catCount = EditorGUILayout.IntSlider("Cat Count", catCount, LevelDefinition.MinimumCatCount, LevelDefinition.MaximumCatCount);
            slingshotPosition = EditorGUILayout.Vector2Field("Slingshot Position", slingshotPosition);
            EditorGUILayout.Space();
        }

        private void DrawPaletteSection()
        {
            EditorGUILayout.LabelField("PALETTE", EditorStyles.boldLabel);
            foreach (LevelPieceType pieceType in Enum.GetValues(typeof(LevelPieceType)))
            {
                bool active = placementType == pieceType;
                Color previous = GUI.backgroundColor;
                if (active)
                {
                    GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
                }

                if (GUILayout.Button(active ? $"Click Scene View: {GetLabel(pieceType)}" : GetLabel(pieceType)))
                {
                    placementType = active ? null : pieceType;
                    EnsureAuthoringScene();
                    SceneView.RepaintAll();
                }

                GUI.backgroundColor = previous;
            }

            EditorGUILayout.Space();
        }

        private void DrawAuthoringSection()
        {
            EditorGUILayout.LabelField("AUTHORING", EditorStyles.boldLabel);
            gridSnap = EditorGUILayout.Toggle("Grid Snap", gridSnap);
            using (new EditorGUI.DisabledScope(!gridSnap))
            {
                gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", gridSize));
            }

            rotationStep = Mathf.Max(1f, EditorGUILayout.FloatField("Rotation Step", rotationStep));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rotate Left"))
                {
                    RotateSelected(rotationStep);
                }

                if (GUILayout.Button("Rotate Right"))
                {
                    RotateSelected(-rotationStep);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Duplicate Selected"))
                {
                    DuplicateSelected();
                }

                if (GUILayout.Button("Delete Selected"))
                {
                    DeleteSelected();
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawToolsSection()
        {
            EditorGUILayout.LabelField("TOOLS", EditorStyles.boldLabel);
            if (GUILayout.Button("Validate Level"))
            {
                ValidateCurrentPreview();
            }

            if (GUILayout.Button("Playtest Current Level", GUILayout.Height(30f)))
            {
                PlaytestCurrentLevel();
            }

            EditorGUILayout.Space();
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("STATUS", EditorStyles.boldLabel);
            if (string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox("Choose or create a level, then Load it to begin authoring.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            if (SceneManager.GetActiveScene().path != AuthoringScenePath)
            {
                return;
            }

            DrawAuthoringGuides();
            DrawSlingshotHandle();
            SnapMovedSelectionOnRelease();

            Event current = Event.current;
            if (placementType.HasValue)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            if (!placementType.HasValue || current.type != EventType.MouseDown || current.button != 0 || current.alt)
            {
                return;
            }

            if (catalog == null || !catalog.TryGetPrefab(placementType.Value, out GameObject prefab))
            {
                SetStatus(MessageType.Error, $"No catalog prefab is available for {placementType.Value}.");
                placementType = null;
                return;
            }

            Transform root = GetAuthoringRoot();
            if (root == null)
            {
                return;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            float distance = Mathf.Abs(ray.direction.z) > 0.0001f ? -ray.origin.z / ray.direction.z : 0f;
            Vector3 world = ray.GetPoint(distance);
            Vector2 position = Snap(new Vector2(world.x, world.y));
            var instance = PrefabUtility.InstantiatePrefab(prefab, root.gameObject.scene) as GameObject;
            if (instance == null)
            {
                SetStatus(MessageType.Error, $"Could not instantiate {placementType.Value}.");
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Place Level Piece");
            instance.transform.SetParent(root, true);
            instance.transform.position = new Vector3(position.x, position.y, 0f);
            instance.transform.rotation = Quaternion.identity;
            Selection.activeGameObject = instance;
            placementType = null;
            current.Use();
        }

        private void DrawAuthoringGuides()
        {
            Handles.color = new Color(0.25f, 0.65f, 1f, 0.8f);
            Vector3 min = new Vector3(LevelValidation.PlayableMinX, LevelValidation.PlayableMinY, 0f);
            Vector3 max = new Vector3(LevelValidation.PlayableMaxX, LevelValidation.PlayableMaxY, 0f);
            Handles.DrawLine(new Vector3(min.x, min.y), new Vector3(max.x, min.y));
            Handles.DrawLine(new Vector3(max.x, min.y), new Vector3(max.x, max.y));
            Handles.DrawLine(new Vector3(max.x, max.y), new Vector3(min.x, max.y));
            Handles.DrawLine(new Vector3(min.x, max.y), new Vector3(min.x, min.y));

            Handles.color = new Color(0.2f, 0.75f, 0.25f, 1f);
            Handles.DrawLine(
                new Vector3(LevelValidation.PlayableMinX, LevelValidation.GroundTopY),
                new Vector3(LevelValidation.PlayableMaxX, LevelValidation.GroundTopY));

            Handles.color = new Color(1f, 0.75f, 0.15f, 0.95f);
            Handles.DrawWireDisc(slingshotPosition, Vector3.forward, LevelValidation.MaximumDragDistance);
            Handles.color = new Color(1f, 0.55f, 0.15f, 0.9f);
            Handles.DrawWireDisc(slingshotPosition, Vector3.forward, LevelValidation.CatColliderRadius);
            Handles.color = new Color(1f, 0.3f, 0.15f, 0.8f);
            Handles.DrawWireDisc(slingshotPosition, Vector3.forward, LevelValidation.ProtectedSlingshotRadius);
            Handles.DrawSolidDisc(slingshotPosition, Vector3.forward, 0.09f);
            Handles.Label(slingshotPosition + Vector2.up * 0.18f, "Launch Anchor");
        }

        private void DrawSlingshotHandle()
        {
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.PositionHandle(slingshotPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                slingshotPosition = Snap(new Vector2(moved.x, moved.y));
                Repaint();
            }
        }

        private void SnapMovedSelectionOnRelease()
        {
            if (!gridSnap || Event.current.type != EventType.MouseUp)
            {
                return;
            }

            foreach (GameObject piece in GetSelectedPieces())
            {
                Vector2 snapped = Snap(piece.transform.position);
                if ((snapped - (Vector2)piece.transform.position).sqrMagnitude < 0.000001f)
                {
                    continue;
                }

                Undo.RecordObject(piece.transform, "Snap Level Piece");
                piece.transform.position = new Vector3(snapped.x, snapped.y, 0f);
            }
        }

        private void CreateNewLevel()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create CatapultCats Level",
                "NewLevel",
                "asset",
                "Choose a LevelDefinition asset path.",
                "Assets/_CatapultCats/Data/Levels");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!EnsureAuthoringScene())
            {
                return;
            }

            var created = CreateInstance<LevelDefinition>();
            created.Configure(
                System.IO.Path.GetFileNameWithoutExtension(path),
                LevelDefinition.DefaultCatCount,
                LevelDefinition.DefaultSlingshotPosition,
                Array.Empty<LevelPiecePlacement>());
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            selectedLevel = created;
            loadedLevel = created;
            selectedLevelPath = path;
            loadedLevelPath = path;
            ReadSettingsFromLoadedLevel();
            ClearPreview();
            SceneView.RepaintAll();
            SetStatus(MessageType.Info, $"Loaded: {loadedLevel.LevelId}");
        }

        private void LoadCurrentLevel()
        {
            string candidatePath = GetAssetPath(selectedLevel);
            if (string.IsNullOrEmpty(candidatePath))
            {
                SetStatus(MessageType.Error, "Assign a LevelDefinition before loading.");
                return;
            }

            if (!EnsureAuthoringScene() || !EnsureCatalog())
            {
                return;
            }

            LevelDefinition candidate = AssetDatabase.LoadAssetAtPath<LevelDefinition>(candidatePath);
            if (candidate == null)
            {
                SetStatus(MessageType.Error, $"Could not reacquire the selected LevelDefinition at {candidatePath}.");
                return;
            }

            selectedLevel = candidate;
            selectedLevelPath = candidatePath;
            loadedLevel = candidate;
            loadedLevelPath = candidatePath;
            ReadSettingsFromLoadedLevel();
            ClearPreview();
            Transform root = GetAuthoringRoot();
            foreach (LevelPiecePlacement placement in loadedLevel.Pieces)
            {
                if (!catalog.TryGetPrefab(placement.PieceType, out GameObject prefab))
                {
                    SetStatus(MessageType.Error, $"Catalog mapping is missing for {placement.PieceType}.");
                    return;
                }

                var instance = PrefabUtility.InstantiatePrefab(prefab, root.gameObject.scene) as GameObject;
                if (instance == null)
                {
                    SetStatus(MessageType.Error, $"Could not instantiate {placement.PieceType}.");
                    return;
                }

                instance.transform.SetParent(root, true);
                instance.transform.SetPositionAndRotation(
                    placement.Position,
                    Quaternion.Euler(0f, 0f, placement.RotationDegrees));
            }

            Selection.objects = Array.Empty<Object>();
            SceneView.RepaintAll();
            Repaint();
            SetStatus(MessageType.Info, $"Loaded: {loadedLevel.LevelId}");
        }

        private bool SaveCurrentLevel()
        {
            if (!TryGetLoadedLevel(out LevelDefinition level))
            {
                SetStatus(MessageType.Error, "Load a LevelDefinition before saving. A selected candidate is not loaded.");
                return false;
            }

            if (!TryCollectPlacements(out List<LevelPiecePlacement> placements, out List<string> errors))
            {
                SetStatus(MessageType.Error, errors);
                return false;
            }

            Undo.RecordObject(level, "Save CatapultCats Level");
            level.Configure(levelId, catCount, slingshotPosition, placements);
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            SetStatus(MessageType.Info, $"Saved {level.LevelId} with {placements.Count} pieces.");
            return true;
        }

        private bool ValidateCurrentPreview()
        {
            if (!TryBuildCandidate(out LevelDefinition candidate, out List<string> collectionErrors))
            {
                SetStatus(MessageType.Error, collectionErrors);
                return false;
            }

            var errors = new List<string>();
            LevelValidationResult dataResult = LevelValidation.Validate(candidate, catalog);
            errors.AddRange(dataResult.Errors);
            errors.AddRange(LevelEditorValidation.ValidateCatalogPrefabs(catalog));
            errors.AddRange(LevelEditorValidation.ValidatePreviewPenetrations(GetAuthoringRoot()));
            errors = errors.Distinct().ToList();
            DestroyImmediate(candidate);

            if (errors.Count > 0)
            {
                SetStatus(MessageType.Error, errors.Select(error => "ERROR: " + error));
                return false;
            }

            SetStatus(MessageType.Info, "Level validation passed.");
            return true;
        }

        private void PlaytestCurrentLevel()
        {
            if (!TryGetLoadedLevel(out LevelDefinition level))
            {
                SetStatus(MessageType.Error, "Load a LevelDefinition before playtesting. A selected candidate is not loaded.");
                return;
            }

            if (!ValidateCurrentPreview() || !SaveCurrentLevel())
            {
                return;
            }

            ClearPreview();
            Scene authoringScene = SceneManager.GetActiveScene();
            if (authoringScene.path == AuthoringScenePath)
            {
                EditorSceneManager.SaveScene(authoringScene, AuthoringScenePath);
            }

            Scene gameplay = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            LevelBootstrap bootstrap = gameplay.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<LevelBootstrap>(true))
                .SingleOrDefault();
            if (bootstrap == null)
            {
                SetStatus(MessageType.Error, "Gameplay has no LevelBootstrap. Run Apply R3 Level System.");
                return;
            }

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("defaultLevel").objectReferenceValue = level;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(gameplay);
            EditorSceneManager.SaveScene(gameplay, GameplayScenePath);
            EditorApplication.EnterPlaymode();
        }

        private bool TryBuildCandidate(out LevelDefinition candidate, out List<string> errors)
        {
            candidate = null;
            if (!TryGetLoadedLevel(out _))
            {
                errors = new List<string> { "Load a LevelDefinition before validating. A selected candidate is not loaded." };
                return false;
            }

            if (!EnsureCatalog())
            {
                errors = new List<string> { "PieceCatalog is unavailable." };
                return false;
            }

            if (!TryCollectPlacements(out List<LevelPiecePlacement> placements, out errors))
            {
                return false;
            }

            candidate = CreateInstance<LevelDefinition>();
            candidate.Configure(levelId, catCount, slingshotPosition, placements);
            return true;
        }

        private bool TryCollectPlacements(out List<LevelPiecePlacement> placements, out List<string> errors)
        {
            placements = new List<LevelPiecePlacement>();
            errors = new List<string>();
            if (!EnsureAuthoringScene() || !EnsureCatalog())
            {
                errors.Add("Authoring scene or catalog is unavailable.");
                return false;
            }

            Transform root = GetAuthoringRoot();
            for (int index = 0; index < root.childCount; index++)
            {
                GameObject instance = root.GetChild(index).gameObject;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                if (source == null || !catalog.TryGetPieceType(source, out LevelPieceType pieceType))
                {
                    errors.Add($"{instance.name} is not a mapped catalog prefab instance.");
                    continue;
                }

                if (Vector3.Distance(instance.transform.localScale, source.transform.localScale) > 0.001f)
                {
                    errors.Add($"{instance.name} uses unsupported arbitrary scale. Restore its prefab scale.");
                    continue;
                }

                placements.Add(new LevelPiecePlacement(
                    pieceType,
                    instance.transform.position,
                    instance.transform.eulerAngles.z));
            }

            return errors.Count == 0;
        }

        private void DuplicateSelected()
        {
            Transform root = GetAuthoringRoot();
            if (root == null)
            {
                return;
            }

            var copies = new List<Object>();
            foreach (GameObject piece in GetSelectedPieces())
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(piece);
                if (source == null || !catalog.TryGetPieceType(source, out _))
                {
                    continue;
                }

                var copy = PrefabUtility.InstantiatePrefab(source, root.gameObject.scene) as GameObject;
                Undo.RegisterCreatedObjectUndo(copy, "Duplicate Level Piece");
                copy.transform.SetParent(root, true);
                Vector2 position = (Vector2)piece.transform.position + Vector2.right * (gridSnap ? gridSize : 0.25f);
                position = Snap(position);
                copy.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), piece.transform.rotation);
                copies.Add(copy);
            }

            Selection.objects = copies.ToArray();
        }

        private void DeleteSelected()
        {
            foreach (GameObject piece in GetSelectedPieces())
            {
                Undo.DestroyObjectImmediate(piece);
            }

            Selection.objects = Array.Empty<Object>();
        }

        private void RotateSelected(float degrees)
        {
            foreach (GameObject piece in GetSelectedPieces())
            {
                Undo.RecordObject(piece.transform, "Rotate Level Piece");
                Vector3 euler = piece.transform.eulerAngles;
                euler.z = Mathf.Repeat(euler.z + degrees + 180f, 360f) - 180f;
                piece.transform.eulerAngles = euler;
            }
        }

        private IEnumerable<GameObject> GetSelectedPieces()
        {
            Transform root = GetAuthoringRoot();
            if (root == null)
            {
                return Array.Empty<GameObject>();
            }

            return Selection.gameObjects
                .Select(selected => FindDirectPiece(selected.transform, root))
                .Where(piece => piece != null)
                .Select(piece => piece.gameObject)
                .Distinct()
                .ToArray();
        }

        private static Transform FindDirectPiece(Transform selected, Transform root)
        {
            Transform current = selected;
            while (current != null && current.parent != root)
            {
                current = current.parent;
            }

            return current != null && current.parent == root ? current : null;
        }

        private bool EnsureAuthoringScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AuthoringScenePath) == null)
            {
                SetStatus(MessageType.Error, "LevelAuthoring scene is missing. Run CatapultCats > Apply R3 Level System.");
                return false;
            }

            if (SceneManager.GetActiveScene().path != AuthoringScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return false;
                }

                EditorSceneManager.OpenScene(AuthoringScenePath, OpenSceneMode.Single);
            }

            if (GetAuthoringRoot() == null)
            {
                new GameObject(AuthoringRootName);
            }

            return true;
        }

        private bool EnsureCatalog()
        {
            if (catalog == null)
            {
                catalog = AssetDatabase.LoadAssetAtPath<PieceCatalog>(CatalogPath);
            }

            if (catalog != null)
            {
                return true;
            }

            SetStatus(MessageType.Error, "PieceCatalog is missing. Run CatapultCats > Apply R3 Level System.");
            return false;
        }

        private static Transform GetAuthoringRoot()
        {
            if (SceneManager.GetActiveScene().path != AuthoringScenePath)
            {
                return null;
            }

            return SceneManager.GetActiveScene().GetRootGameObjects()
                .FirstOrDefault(root => root.name == AuthoringRootName)?.transform;
        }

        private static void ClearPreview()
        {
            Transform root = GetAuthoringRoot();
            if (root == null)
            {
                return;
            }

            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Undo.DestroyObjectImmediate(root.GetChild(index).gameObject);
            }
        }

        private Vector2 Snap(Vector2 value)
        {
            if (!gridSnap)
            {
                return value;
            }

            float step = Mathf.Max(0.01f, gridSize);
            return new Vector2(
                Mathf.Round(value.x / step) * step,
                Mathf.Round(value.y / step) * step);
        }

        private void RehydrateLevelReferences()
        {
            if (!string.IsNullOrEmpty(selectedLevelPath))
            {
                selectedLevel = AssetDatabase.LoadAssetAtPath<LevelDefinition>(selectedLevelPath);
            }

            if (selectedLevel == null)
            {
                selectedLevel = AssetDatabase.LoadAssetAtPath<LevelDefinition>(SandboxPath);
            }

            selectedLevelPath = GetAssetPath(selectedLevel);

            if (!string.IsNullOrEmpty(loadedLevelPath))
            {
                loadedLevel = AssetDatabase.LoadAssetAtPath<LevelDefinition>(loadedLevelPath);
            }

            if (loadedLevel == null)
            {
                loadedLevelPath = string.Empty;
            }
            else
            {
                loadedLevelPath = GetAssetPath(loadedLevel);
            }

            if (string.IsNullOrEmpty(statusMessage))
            {
                statusMessage = loadedLevel != null
                    ? $"Loaded: {loadedLevel.LevelId}"
                    : "Choose or create a level, then Load it to begin authoring.";
            }
        }

        private bool TryGetLoadedLevel(out LevelDefinition level)
        {
            level = null;
            if (string.IsNullOrEmpty(loadedLevelPath))
            {
                loadedLevel = null;
                return false;
            }

            level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(loadedLevelPath);
            if (level == null)
            {
                loadedLevel = null;
                loadedLevelPath = string.Empty;
                return false;
            }

            loadedLevel = level;
            return IsSelectedLevelLoaded();
        }

        private bool IsSelectedLevelLoaded()
        {
            return selectedLevel != null
                && loadedLevel != null
                && string.Equals(GetAssetPath(selectedLevel), loadedLevelPath, StringComparison.Ordinal);
        }

        private static string GetAssetPath(LevelDefinition level)
        {
            return level == null ? string.Empty : AssetDatabase.GetAssetPath(level);
        }

        private void ReadSettingsFromLoadedLevel()
        {
            if (loadedLevel == null)
            {
                return;
            }

            levelId = loadedLevel.LevelId;
            catCount = loadedLevel.CatCount;
            slingshotPosition = loadedLevel.SlingshotPosition;
        }

        private void SetStatus(MessageType type, params string[] messages)
        {
            SetStatus(type, (IEnumerable<string>)messages);
        }

        private void SetStatus(MessageType type, IEnumerable<string> messages)
        {
            statusType = type;
            statusMessage = string.Join("\n", messages.Where(message => !string.IsNullOrEmpty(message)));
            Repaint();
        }

        private static string GetLabel(LevelPieceType pieceType)
        {
            switch (pieceType)
            {
                case LevelPieceType.WoodBeam: return "Wood Beam";
                case LevelPieceType.WoodBlock: return "Wood Block";
                case LevelPieceType.GlassBeam: return "Glass Beam";
                case LevelPieceType.GlassBlock: return "Glass Block";
                case LevelPieceType.HeavyBlock: return "Heavy Block";
                default: return pieceType.ToString();
            }
        }
    }
}
