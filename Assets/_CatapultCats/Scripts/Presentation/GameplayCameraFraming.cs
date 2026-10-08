using UnityEngine;

namespace CatapultCats.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class GameplayCameraFraming : MonoBehaviour
    {
        private Camera view;
        private void Awake() => view = GetComponent<Camera>();
        private void LateUpdate()
        {
            // Keep the accepted landscape view and expose the full pullback/target zone on narrow screens.
            view.orthographicSize = Mathf.Max(5.4f, 9.6f / Mathf.Max(0.1f, view.aspect));
        }
    }
}
