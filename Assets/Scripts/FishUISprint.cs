using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static UnityEditor.ShaderData;

public class FishUISprint : MonoBehaviour
{
    [Header("UI Setup")]
    public RectTransform maskContainer;
    public CanvasGroup canvasGroup;
    public float maxWidth = 500f;
    public float minWidth = 100f;

    [Header("Fade Settings")]
    public float fadeOutDelay = 1.5f;
    public float fadeDuration = 0.5f;

    private float fadeTimer = 0f;
    private bool isFading = false;

    public void UpdateSprintBar(float currentSprint, float maxSprint, bool isSprinting)
    {
        float percent = Mathf.Clamp01(currentSprint / maxSprint);

        float targetWidth = Mathf.Lerp(minWidth, maxWidth, percent);
        maskContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

        if (canvasGroup == null) return;

        bool staminaNotFull = percent < 0.999f;

        if (isSprinting || staminaNotFull)
        {
            fadeTimer = 0f;

            // If hidden or fading out, fade back in
            if (!isFading && canvasGroup.alpha < 1f)
            {
                StopAllCoroutines();
                StartCoroutine(FadeUI(canvasGroup.alpha, 1f));
            }
        }
        else
        {
            fadeTimer += Time.deltaTime;

            if (fadeTimer >= fadeOutDelay && !isFading && canvasGroup.alpha > 0f)
            {
                StopAllCoroutines();
                StartCoroutine(FadeUI(canvasGroup.alpha, 0f));
            }
        }
    }

    IEnumerator FadeUI(float startAlpha, float endAlpha)
    {
        isFading = true;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
        isFading = false;
    }
}
