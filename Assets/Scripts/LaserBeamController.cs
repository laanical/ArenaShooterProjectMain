using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LaserBeamController : MonoBehaviour
{
    [Header("Laser Settings")]
    public int damagePerTick = 5;
    public float damageTickRate = 10f;
    public int maxPierces = 2;
    public float maxRange = 100f;

    [Header("Visuals")]
    public Transform beamCylinder;
    public Transform outerBeamCylinder;
    public float beamWidth = 0.2f;
    public float textureScrollSpeed = 2f;
    public ParticleSystem embersParticles;
    public GameObject impactVFXPrefab;

    [Header("Aiming")]
    [Tooltip("The minimum distance for the aim target. Prevents buggy aiming when close to walls.")]
    public float minAimDistance = 1.5f; // NEW: Minimum distance for aiming

    [Header("System")]
    public LayerMask enemyLayer;

    private float tickTimer;
    private List<EnemyHealth> enemiesHitThisTick = new List<EnemyHealth>();
    private GameObject activeImpactVFX;
    private Camera mainCamera;
    private Material beamMaterialInstance;
    private Material outerBeamMaterialInstance;

    void Awake()
    {
        if (beamCylinder != null)
        {
            beamMaterialInstance = beamCylinder.GetComponent<MeshRenderer>().material;
            beamCylinder.gameObject.SetActive(false);
        }
        if (outerBeamCylinder != null)
        {
            outerBeamMaterialInstance = outerBeamCylinder.GetComponent<MeshRenderer>().material;
            outerBeamCylinder.gameObject.SetActive(false);
        }
        mainCamera = Camera.main;
    }

    void OnDestroy()
    {
        if (activeImpactVFX != null)
        {
            foreach (var ps in activeImpactVFX.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop();
            }
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(cameraRay, out RaycastHit cameraHit, maxRange))
        {
            targetPoint = cameraHit.point;
        }
        else
        {
            targetPoint = cameraRay.GetPoint(maxRange);
        }

        // --- THE FIX ---
        // Check the distance from the BEAM'S ORIGIN (the wand tip) to the target point.
        float distanceToTarget = Vector3.Distance(transform.position, targetPoint);
        if (distanceToTarget < minAimDistance)
        {
            // If the target is too close, override it and just aim straight forward from the camera.
            targetPoint = mainCamera.transform.position + cameraRay.direction * maxRange;
        }
        // --- END OF FIX ---

        Vector3 beamDirection = (targetPoint - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(beamDirection);

        Vector3 startPoint = transform.position;
        RaycastHit[] hits = Physics.RaycastAll(startPoint, transform.forward, maxRange)
                                   .OrderBy(h => h.distance)
                                   .ToArray();
        int pierces = 0;
        Vector3 endPoint = startPoint + transform.forward * maxRange;
        List<EnemyHealth> currentTargets = new List<EnemyHealth>();

        foreach (var hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
            {
                if (pierces < maxPierces)
                {
                    pierces++;
                    if (hit.collider.TryGetComponent<EnemyHealth>(out var enemy))
                    {
                        currentTargets.Add(enemy);
                    }
                }
                else
                {
                    endPoint = hit.point;
                    break;
                }
            }
            else
            {
                endPoint = hit.point;
                break;
            }
        }
        
        UpdateCylinderBeam(startPoint, endPoint);
        UpdateImpactVFX(endPoint);
        
        if (beamMaterialInstance != null)
        {
            float offset = Time.time * -textureScrollSpeed;
            beamMaterialInstance.SetVector("_Texture_Offset", new Vector2(0, offset));
        }
        if (outerBeamMaterialInstance != null)
        {
            float offset = Time.time * -(textureScrollSpeed * 0.75f);
            outerBeamMaterialInstance.SetVector("_Texture_Offset", new Vector2(0, offset));
        }
        
        tickTimer += Time.deltaTime;
        if (tickTimer >= 1f / damageTickRate)
        {
            tickTimer = 0f;
            enemiesHitThisTick.Clear();

            foreach (var enemy in currentTargets)
            {
                if (enemy != null && !enemiesHitThisTick.Contains(enemy))
                {
                    enemy.TakeDamage(damagePerTick);
                    enemiesHitThisTick.Add(enemy);
                }
            }
        }
    }
    
    void UpdateCylinderBeam(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);

        if (beamCylinder != null)
        {
            beamCylinder.localRotation = Quaternion.Euler(90, 0, 0);
            beamCylinder.localPosition = new Vector3(0, 0, distance / 2f);
            beamCylinder.localScale = new Vector3(beamWidth, distance / 2f, beamWidth);

            if (!beamCylinder.gameObject.activeSelf)
            {
                beamCylinder.gameObject.SetActive(true);
            }
            if (beamMaterialInstance != null)
            {
                beamMaterialInstance.SetVector("_Texture_Tiling", new Vector2(1, distance));
            }
        }

        if (outerBeamCylinder != null)
        {
            outerBeamCylinder.localRotation = Quaternion.Euler(90, 0, 0);
            outerBeamCylinder.localPosition = new Vector3(0, 0, distance / 2f);
            outerBeamCylinder.localScale = new Vector3(beamWidth * 1.2f, distance / 2f, beamWidth * 1.2f);

            if (!outerBeamCylinder.gameObject.activeSelf)
            {
                outerBeamCylinder.gameObject.SetActive(true);
            }
            if (outerBeamMaterialInstance != null)
            {
                outerBeamMaterialInstance.SetVector("_Texture_Tiling", new Vector2(2, distance));
            }
        }

        if (embersParticles != null)
        {
            var shape = embersParticles.shape;
            shape.length = distance;
        }
    }

    void UpdateImpactVFX(Vector3 impactPoint)
    {
        if (impactVFXPrefab == null) return;

        if (activeImpactVFX == null)
        {
            activeImpactVFX = Instantiate(impactVFXPrefab, impactPoint, Quaternion.identity);
        }
        else
        {
            activeImpactVFX.transform.position = impactPoint;
        }
    }
}
