using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("Level Objects")]
    [SerializeField] private GameObject[] bosses;
    [SerializeField] private GameObject[] coins;
    [SerializeField] private GameObject[] doors;
    [SerializeField] private GameObject[] levelActivators;
    [SerializeField] private GameObject[] levelDeactivators;

    [Header("Objective UI")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private string firstObjective = "Collect all coins!";
    [SerializeField] private string secondObjective = "Find the exit!";
    [SerializeField] private float objectiveFadeDuration = 0.5f;

    [Header("Display Options")]
    [SerializeField] private bool enableFade = true;         // Fade in/out enabled
    [SerializeField] private bool useDisplayTime = true;     // Toggle auto-hide
    [SerializeField] private float objectiveDisplayTime = 2f; // Duration to display text if useDisplayTime is true

    private Coroutine objectiveCoroutine;
    public bool levelStarted = false;
    public bool LevelStarted => levelStarted; // public read-only property
    private bool levelEnded = false;

    void Start()
    {
        SetActiveArray(bosses, false);
        SetActiveArray(doors, false);
        SetActiveArray(levelDeactivators, false);
        SetActiveArray(coins, false);

        if (objectiveText != null && enableFade)
            objectiveText.alpha = 0f; // start hidden
    }

    public void StartLevel()
    {
        if (levelStarted) return;

        levelStarted = true;
        levelEnded = false;

        Debug.Log($"[{name}] Level started!");
        SetActiveArray(levelActivators, false);
        SetActiveArray(bosses, true);
        SetActiveArray(doors, true);
        SetActiveArray(coins, true);

        // Show first objective
        if (objectiveText != null)
        {
            if (objectiveCoroutine != null)
                StopCoroutine(objectiveCoroutine);

            objectiveCoroutine = StartCoroutine(ShowObjective(firstObjective));
        }
    }

    public void CoinCollected()
    {
        if (!levelStarted || levelEnded) return;

        StartCoroutine(CheckCoinsNextFrame());
    }

    private IEnumerator CheckCoinsNextFrame()
    {
        yield return null; // wait one frame

        bool anyCoinsLeft = false;
        foreach (GameObject coin in coins)
        {
            if (coin != null && coin.activeInHierarchy)
            {
                anyCoinsLeft = true;
                break;
            }
        }

        if (!anyCoinsLeft)
        {
            Debug.Log($"[{name}] All coins collected! Opening exit...");
            OpenExit();

            // Show second objective
            if (objectiveText != null)
            {
                if (objectiveCoroutine != null)
                    StopCoroutine(objectiveCoroutine);

                objectiveCoroutine = StartCoroutine(ShowObjective(secondObjective));
            }
        }
    }

    private void OpenExit()
    {
        Debug.Log($"[{name}] Exit opened. Doors disabled, exit colliders active.");
        SetActiveArray(doors, false);
        SetActiveArray(levelDeactivators, true);
    }

    public void EndLevel()
    {
        if (levelEnded) return;
        levelEnded = true;

        Debug.Log($"[{name}] Level ended! Closing area.");

        SetActiveArray(bosses, false);
        SetActiveArray(levelDeactivators, false);
        SetActiveArray(doors, true);
    }

    private void SetActiveArray(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        foreach (GameObject obj in arr)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }

    // --- Show objective with optional fade and display time ---
    private IEnumerator ShowObjective(string text)
    {
        objectiveText.text = text;

        if (enableFade)
        {
            // Fade in
            float timer = 0f;
            while (timer < objectiveFadeDuration)
            {
                timer += Time.deltaTime;
                objectiveText.alpha = Mathf.Lerp(0f, 1f, timer / objectiveFadeDuration);
                yield return null;
            }
            objectiveText.alpha = 1f;
        }
        else
        {
            objectiveText.alpha = 1f;
        }

        // Wait display time if enabled
        if (useDisplayTime)
        {
            yield return new WaitForSeconds(objectiveDisplayTime);

            if (enableFade)
            {
                // Fade out
                float timer = 0f;
                while (timer < objectiveFadeDuration)
                {
                    timer += Time.deltaTime;
                    objectiveText.alpha = Mathf.Lerp(1f, 0f, timer / objectiveFadeDuration);
                    yield return null;
                }
                objectiveText.alpha = 0f;
            }
            else
            {
                objectiveText.alpha = 0f;
            }
        }
        // else: no time limit, objective stays visible
    }
}
