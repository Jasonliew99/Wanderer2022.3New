using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStateReset : MonoBehaviour
{
    public enum DefaultState
    {
        Patrol,
        Statue
    }

    public DefaultState defaultState;

    [Header("AI References")]
    public PlayerTracker lanternFishAI;
    public WeepingStatueMovement statueAI;
    public RedGhostMovement redGhostAI;
    public TeddyBearController teddyBearAI;

    public void ResetToDefaultState()
    {
        switch (defaultState)
        {
            case DefaultState.Patrol:
                // Reset Lantern Fish
                if (lanternFishAI != null)
                    lanternFishAI.ResetToPatrolState();

                // Reset Red Ghost
                if (redGhostAI != null)
                    redGhostAI.ResetToPatrolState();

                // Reset Teddy Bear
                if (teddyBearAI != null)
                    teddyBearAI.ResetToPatrolState();

                break;

            case DefaultState.Statue:
                if (statueAI != null)
                    statueAI.ResetToStatueState();

                break;
        }
    }
}
