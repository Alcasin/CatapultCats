using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatapultCats.Physics
{
    public sealed class PhysicsResolver2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D projectileBody;
        [SerializeField, Min(0f)] private float minimumObservationTime = 0.3f;
        [SerializeField, Min(0f)] private float linearSettleSpeed = 0.15f;
        [SerializeField, Min(0f)] private float angularSettleSpeed = 10f;
        [SerializeField, Min(0f)] private float settleHoldDuration = 0.35f;
        [SerializeField, Min(0.1f)] private float maximumResolutionTime = 5f;
        [SerializeField] private Vector2 horizontalBounds = new Vector2(-11f, 11f);
        [SerializeField] private float projectileMinimumY = -6.2f;

        private float elapsed;
        private float settledTime;
        private Rigidbody2D[] criticalBodies = Array.Empty<Rigidbody2D>();

        public bool IsResolving { get; private set; }
        public float MinimumObservationTime => minimumObservationTime;
        public Rigidbody2D ProjectileBody => projectileBody;
        public Rigidbody2D[] CriticalBodies => criticalBodies;
        public event Action Resolved;

        public void ConfigureBodies(Rigidbody2D currentProjectileBody, IEnumerable<Rigidbody2D> monitoredBodies)
        {
            projectileBody = currentProjectileBody;
            criticalBodies = monitoredBodies != null
                ? monitoredBodies.Where(body => body != null).Distinct().ToArray()
                : Array.Empty<Rigidbody2D>();
            IsResolving = false;
            elapsed = 0f;
            settledTime = 0f;
        }

        public void BeginResolution()
        {
            elapsed = 0f;
            settledTime = 0f;
            IsResolving = true;
        }

        private void Update()
        {
            if (!IsResolving)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < minimumObservationTime)
            {
                return;
            }

            if (ProjectileIsOutOfBounds() || elapsed >= maximumResolutionTime)
            {
                CompleteResolution();
                return;
            }

            if (AllCriticalBodiesSettled())
            {
                settledTime += Time.deltaTime;
                if (settledTime >= settleHoldDuration)
                {
                    CompleteResolution();
                }
            }
            else
            {
                settledTime = 0f;
            }
        }

        private bool ProjectileIsOutOfBounds()
        {
            if (projectileBody == null)
            {
                return true;
            }

            Vector2 position = projectileBody.position;
            return position.y < projectileMinimumY ||
                position.x < horizontalBounds.x ||
                position.x > horizontalBounds.y;
        }

        private bool AllCriticalBodiesSettled()
        {
            if (criticalBodies == null || criticalBodies.Length == 0)
            {
                return true;
            }

            foreach (Rigidbody2D body in criticalBodies)
            {
                if (body == null || !body.simulated || !body.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (body.linearVelocity.magnitude > linearSettleSpeed ||
                    Mathf.Abs(body.angularVelocity) > angularSettleSpeed)
                {
                    return false;
                }
            }

            return true;
        }

        private void CompleteResolution()
        {
            IsResolving = false;
            Resolved?.Invoke();
        }
    }
}
