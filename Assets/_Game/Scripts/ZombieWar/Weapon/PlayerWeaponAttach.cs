using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// Attaches the current GunData visual (Addressables) to the humanoid RightHand and wires fire/recoil roots.
    /// </summary>
    public sealed class PlayerWeaponAttach : MonoBehaviour, ZombieWar.Player.IPlayerWeaponAttach
    {
        private const string WeaponSocketName = "WeaponSocket";
        private const string RecoilRootName = "RecoilRoot";
        private const string FirePointName = "FirePoint";

        [SerializeField] private WeaponController weaponController;
        [SerializeField] private ProjectileWeapon projectileWeapon;
        [SerializeField] private GunRecoil gunRecoil;
        [SerializeField] private HumanBodyBones handBone = HumanBodyBones.RightHand;

        private Animator characterAnimator;
        private Transform weaponSocket;
        private Transform recoilRoot;
        private GameObject currentVisual;
        private Transform currentFirePoint;
        private AsyncOperationHandle<GameObject> visualHandle;
        private int equipRequestId;
        private bool isListening;

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

        private void OnEnable()
        {
            beginListen();
        }

        private void OnDisable()
        {
            endListen();
            clearVisual();
        }

        public void Bind(Animator humanoidAnimator)
        {
            characterAnimator = humanoidAnimator;
            if (characterAnimator == null)
            {
                return;
            }

            ensureSocketHierarchy();
            gunRecoil?.SetWeapon(projectileWeapon);
            gunRecoil?.SetRecoilRoot(recoilRoot);
            beginListen();

            if (weaponController != null && weaponController.IsReady && weaponController.CurrentGun != null)
            {
                equipVisual(weaponController.CurrentGun, weaponController.CurrentIndex);
            }
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

            if (gunRecoil == null)
            {
                gunRecoil = GetComponent<GunRecoil>();
            }
        }

        private void beginListen()
        {
            if (isListening || weaponController == null)
            {
                return;
            }

            weaponController.WeaponChanged += equipVisual;
            isListening = true;
        }

        private void endListen()
        {
            if (!isListening || weaponController == null)
            {
                return;
            }

            weaponController.WeaponChanged -= equipVisual;
            isListening = false;
        }

        private void ensureSocketHierarchy()
        {
            Transform hand = characterAnimator.GetBoneTransform(handBone);
            if (hand == null)
            {
                return;
            }

            weaponSocket = hand.Find(WeaponSocketName);
            if (weaponSocket == null)
            {
                GameObject socketObject = new GameObject(WeaponSocketName);
                weaponSocket = socketObject.transform;
                weaponSocket.SetParent(hand, false);
                weaponSocket.localPosition = Vector3.zero;
                weaponSocket.localRotation = Quaternion.identity;
                weaponSocket.localScale = Vector3.one;
            }

            recoilRoot = weaponSocket.Find(RecoilRootName);
            if (recoilRoot == null)
            {
                GameObject recoilObject = new GameObject(RecoilRootName);
                recoilRoot = recoilObject.transform;
                recoilRoot.SetParent(weaponSocket, false);
                recoilRoot.localPosition = Vector3.zero;
                recoilRoot.localRotation = Quaternion.identity;
                recoilRoot.localScale = Vector3.one;
            }
        }

        private void equipVisual(GunData gunData, int _)
        {
            if (gunData == null || recoilRoot == null)
            {
                return;
            }

            clearVisual();

            string address = gunData.VisualPrefabAddress;
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            int requestId = ++equipRequestId;
            try
            {
                visualHandle = Addressables.InstantiateAsync(address, recoilRoot);
                visualHandle.Completed += handle => onVisualInstantiated(handle, gunData, requestId);
            }
            catch (InvalidKeyException)
            {
            }
        }

        private void onVisualInstantiated(AsyncOperationHandle<GameObject> handle, GunData gunData, int requestId)
        {
            if (requestId != equipRequestId)
            {
                return;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                return;
            }

            currentVisual = handle.Result;
            currentVisual.name = gunData.DisplayName;
            currentVisual.transform.localPosition = gunData.GripLocalPosition;
            currentVisual.transform.localRotation = Quaternion.Euler(gunData.GripLocalEuler);
            currentVisual.transform.localScale = Vector3.one * gunData.VisualLocalScale;

            currentFirePoint = ensureFirePoint(currentVisual.transform, gunData.FirePointLocalPosition);
            projectileWeapon?.SetFirePoint(currentFirePoint);
            gunRecoil?.SetRecoilRoot(recoilRoot);
            gunRecoil?.SetWeapon(projectileWeapon);
        }

        private static Transform ensureFirePoint(Transform gunRoot, Vector3 localPosition)
        {
            Transform firePoint = gunRoot.Find(FirePointName);
            if (firePoint == null)
            {
                GameObject firePointObject = new GameObject(FirePointName);
                firePoint = firePointObject.transform;
                firePoint.SetParent(gunRoot, false);
            }

            firePoint.localPosition = localPosition;
            firePoint.localRotation = Quaternion.identity;
            firePoint.localScale = Vector3.one;
            return firePoint;
        }

        private void clearVisual()
        {
            equipRequestId++;
            currentFirePoint = null;
            currentVisual = null;

            if (!visualHandle.IsValid())
            {
                return;
            }

            Addressables.ReleaseInstance(visualHandle);
            visualHandle = default;
        }
    }
}
