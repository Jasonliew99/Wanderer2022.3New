using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCoinCollector : MonoBehaviour
{
    [Header("Popup Settings")]
    public CanvasGroup fragmentPopupUI;
    public float popupFadeDuration = 0.5f;
    public float popupDisplayDuration = 2f;

    [Header("Fragment Progress UI")]
    public Image currentImage;
    public Image fadeImage;
    public float fragmentFadeDuration = 0.5f;
    public TextMeshProUGUI coinText;

    [Header("Level Controller")]
    public LevelController levelController;

    private string currentPopupItemID;

    private void Start()
    {
        fadeImage.color = new Color(1, 1, 1, 0);
        fragmentPopupUI.alpha = 0f;
        fragmentPopupUI.gameObject.SetActive(false);
    }

    // ==============================
    // SHOW PREVIOUS → FADE → CURRENT
    // ==============================
    public void ShowItemPopup(string itemID)
    {
        currentPopupItemID = itemID;

        LevelController.LevelBlock lvl =
        levelController.levels[levelController.currentLevelIndex];

        var item =
        lvl.fragmentItems.Find(i => i.itemID == itemID);

        if (item == null) return;

        int prev = item.collected;
        int next = prev + 1;

        // SHOW PREVIOUS FIRST
        currentImage.sprite = item.progressSprites[prev];

        coinText.text =
        prev.ToString("00")
        + " / " +
        item.TotalRequired.ToString("00");

        // PREPARE NEXT
        fadeImage.color = new Color(1, 1, 1, 0);
        fadeImage.sprite = item.progressSprites[next];

        StartCoroutine(ShowFragmentProgressPopup());
    }

    IEnumerator ShowFragmentProgressPopup()
    {
        bool wePausedGame = false;

        if (Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            wePausedGame = true;
        }

        fragmentPopupUI.gameObject.SetActive(true);

        float t = 0f;
        while (t < popupFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fragmentPopupUI.alpha = Mathf.Lerp(0f, 1f, t / popupFadeDuration);
            yield return null;
        }

        fragmentPopupUI.alpha = 1f;

        yield return StartCoroutine(CrossFadeFragmentProgress());

        yield return new WaitForSecondsRealtime(popupDisplayDuration);

        t = 0f;
        while (t < popupFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fragmentPopupUI.alpha = Mathf.Lerp(1f, 0f, t / popupFadeDuration);
            yield return null;
        }

        fragmentPopupUI.alpha = 0f;
        fragmentPopupUI.gameObject.SetActive(false);

        if (wePausedGame)
            Time.timeScale = 1f;
    }

    IEnumerator CrossFadeFragmentProgress()
    {
        float t = 0f;

        fadeImage.color = new Color(1, 1, 1, 0);

        while (t < fragmentFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / fragmentFadeDuration);
            fadeImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        fadeImage.color = new Color(1, 1, 1, 1);

        // UPDATE CURRENT IMAGE
        currentImage.sprite = fadeImage.sprite;

        // NOW update number AFTER fade
        LevelController.LevelBlock lvl =
        levelController.levels[levelController.currentLevelIndex];

        var item =
        lvl.fragmentItems.Find(i => i.itemID == currentPopupItemID);

        coinText.text =
        item.collected.ToString("00")
        + " / " +
        item.TotalRequired.ToString("00");

        fadeImage.color = new Color(1, 1, 1, 0);
    }
}
