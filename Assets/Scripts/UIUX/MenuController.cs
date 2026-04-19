using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "Level1";

    [Header("Video & UI Animation")]
    public VideoPlayer menuVideo;
    public CanvasGroup playButtonCanvasGroup;

    [Tooltip("How many seconds into the video should the button start appearing?")]
    public float delayBeforeFade = 1.5f;
    public float fadeDuration = 1.0f;

    void Start()
    {
        // 1. Initial State: Button is invisible and non-interactable
        if (playButtonCanvasGroup != null)
        {
            playButtonCanvasGroup.alpha = 0;
            playButtonCanvasGroup.interactable = false;
            playButtonCanvasGroup.blocksRaycasts = false;

            // Start the fade timer as soon as the scene loads
            StartCoroutine(FadeInUI());
        }

        // 2. Setup the "Freeze on Last Frame" logic
        if (menuVideo != null)
        {
            menuVideo.loopPointReached += OnVideoFinished;
        }
    }

    // This handles freezing the video background
    void OnVideoFinished(VideoPlayer vp)
    {
        vp.playbackSpeed = 0;
        // Note: We removed the FadeInUI call from here so it doesn't trigger twice.
    }

    System.Collections.IEnumerator FadeInUI()
    {
        // Wait for your "certain point" (delayBeforeFade)
        yield return new WaitForSeconds(delayBeforeFade);

        float counter = 0;
        while (counter < fadeDuration)
        {
            counter += Time.deltaTime;
            playButtonCanvasGroup.alpha = Mathf.Lerp(0, 1, counter / fadeDuration);
            yield return null;
        }

        // Make button fully solid and clickable
        playButtonCanvasGroup.alpha = 1;
        playButtonCanvasGroup.interactable = true;
        playButtonCanvasGroup.blocksRaycasts = true;
    }

    public void PlayGame()
    {
        Debug.Log("CLICK DETECTED!");

        // 1. Instantly make the button unclickable so they can't spam it
        if (playButtonCanvasGroup != null)
        {
            playButtonCanvasGroup.interactable = false;
            playButtonCanvasGroup.blocksRaycasts = false;
            // This makes the button disappear immediately as the fade starts
            playButtonCanvasGroup.alpha = 0;
        }

        // 2. Find the "Immortal" SceneChanger we put in the Main Menu
        SceneChanger fader = FindObjectOfType<SceneChanger>();

        if (fader != null)
        {
            // Tell the fader to take over and load the cutscene
            fader.GoToCutscene(gameSceneName);
        }
        else
        {
            // Safety Fallback: If you forgot to put the SceneChanger in the scene
            Debug.LogWarning("SceneChanger not found! Loading scene instantly instead.");
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
