using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastOriginFollower : MonoBehaviour
{
    //basically an empty that follows the camera(used for the xray vision when player is behind something)
    public Transform targetCamera;
    public Vector3 offset = new Vector3(0, 0, 0);
    public float followSpeed = 10f;

    void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 desiredPosition = targetCamera.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
}
