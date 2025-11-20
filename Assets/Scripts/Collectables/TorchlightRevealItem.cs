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

        SetAlpha(0f); // start hidden or transparent
    }

    void Update()
    {
        Color c = spriteRenderer.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
        spriteRenderer.color = c;
    }

    // when torchlight cone touches this object
    public void OnTorchlightEnter()
    {
        targetAlpha = 1f; // fade in
    }

    // when torchlight cone touches this object
    public void OnTorchlightExit()
    {
        if (!staysVisibleAfterRevealed)
            targetAlpha = 0f; // fade out
    }

    // Check if this coin can be collected
    public bool CanBeCollected()
    {
        return spriteRenderer.color.a > 0.1f; // only when visible, means if not visible cannot be collected
    }

    private void SetAlpha(float alpha)
    {
        Color c = baseColor;
        c.a = alpha;
        spriteRenderer.color = c;
    }
}
