using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Manages the behavior of an instantaneous, chaining lightning effect.
[RequireComponent(typeof(LineRenderer))] // Now requires a Line Renderer
public class ChainLightningProjectile : MonoBehaviour
{
    [Header("Core Settings")]
    public float initialDamage = 20f;
    [Tooltip("The maximum distance of the initial bolt.")]
    public float maxDistance = 50f;
    [Tooltip("How long the lightning effect stays visible.")]
    public float visualDuration = 0.25f;

    [Header("Chaining Logic")]
    [Tooltip("The maximum number of times the effect can chain to a new enemy.")]
    public int maxChains = 2;
    [Tooltip("The radius within which the effect will look for its next target.")]
    public float chainRange = 15f;
    [Tooltip("The damage multiplier for each subsequent chain (e.g., 0.75 for 25% less damage).")]
    [Range(0f, 1f)]
    public float damageFalloff = 0.75f;
    public LayerMask enemyLayer;
    [Tooltip("Layers that the initial bolt will collide with (walls, ground, etc.).")]
    public LayerMask collisionLayer;

    [Header("Ricochet Logic")]
    [Tooltip("Can the initial bolt bounce off a surface to find a target?")]
    public bool canRicochet = true;

    [Header("Visuals")]
    [Tooltip("The particle effect to spawn on each impact point.")]
    public GameObject hitVFX;

    // --- Private State ---
    private LineRenderer lineRenderer;
    private List<Transform> targetsHit; // List to prevent hitting the same target twice.

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        targetsHit = new List<Transform>();
    }

    void Start()
    {
        // The entire effect now happens instantly in Start.
        FireInstantBolt();
        Destroy(gameObject, visualDuration); // Destroy the effect after a short time.
    }

    private void FireInstantBolt()
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
            // --- We hit something ---
            pathPoints.Add(hit.point);
            currentPosition = hit.point;

            EnemyHealth initialEnemy = hit.collider.GetComponent<EnemyHealth>();
            if (initialEnemy != null)
            {
                // If we hit an enemy directly, deal damage and add it to the hit list.
                initialEnemy.TakeDamage((int)currentDamage);
                targetsHit.Add(initialEnemy.transform);
                if (hitVFX != null) Instantiate(hitVFX, currentPosition, Quaternion.identity);
            }
            else if (canRicochet)
            {
                // If we hit a wall and can bounce, try to find a target from the bounce point.
                currentDirection = Vector3.Reflect(currentDirection, hit.normal);
                RaycastHit bounceHit;
                if (Physics.Raycast(currentPosition, currentDirection, out bounceHit, chainRange, enemyLayer))
                {
                    EnemyHealth bouncedEnemy = bounceHit.collider.GetComponent<EnemyHealth>();
                    if (bouncedEnemy != null)
                    {
                        pathPoints.Add(bounceHit.point);
                        currentPosition = bounceHit.point;
                        bouncedEnemy.TakeDamage((int)currentDamage);
                        targetsHit.Add(bouncedEnemy.transform);
                        if (hitVFX != null) Instantiate(hitVFX, currentPosition, Quaternion.identity);
                    }
                }
            }
        }
        else
        {
            // --- We hit nothing ---
            pathPoints.Add(currentPosition + currentDirection * maxDistance);
        }

        // --- Chaining Logic ---
        while (chainsRemaining > 0)
        {
            Transform nextTarget = FindNextTarget(currentPosition);
            if (nextTarget != null)
            {
                chainsRemaining--;
                currentDamage *= damageFalloff; // Reduce damage for the next chain

                currentPosition = nextTarget.position;
                pathPoints.Add(currentPosition);

                nextTarget.GetComponent<EnemyHealth>().TakeDamage((int)currentDamage);
                targetsHit.Add(nextTarget);
                if (hitVFX != null) Instantiate(hitVFX, currentPosition, Quaternion.identity);
            }
            else
            {
                // No more targets found, stop chaining.
                break;
            }
        }

        // --- Draw the final visual ---
        DrawLightning(pathPoints);
    }

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

    private void DrawLightning(List<Vector3> points)
    {
        if (lineRenderer == null || points.Count < 2) return;
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }
}
