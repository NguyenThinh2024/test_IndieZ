using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;

namespace ZombieWar.Level
{
    /// <summary>
    /// Composition root for map load/release.
    /// Loads one Addressable map at a time; rebakes NavMesh after map ready
    /// so zombie agents match the loaded level geometry.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class LevelMapBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelMapCatalog catalog;
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private bool useProfileLevel = true;
        [SerializeField] private Transform mapParent;
        [SerializeField] private bool loadOnEnable = true;
        [SerializeField] private bool preloadNextLevel = true;

        [SerializeField] private WaveManager waveManager;

        [SerializeField] private NavMeshSurface navMeshSurface;
        [SerializeField] private bool bakeNavMeshOnMapReady = true;

        private LevelMapLoader mapLoader;
        private CancellationTokenSource loadCts;
        private bool isMapReady;

        public event Action<GameObject> MapReady;
        public event Action<LevelMapEntry> LevelPrepared;
        public event Action MapReleased;

        public bool IsMapReady => isMapReady;
        public GameObject CurrentMap => mapLoader != null ? mapLoader.MapInstance : null;
        public int LevelNumber => levelNumber;
        public LevelMapEntry CurrentEntry { get; private set; }

        public bool HasNextLevel =>
            catalog != null && catalog.TryGetNextEntry(levelNumber, out _);

        private void Awake()
        {
            mapLoader = new LevelMapLoader(this);
            if (mapParent == null)
            {
                mapParent = transform;
            }

            if (useProfileLevel)
            {
                levelNumber = resolveProfileLevel(levelNumber);
            }
        }

        private void OnEnable()
        {
            if (loadOnEnable)
            {
                if (useProfileLevel)
                {
                    levelNumber = resolveProfileLevel(levelNumber);
                }

                LoadLevelAsync(levelNumber).Forget();
            }
        }

        private void OnDisable()
        {
            cancelLoad();
            releaseMap();
        }

        public void LoadConfiguredLevel()
        {
            LoadLevelAsync(levelNumber).Forget();
        }

        public async UniTask<GameObject> LoadLevelAsync(int targetLevelNumber)
        {
            cancelLoad();
            loadCts = new CancellationTokenSource();
            CancellationToken token = loadCts.Token;

            isMapReady = false;
            levelNumber = targetLevelNumber;

            if (catalog == null || !catalog.TryGetEntry(targetLevelNumber, out LevelMapEntry entry))
            {
                return null;
            }

            CurrentEntry = entry;

            if (entry.HasWaveConfig && waveManager != null)
            {
                waveManager.SetLevelConfig(entry.WaveConfig);
            }

            GameObject map = await mapLoader.LoadAsync(entry.MapAddress, mapParent, token);
            if (map == null)
            {
                return null;
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            bakeNavMeshIfNeeded();
            snapWaveSpawnPointsToNavMesh();

            isMapReady = true;
            LevelPrepared?.Invoke(entry);
            MapReady?.Invoke(map);

            if (preloadNextLevel && catalog.TryGetNextEntry(targetLevelNumber, out LevelMapEntry next))
            {
                mapLoader.PreloadAsync(next.MapAddress, token).Forget();
            }

            return map;
        }

        public void ReleaseMap()
        {
            cancelLoad();
            releaseMap();
        }

        private void bakeNavMeshIfNeeded()
        {
            if (!bakeNavMeshOnMapReady)
            {
                return;
            }

            if (navMeshSurface == null)
            {
                Debug.LogError(
                    "[Zombie War] LevelMapBootstrap.navMeshSurface is not assigned. " +
                    "Create ZW_Navigation in the scene (menu: Zombie War/Setup And Bake NavMesh), " +
                    "then drag NavMeshSurface into LevelMapBootstrap. Do not Find/Add at runtime.",
                    this);
                return;
            }

            navMeshSurface.RemoveData();
            navMeshSurface.BuildNavMesh();
        }

        private void snapWaveSpawnPointsToNavMesh()
        {
            if (waveManager == null)
            {
                return;
            }

            waveManager.SnapSpawnPointsToNavMesh();
        }

        private void releaseMap()
        {
            if (mapLoader == null)
            {
                return;
            }

            bool hadMap = mapLoader.HasMap;
            mapLoader.Release();
            isMapReady = false;
            if (hadMap)
            {
                MapReleased?.Invoke();
            }
        }

        private void cancelLoad()
        {
            if (loadCts == null)
            {
                return;
            }

            loadCts.Cancel();
            loadCts.Dispose();
            loadCts = null;
        }

        private static int resolveProfileLevel(int fallback)
        {
            try
            {
                Nexzap.Base.Data.UserProfileController profile =
                    Nexzap.Base.Data.UserProfileController.Instance;
                if (profile != null)
                {
                    int level = Mathf.Max(1, profile.LEVEL);
                    PlayerPrefs.SetInt(SessionLevelKey, level);
                    return level;
                }
            }
            catch (Exception)
            {
            }

            if (PlayerPrefs.HasKey(SessionLevelKey))
            {
                return Mathf.Max(1, PlayerPrefs.GetInt(SessionLevelKey, fallback));
            }

            return Mathf.Max(1, fallback);
        }

        private const string SessionLevelKey = "ZW_SessionLevel";

        public static void PersistSessionLevel(int level)
        {
            PlayerPrefs.SetInt(SessionLevelKey, Mathf.Max(1, level));
            PlayerPrefs.Save();
        }
    }
}
