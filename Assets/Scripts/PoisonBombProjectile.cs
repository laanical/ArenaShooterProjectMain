using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PoisonBombProjectile : MonoBehaviour
{
    [Header("Launch Physics")]
    public float minLaunchForce = 10f;
    public float maxLaunchForce = 30f;
    public float launchAngle = 30f;

    [Header("Explosion Settings")]
    public GameObject poisonPuddlePrefab;
    public GameObject impactVFX;
    public GameObject expansionBurstVFXPrefab;
    public int impactDamage = 25;
    public float impactRadius = 5f;
    public float initialSplatSize = 1.5f;
    public float expansionInitialSize = 0.2f;

    [Header("Splatter Effect")]
    public GameObject goopletPrefab;
    public int goopletCount = 3;
    public float goopletSplatterForce = 8f;
    public float goopletSpawnDelay = 0.1f;
    [Range(0f, 90f)]
    public float splatterUpwardAngle = 45f;

    [Header("System")]
    public LayerMask enemyLayer;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private bool hasExploded = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(float chargePower)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        StartCoroutine(EnableColliderAfterDelay(0.1f));
        
        float launchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, chargePower);
        
        Vector3 targetPoint;
        Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(cameraRay, out RaycastHit hitInfo, 100f))
        {
            targetPoint = hitInfo.point;
        }
        else
        {
            targetPoint = cameraRay.GetPoint(100f);
        }
        
        Vector3 directionToTarget = (targetPoint - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToTarget);

        Vector3 launchDirection = Quaternion.AngleAxis(-launchAngle, transform.right) * transform.forward;
        rb.AddForce(launchDirection * launchForce, ForceMode.Impulse);
    }

    private IEnumerator EnableColliderAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this != null && TryGetComponent<SphereCollider>(out var col))
        {
            col.enabled = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        hasExploded = true;

        ContactPoint contact = collision.contacts[0];
        StartCoroutine(ExplodeSequence(contact.point, contact.normal, collision.gameObject));
    }
    
    private IEnumerator ExplodeSequence(Vector3 position, Vector3 normal, GameObject hitObject)
    {
        // Stop all physics and movement immediately.
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;
        if (TryGetComponent<TrailRenderer>(out var trail))
        {
            trail.enabled = false;
        }

        // --- [THE FIX] ---
        // Make the original bomb projectile's mesh invisible instantly.
        if (TryGetComponent<Renderer>(out var renderer))
        {
            renderer.enabled = false;
        }
        // --- [END FIX] ---

        if (impactVFX != null)
        {
            Instantiate(impactVFX, position, Quaternion.LookRotation(normal));
        }

        if (expansionBurstVFXPrefab != null)
        {
            GameObject burst = Instantiate(expansionBurstVFXPrefab, position, Quaternion.identity);
            if (burst.TryGetComponent<ExpansionBurstVFX>(out var burstController))
            {
                burstController.initialSize = this.expansionInitialSize;
                burstController.maxSize = this.impactRadius * 2;
            }
        }
        
        Collider[] hitEnemies = Physics.OverlapSphere(position, impactRadius, enemyLayer);
        foreach (var enemyCollider in hitEnemies)
        {
            if (enemyCollider.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.TakeDamage(impactDamage);
            }
        }

        if ((enemyLayer.value & (1 << hitObject.layer)) != 0)
        {
            if (Physics.Raycast(position, Vector3.down, out RaycastHit groundHit, 50f, groundLayer))
            {
                SpawnPuddle(groundHit.point, groundHit.normal);
                StartCoroutine(SpawnGoopletsWithDelay(groundHit.point, groundHit.normal));
            }
            else
            {
                // If there's no ground, we still need to destroy the projectile.
                Destroy(gameObject);
            }
        }
        else
        {
            SpawnPuddle(position, normal);
            StartCoroutine(SpawnGoopletsWithDelay(position, normal));
        }

        yield return null;
    }
    
    private void SpawnPuddle(Vector3 position, Vector3 normal)
    {
        if (poisonPuddlePrefab == null) return;

        GameObject puddleObj = Instantiate(poisonPuddlePrefab, position, Quaternion.identity);
        if (puddleObj.TryGetComponent<PoisonPuddleController>(out var puddleController))
        {
            puddleController.Initialize(normal);
        }
    }

    private IEnumerator SpawnGoopletsWithDelay(Vector3 explosionCenter, Vector3 surfaceNormal)
    {
        yield return new WaitForSeconds(goopletSpawnDelay);

        Vector3 randomReference = (Mathf.Abs(surfaceNormal.y) > 0.9f) ? Vector3.right : Vector3.up;
        Vector3 tangent = Vector3.Cross(surfaceNormal, randomReference).normalized;

        Quaternion upwardRotation = Quaternion.AngleAxis(-splatterUpwardAngle, tangent);
        Vector3 baseLaunchDirection = upwardRotation * surfaceNormal;

        float angleStep = 360f / goopletCount;
        for (int i = 0; i < goopletCount; i++)
        {
            Quaternion rotation = Quaternion.AngleAxis(angleStep * i, surfaceNormal);
            Vector3 launchDirection = rotation * baseLaunchDirection;

            Vector3 spawnPos = explosionCenter + launchDirection * impactRadius;
            GameObject gooplet = Instantiate(goopletPrefab, spawnPos, Quaternion.LookRotation(launchDirection));
            if (gooplet.TryGetComponent<Rigidbody>(out var goopletRb))
            {
                goopletRb.AddForce(launchDirection * goopletSplatterForce, ForceMode.Impulse);
            }
        }

        // Now that the gooplets have been spawned, the projectile's job is done.
        Destroy(gameObject);
    }
}
