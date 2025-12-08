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
    public enum StatueState { Inactive, Triggered, Active } //hahhahahahah statue states ahhahaha

    [Header("References")]
    public Transform player;
    public TorchLightDetector torchDetector;
    public ChaseZoneStatueFish hauntingZone; //this will be removed later once i figure out how to make the statue SMARTER. WHy am i trying to make an AI smarter anyways. This shit is making better choices than me in my life

    [Header("Triggered Animation (One-shot)")]
    public AnimationClip activationClip;
    private Animation activationAnimation;  // Legacy Animation Player

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer; //their skin
    public Sprite inactiveSprite; //sleep skin
    public Sprite activeSprite; //woke up skin

    [Header("Activation Settings")] //wake the fuck up samurai
    public float activationRadius = 5f;
    public float postTriggerDelay = 0.05f;

    [Header("Movement")]
    public float chaseSpeed = 3.5f;
    public float stoppingDistance = 1f;

    [Header("Torch Freeze (Active Only)")] //Go to sleep go to sleep
    public float unfreezeDelay = 0.2f;

    [Header("Shake Settings")]
    public bool enableShake = true;
    public float shakeDuration = 0.15f;
    public float shakeStrength = 0.05f;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool showActivationGizmo = true;

    // Internal
    private StatueState currentState = StatueState.Inactive;
    private NavMeshAgent agent;
    private Rigidbody rb;

    private bool isIlluminated = false;
    private bool isUnfreezeDelayed = false;
    private Coroutine unfreezeRoutine;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Animator animator; // detect and disable it

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

        // LEGACY ANIMATION SETUP — DOES NOT AUTOPLAY
        activationAnimation = gameObject.AddComponent<Animation>();
        activationAnimation.playAutomatically = false;
        activationAnimation.wrapMode = WrapMode.Once;

        if (activationClip != null)
            activationAnimation.AddClip(activationClip, activationClip.name);


        // Animator must NOT run any animation ever
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Torch events (Active only)
        if (torchDetector != null)
        {
            torchDetector.onEnter += TorchEnter;
            torchDetector.onStay += TorchStay;
            torchDetector.onExit += TorchExit;
        }

        // Start with inactive sprite
        if (inactiveSprite != null)
            spriteRenderer.sprite = inactiveSprite;
    }

    void OnDestroy()
    {
        if (torchDetector != null)
        {
            torchDetector.onEnter -= TorchEnter;
            torchDetector.onStay -= TorchStay;
            torchDetector.onExit -= TorchExit;
        }
    }

    void Update()
    {
        if (player == null) return;

        // If player leaves the haunt zone → reset
        if (hauntingZone != null)
        {
            if (!hauntingZone.IsInside(player.position))
            {
                ResetToInitial();
                return;
            }
        }

        switch (currentState)
        {
            case StatueState.Inactive:
                CheckActivation();
                break;

            case StatueState.Active:
                ActiveMovement();
                break;
        }
    }

    // ACTIVATION
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

        // Play the one-shot activation animation (Legacy)
        if (activationClip != null)
            activationAnimation.Play(activationClip.name);

        // Optional shake
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

        // NEVER enable Animator automatically — user can manually enable
        if (!isIlluminated)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        if (debugLogs) Debug.Log("[Statue] Active");
    }

    //  ACTIVE MOVEMENT
    private void ActiveMovement()
    {
        if (isIlluminated || isUnfreezeDelayed)
        {
            StopMovement();
            return;
        }

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
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


    // TORCH EVENTS (ACTIVE ONLY)
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

        if (enableShake) StartCoroutine(ShakeRoutine());
    }

    // SHAKE ROUTINE (world-space safe)
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

    // RESET TO INITIAL POSITION
    private void ResetToInitial()
    {
        if (debugLogs) Debug.Log("[Statue] Reset");

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        currentState = StatueState.Inactive;
        isIlluminated = false;
        isUnfreezeDelayed = false;

        if (inactiveSprite != null)
            spriteRenderer.sprite = inactiveSprite;

        StopMovement();
    }

    // GIZMOS
    private void OnDrawGizmosSelected()
    {
        if (!showActivationGizmo) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
