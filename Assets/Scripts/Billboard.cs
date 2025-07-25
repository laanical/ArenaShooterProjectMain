using UnityEngine;

// Makes the GameObject this script is attached to always face the main camera.
public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // Find the main camera in the scene and store its transform.
        // This is slightly more efficient than calling Camera.main every frame.
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Billboard: Main Camera not found. Make sure your player camera is tagged 'MainCamera'.");
            enabled = false; // Disable script if no camera found.
        }
    }

    // LateUpdate is called after all other Update functions.
    // This is the best place to adjust camera-facing logic to avoid jitter.
    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // This is the best method for UI. It makes the health bar's rotation
            // exactly match the camera's rotation. It won't tilt or skew, it will
            // just stay perfectly flat and face the same direction as the camera.
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}
