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
        [Header("Level Doors")]
        public GameObject[] closedDoors;
        public GameObject[] openDoors;

        [Header("Level Objects")]
        public GameObject[] bosses;
        public GameObject[] coins;

        [Header("Level Logic")]
        public GameObject[] activators;
        public GameObject[] deactivators;

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
    public bool fade = true;

    private Coroutine objectiveRoutine;
    private int currentLevelIndex = -1;
    private bool isLevelRunning = false;

    // Required by RespawnController
    public bool IsAnyLevelRunning()
    {
        return isLevelRunning;
    }

    void Start()
    {
        foreach (var lvl in levels)
        {
            SetActiveArray(lvl.bosses, false);
            SetActiveArray(lvl.coins, false);

            SetActiveArray(lvl.activators, true);
            SetActiveArray(lvl.deactivators, false);

            // Scene start: all doors open
            SetActiveArray(lvl.openDoors, true);
            SetActiveArray(lvl.closedDoors, false);
        }

        if (objectiveText != null)
            objectiveText.alpha = 0f;
    }

    // ========== LEVEL START ==========
    public void StartLevel(int levelID)
    {
        if (isLevelRunning) return;
        if (levelID < 0 || levelID >= levels.Count) return;

        currentLevelIndex = levelID;
        isLevelRunning = true;

        // Reset lives on new level
        var respawn = FindObjectOfType<RespawnController>();
        if (respawn != null)
            respawn.ResetLivesToFull();

        var lvl = levels[levelID];

        // Disable activators for this level
        SetActiveArray(lvl.activators, false);

        // Enable gameplay objects
        SetActiveArray(lvl.bosses, true);
        SetActiveArray(lvl.coins, true);

        // Close doors
        SetActiveArray(lvl.closedDoors, true);
        SetActiveArray(lvl.openDoors, false);

        // Objective text
        if (objectiveRoutine != null)
            StopCoroutine(objectiveRoutine);

        objectiveRoutine = StartCoroutine(ShowObjective(lvl.firstObjective));
    }

    // Backward compatibility (old script calls this)
    public void StartLevel()
    {
        StartLevel(0);
    }

    // ========== COIN COLLECTED ==========
    public void CoinCollected()
    {
        if (!isLevelRunning) return;
        StartCoroutine(CheckCoins());
    }

    private IEnumerator CheckCoins()
    {
        yield return null;

        var lvl = levels[currentLevelIndex];
        bool left = false;

        foreach (var c in lvl.coins)
        {
            if (c != null && c.activeInHierarchy)
            {
                left = true;
                break;
            }
        }

        if (!left)
        {
            Debug.Log($"LEVEL {currentLevelIndex + 1} — all coins collected!");
            OpenExit(currentLevelIndex);

            if (objectiveRoutine != null)
                StopCoroutine(objectiveRoutine);

            objectiveRoutine = StartCoroutine(ShowObjective(lvl.secondObjective));
        }
    }

    // ========== EXIT OPEN ==========
    private void OpenExit(int levelID)
    {
        var lvl = levels[levelID];

        SetActiveArray(lvl.closedDoors, false);
        SetActiveArray(lvl.openDoors, true);

        SetActiveArray(lvl.deactivators, true);

        Debug.Log($"LEVEL {levelID + 1} EXIT OPENED");
    }

    // ========== END LEVEL ==========
    public void EndLevel(int levelID)
    {
        Debug.Log($"LEVEL {levelID + 1} ENDED.");
        isLevelRunning = false;

        var lvl = levels[levelID];

        SetActiveArray(lvl.bosses, false);
        SetActiveArray(lvl.deactivators, false);

        // Close door again after leaving
        SetActiveArray(lvl.closedDoors, true);
        SetActiveArray(lvl.openDoors, false);

        // Unlock next level
        int next = levelID + 1;
        if (next < levels.Count)
        {
            SetActiveArray(levels[next].activators, true);
            Debug.Log($"LEVEL {next + 1} UNLOCKED!");
        }
    }

    // Old compatibility version
    public void EndLevel()
    {
        if (currentLevelIndex >= 0)
            EndLevel(currentLevelIndex);
    }

    // ========== HELPERS ==========

    // Needed by activators
    public int GetLevelIndexForActivator(LevelActivater activater)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            foreach (var obj in levels[i].activators)
            {
                if (obj == activater.gameObject)
                    return i;
            }
        }
        return -1;
    }

    // Needed by deactivators
    public int GetLevelIndexForDeactivator(LevelDeactivator deactivator)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            foreach (var obj in levels[i].deactivators)
            {
                if (obj == deactivator.gameObject)
                    return i;
            }
        }
        return -1;
    }

    private void SetActiveArray(GameObject[] objs, bool state)
    {
        if (objs == null) return;
        foreach (var o in objs)
            if (o != null) o.SetActive(state);
    }

    private IEnumerator ShowObjective(string txt)
    {
        objectiveText.text = txt;

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
