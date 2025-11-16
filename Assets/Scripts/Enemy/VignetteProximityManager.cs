using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteProximityManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Volume postProcessVolume;

    [Header("Vignette Settings")]
    public float baseIntensity = 0.25f;
    public float maxExtraIntensity = 0.35f;
    public float minDistance = 2f;   // distance at which vignette is max
    public float maxDistance = 12f;  // distance at which vignette is back to baseline
    public float smoothSpeed = 3f;   // smoothing for intensity
    public float centerSmoothSpeed = 3f; // smoothing for vignette center

    private Vignette vignette;
    private Camera mainCam;
    private float currentIntensity;
    private Vector2 currentCenter;

    void Start()
    {
        if (postProcessVolume == null)
        {
            Debug.LogError("Missing Post Process Volume!");
            enabled = false;
            return;
        }

        postProcessVolume.profile.TryGet(out vignette);
        mainCam = Camera.main;

        currentIntensity = baseIntensity;
        currentCenter = new Vector2(0.5f, 0.5f); // default center
        vignette.intensity.value = currentIntensity;
        vignette.center.value = currentCenter;
    }

    void Update()
    {
        if (player == null || vignette == null) return;

        PlayerTracker[] enemies = FindObjectsOfType<PlayerTracker>();

        Transform closestEnemy = null;
        float closestDist = float.MaxValue;

        // Find closest enemy
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(player.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = enemy.transform;
            }
        }

        // Calculate target intensity
        float targetIntensity = baseIntensity;

        if (closestEnemy != null && closestDist <= maxDistance)
        {
            float t = Mathf.InverseLerp(maxDistance, minDistance, closestDist);
            t = Mathf.Clamp01(t); // clamp so it never goes beyond 0–1
            targetIntensity = baseIntensity + maxExtraIntensity * t;
        }

        // Smoothly interpolate intensity
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothSpeed);
        vignette.intensity.value = currentIntensity;

        // Smoothly update vignette center
        Vector2 targetCenter = new Vector2(0.5f, 0.5f);
        if (closestEnemy != null && mainCam != null)
        {
            Vector3 screenPos = mainCam.WorldToViewportPoint(closestEnemy.position);
            targetCenter = new Vector2(screenPos.x, screenPos.y);
        }

        currentCenter = Vector2.Lerp(currentCenter, targetCenter, Time.deltaTime * centerSmoothSpeed);
        vignette.center.value = currentCenter;
    }

    // --- Gizmos to visualize min/max radius ---
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, maxDistance); // max distance (baseline)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, minDistance); // min distance (max vignette)
    }
}
