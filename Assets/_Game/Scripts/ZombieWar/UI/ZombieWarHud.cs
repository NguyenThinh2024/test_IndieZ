using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Bomb;
using ZombieWar.Core;
using ZombieWar.Level;
using ZombieWar.Player;
using ZombieWar.Weapon;

namespace ZombieWar.UI
{
    public sealed class ZombieWarHud : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private BombThrower bombThrower;
        [SerializeField] private ZombieWarGameFlow gameFlow;

        [SerializeField] private Image healthFill;
        [SerializeField] private Image timerFill;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private Button switchWeaponButton;
        [SerializeField] private Button bombButton;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        [SerializeField] private GameObject switchWeaponVfxPrefab;
        [SerializeField] private Transform switchWeaponVfxPoint;
        [SerializeField] private float switchWeaponVfxLifeTime = 1.5f;

        private void Awake()
        {
            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }

            if (losePanel != null)
            {
                losePanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthNormalizedChanged += UpdateHealth;
            }

            if (weaponController != null)
            {
                weaponController.WeaponChanged += UpdateWeapon;
            }

            if (gameFlow != null)
            {
                gameFlow.TimeNormalizedChanged += UpdateTimer;
                gameFlow.Won += ShowWin;
                gameFlow.Lost += ShowLose;
            }

            if (switchWeaponButton != null)
            {
                switchWeaponButton.onClick.AddListener(OnSwitchWeaponClicked);
            }

            if (bombButton != null)
            {
                bombButton.onClick.AddListener(OnBombClicked);
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthNormalizedChanged -= UpdateHealth;
            }

            if (weaponController != null)
            {
                weaponController.WeaponChanged -= UpdateWeapon;
            }

            if (gameFlow != null)
            {
                gameFlow.TimeNormalizedChanged -= UpdateTimer;
                gameFlow.Won -= ShowWin;
                gameFlow.Lost -= ShowLose;
            }

            if (switchWeaponButton != null)
            {
                switchWeaponButton.onClick.RemoveListener(OnSwitchWeaponClicked);
            }

            if (bombButton != null)
            {
                bombButton.onClick.RemoveListener(OnBombClicked);
            }
        }

        private void UpdateHealth(float normalized)
        {
            if (healthFill != null)
            {
                healthFill.fillAmount = normalized;
            }
        }

        private void UpdateTimer(float normalized)
        {
            if (timerFill != null)
            {
                timerFill.fillAmount = normalized;
            }
        }

        private void UpdateWeapon(GunData gunData, int _)
        {
            if (gunData == null)
            {
                return;
            }

            if (weaponIcon != null)
            {
                weaponIcon.sprite = gunData.Icon;
            }

            if (weaponNameText != null)
            {
                weaponNameText.text = gunData.DisplayName;
            }
        }

        private void OnSwitchWeaponClicked()
        {
            if (weaponController != null)
            {
                weaponController.SwitchNext();
            }
            else
            {
                playerCombat?.SwitchWeapon();
            }

            playSwitchWeaponVfx();
        }

        private void playSwitchWeaponVfx()
        {
            if (switchWeaponVfxPrefab == null)
            {
                return;
            }

            Transform anchor = switchWeaponVfxPoint != null ? switchWeaponVfxPoint : transform;
            PooledVfx.Spawn(
                switchWeaponVfxPrefab,
                anchor.position,
                anchor.rotation,
                switchWeaponVfxLifeTime);
        }

        private void OnBombClicked()
        {
            bombThrower?.TryThrow();
        }

        private void ShowWin()
        {
            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
        }

        private void ShowLose()
        {
            if (losePanel != null)
            {
                losePanel.SetActive(true);
            }
        }
    }
}
