using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    public float speed = 15f; // Example for a slower sphere, adjust as needed
    public float lifespan = 3.0f; 
    public int damage = 25; // Damage this projectile will do

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("ProjectileBehavior: Rigidbody component not found!", this.gameObject);
        }
    }

    void Start()
    {
        Destroy(gameObject, lifespan);

        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Projectile hit: {collision.gameObject.name} with tag: {collision.gameObject.tag}");

        // Try to get the EnemyHealth component from the object we collided with.
        EnemyHealth enemy = collision.gameObject.GetComponent<EnemyHealth>();

        // If the object has an EnemyHealth component, it's an enemy.
        if (enemy != null)
        {
            // Call the TakeDamage method on the enemy.
            enemy.TakeDamage(damage);
            Debug.Log($"Dealt {damage} damage to {collision.gameObject.name}");
        }
        // Alternative check using tags (if you prefer or for objects without specific health scripts):
        // if (collision.gameObject.CompareTag("Enemy"))
        // {
        //     // Handle hitting a generic "Enemy" tagged object
        // }


        // Destroy the projectile after hitting something.
        // You might want to add more specific conditions here later,
        // e.g., don't destroy if it hits another projectile or the player immediately.
        Destroy(gameObject);
    }
}