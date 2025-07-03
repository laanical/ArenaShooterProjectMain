using UnityEngine;

// A simple, from-scratch Rigidbody-based character controller.
// This version is designed to work with default physics materials.
// It handles ground movement, slope adaptation, jumping, and custom gravity.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;

    [Header("Gravity & Ground Check")]
    public float gravity = -20f; // Custom gravity force.
    public LayerMask groundLayer;
    [Tooltip("The steepest slope the player can walk on, in degrees.")]
    public float maxSlopeAngle = 45f;

    // --- Private Fields ---
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Animator animator; // Added for animation support

    private bool isGrounded;
    private Vector3 groundNormal; // The normal of the slope we're standing on.

    // Animator parameter IDs for efficiency
    private readonly int animIDSpeed = Animator.StringToHash("Speed");
    private readonly int animIDGrounded = Animator.StringToHash("Grounded"); // Make sure this matches your Animator parameter
    private readonly int animIDJump = Animator.StringToHash("Jump");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>(); // Get the animator component
        
        // Let our script control all physics.
        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Handle jump input in Update for responsiveness.
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        // Perform all physics calculations in FixedUpdate.
        CheckIfGrounded();
        MovePlayer();
        UpdateAnimator();
    }

    private void MovePlayer()
    {
        // 1. Get Input
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized;

        if (isGrounded)
        {
            // --- Grounded Movement Logic ---
            Vector3 moveDirection = Vector3.ProjectOnPlane(inputDirection, groundNormal).normalized;
            
            if (inputDirection.sqrMagnitude > 0.01f)
            {
                rb.velocity = moveDirection * moveSpeed;
            }
            else 
            {
                rb.velocity = Vector3.zero;
            }

            // --- THE FIX for the "stair-top hop" ---
            // If we are grounded but somehow moving upwards (e.g., from cresting a ramp),
            // clamp the vertical velocity to zero. This prevents the hop.
            if (rb.velocity.y > 0)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            }
        }
        else
        {
            // --- Airborne Movement Logic ---
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            float verticalVelocity = rb.velocity.y;

            verticalVelocity += gravity * Time.fixedDeltaTime;
            
            rb.velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        }
    }

    private void Jump()
    {
        // To get a consistent jump height, we first reset the vertical velocity before applying the jump force.
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void CheckIfGrounded()
    {
        Vector3 origin = transform.position + new Vector3(0, playerCollider.radius, 0);
        float radius = playerCollider.radius * 0.95f;
        
        // --- THE FIX for the "downhill glide" ---
        // Increase the maxDistance slightly to make the check more reliable when walking off edges.
        float maxDistance = playerCollider.radius + 0.2f; 

        RaycastHit hit;
        if (Physics.SphereCast(origin, radius, Vector3.down, out hit, maxDistance, groundLayer))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle <= maxSlopeAngle)
            {
                isGrounded = true;
                groundNormal = hit.normal;
                return;
            }
        }
        
        isGrounded = false;
        groundNormal = Vector3.up;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        
        animator.SetFloat(animIDSpeed, speed);
        animator.SetBool(animIDGrounded, isGrounded);
    }
}
