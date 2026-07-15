using UnityEngine;

namespace ZombieWar.Level
{
    [CreateAssetMenu(fileName = "LevelWaveConfig", menuName = "Zombie War/Level/Wave Config")]
    public sealed class LevelWaveConfig : ScriptableObject
    {
        [SerializeField] private float durationSeconds = 180f;
        [SerializeField] private WaveData[] waves;

        public float DurationSeconds => durationSeconds;
        public WaveData[] Waves => waves;
    }
}
