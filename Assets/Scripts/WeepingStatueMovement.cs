using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]

//This is the worst script for now, im bouta recreate anothe ww2 bro
//The logic is all over the place and its a mess ngl
//THis thing is easy to think but hard to write cause it keeps splitting cause this AI has more life choices than me
public class WeepingStatueMovement : MonoBehaviour
{
    public enum StatueState { Inactive, Triggered, Active, Returning }

    [Header("References")]
    public Transform player;
    public TorchLightDetector torchDetector;
    public ChaseZoneStatueFish hauntingZone;

    [Header("Triggered Animation")]
    public AnimationClip activationClip;
    private Animation activationAnimation;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite inactiveSprite;
    public Sprite activeSprite;

    [Header("Activation Settings")]
    public float activationRadius = 5f;
    public float postTriggerDelay = 0.05f;

    [Header("Movement")]
    public float chaseSpeed = 3.5f;
    public float stoppingDistance = 1f;

    [Header("Torch Freeze")]
    public float unfreezeDelay = 0.2f;

    [Header("Shake Settings")]
    public bool enableShake = true;
    public float shakeDuration = 0.15f;
    public float shakeStrength = 0.05f;

    [Header("Standby Points (Set Many)")]
    public Transform[] standbyPoints;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool showActivationGizmo = true;

    // STATIC shared occupancy list
    private static Dictionary<Transform, bool> standbyOccupied = new Dictionary<Transform, bool>();

    // Internal
    private StatueState currentState = StatueState.Inactive;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Animator animator;

    private bool isIlluminated = false;
    private bool isUnfreezeDelayed = false;
    private Coroutine unfreezeRoutine;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Transform reservedStandbyPoint = null;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        agent.speed = chaseSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = true;

        // Avoid pushing / clipping
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 80);

        // Init standby dictionary
        foreach (var p in standbyPoints)
        {
            if (!standbyOccupied.ContainsKey(p))
                standbyOccupied[p] = false;
        }

        // Legacy animation setup
        activationAnimation = gameObject.AddComponent<Animation>();
        activationAnimation.playAutomatically = false;
        activationAnimation.wrapMode = WrapMode.Once;

        if (activationClip != null)
            activationAnimation.AddClip(activationClip, activationClip.name);

        if (animator != null) animator.enabled = false;

        if (torchDetector != null)
        {
            torchDetector.onEnter += TorchEnter;
            torchDetector.onStay += TorchStay;
            torchDetector.onExit += TorchExit;
        }

        if (inactiveSprite != null)
            spriteRenderer.sprite = inactiveSprite;
    }

    void Update()
    {
        if (player == null) return;

        // Outside zone = reset
        if (hauntingZone != null && !hauntingZone.IsInside(player.position))
        {
            ResetToInitial();
            return;
        }

        switch (currentState)
        {
            case StatueState.Inactive:
                CheckActivation();
                break;

            case StatueState.Active:
                ActiveMovement();
                break;

            case StatueState.Returning:
                ReturnToStandby();
                break;
        }
    }

    private void CheckActivation()
    {
        if (Vector3.Distance(transform.position, player.position) <= activationRadius)
        {
            StartTriggerState();
        }
    }

    private void StartTriggerState()
    {
        if (currentState != StatueState.Inactive) return;

        currentState = StatueState.Triggered;
        if (debugLogs) Debug.Log("[Statue] Triggered");

        StopMovement();

        if (activationClip != null)
            activationAnimation.Play(activationClip.name);

        if (enableShake)
            StartCoroutine(ShakeRoutine());

        StartCoroutine(WaitAndBecomeActive());
    }

    private IEnumerator WaitAndBecomeActive()
    {
        yield return new WaitForSeconds(activationClip.length + postTriggerDelay);
        BecomeActive();
    }

    private void BecomeActive()
    {
        currentState = StatueState.Active;

        if (activeSprite != null)
            spriteRenderer.sprite = activeSprite;

        if (!isIlluminated)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        if (debugLogs) Debug.Log("[Statue] Active");
    }

    private void ActiveMovement()
    {
        if (isIlluminated || isUnfreezeDelayed)
        {
            StopMovement();
            return;
        }

        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        // If player is too far, start returning
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > activationRadius * 3f) // <-- tune this threshold
        {
            StartReturnToStandby();
        }
    }

    // REQUEST a free standby point
    private Transform GetFreeStandby()
    {
        foreach (var p in standbyPoints)
        {
            if (standbyOccupied.ContainsKey(p) && standbyOccupied[p] == false)
            {
                standbyOccupied[p] = true;
                return p;
            }
        }

        return null;
    }

    private void StartReturnToStandby()
    {
        if (reservedStandbyPoint == null)
            reservedStandbyPoint = GetFreeStandby();

        if (reservedStandbyPoint == null)
        {
            if (debugLogs) Debug.LogWarning("[Statue] No free standby points!");
            return;
        }

        currentState = StatueState.Returning;

        agent.isStopped = false;
        agent.speed = chaseSpeed * 0.7f;
        agent.SetDestination(reservedStandbyPoint.position);

        if (debugLogs)
            Debug.Log("[Statue] Returning to standby: " + reservedStandbyPoint.name);
    }

    private void ReturnToStandby()
    {
        if (!agent.isOnNavMesh) return;
        if (reservedStandbyPoint == null) return;

        if (!agent.pathPending && agent.remainingDistance < 0.3f)
        {
            // Arrive → become statue again
            standbyOccupied[reservedStandbyPoint] = false;
            reservedStandbyPoint = null;

            currentState = StatueState.Inactive;

            StopMovement();
            transform.position = transform.position; // No teleport needed
            spriteRenderer.sprite = inactiveSprite;

            if (debugLogs)
                Debug.Log("[Statue] Arrived at standby → back to statue mode");
        }
    }

    private void StopMovement()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    // TORCH LOGIC (unchanged)
    private bool IsSelf(Collider col)
    {
        return col.transform == transform || col.transform.IsChildOf(transform);
    }

    private void TorchEnter(Collider col)
    {
        if (currentState != StatueState.Active) return;
        if (!IsSelf(col)) return;

        isIlluminated = true;
        StopMovement();
    }

    private void TorchStay(Collider col)
    {
        if (currentState != StatueState.Active) return;
        if (IsSelf(col)) isIlluminated = true;
    }

    private void TorchExit(Collider col)
    {
        if (currentState != StatueState.Active) return;
        if (!IsSelf(col)) return;

        isIlluminated = false;

        if (unfreezeRoutine != null) StopCoroutine(unfreezeRoutine);
        unfreezeRoutine = StartCoroutine(UnfreezeDelayRoutine());
    }

    private IEnumerator UnfreezeDelayRoutine()
    {
        isUnfreezeDelayed = true;
        yield return new WaitForSeconds(unfreezeDelay);
        isUnfreezeDelayed = false;

        if (enableShake)
            StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 original = transform.position;
        float t = 0f;

        while (t < shakeDuration)
        {
            transform.position = original + new Vector3(
                Random.Range(-shakeStrength, shakeStrength),
                Random.Range(-shakeStrength, shakeStrength),
                0f);

            t += Time.deltaTime;
            yield return null;
        }

        transform.position = original;
    }

    private void ResetToInitial()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        currentState = StatueState.Inactive;
        isIlluminated = false;
        isUnfreezeDelayed = false;

        if (inactiveSprite != null)
            spriteRenderer.sprite = inactiveSprite;

        StopMovement();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showActivationGizmo) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
