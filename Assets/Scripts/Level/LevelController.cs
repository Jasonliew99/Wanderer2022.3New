using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("Level Objects")]
    [SerializeField] private GameObject[] bosses;
    [SerializeField] private GameObject[] coins;

    [Header("Door Sets")]
    [SerializeField] private GameObject[] closedDoors;   // closed door version
    [SerializeField] private GameObject[] openDoors;     // open door version

    [SerializeField] private GameObject[] levelActivators;
    [SerializeField] private GameObject[] levelDeactivators;

    [Header("Objective UI")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private string firstObjective = "Collect all coins!";
    [SerializeField] private string secondObjective = "Find the exit!";
    [SerializeField] private float objectiveFadeDuration = 0.5f;

    [Header("Display Options")]
    [SerializeField] private bool enableFade = true;
    [SerializeField] private bool useDisplayTime = true;
    [SerializeField] private float objectiveDisplayTime = 2f;

    private Coroutine objectiveCoroutine;
    private bool levelStarted = false;
    public bool LevelStarted => levelStarted;
    private bool levelEnded = false;

    void Start()
    {
        SetActiveArray(bosses, false);

        // --- NEW LOGIC ---
        // Scene loads OPEN → open door visible, closed door hidden
        SetActiveArray(closedDoors, false);
        SetActiveArray(openDoors, true);

        SetActiveArray(levelDeactivators, false);
        SetActiveArray(coins, false);

        if (objectiveText != null && enableFade)
            objectiveText.alpha = 0f;
    }

    public void StartLevel()
    {
        if (levelStarted) return;

        levelStarted = true;
        levelEnded = false;

        Debug.Log($"[{name}] Level started!");
        SetActiveArray(levelActivators, false);
        SetActiveArray(bosses, true);
        SetActiveArray(coins, true);

        // --- NEW LOGIC ---
        // Level starts → CLOSE the door
        SetActiveArray(closedDoors, true);
        SetActiveArray(openDoors, false);

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
        yield return null;

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
        Debug.Log($"[{name}] Exit opened. Doors swapped.");

        // --- NEW LOGIC ---
        // Exit opens → OPEN DOOR visible
        SetActiveArray(closedDoors, false);
        SetActiveArray(openDoors, true);

        SetActiveArray(levelDeactivators, true);
    }

    public void EndLevel()
    {
        if (levelEnded) return;
        levelEnded = true;

        Debug.Log($"[{name}] Level ended! Closing area.");

        SetActiveArray(bosses, false);
        SetActiveArray(levelDeactivators, false);

        // --- NEW LOGIC ---
        // Level ended → CLOSE DOOR again
        SetActiveArray(closedDoors, true);
        SetActiveArray(openDoors, false);
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

    private IEnumerator ShowObjective(string text)
    {
        objectiveText.text = text;

        if (enableFade)
        {
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

        if (useDisplayTime)
        {
            yield return new WaitForSeconds(objectiveDisplayTime);

            if (enableFade)
            {
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
    }
}
