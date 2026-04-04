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

    [Tooltip("If true, the battery bar will be shown when battery <= this value.")]
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
    public enum TorchMode { MouseFree }

    [Header("Torch Settings")]
    public TorchMode torchMode = TorchMode.MouseFree;
    public Transform flashlight;
    public float offsetDistance = 0.5f;
    public float heightOffset = 0.2f;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Torch SFX")]
    public AudioSource torchAudioSource;
    public AudioClip torchOnSound;
    public AudioClip torchOffSound;

    [Header("Torch Beam Raycast")]
    public float beamDistance = 15f;
    public LayerMask beamBlockMask;
    public LayerMask enemyMask;
    public bool showBeamGizmo = true;

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

    [Header("Secondary Light")]
    public Light secondaryLight;
    private float secondaryBaseIntensity;

    [Header("Battery Settings")]
    [Range(0f, 1f)] public float battery = 1f;
    public float drainSpeed = 0.05f;
    public float rechargeSpeed = 0.1f;
    public float rechargeDelay = 1.2f;

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

    private Vector3 lastFlashlightDir;
    private float baseIntensity;
    private bool isTorchOn = true;
    private float targetFill = 1f;
    private float visibleTimer = 0f;
    private TorchThreshold activeThreshold;
    private Coroutine warningPulseRoutine;
    private float snapTimer = 0f;
    private float rechargeTimer = 0f;

    // --- NEW PROPERTY FOR ANIMATION ---
    // 0: UpRight, 1: UpLeft, 2: DownRight, 3: DownLeft
    public int CurrentSpriteZone { get; private set; }
    public Vector3 LastFlashlightDir => lastFlashlightDir;

    public bool IsTorchOn => isTorchOn;
    public float BatteryPercent => battery;
    public bool IsRecharging => !isTorchOn;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (lightSource != null)
        {
            baseIntensity = lightSource.intensity;
            battery = 1f;
            isTorchOn = true;
            lightSource.enabled = true;
            SetupBatteryImage();
            StartCoroutine(FlickerRoutine());
        }
        if (secondaryLight != null) secondaryBaseIntensity = secondaryLight.intensity;
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
        if (warningIcon != null) warningIcon.gameObject.SetActive(false);

        lastFlashlightDir = player.FacingDirection;
        if (lastFlashlightDir == Vector3.zero) lastFlashlightDir = transform.forward;
    }

    void Update()
    {
        HandleToggle();
        HandleTorchBattery();

        bool torchUsing = isTorchOn && battery > 0f;

        if (lightSource != null)
            lightSource.enabled = isTorchOn && battery > 0f;

        if (secondaryLight != null)
            secondaryLight.enabled = lightSource.enabled;

        HandleMouseFree();

        // --- NEW: UPDATE SPRITE ZONE LOGIC ---
        CalculateSpriteZone();

        if (isTorchOn && battery > 0f)
        {
            HandleBrightness();
        }

        UpdateUI(torchUsing);

        if (batteryFillImage != null && smoothFill)
            batteryFillImage.fillAmount = Mathf.Lerp(batteryFillImage.fillAmount, targetFill, Time.deltaTime * fillLerpSpeed);
        else if (batteryFillImage != null)
            batteryFillImage.fillAmount = targetFill;

        HandleUIFade();
    }

    void CalculateSpriteZone()
    {
        Vector3 f = lastFlashlightDir;
        f.y = 0f;
        if (f.sqrMagnitude < 0.001f) return;

        // Apply isometric offset
        f = Quaternion.AngleAxis(44.6f, Vector3.up) * f;
        float angle = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

        // 4-Way Diagonal Logic (The New Zone X-Pattern)
        if (angle > 0 && angle <= 90) CurrentSpriteZone = 0; // Up-Right
        else if (angle > -90 && angle <= 0) CurrentSpriteZone = 1; // Up-Left
        else if (angle > 90 && angle <= 180) CurrentSpriteZone = 2; // Down-Right
        else CurrentSpriteZone = 3; // Down-Left
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

    void HandleToggle()
    {
        if (Input.GetKeyDown(toggleKey) && battery > 0f)
        {
            isTorchOn = !isTorchOn;

            // --- NEW: PLAY TOGGLE SOUND ---
            if (torchAudioSource != null)
            {
                AudioClip clipToPlay = isTorchOn ? torchOnSound : torchOffSound;
                torchAudioSource.PlayOneShot(clipToPlay);
            }

            ShowTemporaryUI();
        }
    }

    void HandleTorchBattery()
    {
        if (isTorchOn)
        {
            rechargeTimer = 0f;
            if (battery > 0f)
            {
                battery -= drainSpeed * Time.deltaTime;
            }
            else
            {
                // --- THIS ONLY RUNS ONCE WHEN BATTERY HITS 0 ---
                battery = 0f;
                isTorchOn = false;

                // Play the "Off" sound here!
                if (torchAudioSource != null && torchOffSound != null)
                {
                    torchAudioSource.PlayOneShot(torchOffSound);
                }
            }
            // If you put the sound here, it plays EVERY FRAME. Don't do that!
        }
        else
        {
            rechargeTimer += Time.deltaTime;
            if (rechargeTimer >= rechargeDelay && battery < 1f)
                battery += rechargeSpeed * Time.deltaTime;
        }

        battery = Mathf.Clamp01(battery);
        targetFill = battery;
    }

    void HandleBrightness()
    {
        if (lightSource == null) return;
        float intensity = baseIntensity;
        if (battery <= criticalThreshold) intensity *= maxBrightnessAtCritical;
        else if (battery <= warningThreshold) intensity *= maxBrightnessAtWarning;
        lightSource.intensity = intensity;

        if (secondaryLight != null && baseIntensity > 0f)
        {
            float factor = intensity / baseIntensity;
            secondaryLight.intensity = secondaryBaseIntensity * factor;
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
        if (aimDir.sqrMagnitude < 0.0001f) return;

        Vector3 facing = player.FacingDirection;
        if (facing.sqrMagnitude < 0.0001f) facing = transform.forward;

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
        bool shouldShowBar = thresholdVisible || !isTorchOn || visibleTimer > 0f;

        if (!isTorchOn) visibleTimer = Mathf.Max(visibleTimer, uiVisibleDuration);

        if (uiCanvasGroup != null)
        {
            float target = shouldShowBar ? 1f : 0f;
            uiCanvasGroup.alpha = Mathf.MoveTowards(uiCanvasGroup.alpha, target, uiFadeSpeed * Time.deltaTime);
        }

        bool shouldShowWarning = torchUsing && activeThreshold != null && activeThreshold.showWarningSign;

        if (shouldShowWarning) StartWarningPulse(activeThreshold.warningPulseSpeed);
        else StopWarningPulse();
    }

    void HandleUIFade()
    {
        if (uiCanvasGroup == null) return;
        bool thresholdVisible = activeThreshold != null && activeThreshold.showBarWhenReached;
        if (visibleTimer > 0f && !thresholdVisible) visibleTimer -= Time.deltaTime;
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
        if (warningPulseRoutine != null) StopCoroutine(warningPulseRoutine);
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

    public void SetBatteryPercent(float value)
    {
        battery = Mathf.Clamp01(value);
        targetFill = battery;
        if (battery <= 0f) isTorchOn = false;
        ShowTemporaryUI();
    }

    public void ShowTemporaryUI()
    {
        visibleTimer = uiVisibleDuration;
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 1f;
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
                    float flickerValue = Random.Range(minIntensity, maxIntensity);
                    lightSource.intensity = flickerValue;
                    if (secondaryLight != null && baseIntensity > 0f)
                    {
                        float factor = flickerValue / baseIntensity;
                        secondaryLight.intensity = secondaryBaseIntensity * factor;
                    }
                    yield return new WaitForSeconds(flickerInterval);
                }
                HandleBrightness();
            }
        }
    }

    public bool IsBeamHittingEnemy(Transform enemy)
    {
        if (!IsTorchOn || battery <= 0f || flashlight == null) return false;
        Vector3 origin = flashlight.position;
        Vector3 direction = lastFlashlightDir.normalized;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, beamDistance, beamBlockMask | enemyMask);
        if (hits.Length == 0) return false;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            int hitLayer = hit.collider.gameObject.layer;
            if (((1 << hitLayer) & beamBlockMask) != 0) return false;
            if (hit.transform.root == enemy.root) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // --- NEW GIZMO ZONE (X-PATTERN) ---
        Gizmos.color = Color.magenta;
        Vector3 zoneCenter = transform.position + Vector3.up * 0.1f;
        Quaternion isoInv = Quaternion.AngleAxis(-44.6f, Vector3.up);
        Vector3 line1 = isoInv * new Vector3(1, 0, 1).normalized * 3f;
        Vector3 line2 = isoInv * new Vector3(-1, 0, 1).normalized * 3f;
        Gizmos.DrawLine(zoneCenter - line1, zoneCenter + line1);
        Gizmos.DrawLine(zoneCenter - line2, zoneCenter + line2);
        // ----------------------------------

        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        Vector3 facing = player.FacingDirection;
        if (facing.sqrMagnitude < 0.001f) facing = transform.forward;

        Quaternion leftRot = Quaternion.AngleAxis(-freeAimRadius, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(freeAimRadius, Vector3.up);
        Vector3 leftDir = (leftRot * facing).normalized;
        Vector3 rightDir = (rightRot * facing).normalized;
        float length = 2.0f;
        Gizmos.DrawRay(origin, leftDir * length);
        Gizmos.DrawRay(origin, rightDir * length);

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

        Gizmos.color = Color.cyan;
        Quaternion leftSnap = Quaternion.AngleAxis(-(freeAimRadius + snapTolerance), Vector3.up);
        Quaternion rightSnap = Quaternion.AngleAxis((freeAimRadius + snapTolerance), Vector3.up);
        Gizmos.DrawRay(origin, (leftSnap * facing).normalized * length * 1.1f);
        Gizmos.DrawRay(origin, (rightSnap * facing).normalized * length * 1.1f);

        if (showBeamGizmo && flashlight != null)
        {
            Vector3 beamOrigin = flashlight.position;
            Vector3 beamDir = lastFlashlightDir.normalized;
            Gizmos.color = IsTorchOn ? Color.green : Color.red;
            Gizmos.DrawRay(beamOrigin, beamDir * beamDistance);
        }
    }
}
