using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;   // Assign your PauseMenu panel here
    public KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;

    void Update()
    {
        // Toggle pause when pressing the key
        if (Input.GetKeyDown(pauseKey))
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
        Time.timeScale = 0f;          // Freeze everything
        isPaused = true;
    }
}
