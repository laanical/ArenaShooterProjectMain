using UnityEngine;

// Handles first-person camera look: player body yaw and camera pitch.
public class CameraController : MonoBehaviour
{
    // Sensitivity for mouse movement.
    public float mouseSensitivityX = 2.0f;
    public float mouseSensitivityY = 2.0f;

    // Limits for vertical camera rotation (pitch).
    public float minYAngle = -80.0f;
    public float maxYAngle = 80.0f;

    // Reference to the player's body transform (the parent object).
    // Assign this in the Inspector by dragging the PlayerCharacter GameObject here.
    public Transform playerBody;

    // Current vertical rotation of the camera.
    private float pitch = 0.0f;

    void Start()
    {
        // Ensure playerBody is assigned.
        if (playerBody == null)
        {
            // Try to get it from the parent if this script is on the camera and camera is child of player.
            if (transform.parent != null)
            {
                playerBody = transform.parent;
                Debug.Log("CameraController: playerBody automatically assigned to parent: " + playerBody.name);
            }
            else
            {
                Debug.LogError("CameraController: Player Body Transform not assigned! Please assign the player's main GameObject to the 'Player Body' slot.");
                enabled = false; // Disable script if no player body.
                return;
            }
        }

        // Lock and hide cursor.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (playerBody == null) return;

        // Get mouse input.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

        // --- Player Body Rotation (Yaw - Left/Right) ---
        // Rotate the playerBody around its Y-axis based on horizontal mouse movement.
        playerBody.Rotate(Vector3.up * mouseX);

        // --- Camera Rotation (Pitch - Up/Down) ---
        // Adjust the pitch based on vertical mouse movement.
        pitch -= mouseY; // Subtract because mouseY is often inverted for looking up/down.
        // Clamp the pitch to prevent over-rotation (looking straight up/down or flipping).
        pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);

        // Apply the pitch to the camera's local X-axis rotation.
        // We use localRotation because the camera is a child of the playerBody,
        // so we only want to rotate it up/down relative to the playerBody's orientation.
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
