using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThunderEffects : MonoBehaviour
{
    [Header("References")]
    public Light globalLight;                 // Your main Directional Light
    public AudioSource thunderAudioSource;    // Your thunder sound source

    [Header("Lightning Settings")]
    public float minTimeBetweenStrikes = 5f;
    public float maxTimeBetweenStrikes = 15f;

    public float flashIntensity = 2f;         // How bright the flash becomes
    public float flashDuration = 0.1f;        // Time of each flash pulse
    public int flashCount = 2;                // How many flashes for one lightning strike
    public float timeBetweenFlashes = 0.05f;  // Flicker delay

    [Header("Thunder Settings")]
    public AudioClip[] thunderSounds;
    public float minThunderDelay = 0.1f;      // Delay between lightning flash and thunder
    public float maxThunderDelay = 1.2f;

    private float originalIntensity;

    private void Start()
    {
        if (globalLight != null)
            originalIntensity = globalLight.intensity;

        StartCoroutine(LightningLoop());
    }

    private IEnumerator LightningLoop()
    {
        while (true)
        {
            // Wait for random time between lightning strikes
            yield return new WaitForSeconds(Random.Range(minTimeBetweenStrikes, maxTimeBetweenStrikes));

            // Play lightning flash visually
            yield return StartCoroutine(PlayLightningFlash());

            // Random thunder delay
            float thunderDelay = Random.Range(minThunderDelay, maxThunderDelay);
            yield return new WaitForSeconds(thunderDelay);

            // Play thunder audio
            PlayThunderSound();
        }
    }

    private IEnumerator PlayLightningFlash()
    {
        for (int i = 0; i < flashCount; i++)
        {
            if (globalLight != null)
                globalLight.intensity = flashIntensity;

            yield return new WaitForSeconds(flashDuration);

            if (globalLight != null)
                globalLight.intensity = originalIntensity;

            yield return new WaitForSeconds(timeBetweenFlashes);
        }
    }

    private void PlayThunderSound()
    {
        if (thunderSounds.Length > 0)
        {
            thunderAudioSource.clip = thunderSounds[Random.Range(0, thunderSounds.Length)];
            thunderAudioSource.Play();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize flash intensity range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        // Text in Scene View
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"Lightning Flash Intensity: {flashIntensity}\nThunder Delay: {minThunderDelay}-{maxThunderDelay}");
#endif
    }
}
