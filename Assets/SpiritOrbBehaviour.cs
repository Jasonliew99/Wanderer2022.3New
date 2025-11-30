using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritOrbBehaviour : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float floatAmplitude = 0.25f;
    public float floatSpeed = 3f;

    [Header("Lifetime")]
    public float lifeTime = 4f;

    private Transform target;
    private Vector3 startPos;

    public void SetTarget(Transform tgt)
    {
        target = tgt;
    }

    private void Start()
    {
        startPos = transform.position;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target != null)
        {
            Vector3 dir = target.position - transform.position;
            transform.position += dir.normalized * moveSpeed * Time.deltaTime;
        }

        // Simple floating animation
        float floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, startPos.y + floatY, transform.position.z);
    }
}
