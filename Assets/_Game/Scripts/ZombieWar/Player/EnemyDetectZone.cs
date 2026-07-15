using UnityEngine;

namespace ZombieWar.Player
{
    // Trigger zone for EnemyTargetScanner. Assign Scanner + SphereCollider in the Inspector.
    public sealed class EnemyDetectZone : MonoBehaviour
    {
        [SerializeField] private EnemyTargetScanner scanner;
        [SerializeField] private SphereCollider detectCollider;

        [SerializeField]
        [Min(0.1f)]
        private float radius = 12f;

        public float Radius => radius;

        public float WorldRadius
        {
            get
            {
                float scale = Mathf.Max(
                    transform.lossyScale.x,
                    Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
                return radius * Mathf.Max(0.01f, scale);
            }
        }

        private void Awake()
        {
            applySetup();
        }

        private void OnTriggerEnter(Collider other)
        {
            scanner?.NotifyEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            scanner?.NotifyExit(other);
        }

        public void BindScanner(EnemyTargetScanner owner)
        {
            scanner = owner;
            // Scanner Awake may run after this zone; re-apply so triggers stay enabled.
            applySetup();
        }

        private void applySetup()
        {
            if (scanner == null || detectCollider == null)
            {
                enabled = false;
                return;
            }

            enabled = true;
            ensureTriggerRigidbody();
            applyRadiusToCollider();
        }

        private void ensureTriggerRigidbody()
        {
            // Trigger callbacks need a Rigidbody on at least one participant.
            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        private void applyRadiusToCollider()
        {
            detectCollider.isTrigger = true;
            detectCollider.center = Vector3.zero;
            detectCollider.radius = radius;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            if (detectCollider != null && scanner != null)
            {
                applySetup();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.15f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, WorldRadius);
            Gizmos.color = new Color(1f, 0.35f, 0.15f, 0.08f);
            Gizmos.DrawSphere(transform.position, WorldRadius);
        }
#endif
    }
}
