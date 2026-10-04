using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatapultCats.Levels
{
    [CreateAssetMenu(fileName = "PieceCatalog", menuName = "CatapultCats/Piece Catalog")]
    public sealed class PieceCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] private LevelPieceType pieceType;
            [SerializeField] private GameObject prefab;

            public Entry(LevelPieceType pieceType, GameObject prefab)
            {
                this.pieceType = pieceType;
                this.prefab = prefab;
            }

            public LevelPieceType PieceType => pieceType;
            public GameObject Prefab => prefab;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGetPrefab(LevelPieceType pieceType, out GameObject prefab)
        {
            prefab = null;
            bool found = false;
            foreach (Entry entry in entries)
            {
                if (entry.PieceType != pieceType)
                {
                    continue;
                }

                if (found)
                {
                    prefab = null;
                    return false;
                }

                found = true;
                prefab = entry.Prefab;
            }

            return found && prefab != null;
        }

        public bool TryGetPieceType(GameObject prefab, out LevelPieceType pieceType)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Prefab == prefab)
                {
                    pieceType = entry.PieceType;
                    return true;
                }
            }

            pieceType = default;
            return false;
        }

        public void ReplaceEntries(IEnumerable<Entry> newEntries)
        {
            entries = newEntries != null ? newEntries.ToList() : new List<Entry>();
        }

        public IReadOnlyList<string> GetConfigurationErrors()
        {
            var errors = new List<string>();
            var seen = new HashSet<LevelPieceType>();
            foreach (Entry entry in entries)
            {
                if (!Enum.IsDefined(typeof(LevelPieceType), entry.PieceType))
                {
                    errors.Add($"Catalog contains unsupported PieceType value {(int)entry.PieceType}.");
                    continue;
                }

                if (!seen.Add(entry.PieceType))
                {
                    errors.Add($"Catalog contains duplicate PieceType {entry.PieceType}.");
                }

                if (entry.Prefab == null)
                {
                    errors.Add($"Catalog entry {entry.PieceType} has no prefab.");
                }
            }

            foreach (LevelPieceType pieceType in Enum.GetValues(typeof(LevelPieceType)))
            {
                if (!seen.Contains(pieceType))
                {
                    errors.Add($"Catalog has no entry for {pieceType}.");
                }
            }

            return errors;
        }
    }
}
