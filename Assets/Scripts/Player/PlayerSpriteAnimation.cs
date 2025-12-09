using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class PlayerSpriteAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform meshTransform;   // Invisible 3D mesh/capsule
    public Rigidbody rb;              // Player RB for movement detection

    [Header("Settings")]
    public float idleThreshold = 0.05f;

    void Update()
    {
        bool isMoving = rb.velocity.sqrMagnitude > idleThreshold * idleThreshold;
        UpdateSpriteByFacing(isMoving);
    }

    void UpdateSpriteByFacing(bool isMoving)
    {
        // --- GET MESH FACING DIRECTION ---
        Vector3 f = meshTransform.forward;
        f.y = 0;
        f.Normalize();

        // --- ROUND TO NEAREST 8-DIRECTION ---
        Vector3 d = new Vector3(
            Mathf.Round(f.x),
            0,
            Mathf.Round(f.z)
        );

        string anim = "";
        bool flipX = false;

        // -----------------------------
        // 6-direction sprite mapping
        // -----------------------------

        // EAST (3pm)
        if (d == new Vector3(1, 0, 0))
        {
            anim = "Right3pm";
            flipX = false;
        }
        // WEST (9pm)
        else if (d == new Vector3(-1, 0, 0))
        {
            anim = "Right3pm";
            flipX = true;
        }
        // NORTH-EAST (2pm)
        else if (d == new Vector3(1, 0, 1))
        {
            anim = "UpRight2pm";
            flipX = false;
        }
        // NORTH-WEST (11pm)
        else if (d == new Vector3(-1, 0, 1))
        {
            anim = "UpRight2pm";
            flipX = true;
        }
        // SOUTH-EAST (5pm)
        else if (d == new Vector3(1, 0, -1))
        {
            anim = "DownRight5pm";
            flipX = false;
        }
        // SOUTH-WEST (7pm)
        else if (d == new Vector3(-1, 0, -1))
        {
            anim = "DownRight5pm";
            flipX = true;
        }
        // PURE NORTH → fallback to 2pm
        else if (d == new Vector3(0, 0, 1))
        {
            anim = "UpRight2pm";
            flipX = false;
        }
        // PURE SOUTH → fallback to 5pm
        else if (d == new Vector3(0, 0, -1))
        {
            anim = "DownRight5pm";
            flipX = false;
        }

        // Apply flipping
        spriteRenderer.flipX = flipX;

        // -----------------------------
        // MOVEMENT vs IDLE LOGIC
        // -----------------------------
        if (!isMoving)
        {
            // RESET animation to frame 0 AND freeze
            animator.speed = 1f;                     // allow Play() to change frame
            animator.Play(anim, 0, 0f);              // restart at frame 0
            animator.Update(0f);                     // FORCE immediate apply
            animator.speed = 0f;                     // freeze on frame 0
            return;
        }

        // If moving:
        animator.speed = 1f;
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(anim))
            animator.Play(anim);
    }
}
