using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextTrigger : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup textCanvasGroup;
    public TMP_Text textUI;

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
        Debug.Log("Triggered by: " + other.name);

        if (!other.CompareTag("Player")) return;

        Debug.Log("Player entered trigger!");

        if (triggerOnce && hasTriggered) return;
        if (isOnCooldown) return;

        StartCoroutine(ShowTextRoutine());
    }

    IEnumerator ShowTextRoutine()
    {
        hasTriggered = true;
        isOnCooldown = true;

        textUI.text = message;

        yield return StartCoroutine(Fade(0, 1));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(Fade(1, 0));

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
