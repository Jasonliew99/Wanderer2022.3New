using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class PostGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private CanvasGroup buttonCanvasGroup;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Settings")]
    [SerializeField] private float timeBeforeButtonAppears = 5f;
    [SerializeField] private float buttonFadeSpeed = 2f;

    private bool isTransitioning = false;

    void Start()
    {
        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.alpha = 0;
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;
        }

        StartCoroutine(ShowButtonAfterDelay());

        if (videoPlayer != null)
        {
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private IEnumerator ShowButtonAfterDelay()
    {
        yield return new WaitForSeconds(timeBeforeButtonAppears);
        while (buttonCanvasGroup != null && buttonCanvasGroup.alpha < 1)
        {
            buttonCanvasGroup.alpha += Time.deltaTime * buttonFadeSpeed;
            yield return null;
        }

        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.interactable = true;
            buttonCanvasGroup.blocksRaycasts = true;
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("Video Finished! Transitioning now...");
        ReturnToMenu();
    }

    public void OnButtonClick()
    {
        Debug.Log("Button Clicked! Transitioning now...");
        ReturnToMenu();
    }

    private void ReturnToMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        SceneChanger fader = FindObjectOfType<SceneChanger>();

        if (fader != null)
        {
            Debug.Log("Using SceneChanger fader.");
            fader.GoToCutscene(mainMenuSceneName);
        }
        else
        {
            Debug.Log("No fader found, loading scene directly.");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }
}
