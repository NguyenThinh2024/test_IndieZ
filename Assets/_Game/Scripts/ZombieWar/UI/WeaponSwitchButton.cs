using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Player;
using ZombieWar.Weapon;

namespace ZombieWar.UI
{
    /// <summary>
    /// Cycles guns with a cooldown and circular fill overlay on the Switch-Gun button.
    /// </summary>
    public sealed class WeaponSwitchButton : MonoBehaviour
    {
        [SerializeField] private Button switchButton;
        [SerializeField] private Image cooldownFill;

        [SerializeField] private WeaponController weaponController;
        [SerializeField] private PlayerCombat playerCombat;

        [SerializeField] private float cooldownSeconds = 2.5f;

        private float nextReadyTime;
        private bool isCoolingDown;

        public bool IsReady => Time.time >= nextReadyTime;
        public float CooldownNormalized
        {
            get
            {
                if (cooldownSeconds <= 0f)
                {
                    return 0f;
                }

                float remaining = nextReadyTime - Time.time;
                return Mathf.Clamp01(remaining / cooldownSeconds);
            }
        }

        private void Awake()
        {
            bindLocalDependencies();
            applyFillVisual(0f);
            setButtonInteractable(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bindLocalDependencies();
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            configureCooldownFillImage();
        }
#endif

        private void OnEnable()
        {
            if (switchButton != null)
            {
                switchButton.onClick.AddListener(OnSwitchClicked);
            }
        }

        private void OnDisable()
        {
            if (switchButton != null)
            {
                switchButton.onClick.RemoveListener(OnSwitchClicked);
            }
        }

        private void Update()
        {
            if (!isCoolingDown)
            {
                return;
            }

            float fill = CooldownNormalized;
            applyFillVisual(fill);

            if (fill <= 0f)
            {
                isCoolingDown = false;
                setButtonInteractable(true);
            }
        }

        public void SwitchWeapon()
        {
            OnSwitchClicked();
        }

        private void bindLocalDependencies()
        {
            if (switchButton == null)
            {
                switchButton = GetComponent<Button>();
            }

            if (cooldownFill == null)
            {
                Transform fillTransform = transform.Find("CooldownFill");
                if (fillTransform != null)
                {
                    cooldownFill = fillTransform.GetComponent<Image>();
                }
            }

            configureCooldownFillImage();
        }

        private void configureCooldownFillImage()
        {
            if (cooldownFill == null)
            {
                return;
            }

            cooldownFill.raycastTarget = false;
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownFill.fillClockwise = true;
        }

        private void OnSwitchClicked()
        {
            if (!IsReady)
            {
                return;
            }

            bool switched = false;
            int previousIndex = weaponController != null ? weaponController.CurrentIndex : -1;

            if (weaponController != null)
            {
                if (!weaponController.IsReady || !weaponController.IsSwitchReady)
                {
                    return;
                }

                weaponController.SwitchNext();
                switched = weaponController.CurrentIndex != previousIndex;
            }
            else if (playerCombat != null)
            {
                playerCombat.SwitchWeapon();
                switched = true;
            }

            if (!switched)
            {
                return;
            }

            beginCooldown();
        }

        private void beginCooldown()
        {
            float duration = Mathf.Max(0f, cooldownSeconds);
            nextReadyTime = Time.time + duration;
            isCoolingDown = duration > 0f;
            applyFillVisual(isCoolingDown ? 1f : 0f);
            setButtonInteractable(!isCoolingDown);
        }

        private void applyFillVisual(float amount)
        {
            if (cooldownFill == null)
            {
                return;
            }

            cooldownFill.fillAmount = Mathf.Clamp01(amount);
            cooldownFill.enabled = amount > 0.001f;
        }

        private void setButtonInteractable(bool interactable)
        {
            if (switchButton != null)
            {
                switchButton.interactable = interactable;
            }
        }
    }
}
