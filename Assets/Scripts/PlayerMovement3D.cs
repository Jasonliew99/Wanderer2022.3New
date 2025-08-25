using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement3D : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Camera Reference")]
    public Transform cameraPivot; // Empty object as pivot for camera rotation

    private Rigidbody rb;
    private Vector3 inputDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector3(h, 0f, v).normalized;
    }

    void MovePlayer()
    {
        if (inputDirection.magnitude == 0) return;

        Vector3 moveDir = Quaternion.Euler(0, cameraPivot.eulerAngles.y, 0) * inputDirection;
        float speed = Input.GetKey(sprintKey) ? sprintSpeed : walkSpeed;

        rb.MovePosition(rb.position + moveDir * speed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 0.15f);
    }
}
