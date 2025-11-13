using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinCollect : MonoBehaviour
{
    public int value = 1;

    private LevelController levelController;
    private TorchlightRevealItem revealItem;

    private void Start()
    {
        revealItem = GetComponent<TorchlightRevealItem>();
        if (revealItem == null)
            Debug.LogWarning($"[{name}] TorchlightRevealItem missing!");

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

        TorchlightRevealItem revealItem = GetComponent<TorchlightRevealItem>();
        if (revealItem != null && !revealItem.CanBeCollected())
            return; // can't collect if hidden

        // Add to player's coin total
        PlayerCoinCollector collector = other.GetComponent<PlayerCoinCollector>();
        if (collector != null)
            collector.AddCoins(value);

        // Notify LevelController
        if (levelController != null)
            levelController.CoinCollected();

        // Disable coin
        gameObject.SetActive(false);
    }
}
