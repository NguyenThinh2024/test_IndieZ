namespace VietPooling
{
    public interface IAddressablePool
    {
        void Release(PooledInstance instance);
    }
}
