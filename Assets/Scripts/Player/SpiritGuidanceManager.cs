using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritGuidanceManager : MonoBehaviour
{
    [Header("Hint Timing")]
    public float minDelay = 5f;
    public float maxDelay = 10f;

    [Header("References")]
    public GameObject spiritOrbPrefab;

    [Header("Search Settings")]
    public float searchRadius = 100f;

    private GameObject currentOrb;

    private void Start()
    {
        StartCoroutine(HintRoutine());
    }

    IEnumerator HintRoutine()
    {
        while (true)
        {
            // wait until current orb is gone
            while (currentOrb != null)
                yield return null;

            // delay before spawning next orb
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            // FIND NEAREST VALID FRAGMENT
            Transform nearest = FindNearestTarget();

            if (nearest != null)
            {
                currentOrb = SpawnOrb(nearest);
            }
        }
    }

    // NOW USE FragmentSpawnPoint SYSTEM
    Transform FindNearestTarget()
    {
        FragmentSpawnPoint[] targets =
        FindObjectsOfType<FragmentSpawnPoint>();

        Transform nearest = null;
        float shortest = Mathf.Infinity;

        foreach (var f in targets)
        {
            // IGNORE COLLECTED ONES
            if (f.isCollected) continue;

            float d = Vector3.Distance(
                transform.position,
                f.transform.position
            );

            if (d < shortest && d <= searchRadius)
            {
                shortest = d;
                nearest = f.transform;
            }
        }
        return nearest;
    }

    GameObject SpawnOrb(Transform target)
    {
        GameObject orb = Instantiate(
            spiritOrbPrefab,
            transform.position + Vector3.up * 1f,
            Quaternion.identity
        );

        var behavior = orb.GetComponent<SpiritOrbBehaviour>();
        behavior.SetTarget(target);

        StartCoroutine(WatchOrb(orb));

        return orb;
    }

    IEnumerator WatchOrb(GameObject orb)
    {
        while (orb != null)
            yield return null;

        currentOrb = null;
    }
}
