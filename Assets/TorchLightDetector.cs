using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchLightDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public LayerMask detectableLayer; // Layer for statues
    public bool visualizeGizmo = true;

    [Header("Cone Settings")]
    public float coneAngle = 30f; // full angle of vision cone
    public float coneRange = 10f; // distance of cone

    private SphereCollider detectionCollider;

    void Awake()
    {
        detectionCollider = GetComponent<SphereCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = coneRange;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & detectableLayer) != 0)
        {
            if (IsWithinCone(other.transform.position))
            {
                FishStatueController statue = other.GetComponent<FishStatueController>();
                if (statue != null)
                    statue.OnTorchlightEnter();
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & detectableLayer) != 0)
        {
            if (IsWithinCone(other.transform.position))
            {
                FishStatueController statue = other.GetComponent<FishStatueController>();
                if (statue != null)
                    statue.OnTorchlightEnter();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & detectableLayer) != 0)
        {
            FishStatueController statue = other.GetComponent<FishStatueController>();
            if (statue != null)
                statue.OnTorchlightExit();
        }
    }

    private bool IsWithinCone(Vector3 targetPos)
    {
        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToTarget);
        float distance = Vector3.Distance(transform.position, targetPos);

        return angle <= coneAngle * 0.5f && distance <= coneRange;
    }

    void OnDrawGizmos()
    {
        if (!visualizeGizmo) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // semi-transparent yellow
        Vector3 forward = transform.forward * coneRange;

        // Draw cone edges
        Vector3 leftDir = Quaternion.Euler(0, -coneAngle * 0.5f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, coneAngle * 0.5f, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftDir);
        Gizmos.DrawLine(transform.position, transform.position + rightDir);

        // Optionally draw simple arc with lines
        int segments = 10;
        for (int i = 0; i <= segments; i++)
        {
            float lerpAngle = Mathf.Lerp(-coneAngle * 0.5f, coneAngle * 0.5f, i / (float)segments);
            Vector3 dir = Quaternion.Euler(0, lerpAngle, 0) * forward;
            Gizmos.DrawLine(transform.position, transform.position + dir);
        }
    }
}
