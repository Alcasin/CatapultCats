using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatapultCats.Launch
{
    public sealed class SlingshotController2D : MonoBehaviour
    {
        public enum LaunchState
        {
            Ready,
            Dragging,
            Launched
        }

        [Header("Scene References")]
        [SerializeField] private Transform launchAnchor;
        [SerializeField] private Rigidbody2D projectileBody;
        [SerializeField] private Collider2D projectileCollider;
        [SerializeField] private Camera inputCamera;

        [Header("Launch Tuning")]
        [SerializeField, Min(0.01f)] private float maxDragDistance = 1.8f;
        [SerializeField, Min(0f)] private float minimumLaunchDistance = 0.2f;
        [SerializeField, Min(0f)] private float launchSpeedPerUnit = 10.5f;

        private InputAction pointerPositionAction;
        private InputAction pointerPressAction;
        private Vector2 currentDragOffset;
        private LaunchState state;

        public LaunchState State => state;
        public Vector2 CurrentDragOffset => currentDragOffset;
        public Vector2 CurrentProjectilePosition => projectileBody != null ? projectileBody.position : Vector2.zero;
        public Vector2 AnchorPosition => launchAnchor != null ? (Vector2)launchAnchor.position : Vector2.zero;
        public float MaximumDragDistance => maxDragDistance;
        public float MinimumLaunchDistance => minimumLaunchDistance;
        public float LaunchSpeedPerUnit => launchSpeedPerUnit;
        public Vector2 PredictedLaunchVelocity =>
            LaunchMath2D.CalculateLaunchVelocity(currentDragOffset, launchSpeedPerUnit);

        public Transform LaunchAnchor => launchAnchor;
        public Rigidbody2D ProjectileBody => projectileBody;
        public Collider2D ProjectileCollider => projectileCollider;
        public Camera InputCamera => inputCamera;
        public event Action<Vector2> Launched;

        public void ResetForAiming()
        {
            ResetToReady();
        }

        public void SetAnchorPosition(Vector2 worldPosition)
        {
            if (launchAnchor == null)
            {
                return;
            }

            Vector2 delta = worldPosition - AnchorPosition;
            if (delta.sqrMagnitude > 0f)
            {
                transform.position += (Vector3)delta;
                foreach (LineRenderer line in GetComponentsInChildren<LineRenderer>(true))
                {
                    if (!line.useWorldSpace)
                    {
                        continue;
                    }

                    for (int index = 0; index < line.positionCount; index++)
                    {
                        line.SetPosition(index, line.GetPosition(index) + (Vector3)delta);
                    }
                }
            }

            ResetToReady();
        }

        private void OnEnable()
        {
            CreateInputActionsIfNeeded();
            pointerPositionAction.Enable();
            pointerPressAction.Enable();
            ResetToReady();
        }

        private void OnDisable()
        {
            pointerPositionAction?.Disable();
            pointerPressAction?.Disable();
        }

        private void OnDestroy()
        {
            pointerPositionAction?.Dispose();
            pointerPressAction?.Dispose();
        }

        private void Update()
        {
            if (!HasRequiredReferences() || state == LaunchState.Launched)
            {
                return;
            }

            Vector2 pointerWorldPosition = ReadPointerWorldPosition();

            if (state == LaunchState.Ready && pointerPressAction.WasPressedThisFrame())
            {
                TryBeginDrag(pointerWorldPosition);
            }

            if (state != LaunchState.Dragging)
            {
                return;
            }

            UpdateDrag(pointerWorldPosition);
            if (pointerPressAction.WasReleasedThisFrame())
            {
                ReleaseOrCancel();
            }
        }

        private void CreateInputActionsIfNeeded()
        {
            if (pointerPositionAction == null)
            {
                pointerPositionAction = new InputAction(
                    "Pointer Position",
                    InputActionType.PassThrough,
                    "<Pointer>/position");
            }

            if (pointerPressAction == null)
            {
                pointerPressAction = new InputAction(
                    "Pointer Press",
                    InputActionType.Button,
                    "<Pointer>/press");
            }
        }

        private bool HasRequiredReferences()
        {
            return launchAnchor != null && projectileBody != null && projectileCollider != null && inputCamera != null;
        }

        private Vector2 ReadPointerWorldPosition()
        {
            Vector2 screenPosition = pointerPositionAction.ReadValue<Vector2>();
            float depth = Mathf.Abs(projectileBody.transform.position.z - inputCamera.transform.position.z);
            Vector3 worldPosition = inputCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, depth));
            return worldPosition;
        }

        private void TryBeginDrag(Vector2 pointerWorldPosition)
        {
            if (!projectileCollider.OverlapPoint(pointerWorldPosition))
            {
                return;
            }

            state = LaunchState.Dragging;
            UpdateDrag(pointerWorldPosition);
        }

        private void UpdateDrag(Vector2 pointerWorldPosition)
        {
            Vector2 rawDrag = pointerWorldPosition - AnchorPosition;
            currentDragOffset = LaunchMath2D.ClampDrag(rawDrag, maxDragDistance);
            projectileBody.position = AnchorPosition + currentDragOffset;
            projectileBody.linearVelocity = Vector2.zero;
            projectileBody.angularVelocity = 0f;
        }

        private void ReleaseOrCancel()
        {
            if (!LaunchMath2D.IsValidLaunch(currentDragOffset, minimumLaunchDistance))
            {
                ResetToReady();
                return;
            }

            Vector2 launchVelocity = LaunchMath2D.CalculateLaunchVelocity(currentDragOffset, launchSpeedPerUnit);
            projectileBody.position = AnchorPosition + currentDragOffset;
            projectileBody.linearVelocity = Vector2.zero;
            projectileBody.angularVelocity = 0f;
            projectileBody.bodyType = RigidbodyType2D.Dynamic;
            projectileBody.linearVelocity = launchVelocity;
            state = LaunchState.Launched;
            Launched?.Invoke(launchVelocity);
        }

        private void ResetToReady()
        {
            state = LaunchState.Ready;
            currentDragOffset = Vector2.zero;

            if (projectileBody == null || launchAnchor == null)
            {
                return;
            }

            projectileBody.bodyType = RigidbodyType2D.Kinematic;
            projectileBody.linearVelocity = Vector2.zero;
            projectileBody.angularVelocity = 0f;
            projectileBody.position = AnchorPosition;
            projectileBody.rotation = 0f;
        }
    }
}
