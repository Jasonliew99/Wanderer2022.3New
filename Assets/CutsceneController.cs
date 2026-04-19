using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneController : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    [Header("Tutorial UI Settings")]
    public CanvasGroup tutorialCanvasGroup; // Add a Canvas Group to your Tutorial Panel
    public GameObject readyButton;          // Drag your button here
    public float fadeSpeed = 1.5f;          // How fast the panel fades in
    public float waitTime = 3.0f;           // How long before the button appears

    void Start()
    {
        // 1. Ensure everything starts hidden
        if (tutorialCanvasGroup != null) tutorialCanvasGroup.alpha = 0;
        if (readyButton != null) readyButton.SetActive(false);

        // 2. Listen for the video ending
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnCutsceneEnd;
    }

    public void OnCutsceneEnd(VideoPlayer vp)
    {
        videoPlayer.gameObject.SetActive(false);
        // Start the sequence: Fade -> Wait -> Show Button
        StartCoroutine(ShowTutorialSequence());
    }

    IEnumerator ShowTutorialSequence()
    {
        // Step 1: Fade in the Tutorial Panel
        while (tutorialCanvasGroup.alpha < 1)
        {
            tutorialCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // Step 2: Wait for the specified timer (e.g., 3 seconds)
        yield return new WaitForSeconds(waitTime);

        // Step 3: Make the READY button appear
        if (readyButton != null)
        {
            readyButton.SetActive(true);
        }
    }

    public void StartGame()
    {
        Debug.Log("Ready Button Clicked!");
        SceneChanger fader = FindObjectOfType<SceneChanger>();
        if (fader != null)
        {
            fader.GoToCutscene("Shipyard");
        }
        else
        {
            SceneManager.LoadScene("Shipyard");
        }
    }
}
