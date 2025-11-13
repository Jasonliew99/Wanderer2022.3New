using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "Level1"; // Replace with your actual scene name

    // Call this from the Play button
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Optional: Quit button
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
