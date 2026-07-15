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

            // Drop Addressable instance + any baked leftover siblings (e.g. scene-placed level1).
            releaseMap();
            destroyForeignMaps(keepAlive: null);

            GameObject map = await mapLoader.LoadAsync(entry.MapAddress, mapParent, token);
            if (map == null)
            {
                return null;
            }

            destroyForeignMaps(keepAlive: map);

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            bakeNavMeshIfNeeded(map);
            placeActorsOnMap(map);

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

        private void bakeNavMeshIfNeeded(GameObject map)
        {
            if (!bakeNavMeshOnMapReady)
            {
                return;
            }

            // Prefer baked NavMesh carried by the level prefab.
            if (map != null)
            {
                NavMeshSurface mapSurface = map.GetComponentInChildren<NavMeshSurface>(true);
                if (mapSurface != null && mapSurface.navMeshData != null)
                {
                    // Scene surface from a previous map must not stay merged into NavMesh.
                    if (navMeshSurface != null && navMeshSurface != mapSurface)
                    {
                        navMeshSurface.RemoveData();
                    }

                    if (!mapSurface.enabled)
                    {
                        mapSurface.enabled = true;
                    }

                    return;
                }
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

        private void placeActorsOnMap(GameObject map)
        {
            if (waveManager == null)
            {
                return;
            }

            waveManager.PlaceActorsOnMap(map);
        }

        /// <summary>
        /// Removes baked / leftover map children under mapParent that are not the active Addressable instance.
        /// </summary>
        private void destroyForeignMaps(GameObject keepAlive)
        {
            Transform parent = mapParent != null ? mapParent : transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (keepAlive != null && child.gameObject == keepAlive)
                {
                    continue;
                }

                NavMeshSurface leftoverSurface = child.GetComponentInChildren<NavMeshSurface>(true);
                if (leftoverSurface != null)
                {
                    leftoverSurface.RemoveData();
                }

                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
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
            // PlayerPrefs is the easy clear target for "play from start".
            if (PlayerPrefs.HasKey(SessionLevelKey))
            {
                return Mathf.Max(1, PlayerPrefs.GetInt(SessionLevelKey, fallback));
            }

            try
            {
                Nexzap.Base.Data.UserProfileController profile =
                    Nexzap.Base.Data.UserProfileController.Instance;
                if (profile != null)
                {
                    return Mathf.Max(1, profile.LEVEL);
                }
            }
            catch (Exception)
            {
            }

            return Mathf.Max(1, fallback);
        }

        private const string SessionLevelKey = "ZW_SessionLevel";

        public static void PersistSessionLevel(int level)
        {
            int clamped = Mathf.Max(1, level);
            PlayerPrefs.SetInt(SessionLevelKey, clamped);
            // Keep profile key in sync so Menu PLAY and Clear All PlayerPrefs stay consistent.
            PlayerPrefs.SetInt("currentLevel", clamped);
            PlayerPrefs.Save();
        }
    }
}
