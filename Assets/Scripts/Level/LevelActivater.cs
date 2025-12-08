using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelActivater : MonoBehaviour
{
    public LevelController controller;
    public int levelID = 0;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Level {levelID + 1} started.");
            controller.StartLevel(levelID);
        }
    }
}
