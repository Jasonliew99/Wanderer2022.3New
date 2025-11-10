using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class FishStatueController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Volume postProcessingVolume;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public AnimationClip triggerAnimation; // drag-and-drop here
    public Sprite placeholderSprite;
    public Sprite activeSprite;

    [Header("Chase Zone Reference")]
    public ChaseZoneStatueFish chaseZone;

    [Header("Activation & Timing Settings")]
    public float activationRadius = 5f;
    public float vibrationDuration = 1f;
    public float chargeSpeed = 10f;
    public float torchlightInterruptTime = 1f;
    public float cooldownAfterInterrupt = 3f;

    [Header("Vignette Settings")]
    public float baselineIntensity = 0.1f;
    public float maxVignetteIntensity = 0.6f;
    public float vignetteSpeed = 0.5f;

    [Header("Vibration Settings")]
    public float vibrationIntensity = 0.05f;

    private Vector3 originalPosition;
    private Vector3 spriteOriginalPos;
    private Vignette vignette;

    private enum StatueState { Idle, Haunting, Vibrating, Charge, Paused, Cooldown }
    private StatueState state = StatueState.Idle;

    private float timer = 0f;
    private float torchlightTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isPlayerInView = true;
    private bool isInFlashlight = false;

    void Start()
    {
        originalPosition = transform.position;
        spriteOriginalPos = transform.localPosition;

        if (postProcessingVolume != null && postProcessingVolume.profile.TryGet(out vignette))
        {
            vignette.intensity.value = baselineIntensity;
            vignette.center.value = new Vector2(0.5f, 0.5f);
        }

        if (agent != null)
        {
            agent.speed = chargeSpeed;
            agent.isStopped = true;
        }

        ResetSprite();
    }

    void Update()
    {
        bool playerInGround = chaseZone != null && chaseZone.IsInside(player.position);

        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
        isPlayerInView = screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1 && screenPoint.z > 0;

        UpdateVignetteCenter();

        switch (state)
        {
            case StatueState.Idle:
                if (Vector3.Distance(player.position, transform.position) <= activationRadius)
                {
                    state = StatueState.Haunting;
                    timer = 0f;
                    PlayTriggerAnimation();
                }
                break;

            case StatueState.Haunting:
                if (!isPlayerInView)
                {
                    state = StatueState.Vibrating;
                    timer = 0f;
                    agent.isStopped = true;
                }
                break;

            case StatueState.Vibrating:
                if (!isInFlashlight) // pause if flashlight shining
                {
                    timer += Time.deltaTime;
                    VibrateSprite();
                    vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, maxVignetteIntensity, Time.deltaTime * vignetteSpeed);
                }
                else
                {
                    torchlightTimer += Time.deltaTime;
                    FlickerVignette();
                    if (torchlightTimer >= torchlightInterruptTime)
                        StartCooldown();
                }

                if (timer >= vibrationDuration && !isInFlashlight)
                {
                    timer = 0f;
                    state = StatueState.Charge;
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                break;

            case StatueState.Charge:
                if (agent != null && !isInFlashlight)
                    agent.SetDestination(player.position);

                float distanceFactor = Mathf.Clamp01(1f - (Vector3.Distance(player.position, transform.position) / chaseZone.radius));
                vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, baselineIntensity + distanceFactor * (maxVignetteIntensity - baselineIntensity), Time.deltaTime * vignetteSpeed);

                if (isInFlashlight)
                {
                    state = StatueState.Paused;
                    agent.isStopped = true;
                }

                if (Vector3.Distance(player.position, transform.position) < 1f)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                break;

            case StatueState.Paused:
                if (!isInFlashlight)
                {
                    state = StatueState.Charge;
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                break;

            case StatueState.Cooldown:
                cooldownTimer += Time.deltaTime;
                vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, baselineIntensity, Time.deltaTime * vignetteSpeed);

                if (cooldownTimer >= cooldownAfterInterrupt)
                {
                    cooldownTimer = 0f;
                    state = StatueState.Vibrating;
                    timer = 0f;
                }
                break;
        }

        if (!playerInGround && state != StatueState.Idle && state != StatueState.Cooldown)
        {
            ResetStatue();
        }
    }

    void UpdateVignetteCenter()
    {
        if (vignette != null)
        {
            Vector3 screenPos = Camera.main.WorldToViewportPoint(player.position);
            vignette.center.value = new Vector2(screenPos.x, screenPos.y);
        }
    }

    void VibrateSprite()
    {
        transform.localPosition = spriteOriginalPos + new Vector3(
            Mathf.Sin(Time.time * 40f) * vibrationIntensity,
            Mathf.Sin(Time.time * 50f) * vibrationIntensity,
            0
        );
    }

    void FlickerVignette()
    {
        if (vignette != null)
            vignette.intensity.value = Random.Range(baselineIntensity, maxVignetteIntensity);
    }

    void StartCooldown()
    {
        transform.position = new Vector3(0, -100, 0); // temporarily hide
        vignette.intensity.value = baselineIntensity;
        timer = 0f;
        torchlightTimer = 0f;
        state = StatueState.Cooldown;
        agent.isStopped = true;
        ResetSprite();
    }

    void ResetStatue()
    {
        transform.position = originalPosition;
        transform.localPosition = spriteOriginalPos;
        vignette.intensity.value = baselineIntensity;
        state = StatueState.Idle;
        timer = 0f;
        torchlightTimer = 0f;
        cooldownTimer = 0f;
        agent.isStopped = true;
        ResetSprite();
    }

    void PlayTriggerAnimation()
    {
        if (animator != null && triggerAnimation != null)
        {
            animator.Play(triggerAnimation.name, 0, 0f);
        }
        if (spriteRenderer != null && activeSprite != null)
            spriteRenderer.sprite = activeSprite; // stays active sprite after animation
    }

    void ResetSprite()
    {
        if (spriteRenderer != null && placeholderSprite != null)
            spriteRenderer.sprite = placeholderSprite;
    }

    // Called by TorchlightDetector script
    public void OnTorchlightEnter() => isInFlashlight = true;
    public void OnTorchlightExit() => isInFlashlight = false;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
