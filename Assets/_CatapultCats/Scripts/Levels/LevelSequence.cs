using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatapultCats.Levels
{
    [CreateAssetMenu(fileName = "PlayableLevels", menuName = "CatapultCats/Level Sequence")]
    public sealed class LevelSequence : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public IReadOnlyList<LevelDefinition> Levels => levels;
        public int Count => levels.Count;

        public int IndexOf(LevelDefinition level) => levels.IndexOf(level);

        public LevelDefinition GetLevel(int index)
        {
            return index >= 0 && index < levels.Count ? levels[index] : null;
        }

        public LevelDefinition GetNext(LevelDefinition current)
        {
            int index = IndexOf(current);
            return index < 0 ? null : GetLevel(index + 1);
        }

        public void Configure(IEnumerable<LevelDefinition> orderedLevels)
        {
            levels = orderedLevels == null ? new List<LevelDefinition>() : orderedLevels.ToList();
        }
    }
}
