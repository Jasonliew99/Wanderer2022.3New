using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCoinCollector : MonoBehaviour
{
    public int totalCoins = 0;

    public void AddCoins(int amount)
    {
        totalCoins += amount;

        //this shows up in the console
        Debug.Log("Coins: " + totalCoins);
    }
}
