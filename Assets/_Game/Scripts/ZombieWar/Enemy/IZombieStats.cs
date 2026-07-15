namespace ZombieWar.Enemy
{
    public interface IZombieStats
    {
        float MaxHealth { get; }
        float MoveSpeed { get; }
        float AttackDamage { get; }
        float AttackRange { get; }
        float AttackCooldown { get; }
        float DestinationUpdateInterval { get; }

        /// <summary>Animator playback scale while alive (1 = default clip rate).</summary>
        float AnimationSpeed { get; }

        /// <summary>
        /// Minimum locomotion blend (0–1) while moving so chase favors Run over slow Walk.
        /// </summary>
        float LocomotionRunBias { get; }

        /// <summary>
        /// When &gt; 0, approach via a surround offset around the target instead of stacking
        /// on the same path. Dive to the target once within melee commit range.
        /// </summary>
        float FlankRadius { get; }
    }
}
