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

    [Header("Homing")]
    public bool enableHoming = false;
    public float homingTurnSpeed = 5f;
    public float homingRange = 10f;

    [Header("Damage")]
    public int damage = 15; // INT, not float!
    public int pierceCount = 1; // How many times can this missile pierce?

    [Header("Targeting")]
    public LayerMask enemyLayerMask;

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

        // Homing
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

        // Chaos
        _chaosTimer += Time.deltaTime;
        if (_chaosTimer > chaosChangeInterval)
        {
            NewChaosDirection();
            _chaosTimer = 0f;
        }

        // Squiggle
        float phase = (Time.time + _squiggleSeed) * squiggleFrequency;
        float squiggleOffset = Mathf.Sin(phase) * squiggleAmplitude;
        Vector3 squiggle = transform.right * squiggleOffset;

        Vector3 move = (_velocity * Time.deltaTime) + squiggle + (_chaosDir * lateralChaos * Time.deltaTime);
        transform.position += move;

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
                health.TakeDamage(damage); // int, as expected by your EnemyHealth

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
