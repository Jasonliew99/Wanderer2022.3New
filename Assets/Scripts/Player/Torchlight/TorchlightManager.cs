using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

//alright another stupid ass script.
//this one controls the torhclight and its the main script for it
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
    public TorchLightDetector detector;

    [Header("Secondary Light")]
    public Light secondaryLight;
    private float secondaryBaseIntensity;
    private float rangeRatio;    // Captured at start
    private float angleOffset;   // Captured at start

    [Header("Battery Settings")]
    [Range(0f, 1f)] public float battery = 1f;
    public float drainSpeed = 0.05f;
    public float rechargeSpeed = 0.1f;
    public float rechargeDelay = 1.2f;
    [Range(0f, 1f)] public float sizeDrainThreshold = 0.5f;

    [Header("Flicker Settings")]
    public bool enableFlicker = true;
    [Range(0f, 1f)] public float flickerEventChance = 0.05f;
    public int flickerCount = 3;
    public float flickerInterval = 0.05f;
    [Range(0f, 2f)] public float minIntensity = 0.7f;
    [Range(0f, 2f)] public float maxIntensity = 1.2f;

    [Header("Battery Scaling (Horror)")]
    public float minRange = 3f;
    public float minSpotAngle = 20f;

    [Header("Brightness Thresholds")]
    [Range(0f, 1f)] float warningThreshold = 0.4f;
    [Range(0f, 1f)] float criticalThreshold = 0.15f;
    public float maxBrightnessAtWarning = 0.7f;
    public float maxBrightnessAtCritical = 0.5f;

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
    private float baseRange;
    private float baseSpotAngle;
    private bool isTorchOn = true;
    private float targetFill = 1f;
    private float visibleTimer = 0f;
    private TorchThreshold activeThreshold;
    private Coroutine warningPulseRoutine;
    private float snapTimer = 0f;
    private float rechargeTimer = 0f;

    private Vector2 gamepadLookInput;

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
            baseRange = lightSource.range;
            baseSpotAngle = lightSource.spotAngle;

            // Capture Secondary Ratios if light exists
            if (secondaryLight != null)
            {
                secondaryBaseIntensity = secondaryLight.intensity;
                rangeRatio = secondaryLight.range / baseRange;
                angleOffset = secondaryLight.spotAngle - baseSpotAngle;
            }

            battery = 1f;
            isTorchOn = true;
            lightSource.enabled = true;
            SetupBatteryImage();
            StartCoroutine(FlickerRoutine());
        }

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

        if (lightSource != null) lightSource.enabled = torchUsing;
        if (secondaryLight != null) secondaryLight.enabled = torchUsing;

        HandleMouseFree();
        CalculateSpriteZone();

        if (torchUsing) HandleBrightness();

        UpdateUI(torchUsing);

        if (batteryFillImage != null)
        {
            float fillVal = smoothFill ? Mathf.Lerp(batteryFillImage.fillAmount, targetFill, Time.deltaTime * fillLerpSpeed) : targetFill;
            batteryFillImage.fillAmount = fillVal;
        }

        HandleUIFade();
    }

    public void OnLookEvent(InputValue value) => gamepadLookInput = value.Get<Vector2>();

    public void OnToggleTorchEvent(InputValue value)
    {
        if (value.isPressed) ToggleTorchState();
    }

    void HandleToggle()
    {
        if (Input.GetKeyDown(toggleKey)) ToggleTorchState();
    }

    void ToggleTorchState()
    {
        if (battery > 0f)
        {
            isTorchOn = !isTorchOn;
            if (torchAudioSource != null)
            {
                AudioClip clipToPlay = isTorchOn ? torchOnSound : torchOffSound;
                torchAudioSource.PlayOneShot(clipToPlay);
            }
            ShowTemporaryUI();
        }
    }

    void CalculateSpriteZone()
    {
        Vector3 f = lastFlashlightDir;
        f.y = 0f;
        if (f.sqrMagnitude < 0.001f) return;
        f = Quaternion.AngleAxis(44.6f, Vector3.up) * f;
        float angle = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

        if (angle > 0 && angle <= 90) CurrentSpriteZone = 0;
        else if (angle > -90 && angle <= 0) CurrentSpriteZone = 1;
        else if (angle > 90 && angle <= 180) CurrentSpriteZone = 2;
        else CurrentSpriteZone = 3;
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

    void HandleTorchBattery()
    {
        if (isTorchOn)
        {
            rechargeTimer = 0f;
            if (battery > 0f) battery -= drainSpeed * Time.deltaTime;
            else
            {
                battery = 0f;
                isTorchOn = false;
                if (torchAudioSource != null && torchOffSound != null)
                    torchAudioSource.PlayOneShot(torchOffSound);
            }
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

        // 1. Intensity Scaling
        float intensity = baseIntensity;
        if (battery <= 0.15f) intensity *= maxBrightnessAtCritical;
        else if (battery <= 0.4f) intensity *= maxBrightnessAtWarning;
        lightSource.intensity = intensity;

        // 2. Scale Factor (The Threshold)
        float sizeFactor = 1f;
        if (battery < sizeDrainThreshold)
        {
            sizeFactor = battery / sizeDrainThreshold;
        }

        // 3. Apply to Primary
        lightSource.range = Mathf.Lerp(minRange, baseRange, sizeFactor);
        lightSource.spotAngle = Mathf.Lerp(minSpotAngle, baseSpotAngle, sizeFactor);

        // 4. Sync Detector
        if (detector != null)
        {
            detector.coneRange = lightSource.range;
            detector.coneAngle = lightSource.spotAngle;
        }

        // 5. Proportional Secondary Sync
        if (secondaryLight != null && baseIntensity > 0f)
        {
            // Dimming
            float intensityRatio = intensity / baseIntensity;
            secondaryLight.intensity = secondaryBaseIntensity * intensityRatio;

            // Range (stays proportional to primary)
            secondaryLight.range = lightSource.range * rangeRatio;

            // Angle (maintains your manual offset)
            secondaryLight.spotAngle = lightSource.spotAngle + angleOffset;
        }
    }

    void HandleMouseFree()
    {
        Vector3 aimDir = Vector3.zero;
        if (gamepadLookInput.sqrMagnitude > 0.1f)
        {
            aimDir = new Vector3(gamepadLookInput.x, 0f, gamepadLookInput.y).normalized;
        }
        else
        {
            Ray ray = (mainCamera != null ? mainCamera : Camera.main).ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);
            if (groundPlane.Raycast(ray, out float hitDist))
            {
                aimDir = (ray.GetPoint(hitDist) - transform.position).normalized;
            }
        }

        aimDir.y = 0f;
        if (aimDir.sqrMagnitude < 0.0001f) return;

        Vector3 facing = player.FacingDirection;
        if (facing.sqrMagnitude < 0.0001f) facing = transform.forward;

        float angleDiff = Vector3.SignedAngle(facing, aimDir, Vector3.up);
        float absAngle = Mathf.Abs(angleDiff);

        if (absAngle <= freeAimRadius)
        {
            lastFlashlightDir = aimDir;
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
                else lastFlashlightDir = Quaternion.AngleAxis(freeAimRadius * Mathf.Sign(angleDiff), Vector3.up) * facing;
            }
            else
            {
                snapTimer = 0f;
                lastFlashlightDir = Quaternion.AngleAxis(freeAimRadius * Mathf.Sign(angleDiff), Vector3.up) * facing;
            }
        }
    }

    Vector3 Nearest8Direction(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        return new Vector3(Mathf.Sin(Mathf.Round(angle / 45f) * 45f * Mathf.Deg2Rad), 0f, Mathf.Cos(Mathf.Round(angle / 45f) * 45f * Mathf.Deg2Rad)).normalized;
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

        bool shouldShowBar = (activeThreshold != null && activeThreshold.showBarWhenReached) || !isTorchOn || visibleTimer > 0f;

        if (!isTorchOn) visibleTimer = Mathf.Max(visibleTimer, uiVisibleDuration);

        if (uiCanvasGroup != null)
            uiCanvasGroup.alpha = Mathf.MoveTowards(uiCanvasGroup.alpha, shouldShowBar ? 1f : 0f, uiFadeSpeed * Time.deltaTime);

        if (torchUsing && activeThreshold != null && activeThreshold.showWarningSign) StartWarningPulse(activeThreshold.warningPulseSpeed);
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
            float mapped = Mathf.Lerp(0.25f, 1f, (Mathf.Sin(Time.time * speed) + 1f) * 0.5f);
            warningIcon.color = new Color(baseColor.r, baseColor.g, baseColor.b, mapped);
            yield return null;
        }
    }

    public void ShowTemporaryUI() { visibleTimer = uiVisibleDuration; if (uiCanvasGroup != null) uiCanvasGroup.alpha = 1f; }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 5.0f));
            if (!enableFlicker || !isTorchOn || lightSource == null || battery <= 0f) continue;

            float flickerChance = flickerEventChance + (1f - battery) * 0.3f;
            if (Random.value < flickerChance)
            {
                int count = Random.Range(2, flickerCount + 2);
                for (int i = 0; i < count; i++)
                {
                    lightSource.enabled = !lightSource.enabled;
                    yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
                }
                lightSource.enabled = true;

                float originalInten = lightSource.intensity;
                float targetInten = originalInten * 0.2f;
                float t = 0;
                while (t < 0.25f)
                {
                    t += Time.deltaTime;
                    lightSource.intensity = Mathf.Lerp(originalInten, targetInten, t / 0.25f);
                    yield return null;
                }
                yield return new WaitForSeconds(0.1f);
                HandleBrightness();
            }
        }
    }

    public bool IsBeamHittingEnemy(Transform enemyTransform)
    {
        if (!isTorchOn || battery <= 0f) return false;
        Ray ray = new Ray(flashlight.position, lastFlashlightDir.normalized);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, beamDistance, beamBlockMask | enemyMask))
        {
            if (hit.transform == enemyTransform) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (player == null || flashlight == null) return;

        // 1. Draw the Zone Lines
        Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
        Vector3 zoneCenter = transform.position + Vector3.up * 0.1f;
        Quaternion isoInv = Quaternion.AngleAxis(-44.6f, Vector3.up);
        Vector3 line1 = isoInv * new Vector3(1, 0, 1).normalized * 3f;
        Vector3 line2 = isoInv * new Vector3(-1, 0, 1).normalized * 3f;
        Gizmos.DrawLine(zoneCenter - line1, zoneCenter + line1);
        Gizmos.DrawLine(zoneCenter - line2, zoneCenter + line2);

        if (lastFlashlightDir != Vector3.zero)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            Matrix4x4 oldMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(flashlight.position, Quaternion.LookRotation(lastFlashlightDir), Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, baseSpotAngle, baseRange, 0.1f, 1f);

            Gizmos.matrix = oldMatrix;

            if (showBeamGizmo)
            {
                Gizmos.color = (isTorchOn && battery > 0) ? Color.yellow : Color.red;
                Gizmos.DrawRay(flashlight.position, lastFlashlightDir.normalized * (lightSource != null ? lightSource.range : beamDistance));
            }
        }
    }
}
