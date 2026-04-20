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
 
            ChargeSwapLogic swapLogic = GetComponentInParent<ChargeSwapLogic>();

            if (swapLogic != null)
            {
                swapLogic.PlayCollectSound();
            }
            playerCoinCollector.ShowItemPopup(itemID);

            levelController.FragmentCollected(itemID);

            spawnPoint.isCollected = true;


            gameObject.SetActive(false);
        }
    }
}
