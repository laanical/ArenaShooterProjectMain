using UnityEngine;
using UnityEngine.AI;

// Manages enemy AI behavior, including chasing the player and performing a melee attack with animation.
public class EnemyAI : MonoBehaviour
{
    // --- Component & Object References ---
    public NavMeshAgent agent; 
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;
    private Animator animator;

    // --- Melee Attack Logic ---
    public int attackDamage = 10;
    public float attackRange = 2f;
    public Transform attackPoint;

    // --- Attack Cooldown ---
    public float timeBetweenAttacks = 1.5f; 
    private bool alreadyAttacked;

    // --- AI State Ranges ---
    public float sightRange = 20f; 
    
    // --- Animator Parameter Hashes ---
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");
    private readonly int speedHash = Animator.StringToHash("Speed"); 

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); 
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("EnemyAI: Could not find GameObject with tag 'Player'.", this);
        }

        if (agent == null) Debug.LogError("EnemyAI: NavMeshAgent component not found.", this);
        if (animator == null) Debug.LogWarning("EnemyAI: Animator component not found. Enemy will not be animated.", this);
    }

    private void Start()
    {
        if(agent != null)
        {
            // Set the agent to stop just before it reaches the player to perform a melee attack.
            agent.stoppingDistance = attackRange;

            // --- NEW: Increase agent turn and move speed for snappy movement ---
            // Set a very high angular speed to make the agent turn instantly.
            agent.angularSpeed = 5000f;
            // Set a very high acceleration to make the agent reach full speed almost instantly.
            agent.acceleration = 5000f;
        }
    }

    private void Update()
    {
        if (agent == null || player == null) return;

        // Check if the player is within our sight range.
        bool playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);

        // --- STATE LOGIC ---
        if (playerInSightRange)
        {
            // If player is in sight, always try to path towards them.
            agent.SetDestination(player.position);
            
            // Check if the agent has reached its destination (or can't get closer).
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // If we've arrived, switch to attack logic.
                AttackPlayer();
            }
            else
            {
                // If we are still moving towards the player, we are in the "chase" state.
                ChasePlayer();
            }
        }
        else
        {
            // If player is out of sight, do nothing.
            if (!agent.isStopped) agent.isStopped = true;
            UpdateAnimation(0); // Set speed to 0
        }
    }

    // This method is now only for things that happen while chasing (like animation).
    private void ChasePlayer()
    {
        agent.isStopped = false;
        UpdateAnimation(agent.velocity.magnitude); // Pass agent's speed to animator
    }

    private void AttackPlayer()
    {
        agent.isStopped = true;
        UpdateAnimation(0); // Stop walking animation

        // Make the enemy face the player when attacking.
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        
        if (!alreadyAttacked)
        {
            // Trigger the animation.
            if (animator != null)
            {
                animator.SetTrigger(attackTriggerHash);
            }
            
            // Deal damage.
            Collider[] hitColliders = Physics.OverlapSphere(attackPoint.position, attackRange, whatIsPlayer);
            foreach (var hitCollider in hitColliders)
            {
                PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                if(playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"Hit {hitCollider.name} for {attackDamage} damage.");
                    break; 
                }
            }

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }
    
    // Central method to update the animator's speed.
    private void UpdateAnimation(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedHash, speed);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
