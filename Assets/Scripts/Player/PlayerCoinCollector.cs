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
    public float scaleUpAmount = 1.2f;      // pop effect
    public float scaleDuration = 0.2f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;       // fade speed
    public float idleTimeBeforeFade = 3f;   // time before fading out

    [Header("Animation Options")]
    public bool animatePrefix = false;      // animate prefix text
    public bool animateTotalCoins = true;   // animate total coin number

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
        totalCoins += amount;
        UpdateCoinUI();

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
            if (animatePrefix && animateTotalCoins)
            {
                coinText.text = prefixText + totalCoins;
            }
            else if (animatePrefix)
            {
                // Only animate prefix visually
                coinText.text = prefixText + totalCoins;
            }
            else if (animateTotalCoins)
            {
                // Only animate total number visually
                coinText.text = prefixText + totalCoins;
            }
            else
            {
                // No animation, just update text
                coinText.text = prefixText + totalCoins;
            }
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
}
