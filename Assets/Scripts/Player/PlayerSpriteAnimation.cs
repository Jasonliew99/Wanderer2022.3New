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
    public Transform meshTransform;
    public Rigidbody rb;
    public TorchlightManager torchManager;
    [Header("Settings")]
    public float idleThreshold = 0.05f;

    void Start()
    {
        player = GetComponentInParent<PlayerMovement>();
        if (torchManager == null)
            torchManager = GetComponentInParent<TorchlightManager>();
    }

    void Update()
    {
        bool isMoving = rb.velocity.sqrMagnitude > idleThreshold * idleThreshold;
        UpdateSpriteByFacing(isMoving);
    }

    void UpdateSpriteByFacing(bool isMoving)
    {
        if (torchManager == null) return;

        int zone = torchManager.CurrentSpriteZone;

        string anim = "";
        bool flipX = false;

        switch (zone)
        {
            case 0: // Up-Right
                anim = "UpRight2pm";
                flipX = false;
                break;
            case 1: // Up-Left
                anim = "UpRight2pm";
                flipX = true;
                break;
            case 2: // Down-Right
                anim = "DownRight5pm";
                flipX = false;
                break;
            case 3: // Down-Left
                anim = "DownRight5pm";
                flipX = true;
                break;
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
