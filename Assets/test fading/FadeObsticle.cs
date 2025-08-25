using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeObsticle : MonoBehaviour
{
    public Material xrayMaterialTemplate;
    public float fadeDuration = 0.2f;
    public float targetAlpha = 0.3f;

    private class FadingObject
    {
        public Renderer renderer;
        public Material originalMaterial;
        public Material xrayMaterial;
        public Coroutine currentCoroutine;
    }

    private Dictionary<Renderer, FadingObject> tracked = new Dictionary<Renderer, FadingObject>();

    public void HandleObstructions(List<Renderer> obstructingRenderers)
    {
        HashSet<Renderer> currentHits = new HashSet<Renderer>(obstructingRenderers);

        foreach (Renderer rend in currentHits)
        {

            // hit = apply xray material and start fade out
            if (!tracked.ContainsKey(rend))
            {
                Material original = rend.material;
                Material xray = new Material(xrayMaterialTemplate);
                xray.color = new Color(xray.color.r, xray.color.g, xray.color.b, 1f);
                rend.material = xray;

                FadingObject fading = new FadingObject
                {
                    renderer = rend,
                    originalMaterial = original,
                    xrayMaterial = xray,
                    currentCoroutine = StartCoroutine(FadeMaterial(xray, 1f, targetAlpha))
                };

                tracked[rend] = fading;
            }
        }

        // Revert objects when no longer blocking
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var kvp in tracked)
        {
            if (!currentHits.Contains(kvp.Key))
            {
                if (kvp.Value.currentCoroutine != null)
                    StopCoroutine(kvp.Value.currentCoroutine);

                kvp.Value.currentCoroutine = StartCoroutine(RestoreMaterial(kvp.Value));
                toRemove.Add(kvp.Key);
            }
        }

        // Cleanup
        foreach (Renderer r in toRemove)
        {
            tracked.Remove(r);
        }
    }

    // Fades the object's material alpha from start to end
    private IEnumerator FadeMaterial(Material mat, float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = mat.color;

        while (elapsed < fadeDuration)
        {
            float a = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            mat.color = new Color(color.r, color.g, color.b, a);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mat.color = new Color(color.r, color.g, color.b, endAlpha);
    }

    // Restores the material by fading back and swapping original
    private IEnumerator RestoreMaterial(FadingObject fading)
    {
        Material mat = fading.xrayMaterial;
        float currentAlpha = mat.color.a;

        yield return FadeMaterial(mat, currentAlpha, 1f);
        fading.renderer.material = fading.originalMaterial;
    }
}
