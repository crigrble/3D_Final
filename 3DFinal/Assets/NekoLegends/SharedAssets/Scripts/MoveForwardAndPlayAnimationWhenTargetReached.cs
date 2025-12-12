using UnityEngine;

namespace NekoLegends
{
    public class MoveForwardAndPlayAnimationWhenTargetReached : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] Transform targetTransform;
        [SerializeField] float tolerance = 0.5f;

        [Header("Axis Checks")]
        [SerializeField] bool checkX = true;
        [SerializeField] bool checkY = true;
        [SerializeField] bool checkZ = true;

        [Header("Movement")]
        [SerializeField] bool autoMoveForward = false;
        [SerializeField] float moveSpeed = 2f;

        [Header("Animation")]
        [SerializeField] Animator animator;
        [SerializeField] AnimationClip initialAnimation;
        [SerializeField] AnimationClip destinationAnimation;
        [SerializeField] bool autoStartInitialAnimation = true;

        bool hasPlayed = false;

        void Start()
        {
            if (autoStartInitialAnimation && animator != null && initialAnimation != null)
            {
                animator.Play(initialAnimation.name);
            }
        }

        void Update()
        {
            if (hasPlayed || targetTransform == null) return;

            if (autoMoveForward)
            {
                MoveForward();
            }

            if (IsTargetReached())
            {
                PlayDestinationAnimation();
            }
        }

        bool IsTargetReached()
        {
            if (targetTransform == null) return false;

            Vector3 current = transform.position;
            Vector3 target = targetTransform.position;

            // Ignore axes you don’t care about
            if (!checkX) current.x = target.x;
            if (!checkY) current.y = target.y;
            if (!checkZ) current.z = target.z;

            return (target - current).sqrMagnitude <= tolerance * tolerance;
        }


        void MoveForward()
        {
            var tgt = targetTransform.position;
            transform.position = Vector3.MoveTowards(
                transform.position, tgt, moveSpeed * Time.deltaTime);
        }

        void PlayDestinationAnimation()
        {
            if (animator != null && destinationAnimation != null) 
            {
                animator.Play(destinationAnimation.name);
                hasPlayed = true;
            }
        }

        // Public API
        public void SetTarget(Transform target) => targetTransform = target;

        public void PlayInitialAnimation()
        {
            if (animator != null && initialAnimation != null)
                animator.Play(initialAnimation.name);
        }

        public void EnableAutoMove(bool enable) => autoMoveForward = enable;

        public void PlayDestinationManually() => PlayDestinationAnimation();
    }
}
