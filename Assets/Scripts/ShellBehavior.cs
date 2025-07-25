using UnityEngine;
using System.Collections; // Required for using Coroutines

// This script controls the behavior of an ejected shell casing.
public class ShellBehavior : MonoBehaviour
{
    [Header("Settings")]
    public float lifespan = 10.0f; // Increased lifespan so we can see it stop
    public float spinForce = 10f;

    [Header("Stopping Behavior")]
    [Tooltip("Time in seconds before we force the shell to stop moving.")]
    public float timeUntilSleep = 2.5f;
    [Tooltip("How quickly the shell stops after the sleep timer. Higher is faster.")]
    public float sleepDrag = 5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("ShellBehavior: Rigidbody component not found!", this);
            enabled = false;
        }
    }

    void Start()
    {
        // Destroy the shell casing after its lifespan to clean up the scene.
        Destroy(gameObject, lifespan);

        // Apply some initial random spin to make the ejection look dynamic.
        if (rb != null && spinForce > 0)
        {
            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            );
            rb.AddTorque(randomTorque.normalized * spinForce, ForceMode.Impulse);
        }

        // --- NEW: Start the coroutine to make the shell go to sleep ---
        StartCoroutine(GoToSleep());
    }

    /// <summary>
    /// After a delay, this coroutine increases the Rigidbody's drag
    /// to make the shell come to a complete stop naturally.
    /// </summary>
    private IEnumerator GoToSleep()
    {
        // Wait for the specified amount of time.
        yield return new WaitForSeconds(timeUntilSleep);

        // After waiting, if the shell still exists...
        if (rb != null)
        {
            // Dramatically increase the linear and angular drag.
            // This simulates friction and air resistance, stopping the shell.
            rb.drag = sleepDrag;
            rb.angularDrag = sleepDrag;
        }
    }
}
