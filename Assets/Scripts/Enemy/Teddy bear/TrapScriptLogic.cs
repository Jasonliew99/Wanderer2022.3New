using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapScriptLogic : MonoBehaviour
{
    [Header("Escape Tuning")]
    public float escapeThreshold = 300f;
    public float shakePowerMultiplier = 0.8f;
    public float maxContributionPerShake = 8f;
    public float decayRate = 25f;

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

    private bool isFading = false;

    // ---------------- SETUP ----------------
    public void Init(TeddyBearController trapper)
    {
        owner = trapper;
        Debug.Log("[Trap] Owner set: " + trapper.name);
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Debug.Log("[Trap] Spawned at: " + transform.position);

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

        if (other.GetComponent<TeddyBearController>() != null)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null)
            return;

        trappingPlayer = true;
        trappedPlayer = player;

        trappedPlayer.TrapImmobilize();

        escapeProgress = 0f;
        lastMouseX = Input.mousePosition.x;
        lastShakeDirection = 0f;

        TrapEscapeUI.Instance.Show();

        // Make fully visible when triggered
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;

            if (triggeredSprite != null)
                spriteRenderer.sprite = triggeredSprite;
        }

        Debug.Log("[Trap] Player trapped → fully visible");
    }

    // ---------------- UPDATE ----------------
    void Update()
    {
        if (!trappingPlayer || trappedPlayer == null)
            return;

        HandleShakeEscape();
    }

    // ---------------- ESCAPE LOGIC ----------------
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
        }

        escapeProgress -= decayRate * Time.deltaTime;
        escapeProgress = Mathf.Clamp(escapeProgress, 0f, escapeThreshold);

        TrapEscapeUI.Instance.SetProgress(escapeProgress / escapeThreshold);

        lastMouseX = currentMouseX;

        if (escapeProgress >= escapeThreshold)
        {
            EscapeTrap();
        }
    }

    // ---------------- ESCAPE SUCCESS ----------------
    void EscapeTrap()
    {
        Debug.Log("[Trap] Player escaped");

        trappingPlayer = false;

        if (trappedPlayer != null)
        {
            trappedPlayer.TrapRelease();
            trappedPlayer = null;
        }

        TrapEscapeUI.Instance.Hide();

        owner?.OnTrapTriggered(transform.position, gameObject);

        // 🔥 Start fade instead of instant destroy
        if (!isFading)
        {
            StartCoroutine(FadeAndDestroy());
        }
    }

    // ---------------- FADE LOGIC ----------------
    IEnumerator FadeAndDestroy()
    {
        isFading = true;

        float time = 0f;
        Color originalColor = spriteRenderer.color;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;

            float alpha = Mathf.Lerp(1f, 0f, t);

            spriteRenderer.color = new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                alpha
            );

            time += Time.deltaTime;
            yield return null;
        }

        // Ensure fully transparent at end
        spriteRenderer.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            0f
        );

        Destroy(gameObject);
    }
}
