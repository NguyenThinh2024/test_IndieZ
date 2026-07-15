using System.Collections.Generic;
using UnityEngine;
using Nexzap.Base.Data;

namespace Nexzap.Base.Gameplay
{
    public class BoosterArmedRouter : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> actions = new();

        private readonly Dictionary<BoosterType, IBoosterArmedAction> _map = new();

        private void Awake()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map.Clear();
            foreach (var mb in actions)
            {
                if (mb is IBoosterArmedAction a)
                    _map[a.Type] = a;
            }
        }

        public bool IsValidTarget(BoosterType type, Component target)
        {
            return _map.TryGetValue(type, out var a) && a != null && a.IsValidTarget(target);
        }

        public bool TryApply(BoosterType type, Component target)
        {
            if (!_map.TryGetValue(type, out var a) || a == null) return false;
            if (!a.IsValidTarget(target)) return false;
            AudioController.Instance.PlaySound(SoundName.UI_BoosterUse);
            return a.TryApplyTo(target);
        }
    }
}