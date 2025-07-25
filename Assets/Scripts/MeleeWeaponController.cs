using UnityEngine;
using System.Collections.Generic;

// This script handles a melee weapon with a damage zone active for a duration.
public class MeleeWeaponController : MonoBehaviour
{
    // --- Attack Parameters ---
    public int attackDamage = 40;
    public float attackRate = 1.0f; 
    public float attackRange = 1.5f;
    public float attackRadius = 0.5f;

    // --- References ---
    public Transform attackPoint;
    public LayerMask enemyLayers;
    private Animator weaponAnimator;
    
    // --- Animator Parameter Hash ---
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");

    // --- Internal State ---
    private float nextAttackTime = 0f;
    private bool isDamageZoneActive = false;
    private List<EnemyHealth> enemiesHitThisSwing; // List to track enemies already hit in a single swing

    void Start()
    {
        weaponAnimator = GetComponent<Animator>();
        if (weaponAnimator == null)
        {
            Debug.LogError("MeleeWeaponController: Animator component not found!", this.gameObject);
            enabled = false;
            return;
        }

        if (attackPoint == null)
        {
            Debug.LogError("MeleeWeaponController: Attack Point transform not assigned!", this.gameObject);
            enabled = false;
        }

        // Initialize the list of enemies hit
        enemiesHitThisSwing = new List<EnemyHealth>();
    }

    void Update()
    {
        // Only check for input if the weapon is active and visible
        if (!gameObject.activeInHierarchy) return;

        // Check for attack input
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + 1f / attackRate;
            Attack();
        }

        // If the damage zone is active, perform the damage check every frame.
        if (isDamageZoneActive)
        {
            PerformDamageCheck();
        }
    }

    void Attack()
    {
        weaponAnimator.SetTrigger(attackTriggerHash);
    }

    // This method is called by the START animation event for a swing's damage window.
    public void StartDamageCheck()
    {
        Debug.Log("Damage Zone ON");
        isDamageZoneActive = true;
        enemiesHitThisSwing.Clear(); // Clear the list of hit enemies for this new swing
    }

    // This method is called by the END animation event for a swing's damage window.
    public void EndDamageCheck()
    {
        Debug.Log("Damage Zone OFF");
        isDamageZoneActive = false;
    }

    // This method is now called every frame while the damage zone is active.
    private void PerformDamageCheck()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayers);

        foreach (Collider enemyCollider in hitEnemies)
        {
            EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
            // If the collider has an EnemyHealth script and we haven't hit it yet in this swing
            if (enemyHealth != null && !enemiesHitThisSwing.Contains(enemyHealth))
            {
                Debug.Log("We hit " + enemyCollider.name);
                enemyHealth.TakeDamage(attackDamage);
                // Add the enemy to our list so we only damage them once per swing.
                enemiesHitThisSwing.Add(enemyHealth); 
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
