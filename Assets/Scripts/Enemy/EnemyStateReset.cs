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

    public PlayerTracker lanternFishAI;
    public WeepingStatueMovement statueAI;

    public void ResetToDefaultState()
    {
        switch (defaultState)
        {
            case DefaultState.Patrol:

                if (lanternFishAI != null)
                    lanternFishAI.ResetToPatrolState();

                break;

            case DefaultState.Statue:

                if (statueAI != null)
                    statueAI.ResetToStatueState();

                break;
        }
    }
}
