using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class VignetteProximityManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Volume postProcessVolume;

    [Header("Distance Settings")]
    public float maxDistance = 15f;
    public float minDistance = 2f;

    [Header("Vignette Settings")]
    public float baseIntensity = 0.25f;
    public float maxIntensity = 0.75f;

    [Header("Chromatic Aberration Settings")]
    public float baseAbberation = 0.242f;
    public float maxExtraAbberation = 0.6f;

    [Header("Dynamic Pulse")]
    public float pulseSpeed = 7f;
    public float pulseAmount = 0.08f;

    private Vignette vignette;
    private ChromaticAberration chromatic;
    private float currentLerpedIntensity;

    void Start()
    {
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out chromatic);
        }

        if (vignette != null) vignette.intensity.overrideState = true;
        if (chromatic != null) chromatic.intensity.overrideState = true;
    }

    void Update()
    {
        if (player == null) return;

        float closestDist = GetClosestEnemyDistance();
        float panicFactor = 1f - Mathf.InverseLerp(minDistance, maxDistance, closestDist);
        float pulse = Mathf.Pow(Mathf.Sin(Time.time * pulseSpeed), 4) * pulseAmount * panicFactor;

        // --- Handle Vignette ---
        if (vignette != null)
        {
            float targetVignette = Mathf.Lerp(baseIntensity, maxIntensity, panicFactor) + pulse;
            currentLerpedIntensity = Mathf.Lerp(currentLerpedIntensity, targetVignette, Time.deltaTime * 4f);
            vignette.intensity.value = Mathf.Clamp01(currentLerpedIntensity);
            vignette.color.value = Color.Lerp(Color.black, new Color(0.4f, 0.05f, 0.05f), panicFactor);
        }

        // --- Handle Chromatic Aberration ---
        if (chromatic != null)
        {
            float targetAbberation = baseAbberation + (panicFactor * maxExtraAbberation) + (pulse * 2f);
            chromatic.intensity.value = Mathf.Clamp01(targetAbberation);
        }
    }

    float GetClosestEnemyDistance()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float min = Mathf.Infinity;

        if (enemies.Length == 0) return Mathf.Infinity;

        foreach (GameObject e in enemies)
        {
            if (e == null) continue;
            float d = Vector3.Distance(player.position, e.transform.position);
            if (d < min) min = d;
        }
        return min;
    }

    private void OnDrawGizmos()
    {
        if (player == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, maxDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, minDistance);
    }
}