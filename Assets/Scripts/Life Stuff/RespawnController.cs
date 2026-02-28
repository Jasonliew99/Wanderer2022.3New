using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RespawnController : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject player;
    public int maxLives = 3;

    [Header("UI")]
    public Canvas lifeCanvas;
    public Image[] lifeImages;
    public Sprite hurtSprite;
    public Sprite fullSprite;
    public Canvas deathCanvas;
    public float lifeDisplayTime = 1f;

    [Header("Level Controller")]
    public LevelController levelController;

    [Header("Retry Scene")]
    public string retrySceneName;

    private int currentLives;
    private bool isRespawning = false;
    private Transform[] currentRespawnPoints;

    void Start()
    {
        currentLives = maxLives;
        lifeCanvas.gameObject.SetActive(false);
        deathCanvas.gameObject.SetActive(false);
    }

    // CALLED BY LEVELCONTROLLER WHEN ENTER NEW AREA
    public void OnLevelStarted(Transform[] newPoints)
    {
        currentRespawnPoints = newPoints;
        ResetLivesToFull();
    }

    public void ResetLivesToFull()
    {
        currentLives = maxLives;

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                lifeImages[i].enabled = true;
                lifeImages[i].sprite = fullSprite; // THIS FIXES UI RESET
            }
        }
    }

    // CALLED BY ENEMY WHEN PLAYER CAUGHT
    public void HandlePlayerDeath()
    {
        if (!isRespawning)
            StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        Time.timeScale = 0f;

        if (currentLives > 1)
        {
            lifeCanvas.gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(lifeDisplayTime);

            lifeImages[currentLives - 1].sprite = hurtSprite;

            yield return new WaitForSecondsRealtime(lifeDisplayTime);

            lifeCanvas.gameObject.SetActive(false);
            Time.timeScale = 1f;

            currentLives--;

            if (levelController != null)
                levelController.ResetEnemiesForRespawn();

            RespawnPlayer();
        }
        else
        {
            deathCanvas.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        isRespawning = false;
    }

    private void RespawnPlayer()
    {
        if (currentRespawnPoints == null || currentRespawnPoints.Length == 0)
            return;

        int i = Random.Range(0, currentRespawnPoints.Length);
        Vector3 spawnPos = currentRespawnPoints[i].position;

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        if (movement != null)
            movement.enabled = false;

        if (agent != null)
        {
            agent.ResetPath();
            agent.Warp(spawnPos);
        }

        player.transform.position = spawnPos;

        StartCoroutine(ReEnablePlayerMovement(movement));
    }

    private IEnumerator ReEnablePlayerMovement(PlayerMovement movement)
    {
        yield return null;

        if (movement != null)
            movement.enabled = true;
    }

    // RETRY NOW LOADS CLEANLY
    public void RetryLevel()
    {
        StartCoroutine(RetryRoutine());
    }

    private IEnumerator RetryRoutine()
    {
        Time.timeScale = 1f;
        yield return null;

        if (!string.IsNullOrEmpty(retrySceneName))
            SceneManager.LoadScene(retrySceneName);
        else
            Debug.LogWarning("Retry Scene Name not set!");
    }

    public void ExitToMenu(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
