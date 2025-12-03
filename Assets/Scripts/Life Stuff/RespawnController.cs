using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RespawnController : MonoBehaviour
{
    [System.Serializable]
    public class LevelBlock
    {
        [Header("Level Doors")]
        public GameObject[] closedDoors; // closed door models
        public GameObject[] openDoors;   // open door models

        [Header("Level Objects")]
        public GameObject[] bosses; // enemies for this level
        public GameObject[] coins;  // collectibles

        [Header("Level Triggers")]
        public GameObject[] activators;    // triggers that start this level
        public GameObject[] deactivators;  // triggers that end this level

        [Header("Objective Text")]
        public string firstObjective = "Collect all coins!";
        public string secondObjective = "Find the exit!";
    }

    [Header("All Levels")]
    public List<LevelBlock> levels = new List<LevelBlock>();

    [Header("Objective UI")]
    public TextMeshProUGUI objectiveText;
    public float fadeDuration = 0.5f;
    public float displayTime = 2f;

    private Coroutine objectiveRoutine;
    private int currentLevelIndex = -1;
    private bool isLevelRunning = false;


    // Used by respawn controller
    public bool IsAnyLevelRunning()
    {
        return isLevelRunning;
    }

    void Start()
    {
        // Initialize all levels
        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];

            // Spawn enemies but keep them frozen
            SetEnemiesFrozen(lvl.bosses, true);

            // Coins disabled at scene start
            SetActiveArray(lvl.coins, false);

            // Deactivators always off at scene start
            SetActiveArray(lvl.deactivators, false);

            if (i == 0)
            {
                // LEVEL 1 START CONDITION
                SetActiveArray(lvl.openDoors, true);
                SetActiveArray(lvl.closedDoors, false);
                SetActiveArray(lvl.activators, true);
            }
            else
            {
                // LEVEL 2+ START LOCKED
                SetActiveArray(lvl.openDoors, false);
                SetActiveArray(lvl.closedDoors, true);
                SetActiveArray(lvl.activators, false);
            }
        }

        if (objectiveText != null)
            objectiveText.alpha = 0f;
    }


    // ================================
    //          START LEVEL
    // ================================
    public void StartLevel(int levelID)
    {
        if (isLevelRunning) return;
        if (levelID < 0 || levelID >= levels.Count) return;

        currentLevelIndex = levelID;
        isLevelRunning = true;

        LevelBlock lvl = levels[levelID];

        // Reset player lives
        FindObjectOfType<RespawnController>()?.ResetLivesToFull();

        // Freeze other level enemies
        FreezeAllEnemiesExcept(levelID);

        // Unfreeze enemies for this level
        SetEnemiesFrozen(lvl.bosses, false);

        // Activate coins
        SetActiveArray(lvl.coins, true);

        // Close this level's door
        SetActiveArray(lvl.openDoors, false);
        SetActiveArray(lvl.closedDoors, true);

        // Disable this level's activators
        SetActiveArray(lvl.activators, false);

        // Show objective 1
        if (objectiveRoutine != null)
            StopCoroutine(objectiveRoutine);

        objectiveRoutine = StartCoroutine(ShowText(lvl.firstObjective));
    }


    // ================================
    //       COIN COLLECTION CHECK
    // ================================
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

        foreach (var coin in lvl.coins)
        {
            if (coin != null && coin.activeInHierarchy)
            {
                anyLeft = true;
                break;
            }
        }

        if (!anyLeft)
        {
            Debug.Log($"Level {currentLevelIndex + 1} complete!");

            // Open exits
            SetActiveArray(lvl.closedDoors, false);
            SetActiveArray(lvl.openDoors, true);

            // Enable end trigger
            SetActiveArray(lvl.deactivators, true);

            if (objectiveRoutine != null)
                StopCoroutine(objectiveRoutine);

            objectiveRoutine = StartCoroutine(ShowText(lvl.secondObjective));
        }
    }


    // ================================
    //            END LEVEL
    // ================================
    public void EndLevel(int levelID)
    {
        LevelBlock lvl = levels[levelID];
        isLevelRunning = false;

        // Freeze this level's enemies
        SetEnemiesFrozen(lvl.bosses, true);

        // Disable coins and exit triggers
        SetActiveArray(lvl.coins, false);
        SetActiveArray(lvl.deactivators, false);

        // Close this level’s doors behind player
        SetActiveArray(lvl.openDoors, false);
        SetActiveArray(lvl.closedDoors, true);

        // Unlock next level
        int next = levelID + 1;
        if (next < levels.Count)
        {
            Debug.Log($"Level {next + 1} unlocked!");
            SetActiveArray(levels[next].openDoors, true);
            SetActiveArray(levels[next].closedDoors, false);
            SetActiveArray(levels[next].activators, true);
        }
    }


    // ================================
    //        ENEMY FREEZE / UNFREEZE
    // ================================
    private void SetEnemiesFrozen(GameObject[] enemies, bool freeze)
    {
        foreach (var e in enemies)
        {
            if (e == null) continue;

            e.SetActive(true); // always visible

            // Whatever controls your enemy movement:
            var ai = e.GetComponent<MonoBehaviour>();
            if (ai != null)
                ai.enabled = !freeze;
        }
    }

    private void FreezeAllEnemiesExcept(int levelID)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (i == levelID) continue; // skip current level
            SetEnemiesFrozen(levels[i].bosses, true);
        }
    }


    // ================================
    //              HELPERS
    // ================================
    private void SetActiveArray(GameObject[] arr, bool state)
    {
        foreach (var obj in arr)
            if (obj != null)
                obj.SetActive(state);
    }

    private IEnumerator ShowText(string msg)
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
