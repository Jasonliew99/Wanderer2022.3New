using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XrayRaycaster : MonoBehaviour
{
    public Transform player;
    public LayerMask obstructionMask;
    public FadeObsticle faderScript;

    void Update()
    {
        if (player == null || faderScript == null) return;

        // Cast ray from camera to player
        Vector3 dir = player.position - transform.position;
        float distance = dir.magnitude;

        // Collect all objects currently blocking view  
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir.normalized, distance, obstructionMask);
        List<Renderer> hitRenderers = new List<Renderer>();

        foreach (var hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
                hitRenderers.Add(rend);
        }

        faderScript.HandleObstructions(hitRenderers);
    }

    //a line to player from camera visible
    void OnDrawGizmos()
    {
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(player.position, 0.1f);
        }
    }
}
