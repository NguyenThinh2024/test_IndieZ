using TBN;
using UnityEngine;

namespace ZombieWar.Bomb
{
    public sealed class BombThrower : MonoBehaviour
    {
        [SerializeField] private BombController bombPrefab;
        [SerializeField] private BombData bombData;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float throwForce = 8f;
        [SerializeField] private float upwardForce = 3f;
        [SerializeField] private float cooldown = 5f;

        private float nextThrowTime;

        public bool TryThrow()
        {
            if (bombPrefab == null || throwPoint == null || Time.time < nextThrowTime)
            {
                return false;
            }

            nextThrowTime = Time.time + cooldown;
            BombController bomb = bombPrefab.Spawn(throwPoint.position, throwPoint.rotation);
            if (bomb == null)
            {
                return false;
            }

            bomb.Initialize(bombData, gameObject);
            Rigidbody body = bomb.Body;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                Vector3 force = throwPoint.forward * throwForce + Vector3.up * upwardForce;
                body.AddForce(force, ForceMode.VelocityChange);
            }

            return true;
        }
    }
}
