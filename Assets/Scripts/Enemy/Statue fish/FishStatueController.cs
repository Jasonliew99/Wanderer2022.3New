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
    public Light flashlight;
    public LayerMask torchlightLayer;
    public Volume postProcessingVolume;
    public List<Transform> teleportPoints;
    public NavMeshAgent agent;

    [Header("Chase Zone Reference")]
    public ChaseZoneStatueFish chaseZone;

    [Header("Activation & Timing Settings")]
    public float activationRadius = 5f;
    public float despawnDelay = 0.5f;
    public float vibrationDuration = 1f;
    public float chargeSpeed = 10f;
    public float flickerDuration = 0.5f;
    public float torchlightInterruptTime = 1f;
    public float cooldownAfterInterrupt = 3f;

    [Header("Vignette Settings")]
    public float baselineIntensity = 0.1f;
    public float maxVignetteIntensity = 0.6f;
    public float vignetteSpeed = 0.5f;

    [Header("Vibration Settings")]
    public float vibrationIntensity = 0.05f;

    [Header("Torchlight Detection Settings")]
    public float flashlightAngle = 25f;
    public float flashlightRange = 20f;

    private Vector3 originalPosition;
    private Vector3 spriteOriginalPos;
    private Vignette vignette;

    private enum StatueState { Idle, Haunting, Teleport, Vibrating, Charge, Paused, Cooldown }
    private StatueState state = StatueState.Idle;

    private float timer = 0f;
    private float torchlightTimer = 0f;
    private float cooldownTimer = 0f;
    private float despawnTimer = 0f;
    private bool isPlayerInView = true;

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
                }
                break;

            case StatueState.Haunting:
                if (!isPlayerInView)
                {
                    despawnTimer += Time.deltaTime;
                    if (despawnTimer >= despawnDelay)
                    {
                        despawnTimer = 0f;
                        state = StatueState.Teleport;
                    }
                }
                else
                {
                    despawnTimer = 0f;
                }
                break;

            case StatueState.Teleport:
                TeleportToRandomPoint();
                timer = 0f;
                state = StatueState.Vibrating;
                agent.isStopped = true;
                break;

            case StatueState.Vibrating:
                timer += Time.deltaTime;
                VibrateSprite();

                vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, maxVignetteIntensity, Time.deltaTime * vignetteSpeed);

                if (FlashlightHitsStatue())
                {
                    torchlightTimer += Time.deltaTime;
                    FlickerVignette();

                    if (torchlightTimer >= torchlightInterruptTime)
                        DespawnAndCooldown();
                }
                else
                {
                    torchlightTimer = 0f;
                }

                if (timer >= vibrationDuration)
                {
                    timer = 0f;
                    state = StatueState.Charge;
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                break;

            case StatueState.Charge:
                if (agent != null)
                    agent.SetDestination(player.position);

                float distanceFactor = Mathf.Clamp01(1f - (Vector3.Distance(player.position, transform.position) / chaseZone.radius));
                vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, baselineIntensity + distanceFactor * (maxVignetteIntensity - baselineIntensity), Time.deltaTime * vignetteSpeed);

                if (FlashlightHitsStatue())
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
                if (!FlashlightHitsStatue())
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

    void TeleportToRandomPoint()
    {
        if (teleportPoints.Count > 0)
        {
            Transform point = teleportPoints[Random.Range(0, teleportPoints.Count)];

            if (teleportPoints.Count > 1)
            {
                int attempts = 0;
                while (point.position == transform.position && attempts < 10)
                {
                    point = teleportPoints[Random.Range(0, teleportPoints.Count)];
                    attempts++;
                }
            }

            transform.position = point.position;
            transform.forward = (player.position - transform.position).normalized;
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

    void DespawnAndCooldown()
    {
        transform.position = new Vector3(0, -100, 0);
        vignette.intensity.value = baselineIntensity;
        timer = 0f;
        torchlightTimer = 0f;
        state = StatueState.Cooldown;
        agent.isStopped = true;
    }

    bool FlashlightHitsStatue()
    {
        if (flashlight == null) return false;

        Vector3 dirToStatue = (transform.position - flashlight.transform.position).normalized;
        float angle = Vector3.Angle(flashlight.transform.forward, dirToStatue);
        float distance = Vector3.Distance(flashlight.transform.position, transform.position);

        if (angle <= flashlightAngle && distance <= flashlightRange)
        {
            if (Physics.Raycast(flashlight.transform.position, dirToStatue, out RaycastHit hit, flashlightRange, torchlightLayer))
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                    return true;
            }
        }

        return false;
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
        despawnTimer = 0f;
        agent.isStopped = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        if (teleportPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var point in teleportPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.2f);
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }
        }

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, agent.destination);
        }
    }
}
