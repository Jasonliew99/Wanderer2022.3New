using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeObsticle : MonoBehaviour
{
    [Header("Material Settings")]
    public Material xrayMaterial;

    private Renderer[] renderers;
    private Material[] originalMaterials;
    private bool isFaded = false;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }
    }

    public void SetFaded(bool faded)
    {
        if (faded && !isFaded)
        {
            // Switch to X-Ray material
            foreach (var r in renderers)
                r.material = xrayMaterial;
            isFaded = true;
        }
        else if (!faded && isFaded)
        {
            // Restore original material
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material = originalMaterials[i];
            isFaded = false;
        }
    }
}
