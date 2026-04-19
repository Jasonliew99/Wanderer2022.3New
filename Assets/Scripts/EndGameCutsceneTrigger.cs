using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndGameCutsceneTrigger : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("The exact name of the scene in your Build Settings")]
    [SerializeField] private string nextSceneName = "PostGameCutscene";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            SceneChanger fader = FindObjectOfType<SceneChanger>();

            if (fader != null)
            {
                Debug.Log($"Triggering Global Fader to load: {nextSceneName}");
                fader.GoToCutscene(nextSceneName);
            }
            else
            {
                // Fail-safe
                Debug.LogWarning("Global SceneChanger not found! Loading scene directly.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
