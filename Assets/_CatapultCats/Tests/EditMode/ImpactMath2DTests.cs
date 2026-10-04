using CatapultCats.Physics;
using NUnit.Framework;

namespace CatapultCats.Tests.EditMode
{
    public sealed class ImpactMath2DTests
    {
        [Test]
        public void ZeroImpulseBelowPositiveThresholdDoesNotSucceed()
        {
            Assert.That(ImpactMath2D.MeetsThreshold(0f, 1f), Is.False);
        }

        [Test]
        public void BelowThresholdDoesNotSucceed()
        {
            Assert.That(ImpactMath2D.MeetsThreshold(4.99f, 5f), Is.False);
        }

        [Test]
        public void EqualityAtThresholdSucceeds()
        {
            Assert.That(ImpactMath2D.MeetsThreshold(5f, 5f), Is.True);
        }

        [Test]
        public void AboveThresholdSucceeds()
        {
            Assert.That(ImpactMath2D.MeetsThreshold(5.01f, 5f), Is.True);
        }

        [Test]
        public void NegativeImpulseIsRejected()
        {
            Assert.That(ImpactMath2D.MeetsThreshold(-1f, 5f), Is.False);
        }

        [Test]
        public void NaNImpulseIsRejected()
        {
            Assert.That(ImpactMath2D.MeetsThreshold(float.NaN, 1f), Is.False);
        }

        [Test]
        public void PositiveInfinityImpulseIsRejected()
        {
            Assert.That(ImpactMath2D.MeetsThreshold(float.PositiveInfinity, 1f), Is.False);
        }

        [Test]
        public void MassBelowCrushThresholdDoesNotQualify()
        {
            Assert.That(ImpactMath2D.QualifiesAsCrush(3.99f, 4f, 1f, 0f, 0.5f), Is.False);
        }

        [Test]
        public void MassEqualToCrushThresholdQualifiesFromAbove()
        {
            Assert.That(ImpactMath2D.QualifiesAsCrush(4f, 4f, 1f, 0f, 0.5f), Is.True);
        }

        [Test]
        public void MassAboveCrushThresholdQualifiesFromAbove()
        {
            Assert.That(ImpactMath2D.QualifiesAsCrush(5f, 4f, 1f, 0f, 0.5f), Is.True);
        }

        [Test]
        public void HeavyBodyBelowOrAtSideDoesNotQualify()
        {
            Assert.That(ImpactMath2D.QualifiesAsCrush(5f, 4f, -1f, 0f, -0.5f), Is.False);
            Assert.That(ImpactMath2D.QualifiesAsCrush(5f, 4f, 0.5f, 0f, 0f), Is.False);
        }

        [Test]
        public void InvalidCrushingMassIsRejected()
        {
            Assert.That(ImpactMath2D.QualifiesAsCrush(float.NaN, 4f, 1f, 0f, 0.5f), Is.False);
            Assert.That(ImpactMath2D.QualifiesAsCrush(float.PositiveInfinity, 4f, 1f, 0f, 0.5f), Is.False);
        }
    }
}
