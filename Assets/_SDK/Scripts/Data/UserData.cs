using System;

namespace Nexzap.Base.Data
{
    [Serializable]
    public sealed class UserData
    {
        public int coin;
        public LifeData life = new LifeData();
    }

    [Serializable]
    public sealed class LifeData
    {
        public const int MAX_LIVES = 5;

        public int liveAmount = MAX_LIVES;
        public long nextRefillUnixTime;
        public long liveInfinityEndUnixTime;
    }
}
