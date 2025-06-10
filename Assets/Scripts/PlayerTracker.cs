using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    public float sightRange = 10f;

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

    void Start()
    {
        currentState = State.Patrol;
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
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

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= sightRange)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;

            if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distance, obstructionMask))
            {
                playerInSight = true;
                lastKnownPosition = player.position;
                currentState = State.Chase;
                isWaiting = false;
            }
        }

        if (!playerInSight && currentState == State.Chase)
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
        // Only active if toggle is enabled
        if (!SmartSearchMode)
        {
            currentState = State.Patrol;
            return;
        }

        searchTimer += Time.deltaTime;

        // If reached last known position, start random search
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

            // OPTIONAL: show visual feedback like a ? mark or change material/color
            // e.g. Instantiate(searchEffectPrefab, transform.position, Quaternion.identity);
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastKnownPosition, searchRadius);

        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
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
}
