using UnityEngine;
using UnityEngine.UI; // Required for UI components

// This script manages the UI for the wand's spell action bar.
public class SpellUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent GameObject for the entire spell bar, used to show/hide it.")]
    public GameObject spellBarParent;
    [Tooltip("An array of Image components used as highlights for the selected spell.")]
    public Image[] spellSlotHighlights;

    void OnEnable()
    {
        // Subscribe to the event from WandController when this component is enabled.
        WandController.OnSpellChanged += UpdateSpellUI;
    }

    void OnDisable()
    {
        // It's crucial to unsubscribe when the component is disabled to prevent errors.
        WandController.OnSpellChanged -= UpdateSpellUI;
    }

    void Start()
    {
        // Initial validation to ensure everything is set up in the Inspector.
        if (spellBarParent == null || spellSlotHighlights.Length == 0)
        {
            Debug.LogError("SpellUI: UI references are not assigned in the Inspector!", this);
            // Hide the bar on start if it's not configured correctly.
            if(spellBarParent != null) spellBarParent.SetActive(false);
            enabled = false;
        }
        else
        {
            // Start with the spell bar hidden. It will be shown when the wand is equipped.
            spellBarParent.SetActive(false);
        }
    }

    // This method is called automatically whenever the OnSpellChanged event is fired from the WandController.
    private void UpdateSpellUI(int selectedSpellIndex)
    {
        // If the index is -1, it means the wand has been unequipped. Hide the entire bar.
        if (selectedSpellIndex == -1)
        {
            spellBarParent.SetActive(false);
            return;
        }

        // If we received a valid index, first make sure the spell bar is visible.
        spellBarParent.SetActive(true);

        // Loop through all the highlight images.
        for (int i = 0; i < spellSlotHighlights.Length; i++)
        {
            // Check if the current highlight corresponds to the selected spell index.
            if (i == selectedSpellIndex)
            {
                // If it's the selected one, turn its highlight on.
                spellSlotHighlights[i].enabled = true;
            }
            else
            {
                // Otherwise, turn its highlight off.
                spellSlotHighlights[i].enabled = false;
            }
        }
    }
}
