using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointGizmo : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
