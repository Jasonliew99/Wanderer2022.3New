using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelDeactivator : MonoBehaviour
{
    public LevelController controller;
    public int levelID = 0; // Which level this ends

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Level {levelID + 1} DEACTIVATOR touched");
            controller.EndLevel(levelID);
        }
    }
}
