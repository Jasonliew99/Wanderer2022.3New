using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RedGhostMovement : MonoBehaviour
{
    //this particular ghost charges at the player when within a certain range, just like a spanish bull
    public enum State { Patrol, Aim, Charge, Search }
    State currentState;

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask obstructionMask;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 3.5f;

    [Header("Detection")]
    public float visionRadius = 8f;
    public float visionConeAngle = 90f;
    public float visionConeRange = 6f;

    [Header("Charge")]
    public float aimTime = 0.5f;
    public float chargeSpeed = 10f;
    [Range(0.1f, 0.6f)] public float overshootPercent = 0.25f;
    public float minOvershootDistance = 1.2f;   // 🔥 GUARANTEED momentum
    public float wallCheckDistance = 0.4f;

    [Header("Search")]
    public float searchSpeed = 5f;
    public float searchRadius = 6f;
    public float searchDuration = 5f;

    int patrolIndex;
    int searchIndex;

    Vector3 lastKnownPlayerPos;
    Vector3 chargeDirection;

    float chargeDistance;
    float chargeTravelled;
    float aimTimer;
    float searchTimer;

    List<Transform> searchPoints = new List<Transform>();

    // ================= START =================

    void Start()
    {
        currentState = State.Patrol;

        agent.updateRotation = false;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.05f;

        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
            {
                Debug.LogError("RedGhost not on NavMesh");
                enabled = false;
                return;
            }
        }

        patrolIndex = GetClosestPatrolIndex();
        GoToNextPatrol();
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                DetectPlayer();
                break;

            case State.Aim:
                Aim();
                break;

            case State.Charge:
                Charge();
                break;

            case State.Search:
                Search();
                DetectPlayer();
                break;
        }
    }

    // ================= PATROL =================

    void Patrol()
    {
        if (!agent.isOnNavMesh) return;

        SnapFacing(agent.velocity);

        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
            GoToNextPatrol();
    }

    void GoToNextPatrol()
    {
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        agent.speed = patrolSpeed;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    // ================= DETECTION =================

    void DetectPlayer()
    {
        if (!player) return;

        Vector3 dir = player.position - transform.position;
        float dist = dir.magnitude;
        dir.Normalize();

        bool inRadius = dist <= visionRadius;
        bool inCone = dist <= visionConeRange &&
                      Vector3.Angle(transform.forward, dir) <= visionConeAngle * 0.5f;

        bool hasLOS = !Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            dir,
            dist,
            obstructionMask
        );

        if ((inRadius || inCone) && hasLOS)
        {
            lastKnownPlayerPos = player.position;

            // 🔥 ADAPTIVE + GUARANTEED OVERSHOOT
            float perceptionLimit = inCone ? visionConeRange : visionRadius;
            float baseDistance = Mathf.Min(dist, perceptionLimit);
            float overshoot = Mathf.Max(baseDistance * overshootPercent, minOvershootDistance);

            chargeDistance = baseDistance + overshoot;

            EnterAim();
        }
    }

    // ================= AIM =================

    void EnterAim()
    {
        currentState = State.Aim;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        SnapFacing(lastKnownPlayerPos - transform.position);
        aimTimer = aimTime;
    }

    void Aim()
    {
        aimTimer -= Time.deltaTime;
        if (aimTimer <= 0f)
            StartCharge();
    }

    // ================= CHARGE =================

    void StartCharge()
    {
        currentState = State.Charge;
        chargeDirection = SnapDirection(lastKnownPlayerPos - transform.position);
        chargeTravelled = 0f;
    }

    void Charge()
    {
        float step = chargeSpeed * Time.deltaTime;

        if (Physics.Raycast(transform.position, chargeDirection, wallCheckDistance, obstructionMask))
        {
            EnterSearch();
            return;
        }

        transform.position += chargeDirection * step;
        chargeTravelled += step;

        if (chargeTravelled >= chargeDistance)
            EnterSearch();
    }

    // ================= SEARCH =================

    void EnterSearch()
    {
        currentState = State.Search;

        agent.isStopped = false;
        agent.speed = searchSpeed;

        searchTimer = searchDuration;
        searchPoints.Clear();

        foreach (Transform p in patrolPoints)
        {
            if (Vector3.Distance(lastKnownPlayerPos, p.position) <= searchRadius)
                searchPoints.Add(p);
        }

        searchIndex = 0;

        if (searchPoints.Count > 0)
            agent.SetDestination(searchPoints[0].position);
        else
            ReturnToPatrol();
    }

    void Search()
    {
        if (!agent.isOnNavMesh) return;

        SnapFacing(agent.velocity);
        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            ReturnToPatrol();
            return;
        }

        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            searchIndex++;
            if (searchIndex >= searchPoints.Count)
                ReturnToPatrol();
            else
                agent.SetDestination(searchPoints[searchIndex].position);
        }
    }

    void ReturnToPatrol()
    {
        currentState = State.Patrol;
        agent.speed = patrolSpeed;
        GoToNextPatrol();
    }

    // ================= HELPERS =================

    int GetClosestPatrolIndex()
    {
        float min = float.MaxValue;
        int idx = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (d < min)
            {
                min = d;
                idx = i;
            }
        }
        return idx;
    }

    void SnapFacing(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.LookRotation(SnapDirection(dir));
    }

    Vector3 SnapDirection(Vector3 dir)
    {
        dir.y = 0;
        dir.Normalize();
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angle / 45f) * 45f;
        return Quaternion.Euler(0, snapped, 0) * Vector3.forward;
    }

    // ================= GIZMOS =================

    void OnDrawGizmosSelected()
    {
        Vector3 ground = new Vector3(lastKnownPlayerPos.x, transform.position.y, lastKnownPlayerPos.z);

        // Vision
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = Color.green;
        float half = visionConeAngle * 0.5f;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -half, 0) * transform.forward * visionConeRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, half, 0) * transform.forward * visionConeRange);

        // 🔵 Last known position (same style as patrol points)
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(ground, 0.35f);

        // Search radius
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(ground, searchRadius);

        // Search patrol points
        Gizmos.color = new Color(1f, 0.6f, 0f);
        foreach (Transform p in patrolPoints)
        {
            if (Vector3.Distance(lastKnownPlayerPos, p.position) <= searchRadius)
            {
                Gizmos.DrawLine(ground, p.position);
                Gizmos.DrawSphere(p.position, 0.25f);
            }
        }

        // Charge target (circle + X)
        if (currentState == State.Aim || currentState == State.Charge)
        {
            Vector3 end = transform.position + chargeDirection * chargeDistance;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(end, 0.5f);
            Gizmos.DrawLine(transform.position, end);

            Gizmos.DrawLine(end + Vector3.forward * 0.35f, end - Vector3.forward * 0.35f);
            Gizmos.DrawLine(end + Vector3.right * 0.35f, end - Vector3.right * 0.35f);
        }

        // Patrol points
        Gizmos.color = Color.blue;
        foreach (Transform p in patrolPoints)
            if (p) Gizmos.DrawSphere(p.position, 0.3f);

        // Current patrol target
        if (Application.isPlaying && patrolPoints.Length > 0)
            Gizmos.DrawLine(transform.position, patrolPoints[patrolIndex].position);
    }
}
