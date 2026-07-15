using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ZombieWar.Weapon
{
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private ProjectileWeapon weapon;
        [SerializeField] private AssetReferenceT<TextAsset>[] gunConfigReferences;
        [SerializeField] private float switchCooldownSeconds = 2.5f;

        public event Action<GunData, int> WeaponChanged;
        public event Action GunsReady;

        public GunData CurrentGun { get; private set; }
        public int CurrentIndex { get; private set; }
        public bool IsReady { get; private set; }
        public bool IsSwitchReady => Time.time >= nextSwitchTime;

        private GunData[] guns;
        private GunConfigLoader[] loaders;
        private int pendingLoads;
        private float nextSwitchTime;

        private void OnEnable()
        {
            loadGunConfigs();
        }

        private void OnDisable()
        {
            releaseLoaders();
            IsReady = false;
            guns = null;
            CurrentGun = null;
        }

        public void SetWeapon(ProjectileWeapon value)
        {
            weapon = value;
        }

        public bool TryFire(Vector3 targetPoint, GameObject owner)
        {
            if (!IsReady || weapon == null || CurrentGun == null)
            {
                return false;
            }

            return weapon.TryFire(CurrentGun, targetPoint, owner);
        }

        public void Equip(int index)
        {
            if (!IsReady || guns == null || guns.Length == 0)
            {
                return;
            }

            CurrentIndex = Mathf.Clamp(index, 0, guns.Length - 1);
            CurrentGun = guns[CurrentIndex];
            WeaponChanged?.Invoke(CurrentGun, CurrentIndex);
        }

        public void SwitchNext()
        {
            if (!IsReady || guns == null || guns.Length == 0 || !IsSwitchReady)
            {
                return;
            }

            Equip((CurrentIndex + 1) % guns.Length);
            nextSwitchTime = Time.time + Mathf.Max(0f, switchCooldownSeconds);
        }

        private void loadGunConfigs()
        {
            releaseLoaders();
            IsReady = false;
            CurrentGun = null;

            if (gunConfigReferences == null || gunConfigReferences.Length == 0)
            {
                return;
            }

            guns = new GunData[gunConfigReferences.Length];
            loaders = new GunConfigLoader[gunConfigReferences.Length];
            pendingLoads = gunConfigReferences.Length;

            for (int i = 0; i < gunConfigReferences.Length; i++)
            {
                int index = i;
                loaders[i] = new GunConfigLoader(this);
                loaders[i].Load(gunConfigReferences[i], loaded => onGunConfigLoaded(index, loaded));
            }
        }

        private void onGunConfigLoaded(int index, GunData loaded)
        {
            if (guns == null || index < 0 || index >= guns.Length)
            {
                return;
            }

            guns[index] = loaded;
            pendingLoads = Mathf.Max(0, pendingLoads - 1);

            if (pendingLoads > 0)
            {
                return;
            }

            int validCount = 0;
            for (int i = 0; i < guns.Length; i++)
            {
                if (guns[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return;
            }

            // Compact null holes so Equip indices stay dense.
            if (validCount != guns.Length)
            {
                GunData[] compacted = new GunData[validCount];
                int write = 0;
                for (int i = 0; i < guns.Length; i++)
                {
                    if (guns[i] != null)
                    {
                        compacted[write++] = guns[i];
                    }
                }

                guns = compacted;
            }

            IsReady = true;
            GunsReady?.Invoke();
            Equip(0);
        }

        private void releaseLoaders()
        {
            if (loaders == null)
            {
                return;
            }

            for (int i = 0; i < loaders.Length; i++)
            {
                loaders[i]?.Release();
            }

            loaders = null;
            pendingLoads = 0;
        }
    }
}
