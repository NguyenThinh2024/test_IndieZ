using Nexzap.Base.Gameplay;
using Nexzap.Template;
using UnityEngine;

public sealed class ReviveActionRouter : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] actions;

    public IReviveAction Resolve(FailType failType)
    {
        if (actions != null)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                var mb = actions[i];
                if (mb == null) continue;

                if (mb is IReviveAction act && act.FailType == failType)
                    return act;
            }
        }

        return FindActionInScene(failType);
    }

    public void TryExecute(FailType failType)
    {
        var act = Resolve(failType);
        act?.Execute();
    }

    private static IReviveAction FindActionInScene(FailType failType)
    {
#if UNITY_2023_1_OR_NEWER
        MonoBehaviour[] allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        MonoBehaviour[] allBehaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            MonoBehaviour mb = allBehaviours[i];
            if (mb is IReviveAction action && action.FailType == failType)
            {
                return action;
            }
        }

        return null;
    }
}