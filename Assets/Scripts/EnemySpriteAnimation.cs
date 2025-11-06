using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;

public class EnemySpriteAnimation : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Animation Clips")]
    public AnimationClip frontRightClip; // used for SE (front)
    public AnimationClip backRightClip;  // used for NE (back)

    private string currentClipName = "";

    void Update()
    {
        // Play animation only when moving
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            PlayMovementAnimation();
        }
    }

    void PlayMovementAnimation()
    {
        // Get the cube’s current facing angle (0 = top-right)
        float angle = transform.eulerAngles.y;

        // Normalize to 0–360 for clarity
        if (angle > 180f) angle -= 360f;

        AnimationClip clipToPlay = null;
        bool flipX = false;

        // --- Mapping based on your cube’s facing angles ---
        if (angle >= -45f && angle < 45f)
        {
            // Facing top-right
            clipToPlay = backRightClip;
            flipX = false;
        }
        else if (angle >= 45f && angle < 135f)
        {
            // Facing bottom-right
            clipToPlay = frontRightClip;
            flipX = false;
        }
        else if (angle >= 135f || angle < -135f)
        {
            // Facing bottom-left
            clipToPlay = frontRightClip;
            flipX = true;
        }
        else // angle between -135 and -45
        {
            // Facing top-left
            clipToPlay = backRightClip;
            flipX = true;
        }

        spriteRenderer.flipX = flipX;

        if (clipToPlay != null && clipToPlay.name != currentClipName)
        {
            animator.Play(clipToPlay.name);
            currentClipName = clipToPlay.name;
        }
    }
}
