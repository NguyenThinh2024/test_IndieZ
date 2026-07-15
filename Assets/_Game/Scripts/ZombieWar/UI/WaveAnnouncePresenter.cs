using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZombieWar.Level;

namespace ZombieWar.UI
{
    /// <summary>
    /// UI composition root for wave announce.
    /// Mirrors template flow: event → SetData → Show → auto Hide.
    /// Does not touch spawn / Addressables ownership.
    /// </summary>
    public sealed class WaveAnnouncePresenter : MonoBehaviour
    {
        [SerializeField] private WaveManager waveManager;

        [SerializeField] private UIWaveAnnouncePopup wavePopup;
        [SerializeField] private UIWaveAnnouncePopup bossPopup;
        [SerializeField] private UIWaveBossFlashOverlay bossFlashOverlay;

        [SerializeField] private float waveAutoHideSeconds = 2.2f;
        [SerializeField] private float bossAutoHideSeconds = 3.8f;

        private int showToken;

        private void OnEnable()
        {
            bindWaveManager(waveManager);
            hideViewsImmediate();
        }

        private void OnDisable()
        {
            unbindWaveManager(waveManager);
            showToken++;
            hideViewsImmediate();
        }

        public void Bind(WaveManager manager)
        {
            if (waveManager == manager)
            {
                return;
            }

            unbindWaveManager(waveManager);
            waveManager = manager;
            bindWaveManager(waveManager);
        }

        public void OnWaveAnnounced(WaveAnnounceInfo info)
        {
            showToken++;
            int token = showToken;

            if (info.IsBoss)
            {
                showBoss(info, token).Forget();
            }
            else
            {
                showWave(info, token).Forget();
            }
        }

        private void bindWaveManager(WaveManager manager)
        {
            if (manager != null)
            {
                manager.WaveAnnounced += OnWaveAnnounced;
            }
        }

        private void unbindWaveManager(WaveManager manager)
        {
            if (manager != null)
            {
                manager.WaveAnnounced -= OnWaveAnnounced;
            }
        }

        private async UniTaskVoid showWave(WaveAnnounceInfo info, int token)
        {
            UIWaveAnnouncePopup popup = wavePopup != null ? wavePopup : bossPopup;
            if (popup == null)
            {
                return;
            }

            bossPopup?.Hide();
            bossFlashOverlay?.Stop();

            popup.SetData(info);
            popup.Show();

            float hideAfter = Mathf.Max(0.2f, waveAutoHideSeconds);
            bool canceled = await UniTask
                .Delay(TimeSpan.FromSeconds(hideAfter), DelayType.UnscaledDeltaTime, cancellationToken: this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();

            if (canceled || token != showToken)
            {
                return;
            }

            popup.Hide();
        }

        private async UniTaskVoid showBoss(WaveAnnounceInfo info, int token)
        {
            UIWaveAnnouncePopup popup = bossPopup != null ? bossPopup : wavePopup;
            if (popup == null)
            {
                return;
            }

            wavePopup?.Hide();
            bossFlashOverlay?.Play();

            popup.SetData(info);
            popup.Show();

            float hideAfter = Mathf.Max(0.2f, bossAutoHideSeconds);
            bool canceled = await UniTask
                .Delay(TimeSpan.FromSeconds(hideAfter), DelayType.UnscaledDeltaTime, cancellationToken: this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();

            if (canceled || token != showToken)
            {
                return;
            }

            popup.Hide();
            if (token == showToken)
            {
                bossFlashOverlay?.Stop();
            }
        }

        private void hideViewsImmediate()
        {
            wavePopup?.Hide();
            bossPopup?.Hide();
            bossFlashOverlay?.Stop();
        }
    }
}
