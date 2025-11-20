using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Charge Settings")]
    [Tooltip("How fast charge fills per second when shined upon.")]
    public float chargeSpeed = 0.5f;

    [Tooltip("How fast charge drains per second when NOT shined upon.")]
    public float drainSpeed = 0.3f;

    [Tooltip("Current charge (0 = none, 1 = full).")]
    [Range(0f, 1f)]
    public float chargeValue = 0f;

    [Tooltip("If true, enemy dies when fully charged.")]
    public bool destroyOnFullCharge = true;

    private bool isBeingShined = false;

    [Header("Charge States (Read Only)")]
    public bool isCharging = false;     // True while light is on enemy
    public bool isChargeFinished = false; // True when chargeValue reaches 1

    [Header("References")]
    [Tooltip("Drag in the TorchLightDetector that belongs to this enemy.")]
    public TorchLightDetector torchLightDetector;


    private void Awake()
    {
        if (torchLightDetector != null)
        {
            torchLightDetector.onEnter += OnTorchlightEnter;
            torchLightDetector.onExit += OnTorchlightExit;
        }
        else
        {
            Debug.LogWarning($"{name} has no TorchLightDetector assigned!", this);
        }
    }

    private void OnDestroy()
    {
        // Cleanly unregister  
        if (torchLightDetector != null)
        {
            torchLightDetector.onEnter -= OnTorchlightEnter;
            torchLightDetector.onExit -= OnTorchlightExit;
        }
    }

    void Update()
    {
        if (isBeingShined)
        {
            isCharging = true;

            chargeValue += chargeSpeed * Time.deltaTime;
            chargeValue = Mathf.Clamp01(chargeValue);

            if (chargeValue >= 1f && !isChargeFinished)
            {
                isChargeFinished = true;

                if (destroyOnFullCharge)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else
        {
            isCharging = false;

            if (chargeValue > 0f)
            {
                chargeValue -= drainSpeed * Time.deltaTime;
                chargeValue = Mathf.Clamp01(chargeValue);

                if (chargeValue < 1f)
                {
                    isChargeFinished = false;
                }
            }
        }

        // Reset every frame
        isBeingShined = false;
    }


    // here will be called by TorchLightDetector

    public void OnTorchlightEnter()
    {
        isBeingShined = true;
    }

    public void OnTorchlightExit()
    {
        // nothing here
    }
}
