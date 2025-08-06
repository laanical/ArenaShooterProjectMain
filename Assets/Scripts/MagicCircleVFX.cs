using UnityEngine;

public class MagicCircleVFX : MonoBehaviour
{
    public float spinSpeed = 180f; // degrees per second

    private bool spinning = false;

    void Update()
    {
        if (spinning)
        {
            // Spin around the local Z axis
            transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);
        }
    }

    public void SetSpinning(bool isSpinning)
    {
        spinning = isSpinning;
    }
}
