using UnityEngine;
using System.Collections; // Required for smooth crouching

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float moveSpeed = 6f;
    [Tooltip("How much faster the player moves when sprinting. 1.5 = 50% faster.")]
    public float sprintMultiplier = 1.5f;
    [Tooltip("How much slower the player moves when crouching. 0.5 = 50% slower.")]
    public float crouchMultiplier = 0.5f;

    [Header("Jumping & Air Control")]
    public float jumpForce = 8f;
    [Tooltip("How much control the player has over movement while airborne.")]
    public float airControlFactor = 0.5f;

    [Header("Crouching")]
    public float standingHeight = 2.0f;
    public float crouchingHeight = 1.0f;
    public float crouchTransitionSpeed = 10f;
    public Transform cameraTransform; // Assign your player camera transform here

    [Header("Gravity & Ground Check")]
    public float gravity = -20f;
    public LayerMask groundLayer;
    public float maxSlopeAngle = 45f;
    
    [Header("Debugging")]
    [Tooltip("Shows the current grounded state in the Inspector.")]
    [SerializeField] private bool _isGrounded_DEBUG;

    // --- Private State Fields ---
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Vector3 originalCameraPosition;
    private bool isGrounded;
    private Vector3 groundNormal;
    private bool isCrouching = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        
        if (cameraTransform == null)
        {
            Debug.LogError("Player Camera Transform not assigned in the inspector!", this);
            enabled = false;
            return;
        }
        originalCameraPosition = cameraTransform.localPosition;

        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void Update()
    {
        HandleInput();
        HandleCrouch();
    }

    void FixedUpdate()
    {
        CheckIfGrounded();
        MovePlayer();
    }

    private void HandleInput()
    {
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }
    }

    private void MovePlayer()
    {
        float currentSpeed = moveSpeed;
        if (isCrouching)
        {
            currentSpeed *= crouchMultiplier;
        }
        else if (Input.GetKey(KeyCode.LeftShift)) 
        {
            currentSpeed *= sprintMultiplier;
        }

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized;

        if (isGrounded)
        {
            Vector3 targetVelocity = Vector3.ProjectOnPlane(inputDirection, groundNormal) * currentSpeed;
            rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
        }
        else
        {
            Vector3 airForce = inputDirection * moveSpeed * airControlFactor;
            rb.AddForce(airForce);
        }

        rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    // --- REVISED AND FIXED CROUCH HANDLING ---
    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        
        // --- THE FIX ---
        // We must also adjust the collider's center point as its height changes.
        // The center of a capsule is always at half its height.
        Vector3 targetCenter = new Vector3(0, targetHeight / 2f, 0);

        // Smoothly transition the collider's height AND center.
        playerCollider.height = Mathf.Lerp(playerCollider.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        playerCollider.center = Vector3.Lerp(playerCollider.center, targetCenter, Time.deltaTime * crouchTransitionSpeed);

        // Also adjust the camera position to match the crouch.
        Vector3 targetCameraPos = isCrouching ? 
            new Vector3(originalCameraPosition.x, originalCameraPosition.y - (standingHeight - crouchingHeight), originalCameraPosition.z) : 
            originalCameraPosition;
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetCameraPos, Time.deltaTime * crouchTransitionSpeed);
    }

    private void CheckIfGrounded()
    {
        Vector3 spherePosition = transform.position + playerCollider.center;
        float sphereRadius = playerCollider.radius * 0.9f;
        float checkDistance = (playerCollider.height / 2f) - playerCollider.radius + 0.1f;

        RaycastHit hit;
        if (Physics.SphereCast(spherePosition, sphereRadius, Vector3.down, out hit, checkDistance, groundLayer))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle <= maxSlopeAngle)
            {
                isGrounded = true;
                groundNormal = hit.normal;
                _isGrounded_DEBUG = true;
                return;
            }
        }
        
        isGrounded = false;
        groundNormal = Vector3.up;
        _isGrounded_DEBUG = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Gizmos.color = Color.green;
            Vector3 spherePosition = transform.position + playerCollider.center;
            float sphereRadius = playerCollider.radius * 0.9f;
            float checkDistance = (playerCollider.height / 2f) - playerCollider.radius + 0.1f;
            
            Vector3 endPosition = spherePosition + (Vector3.down * checkDistance);
            Gizmos.DrawWireSphere(endPosition, sphereRadius);
        }
    }
}
