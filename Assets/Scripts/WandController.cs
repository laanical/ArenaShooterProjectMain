using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum SpellType
{
    Projectile,
    Beam,
    ChargedProjectile
}

[System.Serializable]
public class Spell
{
    public string name;
    public GameObject projectilePrefab; 
    public SpellType type = SpellType.Projectile;
    public float minChargeTime = 0.5f;
    public float maxChargeTime = 2.0f;
}


public class WandController : MonoBehaviour
{
    [Header("Spell Settings")]
    public List<Spell> spells = new List<Spell>();
    
    [Header("Weapon Stats")]
    public float attackSpeed = 2f;

    [Header("Common References")]
    public Transform projectileSpawnPoint;
    public float maxShootingDistance = 100f;

    public static event Action<int> OnSpellChanged;

    private Camera mainCamera;
    private float nextFireTime = 0f;
    private int currentSpellIndex = 0;
    private GameObject activeBeam = null;

    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private GameObject activeChargeObject = null;
    private ChargeVFXController activeChargeController = null;
    private PoisonBombProjectile activeProjectileController = null;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("WandController: Main Camera not found.", this);
            enabled = false;
        }
    }
    
    void OnEnable()
    {
        OnSpellChanged?.Invoke(currentSpellIndex);
    }
    
    void OnDisable()
    {
        if (activeBeam != null)
        {
            Destroy(activeBeam);
        }
        if (isCharging)
        {
            CancelCharge();
        }
        OnSpellChanged?.Invoke(-1);
    }

    void Update()
    {
        HandleInput();

        if (activeBeam != null)
        {
            activeBeam.transform.position = projectileSpawnPoint.position;
        }

        if (isCharging)
        {
            currentChargeTime += Time.deltaTime;

            if (activeChargeController != null)
            {
                Spell currentSpell = spells[currentSpellIndex];
                float chargeProgress = Mathf.Clamp01(currentChargeTime / currentSpell.maxChargeTime);
                activeChargeController.UpdateChargeVisual(chargeProgress);
            }
        }
    }

    private void HandleInput()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSpell(i);
                break; 
            }
        }
        
        if (spells.Count <= currentSpellIndex) return;
        Spell currentSpell = spells[currentSpellIndex];

        switch (currentSpell.type)
        {
            // --- [THE FIX] ---
            // This logic was broken. It now correctly calls a dedicated
            // method for simple, non-charged projectiles.
            case SpellType.Projectile:
                if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + (1f / attackSpeed);
                    FireSimpleProjectile(currentSpell);
                }
                break;

            // This logic was also missing. It's been restored.
            case SpellType.Beam:
                if (Input.GetMouseButtonDown(0))
                {
                    if (activeBeam == null)
                    {
                        activeBeam = Instantiate(currentSpell.projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    if (activeBeam != null)
                    {
                        Destroy(activeBeam);
                    }
                }
                break;
            // --- [END FIX] ---

            case SpellType.ChargedProjectile:
                if (Input.GetMouseButtonDown(0) && !isCharging)
                {
                    StartCharge(currentSpell);
                }
                else if (Input.GetMouseButtonUp(0) && isCharging)
                {
                    ReleaseCharge(currentSpell);
                }
                break;
        }
    }

    private void SelectSpell(int index)
    {
        if (activeBeam != null) Destroy(activeBeam);
        if (isCharging) CancelCharge();
        
        if (index >= 0 && index < spells.Count)
        {
            currentSpellIndex = index;
            OnSpellChanged?.Invoke(currentSpellIndex);
        }
    }
    
    // --- [NEW] ---
    // A dedicated method for simple, non-charged projectiles.
    private void FireSimpleProjectile(Spell spellToFire)
    {
        if (spellToFire.projectilePrefab == null) return;
        
        Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;
        if (Physics.Raycast(cameraRay, out RaycastHit hitInfo, maxShootingDistance))
        {
            targetPoint = hitInfo.point;
        }
        else
        {
            targetPoint = cameraRay.GetPoint(maxShootingDistance);
        }

        Vector3 direction = (targetPoint - projectileSpawnPoint.position).normalized;
        Instantiate(spellToFire.projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(direction));
    }
    // --- [END NEW] ---

    private void StartCharge(Spell spell)
    {
        isCharging = true;
        currentChargeTime = 0f;

        activeChargeObject = Instantiate(spell.projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation, projectileSpawnPoint);
        
        activeChargeController = activeChargeObject.GetComponent<ChargeVFXController>();
        activeProjectileController = activeChargeObject.GetComponent<PoisonBombProjectile>();

        if (activeChargeObject.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (activeChargeController != null)
        {
            activeChargeController.UpdateChargeVisual(0f);
        }
    }

    private void ReleaseCharge(Spell spell)
    {
        if (currentChargeTime >= spell.minChargeTime)
        {
            float chargePower = Mathf.Clamp01(currentChargeTime / spell.maxChargeTime);
            
            if (activeProjectileController != null)
            {
                activeChargeObject.transform.SetParent(null);

                if (activeChargeController != null)
                {
                    activeChargeController.enabled = false;
                }

                activeProjectileController.Initialize(chargePower);
            }
        }
        else
        {
            Debug.Log("Charge released too early. Fizzled.");
            Destroy(activeChargeObject);
        }
        
        isCharging = false;
        activeChargeObject = null;
        activeChargeController = null;
        activeProjectileController = null;
    }

    private void CancelCharge()
    {
        isCharging = false;
        currentChargeTime = 0f;
        if (activeChargeObject != null)
        {
            Destroy(activeChargeObject);
        }
        activeChargeObject = null;
        activeChargeController = null;
        activeProjectileController = null;
    }
}
