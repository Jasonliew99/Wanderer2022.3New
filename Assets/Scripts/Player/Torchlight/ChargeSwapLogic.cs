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

    [Header("Audio")]
    public AudioClip collectSound;
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Header("Fragment Item")]
    public string itemID;
    public LevelController levelController;

    [Header("References")]
    public TorchLightDetector torchLightDetector;
    public ParticleSystem particleEffect;
    public SpriteRenderer spriteRenderer;

    [Tooltip("Drag FragmentPickup here (on CHILD object)")]
    public MonoBehaviour collectableScript;

    private bool isBeingShined = false;
    private Collider pickupCollider;

    private void Awake()
    {
        if (particleEffect != null)
            particleEffect.gameObject.SetActive(true);

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        // FORCE DISABLE PICKUP AT START
        if (collectableScript != null)
        {
            collectableScript.enabled = false;

            pickupCollider = collectableScript.GetComponent<Collider>();
            if (pickupCollider != null)
                pickupCollider.enabled = false;
        }

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

    public void PlayCollectSound()
    {
        if (collectSound != null)
        {
            // Plays at position and survives the destruction of this object
            AudioSource.PlayClipAtPoint(collectSound, transform.position, volume);
        }
    }

    private void RevealObject()
    {
        if (particleEffect != null)
            particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Fetch the sprite from your controller
        Sprite next = levelController.RequestNextFragment(itemID);

        if (next != null)
        {
            spriteRenderer.sprite = next;
            spriteRenderer.enabled = true;
        }

        // ENABLE PICKUP AFTER REVEAL
        if (collectableScript != null)
            collectableScript.enabled = true;

        if (pickupCollider != null)
            pickupCollider.enabled = true;
    }
}
