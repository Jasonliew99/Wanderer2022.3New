using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchLight3D : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public Transform flashlight;
    public float flashlightDistance = 1f;
    public float flashlightHeight = 0.5f;
    public float flashlightTurnSpeed = 10f;
    public float playerTurnSpeed = 5f;
    public float maxAngleFromForward = 45f; // soft rotation radius

    [Header("Aim Mode")]
    public KeyCode aimKey = KeyCode.Mouse1;
    public float aimZoomFOV = 40f;
    public float normalFOV = 60f;
    public float fovLerpSpeed = 5f;
    public float aimMoveSpeedMultiplier = 0.5f; // accessed by PlayerMovement3D

    [Header("Flicker Settings")]
    public bool enableFlicker = true;
    [Range(0f, 1f)] public float flickerChancePerSecond = 0.2f;
    public float flickerSpeed = 0.05f;
    public int minFlickers = 1;
    public int maxFlickers = 4;

    [Header("References")]
    public Transform player;
    public Camera mainCamera;
    public Light flashlightLight; // spotlight or point light

    [Header("Debug")]
    public bool showGizmoRadius = true;
    public Color gizmoColor = Color.cyan;

    private bool isFlickering = false;
    private Vector3 lastMouseWorldPos;
    private bool isAiming = false;

    public bool IsAiming => isAiming; // Public access for movement

    void Update()
    {
        if (flashlight == null || player == null || mainCamera == null)
            return;

        isAiming = Input.GetKey(aimKey);

        HandleFlashlightRotation();
        HandleCameraZoom();

        if (enableFlicker && flashlightLight != null && !isFlickering)
        {
            if (Random.value < flickerChancePerSecond * Time.deltaTime)
                StartCoroutine(FlickerFlashlight());
        }
    }

    void HandleFlashlightRotation()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, player.position + Vector3.up * flashlightHeight);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 aimDirection = (hitPoint - player.position).normalized;
            Vector3 playerForward = player.forward;

            // Clamp to max angle (soft radius)
            float angle = Vector3.Angle(playerForward, aimDirection);

            if (angle > maxAngleFromForward && !isAiming)
            {
                Vector3 limitedDir = Vector3.RotateTowards(playerForward, aimDirection, Mathf.Deg2Rad * maxAngleFromForward, 0f);
                aimDirection = limitedDir.normalized;
            }

            // Position flashlight
            Vector3 flashlightTargetPos = player.position + aimDirection * flashlightDistance + Vector3.up * flashlightHeight;
            flashlight.position = Vector3.Lerp(flashlight.position, flashlightTargetPos, Time.deltaTime * flashlightTurnSpeed);

            // Rotate flashlight
            Quaternion targetRot = Quaternion.LookRotation(aimDirection);
            flashlight.rotation = Quaternion.Slerp(flashlight.rotation, targetRot, Time.deltaTime * flashlightTurnSpeed);

            // If angle exceeds max, rotate player slowly
            if (angle > maxAngleFromForward)
            {
                Quaternion playerTargetRot = Quaternion.LookRotation(aimDirection);
                player.rotation = Quaternion.Slerp(player.rotation, playerTargetRot, Time.deltaTime * playerTurnSpeed);
            }
        }
    }

    void HandleCameraZoom()
    {
        if (mainCamera == null) return;

        float targetFOV = isAiming ? aimZoomFOV : normalFOV;
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);

        if (flashlightLight != null)
        {
            flashlightLight.spotAngle = Mathf.Lerp(flashlightLight.spotAngle, isAiming ? 25f : 50f, Time.deltaTime * 8f);
        }
    }

    IEnumerator FlickerFlashlight()
    {
        isFlickering = true;
        int flicks = Random.Range(minFlickers, maxFlickers + 1);

        for (int i = 0; i < flicks; i++)
        {
            if (flashlightLight != null) flashlightLight.enabled = false;
            yield return new WaitForSeconds(flickerSpeed);
            if (flashlightLight != null) flashlightLight.enabled = true;
            yield return new WaitForSeconds(flickerSpeed);
        }

        isFlickering = false;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmoRadius || player == null) return;

        Gizmos.color = gizmoColor;
        Vector3 forward = player.forward;
        Vector3 origin = player.position + Vector3.up * flashlightHeight;
        Quaternion leftLimit = Quaternion.AngleAxis(-maxAngleFromForward, Vector3.up);
        Quaternion rightLimit = Quaternion.AngleAxis(maxAngleFromForward, Vector3.up);

        Vector3 leftDir = leftLimit * forward * flashlightDistance;
        Vector3 rightDir = rightLimit * forward * flashlightDistance;

        Gizmos.DrawLine(origin, origin + leftDir);
        Gizmos.DrawLine(origin, origin + rightDir);
        Gizmos.DrawWireSphere(origin, flashlightDistance);
    }
}
