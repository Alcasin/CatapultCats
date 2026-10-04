using UnityEngine;

namespace CatapultCats.Physics
{
    public static class ImpactMath2D
    {
        public static float GetImpactImpulse(Collision2D collision)
        {
            if (collision == null || collision.contactCount <= 0)
            {
                return 0f;
            }

            float totalNormalImpulse = 0f;
            for (int index = 0; index < collision.contactCount; index++)
            {
                float normalImpulse = collision.GetContact(index).normalImpulse;
                if (!IsFiniteNonnegative(normalImpulse))
                {
                    return 0f;
                }

                totalNormalImpulse += normalImpulse;
                if (!IsFiniteNonnegative(totalNormalImpulse))
                {
                    return 0f;
                }
            }

            return totalNormalImpulse;
        }

        public static bool MeetsThreshold(float impactImpulse, float threshold)
        {
            return IsFiniteNonnegative(impactImpulse) &&
                IsFiniteNonnegative(threshold) &&
                impactImpulse >= threshold;
        }

        public static bool QualifiesAsCrush(
            float contactingMass,
            float crushMassThreshold,
            float contactingBodyCenterY,
            float targetCenterY,
            float contactPointY)
        {
            const float topContactTolerance = 0.05f;
            return IsFiniteNonnegative(contactingMass) &&
                IsFiniteNonnegative(crushMassThreshold) &&
                IsFinite(contactingBodyCenterY) &&
                IsFinite(targetCenterY) &&
                IsFinite(contactPointY) &&
                contactingMass >= crushMassThreshold &&
                contactingBodyCenterY > targetCenterY + topContactTolerance &&
                contactPointY > targetCenterY + topContactTolerance;
        }

        public static bool IsFiniteNonnegative(float value)
        {
            return value >= 0f && IsFinite(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
