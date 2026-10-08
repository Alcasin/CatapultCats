using CatapultCats.Launch;
using UnityEngine;

namespace CatapultCats.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CatFlightVisual2D : MonoBehaviour
    {
        [SerializeField] private SlingshotController2D controller;
        [SerializeField] private TrailRenderer trail;

        public SlingshotController2D Controller => controller;
        public TrailRenderer Trail => trail;

        private void LateUpdate()
        {
            if (controller == null || trail == null) return;
            bool flying = controller.State == SlingshotController2D.LaunchState.Launched;
            trail.emitting = flying;
            if (!flying) trail.Clear();
        }
    }
}
