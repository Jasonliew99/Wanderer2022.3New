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
        public GameObject[] closedDoors;     // Pintu tutup
        public GameObject[] openDoors;       // Pintu Buka

        [Header("Objects")]
        public GameObject[] bosses;          // Shit that want to kill u
        public GameObject[] coins;           // Shit to collect

        [Header("Triggers")]
        public GameObject[] activators;      // Molest this to activate
        public GameObject[] deactivators;    // Molest this to deactivate

        //this thing makes the game looked low quality but if this shit doesnt exist player dont know what to do, like baby on day 1
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

    private int currentLevelIndex = -1;
    private bool isLevelRunning = false;
    private Coroutine objectiveRoutine;

    // INITIAL SETUP
    void Start()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];

            // Disable enemies & coins at the start
            SetActive(lvl.bosses, false);
            SetActive(lvl.coins, false);

            // Disable deactivators
            SetActive(lvl.deactivators, false);

            if (i == 0)
            {
                // LEVEL 1 starts OPEN
                SetActive(lvl.openDoors, true);
                SetActive(lvl.closedDoors, false);
                SetActive(lvl.activators, true);
            }
            else
            {
                // LEVEL 2+ start CLOSED
                SetActive(lvl.openDoors, false);
                SetActive(lvl.closedDoors, true);
                SetActive(lvl.activators, false);
            }
        }

        if (objectiveText != null)
            objectiveText.alpha = 0;
    }



    // --------------------------------------------------------------------
    // START LEVEL
    // --------------------------------------------------------------------
    public void StartLevel(int levelID)
    {
        if (isLevelRunning) return;

        currentLevelIndex = levelID;
        isLevelRunning = true;

        LevelBlock lvl = levels[levelID];

        // Close all doors for this level
        SetActive(lvl.openDoors, false);
        SetActive(lvl.closedDoors, true);

        // Activate enemies & coins
        SetActive(lvl.bosses, true);
        SetActive(lvl.coins, true);

        // Disable activators so player can't restart level
        SetActive(lvl.activators, false);

        // Show objective text
        ShowObjective(lvl.firstObjective);
    }


    // COIN CHECK
    public void CoinCollected()
    {
        if (!isLevelRunning) return;
        StartCoroutine(CheckCoinsRoutine());
    }

    private IEnumerator CheckCoinsRoutine()
    {
        yield return null;

        LevelBlock lvl = levels[currentLevelIndex];

        bool anyLeft = false;
        foreach (var c in lvl.coins)
            if (c.activeInHierarchy)
                anyLeft = true;

        if (!anyLeft)
        {
            // Open level exit
            SetActive(lvl.openDoors, true);
            SetActive(lvl.closedDoors, false);

            SetActive(lvl.deactivators, true);

            ShowObjective(lvl.secondObjective);
        }
    }


    // END LEVEL
    public void EndLevel(int levelID)
    {
        isLevelRunning = false;

        LevelBlock lvl = levels[levelID];

        // Disable enemies & coins
        SetActive(lvl.bosses, false);
        SetActive(lvl.coins, false);

        // Disable exit triggers
        SetActive(lvl.deactivators, false);

        // Close this level's doors behind player
        SetActive(lvl.openDoors, false);
        SetActive(lvl.closedDoors, true);

        // Unlock next fucking level
        int next = levelID + 1;

        if (next < levels.Count)
        {
            LevelBlock nextLvl = levels[next];

            SetActive(nextLvl.openDoors, true);
            SetActive(nextLvl.closedDoors, false);
            SetActive(nextLvl.activators, true);
        }
    }



    // HELPERS more like unhelpers making my life more miserable
    private void SetActive(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        foreach (var o in arr)
            if (o != null) o.SetActive(state);
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

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            objectiveText.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }

        yield return new WaitForSeconds(displayTime);

        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            objectiveText.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }
    }
}
