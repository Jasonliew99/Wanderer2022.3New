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

        // Only detect if torch is ON and has battery
        if (!torchManager.IsTorchOn || torchManager.BatteryPercent <= 0f)
        {
            // Hide all currently revealed items
            foreach (var item in revealedItems)
            {
                if (item != null) item.OnTorchlightExit();
            }
            revealedItems.Clear();
            return;
        }

        // Detect all colliders in range
        Collider[] hits = Physics.OverlapSphere(transform.position, coneRange, detectableLayer);

        // Keep track of items detected this frame
        List<TorchlightRevealItem> currentFrameItems = new List<TorchlightRevealItem>();

        foreach (Collider col in hits)
        {
            TorchlightRevealItem item = col.GetComponent<TorchlightRevealItem>();
            if (item == null) continue;

            bool inCone = IsWithinCone(col.transform.position);

            if (inCone)
            {
                item.OnTorchlightEnter();
                currentFrameItems.Add(item);
            }
            else
            {
                item.OnTorchlightExit();
            }
        }

        // Update revealedItems list
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
