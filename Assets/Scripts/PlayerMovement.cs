using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
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
    public RectTransform sprintBarFill;
    public CanvasGroup sprintBarGroup;

    [Header("Sprint Bar Fade Settings")]
    public float fadeOutDelay = 1.5f;
    public float fadeDuration = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;
    private bool isGrounded;

    [Header("Gravity")]
    public float gravityMultiplier = 2.5f; // makes falling heavier

    private float sprintTimer = 0f;
    private bool isSprinting = false;
    private bool isSneaking = false;
    private float fadeTimer = 0f;
    private bool isFading = false;

    private Rigidbody rb;
    private Vector3 input;

    private enum MovementMode { Normal, Sprinting, Sneaking }
    private MovementMode currentMode = MovementMode.Normal;
    private KeyCode lastPressedKey = KeyCode.None;

    public bool IsSneaking => isSneaking;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true; // keep Unity gravity
        sprintTimer = sprintDuration;

        if (sprintBarGroup != null)
        {
            sprintBarGroup.alpha = 1f;
        }
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        HandleInput();
        HandleSprintSneakLogic();
        UpdateCameraZoom();
        UpdateSprintUI();
    }

    void FixedUpdate()
    {
        float currentSpeed = moveSpeed;

        switch (currentMode)
        {
            case MovementMode.Sneaking:
                currentSpeed = sneakSpeed;
                break;
            case MovementMode.Sprinting:
                currentSpeed = sprintSpeed;
                break;
        }

        // Preserve gravity by not touching Y
        Vector3 horizontalVelocity = new Vector3(input.x * currentSpeed, 0f, input.z * currentSpeed);
        rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);

        // Add stronger gravity when in air
        if (!isGrounded)
        {
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    void HandleInput()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(moveLeftKey)) x -= 1f;
        if (Input.GetKey(moveRightKey)) x += 1f;
        if (Input.GetKey(moveDownKey)) z -= 1f;
        if (Input.GetKey(moveUpKey)) z += 1f;

        input = new Vector3(x, 0f, z).normalized;

        if (input != Vector3.zero)
        {
            transform.forward = input;
        }

        if (Input.GetKeyDown(sprintKey)) lastPressedKey = sprintKey;
        if (Input.GetKeyDown(sneakKey)) lastPressedKey = sneakKey;
    }

    //sprint sneak logic
    void HandleSprintSneakLogic()
    {
        bool holdingSprint = Input.GetKey(sprintKey);
        bool holdingSneak = Input.GetKey(sneakKey);
        bool isPhysicallyMoving = rb.velocity.magnitude > 0.05f;
        float drainMultiplier = GetSprintDrainMultiplier();

        if (holdingSprint && (!holdingSneak || lastPressedKey == sprintKey) && sprintTimer > 0f && isPhysicallyMoving)
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

        isSprinting = (currentMode == MovementMode.Sprinting);
        isSneaking = (currentMode == MovementMode.Sneaking);
    }

    //camera zoom
    void UpdateCameraZoom()
    {
        if (mainCamera == null) return;

        float targetSize = isSneaking ? sneakSize : normalSize;
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
    }

    //sprint ui
    void UpdateSprintUI()
    {
        float percent = Mathf.Clamp01(sprintTimer / sprintDuration);

        if (sprintBarFill != null)
        {
            sprintBarFill.localScale = new Vector3(percent, 1f, 1f);
        }

        if (sprintBarGroup == null) return;

        bool isSprintingNow = isSprinting && rb.velocity.magnitude > 0.05f;
        bool staminaNotFull = !Mathf.Approximately(percent, 1f);

        if (isSprintingNow || staminaNotFull)
        {
            fadeTimer = 0f;

            if (isFading)
            {
                StopAllCoroutines();
                StartCoroutine(FadeCanvasGroup(sprintBarGroup, sprintBarGroup.alpha, 1f, fadeDuration));
                isFading = false;
            }
        }
        else
        {
            fadeTimer += Time.deltaTime;

            if (fadeTimer >= fadeOutDelay && !isFading)
            {
                StartCoroutine(FadeCanvasGroup(sprintBarGroup, sprintBarGroup.alpha, 0f, fadeDuration));
                isFading = true;
            }
        }
    }

    //spritn drain
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

    //fade ui sprint bar
    IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        group.alpha = endAlpha;
    }

    //gizmo for enemy proximity and ground check
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