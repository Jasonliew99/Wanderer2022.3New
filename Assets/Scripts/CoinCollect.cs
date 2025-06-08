using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinCollect : MonoBehaviour
{
    public int value = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCoinCollector coinCollector = other.GetComponent<PlayerCoinCollector>();
            if (coinCollector != null)
            {
                coinCollector.AddCoins(value);
            }

            Destroy(gameObject); // Remove coin
        }
    }
}
