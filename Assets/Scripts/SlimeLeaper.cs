using UnityEngine;

public class SlimeLeaper : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f;

    [Header("Leaping")]
    public float leapHorizontalSpeed = 10f;
    public float leapArcHeight = 3f;
    public float leapCooldown = 2f;
    public float chargeTime = 0.7f;
    public float leapRange = 8f;
    [Range(0, 1)]
    public float predictionStrength = 0.65f;
    [Header("Leaping Limits")]
    public float maxLeapTime = 2.0f;
    public float minLeapTime = 0.3f;
    public float maxLeapDistance = 20f;
    public float maxUsedVelocity = 12f;

    [Header("Detection")]
    public LayerMask playerLayer;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.35f;
    public float groundCheckYOffset = 0.5f;
    
    [Header("Attack")]
    public int damage = 10;
    public float damageCooldown = 0.25f;
    private float lastDamageTime = -100f;
    private bool canDealDamage = false;

    private enum State { Chasing, Charging, Leaping, Cooldown }
    private State currentState = State.Chasing;
    private Transform player;
    private Rigidbody playerRb;
    private Rigidbody rb;
    private float stateTimer = 0f;
    private Vector3 leapTarget;
    private bool leapLaunched = false;
    private Vector3 lastPlayerPos;
    private Vector3 playerVelocityTracked;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        player = FindPlayer();
        if (player)
        {
            playerRb = player.GetComponent<Rigidbody>();
            lastPlayerPos = player.position;
        }
        else
        {
            Debug.LogWarning("SLIME: No player found at Start!");
        }
    }

    void Update()
    {
        if (!player)
        {
            player = FindPlayer();
            if (player)
            {
                playerRb = player.GetComponent<Rigidbody>();
                lastPlayerPos = player.position;
            }
            else
            {
                return;
            }
        }

        playerVelocityTracked = (player.position - lastPlayerPos) / Mathf.Max(Time.deltaTime, 0.001f);
        lastPlayerPos = player.position;

        RotateLookAtPlayer();

        // Reset "canDealDamage" as soon as we land after a leap
        if (canDealDamage && IsGrounded() && currentState == State.Cooldown)
        {
            canDealDamage = false;
        }

        switch (currentState)
        {
            case State.Chasing:
                DoChase();
                break;
            case State.Charging:
                DoCharging();
                break;
            case State.Leaping:
                break;
            case State.Cooldown:
                DoCooldown();
                break;
        }
    }

    private Transform FindPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 100f, playerLayer);
        if (hits.Length > 0)
            return hits[0].transform;
        return null;
    }

    void DoChase()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > stopDistance)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            dir.Normalize();
            Vector3 move = dir * moveSpeed;
            move.y = rb.velocity.y;
            rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);
        }
        else
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }

        if (dist <= leapRange && IsGrounded())
        {
            currentState = State.Charging;
            stateTimer = chargeTime;
            leapTarget = PredictTargetPosition();
            rb.velocity = Vector3.zero;
            Debug.Log($"[SLIME] Charging leap! Predicted: {leapTarget}, Player: {player.position}, TrackedPlayerVel: {playerVelocityTracked}");
        }
    }

    void DoCharging()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            if (IsGrounded())
            {
                currentState = State.Leaping;
                leapLaunched = false;
            }
            else
            {
                Debug.Log("[SLIME] Not grounded, can't leap yet.");
                stateTimer = 0.1f;
            }
        }
    }

    void FixedUpdate()
    {
        if (currentState == State.Leaping && !leapLaunched)
        {
            if (!IsGrounded())
                return;

            Vector3 start = transform.position;
            Vector3 end = leapTarget;
            Vector3 jumpVector = CalculateLeapVelocity(start, end, leapArcHeight);

            rb.velocity = Vector3.zero;
            rb.AddForce(jumpVector, ForceMode.VelocityChange);

            leapLaunched = true;
            stateTimer = leapCooldown;
            currentState = State.Cooldown;

            canDealDamage = true; // Activate damage as soon as leap occurs

            Debug.Log($"[SLIME] Leaping! JumpVec: {jumpVector}, Target: {leapTarget}");
        }
    }

    void DoCooldown()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f && IsGrounded())
        {
            currentState = State.Chasing;
        }
    }

    bool IsGrounded()
    {
        Vector3 checkPos = transform.position + Vector3.down * groundCheckYOffset;
        bool grounded = Physics.CheckSphere(checkPos, groundCheckRadius, groundLayer);

#if UNITY_EDITOR
        Color color = grounded ? Color.green : Color.red;
        Debug.DrawLine(transform.position, checkPos, color, 0.1f);
#endif

        return grounded;
    }

    Vector3 PredictTargetPosition()
    {
        if (!player) return transform.position;

        Vector3 velocityFlat = new Vector3(playerVelocityTracked.x, 0, playerVelocityTracked.z);

        if (velocityFlat.magnitude > maxUsedVelocity)
            velocityFlat = velocityFlat.normalized * maxUsedVelocity;

        Vector3 slimeToPlayer = player.position - transform.position;
        slimeToPlayer.y = 0;
        float distance = slimeToPlayer.magnitude;
        float moveDirectionFactor =
            velocityFlat.magnitude > 0.05f
            ? Vector3.Dot(velocityFlat.normalized, slimeToPlayer.normalized)
            : 0f;

        float horizontalSpeed = Mathf.Max(leapHorizontalSpeed, 0.01f);
        float time = distance / horizontalSpeed;
        time = Mathf.Clamp(time, minLeapTime, maxLeapTime);

        Vector3 predicted = player.position + velocityFlat * time;
        predicted.y = player.position.y;

        float adjustedPrediction = predictionStrength;
        if (moveDirectionFactor < -0.7f)
            adjustedPrediction = 0.0f;
        else if (moveDirectionFactor < -0.3f)
            adjustedPrediction = predictionStrength * 0.3f;

        Vector3 result = Vector3.Lerp(player.position, predicted, adjustedPrediction);

        Vector3 clampedDir = (result - transform.position);
        clampedDir.y = 0;
        if (clampedDir.magnitude > maxLeapDistance)
        {
            clampedDir = clampedDir.normalized * maxLeapDistance;
            result = transform.position + clampedDir;
            result.y = player.position.y;
        }

        Debug.Log($"[SLIME] Predict: Now={player.position}, Vel={velocityFlat}, DirFac={moveDirectionFactor}, Time={time}, Predict={predicted}, Result={result}, AdjustedPred={adjustedPrediction}");
        return result;
    }

    Vector3 CalculateLeapVelocity(Vector3 start, Vector3 end, float arcHeight)
    {
        Vector3 delta = end - start;
        Vector3 deltaXZ = new Vector3(delta.x, 0, delta.z);

        float horizontalDistance = deltaXZ.magnitude;

        if (horizontalDistance > maxLeapDistance)
        {
            deltaXZ = deltaXZ.normalized * maxLeapDistance;
            horizontalDistance = maxLeapDistance;
            end = start + deltaXZ;
        }

        float horizontalSpeed = Mathf.Max(leapHorizontalSpeed, 0.01f);
        float time = horizontalDistance / horizontalSpeed;
        time = Mathf.Clamp(time, minLeapTime, maxLeapTime);

        float gravity = Mathf.Abs(Physics.gravity.y);
        float verticalVelocity = (end.y - start.y + arcHeight) / time + 0.5f * gravity * time;

        Vector3 velocity = deltaXZ.normalized * horizontalSpeed;
        velocity.y = verticalVelocity;

        Debug.Log($"[SLIME] CalcLeap: Start={start}, End={end}, Time={time}, Vx={velocity.x}, Vy={velocity.y}, Vz={velocity.z}");
        return velocity;
    }

    void RotateLookAtPlayer()
    {
        if (!player) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            Vector3 euler = lookRot.eulerAngles;
            euler.x = 0f;
            euler.z = 0f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), Time.deltaTime * 10f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 checkPos = transform.position + Vector3.down * groundCheckYOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (canDealDamage && collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            var playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth)
            {
                if (Time.time > lastDamageTime + damageCooldown)
                {
                    playerHealth.TakeDamage(damage);
                    Debug.Log("[SLIME] Splat! Hit the player for " + damage);
                    lastDamageTime = Time.time;
                    canDealDamage = false; // Only damage once per leap/landing
                }
            }
        }
    }
}
