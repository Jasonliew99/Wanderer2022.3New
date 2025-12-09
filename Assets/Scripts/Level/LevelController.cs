using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    // ---------------------------
    // DOOR PAIR (Open + Closed)
    // ---------------------------
    [System.Serializable]
    public class DoorPair
    {
        public GameObject open;       // Open state sprite/mesh
        public GameObject closed;     // Closed state sprite/mesh
        public bool startOpened = false;
    }

    // ---------------------------
    // LEVEL BLOCK
    // ---------------------------
    [System.Serializable]
    public class LevelBlock
    {
        [Header("Doors")]
        public DoorPair entranceDoor;     // For Level 1 = A, Level 2 = B, Level 3 = C
        public DoorPair exitDoor;         // For Level 1 = B, Level 2 = C, Level 3 = NONE

        [Header("Objects")]
        public GameObject[] enemies;
        public GameObject[] coins;

        [Header("Triggers")]
        public GameObject activator;
        public GameObject deactivator;

        [Header("Text")]
        public string firstObjective = "Collect all coins!";
        public string secondObjective = "Find the exit!";
    }

    // ---------------------------
    // INSPECTOR
    // ---------------------------
    public List<LevelBlock> levels = new List<LevelBlock>();

    [Header("UI")]
    public TextMeshProUGUI objectiveText;
    public float fadeDuration = 0.4f;
    public float displayTime = 1.6f;

    private int currentLevelIndex = -1;
    private Coroutine uiRoutine;
    private bool levelRunning = false;



    // ---------------------------------------------------------
    // INITIAL SETUP
    // ---------------------------------------------------------
    void Start()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];

            // Set starting door states
            InitializeDoor(lvl.entranceDoor);
            InitializeDoor(lvl.exitDoor);

            // Objects OFF until level is active
            SetActive(lvl.enemies, false);
            SetActive(lvl.coins, false);
            SetActive(lvl.deactivator, false);

            // Level 1 activator ON, others OFF
            lvl.activator.SetActive(i == 0);
        }

        if (objectiveText != null)
            objectiveText.alpha = 0;
    }

    private void InitializeDoor(DoorPair door)
    {
        if (door == null) return;

        if (door.startOpened)
        {
            if (door.open) door.open.SetActive(true);
            if (door.closed) door.closed.SetActive(false);
        }
        else
        {
            if (door.open) door.open.SetActive(false);
            if (door.closed) door.closed.SetActive(true);
        }
    }



    // ---------------------------------------------------------
    // START LEVEL
    // ---------------------------------------------------------
    public void StartLevel(int id)
    {
        if (levelRunning) return;

        currentLevelIndex = id;
        levelRunning = true;

        LevelBlock lvl = levels[id];

        // CLOSE entrance
        CloseDoor(lvl.entranceDoor);

        // Turn on gameplay
        SetActive(lvl.enemies, true);
        SetActive(lvl.coins, true);

        // Disable activator once used
        lvl.activator.SetActive(false);

        ShowObjective(lvl.firstObjective);
    }



    // ---------------------------------------------------------
    // COIN COLLECTION CHECK
    // ---------------------------------------------------------
    public void CoinCollected()
    {
        if (!levelRunning) return;
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
            // OPEN EXIT DOOR
            OpenDoor(lvl.exitDoor);

            // Enable exit trigger
            lvl.deactivator.SetActive(true);

            ShowObjective(lvl.secondObjective);
        }
    }



    // ---------------------------------------------------------
    // END LEVEL
    // ---------------------------------------------------------
    public void EndLevel(int id)
    {
        levelRunning = false;
        LevelBlock lvl = levels[id];

        // Disable objects
        SetActive(lvl.enemies, false);
        SetActive(lvl.coins, false);
        lvl.deactivator.SetActive(false);

        // Close exit door (unless it's level 3 end)
        if (id < levels.Count - 1)
            CloseDoor(lvl.exitDoor);

        // IF LAST LEVEL → OPEN ALL DOORS FOR ESCAPE
        if (id == levels.Count - 1)
        {
            OpenAllDoorsForEscape();
            ShowObjective("Escape!");
            return;
        }

        // OPEN next level entrance
        LevelBlock nextLvl = levels[id + 1];
        OpenDoor(nextLvl.entranceDoor);

        // Enable next level activator
        nextLvl.activator.SetActive(true);
    }



    // ---------------------------------------------------------
    // DOOR HELPERS
    // ---------------------------------------------------------
    private void OpenDoor(DoorPair door)
    {
        if (door == null) return;
        if (door.open) door.open.SetActive(true);
        if (door.closed) door.closed.SetActive(false);
    }

    private void CloseDoor(DoorPair door)
    {
        if (door == null) return;
        if (door.open) door.open.SetActive(false);
        if (door.closed) door.closed.SetActive(true);
    }

    private void OpenAllDoorsForEscape()
    {
        Debug.Log("Opening ALL doors for final escape…");

        foreach (var lvl in levels)
        {
            OpenDoor(lvl.entranceDoor);
            OpenDoor(lvl.exitDoor);
        }
    }



    // ---------------------------------------------------------
    // VISIBILITY HELPERS
    // ---------------------------------------------------------
    private void SetActive(GameObject obj, bool state)
    {
        if (obj) obj.SetActive(state);
    }

    private void SetActive(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        foreach (var o in arr)
            if (o) o.SetActive(state);
    }



    // ---------------------------------------------------------
    // UI OBJECTIVE HANDLING
    // ---------------------------------------------------------
    private void ShowObjective(string msg)
    {
        if (uiRoutine != null)
            StopCoroutine(uiRoutine);

        uiRoutine = StartCoroutine(ObjectiveRoutine(msg));
    }

    private IEnumerator ObjectiveRoutine(string msg)
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
