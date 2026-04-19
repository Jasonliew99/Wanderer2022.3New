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

    [Header("Blood Visuals")]
    public Animator bloodAnimator;
    public CanvasGroup bloodCanvasGroup;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip deathSound;

    private int currentLives;
    private bool isRespawning = false;
    private Transform[] currentRespawnPoints;

    void Start()
    {
        currentLives = maxLives;

        // Ensure everything is invisible at the start
        if (lifeCanvas != null) lifeCanvas.GetComponent<CanvasGroup>().alpha = 0;
        if (deathCanvas != null) deathCanvas.GetComponent<CanvasGroup>().alpha = 0;
        if (bloodCanvasGroup != null) bloodCanvasGroup.alpha = 0;

        lifeCanvas.gameObject.SetActive(false);
        deathCanvas.gameObject.SetActive(false);
    }

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
                lifeImages[i].sprite = fullSprite;
            }
        }
        if (deathCanvas != null) deathCanvas.gameObject.SetActive(false);
    }

    public void HandlePlayerDeath()
    {
        if (!isRespawning)
            StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        // 1. Instant Blood & Sound - Triggered immediately on death
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (bloodCanvasGroup != null) bloodCanvasGroup.alpha = 1;
        if (bloodAnimator != null) bloodAnimator.SetTrigger("PlayBlood");

        // 2. Short delay for physics impact
        yield return new WaitForSeconds(0.1f);

        // 3. Pause the game
        Time.timeScale = 0f;

        // 4. Wait for the splash to peak (Realtime because game is paused)
        yield return new WaitForSecondsRealtime(1.0f);

        // 5. Logic for Life vs Death Screen
        if (currentLives > 1)
        {
            yield return StartCoroutine(FadeInUI(lifeCanvas.GetComponent<CanvasGroup>()));

            lifeImages[currentLives - 1].sprite = hurtSprite;
            yield return new WaitForSecondsRealtime(lifeDisplayTime);

            // Cleanup for Respawn
            lifeCanvas.GetComponent<CanvasGroup>().alpha = 0;
            bloodCanvasGroup.alpha = 0;
            currentLives--;

            if (levelController != null)
                levelController.ResetEnemiesForRespawn();

            Time.timeScale = 1f;
            RespawnPlayer();
        }
        else
        {
            yield return StartCoroutine(FadeInUI(deathCanvas.GetComponent<CanvasGroup>()));
        }

        isRespawning = false;
    }

    private IEnumerator FadeInUI(CanvasGroup cg)
    {
        if (cg == null) yield break;
        cg.gameObject.SetActive(true);
        cg.alpha = 0;
        while (cg.alpha < 1)
        {
            cg.alpha += Time.unscaledDeltaTime * 2f;
            yield return null;
        }
    }

    private void RespawnPlayer()
    {
        if (currentRespawnPoints == null || currentRespawnPoints.Length == 0) return;

        int i = Random.Range(0, currentRespawnPoints.Length);
        Vector3 spawnPos = currentRespawnPoints[i].position;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        if (movement != null) movement.enabled = false;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos;
            rb.rotation = Quaternion.identity;
            rb.Sleep();
            rb.WakeUp();
        }
        else
        {
            player.transform.position = spawnPos;
        }

        StartCoroutine(ReEnableMovement(movement));
    }

    private IEnumerator ReEnableMovement(PlayerMovement movement)
    {
        yield return null;
        yield return null;

        if (movement != null)
        {
            movement.enabled = true;
        }
    }

    public void RetryLevel()
    {
        StartCoroutine(RetryRoutine());
    }

    private IEnumerator RetryRoutine()
    {
        // Reset time and hide blood before loading new scene
        Time.timeScale = 1f;
        if (bloodCanvasGroup != null) bloodCanvasGroup.alpha = 0;

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
