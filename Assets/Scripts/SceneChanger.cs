using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SceneChanger : MonoBehaviour
{
    public CanvasGroup fader;
    [Range(0.1f, 5f)]
    public float fadeSpeed = 1.0f; // Lower is slower/more cinematic

    private static SceneChanger instance;

    void Awake()
    {
        // Prevents having two managers if you go back to the menu
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Start invisible
            if (fader != null) fader.alpha = 0;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GoToCutscene(string sceneName)
    {
        StartCoroutine(LoadSequence(sceneName));
    }

    IEnumerator LoadSequence(string sceneName)
    {
        // 1. Fade OUT to Black
        while (fader.alpha < 1)
        {
            fader.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // Optional: Stay black for a split second to breathe
        yield return new WaitForSeconds(0.2f);

        // 2. Load the scene
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        // 3. Prepare the Video
        VideoPlayer vp = FindObjectOfType<VideoPlayer>();
        if (vp != null)
        {
            vp.Prepare();
            while (!vp.isPrepared) yield return null;
            vp.Play();
        }

        // 4. Fade IN to the new scene
        while (fader.alpha > 0)
        {
            fader.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }
}
