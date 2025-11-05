using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpriteAnimation : MonoBehaviour
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyAnimationController_Flip : MonoBehaviour
    {
        [Header("References")]
        public NavMeshAgent agent;
        public Animator animator;
        public SpriteRenderer spriteRenderer;

        [Header("Animation Clips (Drag & Drop)")]
        public AnimationClip frontRightClip; // SE
        public AnimationClip backRightClip;  // NE

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

            AnimationClip clipToPlay = null;
            bool flipX = false;

            if (angle >= -45f && angle < 45f)
            {
                clipToPlay = frontRightClip; // SE
                flipX = false;
            }
            else if (angle >= 45f && angle < 135f)
            {
                clipToPlay = backRightClip;  // NE
                flipX = false;
            }
            else if (angle >= 135f || angle < -135f)
            {
                clipToPlay = backRightClip;  // NW
                flipX = true;
            }
            else // -135 to -45
            {
                clipToPlay = frontRightClip; // SW
                flipX = true;
            }

            // Apply flip
            spriteRenderer.flipX = flipX;

            // Play animation if different
            if (clipToPlay != null && clipToPlay.name != currentClipName)
            {
                animator.Play(clipToPlay.name);
                currentClipName = clipToPlay.name;
            }
        }
    }
}
