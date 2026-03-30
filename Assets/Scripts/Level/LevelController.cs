using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    // ===========================
    // ESCAPE MODE STATE
    // ===========================
    public bool EscapeMode { get; private set; } = false;
    public System.Action OnEscapeMode;

    // ===========================
    // FRAGMENT DATA
    // ===========================
    [System.Serializable]
    public class FragmentItemData
    {
        public string itemID;
        public Sprite[] revealOrder;
        public Sprite[] progressSprites;

        [HideInInspector] public int revealed = 0;
        [HideInInspector] public int collected = 0;

        public int TotalRequired => revealOrder.Length;

        public void ResetRuntime()
        {
            revealed = 0;
            collected = 0;
        }
    }

    // ===========================
    // DOOR SYSTEM
    // ===========================
    [System.Serializable]
    public class DoorPair
    {
        public GameObject open;
        public GameObject closed;
        public bool startOpened = false;
    }

    // ===========================
    // LEVEL BLOCK
    // ===========================
    [System.Serializable]
    public class LevelBlock
    {
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

    public List<LevelBlock> levels = new List<LevelBlock>();

    [Header("Respawn Controller")]
    public RespawnController respawnController;

    public TextMeshProUGUI objectiveText;
    public float fadeDuration = 0.4f;
    public float displayTime = 1.6f;

    public int currentLevelIndex = -1;
    private Coroutine uiRoutine;

    // ===========================
    // INITIAL SETUP
    // ===========================
    void Start()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            LevelBlock lvl = levels[i];

            InitializeDoor(lvl.entranceDoor);
            InitializeDoor(lvl.exitDoor);

            SetActive(lvl.enemies, false);
            SetActive(lvl.coins, false);
            SetActive(lvl.deactivator, false);

            lvl.activator.SetActive(i == 0);
        }

        if (objectiveText != null)
            objectiveText.alpha = 0;
    }

    // ===========================
    // START LEVEL
    // ===========================
    public void StartLevel(int id)
    {
        if (EscapeMode) return; // prevent starting levels during escape

        currentLevelIndex = id;
        LevelBlock lvl = levels[id];

        foreach (var item in lvl.fragmentItems)
            item.ResetRuntime();

        CloseDoor(lvl.entranceDoor);
        CloseDoor(lvl.exitDoor);

        SetActive(lvl.enemies, false);
        SetActive(lvl.coins, false);

        SetActive(lvl.enemies, true);
        SetActive(lvl.coins, true);

        SetActive(lvl.deactivator, false);
        lvl.activator.SetActive(false);

        ShowObjective(lvl.firstObjective);

        // Reset player lives + respawn
        if (respawnController != null)
            respawnController.OnLevelStarted(lvl.respawnPoints);
    }

    // ===========================
    // ENEMY RESET (RESPAWN)
    // ===========================
    public void ResetEnemiesForRespawn()
    {
        LevelBlock lvl = levels[currentLevelIndex];

        if (lvl.enemyResetPoints == null || lvl.enemyResetPoints.Length == 0)
            return;

        List<Transform> availablePoints =
            new List<Transform>(lvl.enemyResetPoints);

        foreach (var enemy in lvl.enemies)
        {
            if (enemy == null) continue;
            if (availablePoints.Count == 0) break;

            int i = Random.Range(0, availablePoints.Count);
            Transform chosenPoint = availablePoints[i];
            availablePoints.RemoveAt(i);

            enemy.transform.position = chosenPoint.position;

            EnemyStateReset reset =
                enemy.GetComponent<EnemyStateReset>();

            if (reset != null)
                reset.ResetToDefaultState();
        }
    }

    // ===========================
    // FRAGMENT SYSTEM
    // ===========================
    public Sprite RequestNextFragment(string itemID)
    {
        LevelBlock lvl = levels[currentLevelIndex];

        FragmentItemData item =
            lvl.fragmentItems.Find(i => i.itemID == itemID);

        if (item == null) return null;
        if (item.revealed >= item.revealOrder.Length)
            return null;

        Sprite next = item.revealOrder[item.revealed];
        item.revealed++;

        return next;
    }

    public void FragmentCollected(string itemID)
    {
        LevelBlock lvl = levels[currentLevelIndex];

        FragmentItemData item =
            lvl.fragmentItems.Find(i => i.itemID == itemID);

        if (item == null) return;

        item.collected++;
        StartCoroutine(CheckItemsComplete());
    }

    private IEnumerator CheckItemsComplete()
    {
        yield return null;

        LevelBlock lvl = levels[currentLevelIndex];
        bool allDone = true;

        foreach (var item in lvl.fragmentItems)
            if (item.collected < item.TotalRequired)
                allDone = false;

        if (allDone)
        {
            OpenDoor(lvl.exitDoor);
            SetActive(lvl.deactivator, true);
            ShowObjective(lvl.secondObjective);
        }
    }

    // ===========================
    // END LEVEL
    // ===========================
    public void EndLevel(int id)
    {
        LevelBlock lvl = levels[id];

        SetActive(lvl.enemies, false);
        SetActive(lvl.coins, false);
        SetActive(lvl.deactivator, false);

        // ⭐ FINAL LEVEL → ESCAPE MODE
        if (id == levels.Count - 1)
        {
            StartEscapeMode();
            return;
        }

        // Normal progression
        LevelBlock nextLvl = levels[id + 1];
        OpenDoor(nextLvl.entranceDoor);
        nextLvl.activator.SetActive(true);
    }

    // ===========================
    // ESCAPE MODE
    // ===========================
    private void StartEscapeMode()
    {
        Debug.Log("ESCAPE MODE STARTED");

        EscapeMode = true;

        // Open ALL doors
        foreach (var lvl in levels)
        {
            OpenDoor(lvl.entranceDoor);
            OpenDoor(lvl.exitDoor);
        }

        // Reactivate ALL enemies
        foreach (var lvl in levels)
        {
            SetActive(lvl.enemies, true);
        }

        // Disable ALL triggers
        foreach (var lvl in levels)
        {
            if (lvl.activator) lvl.activator.SetActive(false);
            if (lvl.deactivator) lvl.deactivator.SetActive(false);
        }

        ShowObjective("ESCAPE!");

        // Notify other systems
        OnEscapeMode?.Invoke();
    }

    // ===========================
    // DOOR HELPERS
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

    // ===========================
    // UTILITIES
    // ===========================
    private void SetActive(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        foreach (var o in arr)
            if (o) o.SetActive(state);
    }

    private void SetActive(GameObject obj, bool state)
    {
        if (obj != null)
            obj.SetActive(state);
    }

    // ===========================
    // UI
    // ===========================
    private void ShowObjective(string msg)
    {
        if (uiRoutine != null)
            StopCoroutine(uiRoutine);

        uiRoutine = StartCoroutine(ObjectiveRoutine(msg));
    }

    private IEnumerator ObjectiveRoutine(string msg)
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
