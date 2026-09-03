using UnityEngine;

namespace ZombieWar.Player
{
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Joystick joystick = null;
        [SerializeField] private Transform visualRoot = null;
        [SerializeField] private EnemyTargetScanner targetScanner = null;

        [SerializeField] private float rotationDegreesPerSecond = 360f;

        // Gravity
        [SerializeField] private float gravity = -25f;

        // Ground detection
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundCheckHeight = 1.0f;
        [SerializeField] private float groundCheckDistance = 2.0f;
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField] private float maxSlopeAngle = 50f;

        [SerializeField] private float combatFacingExitGrace = 0.2f;

        private float moveSpeed = 5f;
        private float verticalVelocity;

        private Vector3 moveDirection;
        private bool combatFacingActive;
        private float combatFacingExitAt;

        public float MoveAmount { get; private set; }

        public Vector3 MoveDirection => moveDirection;
        public Vector3 FacingDirection
        {
            get
            {
                Transform facingRoot = visualRoot != null ? visualRoot : transform;

                Vector3 facing = facingRoot.forward;
                facing.y = 0f;

                return facing.sqrMagnitude > 0.0001f
                    ? facing.normalized
                    : Vector3.forward;
            }
        }

        public bool HasCombatTarget =>
            targetScanner != null &&
            targetScanner.CurrentTarget != null;

        public void ApplyMoveSpeed(float value)
        {
            moveSpeed = Mathf.Max(0f, value);
        }

        public void SnapToGroundHeight(float worldY)
        {
            Vector3 position = transform.position;
            position.y = worldY;

            transform.position = position;
            verticalVelocity = 0f;
        }
        public void FaceWorldDirection(Vector3 worldDirection, bool instant)
        {
            worldDirection.y = 0f;

            if (worldDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Transform facingRoot = visualRoot != null
                ? visualRoot
                : transform;

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    worldDirection.normalized,
                    Vector3.up);

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

            maxSlopeAngle = Mathf.Clamp(maxSlopeAngle, 0f, 89f);
        }

#endif

        #endregion

        #region Update

        private void Update()
        {
            updateMoveDirection();
            movePlayer();
            updateCombatFacingLatch();

            // PlayerCombat owns rotation while combat facing is active.
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
                combatFacingExitAt =
                    Time.time + Mathf.Max(0f, combatFacingExitGrace);

                return;
            }

            if (combatFacingActive &&
                Time.time >= combatFacingExitAt)
            {
                combatFacingActive = false;
            }
        }

        #endregion

        #region Movement

        private void updateMoveDirection()
        {
            Vector2 input = getMoveInput();

            moveDirection = new Vector3(
                input.x,
                0f,
                input.y);

            MoveAmount =
                Mathf.Clamp01(moveDirection.magnitude);

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
            Vector3 displacement =
                moveDirection *
                (moveSpeed * Time.deltaTime);

            // First try to detect the ground at the current position.
            RaycastHit groundHit;

            if (TryGetGround(out groundHit))
            {
                // Check whether this surface is walkable.
                float slopeAngle =
                    Vector3.Angle(
                        groundHit.normal,
                        Vector3.up);

                if (slopeAngle <= maxSlopeAngle)
                {
                    // Project movement onto the slope.
                    Vector3 slopeMovement =
                        Vector3.ProjectOnPlane(
                            displacement,
                            groundHit.normal);

                    displacement.x = slopeMovement.x;
                    displacement.z = slopeMovement.z;

                    // Put the player on the slope.
                    float targetY =
                        groundHit.point.y + groundOffset;

                    displacement.y =
                        targetY - transform.position.y;

                    verticalVelocity = 0f;

                    transform.position += displacement;

                    return;
                }
            }
            applyGravity(ref displacement);

            transform.position += displacement;
        }

        private bool TryGetGround(out RaycastHit hit)
        {
            Vector3 origin =
                transform.position +
                Vector3.up * groundCheckHeight;

            float rayDistance =
                groundCheckHeight +
                groundCheckDistance;

            return Physics.Raycast(
                origin,
                Vector3.down,
                out hit,
                rayDistance,
                groundMask,
                QueryTriggerInteraction.Collide);
        }

        private void applyGravity(ref Vector3 displacement)
        {
            verticalVelocity +=
                gravity * Time.deltaTime;

            displacement.y +=
                verticalVelocity * Time.deltaTime;
        }

        private void rotateVisual(Vector3 direction)
        {
            if (visualRoot == null ||
                direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up);

            visualRoot.rotation =
                Quaternion.RotateTowards(
                    visualRoot.rotation,
                    targetRotation,
                    rotationDegreesPerSecond *
                    Time.deltaTime);
        }

        #endregion
    }
}

