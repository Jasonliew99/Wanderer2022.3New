using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinCollect : MonoBehaviour
{
    public int value = 1;
    public LevelController levelController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCoinCollector coinCollector = other.GetComponent<PlayerCoinCollector>();
            if (coinCollector != null)
            {
                coinCollector.AddCoins(value);
            }

            // Disable the coin first
            gameObject.SetActive(false);

            // notify level controller
            levelController?.CoinCollected();
        }
    }
}
