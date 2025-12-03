using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelActivater : MonoBehaviour
{
    public LevelController controller;
    public int levelID = 0; // Which level this starts

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Level {levelID + 1} ACTIVATOR touched");
            controller.StartLevel(levelID);
        }
    }
}
