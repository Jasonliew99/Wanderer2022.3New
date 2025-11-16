using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraForObjectToFade : MonoBehaviour
{
    public GameObject player;
    private FaderForObjects _fader;

    public Color rayColor = Color.red; // Color of the gizmo ray
    public float maxRayDistance = 100f; // Optional max distance

    void Update()
    {
        if (player == null) return;

        Vector3 dir = player.transform.position - transform.position;
        Ray ray = new Ray(transform.position, dir);
        RaycastHit hit;

        // Draw the ray for visualization
        Debug.DrawRay(transform.position, dir.normalized * maxRayDistance, rayColor);

        if (Physics.Raycast(ray, out hit, maxRayDistance))
        {
            if (hit.collider == null) return;

            if (hit.collider.gameObject == player)
            {
                // nothing blocking player
                if (_fader != null)
                    _fader.DoFade = false;
            }
            else
            {
                _fader = hit.collider.GetComponent<FaderForObjects>();
                if (_fader != null)
                    _fader.DoFade = true;
            }
        }
    }
}
