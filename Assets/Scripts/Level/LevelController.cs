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
    [Tooltip("Drag objects here that should be ACTIVE during escape (e.g., open door paths).")]
    public GameObject[] objectsToEnableOnEscape;

    [Tooltip("Drag objects here that should be INACTIVE during escape (e.g., closed door blockers).")]
    public GameObject[] objectsToDisableOnEscape;

    [Tooltip("Specific enemies that only spawn/activate during the escape run.")]
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

    [Range(0.5f, 10f)]
    public float dialogueDisplayTime = 2.0f;

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
        [Header("Player Respawn Points")]
        public Transform[] respawnPoints;
        [Header("Enemy Reset Points")]
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
        SyncLevelObjects(-1);
        SetActive(objectsToEnableOnEscape, false);
        SetActive(escapeEnemies, false);

        if (levels.Count > 0 && levels[0].activator != null)
            levels[0].activator.SetActive(true);

        if (objectiveText != null)
            objectiveText.alpha = 0;
    }

    private void SyncLevelObjects(int activeID)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            bool isCurrent = (i == activeID);
            LevelBlock lvl = levels[i];

            SetActive(lvl.enemies, isCurrent);
            SetActive(lvl.coins, isCurrent);
            SetActive(lvl.deactivator, false);

            if (lvl.activator != null)
                lvl.activator.SetActive(i > activeID);

            if (isCurrent)
            {
                CloseDoor(lvl.entranceDoor);
                CloseDoor(lvl.exitDoor);
            }
        }
    }

    // ===========================
    // START LEVEL
    // ===========================
    public void StartLevel(int id)
    {
        if (EscapeMode) return;
        if (id <= currentLevelIndex && currentLevelIndex != -1) return;

        Debug.Log($"<color=cyan>LevelController:</color> Starting Level {id} ({levels[id].areaName})");

        currentLevelIndex = id;
        SyncLevelObjects(id);

        foreach (var item in levels[id].fragmentItems)
            item.ResetRuntime();

        ShowObjective(levels[id].firstObjective);

        if (respawnController != null)
            respawnController.OnLevelStarted(levels[id].respawnPoints);
    }

    // ===========================
    // ENEMY RESET
    // ===========================
    public void ResetEnemiesForRespawn()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count) return;
        ResetSpecificLevelEnemies(currentLevelIndex);
    }

    private void ResetSpecificLevelEnemies(int id)
    {
        LevelBlock lvl = levels[id];
        if (lvl.enemyResetPoints == null || lvl.enemyResetPoints.Length == 0) return;

        List<Transform> availablePoints = new List<Transform>(lvl.enemyResetPoints);

        foreach (var enemy in lvl.enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;
            if (availablePoints.Count == 0) break;

            int i = Random.Range(0, availablePoints.Count);
            enemy.transform.position = availablePoints[i].position;
            availablePoints.RemoveAt(i);

            EnemyStateReset reset = enemy.GetComponent<EnemyStateReset>();
            if (reset != null) reset.ResetToDefaultState();
        }
    }

    // ===========================
    // FRAGMENT SYSTEM
    // ===========================
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
        Debug.Log($"<color=yellow>Fragment:</color> {itemID} collected ({item.collected}/{item.TotalRequired})");
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
            Debug.Log($"<color=green>Area Complete:</color> All fragments found in {lvl.areaName}");

            // --- CHANGE: TRIGGER ESCAPE IMMEDIATELY IF IN LAST LEVEL ---
            if (currentLevelIndex == levels.Count - 1)
            {
                Debug.Log("<color=red>Final Level Complete! Triggering Instant Escape Mode.</color>");
                StartEscapeMode();
            }
            else
            {
                OpenDoor(lvl.exitDoor);
                SetActive(lvl.deactivator, true);
                ShowObjective(lvl.secondObjective);
            }
        }
    }

    // ===========================
    // END LEVEL
    // ===========================
    public void EndLevel(int id)
    {
        Debug.Log($"<color=orange>EndLevel Triggered:</color> ID {id}. Current Escape Mode: {EscapeMode}");

        // WIN CONDITION
        if (EscapeMode && id == levels.Count - 1)
        {
            WinGame();
            return;
        }

        if (id != currentLevelIndex) return;

        LevelBlock lvl = levels[id];
        SetActive(lvl.enemies, false);
        SetActive(lvl.coins, false);
        SetActive(lvl.deactivator, false);

        if (id == levels.Count - 1)
        {
            StartEscapeMode();
        }
        else
        {
            LevelBlock nextLvl = levels[id + 1];
            OpenDoor(nextLvl.entranceDoor);
            if (nextLvl.activator != null)
                nextLvl.activator.SetActive(true);
        }
    }

    // ===========================
    // ESCAPE MODE
    // ===========================
    private void StartEscapeMode()
    {
        if (EscapeMode) return; // Prevent double firing

        Debug.Log("<color=red>!!! ESCAPE MODE ACTIVATED !!!</color>");
        EscapeMode = true;

        SetActive(objectsToEnableOnEscape, true);
        SetActive(objectsToDisableOnEscape, false);
        SetActive(escapeEnemies, true);

        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];
            OpenDoor(lvl.entranceDoor);
            OpenDoor(lvl.exitDoor);

            if (i < levels.Count - 1)
            {
                SetActive(lvl.enemies, true);
                ResetSpecificLevelEnemies(i);
            }

            if (lvl.activator) lvl.activator.SetActive(false);

            if (i == levels.Count - 1)
                SetActive(lvl.deactivator, true);
            else
                SetActive(lvl.deactivator, false);
        }

        if (uiRoutine != null) StopCoroutine(uiRoutine);
        uiRoutine = StartCoroutine(SequenceRoutine(escapeDialogues));
        OnEscapeMode?.Invoke();
    }

    private void WinGame()
    {
        Debug.Log("<color=gold>VICTORY!</color>");
        if (uiRoutine != null) StopCoroutine(uiRoutine);

        SetActive(escapeEnemies, false);
        SetActive(objectsToEnableOnEscape, false);

        foreach (var lvl in levels)
        {
            SetActive(lvl.enemies, false);
            SetActive(lvl.coins, false);
            CloseDoor(lvl.entranceDoor);
            CloseDoor(lvl.exitDoor);
        }
        uiRoutine = StartCoroutine(SequenceRoutine(winDialogues));
    }

    private IEnumerator SequenceRoutine(string[] dialogueList)
    {
        foreach (string msg in dialogueList)
            yield return StartCoroutine(ObjectiveRoutine(msg, dialogueDisplayTime));
    }

    // ===========================
    // HELPERS & UI
    // ===========================
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

    private void SetActive(GameObject obj, bool state)
    {
        if (obj != null) obj.SetActive(state);
    }

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
        while (t < fadeDuration) { t += Time.deltaTime; objectiveText.alpha = Mathf.Lerp(0, 1, t / fadeDuration); yield return null; }
        yield return new WaitForSeconds(duration);
        t = 0;
        while (t < fadeDuration) { t += Time.deltaTime; objectiveText.alpha = Mathf.Lerp(1, 0, t / fadeDuration); yield return null; }
    }
}
