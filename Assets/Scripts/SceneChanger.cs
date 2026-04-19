using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SceneChanger : MonoBehaviour
{
    public CanvasGroup fader;
    [Range(0.1f, 5f)]
    public float fadeSpeed = 1.0f;
    public static SceneChanger Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fader != null)
            {
                fader.alpha = 0;
                fader.blocksRaycasts = false;
            }
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
        if (fader != null) fader.blocksRaycasts = true;

        float startVolume = AudioListener.volume;

        while (fader.alpha < 1)
        {
            float delta = Time.unscaledDeltaTime * fadeSpeed;
            fader.alpha += delta;

            AudioListener.volume = Mathf.Max(0, AudioListener.volume - delta);

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
        AudioListener.volume = 0;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        VideoPlayer vp = FindObjectOfType<VideoPlayer>();
        if (vp != null)
        {
            vp.Prepare();
            while (!vp.isPrepared) yield return null;
            vp.Play();
        }

        while (fader.alpha > 0)
        {
            float delta = Time.unscaledDeltaTime * fadeSpeed;
            fader.alpha -= delta;

            AudioListener.volume = Mathf.Min(startVolume, AudioListener.volume + delta);

            yield return null;
        }

        AudioListener.volume = startVolume;
        if (fader != null) fader.blocksRaycasts = false;
    }
}
