using UnityEngine;
using System; // Required for using Actions/Events
using UnityEngine.SceneManagement; // Required for reloading the scene

// Manages the player's health, handles taking damage, and notifies other scripts of changes.
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int _currentHealth;

    // This static event broadcasts the current and max health whenever health changes.
    // The UI script will listen for this broadcast.
    public static event Action<int, int> OnHealthChanged;

    public int CurrentHealth
    {
        get { return _currentHealth; }
        private set
        {
            _currentHealth = Mathf.Clamp(value, 0, maxHealth);

            // When health is set, fire the event to notify any listeners (like the UI).
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
    }

    void Awake()
    {
        // Set health to full on startup. Using the property ensures the event fires.
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        CurrentHealth -= damageAmount;
        Debug.Log($"Player took {damageAmount} damage. Current health: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        CurrentHealth += healAmount;
        Debug.Log($"Player healed {healAmount}. Current health: {CurrentHealth}/{maxHealth}");
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        // Reload the current scene as a simple game over mechanic.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
