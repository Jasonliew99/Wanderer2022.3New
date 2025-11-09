using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class PlayerSpriteAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer; // Your single sprite

    [Header("Settings")]
    public Transform playerTransform;

    private Vector3 lastMoveDir = Vector3.down; // default facing

    void Update()
    {
        Vector3 moveDir = playerTransform.forward; // or your input dir
        if (moveDir.sqrMagnitude > 0.001f)
            lastMoveDir = moveDir.normalized;

        UpdateSprite(lastMoveDir);
    }

    void UpdateSprite(Vector3 dir)
    {
        // Convert direction to angle
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // Determine animation state (simplified 4 main directions)
        string animState = "Idle";

        if (angle >= -45 && angle < 45)          // Right side
            animState = "Right";
        else if (angle >= 45 && angle < 135)     // Up
            animState = "Up";
        else if (angle >= -135 && angle < -45)   // Down
            animState = "Down";
        else                                     // Left side
            animState = "Right"; // reuse right animation

        animator.Play(animState);

        // Flip for left side
        if (angle < -45 && angle >= -135 || angle > 135)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;
    }
}
