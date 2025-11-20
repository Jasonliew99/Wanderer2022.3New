using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using static System.Net.Mime.MediaTypeNames;

[System.Serializable]
public class TorchThreshold
{
    [Tooltip("Battery value (0..1). Threshold applies when battery <= this value.")]
    [Range(0f, 1f)] public float thresholdValue = 0.2f;

    [Tooltip("Color for the battery bar when this threshold is active.")]
    public Color barColor = Color.red;

    [Tooltip("If true, the battery bar will be shown when battery <= thresholdValue.")]
    public bool showBarWhenReached = true;

    [Tooltip("If true, the warning icon will show (when torch is draining) at this threshold.")]
    public bool showWarningSign = true;

    [Tooltip("Pulse speed for the warning icon at this threshold.")]
    public float warningPulseSpeed = 3f;

    [Tooltip("Enable/disable this threshold without removing it.")]
    public bool enabled = true;
}

public class TorchlightManager : MonoBehaviour
{
    public enum TorchMode { KeyboardFixed, MouseFixed, MouseFree }

    [Header("Torchlight Settings")]
    public TorchMode torchMode = TorchMode.MouseFree; // default to MouseFree as requested
    public Transform flashlight;
    public float offsetDistance = 0.5f;
    public float heightOffset = 0.2f;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Rotation Speeds")]
    public float rotationSpeed = 10f;
    public float tiltRotationSpeed = 20f;

    [Header("Tilt Settings")]
    [Range(0f, 90f)] public float tiltAngle = 60f;

    [Header("Free-Aim Settings (angles in degrees)")]
    [Tooltip("Max angle (degrees) the torch can deviate from player's facing before we consider snapping the player.")]
    [Range(10f, 180f)]
    public float freeAimRadius = 60f; // default 60, adjustable

    [Tooltip("A small tolerance (deg) beyond the radius before snapping to avoid tiny overshoots.")]
    public float snapTolerance = 10f; // small buffer

    [Tooltip("Time (seconds) the aim must remain beyond (freeAimRadius + snapTolerance) before snapping.")]
    public float snapHoldTime = 0.18f; // small hold to require intentional aim

    [Header("Direction Keys (unused in MouseFree but kept)")]
    public KeyCode upKey = KeyCode.UpArrow;
    public KeyCode downKey = KeyCode.DownArrow;
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;

    [Header("References")]
    public PlayerMovement player;
    public Light lightSource;
    public Camera mainCamera; // explicit camera ref (if null uses Camera.main)

    [Header("Battery Settings")]
    [Range(0f, 1f)] public float battery = 1f;
    public float drainSpeed = 0.05f;
    public float rechargeSpeed = 0.1f;
    public KeyCode rechargeKey = KeyCode.R;

    [Header("Flicker Settings")]
    public bool enableFlicker = true;
    [Range(0f, 1f)] public float flickerEventChance = 0.05f;
    public int flickerCount = 3;
    public float flickerInterval = 0.05f;
    [Range(0f, 2f)] public float minIntensity = 0.7f;
    [Range(0f, 2f)] public float maxIntensity = 1.2f;

    [Header("Warning / Brightness Settings")]
    [Range(0f, 1f)] public float warningThreshold = 0.4f;
    [Range(0f, 1f)] public float criticalThreshold = 0.15f;
    [Range(0f, 1f)] public float maxBrightnessAtWarning = 0.7f;
    [Range(0f, 1f)] public float maxBrightnessAtCritical = 0.5f;

    [Header("UI - Battery & Warning")]
    public Image batteryFillImage;
    public RectTransform batteryFillRect;
    public CanvasGroup uiCanvasGroup;
    public Image warningIcon;

    [Header("UI - Behavior")]
    public bool smoothFill = true;
    public float fillLerpSpeed = 8f;
    public float uiVisibleDuration = 1.5f;
    public float uiFadeSpeed = 5f;

    [Header("Threshold List (customizable)")]
    public List<TorchThreshold> thresholds = new List<TorchThreshold>();

    // internal
    private Vector2 lastInputDir = Vector2.down;
    private int currentTilt = 0;
    private Vector3 lastFlashlightDir;
    private float baseIntensity;
    private bool isTorchOn = true;
    private Coroutine warningPulseRoutine;
    private float targetFill = 1f;
    private float visibleTimer = 0f;
    private TorchThreshold activeThreshold = null;

    // --- new internal snap timer ---
    private float snapTimer = 0f;

