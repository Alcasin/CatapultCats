using CatapultCats.Launch;
using NUnit.Framework;
using UnityEngine;

namespace CatapultCats.Tests.EditMode
{
    public sealed class LaunchMath2DTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void ClampDrag_ZeroDragRemainsZero()
        {
            AssertVector(Vector2.zero, LaunchMath2D.ClampDrag(Vector2.zero, 1.8f));
        }

        [Test]
        public void ClampDrag_InsideMaximumRadiusRemainsUnchanged()
        {
            var drag = new Vector2(-0.7f, 0.4f);
            AssertVector(drag, LaunchMath2D.ClampDrag(drag, 1.8f));
        }

        [Test]
        public void ClampDrag_ExcessiveDragClampsToMaximumMagnitude()
        {
            Vector2 result = LaunchMath2D.ClampDrag(new Vector2(3f, 4f), 1.8f);
            Assert.That(result.magnitude, Is.EqualTo(1.8f).Within(Tolerance));
        }

        [Test]
        public void ClampDrag_PreservesDirection()
        {
            Vector2 source = new Vector2(-3f, -4f);
            Vector2 result = LaunchMath2D.ClampDrag(source, 1.8f);
            Assert.That(Vector2.Dot(source.normalized, result.normalized), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void CalculateLaunchVelocity_IsOppositeDragDirection()
        {
            Vector2 drag = new Vector2(-1f, -0.5f);
            Vector2 velocity = LaunchMath2D.CalculateLaunchVelocity(drag, 7f);
            Assert.That(Vector2.Dot(drag, velocity), Is.LessThan(0f));
        }

        [Test]
        public void CalculateLaunchVelocity_ScalesWithDrag()
        {
            AssertVector(new Vector2(3.5f, -7f), LaunchMath2D.CalculateLaunchVelocity(new Vector2(-0.5f, 1f), 7f));
        }

        [Test]
        public void CalculateLaunchVelocity_MaximumDragProducesExpectedSpeed()
        {
            Vector2 velocity = LaunchMath2D.CalculateLaunchVelocity(Vector2.left * 1.8f, 10.5f);
            Assert.That(velocity.magnitude, Is.EqualTo(18.9f).Within(Tolerance));
        }

        [Test]
        public void IsValidLaunch_BelowMinimumIsInvalid()
        {
            Assert.That(LaunchMath2D.IsValidLaunch(Vector2.right * 0.199f, 0.2f), Is.False);
        }

        [Test]
        public void IsValidLaunch_EqualityAtMinimumIsValid()
        {
            Assert.That(LaunchMath2D.IsValidLaunch(Vector2.up * 0.2f, 0.2f), Is.True);
        }

        [Test]
        public void EvaluateTrajectoryPosition_AtZeroTimeEqualsStart()
        {
            Vector2 start = new Vector2(2f, 3f);
            AssertVector(start, LaunchMath2D.EvaluateTrajectoryPosition(start, new Vector2(4f, 5f), Vector2.down * 9.81f, 0f));
        }

        [Test]
        public void EvaluateTrajectoryPosition_GravityAffectsExpectedAxis()
        {
            Vector2 result = LaunchMath2D.EvaluateTrajectoryPosition(Vector2.zero, Vector2.right, new Vector2(0f, -10f), 1f);
            AssertVector(new Vector2(1f, -5f), result);
        }

        [Test]
        public void EvaluateTrajectoryPosition_KnownCaseIsDeterministic()
        {
            Vector2 result = LaunchMath2D.EvaluateTrajectoryPosition(
                new Vector2(-2f, 1f),
                new Vector2(6f, 8f),
                new Vector2(0f, -10f),
                0.5f);
            AssertVector(new Vector2(1f, 3.75f), result);
        }

        [Test]
        public void InvalidConfiguration_ReturnsSafeResults()
        {
            AssertVector(Vector2.zero, LaunchMath2D.ClampDrag(Vector2.one, float.NaN));
            AssertVector(Vector2.zero, LaunchMath2D.CalculateLaunchVelocity(Vector2.one, -1f));
            Assert.That(LaunchMath2D.IsValidLaunch(Vector2.one, float.PositiveInfinity), Is.False);
        }

        private static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance));
        }
    }
}
