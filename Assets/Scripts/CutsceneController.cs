using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneController : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    [Header("Tutorial UI Settings")]
    public CanvasGroup tutorialCanvasGroup;
    public GameObject readyButton;
    public float fadeSpeed = 1.5f;
    public float waitTime = 3.0f;

    void Start()
    {
        if (tutorialCanvasGroup != null) tutorialCanvasGroup.alpha = 0;
        if (readyButton != null) readyButton.SetActive(false);

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
        while (tutorialCanvasGroup.alpha < 1)
        {
            tutorialCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        yield return new WaitForSeconds(waitTime);

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
