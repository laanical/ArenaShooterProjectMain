using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Manages all player-related UI elements.
public class PlayerUI : MonoBehaviour
{
    [Header("Health & Dash References")]
    public TextMeshProUGUI healthText;
    public Slider dashCooldownSlider;

    [Header("Jump UI References")]
    [Tooltip("The Image components for the jump indicator dots.")]
    public Image[] jumpDots; // NEW: An array to hold the dot images
    public Color jumpAvailableColor = Color.white; // NEW: Color for an available jump
    public Color jumpUsedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // NEW: Color for a used jump (dark grey, semi-transparent)

    [Header("Player References")]
    public PlayerHealth playerHealth;
    public SimplePlayerController playerController;

    void OnEnable()
    {
        // Subscribe to events
        PlayerHealth.OnHealthChanged += UpdateHealthUI;
        SimplePlayerController.OnJumpsChanged += UpdateJumpUI;
    }

    void OnDisable()
    {
        // Unsubscribe from events
        PlayerHealth.OnHealthChanged -= UpdateHealthUI;
        SimplePlayerController.OnJumpsChanged -= UpdateJumpUI;
    }

    void Start()
    {
        // Initial setup validation
        if (playerHealth == null || playerController == null)
        {
            Debug.LogError("PlayerUI: PlayerHealth or SimplePlayerController not assigned!", this);
            enabled = false;
            return;
        }
        if (healthText == null || dashCooldownSlider == null || jumpDots.Length == 0)
        {
            Debug.LogError("PlayerUI: A UI reference has not been assigned!", this);
            enabled = false;
            return;
        }

        // Initialize the UI with the starting values.
        UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.maxHealth);
        UpdateJumpUI(playerController.JumpsRemaining, playerController.MaxAirJumps);
        dashCooldownSlider.value = 1;
    }

    void Update()
    {
        UpdateDashUI();
    }

    private void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"HP: {currentHealth} / {maxHealth}";
        }
    }

    // --- REVISED JUMP UI LOGIC ---
    // This method now updates the color of the dots.
    private void UpdateJumpUI(int remainingJumps, int maxJumps)
    {
        // Loop through all the dot images we have.
        for (int i = 0; i < jumpDots.Length; i++)
        {
            // If the dot's index is less than the number of jumps we have left,
            // it means this jump is available. Set it to the bright color.
            if (i < remainingJumps)
            {
                jumpDots[i].color = jumpAvailableColor;
            }
            // Otherwise, this jump has been used. Set it to the dim color.
            else
            {
                jumpDots[i].color = jumpUsedColor;
            }
        }
    }

    private void UpdateDashUI()
    {
        if (playerController != null && dashCooldownSlider != null)
        {
            float cooldownProgress = 1 - (playerController.DashCooldownTimer / playerController.dashCooldown);
            dashCooldownSlider.value = cooldownProgress;
        }
    }
}
