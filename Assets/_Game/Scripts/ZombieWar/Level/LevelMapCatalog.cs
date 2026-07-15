using UnityEngine;

namespace ZombieWar.Level
{
    /// <summary>
    /// Authored list of level map Addressable addresses.
    /// Config only — no runtime instance state.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelMapCatalog", menuName = "Zombie War/Level/Map Catalog")]
    public sealed class LevelMapCatalog : ScriptableObject
    {
        [SerializeField] private LevelMapEntry[] entries;

        public LevelMapEntry[] Entries => entries;

        public bool TryGetEntry(int levelNumber, out LevelMapEntry entry)
        {
            entry = null;
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                LevelMapEntry candidate = entries[i];
                if (candidate != null && candidate.LevelNumber == levelNumber && candidate.HasAddress)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetNextEntry(int levelNumber, out LevelMapEntry entry)
        {
            entry = null;
            if (entries == null || entries.Length == 0)
            {
                return false;
            }

            LevelMapEntry best = null;
            for (int i = 0; i < entries.Length; i++)
            {
                LevelMapEntry candidate = entries[i];
                if (candidate == null || !candidate.HasAddress)
                {
                    continue;
                }

                if (candidate.LevelNumber <= levelNumber)
                {
                    continue;
                }

                if (best == null || candidate.LevelNumber < best.LevelNumber)
                {
                    best = candidate;
                }
            }

            entry = best;
            return best != null;
        }
    }
}
