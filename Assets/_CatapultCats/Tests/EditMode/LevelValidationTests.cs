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
                        piecePosition ?? new Vector2(3f, -4.6f),
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
