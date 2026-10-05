using System;
using System.Linq;
using CatapultCats.Levels;
using NUnit.Framework;
using UnityEngine;

namespace CatapultCats.Tests.EditMode
{
    public sealed class LevelValidationTests
    {
        private static readonly LevelPieceType[] MouseMapping = { LevelPieceType.Mouse };
        private LevelDefinition level;

        [SetUp]
        public void SetUp()
        {
            level = ScriptableObject.CreateInstance<LevelDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(level);
        }

        [Test]
        public void EmptyLevelIdIsRejected()
        {
            ConfigureValid(levelId: " ");
            AssertError("Level Id", Validate());
        }

        [Test]
        public void ZeroMiceIsRejected()
        {
            level.Configure("NoMouse", 3, LevelDefinition.DefaultSlingshotPosition, Array.Empty<LevelPiecePlacement>());
            AssertError("at least one Mouse", Validate());
        }

        [Test]
        public void ValidSimpleLevelIsAccepted()
        {
            ConfigureValid();
            Assert.That(Validate().IsValid, Is.True);
        }

        [Test]
        public void CatCountBelowSupportedRangeIsRejected()
        {
            ConfigureValid(catCount: 0);
            AssertError("Cat Count", Validate());
        }

        [Test]
        public void CatCountAboveSupportedRangeIsRejected()
        {
            ConfigureValid(catCount: 6);
            AssertError("Cat Count", Validate());
        }

        [Test]
        public void NonfiniteSlingshotPositionIsRejected()
        {
            ConfigureValid(slingshot: new Vector2(float.NaN, -2.4f));
            AssertError("Slingshot position", Validate());
        }

        [Test]
        public void NonfinitePlacementPositionIsRejected()
        {
            ConfigureValid(piecePosition: new Vector2(float.PositiveInfinity, 0f));
            AssertError("placement position", Validate());
        }

        [Test]
        public void NonfinitePlacementRotationIsRejected()
        {
            level.Configure(
                "BadRotation",
                3,
                LevelDefinition.DefaultSlingshotPosition,
                new[] { new LevelPiecePlacement(LevelPieceType.Mouse, new Vector2(3f, -4.6f), float.NaN) });
            AssertError("rotation", Validate());
        }

        [Test]
        public void UnmappedPieceTypeIsRejected()
        {
            ConfigureValid();
            AssertError("not mapped", LevelValidation.Validate(level, Array.Empty<LevelPieceType>()));
        }

        [Test]
        public void SlingshotEnvelopeOutsidePlayableBoundsIsRejected()
        {
            ConfigureValid(slingshot: new Vector2(-9f, -2.4f));
            AssertError("pull-back radius", Validate());
        }

        [Test]
        public void PieceInsideProtectedSlingshotRegionIsRejected()
        {
            ConfigureValid(piecePosition: new Vector2(-4.5f, -2.4f));
            AssertError("protected slingshot", Validate());
        }

        [Test]
        public void PieceBelowGroundIsRejected()
        {
            ConfigureValid(piecePosition: new Vector2(3f, -4.8f));
            AssertError("below ground", Validate());
        }

        [Test]
        public void ConstructionDimensionsAlignToModularGrid()
        {
            LevelPieceType[] structureTypes =
            {
                LevelPieceType.WoodBeam,
                LevelPieceType.WoodBlock,
                LevelPieceType.GlassBeam,
                LevelPieceType.GlassBlock,
                LevelPieceType.HeavyBlock,
                LevelPieceType.Ramp
            };

            foreach (LevelPieceType pieceType in structureTypes)
            {
                Vector2 size = LevelValidation.GetPieceSize(pieceType);
                Assert.That(
                    size.x / LevelValidation.ConstructionGridUnit,
                    Is.EqualTo(Mathf.Round(size.x / LevelValidation.ConstructionGridUnit)).Within(0.0001f),
                    $"{pieceType} width is not grid-aligned.");
                Assert.That(
                    size.y / LevelValidation.ConstructionGridUnit,
                    Is.EqualTo(Mathf.Round(size.y / LevelValidation.ConstructionGridUnit)).Within(0.0001f),
                    $"{pieceType} height is not grid-aligned.");
            }
        }

        [TestCase(LevelPieceType.WoodBeam, 1.5f, 0.25f)]
        [TestCase(LevelPieceType.WoodBlock, 0.5f, 0.5f)]
        [TestCase(LevelPieceType.GlassBeam, 1.5f, 0.25f)]
        [TestCase(LevelPieceType.GlassBlock, 0.5f, 0.5f)]
        [TestCase(LevelPieceType.HeavyBlock, 1f, 0.75f)]
        [TestCase(LevelPieceType.Ramp, 1.5f, 0.25f)]
        [TestCase(LevelPieceType.Mouse, 0.6f, 0.6f)]
        public void AuthoringDimensionsMatchFixedStandard(LevelPieceType pieceType, float width, float height)
        {
            Vector2 size = LevelValidation.GetPieceSize(pieceType);
            Assert.That(size.x, Is.EqualTo(width).Within(0.0001f));
            Assert.That(size.y, Is.EqualTo(height).Within(0.0001f));
        }

        [TestCase(-0.02f, false)]
        [TestCase(-0.0205f, false)]
        [TestCase(-0.04f, true)]
        public void ContactEnvelopeIsRemovedBeforeTestingActualPenetration(float colliderDistance, bool expected)
        {
            float penetration = LevelValidation.CalculateGeometricPenetration(colliderDistance, 0.01f);
            Assert.That(LevelValidation.IsSignificantInitialPenetration(-penetration), Is.EqualTo(expected));
        }

        private void ConfigureValid(
            string levelId = "Simple",
            int catCount = 3,
            Vector2? slingshot = null,
            Vector2? piecePosition = null)
        {
            level.Configure(
                levelId,
                catCount,
                slingshot ?? LevelDefinition.DefaultSlingshotPosition,
                new[]
                {
                    new LevelPiecePlacement(
                        LevelPieceType.Mouse,
                        piecePosition ?? new Vector2(3f, -4.575f),
                        0f)
                });
        }

        private LevelValidationResult Validate()
        {
            return LevelValidation.Validate(level, MouseMapping);
        }

        private static void AssertError(string text, LevelValidationResult result)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.Contains(text)), Is.True, string.Join("\n", result.Errors));
        }
    }
}
