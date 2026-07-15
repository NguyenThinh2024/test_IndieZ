using UnityEngine;
using ZombieWar.Weapon;

namespace ZombieWar.Player
{
    /// <summary>
    /// Faces and auto-fires the nearest in-zone target.
    /// Joystick only moves the player; combat always aims (and can fire) while idle or moving.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private EnemyTargetScanner targetScanner;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private PlayerAnimation playerAnimation;
        [SerializeField] private Transform aimRoot;

        [SerializeField] private bool autoFire = true;
        [SerializeField] private float aimRotationDegreesPerSecond = 540f;
        [SerializeField] private PlayerMovement playerMovement;

        [Tooltip("Inside this planar distance, aim snaps instantly (muzzle can sit past the enemy).")]
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

            // Always face the engaged target so move + shoot both work (twin-stick).
            RotateToTarget(targetPosition, snap: isClose);

            if (!autoFire || weaponController == null)
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
                direction = resolveBodyFacing();
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

            // Body faces shoot direction so muzzle lines up while strafing or standing.
            if (playerMovement != null)
            {
                playerMovement.FaceWorldDirection(direction, instant: snap);
            }
        }

        private Vector3 resolveBodyFacing()
        {
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
