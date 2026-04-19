using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class TrapScriptLogic : MonoBehaviour
{
    [Header("Escape Tuning")]
    public float escapeThreshold = 300f;
    public float shakePowerMultiplier = 0.8f;
    public float maxContributionPerShake = 8f;
    public float decayRate = 25f;

    [Header("Audio")]
    public AudioSource trapAudioSource;
    public AudioClip trapTriggerSound;
    public AudioClip wiggleLoopSound;
    public AudioClip escapeSound;
    public float wiggleFadeSpeed = 5f;

    [Header("Visibility Settings")]
    [Range(0f, 1f)] public float idleAlpha = 0.3f;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public Sprite idleSprite;
    public Sprite triggeredSprite;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    private TeddyBearController owner;
    private PlayerMovement trappedPlayer;

    private bool trappingPlayer;
    private float escapeProgress;

    private float lastMouseX;
    private float lastShakeDirection;
    private float targetWiggleVolume = 0f;
    private bool isFading = false;

    // ---------------- SETUP ----------------
    public void Init(TeddyBearController trapper)
    {
        owner = trapper;
    }

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (trapAudioSource == null) trapAudioSource = GetComponent<AudioSource>();

        if (trapAudioSource != null)
        {
            trapAudioSource.loop = true;
            trapAudioSource.playOnAwake = false;
            trapAudioSource.volume = 0;
        }
    }

    void Start()
    {
        if (spriteRenderer != null && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
            Color c = spriteRenderer.color;
            c.a = idleAlpha;
            spriteRenderer.color = c;
        }
    }

    // ---------------- TRIGGER ----------------
    private void OnTriggerEnter(Collider other)
    {
        if (trappingPlayer || isFading) return;

        // Ignore the enemy if they walk over it
        if (other.GetComponent<TeddyBearController>() != null) return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        trappingPlayer = true;
        trappedPlayer = player;
        trappedPlayer.TrapImmobilize();

        if (trapAudioSource != null && trapTriggerSound != null)
            trapAudioSource.PlayOneShot(trapTriggerSound);

        if (trapAudioSource != null && wiggleLoopSound != null)
        {
            trapAudioSource.clip = wiggleLoopSound;
            trapAudioSource.volume = 0;
            trapAudioSource.Play();
        }

        escapeProgress = 0f;
        lastMouseX = Input.mousePosition.x;
        lastShakeDirection = 0f;

        // UI check
        if (TrapEscapeUI.Instance != null) TrapEscapeUI.Instance.Show();

        // VISUALS check
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
            if (triggeredSprite != null)
                spriteRenderer.sprite = triggeredSprite;
        }
    }

    void Update()
    {
        if (!trappingPlayer || trappedPlayer == null)
            return;

        HandleShakeEscape();
        HandleDynamicAudio();
    }

    void HandleDynamicAudio()
    {
        if (trapAudioSource == null || trapAudioSource.clip == null) return;

        trapAudioSource.volume = Mathf.MoveTowards(trapAudioSource.volume, targetWiggleVolume, wiggleFadeSpeed * Time.deltaTime);
        targetWiggleVolume = Mathf.MoveTowards(targetWiggleVolume, 0f, wiggleFadeSpeed * Time.deltaTime);
    }

    void HandleShakeEscape()
    {
        float currentMouseX = Input.mousePosition.x;
        float delta = currentMouseX - lastMouseX;

        float direction = Mathf.Sign(delta);
        float absDelta = Mathf.Abs(delta);

        if (absDelta > 3f && direction != 0 && direction != lastShakeDirection)
        {
            float contribution = absDelta * shakePowerMultiplier;
            contribution = Mathf.Min(contribution, maxContributionPerShake);

            escapeProgress += contribution;
            lastShakeDirection = direction;

            targetWiggleVolume = 1.0f;
        }

        escapeProgress -= decayRate * Time.deltaTime;
        escapeProgress = Mathf.Clamp(escapeProgress, 0f, escapeThreshold);

        if (TrapEscapeUI.Instance != null)
            TrapEscapeUI.Instance.SetProgress(escapeProgress / escapeThreshold);

        lastMouseX = currentMouseX;

        if (escapeProgress >= escapeThreshold)
        {
            EscapeTrap();
        }
    }

    void EscapeTrap()
    {
        trappingPlayer = false;
        targetWiggleVolume = 0;

        if (trapAudioSource != null)
        {
            trapAudioSource.Stop();
            if (escapeSound != null)
                trapAudioSource.PlayOneShot(escapeSound);
        }

        if (trappedPlayer != null)
        {
            trappedPlayer.TrapRelease();
            trappedPlayer = null;
        }

        if (TrapEscapeUI.Instance != null) TrapEscapeUI.Instance.Hide();
        owner?.OnTrapTriggered(transform.position, gameObject);

        if (!isFading) StartCoroutine(FadeAndDestroy());
    }

    public void ForceRelease()
    {
        trappingPlayer = false;
        if (trapAudioSource != null) trapAudioSource.Stop();

        if (trappedPlayer != null)
        {
            trappedPlayer.TrapRelease();
            trappedPlayer = null;
        }

        if (TrapEscapeUI.Instance != null) TrapEscapeUI.Instance.Hide();

        Destroy(gameObject);
    }

    IEnumerator FadeAndDestroy()
    {
        isFading = true;
        float time = 0f;

        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Color originalColor = spriteRenderer.color;
        while (time < fadeDuration)
        {
            if (spriteRenderer == null) break;

            float t = time / fadeDuration;
            float alpha = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            time += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
