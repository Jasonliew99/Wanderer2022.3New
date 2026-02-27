using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapScriptLogic : MonoBehaviour
{
    [Header("Escape Tuning")]
    public float escapeThreshold = 300f;          // TOTAL effort needed
    public float shakePowerMultiplier = 0.8f;     // How much intensity helps
    public float maxContributionPerShake = 8f;    // HARD CAP per shake
    public float decayRate = 25f;                 // Progress loss per second

    private TeddyBearController owner;
    private PlayerMovement trappedPlayer;

    private bool trappingPlayer;
    private float escapeProgress;

    private float lastMouseX;
    private float lastShakeDirection;

    // ---------------- SETUP ----------------
    public void Init(TeddyBearController trapper)
    {
        owner = trapper;
        Debug.Log("[Trap] Owner set: " + trapper.name);
    }

    void Start()
    {
        Debug.Log("[Trap] Spawned at: " + transform.position);
    }

    // ---------------- TRIGGER ----------------
    private void OnTriggerEnter(Collider other)
    {
        if (trappingPlayer) return;

        // Ignore enemy
        if (other.GetComponent<TeddyBearController>() != null)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null)
            return;

        // Trap player
        trappingPlayer = true;
        trappedPlayer = player;

        trappedPlayer.TrapImmobilize();

        escapeProgress = 0f;
        lastMouseX = Input.mousePosition.x;
        lastShakeDirection = 0f;

        TrapEscapeUI.Instance.Show();

        Debug.Log("[Trap] Player trapped → shake to escape");
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

        // Only count meaningful movement
        if (absDelta > 3f && direction != 0 && direction != lastShakeDirection)
        {
            float contribution = absDelta * shakePowerMultiplier;

            // Clamp so 1 flick can't win
            contribution = Mathf.Min(contribution, maxContributionPerShake);

            escapeProgress += contribution;
            lastShakeDirection = direction;
        }

        // Decay when not shaking
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

        trappedPlayer.TrapRelease();
        trappedPlayer = null;

        TrapEscapeUI.Instance.Hide();

        // Notify enemy that trap was triggered
        owner?.OnTrapTriggered(transform.position, gameObject);

        Destroy(gameObject);
    }
}
