using UnityEngine;

// This script handles the firing logic for a simple projectile-based weapon like a wand.
public class WandController : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("The projectile prefab to be fired.")]
    public GameObject projectilePrefab;
    [Tooltip("The point from which the projectile is fired (e.g., the tip of the wand).")]
    public Transform projectileSpawnPoint;
    [Tooltip("The number of shots the wand can fire per second. Higher is faster.")]
    public float attackSpeed = 2f; // Changed from fireRate to attackSpeed

    [Header("Aiming")]
    [Tooltip("The maximum distance the aiming ray will check for targets.")]
    public float maxShootingDistance = 100f;

    // --- Private Fields ---
    private Camera mainCamera;
    private float nextFireTime = 0f;

    void Start()
    {
        // Find and store a reference to the main camera for aiming calculations.
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("WandController: Main Camera not found. Ensure your player camera is tagged 'MainCamera'.", this);
            enabled = false; // Disable the script if no camera is found.
        }

        // Safety checks for required components.
        if (projectilePrefab == null) Debug.LogError("WandController: Projectile Prefab not assigned.", this);
        if (projectileSpawnPoint == null) Debug.LogError("WandController: Projectile Spawn Point not assigned.", this);
    }

    void Update()
    {
        // Check for fire input
        if (Input.GetMouseButtonDown(0))
        {
            // Check if the cooldown has passed.
            if (Time.time >= nextFireTime)
            {
                // --- REVISED: Calculate cooldown based on attacks per second ---
                // We check if attackSpeed is positive to avoid division by zero errors.
                if (attackSpeed > 0)
                {
                    // Set the time for the next available shot. The delay is 1 / attacks per second.
                    nextFireTime = Time.time + (1f / attackSpeed);
                    Fire();
                }
            }
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || mainCamera == null) return;

        // --- Aiming Logic ---
        // 1. Create a ray from the center of the camera's viewport.
        Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        // 2. Perform a raycast to see what the camera is looking at.
        if (Physics.Raycast(cameraRay, out RaycastHit hitInfo, maxShootingDistance))
        {
            // If the ray hits something, the target is the exact point of impact.
            targetPoint = hitInfo.point;
        }
        else
        {
            // If the ray hits nothing, the target is a point far in the distance.
            targetPoint = cameraRay.GetPoint(maxShootingDistance);
        }

        // --- Firing Logic ---
        // 3. Calculate the direction from the wand's tip to the target point.
        Vector3 directionToTarget = (targetPoint - projectileSpawnPoint.position).normalized;

        // 4. Instantiate the projectile at the spawn point, rotated to face the calculated direction.
        Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(directionToTarget));
    }
}
