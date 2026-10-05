using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatapultCats.Levels
{
    public sealed class LevelValidationResult
    {
        private readonly List<string> errors = new List<string>();

        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors;

        public void AddError(string message)
        {
            errors.Add(message);
        }
    }

    public static class LevelValidation
    {
        public const float PlayableMinX = -9.6f;
        public const float PlayableMaxX = 9.6f;
        public const float PlayableMinY = -5.4f;
        public const float PlayableMaxY = 5.4f;
        public const float ConstructionGridUnit = 0.125f;
        public const float InitialOverlapTolerance = 0.001f;
        public const float GroundTopY = -4.875f;
        public const float MaximumDragDistance = 1.8f;
        public const float CatColliderRadius = 0.38f;
        public const float SlingshotSafetyMargin = 0.15f;
        public const float ProtectedSlingshotRadius = MaximumDragDistance + CatColliderRadius + SlingshotSafetyMargin;

        public static LevelValidationResult Validate(LevelDefinition level, PieceCatalog catalog)
        {
            IReadOnlyCollection<LevelPieceType> mappedTypes = catalog == null
                ? Array.Empty<LevelPieceType>()
                : catalog.Entries.Where(entry => entry.Prefab != null).Select(entry => entry.PieceType).ToArray();
            LevelValidationResult result = Validate(level, mappedTypes);
            if (catalog == null)
            {
                result.AddError("PieceCatalog is not assigned.");
            }
            else
            {
                foreach (string error in catalog.GetConfigurationErrors())
                {
                    result.AddError(error);
                }
            }

            return result;
        }

        public static LevelValidationResult Validate(
            LevelDefinition level,
            IReadOnlyCollection<LevelPieceType> mappedTypes)
        {
            var result = new LevelValidationResult();
            if (level == null)
            {
                result.AddError("LevelDefinition is not assigned.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(level.LevelId))
            {
                result.AddError("Level Id must not be empty.");
            }

            if (level.CatCount < LevelDefinition.MinimumCatCount || level.CatCount > LevelDefinition.MaximumCatCount)
            {
                result.AddError($"Cat Count must be between {LevelDefinition.MinimumCatCount} and {LevelDefinition.MaximumCatCount}.");
            }

            if (!IsFinite(level.SlingshotPosition))
            {
                result.AddError("Slingshot position must be finite.");
            }
            else
            {
                ValidateSlingshotEnvelope(level.SlingshotPosition, result);
            }

            IReadOnlyList<LevelPiecePlacement> pieces = level.Pieces ?? Array.Empty<LevelPiecePlacement>();
            int mouseCount = 0;
            for (int index = 0; index < pieces.Count; index++)
            {
                LevelPiecePlacement placement = pieces[index];
                string label = $"{placement.PieceType} at {Format(placement.Position)}";
                if (!Enum.IsDefined(typeof(LevelPieceType), placement.PieceType))
                {
                    result.AddError($"Placement {index + 1} has unsupported PieceType value {(int)placement.PieceType}.");
                    continue;
                }

                if (placement.PieceType == LevelPieceType.Mouse)
                {
                    mouseCount++;
                }

                if (mappedTypes == null || !mappedTypes.Contains(placement.PieceType))
                {
                    result.AddError($"{label} is not mapped by the PieceCatalog.");
                }

                if (!IsFinite(placement.Position))
                {
                    result.AddError($"{placement.PieceType} placement position must be finite.");
                    continue;
                }

                if (!IsFinite(placement.RotationDegrees))
                {
                    result.AddError($"{label} rotation must be finite.");
                    continue;
                }

                Bounds2D bounds = GetPlacementBounds(placement);
                if (bounds.MinX < PlayableMinX || bounds.MaxX > PlayableMaxX ||
                    bounds.MinY < PlayableMinY || bounds.MaxY > PlayableMaxY)
                {
                    result.AddError($"{label} extends outside playable bounds.");
                }

                if (bounds.MinY < GroundTopY - InitialOverlapTolerance)
                {
                    result.AddError($"{label} extends below ground.");
                }

                if (IsFinite(level.SlingshotPosition) && OverlapsProtectedSlingshot(bounds, level.SlingshotPosition))
                {
                    result.AddError($"{label} overlaps the protected slingshot drag region.");
                }
            }

            if (mouseCount == 0)
            {
                result.AddError("Level must contain at least one Mouse.");
            }

            return result;
        }

        public static Vector2 GetPieceSize(LevelPieceType pieceType)
        {
            switch (pieceType)
            {
                case LevelPieceType.WoodBeam:
                    return new Vector2(1.5f, 0.25f);
                case LevelPieceType.WoodBlock:
                    return new Vector2(0.5f, 0.5f);
                case LevelPieceType.GlassBeam:
                    return new Vector2(1.5f, 0.25f);
                case LevelPieceType.GlassBlock:
                    return new Vector2(0.5f, 0.5f);
                case LevelPieceType.HeavyBlock:
                    return new Vector2(1f, 0.75f);
                case LevelPieceType.Ramp:
                    return new Vector2(1.5f, 0.25f);
                case LevelPieceType.Mouse:
                    return new Vector2(0.6f, 0.6f);
                default:
                    return Vector2.one;
            }
        }

        public static bool IsSignificantInitialPenetration(float signedDistance)
        {
            return IsFinite(signedDistance) && signedDistance < -InitialOverlapTolerance;
        }

        public static float CalculateGeometricPenetration(float signedDistance, float contactOffset)
        {
            if (!IsFinite(signedDistance) || !IsFinite(contactOffset) || contactOffset < 0f)
            {
                return 0f;
            }

            return Mathf.Max(0f, -signedDistance - contactOffset * 2f);
        }

        private static void ValidateSlingshotEnvelope(Vector2 position, LevelValidationResult result)
        {
            if (position.x - ProtectedSlingshotRadius < PlayableMinX ||
                position.x + ProtectedSlingshotRadius > PlayableMaxX ||
                position.y - ProtectedSlingshotRadius < PlayableMinY ||
                position.y + ProtectedSlingshotRadius > PlayableMaxY)
            {
                result.AddError("Slingshot and its full protected pull-back radius must remain inside playable bounds.");
            }

            if (position.y - ProtectedSlingshotRadius <= GroundTopY)
            {
                result.AddError("Slingshot protected pull-back radius must remain above the shared ground.");
            }
        }

        private static Bounds2D GetPlacementBounds(LevelPiecePlacement placement)
        {
            Vector2 size = GetPieceSize(placement.PieceType);
            float radians = placement.RotationDegrees * Mathf.Deg2Rad;
            float cosine = Mathf.Abs(Mathf.Cos(radians));
            float sine = Mathf.Abs(Mathf.Sin(radians));
            float halfWidth = (cosine * size.x + sine * size.y) * 0.5f;
            float halfHeight = (sine * size.x + cosine * size.y) * 0.5f;
            return new Bounds2D(
                placement.Position.x - halfWidth,
                placement.Position.x + halfWidth,
                placement.Position.y - halfHeight,
                placement.Position.y + halfHeight);
        }

        private static bool OverlapsProtectedSlingshot(Bounds2D bounds, Vector2 slingshotPosition)
        {
            float closestX = Mathf.Clamp(slingshotPosition.x, bounds.MinX, bounds.MaxX);
            float closestY = Mathf.Clamp(slingshotPosition.y, bounds.MinY, bounds.MaxY);
            return Vector2.Distance(slingshotPosition, new Vector2(closestX, closestY)) <= ProtectedSlingshotRadius;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string Format(Vector2 position)
        {
            return $"({position.x:0.##}, {position.y:0.##})";
        }

        private readonly struct Bounds2D
        {
            public Bounds2D(float minX, float maxX, float minY, float maxY)
            {
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
            }

            public float MinX { get; }
            public float MaxX { get; }
            public float MinY { get; }
            public float MaxY { get; }
        }
    }
}
