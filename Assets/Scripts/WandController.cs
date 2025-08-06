using UnityEngine;
using System;
using System.Collections.Generic;

public enum SpellType
{
    Projectile,
    Beam,
    ChargedProjectile,
    Shotgun
}

[System.Serializable]
public class Spell
{
    [Header("General")]
    public string name;
    public SpellType type = SpellType.Projectile;
    public GameObject projectilePrefab;

    [Header("Attack")]
    [Tooltip("Shots or ticks per second, or scales the charge time for charge spells.")]
    public float attackSpeed = 2.0f;

    [Header("Charged Spell Settings")]
    public float minChargeTime = 0.5f;
    public float maxChargeTime = 2.0f;
}

public class WandController : MonoBehaviour
{
    [Header("Spell Settings")]
    public List<Spell> spells = new List<Spell>();

    [Header("Common References")]
    public Transform projectileSpawnPoint;
    public float maxShootingDistance = 100f;

    [Header("Projectile Spawn Safety")]
    [SerializeField] private LayerMask projectileBlockMask; // Assign to Player + Environment in inspector
    [SerializeField] private float minProjectileSpawnDistance = 0.2f; // 20cm out from wand

    [Header("Magic Circle VFX")]
    public MagicCircleVFX magicCircleVFX; // <-- Add this in Inspector

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

    private float curMinCharge = 0f;
    private float curMaxCharge = 0f;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("WandController: Main Camera not found.", this);
            enabled = false;
        }

        // Make sure the magic circle starts hidden
        if (magicCircleVFX != null)
        {
            magicCircleVFX.gameObject.SetActive(false);
            magicCircleVFX.SetSpinning(false);
        }
    }

    void OnEnable()
    {
        nextFireTime = 0f;
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

        // Make sure to hide VFX when disabled
        if (magicCircleVFX != null)
        {
            magicCircleVFX.SetSpinning(false);
            magicCircleVFX.gameObject.SetActive(false);
        }
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
                float chargeProgress = Mathf.Clamp01(currentChargeTime / curMaxCharge);
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

        // --- MAGIC CIRCLE VFX LOGIC ---
        if (magicCircleVFX != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                magicCircleVFX.gameObject.SetActive(true);
                magicCircleVFX.SetSpinning(true);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                magicCircleVFX.SetSpinning(false);
                magicCircleVFX.gameObject.SetActive(false);
            }
        }
        // --- END VFX LOGIC ---

        if (spells.Count <= currentSpellIndex) return;
        Spell currentSpell = spells[currentSpellIndex];

        switch (currentSpell.type)
        {
            case SpellType.Projectile:
                if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + (1f / currentSpell.attackSpeed);
                    FireSimpleProjectile(currentSpell);
                }
                break;

            case SpellType.Beam:
                if (Input.GetMouseButtonDown(0))
                {
                    if (activeBeam == null)
                    {
                        activeBeam = Instantiate(currentSpell.projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
                        var beamScript = activeBeam.GetComponent<LaserBeamController>();
                        if (beamScript != null)
                        {
                            beamScript.damageTickRate = currentSpell.attackSpeed;
                        }
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

            case SpellType.Shotgun:
                if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + (1f / currentSpell.attackSpeed);
                    FireShotgun(currentSpell);
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

    private void FireSimpleProjectile(Spell spellToFire)
    {
        if (spellToFire.projectilePrefab == null) return;

        // Get firing direction based on camera
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
        Quaternion rotation = Quaternion.LookRotation(direction);

        // Use safe spawn!
        Vector3 safeSpawn = ProjectileUtility.GetSafeProjectileSpawnPoint(
            projectileSpawnPoint.position, direction, minProjectileSpawnDistance, projectileBlockMask
        );

        Instantiate(spellToFire.projectilePrefab, safeSpawn, rotation);
    }

    private void FireShotgun(Spell spellToFire)
    {
        var projectileScript = spellToFire.projectilePrefab?.GetComponent<IceShardProjectile>();
        if (projectileScript == null)
        {
            Debug.LogError("The assigned projectile prefab for the shotgun spell is missing the 'IceShardProjectile' script!");
            return;
        }
        int projectileCount = projectileScript.projectileCount;
        float horizontalSpread = projectileScript.horizontalSpreadAngle;
        float verticalSpread = projectileScript.verticalSpreadAngle;

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
        Vector3 centralDirection = (targetPoint - projectileSpawnPoint.position).normalized;
        Quaternion centralRotation = Quaternion.LookRotation(centralDirection);

        for (int i = 0; i < projectileCount; i++)
        {
            float randomHorizontal = UnityEngine.Random.Range(-horizontalSpread, horizontalSpread);
            float randomVertical = UnityEngine.Random.Range(-verticalSpread, verticalSpread);
            Quaternion spreadRotation = Quaternion.Euler(randomVertical, randomHorizontal, 0);
            Quaternion finalRotation = centralRotation * spreadRotation;

            Vector3 spreadDirection = finalRotation * Vector3.forward;

            // Use safe spawn!
            Vector3 shotgunSafeSpawn = ProjectileUtility.GetSafeProjectileSpawnPoint(
                projectileSpawnPoint.position, spreadDirection, minProjectileSpawnDistance, projectileBlockMask
            );

            Instantiate(spellToFire.projectilePrefab, shotgunSafeSpawn, finalRotation);
        }
    }

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

        // Scale charge window by attack speed for THIS charge
        curMinCharge = spell.minChargeTime / spell.attackSpeed;
        curMaxCharge = spell.maxChargeTime / spell.attackSpeed;
        // Clamp: don't go below 0.05s min or invert min/max by mistake
        curMinCharge = Mathf.Max(0.05f, curMinCharge);
        curMaxCharge = Mathf.Max(curMinCharge, curMaxCharge);
    }

    private void ReleaseCharge(Spell spell)
    {
        if (currentChargeTime >= curMinCharge)
        {
            float chargePower = Mathf.Clamp01(currentChargeTime / curMaxCharge);
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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(projectileSpawnPoint.position, 0.02f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(projectileSpawnPoint.position, projectileSpawnPoint.position + projectileSpawnPoint.forward * minProjectileSpawnDistance);
        }
    }
#endif
}
