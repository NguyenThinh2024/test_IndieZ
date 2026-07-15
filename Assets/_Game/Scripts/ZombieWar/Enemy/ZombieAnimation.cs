using UnityEngine;

namespace ZombieWar.Enemy
{
    public sealed class ZombieAnimation : MonoBehaviour
    {
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField] private Animator animator;
        [SerializeField] private ZombieMovement movement;
        [SerializeField] private ZombieHealth health;

        private bool usesCentralTick;
        private float animationSpeed = 1f;
        private float locomotionRunBias = 0.9f;

        public void SetCentralTick(bool enabled)
        {
            usesCentralTick = enabled;
        }

        public void PlayAttack()
        {
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Hit += OnHit;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Hit -= OnHit;
                health.Died -= OnDied;
            }
        }

        public void OnSpawn(IZombieStats stats)
        {
            animationSpeed = stats != null ? stats.AnimationSpeed : 1f;
            locomotionRunBias = stats != null ? stats.LocomotionRunBias : 0.9f;

            if (animator == null)
            {
                return;
            }

            animator.speed = animationSpeed;
            animator.SetFloat(MoveSpeedHash, 0f);
        }

        public void OnDespawn()
        {
            usesCentralTick = false;
            animationSpeed = 1f;
            locomotionRunBias = 0.9f;

            if (animator == null)
            {
                return;
            }

            animator.speed = 1f;
            animator.SetFloat(MoveSpeedHash, 0f);
            animator.ResetTrigger(AttackHash);
            animator.ResetTrigger(HitHash);
            animator.ResetTrigger(DieHash);
        }

        private void Update()
        {
            if (usesCentralTick)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (animator == null || movement == null)
            {
                return;
            }

            float normalized = movement.SpeedNormalized;
            float blend = normalized > 0.05f
                ? Mathf.Max(locomotionRunBias, normalized)
                : 0f;

            animator.SetFloat(MoveSpeedHash, blend, 0.08f, deltaTime);
        }

        private void OnHit(Core.DamageInfo _)
        {
            if (animator != null)
            {
                animator.SetTrigger(HitHash);
            }
        }

        private void OnDied()
        {
            if (animator == null)
            {
                return;
            }

            animator.speed = 1f;
            animator.SetTrigger(DieHash);
        }
    }
}
