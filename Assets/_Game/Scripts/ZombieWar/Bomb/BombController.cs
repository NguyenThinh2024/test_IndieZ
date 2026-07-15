using System.Collections;
using System.Collections.Generic;
using TBN;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Bomb
{
    public sealed class BombController : MonoBehaviour
    {
        [SerializeField] private BombData data;
        [SerializeField] private Rigidbody body;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private DamageableHitboxResolver hitboxResolver;
        [SerializeField] private int maxHits = 128;

        public Rigidbody Body => body;

        private readonly Dictionary<Collider, DamageableHitbox> hitboxCache = new Dictionary<Collider, DamageableHitbox>(128);
        private Collider[] hits;
        private GameObject owner;
        
        private void Awake()
        {
            hits = new Collider[Mathf.Max(16, maxHits)];
        }

        public void Initialize(BombData bombData, GameObject source)
        {
            data = bombData;
            owner = source;
            hitboxCache.Clear();
            StopAllCoroutines();
            StartCoroutine(FuseRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            float fuse = data != null ? data.FuseTime : 1f;
            yield return new WaitForSeconds(fuse);
            Explode();
        }

        private void Explode()
        {
            if (data == null)
            {
                gameObject.Recycle();
                return;
            }

            Vector3 center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, data.Radius, hits, data.DamageMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                Vector3 closestPoint = hit.ClosestPoint(center);
                Vector3 offset = closestPoint - center;
                float distance = offset.magnitude;
                float t = Mathf.Clamp01(distance / Mathf.Max(0.01f, data.Radius));
                float damage = Mathf.Lerp(data.MaxDamage, data.MinDamage, t);
                Vector3 direction = offset.sqrMagnitude > 0.0001f ? offset / distance : Vector3.up;

                DamageableHitbox hitbox = GetHitbox(hit);
                if (hitbox != null && hitbox.Damageable != null)
                {
                    DamageInfo damageInfo = new DamageInfo(damage, closestPoint, direction, owner, DamageType.Explosion);
                    hitbox.Damageable.TakeDamage(damageInfo);
                }

                if (hit.attachedRigidbody != null)
                {
                    hit.attachedRigidbody.AddExplosionForce(data.ExplosionForce, center, data.Radius, 0.5f, ForceMode.Impulse);
                }
            }

            PooledVfx.Spawn(data.ExplosionVfxPrefab, center, Quaternion.identity, 2f);
            if (audioSource != null && data.ExplosionClip != null)
            {
                audioSource.PlayOneShot(data.ExplosionClip);
                gameObject.Recycle(data.ExplosionClip.length);
            }
            else
            {
                gameObject.Recycle();
            }
        }

        private DamageableHitbox GetHitbox(Collider hit)
        {
            if (hit == null)
            {
                return null;
            }

            if (hitboxResolver != null && hitboxResolver.TryResolve(hit, out DamageableHitbox resolvedHitbox))
            {
                hitboxCache[hit] = resolvedHitbox;
                return resolvedHitbox;
            }

            if (hitboxCache.TryGetValue(hit, out DamageableHitbox cachedHitbox))
            {
                return cachedHitbox;
            }

            hit.TryGetComponent(out DamageableHitbox hitbox);
            hitboxCache[hit] = hitbox;
            return hitbox;
        }
    }
}
