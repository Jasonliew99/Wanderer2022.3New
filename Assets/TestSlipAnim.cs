//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class TestSlipAnim : MonoBehaviour
//{
//    private Animator anim;

//    public string[] staticDirections = { "Static N", "Static NW", "Static W", "Static SW", "Static S", "Static SE", "Static E", "Static NE" };
//    public string[] runDirections = { "Run N", "Run NW", "Run W", "Run SW", "Run S", "Run SE", "Run E", "Run NE" };

//    int lastDirection;

//    private void Awake()
//    {
//        anim = GetComponent<Animator>();

//        float result1 = Vector3.SignedAngle(Vector3.up, Vector3.right);
//        Debug.Log("R1" + result1);

//        float result2 = Vector3.SignedAngle(Vector3.up, Vector3.left);
//        Debug.Log("R2" + result2);

//        float result3 = Vector3.SignedAngle(Vector3.up, Vector3.down);
//        Debug.Log("R3" + result3);
//    }

//    //marker: each direction will match with one strign element
//    //marker: we used direction to determine theri animation

//    public void SetDirection(Vector3 direction)
//    {
//        string[] directionArray = null;

//        if(_direction.magnitide < 0.01)//marker charccater is static and his velocity is close to zero
//        {
//            directionArray = staticDirections;
//        }
//        else
//        {
//            directionArray = runDirections;
//            lastDirection = DirectionToIndex(_direction); //marker: get the index of the slice from the direction vector
//        }

//        anim.Play(directionArray[lastDirection]);
//    }

//    //marker: converts a vector3 direction to an index to a slice around a circle
//    //core: this goes in a counter-clockwise direction
//    private int DirectionToIndex(Vector3 direction)
//    {
//        Vector3 norDir = _direction.normalized;

//        float step = 360 / 8; //marker: 45 one circle and 8 slices
//        float offset = step / 2; //maker: 22.5

//        float angle = Vector3.SignedAngle(Vector3.up, norDir); //marker: return the signed angle in degrees between A and B

//        angle += offset;
//        if(angle < 0)
//        {
//            angle += 360;
//        }

//        float setpCount = angle / step;
//        return Mathf.FloorToInt(stepCount);
//}
