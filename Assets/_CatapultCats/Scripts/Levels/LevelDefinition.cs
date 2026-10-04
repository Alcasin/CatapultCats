using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatapultCats.Levels
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "CatapultCats/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public const int MinimumCatCount = 1;
        public const int MaximumCatCount = 5;
        public const int DefaultCatCount = 3;
        public static readonly Vector2 DefaultSlingshotPosition = new Vector2(-6f, -2.4f);

        [SerializeField] private string levelId = "NewLevel";
        [SerializeField, Range(MinimumCatCount, MaximumCatCount)] private int catCount = DefaultCatCount;
        [SerializeField] private Vector2 slingshotPosition = new Vector2(-6f, -2.4f);
        [SerializeField] private List<LevelPiecePlacement> pieces = new List<LevelPiecePlacement>();

        public string LevelId => levelId;
        public int CatCount => catCount;
        public Vector2 SlingshotPosition => slingshotPosition;
        public IReadOnlyList<LevelPiecePlacement> Pieces => pieces;

        public void Configure(
            string newLevelId,
            int newCatCount,
            Vector2 newSlingshotPosition,
            IEnumerable<LevelPiecePlacement> newPieces)
        {
            levelId = newLevelId;
            catCount = newCatCount;
            slingshotPosition = newSlingshotPosition;
            pieces = newPieces != null
                ? newPieces.ToList()
                : new List<LevelPiecePlacement>();
        }
    }
}
