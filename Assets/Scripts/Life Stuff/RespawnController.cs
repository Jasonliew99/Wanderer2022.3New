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
    public Transform[] respawnPoints;
    public int maxLives = 3;

    [Header("UI")]
    public Canvas lifeCanvas;
    public Image[] lifeImages;
    public Sprite hurtSprite;
    public Canvas deathCanvas;
    public float lifeDisplayTime = 1f;

    [Header("Level Controller")]
    public LevelController levelController;

    [Header("Enemy Settings")]
    public string enemyTag = "Enemy";

    private int currentLives;
    private bool isRespawning = false;

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

    //              PUBLIC RESET LIFE FUNCTION
    public void ResetLivesToFull()
    {
        currentLives = maxLives;

        foreach (var img in lifeImages)
        {
            if (img != null)
            {
                img.enabled = true;
            }
        }

        Debug.Log("[RespawnController] Lives reset to full.");
    }


    //                ON PLAYER DEATH
    public void HandlePlayerDeath()
    {
        //if (!levelController.IsAnyLevelRunning()) return;
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
        int i = Random.Range(0, respawnPoints.Length);
        player.transform.position = respawnPoints[i].position;
    }


    // UI Buttons
    public void RetryLevel()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMenu(string sceneName)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(sceneName);
    }
}
