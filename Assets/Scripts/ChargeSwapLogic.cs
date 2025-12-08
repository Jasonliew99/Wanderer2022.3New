using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeSwapLogic : MonoBehaviour
{
    [Header("Charge Settings")]
    public float chargeSpeed = 0.5f;
    public float drainSpeed = 0.3f;
    [Range(0f, 1f)]
    public float chargeValue = 0f;
    public bool isCharged = false;

    [Header("References")]
    public TorchLightDetector torchLightDetector;
    public ParticleSystem particleEffect;
    public SpriteRenderer spriteRenderer;

    [Tooltip("Script that handles collecting the item. Will be disabled until fully charged.")]
    public MonoBehaviour collectableScript;     // for the collectable behavior, bascially can only be collected when fully charged only if not untouchable

    private bool isBeingShined = false;

    private void Awake()
    {
        if (particleEffect != null) particleEffect.gameObject.SetActive(true);
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // Disable collectable script at start maybe if my logic is corrects
        if (collectableScript != null)
            collectableScript.enabled = false;

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

    // wtf why am i even using ondestroy bruh but if its works it works ig
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
        else
        {
            if (chargeValue > 0f)
            {
                chargeValue -= drainSpeed * Time.deltaTime;
                chargeValue = Mathf.Clamp01(chargeValue);
            }
        }

        isBeingShined = false;
    }

    // for when the light is shining on it, like how u got flasshed by a pervert
    private void OnDetectorStay(Collider col)
    {
        if (col == GetComponent<Collider>() || col.transform.IsChildOf(transform))
            isBeingShined = true;
    }

    // for when the light leaves
    private void OnDetectorExit(Collider col)
    {
        if (col == GetComponent<Collider>() || col.transform.IsChildOf(transform))
            isBeingShined = false;
    }

    //for when fully charged up if not this shit doesnt exist
    private void RevealObject()
    {
        if (particleEffect != null)
            particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        // now the item becomes collectable when charged fully
        if (collectableScript != null)
            collectableScript.enabled = true;
    }
}
