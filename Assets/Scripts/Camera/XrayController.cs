using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XrayController : MonoBehaviour
{
    //for swapping origin material to xray material when player blocked by something.

    public Transform player; //raycast from camera to whom?
    public LayerMask obstructionMask; //all objeccts within this layer will be affected
    public Material xrayMaterialTemplate; // the xray material
    public float fadeDuration = 0f; 
    public float targetAlpha = 0f;

    // Tracks objects currently faded
    private class FadingObject
    {
        public Renderer renderer;
        public Material originalMaterial;
        public Material xrayMaterial;
        public Coroutine currentCoroutine;
    }

    private Dictionary<Renderer, FadingObject> trackedObjects = new Dictionary<Renderer, FadingObject>();

    void Update()
    {
        // Cast ray from camera to player
        Vector3 dir = player.position - transform.position;
        float distance = dir.magnitude;

        // Collect all objects currently blocking view  
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir.normalized, distance, obstructionMask);
        HashSet<Renderer> hitRenderers = new HashSet<Renderer>();

        foreach (var hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend == null) continue;

            hitRenderers.Add(rend);

            // hit = apply xray material and start fade out
            if (!trackedObjects.ContainsKey(rend))
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

                trackedObjects[rend] = fading;
            }
        }

        // Revert objects when no longer blocking
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var kvp in trackedObjects)
        {
            if (!hitRenderers.Contains(kvp.Key))
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
            trackedObjects.Remove(r);
        }
    }

    // Fades the object's material alpha from start to end
    IEnumerator FadeMaterial(Material mat, float startAlpha, float endAlpha)
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
    IEnumerator RestoreMaterial(FadingObject fading)
    {
        Material mat = fading.xrayMaterial;
        float currentAlpha = mat.color.a;

        yield return FadeMaterial(mat, currentAlpha, 1f);
        fading.renderer.material = fading.originalMaterial;
    }
}
