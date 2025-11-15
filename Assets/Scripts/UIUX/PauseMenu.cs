using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; //pausepanel here
    public KeyCode pauseKey = KeyCode.Escape; //the button that triggers

    private bool isPaused = false;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu"; // scene to load when exiting

    void Update()
    {
        if (Input.GetKeyDown(pauseKey)) //toggle when pressed
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    // Call this from Resume button OnClick
    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Hide pause menu
        Time.timeScale = 1f;          // Resume game
        isPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);  // Show pause menu
        Time.timeScale = 0f;          // pause everything
        isPaused = true;
    }

    // ---------------------------
    // NEW FUNCTIONS BELOW
    // ---------------------------

    // Restart the current scene
    public void RestartLevel()
    {
        Time.timeScale = 1f; // important so the new scene isn't frozen
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    // Exit to Main Menu scene
    public void ExitToMainMenu()
    {
        Time.timeScale = 1f; // reset timescale
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
