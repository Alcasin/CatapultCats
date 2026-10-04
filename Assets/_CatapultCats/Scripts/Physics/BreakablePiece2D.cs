using System;
using System.Collections;
using UnityEngine;

namespace CatapultCats.Physics
{
    public sealed class BreakablePiece2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float breakThreshold = 5f;
        [SerializeField] private Rigidbody2D intactBody;
        [SerializeField] private Collider2D intactCollider;
        [SerializeField] private SpriteRenderer intactVisual;
        [SerializeField] private Rigidbody2D[] debrisBodies;
        [SerializeField, Min(0f)] private float debrisVelocityScale = 0.22f;
        [SerializeField, Min(0.1f)] private float debrisLifetime = 3.5f;

        public bool IsBroken { get; private set; }
        public float BreakThreshold => breakThreshold;
        public event Action<BreakablePiece2D> Broken;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryBreak(ImpactMath2D.GetImpactImpulse(collision), collision.relativeVelocity.magnitude);
        }

        public bool TryBreak(float impactImpulse)
        {
            return TryBreak(impactImpulse, 0f);
        }

        private bool TryBreak(float impactImpulse, float debrisSpeed)
        {
            if (IsBroken || !ImpactMath2D.MeetsThreshold(impactImpulse, breakThreshold))
            {
                return false;
            }

            IsBroken = true;
            Vector2 inheritedVelocity = intactBody != null ? intactBody.linearVelocity : Vector2.zero;
            if (intactCollider != null)
            {
                intactCollider.enabled = false;
            }

            if (intactVisual != null)
            {
                intactVisual.enabled = false;
            }

            if (intactBody != null)
            {
                intactBody.linearVelocity = Vector2.zero;
                intactBody.angularVelocity = 0f;
                intactBody.simulated = false;
            }

            ActivateDebris(inheritedVelocity, debrisSpeed);
            Broken?.Invoke(this);
            return true;
        }

        private void ActivateDebris(Vector2 inheritedVelocity, float debrisSpeed)
        {
            if (debrisBodies == null)
            {
                return;
            }

            for (int index = 0; index < debrisBodies.Length; index++)
            {
                Rigidbody2D debris = debrisBodies[index];
                if (debris == null)
                {
                    continue;
                }

                debris.gameObject.SetActive(true);
                debris.simulated = true;
                debris.bodyType = RigidbodyType2D.Dynamic;
                Vector2 direction = new Vector2(index % 2 == 0 ? -1f : 1f, 0.65f).normalized;
                debris.linearVelocity = inheritedVelocity + direction * debrisSpeed * debrisVelocityScale;
                debris.angularVelocity = (index % 2 == 0 ? -1f : 1f) * 90f;
            }

            StartCoroutine(DeactivateDebrisAfterDelay());
        }

        private IEnumerator DeactivateDebrisAfterDelay()
        {
            yield return new WaitForSeconds(debrisLifetime);
            foreach (Rigidbody2D debris in debrisBodies)
            {
                if (debris != null)
                {
                    debris.gameObject.SetActive(false);
                }
            }
        }
    }
}
