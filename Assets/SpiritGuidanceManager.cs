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

    [Header("Target Settings")]
    public string targetTag = "HintTarget";
    public float searchRadius = 100f;

    private GameObject currentOrb; // keeps track of active orb

    private void Start()
    {
        StartCoroutine(HintRoutine());
    }

    IEnumerator HintRoutine()
    {
        while (true)
        {
            // If there is still an orb, wait until it's gone
            while (currentOrb != null)
                yield return null;

            // Delay AFTER orb is gone
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            // Find nearest target and spawn orb
            Transform nearest = FindNearestTarget();
            if (nearest != null)
            {
                currentOrb = SpawnOrb(nearest);
            }
        }
    }

    Transform FindNearestTarget()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        Transform nearest = null;
        float shortest = Mathf.Infinity;

        foreach (GameObject obj in targets)
        {
            float d = Vector3.Distance(transform.position, obj.transform.position);
            if (d < shortest && d <= searchRadius)
            {
                shortest = d;
                nearest = obj.transform;
            }
        }
        return nearest;
    }

    GameObject SpawnOrb(Transform target)
    {
        GameObject orb = Instantiate(spiritOrbPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);

        var behavior = orb.GetComponent<SpiritOrbBehaviour>();
        behavior.SetTarget(target);

        // When orb is destroyed, clear reference
        StartCoroutine(WatchOrb(orb));

        return orb;
    }

    IEnumerator WatchOrb(GameObject orb)
    {
        // Wait until orb is destroyed
        while (orb != null)
            yield return null;

        currentOrb = null;
    }
}
