using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [Header("Torchlight Settings")]
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
    [Range(10f, 180f)] public float freeAimRadius = 60f;
    public float snapTolerance = 10f;
    public float snapHoldTime = 0.18f;

    [Header("References")]
    public PlayerMovement player;
    public Light lightSource;
    public Camera mainCamera;

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

    [Header("Thresholds")]
    public TorchThreshold[] thresholds;

    // Internal
    private Vector3 lastFlashlightDir;
    private float baseIntensity;
    private bool isTorchOn = true;
    private float snapTimer = 0f;
    private float visibleTimer = 0f;
    private float targetFill = 1f;
    private TorchThreshold activeThreshold;
    private Coroutine warningPulseRoutine;

    public bool IsTorchOn => isTorchOn;
    public float BatteryPercent => battery;
    public bool IsRecharging => Input.GetKey(rechargeKey);

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (lightSource != null)
        {
            baseIntensity = lightSource.intensity;
            battery = 1f;
            isTorchOn = true;
            lightSource.enabled = isTorchOn;
            StartCoroutine(FlickerRoutine());
        }

        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
        if (warningIcon != null) warningIcon.gameObject.SetActive(false);

        if (player != null)
        {
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
        if (lightSource != null) lightSource.enabled = torchUsing;

        if (torchUsing) HandleMouseFree();

        HandleBatteryBrightness();
        UpdateIntegratedUI(battery, isTorchOn, Input.GetKey(rechargeKey), torchUsing);
        UpdateUIFill();
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

    void HandleTorchBattery()
    {
        bool isRecharging = Input.GetKey(rechargeKey);
        if (isTorchOn && !isRecharging) battery -= drainSpeed * Time.deltaTime;
        if (isRecharging) battery += rechargeSpeed * Time.deltaTime;

        battery = Mathf.Clamp01(battery);
        if (battery <= 0f) isTorchOn = false;
    }

    void HandleBatteryBrightness()
    {
        if (lightSource == null) return;
        float brightness = baseIntensity;
        if (battery <= criticalThreshold) brightness *= maxBrightnessAtCritical;
        else if (battery <= warningThreshold) brightness *= maxBrightnessAtWarning;
        lightSource.intensity = brightness;
    }

    void HandleToggle()
    {
        if (Input.GetKeyDown(toggleKey) && battery > 0f)
        {
            isTorchOn = !isTorchOn;
            visibleTimer = uiVisibleDuration;
            if (uiCanvasGroup != null) uiCanvasGroup.alpha = 1f;
        }
    }

    void HandleMouseFree()
    {
        Ray ray = (mainCamera != null ? mainCamera : Camera.main).ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (!groundPlane.Raycast(ray, out float hitDist)) return;

        Vector3 hitPoint = ray.GetPoint(hitDist);
        Vector3 aimDir = (hitPoint - transform.position).normalized;
        aimDir.y = 0f;
        if (aimDir.sqrMagnitude <= 0.0001f) return;

        Vector3 playerFacing = player.FacingDirection;
        if (playerFacing.sqrMagnitude <= 0.0001f)
            playerFacing = transform.forward;

        float angleDiff = Vector3.SignedAngle(playerFacing, aimDir, Vector3.up);
        float absAngle = Mathf.Abs(angleDiff);

        if (absAngle <= freeAimRadius)
        {
            lastFlashlightDir = Quaternion.AngleAxis(angleDiff, Vector3.up) * playerFacing;
            snapTimer = 0f;
        }
        else
        {
            if (absAngle > freeAimRadius + snapTolerance)
            {
                snapTimer += Time.deltaTime;
                if (snapTimer >= snapHoldTime)
                {
                    Vector3 snapped = Nearest8Direction(aimDir);
                    player.SetFacingDirection(snapped);
                    lastFlashlightDir = snapped;
                    snapTimer = 0f;
                }
                else
                {
                    float sign = Mathf.Sign(angleDiff);
                    float clampedAngle = freeAimRadius * sign;
                    lastFlashlightDir = Quaternion.AngleAxis(clampedAngle, Vector3.up) * playerFacing;
                }
            }
            else
            {
                float sign = Mathf.Sign(angleDiff);
                float clampedAngle = freeAimRadius * sign;
                lastFlashlightDir = Quaternion.AngleAxis(clampedAngle, Vector3.up) * playerFacing;
                snapTimer = 0f;
            }
        }

        if (player.mesh != null)
            player.mesh.forward = Vector3.Slerp(
                player.mesh.forward,
                player.FacingDirection,
                rotationSpeed * Time.deltaTime
            );
    }

    Vector3 Nearest8Direction(Vector3 dir)
    {
        dir.y = 0f;
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
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

    public Vector3 GetFacingDirection() => lastFlashlightDir.sqrMagnitude < 0.0001f ? transform.forward : lastFlashlightDir.normalized;

    void UpdateUIFill()
    {
        if (batteryFillImage != null)
            batteryFillImage.fillAmount = smoothFill ? Mathf.Lerp(batteryFillImage.fillAmount, targetFill, Time.deltaTime * fillLerpSpeed) : targetFill;
        else if (batteryFillRect != null)
        {
            Vector3 s = batteryFillRect.localScale;
            s.x = targetFill;
            batteryFillRect.localScale = s;
        }
    }

    void UpdateIntegratedUI(float batteryValue, bool torchOn, bool recharging, bool torchUsing)
    {
        float b = Mathf.Clamp01(batteryValue);
        TorchThreshold newActive = null;
        if (thresholds != null)
        {
            foreach (var t in thresholds)
            {
                if (t == null || !t.enabled) continue;
                if (b <= t.thresholdValue)
                {
                    if (newActive == null || t.thresholdValue < newActive.thresholdValue)
                        newActive = t;
                }
            }
        }

        if (newActive != activeThreshold)
        {
            activeThreshold = newActive;
            StopWarningPulse();
            visibleTimer = Mathf.Max(visibleTimer, uiVisibleDuration * 0.5f);
        }

        Color targetColor = (activeThreshold != null) ? activeThreshold.barColor : Color.white;
        if (batteryFillImage != null) batteryFillImage.color = Color.Lerp(batteryFillImage.color, targetColor, Time.deltaTime * 6f);

        bool thresholdVisible = activeThreshold != null && activeThreshold.showBarWhenReached;
        bool shouldShowBar = recharging || thresholdVisible || visibleTimer > 0f;

        if (recharging) visibleTimer = Mathf.Max(visibleTimer, uiVisibleDuration);

        if (uiCanvasGroup != null)
        {
            float targetAlpha = shouldShowBar ? 1f : 0f;
            uiCanvasGroup.alpha = Mathf.MoveTowards(uiCanvasGroup.alpha, targetAlpha, uiFadeSpeed * Time.deltaTime);
        }

        bool shouldShowWarning = torchUsing && activeThreshold != null && activeThreshold.showWarningSign && !recharging;
        if (shouldShowWarning) StartWarningPulse(activeThreshold.warningPulseSpeed);
        else StopWarningPulse();
    }

    void StartWarningPulse(float speed)
    {
        if (warningIcon == null) return;
        if (warningPulseRoutine != null) StopCoroutine(warningPulseRoutine);
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

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        Vector3 origin = player.transform.position;

        Vector3 forward = player.FacingDirection;
        if (forward.sqrMagnitude < 0.0001f) forward = player.transform.forward;

        // Draw free aim radius lines
        Quaternion leftRot = Quaternion.AngleAxis(-freeAimRadius, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(freeAimRadius, Vector3.up);
        Vector3 leftDir = (leftRot * forward).normalized;
        Vector3 rightDir = (rightRot * forward).normalized;
        float len = 2.0f;
        Gizmos.DrawRay(origin, leftDir * len);
        Gizmos.DrawRay(origin, rightDir * len);

        // Draw small arc for free aim radius
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

        // Draw snap tolerance lines
        Gizmos.color = Color.cyan;
        Quaternion leftSnap = Quaternion.AngleAxis(-(freeAimRadius + snapTolerance), Vector3.up);
        Quaternion rightSnap = Quaternion.AngleAxis(freeAimRadius + snapTolerance, Vector3.up);
        Gizmos.DrawRay(origin, (leftSnap * forward).normalized * len * 1.05f);
        Gizmos.DrawRay(origin, (rightSnap * forward).normalized * len * 1.05f);

        // Optional: draw tilt limits relative to forward (if needed)
        Gizmos.color = Color.magenta;
        Quaternion tiltUp = Quaternion.AngleAxis(tiltAngle, Vector3.right);
        Quaternion tiltDown = Quaternion.AngleAxis(-tiltAngle, Vector3.right);
        // For visualization purposes only
        Gizmos.DrawRay(origin, tiltUp * forward * len);
        Gizmos.DrawRay(origin, tiltDown * forward * len);
    }
}
