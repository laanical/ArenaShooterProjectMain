using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages an icy patch on the ground that slows and chills enemies.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IcyPatchController : MonoBehaviour
{
    [Header("Patch Settings")]
    [Tooltip("How long the patch lasts before starting to fade.")]
    public float duration = 5f;
    [Tooltip("How long it takes for the patch to fade out completely.")]
    public float fadeOutDuration = 2f;
    [Tooltip("The amount of chill applied per second to enemies in the patch.")]
    public float chillPerSecond = 20f;

    [Header("Visuals")]
    [Tooltip("The material of the patch, used for the fade effect. Must be a transparent/fade material.")]
    public Material patchMaterial;
    private Color startColor;
    
    [Header("Placement")]
    [Tooltip("How far to offset the patch from the surface to prevent Z-fighting.")]
    public float surfaceOffset = 0.01f;

    // A dictionary to track the last time we applied chill to each enemy,
    // to ensure we only apply it once per second per enemy.
    private Dictionary<EnemyStatusController, float> chilledEnemies = new Dictionary<EnemyStatusController, float>();
    private const float CHILL_INTERVAL = 1.0f; // Apply chill every 1 second.

    void Awake()
    {
        // Ensure the collider is a trigger so enemies can pass through it.
        GetComponent<Collider>().isTrigger = true;

        // Clone the material so our fading doesn't affect other icy patches.
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            patchMaterial = rend.material;
            startColor = patchMaterial.color;
        }
        else
        {
            Debug.LogError("IcyPatchController requires a Renderer component for fade effect!", this);
            enabled = false;
        }
    }

    void Start()
    {
        // --- [THE FIX] ---
        // Nudge the patch slightly upwards along its local up-axis (which is aligned with the surface normal)
        // using the new editable variable to prevent Z-fighting.
        transform.position += transform.up * surfaceOffset;

        // Start the lifetime coroutine.
        StartCoroutine(PatchLifecycle());
    }

    /// <summary>
    /// This is called continuously for every collider that stays inside the trigger.
    /// </summary>
    void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<EnemyStatusController>(out EnemyStatusController enemyStatus))
        {
            // Check if we have already chilled this enemy recently.
            if (!chilledEnemies.ContainsKey(enemyStatus) || Time.time - chilledEnemies[enemyStatus] >= CHILL_INTERVAL)
            {
                // Apply chill and update the last chilled time.
                enemyStatus.ApplyChill(chillPerSecond * CHILL_INTERVAL);
                chilledEnemies[enemyStatus] = Time.time;
            }
        }
    }

    /// <summary>
    /// Manages the patch's lifetime, from active to faded out.
    /// </summary>
    private IEnumerator PatchLifecycle()
    {
        // Wait for the main duration of the patch.
        yield return new WaitForSeconds(duration);

        // Now, start the fade out process.
        float timer = 0f;
        while (timer < fadeOutDuration)
        {
            // Calculate the new alpha value based on the fade progress.
            float alpha = Mathf.Lerp(startColor.a, 0f, timer / fadeOutDuration);
            patchMaterial.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame.
        }

        // Once faded, destroy the patch GameObject.
        Destroy(gameObject);
    }
}
