using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentPickup : MonoBehaviour
{
    public string itemID;
    public LevelController levelController;
    public PlayerCoinCollector playerCoinCollector;
    private FragmentSpawnPoint spawnPoint;

    private void Awake()
    {
        spawnPoint = GetComponentInParent<FragmentSpawnPoint>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // notify PlayerCoinCollector to show popup
            playerCoinCollector.ShowItemPopup(itemID);

            //notify LevelController to update progress
            levelController.FragmentCollected(itemID);

            spawnPoint.isCollected = true;
            gameObject.SetActive(false);
        }
    }
}
