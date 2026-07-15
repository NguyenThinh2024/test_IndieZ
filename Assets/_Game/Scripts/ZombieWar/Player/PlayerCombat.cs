using UnityEngine;
using ZombieWar.Weapon;

namespace ZombieWar.Player
{
    /// <summary>
    /// Faces and fires the nearest in-zone target.
    /// Joystick movement is owned by PlayerMovement; this owns combat facing.
    /// While moving, fire only inside the forward cone. While idle, snap aim and fire.
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

        private void Awake()
        {
            bindLocalDependencies();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bindLocalDependencies();
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

            bool isIdle = playerMovement == null || playerMovement.MoveAmount < IdleMoveThreshold;
            RotateToTarget(target.position, isIdle);

            if (!autoFire || weaponController == null)
            {
                return;
            }

            // Standing still: always fire at the locked target once aim has snapped.
            // Moving: keep the forward cone so the player does not shoot while running away.
            if (!isIdle && !IsTargetInForwardFireCone(target.position))
            {
                return;
            }

            if (weaponController.TryFire(target.position, gameObject))
            {
                playerAnimation?.PlayShoot();
            }
        }

        public void SwitchWeapon()
        {
            weaponController?.SwitchNext();
        }

        private void RotateToTarget(Vector3 targetPosition, bool isIdle)
        {
            if (aimRoot == null)
            {
                return;
            }

            Vector3 direction = targetPosition - aimRoot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            // Idle: snap immediately so release-stick auto-fire is not gated by slow turn + cone.
            if (isIdle)
            {
                aimRoot.rotation = targetRotation;
                return;
            }

            aimRoot.rotation = Quaternion.RotateTowards(
                aimRoot.rotation,
                targetRotation,
                aimRotationDegreesPerSecond * Time.deltaTime);
        }

        private bool IsTargetInForwardFireCone(Vector3 targetPosition)
        {
            if (aimRoot == null)
            {
                return false;
            }

            Vector3 toTarget = targetPosition - aimRoot.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            Vector3 facing = aimRoot.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            float angle = Vector3.Angle(facing.normalized, toTarget.normalized);
            return angle <= fireFacingHalfAngleDegrees;
        }
    }
}
