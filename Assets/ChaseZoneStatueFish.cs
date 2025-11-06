using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseZoneStatueFish : MonoBehaviour
{
    [Header("Chase Zone Settings")]
    public float radius = 10f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    // Check if a position is inside this zone
    public bool IsInside(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= radius;
    }
}
