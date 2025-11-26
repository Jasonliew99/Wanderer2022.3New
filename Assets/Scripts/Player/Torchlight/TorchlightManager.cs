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
    public enum TorchMode { MouseFree } // Clean: only the used mode

    [Header("Torch Settings")]
    public TorchMode torchMode = TorchMode.MouseFree;
    public Transform flashlight;
    public float offsetDistance = 0.5f;
    public float heightOffset = 0.2f;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Rotation Settings")]
    public float rotationSpeed = 10f;

    [Header("Free-Aim Settings")]
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

    [Header("Brightness Thresholds")]
    [Range(0f, 1f)] public float warningThreshold = 0.4f;
    [Range(0f, 1f)] public float criticalThreshold = 0.15f;
    [Range(0f, 1f)] public float maxBrightnessAtWarning = 0.7f;
    [Range(0f, 1f)] public float maxBrightnessAtCritical = 0.5f;

    [Header("UI References")]
    public Image batteryFillImage;
    public RectTransform batteryFillRect;
    public CanvasGroup uiCanvasGroup;
    public Image warningIcon;

    [Header("UI Behavior")]
    public bool smoothFill = true;
    public float fillLerpSpeed = 8f;
    public float uiVisibleDuration = 1.5f;
    public float uiFadeSpeed = 5f;

    [Header("Battery Thresholds")]
    public List<TorchThreshold> thresholds = new List<TorchThreshold>();

    // Internal
    private Vector3 lastFlashlightDir;
    private float baseIntensity;
    private bool isTorchOn = true;
    private float targetFill = 1f;
    private float visibleTimer = 0f;
    private TorchThreshold activeThreshold;
    private Coroutine warningPulseRoutine;
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
            lightSource.enabled = true;
            SetupBatteryImage();
            StartCoroutine(FlickerRoutine());
        }

        if (uiCanvasGroup != null)
            uiCanvasGroup.alpha = 0f;

        if (warningIcon != null)
            warningIcon.gameObject.SetActive(false);

        // initial direction
        lastFlashlightDir = player.FacingDirection;
        if (lastFlashlightDir == Vector3.zero)
            lastFlashlightDir = transform.forward;
    }

    void Update()
    {
        HandleToggle();
        HandleTorchBattery();

        bool torchUsing = isTorchOn && !Input.GetKey(rechargeKey) && battery > 0f;

        if (lightSource != null)
            lightSource.enabled = isTorchOn && battery > 0f;

        if (isTorchOn && battery > 0f)
        {
            HandleMouseFree();
            HandleBrightness();
        }

        UpdateUI(torchUsing);

        if (batteryFillImage != null && smoothFill)
            batteryFillImage.fillAmount = Mathf.Lerp(batteryFillImage.fillAmount, targetFill, Time.deltaTime * fillLerpSpeed);
        else if (batteryFillImage != null)
            batteryFillImage.fillAmount = targetFill;

        HandleUIFade();
    }

    void LateUpdate()
    {
        if (flashlight == null) return;

        Vector3 targetPos = transform.position + lastFlashlightDir.normalized * offsetDistance + Vector3.up * heightOffset;
        flashlight.position = targetPos;

        if (lastFlashlightDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lastFlashlightDir);
            flashlight.rotation = Quaternion.Slerp(flashlight.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    // ------------------------
    // BATTERY + TOGGLE
    // ------------------------
    void HandleToggle()
    {
        if (Input.GetKeyDown(toggleKey) && battery > 0f)
        {
            isTorchOn = !isTorchOn;
            ShowTemporaryUI();
        }
    }

    void HandleTorchBattery()
    {
        bool charging = Input.GetKey(rechargeKey);

        if (isTorchOn && !charging)
            battery -= drainSpeed * Time.deltaTime;

        if (charging)
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

    void HandleBrightness()
    {
        if (lightSource == null) return;

        float intensity = baseIntensity;

        if (battery <= criticalThreshold)
            intensity *= maxBrightnessAtCritical;
        else if (battery <= warningThreshold)
            intensity *= maxBrightnessAtWarning;

        lightSource.intensity = intensity;
    }

    // ------------------------
    // MOUSE FREE MODE ONLY
    // ------------------------
    void HandleMouseFree()
    {
        Ray ray = (mainCamera != null ? mainCamera : Camera.main).ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (!groundPlane.Raycast(ray, out float hitDist)) return;

        Vector3 hitPoint = ray.GetPoint(hitDist);
        Vector3 aimDir = (hitPoint - transform.position).normalized;
        aimDir.y = 0f;
        if (aimDir.sqrMagnitude < 0.0001f) return;

        Vector3 facing = player.FacingDirection;
        if (facing.sqrMagnitude < 0.0001f)
            facing = transform.forward;

        float angleDiff = Vector3.SignedAngle(facing, aimDir, Vector3.up);
        float absAngle = Mathf.Abs(angleDiff);

        if (absAngle <= freeAimRadius)
        {
            lastFlashlightDir = Quaternion.AngleAxis(angleDiff, Vector3.up) * facing;
            snapTimer = 0f;
            return;
        }

        float snapZone = freeAimRadius + snapTolerance;

        if (absAngle > snapZone)
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
                lastFlashlightDir = Quaternion.AngleAxis(freeAimRadius * sign, Vector3.up) * facing;
            }
        }
        else
        {
            snapTimer = 0f;
            float sign = Mathf.Sign(angleDiff);
            lastFlashlightDir = Quaternion.AngleAxis(freeAimRadius * sign, Vector3.up) * facing;
        }
    }

    Vector3 Nearest8Direction(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angle / 45f) * 45f;
        float rad = snapped * Mathf.Deg2Rad;

        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)).normalized;
    }

    // ------------------------
    // UI SYSTEM
    // ------------------------
    void SetupBatteryImage()
    {
        if (batteryFillImage == null) return;

        batteryFillImage.type = Image.Type.Filled;
        batteryFillImage.fillMethod = Image.FillMethod.Horizontal;
        batteryFillImage.fillAmount = battery;
        targetFill = battery;
    }

    void UpdateUI(bool torchUsing)
    {
        float b = Mathf.Clamp01(battery);

        TorchThreshold newActive = null;
        foreach (var t in thresholds)
        {
            if (t.enabled && b <= t.thresholdValue)
            {
                if (newActive == null || t.thresholdValue < newActive.thresholdValue)
                    newActive = t;
            }
        }

        if (newActive != activeThreshold)
        {
            activeThreshold = newActive;
            StopWarningPulse();
            visibleTimer = Mathf.Max(visibleTimer, uiVisibleDuration * 0.5f);
        }

        if (batteryFillImage != null)
        {
            Color targetColor = (activeThreshold != null) ? activeThreshold.barColor : Color.white;
            batteryFillImage.color = Color.Lerp(batteryFillImage.color, targetColor, Time.deltaTime * 6f);
        }

        bool thresholdVisible = activeThreshold != null && activeThreshold.showBarWhenReached;
        bool shouldShowBar = thresholdVisible || Input.GetKey(rechargeKey) || visibleTimer > 0f;

        if (Input.GetKey(rechargeKey))
            visibleTimer = Mathf.Max(visibleTimer, uiVisibleDuration);

        if (uiCanvasGroup != null)
        {
            float target = shouldShowBar ? 1f : 0f;
            uiCanvasGroup.alpha = Mathf.MoveTowards(uiCanvasGroup.alpha, target, uiFadeSpeed * Time.deltaTime);
        }

        bool shouldShowWarning = torchUsing && activeThreshold != null &&
                                 activeThreshold.showWarningSign &&
                                 !Input.GetKey(rechargeKey);

        if (shouldShowWarning)
            StartWarningPulse(activeThreshold.warningPulseSpeed);
        else
            StopWarningPulse();
    }

    void HandleUIFade()
    {
        if (uiCanvasGroup == null) return;

        bool thresholdVisible = activeThreshold != null && activeThreshold.showBarWhenReached;

        if (visibleTimer > 0f && !Input.GetKey(rechargeKey) && !thresholdVisible)
            visibleTimer -= Time.deltaTime;
    }

    void StartWarningPulse(float speed)
    {
        if (warningIcon == null) return;

        if (warningPulseRoutine != null)
            StopCoroutine(warningPulseRoutine);

        warningIcon.gameObject.SetActive(true);
        warningPulseRoutine = StartCoroutine(WarningPulseCoroutine(speed));
    }

    void StopWarningPulse()
    {
        if (warningIcon == null) return;

        if (warningPulseRoutine != null)
            StopCoroutine(warningPulseRoutine);

        warningPulseRoutine = null;
        warningIcon.gameObject.SetActive(false);
    }

    IEnumerator WarningPulseCoroutine(float speed)
    {
        Color baseColor = warningIcon.color;

        while (true)
        {
            float alpha = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            float mapped = Mathf.Lerp(0.25f, 1f, alpha);
            warningIcon.color = new Color(baseColor.r, baseColor.g, baseColor.b, mapped);
            yield return null;
        }
    }

    // ------------------------
    // BATTERY SETTER (for enemy stun)
    // ------------------------
    public void SetBatteryPercent(float value)
    {
        battery = Mathf.Clamp01(value);
        targetFill = battery;

        if (battery <= 0f)
        {
            isTorchOn = false;
            if (lightSource != null)
                lightSource.enabled = false;
        }

        ShowTemporaryUI();
    }

    // ------------------------
    // UI Helper
    // ------------------------
    public void ShowTemporaryUI()
    {
        visibleTimer = uiVisibleDuration;
        if (uiCanvasGroup != null)
            uiCanvasGroup.alpha = 1f;
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
                HandleBrightness();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;

        Vector3 origin = transform.position;
        Vector3 facing = (player != null ? player.FacingDirection : transform.forward);

        if (facing.sqrMagnitude < 0.001f)
            facing = transform.forward;

        // left and right bounds of the free aim radius
        Quaternion leftRot = Quaternion.AngleAxis(-freeAimRadius, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(freeAimRadius, Vector3.up);

        Vector3 leftDir = (leftRot * facing).normalized;
        Vector3 rightDir = (rightRot * facing).normalized;

        float length = 2.0f;

        Gizmos.DrawRay(origin, leftDir * length);
        Gizmos.DrawRay(origin, rightDir * length);

        // Draw arc
        int steps = 24;
        Vector3 prev = leftDir * length;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            float angle = Mathf.Lerp(-freeAimRadius, freeAimRadius, t);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * facing;
            Vector3 cur = dir.normalized * length;

            Gizmos.DrawLine(origin + prev, origin + cur);
            prev = cur;
        }

        // Draw snap tolerance lines
        Gizmos.color = Color.cyan;

        Quaternion leftSnap = Quaternion.AngleAxis(-(freeAimRadius + snapTolerance), Vector3.up);
        Quaternion rightSnap = Quaternion.AngleAxis((freeAimRadius + snapTolerance), Vector3.up);

        Gizmos.DrawRay(origin, (leftSnap * facing).normalized * length * 1.1f);
        Gizmos.DrawRay(origin, (rightSnap * facing).normalized * length * 1.1f);
    }
}
