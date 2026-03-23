using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//bro this script is like teachign a baby how to walk
// after all this shit and balls you tell me the best is just walk and sprint in the most basic matter????
//hell nah overthinking is crazyyyyyy.
public class PlayerMovement : MonoBehaviour
{
    public float worldRotationOffset = -44.6f;

    [Header("Movement Speeds")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float sneakSpeed = 2.5f;

    [Header("Movement Keys")]
    public KeyCode moveUpKey = KeyCode.W;
    public KeyCode moveDownKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;

    [Header("Sprint Settings")]
    public float sprintDuration = 3f;
    public float sprintCooldown = 2f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode sneakKey = KeyCode.LeftControl;

    [Header("Trap / Immobilize")]
    public bool isImmobilized = false;

    [Header("Enemy Proximity Settings")]
    public float mediumDangerRadius = 5f;
    public float closeDrainMultiplier = 4f;
    public string enemyTag = "Enemy";

    [Header("Camera Zoom Settings")]
    public Camera mainCamera;
    public float normalSize = 5f;
    public float sneakSize = 4f;
    public float zoomSpeed = 5f;

    [Header("UI")]
    public SprintBarUI sprintBarUI;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;
    private bool isGrounded;

    [Header("Gravity")]
    public float gravityMultiplier = 2.5f;

    private float sprintTimer = 0f;
    private bool isSprinting = false;
    private bool isSneaking = false;

    private Rigidbody rb;
    private Vector3 input;

    private enum MovementMode { Normal, Sprinting, Sneaking }
    private MovementMode currentMode = MovementMode.Normal;
    private KeyCode lastPressedKey = KeyCode.None;

    public bool IsSneaking => isSneaking;

    [Header("Facing / Backward Settings (Unused now)")]
    [Range(0f, 1f)] public float backwardMultiplier = 0.6f;
    [Range(0f, 180f)] public float forwardAngleForSprint = 45f;

    private Vector3 facingDirection = Vector3.forward;
    public Vector3 FacingDirection => facingDirection;

    // --- Animation support ---
    private Vector2 lastMoveDir = Vector2.zero;
    public Vector2 GetLastMoveDirection() => lastMoveDir;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;

        sprintTimer = sprintDuration;
        facingDirection = transform.forward;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        HandleInput();
        HandleSprintSneakLogic();
        UpdateCameraZoom();
        UpdateSprintBarUI();
    }

    void FixedUpdate()
    {
        if (isImmobilized)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        float currentSpeed = moveSpeed;

        if (currentMode == MovementMode.Sprinting)
            currentSpeed = sprintSpeed;
        else if (currentMode == MovementMode.Sneaking)
            currentSpeed = sneakSpeed;

        // Rotate input into isometric space
        Vector3 rotatedInput = Quaternion.AngleAxis(worldRotationOffset, Vector3.up) * input;

        Vector3 horizontalVelocity = new Vector3(
            rotatedInput.x * currentSpeed,
            0f,
            rotatedInput.z * currentSpeed
        );

        rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);

        if (!isGrounded)
        {
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    // ================= INPUT =================
    void HandleInput()
    {
        if (isImmobilized)
        {
            input = Vector3.zero;
            return;
        }

        float x = 0f;
        float z = 0f;

        if (Input.GetKey(moveLeftKey)) x -= 1f;
        if (Input.GetKey(moveRightKey)) x += 1f;
        if (Input.GetKey(moveDownKey)) z -= 1f;
        if (Input.GetKey(moveUpKey)) z += 1f;

        Vector3 rawInput = new Vector3(x, 0f, z);

        if (rawInput.sqrMagnitude > 0.01f)
        {
            rawInput.Normalize();

            input = SnapTo8Directions(rawInput);

            // ✅ FIX: Use rotated direction for animation
            Vector3 rotated = Quaternion.AngleAxis(worldRotationOffset, Vector3.up) * input;
            lastMoveDir = new Vector2(rotated.x, rotated.z);
        }
        else
        {
            input = Vector3.zero;
        }

        if (Input.GetKeyDown(sprintKey)) lastPressedKey = sprintKey;
        if (Input.GetKeyDown(sneakKey)) lastPressedKey = sneakKey;
    }

    // ================= SPRINT / SNEAK =================
    void HandleSprintSneakLogic()
    {
        if (isImmobilized) return;

        bool holdingSprint = Input.GetKey(sprintKey);
        bool holdingSneak = Input.GetKey(sneakKey);
        bool isPhysicallyMoving = rb.velocity.magnitude > 0.05f;
        float drainMultiplier = GetSprintDrainMultiplier();

        // ✅ SIMPLIFIED: No direction restriction
        if (holdingSprint && (!holdingSneak || lastPressedKey == sprintKey) &&
            sprintTimer > 0f && isPhysicallyMoving)
        {
            currentMode = MovementMode.Sprinting;
            sprintTimer -= Time.deltaTime * drainMultiplier;
            sprintTimer = Mathf.Max(sprintTimer, 0f);
        }
        else if (holdingSneak && (!holdingSprint || lastPressedKey == sneakKey))
        {
            currentMode = MovementMode.Sneaking;
        }
        else
        {
            currentMode = MovementMode.Normal;
        }

        if (currentMode != MovementMode.Sprinting)
        {
            sprintTimer += Time.deltaTime * (sprintDuration / sprintCooldown);
            sprintTimer = Mathf.Min(sprintTimer, sprintDuration);
        }

        isSprinting = currentMode == MovementMode.Sprinting;
        isSneaking = currentMode == MovementMode.Sneaking;
    }

    // ================= CAMERA =================
    void UpdateCameraZoom()
    {
        if (mainCamera == null) return;

        float targetSize = isSneaking ? sneakSize : normalSize;
        mainCamera.orthographicSize =
            Mathf.Lerp(mainCamera.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
    }

    // ================= UI =================
    void UpdateSprintBarUI()
    {
        if (sprintBarUI == null) return;

        float percent = sprintTimer / sprintDuration;
        bool sprintingNow = isSprinting && rb.velocity.magnitude > 0.05f;

        sprintBarUI.UpdateSprintBar(percent, sprintingNow);
    }

    // ================= IMMOBILIZE =================
    public IEnumerator Immobilize(float duration)
    {
        isImmobilized = true;
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        yield return new WaitForSeconds(duration);
        isImmobilized = false;
    }

    public void TrapImmobilize()
    {
        isImmobilized = true;
    }

    public void TrapRelease()
    {
        isImmobilized = false;
    }

    // ================= HELPERS =================
    float GetSprintDrainMultiplier()
    {
        float highestMultiplier = 1f;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist <= mediumDangerRadius)
            {
                float t = Mathf.InverseLerp(mediumDangerRadius, 0f, dist);
                float scaled = Mathf.Lerp(1f, closeDrainMultiplier, t);
                highestMultiplier = Mathf.Max(highestMultiplier, scaled);
            }
        }
        return highestMultiplier;
    }

    public void SetFacingDirection(Vector3 newFacing)
    {
        if (isSneaking) return;
        if (newFacing.sqrMagnitude <= 0.001f) return;

        newFacing.y = 0f;
        facingDirection = newFacing.normalized;

        transform.forward = facingDirection;
    }

    Vector3 SnapTo8Directions(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angle / 45f) * 45f;
        float rad = snapped * Mathf.Deg2Rad;

        return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)).normalized;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, mediumDangerRadius);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}