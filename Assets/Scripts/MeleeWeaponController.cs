using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This script handles a melee weapon with a damage zone active for a duration.
// VERSION 6: Adds a manual trigger reset to prevent animation re-firing at high speed.
public class MeleeWeaponController : MonoBehaviour
{
    // --- Attack Parameters ---
    public int attackDamage = 40;
    [Tooltip("The number of swings the weapon can perform per second. Higher is faster.")]
    public float attackSpeed = 1.0f;
    public float attackRadius = 0.5f;

    // --- Attack Timing ---
    [Header("Attack Timing (Based on original animation)")]
    [Tooltip("The delay in seconds from starting the attack until the damage window opens.")]
    public float damageWindowStartDelay = 0.2f;
    [Tooltip("The duration in seconds that the damage window stays open.")]
    public float damageWindowDuration = 0.3f;

    [Header("Animation")]
    [Tooltip("The animation clip for the attack. This is required for the new lock timing.")]
    public AnimationClip attackAnimation; // You must assign this in the Inspector

    // --- References ---
    public Transform attackPoint;
    public LayerMask enemyLayers;
    private Animator weaponAnimator;
    
    // --- Animator Parameter Hashes ---
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");
    private readonly int attackSpeedMultiplierHash = Animator.StringToHash("AttackSpeedMultiplier");

    // --- Internal State ---
    private List<EnemyHealth> enemiesHitThisSwing;
    private bool isCurrentlyAttacking = false;

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
            return;
        }

        if (attackAnimation == null)
        {
            Debug.LogError("MeleeWeaponController: Attack Animation clip not assigned in the Inspector!", this.gameObject);
            enabled = false;
            return;
        }

        enemiesHitThisSwing = new List<EnemyHealth>();
        // DEBUG: Announce that this script instance is ready.
        Debug.Log($"MeleeWeaponController with ID {GetInstanceID()} has started on {gameObject.name}");
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (!isCurrentlyAttacking && Input.GetMouseButtonDown(0))
        {
            if (attackSpeed > 0)
            {
                Attack();
            }
        }
    }

    void Attack()
    {
        // DEBUG: Log the attack call with the instance ID.
        Debug.Log($"Attack() called on instance {GetInstanceID()}. Locking attack.", this.gameObject);
        
        CancelInvoke(nameof(UnlockAttack));

        isCurrentlyAttacking = true;
        weaponAnimator.SetFloat(attackSpeedMultiplierHash, attackSpeed);

        // --- THE FIX ---
        // Manually reset the trigger to clear any lingering state from the previous frame.
        // This prevents the animator from re-triggering the animation immediately.
        weaponAnimator.ResetTrigger(attackTriggerHash);
        // Now, set the trigger to start the animation for the current attack.
        weaponAnimator.SetTrigger(attackTriggerHash);
        
        StartCoroutine(DamageWindowCoroutine());

        float unlockDelay = attackAnimation.length / attackSpeed;
        Invoke(nameof(UnlockAttack), unlockDelay);
    }
    
    private void UnlockAttack()
    {
        // DEBUG: Log the unlock call with the instance ID.
        Debug.Log($"UnlockAttack() called on instance {GetInstanceID()}. Unlocking attack.", this.gameObject);
        isCurrentlyAttacking = false;
    }

    private IEnumerator DamageWindowCoroutine()
    {
        yield return new WaitForSeconds(damageWindowStartDelay / attackSpeed);
        enemiesHitThisSwing.Clear();
        float damageWindowEndTime = Time.time + (damageWindowDuration / attackSpeed);
        
        while (Time.time < damageWindowEndTime)
        {
            PerformDamageCheck();
            yield return new WaitForFixedUpdate();
        }
    }
    
    private void PerformDamageCheck()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayers);

        foreach (Collider enemyCollider in hitEnemies)
        {
            EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !enemiesHitThisSwing.Contains(enemyHealth))
            {
                enemyHealth.TakeDamage(attackDamage);
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