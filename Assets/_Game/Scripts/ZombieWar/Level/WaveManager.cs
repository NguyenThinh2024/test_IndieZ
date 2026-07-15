using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TBN;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using ZombieWar.Enemy;
using ZombieWar.Player;
using Random = UnityEngine.Random;

namespace ZombieWar.Level
{
    /// <summary>
    /// Spawns waves from LevelWaveConfig. Level is cleared when all wave spawn quotas
    /// are finished (or duration ends for unlimited waves) and no zombies remain alive.
    /// </summary>
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private LevelWaveConfig levelConfig;
        [SerializeField] private ZombieSpawnPoint[] spawnPoints;

        [SerializeField] private Transform playerTarget;
        [SerializeField] private PlayerHealth playerHealth;

        [SerializeField] private float cleanupInterval = 0.25f;
        [SerializeField] private EnemyTickSystem enemyTickSystem;

        private readonly List<ZombieWar.Enemy.Enemy> aliveZombies = new List<ZombieWar.Enemy.Enemy>(128);
        private readonly Dictionary<ZombieWar.Enemy.Enemy, Action> zombieDiedHandlers =
            new Dictionary<ZombieWar.Enemy.Enemy, Action>(128);
        private readonly Dictionary<WaveData, float> nextSpawnTimes = new Dictionary<WaveData, float>(16);
        private readonly Dictionary<WaveData, int> spawnedCounts = new Dictionary<WaveData, int>(16);
        private readonly Dictionary<WaveData, ZombieEnemyConfig> addressableConfigs = new Dictionary<WaveData, ZombieEnemyConfig>(16);
        private readonly Dictionary<WaveData, ZombieEnemyConfigLoader> configLoaders = new Dictionary<WaveData, ZombieEnemyConfigLoader>(16);
        private readonly HashSet<WaveData> announcedWaves = new HashSet<WaveData>();
        private readonly Dictionary<WaveData, float> waveUnlockTimes = new Dictionary<WaveData, float>(16);
        private ZombieEnemyPool zombieEnemyPool;
        private ZombieEnemyPresentationAssets presentationAssets;
        private float elapsedTime;
        private float nextCleanupTime;
        private bool isRunning;
        private bool hasRaisedCleared;
        private int pendingSpawns;

        /// <summary>
        /// Raised once per wave when elapsed reaches announce time (StartTime - lead).
        /// UI listens via WaveAnnouncePresenter — WaveManager does not own UI.
        /// </summary>
        public event Action<WaveAnnounceInfo> WaveAnnounced;

        public event Action Cleared;

        public float ElapsedTime => elapsedTime;
        public float Duration => levelConfig != null ? levelConfig.DurationSeconds : 180f;
        public float NormalizedTime => Duration > 0f ? Mathf.Clamp01(elapsedTime / Duration) : 1f;
        public int AliveCount => countLivingZombies();
        public bool AreSpawnsFinished => evaluateSpawnsFinished();
        public bool IsCleared =>
            hasStarted && AreSpawnsFinished && pendingSpawns <= 0 && AliveCount == 0;

        private bool hasStarted;

        private void OnValidate()
        {
            cleanupInterval = Mathf.Max(0.05f, cleanupInterval);
        }

        private void Awake()
        {
            cleanupInterval = Mathf.Max(0.05f, cleanupInterval);
            zombieEnemyPool = new ZombieEnemyPool(this);
            presentationAssets = new ZombieEnemyPresentationAssets(this);
        }

        private void OnDestroy()
        {
            zombieEnemyPool?.ReleaseAll();
            presentationAssets?.Release();
        }

        public void StartWaves()
        {
            unbindAllZombieDiedHandlers();
            elapsedTime = 0f;
            nextCleanupTime = 0f;
            isRunning = true;
            hasStarted = true;
            hasRaisedCleared = false;
            pendingSpawns = 0;
            aliveZombies.Clear();
            nextSpawnTimes.Clear();
            spawnedCounts.Clear();
            addressableConfigs.Clear();
            announcedWaves.Clear();
            waveUnlockTimes.Clear();
        }

        public void StopWaves()
        {
            isRunning = false;
        }

        /// <summary>
        /// Swap wave schedule (e.g. when LevelMapBootstrap loads a different map level).
        /// Call before StartWaves.
        /// </summary>
        public void SetLevelConfig(LevelWaveConfig config)
        {
            levelConfig = config;
        }

