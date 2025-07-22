using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastOriginFollower : MonoBehaviour
{
    //basically an empty that follows the camera(used for the xray vision when player is behind something)
    public Transform targetCamera;
    public float followSpeed = 10f;

    void LateUpdate()
    {
        if (targetCamera != null)
        {
            // follows camera
            transform.position = Vector3.Lerp(transform.position, targetCamera.position, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetCamera.rotation, Time.deltaTime * followSpeed);
        }
    }
}
