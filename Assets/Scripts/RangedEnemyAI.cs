using UnityEngine;
using UnityEngine.AI;

public class RangedEnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject projectilePrefab;
    public Transform firePoint;
    [Tooltip("Set this to the layers you want to be considered obstacles (e.g., 'Default', 'Walls'). The player should NOT be on this layer.")]
    public LayerMask obstacleLayers; // RENAMED for clarity
    private Animator animator;

    [Header("AI Behavior")]
    [Tooltip("The maximum distance the enemy can see the player.")]
    public float sightRange = 35f;
    [Tooltip("The maximum distance the enemy can shoot from.")]
    public float attackRange = 30f;
    [Tooltip("The ideal distance the enemy wants to keep from the player.")]
    public float optimalDistance = 20f;
    [Tooltip("If the player gets closer than this, the enemy will back away.")]
    public float retreatDistance = 10f;

    [Header("Attacking")]
    public float timeBetweenAttacks = 2f;
    [Tooltip("How much random offset to add to each shot. 0 = perfect aim.")]
    public float aimInaccuracy = 0.5f;

    // --- Private Fields ---
    private NavMeshAgent agent;
    private Collider playerCollider;
    private float timeUntilNextAttack;
    private readonly int speedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerCollider = playerObject.GetComponent<Collider>();
        }
        else
        {
            // --- IMPROVEMENT 1: Better 'Player Not Found' Handling ---
            Debug.LogError("RangedEnemyAI: Could not find GameObject with tag 'Player'. Disabling AI.", this);
            enabled = false; // Disable this script if the player doesn't exist.
            return;
        }

        // Safety checks
        if (agent == null) Debug.LogError("RangedEnemyAI: NavMeshAgent component not found.", this);
        if (animator == null) Debug.LogWarning("RangedEnemyAI: Animator component not found. Enemy will not be animated.", this);
    }

    void Update()
    {
        // This check is still good practice, even with the Awake change.
        if (player == null || agent == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > sightRange)
        {
            agent.isStopped = true;
            UpdateAnimation(0);
        }
        else
        {
            FacePlayer();

            if (distanceToPlayer < retreatDistance)
            {
                Retreat();
            }
            else if (distanceToPlayer > optimalDistance && distanceToPlayer < attackRange)
            {
                Chase();
            }
            else if (distanceToPlayer <= attackRange) // In shooting range, but not too close or too far
            {
                agent.isStopped = true;
                if (Time.time >= timeUntilNextAttack && HasLineOfSight())
                {
                    Attack();
                }
            }
            else // Outside attack range, but inside sight range
            {
                 Chase();
            }
            
            UpdateAnimation(agent.velocity.magnitude);
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private void Retreat()
    {
        agent.isStopped = false;
        Vector3 directionFromPlayer = (transform.position - player.position).normalized;
        Vector3 retreatDestination = transform.position + directionFromPlayer * 5f;
        agent.SetDestination(retreatDestination);
    }

    private void Chase()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void Attack()
    {
        timeUntilNextAttack = Time.time + timeBetweenAttacks;

        if (projectilePrefab == null || firePoint == null || playerCollider == null) return;

        Vector3 targetPosition = playerCollider.bounds.center;
        Vector3 randomOffset = Random.insideUnitSphere * aimInaccuracy;
        Vector3 directionToTarget = (targetPosition + randomOffset - firePoint.position).normalized;
        
        Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(directionToTarget));
    }

    private bool HasLineOfSight()
    {
        // --- IMPROVEMENT 2: More Robust Line of Sight ---
        if (playerCollider == null) return false;
        
        Vector3 targetPosition = playerCollider.bounds.center;
        Vector3 direction = (targetPosition - firePoint.position).normalized;
        float distance = Vector3.Distance(firePoint.position, targetPosition);

        // If the raycast hits an object on an obstacle layer, our view is blocked.
        if (Physics.Raycast(firePoint.position, direction, distance, obstacleLayers))
        {
            Debug.DrawRay(firePoint.position, direction * distance, Color.red);
            return false; // View is blocked
        }

        // Otherwise, we have a clear shot.
        Debug.DrawRay(firePoint.position, direction * distance, Color.green);
        return true; // View is clear
    }

    private void UpdateAnimation(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedHash, speed);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, optimalDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
}
