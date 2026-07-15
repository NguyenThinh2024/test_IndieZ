using UnityEngine;
using Nexzap.Base.Data;

namespace Nexzap.Base.Gameplay
{
    public interface IBoosterArmedAction
    {
        BoosterType Type { get; }

        // target là object gameplay player click (cell/block/item...). Bạn quyết định truyền gì.
        bool IsValidTarget(Component target);
        bool TryApplyTo(Component target);
    }
}