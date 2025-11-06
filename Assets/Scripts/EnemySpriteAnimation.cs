using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpriteAnimation : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Animation Clips (Drag & Drop)")]
    public AnimationClip frontRightClip; // SE (facing player)
    public AnimationClip backRightClip;  // NE (back facing)

    [Header("Settings")]
    public float directionBuffer = 10f; // prevents jitter near boundaries

    private string currentClipName = "";
    private float lastAngle = 0f;

    void Update()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 0.01f)
        {
            PlayMovementAnimation(velocity.normalized);
        }
    }

    void PlayMovementAnimation(Vector3 dir)
    {
        // Get angle in 360° range
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // Only update if direction changed significantly
        if (Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle)) < directionBuffer)
            return;

        lastAngle = angle;

        AnimationClip clipToPlay = null;
        bool flipX = false;

        // --- Direction Zones ---
        // SE: 0°–90°
        if (angle >= 0f && angle < 90f)
        {
            clipToPlay = frontRightClip;
            flipX = false;
        }
        // SW: 90°–180°
        else if (angle >= 90f && angle < 180f)
        {
            clipToPlay = frontRightClip;
            flipX = true;
        }
        // NW: 180°–270°
        else if (angle >= 180f && angle < 270f)
        {
            clipToPlay = backRightClip;
            flipX = true;
        }
        // NE: 270°–360°
        else if (angle >= 270f && angle < 360f)
        {
            clipToPlay = backRightClip;
            flipX = false;
        }

        // Apply flip
        spriteRenderer.flipX = flipX;

        // Play only if changed
        if (clipToPlay != null && clipToPlay.name != currentClipName)
        {
            animator.Play(clipToPlay.name);
            currentClipName = clipToPlay.name;
        }
    }
}
