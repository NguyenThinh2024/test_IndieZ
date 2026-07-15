using UnityEngine;

namespace ZombieWar.Player
{
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Joystick joystick = null;
        [SerializeField] private Transform visualRoot = null;
        [SerializeField] private EnemyTargetScanner targetScanner = null;

        [SerializeField] private float rotationDegreesPerSecond = 360f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float combatFacingExitGrace = 0.2f;

        private float moveSpeed = 5f;
        private float verticalVelocity;
        private float groundHeight;
        private Vector3 moveDirection;
        private bool combatFacingActive;
        private float combatFacingExitAt;

        public float MoveAmount { get; private set; }
        public Vector3 MoveDirection => moveDirection;

        /// <summary>
        /// Body facing used for fire cone / shoot alignment (visual root when present).
        /// </summary>
        public Vector3 FacingDirection
        {
            get
            {
                Transform facingRoot = visualRoot != null ? visualRoot : transform;
                Vector3 facing = facingRoot.forward;
                facing.y = 0f;
                return facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector3.forward;
            }
        }

        public bool HasCombatTarget => targetScanner != null && targetScanner.CurrentTarget != null;

        public void ApplyMoveSpeed(float value)
        {
            moveSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Call after level map / NavMesh is ready so the player sits on walkable ground
        /// instead of the Awake-time Y (often floating above the Addressable map).
        /// </summary>
        public void SnapToGroundHeight(float worldY)
        {
            Vector3 position = transform.position;
            position.y = worldY;
            transform.position = position;
            groundHeight = worldY;
            verticalVelocity = 0f;
        }

        /// <summary>
        /// Faces the visual root (body) toward a world direction. Used by combat when idle aiming.
        /// </summary>
        public void FaceWorldDirection(Vector3 worldDirection, bool instant)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Transform facingRoot = visualRoot != null ? visualRoot : transform;
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
            if (instant)
            {
                facingRoot.rotation = targetRotation;
                return;
            }

            facingRoot.rotation = Quaternion.RotateTowards(
                facingRoot.rotation,
                targetRotation,
                rotationDegreesPerSecond * Time.deltaTime);
        }

        #region Unity Lifecycle

        private void Awake()
        {
            groundHeight = transform.position.y;
            if (targetScanner == null)
            {
                targetScanner = GetComponent<EnemyTargetScanner>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetScanner == null)
            {
                targetScanner = GetComponent<EnemyTargetScanner>();
            }
        }
#endif

        #endregion

        #region Unity Update lifecycle

        private void Update()
        {
            // Use Unity Update for per-frame character movement.
            updateMoveDirection();
            movePlayer();
            updateCombatFacingLatch();

            // While combat facing is latched, PlayerCombat owns rotation (nearest enemy).
            // Joystick only moves — avoid snapping ownership every frame at zone edge.
            if (!combatFacingActive)
            {
                rotateVisual(moveDirection);
            }
        }

        private void updateCombatFacingLatch()
        {
            if (HasCombatTarget)
            {
                combatFacingActive = true;
                combatFacingExitAt = Time.time + Mathf.Max(0f, combatFacingExitGrace);
                return;
            }

            if (combatFacingActive && Time.time >= combatFacingExitAt)
            {
                combatFacingActive = false;
            }
        }

        #endregion

        #region Movement

        private void updateMoveDirection()
        {
            Vector2 input = getMoveInput();
            moveDirection = new Vector3(input.x, 0f, input.y);
            MoveAmount = Mathf.Clamp01(moveDirection.magnitude);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }
        }

        private Vector2 getMoveInput()
        {
            if (joystick == null)
            {
                return Vector2.zero;
            }

            return joystick.Direction;
        }

        private void movePlayer()
        {
            Vector3 displacement = moveDirection * (moveSpeed * Time.deltaTime);
            applyGravity(ref displacement);
            transform.position += displacement;
        }

        private void applyGravity(ref Vector3 displacement)
        {
            verticalVelocity += gravity * Time.deltaTime;
            displacement.y += verticalVelocity * Time.deltaTime;

            float nextHeight = transform.position.y + displacement.y;
            if (nextHeight <= groundHeight)
            {
                displacement.y = groundHeight - transform.position.y;
                verticalVelocity = 0f;
            }
        }

        private void rotateVisual(Vector3 direction)
        {
            if (visualRoot == null || direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            visualRoot.rotation = Quaternion.RotateTowards(
                visualRoot.rotation,
                targetRotation,
                rotationDegreesPerSecond * Time.deltaTime);
        }

        #endregion
    }
}
