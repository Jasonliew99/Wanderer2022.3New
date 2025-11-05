using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpriteAnimation : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Animation Clips (Drag & Drop)")]
    public AnimationClip frontRightClip; // SE
    public AnimationClip frontLeftClip;  // SW
    public AnimationClip backRightClip;  // NE
    public AnimationClip backLeftClip;   // NW

    private string currentClipName = "";

    void Update()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 0.01f)
        {
            PlayMovementAnimation(velocity.normalized);
        }
    }

    void PlayMovementAnimation(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        AnimationClip clipToPlay;

        if (angle >= -45f && angle < 45f)
            clipToPlay = frontRightClip; // SE
        else if (angle >= 45f && angle < 135f)
            clipToPlay = backRightClip;  // NE
        else if (angle >= 135f || angle < -135f)
            clipToPlay = backLeftClip;   // NW
        else
            clipToPlay = frontLeftClip;  // SW

        if (clipToPlay != null && clipToPlay.name != currentClipName)
        {
            animator.Play(clipToPlay.name);
            currentClipName = clipToPlay.name;
        }
    }
}
