using System.Collections.Generic;
using UnityEngine;
using Nexzap.Base.Data;

namespace Nexzap.Base.Gameplay
{
    public class BoosterInstantRouter : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> actions = new();

        private readonly Dictionary<BoosterType, IBoosterInstantAction> _map = new();

        private void Awake()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map.Clear();
            for (int i = 0; i < actions.Count; i++)
            {
                var mb = actions[i];
                if (mb is IBoosterInstantAction a)
                    _map[a.Type] = a;
            }
        }

        public bool TryApply(BoosterType type)
        {
            if (_map.TryGetValue(type, out var a) && a != null)
            {
                AudioController.Instance.PlaySound(SoundName.UI_BoosterUse);
                return a.TryApply();
            }
                

            Debug.LogWarning($"[BoosterInstantRouter] No instant action registered for {type}");
            return false;
        }
    }
}