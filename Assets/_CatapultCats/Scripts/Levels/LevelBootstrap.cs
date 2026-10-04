using UnityEngine;

namespace CatapultCats.Levels
{
    public sealed class LevelBootstrap : MonoBehaviour
    {
        [SerializeField] private PieceCatalog pieceCatalog;
        [SerializeField] private LevelDefinition defaultLevel;
        [SerializeField] private LevelLoader levelLoader;

        public PieceCatalog PieceCatalog => pieceCatalog;
        public LevelDefinition DefaultLevel => defaultLevel;
        public LevelLoader Loader => levelLoader;

        private void Start()
        {
            if (pieceCatalog == null || defaultLevel == null || levelLoader == null)
            {
                Debug.LogError("[CatapultCats] LevelBootstrap references are incomplete.");
                return;
            }

            try
            {
                levelLoader.Load(defaultLevel);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[CatapultCats] Level load failed: " + exception.Message);
            }
        }
    }
}
