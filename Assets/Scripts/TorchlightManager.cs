using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchlightManager : MonoBehaviour
{
    public enum TorchMode
    {
        KeyboardFixed,   // Your current system (arrow keys for tilt, WASD for facing)
        MouseFixed,      // Mouse controls facing, torch snaps to 3 fixed angles
        MouseFree        // Mouse controls facing, torch can move freely within radius
    }

    [Header("Torchlight Settings")]
    public TorchMode torchMode = TorchMode.KeyboardFixed;
    public Transform flashlight;
    public float offsetDistance = 0.5f;
    public float heightOffset = 0.2f;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Torchlight Rotation Speeds")]
    public float rotationSpeed = 10f;
    public float tiltRotationSpeed = 20f;

    [Header("Tilt Settings")]
    [Range(0f, 90f)] public float tiltAngle = 60f; // For fixed tilt
    [Range(0f, 90f)] public float freeAimRadius = 45f; // For mouse free mode

    [Header("Direction Keys (Keyboard Mode Only)")]
    public KeyCode upKey = KeyCode.UpArrow;
    public KeyCode downKey = KeyCode.DownArrow;
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;

    [Header("Movement Keys (Keyboard Mode Only)")]
    public KeyCode moveUpKey = KeyCode.W;
    public KeyCode moveDownKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;

    private Vector2 lastInputDir = Vector2.down;
    private int currentTilt = 0;
    private Vector3 lastFlashlightDir;

    void Update()
    {
        if (torchMode == TorchMode.KeyboardFixed)
        {
            HandleKeyboardInput();
            HandleFlashlightTilt();
        }
        else if (torchMode == TorchMode.MouseFixed)
        {
            HandleMouseFixed();
        }
        else if (torchMode == TorchMode.MouseFree)
        {
            HandleMouseFree();
        }
    }

    void LateUpdate()
    {
        if (flashlight == null || !flashlight.gameObject.activeSelf) return;

        Vector3 dir3D = lastFlashlightDir.normalized;
        Vector3 targetPos = transform.position + dir3D * offsetDistance + Vector3.up * heightOffset;

        flashlight.position = targetPos;
        if (dir3D != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir3D);
            flashlight.rotation = Quaternion.Slerp(flashlight.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // ----------------------------
    // MODE 1: Keyboard Fixed
    // ----------------------------
    void HandleKeyboardInput()
    {
        Vector2 movementInput = Vector2.zero;
        if (Input.GetKey(moveUpKey)) movementInput += Vector2.up;
        if (Input.GetKey(moveDownKey)) movementInput += Vector2.down;
        if (Input.GetKey(moveLeftKey)) movementInput += Vector2.left;
        if (Input.GetKey(moveRightKey)) movementInput += Vector2.right;

        if (movementInput != Vector2.zero)
            lastInputDir = movementInput.normalized;

        Vector3 dir3D = GetDirectionFromFacing(lastInputDir, currentTilt);
        lastFlashlightDir = dir3D;
    }

    void HandleFlashlightTilt()
    {
        currentTilt = 0;
        bool up = Input.GetKey(upKey);
        bool down = Input.GetKey(downKey);
        bool left = Input.GetKey(leftKey);
        bool right = Input.GetKey(rightKey);

        Vector2 facing = GetCardinalDirection(lastInputDir);

        if (facing == Vector2.up)
        {
            if (left || down) currentTilt = -1;
            else if (right) currentTilt = 1;
        }
        else if (facing == Vector2.right)
        {
            if (left || up) currentTilt = -1;
            else if (down) currentTilt = 1;
        }
        else if (facing == Vector2.down)
        {
            if (right || up) currentTilt = -1;
            else if (left) currentTilt = 1;
        }
        else if (facing == Vector2.left)
        {
            if (up || right) currentTilt = 1;
            else if (down) currentTilt = -1;
        }
    }

    Vector3 GetDirectionFromFacing(Vector2 facingDir, int tilt)
    {
        Vector3 baseDir = new Vector3(facingDir.x, 0f, facingDir.y);
        if (tilt == 0) return baseDir;

        float angle = (tilt == -1) ? -tiltAngle : tiltAngle;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
        return rotation * baseDir;
    }

    // ----------------------------
    // MODE 2: Mouse Fixed
    // ----------------------------
    void HandleMouseFixed()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float hitDist))
        {
            Vector3 hitPoint = ray.GetPoint(hitDist);
            Vector3 dir = (hitPoint - transform.position).normalized;

            // Snap to 3 fixed directions (center, left tilt, right tilt)
            float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

            if (angle < -tiltAngle / 2f) dir = Quaternion.AngleAxis(-tiltAngle, Vector3.up) * transform.forward;
            else if (angle > tiltAngle / 2f) dir = Quaternion.AngleAxis(tiltAngle, Vector3.up) * transform.forward;
            else dir = transform.forward;

            lastFlashlightDir = dir;
        }
    }

    // ----------------------------
    // MODE 3: Mouse Free Radius
    // ----------------------------
    void HandleMouseFree()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float hitDist))
        {
            Vector3 hitPoint = ray.GetPoint(hitDist);
            Vector3 dir = (hitPoint - transform.position).normalized;

            // Clamp within radius
            float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            angle = Mathf.Clamp(angle, -freeAimRadius, freeAimRadius);

            dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            lastFlashlightDir = dir;
        }
    }

    // Helpers
    Vector2 GetCardinalDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return Vector2.down;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? Vector2.right : Vector2.left;
        else
            return dir.y > 0 ? Vector2.up : Vector2.down;
    }
}