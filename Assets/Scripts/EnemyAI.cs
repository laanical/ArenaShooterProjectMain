using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// Manages enemy AI behavior with a single, consistent charge-and-stab attack pattern.
// VERSION 15 (Restored): Charge now locks onto the player's initial position for dodgeable attacks.
public class EnemyAI : MonoBehaviour
{
    // --- Component & Object References ---
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsPlayer;
    private Animator animator;

    // --- General Attack Settings ---
    [Header("General Attack")]
    public int attackDamage = 15;
    public float attackCooldown = 2f;
    public Transform attackPoint;
    [Tooltip("A brief pause after an attack to kill momentum before the cooldown starts.")]
    public float postAttackPause = 0.2f;
    private bool canPerformAction = true;

    // --- Charge-Stab Attack ---
    [Header("Charge-Stab Attack")]
    [Tooltip("The range at which the enemy will begin its charge.")]
    public float chargeEngageRange = 30f;
    [Tooltip("The movement speed during the charge.")]
    public float chargeSpeed = 25f;
    [Tooltip("The acceleration used when charging the player.")]
    public float chargeAcceleration = 50f;
    [Tooltip("The maximum duration of a single charge attempt.")]
    public float maxChargeTime = 2.0f;
    [Tooltip("The actual range of the stab at the end of the charge.")]
    public float stabRange = 3f;

    // --- Detection ---
    [Header("Detection")]
    public float sightRange = 40f;

    // --- Animator Hashes ---
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");
    private readonly int speedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        else Debug.LogError("EnemyAI: Player not found!", this);
    }

    private void Update()
    {
        if (agent == null || player == null || !canPerformAction) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= chargeEngageRange)
        {
            StartCoroutine(ChargeAndStabSequence());
        }
        else if (distanceToPlayer <= sightRange)
        {
            ChasePlayer();
        }
        else
        {
            agent.isStopped = true;
            UpdateAnimation(0);
        }
    }

    private void ChasePlayer()
    {
        agent.acceleration = 8; 
        agent.isStopped = false;
        agent.SetDestination(player.position);
        UpdateAnimation(agent.velocity.magnitude);
    }

    private IEnumerator ChargeAndStabSequence()
    {
        canPerformAction = false;

        // Lock the target position at the start of the charge.
        Vector3 chargeTargetPosition = player.position;

        // --- Charge Setup ---
        float originalSpeed = agent.speed;
        float originalAcceleration = agent.acceleration;
        agent.speed = chargeSpeed;
        agent.acceleration = chargeAcceleration;
        agent.isStopped = false;

        // Set the destination only ONCE to the locked position.
        agent.SetDestination(chargeTargetPosition);

        // --- Charge Phase ---
        float chargeStartTime = Time.time;
        while (Time.time < chargeStartTime + maxChargeTime)
        {
            // Break the loop if we have reached our destination.
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                break;
            }
            
            // Visually track the player while charging to the locked spot.
            FacePlayer();
            UpdateAnimation(agent.velocity.magnitude);
            yield return null;
        }

        // --- Stab Phase & Cleanup ---
        agent.speed = originalSpeed;
        agent.acceleration = originalAcceleration;
        
        // Hard stop to kill all momentum.
        agent.velocity = Vector3.zero;
        agent.isStopped = true;
        agent.ResetPath();
        UpdateAnimation(0);

        // ALWAYS perform the stab at the end of the charge sequence.
        FacePlayer();
        PerformStab();

        // --- Cooldown & Pause ---
        yield return new WaitForSeconds(postAttackPause);
        yield return new WaitForSeconds(attackCooldown);
        canPerformAction = true;
    }

    private void PerformStab()
    {
        if (animator != null)
        {
            animator.SetTrigger(attackTriggerHash);
        }
        StartCoroutine(DealDamageAfterDelay(0.25f));
    }

    private IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Collider[] hitColliders = Physics.OverlapSphere(attackPoint.position, stabRange, whatIsPlayer);
        if (hitColliders.Length > 0)
        {
            if (hitColliders[0].TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth) || hitColliders[0].GetComponentInParent<PlayerHealth>() != null)
            {
                if (playerHealth == null) playerHealth = hitColliders[0].GetComponentInParent<PlayerHealth>();
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        }
    }

    private void UpdateAnimation(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedHash, speed, 0.1f, Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chargeEngageRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, stabRange);
        }
    }
}