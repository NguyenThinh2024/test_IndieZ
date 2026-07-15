using UnityEngine;
using UnityEngine.AI;

namespace ZombieWar.Enemy
{
    public sealed class ZombieMovement : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private ZombieHealth health;
        [SerializeField] private float navMeshSampleRadius = 4f;

        private Transform target;
        private IZombieStats stats;
        private bool usesCentralTick;
        private float nextDestinationTime;
        private float flankAngleRadians;

        public float SpeedNormalized { get; private set; }
        public float DistanceToTarget { get; private set; } = float.MaxValue;
        public float DestinationUpdateInterval => stats != null ? stats.DestinationUpdateInterval : 0.2f;

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += Stop;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= Stop;
            }
        }

        public void Initialize(IZombieStats zombieStats, Transform chaseTarget)
        {
            OnSpawn(zombieStats, chaseTarget);
        }

        public void OnSpawn(IZombieStats zombieStats, Transform chaseTarget)
        {
            stats = zombieStats;
            target = chaseTarget;
            SpeedNormalized = 0f;
            DistanceToTarget = float.MaxValue;
            nextDestinationTime = 0f;
            // Stable per-instance surround slot so elites fan out instead of sharing one path.
            flankAngleRadians = (Mathf.Abs(GetInstanceID()) % 360) * Mathf.Deg2Rad;

            if (agent == null)
            {
                return;
            }

            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.updateUpAxis = true;
            agent.speed = stats != null ? stats.MoveSpeed : agent.speed;
            // Snappier chase so fast zombies do not look like they are crawling into Run.
            agent.acceleration = Mathf.Max(agent.acceleration, agent.speed * 3.5f);
            agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360f);
            // Stop inside melee range so ZombieAttack can fire (was often overshooting / orbiting).
            if (stats != null)
            {
                agent.stoppingDistance = Mathf.Max(0.35f, stats.AttackRange * 0.65f);
                if (stats.FlankRadius > 0.01f)
                {
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                    agent.avoidancePriority = 20 + (Mathf.Abs(GetInstanceID()) % 40);
                }
            }

            // Never touch isStopped / Resume before the agent is on a NavMesh.
            if (!tryPlaceOnNavMesh())
            {
                return;
            }

            agent.isStopped = false;
            // Set destination immediately so agent starts walking before the next tick bucket.
            setDestinationToTarget(force: true);
        }

        public void SetCentralTick(bool enabled)
        {
            usesCentralTick = enabled;
        }

        private void Update()
        {
            if (usesCentralTick)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void OnDespawn()
        {
            usesCentralTick = false;
            Stop();
            stats = null;
            target = null;
            SpeedNormalized = 0f;
            DistanceToTarget = float.MaxValue;
            nextDestinationTime = 0f;
            flankAngleRadians = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (agent == null || target == null || stats == null || !agent.enabled)
            {
                SpeedNormalized = 0f;
                DistanceToTarget = float.MaxValue;
                return;
            }

            DistanceToTarget = Vector3.Distance(transform.position, target.position);

            if (!IsAgentReady())
            {
                SpeedNormalized = 0f;
                tryPlaceOnNavMesh();
                return;
            }

            if (Time.time >= nextDestinationTime)
            {
                setDestinationToTarget(force: false);
                nextDestinationTime = Time.time + Mathf.Max(0.05f, DestinationUpdateInterval);
            }

            SpeedNormalized = agent.velocity.sqrMagnitude > 0f
                ? agent.velocity.magnitude / Mathf.Max(0.01f, agent.speed)
                : 0f;
        }

        private void setDestinationToTarget(bool force)
        {
            if (target == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            Vector3 destination = resolveChaseDestination();
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                destination = hit.position;
            }

            if (!force && agent.hasPath && !agent.pathPending
                && (agent.destination - destination).sqrMagnitude <= 0.25f)
            {
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        private Vector3 resolveChaseDestination()
        {
            Vector3 targetPosition = target.position;
            float flankRadius = stats != null ? stats.FlankRadius : 0f;
            if (flankRadius <= 0.01f)
            {
                return targetPosition;
            }

            float attackRange = stats != null ? stats.AttackRange : 1.5f;
            float commitDistance = Mathf.Max(attackRange * 1.4f, flankRadius * 0.85f);
            if (DistanceToTarget <= commitDistance)
            {
                return targetPosition;
            }

            Vector3 offset = new Vector3(
                Mathf.Sin(flankAngleRadians) * flankRadius,
                0f,
                Mathf.Cos(flankAngleRadians) * flankRadius);
            return targetPosition + offset;
        }

        public void Stop()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        public void ResumeChase()
        {
            if (agent == null || !agent.enabled || target == null || stats == null)
            {
                return;
            }

            if (!IsAgentReady())
            {
                tryPlaceOnNavMesh();
                if (!IsAgentReady())
                {
                    return;
                }
            }

            agent.isStopped = false;
            setDestinationToTarget(force: true);
            nextDestinationTime = Time.time + Mathf.Max(0.05f, DestinationUpdateInterval);
        }

        private bool tryPlaceOnNavMesh()
        {
            if (agent == null)
            {
                return false;
            }

            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            if (agent.isOnNavMesh)
            {
                return true;
            }

            // Expand search — spawn points are often outside small sample radius before map bake.
            float[] radii =
            {
                Mathf.Max(1f, navMeshSampleRadius),
                12f,
                28f,
                50f,
            };

            for (int i = 0; i < radii.Length; i++)
            {
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, radii[i], NavMesh.AllAreas))
                {
                    continue;
                }

                agent.Warp(hit.position);
                transform.position = hit.position;
                return agent.isOnNavMesh;
            }

            // Last attempt: sample from high above so we drop onto floor from a floater spawn.
            Vector3 fromAbove = transform.position + Vector3.up * 25f;
            if (NavMesh.SamplePosition(fromAbove, out NavMeshHit aboveHit, 50f, NavMesh.AllAreas))
            {
                agent.Warp(aboveHit.position);
                transform.position = aboveHit.position;
                return agent.isOnNavMesh;
            }

            return false;
        }

        private bool IsAgentReady()
        {
            return agent != null && agent.enabled && agent.gameObject.activeInHierarchy && agent.isOnNavMesh;
        }
    }
}
