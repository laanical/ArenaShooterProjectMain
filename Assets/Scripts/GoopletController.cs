using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class GoopletController : MonoBehaviour
{
    [Header("Splat Visuals")]
    public float splatSize = 0.8f;
    public float splatAnimationDuration = 0.1f;

    [Header("Lifetime")]
    public float lifetime = 8f;
    public float fadeDuration = 2f;

    [Header("Collision")]
    public LayerMask stickableLayers;
    public LayerMask enemyLayer;
    public float groundSearchRadius = 10f;

    // --- Private State ---
    private Renderer goopRenderer;
    private MaterialPropertyBlock propBlock;
    private Color startColor;
    private Rigidbody rb;
    private Collider col;
    private bool hasStuck = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        goopRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (goopRenderer != null && goopRenderer.material.HasProperty("_BaseColor"))
        {
            startColor = goopRenderer.material.GetColor("_BaseColor");
        }
        else
        {
            Debug.LogError("GoopletController requires a Renderer with a material that has a '_BaseColor' property.", this);
            Destroy(gameObject, 0.1f);
        }
        
        Destroy(gameObject, lifetime + fadeDuration + 10f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasStuck) return;
        int hitLayer = collision.gameObject.layer;

        if ((stickableLayers.value & (1 << hitLayer)) != 0)
        {
            hasStuck = true;
            rb.isKinematic = true;
            col.enabled = false;
            ContactPoint contact = collision.contacts[0];
            StartCoroutine(AnimateSplat(contact.point, contact.normal));
        }
        else if ((enemyLayer.value & (1 << hitLayer)) != 0)
        {
            hasStuck = true;
            col.enabled = false;
            StartCoroutine(HandleEnemyCollision());
        }
    }
    
    private IEnumerator HandleEnemyCollision()
    {
        // --- [THE FIX for ClosestPoint Error] ---
        // Instead of using OverlapSphere and ClosestPoint which can fail on non-convex meshes,
        // we now use a simple, reliable raycast straight down.
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundSearchRadius, stickableLayers))
        {
            // We found ground directly below the enemy.
            rb.isKinematic = true;
            yield return StartCoroutine(AnimateSplat(hit.point, hit.normal));
        }
        else
        {
            // No ground found below. Re-enable physics and let it fall naturally.
            rb.isKinematic = false;
            col.enabled = true;
            hasStuck = false; // Allow it to try sticking again on its next collision.
        }
    }

    private IEnumerator AnimateSplat(Vector3 splatPosition, Vector3 surfaceNormal)
    {
        transform.position = splatPosition + surfaceNormal * 0.01f;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
        transform.Rotate(0, Random.Range(0, 360), 0, Space.Self);

        float timer = 0f;
        Vector3 initialScale = transform.localScale;
        Vector3 finalScale = new Vector3(splatSize, 0.01f, splatSize);

        while (timer < splatAnimationDuration)
        {
            transform.localScale = Vector3.Lerp(initialScale, finalScale, timer / splatAnimationDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = finalScale;
        yield return new WaitForSeconds(lifetime);

        timer = 0f;
        if (goopRenderer != null && goopRenderer.material.HasProperty("_BaseColor"))
        {
            while (timer < fadeDuration)
            {
                float alpha = Mathf.Lerp(startColor.a, 0f, timer / fadeDuration);
                propBlock.SetColor("_BaseColor", new Color(startColor.r, startColor.g, startColor.b, alpha));
                goopRenderer.SetPropertyBlock(propBlock);
                timer += Time.deltaTime;
                yield return null;
            }
        }
        
        Destroy(gameObject);
    }
}
