using CatapultCats.Launch;
using UnityEngine;

namespace CatapultCats.Presentation
{
    public sealed class TrajectoryPreview2D : MonoBehaviour
    {
        private static readonly float[] DotTimes =
        {
            0.13f,
            0.21f,
            0.29f,
            0.37f
        };

        [SerializeField] private SlingshotController2D controller;
        [SerializeField] private Rigidbody2D projectileBody;
        [SerializeField] private SpriteRenderer[] dots;

        public SlingshotController2D Controller => controller;
        public Rigidbody2D ProjectileBody => projectileBody;
        public int DotCount => dots?.Length ?? 0;
        public bool HasCompleteDotReferences
        {
            get
            {
                if (dots == null || dots.Length == 0)
                {
                    return false;
                }

                foreach (SpriteRenderer dot in dots)
                {
                    if (dot == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void LateUpdate()
        {
            bool shouldShow = controller != null && projectileBody != null &&
                controller.State == SlingshotController2D.LaunchState.Dragging &&
                LaunchMath2D.IsValidLaunch(controller.CurrentDragOffset, controller.MinimumLaunchDistance);

            if (!shouldShow)
            {
                SetVisible(false);
                return;
            }

            Vector2 start = controller.CurrentProjectilePosition;
            Vector2 velocity = controller.PredictedLaunchVelocity;
            Vector2 gravity = Physics2D.gravity * projectileBody.gravityScale;

            int visibleDots = Mathf.Min(DotTimes.Length, DotCount);
            for (int index = 0; index < DotCount; index++)
            {
                bool visible = index < visibleDots && dots[index] != null;
                if (!visible)
                {
                    if (dots[index] != null)
                    {
                        dots[index].enabled = false;
                    }

                    continue;
                }

                dots[index].enabled = true;
                dots[index].transform.position = LaunchMath2D.EvaluateTrajectoryPosition(
                    start,
                    velocity,
                    gravity,
                    DotTimes[index]);
            }
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (dots == null)
            {
                return;
            }

            foreach (SpriteRenderer dot in dots)
            {
                if (dot != null)
                {
                    dot.enabled = visible;
                }
            }
        }
    }
}
