using UnityEngine;
using ZombieWar.Weapon;

namespace ZombieWar.Player
{
    /// <summary>
    /// Faces and fires the nearest in-zone target.
    /// Joystick movement is owned by PlayerMovement; this owns combat facing.
    /// While moving, fire only inside the forward cone. While idle / melee-close, snap aim and fire.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        private const float IdleMoveThreshold = 0.05f;

        [SerializeField] private EnemyTargetScanner targetScanner;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private PlayerAnimation playerAnimation;
        [SerializeField] private Transform aimRoot;

        [SerializeField] private bool autoFire = true;
        [SerializeField] private float aimRotationDegreesPerSecond = 540f;
        [SerializeField] private PlayerMovement playerMovement;

        [SerializeField] [Range(1f, 180f)] private float fireFacingHalfAngleDegrees = 110f;

        [Tooltip("Inside this planar distance, always snap-aim and fire (muzzle can sit past the enemy).")]
        [SerializeField] private float closeRangeEngageDistance = 2.75f;

        private void Awake()
        {
            bindLocalDependencies();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bindLocalDependencies();
            closeRangeEngageDistance = Mathf.Max(0.5f, closeRangeEngageDistance);
        }
#endif

        private void bindLocalDependencies()
        {
            if (targetScanner == null)
            {
                targetScanner = GetComponent<EnemyTargetScanner>();
            }

            if (weaponController == null)
            {
                weaponController = GetComponent<WeaponController>();
            }

            if (playerAnimation == null)
            {
                playerAnimation = GetComponent<PlayerAnimation>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }

            if (aimRoot == null)
            {
                aimRoot = transform;
            }
        }

        private void Update()
        {
            Transform target = targetScanner != null ? targetScanner.CurrentTarget : null;
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position;
            float planarDistance = planarDistanceTo(targetPosition);
            bool isClose = planarDistance <= closeRangeEngageDistance;
            bool isIdle = playerMovement == null || playerMovement.MoveAmount < IdleMoveThreshold;
            bool shouldSnapAim = isIdle || isClose;

            RotateToTarget(targetPosition, shouldSnapAim);

            if (!autoFire || weaponController == null)
            {
                return;
            }

            // Cone uses move/body facing — not aimRoot after it already turned toward the enemy.
            // Close range always fires: muzzle often sits past the enemy so cone / aim would fail.
            if (!shouldSnapAim && !IsTargetInForwardFireCone(targetPosition))
            {
                return;
            }

            if (weaponController.TryFire(targetPosition, gameObject))
            {
                playerAnimation?.PlayShoot();
            }
        }

        public void SwitchWeapon()
        {
            weaponController?.SwitchNext();
        }

        private void RotateToTarget(Vector3 targetPosition, bool snap)
        {
            Vector3 origin = aimRoot != null ? aimRoot.position : transform.position;
            Vector3 direction = targetPosition - origin;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                // Enemy almost under the player — keep facing, still allow fire downstream.
                direction = resolveFireFacing();
                if (direction.sqrMagnitude < 0.0001f)
                {
                    return;
                }
            }

            direction.Normalize();

            if (aimRoot != null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                if (snap)
                {
                    aimRoot.rotation = targetRotation;
                }
                else
                {
                    aimRoot.rotation = Quaternion.RotateTowards(
                        aimRoot.rotation,
                        targetRotation,
                        aimRotationDegreesPerSecond * Time.deltaTime);
                }
            }

            // Keep body facing shoot direction when snap-aiming so muzzle lines up with bullets.
            if (snap && playerMovement != null)
            {
                playerMovement.FaceWorldDirection(direction, instant: true);
            }
        }

        private bool IsTargetInForwardFireCone(Vector3 targetPosition)
        {
            Vector3 origin = aimRoot != null ? aimRoot.position : transform.position;
            Vector3 toTarget = targetPosition - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            Vector3 facing = resolveFireFacing();
            if (facing.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            float angle = Vector3.Angle(facing.normalized, toTarget.normalized);
            return angle <= fireFacingHalfAngleDegrees;
        }

        private Vector3 resolveFireFacing()
        {
            if (playerMovement != null && playerMovement.MoveAmount >= IdleMoveThreshold)
            {
                Vector3 moveFacing = playerMovement.MoveDirection;
                if (moveFacing.sqrMagnitude > 0.0001f)
                {
                    return moveFacing;
                }

                return playerMovement.FacingDirection;
            }

            if (playerMovement != null)
            {
                return playerMovement.FacingDirection;
            }

            if (aimRoot == null)
            {
                return transform.forward;
            }

            Vector3 facing = aimRoot.forward;
            facing.y = 0f;
            return facing;
        }

        private float planarDistanceTo(Vector3 worldPosition)
        {
            Vector3 origin = aimRoot != null ? aimRoot.position : transform.position;
            Vector3 delta = worldPosition - origin;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
