using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

//this script allows an enemy to patrol, chase the player, search for the player, and charge at the player
//it has been herer since day 1 but it is the most diaboical of scripts, mother of all scripts
//i want to quit man fuck coding
//this shit is so ass man
//fuck codes

public class PlayerTracker : MonoBehaviour
{
    public enum State { Patrol, Chase, Search }
    private State currentState;

    public NavMeshAgent agent;
    public Transform player;
    public LayerMask obstructionMask; //for walls

    [Header("Movement Speeds")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6f;
    public float searchSpeed = 4f;

    [Header("Detection Settings")]
    public float visionRadius = 10f;
    [Range(0f, 360f)]
    public float visionConeAngle = 90f;
    public float visionConeRange = 8f;

    [Header("Sneak Detection Reduction")]
    public float sneakRadiusMultiplier = 0.5f;
    public float sneakConeRangeMultiplier = 0.5f;
    public float sneakConeAngleMultiplier = 0.75f;

    [Header("Search Settings")]
    public bool SmartSearchMode = true;
    public float searchRadius = 6f;
    public float searchDuration = 5f;
    public float idleTime = 1f;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public bool patrolRandom = false;

    [Header("Charge Settings")]
    public float chargeDistance = 4f;   // Forward cone distance for charge
    [Range(0f, 180f)]
    public float chargeAngle = 60f;     // Forward cone angle for charge
    public float chargeSpeedMultiplier = 2f; // Speed boost for charge
    public float chargeDuration = 0.5f; // How long charge lasts

    private int patrolIndex = 0;
    private bool isWaiting = false;
    public Coroutine currentCoroutine;

    private bool playerInSight;
    private Vector3 lastKnownPosition;
    private float searchTimer = 0f;
    private Vector3 currentSearchPoint;

    private PlayerMovement playerMovement;

    private bool isCharging = false; // prevents overlapping charges

    // --- Temporary death logic ---
    private bool hasKilledPlayer = false;  //aint using it anymore but imma keep it here just in case

    void Start()
    {
        //Initialize state and set first patrol destination
        currentState = State.Patrol;
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }

        playerMovement = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        //state machine logic for detecting player and switching between patrol, chase, and search states
        DetectPlayer();

        switch (currentState)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                Patrol();
                break;
            case State.Chase:
                agent.speed = chaseSpeed;
                Chase();
                break;
            case State.Search:
                agent.speed = searchSpeed;
                Search();
                break;
        }

        HandleCharge();
    }

    void DetectPlayer()
    {
        playerInSight = false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        // Adjust detection if player is sneaking
        float radius = visionRadius;
        float coneRange = visionConeRange;
        float coneAngle = visionConeAngle;

        if (playerMovement != null && playerMovement.IsSneaking)
        {
            radius *= sneakRadiusMultiplier;
            coneRange *= sneakConeRangeMultiplier;
            coneAngle *= sneakConeAngleMultiplier;
        }

        bool inRadius = distance <= radius;
        bool inCone = distance <= coneRange &&
                      Vector3.Angle(transform.forward, dirToPlayer) <= coneAngle * 0.5f;

        bool hasLOS = !Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distance, obstructionMask);

        if ((inRadius || inCone) && hasLOS)
        {
            playerInSight = true;
            lastKnownPosition = player.position;
            currentState = State.Chase;
            isWaiting = false;
        }
        else if (!playerInSight && currentState == State.Chase)
        {
            currentState = State.Search;
            searchTimer = 0f;
            agent.SetDestination(lastKnownPosition);
        }
    }

    void Patrol()
    {
        //Patrol logic that moves between points and idles at each point before moving to the next one, also handles random patrol if enabled
        if (patrolPoints.Length == 0 || isWaiting) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log(currentCoroutine);

            if (currentCoroutine == null) 
            {
                Debug.Log("it is running" + this.gameObject.name);
                currentCoroutine = StartCoroutine(IdleThenNextPatrol());
            }
        }
    }

    IEnumerator IdleThenNextPatrol()
    {
        // This coroutine handles the idle time at each patrol point before moving to the next one
        Debug.Log("starting coroutine" + this.gameObject.name);
        isWaiting = true;
        RotateToward(patrolPoints[patrolIndex].position);
        yield return new WaitForSeconds(idleTime);
        Debug.Log("finish running" + this.gameObject.name);
        isWaiting = false;

        patrolIndex = patrolRandom ? Random.Range(0, patrolPoints.Length) : (patrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void Chase()
    {
        //if the player is in sight, keep chasing them, if not, go to last known position and switch to search state
        if (!isCharging)
            agent.SetDestination(player.position);
    }

    void Search()
    {
        // how does this even work bro, this is so scuffed, i want to die
        if (!SmartSearchMode)
        {
            currentState = State.Patrol;
            return;
        }

        searchTimer += Time.deltaTime;

        //some pathfinding stuff to make enemy move to random points aroud last known postion
        //if enemy reaches search point and player is not found after certain amount of time, go back to patrol state, if player is found, go to chase state
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (searchTimer >= searchDuration)
            {
                currentState = State.Patrol;
                patrolIndex = GetClosestPatrolIndex();
                agent.SetDestination(patrolPoints[patrolIndex].position);
                return;
            }

            currentSearchPoint = GetRandomPointNear(lastKnownPosition, searchRadius);
            agent.SetDestination(currentSearchPoint);
        }
    }

    //so this shit is just pick random point within the search sphere la basically
    Vector3 GetRandomPointNear(Vector3 origin, float radius)
    {
        Vector3 randomDir = Random.insideUnitSphere * radius;
        randomDir.y = 0f;
        Vector3 target = origin + randomDir;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return origin;
    }

    void RotateToward(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    //get to the closest patrol point
    int GetClosestPatrolIndex()
    {
        float minDist = Mathf.Infinity;
        int closest = 0;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }
        return closest;
    }

    // This method checks if the player is within the charge cone and initiates the charge if conditions are met
    void HandleCharge()
    {
        if (isCharging || currentState != State.Chase || player == null) return;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);
        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

        if (distance <= chargeDistance && angleToPlayer <= chargeAngle * 0.5f)
        {
            StartCoroutine(ChargeForward());
        }
    }

    // so this thingy just give a tempo speed boost for the enemy to charge forward
    //the condition is check in the HandleCharge method, if player within charge cone, then do it
    IEnumerator ChargeForward()
    {
        isCharging = true;
        float originalSpeed = agent.speed;
        agent.speed *= chargeSpeedMultiplier;
        agent.SetDestination(player.position);

        yield return new WaitForSeconds(chargeDuration);

        agent.speed = chaseSpeed;
        isCharging = false;
    }

    //the gizmo for vision cone, vision radius, search radius, patrol points, and line to player if in sight, also changes color of vision cone if player is sneaking
    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 forward = transform.forward;

        bool isSneaking = false;
