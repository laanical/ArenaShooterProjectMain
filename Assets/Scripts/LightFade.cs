// --- LightFade.cs ---
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFade : MonoBehaviour
{
    public float fadeDuration = 0.3f;
    private Light pointLight;
    private float initialIntensity;
    private float timer;

    void Awake()
    {
        pointLight = GetComponent<Light>();
        initialIntensity = pointLight.intensity;
    }

    void Update()
    {
        if (pointLight == null) return;

        timer += Time.deltaTime;
        float progress = timer / fadeDuration;
        // Smoothly decrease intensity from its starting value to zero.
        pointLight.intensity = Mathf.Lerp(initialIntensity, 0f, progress);
    }
}