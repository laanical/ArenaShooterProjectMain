using UnityEngine;

// This script manages the player's weapons, handling swapping between them.
public class WeaponManager : MonoBehaviour
{
    // A list of all weapon GameObjects the player can use.
    // Assign your weapon GameObjects (e.g., your pistol, your axe) to this list in the Inspector.
    public GameObject[] weapons;

    // The index of the currently active weapon in the 'weapons' array.
    private int currentWeaponIndex = 0;

    void Start()
    {
        // On start, select the first weapon in the list (index 0).
        SelectWeapon(currentWeaponIndex);
    }

    void Update()
    {
        // --- Weapon Swapping Input ---
        
        // Example using number keys (1, 2, 3, etc.)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectWeapon(0); // Select first weapon
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectWeapon(1); // Select second weapon
        }
        
        // Example using mouse scroll wheel
        if (Input.GetAxis("Mouse ScrollWheel") > 0f) // Scroll up
        {
            SelectNextWeapon();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f) // Scroll down
        {
            SelectPreviousWeapon();
        }
    }

    // Activates the weapon at the given index and deactivates all others.
    void SelectWeapon(int weaponIndex)
    {
        // Ensure the selected index is valid for our weapons array.
        if (weaponIndex < 0 || weaponIndex >= weapons.Length)
        {
            Debug.LogWarning("Tried to select an invalid weapon index: " + weaponIndex);
            return;
        }

        // Set the new current weapon index.
        currentWeaponIndex = weaponIndex;

        // Loop through all weapons.
        for (int i = 0; i < weapons.Length; i++)
        {
            // Activate the weapon if its index matches the selected index,
            // otherwise, deactivate it.
            weapons[i].SetActive(i == currentWeaponIndex);
        }
        
        Debug.Log("Selected weapon: " + weapons[currentWeaponIndex].name);
    }

    void SelectNextWeapon()
    {
        int nextWeaponIndex = (currentWeaponIndex + 1) % weapons.Length;
        SelectWeapon(nextWeaponIndex);
    }

    void SelectPreviousWeapon()
    {
        int previousWeaponIndex = (currentWeaponIndex - 1 + weapons.Length) % weapons.Length;
        SelectWeapon(previousWeaponIndex);
    }
}
