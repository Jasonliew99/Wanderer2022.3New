using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritOrbBehaviour : MonoBehaviour
{
    public enum OrbState
    {
        MovingToTarget,
        WaitingAtTarget,
        FadingOut
    }

    [Header("Movement Settings")]
    [Tooltip("How fast the orb moves towards the target.")]
    public float moveSpeed = 3f;

    [Tooltip("How high the orb floats up and down.")]
    public float floatAmplitude = 0.25f;

    [Tooltip("How fast the floating animation is.")]
    public float floatSpeed = 3f;

    [Header("Target & Stop Settings")]
    [Tooltip("Distance (on the ground plane) from the target where the orb considers itself 'arrived'.")]
    public float stopDistance = 0.5f;

    [Tooltip("How long (in seconds) the orb floats around the target before fading out.")]
    public float waitAtTargetDuration = 1.5f;

    [Header("Fade Settings")]
    [Tooltip("How long the fade-in takes when the orb spawns.")]
    public float fadeInDuration = 0.4f;

    [Tooltip("How long the fade-out takes after waiting at the target.")]
    public float fadeOutDuration = 0.6f;

    [Header("Safety Settings")]
    [Tooltip("Maximum lifetime in case something goes wrong and it never reaches the target.")]
    public float maxLifeTime = 15f;

    private Transform target;
    private OrbState state = OrbState.MovingToTarget;
    private float stateTimer = 0f;
    private float age = 0f;

    private float baseY;
    private SpriteRenderer[] spriteRenderers;

    public void SetTarget(Transform tgt)
    {
        target = tgt;
    }

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        // Start fully transparent (for fade-in)
        SetAlpha(0f);
    }

    private void Start()
    {
        baseY = transform.position.y;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        stateTimer += dt;

        // Safety destroy if something goes wrong
        if (age > maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        HandleMovement(dt);
        HandleFloating();
        HandleFade();
    }

    private void HandleMovement(float dt)
    {
        if (target == null)
            return;

        switch (state)
        {
            case OrbState.MovingToTarget:
                MoveTowardsTarget(dt);
                break;

            case OrbState.WaitingAtTarget:
                if (stateTimer >= waitAtTargetDuration)
                {
                    // Start fading out
                    state = OrbState.FadingOut;
                    stateTimer = 0f;
                }
                break;

            case OrbState.FadingOut:
                // Movement can stop or keep tiny motion; here we just keep floating (no move)
                break;
        }
    }

    private void MoveTowardsTarget(float dt)
    {
        // Move only on ground plane (XZ) so floating Y doesn't break the distance check
        Vector3 selfPos = transform.position;
        Vector3 targetPos = target.position;

        Vector3 flatSelf = new Vector3(selfPos.x, 0f, selfPos.z);
        Vector3 flatTarget = new Vector3(targetPos.x, 0f, targetPos.z);

        float flatDistance = Vector3.Distance(flatSelf, flatTarget);

        if (flatDistance > stopDistance)
        {
            Vector3 dir = (flatTarget - flatSelf).normalized;
            Vector3 newFlatPos = flatSelf + dir * moveSpeed * dt;

            // Keep current Y; XZ follows the target
            transform.position = new Vector3(newFlatPos.x, transform.position.y, newFlatPos.z);
        }
        else
        {
            // Reached the target -> start waiting phase
            state = OrbState.WaitingAtTarget;
            stateTimer = 0f;
        }
    }

    private void HandleFloating()
    {
        // Simple bobbing around baseY
        float floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        Vector3 pos = transform.position;
        pos.y = baseY + floatY;
        transform.position = pos;
    }

    private void HandleFade()
    {
        float alpha = 1f;

        // Fade-in during the first fadeInDuration seconds
        if (fadeInDuration > 0f && age <= fadeInDuration)
        {
            alpha = Mathf.Clamp01(age / fadeInDuration);
        }

        // Fade-out when in FadingOut state
        if (state == OrbState.FadingOut && fadeOutDuration > 0f)
        {
            float t = Mathf.Clamp01(stateTimer / fadeOutDuration);
            alpha = Mathf.Lerp(1f, 0f, t);

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
        }

        SetAlpha(alpha);
    }

    private void SetAlpha(float a)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            return;

        foreach (var sr in spriteRenderers)
        {
            if (sr == null) continue;

            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}
