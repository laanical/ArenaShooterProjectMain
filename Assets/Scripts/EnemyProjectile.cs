using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 10;
    public float lifespan = 4.0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Set the projectile's initial velocity. It will fly in the direction it's facing when spawned.
        rb.velocity = transform.forward * speed;

        // Destroy the projectile after a set time to clean up the scene if it doesn't hit anything.
        Destroy(gameObject, lifespan);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the object we hit has a PlayerHealth component.
        // Using CompareTag is a quick way to avoid checking every single object.
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // If it's the player, deal damage.
                playerHealth.TakeDamage(damage);
                Debug.Log("Enemy projectile hit the player!");
            }
        }
        
        // Destroy the projectile after it hits anything (player, wall, floor, etc.).
        Destroy(gameObject);
    }
}