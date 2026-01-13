using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SprintBarUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform fillBar;
    public CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    public float fadeOutDelay = 1.5f;
    public float fadeDuration = 0.5f;

    private float fadeTimer = 0f;
    private bool isFading = false;

    public void UpdateSprintBar(float percent, bool isSprinting)
    {
        if (fillBar == null) return;

        percent = Mathf.Clamp01(percent);
        fillBar.localScale = new Vector3(percent, 1f, 1f);

        if (canvasGroup == null) return;

        bool staminaNotFull = percent < 0.999f;

        if (isSprinting || staminaNotFull)
        {
            fadeTimer = 0f;

            if (isFading)
            {
                StopAllCoroutines();
                StartCoroutine(Fade(canvasGroup.alpha, 1f));
                isFading = false;
            }
        }
        else
        {
            fadeTimer += Time.deltaTime;

            if (fadeTimer >= fadeOutDelay && !isFading)
            {
                StartCoroutine(Fade(canvasGroup.alpha, 0f));
                isFading = true;
            }
        }
    }

    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }
}
