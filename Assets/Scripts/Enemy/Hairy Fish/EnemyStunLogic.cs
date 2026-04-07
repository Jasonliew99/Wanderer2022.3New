using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class EnemyStunLogic : MonoBehaviour
{
    [Header("Charge Settings")]
    public float chargeSpeed = 0.7f;
    public float drainSpeed = 0.4f;
    [Range(0f, 1f)]
    public float chargeValue = 0f;

    [Header("Stun Settings")]
    public float stunDuration = 2.0f;
    public float cooldownDuration = 2.5f;
    public bool isOnCooldown = false;

    [Header("Visual Shake / Flash")]
    public float shakeAmount = 0.08f;
    public float shakeDuration = 0.15f;
    public Color flashColor = Color.white;
    public float flashDuration = 0.12f;

    [Header("References")]
    public TorchLightDetector torchLightDetector;
    public TorchlightManager torchlightManager;

    public NavMeshAgent agent;
    public MonoBehaviour aiBehaviour;
    public MonoBehaviour spriteAnimBehaviour;
    public Animator animatorToDisable;

    public Transform spriteTransform;
    public SpriteRenderer spriteRenderer;

    [Header("Floating UI Bar")]
    public Canvas worldSpaceCanvas;
    public Image fillBar;
    public float barHeight = 2.0f;
    public bool hideBarWhenIdle = true;

    [Header("Events")]
    public UnityEvent onStunStarted;
    public UnityEvent onStunEnded;

    private bool isBeingShined = false;
    private bool isStunned = false;
    private Vector3 spriteOriginalPos;
    private Color spriteOriginalColor;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // Setup sprite references
        if (spriteTransform == null && transform.childCount > 0)
            spriteTransform = transform.GetChild(0);

        if (spriteRenderer == null && spriteTransform != null)
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();

        if (spriteTransform != null)
            spriteOriginalPos = spriteTransform.localPosition;

        if (spriteRenderer != null)
            spriteOriginalColor = spriteRenderer.color;

        // Subscribe to detector
        if (torchLightDetector != null)
        {
            torchLightDetector.onStay += OnDetectorStay;
            torchLightDetector.onExit += OnDetectorExit;
        }

        if (worldSpaceCanvas != null) worldSpaceCanvas.gameObject.SetActive(false);
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
        UpdateBarPosition();

        if (isStunned || isOnCooldown)
        {
            isBeingShined = false;
            return;
        }

        if (isBeingShined)
        {
            chargeValue += chargeSpeed * Time.deltaTime;
            chargeValue = Mathf.Clamp01(chargeValue);
            UpdateBarUI();

            if (chargeValue >= 1f) StartCoroutine(StunRoutine());
        }
        else
        {
            if (chargeValue > 0f)
            {
                chargeValue -= drainSpeed * Time.deltaTime;
                chargeValue = Mathf.Clamp01(chargeValue);
                UpdateBarUI();
            }
            else if (hideBarWhenIdle && worldSpaceCanvas != null)
            {
                worldSpaceCanvas.gameObject.SetActive(false);
            }
        }

        isBeingShined = false;
    }

    private void OnDetectorStay(Collider col)
    {
        // Check if the light hitting us is actually coming from the player's torch
        if (torchlightManager != null && !torchlightManager.IsTorchOn) return;

        if (col == GetComponent<Collider>() || col.transform.IsChildOf(transform))
            isBeingShined = true;
    }

    private void OnDetectorExit(Collider col)
    {
        if (col == GetComponent<Collider>() || col.transform.IsChildOf(transform))
            isBeingShined = false;
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        onStunStarted?.Invoke();

        if (worldSpaceCanvas != null) worldSpaceCanvas.gameObject.SetActive(true);

        // Shake and Flash
        if (spriteTransform != null && spriteRenderer != null)
        {
            StartCoroutine(ShakeAndFlash());
        }

        // Disable movement
        if (agent != null) { agent.isStopped = true; agent.velocity = Vector3.zero; }
        if (aiBehaviour != null) aiBehaviour.enabled = false;
        if (spriteAnimBehaviour != null) spriteAnimBehaviour.enabled = false;
        if (animatorToDisable != null) animatorToDisable.enabled = false;

        yield return new WaitForSeconds(stunDuration);

        // --- FIXED BATTERY DRAIN ---
        // Instead of SetBatteryPercent, we use the internal battery variable 
        // since the torch handles its own UI updates in its Update loop.
        if (torchlightManager != null)
        {
            torchlightManager.battery = 0f; // This kills the battery
            torchlightManager.ShowTemporaryUI();
        }

        isStunned = false;
        chargeValue = 0f;

        // Resume movement
        if (agent != null) agent.isStopped = false;
        if (aiBehaviour != null) aiBehaviour.enabled = true;
        if (spriteAnimBehaviour != null) spriteAnimBehaviour.enabled = true;
        if (animatorToDisable != null) animatorToDisable.enabled = true;

        onStunEnded?.Invoke();
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator ShakeAndFlash()
    {
        spriteRenderer.color = flashColor;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            spriteTransform.localPosition = spriteOriginalPos + (Vector3)Random.insideUnitCircle * shakeAmount;
            elapsed += Time.deltaTime;
            yield return null;
        }
        spriteTransform.localPosition = spriteOriginalPos;
        spriteRenderer.color = spriteOriginalColor;
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        if (worldSpaceCanvas != null) worldSpaceCanvas.gameObject.SetActive(false);
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
    }

    private void UpdateBarPosition()
    {
        if (worldSpaceCanvas == null || !worldSpaceCanvas.gameObject.activeSelf) return;
        worldSpaceCanvas.transform.position = transform.position + Vector3.up * barHeight;
        if (cam != null) worldSpaceCanvas.transform.rotation = cam.transform.rotation;
    }

    private void UpdateBarUI()
    {
        if (worldSpaceCanvas == null || fillBar == null) return;
        if (!worldSpaceCanvas.gameObject.activeSelf) worldSpaceCanvas.gameObject.SetActive(true);
        fillBar.fillAmount = chargeValue;
    }
}
