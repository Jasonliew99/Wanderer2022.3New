using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapPlacementZone : MonoBehaviour
{
    public float minSpacing = 0.8f; // minimum distance between traps
    private List<Vector3> usedPoints = new();

    private BoxCollider box;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    public bool TryGetRandomPoint(out Vector3 point, int attempts = 10)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 randomPoint = GetRandomPointInside();

            bool tooClose = false;
            foreach (Vector3 used in usedPoints)
            {
                if (Vector3.Distance(randomPoint, used) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                usedPoints.Add(randomPoint);
                point = randomPoint;
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }

    Vector3 GetRandomPointInside()
    {
        Vector3 local = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0f,
            Random.Range(-0.5f, 0.5f)
        );

        Vector3 scaled = Vector3.Scale(local, box.size);
        Vector3 world = transform.TransformPoint(box.center + scaled);
        world.y = transform.position.y;

        return world;
    }

    void OnDrawGizmosSelected()
    {
        if (box == null)
            box = GetComponent<BoxCollider>();

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
