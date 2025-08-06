using UnityEngine;
using System.Collections;
using UnityEngine.AI; // Required to interact with the NavMeshAgent

/// <summary>
/// Manages all status effects on an enemy, focusing on the Chill/Freeze mechanic.
/// This component should be placed on any enemy prefab that can be affected by the Ice Shotgun.
/// </summary>
public class EnemyStatusController : MonoBehaviour
{
    [Header("Chill & Freeze Stats")]
    [Tooltip("The current amount of chill applied to the enemy.")]
    [Range(0, 100)]
    public float currentChill = 0f;

    [Tooltip("The chill value at which the enemy will become frozen.")]
    public float freezeThreshold = 100f;

    [Tooltip("How long the enemy remains frozen, in seconds.")]
    public float freezeDuration = 3f;

    [Tooltip("The amount of chill remaining after an enemy thaws from a frozen state.")]
    public float postFreezeChillAmount = 25f;

    [Header("Visuals")]
    [Tooltip("The original material of the enemy. Will be grabbed automatically if left null.")]
    public Material originalMaterial;
    [Tooltip("The material to apply when the enemy is frozen.")]
    public Material frozenMaterial;
    [Tooltip("The main renderer for the enemy's body.")]
    public Renderer characterRenderer;

    // --- Private State & Component References ---
    private bool isFrozen = false;
    private NavMeshAgent agent;
    private EnemyAI meleeAI; // Reference for the melee AI
    private RangedEnemyAI rangedAI; // Reference for the ranged AI
    private float originalSpeed;

    void Awake()
    {
        // Get references to the AI components. An enemy might have one or the other.
        agent = GetComponent<NavMeshAgent>();
        meleeAI = GetComponent<EnemyAI>();
        rangedAI = GetComponent<RangedEnemyAI>();

        if (agent != null)
        {
            originalSpeed = agent.speed;
        }

        // Automatically find the renderer and store its material if not assigned.
        if (characterRenderer == null)
        {
            characterRenderer = GetComponentInChildren<Renderer>();
        }
        if (characterRenderer != null)
        {
            originalMaterial = characterRenderer.material;
        }
        else
        {
            Debug.LogError("EnemyStatusController: No Renderer found on this object or its children!", this);
            enabled = false; // Disable the script if no renderer is found.
        }
    }

    /// <summary>
    /// Public method to be called by projectiles or other effects to apply chill.
    /// </summary>
    /// <param name="amount">The amount of chill to add.</param>
    public void ApplyChill(float amount)
    {
        if (isFrozen) return; // Can't apply chill to an already frozen target.

        currentChill = Mathf.Clamp(currentChill + amount, 0, freezeThreshold);
        ApplySlow();

        // Check if the chill has reached the threshold to freeze the enemy.
        if (currentChill >= freezeThreshold)
        {
            StartCoroutine(FreezeRoutine());
        }
    }

    /// <summary>
    /// Applies a movement speed slow based on the current chill percentage.
    /// </summary>
    private void ApplySlow()
    {
        if (agent == null || isFrozen) return;

        // Calculate the slowdown multiplier. 0 chill = 1x speed, 100 chill = 0x speed.
        float slowMultiplier = 1f - (currentChill / freezeThreshold);
        agent.speed = originalSpeed * slowMultiplier;
    }

    /// <summary>
    /// The main coroutine for handling the entire freeze-thaw cycle.
    /// </summary>
    private IEnumerator FreezeRoutine()
    {
        isFrozen = true;
        Debug.Log($"{gameObject.name} is FROZEN!");

        // --- Apply Freeze Effects ---
        if (agent != null)
        {
            agent.speed = 0; // Completely stop movement.
            agent.isStopped = true;
        }
        // Disable the AI scripts to stop them from trying to attack or move.
        if (meleeAI != null) meleeAI.enabled = false;
        if (rangedAI != null) rangedAI.enabled = false;


        // Change to the frozen material.
        if (characterRenderer != null && frozenMaterial != null)
        {
            characterRenderer.material = frozenMaterial;
        }

        // --- Wait for Duration ---
        yield return new WaitForSeconds(freezeDuration);

        // --- Unfreeze and Reset ---
        Debug.Log($"{gameObject.name} has thawed.");
        isFrozen = false;
        currentChill = postFreezeChillAmount; // Set chill to the baseline value.

        // Re-enable AI scripts.
        if (meleeAI != null) meleeAI.enabled = true;
        if (rangedAI != null) rangedAI.enabled = true;

        // Restore the original material.
        if (characterRenderer != null && originalMaterial != null)
        {
            characterRenderer.material = originalMaterial;
        }
        
        if (agent != null)
        {
            agent.isStopped = false;
        }

        // Re-apply the baseline slow effect now that the enemy has thawed.
        ApplySlow();
    }
}
