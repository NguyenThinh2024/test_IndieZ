using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VietPooling
{
    public sealed class PooledInstance : MonoBehaviour
    {
        private IAddressablePool pool;
        private IPoolReleaseListener releaseListener;
        private string poolKey;
        private CancellationTokenSource releaseCts;

        public string PoolKey => poolKey;

        public void Bind(IAddressablePool ownerPool, string key, IPoolReleaseListener listener)
        {
            pool = ownerPool;
            poolKey = key;
            releaseListener = listener;
        }

        private void OnEnable()
        {
            if (releaseListener != null)
            {
                releaseListener.ReleaseRequested += scheduleRelease;
            }
        }

        private void OnDisable()
        {
            if (releaseListener != null)
            {
                releaseListener.ReleaseRequested -= scheduleRelease;
            }

            cancelRelease();
        }

        private void scheduleRelease()
        {
            if (pool == null)
            {
                return;
            }

            cancelRelease();
            releaseCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            releaseAfterDelayAsync(releaseCts.Token).Forget();
        }

        private async UniTask releaseAfterDelayAsync(CancellationToken cancellationToken)
        {
            float delay = releaseListener != null ? releaseListener.ReleaseDelay : 0f;
            if (delay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested || pool == null)
            {
                return;
            }

            pool.Release(this);
        }

        private void cancelRelease()
        {
            if (releaseCts == null)
            {
                return;
            }

            releaseCts.Cancel();
            releaseCts.Dispose();
            releaseCts = null;
        }
    }
}
