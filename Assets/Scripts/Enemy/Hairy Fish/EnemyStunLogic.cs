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
    public float cooldownDuration = 2.5f; //cannot be stunned during this time
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
    public MonoBehaviour aiBehaviour;           // PlayerTracker
    public MonoBehaviour spriteAnimBehaviour;   // EnemySpriteAnimation
    public Animator animatorToDisable;

    public Transform spriteTransform;
    public SpriteRenderer spriteRenderer;

    [Header("Floating UI Bar")]
    public Canvas worldSpaceCanvas;         // parent canvas
    public Image fillBar;                   // fill bar inside the canvas
    public float barHeight = 2.0f;          // height above enemy
    public bool hideBarWhenIdle = true;

    [Header("Events")]
    public UnityEvent onStunStarted;
    public UnityEvent onStunEnded;

    // Internal
    private bool isBeingShined = false;
    private bool isStunned = false;
    private Vector3 spriteOriginalPos;
    private Color spriteOriginalColor;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (aiBehaviour == null) aiBehaviour = GetComponent<MonoBehaviour>();

        if (spriteTransform == null && transform.childCount > 0)
            spriteTransform = transform.GetChild(0);

        if (spriteRenderer == null && spriteTransform != null)
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();

        if (spriteTransform != null)
            spriteOriginalPos = spriteTransform.localPosition;

        if (spriteRenderer != null)
            spriteOriginalColor = spriteRenderer.color;

        // subscribe detector
        if (torchLightDetector != null)
        {
            torchLightDetector.onStay += OnDetectorStay;
            torchLightDetector.onExit += OnDetectorExit;
        }

        // hide bar initially
        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.gameObject.SetActive(false);
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
        UpdateBarPosition();

        if (isStunned || isOnCooldown)
        {
            isBeingShined = false;
            return;
        }

        //CHARGING
        if (isBeingShined)
        {
            chargeValue += chargeSpeed * Time.deltaTime;
            chargeValue = Mathf.Clamp01(chargeValue);

            UpdateBarUI();

            if (chargeValue >= 1f)
                StartCoroutine(StunRoutine());
        }
        else
        {
            //DRAINING
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

        isBeingShined = false; // reset per-frame flag
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

    // STUN ROUTINE
    private IEnumerator StunRoutine()
    {
        isStunned = true;
        onStunStarted?.Invoke();

        // show UI bar full during stun
        if (worldSpaceCanvas != null)
            worldSpaceCanvas.gameObject.SetActive(true);

        //shake + flash
        if (spriteTransform != null && spriteRenderer != null)
        {
            spriteRenderer.color = flashColor;

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-shakeAmount, shakeAmount),
                    Random.Range(-shakeAmount, shakeAmount),
                    0f);
                spriteTransform.localPosition = spriteOriginalPos + offset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            spriteTransform.localPosition = spriteOriginalPos;
            spriteRenderer.color = spriteOriginalColor;
        }

        //disable movement and animations
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        if (aiBehaviour != null) aiBehaviour.enabled = false;
        if (spriteAnimBehaviour != null) spriteAnimBehaviour.enabled = false;
        if (animatorToDisable != null) animatorToDisable.enabled = false;

        //stun duration
        float timer = 0f;
        while (timer < stunDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        //BATTERY DRAIN
        if (torchlightManager != null)
            torchlightManager.SetBatteryPercent(0f);

        // reset values
        isStunned = false;
        chargeValue = 0f;

        // resume movement/AI
        if (agent != null) agent.isStopped = false;
        if (aiBehaviour != null) aiBehaviour.enabled = true;
        if (spriteAnimBehaviour != null) spriteAnimBehaviour.enabled = true;
        if (animatorToDisable != null) animatorToDisable.enabled = true;

        onStunEnded?.Invoke();

        // enter cooldown
        StartCoroutine(CooldownRoutine());
    }

    // COOLDOWN
    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;

        // hide bar
        if (worldSpaceCanvas != null)
            worldSpaceCanvas.gameObject.SetActive(false);

        float t = 0f;
        while (t < cooldownDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        isOnCooldown = false;
    }

    // Floating UI Bar Logic
    private void UpdateBarPosition()
    {
        if (worldSpaceCanvas == null) return;
        if (!worldSpaceCanvas.gameObject.activeSelf) return;

        Vector3 pos = transform.position + Vector3.up * barHeight;
        worldSpaceCanvas.transform.position = pos;

        if (cam != null)
            worldSpaceCanvas.transform.LookAt(worldSpaceCanvas.transform.position + cam.transform.forward);
    }

    private void UpdateBarUI()
    {
        if (worldSpaceCanvas == null || fillBar == null) return;

        if (!worldSpaceCanvas.gameObject.activeSelf)
            worldSpaceCanvas.gameObject.SetActive(true);

        fillBar.fillAmount = chargeValue;
    }
}
