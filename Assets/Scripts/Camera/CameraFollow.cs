using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    //projection: orthographic
    //size: 17
    //clipping: near = -30 far = 10000

    public Transform target;
    public Vector3 offset = new Vector3(0, 10, -5);
    public float followSpeed = 5f;

    // How far ahead the camera looks in the movement direction
    public float leadDistance = 3f;
    // How quickly the lead adjusts to player movement
    public float leadSmooth = 5f;

    private Vector3 currentLeadOffset = Vector3.zero;
    private Vector3 lastTargetPosition;

    void Start()
    {
        if (target != null)
            lastTargetPosition = target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate player movement direction
        Vector3 movement = target.position - lastTargetPosition;
        lastTargetPosition = target.position;

        // Smoothly update the lead offset based on movement direction
        Vector3 targetLead = movement.normalized * leadDistance;
        currentLeadOffset = Vector3.Lerp(currentLeadOffset, targetLead, leadSmooth * Time.deltaTime);

        // Desired camera position: base offset + lead offset
        Vector3 desiredPosition = target.position + offset + currentLeadOffset;

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
}
