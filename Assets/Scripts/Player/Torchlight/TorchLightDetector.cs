using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchLightDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public LayerMask detectableLayer;
    public float coneAngle = 30f;
    public float coneRange = 10f;
    public bool visualizeGizmo = true;

    [Header("References")]
    public TorchlightManager torchManager; // reference to your torch manager

    private List<TorchlightRevealItem> revealedItems = new List<TorchlightRevealItem>();

    void Update()
    {
        if (torchManager == null)
            return;

        // Torch is OFF or battery empty to exit everything
        if (!torchManager.IsTorchOn || torchManager.BatteryPercent <= 0f)
        {
            foreach (var item in revealedItems)
            {
                if (item != null)
                    item.OnTorchlightExit();
            }

            revealedItems.Clear();
            return;
        }

        // Detect colliders within range
        Collider[] hits = Physics.OverlapSphere(transform.position, coneRange, detectableLayer);

        // Track items hit and inside cone THIS frame
        List<TorchlightRevealItem> currentFrameItems = new List<TorchlightRevealItem>();

        foreach (Collider col in hits)
        {
            TorchlightRevealItem item = col.GetComponent<TorchlightRevealItem>();
            if (item == null) continue;

            // Check cone
            bool inCone = IsWithinCone(col.transform.position);

            if (inCone)
            {
                currentFrameItems.Add(item);

                // Only call enter if it was NOT revealed last frame
                if (!revealedItems.Contains(item))
                    item.OnTorchlightEnter();
            }
        }

        // Handle items that were revealed last frame but not this frame
        foreach (var oldItem in revealedItems)
        {
            if (!currentFrameItems.Contains(oldItem))
                oldItem.OnTorchlightExit();
        }

        revealedItems = currentFrameItems;
    }

    private bool IsWithinCone(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        float distance = Vector3.Distance(transform.position, target);

        return angle <= coneAngle * 0.5f && distance <= coneRange;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!visualizeGizmo) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Vector3 forward = transform.forward * coneRange;
        Vector3 leftDir = Quaternion.Euler(0, -coneAngle * 0.5f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, coneAngle * 0.5f, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftDir);
        Gizmos.DrawLine(transform.position, transform.position + rightDir);

        // Optional: draw sphere for range
        Gizmos.DrawWireSphere(transform.position, coneRange);
    }
#endif
}
