using System;
using UnityEngine;

namespace CatapultCats.Physics
{
    public sealed class MouseTarget2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float defeatThreshold = 3f;
        [SerializeField, Min(0f)] private float crushMassThreshold = 4f;
        [SerializeField] private Rigidbody2D targetBody;
        [SerializeField] private Collider2D targetCollider;
        [SerializeField] private SpriteRenderer targetVisual;

        public bool IsDefeated { get; private set; }
        public float DefeatThreshold => defeatThreshold;
        public float CrushMassThreshold => crushMassThreshold;
        public event Action<MouseTarget2D> Defeated;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!TryDefeat(ImpactMath2D.GetImpactImpulse(collision)))
            {
                TryDefeatByCrush(collision);
            }
        }

        public bool TryDefeat(float impactImpulse)
        {
            if (IsDefeated || !ImpactMath2D.MeetsThreshold(impactImpulse, defeatThreshold))
            {
                return false;
            }

            Defeat();
            return true;
        }

        private bool TryDefeatByCrush(Collision2D collision)
        {
            if (IsDefeated || collision == null)
            {
                return false;
            }

            Rigidbody2D contactingBody = collision.rigidbody;
            if (contactingBody == null || contactingBody == targetBody)
            {
                contactingBody = collision.otherRigidbody;
            }

            if (contactingBody == null || contactingBody == targetBody)
            {
                return false;
            }

            float targetCenterY = targetBody != null ? targetBody.worldCenterOfMass.y : transform.position.y;
            for (int index = 0; index < collision.contactCount; index++)
            {
                ContactPoint2D contact = collision.GetContact(index);
                if (ImpactMath2D.QualifiesAsCrush(
                    contactingBody.mass,
                    crushMassThreshold,
                    contactingBody.worldCenterOfMass.y,
                    targetCenterY,
                    contact.point.y))
                {
                    Defeat();
                    return true;
                }
            }

            return false;
        }

        private void Defeat()
        {
            IsDefeated = true;
            if (targetCollider != null)
            {
                targetCollider.enabled = false;
            }

            if (targetBody != null)
            {
                targetBody.linearVelocity = Vector2.zero;
                targetBody.angularVelocity = 0f;
                targetBody.simulated = false;
            }

            if (targetVisual != null)
            {
                targetVisual.transform.localScale = new Vector3(1.1f, 0.28f, 1f);
                targetVisual.color = new Color(0.72f, 0.72f, 0.72f, 0.65f);
            }

            Defeated?.Invoke(this);
        }
    }
}
