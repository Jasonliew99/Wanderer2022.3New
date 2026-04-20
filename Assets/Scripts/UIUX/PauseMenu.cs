using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; 
        isPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; 
        isPaused = true;
    }

    // Restart the current scene
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;

        if (SceneChanger.Instance != null)
        {
            SceneChanger.Instance.GoToCutscene(currentScene);
        }
        else
        {
            SceneManager.LoadScene(currentScene);
        }
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;

        if (SceneChanger.Instance != null)
        {
            SceneChanger.Instance.GoToCutscene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
