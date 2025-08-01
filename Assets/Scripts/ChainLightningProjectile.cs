using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;
using System.Linq;

// Manages the behavior of an instantaneous, chaining lightning effect.
[RequireComponent(typeof(LineRenderer))]
public class ChainLightningProjectile : MonoBehaviour
{
    [Header("Core Settings")]
    public float initialDamage = 20f;
    public float maxDistance = 50f;
    public float visualDuration = 0.25f;

    [Header("Chaining Logic")]
    public int maxChains = 2;
    public float chainRange = 15f;
    [Range(0f, 1f)]
    public float damageFalloff = 0.75f;
    public LayerMask enemyLayer;
    public LayerMask collisionLayer;

    [Header("Ricochet Logic")]
    public bool canRicochet = true;

    [Header("Visuals")]
    public GameObject hitVFX;
    public int pointsPerSegment = 15;
    public float jitterAmount = 0.3f;

    // --- Private State ---
    private LineRenderer lineRenderer;
    private List<Transform> targetsHit;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        targetsHit = new List<Transform>();
    }

    void Start()
    {
        // Instead of calculating the bolt and then destroying it later,
        // we now start a coroutine that handles the entire lifecycle.
        StartCoroutine(AnimateBoltLifecycle());
    }

    // This new coroutine manages the bolt's creation, decay, and destruction.
    private IEnumerator AnimateBoltLifecycle()
    {
        // --- Phase 1: Create the Bolt ---
        // This part is the same as before: calculate the path and draw the initial bolt.
        List<Vector3> pathPoints = CalculateBoltPath();
        DrawLightning(pathPoints);

        // --- Phase 2: Animate the Decay ---
        float elapsedTime = 0f;
        
        // Store the original start and end colors of the gradient.
        Gradient originalGradient = lineRenderer.colorGradient;
        GradientColorKey[] originalColorKeys = originalGradient.colorKeys;

        while (elapsedTime < visualDuration)
        {
            elapsedTime += Time.deltaTime;
            float fadeProgress = elapsedTime / visualDuration;

            // Create a new gradient for this frame.
            Gradient decayingGradient = new Gradient();

            // The alpha keys define the transparency along the line.
            // We create a sharp cutoff that moves from start (0.0) to end (1.0).
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            // Key 1: The line is fully transparent from the start up to the current progress.
            alphaKeys[0] = new GradientAlphaKey(0f, fadeProgress);
            // Key 2: The line is fully opaque from the progress point to the end.
            alphaKeys[1] = new GradientAlphaKey(1f, fadeProgress + 0.001f); // The tiny offset ensures a sharp line.

            // Set the new gradient with the original colors but the new, animated alpha.
            decayingGradient.SetKeys(originalColorKeys, alphaKeys);
            lineRenderer.colorGradient = decayingGradient;

            yield return null; // Wait for the next frame.
        }

        // --- Phase 3: Destroy the GameObject ---
        // Once the animation is finished, clean up the bolt.
        Destroy(gameObject);
    }

    // I've moved the bolt calculation logic into its own function to keep things clean.
    private List<Vector3> CalculateBoltPath()
    {
        float currentDamage = initialDamage;
        int chainsRemaining = maxChains;
        Vector3 currentPosition = transform.position;
        Vector3 currentDirection = transform.forward;

        List<Vector3> pathPoints = new List<Vector3> { currentPosition };

        // --- Initial Hit ---
        RaycastHit hit;
        if (Physics.Raycast(currentPosition, currentDirection, out hit, maxDistance, collisionLayer | enemyLayer))
        {
            pathPoints.Add(hit.point);
            currentPosition = hit.point;
            if (hitVFX != null) Instantiate(hitVFX, currentPosition, Quaternion.identity);

            EnemyHealth initialEnemy = hit.collider.GetComponent<EnemyHealth>();
            if (initialEnemy != null)
            {
                initialEnemy.TakeDamage((int)currentDamage);
                targetsHit.Add(initialEnemy.transform);
            }
            else if (canRicochet)
            {
                currentDirection = Vector3.Reflect(currentDirection, hit.normal);
                RaycastHit bounceHit;
                if (Physics.Raycast(currentPosition, currentDirection, out bounceHit, chainRange, enemyLayer))
                {
                    EnemyHealth bouncedEnemy = bounceHit.collider.GetComponent<EnemyHealth>();
                    if (bouncedEnemy != null)
                    {
                        pathPoints.Add(bounceHit.point);
                        currentPosition = bounceHit.point;
                        if (hitVFX != null) Instantiate(hitVFX, currentPosition, Quaternion.identity);
                        bouncedEnemy.TakeDamage((int)currentDamage);
                        targetsHit.Add(bouncedEnemy.transform);
                    }
                }
            }
        }
        else
        {
            pathPoints.Add(currentPosition + currentDirection * maxDistance);
        }

        // --- Chaining Logic ---
        while (chainsRemaining > 0)
        {
            Transform nextTarget = FindNextTarget(currentPosition);
            if (nextTarget != null)
            {
                chainsRemaining--;
                currentDamage *= damageFalloff;
                currentPosition = nextTarget.position;
                pathPoints.Add(currentPosition);
                if (hitVFX != null) Instantiate(hitVFX, currentPosition, Quaternion.identity);
                nextTarget.GetComponent<EnemyHealth>().TakeDamage((int)currentDamage);
                targetsHit.Add(nextTarget);
            }
            else
            {
                break;
            }
        }
        return pathPoints;
    }

    // --- Helper functions (unchanged) ---

    private Transform FindNextTarget(Vector3 fromPosition)
    {
        Collider[] potentialTargets = Physics.OverlapSphere(fromPosition, chainRange, enemyLayer);
        Transform closestTarget = null;
        float minDistance = float.MaxValue;

        foreach (Collider targetCollider in potentialTargets)
        {
            if (!targetsHit.Contains(targetCollider.transform))
            {
                float distance = Vector3.Distance(fromPosition, targetCollider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTarget = targetCollider.transform;
                }
            }
        }
        return closestTarget;
    }

    private void DrawLightning(List<Vector3> mainPathPoints)
    {
        if (lineRenderer == null || mainPathPoints.Count < 2) return;

        List<Vector3> finalPoints = new List<Vector3>();

        for (int i = 0; i < mainPathPoints.Count - 1; i++)
        {
            Vector3 startPoint = mainPathPoints[i];
            Vector3 endPoint = mainPathPoints[i + 1];
            finalPoints.Add(startPoint);
            for (int j = 1; j < pointsPerSegment; j++)
            {
                Vector3 pointOnLine = Vector3.Lerp(startPoint, endPoint, (float)j / pointsPerSegment);
                Vector3 jitter = Random.insideUnitSphere * jitterAmount;
                finalPoints.Add(pointOnLine + jitter);
            }
        }
        finalPoints.Add(mainPathPoints[mainPathPoints.Count - 1]);

        lineRenderer.positionCount = finalPoints.Count;
        lineRenderer.SetPositions(finalPoints.ToArray());
    }
}
