using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FishStatueController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public ChaseZoneStatueFish chaseZone; // reference to the chase zone

    [Header("Settings")]
    public float activationRadius = 5f; // radius to start chasing
    public float moveSpeed = 3f;        // chasing speed

    private Vector3 originalPosition;
    private bool isActive = false;

    void Start()
    {
        originalPosition = transform.position;
        isActive = false;
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(player.position, transform.position);

        // Check if player is within activation radius
        if (!isActive && distanceToPlayer <= activationRadius)
        {
            ActivateStatue();
        }

        if (isActive)
        {
            // Check if player is inside the fixed chase zone
            if (chaseZone != null && chaseZone.IsInside(player.position))
            {
                ChasePlayer();
            }
            else
            {
                // Player left chase zone, teleport back instantly
                TeleportBack();
            }
        }
    }

    void ActivateStatue()
    {
        isActive = true;
        Debug.Log("Fish Statue Activated!");
        // Add animations, sound, etc.
    }

    void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.forward = direction; // face player
    }

    void TeleportBack()
    {
        transform.position = originalPosition;
        isActive = false;
        Debug.Log("Fish Statue Deactivated! Teleported back.");
        // Reset animations, sounds, etc.
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
