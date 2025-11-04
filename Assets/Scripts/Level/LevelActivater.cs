using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelActivater : MonoBehaviour
{
    public LevelController controller;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[{name}] Activator triggered — starting {controller.name}");
            controller.StartLevel();
        }
    }
}
