using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PlayerTracker : MonoBehaviour
{
    public enum State { Patrol, Chase, Search }
    private State currentState;

    public NavMeshAgent agent;
    public Transform player;
    public LayerMask obstructionMask;

    [Header("Movement Speeds")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6f;
    public float searchSpeed = 4f;

    [Header("Detection Settings")]
    public float visionRadius = 10f;
    public float visionConeRange = 8f;
    [Range(0f, 360f)]
    public float visionConeAngle = 90f;

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

    private int patrolIndex = 0;
    private bool isWaiting = false;

    private bool playerInSight;
    private Vector3 lastKnownPosition;
    private float searchTimer = 0f;
    private Vector3 currentSearchPoint;

    private PlayerMovement playerMovement;

    // --- Temporary death logic ---
    private bool hasKilledPlayer = false; // prevents multiple reloads

    void Start()
    {
        currentState = State.Patrol;
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }

        playerMovement = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
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
        if (patrolPoints.Length == 0 || isWaiting) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartCoroutine(IdleThenNextPatrol());
        }
    }

    IEnumerator IdleThenNextPatrol()
    {
        isWaiting = true;
        RotateToward(patrolPoints[patrolIndex].position);
        yield return new WaitForSeconds(idleTime);
        isWaiting = false;

        patrolIndex = patrolRandom ? Random.Range(0, patrolPoints.Length) : (patrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void Chase()
    {
        agent.SetDestination(player.position);
    }

    void Search()
    {
        if (!SmartSearchMode)
        {
            currentState = State.Patrol;
            return;
        }

        searchTimer += Time.deltaTime;

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
    }

    // --- PLAYER CATCH LOGIC (uses respawn manager) ---
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

    // --- TEMPORARY DEATH LOGIC ---
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (!hasKilledPlayer && collision.transform == player)
    //    {
    //        hasKilledPlayer = true;
    //        ReloadLevel();
    //    }
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (!hasKilledPlayer && other.transform == player)
    //    {
    //        hasKilledPlayer = true;
    //        ReloadLevel();
    //    }
    //}

    //private void ReloadLevel()
    //{
    //    Scene currentScene = SceneManager.GetActiveScene();
    //    SceneManager.LoadScene(currentScene.name);
    //}
}
