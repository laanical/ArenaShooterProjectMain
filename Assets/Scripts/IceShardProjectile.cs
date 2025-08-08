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

    // Debugging vars
    private float spawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        spawnTime = Time.time;
        //Debug.Log($"[ICE SHARD SPAWNED] {name} at {transform.position} | Layer: {gameObject.layer} | Time: {spawnTime}", this);

        rb.velocity = transform.forward * speed;
        // Random rotation on spawn (as before)
        transform.rotation = Quaternion.Euler(
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f)
        );
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Add a small grace period after spawn, in case of physics weirdness
        if (Time.time - spawnTime < 0.03f) {
            //Debug.Log($"[ICE SHARD IGNORED COLLISION] {name} with {other.name} ({other.gameObject.layer}) due to grace period", this);
            return;
        }

        //Debug.Log($"[ICE SHARD COLLISION] {name} ({gameObject.layer}) hit {other.name} ({other.gameObject.layer}) at {transform.position} | Time: {Time.time}", this);

        if (other.gameObject.layer == this.gameObject.layer)
        {
            //Debug.Log($"[ICE SHARD SELF-LAYER] {name} collided with another projectile, ignoring.", this);
            return;
        }

        EnemyStatusController enemyStatus = other.GetComponent<EnemyStatusController>();

        if (enemyStatus != null)
        {
            //Debug.Log($"[ICE SHARD HIT ENEMY] {name} hit {other.name}, applying chill/damage.", this);
            enemyStatus.ApplyChill(chillAmount);
            if(other.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
            {
                enemyHealth.TakeDamage(damage);
            }
        }
        else if (((1 << other.gameObject.layer) & environmentLayers) != 0)
        {
            //Debug.Log($"[ICE SHARD HIT ENVIRONMENT] {name} hit {other.name} (layer {other.gameObject.layer}).", this);
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
                    Vector2 randomCircle = Random.insideUnitCircle * groundShardSpreadRadius;
                    Vector3 randomSpawnPoint = spawnPoint + new Vector3(randomCircle.x, 0, randomCircle.y);

                    RaycastHit groundHit;
                    if (Physics.Raycast(randomSpawnPoint + Vector3.up * 2f, Vector3.down, out groundHit, 4f, environmentLayers))
                    {
                        Quaternion randomRotation = Quaternion.Euler(
                            Random.Range(0f, 360f),
                            Random.Range(0f, 360f),
                            Random.Range(0f, 360f)
                        );
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

        //Debug.Log($"[ICE SHARD DESTROYED] {name} at {transform.position} | Time: {Time.time}", this);
        Destroy(gameObject);
    }
}
