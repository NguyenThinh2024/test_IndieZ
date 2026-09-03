using System;
using Thinh.Template;

namespace Thinh.Base.Gameplay
{
    public interface IReviveAction
    {
        FailType FailType { get; }
        void Execute();
    }
}
