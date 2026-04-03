using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RedGhostMovement : MonoBehaviour
{
    //this particular stupid ass ghost charges at the player when within a certain range, just like a spanish bull
    public enum State { Patrol, Aim, Charge, Search }
    State currentState;

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask obstructionMask;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 3.5f;
    public float patrolWaitTime = 1.0f;

    [Header("Detection")]
    public float visionRadius = 8f;
    public float visionConeAngle = 90f;
    public float visionConeRange = 6f;

    [Header("Charge")]
    public float aimTime = 0.5f;
    public float chargeSpeed = 12f;
    [Range(0.1f, 0.6f)] public float overshootPercent = 0.2f;
    public float minOvershootDistance = 1.0f;
    public float maxOvershootDistance = 3.0f;
    public float wallCheckDistance = 0.4f;

    [Header("Drift")]
    public float driftDuration = 0.25f;
    public float driftTurnSpeed = 8f;

    [Header("Search")]
    public float searchSpeed = 5f;
    public float searchRadius = 6f;
    public float searchDuration = 5f;

    [Header("Recovery")]
    public float recoveryTime = 1.0f;

    [Header("Aim Settings")]
    public float lostSightThreshold = 0.7f;

    int patrolIndex;
    int searchIndex;

    Vector3 lastKnownPlayerPos;
    Vector3 chargeDirection;

    float chargeDistance;
    float chargeTravelled;
    float aimTimer;
    float searchTimer;
    float lostSightTimer;

    float patrolWaitTimer;
    bool isWaiting;

    float lookTimer;
    int lookDirection = 1;

    float currentSpeed;

    float recoveryTimer;
    bool isRecovering;

    float driftTimer;
    bool isDriftingTurn;

    List<Transform> searchPoints = new List<Transform>();

    void Start()
    {
        currentState = State.Patrol;

        agent.updateRotation = false;
        agent.speed = patrolSpeed;

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

    // ---------------- PATROL ----------------

    void Patrol()
    {
        if (isWaiting)
        {
            patrolWaitTimer -= Time.deltaTime;
            lookTimer -= Time.deltaTime;

            if (lookTimer <= 0f)
            {
                lookTimer = Random.Range(0.4f, 0.8f);
                lookDirection = Random.value > 0.5f ? 1 : -1;
            }

            transform.Rotate(0, lookDirection * 40f * Time.deltaTime, 0);

            if (patrolWaitTimer <= 0f)
            {
                isWaiting = false;
                GoToNextPatrol();
            }
            return;
        }

        SnapFacing(agent.velocity);

        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaiting = true;
            patrolWaitTimer = patrolWaitTime;
            agent.velocity = Vector3.zero;
        }
    }

    void GoToNextPatrol()
    {
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    // ---------------- DETECTION ----------------

    void DetectPlayer()
    {
        if (!player) return;
        if (!CanCurrentlySeePlayer()) return;

        lastKnownPlayerPos = player.position;

        float dist = Vector3.Distance(transform.position, player.position);

        // ✅ FIX: use FULL distance (no more short charge)
        float perceptionLimit = dist;

        float rawOvershoot = Mathf.Max(perceptionLimit * overshootPercent, minOvershootDistance);
        float overshoot = Mathf.Min(rawOvershoot, maxOvershootDistance);

        chargeDistance = perceptionLimit + overshoot;

        if (currentState != State.Aim && currentState != State.Charge)
            EnterAim();
    }

    bool CanCurrentlySeePlayer()
    {
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

        return (inRadius || inCone) && hasLOS;
    }

    // ---------------- AIM ----------------

    void EnterAim()
    {
        currentState = State.Aim;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        aimTimer = aimTime;
        lostSightTimer = 0f;
    }

    void Aim()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0;

        float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);

        bool inCone = angleToPlayer <= visionConeAngle * 0.5f;

        if (CanCurrentlySeePlayer() && inCone)
        {
            // keep updating target
            lastKnownPlayerPos = player.position;
            lostSightTimer = 0f;

            // 🔥 STRONG tracking (like lock-on)
            Quaternion targetRot = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                360f * Time.deltaTime // fast turning
            );

            aimTimer -= Time.deltaTime;
        }
        else
        {
            // player escaped cone
            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= lostSightThreshold)
            {
                ReturnToPatrol();
                return;
            }
        }

        if (aimTimer <= 0f)
            StartCharge();
    }

    // ---------------- CHARGE ----------------

    void StartCharge()
    {
        currentState = State.Charge;

        chargeDirection = (lastKnownPlayerPos - transform.position).normalized;
        chargeTravelled = 0f;

        currentSpeed = chargeSpeed;
        isDriftingTurn = false;

        agent.isStopped = true;
    }

    void Charge()
    {
        Vector3 origin = transform.position + Vector3.up * 0.3f;

        if (Physics.Raycast(origin, chargeDirection, wallCheckDistance, obstructionMask))
        {
            StartDrift();
            return;
        }

        float step = currentSpeed * Time.deltaTime;

        transform.position += chargeDirection * step;
        chargeTravelled += step;

        SnapFacing(chargeDirection);

        // ✅ straight line until full distance
        if (!isDriftingTurn && chargeTravelled >= chargeDistance)
        {
            StartDrift();
        }

        // drift phase
        if (isDriftingTurn)
        {
            driftTimer -= Time.deltaTime;

            currentSpeed = Mathf.Lerp(currentSpeed, 0f, 12f * Time.deltaTime);

            Vector3 dirToPlayer = SnapDirection(player.position - transform.position);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dirToPlayer),
                Time.deltaTime * driftTurnSpeed
            );

            if (driftTimer <= 0f)
                EnterSearch();
        }
    }

    void StartDrift()
    {
        isDriftingTurn = true;
        driftTimer = driftDuration;

        // small push forward for impact
        transform.position += chargeDirection * 0.2f;
    }

    // ---------------- SEARCH ----------------

    void EnterSearch()
    {
        currentState = State.Search;

        agent.isStopped = true;

        recoveryTimer = recoveryTime;
        isRecovering = true;
    }

    void Search()
    {
        if (isRecovering)
        {
            recoveryTimer -= Time.deltaTime;

            transform.Rotate(0, Mathf.Sin(Time.time * 10f) * 2f, 0);

            if (recoveryTimer <= 0f)
            {
                isRecovering = false;

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
            return;
        }

        if (agent.pathPending) return;

        SnapFacing(agent.velocity);

        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            ReturnToPatrol();
            return;
        }

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

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        isWaiting = false;

        GoToNextPatrol();
    }

    // ---------------- HELPERS ----------------

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

    // --- KILL LOGIC ---
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the thing we hit is the player
        if (collision.transform == player)
        {
            KillPlayer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Double check for triggers in case the player has a trigger collider
        if (other.transform == player)
        {
            KillPlayer();
        }
    }

    void KillPlayer()
    {
        RespawnController respawnController = FindObjectOfType<RespawnController>();
        if (respawnController != null)
        {
            respawnController.HandlePlayerDeath();
        }
        else
        {
            Debug.LogError("No RespawnController found in the scene! The player should be dead but I don't know how to kill them.");
        }
    }

    // ---------------- GIZMOS ----------------

    void OnDrawGizmosSelected()
    {
        Vector3 ground = new Vector3(lastKnownPlayerPos.x, transform.position.y, lastKnownPlayerPos.z);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = Color.green;
        float half = visionConeAngle * 0.5f;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -half, 0) * transform.forward * visionConeRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, half, 0) * transform.forward * visionConeRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(ground, 0.35f);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(ground, searchRadius);

        Gizmos.color = new Color(1f, 0.6f, 0f);
        foreach (Transform p in patrolPoints)
        {
            if (Vector3.Distance(lastKnownPlayerPos, p.position) <= searchRadius)
            {
                Gizmos.DrawLine(ground, p.position);
                Gizmos.DrawSphere(p.position, 0.25f);
            }
        }

        if (currentState == State.Aim || currentState == State.Charge)
        {
            Vector3 end = transform.position + chargeDirection * chargeDistance;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(end, 0.5f);
            Gizmos.DrawLine(transform.position, end);

            Gizmos.DrawLine(end + Vector3.forward * 0.35f, end - Vector3.forward * 0.35f);
            Gizmos.DrawLine(end + Vector3.right * 0.35f, end - Vector3.right * 0.35f);
        }

        Gizmos.color = Color.blue;
        foreach (Transform p in patrolPoints)
            if (p) Gizmos.DrawSphere(p.position, 0.3f);

        if (Application.isPlaying && patrolPoints.Length > 0)
            Gizmos.DrawLine(transform.position, patrolPoints[patrolIndex].position);
    }
}
