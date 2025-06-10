using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("Level Objects")]
    [SerializeField] private GameObject[] bosses;
    [SerializeField] private GameObject[] coins;
    [SerializeField] private GameObject[] doors;
    [SerializeField] private GameObject[] levelActivators;
    [SerializeField] private GameObject[] levelDeactivators;

    private bool levelStarted = false;
    private bool levelEnded = false;

    void Start()
    {
        // Hide bosses at start
        if (bosses != null)
        {
            foreach (GameObject boss in bosses)
                if (boss != null) boss.SetActive(false);
        }

        // Hide doors at start
        if (doors != null)
        {
            foreach (GameObject door in doors)
                if (door != null) door.SetActive(false);
        }

        // Hide deactivators at start
        if (levelDeactivators != null)
        {
            foreach (GameObject deactivator in levelDeactivators)
                if (deactivator != null) deactivator.SetActive(false);
        }

        // Hide coins at start
        if (coins != null)
        {
            foreach (GameObject coin in coins)
                if (coin != null) coin.SetActive(false);
        }
    }

    //when player collides with level activator
    public void StartLevel()
    {
        if (levelStarted) return;

        levelStarted = true;

        // Hide activators after player collided
        if (levelActivators != null)
        {
            foreach (GameObject activator in levelActivators)
                if (activator != null) activator.SetActive(false);
        }

        // Show bosses
        if (bosses != null)
        {
            foreach (GameObject boss in bosses)
                if (boss != null) boss.SetActive(true);
        }

        // Show doors
        if (doors != null)
        {
            foreach (GameObject door in doors)
                if (door != null) door.SetActive(true);
        }

        // Show coins
        if (coins != null)
        {
            foreach (GameObject coin in coins)
                if (coin != null) coin.SetActive(true);
        }
    }

    public void CoinCollected()
    {
        bool allCollected = true;

        foreach (GameObject coin in coins)
        {
            if (coin != null && coin.activeInHierarchy)
            {
                allCollected = false;

                // a checker
                //Debug.Log($"Coin {coin.name} is still active!");
                break;
            }
        }

        if (coins != null)
        {
            foreach (GameObject coin in coins)
            {
                if (coin != null)
                {
                    // a checker
                    //Debug.Log($"Coin {coin.name} active: {coin.activeInHierarchy}");
                }
            }
        }

        if (allCollected)
        {
            Debug.Log("All coins collected, opening exit...");
            OpenExit();
        }
    }

    private void OpenExit()
    {
        Debug.Log("OpenExit called: unlocking door and showing exit trigger");

        if (doors != null)
        {
            foreach (GameObject door in doors)
                if (door != null) door.SetActive(false);
        }

        if (levelDeactivators != null)
        {
            foreach (GameObject deactivator in levelDeactivators)
                if (deactivator != null) deactivator.SetActive(true);
        }
    }

    public void EndLevel()
    {
        if (levelEnded) return;

        levelEnded = true;

        // Hide bosses after player touches level deactivator
        if (bosses != null)
        {
            foreach (GameObject boss in bosses)
                if (boss != null) boss.SetActive(false);
        }

        // Hide deactivators after player collides
        if (levelDeactivators != null)
        {
            foreach (GameObject deactivator in levelDeactivators)
                if (deactivator != null) deactivator.SetActive(false);
        }
    }
}
