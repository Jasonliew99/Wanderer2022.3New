using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TorchlightRevealItem : MonoBehaviour
{
    [Header("Reveal Settings")]
    public float fadeSpeed = 5f;
    public bool staysVisibleAfterRevealed = false;

    [Header("References")]
    public SpriteRenderer spriteRenderer;  // assign child sprite here

    private float targetAlpha = 0f;
    private Color baseColor;

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseColor = spriteRenderer.color;

        SetAlpha(0f); // start hidden
    }

    void Update()
    {
        Color c = spriteRenderer.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
        spriteRenderer.color = c;
    }

    /// <summary>Call when torchlight cone touches this item</summary>
    public void OnTorchlightEnter()
    {
        targetAlpha = 1f; // fade in
    }

    /// <summary>Call when torchlight cone exits this item</summary>
    public void OnTorchlightExit()
    {
        if (!staysVisibleAfterRevealed)
            targetAlpha = 0f; // fade out
    }

    /// <summary>Check if this coin can be collected</summary>
    public bool CanBeCollected()
    {
        return spriteRenderer.color.a > 0.1f; // only when visible
    }

    private void SetAlpha(float alpha)
    {
        Color c = baseColor;
        c.a = alpha;
        spriteRenderer.color = c;
    }
}
