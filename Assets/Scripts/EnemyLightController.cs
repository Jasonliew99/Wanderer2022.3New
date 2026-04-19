using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLightController : MonoBehaviour
{
    [Header("Detection")]
    public float effectRadius = 20f;
    public float maxIntensity = 101f;

    [Header("Target Lights")]
    public List<GameObject> streetlights;

    private Dictionary<Light, float> nextBlinkTime = new Dictionary<Light, float>();

    void Update()
    {
        foreach (GameObject lightObject in streetlights)
        {
            if (lightObject == null) continue;
            Light l = lightObject.GetComponentInChildren<Light>();
            if (l == null) continue;

            float dist = Vector3.Distance(transform.position, lightObject.transform.position);

            if (dist < effectRadius)
            {
                float panicFactor = 1f - (dist / effectRadius);

                if (!nextBlinkTime.ContainsKey(l) || Time.time >= nextBlinkTime[l])
                {
                    StartCoroutine(BlinkSequence(l, panicFactor));
                    float waitTime = Random.Range(0.5f, 2.0f) * (1f - panicFactor);
                    nextBlinkTime[l] = Time.time + waitTime + 0.5f;
                }
            }
            else
            {
                l.intensity = Mathf.Lerp(l.intensity, maxIntensity, Time.deltaTime * 2f);
            }
        }
    }

    IEnumerator BlinkSequence(Light l, float panic)
    {
        int flashCount = Random.Range(2, (int)Mathf.Lerp(4, 8, panic));

        for (int i = 0; i < flashCount; i++)
        {
            l.intensity = 0f;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f) * (1f - (panic * 0.5f)));

            l.intensity = maxIntensity;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f) * (1f - (panic * 0.5f)));
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, effectRadius);

        if (streetlights == null) return;

        foreach (GameObject lightObject in streetlights)
        {
            if (lightObject != null)
            {
                float distance = Vector3.Distance(transform.position, lightObject.transform.position);

                if (distance < effectRadius)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, lightObject.transform.position);
                    Gizmos.DrawSphere(lightObject.transform.position, 0.3f);
                }
            }
        }
    }
}
