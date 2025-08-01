using UnityEngine;

// This script controls the visual behavior of a charging spell effect.
// It now handles both scaling and moving the effect forward based on charge progress.
public class ChargeVFXController : MonoBehaviour
{
    [Header("Scaling")]
    [Tooltip("The initial scale of the effect at 0% charge.")]
    public Vector3 minScale = new Vector3(0.1f, 0.1f, 0.1f);
    [Tooltip("The final scale of the effect at 100% charge.")]
    public Vector3 maxScale = new Vector3(1.0f, 1.0f, 1.0f);

    [Header("Position Offset")]
    [Tooltip("The starting local position of the effect at 0% charge.")]
    public Vector3 minPosition = Vector3.zero;
    [Tooltip("The final local position of the effect at 100% charge. The Z axis controls the distance FORWARD from the wand tip.")]
    public Vector3 maxPosition = new Vector3(0, 0, 0.5f);

    /// <summary>
    /// Updates the visual scale and position of the effect based on the charge progress.
    /// This is called by the WandController every frame during the charge.
    /// </summary>
    /// <param name="chargeProgress">A value from 0.0 to 1.0 representing the charge level.</param>
    public void UpdateChargeVisual(float chargeProgress)
    {
        // We use Lerp (Linear Interpolation) to find the scale between min and max
        // based on the charge percentage.
        transform.localScale = Vector3.Lerp(minScale, maxScale, chargeProgress);

        // We do the same for the local position. Because this script is on an object
        // that is a child of the wand's spawn point, changing its 'localPosition'
        // moves it relative to that spawn point. The Z axis is the "forward" direction.
        transform.localPosition = Vector3.Lerp(minPosition, maxPosition, chargeProgress);
    }
}
