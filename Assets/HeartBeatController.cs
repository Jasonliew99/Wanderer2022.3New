using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartBeatController : MonoBehaviour
{
    [Header("References")]
    public AudioSource heartSource;
    public Transform player;
    public string enemyTag = "Enemy";

    [Header("Distance Settings")]
    public float maxDistance = 20f;
    public float minDistance = 3f;

    [Header("Pitch/Speed Settings")]
    public float slowPitch = 0.8f;
    public float fastPitch = 2.2f;

    void Update()
    {
        float closestDist = GetClosestEnemyDistance();

        if (closestDist < maxDistance)
        {
            if (!heartSource.isPlaying) heartSource.Play();

            float t = 1f - Mathf.InverseLerp(minDistance, maxDistance, closestDist);

            heartSource.pitch = Mathf.Lerp(slowPitch, fastPitch, t);
            heartSource.volume = Mathf.Lerp(0.2f, 1.0f, t);
        }
        else
        {
            heartSource.volume = Mathf.Lerp(heartSource.volume, 0, Time.deltaTime * 2f);
            if (heartSource.volume < 0.05f) heartSource.Stop();
        }
    }

    float GetClosestEnemyDistance()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float min = Mathf.Infinity;

        if (enemies.Length == 0) return Mathf.Infinity;

        foreach (GameObject e in enemies)
        {
            float d = Vector3.Distance(player.position, e.transform.position);
            if (d < min) min = d;
        }
        return min;
    }

    void OnDrawGizmosSelected()
    {

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, maxDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);


        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, minDistance);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < maxDistance)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, enemy.transform.position);
            }
        }
    }
}
