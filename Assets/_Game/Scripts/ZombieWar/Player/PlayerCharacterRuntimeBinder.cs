using UnityEngine;

namespace ZombieWar.Player
{
    public sealed class PlayerCharacterRuntimeBinder
    {
        private readonly PlayerMovement movement;
        private readonly PlayerAnimation playerAnimation;

        public PlayerCharacterRuntimeBinder(PlayerMovement movement, PlayerAnimation playerAnimation)
        {
            this.movement = movement;
            this.playerAnimation = playerAnimation;
        }

        public void ApplyStats(PlayerCharacterStatsConfig stats)
        {
            if (movement == null || stats == null)
            {
                return;
            }

            movement.ApplyMoveSpeed(stats.MoveSpeed);
        }

        public void BindAnimator(
            PlayerCharacterAnimationConfig animationConfig,
            Animator characterAnimator,
            RuntimeAnimatorController controller)
        {
            if (playerAnimation == null)
            {
                return;
            }

            if (characterAnimator == null)
            {
                return;
            }

            playerAnimation.ApplyConfig(animationConfig);
            playerAnimation.SetAnimator(characterAnimator, controller);
        }
    }
}
