using UnityEngine;

namespace ZombieWar.Weapon
{
    public sealed class GunRecoil : MonoBehaviour
    {
        [SerializeField] private ProjectileWeapon weapon;
        [SerializeField] private Transform recoilRoot;
        [SerializeField] private Vector3 recoilAxis = Vector3.back;
        [SerializeField] private float recoilPitchDegrees = 3f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private float recoilDistance;
        private float recoilDuration;
        private float activePitchDegrees;
        private float recoilTimer;
        private bool hasBasePose;

        private void OnEnable()
        {
            if (weapon != null)
            {
                weapon.Recoiled += Play;
            }
        }

        private void OnDisable()
        {
            if (weapon != null)
            {
                weapon.Recoiled -= Play;
            }

            resetPose();
        }

        private void Update()
        {
            if (recoilRoot == null || !hasBasePose || recoilTimer <= 0f)
            {
                return;
            }

            recoilTimer -= Time.deltaTime;
            float normalized = recoilDuration > 0f ? Mathf.Clamp01(recoilTimer / recoilDuration) : 0f;
            float curve = Mathf.Sin(normalized * Mathf.PI);

            recoilRoot.localPosition = baseLocalPosition + recoilAxis.normalized * (curve * recoilDistance);
            recoilRoot.localRotation = baseLocalRotation
                * Quaternion.Euler(-activePitchDegrees * curve, 0f, 0f);

            if (recoilTimer <= 0f)
            {
                resetPose();
            }
        }

        public void SetWeapon(ProjectileWeapon value)
        {
            if (weapon == value)
            {
                return;
            }

            if (weapon != null && isActiveAndEnabled)
            {
                weapon.Recoiled -= Play;
            }

            weapon = value;

            if (weapon != null && isActiveAndEnabled)
            {
                weapon.Recoiled += Play;
            }
        }

        public void SetRecoilRoot(Transform value)
        {
            if (recoilRoot == value)
            {
                return;
            }

            resetPose();
            recoilRoot = value;
            cacheBasePose();
        }

        public void Play(float distance, float duration, float pitchDegrees)
        {
            if (recoilRoot == null)
            {
                return;
            }

            if (!hasBasePose)
            {
                cacheBasePose();
            }

            recoilDistance = distance;
            activePitchDegrees = pitchDegrees;
            recoilDuration = Mathf.Max(0.01f, duration);
            recoilTimer = recoilDuration;
        }

        private void cacheBasePose()
        {
            if (recoilRoot == null)
            {
                hasBasePose = false;
                return;
            }

            baseLocalPosition = recoilRoot.localPosition;
            baseLocalRotation = recoilRoot.localRotation;
            hasBasePose = true;
        }

        private void resetPose()
        {
            if (recoilRoot == null || !hasBasePose)
            {
                return;
            }

            recoilRoot.localPosition = baseLocalPosition;
            recoilRoot.localRotation = baseLocalRotation;
            recoilTimer = 0f;
        }
    }
}
