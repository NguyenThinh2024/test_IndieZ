using System;
using UnityEngine;

namespace ZombieWar.Enemy
{
    [Serializable]
    public sealed class ZombieEnemyAudioConfig
    {
        [SerializeField] private string chaseClipAddress;

        [SerializeField] private string hitClipAddress;
        [SerializeField] private string deathClipAddress;

        [SerializeField] private float chaseIntervalMin = 2.8f;
        [SerializeField] private float chaseIntervalMax = 4.5f;

        public string ChaseClipAddress => chaseClipAddress;
        public string HitClipAddress => hitClipAddress;
        public string DeathClipAddress => deathClipAddress;
        public float ChaseIntervalMin => Mathf.Max(0.25f, chaseIntervalMin);
        public float ChaseIntervalMax => Mathf.Max(ChaseIntervalMin, chaseIntervalMax);

        // Runtime caches filled after Addressable load.
        public AudioClip ChaseClip { get; set; }
        public AudioClip HitClip { get; set; }
        public AudioClip DeathClip { get; set; }
    }
}
