using CatapultCats.Launch;
using UnityEngine;

namespace CatapultCats.Presentation
{
    public sealed class SlingshotVisual2D : MonoBehaviour
    {
        [SerializeField] private SlingshotController2D controller;
        [SerializeField] private Transform leftBandAnchor;
        [SerializeField] private Transform rightBandAnchor;
        [SerializeField] private LineRenderer leftBand;
        [SerializeField] private LineRenderer rightBand;

        public SlingshotController2D Controller => controller;
        public Transform LeftBandAnchor => leftBandAnchor;
        public Transform RightBandAnchor => rightBandAnchor;
        public LineRenderer LeftBand => leftBand;
        public LineRenderer RightBand => rightBand;

        private void LateUpdate()
        {
            if (controller == null || leftBandAnchor == null || rightBandAnchor == null ||
                leftBand == null || rightBand == null)
            {
                return;
            }

            Vector2 bandTarget = controller.State == SlingshotController2D.LaunchState.Launched
                ? controller.AnchorPosition
                : controller.CurrentProjectilePosition;

            UpdateBand(leftBand, leftBandAnchor.position, bandTarget);
            UpdateBand(rightBand, rightBandAnchor.position, bandTarget);
        }

        private static void UpdateBand(LineRenderer band, Vector3 anchor, Vector2 target)
        {
            band.positionCount = 2;
            band.SetPosition(0, anchor);
            band.SetPosition(1, new Vector3(target.x, target.y, 0f));
        }
    }
}
