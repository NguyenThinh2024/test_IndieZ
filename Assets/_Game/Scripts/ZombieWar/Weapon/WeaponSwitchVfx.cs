using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// Spawns pooled switch VFX (prefab) + one-shot SFX when the gun changes.
    /// Assign PlayerUpgradeFX prefab to Switch Vfx Prefab.
    /// </summary>
    public sealed class WeaponSwitchVfx : MonoBehaviour
    {
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private ProjectileWeapon projectileWeapon;

        [SerializeField] private GameObject switchVfxPrefab;

        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float lifeTime = 2.5f;

        [SerializeField] private AudioClip switchClip;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] [Range(0f, 1f)] private float switchVolume = 1f;
        [SerializeField] private bool playOnInitialEquip;

        private bool hasSeenInitialEquip;

        private void Awake()
        {
            bindLocalDependencies();
            ensureAudioSource();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bindLocalDependencies();
        }
#endif

        private void OnEnable()
        {
            hasSeenInitialEquip = weaponController != null && weaponController.IsReady;

            if (weaponController != null)
            {
                weaponController.WeaponChanged += onWeaponChanged;
            }
        }

        private void OnDisable()
        {
            if (weaponController != null)
            {
                weaponController.WeaponChanged -= onWeaponChanged;
            }
        }

        public void Play()
        {
            PlaySwitchEffects();
        }

        public void PlaySwitchEffects()
        {
            spawnSwitchVfx();
            PlaySwitchSound();
        }

        public void PlaySwitchSound()
        {
            if (switchClip == null)
            {
                return;
            }

            ensureAudioSource();
            if (audioSource == null)
            {
                return;
            }

            audioSource.PlayOneShot(switchClip, Mathf.Clamp01(switchVolume));
        }

        private void bindLocalDependencies()
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<WeaponController>();
            }

            if (projectileWeapon == null)
            {
                projectileWeapon = GetComponent<ProjectileWeapon>();
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
            audioSource.spatialBlend = 0.2f;
            audioSource.loop = false;
        }

        private void onWeaponChanged(GunData _, int __)
        {
            if (!playOnInitialEquip && !hasSeenInitialEquip)
            {
                hasSeenInitialEquip = true;
                return;
            }

            hasSeenInitialEquip = true;
            PlaySwitchEffects();
        }

        private void spawnSwitchVfx()
        {
            if (switchVfxPrefab == null)
            {
                return;
            }

            Transform anchor = resolveSpawnPoint();
            PooledVfx.Spawn(switchVfxPrefab, anchor.position, anchor.rotation, lifeTime);
        }

        private Transform resolveSpawnPoint()
        {
            if (spawnPoint != null)
            {
                return spawnPoint;
            }

            if (projectileWeapon != null && projectileWeapon.FirePoint != null)
            {
                return projectileWeapon.FirePoint;
            }

            return transform;
        }
    }
}
