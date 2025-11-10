using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerCoinCollector : MonoBehaviour
{
    [Header("Coin Settings")]
    public int totalCoins = 0;
    [Tooltip("Text prefix (like 'Coins: ')")]
    public string prefixText = "Coins: ";

    [Header("UI References")]
    public TextMeshProUGUI coinText;

    [Header("Animation Settings")]
    public float scaleUpAmount = 1.2f;
    public float scaleDuration = 0.2f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    public float idleTimeBeforeFade = 3f;

    [Header("Animation Options")]
    public bool animatePrefix = false;
    public bool animateTotalCoins = true;

    // 👇 Add these for the popup PNG
    [Header("First Coin Popup Settings")]
    public CanvasGroup firstCoinPopup; // assign CanvasGroup of PNG + text
    public float popupFadeDuration = 1f;
    public float popupDisplayDuration = 2f;
    public static bool hasShownFirstCoinPopup = false;

    private Vector3 originalScale;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        if (coinText != null)
        {
            originalScale = coinText.transform.localScale;
            UpdateCoinUI();
            SetAlpha(0f); // start hidden
        }
    }

    // Compatibility with CoinCollect
    public void AddCoins(int amount = 1)
    {
        CollectCoins(amount);
    }

    // Main coin collection logic
    public void CollectCoins(int amount = 1)
    {
        bool wasZeroBefore = (totalCoins == 0); // 👈 check if first coin
        totalCoins += amount;
        UpdateCoinUI();

        // 👇 show popup if this is first coin collected
        if (wasZeroBefore && !hasShownFirstCoinPopup)
        {
            hasShownFirstCoinPopup = true;
            if (firstCoinPopup != null)
                StartCoroutine(ShowAndFadePopup());
        }

        // Animate if options are enabled
        if (coinText != null && (animatePrefix || animateTotalCoins))
            StartCoroutine(AnimateCoinText());

        // Fade in & fade out after idle
        if (coinText != null)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeInThenOut());
        }

        Debug.Log("Coins: " + totalCoins);
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = prefixText + totalCoins;
        }
    }

    private IEnumerator AnimateCoinText()
    {
        Vector3 targetScale = originalScale * scaleUpAmount;

        float timer = 0f;
        while (timer < scaleDuration)
        {
            timer += Time.deltaTime;
            coinText.transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / scaleDuration);
            yield return null;
        }

        timer = 0f;
        while (timer < scaleDuration)
        {
            timer += Time.deltaTime;
            coinText.transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / scaleDuration);
            yield return null;
        }

        coinText.transform.localScale = originalScale;
    }

    private IEnumerator FadeInThenOut()
    {
        float timer = 0f;
        Color c = coinText.color;

        // Fade in
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(c.a, 1f, timer / fadeDuration);
            coinText.color = c;
            yield return null;
        }
        c.a = 1f;
        coinText.color = c;

        yield return new WaitForSeconds(idleTimeBeforeFade);

        // Fade out
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            coinText.color = c;
            yield return null;
        }
        c.a = 0f;
        coinText.color = c;
    }

    private void SetAlpha(float a)
    {
        Color c = coinText.color;
        c.a = a;
        coinText.color = c;
    }

    // 👇 Handles first coin popup fade
    private IEnumerator ShowAndFadePopup()
    {
        bool wePausedGame = false;

        // Only pause if not already paused by something else (like pause menu or respawn)
        if (Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            wePausedGame = true;
        }

        firstCoinPopup.gameObject.SetActive(true);

        // Fade in
        float t = 0f;
        while (t < popupFadeDuration)
        {
            t += Time.unscaledDeltaTime; // use unscaled time since we paused the game
            firstCoinPopup.alpha = Mathf.Lerp(0f, 1f, t / popupFadeDuration);
            yield return null;
        }

        firstCoinPopup.alpha = 1f;
        yield return new WaitForSecondsRealtime(popupDisplayDuration);

        // Fade out
        t = 0f;
        while (t < popupFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            firstCoinPopup.alpha = Mathf.Lerp(1f, 0f, t / popupFadeDuration);
            yield return null;
        }

        firstCoinPopup.alpha = 0f;
        firstCoinPopup.gameObject.SetActive(false);

        // Only resume if we were the ones who paused
        if (wePausedGame)
        {
            Time.timeScale = 1f;
        }
    }
}
