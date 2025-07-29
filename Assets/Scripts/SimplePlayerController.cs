using UnityEngine;
using System.Collections;
using System; // Required for events

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.5f;
    public float crouchMultiplier = 0.5f;

    [Header("Jumping & Gravity")]
    public float jumpForce = 8f;
    [Tooltip("How many times the player can jump before needing to touch a surface.")]
    public int maxAirJumps = 3;
    public float gravity = -30f;

    [Header("Crouching")]
    public float standingHeight = 2.0f;
    public float crouchingHeight = 1.0f;
    public float crouchTransitionSpeed = 10f;

    [Header("Dashing")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2f;

    [Header("Enemy Interaction")]
    public LayerMask enemyLayer;
    public float enemyCollisionSlowdown = 0.25f;
    public float slowdownLingerDuration = 0.5f;
    public float enemyContactRadius = 0.6f;

    // --- Private State Fields ---
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private bool isCrouching = false;
    private int jumpsRemaining;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;
    private float slowdownTimer = 0f;

    // --- Public Properties for UI/Other Scripts ---
    public bool IsCrouching => isCrouching;
    public float DashCooldownTimer => dashCooldownTimer;
    // NEW: Public properties for the jump UI
    public int JumpsRemaining => jumpsRemaining;
    public int MaxAirJumps => maxAirJumps;

    // NEW: Event to notify UI when jumps change
    public static event Action<int, int> OnJumpsChanged;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        jumpsRemaining = maxAirJumps;
        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // Fire event on start to initialize UI
        OnJumpsChanged?.Invoke(jumpsRemaining, maxAirJumps);
    }

    void Update()
    {
        HandleInput();
        HandleEnemyContact(); 

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (slowdownTimer > 0)
        {
            slowdownTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        HandleCrouchCollider();
        MovePlayer();
    }

    private void HandleInput()
    {
        if (jumpsRemaining > 0 && Input.GetButtonDown("Jump"))
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (isCrouching)
            {
                if (!CheckForHeadObstruction())
                {
                    isCrouching = false;
                }
            }
            else
            {
                isCrouching = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) && !isDashing && dashCooldownTimer <= 0)
        {
            StartCoroutine(DashCoroutine());
        }
    }

    private void MovePlayer()
    {
        rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);

        if (isDashing) return;

        float speedMultiplier = 1.0f;
        if (isCrouching)
        {
            speedMultiplier = crouchMultiplier;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            speedMultiplier = sprintMultiplier;
        }
        
        float currentSpeed = moveSpeed * speedMultiplier;

        if (slowdownTimer > 0)
        {
            float decayProgress = slowdownTimer / slowdownLingerDuration;
            float currentSlowdown = Mathf.Lerp(1f, enemyCollisionSlowdown, decayProgress);
            currentSpeed *= currentSlowdown;
        }
        
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized;
        
        Vector3 targetVelocity = inputDirection * currentSpeed;
        Vector3 velocityChange = (targetVelocity - new Vector3(rb.velocity.x, 0, rb.velocity.z));
        
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void HandleEnemyContact()
    {
        Vector3 checkCenter = transform.TransformPoint(playerCollider.center);
        if (Physics.CheckSphere(checkCenter, enemyContactRadius, enemyLayer))
        {
            slowdownTimer = slowdownLingerDuration;
        }
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");
        Vector3 dashDirection = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized;

        if (dashDirection == Vector3.zero)
        {
            dashDirection = transform.forward;
        }

        rb.AddForce(dashDirection * dashForce, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }

    private void Jump()
    {
        jumpsRemaining--;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        // Fire the event to update the UI
        OnJumpsChanged?.Invoke(jumpsRemaining, maxAirJumps);
    }
    
    // --- REVISED JUMP RESET LOGIC ---
    private void OnCollisionEnter(Collision collision)
    {
        // If we have used any jumps, reset them upon touching any surface.
        if (jumpsRemaining < maxAirJumps)
        {
            jumpsRemaining = maxAirJumps;
            // Fire the event to update the UI
            OnJumpsChanged?.Invoke(jumpsRemaining, maxAirJumps);
        }
    }

    private void HandleCrouchCollider()
    {
        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        Vector3 targetCenter = new Vector3(0, targetHeight / 2f, 0);

        playerCollider.height = Mathf.Lerp(playerCollider.height, targetHeight, Time.fixedDeltaTime * crouchTransitionSpeed);
        playerCollider.center = Vector3.Lerp(playerCollider.center, targetCenter, Time.fixedDeltaTime * crouchTransitionSpeed);
    }

    private bool CheckForHeadObstruction()
    {
        float checkRadius = playerCollider.radius;
        Vector3 point1 = transform.position + new Vector3(0, checkRadius, 0);
        Vector3 point2 = transform.position + new Vector3(0, standingHeight - checkRadius, 0);
        
        return Physics.CheckCapsule(point1, point2, checkRadius, ~LayerMask.GetMask("Player"));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (playerCollider != null)
        {
            Vector3 gizmoCenter = transform.TransformPoint(playerCollider.center);
            Gizmos.DrawWireSphere(gizmoCenter, enemyContactRadius);
        }
    }
}
