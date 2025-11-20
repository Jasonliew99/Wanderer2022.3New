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
    public TorchLightDetector torchLightDetector; // optional, drag here

    private float targetAlpha = 0f;
    private Color baseColor;

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        baseColor = spriteRenderer.color;

        SetAlpha(0f); // start hidden

        // Subscribe to modular detector if assigned
        if (torchLightDetector != null)
        {
            torchLightDetector.onEnter += OnDetectorEnter;
            torchLightDetector.onExit += OnDetectorExit;
        }
    }

    void OnDestroy()
    {
        if (torchLightDetector != null)
        {
            torchLightDetector.onEnter -= OnDetectorEnter;
            torchLightDetector.onExit -= OnDetectorExit;
        }
    }

    void Update()
    {
        Color c = spriteRenderer.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
        spriteRenderer.color = c;
    }

    // ITorchlightDetectable interface
    public void OnTorchlightEnter()
    {
        targetAlpha = 1f; // fade in
    }

    public void OnTorchlightExit()
    {
        if (!staysVisibleAfterRevealed)
            targetAlpha = 0f; // fade out
    }

    // helper called by detector
    private void OnDetectorEnter(Collider col)
    {
        if (col == GetComponent<Collider>())
            OnTorchlightEnter();
    }

    private void OnDetectorExit(Collider col)
    {
        if (col == GetComponent<Collider>())
            OnTorchlightExit();
    }

    // Check if item can be collected
    public bool CanBeCollected()
    {
        return spriteRenderer.color.a > 0.1f;
    }

    private void SetAlpha(float alpha)
    {
        Color c = baseColor;
        c.a = alpha;
        spriteRenderer.color = c;
    }
}
