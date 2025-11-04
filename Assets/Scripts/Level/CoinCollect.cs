using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinCollect : MonoBehaviour
{
    public int value = 1;

    private LevelController levelController;

    private void Start()
    {
        // Automatically find the nearest active LevelController in the scene
        levelController = FindObjectOfType<LevelController>();
        if (levelController == null)
        {
            Debug.LogWarning($"[{name}] No LevelController found in the scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Add to player's coin total
        PlayerCoinCollector collector = other.GetComponent<PlayerCoinCollector>();
        if (collector != null)
        {
            collector.AddCoins(value);
        }

        // Notify LevelController
        if (levelController != null)
        {
            Debug.Log($"[{name}] notifying {levelController.name} that coin was collected.");
            levelController.CoinCollected();
        }

        // Disable coin
        gameObject.SetActive(false);
    }
}
