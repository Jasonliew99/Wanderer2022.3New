using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelDeactivator : MonoBehaviour
{
    public LevelController controller;
    public int levelID;

    //if molested by player, end level
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            controller.EndLevel(levelID);
    }
}
