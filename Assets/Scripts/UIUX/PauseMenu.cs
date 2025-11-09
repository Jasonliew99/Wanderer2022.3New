using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; //pausepanel here
    public KeyCode pauseKey = KeyCode.Escape; //the button that triggers

    private bool isPaused = false;

    void Update()
    {

        if (Input.GetKeyDown(pauseKey))//toggle when pressed
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
}
