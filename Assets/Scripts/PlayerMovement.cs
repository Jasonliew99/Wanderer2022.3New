using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float MoveSpeed = 10f;

    private Rigidbody rb;
    private Vector3 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float MovementX = Input.GetAxisRaw("Horizontal");
        float MovementZ = Input.GetAxisRaw("Vertical");

        movement = new Vector3(MovementX, 0f, MovementZ).normalized;
    }

    void FixedUpdate()
    {
        Vector3 velocity = movement * MoveSpeed;

        rb.velocity = velocity;
    }
}