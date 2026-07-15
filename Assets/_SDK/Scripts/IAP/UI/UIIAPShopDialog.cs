#if IAP
using UnityEngine;
using Nexzap.Base.UI;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Nexzap.Base.IAP;
using Nexzap.Base.Ads;
using Nexzap.Base.Data;

public class UIIAPShopDialog : UIBaseDialog
{
    public IAPShopUI ShopUI;

    public override void Show()
    {
        UIDialogManager.Instance.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        base.Show();

        AdsController.Instance.HideBannerAds();
    }

    protected override void HideCompleted()
    {
        base.HideCompleted();
        UIDialogManager.Instance.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        if (!UIDialogManager.Instance.IsActiveDialog<UILoseDialog>() 
        && !UIDialogManager.Instance.IsActiveDialog<UIWinDialog>()
        && !UIDialogManager.Instance.IsActiveDialog<UIReviveDialog>())
        {
            ResourceManager.Instance.HideUIResource();
            LevelManager.Instance.PauseTime(false);

            AdsController.Instance.ShowBannerAds();
        }
    }
}
#endif