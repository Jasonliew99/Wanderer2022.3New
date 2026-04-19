using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FirstObjectiveText : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign a CanvasGroup component here to control transparency.")]
    public CanvasGroup textCanvasGroup;
    public TMP_Text textUI;

    [Header("Content")]
    [TextArea]
    public string objectiveMessage = "Find a way out...";

    [Header("Timing")]
    public float startDelay = 1.0f;
    public float fadeDuration = 0.5f;
    public float displayDuration = 3f;
    void Start()
    {
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0;
            textUI.text = objectiveMessage;

            StartCoroutine(IntroSequence());
        }
        else
        {
            Debug.LogError("Please assign a CanvasGroup to the script!");
        }
    }

    IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(startDelay);

        yield return StartCoroutine(Fade(0, 1));

        yield return new WaitForSeconds(displayDuration);

        yield return StartCoroutine(Fade(1, 0));
    }

    IEnumerator Fade(float start, float end)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(start, end, time / fadeDuration);
            textCanvasGroup.alpha = alpha;
            yield return null;
        }

        textCanvasGroup.alpha = end;
    }
}
