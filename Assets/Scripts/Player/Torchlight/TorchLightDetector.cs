using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchLightDetector : MonoBehaviour
{
    [Header("Torch Reference")]
    public TorchlightManager torchManager;

    [Header("Detection Settings")]
    public LayerMask detectableLayers;
    public float coneAngle = 30f;
    public float coneRange = 10f;
    public bool showGizmos = true;

    [Header("Events")]
    public System.Action<Collider> onEnter;
    public System.Action<Collider> onStay;
    public System.Action<Collider> onExit;

    private List<Collider> objectsInside = new List<Collider>();

    void Update()
    {
        // ===============================
        // Torch OFF = detector disabled
        // ===============================
        if (torchManager == null || !torchManager.IsTorchOn || torchManager.BatteryPercent <= 0f)
        {
            ClearAllObjects();
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, coneRange, detectableLayers);
        List<Collider> currentFrame = new List<Collider>();

        foreach (Collider col in hits)
        {
            if (IsWithinCone(col.transform.position))
            {
                currentFrame.Add(col);

                if (!objectsInside.Contains(col))
                {
                    objectsInside.Add(col);
                    onEnter?.Invoke(col);
                }

                onStay?.Invoke(col);
            }
        }

        for (int i = objectsInside.Count - 1; i >= 0; i--)
        {
            if (!currentFrame.Contains(objectsInside[i]))
            {
                onExit?.Invoke(objectsInside[i]);
                objectsInside.RemoveAt(i);
            }
        }
    }

    void ClearAllObjects()
    {
        for (int i = objectsInside.Count - 1; i >= 0; i--)
        {
            onExit?.Invoke(objectsInside[i]);
        }
        objectsInside.Clear();
    }

    private bool IsWithinCone(Vector3 point)
    {
        Vector3 dir = (point - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        float distance = Vector3.Distance(transform.position, point);
        return angle <= coneAngle * 0.5f && distance <= coneRange;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // ============================
        // GIZMO COLOR BASED ON TORCH
        // ============================
        if (torchManager == null)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.25f); // grey
        }
        else if (torchManager.IsTorchOn && torchManager.BatteryPercent > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // yellow
        }
        else
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f); // red
        }

        Vector3 forward = transform.forward * coneRange;
        Vector3 leftDir = Quaternion.Euler(0, -coneAngle * 0.5f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, coneAngle * 0.5f, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftDir);
        Gizmos.DrawLine(transform.position, transform.position + rightDir);

        int segments = 20;
        Vector3 prev = transform.position + leftDir;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angleStep = -coneAngle * 0.5f + t * coneAngle;
            Vector3 next = transform.position +
                Quaternion.Euler(0, angleStep, 0) * (transform.forward * coneRange);

            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        Gizmos.DrawWireSphere(transform.position, coneRange);
    }
#endif
}
