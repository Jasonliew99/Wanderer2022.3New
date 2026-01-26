using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RedGhostMovement : MonoBehaviour
{
    //this particular ghost charges at the player when within a certain range, just like a spanish bull
    public enum State { Patrol, chase, ram, Search }

    public NavMeshAgent agent;
    public Transform player;
    public LayerMask theWall;

    [Header("Movement Speeds")]
    public float patrolSpeed = 1f;
    public float chaseSpeed = 3.5f;
    public float ramSpeed = 7f;
    public float ramCooldown = 5f;
    public float ramDuration = 2f;

    [Header("Patrol Settings")]

    [Header("Patrol Settings")]

    [Header("Detection Settings")]

    [Header("Search Settings")]


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //the is or patroling state, it just moves from point to point
    void Patroling()
    {

    }

    //the chasing state, it chases after the player upon detecting them
    void Chasing()
    {

    }

    //the ramming state, it charges at the player. The cooldown period is herer too
    void Ramming()
    {

    }

    //this will be activated when lost sight of player(lost sight of player at chasing state)
    void Searching()
    {

    }
}
