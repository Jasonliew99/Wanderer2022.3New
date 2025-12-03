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

    private void Start()
    {
        StartCoroutine(HintRoutine());
    }

    IEnumerator HintRoutine()
    {
        while (true)
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            Transform nearest = FindNearestTarget();
            if (nearest != null)
            {
                SpawnOrb(nearest);
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

    void SpawnOrb(Transform target)
    {
        GameObject orb = Instantiate(spiritOrbPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        orb.GetComponent<SpiritOrbBehaviour>().SetTarget(target);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f); // light blue, semi-transparent
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}
