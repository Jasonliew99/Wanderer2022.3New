using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The universal script that makes the sprite look at the camera
public class SpriteBillboard : MonoBehaviour
{

    private Transform cameraTransform;

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            cameraTransform = cam.transform;
        }

        // Make the sprite face the same direction as the camera
        transform.rotation = Quaternion.LookRotation(
            -cameraTransform.forward,
            cameraTransform.up
        );

        // Fix sprite default facing (keep this if your art needs it)
        transform.Rotate(0f, 180f, 0f);
    }
    //public Transform cameraTransform; // Assign main camera manually or leave blank

    //void LateUpdate()
    //{
    //    if (cameraTransform == null)
    //    {
    //        if (Camera.main != null)
    //            cameraTransform = Camera.main.transform;
    //        else
    //            return;
    //    }

    //    // Make the sprite face the same direction as the camera (like a real billboard)
    //    transform.rotation = Quaternion.LookRotation(-cameraTransform.forward, cameraTransform.up);

    //    // Fix sprite default facing (if it appears backward)
    //    transform.Rotate(0, 180, 0);
    //}
}
