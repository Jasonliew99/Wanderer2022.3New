using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    [System.Serializable]
    public class LevelBlock
    {
        [Header("Doors")]
        public GameObject[] closedDoors;
        public GameObject[] openDoors;

        [Header("Objects")]
        public GameObject[] bosses; // ENEMIES for this level
        public GameObject[] coins;

        [Header("Triggers")]
        public GameObject[] activators;
        public GameObject[] deactivators;

        [Header("Objectives")]
        public string firstObjective = "Collect all coins!";
        public string secondObjective = "Find the exit!";
    }

    [Header("All Levels")]
    public List<LevelBlock> levels = new List<LevelBlock>();

    [Header("UI")]
    public TextMeshProUGUI objectiveText;
    public float fadeDuration = 0.5f;
    public float displayTime = 2f;

    private Coroutine objectiveRoutine;
    private int currentLevelIndex = -1;
    private bool isLevelRunning = false;


    // ============================
    //          SCENE START
    // ============================
    void Start()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];

            // ENEMIES OFF AT START
            SetActive(lvl.bosses, false);

            // Coins off
            SetActive(lvl.coins, false);

            // Deactivators off
            SetActive(lvl.deactivators, false);

            if (i == 0)
            {
                // Level 1 door open
                SetActive(lvl.openDoors, true);
                SetActive(lvl.closedDoors, false);
                SetActive(lvl.activators, true);
            }
            else
            {
                // Level 2+ closed
                SetActive(lvl.openDoors, false);
                SetActive(lvl.closedDoors, true);
                SetActive(lvl.activators, false);
            }
        }

        if (objectiveText != null)
            objectiveText.alpha = 0f;
    }


    // ============================
    //          START LEVEL
    // ============================
    public void StartLevel(int levelID)
    {
        if (isLevelRunning) return;

        currentLevelIndex = levelID;
        isLevelRunning = true;

        LevelBlock lvl = levels[levelID];

        // Reset lives
        FindObjectOfType<RespawnController>()?.ResetLivesToFull();

        // TURN ON ENEMIES FOR THIS LEVEL
        SetActive(lvl.bosses, true);

        // Spawn coins
        SetActive(lvl.coins, true);

        // Close door
        SetActive(lvl.openDoors, false);
        SetActive(lvl.closedDoors, true);

        // Disable activator
        SetActive(lvl.activators, false);

        // Show objective
        ShowObjective(lvl.firstObjective);
    }


    // ============================
    //     COIN COLLECT CHECK
    // ============================
    public void CoinCollected()
    {
        if (!isLevelRunning) return;
        StartCoroutine(CheckCoins());
    }

    private IEnumerator CheckCoins()
    {
        yield return null;

        LevelBlock lvl = levels[currentLevelIndex];

        bool anyLeft = false;
        foreach (var c in lvl.coins)
            if (c.activeInHierarchy)
                anyLeft = true;

        if (!anyLeft)
        {
            // Open exit
            SetActive(lvl.openDoors, true);
            SetActive(lvl.closedDoors, false);

            // Enable exit trigger
            SetActive(lvl.deactivators, true);

            ShowObjective(lvl.secondObjective);
        }
    }


    // ============================
    //          END LEVEL
    // ============================
    public void EndLevel(int levelID)
    {
        isLevelRunning = false;

        LevelBlock lvl = levels[levelID];

        // TURN OFF ENEMIES
        SetActive(lvl.bosses, false);

        // Hide coins + exit trigger
        SetActive(lvl.coins, false);
        SetActive(lvl.deactivators, false);

        // Close door
        SetActive(lvl.openDoors, false);
        SetActive(lvl.closedDoors, true);

        // Unlock next level
        int next = levelID + 1;
        if (next < levels.Count)
        {
            var nextLvl = levels[next];

            SetActive(nextLvl.openDoors, true);
            SetActive(nextLvl.closedDoors, false);
            SetActive(nextLvl.activators, true);
        }
    }


    // ============================
    //          HELPERS
    // ============================
    private void SetActive(GameObject[] arr, bool state)
    {
        foreach (var o in arr)
            if (o != null)
                o.SetActive(state);
    }

    private void ShowObjective(string msg)
    {
        if (objectiveRoutine != null)
            StopCoroutine(objectiveRoutine);

        objectiveRoutine = StartCoroutine(FadeObjective(msg));
    }

    private IEnumerator FadeObjective(string msg)
    {
        objectiveText.text = msg;

        // Fade in
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            objectiveText.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }

        yield return new WaitForSeconds(displayTime);

        // Fade out
        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            objectiveText.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }
    }
}
