using UnityEngine;

namespace CatapultCats.Launch
{
    public static class LaunchMath2D
    {
        public static Vector2 ClampDrag(Vector2 rawDragOffset, float maximumDragDistance)
        {
            if (!IsFinite(rawDragOffset) || !IsFinite(maximumDragDistance) || maximumDragDistance <= 0f)
            {
                return Vector2.zero;
            }

            float maximumDistanceSquared = maximumDragDistance * maximumDragDistance;
            if (rawDragOffset.sqrMagnitude <= maximumDistanceSquared)
            {
                return rawDragOffset;
            }

            return rawDragOffset.normalized * maximumDragDistance;
        }

        public static bool IsValidLaunch(Vector2 dragOffset, float minimumLaunchDistance)
        {
            if (!IsFinite(dragOffset) || !IsFinite(minimumLaunchDistance) || minimumLaunchDistance < 0f)
            {
                return false;
            }

            return dragOffset.sqrMagnitude >= minimumLaunchDistance * minimumLaunchDistance;
        }

        public static Vector2 CalculateLaunchVelocity(Vector2 dragOffset, float launchSpeedPerUnit)
        {
            if (!IsFinite(dragOffset) || !IsFinite(launchSpeedPerUnit) || launchSpeedPerUnit < 0f)
            {
                return Vector2.zero;
            }

            return -dragOffset * launchSpeedPerUnit;
        }

        public static Vector2 EvaluateTrajectoryPosition(
            Vector2 startPosition,
            Vector2 initialVelocity,
            Vector2 gravity,
            float time)
        {
            Vector2 safeStart = IsFinite(startPosition) ? startPosition : Vector2.zero;
            if (!IsFinite(initialVelocity) || !IsFinite(gravity) || !IsFinite(time) || time < 0f)
            {
                return safeStart;
            }

            return safeStart + initialVelocity * time + 0.5f * gravity * time * time;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
