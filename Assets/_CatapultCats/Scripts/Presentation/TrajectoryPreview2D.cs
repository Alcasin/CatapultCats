using CatapultCats.Launch;
using UnityEngine;

namespace CatapultCats.Presentation
{
    public sealed class TrajectoryPreview2D : MonoBehaviour
    {
        private const float SolidSegmentTime = 0.18f;

        private static readonly float[] DotTimes =
        {
            0.35f,
            0.55f,
            0.75f,
            0.95f,
            1.15f
        };

        [SerializeField] private SlingshotController2D controller;
        [SerializeField] private Rigidbody2D projectileBody;
        [SerializeField] private LineRenderer solidSegment;
        [SerializeField] private SpriteRenderer[] dots;

        public SlingshotController2D Controller => controller;
        public Rigidbody2D ProjectileBody => projectileBody;
        public LineRenderer SolidSegment => solidSegment;
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
            bool shouldShow = controller != null && projectileBody != null && solidSegment != null &&
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

            solidSegment.enabled = true;
            solidSegment.positionCount = 2;
            solidSegment.SetPosition(0, start);
            solidSegment.SetPosition(
                1,
                LaunchMath2D.EvaluateTrajectoryPosition(start, velocity, gravity, SolidSegmentTime));

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
            if (solidSegment != null)
            {
                solidSegment.enabled = visible;
            }

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
