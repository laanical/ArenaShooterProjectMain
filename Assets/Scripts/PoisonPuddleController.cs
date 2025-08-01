
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PoisonPuddleController : MonoBehaviour
{
    [Header("Puddle Settings")]
    public float duration = 8f;
    public float initialRadius = 0.5f;
    public float maxRadius = 5f;
    public float growthDuration = 0.5f;

    [Header("Damage & Healing")]
    public int damagePerTick = 5;
    public int healPerTick = 3;
    public float tickInterval = 1.0f;
    
    [Header("Visuals")]
    [Range(0f, 1f)]
    public float activationPercentage = 0.75f;
    public float fadeOutDuration = 1.5f;

    [Header("Placement")]
    public LayerMask groundLayer;
    [Tooltip("How far above the surface to spawn before settling. Prevents spawning inside walls.")]
    public float spawnHeightOffset = 0.5f;

    [Header("System")]
    public LayerMask enemyLayer;
    public LayerMask playerLayer;

    private float lifetimeTimer = 0f;
    private float tickTimer = 0f;
    private SphereCollider damageArea;
    private Renderer[] puddleRenderers;
    private bool isSettled = false;

    void Awake()
    {
        transform.localScale = Vector3.zero;
        List<Renderer> allRenderers = GetComponentsInChildren<Renderer>(true).ToList();
        if (allRenderers.Count == 0) { Destroy(gameObject); return; }
        
        for (int i = 0; i < allRenderers.Count; i++) {
            Renderer temp = allRenderers[i];
            int randomIndex = Random.Range(i, allRenderers.Count);
            allRenderers[i] = allRenderers[randomIndex];
            allRenderers[randomIndex] = temp;
        }

        int countToActivate = Mathf.RoundToInt(allRenderers.Count * activationPercentage);
        for (int i = 0; i < allRenderers.Count; i++)
        {
            allRenderers[i].gameObject.SetActive(i < countToActivate);
        }
        
        puddleRenderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in puddleRenderers)
        {
            rend.material = new Material(rend.material);
        }

        damageArea = gameObject.AddComponent<SphereCollider>();
        damageArea.isTrigger = true;
        damageArea.radius = 0;
        
        this.enabled = false;
    }

    // The projectile now passes the surface normal so we know which way to cast.
    public void Initialize(Vector3 impactNormal)
    {
        PlaceAndSettle(impactNormal);
    }

    private void PlaceAndSettle(Vector3 normal)
    {
        // --- [NEW] Reliable Raycast Placement ---
        // 1. Define a safe starting position for our search, offset from the impact point along the surface normal.
        Vector3 searchStartPosition = transform.position + normal * spawnHeightOffset;
        
        // 2. Raycast back towards the surface to find the precise, non-clipped landing spot.
        if (Physics.Raycast(searchStartPosition, -normal, out RaycastHit hit, spawnHeightOffset * 2, groundLayer))
        {
            // 3. Snap directly to the final position and rotation. This is instantaneous.
            transform.position = hit.point;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // 4. The puddle is now settled. Enable the main Update loop to start the growth.
            isSettled = true;
            this.enabled = true;
        }
        else
        {
            // This can happen if the bomb explodes in mid-air away from any surfaces.
            Debug.LogWarning("Poison Puddle settle raycast failed. Destroying.");
            Destroy(gameObject);
        }
    }


    void Update()
    {
        if (!isSettled) return;
        lifetimeTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;

        if (lifetimeTimer >= duration) { Destroy(gameObject); return; }

        HandleGrowthAndSizing();
        HandleFade();

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            ApplyAreaEffects();
        }
    }

    private void HandleGrowthAndSizing()
    {
        float currentRadius;
        if (lifetimeTimer < growthDuration)
        {
            float growthProgress = lifetimeTimer / growthDuration;
            currentRadius = Mathf.SmoothStep(initialRadius, maxRadius, growthProgress);
        }
        else
        {
            currentRadius = maxRadius;
        }

        transform.localScale = new Vector3(currentRadius * 2f, 0.1f, currentRadius * 2f);
        damageArea.radius = currentRadius;
    }

    private void HandleFade()
    {
        float remainingTime = duration - lifetimeTimer;
        if (remainingTime <= fadeOutDuration)
        {
            foreach (var rend in puddleRenderers)
            {
                if (rend.material.HasProperty("_BaseColor"))
                {
                    Color currentColor = rend.material.GetColor("_BaseColor");
                    float alpha = Mathf.Clamp01(remainingTime / fadeOutDuration);
                    rend.material.SetColor("_BaseColor", new Color(currentColor.r, currentColor.g, currentColor.b, alpha));
                }
            }
        }
    }

    private void ApplyAreaEffects()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageArea.radius, enemyLayer | playerLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (((1 << hitCollider.gameObject.layer) & enemyLayer) != 0)
            {
                if (hitCollider.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
                {
                    enemyHealth.TakeDamage(damagePerTick);
                }
            }
            else if (((1 << hitCollider.gameObject.layer) & playerLayer) != 0)
            {
                if (hitCollider.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
                {
                    playerHealth.Heal(healPerTick);
                }
            }
        }
    }
}