        public LevelWaveConfig LevelConfig => levelConfig;

        private void Update()
        {
            if (!hasStarted || levelConfig == null)
            {
                return;
            }

            if (isRunning)
            {
                elapsedTime += Time.deltaTime;
            }

            if (elapsedTime >= nextCleanupTime)
            {
                nextCleanupTime = elapsedTime + Mathf.Max(0.05f, cleanupInterval);
                CleanupAliveList();
            }

            if (isRunning)
            {
                WaveData[] waves = levelConfig.Waves;
                if (waves != null)
                {
                    tryUnlockWaves(waves);
                    tryAnnounceWaves(waves);
                    for (int i = 0; i < waves.Length; i++)
                    {
                        TickWave(waves[i]);
                    }
                }

                if (AreSpawnsFinished && pendingSpawns <= 0)
                {
                    isRunning = false;
                }
            }

            // Count living zombies every frame so corpses do not block Cleared/Win.
            tryRaiseCleared();
        }

        private void tryUnlockWaves(WaveData[] waves)
        {
            for (int i = 0; i < waves.Length; i++)
            {
                WaveData wave = waves[i];
                if (wave == null || waveUnlockTimes.ContainsKey(wave))
                {
                    continue;
                }

                if (!arePreviousWavesCleared(waves, i))
                {
                    continue;
                }

                // Wave 0 unlocks at t=0; later waves unlock when previous are cleared.
                waveUnlockTimes[wave] = i == 0 ? 0f : elapsedTime;
            }
        }

        private bool arePreviousWavesCleared(WaveData[] waves, int waveIndex)
        {
            for (int i = 0; i < waveIndex; i++)
            {
                WaveData previous = waves[i];
                if (previous == null)
                {
                    continue;
                }

                if (!isWaveSpawnQuotaReached(previous))
                {
                    return false;
                }
            }

            if (waveIndex <= 0)
            {
                return true;
            }

            return AliveCount == 0 && pendingSpawns <= 0;
        }

        private void tryAnnounceWaves(WaveData[] waves)
        {
            for (int i = 0; i < waves.Length; i++)
            {
                WaveData wave = waves[i];
                if (wave == null || announcedWaves.Contains(wave))
                {
                    continue;
                }

                if (!wave.AnnounceEnabled)
                {
                    announcedWaves.Add(wave);
                    continue;
                }

                if (!waveUnlockTimes.TryGetValue(wave, out float unlockTime))
                {
                    continue;
                }

                float spawnAt = unlockTime + wave.StartTime;
                float announceAt = Mathf.Max(unlockTime, spawnAt - wave.AnnounceLeadSeconds);
                if (elapsedTime < announceAt)
                {
                    continue;
                }

                announcedWaves.Add(wave);
                WaveAnnounced?.Invoke(new WaveAnnounceInfo(
                    i,
                    wave.DisplayName,
                    wave.IsBoss,
                    spawnAt,
                    wave.AnnounceSubtitle));
            }
        }

        private void TickWave(WaveData wave)
        {
            if (wave == null)
            {
                return;
            }

            if (!waveUnlockTimes.TryGetValue(wave, out float unlockTime))
            {
                return;
            }

            float spawnAt = unlockTime + wave.StartTime;
            if (elapsedTime < spawnAt)
            {
                return;
            }

            if (wave.ZombiePrefab == null && !hasValidConfigReference(wave))
            {
                return;
            }

            if (isWaveSpawnQuotaReached(wave))
            {
                return;
            }

            if (AliveCount >= wave.MaxAlive)
            {
                return;
            }

            if (!nextSpawnTimes.TryGetValue(wave, out float nextSpawnTime))
            {
                nextSpawnTime = spawnAt;
            }

            if (elapsedTime < nextSpawnTime)
            {
                return;
            }

            nextSpawnTimes[wave] = elapsedTime + Mathf.Max(0.05f, wave.SpawnInterval);
            SpawnZombie(wave);
        }

        private void SpawnZombie(WaveData wave)
        {
            ZombieSpawnPoint spawnPoint = PickSpawnPoint();
            if (spawnPoint == null)
            {
                return;
            }

            if (hasValidConfigReference(wave))
            {
                SpawnAddressableZombie(wave, spawnPoint);
                return;
            }

            Vector3 spawnPosition = resolveNavMeshSpawnPosition(spawnPoint.Position);
            ZombieWar.Enemy.Enemy zombie = wave.ZombiePrefab.Spawn(spawnPosition, spawnPoint.Rotation);
            if (zombie == null)
            {
                return;
            }

            registerSpawned(wave, zombie);
            zombie.Initialize(wave.ZombieDataOverride, playerTarget, playerHealth);
        }

