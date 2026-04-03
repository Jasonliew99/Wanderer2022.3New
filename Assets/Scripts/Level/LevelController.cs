using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    // ===========================
    // CORE ARRAYS (AREAS)
    // ===========================
    [Header("Level Configuration")]
    public List<LevelBlock> levels = new List<LevelBlock>();

    // ===========================
    // ESCAPE MODE STATE
    // ===========================
    public bool EscapeMode { get; private set; } = false;
    public System.Action OnEscapeMode;

    [Space(20)]
    [Header("--- ESCAPE MODE SETTINGS ---")]
    public GameObject[] objectsToEnableOnEscape;
    public GameObject[] objectsToDisableOnEscape;
    public GameObject[] escapeEnemies;

    [Space(10)]
    public string[] escapeDialogues = {
        "All fragments collected!",
        "The facility is destabilizing...",
        "ESCAPE TO THE ENTRANCE!"
    };

    [Header("Win Settings (After Reaching Entrance)")]
    public string[] winDialogues = {
        "You made it out...",
        "The facility is now locked down.",
        "Thank you for playing!"
    };

    [System.Serializable]
    public class FragmentItemData
    {
        public string itemID;
        public Sprite[] revealOrder;
        public Sprite[] progressSprites;
        [HideInInspector] public int revealed = 0;
        [HideInInspector] public int collected = 0;
        public int TotalRequired => revealOrder.Length;
        public void ResetRuntime() { revealed = 0; collected = 0; }
    }

    [System.Serializable]
    public class DoorPair
    {
        public GameObject open;
        public GameObject closed;
        public bool startOpened = false;
    }

    [System.Serializable]
    public class LevelBlock
    {
        public string areaName = "Unnamed Area";
        public Transform[] respawnPoints;
        public Transform[] enemyResetPoints;
        public DoorPair entranceDoor;
        public DoorPair exitDoor;
        public List<FragmentItemData> fragmentItems;
        public GameObject[] enemies;
        public GameObject[] coins;
        public GameObject activator;
        public GameObject deactivator;
        public string firstObjective = "Collect all fragments!";
        public string secondObjective = "Find the exit!";
    }

    [Header("UI & Respawn References")]
    public RespawnController respawnController;
    public TextMeshProUGUI objectiveText;
    public float fadeDuration = 0.4f;
    public float displayTime = 1.6f;

    [HideInInspector] public int currentLevelIndex = -1;
    private Coroutine uiRoutine;

    // ===========================
    // INITIAL SETUP
    // ===========================
    void Start()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];
            SetActive(lvl.enemies, false);
            SetActive(lvl.coins, false);

            // FORCE Level 1 Entrance to be Open at the very start of the game
            if (i == 0)
            {
                OpenDoor(lvl.entranceDoor);
            }
            else if (lvl.entranceDoor != null && lvl.entranceDoor.startOpened)
            {
                OpenDoor(lvl.entranceDoor);
            }
            else
            {
                CloseDoor(lvl.entranceDoor);
            }

            CloseDoor(lvl.exitDoor);
        }

        // Only enable the first level's trigger
        if (levels.Count > 0 && levels[0].activator != null)
            levels[0].activator.SetActive(true);

        if (objectiveText != null) objectiveText.alpha = 0;
    }

    private void SyncLevelObjects(int activeID)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];
            bool isCurrent = (i == activeID);

            SetActive(lvl.enemies, isCurrent);
            SetActive(lvl.coins, isCurrent);

            if (lvl.activator != null)
                lvl.activator.SetActive(i > activeID);

            if (isCurrent && !EscapeMode)
            {
                // THE SWAP FIX:
                // If this is Level 1 (Index 0), we want the door to CLOSE now 
                // because the player has entered and the level has officially started.
                if (activeID == 0)
                {
                    // This forces the 'Open' door to hide and 'Closed' door to show
                    CloseDoor(lvl.entranceDoor);
                }
                else
                {
                    // For other levels, respect the toggle
                    if (lvl.entranceDoor.startOpened) OpenDoor(lvl.entranceDoor);
                    else CloseDoor(lvl.entranceDoor);
                }

                CloseDoor(lvl.exitDoor);
            }
        }
    }

    public void StartLevel(int id)
    {
        if (EscapeMode) return;
        if (id <= currentLevelIndex && currentLevelIndex != -1) return;

        Debug.Log($"<color=orange>LevelController: Starting Level {id}</color>");
        currentLevelIndex = id;
        SyncLevelObjects(id);

        foreach (var item in levels[id].fragmentItems)
            item.ResetRuntime();

        ShowObjective(levels[id].firstObjective);

        if (respawnController != null)
            respawnController.OnLevelStarted(levels[id].respawnPoints);
    }

    public void ResetEnemiesForRespawn()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count) return;
        ResetSpecificLevelEnemies(currentLevelIndex);
    }

    private void ResetSpecificLevelEnemies(int id)
    {
        LevelBlock lvl = levels[id];
        if (lvl.enemies == null || lvl.enemies.Length == 0) return;

        // 1. Shuffle points to ensure no two enemies are in the same spot
        List<Transform> availablePoints = new List<Transform>(lvl.enemyResetPoints);
        for (int i = 0; i < availablePoints.Count; i++)
        {
            Transform temp = availablePoints[i];
            int randomIndex = Random.Range(i, availablePoints.Count);
            availablePoints[i] = availablePoints[randomIndex];
            availablePoints[randomIndex] = temp;
        }

        // 2. Loop through enemies and apply the State Reset
        for (int i = 0; i < lvl.enemies.Length; i++)
        {
            GameObject enemy = lvl.enemies[i];
            if (enemy == null) continue;

            // Move to unique point
            int pointIndex = i % availablePoints.Count;
            enemy.transform.position = availablePoints[pointIndex].position;
            enemy.transform.rotation = availablePoints[pointIndex].rotation;

            // 3. TRIGGER THE STATE DETERMINATION
            // This checks your 'DefaultState' enum and runs the correct behavior
            EnemyStateReset resetScript = enemy.GetComponent<EnemyStateReset>();
            if (resetScript != null)
            {
                resetScript.ResetToDefaultState();
            }
            else
            {
                // Fallback for enemies without the script
                enemy.SetActive(false);
                enemy.SetActive(true);
            }
        }
    }

    public Sprite RequestNextFragment(string itemID)
    {
        if (currentLevelIndex < 0) return null;
        LevelBlock lvl = levels[currentLevelIndex];
        FragmentItemData item = lvl.fragmentItems.Find(i => i.itemID == itemID);
        if (item == null || item.revealed >= item.revealOrder.Length) return null;

        Sprite next = item.revealOrder[item.revealed];
        item.revealed++;
        return next;
    }

    public void FragmentCollected(string itemID)
    {
        if (currentLevelIndex < 0) return;
        LevelBlock lvl = levels[currentLevelIndex];
        FragmentItemData item = lvl.fragmentItems.Find(i => i.itemID == itemID);
        if (item == null) return;

        item.collected++;
        StartCoroutine(CheckItemsComplete());
    }

    private IEnumerator CheckItemsComplete()
    {
        yield return new WaitForEndOfFrame();
        LevelBlock lvl = levels[currentLevelIndex];
        bool allDone = true;

        foreach (var item in lvl.fragmentItems)
            if (item.collected < item.TotalRequired)
                allDone = false;

        if (allDone)
        {
            ShowObjective(lvl.secondObjective);

            // Open the exit door for the current level
            OpenDoor(lvl.exitDoor);

            // If we are in the FINAL level (Level 3), start escape mode immediately
            if (currentLevelIndex == levels.Count - 1)
            {
                StartEscapeMode();
            }
            else
            {
                // Just enable the deactivator (exit trigger) for normal levels
                SetActive(lvl.deactivator, true);
            }
        }
    }

    public void EndLevel(int id)
    {
        if (EscapeMode && id == 0)
        {
            WinGame();
            return;
        }

        if (id != currentLevelIndex) return;

        LevelBlock lvl = levels[id];
        SetActive(lvl.enemies, false);
        SetActive(lvl.coins, false);
        SetActive(lvl.deactivator, false);

        if (id < levels.Count - 1)
        {
            LevelBlock nextLvl = levels[id + 1];
            OpenDoor(nextLvl.entranceDoor);
            if (nextLvl.activator != null)
                nextLvl.activator.SetActive(true);
        }
    }

    private void StartEscapeMode()
    {
        if (EscapeMode) return;
        EscapeMode = true;

        Debug.Log("<color=red>ESCAPE MODE: Level 3 complete. Activating Facility Lockdown!</color>");

        if (respawnController != null)
            respawnController.ResetLivesToFull();

        // Enable alarms/red lights
        SetActive(objectsToEnableOnEscape, true);
        SetActive(objectsToDisableOnEscape, false);

        // Spawn special escape-only enemies if you have any
        SetActive(escapeEnemies, true);

        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];

            // 1. OPEN ALL DOORS for the run back
            OpenDoor(lvl.entranceDoor);
            OpenDoor(lvl.exitDoor);

            // 2. ENEMY LOGIC:
            // If it's Level 1 or Level 2 (index 0 or 1), spawn them back in.
            if (i < levels.Count - 1)
            {
                Debug.Log($"<color=orange>Escape Mode: Re-spawning enemies for {lvl.areaName}</color>");
                SetActive(lvl.enemies, true);
                ResetSpecificLevelEnemies(i); // Put them at their start points
            }
            else
            {
                // Level 3 enemies are already active, so we do nothing to them.
                Debug.Log("<color=orange>Escape Mode: Keeping Level 3 enemies as they are.</color>");
            }

            // 3. CLEANUP: Disable start triggers so they don't interfere
            if (lvl.activator) lvl.activator.SetActive(false);

            // Ensure Level 1's "End" trigger is ready to catch the player for the win
            if (i == 0 && lvl.deactivator) lvl.deactivator.SetActive(true);
        }

        if (uiRoutine != null) StopCoroutine(uiRoutine);
        uiRoutine = StartCoroutine(SequenceRoutine(escapeDialogues));
        OnEscapeMode?.Invoke();
    }

    private void WinGame()
    {
        if (uiRoutine != null) StopCoroutine(uiRoutine);
        SetActive(escapeEnemies, false);
        SetActive(objectsToEnableOnEscape, false);

        foreach (var lvl in levels)
        {
            SetActive(lvl.enemies, false);
            CloseDoor(lvl.entranceDoor);
            CloseDoor(lvl.exitDoor);
        }
        uiRoutine = StartCoroutine(SequenceRoutine(winDialogues));
    }

    private IEnumerator SequenceRoutine(string[] dialogueList)
    {
        foreach (string msg in dialogueList)
            yield return StartCoroutine(ObjectiveRoutine(msg, displayTime));
    }

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

    private void SetActive(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        foreach (var o in arr) if (o) o.SetActive(state);
    }

    private void SetActive(GameObject obj, bool state) { if (obj != null) obj.SetActive(state); }

    private void ShowObjective(string msg)
    {
        if (uiRoutine != null) StopCoroutine(uiRoutine);
        uiRoutine = StartCoroutine(ObjectiveRoutine(msg, displayTime));
    }

    private IEnumerator ObjectiveRoutine(string msg, float duration)
    {
        if (objectiveText == null) yield break;
        objectiveText.text = msg;
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            objectiveText.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
        yield return new WaitForSeconds(duration);
        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            objectiveText.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }
    }
}
