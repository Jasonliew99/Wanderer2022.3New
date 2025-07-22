using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchlightManager : MonoBehaviour
{
    [Header("Torchlight Settings")]
    public Transform flashlight;
    public float offsetDistance = 0.5f;
    public float heightOffset = 0.2f;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Torchlight Rotation Speeds")]
    public float rotationSpeed = 10f;        // For facing direction changes
    public float tiltRotationSpeed = 20f;    // For arrow-key tilts

    [Header("Tilt Settings")]
    [Range(0f, 90f)]
    public float tiltAngle = 60f; // Degree of tilt

    [Header("Flicker Settings")]
    public bool enableFlicker = true;
    [Range(0f, 1f)]
    public float flickerChancePerSecond = 0.2f;
    public float flickerSpeed = 0.05f;
    public int minFlickers = 1;
    public int maxFlickers = 4;

    [Header("Direction Keys (Arrow Keys for Aiming)")]
    public KeyCode upKey = KeyCode.UpArrow;
    public KeyCode downKey = KeyCode.DownArrow;
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;

    [Header("Movement Keys (WASD or custom)")]
    public KeyCode moveUpKey = KeyCode.W;
    public KeyCode moveDownKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;

    private Vector2 lastInputDir = Vector2.down; // default facing down
    private int currentTilt = 0; // -1 = left, 0 = center, 1 = right
    private Vector3 lastFlashlightDir;

    private bool isFlickering = false;

    void Start()
    {
        if (flashlight != null)
        {
            Vector3 dir3D = new Vector3(lastInputDir.x, 0f, lastInputDir.y);
            Vector3 startPos = transform.position + dir3D * offsetDistance + Vector3.up * heightOffset;
            flashlight.position = startPos;
            flashlight.rotation = Quaternion.LookRotation(dir3D);
            lastFlashlightDir = dir3D;
        }
    }

    void Update()
    {
        // Update facing direction ONLY from movement keys
        Vector2 movementInput = Vector2.zero;
        if (Input.GetKey(moveUpKey)) movementInput += Vector2.up;
        if (Input.GetKey(moveDownKey)) movementInput += Vector2.down;
        if (Input.GetKey(moveLeftKey)) movementInput += Vector2.left;
        if (Input.GetKey(moveRightKey)) movementInput += Vector2.right;

        if (movementInput != Vector2.zero)
            lastInputDir = movementInput.normalized;

        HandleFlashlightTilt();

        if (Input.GetKeyDown(toggleKey) && flashlight != null)
        {
            bool newState = !flashlight.gameObject.activeSelf;
            flashlight.gameObject.SetActive(newState);

            if (newState)
            {
                Vector3 dir3D = GetDirectionFromFacing(lastInputDir, currentTilt);
                flashlight.position = transform.position + dir3D * offsetDistance + Vector3.up * heightOffset;
                flashlight.rotation = Quaternion.LookRotation(dir3D);
                lastFlashlightDir = dir3D;
            }
        }

        if (enableFlicker && flashlight != null && flashlight.gameObject.activeSelf && !isFlickering)
        {
            if (Random.value < flickerChancePerSecond * Time.deltaTime)
            {
                StartCoroutine(FlickerFlashlight());
            }
        }
    }

    void LateUpdate()
    {
        if (flashlight == null || !flashlight.gameObject.activeSelf)
            return;

        Vector3 dir3D = GetDirectionFromFacing(lastInputDir, currentTilt);
        Vector3 targetPos = transform.position + dir3D * offsetDistance + Vector3.up * heightOffset;

        flashlight.position = targetPos;

        if (dir3D != Vector3.zero)
        {
            float speed = (currentTilt == 0) ? rotationSpeed : tiltRotationSpeed;
            Quaternion targetRotation = Quaternion.LookRotation(dir3D);
            flashlight.rotation = Quaternion.Slerp(flashlight.rotation, targetRotation, speed * Time.deltaTime);
            lastFlashlightDir = dir3D;
        }
    }

    void HandleFlashlightTilt()
    {
        currentTilt = 0; // Default to center

        bool up = Input.GetKey(upKey);
        bool down = Input.GetKey(downKey);
        bool left = Input.GetKey(leftKey);
        bool right = Input.GetKey(rightKey);

        Vector2 facing = GetCardinalDirection(lastInputDir);

        if (IsFacingUp(facing))
        {
            if (left || down) currentTilt = -1;  // tilt left
            else if (right) currentTilt = 1;     // tilt right
        }
        else if (IsFacingRight(facing))
        {
            if (left || up) currentTilt = -1;    // tilt left
            else if (down) currentTilt = 1;      // tilt right
        }
        else if (IsFacingDown(facing))
        {
            if (right || up) currentTilt = -1;   // tilt left
            else if (left) currentTilt = 1;      // tilt right
        }
        else if (IsFacingLeft(facing))
        {
            if (up || right) currentTilt = 1;    // tilt right
            else if (down) currentTilt = -1;     // tilt left
        }
    }

    Vector3 GetDirectionFromFacing(Vector2 facingDir, int tilt)
    {
        Vector3 baseDir = new Vector3(facingDir.x, 0f, facingDir.y);

        if (tilt == 0)
            return baseDir;

        float angle = (tilt == -1) ? -tiltAngle : tiltAngle;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
        return rotation * baseDir;
    }

    Vector2 GetCardinalDirection(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return Vector2.down; // default

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? Vector2.right : Vector2.left;
        else
            return dir.y > 0 ? Vector2.up : Vector2.down;
    }

    bool IsFacingUp(Vector2 dir) => dir == Vector2.up;
    bool IsFacingDown(Vector2 dir) => dir == Vector2.down;
    bool IsFacingLeft(Vector2 dir) => dir == Vector2.left;
    bool IsFacingRight(Vector2 dir) => dir == Vector2.right;

    IEnumerator FlickerFlashlight()
    {
        isFlickering = true;

        int flickers = Random.Range(minFlickers, maxFlickers + 1);

        for (int i = 0; i < flickers; i++)
        {
            if (flashlight != null)
                flashlight.gameObject.SetActive(false);

            yield return new WaitForSeconds(flickerSpeed);

            if (flashlight != null)
                flashlight.gameObject.SetActive(true);

            yield return new WaitForSeconds(flickerSpeed);
        }

        isFlickering = false;
    }
}