using UnityEngine;

// Handles first-person camera look, crouching, and prevents clipping through walls.
public class CameraController : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float mouseSensitivityX = 2.0f;
    public float mouseSensitivityY = 2.0f;

    [Header("Look Clamping")]
    public float minYAngle = -89.9f;
    public float maxYAngle = 89.9f;

    [Header("References")]
    public Transform playerBody;

    [Header("Camera Collision")]
    [Tooltip("The layers the camera should collide with and be pushed by.")]
    public LayerMask collisionMask;
    [Tooltip("How far the camera should be from the wall it collides with to prevent clipping.")]
    public float collisionOffset = 0.1f;
    [Tooltip("A small sphere is cast to represent the camera's volume. This helps prevent clipping at corners.")]
    public float cameraRadius = 0.1f;

    // --- Private Fields ---
    private float pitch = 0.0f;
    private Vector3 initialLocalPosition;
    
    // --- NEW: References for crouching ---
    private SimplePlayerController playerController;
    private Vector3 standingCameraPos;
    private Vector3 crouchingCameraPos;
    public float crouchTransitionSpeed = 10f; // We need a transition speed here now

    void Start()
    {
        if (playerBody == null)
        {
            if (transform.parent != null) playerBody = transform.parent;
            else
            {
                Debug.LogError("CameraController: Player Body Transform not assigned!", this);
                enabled = false;
                return;
            }
        }
        
        // NEW: Get the player controller component
        playerController = playerBody.GetComponent<SimplePlayerController>();
        if (playerController == null)
        {
            Debug.LogError("CameraController could not find SimplePlayerController script on PlayerBody!", this);
            enabled = false;
            return;
        }

        // Store the camera's starting position.
        initialLocalPosition = transform.localPosition;

        // NEW: Define the standing and crouching local positions based on values from the player controller.
        standingCameraPos = initialLocalPosition;
        crouchingCameraPos = new Vector3(
            initialLocalPosition.x,
            initialLocalPosition.y - (playerController.standingHeight - playerController.crouchingHeight),
            initialLocalPosition.z
        );

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Use LateUpdate for ALL camera work to ensure it happens after player movement.
    void LateUpdate()
    {
        HandleLook();
        HandlePositionAndCollision();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;
        
        playerBody.Rotate(Vector3.up * mouseX);
        
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);
        
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandlePositionAndCollision()
    {
        // NEW: Determine the target local position based on the player's crouch state.
        Vector3 targetLocalPos = playerController.IsCrouching ? crouchingCameraPos : standingCameraPos;

        // NEW: Smoothly transition the camera's local position.
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * crouchTransitionSpeed);

        // --- Collision logic now works with the smoothly transitioned position ---
        
        // 1. Define the camera's ideal position in world space based on its CURRENT local position.
        Vector3 idealPosition = playerBody.TransformPoint(transform.localPosition);

        // 2. Define the ray for the cast, starting from the player's pivot.
        Ray ray = new Ray(playerBody.position, idealPosition - playerBody.position);
        float rayDistance = Vector3.Distance(playerBody.position, idealPosition);

        // 3. Perform the cast.
        if (Physics.SphereCast(ray, cameraRadius, out RaycastHit hit, rayDistance, collisionMask))
        {
            // 4. If we hit something, move the camera to the impact point.
            transform.position = ray.GetPoint(hit.distance - collisionOffset);
        }
        else
        {
            // 5. If we don't hit anything, we can safely use the ideal position.
            // Since we already set the localPosition, we don't need to do anything here,
            // but we can ensure it's set to the world position for clarity.
            transform.position = idealPosition;
        }
    }
}
