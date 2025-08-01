
using UnityEngine;
using System.Collections;

public class ExpansionBurstVFX : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The size the sphere will be when it first appears.")]
    public float initialSize = 0.1f; // <-- [NEW]
    [Tooltip("The final diameter the sphere will expand to.")]
    public float maxSize = 10f;
    [Tooltip("How long it takes to expand to full size.")]
    public float expansionDuration = 0.15f;

    [Header("Visuals")]
    [Tooltip("The color of the sphere at the start of the expansion.")]
    public Color startColor = new Color(0.5f, 0f, 1f, 0.8f);
    [Tooltip("The color the sphere will fade to at the end.")]
    public Color endColor = new Color(0.5f, 0f, 1f, 0f);

    private MaterialPropertyBlock propBlock;
    private Renderer sphereRenderer;

    void Awake()
    {
        sphereRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (sphereRenderer == null)
        {
            Debug.LogError("ExpansionBurstVFX requires a Renderer component!", this);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(AnimateBurst());
    }

    private IEnumerator AnimateBurst()
    {
        float timer = 0f;

        while (timer < expansionDuration)
        {
            float progress = timer / expansionDuration;

            // --- [MODIFIED] Growth now starts from the initialSize ---
            float currentSize = Mathf.SmoothStep(initialSize, maxSize, progress);
            transform.localScale = new Vector3(currentSize, currentSize, currentSize);

            Color currentColor = Color.Lerp(startColor, endColor, progress);
            
            sphereRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", currentColor);
            sphereRenderer.SetPropertyBlock(propBlock);

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
