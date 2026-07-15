using UnityEngine;

namespace ZombieWar.Bomb
{
    [CreateAssetMenu(fileName = "BombData", menuName = "Zombie War/Bomb/Bomb Data")]
    public sealed class BombData : ScriptableObject
    {
        [SerializeField] private float fuseTime = 1.2f;
        [SerializeField] private float radius = 5f;
        [SerializeField] private float maxDamage = 120f;
        [SerializeField] private float minDamage = 25f;
        [SerializeField] private float explosionForce = 650f;
        [SerializeField] private LayerMask damageMask;
        [SerializeField] private GameObject explosionVfxPrefab;
        [SerializeField] private AudioClip explosionClip;

        public float FuseTime => fuseTime;
        public float Radius => radius;
        public float MaxDamage => maxDamage;
        public float MinDamage => minDamage;
        public float ExplosionForce => explosionForce;
        public LayerMask DamageMask => damageMask;
        public GameObject ExplosionVfxPrefab => explosionVfxPrefab;
        public AudioClip ExplosionClip => explosionClip;
    }
}
