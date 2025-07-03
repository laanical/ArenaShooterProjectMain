using UnityEngine;

// Makes the GameObject this script is attached to always face the main camera.
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // Find the main camera in the scene.
        // Ensure your main player camera is tagged as "MainCamera" in the Inspector.
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Billboard: Main Camera not found. Make sure your player camera is tagged 'MainCamera'.");
            enabled = false; // Disable script if no camera found.
        }
    }

    void LateUpdate()
    {
        // If a main camera is found, make this GameObject's forward direction
        // point towards the camera.
        if (mainCamera != null)
        {
            // Option 1: Simple look at (can sometimes cause flipping if camera goes directly above/below)
            // transform.LookAt(mainCamera.transform);

            // Option 2: More robust - aligns rotation with camera but keeps its own up vector
            // This makes it face the camera's position but doesn't tilt with the camera's pitch.
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

            // Option 3 (Often best for UI Billboarding):
            // Make the Canvas face the same direction as the camera.
            // transform.rotation = mainCamera.transform.rotation;
        }
    }
}
