#if IAP
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nexzap.Base.UI;
using Nexzap.Base.Ads;
using Nexzap.Base.Analytics;
using Cysharp.Threading.Tasks;
using Nexzap.Base;
using Nexzap.Base.RemoteConfigs;

public class FreeCoinButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIBaseButton freeCoinButton;
    [SerializeField] private TextMeshProUGUI freeCoinText; // Hiển thị "Miễn phí (x/5)"

    [Header("Config")]
    [SerializeField] private int maxFreeClaimPerDay = 5;   // Số lần nhận miễn phí tối đa mỗi ngày (mặc định: 5)
    private const string PREF_REMAINING = "FREE_COIN_REMAINING";
    private const string PREF_DATE = "FREE_COIN_DATE";
    private const string ADS_PLACEMENT = "free_coin_button";

    private int _remainingToday;

    private void Awake()
    {
        RegisterButton(true);
    }

    private void OnDestroy()
    {
        RegisterButton(false);
    }

    private void Start()
    {
        // Load dữ liệu trong PlayerPrefs, nếu sang ngày mới thì reset lại số lần
        LoadOrResetDailyData();
        // Cập nhật UI text + trạng thái button
        RefreshUI();
    }

    /// <summary>
    /// Hàm callback khi nhấn nút nhận coin miễn phí
    /// </summary>
    private void OnClickFreeCoin()
    {
        if (_remainingToday <= 0)
        {
            // Hết lượt trong ngày, không cho nhấn nữa
            return;
        }

        AdsController.Instance.ShowRewardedAds(ADS_PLACEMENT, success =>
        {
            if (!success) return;
            SoundManager.Instance.PlaySound(SoundName.UI_RewardAds);
            // Phát coin cho người chơi —— ở đây gọi vào hệ thống tiền tệ thực tế của game
            GrantFreeCoin();

            _remainingToday--;
            SaveDailyData();
            RefreshUI();
        });
        
    }

    /// <summary>
    /// Hàm thực sự cộng coin cho người chơi
    /// Tùy dự án mà đổi sang hàm AddCoin thực tế
    /// </summary>
    private void GrantFreeCoin()
    {
        PlayAnimCoins(RemoteConfigsController.coin_watch_ads).Forget();
    }

    private async UniTask PlayAnimCoins(int value)
    {
        Canvas canvas = ResourceManager.Instance.GetComponent<Canvas>();
        RectTransform src = GetCenterScreenSource(canvas);
        RectTransform dst = ResourceManager.Instance.UIResource.coinImg.rectTransform;

        // Giới hạn số lượng coin trong animation (tối đa 20 coin)
        int animCount = Mathf.Min(value, 20);
        RewardData animData = RewardAnimation.CreateReward(eItemType.Currency, animCount);

        // Tạo reward data thực tế với số lượng đúng
        RewardData realReward = RewardAnimation.CreateReward(eItemType.Currency, value);

        // await RewardAnimation.Play(animData, src, dst, canvas, (reward) =>
        // {
        //     // Grant reward với số lượng thực tế
        //     RewardUtility.GainReward(realReward);
        // });

        await RewardUtility.PlayRewardAnimation(realReward, src, canvas);
    }

    private RectTransform GetCenterScreenSource(Canvas canvas)
    {
        GameObject go = new GameObject("IAPRewardSource", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();

        // Đặt ở giữa màn hình
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        // Tự động xóa sau một thời gian
        GameObject.Destroy(go, 5f);

        return rt;
    }


    /// <summary>
    /// Đọc dữ liệu từ PlayerPrefs.
    /// Nếu là ngày mới (khác yyyyMMdd) thì reset lại số lần nhận trong ngày.
    /// </summary>
    private void LoadOrResetDailyData()
    {
        string today = DateTime.UtcNow.Date.ToString("yyyyMMdd");
        string savedDate = PlayerPrefs.GetString(PREF_DATE, string.Empty);

        if (string.IsNullOrEmpty(savedDate) || savedDate != today)
        {
            // Ngày mới -> reset lại số lần
            _remainingToday = maxFreeClaimPerDay;
            PlayerPrefs.SetString(PREF_DATE, today);
            PlayerPrefs.SetInt(PREF_REMAINING, _remainingToday);
            PlayerPrefs.Save();
        }
        else
        {
            _remainingToday = PlayerPrefs.GetInt(PREF_REMAINING, maxFreeClaimPerDay);
        }
    }

    /// <summary>
    /// Lưu lại số lần còn lại trong ngày vào PlayerPrefs
    /// </summary>
    private void SaveDailyData()
    {
        PlayerPrefs.SetInt(PREF_REMAINING, _remainingToday);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Cập nhật text hiển thị và trạng thái button
    /// </summary>
    private void RefreshUI()
    {
        if (freeCoinText != null)
        {
            // Text hiển thị: "Miễn phí (x/5)"
            freeCoinText.text = $"Free({_remainingToday}/{maxFreeClaimPerDay})";
        }

        if (freeCoinButton != null && freeCoinButton.button != null)
        {
            // Hết lượt thì tắt interactable của nút
            freeCoinButton.button.interactable = _remainingToday > 0;
        }
    }

    /// <summary>
    /// Đăng ký hoặc huỷ đăng ký sự kiện click trên UIBaseButton
    /// </summary>
    /// <param name="register">true = đăng ký, false = huỷ</param>
    private void RegisterButton(bool register)
    {
        if (freeCoinButton == null) return;

        if (register)
        {
            freeCoinButton.onClick.AddListener(OnClickFreeCoin);
        }
        else
        {
            freeCoinButton.onClick.RemoveListener(OnClickFreeCoin);
        }
    }
}
#endif