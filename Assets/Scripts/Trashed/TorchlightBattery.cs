using System.Collections;
//using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
//using static System.Net.Mime.MediaTypeNames;

public class TorchlightBattery : MonoBehaviour
{
    [Header("References")]
    public TorchlightManager torchlight;
    public Image batteryBar;
    public Image warningIcon;

    [Header("Settings")]
    public float warningThreshold = 0.4f;
    public float criticalThreshold = 0.15f;
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;
    public KeyCode rechargeKey = KeyCode.R;

    [Header("Battery Settings")]
    [Range(0f, 1f)] public float battery = 1f;
    public float drainSpeed = 0.05f;
    public float rechargeSpeed = 0.1f;
    public float smoothSpeed = 5f;

    [Header("UI Fade")]
    public float fadeDuration = 0.5f;
    public float stayDuration = 2f;

    private float displayedBattery;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (batteryBar == null)
            Debug.LogError("Assign batteryBar in Inspector!");

        canvasGroup = batteryBar.GetComponentInParent<CanvasGroup>();
        if (canvasGroup == null)
            Debug.LogError("Battery UI parent must have CanvasGroup.");

        if (warningIcon != null)
            warningIcon.gameObject.SetActive(false);

        displayedBattery = battery;
    }

    void Update()
    {
        if (torchlight == null) return;

        bool isTorchOn = torchlight.IsTorchOn;
        bool isRecharging = Input.GetKey(rechargeKey);

        // Drain battery if torch is on
        if (isTorchOn && !isRecharging)
            battery -= drainSpeed * Time.deltaTime;

        // Recharge battery
        if (isRecharging)
            battery += rechargeSpeed * Time.deltaTime;

        battery = Mathf.Clamp01(battery);

        // Smooth fill
        displayedBattery = Mathf.Lerp(displayedBattery, battery, Time.deltaTime * smoothSpeed);

        UpdateUI(displayedBattery, isRecharging);
    }

    void UpdateUI(float normalized, bool recharging)
    {
        if (batteryBar != null)
        {
            batteryBar.fillAmount = normalized;

            if (normalized <= criticalThreshold)
                batteryBar.color = criticalColor;
            else if (normalized <= warningThreshold)
                batteryBar.color = warningColor;
            else
                batteryBar.color = normalColor;
        }

        if (warningIcon != null)
        {
            bool showWarning = normalized <= criticalThreshold;
            warningIcon.gameObject.SetActive(showWarning);
            if (showWarning)
            {
                float alpha = (Mathf.Sin(Time.time * 5f) + 1f) / 2f;
                warningIcon.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        if (recharging || normalized <= warningThreshold)
            FadeIn();
        else
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutAfterDelay(stayDuration));
        }
    }

    void FadeIn()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
