using System;
using UnityEngine;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// Runtime gun config parsed from Addressable JSON (not a ScriptableObject).
    /// </summary>
    [Serializable]
    public sealed class GunData
    {
        [SerializeField] private string id = "gun";
        [SerializeField] private string displayName = "Rifle";
        [SerializeField] private string visualPrefabAddress;
        // config scale is 0.2f because there wasn't enough time to scale the gun in the editor
        [SerializeField] private float visualLocalScale = 0.2f;

        [SerializeField] private Vector3 gripLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 gripLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 firePointLocalPosition = new Vector3(0f, 0.05f, 0.55f);

        [SerializeField] private float damage = 10f;
        [SerializeField] private float fireRate = 0.12f;
        [SerializeField] private float range = 16f;
        [SerializeField] private float bulletSpeed = 45f;
        [SerializeField] private int pelletCount = 1;
        [SerializeField] private float spreadAngle = 0f;
        [SerializeField] private int hitMask = -1;

        [SerializeField] private float recoilDistance = 0.04f;
        [SerializeField] private float recoilDuration = 0.05f;
        [SerializeField] private float recoilPitchDegrees = 3f;

        [SerializeField] private string bulletPrefabAddress;
        [SerializeField] private string muzzleVfxAddress;
        [SerializeField] private string hitVfxAddress;
        [SerializeField] private string fireClipAddress;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => null;
        public string VisualPrefabAddress => visualPrefabAddress;
        public Vector3 GripLocalPosition => gripLocalPosition;
        public Vector3 GripLocalEuler => gripLocalEuler;
        public Vector3 FirePointLocalPosition => firePointLocalPosition;
        public float VisualLocalScale => visualLocalScale > 0f ? visualLocalScale : 0.2f;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public float BulletSpeed => bulletSpeed > 0f ? bulletSpeed : 45f;
        public int PelletCount => Mathf.Max(1, pelletCount);
        public float SpreadAngle => spreadAngle;
        public LayerMask HitMask => hitMask;
        public float RecoilDistance => recoilDistance;
        public float RecoilDuration => recoilDuration;
        public float RecoilPitchDegrees => recoilPitchDegrees;
        public string BulletPrefabAddress => bulletPrefabAddress;
        public string MuzzleVfxAddress => muzzleVfxAddress;
        public string HitVfxAddress => hitVfxAddress;
        public string FireClipAddress => fireClipAddress;

        // Runtime-only caches filled after Addressable asset load.
        public GameObject BulletPrefab { get; set; }
        public GameObject MuzzleVfxPrefab { get; set; }
        public GameObject HitVfxPrefab { get; set; }
        public AudioClip FireClip { get; set; }
    }
}
