using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteProximityManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your Player transform here!")]
    public Transform player;
    public Volume postProcessVolume;

    [Header("Distance Settings")]
    public float maxDistance = 15f;
    public float minDistance = 2f;

    [Header("Vignette Intensity")]
    public float baseIntensity = 0.25f;
    public float maxIntensity = 0.75f;

    [Header("Dynamic Pulse")]
    public float pulseSpeed = 7f;
    public float pulseAmount = 0.08f;

    private Vignette vignette;
    private float currentLerpedIntensity;

    void Start()
    {
        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out vignette))
        {
            vignette.intensity.overrideState = true;
            vignette.color.overrideState = true;
            vignette.center.value = new Vector2(0.5f, 0.5f);
        }
        else
        {
            Debug.LogError("Vignette not found in Post Process Volume Profile!");
        }
    }

    void Update()
    {
        if (player == null || vignette == null) return;

        float closestDist = GetClosestEnemyDistance();

        float panicFactor = 1f - Mathf.InverseLerp(minDistance, maxDistance, closestDist);

        float pulse = Mathf.Pow(Mathf.Sin(Time.time * pulseSpeed), 4) * pulseAmount * panicFactor;

        float targetIntensity = Mathf.Lerp(baseIntensity, maxIntensity, panicFactor) + pulse;

        currentLerpedIntensity = Mathf.Lerp(currentLerpedIntensity, targetIntensity, Time.deltaTime * 4f);
        vignette.intensity.value = Mathf.Clamp01(currentLerpedIntensity);

        vignette.color.value = Color.Lerp(Color.black, new Color(0.4f, 0.05f, 0.05f), panicFactor);
    }

    float GetClosestEnemyDistance()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float min = Mathf.Infinity;

        if (enemies.Length == 0) return Mathf.Infinity;

        foreach (GameObject e in enemies)
        {
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
