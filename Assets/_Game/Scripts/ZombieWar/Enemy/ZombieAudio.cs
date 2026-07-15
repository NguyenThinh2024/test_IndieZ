using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Enemy
{
    /// <summary>
    /// Zombie SFX: chase (moan/grunt), hit, death.
    /// Drag clips in Inspector or apply Addressable-loaded clips from config.
    /// </summary>
    public sealed class ZombieAudio : MonoBehaviour
    {
        [SerializeField] private ZombieHealth health;
        [SerializeField] private AudioSource audioSource;

        [SerializeField] private AudioClip chaseClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip deathClip;

        [SerializeField] private float chaseIntervalMin = 2.8f;
        [SerializeField] private float chaseIntervalMax = 4.5f;
        [SerializeField] [Range(0f, 1f)] private float chaseVolume = 0.85f;
        [SerializeField] [Range(0f, 1f)] private float hitVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float deathVolume = 1f;

        private float nextChaseSoundTime;
        private bool isListening;
        private bool isActive;
        private bool usesCentralTick;

        private void Awake()
        {
            bindLocalDependencies();
            ensureAudioSource();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bindLocalDependencies();
        }
#endif

        private void OnEnable()
        {
            beginListen();
        }

        private void OnDisable()
        {
            endListen();
            stopVoice();
        }

        private void Update()
        {
            if (usesCentralTick)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void SetCentralTick(bool value)
        {
            usesCentralTick = value;
        }

        public void ApplyConfig(ZombieEnemyAudioConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (config.ChaseClip != null)
            {
                chaseClip = config.ChaseClip;
            }

            if (config.HitClip != null)
            {
                hitClip = config.HitClip;
            }

            if (config.DeathClip != null)
            {
                deathClip = config.DeathClip;
            }

            chaseIntervalMin = config.ChaseIntervalMin;
            chaseIntervalMax = config.ChaseIntervalMax;
        }

        public void OnSpawn()
        {
            isActive = true;
            bindLocalDependencies();
            ensureAudioSource();
            beginListen();

            // First chase voice shortly after spawn so loading→spawn feedback is audible.
            PlayChase();
            scheduleNextChaseSound(immediate: false);
        }

        public void OnDespawn()
        {
            isActive = false;
            stopVoice();
        }

        public void Tick(float deltaTime)
        {
            if (!isActive || health == null || !health.IsAlive)
            {
                return;
            }

            if (Time.time < nextChaseSoundTime)
            {
                return;
            }

            PlayChase();
            scheduleNextChaseSound(immediate: false);
        }

        public void PlayChase()
        {
            playOneShot(chaseClip, chaseVolume);
        }

        public void PlayHit()
        {
            playOneShot(hitClip, hitVolume);
        }

        public void PlayDeath()
        {
            stopVoice();
            playOneShot(deathClip, deathVolume);
        }

        private void bindLocalDependencies()
        {
            if (health == null)
            {
                health = GetComponent<ZombieHealth>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void ensureAudioSource()
        {
            if (audioSource != null)
            {
                configureAudioSource(audioSource);
                return;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            configureAudioSource(audioSource);
        }

        private static void configureAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            // Mix 2D/3D so chase voice stays audible while zombies approach from distance.
            source.spatialBlend = 0.65f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3f;
            source.maxDistance = 45f;
        }

        private void beginListen()
        {
            if (isListening || health == null)
            {
                return;
            }

            health.Hit += onHit;
            health.Died += onDied;
            isListening = true;
        }

        private void endListen()
        {
            if (!isListening || health == null)
            {
                return;
            }

            health.Hit -= onHit;
            health.Died -= onDied;
            isListening = false;
        }

        private void onHit(DamageInfo _)
        {
            if (!isActive)
            {
                return;
            }

            PlayHit();
        }

        private void onDied()
        {
            isActive = false;
            PlayDeath();
        }

        private void scheduleNextChaseSound(bool immediate)
        {
            float delay = immediate
                ? Random.Range(0.15f, 0.8f)
                : Random.Range(chaseIntervalMin, chaseIntervalMax);
            nextChaseSoundTime = Time.time + delay;
        }

        private void playOneShot(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            ensureAudioSource();
            if (audioSource == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void stopVoice()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}
