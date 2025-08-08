using UnityEngine;
using System.Collections.Generic;

public class MagicMissile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 20f;
    public float lifetime = 5f;

    [Header("Chaos")]
    public float squiggleAmplitude = 0.3f;
    public float squiggleFrequency = 8f;
    public float lateralChaos = 1.5f;
    public float chaosChangeInterval = 0.25f;

    [Header("Grace")]
    public float gracePeriod = 0.5f; // <-- Add this for fly-straight time

    [Header("Homing")]
    public bool enableHoming = false;
    public float homingTurnSpeed = 5f;
    public float homingRange = 10f;

    [Header("Damage")]
    public int damage = 15; // INT, not float!
    public int pierceCount = 1; // How many times can this missile pierce?

    [Header("Targeting")]
    public LayerMask enemyLayerMask;

    // --- Fields ---
    private Vector3 _velocity;
    private float _timer;
    private float _squiggleSeed;
    private Vector3 _chaosDir = Vector3.zero;
    private float _chaosTimer = 0f;
    private HashSet<EnemyHealth> _damagedEnemies = new HashSet<EnemyHealth>();

    void Start()
    {
        _velocity = transform.forward * speed;
        _squiggleSeed = Random.value * 100f;
        NewChaosDirection();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // --- GRACE PERIOD: Fly Straight ---
        if (_timer < gracePeriod)
        {
            // Just move straight, no chaos/no homing
            transform.position += _velocity * Time.deltaTime;
            // Keep forward direction constant
            if (_velocity != Vector3.zero) transform.forward = _velocity.normalized;
            return;
        }

        // --- AFTER GRACE: Start Chaos/Homing ---

        // Homing (if enabled)
        if (enableHoming)
        {
            Transform target = GetBestHomingTarget();
            if (target)
            {
                Vector3 toTarget = (target.position - transform.position).normalized;
                float turnStep = homingTurnSpeed * Time.deltaTime;
                _velocity = Vector3.RotateTowards(_velocity, toTarget * speed, turnStep, 0f);
            }
        }

        // Chaos direction change timer
        _chaosTimer += Time.deltaTime;
        if (_chaosTimer > chaosChangeInterval)
        {
            NewChaosDirection();
            _chaosTimer = 0f;
        }

        // Squiggle (sideways wiggle/distortion)
        float phase = (Time.time + _squiggleSeed) * squiggleFrequency;
        float squiggleOffset = Mathf.Sin(phase) * squiggleAmplitude;
        Vector3 squiggle = transform.right * squiggleOffset;

        // Combine velocity, squiggle, and chaos
        Vector3 move = (_velocity * Time.deltaTime) + squiggle + (_chaosDir * lateralChaos * Time.deltaTime);
        transform.position += move;

        // Update look direction
        if (move != Vector3.zero)
            transform.forward = move.normalized;
    }

    void OnTriggerEnter(Collider other)
    {
        // Only deal damage if it's an enemy
        if (((1 << other.gameObject.layer) & enemyLayerMask.value) != 0)
        {
            var health = other.GetComponent<EnemyHealth>();
            if (health != null && !_damagedEnemies.Contains(health))
            {
                _damagedEnemies.Add(health);
                health.TakeDamage(damage);

                pierceCount--;
                if (pierceCount <= 0)
                {
                    Destroy(gameObject);
                }
                // else: Missile keeps flying, looking for next victim
            }
        }
    }

    private void NewChaosDirection()
    {
        Vector3 randomDir = Vector3.Cross(transform.forward, Random.onUnitSphere).normalized;
        _chaosDir = randomDir;
    }

    Transform GetBestHomingTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, homingRange, enemyLayerMask);
        foreach (var hit in hits)
        {
            if (hit != null)
            {
                var health = hit.GetComponent<EnemyHealth>();
                if (health != null && !_damagedEnemies.Contains(health))
                    return hit.transform;
            }
        }
        return null;
    }
}
