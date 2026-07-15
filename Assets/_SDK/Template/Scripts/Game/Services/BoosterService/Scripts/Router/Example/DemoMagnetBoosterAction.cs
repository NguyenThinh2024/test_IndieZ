using UnityEngine;
using Nexzap.Base.Data;

namespace Nexzap.Base.Gameplay
{
    public class DemoMagnetBoosterAction : MonoBehaviour, IBoosterInstantAction
    {
        public BoosterType Type => BoosterType.Booster1;

        public bool TryApply()
        {
            Debug.Log("[DemoMagnetBoosterAction] APPLY magnet booster (demo)");
            return true; // trả true để service consume booster/coin
        }
    }
}