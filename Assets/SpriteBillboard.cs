using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteBillboard : MonoBehaviour
{
    public Transform cameraTransform; // Assign main camera manually or leave blank

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                return;
        }

        // Make the sprite face the same direction as the camera (like a real billboard)
        transform.rotation = Quaternion.LookRotation(-cameraTransform.forward, cameraTransform.up);

        // Fix sprite default facing (if it appears backward)
        transform.Rotate(0, 180, 0);
    }
}
