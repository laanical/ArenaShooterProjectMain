using UnityEngine;
// If you plan to use a UI to show health, you'll need this.
// using UnityEngine.UI; 
// To reload the scene on death, you need this.
using UnityEngine.SceneManagement;

// Manages the player's health and handles what happens when the player dies.
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    // Optional: A reference to a UI health bar or text.
    // public Slider healthBar; 

    // Called when the script instance is being loaded.
    void Awake()
    {
        currentHealth = maxHealth;
        // if (healthBar != null) healthBar.value = currentHealth;
    }

    // A public method that can be called by other scripts (like projectiles) to deal damage.
    public void TakeDamage(int damageAmount)
    {
        // Don't do anything if the player is already dead.
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        Debug.Log($"Player took {damageAmount} damage. Current health: {currentHealth}/{maxHealth}");

        // Update UI if you have one
        // if (healthBar != null) healthBar.value = currentHealth;

        if (currentHealth <= 0)
        {
            currentHealth = 0; // Ensure health doesn't go below zero.
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        
        // --- Game Over Logic ---
        // For now, we will simply reload the current scene.
        // You could also show a "Game Over" screen, etc.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
