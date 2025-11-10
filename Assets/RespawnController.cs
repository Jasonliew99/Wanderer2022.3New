using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RespawnController : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject player;
    public Transform[] respawnPoints;
    public int maxLives = 3;

    [Header("UI Settings")]
    public Canvas lifeCanvas;        // Life remaining canvas
    public Image[] lifeImages;       // 3 head images
    public Sprite hurtSprite;        // Sprite to swap when life lost
    public Canvas deathCanvas;       // Death canvas with retry/exit buttons
    public float lifeDisplayTime = 1f; // How long to show life hurt animation

    [Header("Level Controller")]
    public LevelController levelController; // Drag your LevelController here

    [Header("Enemy Settings")]
    public string enemyTag = "Enemy";   // Tag for enemies that can kill player
    public EnemyRespawn[] enemies;      // Array to reset enemy positions if needed

    [System.Serializable]
    public class EnemyRespawn
    {
        public GameObject enemy;
        public Transform[] respawnPoints; // Optional: multiple spawn points for enemy
        public bool stayAtInitialPosition = true; // If true, enemy respawns at initial position
    }

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
        // Detect collisions with enemies using overlap check
        if (!isRespawning && player != null)
        {
            Collider[] hits = Physics.OverlapSphere(player.transform.position, 0.5f);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag(enemyTag))
                {
                    HandlePlayerDeath();
                    break;
                }
            }
        }
    }

    // Call this when player dies
    public void HandlePlayerDeath()
    {
        if (isRespawning) return;
        if (levelController == null || !levelController.LevelStarted) return;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        // Pause game
        Time.timeScale = 0f;

        if (currentLives > 1)
        {
            // Show Life Canvas
            lifeCanvas.gameObject.SetActive(true);

            // Update head images
            for (int i = 0; i < lifeImages.Length; i++)
            {
                if (i < currentLives)
                    lifeImages[i].sprite = lifeImages[i].sprite; // normal sprite
                else
                    lifeImages[i].enabled = false; // hide extra heads
            }

            // Wait a short moment then swap current life to hurt
            yield return new WaitForSecondsRealtime(lifeDisplayTime);
            lifeImages[currentLives - 1].sprite = hurtSprite;

            // Wait again before fade out
            yield return new WaitForSecondsRealtime(lifeDisplayTime);

            // Hide life canvas and resume game
            lifeCanvas.gameObject.SetActive(false);
            Time.timeScale = 1f;

            currentLives--;

            // Respawn player
            RespawnPlayer();

            // Reset enemies
            ResetEnemies();
        }
        else
        {
            // Last life -> show Death Canvas
            deathCanvas.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        isRespawning = false;
    }

    private void RespawnPlayer()
    {
        if (respawnPoints.Length == 0 || player == null) return;

        // Choose random respawn point
        Transform spawnPoint = respawnPoints[Random.Range(0, respawnPoints.Length)];
        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        // Reset player state if needed (health, velocity, etc.)
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ResetEnemies()
    {
        foreach (var e in enemies)
        {
            if (e.enemy == null) continue;

            if (e.stayAtInitialPosition || e.respawnPoints.Length == 0)
            {
                // Reset to initial position
                e.enemy.transform.position = e.enemy.transform.position; // original position
            }
            else
            {
                // Choose random respawn point
                Transform spawnPoint = e.respawnPoints[Random.Range(0, e.respawnPoints.Length)];
                e.enemy.transform.position = spawnPoint.position;
                e.enemy.transform.rotation = spawnPoint.rotation;
            }

            // Optional: reset NavMeshAgent if enemy uses it
            var agent = e.enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.ResetPath();
            }
        }
    }

    // Buttons for Death Canvas
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void ExitToMenu(string menuSceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}
