using System.Collections;
using UnityEngine;

namespace NekoLegends
{
    public class RotateToFromAnimator : MonoBehaviour
    {
        public bool isEnabled = false;
        public float startDelay = 0f;
        public float duration = 1f;

        public Vector3 fromRotation = Vector3.zero;
        public Vector3 toRotation = Vector3.zero;

        public bool useEasing = true;
        public bool rotateClockwise = true;
        public bool setFromRotationImmediately = true;

        private float timeElapsed = 0f;
        private bool isStarted = false;
        private bool rotationCompleted = false;

        private Quaternion fromQuat;
        private Quaternion toQuat;

        private void Start()
        {
            fromQuat = Quaternion.Euler(fromRotation);
            toQuat = Quaternion.Euler(toRotation);

            if (setFromRotationImmediately && isEnabled)
            {
                transform.rotation = rotateClockwise ? fromQuat : toQuat;
            }
        }

        private void Update()
        {
            if (!isEnabled || rotationCompleted) return;

            timeElapsed += Time.deltaTime;

            // Handle delay before rotation starts
            if (!isStarted)
            {
                if (timeElapsed >= startDelay)
                {
                    isStarted = true;
                    timeElapsed = 0f; // reset for rotation duration
                }
                return;
            }

            float t = Mathf.Clamp01(timeElapsed / duration);
            if (useEasing)
            {
                t = Mathf.SmoothStep(0f, 1f, t);
            }

            Quaternion currentRotation = Quaternion.Lerp(
                rotateClockwise ? fromQuat : toQuat,
                rotateClockwise ? toQuat : fromQuat,
                t
            );

            transform.rotation = currentRotation;

            if (timeElapsed >= duration)
            {
                transform.rotation = rotateClockwise ? toQuat : fromQuat;
                isEnabled = false;
                rotationCompleted = true;
            }
        }

        public void StartRotation()
        {
            timeElapsed = 0f;
            isStarted = false;
            rotationCompleted = false;
            isEnabled = true;

            fromQuat = Quaternion.Euler(fromRotation);
            toQuat = Quaternion.Euler(toRotation);

            if (setFromRotationImmediately)
            {
                transform.rotation = rotateClockwise ? fromQuat : toQuat;
            }
        }
    }
}
