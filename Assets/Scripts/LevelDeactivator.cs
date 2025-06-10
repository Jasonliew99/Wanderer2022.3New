using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelDeactivator : MonoBehaviour
{
    public LevelController controller;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            controller.EndLevel();
        }
    }
}
