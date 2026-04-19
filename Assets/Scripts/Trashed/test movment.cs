//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class testmovment : MonoBehaviour
//{
//    private Rigidbody rb;
//    private float moveH, moveV;
//    [SerializeField] private float moveSpeed = 5f;

//    private void Awake()
//    {
//        rb = GetComponent<Rigidbody>();
//    }

//    private void FixedUpdate()
//    {
//        moveH = Input.GetAxis("Horizontal") * moveSpeed;
//        moveV = Input.GetAxis("Vertical") * moveSpeed;
//        rb.velocity = new Vector3(moveH, moveV);
//    }
//}
