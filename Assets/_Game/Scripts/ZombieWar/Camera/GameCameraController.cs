using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieWar.CameraSystem
{
    public sealed class GameCameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera playerFollowCamera;

        [SerializeField] private int worldOverviewPriority = 5;
        [SerializeField] private int playerFollowPriority = 10;
        [SerializeField] private float playerZoomMin = 5f;
        [SerializeField] private float playerZoomMax = 14f;
        [SerializeField] private float playerZoomDefault = 8f;
        [SerializeField] private bool enableScrollZoom = true;
        [SerializeField] private float scrollZoomSpeed = 1f;

        private CinemachineFollow playerFollow;
        private Vector3 defaultPlayerFollowOffset;
        private float currentPlayerZoomDistance;
        private bool isInitialized;

        public float PlayerZoomDistance => currentPlayerZoomDistance;

        private void Awake()
        {
            cachePlayerFollow();
            currentPlayerZoomDistance = Mathf.Clamp(playerZoomDefault, playerZoomMin, playerZoomMax);
            applyPlayerZoom(currentPlayerZoomDistance);
            ShowPlayerCamera();
            isInitialized = true;
        }

        private void Update()
        {
            if (!enableScrollZoom || !isPlayerCameraActive())
            {
                return;
            }

            // Player Settings uses Input System only — do not call UnityEngine.Input.
            float scrollDelta = readMouseScrollY();
            if (Mathf.Approximately(scrollDelta, 0f))
            {
                return;
            }

            AdjustPlayerZoom(scrollDelta * scrollZoomSpeed);
        }

        public void SetPlayerTarget(Transform playerTarget)
        {
            if (!validatePlayerCamera())
            {
                return;
            }

            if (playerTarget == null)
            {
                return;
            }

            playerFollowCamera.Target.TrackingTarget = playerTarget;
        }

        public void ShowWorldOverview()
        {
            if (!validateCameras())
            {
                return;
            }

            setPriorities(worldActive: true);
        }

        public void ShowPlayerCamera()
        {
            if (!validateCameras())
            {
                return;
            }

            setPriorities(worldActive: false);
        }

        public void SetPlayerZoom(float distance)
        {
            if (!validatePlayerFollow())
            {
                return;
            }

            currentPlayerZoomDistance = Mathf.Clamp(distance, playerZoomMin, playerZoomMax);
            applyPlayerZoom(currentPlayerZoomDistance);
        }

        public void AdjustPlayerZoom(float delta)
        {
            SetPlayerZoom(currentPlayerZoomDistance + delta);
        }

        private void cachePlayerFollow()
        {
            if (playerFollowCamera == null)
            {
                return;
            }

            playerFollow = playerFollowCamera.GetComponent<CinemachineFollow>();
            if (playerFollow == null)
            {
                return;
            }

            defaultPlayerFollowOffset = playerFollow.FollowOffset;
        }

        private void applyPlayerZoom(float distance)
        {
            if (playerFollow == null)
            {
                return;
            }

            Vector3 direction = defaultPlayerFollowOffset.sqrMagnitude > 0.0001f
                ? defaultPlayerFollowOffset.normalized
                : new Vector3(0f, 0.6f, -1f).normalized;

            playerFollow.FollowOffset = direction * distance;
        }

        private void setPriorities(bool worldActive)
        {
            if (worldActive)
            {
                playerFollowCamera.Priority.Enabled = true;
                playerFollowCamera.Priority.Value = worldOverviewPriority;
                return;
            }

            playerFollowCamera.Priority.Enabled = true;
            playerFollowCamera.Priority.Value = playerFollowPriority;
        }

        private bool isPlayerCameraActive()
        {
            return playerFollowCamera != null
                && playerFollowCamera.Priority.Enabled
                && playerFollowCamera.Priority.Value >= playerFollowPriority;
        }

        private bool validateCameras()
        {
            if (playerFollowCamera == null)
            {
                return false;
            }

            return true;
        }

        private bool validatePlayerCamera()
        {
            return playerFollowCamera != null;
        }

        private bool validatePlayerFollow()
        {
            return playerFollow != null;
        }

        private static float readMouseScrollY()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return 0f;
            }

            // Input System scroll is often ~120 per notch; normalize to old Input-style steps.
            return mouse.scroll.ReadValue().y / 120f;
        }
    }
}
