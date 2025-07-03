using UnityEngine;
using TMPro; // Required for TextMeshPro

// Manages the health of an enemy and updates its health display.
public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    // Assign this in the Inspector by dragging the TextMeshPro child object here.
    public TextMeshProUGUI healthTextDisplay; // Changed from TextMeshPro to TextMeshProUGUI for UI text

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Ensure the health display is assigned
        if (healthTextDisplay == null)
        {
            Debug.LogWarning("EnemyHealth: HealthTextDisplay (TextMeshProUGUI) not assigned in the Inspector for " + gameObject.name + ". Finding it automatically...");
            // Try to find it on a child object if not assigned.
            // This is a fallback, direct assignment in Inspector is better.
            healthTextDisplay = GetComponentInChildren<TextMeshProUGUI>();
            if (healthTextDisplay == null)
            {
                 Debug.LogError("EnemyHealth: Could not find TextMeshProUGUI component on children of " + gameObject.name);
            }
        }
        UpdateHealthDisplay();
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Debug.Log($"{gameObject.name} took {damageAmount} damage. Current health: {currentHealth}/{maxHealth}");
        UpdateHealthDisplay();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died!");

        // --- NEW CODE TO ADD ---
        // Find the EnemySpawner in the scene and call its EnemyDefeated() method.
        // The 'instance' is a static variable, so we can access it directly from the class.
        if (EnemySpawner.instance != null)
        {
            EnemySpawner.instance.EnemyDefeated();
        }
        // --- END OF NEW CODE ---

        // Optionally hide the health display immediately
        if (healthTextDisplay != null && healthTextDisplay.transform.parent != null) // Check parent canvas
        {
            healthTextDisplay.transform.parent.gameObject.SetActive(false); // Hide the canvas
        }
        Destroy(gameObject);
    }

    void UpdateHealthDisplay()
    {
        if (healthTextDisplay != null)
        {
            healthTextDisplay.text = currentHealth.ToString();
        }
    }
}
