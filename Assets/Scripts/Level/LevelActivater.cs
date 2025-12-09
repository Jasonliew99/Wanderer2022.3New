using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelActivater : MonoBehaviour
{
    public LevelController controller;
    public int levelID;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            controller.StartLevel(levelID);
    }
}
