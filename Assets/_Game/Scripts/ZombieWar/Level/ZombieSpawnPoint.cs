using UnityEngine;

namespace ZombieWar.Level
{
    public sealed class ZombieSpawnPoint : MonoBehaviour
    {
        [SerializeField] private float weight = 1f;

        public float Weight => Mathf.Max(0.01f, weight);
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
    }
}