        private void SpawnAddressableZombie(WaveData wave, ZombieSpawnPoint spawnPoint)
        {
            // Count quota before async arrive so rapid ticks do not overshoot.
            incrementSpawnedCount(wave);
            pendingSpawns++;

            if (addressableConfigs.TryGetValue(wave, out ZombieEnemyConfig config))
            {
                spawnPreparedZombie(config, spawnPoint).Forget();
                return;
            }

            ZombieEnemyConfigLoader loader = getConfigLoader(wave);
            loader.Load(loadedConfig =>
            {
                if (loadedConfig == null)
                {
                    pendingSpawns = Mathf.Max(0, pendingSpawns - 1);
                    tryRaiseCleared();
                    return;
                }

                addressableConfigs[wave] = loadedConfig;
                spawnPreparedZombie(loadedConfig, spawnPoint).Forget();
            });
        }

        private async UniTaskVoid spawnPreparedZombie(ZombieEnemyConfig config, ZombieSpawnPoint spawnPoint)
        {
            if (config == null || spawnPoint == null)
            {
                pendingSpawns = Mathf.Max(0, pendingSpawns - 1);
                tryRaiseCleared();
                return;
            }

            try
            {
                await presentationAssets.EnsureLoadedAsync(config, this.GetCancellationTokenOnDestroy());
                Vector3 spawnPosition = resolveNavMeshSpawnPosition(spawnPoint.Position);
                zombieEnemyPool.Get(
                    config,
                    spawnPosition,
                    spawnPoint.Rotation,
                    initializeAddressableZombie);
            }
            catch
            {
                pendingSpawns = Mathf.Max(0, pendingSpawns - 1);
                tryRaiseCleared();
            }
        }

        private void initializeAddressableZombie(ZombieWar.Enemy.Enemy zombie, ZombieEnemyConfig config)
        {
            pendingSpawns = Mathf.Max(0, pendingSpawns - 1);

            if (zombie == null)
            {
                tryRaiseCleared();
                return;
            }

            // Apply clips/skin before Initialize → OnSpawn plays chase with valid clips.
            presentationAssets.Apply(zombie, config);
            zombie.Initialize(config.Stats, playerTarget, playerHealth, enemyTickSystem);
            trackAlive(zombie);
            tryRaiseCleared();
        }

        private void registerSpawned(WaveData wave, ZombieWar.Enemy.Enemy zombie)
        {
            incrementSpawnedCount(wave);
            trackAlive(zombie);
        }

        private void trackAlive(ZombieWar.Enemy.Enemy zombie)
        {
            if (zombie == null)
            {
                return;
            }

            if (!aliveZombies.Contains(zombie))
            {
                aliveZombies.Add(zombie);
            }

            bindZombieDied(zombie);
        }

        private void bindZombieDied(ZombieWar.Enemy.Enemy zombie)
        {
            if (zombie == null || zombie.Health == null || zombieDiedHandlers.ContainsKey(zombie))
            {
                return;
            }

            Action handler = () => unregisterAlive(zombie);
            zombieDiedHandlers[zombie] = handler;
            zombie.Health.Died += handler;
        }

        private void unregisterAlive(ZombieWar.Enemy.Enemy zombie)
        {
            if (zombie == null)
            {
                return;
            }

            unbindZombieDied(zombie);
            aliveZombies.Remove(zombie);
            tryRaiseCleared();
        }

        private void unbindZombieDied(ZombieWar.Enemy.Enemy zombie)
        {
            if (zombie == null || !zombieDiedHandlers.TryGetValue(zombie, out Action handler))
            {
                return;
            }

            if (zombie.Health != null)
            {
                zombie.Health.Died -= handler;
            }

            zombieDiedHandlers.Remove(zombie);
        }

        private void unbindAllZombieDiedHandlers()
        {
            foreach (KeyValuePair<ZombieWar.Enemy.Enemy, Action> pair in zombieDiedHandlers)
            {
                if (pair.Key != null && pair.Key.Health != null)
                {
                    pair.Key.Health.Died -= pair.Value;
                }
            }

            zombieDiedHandlers.Clear();
        }

