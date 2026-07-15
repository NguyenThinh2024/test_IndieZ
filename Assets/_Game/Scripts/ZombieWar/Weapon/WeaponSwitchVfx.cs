using UnityEngine;
using UnityEngine.Serialization;
using ZombieWar.Core;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// Plays switch FX (particles + one-shot SFX) when the gun changes.
    /// Assign <c>Assets/_Game/FX/PlayerUpgradeFX</c> to Switch Fx Prefab.
    /// PlayerUpgradeFX uses playOnAwake=false — spawn goes through <see cref="PooledVfx"/> so particles restart.
    /// </summary>
    public sealed class WeaponSwitchVfx : MonoBehaviour
    {
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private ProjectileWeapon projectileWeapon;

        [Header("Switch FX")]
        [Tooltip("Particle FX prefab (e.g. PlayerUpgradeFX). Not a plain empty GameObject.")]
        [FormerlySerializedAs("switchVfxPrefab")]
        [SerializeField] private GameObject switchFxPrefab;

        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool parentToSpawnPoint;
        [SerializeField] private float lifeTime = 2.5f;

        [Header("Switch SFX")]
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
            lifeTime = Mathf.Max(0.1f, lifeTime);
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
            spawnSwitchFx();
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

        private void spawnSwitchFx()
        {
            if (switchFxPrefab == null)
            {
                return;
            }

            Transform anchor = resolveSpawnPoint();
            Transform parent = parentToSpawnPoint ? anchor : null;

            // PooledVfx.Spawn + RestartParticles — required because PlayerUpgradeFX playOnAwake=false.
            PooledVfx.Spawn(
                switchFxPrefab,
                anchor.position,
                Quaternion.identity,
                lifeTime,
                parent);
        }

        private Transform resolveSpawnPoint()
        {
            if (spawnPoint != null)
            {
                return spawnPoint;
            }

            // Upgrade-style FX belongs on the body, not the muzzle tip.
            return transform;
        }
    }
}
