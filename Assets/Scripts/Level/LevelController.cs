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

    [Header("Escape Lighting")]
    public List<Light> lightsToChange = new List<Light>(); // Drag lights here in Inspector
    public Color escapeColor = Color.red;
    public float escapeIntensity = 1.5f;

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
                if (activeID == 0)
                {
                    CloseDoor(lvl.entranceDoor);
                }
                else
                {
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
        if (EscapeMode)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                ResetSpecificLevelEnemies(i);
            }
        }
        else
        {
            if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count) return;
            ResetSpecificLevelEnemies(currentLevelIndex);
        }
    }

    private void ResetSpecificLevelEnemies(int id)
    {
        LevelBlock lvl = levels[id];
        if (lvl.enemies == null || lvl.enemies.Length == 0) return;

        List<Transform> availablePoints = new List<Transform>(lvl.enemyResetPoints);
        for (int i = 0; i < availablePoints.Count; i++)
        {
            Transform temp = availablePoints[i];
            int randomIndex = Random.Range(i, availablePoints.Count);
            availablePoints[i] = availablePoints[randomIndex];
            availablePoints[randomIndex] = temp;
        }

        for (int i = 0; i < lvl.enemies.Length; i++)
        {
            GameObject enemy = lvl.enemies[i];
            if (enemy == null) continue;

            int pointIndex = i % availablePoints.Count;
            enemy.transform.position = availablePoints[pointIndex].position;
            enemy.transform.rotation = availablePoints[pointIndex].rotation;

            EnemyStateReset resetScript = enemy.GetComponent<EnemyStateReset>();
            if (resetScript != null)
            {
                resetScript.ResetToDefaultState();
            }
            else
            {
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
            OpenDoor(lvl.exitDoor);

            if (currentLevelIndex == levels.Count - 1)
            {
                StartEscapeMode();
            }
            else
            {
                SetActive(lvl.deactivator, true);
            }
        }
    }

    public void EndLevel(int id)
    {
        if (EscapeMode && id == levels.Count - 1)
        {
            WinGame();
            return;
        }

        if (EscapeMode) return;
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

        // --- LIGHTING CHANGE ---
        ChangeManualLights(escapeColor, escapeIntensity);

        if (respawnController != null)
            respawnController.ResetLivesToFull();

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
                if (lvl.deactivator) lvl.deactivator.SetActive(false);
            }

            if (lvl.activator) lvl.activator.SetActive(false);
        }

        if (levels[levels.Count - 1].deactivator)
            levels[levels.Count - 1].deactivator.SetActive(true);

        if (uiRoutine != null) StopCoroutine(uiRoutine);
        uiRoutine = StartCoroutine(SequenceRoutine(escapeDialogues));
        OnEscapeMode?.Invoke();
    }

    private void ChangeManualLights(Color targetColor, float targetIntensity)
    {
        foreach (Light l in lightsToChange)
        {
            if (l != null)
            {
                l.color = targetColor;
                l.intensity = targetIntensity;
            }
        }
    }

    private void WinGame()
    {
        if (uiRoutine != null) StopCoroutine(uiRoutine);

        // --- RESET LIGHTS TO NORMAL ---
        ChangeManualLights(Color.white, 1.0f);

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
        objectiveText.gameObject.SetActive(true);
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