#if UNITY_EDITOR
        if (Application.isPlaying && player != null)
        {
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null && pm.IsSneaking)
            {
                isSneaking = true;
            }
        }
#endif

        float coneRange = isSneaking ? visionConeRange * sneakConeRangeMultiplier : visionConeRange;
        float coneAngle = isSneaking ? visionConeAngle * sneakConeAngleMultiplier : visionConeAngle;
        float radius = isSneaking ? visionRadius * sneakRadiusMultiplier : visionRadius;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);

        Gizmos.color = isSneaking ? new Color(0f, 1f, 0f, 1f) : Color.green;
        float halfAngle = coneAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward * coneRange;
        Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward * coneRange;

        Gizmos.DrawRay(origin, leftDir);
        Gizmos.DrawRay(origin, rightDir);
        Gizmos.DrawLine(origin + leftDir, origin + rightDir);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastKnownPosition, searchRadius);

        Gizmos.color = Color.blue;
        if (patrolPoints != null)
        {
            foreach (Transform point in patrolPoints)
            {
                if (point != null)
                    Gizmos.DrawSphere(point.position, 0.3f);
            }
        }

        if (player != null)
        {
            Gizmos.color = playerInSight ? Color.cyan : Color.gray;
            Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
        }

        // --- Charge cone gizmo ---
        Gizmos.color = Color.magenta;
        Vector3 chargeForward = transform.forward * chargeDistance;
        Quaternion leftRot = Quaternion.Euler(0f, -chargeAngle * 0.5f, 0f);
        Quaternion rightRot = Quaternion.Euler(0f, chargeAngle * 0.5f, 0f);
        Vector3 leftDirC = leftRot * chargeForward;
        Vector3 rightDirC = rightRot * chargeForward;
        Gizmos.DrawRay(transform.position, leftDirC);
        Gizmos.DrawRay(transform.position, rightDirC);
        Gizmos.DrawLine(transform.position + leftDirC, transform.position + rightDirC);
    }

    // Player catch logic that uses respawn manager
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform == player)
        {
            RespawnController respawnController = FindObjectOfType<RespawnController>();
            if (respawnController != null)
            {
                respawnController.HandlePlayerDeath();
            }
        }
    }

    // also catch trigger just in case, since sometimes the agent might be moving too fast and miss the collision
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            RespawnController respawnController = FindObjectOfType<RespawnController>();
            if (respawnController != null)
            {
                respawnController.HandlePlayerDeath();
            }
        }
    }

    // This method can be called by other scripts (like the statue fish) to reset this enemy back to patrol state
    public void ResetToPatrolState()
    {
        StopAllCoroutines();

        currentState = State.Patrol;

        playerInSight = false;
        isWaiting = false;
        isCharging = false;
        searchTimer = 0f;

        //if the agent was in the middle of a charge, reset its speed back to normal
        if (agent != null)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
            agent.speed = patrolSpeed;
        }

        patrolIndex = GetClosestPatrolIndex();

        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[patrolIndex].position);
    }
}
