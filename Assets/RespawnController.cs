using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnController : MonoBehaviour
{
    [Header("References")]
    public LevelController levelController;
    public Transform player;
    public Transform[] respawnPoints;

    [Header("Respawn Settings")]
    public int maxLives = 3;
    public float respawnDelay = 2f;

    private int currentLives;
    private bool isRespawning = false;

    void Start()
    {
        currentLives = maxLives;

        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    /// <summary>
    /// Called by enemies or hazards when the player dies.
    /// </summary>
    public void HandlePlayerDeath()
    {
        if (isRespawning) return;
        if (levelController == null || !levelController.LevelStarted) return;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        // Pause the game temporarily
        Time.timeScale = 0f;

        Debug.Log("[RespawnController] Player died! Waiting to respawn...");

        // Wait (in real time, not affected by timescale)
        yield return new WaitForSecondsRealtime(respawnDelay);

        currentLives--;

        if (currentLives > 0)
        {
            // Choose a random respawn point
            Transform randomPoint = respawnPoints[Random.Range(0, respawnPoints.Length)];
            player.position = randomPoint.position;

            Debug.Log($"[RespawnController] Player respawned. Lives left: {currentLives}");

            // Resume game
            Time.timeScale = 1f;
            isRespawning = false;
        }
        else
        {
            Debug.Log("[RespawnController] Out of lives! Restarting level...");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
