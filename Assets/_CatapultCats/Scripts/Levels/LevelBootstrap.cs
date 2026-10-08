using UnityEngine;

namespace CatapultCats.Levels
{
    public sealed class LevelBootstrap : MonoBehaviour
    {
        public const string EditorPlaytestKey = "CatapultCats.PlaytestLevelPath";
        [SerializeField] private PieceCatalog pieceCatalog;
        [SerializeField] private LevelDefinition defaultLevel;
        [SerializeField] private LevelLoader levelLoader;

        public PieceCatalog PieceCatalog => pieceCatalog;
        public LevelDefinition DefaultLevel => defaultLevel;
        public LevelLoader Loader => levelLoader;

        private void Start()
        {
            LevelDefinition editorOverride = null;
#if UNITY_EDITOR
            string playtestPath = UnityEditor.SessionState.GetString(EditorPlaytestKey, string.Empty);
            UnityEditor.SessionState.EraseString(EditorPlaytestKey);
            if (!string.IsNullOrEmpty(playtestPath))
            {
                editorOverride = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDefinition>(playtestPath);
            }
#endif
            LevelFlowController flow = GetComponent<LevelFlowController>();
            if (flow != null)
            {
                flow.Begin(editorOverride);
                return;
            }

            if (pieceCatalog == null || defaultLevel == null || levelLoader == null)
            {
                Debug.LogError("[CatapultCats] LevelBootstrap references are incomplete.");
                return;
            }

            try
            {
                levelLoader.Load(editorOverride != null ? editorOverride : defaultLevel);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[CatapultCats] Level load failed: " + exception.Message);
            }
        }
    }
}
