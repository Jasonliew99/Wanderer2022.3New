using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentPickup : MonoBehaviour
{
    public string itemID;
    public LevelController levelController;
    public PlayerCoinCollector playerCoinCollector;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // notify PlayerCoinCollector to show popup
            playerCoinCollector.ShowItemPopup(itemID);

            //notify LevelController to update progress
            levelController.FragmentCollected(itemID);

            gameObject.SetActive(false);
        }
    }
}
