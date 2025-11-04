using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using static System.Net.Mime.MediaTypeNames;
using UnityEngine.UI;


public class TorchlightUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform fillBar;     // Battery fill
    public Image fillImage;           // For color change
    public Image warningIcon;         // Warning sign

    [Header("Settings")]
    public Color normalColor = Color.white;
    public Color criticalColor = Color.red;
    public float fadeSpeed = 2f;           // Speed of fading UI
    public float showDuration = 1.5f;      // How long UI stays after interaction
    public float warningPulseSpeed = 2f;
    public float criticalPulseSpeed = 5f;

    [Header("Thresholds")]
    public float warningThreshold = 0.4f;
    public float criticalThreshold = 0.15f;

    // State
    private CanvasGroup canvasGroup;
    private float showTimer = 0f;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f; // start hidden
    }

    public void UpdateUI(float battery, bool isTorchOn, bool isRecharging, bool torchUsing)
    {
        // --------------------------
        // Handle fill bar size & color
        // --------------------------
        if (fillBar != null)
        {
            // Scale X from right pivot
            battery = Mathf.Clamp01(battery);
            fillBar.localScale = new Vector3(battery, 1f, 1f);

            // Change color only when critical or recharging
            if (battery <= criticalThreshold)
                fillImage.color = criticalColor;
            else
                fillImage.color = normalColor;
        }

        // --------------------------
        // Handle warning icon
        // --------------------------
        if (warningIcon != null)
        {
            if (torchUsing && !isRecharging && battery <= warningThreshold)
            {
                warningIcon.gameObject.SetActive(true);

                float pulseSpeed = (battery <= criticalThreshold) ? criticalPulseSpeed : warningPulseSpeed;
                float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                warningIcon.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                warningIcon.gameObject.SetActive(false);
            }
        }

        // --------------------------
        // Handle UI fade in/out
        // --------------------------
        bool shouldShow = isRecharging || battery <= warningThreshold || showTimer > 0f;

        if (shouldShow)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            if (showTimer > 0f) showTimer -= Time.deltaTime;
        }
        else
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    // Call this when player toggles torch or interacts to show UI
    public void ShowTemporaryUI()
    {
        showTimer = showDuration;
    }
}