        private int countLivingZombies()
        {
            int count = 0;
            for (int i = 0; i < aliveZombies.Count; i++)
            {
                ZombieWar.Enemy.Enemy zombie = aliveZombies[i];
                if (zombie != null && zombie.gameObject.activeInHierarchy && zombie.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private void incrementSpawnedCount(WaveData wave)
        {
            if (!spawnedCounts.TryGetValue(wave, out int count))
            {
                count = 0;
            }

            spawnedCounts[wave] = count + 1;
        }

        private bool isWaveSpawnQuotaReached(WaveData wave)
        {
            if (wave.SpawnCount <= 0)
            {
                return elapsedTime >= Duration;
            }

            spawnedCounts.TryGetValue(wave, out int spawned);
            return spawned >= wave.SpawnCount;
        }

        private bool evaluateSpawnsFinished()
        {
            if (!hasStarted || levelConfig == null)
            {
                return false;
            }

            WaveData[] waves = levelConfig.Waves;
            if (waves == null || waves.Length == 0)
            {
                return elapsedTime >= Duration;
            }

            for (int i = 0; i < waves.Length; i++)
            {
                WaveData wave = waves[i];
                if (wave == null)
                {
                    continue;
                }

                if (!isWaveSpawnQuotaReached(wave))
                {
                    return false;
                }
            }

            return true;
        }

        private void tryRaiseCleared()
        {
            if (hasRaisedCleared || !IsCleared)
            {
                return;
            }

            hasRaisedCleared = true;
            Cleared?.Invoke();
        }

        private ZombieEnemyConfigLoader getConfigLoader(WaveData wave)
        {
            if (configLoaders.TryGetValue(wave, out ZombieEnemyConfigLoader loader))
            {
                return loader;
            }

            loader = new ZombieEnemyConfigLoader(wave.ZombieConfigReference, this);
            configLoaders.Add(wave, loader);
            return loader;
        }

        private bool hasValidConfigReference(WaveData wave)
        {
            AssetReferenceT<TextAsset> reference = wave.ZombieConfigReference;
            return reference != null && reference.RuntimeKeyIsValid();
        }

        /// <summary>
        /// Call after NavMesh bake so authored spawn transforms sit on walkable ground.
        /// </summary>
        public void SnapSpawnPointsToNavMesh()
        {
            if (spawnPoints == null)
            {
                return;
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                ZombieSpawnPoint spawnPoint = spawnPoints[i];
                if (spawnPoint == null)
                {
                    continue;
                }

                Vector3 snapped = resolveNavMeshSpawnPosition(spawnPoint.Position);
                spawnPoint.transform.position = snapped;
            }
        }

        private Vector3 resolveNavMeshSpawnPosition(Vector3 desired)
        {
            float[] radii = { 2f, 8f, 20f, 40f };
            for (int i = 0; i < radii.Length; i++)
            {
                if (NavMesh.SamplePosition(desired, out NavMeshHit hit, radii[i], NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            if (playerTarget != null)
            {
                Vector3 nearPlayer = playerTarget.position + new Vector3(6f, 0f, 6f);
                for (int i = 0; i < radii.Length; i++)
                {
                    if (NavMesh.SamplePosition(nearPlayer, out NavMeshHit hit, radii[i], NavMesh.AllAreas))
                    {
                        return hit.position;
                    }
                }
            }

            return desired;
        }

        private ZombieSpawnPoint PickSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    totalWeight += spawnPoints[i].Weight;
                }
            }

            float random = Random.value * totalWeight;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                ZombieSpawnPoint spawnPoint = spawnPoints[i];
                if (spawnPoint == null)
                {
                    continue;
                }

                random -= spawnPoint.Weight;
                if (random <= 0f)
                {
                    return spawnPoint;
                }
            }

            return spawnPoints[0];
        }

        private void CleanupAliveList()
        {
            for (int i = aliveZombies.Count - 1; i >= 0; i--)
            {
                ZombieWar.Enemy.Enemy zombie = aliveZombies[i];
                if (zombie == null || !zombie.gameObject.activeInHierarchy || !zombie.IsAlive)
                {
                    unbindZombieDied(zombie);
                    aliveZombies.RemoveAt(i);
                }
            }
        }
    }
}
