using UnityEngine;

/// <summary>
/// Manages a single ice shard. It now also holds the settings for the shotgun burst effect.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class IceShardProjectile : MonoBehaviour
{
    [Header("Shotgun Settings")]
    [Tooltip("The number of projectiles to fire in a single burst. The WandController reads this value.")]
    public int projectileCount = 8;
    [Tooltip("The maximum horizontal angle (in degrees) of spread.")]
    public float horizontalSpreadAngle = 15.0f;
    [Tooltip("The maximum vertical angle (in degrees) of spread. Try a smaller value than horizontal.")]
    public float verticalSpreadAngle = 10.0f;

    [Header("Shard Properties")]
    [Tooltip("The forward travel speed of the shard.")]
    public float speed = 30f;
    [Tooltip("The damage dealt to an enemy on a direct hit.")]
    public int damage = 10;
    [Tooltip("How much 'chill' this shard applies on hit.")]
    public float chillAmount = 15f;
    [Tooltip("Max time the shard will exist in the world if it hits nothing.")]
    public float lifetime = 5f;

    [Header("Impact Effects")]
    [Tooltip("A particle effect to spawn on impact (e.g., a small frost puff).")]
    public GameObject impactVFXPrefab;
    [Tooltip("The hanging 'ice wall' effect prefab.")]
    public GameObject icyPatchPrefab;

    [Header("Ground Shatter Effect")]
    [Tooltip("The prefab to use for the shattered ice on the ground. Can be the same as the Icy Patch.")]
    public GameObject groundShardPrefab;
    [Tooltip("How many shards to spawn on the ground.")]
    public int groundShardCount = 3;
    [Tooltip("How far from the impact point the ground shards can spread.")]
    public float groundShardSpreadRadius = 1.5f;
    [Tooltip("The scale of the spawned ground shards.")]
    public float groundShardScale = 0.5f;

    [Header("System")]
    [Tooltip("Layers that the shard should consider 'environment'.")]
    public LayerMask environmentLayers;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        rb.velocity = transform.forward * speed;
        transform.rotation = Quaternion.Euler(
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f)
        );
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == this.gameObject.layer)
        {
            return;
        }

        EnemyStatusController enemyStatus = other.GetComponent<EnemyStatusController>();

        if (enemyStatus != null)
        {
            enemyStatus.ApplyChill(chillAmount);
            if(other.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
            {
                enemyHealth.TakeDamage(damage);
            }
        }
        else if (((1 << other.gameObject.layer) & environmentLayers) != 0)
        {
            Vector3 spawnPoint = transform.position;

            // --- Spawn the Hanging Ice Wall (Existing Logic) ---
            if (icyPatchPrefab != null)
            {
                Vector3 surfaceNormal = -transform.forward;
                Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
                Quaternion randomSpin = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), surfaceNormal);
                Quaternion finalRotation = randomSpin * surfaceRotation;
                Instantiate(icyPatchPrefab, spawnPoint, finalRotation);
            }

            // --- Spawn Jagged Ground Shards ---
            if (groundShardPrefab != null)
            {
                for (int i = 0; i < groundShardCount; i++)
                {
                    // Get a random point in a circle around the impact point.
                    Vector2 randomCircle = Random.insideUnitCircle * groundShardSpreadRadius;
                    Vector3 randomSpawnPoint = spawnPoint + new Vector3(randomCircle.x, 0, randomCircle.y);

                    // Raycast down from this random point to find the actual ground position.
                    RaycastHit groundHit;
                    if (Physics.Raycast(randomSpawnPoint + Vector3.up * 2f, Vector3.down, out groundHit, 4f, environmentLayers))
                    {
                        // --- [THE FIX] ---
                        // Instead of aligning to the ground, give each shard a completely random 3D rotation.
                        Quaternion randomRotation = Quaternion.Euler(
                            Random.Range(0f, 360f),
                            Random.Range(0f, 360f),
                            Random.Range(0f, 360f)
                        );

                        // Spawn the shard at the ground point with the new random rotation.
                        GameObject shard = Instantiate(groundShardPrefab, groundHit.point, randomRotation);
                        shard.transform.localScale = Vector3.one * groundShardScale;
                    }
                }
            }
        }

        if (impactVFXPrefab != null)
        {
            Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
