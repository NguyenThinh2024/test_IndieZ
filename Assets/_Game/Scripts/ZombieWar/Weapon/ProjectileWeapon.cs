using System;
using TBN;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// Fires pooled <see cref="BulletProjectile"/> instances through <see cref="BulletProjectileSystem"/>.
    /// </summary>
    public sealed class ProjectileWeapon : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform recoilRoot;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private DamageableHitboxResolver hitboxResolver;
        [SerializeField] private BulletProjectileSystem bulletSystem;
        [SerializeField] private GameObject fallbackBulletPrefab;
        [SerializeField] private GameObject defaultMuzzleVfxPrefab;

        public event Action<float, float, float> Recoiled;
        public event Action Fired;

        private float nextFireTime;
        private Transform fallbackFirePoint;

        private void Awake()
        {
            bindLocalDependencies();
            ensureAudioSource();
            ensureFallbackFirePoint();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bindLocalDependencies();
        }
#endif

        private void bindLocalDependencies()
        {
            if (bulletSystem == null)
            {
                bulletSystem = GetComponent<BulletProjectileSystem>();
            }

            if (hitboxResolver == null)
            {
                hitboxResolver = GetComponent<DamageableHitboxResolver>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void ensureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.35f;
            audioSource.loop = false;
        }

        private void ensureFallbackFirePoint()
        {
            if (firePoint != null)
            {
                return;
            }

            const string fallbackName = "FallbackFirePoint";
            Transform existing = transform.Find(fallbackName);
            if (existing != null)
            {
                fallbackFirePoint = existing;
            }
            else
            {
                GameObject pointObject = new GameObject(fallbackName);
                fallbackFirePoint = pointObject.transform;
                fallbackFirePoint.SetParent(transform, false);
                // Chest-forward until gun visual assigns the real muzzle FirePoint.
                fallbackFirePoint.localPosition = new Vector3(0.25f, 1.2f, 0.55f);
                fallbackFirePoint.localRotation = Quaternion.identity;
            }

            firePoint = fallbackFirePoint;
        }

        public void SetFirePoint(Transform value)
        {
            // Prefer gun muzzle. Ignore null so fallback remains usable.
            if (value != null)
            {
                firePoint = value;
            }
        }

        public Transform FirePoint => firePoint;

        public void SetBulletSystem(BulletProjectileSystem value)
        {
            bulletSystem = value;
        }

        public bool TryFire(GunData gunData, Vector3 targetPoint, GameObject owner)
        {
            if (gunData == null || firePoint == null || bulletSystem == null || Time.time < nextFireTime)
            {
                return false;
            }

            nextFireTime = Time.time + Mathf.Max(0.01f, gunData.FireRate);

            Vector3 baseDirection = (targetPoint - firePoint.position).normalized;
            if (baseDirection.sqrMagnitude < 0.001f)
            {
                baseDirection = firePoint.forward;
            }

            for (int i = 0; i < gunData.PelletCount; i++)
            {
                Vector3 shotDirection = ApplySpread(baseDirection, gunData.SpreadAngle, i, gunData.PelletCount);
                FireBullet(gunData, shotDirection, owner);
            }

            GameObject muzzleVfx = gunData.MuzzleVfxPrefab != null ? gunData.MuzzleVfxPrefab : defaultMuzzleVfxPrefab;
            PooledVfx.Spawn(muzzleVfx, firePoint.position, firePoint.rotation, 1f);
            PlayAudio(gunData.FireClip);
            Recoiled?.Invoke(gunData.RecoilDistance, gunData.RecoilDuration, gunData.RecoilPitchDegrees);
            Fired?.Invoke();
            return true;
        }

        private void FireBullet(GunData gunData, Vector3 direction, GameObject owner)
        {
            GameObject prefab = gunData.BulletPrefab != null ? gunData.BulletPrefab : fallbackBulletPrefab;
            if (prefab == null)
            {
                return;
            }

            GameObject instance = prefab.Spawn(firePoint.position, Quaternion.LookRotation(direction));
            if (instance == null)
            {
                return;
            }

            if (!instance.TryGetComponent(out BulletProjectile bullet))
            {
                instance.Recycle();
                return;
            }

            BulletFireContext context = new BulletFireContext(
                firePoint.position,
                direction,
                gunData.BulletSpeed,
                gunData.Damage,
                gunData.Range,
                gunData.HitMask,
                owner,
                gunData.HitVfxPrefab,
                hitboxResolver);

            bullet.Launch(in context, bulletSystem);
        }

        private void PlayAudio(AudioClip clip)
        {
            if (audioSource == null || clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip);
        }

        private static Vector3 ApplySpread(Vector3 direction, float spreadAngle, int pelletIndex, int pelletCount)
        {
            if (spreadAngle <= 0f || pelletCount <= 1)
            {
                return direction;
            }

            float yaw = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            float pitch = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            Quaternion spread = Quaternion.Euler(pitch, yaw, 0f);
            return spread * direction;
        }
    }
}
