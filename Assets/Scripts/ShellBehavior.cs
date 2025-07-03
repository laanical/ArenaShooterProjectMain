using UnityEngine;

// This script controls the behavior of an ejected shell casing.
public class ShellBehavior : MonoBehaviour
{
    // How long the shell will exist in the scene before being destroyed (in seconds).
    public float lifespan = 4.0f;

    // Optional: Initial force to give the shell a bit more of a "kick" outwards.
    // This will be *in addition* to the force applied by the gun.
    // You can often leave these at 0 if the gun's ejection force is enough.
    public float additionalEjectionForce = 0.01f; // A very small kick
    public float additionalUpwardsForce = 0.02f;  // A slight upward kick

    // Optional: Torque for spin
    public float spinForce = 10f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Destroy the shell casing after 'lifespan' seconds.
        Destroy(gameObject, lifespan);

        // --- Optional: Apply a small initial local force and torque ---
        // This helps make the ejection look a bit more dynamic and less uniform if needed.
        // The main ejection force will come from the gun script when it's spawned.

        if (rb != null)
        {
            // Apply a small force in the shell's local right direction (often outward from ejection port)
            if (additionalEjectionForce > 0)
            {
                rb.AddRelativeForce(Vector3.right * additionalEjectionForce, ForceMode.Impulse);
            }

            // Apply a small force in the shell's local up direction
            if (additionalUpwardsForce > 0)
            {
                rb.AddRelativeForce(Vector3.up * additionalUpwardsForce, ForceMode.Impulse);
            }

            // Apply some random spin
            if (spinForce > 0)
            {
                Vector3 randomTorque = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                );
                rb.AddTorque(randomTorque.normalized * spinForce, ForceMode.Impulse);
            }
        }
    }

    // Optional: You could add OnCollisionEnter logic here if you want shells
    // to make a sound when they hit something, for example.
    // void OnCollisionEnter(Collision collision)
    // {
    //     // if (collision.relativeVelocity.magnitude > 0.5f) // Only play sound if it hits with some force
    //     // {
    //     //     // Play a shell impact sound
    //     // }
    // }
}