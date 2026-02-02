using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapScriptLogic : MonoBehaviour
{
    public float immobilizeDuration = 0.8f;

    private TeddyBearController owner;
    private bool triggered;

    void Start()
    {
        Debug.Log("[Trap] Spawned and alive at: " + transform.position);
        Invoke(nameof(DebugStillAlive), 2f);
    }

    void DebugStillAlive()
    {
        Debug.Log("[Trap] Still alive after 2 seconds");
    }

    public void Init(TeddyBearController trapper)
    {
        owner = trapper;
        Debug.Log("[Trap] Owner set: " + trapper.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // IGNORE ENEMY
        if (other.GetComponent<TeddyBearController>() != null)
            return;

        if (!other.CompareTag("Player"))
            return;

        triggered = true;

        Debug.Log("[Trap] Triggered by PLAYER");

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.StartCoroutine(player.Immobilize(immobilizeDuration));
        }

        owner?.OnTrapTriggered(transform.position, gameObject);

        // TEMP: comment this out if needed
        Destroy(gameObject);
    }
}
