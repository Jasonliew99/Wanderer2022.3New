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
        // USE TRUE GAMEPLAY FACING
        Vector3 f = GetComponentInParent<PlayerMovement>().FacingDirection;

        f.y = 0;
        f.Normalize();

        // Round to nearest 8-dir AFTER gameplay snap
        Vector3 d = new Vector3(
            Mathf.Round(f.x),
            0,
            Mathf.Round(f.z)
        );

        string anim = "";
        bool flipX = false;

        if (d == new Vector3(1, 0, 0))
        {
            anim = "Right3pm";
            flipX = false;
        }
        else if (d == new Vector3(-1, 0, 0))
        {
            anim = "Right3pm";
            flipX = true;
        }
        else if (d == new Vector3(1, 0, 1))
        {
            anim = "UpRight2pm";
            flipX = false;
        }
        else if (d == new Vector3(-1, 0, 1))
        {
            anim = "UpRight2pm";
            flipX = true;
        }
        else if (d == new Vector3(1, 0, -1))
        {
            anim = "DownRight5pm";
            flipX = false;
        }
        else if (d == new Vector3(-1, 0, -1))
        {
            anim = "DownRight5pm";
            flipX = true;
        }
        else if (d == new Vector3(0, 0, 1))
        {
            anim = "UpRight2pm";
            flipX = false;
        }
        else if (d == new Vector3(0, 0, -1))
        {
            anim = "DownRight5pm";
            flipX = false;
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
