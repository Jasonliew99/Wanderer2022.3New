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

    [Header("Detection Settings")]
    public float sightRange = 10f;
    public float searchTime = 3f;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public bool patrolRandom = false;
    public float idleTime = 1f;

    private int patrolIndex = 0;
    private bool isWaiting = false;

    private List<Transform> searchTargets = new List<Transform>();
    private int searchIndex = 0;
    private bool isSearchingNearby = false;

    private bool playerInSight;
    private float lastSeenTime;
    private Vector3 lastKnownPosition;

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
                //patrol
            case State.Patrol:
                agent.speed = patrolSpeed;
                Patrol();
                break;

                //chase
            case State.Chase:
                agent.speed = chaseSpeed;
                Chase();
                break;

                //Search
            case State.Search:
                agent.speed = patrolSpeed;
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
                lastSeenTime = Time.time;
                currentState = State.Chase;
                isWaiting = false;
                isSearchingNearby = false;
            }
        }

        if (!playerInSight && currentState == State.Chase)
        {
            currentState = State.Search;
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0 || isWaiting) return;

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                agent.ResetPath();
                StartCoroutine(IdleThenNextPatrol());
            }
        }
        else
        {
            if (!agent.hasPath)
            {
                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
        }
    }

    IEnumerator IdleThenNextPatrol()
    {
        isWaiting = true;
        RotateToward(patrolPoints[patrolIndex].position);
        yield return new WaitForSeconds(idleTime);
        isWaiting = false;

        if (patrolRandom)
            patrolIndex = Random.Range(0, patrolPoints.Length);
        else
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void Chase()
    {
        float directDistance = Vector3.Distance(transform.position, player.position);

        // Find patrol points near the player within 7 units
        List<Transform> nearbyPoints = new List<Transform>();
        foreach (Transform point in patrolPoints)
        {
            if (Vector3.Distance(player.position, point.position) < 7f)
                nearbyPoints.Add(point);
        }

        if (nearbyPoints.Count > 0)
        {
            // Find closest flank point to enemy
            Transform flankPoint = nearbyPoints[0];
            float flankDistance = Vector3.Distance(transform.position, flankPoint.position);

            foreach (var p in nearbyPoints)
            {
                float dist = Vector3.Distance(transform.position, p.position);
                if (dist < flankDistance)
                {
                    flankDistance = dist;
                    flankPoint = p;
                }
            }

            float margin = 1.0f; //aggressiveness

            if (directDistance <= flankDistance + margin)
            {
                // Chase player directly
                agent.SetDestination(player.position);
            }
            else
            {
                // Go to flank point
                agent.SetDestination(flankPoint.position);
            }
        }
        else
        {
            // No flank points nearby, just chase directly
            agent.SetDestination(player.position);
        }
    }

    void Search()
    {
        if (!isSearchingNearby)
        {
            agent.SetDestination(lastKnownPosition);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                searchTargets.Clear();
                foreach (Transform point in patrolPoints)
                {
                    if (Vector3.Distance(lastKnownPosition, point.position) < 7f)
                    {
                        searchTargets.Add(point);
                    }
                }

                if (searchTargets.Count == 0)
                {
                    currentState = State.Patrol;
                    patrolIndex = GetClosestPatrolIndex();
                    agent.SetDestination(patrolPoints[patrolIndex].position);
                    return;
                }

                isSearchingNearby = true;
                searchIndex = 0;
                StartCoroutine(IdleThenNextSearch());
            }
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isWaiting)
            {
                StartCoroutine(IdleThenNextSearch());
            }
        }
    }

    IEnumerator IdleThenNextSearch()
    {
        isWaiting = true;
        if (searchIndex < searchTargets.Count)
        {
            RotateToward(searchTargets[searchIndex].position);
            yield return new WaitForSeconds(idleTime);
            agent.SetDestination(searchTargets[searchIndex].position);
            searchIndex++;
            isWaiting = false;
        }
        else
        {
            yield return new WaitForSeconds(idleTime);
            isSearchingNearby = false;
            isWaiting = false;
            currentState = State.Patrol;
            patrolIndex = GetClosestPatrolIndex();
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
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
        float minDistance = Mathf.Infinity;
        int closestIndex = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);

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
