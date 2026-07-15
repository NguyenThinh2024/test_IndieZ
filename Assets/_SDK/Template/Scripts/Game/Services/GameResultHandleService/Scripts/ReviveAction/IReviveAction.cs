using System;
using Nexzap.Template;

namespace Nexzap.Base.Gameplay
{
    public interface IReviveAction
    {
        FailType FailType { get; }
        void Execute();
    }
}
