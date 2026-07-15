using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace VietPooling
{
    public sealed class AddressableComponentPool<TComponent> : IAddressablePool
        where TComponent : Component
    {
        private readonly MonoBehaviour owner;
        private readonly Transform poolRoot;
        private readonly Dictionary<string, PoolBucket> buckets = new Dictionary<string, PoolBucket>(8);
        private readonly Dictionary<string, UniTask<PoolBucket>> loadTasks = new Dictionary<string, UniTask<PoolBucket>>(8);

        public AddressableComponentPool(MonoBehaviour owner, string rootName)
        {
            this.owner = owner;

            GameObject rootObject = new GameObject(rootName);
            poolRoot = rootObject.transform;
            poolRoot.SetParent(owner.transform, false);
        }

        public void Prewarm(string assetAddress, int count, Action completedHandler = null)
        {
            prewarmAsync(assetAddress, count, owner.GetCancellationTokenOnDestroy())
                .ContinueWith(() => completedHandler?.Invoke())
                .Forget();
        }

        public UniTask PrewarmAsync(string assetAddress, int count, CancellationToken cancellationToken)
        {
            return prewarmAsync(assetAddress, count, cancellationToken);
        }

        public void Get(string assetAddress, Vector3 position, Quaternion rotation, Action<TComponent> completedHandler)
        {
            getAsync(assetAddress, position, rotation, owner.GetCancellationTokenOnDestroy())
                .ContinueWith(component => completedHandler?.Invoke(component))
                .Forget();
        }

        public UniTask<TComponent> GetAsync(
            string assetAddress,
            Vector3 position,
            Quaternion rotation,
            CancellationToken cancellationToken)
        {
            return getAsync(assetAddress, position, rotation, cancellationToken);
        }

        public void Release(PooledInstance pooledInstance)
        {
            if (pooledInstance == null)
            {
                return;
            }

            TComponent component = pooledInstance.GetComponent<TComponent>();
            if (component == null)
            {
                return;
            }

            string assetAddress = pooledInstance.PoolKey;
            if (!isValidAddress(assetAddress) || !buckets.TryGetValue(assetAddress, out PoolBucket bucket))
            {
                despawnAndDestroy(component);
                return;
            }

            despawn(component);
            bucket.Active.Remove(component);
            bucket.Available.Push(component);
            component.transform.SetParent(poolRoot, false);
        }

        public void ReleaseAll()
        {
            loadTasks.Clear();

            foreach (KeyValuePair<string, PoolBucket> pair in buckets)
            {
                destroyBucketInstances(pair.Value);
                releasePrefabHandle(pair.Value);
            }

            buckets.Clear();
        }

        private async UniTask prewarmAsync(string assetAddress, int count, CancellationToken cancellationToken)
        {
            if (!isValidAddress(assetAddress))
            {
                return;
            }

            PoolBucket bucket = await ensureBucketLoadedAsync(assetAddress, cancellationToken);
            if (bucket == null)
            {
                return;
            }

            int targetCount = Mathf.Max(0, count);
            for (int i = bucket.Available.Count; i < targetCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                TComponent created = createInstance(bucket, assetAddress);
                if (created != null)
                {
                    bucket.Available.Push(created);
                }
            }
        }

        private async UniTask<TComponent> getAsync(
            string assetAddress,
            Vector3 position,
            Quaternion rotation,
            CancellationToken cancellationToken)
        {
            if (!isValidAddress(assetAddress))
            {
                return null;
            }

            PoolBucket bucket = await ensureBucketLoadedAsync(assetAddress, cancellationToken);
            if (bucket == null)
            {
                return null;
            }

            TComponent instance = takeOrCreate(bucket, assetAddress);
            if (instance == null)
            {
                Debug.LogWarning($"Pool could not provide instance. Address: {assetAddress}", owner);
                return null;
            }

            activateInstance(instance, position, rotation);
            return instance;
        }

        private static bool isValidAddress(string assetAddress)
        {
            return !string.IsNullOrWhiteSpace(assetAddress);
        }

        private UniTask<PoolBucket> ensureBucketLoadedAsync(string assetAddress, CancellationToken cancellationToken)
        {
            if (buckets.TryGetValue(assetAddress, out PoolBucket loadedBucket) && loadedBucket.Prefab != null)
            {
                return UniTask.FromResult(loadedBucket);
            }

            if (loadTasks.TryGetValue(assetAddress, out UniTask<PoolBucket> existingTask))
            {
                return existingTask;
            }

            UniTask<PoolBucket> loadTask = loadBucketAsync(assetAddress, cancellationToken);
            loadTasks[assetAddress] = loadTask;
            return loadTask;
        }

        private async UniTask<PoolBucket> loadBucketAsync(string assetAddress, CancellationToken cancellationToken)
        {
            try
            {
                if (!buckets.TryGetValue(assetAddress, out PoolBucket bucket))
                {
                    bucket = new PoolBucket();
                    buckets[assetAddress] = bucket;
                }

                if (bucket.Prefab != null)
                {
                    return bucket;
                }

                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(assetAddress);
                await handle.ToUniTask(cancellationToken: cancellationToken);

                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    Debug.LogWarning(
                        $"Addressable prefab was not loaded. Address: {assetAddress}, Status: {handle.Status}, Exception: {handle.OperationException?.Message}",
                        owner);
                    buckets.Remove(assetAddress);
                    return null;
                }

                bucket.PrefabHandle = handle;
                bucket.Prefab = handle.Result;
                return bucket;
            }
            finally
            {
                loadTasks.Remove(assetAddress);
            }
        }

        private TComponent takeOrCreate(PoolBucket bucket, string assetAddress)
        {
            while (bucket.Available.Count > 0)
            {
                TComponent candidate = bucket.Available.Pop();
                if (candidate != null)
                {
                    bucket.Active.Add(candidate);
                    return candidate;
                }
            }

            TComponent created = createInstance(bucket, assetAddress);
            if (created != null)
            {
                bucket.Active.Add(created);
            }

            return created;
        }

        private TComponent createInstance(PoolBucket bucket, string assetAddress)
        {
            if (bucket.Prefab == null)
            {
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(bucket.Prefab, poolRoot);
            instance.name = bucket.Prefab.name;

            TComponent component = instance.GetComponent<TComponent>();
            if (component == null)
            {
                Debug.LogWarning($"Prefab does not have required component {typeof(TComponent).Name}. Address: {assetAddress}", owner);
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            bindPooledInstance(instance, assetAddress);
            instance.SetActive(false);
            return component;
        }

        private void bindPooledInstance(GameObject instance, string assetAddress)
        {
            PooledInstance pooledInstance = instance.GetComponent<PooledInstance>();
            if (pooledInstance == null)
            {
                pooledInstance = instance.AddComponent<PooledInstance>();
            }

            IPoolReleaseListener releaseListener = instance.GetComponent<IPoolReleaseListener>();
            pooledInstance.Bind(this, assetAddress, releaseListener);
        }

        private static void activateInstance(TComponent component, Vector3 position, Quaternion rotation)
        {
            Transform instanceTransform = component.transform;
            // NavMeshAgent must not stay under a pooled parent with scale/rotation.
            instanceTransform.SetParent(null, true);
            instanceTransform.SetPositionAndRotation(position, rotation);
            component.gameObject.SetActive(true);
        }

        private static void despawn(TComponent component)
        {
            if (component is IPoolable poolable)
            {
                poolable.OnDespawn();
                return;
            }

            component.gameObject.SetActive(false);
        }

        private static void despawnAndDestroy(TComponent component)
        {
            despawn(component);
            UnityEngine.Object.Destroy(component.gameObject);
        }

        private static void destroyBucketInstances(PoolBucket bucket)
        {
            foreach (TComponent component in bucket.Active)
            {
                if (component != null)
                {
                    despawnAndDestroy(component);
                }
            }

            bucket.Active.Clear();

            while (bucket.Available.Count > 0)
            {
                TComponent component = bucket.Available.Pop();
                if (component != null)
                {
                    UnityEngine.Object.Destroy(component.gameObject);
                }
            }
        }

        private static void releasePrefabHandle(PoolBucket bucket)
        {
            if (!bucket.PrefabHandle.IsValid())
            {
                return;
            }

            Addressables.Release(bucket.PrefabHandle);
            bucket.PrefabHandle = default;
            bucket.Prefab = null;
        }

        private sealed class PoolBucket
        {
            public GameObject Prefab;
            public AsyncOperationHandle<GameObject> PrefabHandle;
            public readonly Stack<TComponent> Available = new Stack<TComponent>(32);
            public readonly HashSet<TComponent> Active = new HashSet<TComponent>();
        }
    }
}
