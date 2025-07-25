using UnityEngine;

public class GunFireController : MonoBehaviour
{
    // --- Shell Ejection Variables ---
    public GameObject shellPrefab;
    public Transform shellEjectionPoint;
    public float shellEjectionForce = 0.7f;
    public float shellEjectionUpwardForce = 0.2f;
    public float shellEjectionTorque = 0.5f;

    // --- Projectile Firing Variables ---
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint; // Muzzle of the gun
    public float fireRate = 0.2f;

    // --- Animator Variables ---
    public string shootAnimationStateName = "Shoot_Edit";
    private Animator weaponAnimator;

    // --- Targeting Variables ---
    private Camera mainCamera;
    public float maxShootingDistance = 100f; // How far the camera's aiming raycast will check
    public float debugRayLength = 50f; // Length for debug visualization rays

    // --- Internal Cooldown ---
    private float nextFireTime = 0f;

    void Start()
    {
        weaponAnimator = GetComponent<Animator>();
        if (weaponAnimator == null)
        {
            Debug.LogError("GunFireController: Animator component not found!", this.gameObject);
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("GunFireController: Main Camera not found. Make sure your player camera is tagged 'MainCamera'.", this.gameObject);
            enabled = false;
            return;
        }

        // Safety checks
        if (shellPrefab == null) Debug.LogWarning("GunFireController: Shell Prefab not assigned.", this.gameObject);
        if (shellEjectionPoint == null) Debug.LogWarning("GunFireController: Shell Ejection Point not assigned.", this.gameObject);
        if (projectilePrefab == null) Debug.LogWarning("GunFireController: Projectile Prefab not assigned.", this.gameObject);
        if (projectileSpawnPoint == null) Debug.LogWarning("GunFireController: Projectile Spawn Point not assigned.", this.gameObject);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            if (fireRate > 0) nextFireTime = Time.time + fireRate;
            else nextFireTime = Time.time; 

            if (weaponAnimator != null)
            {
                weaponAnimator.Play(shootAnimationStateName, 0, 0f);
            }
            FireActualProjectile();
        }
    }

    void FireActualProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || mainCamera == null)
        {
            return;
        }

        // Step 1: Determine what the camera (crosshair) is looking at.
        Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Ray from center of screen
        RaycastHit hitInfo; 
        Vector3 targetPoint;

        if (Physics.Raycast(cameraRay, out hitInfo, maxShootingDistance))
        {
            // If the ray hits something, target the *exact point* of impact.
            targetPoint = hitInfo.point; 
            Debug.Log("<color=green>SUCCESSFUL RAYCAST:</color> Crosshair ray hit: " + hitInfo.collider.name + " (Tag: " + hitInfo.collider.tag + "). Targeting exact hit point: " + targetPoint.ToString("F3"));
            // Visualize ray from camera to the exact hit point
            Debug.DrawRay(cameraRay.origin, (targetPoint - cameraRay.origin), Color.yellow, 3.0f);
        }
        else
        {
            // If the ray doesn't hit anything, aim at a point far along the camera's direction.
            targetPoint = cameraRay.GetPoint(maxShootingDistance);
            Debug.Log("<color=orange>RAYCAST MISS:</color> Crosshair ray hit nothing within " + maxShootingDistance + "m. Targeting point in distance: " + targetPoint.ToString("F3"));
            Debug.DrawRay(cameraRay.origin, cameraRay.direction * maxShootingDistance, Color.blue, 3.0f);
        }

        // Step 2: Calculate the direction from the gun's muzzle to this targetPoint.
        Vector3 directionToTarget = (targetPoint - projectileSpawnPoint.position).normalized;

        // Visualize the projectile's intended path from muzzle to target point
        float distanceToTarget = Vector3.Distance(projectileSpawnPoint.position, targetPoint);
        Debug.DrawRay(projectileSpawnPoint.position, directionToTarget * Mathf.Min(distanceToTarget, debugRayLength), Color.red, 3.0f);
        
        // Step 3: Instantiate the projectile at the muzzle, rotated to face the targetPoint.
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(directionToTarget));
    }

    public void EjectShell()
    {
        if (shellPrefab == null || shellEjectionPoint == null) return;
        GameObject spawnedShell = Instantiate(shellPrefab, shellEjectionPoint.position, shellEjectionPoint.rotation);
        Rigidbody shellRb = spawnedShell.GetComponent<Rigidbody>();
        if (shellRb != null)
        {
            Vector3 ejectionDirection = (shellEjectionPoint.right * shellEjectionForce) + (shellEjectionPoint.up * shellEjectionUpwardForce);
            shellRb.AddForce(ejectionDirection, ForceMode.Impulse);
            if (shellEjectionTorque > 0)
            {
                Vector3 randomTorque = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                shellRb.AddTorque(randomTorque.normalized * shellEjectionTorque, ForceMode.Impulse);
            }
        }
        else Debug.LogWarning("Spawned shell prefab is missing a Rigidbody component!", spawnedShell);
    }
}
