using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "Level1";

    [Header("Audio Settings")]
    public AudioSource menuAudioSource;
    public AudioClip clickSound;

    [Header("Video & UI Animation")]
    public VideoPlayer menuVideo;
    public CanvasGroup playButtonCanvasGroup;

    public float delayBeforeFade = 1.5f;
    public float fadeDuration = 1.0f;

    void Start()
    {
        if (playButtonCanvasGroup != null)
        {
            playButtonCanvasGroup.alpha = 0;
            playButtonCanvasGroup.interactable = false;
            playButtonCanvasGroup.blocksRaycasts = false;

            StartCoroutine(FadeInUI());
        }

        if (menuVideo != null)
        {
            menuVideo.loopPointReached += OnVideoFinished;
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        vp.playbackSpeed = 0;
    }

    IEnumerator FadeInUI()
    {
        yield return new WaitForSeconds(delayBeforeFade);

        float counter = 0;
        while (counter < fadeDuration)
        {
            counter += Time.deltaTime;
            playButtonCanvasGroup.alpha = Mathf.Lerp(0, 1, counter / fadeDuration);
            yield return null;
        }

        playButtonCanvasGroup.alpha = 1;
        playButtonCanvasGroup.interactable = true;
        playButtonCanvasGroup.blocksRaycasts = true;
    }

    public void PlayGame()
    {
        if (menuAudioSource != null && clickSound != null)
        {
            menuAudioSource.PlayOneShot(clickSound);
        }

        if (playButtonCanvasGroup != null)
        {
            playButtonCanvasGroup.interactable = false;
            playButtonCanvasGroup.blocksRaycasts = false;
            playButtonCanvasGroup.alpha = 0;
        }

        if (SceneChanger.Instance != null)
        {
            SceneChanger.Instance.GoToCutscene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
