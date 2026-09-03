using System;

namespace ThinhPooling
{
    public interface IPoolReleaseListener
    {
        event Action ReleaseRequested;
        float ReleaseDelay { get; }
    }
}
