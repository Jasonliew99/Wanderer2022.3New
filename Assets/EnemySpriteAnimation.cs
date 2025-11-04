using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpriteAnimation : MonoBehaviour
{
    [Header("References")]
    public Transform enemyBody; // The cube / main body
    public Camera mainCamera;
    public SpriteRenderer spriteRenderer;

    [Header("Sprites for Directions")]
    public Sprite front;
    public Sprite back;
    public Sprite left;
    public Sprite right;
    public Sprite frontLeft;
    public Sprite frontRight;
    public Sprite backLeft;
    public Sprite backRight;

    private Vector3 lastDirection;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Hide cube mesh if it has one
        MeshRenderer mesh = enemyBody.GetComponent<MeshRenderer>();
        if (mesh != null)
            mesh.enabled = false;
    }

    void LateUpdate()
    {
        // === 1. Billboard (full face to camera, including tilt) ===
        transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);

        // === 2. Get cube’s facing direction (movement direction) ===
        Vector3 dir = enemyBody.forward;
        dir.y = 0; // ignore vertical
        dir.Normalize();

        // === 3. Use last known direction if not moving ===
        if (dir.magnitude > 0.1f)
            lastDirection = dir;

        // === 4. Update sprite based on direction ===
        UpdateSpriteDirection(lastDirection);
    }

    void UpdateSpriteDirection(Vector3 dir)
    {
        if (dir == Vector3.zero) return;

        // Calculate angle based on cube forward
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;

        if (angle >= 337.5f || angle < 22.5f)
            spriteRenderer.sprite = front;
        else if (angle < 67.5f)
            spriteRenderer.sprite = frontRight;
        else if (angle < 112.5f)
            spriteRenderer.sprite = right;
        else if (angle < 157.5f)
            spriteRenderer.sprite = backRight;
        else if (angle < 202.5f)
            spriteRenderer.sprite = back;
        else if (angle < 247.5f)
            spriteRenderer.sprite = backLeft;
        else if (angle < 292.5f)
            spriteRenderer.sprite = left;
        else
            spriteRenderer.sprite = frontLeft;
    }
}
