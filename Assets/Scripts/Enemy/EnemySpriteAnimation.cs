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
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform meshTransform; // reference to the actual mesh

    [Header("Animation Clips")]
    public AnimationClip frontRightClip; // SE (front)
    public AnimationClip backRightClip;  // NE (back)

    private string currentClipName = "";

    void Start()
    {
        if (meshTransform == null)
            meshTransform = transform; // default to player object
    }

    void Update()
    {
        PlayFacingAnimation();
    }

    void PlayFacingAnimation()
    {
        float angle = meshTransform.eulerAngles.y;

        // Normalize angle to -180 -> 180
        if (angle > 180f) angle -= 360f;

        AnimationClip clipToPlay = null;
        bool flipX = false;

        // --- Use same mapping as enemy ---
        if (angle >= -45f && angle < 45f)
        {
            // Top-right
            clipToPlay = backRightClip;
            flipX = false;
        }
        else if (angle >= 45f && angle < 135f)
        {
            // Bottom-right
            clipToPlay = frontRightClip;
            flipX = false;
        }
        else if (angle >= 135f || angle < -135f)
        {
            // Bottom-left
            clipToPlay = frontRightClip;
            flipX = true;
        }
        else // angle between -135 and -45
        {
            // Top-left
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
