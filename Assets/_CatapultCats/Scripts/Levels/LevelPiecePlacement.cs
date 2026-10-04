using System;
using UnityEngine;

namespace CatapultCats.Levels
{
    [Serializable]
    public struct LevelPiecePlacement
    {
        [SerializeField] private LevelPieceType pieceType;
        [SerializeField] private Vector2 position;
        [SerializeField] private float rotationDegrees;

        public LevelPiecePlacement(LevelPieceType pieceType, Vector2 position, float rotationDegrees)
        {
            this.pieceType = pieceType;
            this.position = position;
            this.rotationDegrees = rotationDegrees;
        }

        public LevelPieceType PieceType => pieceType;
        public Vector2 Position => position;
        public float RotationDegrees => rotationDegrees;
    }
}
