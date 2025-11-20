using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritMovement : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;          // Points it will move between
    public float moveSpeed = 2f;              // Move speed
    public float waitTimeAtPoint = 2f;        // Wait at each point
    public bool randomOrder = false;          // Random or in sequence

    [Header("Floating Visual")] // the sprite which is the child of the actual 3d mesh
    public Transform visual;                  // The sprite only
    public float floatAmplitude = 0.2f;       // Height of up n down floaty movement
    public float floatSpeed = 2f;             // Speed of float effect (how fast it goes up n down)

    private int currentPointIndex = 0;
    private bool isWaiting = false;
    private Vector3 visualStartLocalPos;

    void Start()
    {
        if (visual != null)
            visualStartLocalPos = visual.localPosition;
    }

    void Update()
    {
        if (!isWaiting)
            MoveToTarget();

        FloatVisual();
    }

    void MoveToTarget()
    {
        if (patrolPoints.Length == 0)
            return;

        Transform target = patrolPoints[currentPointIndex];

        // Move root transform (actual enemy movement)
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        // When reached point
        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            StartCoroutine(WaitAtPoint());
        }
    }

    IEnumerator WaitAtPoint()
    {
        isWaiting = true;

        yield return new WaitForSeconds(waitTimeAtPoint);

        // Choose next point
        if (randomOrder)
        {
            currentPointIndex = Random.Range(0, patrolPoints.Length);
        }
        else
        {
            currentPointIndex++;
            if (currentPointIndex >= patrolPoints.Length)
                currentPointIndex = 0;
        }

        isWaiting = false;
    }

    void FloatVisual()
    {
        if (visual == null) return;

        // Floating motion for visual only
        float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        visual.localPosition = visualStartLocalPos + new Vector3(0, offset, 0);
    }
}
