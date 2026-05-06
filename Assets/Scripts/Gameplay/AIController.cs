using UnityEngine;
using UnityEngine.AI;
using ChaosTower.Managers;

namespace ChaosTower.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIController : MonoBehaviour
    {
        [Header("Settings")]
        public float PatrolRadius = 10f;
        public float InteractionInterval = 25f;
        public float NudgeForce = 1f;
        public AudioClip EngineClip;

        private NavMeshAgent agent;
        private AudioSource engineSource;
        private float nextInteractionTime;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            
            // Setup 3D AudioSource
            engineSource = gameObject.AddComponent<AudioSource>();
            engineSource.playOnAwake = false;
            engineSource.loop = true;
            engineSource.spatialBlend = 1.0f; // Full 3D
            engineSource.minDistance = 3f;
            engineSource.maxDistance = 50f;
            engineSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void Start()
        {
            nextInteractionTime = Time.time + InteractionInterval;
            
            // Force the agent to snap to the NavMesh on start
            if (agent != null)
            {
                NavMeshHit hit;
                // Increased radius to 5.0f to be more forgiving
                if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.Log($"AI Agent: Warp successful to {hit.position}");
                }
                else
                {
                    Debug.LogError($"AI Agent: Failed to find NavMesh at {transform.position}! Is it baked? Check 'Navigation' window.");
                }
            }
            
            if (EngineClip != null)
            {
                engineSource.clip = EngineClip;
                engineSource.Play();
                Debug.Log($"AI Agent: Engine sound STARTED. Clip: {EngineClip.name}");
            }
            else
            {
                Debug.LogWarning("AI Agent: EngineClip is NULL!");
            }
            
            SetNewDestination();
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            if (GameManager.Instance.CurrentState != GameState.Playing)
            {
                if (agent.isActiveAndEnabled) agent.isStopped = true;
                if (engineSource != null) engineSource.mute = true;
                return;
            }

            agent.isStopped = false;
            if (engineSource != null) engineSource.mute = false;

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                SetNewDestination();
            }

            if (Time.time > nextInteractionTime)
            {
                TryNudgeTower();
                nextInteractionTime = Time.time + InteractionInterval;
            }
        }

        private void SetNewDestination()
        {
            // Restrict movement to 4 cardinal directions (no diagonals)
            Vector3[] directions = new Vector3[] {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right
            };
            
            Vector3 chosenDir = directions[Random.Range(0, directions.Length)];
            float distance = Random.Range(3f, PatrolRadius);
            
            Vector3 targetPos = transform.position + (chosenDir * distance);
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        private void TryNudgeTower()
        {
            // Find anything with a Rigidbody within range (e.g. the base or lowest blocks)
            Collider[] colliders = Physics.OverlapSphere(transform.position, 3f);
            foreach (var col in colliders)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 forceDirection = (col.transform.position - transform.position).normalized;
                    forceDirection.y = 0; // Only horizontal nudge
                    rb.AddForce(forceDirection * NudgeForce, ForceMode.Impulse);
                    Debug.Log($"AI Agent: Nudged {col.gameObject.name}!");
                    break;
                }
            }
        }
    }
}
