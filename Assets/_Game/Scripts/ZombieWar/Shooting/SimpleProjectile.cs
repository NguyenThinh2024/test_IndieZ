using UnityEngine;

namespace ZombieWar.Shooting
{
    public sealed class SimpleProjectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private Collider projectileCollider;
        [SerializeField] private float lifeTime = 2f;

        private float disableTime;

        public bool IsAvailable => !gameObject.activeSelf;

        public void Fire(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
            disableTime = Time.time + Mathf.Max(0.05f, lifeTime);

            if (projectileCollider != null)
            {
                projectileCollider.enabled = true;
            }

            if (body != null)
            {
                body.linearVelocity = velocity;
                body.angularVelocity = Vector3.zero;
            }
        }

        public void Release()
        {
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (projectileCollider != null)
            {
                projectileCollider.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Time.time >= disableTime)
            {
                Release();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Release();
        }
    }
}
