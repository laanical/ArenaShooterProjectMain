using UnityEngine;
using System; // Required for using Actions/Events
using System.Collections.Generic; // Required for using Lists

// This helper class is used to organize spell data in the Inspector.
// It's not a component, just a data container.
[System.Serializable]
public class Spell
{
    public string name;
    public GameObject projectilePrefab;
    // We can add more spell-specific data here later, like mana cost, cooldown, etc.
}

// This script handles firing and swapping between a list of available spells.
public class WandController : MonoBehaviour
{
    [Header("Spell Settings")]
    [Tooltip("The list of spells this wand can use.")]
    public List<Spell> spells = new List<Spell>();
    
    [Header("Weapon Stats")]
    [Tooltip("The number of shots the wand can fire per second.")]
    public float attackSpeed = 2f;

    [Header("Common References")]
    [Tooltip("The point from which projectiles are fired.")]
    public Transform projectileSpawnPoint;
    [Tooltip("The maximum distance the aiming ray will check for targets.")]
    public float maxShootingDistance = 100f;

    // --- Public Events ---
    // This event will notify the UI when the spell has changed.
    public static event Action<int> OnSpellChanged;

    // --- Private State ---
    private Camera mainCamera;
    private float nextFireTime = 0f;
    private int currentSpellIndex = 0;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("WandController: Main Camera not found.", this);
            enabled = false;
        }

        // Safety checks
        if (spells.Count == 0) Debug.LogError("WandController: No spells assigned in the Inspector!", this);
        if (projectileSpawnPoint == null) Debug.LogError("WandController: Projectile Spawn Point not assigned.", this);
    }

    // This function is called by WeaponManager when this weapon is selected.
    void OnEnable()
    {
        // When the wand becomes active, immediately notify the UI of the current spell.
        OnSpellChanged?.Invoke(currentSpellIndex);
    }

    // This function is called by WeaponManager when this weapon is deselected.
    void OnDisable()
    {
        // When the wand is put away, we can tell the UI to hide the spell bar.
        // We pass -1 to indicate no spell is active.
        OnSpellChanged?.Invoke(-1);
    }

    void Update()
    {
        // This script's Update only runs when the Wand GameObject is active.
        HandleInput();
    }

    private void HandleInput()
    {
        // --- Spell Selection Input ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSpell(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSpell(1);
        // Add more here for Alpha3, Alpha4, etc. if you add more spells.

        // --- Firing Input ---
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            if (attackSpeed > 0)
            {
                nextFireTime = Time.time + (1f / attackSpeed);
                Fire();
            }
        }
    }

    private void SelectSpell(int index)
    {
        // Check if the selected spell index is valid for our list of spells.
        if (index >= 0 && index < spells.Count)
        {
            currentSpellIndex = index;
            Debug.Log("Switched to spell: " + spells[currentSpellIndex].name);
            
            // Fire the event to notify the UI that the spell has changed.
            OnSpellChanged?.Invoke(currentSpellIndex);
        }
    }

    private void Fire()
    {
        // Check if we have any spells to begin with.
        if (spells.Count == 0 || spells[currentSpellIndex].projectilePrefab == null)
        {
            Debug.LogError("No projectile prefab assigned for the current spell!");
            return;
        }

        GameObject projectileToFire = spells[currentSpellIndex].projectilePrefab;

        // --- Aiming Logic ---
        Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(cameraRay, out RaycastHit hitInfo, maxShootingDistance))
        {
            targetPoint = hitInfo.point;
        }
        else
        {
            targetPoint = cameraRay.GetPoint(maxShootingDistance);
        }

        // --- Firing Logic ---
        Vector3 directionToTarget = (targetPoint - projectileSpawnPoint.position).normalized;
        Instantiate(projectileToFire, projectileSpawnPoint.position, Quaternion.LookRotation(directionToTarget));
    }
}
