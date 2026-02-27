using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    public Canvas deathCanvas;
    public float lifeDisplayTime = 1f;

    [Header("Level Controller")]
    public LevelController levelController;

    [Header("Retry Scene")]
    public string retrySceneName;

    [Header("Enemy Settings")]
    public string enemyTag = "Enemy";

    private int currentLives;
    private bool isRespawning = false;
    private Transform[] currentRespawnPoints;

    void Start()
    {
        currentLives = maxLives;
        lifeCanvas.gameObject.SetActive(false);
        deathCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isRespawning) return;

        Collider[] hits = Physics.OverlapSphere(player.transform.position, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag(enemyTag))
            {
                HandlePlayerDeath();
                return;
            }
        }
    }

    public void OnLevelStarted(Transform[] newPoints)
    {
        currentRespawnPoints = newPoints;
        ResetLivesToFull();
    }

    public void ResetLivesToFull()
    {
        currentLives = maxLives;

        foreach (var img in lifeImages)
            if (img != null)
                img.enabled = true;
    }

    public void HandlePlayerDeath()
    {
        if (!isRespawning)
            StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        Time.timeScale = 0f;

        // STILL HAS LIVES → SOFT RESPAWN
        if (currentLives > 1)
        {
            lifeCanvas.gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(lifeDisplayTime);

            lifeImages[currentLives - 1].sprite = hurtSprite;

            yield return new WaitForSecondsRealtime(lifeDisplayTime);

            lifeCanvas.gameObject.SetActive(false);
            Time.timeScale = 1f;

            currentLives--;

            // TELEPORT & RESET ENEMIES
            if (levelController != null)
                levelController.ResetEnemiesForRespawn();

            RespawnPlayer();
        }
        // NO LIVES LEFT → GAME OVER
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
        player.transform.position = currentRespawnPoints[i].position;
    }

    //  LOAD SELECTED SCENE
    public void RetryLevel()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(retrySceneName))
            SceneManager.LoadScene(retrySceneName);
        else
            Debug.LogWarning("Retry Scene Name not set in RespawnController!");
    }

    public void ExitToMenu(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
