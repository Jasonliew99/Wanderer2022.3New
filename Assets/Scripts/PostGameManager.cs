using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class PostGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private CanvasGroup buttonCanvasGroup; // Use CanvasGroup instead of GameObject
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Settings")]
    [SerializeField] private float timeBeforeButtonAppears = 5f;
    [SerializeField] private float buttonFadeSpeed = 2f;

    private bool isTransitioning = false;

    void Start()
    {
        // 1. Setup the button to be invisible but ready
        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.alpha = 0;
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;
        }

        // 2. Start sequences
        StartCoroutine(ShowButtonAfterDelay());

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private IEnumerator ShowButtonAfterDelay()
    {
        yield return new WaitForSeconds(timeBeforeButtonAppears);

        while (buttonCanvasGroup.alpha < 1)
        {
            buttonCanvasGroup.alpha += Time.deltaTime * buttonFadeSpeed;
            yield return null;
        }

        // Clickable only once it's visible
        buttonCanvasGroup.interactable = true;
        buttonCanvasGroup.blocksRaycasts = true;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        ReturnToMenu();
    }

    public void OnButtonClick()
    {
        ReturnToMenu();
    }

    private void ReturnToMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        SceneChanger fader = FindObjectOfType<SceneChanger>();
        if (fader != null)
        {
            fader.GoToCutscene(mainMenuSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
    }
}
