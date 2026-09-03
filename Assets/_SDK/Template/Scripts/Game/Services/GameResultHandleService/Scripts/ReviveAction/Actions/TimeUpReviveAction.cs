using Thinh.Base.Ads;
using Thinh.Base.Gameplay;
using Thinh.Template;
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