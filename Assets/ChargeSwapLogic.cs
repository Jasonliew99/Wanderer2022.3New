using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeSwapLogic : MonoBehaviour
{
    [Header("Charge Settings")]
    public float chargeSpeed = 0.5f;
    [Range(0f, 1f)]
    public float chargeValue = 0f;
    public bool isCharged = false;

    [Header("References")]
    public TorchLightDetector torchLightDetector;
    public ParticleSystem particleEffect;
    public SpriteRenderer spriteRenderer;

    private bool isBeingShined = false;

    private void Awake()
    {
        if (particleEffect != null) particleEffect.gameObject.SetActive(true);
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        if (torchLightDetector != null)
        {
            torchLightDetector.onStay += OnDetectorStay;
            torchLightDetector.onExit += OnDetectorExit;
        }
        else
        {
            Debug.LogWarning($"{name}: TorchLightDetector not assigned!");
        }
    }

    private void OnDestroy()
    {
        if (torchLightDetector != null)
        {
            torchLightDetector.onStay -= OnDetectorStay;
            torchLightDetector.onExit -= OnDetectorExit;
        }
    }

    private void Update()
    {
        if (isCharged) return;

        if (isBeingShined)
        {
            chargeValue += chargeSpeed * Time.deltaTime;
            chargeValue = Mathf.Clamp01(chargeValue);

            if (chargeValue >= 1f)
            {
                isCharged = true;
                RevealObject();
            }
        }

        isBeingShined = false; // reset each frame
    }

    private void OnDetectorStay(Collider col)
    {
        if (col == GetComponent<Collider>() || col.transform.IsChildOf(transform))
            isBeingShined = true;
    }

    private void OnDetectorExit(Collider col)
    {
        if (col == GetComponent<Collider>() || col.transform.IsChildOf(transform))
            isBeingShined = false;
    }

    private void RevealObject()
    {
        if (particleEffect != null)
            particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }
}
