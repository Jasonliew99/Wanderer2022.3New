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
    public float trapRushSpeed = 6.5f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int patrolIndex;

    [Header("Detection")]
    public string playerTag = "Player";
    public float soundRadius = 8f;
    public LayerMask obstructionMask;

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

    private State currentState = State.Patrol;
    private Transform player;

    private readonly List<GameObject> activeTraps = new();
    private bool busy;

    private TrapPlacementZone currentZone;
    private Vector3 currentPlacePoint;
    private int trapsLeftToPlace;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) player = playerObj.transform;

        agent.updateRotation = true;
        agent.speed = patrolSpeed;

        if (patrolPoints.Length > 0)
        {
            patrolIndex = 0;
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        DetectPlayerBySound();

        if (currentState == State.Chase)
        {
            ChasePlayer();
            return;
        }

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.MovingToPlaceTrap:
                CheckReachedPlacementPoint();
                break;
            case State.InvestigateTrap:
                break;
        }
    }

    void DetectPlayerBySound()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= soundRadius)
        {
            if (currentState != State.Chase)
            {
                StopAllCoroutines();
                busy = false;
                currentState = State.Chase;

                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.isStopped = false;
                agent.speed = chaseSpeed;
            }

            agent.SetDestination(player.position);
            return;
        }

        if (currentState == State.Chase)
        {
            currentState = State.Patrol;
            agent.speed = patrolSpeed;
            ResumePatrol();
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

    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag(playerTag))
        {
            ExecuteDeath();
        }

        TrapPlacementZone zone = other.GetComponent<TrapPlacementZone>();
        if (zone != null && currentState == State.Patrol)
        {
            TryStartTrapPlacement(zone);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            ExecuteDeath();
        }
    }

    void ExecuteDeath()
    {
        RespawnController respawnController = FindObjectOfType<RespawnController>();
        if (respawnController != null)
        {
            respawnController.HandlePlayerDeath();
        }
    }

    // -------- TRAP LOGIC--------
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

    public void OnTrapTriggered(Vector3 trapPosition, GameObject trap)
    {
        if (activeTraps.Contains(trap))
            activeTraps.Remove(trap);

        // If we are ALREADY chasing the player, don't stop chasing to look at a trap.
        if (currentState == State.Chase)
            return;

        // FORCE the bear to care immediately
        StopAllCoroutines(); // Kill any "Placing Trap" or "Patrol" wait timers
        busy = false;        // Unblock the bear

        StartCoroutine(RushToTrap(trapPosition));
    }

    IEnumerator RushToTrap(Vector3 pos)
    {
        if (currentState == State.Chase) yield break;

        busy = true;
        currentState = State.InvestigateTrap;

        // --- THE INSTANT SPRINT FIX ---
        agent.isStopped = true;
        agent.ResetPath();

        agent.speed = trapRushSpeed;

        Vector3 direction = (pos - transform.position).normalized;
        agent.velocity = direction * trapRushSpeed;

        agent.SetDestination(pos);
        agent.isStopped = false;

        while (agent.pathPending || agent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        yield return new WaitForSeconds(investigateTrapTime);

        busy = false;
        currentState = State.Patrol;
        agent.speed = patrolSpeed;
        ResumePatrol();
    }

    void ResumePatrol()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, soundRadius);
    }

    public void ResetToPatrolState()
    {
        StopAllCoroutines();
        busy = false;
        currentState = State.Patrol;

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.velocity = Vector3.zero;

        foreach (GameObject trapObj in activeTraps)
        {
            if (trapObj != null)
            {
                TrapScriptLogic trapScript = trapObj.GetComponent<TrapScriptLogic>();
                if (trapScript != null)
                {
                    trapScript.ForceRelease();
                }
                else
                {
                    Destroy(trapObj);
                }
            }
        }
        activeTraps.Clear();

        ResumePatrol();
    }
}