    public bool IsTorchOn => isTorchOn;
    public float BatteryPercent => battery;
    public bool IsRecharging => Input.GetKey(rechargeKey);

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (lightSource != null)
        {
            baseIntensity = lightSource.intensity;
            battery = 1f;
            isTorchOn = true;
            lightSource.enabled = isTorchOn;
            SetupBatteryImage();
            StartCoroutine(FlickerRoutine());
        }

        if (uiCanvasGroup != null)
            uiCanvasGroup.alpha = 0f;

        if (warningIcon != null)
            warningIcon.gameObject.SetActive(false);

        // ensure we have a valid player facing initial value
        if (player != null)
        {
            // initialize lastFlashlightDir to player's facing
            lastFlashlightDir = player.FacingDirection;
            if (lastFlashlightDir == Vector3.zero)
                lastFlashlightDir = transform.forward;
        }
    }

    void Update()
    {
        if (flashlight == null || player == null) return;

        HandleToggle();
        HandleTorchBattery();

        bool torchUsing = isTorchOn && !Input.GetKey(rechargeKey) && battery > 0f;

        if (lightSource != null)
            lightSource.enabled = isTorchOn && battery > 0f;

        if (isTorchOn && battery > 0f)
        {
            HandleTorchDirection();
            HandleBatteryBrightness();
        }

        UpdateIntegratedUI(battery, isTorchOn, Input.GetKey(rechargeKey), torchUsing);

        if (batteryFillImage != null && smoothFill)
            batteryFillImage.fillAmount = Mathf.Lerp(batteryFillImage.fillAmount, targetFill, Time.deltaTime * fillLerpSpeed);
        else if (batteryFillImage != null)
            batteryFillImage.fillAmount = targetFill;

        if (batteryFillRect != null && batteryFillImage == null)
        {
            Vector3 cur = batteryFillRect.localScale;
            float t = smoothFill ? (1f - Mathf.Exp(-fillLerpSpeed * Time.deltaTime)) : 1f;
            cur.x = Mathf.Lerp(cur.x, targetFill, t);
            batteryFillRect.localScale = cur;
        }

        HandleUIFade();
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
    // Battery & Recharge
    // ----------------------------
    void HandleTorchBattery()
    {
        bool isRecharging = Input.GetKey(rechargeKey);
        if (isTorchOn && !isRecharging)
            battery -= drainSpeed * Time.deltaTime;

        if (isRecharging)
            battery += rechargeSpeed * Time.deltaTime;

        battery = Mathf.Clamp01(battery);
        targetFill = battery;

        if (battery <= 0f)
        {
            isTorchOn = false;
            if (lightSource != null)
                lightSource.enabled = false;
        }
    }

    void HandleBatteryBrightness()
    {
        if (lightSource == null) return;
        float brightness = baseIntensity;

        if (battery <= criticalThreshold)
            brightness *= maxBrightnessAtCritical;
        else if (battery <= warningThreshold)
            brightness *= maxBrightnessAtWarning;

        lightSource.intensity = brightness;
    }

    void HandleToggle()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (battery > 0f)
            {
                isTorchOn = !isTorchOn;
                ShowTemporaryUI();
            }
        }
    }

    public void ShowTemporaryUI()
    {
        visibleTimer = uiVisibleDuration;
        if (uiCanvasGroup != null)
            uiCanvasGroup.alpha = 1f;
    }

    // ----------------------------
    // Torch Direction Logic
    // ----------------------------
    void HandleTorchDirection()
    {
        switch (torchMode)
        {
            case TorchMode.KeyboardFixed:
                HandleKeyboardFixed();
                HandleFlashlightTilt();
                break;
            case TorchMode.MouseFixed:
                HandleMouseFixed();
                break;
            case TorchMode.MouseFree:
                HandleMouseFree(); // We'll use MouseFree primarily
                break;
        }
    }

    void HandleKeyboardFixed()
    {
        Vector3 forward = player.transform.forward;
        lastInputDir = new Vector2(forward.x, forward.z).normalized;
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

    Vector2 GetCardinalDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return Vector2.down;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? Vector2.right : Vector2.left;
        else
            return dir.y > 0 ? Vector2.up : Vector2.down;
    }

    void HandleMouseFixed()
    {
        Ray ray = (mainCamera != null ? mainCamera : Camera.main).ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float hitDist))
        {
            Vector3 hitPoint = ray.GetPoint(hitDist);
            Vector3 dir = (hitPoint - transform.position).normalized;

            float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            if (angle < -tiltAngle / 2f)
                dir = Quaternion.AngleAxis(-tiltAngle, Vector3.up) * transform.forward;
            else if (angle > tiltAngle / 2f)
                dir = Quaternion.AngleAxis(tiltAngle, Vector3.up) * transform.forward;
            else
                dir = transform.forward;

            lastFlashlightDir = dir;
        }
    }

    // ----------------------------
    // MouseFree mode (updated)
    // ----------------------------
    void HandleMouseFree()
    {
        Ray ray = (mainCamera != null ? mainCamera : Camera.main).ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (!groundPlane.Raycast(ray, out float hitDist)) return;

        Vector3 hitPoint = ray.GetPoint(hitDist);
        Vector3 aimDir = (hitPoint - transform.position).normalized;
        aimDir.y = 0f;
        if (aimDir.sqrMagnitude <= 0.0001f) return;

        // Use player's facing as the base
        Vector3 playerFacing = player.FacingDirection;
        if (playerFacing.sqrMagnitude <= 0.0001f)
            playerFacing = transform.forward;

        float angleDiff = Vector3.SignedAngle(playerFacing, aimDir, Vector3.up);
        float absAngle = Mathf.Abs(angleDiff);

        // If inside the freeAimRadius => just tilt flashlight
        if (absAngle <= freeAimRadius)
        {
            lastFlashlightDir = Quaternion.AngleAxis(angleDiff, Vector3.up) * playerFacing;
            snapTimer = 0f; // reset snap timer (not aiming to snap)
        }
        else
        {
            // beyond freeAimRadius — check tolerance / hold for intentional snap
            if (absAngle > freeAimRadius + snapTolerance)
            {
                snapTimer += Time.deltaTime;
                // only snap if held long enough (player intentionally aimed)
                if (snapTimer >= snapHoldTime)
                {
                    // Snap: compute nearest 8-way direction from aimDir and tell player
                    Vector3 snapped = Nearest8Direction(aimDir);
                    player.SetFacingDirection(snapped);

                    // set flashlight to the snapped facing (centered)
                    lastFlashlightDir = snapped;

                    snapTimer = 0f; // reset
                }
                else
                {
                    // show flashlight at the clamped edge for visual feedback
                    float sign = Mathf.Sign(angleDiff);
                    float clampedAngle = freeAimRadius * sign;
                    lastFlashlightDir = Quaternion.AngleAxis(clampedAngle, Vector3.up) * playerFacing;
                }
            }
            else
            {
                // small overshoot within tolerance — don't start snap timer yet
                snapTimer = 0f;
                float sign = Mathf.Sign(angleDiff);
                float clampedAngle = freeAimRadius * sign;
                lastFlashlightDir = Quaternion.AngleAxis(clampedAngle, Vector3.up) * playerFacing;
            }
        }
    }

    // Snap facing to nearest of 8 directions (N, NE, E, ...)
    Vector3 Nearest8Direction(Vector3 dir)
    {
        dir.y = 0f;
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; // note: Atan2(x,z) to match previous usage
        float snapped = Mathf.Round(angle / 45f) * 45f;
        float rad = snapped * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)).normalized;
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (!enableFlicker || !isTorchOn || lightSource == null) continue;

            if (Random.value < flickerEventChance)
            {
                for (int i = 0; i < flickerCount; i++)
                {
                    lightSource.intensity = Random.Range(minIntensity, maxIntensity);
                    yield return new WaitForSeconds(flickerInterval);
                    if (Random.value < 0.3f)
                    {
                        lightSource.enabled = false;
                        yield return new WaitForSeconds(flickerInterval);
                        lightSource.enabled = true;
                    }
                }
                HandleBatteryBrightness();
            }
        }
    }

    public Vector3 GetFacingDirection()
    {
        // Returns the torch’s current facing direction (normalized)
        if (lastFlashlightDir.sqrMagnitude < 0.0001f)
            return transform.forward;
        return lastFlashlightDir.normalized;
    }

    void SetupBatteryImage()
    {
        if (batteryFillImage == null) return;
        if (batteryFillImage.type != Image.Type.Filled)
            batteryFillImage.type = Image.Type.Filled;
        batteryFillImage.fillMethod = Image.FillMethod.Horizontal;
        batteryFillImage.fillAmount = battery;
        targetFill = battery;
    }

    void UpdateIntegratedUI(float batteryValue, bool torchOn, bool recharging, bool torchUsing)
    {
        float b = Mathf.Clamp01(batteryValue);

        // --- Determine active threshold ---
        TorchThreshold newActive = null;
        foreach (var t in thresholds)
        {
            if (t == null || !t.enabled) continue;
            if (b <= t.thresholdValue)
            {
                if (newActive == null || t.thresholdValue < newActive.thresholdValue)
                    newActive = t;
            }
        }

        // --- Handle threshold change ---
        if (newActive != activeThreshold)
        {
            activeThreshold = newActive;
            StopWarningPulse();

            // Prevent flicker: extend visibility & smooth transition
            visibleTimer = Mathf.Max(visibleTimer, uiVisibleDuration * 0.5f);
        }

        // --- Smooth color & fill ---
        Color targetColor = (activeThreshold != null) ? activeThreshold.barColor : Color.white;

        if (batteryFillImage != null)
        {
            batteryFillImage.color = Color.Lerp(batteryFillImage.color, targetColor, Time.deltaTime * 6f);
            targetFill = b;
        }
        else if (batteryFillRect != null)
        {
            Image possible = batteryFillRect.GetComponent<Image>();
            if (possible != null)
                possible.color = Color.Lerp(possible.color, targetColor, Time.deltaTime * 6f);

            Vector3 s = batteryFillRect.localScale;
            s.x = b;
            batteryFillRect.localScale = s;
            targetFill = b;
        }

        // --- UI Visibility ---
        bool thresholdVisible = activeThreshold != null && activeThreshold.showBarWhenReached;
        bool shouldShowBar = recharging || thresholdVisible || visibleTimer > 0f;

        if (recharging)
            visibleTimer = Mathf.Max(visibleTimer, uiVisibleDuration);

        if (uiCanvasGroup != null)
        {
            float targetAlpha = shouldShowBar ? 1f : 0f;
            uiCanvasGroup.alpha = Mathf.MoveTowards(uiCanvasGroup.alpha, targetAlpha, uiFadeSpeed * Time.deltaTime);
        }

        // --- Warning icon logic ---
        bool shouldShowWarning = torchUsing && activeThreshold != null && activeThreshold.showWarningSign && !recharging;
        if (shouldShowWarning)
            StartWarningPulse(activeThreshold.warningPulseSpeed);
        else
            StopWarningPulse();
    }

    void StartWarningPulse(float speed)
    {
        if (warningIcon == null) return;
        if (warningPulseRoutine != null)
        {
            StopCoroutine(warningPulseRoutine);
            warningPulseRoutine = null;
        }
        warningIcon.gameObject.SetActive(true);
        warningPulseRoutine = StartCoroutine(WarningPulseCoroutine(speed));
    }

    void StopWarningPulse()
    {
        if (warningIcon == null) return;
        if (warningPulseRoutine != null)
        {
            StopCoroutine(warningPulseRoutine);
            warningPulseRoutine = null;
        }
        warningIcon.gameObject.SetActive(false);
    }

    IEnumerator WarningPulseCoroutine(float speed)
    {
        Color baseColor = warningIcon.color;
        while (true)
        {
            float alpha = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
            float mapped = Mathf.Lerp(0.25f, 1f, alpha);
            warningIcon.color = new Color(baseColor.r, baseColor.g, baseColor.b, mapped);
            yield return null;
        }
    }

    void HandleUIFade()
    {
        if (uiCanvasGroup == null) return;
        bool isRecharging = Input.GetKey(rechargeKey);
        bool thresholdVisible = activeThreshold != null && activeThreshold.showBarWhenReached;

        if (visibleTimer > 0f && !isRecharging && !thresholdVisible)
            visibleTimer -= Time.deltaTime;
    }

    // Draw angle gizmo lines in scene view when object selected
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        Vector3 origin = player.transform.position;

        Vector3 forward = player.FacingDirection;
        if (forward.sqrMagnitude < 0.0001f) forward = player.transform.forward;

        // two rays for +/- freeAimRadius
        Quaternion leftRot = Quaternion.AngleAxis(-freeAimRadius, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(freeAimRadius, Vector3.up);

        Vector3 leftDir = (leftRot * forward).normalized;
        Vector3 rightDir = (rightRot * forward).normalized;

        float len = 2.0f;
        Gizmos.DrawRay(origin, leftDir * len);
        Gizmos.DrawRay(origin, rightDir * len);

        // draw small arc between them (coarse)
        int steps = 18;
        Vector3 prev = leftDir * len;
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            float a = Mathf.Lerp(-freeAimRadius, freeAimRadius, t);
            Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * forward;
            Vector3 cur = dir.normalized * len;
            Gizmos.DrawLine(origin + prev, origin + cur);
            prev = cur;
        }

        // draw the snap threshold lines too
        Gizmos.color = Color.cyan;
        Quaternion leftSnap = Quaternion.AngleAxis(-(freeAimRadius + snapTolerance), Vector3.up);
        Quaternion rightSnap = Quaternion.AngleAxis((freeAimRadius + snapTolerance), Vector3.up);
        Gizmos.DrawRay(origin, (leftSnap * forward).normalized * len * 1.05f);
        Gizmos.DrawRay(origin, (rightSnap * forward).normalized * len * 1.05f);
    }
}
