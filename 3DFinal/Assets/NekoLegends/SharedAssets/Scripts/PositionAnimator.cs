using System.Collections;
using UnityEngine;

namespace NekoLegends
{
    public class PositionAnimator : MonoBehaviour
    {
        [Header("Runtime Control")]
        [Tooltip("If true, the animator will be playing. Change at runtime to start/stop.")]
        public bool playOnAwake = true;

        private bool _lastPlayState;
        private bool toggleStopResume = true;

        [Header("Positions")]
        public Vector3 StartPosition = Vector3.zero;
        public Vector3 EndPosition = Vector3.zero;

        [Header("Duration Mode (Default)")]
        public float Duration = 2f;

        [Header("Lock Play")]
        [Tooltip("Minimum seconds between play calls; prevents spamming.")]
        public float playLockDuration = 2f;
        private float _lastPlayTime = -Mathf.Infinity;

        [Header("Speed Mode (overrides duration mode)")]
        public bool useConstantSpeed = false;
        [Tooltip("Base movement speed when not using random range.")]
        public float movementSpeed = 0f;

        [Header("Speed Range")]
        [Tooltip("Enable to use min/max random speed range.")]
        public bool useSpeedRange = false;
        public float minMovementSpeed = 0f;
        public float maxMovementSpeed = 0f;

        [Header("Direction (Speed mode only. overrides end position)")]
        public bool overrideWithDirection = false;
        public Vector3 direction = Vector3.zero;

        [Space]
        public bool Loop = true;
        public bool BounceBack = true;
        public bool setStartPositionImmediately = true;

        [Header("Start Delay")]
        public float startDelay = 0f;
        public float minStartDelay = 0f;
        public float maxStartDelay = 0f;

        [Header("Other")]
        public bool UseLocalPosition = false;
        public bool useEasing = true;

        private Coroutine animationCoroutine;
        private bool isAnimating;
        private Vector3 resumeStartPosition;

        private void Update()
        {
            if (playOnAwake != _lastPlayState)
            {
                if (playOnAwake) StartAnimation();
                else StopAnimation();
                _lastPlayState = playOnAwake;
            }
        }

        private void OnEnable()
        {
            if (toggleStopResume && playOnAwake)
                StartAnimation();
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void StartAnimation()
        {
            // respect lock
            if (Time.time - _lastPlayTime < playLockDuration)
                return;

            if (isAnimating) return;

            _lastPlayTime = Time.time;
            StopAnimation();
            animationCoroutine = StartCoroutine(AnimatePosition());
            isAnimating = true;
        }

        public void StopAnimation()
        {
            if (animationCoroutine != null)
            {
                resumeStartPosition = UseLocalPosition ? transform.localPosition : transform.position;
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
                isAnimating = false;
            }
        }

        private IEnumerator AnimatePosition()
        {
            if (setStartPositionImmediately)
            {
                if (UseLocalPosition) transform.localPosition = StartPosition;
                else transform.position = StartPosition;
            }

            // choose start delay
            float delay = startDelay;
            if (maxStartDelay > minStartDelay)
                delay = Random.Range(minStartDelay, maxStartDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            // determine speed
            float speed = movementSpeed;
            if (useSpeedRange && maxMovementSpeed > minMovementSpeed)
                speed = Random.Range(minMovementSpeed, maxMovementSpeed);

            // nothing to do?
            bool isDirectional = useConstantSpeed && overrideWithDirection && direction != Vector3.zero;
            bool isTween = StartPosition != EndPosition;
            if (!isDirectional && !isTween)
                yield break;

            if (isDirectional)
            {
                // Always move if toggled on; Loop flag no longer gates directional movement
                while (toggleStopResume)
                {
                    transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);
                    yield return null;
                }
                yield break;
            }
            try
            {
                do
                {
                    yield return AnimateFromTo(StartPosition, EndPosition, speed);
                    if (BounceBack)
                        yield return AnimateFromTo(EndPosition, StartPosition, speed);
                }
                while (Loop && toggleStopResume);
            }
            finally
            {
                // this runs when the coroutine finishes naturally
                isAnimating = false;
                animationCoroutine = null;
            }
            
        }

        private IEnumerator AnimateFromTo(Vector3 startPos, Vector3 endPos, float speed)
        {
            float elapsed = 0f;
            float distance = Vector3.Distance(startPos, endPos);
            float duration = useConstantSpeed ? distance / speed : Duration;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (useEasing) t = Mathf.SmoothStep(0f, 1f, t);

                Vector3 newPos = Vector3.Lerp(startPos, endPos, t);
                if (UseLocalPosition) transform.localPosition = newPos;
                else transform.position = newPos;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (UseLocalPosition) transform.localPosition = endPos;
            else transform.position = endPos;
        }

        public void ToggleAnimation()
        {
            if (isAnimating)
            {
                StopAnimation();
            }
            else
            {
                // make sure we’re in “play” mode before starting
                toggleStopResume = true;
                StartAnimation();
            }
        }

        public void PlayAnimation()
        {
            if (!isAnimating)
                StartAnimation();
        }


        public void PauseAnimation()
        {
            if (isAnimating)
            {
                toggleStopResume = false;
                StopAnimation();
            }
        }

        public void ResetAnimation()
        {
            StopAnimation();
            if (setStartPositionImmediately)
            {
                if (UseLocalPosition) transform.localPosition = StartPosition;
                else transform.position = StartPosition;
            }
            StartAnimation();
        }

        public void RestartFromCurrentPosition()
        {
            StartPosition = UseLocalPosition ? transform.localPosition : transform.position;
            ResetAnimation();
        }


        private void OnApplicationQuit()
        {
            StopAnimation();
        }
    }
}
