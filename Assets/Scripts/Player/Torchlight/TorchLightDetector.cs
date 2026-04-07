using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchLightDetector : MonoBehaviour
{
    [Header("Torch Reference")]
    public TorchlightManager torchManager;

    [Header("Detection Settings")]
    public LayerMask detectableLayers;
    // These are now modified by the TorchlightManager in real-time
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
        if (torchManager == null || !torchManager.IsTorchOn || torchManager.BatteryPercent <= 0f)
        {
            ClearAllObjects();
            return;
        }

        // Uses the dynamic coneRange and coneAngle scaled by battery
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

        // Color based on state
        if (torchManager == null) Gizmos.color = Color.gray;
        else if (torchManager.IsTorchOn && torchManager.BatteryPercent > 0f) Gizmos.color = new Color(1f, 0.9f, 0f, 0.4f);
        else Gizmos.color = new Color(1f, 0f, 0f, 0.2f);

        // Draw the Arc on the horizontal plane
        Vector3 pos = transform.position;
        Vector3 forward = transform.forward;

        // Draw the Range Sphere
        Gizmos.DrawWireSphere(pos, coneRange);

        // Draw the Cone Edges (4 lines to show volume)
        float halfAngle = coneAngle * 0.5f;
        Quaternion upRay = Quaternion.AngleAxis(-halfAngle, transform.right);
        Quaternion downRay = Quaternion.AngleAxis(halfAngle, transform.right);
        Quaternion leftRay = Quaternion.AngleAxis(-halfAngle, transform.up);
        Quaternion rightRay = Quaternion.AngleAxis(halfAngle, transform.up);

        Gizmos.DrawLine(pos, pos + (leftRay * forward * coneRange));
        Gizmos.DrawLine(pos, pos + (rightRay * forward * coneRange));
        Gizmos.DrawLine(pos, pos + (upRay * forward * coneRange));
        Gizmos.DrawLine(pos, pos + (downRay * forward * coneRange));

        // Draw the End Cap Circle
        int segments = 20;
        Vector3 previousPoint = pos + (leftRay * forward * coneRange);
        for (int i = 1; i <= segments; i++)
        {
            float angle = -halfAngle + (coneAngle / segments) * i;
            Vector3 nextPoint = pos + (Quaternion.AngleAxis(angle, transform.up) * forward * coneRange);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
#endif
}
