using System;

namespace VietPooling
{
    public interface IPoolReleaseListener
    {
        event Action ReleaseRequested;
        float ReleaseDelay { get; }
    }
}
