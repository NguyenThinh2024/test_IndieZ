using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Thinh.Base.UI;
using Thinh.Base.Gameplay;
using Thinh.Base.Data;

public class UIBoosterButton : MonoBehaviour
{
    [SerializeField] private BoosterType boosterType;
    public BoosterType Type => boosterType;

    [Header("UI References")]
    [SerializeField] private UIBaseButton button;
    [SerializeField] private Image iconImage;

    [Header("Amount")]
    [SerializeField] private GameObject amountGO;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Price")]
    [SerializeField] private GameObject priceGO;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("Lock")]
    [SerializeField] private GameObject lockGO;
    [SerializeField] private TextMeshProUGUI lockLevelText;

    [Header("Cooldown")]
    [Tooltip("Set Image.Type = Filled for fillAmount to work.")]
    [SerializeField] private Image cooldownImg;

    [Header("Armed")]
    [SerializeField] private GameObject armedGO;

    private BoosterService _service;

    private float _cdRemaining;
    private float _cdTotal;

    public void Bind(BoosterService service)
    {
        _service = service;

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (_service == null) return;
        _service.ClickBooster(boosterType);
    }

    public void Render(in BoosterService.BoosterState st, bool lockOthers)
    {
        // 1) Icon
        if (iconImage != null && st.icon != null)
            iconImage.sprite = st.icon;

        // 2) Lock
        if (lockGO != null) lockGO.SetActive(!st.unlocked);
        if (lockLevelText != null) lockLevelText.text = $"Lv.{st.unlockLevel}";

        if (!st.unlocked)
        {
            // Locked: hide amount/price
            if (amountGO != null) amountGO.SetActive(false);
            if (priceGO != null) priceGO.SetActive(false);

            // cooldown/armed vẫn có thể reset UI
            if (armedGO != null) armedGO.SetActive(false);
            if (cooldownImg != null)
            {
                cooldownImg.gameObject.SetActive(false);
                cooldownImg.fillAmount = 0f;
            }

            SetInteractableSafe(false);
            return;
        }

        // 3) Amount vs Price
        if (st.amount > 0)
        {
            if (amountGO != null) amountGO.SetActive(true);
            if (countText != null) countText.text = st.amount.ToString();

            if (priceGO != null) priceGO.SetActive(false);
        }
        else
        {
            if (amountGO != null) amountGO.SetActive(false);
            if (countText != null) countText.text = "0";

            if (priceGO != null) priceGO.SetActive(true);
            if (priceText != null) priceText.text = st.price.ToString();
        }

        // 4) Armed overlay
        if (armedGO != null) armedGO.SetActive(st.isArmed);

        // 5) Cooldown
        if (cooldownImg != null)
        {
            cooldownImg.gameObject.SetActive(st.inCooldown);

            if (st.inCooldown && st.cooldownTotal > 0f)
            {
                _cdRemaining = st.cooldownTotal;
                _cdTotal = st.cooldownTotal;

                float total = Mathf.Max(0.0001f, st.cooldownTotal);
                cooldownImg.fillAmount = Mathf.Clamp01(st.cooldownRemaining / total);
            }
            else
            {
                cooldownImg.fillAmount = 0f;
            }
        }

        // 6) Interactable rule
        bool canInteract = true;

        // cooldown -> disable
        if (st.inCooldown) canInteract = false;

        // another booster armed -> disable
        if (lockOthers) canInteract = false;

        // booster đang armed vẫn interactable để cancel (lockOthers phải false cho nó)
        SetInteractableSafe(canInteract);
    }

    private void Update()
    {
        if (_cdRemaining <= 0f) return;

        _cdRemaining -= Time.unscaledDeltaTime;
        if (_cdRemaining <= 0f)
        {
            _cdRemaining = 0f;
            if (cooldownImg != null)
            {
                cooldownImg.gameObject.SetActive(false);
                cooldownImg.fillAmount = 0f;
            }
            return;
        }

        if (cooldownImg != null && _cdTotal > 0f)
            cooldownImg.fillAmount = Mathf.Clamp01(_cdRemaining / _cdTotal);
    }

    private void SetInteractableSafe(bool v)
    {
        if (button == null) return;
        try
        {
            button.SetInteractable(v);
        }
        catch
        {
            // fallback: nếu UIBaseButton wrap UnityEngine.UI.Button
            var unityBtn = button.GetComponent<UnityEngine.UI.Button>();
            if (unityBtn != null) unityBtn.interactable = v;
        }
    }
}