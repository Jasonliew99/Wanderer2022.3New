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

    private RespawnController respawnController;
    private Animator anim;
    private TorchlightManager torch;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite inactiveSprite;
    public Sprite activeSprite;

    [Header("Activation Settings")]
    public float activationRadius = 5f;
    public float postTriggerDelay = 0.05f;
    public float wakeAnimationLength = 0.4f;

    [Header("Movement")]
    public float chaseSpeed = 3.5f;

    [Header("Torch Freeze")]
    public float unfreezeDelay = 0.2f;

    [Header("Standby Points")]
    public Transform[] standbyPoints;

    [Header("Debug")]
    public bool showActivationGizmo = true;

    private StatueState currentState = StatueState.Inactive;
    private NavMeshAgent agent;
    private Rigidbody rb;

    private bool hasTriggered = false;
    private bool isActivating = false;
    private bool isTransitionPlaying = false;
    private bool isIlluminated = false;
    private bool isUnfreezeDelayed = false;

    private Coroutine unfreezeRoutine;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Dictionary<Transform, bool> standbyOccupied = new Dictionary<Transform, bool>();
    private Transform reservedStandbyPoint = null;

    void Awake()
    {
        torch = FindObjectOfType<TorchlightManager>();
        respawnController = FindObjectOfType<RespawnController>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        agent.speed = chaseSpeed;
        agent.stoppingDistance = 0.1f;
        agent.isStopped = true;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (anim != null) anim.enabled = false;

        foreach (var p in standbyPoints)
            if (p != null) standbyOccupied[p] = false;

        if (inactiveSprite != null) spriteRenderer.sprite = inactiveSprite;

        if (torchDetector != null)
        {
            torchDetector.onEnter += TorchEnter;
            torchDetector.onStay += TorchStay;
            torchDetector.onExit += TorchExit;
        }
    }

    void Update()
    {
        if (player == null) return;

        if (hauntingZone != null && !hauntingZone.IsInside(player.position))
        {
            ResetToInitial();
            return;
        }

        switch (currentState)
        {
            case StatueState.Inactive:
                if (!hasTriggered && !isActivating && Vector3.Distance(transform.position, player.position) <= activationRadius)
                {
                    hasTriggered = true;
                    isActivating = true;
                    StartCoroutine(ActivateStatue());
                }
                break;

            case StatueState.Active:
                ActiveMovement();
                break;

            case StatueState.Returning:
                ReturnToStandby();
                break;
        }
    }

    IEnumerator ActivateStatue()
    {
        currentState = StatueState.Triggered;
        yield return new WaitForSeconds(postTriggerDelay);
        BecomeActive();
        isActivating = false;
    }

    void BecomeActive()
    {
        currentState = StatueState.Active;

        isIlluminated = false;
        isUnfreezeDelayed = false;
        isTransitionPlaying = true;

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }

        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("WakeUp");
            StartCoroutine(FinishTransition());
        }
    }

    IEnumerator FinishTransition()
    {
        yield return new WaitForSeconds(wakeAnimationLength);
        if (anim != null) anim.enabled = false;
        if (activeSprite != null) spriteRenderer.sprite = activeSprite;
        isTransitionPlaying = false;
    }

    void ActiveMovement()
    {
        if (isIlluminated || isUnfreezeDelayed || isTransitionPlaying)
        {
            StopMovement();
            return;
        }

        if (!agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        agent.isStopped = false;

        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) > activationRadius * 4f)
            StartReturnToStandby();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != StatueState.Active || isTransitionPlaying) return;
        if (isIlluminated || isUnfreezeDelayed) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            StopMovement();
            if (agent != null) agent.enabled = false;
            Time.timeScale = 1f;

            if (respawnController != null)
                respawnController.HandlePlayerDeath();
        }
    }

    // --- TORCH LOGIC ---
    void TorchEnter(Collider col) { isIlluminated = true; StopMovement(); }
    void TorchStay(Collider col) { isIlluminated = true; }
    void TorchExit(Collider col)
    {
        if (currentState != StatueState.Active) return;
        bool beamStillHits = (torch != null) && torch.IsBeamHittingEnemy(transform);
        if (!beamStillHits)
        {
            isIlluminated = false;
            if (unfreezeRoutine != null) StopCoroutine(unfreezeRoutine);
            unfreezeRoutine = StartCoroutine(UnfreezeDelayRoutine());
        }
    }

    IEnumerator UnfreezeDelayRoutine()
    {
        isUnfreezeDelayed = true;
        yield return new WaitForSeconds(unfreezeDelay);
        isUnfreezeDelayed = false;
    }

    // --- STANDBY & RESET LOGIC ---
    void StartReturnToStandby()
    {
        if (reservedStandbyPoint == null) reservedStandbyPoint = GetFreeStandby();
        if (reservedStandbyPoint == null) return;

        currentState = StatueState.Returning;
        agent.isStopped = false;
        agent.speed = chaseSpeed * 0.7f;
        agent.SetDestination(reservedStandbyPoint.position);
    }

    Transform GetFreeStandby()
    {
        foreach (var p in standbyPoints)
            if (p != null && !standbyOccupied[p]) { standbyOccupied[p] = true; return p; }
        return null;
    }

    void ReturnToStandby()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.3f)
        {
            if (reservedStandbyPoint != null) standbyOccupied[reservedStandbyPoint] = false;
            reservedStandbyPoint = null;
            currentState = StatueState.Inactive;
            hasTriggered = false;
            if (inactiveSprite != null) spriteRenderer.sprite = inactiveSprite;
            StopMovement();
        }
    }

    void StopMovement() { if (agent.isActiveAndEnabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); } }

    void ResetToInitial()
    {
        StopAllCoroutines();
        hasTriggered = false;
        isActivating = false;
        isTransitionPlaying = false;
        isIlluminated = false;
        isUnfreezeDelayed = false;
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        currentState = StatueState.Inactive;
        StopMovement();
    }

    public void ResetToStatueState()
    {
        StopAllCoroutines();
        hasTriggered = false;
        isActivating = false;
        isTransitionPlaying = false;
        isIlluminated = false;
        isUnfreezeDelayed = false;
        reservedStandbyPoint = null;
        currentState = StatueState.Inactive;
        if (agent != null) { agent.enabled = true; if (agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; } }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showActivationGizmo) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
