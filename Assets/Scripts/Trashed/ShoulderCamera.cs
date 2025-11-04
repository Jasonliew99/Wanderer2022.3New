using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShoulderCamera : MonoBehaviour
{
    [Header("Target & View Settings")]
    public Transform target;                     // Player to follow
    public Vector3 pivotOffset = new Vector3(0, 1.5f, 0); // Offset to follow player chest/head

    [Header("Camera Rotation")]
    public float mouseSensitivityX = 120f;
    public float mouseSensitivityY = 80f;
    public Vector2 pitchClamp = new Vector2(-30f, 45f);

    [Header("Camera Distance")]
    public float defaultDistance = 4f;
    public float minDistance = 1f;
    public float smoothSpeed = 10f;
    public float sphereRadius = 0.3f;

    [Header("Collision")]
    public LayerMask collisionMask;

    [Header("Zoom Control")]
    public float cameraZoomMultiplier = 1f; // 1 = normal, <1 = zoom in (e.g. 0.85f)

    private float yaw;
    private float pitch;
    private float currentDistance;

    private Transform cam;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cam = Camera.main.transform;
        currentDistance = defaultDistance;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Rotate based on mouse input
        yaw += Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchClamp.x, pitchClamp.y);

        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivotPoint = target.position + pivotOffset;

        // Desired camera position with zoom multiplier
        float zoomedDistance = defaultDistance * cameraZoomMultiplier;
        Vector3 desiredCameraPos = pivotPoint - camRotation * Vector3.forward * zoomedDistance;

        // Raycast to handle wall clipping
        float targetDistance = zoomedDistance;
        if (Physics.SphereCast(pivotPoint, sphereRadius, (desiredCameraPos - pivotPoint).normalized, out RaycastHit hit, zoomedDistance, collisionMask))
        {
            targetDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, zoomedDistance);
        }

        // Smooth camera zoom
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * smoothSpeed);

        // Apply camera position and rotation
        cam.position = pivotPoint - camRotation * Vector3.forward * currentDistance;
        cam.rotation = camRotation;
    }
}
