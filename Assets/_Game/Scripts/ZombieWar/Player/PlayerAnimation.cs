using UnityEngine;

namespace ZombieWar.Player
{
    public sealed class PlayerAnimation : MonoBehaviour
    {
        private static readonly int DefaultMoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int DefaultMotionSpeedHash = Animator.StringToHash("MotionSpeed");
        private static readonly int DefaultGroundedHash = Animator.StringToHash("Grounded");
        private static readonly int DefaultShootHash = Animator.StringToHash("Shoot");
        private static readonly int DefaultHitHash = Animator.StringToHash("Hit");
        private static readonly int DefaultDieHash = Animator.StringToHash("Die");

        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private int upperBodyLayerIndex = 1;
        [SerializeField] private float upperBodyWeight = 1f;
        // StarterAssets blend thresholds: Idle 0 / Shoot_Rifle 1.5 / Shoot_Rifle 6.
        [SerializeField] private float blendTreeMaxSpeed = 6f;
        [SerializeField] private float speedDampTime = 0.08f;
        [SerializeField] private float moveSnapIn = 0.85f;
        [SerializeField] private float moveSnapOut = 0.05f;
        [SerializeField] private float shootAnimCooldown = 0.2f;

        private Animator animator;
        private bool hasMotionSpeedParameter;
        private bool hasGroundedParameter;
        private bool hasMoveSpeedParameter;
        private bool usesManualAnimatorTick;
        private float nextShootAnimTime;

        private int moveSpeedHash = DefaultMoveSpeedHash;
        private int motionSpeedHash = DefaultMotionSpeedHash;
        private int groundedHash = DefaultGroundedHash;
        private int shootHash = DefaultShootHash;
        private int hitHash = DefaultHitHash;
        private int dieHash = DefaultDieHash;

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
            if (movement == null)
            {
                movement = GetComponent<PlayerMovement>();
            }

            if (health == null)
            {
                health = GetComponent<PlayerHealth>();
            }
        }

        private void OnEnable()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged += playHitHandler;
            health.Died += playDieHandler;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= playHitHandler;
            health.Died -= playDieHandler;
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            tickLocomotion(Time.deltaTime);
        }

        public void ApplyConfig(PlayerCharacterAnimationConfig config)
        {
            if (config == null)
            {
                return;
            }

            moveSpeedHash = toHash(config.MoveSpeedParameter, DefaultMoveSpeedHash);
            shootHash = toHash(config.ShootTrigger, DefaultShootHash);
            hitHash = toHash(config.HitTrigger, DefaultHitHash);
            dieHash = toHash(config.DieTrigger, DefaultDieHash);
        }

        public void SetAnimator(Animator value, RuntimeAnimatorController controller)
        {
            animator = value;
            if (animator == null)
            {
                clearParameterFlags();
                usesManualAnimatorTick = false;
                return;
            }

            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }

            prepareAnimator();
            cacheParameterFlags();
            applyDefaults();
            applyUpperBodyLayer();
        }

        public void PlayShoot()
        {
            if (animator == null || Time.time < nextShootAnimTime)
            {
                return;
            }

            nextShootAnimTime = Time.time + Mathf.Max(0.05f, shootAnimCooldown);
            animator.SetTrigger(shootHash);
        }

        private void tickLocomotion(float deltaTime)
        {
            if (hasMotionSpeedParameter)
            {
                animator.SetFloat(motionSpeedHash, 1f);
            }

            if (hasGroundedParameter)
            {
                animator.SetBool(groundedHash, true);
            }

            if (hasMoveSpeedParameter && movement != null)
            {
                float moveAmount = movement.MoveAmount;
                float blendSpeed = moveAmount * blendTreeMaxSpeed;

                // Soft snap near idle/full run so Speed does not linger in mid-blend.
                if (moveAmount <= moveSnapOut)
                {
                    animator.SetFloat(moveSpeedHash, 0f);
                }
                else if (moveAmount >= moveSnapIn)
                {
                    animator.SetFloat(moveSpeedHash, blendTreeMaxSpeed);
                }
                else
                {
                    animator.SetFloat(moveSpeedHash, blendSpeed, speedDampTime, deltaTime);
                }
            }

            // Manual-only tick: animator.enabled is false so Unity does not also advance it
            // (double Update was causing jerky locomotion).
            if (usesManualAnimatorTick)
            {
                animator.Update(deltaTime);
            }
        }

        private void prepareAnimator()
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Addressable Survivalist needs an explicit tick in this scene, but must not
            // also receive Unity's automatic Animator update (that doubles delta and jitters).
            usesManualAnimatorTick = true;
            animator.enabled = false;
            animator.Rebind();
            animator.Update(0f);
        }

        private void applyDefaults()
        {
            if (hasMotionSpeedParameter)
            {
                animator.SetFloat(motionSpeedHash, 1f);
            }

            if (hasGroundedParameter)
            {
                animator.SetBool(groundedHash, true);
            }
        }

        private void applyUpperBodyLayer()
        {
            if (upperBodyLayerIndex < 0 || upperBodyLayerIndex >= animator.layerCount)
            {
                return;
            }

            animator.SetLayerWeight(upperBodyLayerIndex, upperBodyWeight);
        }

        private void cacheParameterFlags()
        {
            hasMoveSpeedParameter = hasParameter(moveSpeedHash);
            hasMotionSpeedParameter = hasParameter(motionSpeedHash);
            hasGroundedParameter = hasParameter(groundedHash);
        }

        private void clearParameterFlags()
        {
            hasMoveSpeedParameter = false;
            hasMotionSpeedParameter = false;
            hasGroundedParameter = false;
        }

        private bool hasParameter(int parameterHash)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == parameterHash)
                {
                    return true;
                }
            }

            return false;
        }

        private static int toHash(string parameterName, int fallbackHash)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return fallbackHash;
            }

            return Animator.StringToHash(parameterName);
        }

        private void playHitHandler()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetTrigger(hitHash);
        }

        private void playDieHandler()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetTrigger(dieHash);
        }
    }
}
