using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class PlayerSpriteAnimation : MonoBehaviour
{
    [Header("References")]
    private PlayerMovement player;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform meshTransform;   // Invisible 3D mesh/capsule
    public Rigidbody rb;              // Player RB for movement detection

    [Header("Settings")]
    public float idleThreshold = 0.05f;

    void Start()
    {
        player = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        bool isMoving = rb.velocity.sqrMagnitude > idleThreshold * idleThreshold;
        UpdateSpriteByFacing(isMoving);
    }

    void UpdateSpriteByFacing(bool isMoving)
    {
        Vector3 f = player.FacingDirection;
        f.y = 0f;

        if (f.sqrMagnitude < 0.001f)
            return;

        // Undo the isometric rotation used in movement
        f = Quaternion.AngleAxis(44.6f, Vector3.up) * f;

        f.Normalize();

        float angle = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

        string anim = "";
        bool flipX = false;

        if (angle >= 67.5f && angle < 112.5f) // RIGHT
        {
            anim = "Right3pm";
            flipX = false;
        }
        else if (angle >= -112.5f && angle < -67.5f) // LEFT
        {
            anim = "Right3pm";
            flipX = true;
        }
        else if (angle >= -67.5f && angle < 67.5f) // UP / UPRIGHT / UPLEFT
        {
            anim = "UpRight2pm";
            flipX = angle < 0;
        }
        else // DOWN / DOWNRIGHT / DOWNLEFT
        {
            anim = "DownRight5pm";
            flipX = angle < 0;
        }

        spriteRenderer.flipX = flipX;

        if (!isMoving)
        {
            animator.speed = 1f;
            animator.Play(anim, 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
            return;
        }

        animator.speed = 1f;

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(anim))
            animator.Play(anim);
    }
}
