using Nexzap.Base.Ads;
using Nexzap.Base.Gameplay;
using Nexzap.Template;
using UnityEngine;

public sealed class TimeUpReviveAction : MonoBehaviour, IReviveAction
{
    [Header("Route")]
    [SerializeField] private FailType failType = FailType.TimeUp; 

    [Header("Effect")]
    [SerializeField] private int addSeconds = 30;


    public FailType FailType => failType;

    public void Execute()
    {
        GameController.Instance.Services.Get<TimerService>().AddTime(addSeconds);
    }
}