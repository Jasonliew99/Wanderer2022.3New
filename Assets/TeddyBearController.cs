using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TeddyBearController : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Chase,
        MovingToPlaceTrap,
        PlacingTrap,
        InvestigateTrap
    }

    [Header("Movement")]
    public NavMeshAgent agent;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4.5f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int patrolIndex;

    [Header("Detection")]
    public string playerTag = "Player";

    [Header("Trap Prefab")]
    public GameObject trapPrefab;
    public int maxActiveTraps = 4;

    [Header("Trap Placement Zones")]
    public TrapPlacementZone[] trapZones;

    [Header("Trap Chance")]
    [Range(0f, 1f)] public float placeTrapChance = 0.6f;
    [Range(1, 3)] public int maxTrapsPerVisit = 2;
    [Range(0f, 1f)] public float extraTrapChance = 0.4f;

    [Header("Timings")]
    public float placeTrapPause = 0.4f;
    public float investigateTrapTime = 1.2f;

    // -------- INTERNAL --------
    private State currentState = State.Patrol;
    private Transform player;

    private readonly List<GameObject> activeTraps = new();
    private bool busy;

    private TrapPlacementZone currentZone;
    private Vector3 currentPlacePoint;
    private int trapsLeftToPlace;

    // -------- UNITY --------
    void Start()
    {
        player = GameObject.FindGameObjectWithTag(playerTag)?.transform;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = patrolSpeed;

        if (patrolPoints.Length > 0)
        {
            patrolIndex = 0;
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Chase:
                ChasePlayer();
                break;

            case State.MovingToPlaceTrap:
                CheckReachedPlacementPoint();
                break;
        }
    }

    // -------- PATROL --------
    void Patrol()
    {
        if (busy || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    // -------- CHASE --------
    void ChasePlayer()
    {
        if (player == null) return;
        agent.SetDestination(player.position);
    }

    // -------- DETECTION --------
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            StopAllCoroutines();
            busy = false;
            currentState = State.Chase;

            agent.speed = chaseSpeed;
            agent.isStopped = false;
            return;
        }

        TrapPlacementZone zone = other.GetComponent<TrapPlacementZone>();
        if (zone != null && currentState == State.Patrol)
        {
            TryStartTrapPlacement(zone);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            currentState = State.Patrol;
            agent.speed = patrolSpeed;
            ResumePatrol();
        }
    }

    // -------- TRAP LOGIC --------
    void TryStartTrapPlacement(TrapPlacementZone zone)
    {
        if (busy) return;
        if (activeTraps.Count >= maxActiveTraps) return;
        if (Random.value > placeTrapChance) return;

        currentZone = zone;

        trapsLeftToPlace = 1;
        for (int i = 1; i < maxTrapsPerVisit; i++)
        {
            if (Random.value <= extraTrapChance)
                trapsLeftToPlace++;
        }

        trapsLeftToPlace = Mathf.Min(
            trapsLeftToPlace,
            maxActiveTraps - activeTraps.Count
        );

        MoveToNextTrapPoint();
    }

    void MoveToNextTrapPoint()
    {
        if (trapsLeftToPlace <= 0)
        {
            FinishTrapPlacement();
            return;
        }

        if (!currentZone.TryGetRandomPoint(out currentPlacePoint))
        {
            FinishTrapPlacement();
            return;
        }

        busy = true;
        currentState = State.MovingToPlaceTrap;

        agent.speed = patrolSpeed;
        agent.isStopped = false;
        agent.SetDestination(currentPlacePoint);
    }

    void CheckReachedPlacementPoint()
    {
        if (agent.pathPending) return;

        if (agent.remainingDistance <= 0.2f)
        {
            StartCoroutine(PlaceTrapAtFeet());
        }
    }

    IEnumerator PlaceTrapAtFeet()
    {
        currentState = State.PlacingTrap;
        agent.isStopped = true;

        yield return new WaitForSeconds(placeTrapPause);

        Vector3 spawnPos = transform.position;
        spawnPos.y += 0.05f;

        GameObject trap = Instantiate(trapPrefab, spawnPos, Quaternion.identity);
        trap.GetComponent<TrapScriptLogic>()?.Init(this);
        activeTraps.Add(trap);

        trapsLeftToPlace--;

        agent.isStopped = false;

        MoveToNextTrapPoint();
    }

    void FinishTrapPlacement()
    {
        busy = false;
        currentState = State.Patrol;
        ResumePatrol();
    }

    // -------- TRAP CALLBACK --------
    public void OnTrapTriggered(Vector3 trapPosition, GameObject trap)
    {
        if (activeTraps.Contains(trap))
            activeTraps.Remove(trap);

        if (currentState == State.Chase)
            return;

        StartCoroutine(RushToTrap(trapPosition));
    }

    IEnumerator RushToTrap(Vector3 pos)
    {
        busy = true;
        currentState = State.InvestigateTrap;

        agent.speed = chaseSpeed;
        agent.SetDestination(pos);

        while (!agent.pathPending && agent.remainingDistance > 0.2f)
            yield return null;

        yield return new WaitForSeconds(investigateTrapTime);

        busy = false;
        currentState = State.Patrol;
        agent.speed = patrolSpeed;

        ResumePatrol();
    }

    // -------- HELPERS --------
    void ResumePatrol()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }
}
