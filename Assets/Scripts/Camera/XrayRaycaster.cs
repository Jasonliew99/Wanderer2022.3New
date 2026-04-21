using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XrayRaycaster : MonoBehaviour
{
    public Transform player;
    public LayerMask obstacleMask;

    private List<FadeObsticle> currentFadedObjects = new List<FadeObsticle>();

    void Update()
    {
        HandleObstacles();
    }

    void HandleObstacles()
    {
        foreach (var fader in currentFadedObjects)
            fader.SetFaded(false);
        currentFadedObjects.Clear();

        Vector3 direction = player.position - transform.position;
        float distance = Vector3.Distance(player.position, transform.position);

        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, distance, obstacleMask);

        foreach (var hit in hits)
        {
            FadeObsticle fader = hit.collider.GetComponentInParent<FadeObsticle>();
            if (fader != null)
            {
                fader.SetFaded(true);
                if (!currentFadedObjects.Contains(fader))
                    currentFadedObjects.Add(fader);
            }
        }
    }
    void OnDrawGizmos()
    {
        if (player == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, player.position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(player.position, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.1f);


    }
}
