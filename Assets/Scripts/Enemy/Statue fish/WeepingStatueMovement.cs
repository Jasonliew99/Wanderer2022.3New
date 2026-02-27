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

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite inactiveSprite;
    public Sprite activeSprite;

    [Header("Activation Settings")]
    public float activationRadius = 5f;
    public float postTriggerDelay = 0.05f;

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

    private bool isIlluminated = false;
    private bool isUnfreezeDelayed = false;
    private Coroutine unfreezeRoutine;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private static Dictionary<Transform, bool> standbyOccupied = new Dictionary<Transform, bool>();
    private Transform reservedStandbyPoint = null;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        agent.speed = chaseSpeed;
        agent.stoppingDistance = 0f;
        agent.isStopped = true;

        // ⚠️ MUST BE NON-KINEMATIC FOR OnCollisionEnter
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        foreach (var p in standbyPoints)
        {
            if (!standbyOccupied.ContainsKey(p))
                standbyOccupied[p] = false;
        }

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

        if (hauntingZone != null && !hauntingZone.IsInside(player.position))
        {
            ResetToInitial();
            return;
        }

        switch (currentState)
        {
            case StatueState.Inactive:
                if (Vector3.Distance(transform.position, player.position) <= activationRadius)
                    StartCoroutine(ActivateStatue());
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
    }

    void BecomeActive()
    {
        currentState = StatueState.Active;

        if (activeSprite != null)
            spriteRenderer.sprite = activeSprite;

        if (!isIlluminated)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    void ActiveMovement()
    {
        if (isIlluminated || isUnfreezeDelayed)
        {
            StopMovement();
            return;
        }

        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > activationRadius * 3f)
            StartReturnToStandby();
    }

    // 🔥 SAME KILL SYSTEM AS PLAYERTRACKER
    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != StatueState.Active) return;
        if (isIlluminated || isUnfreezeDelayed) return;

        if (collision.transform.root == player)
        {
            RespawnController respawnController = FindObjectOfType<RespawnController>();
            if (respawnController != null)
            {
                respawnController.HandlePlayerDeath();
            }
        }
    }

    void StartReturnToStandby()
    {
        if (reservedStandbyPoint == null)
            reservedStandbyPoint = GetFreeStandby();

        if (reservedStandbyPoint == null) return;

        currentState = StatueState.Returning;

        agent.isStopped = false;
        agent.speed = chaseSpeed * 0.7f;
        agent.SetDestination(reservedStandbyPoint.position);
    }

    Transform GetFreeStandby()
    {
        foreach (var p in standbyPoints)
        {
            if (!standbyOccupied[p])
            {
                standbyOccupied[p] = true;
                return p;
            }
        }
        return null;
    }

    void ReturnToStandby()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.3f)
        {
            standbyOccupied[reservedStandbyPoint] = false;
            reservedStandbyPoint = null;
            currentState = StatueState.Inactive;
            StopMovement();
            spriteRenderer.sprite = inactiveSprite;
        }
    }

    void StopMovement()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    void TorchEnter(Collider col)
    {
        if (currentState != StatueState.Active) return;
        isIlluminated = true;
        StopMovement();
    }

    void TorchStay(Collider col)
    {
        if (currentState != StatueState.Active) return;
        isIlluminated = true;
    }

    void TorchExit(Collider col)
    {
        if (currentState != StatueState.Active) return;
        isIlluminated = false;

        if (unfreezeRoutine != null) StopCoroutine(unfreezeRoutine);
        unfreezeRoutine = StartCoroutine(UnfreezeDelayRoutine());
    }

    IEnumerator UnfreezeDelayRoutine()
    {
        isUnfreezeDelayed = true;
        yield return new WaitForSeconds(unfreezeDelay);
        isUnfreezeDelayed = false;
    }

    void ResetToInitial()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        currentState = StatueState.Inactive;
        isIlluminated = false;
        isUnfreezeDelayed = false;
        spriteRenderer.sprite = inactiveSprite;
        StopMovement();
    }

    public void ResetToStatueState()
    {
        StopAllCoroutines();
        currentState = StatueState.Inactive;
        isIlluminated = false;
        isUnfreezeDelayed = false;

        if (agent != null)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }

        if (inactiveSprite != null)
            spriteRenderer.sprite = inactiveSprite;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showActivationGizmo) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
