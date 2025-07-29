using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// A completely self-contained melee weapon system.
// This script handles attack input, cooldown, animation speed, and the damage hurtbox.
public class SimpleMeleeController : MonoBehaviour
{
    [Header("Core Settings")]
    [Tooltip("The number of attacks per second.")]
    public float attackSpeed = 1f;
    [Tooltip("The damage dealt by a single hit.")]
    public int attackDamage = 25;
    [Tooltip("The radius of the damage-checking sphere.")]
    public float attackRadius = 0.7f;

    [Header("References")]
    [Tooltip("The Animator component for the weapon.")]
    public Animator weaponAnimator;
    [Tooltip("The AnimationClip of the swing animation.")]
    public AnimationClip attackAnimation;
    [Tooltip("The point from which the attack sphere is cast.")]
    public Transform attackPoint;
    [Tooltip("Which layers should be considered enemies.")]
    public LayerMask enemyLayers;
    [Tooltip("The exact name of the Idle state in your Animator.")]
    public string idleStateName = "Idle"; // Make sure this matches your Animator's Idle state name

    // --- Private State ---
    private float cooldownTimer = 0f;
    private bool isAttacking = false;
    private List<Collider> enemiesHitThisSwing;

    void Start()
    {
        if (weaponAnimator == null) Debug.LogError("Animator not assigned!", this);
        if (attackAnimation == null) Debug.LogError("Attack Animation not assigned!", this);
        if (attackPoint == null) Debug.LogError("Attack Point not assigned!", this);

        enemiesHitThisSwing = new List<Collider>();
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (Input.GetMouseButtonDown(0) && !isAttacking && cooldownTimer <= 0)
        {
            Attack();
        }
    }

    private void Attack()
    {
        isAttacking = true;
        cooldownTimer = 1f / attackSpeed;
        
        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        // --- Animation Phase ---
        weaponAnimator.speed = attackSpeed;
        weaponAnimator.Play(attackAnimation.name, 0, 0f);

        // --- Hurtbox Phase ---
        yield return new WaitForSeconds(0.05f / attackSpeed);

        enemiesHitThisSwing.Clear(); 
        float hurtboxActiveTime = (attackAnimation.length * 0.7f) / attackSpeed;
        float hurtboxTimer = 0f;

        while (hurtboxTimer < hurtboxActiveTime)
        {
            PerformDamageCheck();
            hurtboxTimer += Time.deltaTime;
            yield return null; 
        }

        // --- Cooldown Phase ---
        float remainingAnimTime = (attackAnimation.length / attackSpeed) - hurtboxActiveTime - (0.05f / attackSpeed);
        if(remainingAnimTime > 0)
        {
            yield return new WaitForSeconds(remainingAnimTime);
        }
        
        // --- Return to Idle ---
        // The sequence is over. Explicitly tell the Animator to return to the Idle state.
        weaponAnimator.Play(idleStateName);
        weaponAnimator.speed = 1f; // Reset animator speed to normal.
        isAttacking = false; // Unlock the attack.
    }

    private void PerformDamageCheck()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            if (!enemiesHitThisSwing.Contains(enemy))
            {
                enemiesHitThisSwing.Add(enemy);
                
                if (enemy.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
                {
                    enemyHealth.TakeDamage(attackDamage);
                }
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
