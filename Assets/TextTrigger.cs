using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextTrigger : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup textCanvasGroup;
    public Text textUI;

    [TextArea]
    public string message;

    [Header("Timing")]
    public float fadeDuration = 0.5f;
    public float displayDuration = 2f;
    public float cooldown = 3f;

    [Header("Options")]
    public bool triggerOnce = false;

    private bool isOnCooldown = false;
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (triggerOnce && hasTriggered) return;
        if (isOnCooldown) return;

        StartCoroutine(ShowTextRoutine());
    }

    IEnumerator ShowTextRoutine()
    {
        hasTriggered = true;
        isOnCooldown = true;

        textUI.text = message;

        // Fade In
        yield return StartCoroutine(Fade(0, 1));

        // Stay
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        yield return StartCoroutine(Fade(1, 0));

        // Cooldown
        yield return new WaitForSeconds(cooldown);

        isOnCooldown = false;
    }

    IEnumerator Fade(float start, float end)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            float alpha = Mathf.Lerp(start, end, time / fadeDuration);
            textCanvasGroup.alpha = alpha;

            time += Time.deltaTime;
            yield return null;
        }

        textCanvasGroup.alpha = end;
    }
}
