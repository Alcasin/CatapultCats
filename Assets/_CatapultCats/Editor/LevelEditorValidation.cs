using System.Collections.Generic;
using CatapultCats.Levels;
using CatapultCats.Physics;
using UnityEngine;

namespace CatapultCats.Editor
{
    internal static class LevelEditorValidation
    {
        public static IReadOnlyList<string> ValidateCatalogPrefabs(PieceCatalog catalog)
        {
            var errors = new List<string>();
            if (catalog == null)
            {
                errors.Add("PieceCatalog is not assigned.");
                return errors;
            }

            foreach (string error in catalog.GetConfigurationErrors())
            {
                errors.Add(error);
            }

            foreach (PieceCatalog.Entry entry in catalog.Entries)
            {
                GameObject prefab = entry.Prefab;
                if (prefab == null)
                {
                    continue;
                }

                if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Collider>(true).Length > 0)
                {
                    errors.Add($"{entry.PieceType} prefab contains forbidden 3D physics components.");
                }

                switch (entry.PieceType)
                {
                    case LevelPieceType.WoodBeam:
                    case LevelPieceType.WoodBlock:
                        ValidateBreakable(prefab, 5f, entry.PieceType, errors);
                        break;
                    case LevelPieceType.GlassBeam:
                    case LevelPieceType.GlassBlock:
                        ValidateBreakable(prefab, 1.5f, entry.PieceType, errors);
                        break;
                    case LevelPieceType.HeavyBlock:
                        Rigidbody2D heavyBody = prefab.GetComponent<Rigidbody2D>();
                        if (heavyBody == null || heavyBody.bodyType != RigidbodyType2D.Dynamic || Mathf.Abs(heavyBody.mass - 5f) > 0.001f)
                        {
                            errors.Add("HeavyBlock prefab must have a dynamic Rigidbody2D with mass 5.");
                        }

                        if (prefab.GetComponent<BreakablePiece2D>() != null || prefab.GetComponent<BoxCollider2D>() == null)
                        {
                            errors.Add("HeavyBlock prefab must be an unbreakable BoxCollider2D body.");
                        }

                        break;
                    case LevelPieceType.Mouse:
                        MouseTarget2D mouse = prefab.GetComponent<MouseTarget2D>();
                        if (mouse == null || Mathf.Abs(mouse.DefeatThreshold - 3f) > 0.001f || Mathf.Abs(mouse.CrushMassThreshold - 4f) > 0.001f)
                        {
                            errors.Add("Mouse prefab must preserve MouseTarget2D thresholds 3 and 4.");
                        }

                        break;
                    case LevelPieceType.Ramp:
                        if (prefab.GetComponent<Collider2D>() == null || prefab.GetComponent<Rigidbody2D>() != null)
                        {
                            errors.Add("Ramp prefab must have a static 2D collider and no Rigidbody2D.");
                        }

                        break;
                }

                if (entry.PieceType != LevelPieceType.Mouse)
                {
                    ValidateStructureDimensions(prefab, entry.PieceType, errors);
                }
            }

            return errors;
        }

        public static IReadOnlyList<string> ValidatePreviewPenetrations(Transform authoringRoot)
        {
            var errors = new List<string>();
            if (authoringRoot == null)
            {
                return errors;
            }

            var colliders = new List<Collider2D>();
            for (int index = 0; index < authoringRoot.childCount; index++)
            {
                Collider2D collider = authoringRoot.GetChild(index).GetComponent<Collider2D>();
                if (collider != null && collider.enabled)
                {
                    colliders.Add(collider);
                }
            }

            Physics2D.SyncTransforms();
            for (int first = 0; first < colliders.Count; first++)
            {
                for (int second = first + 1; second < colliders.Count; second++)
                {
                    ColliderDistance2D distance = Physics2D.Distance(colliders[first], colliders[second]);
                    float penetrationDepth = LevelValidation.CalculateGeometricPenetration(
                        distance.distance,
                        Physics2D.defaultContactOffset);
                    if (distance.isOverlapped &&
                        LevelValidation.IsSignificantInitialPenetration(-penetrationDepth))
                    {
                        errors.Add(
                            $"{colliders[first].name} overlaps {colliders[second].name} " +
                            $"by {penetrationDepth:0.###} units.");
                    }
                }
            }

            return errors;
        }

        private static void ValidateStructureDimensions(
            GameObject prefab,
            LevelPieceType pieceType,
            ICollection<string> errors)
        {
            BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                return;
            }

            Vector3 scale = prefab.transform.localScale;
            Vector2 actualSize = Vector2.Scale(
                collider.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Vector2 expectedSize = LevelValidation.GetPieceSize(pieceType);
            if ((actualSize - expectedSize).sqrMagnitude > 0.000001f)
            {
                errors.Add(
                    $"{pieceType} collider must be {expectedSize.x:0.###} x {expectedSize.y:0.###}; " +
                    $"found {actualSize.x:0.###} x {actualSize.y:0.###}.");
            }

            if (collider.edgeRadius > 0.000001f)
            {
                errors.Add($"{pieceType} collider edge radius must be zero for modular face contact.");
            }
        }

        private static void ValidateBreakable(
            GameObject prefab,
            float expectedThreshold,
            LevelPieceType pieceType,
            ICollection<string> errors)
        {
            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            BreakablePiece2D breakable = prefab.GetComponent<BreakablePiece2D>();
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic || prefab.GetComponent<BoxCollider2D>() == null)
            {
                errors.Add($"{pieceType} prefab must have a dynamic Rigidbody2D and BoxCollider2D.");
            }

            if (breakable == null || Mathf.Abs(breakable.BreakThreshold - expectedThreshold) > 0.001f)
            {
                errors.Add($"{pieceType} prefab must use break threshold {expectedThreshold:0.0}.");
            }

            Rigidbody2D[] bodies = prefab.GetComponentsInChildren<Rigidbody2D>(true);
            if (bodies.Length < 2)
            {
                errors.Add($"{pieceType} prefab must contain pre-authored debris bodies.");
            }
        }
    }
}
