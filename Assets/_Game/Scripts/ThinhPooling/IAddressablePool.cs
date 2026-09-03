namespace ThinhPooling
{
    public interface IAddressablePool
    {
        void Release(PooledInstance instance);
    }
}
