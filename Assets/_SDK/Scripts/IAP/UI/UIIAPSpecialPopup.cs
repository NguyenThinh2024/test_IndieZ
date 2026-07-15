using UnityEngine;
using Nexzap.Base.UI;
using System;

public class UIIAPSpecialPopup : UIBasePopup
{
    public static event Action onHide;
    protected override void HideCompleted()
    {
        base.HideCompleted();
        onHide?.Invoke();
    }
}
