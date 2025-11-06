using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class PlayerSpriteAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform meshTransform; // actual mesh to read facing direction

    [Header("Animation Clips")]
    public AnimationClip frontClip; // front walking animation
    public AnimationClip backClip;  // back walking animation

    [Header("Settings")]
    public float minMoveSpeed = 0.05f;

    private string currentClipName = "";
    private bool isIdle = true;
    private Rigidbody rb;

    void Start()
    {
        if (meshTransform == null)
            meshTransform = transform; // default to this object

        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // --- Movement check ---
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float speed = velocity.magnitude;

        if (speed <= minMoveSpeed)
        {
            if (!isIdle)
            {
                animator.speed = 0f;
                isIdle = true;
            }
        }
        else
        {
            if (isIdle)
            {
                animator.speed = 1f;
                isIdle = false;
            }
        }

        // --- Determine animation from mesh facing ---
        float angle = meshTransform.eulerAngles.y;

        // Normalize to -180 → 180
        if (angle > 180f) angle -= 360f;

        AnimationClip clipToPlay = null;
        bool flipX = false;

        // --- Four-direction mapping ---
        if (angle >= -45f && angle <= 45f)
        {
            // Top Right
            clipToPlay = backClip;
            flipX = false;
        }
        else if (angle > 45f && angle <= 135f)
        {
            // Bottom Right
            clipToPlay = frontClip;
            flipX = false;
        }
        else if (angle < -45f && angle >= -135f)
        {
            // Top Left
            clipToPlay = backClip;
            flipX = true;
        }
        else // Bottom Left
        {
            clipToPlay = frontClip;
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
