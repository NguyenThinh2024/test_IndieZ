using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieWar.CameraSystem
{
    /// <summary>
    /// Drives one CinemachineCamera that follows player__root.
    /// Main Camera is owned by CinemachineBrain — edit Follow Offset / FOV on this component.
    /// </summary>
    public sealed class GameCameraController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private CinemachineCamera playerFollowCamera;
        [SerializeField] private int playerFollowPriority = 10;

        [SerializeField] private Vector3 followOffset = new Vector3(0f, 8f, -6f);
        [SerializeField] private float fieldOfView = 90f;
        [SerializeField] private bool enableScrollZoom = false;
        [SerializeField] private float zoomMin = 5f;
        [SerializeField] private float zoomMax = 18f;
        [SerializeField] private float scrollZoomSpeed = 1f;

        private CinemachineFollow playerFollow;
        private CinemachineBrain cinemachineBrain;
        private float currentZoomDistance;
        private bool isInitialized;

        public Vector3 FollowOffset => followOffset;
        public float FieldOfView => fieldOfView;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            resolveFollowTarget();

            if (mainCamera != null)
            {
                cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
                if (cinemachineBrain != null)
                {
                    cinemachineBrain.enabled = true;
                }
            }

            cachePlayerFollow();
            currentZoomDistance = followOffset.magnitude;
            applyCinemachineLive();
            applyFraming();
            isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!isInitialized)
            {
                return;
            }

            if (enableScrollZoom)
            {
                float scrollDelta = readMouseScrollY();
                if (!Mathf.Approximately(scrollDelta, 0f))
                {
                    AdjustZoom(scrollDelta * scrollZoomSpeed);
                }
            }

            applyFraming();
        }

        public void SetPlayerTarget(Transform playerTarget)
        {
            followTarget = playerTarget;
            if (playerFollowCamera != null && playerTarget != null)
            {
                playerFollowCamera.Target.TrackingTarget = playerTarget;
            }
        }

        public void SetFollowOffset(Vector3 offset)
        {
            followOffset = offset;
            currentZoomDistance = Mathf.Max(0.01f, offset.magnitude);
            applyFraming();
        }

        public void SetFieldOfView(float fov)
        {
            fieldOfView = Mathf.Clamp(fov, 10f, 120f);
            applyFraming();
        }

        public void AdjustZoom(float delta)
        {
            currentZoomDistance = Mathf.Clamp(currentZoomDistance + delta, zoomMin, zoomMax);
            Vector3 direction = followOffset.sqrMagnitude > 0.0001f
                ? followOffset.normalized
                : new Vector3(0f, 0.6f, -1f).normalized;
            followOffset = direction * currentZoomDistance;
            applyFraming();
        }

        public void ShowPlayerCamera()
        {
            applyCinemachineLive();
        }

        private void resolveFollowTarget()
        {
            if (followTarget != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("player__root");
            }

            if (player == null)
            {
                return;
            }

            // Prefer chest target under player__root; fall back to the root itself.
            Transform cameraTarget = player.transform.Find("PlayerCameraTarget");
            followTarget = cameraTarget != null ? cameraTarget : player.transform;
        }

        private void applyCinemachineLive()
        {
            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = true;
            }

            if (playerFollowCamera == null)
            {
                return;
            }

            playerFollowCamera.enabled = true;
            playerFollowCamera.Priority.Enabled = true;
            playerFollowCamera.Priority.Value = playerFollowPriority;

            if (followTarget != null)
            {
                playerFollowCamera.Target.TrackingTarget = followTarget;
            }
        }

        private void applyFraming()
        {
            if (playerFollow != null)
            {
                playerFollow.FollowOffset = followOffset;
            }

            if (playerFollowCamera == null)
            {
                return;
            }

            LensSettings lens = playerFollowCamera.Lens;
            lens.FieldOfView = fieldOfView;
            playerFollowCamera.Lens = lens;

            if (followTarget != null)
            {
                playerFollowCamera.Target.TrackingTarget = followTarget;
            }
        }

        private void cachePlayerFollow()
        {
            if (playerFollowCamera == null)
            {
                return;
            }

            playerFollow = playerFollowCamera.GetComponent<CinemachineFollow>();
        }

        private static float readMouseScrollY()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return 0f;
            }

            return mouse.scroll.ReadValue().y / 120f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            fieldOfView = Mathf.Clamp(fieldOfView, 10f, 120f);
            if (isInitialized)
            {
                applyCinemachineLive();
                applyFraming();
            }
        }
#endif
    }
}
