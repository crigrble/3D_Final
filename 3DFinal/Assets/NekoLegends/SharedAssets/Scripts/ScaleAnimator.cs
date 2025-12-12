using System.Collections;
using UnityEngine;

namespace NekoLegends
{
    public class ScaleAnimator : MonoBehaviour
    {
        public enum ScaleType
        {
            Oscillate,
            ScaleOverTime
        }

        public enum AnimationCurveType
        {
            Linear,
            EaseIn,
            EaseOut,
            EaseInOut
        }

        [Header("General")]
        public ScaleType scaleType = ScaleType.Oscillate;
        public AnimationCurveType animationCurveType = AnimationCurveType.Linear;
        public Vector3 startScale = new Vector3(0f, 0f, 0f);
        public float initialScaleDuration = .1f;

        [Header("Oscillate Settings")]
        public float scaleFrequency = 1.0f;
        public float minScale = 0.8f;
        public float maxScale = 1.2f;

        [Header("Scale Over Time Settings")]
        public Vector3 targetScale = new Vector3(0.1f, 0.1f, 0.1f);
        public float scaleSpeed = 0.1f;

        [Header("ScaleFinal")]
        public bool triggerFinalScale = false;
        public Vector3 finalScale = Vector3.zero;
        public float finalScaleDelay = 5f;
        public float finalScaleDuration = 1f;  // Time to scale to the final scale
        public AnimationCurveType finalScaleCurveType = AnimationCurveType.Linear;

        private bool isInitialScalingDone = false;
        private bool isFinalScaling = false;

        // Reference to the current bounce coroutine
        private Coroutine currentBounceCoroutine = null;

        private void Start()
        {

        }

        private void OnEnable()
        {
            StartScaleAnim();
        }

        public void StartScaleAnim()
        {
            transform.localScale = startScale;
            StopCoroutine(InitialScalingCoroutine());
            StartCoroutine(InitialScalingCoroutine());

            if (triggerFinalScale)
            {
                Invoke("StartFinalScale", finalScaleDelay);
            }
        }

        private void Update()
        {
            if (isFinalScaling || (!isInitialScalingDone && scaleType == ScaleType.ScaleOverTime))
                return;

            float t = GetTBasedOnCurveType(animationCurveType);

            switch (scaleType)
            {
                case ScaleType.Oscillate:
                    float oscillateValue = Mathf.Lerp(minScale, maxScale, t);
                    transform.localScale = new Vector3(oscillateValue, oscillateValue, oscillateValue);
                    break;
                case ScaleType.ScaleOverTime:
                    transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
                    break;
            }
        }

        private float GetTBasedOnCurveType(AnimationCurveType curveType)
        {
            float t = 0f;
            switch (curveType)
            {
                case AnimationCurveType.Linear:
                    t = (Mathf.Sin(Time.time * scaleFrequency) + 1) * 0.5f;
                    break;
                case AnimationCurveType.EaseIn:
                    t = Mathf.Sin(0.5f * Mathf.PI * Time.time * scaleFrequency);
                    break;
                case AnimationCurveType.EaseOut:
                    t = 1f - Mathf.Cos(0.5f * Mathf.PI * Time.time * scaleFrequency);
                    break;
                case AnimationCurveType.EaseInOut:
                    t = 0.5f * (1f + Mathf.Sin(Mathf.PI * Time.time * scaleFrequency - 0.5f * Mathf.PI));
                    break;
            }
            return t;
        }

        private void StartFinalScale()
        {
            Utils.StopAllCoroutinesInChildren(this.transform);
            isFinalScaling = true;
            StartCoroutine(FinalScaleCoroutine());
        }

        private IEnumerator FinalScaleCoroutine()
        {
            Vector3 initialScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < finalScaleDuration)
            {
                float t = elapsed / finalScaleDuration;
                float easedT = GetEasedValueForT(t, finalScaleCurveType);  // Adjust t based on the selected easing curve
                transform.localScale = Vector3.Lerp(initialScale, finalScale, easedT);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = finalScale;
        }

        private float GetEasedValueForT(float t, AnimationCurveType curveType)
        {
            switch (curveType)
            {
                case AnimationCurveType.Linear:
                    return t;
                case AnimationCurveType.EaseIn:
                    return Mathf.Sin(0.5f * Mathf.PI * t);
                case AnimationCurveType.EaseOut:
                    return 1f - Mathf.Cos(0.5f * Mathf.PI * t);
                case AnimationCurveType.EaseInOut:
                    return 0.5f * (1f + Mathf.Sin(Mathf.PI * t - 0.5f * Mathf.PI));
                default:
                    return t;
            }
        }

        private IEnumerator InitialScalingCoroutine()
        {
            float startTime = Time.time;
            if (initialScaleDuration <= 0)
                initialScaleDuration = .001f;

            while (Time.time - startTime <= initialScaleDuration)
            {
                float t = (Time.time - startTime) / initialScaleDuration;
                transform.localScale = Vector3.Lerp(startScale, new Vector3(minScale, minScale, minScale), t);
                yield return null;
            }
            transform.localScale = new Vector3(minScale, minScale, minScale);
            isInitialScalingDone = true;
        }

        /// <summary>
        /// Initiates a scale bounce effect.
        /// </summary>
        /// <param name="scaleFactor">The factor by which to scale the current scale.</param>
        /// <param name="duration">The total duration of the bounce effect in seconds.</param>
        public void ScaleBounceEffect(float scaleFactor, float duration)
        {
            // If a bounce is already in progress, stop it
            if (currentBounceCoroutine != null)
            {
                StopCoroutine(currentBounceCoroutine);
            }

            currentBounceCoroutine = StartCoroutine(ScaleBounceCoroutine(scaleFactor, duration));
        }

        private IEnumerator ScaleBounceCoroutine(float scaleFactor, float duration)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * scaleFactor;

            float halfDuration = duration / 2f;

            // Scaling up
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                float easedT = EaseOut(t); // Using EaseOut for a smooth scaling up
                transform.localScale = Vector3.Lerp(originalScale, targetScale, easedT);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = targetScale;

            // Scaling back down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                float easedT = EaseIn(t); // Using EaseIn for a smooth scaling down
                transform.localScale = Vector3.Lerp(targetScale, originalScale, easedT);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = originalScale;

            // Reset the coroutine reference
            currentBounceCoroutine = null;
        }

        // Easing functions for the bounce effect
        private float EaseIn(float t)
        {
            return t * t;
        }

        private float EaseOut(float t)
        {
            return t * (2f - t);
        }
    }
}
