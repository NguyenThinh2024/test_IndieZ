using UnityEngine;
using Thinh.Base.Data;

namespace Thinh.Base.Gameplay
{
    public class DemoHammerBoosterAction : MonoBehaviour, IBoosterArmedAction
    {
        private BoosterService boosterService;

        // Đổi đúng BoosterType của hammer/booster armed của bạn
        public BoosterType Type => BoosterType.Booster2;


        private void Start()
        {
            TryResolveService();
        }

        private void TryResolveService()
        {
            if (boosterService != null) return;
            if (GameController.Instance == null) return;

            // Services.Get<T>() của bạn (nếu có) hoặc TryGet(out)
            boosterService = GameController.Instance.Services.Get<BoosterService>();
        }

        public bool IsValidTarget(Component target)
        {
            // Demo: click anywhere is valid (target can be null)
            return true;
        }

        public bool TryApplyTo(Component target)
        {
            if (target != null)
                Debug.Log($"[DemoHammerBoosterAction] APPLY hammer (demo) -> target: {target.name} ({target.GetType().Name})");
            else
                Debug.Log("[DemoHammerBoosterAction] APPLY hammer (demo) -> no target (click anywhere)");

            return true; // return true => BoosterService will consume + unarm
        }

        private void Update()
        {
            if (boosterService == null)
            {
                TryResolveService();
                if (boosterService == null) return;
            }

            // ✅ chỉ xử lý input khi booster này đang được armed/active
            if (!boosterService.ArmedBooster.HasValue) return;
            if (boosterService.ArmedBooster.Value != Type) return;

            if (Input.GetMouseButtonDown(0))
            {
                // demo: click anywhere -> no target
                boosterService.TryApplyArmedOnTarget(null);
            }
        }
    }
}