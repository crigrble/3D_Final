using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NekoLegends
{
    public class DemoCameraSceneTargetsWithFocus : DemoCameraScene
    {
        [SerializeField] private List<Transform> FocusTargets;
        [SerializeField] private List<GameObject> Items;
        [SerializeField] private List<float> TargetTransitionDurations;
        [SerializeField] private bool UseTransitionDurationForDOF = true;
        [SerializeField] private bool UseTargetPositionForDescription = false;
        [SerializeField] private bool AutoPlayCameraTargets = false; // Checkbox for auto-play
        [SerializeField] private float SecondsDelayBeforeNextTarget = 1.0f; // Delay between transitions in seconds
         [Header("Aperature Calculation Distances")]
        [SerializeField]private float minDistance = 1f;
        [SerializeField]private float maxDistance = 5.0f;
        [SerializeField]private float minAperture = 1.4f;
        [SerializeField]private float maxAperture = 16.0f;
        private Coroutine autoPlayCoroutine; // Track the auto-play coroutine

        protected override void Start()
        {
            base.Start();

            currentTargetIndex = 0;

            if (FocusTargets == null || FocusTargets.Count == 0 || Targets == null || Targets.Count == 0)
            {
                Debug.LogWarning("FocusTargets or Targets are empty or null in DemoCameraSceneTargetsWithFocus.");
                return;
            }

            if (FocusTargets.Count != Targets.Count)
            {
                Debug.LogWarning($"FocusTargets count ({FocusTargets.Count}) does not match Targets count ({Targets.Count}).");
            }

            if (TargetTransitionDurations != null && TargetTransitionDurations.Count > 0 && TargetTransitionDurations.Count < Targets.Count)
            {
                Debug.LogWarning($"TargetTransitionDurations count ({TargetTransitionDurations.Count}) is less than Targets count ({Targets.Count}).");
            }

            if (!DisableCameraControl && _cameraController != null)
            {
                dummyTarget.position = Targets[currentTargetIndex].position + dummyTargetOffset;
                if (UseTargetTransformRotation)
                {
                    _cameraController.mainCamera.transform.rotation = Targets[currentTargetIndex].rotation;
                }
                _cameraController.target = dummyTarget;
            }

            InitTargets();

            // Start auto-play if enabled
            if (AutoPlayCameraTargets)
            {
                autoPlayCoroutine = StartCoroutine(AutoPlayTargets());
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Ensure auto-play resumes if the script is re-enabled
            if (AutoPlayCameraTargets && autoPlayCoroutine == null)
            {
                autoPlayCoroutine = StartCoroutine(AutoPlayTargets());
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Stop auto-play coroutine when the script is disabled
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
        }

        // Called when values are changed in the Inspector
        private void OnValidate()
        {
            // If AutoPlayCameraTargets is toggled in the Inspector while the game is running
            if (Application.isPlaying)
            {
                if (AutoPlayCameraTargets && autoPlayCoroutine == null)
                {
                    autoPlayCoroutine = StartCoroutine(AutoPlayTargets());
                }
                else if (!AutoPlayCameraTargets && autoPlayCoroutine != null)
                {
                    StopCoroutine(autoPlayCoroutine);
                    autoPlayCoroutine = null;
                }

                // Ensure SecondsDelayBeforeNextTarget is non-negative
                if (SecondsDelayBeforeNextTarget < 0f)
                {
                    Debug.LogWarning("SecondsDelayBeforeNextTarget cannot be negative. Setting to 0.");
                    SecondsDelayBeforeNextTarget = 0f;
                }
            }
        }

        private IEnumerator AutoPlayTargets()
        {
            while (true)
            {
                // Wait for camera transition to finish
                yield return new WaitUntil(() => !isTransitioning);

                // Apply the delay
                if (SecondsDelayBeforeNextTarget > 0f)
                {
                    yield return new WaitForSeconds(SecondsDelayBeforeNextTarget);
                }

                // Move to next target
                ChangeTarget();
            }
        }

        protected override bool ShouldSetAutoDOFTarget()
        {
            return false;
        }

        private void InitTargets()
        {
            currentVisibleTargetIndex = 0;

            if (Items == null || Items.Count == 0)
            {
                Debug.LogWarning("Items list is empty or null in DemoCameraSceneTargetsWithFocus.");
                return;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                int targetIndex = i % Items.Count;
                GameObject targetTransform = Items[targetIndex];
                targetTransform.SetActive(i == 0);
            }

            if (Items.Count > 0)
            {
                SetDescriptionText(UseTargetPositionForDescription ? Targets[0].name : Items[0].name);
            }
        }

        private float GetTransitionDuration(int targetIndex)
        {
            if (TargetTransitionDurations != null && TargetTransitionDurations.Count > 0 && targetIndex < TargetTransitionDurations.Count)
            {
                return TargetTransitionDurations[targetIndex];
            }
            return transitionDuration;
        }

        protected override void ChangeTarget()
        {
            if (isTransitioning)
            {
                Debug.Log("Transition in progress, skipping ChangeTarget.");
                return;
            }

            if (FocusTargets.Count == 0 || Targets.Count == 0) return;

            currentTargetIndex = (currentTargetIndex + 1) % FocusTargets.Count;

            if (!DisableCameraControl && _cameraController != null)
            {
                float duration = GetTransitionDuration(currentTargetIndex);

                // Temporarily disable AutoDOFTarget to prevent interference
                Transform previousAutoDOFTarget = _cameraController.AutoDOFTarget;
                _cameraController.AutoDOFTarget = null;

                // Move the camera
                MoveTargetSmoothly(Targets[currentTargetIndex].position, Targets[currentTargetIndex].rotation, duration);

                // Ensure we're using global positions for both camera and target
                Vector3 cameraPosition = _cameraController.mainCamera.transform.position;
                Vector3 targetPosition = FocusTargets[currentTargetIndex].position; // Should already be in world space

                // Calculate the distance between the camera and the focus target
                float cameraTargetDistance = Vector3.Distance(cameraPosition, targetPosition);

                // Define the distance range for aperture mapping
                float distanceRatio = Mathf.InverseLerp(minDistance, maxDistance, cameraTargetDistance);

                float adjustedRatio = Mathf.Pow(distanceRatio, 2); // Non-linear curve
                float targetAperture = Mathf.Lerp(minAperture, maxAperture, adjustedRatio);
                // Debug log to verify values
                //Debug.Log($"Target: {FocusTargets[currentTargetIndex].name}, CameraPos: {cameraPosition}, TargetPos: {targetPosition}, Distance: {cameraTargetDistance}, DistanceRatio: {distanceRatio}, TargetAperture: {targetAperture}");

                if (!_cameraController.UseManualDOF)
                {
                    if (UseTransitionDurationForDOF)
                    {
                        Vector3 cameraEndPosition = Targets[currentTargetIndex].position + dummyTargetOffset;
                        DemoScenes.Instance.SetDOFToTarget(FocusTargets[currentTargetIndex], cameraEndPosition, targetAperture, duration);
                        _cameraController.SetDOFTransitioning(true);
                        StartCoroutine(ResetDOFTransitioning(duration, previousAutoDOFTarget));
                    }
                    else
                    {
                        float focusDistance = Vector3.Distance(_cameraController.mainCamera.transform.position, FocusTargets[currentTargetIndex].position);
                        DemoScenes.Instance.SetDOFImmediate(focusDistance, targetAperture);
                        _cameraController.AutoDOFTarget = previousAutoDOFTarget;
                    }
                }

                SetDescriptionText(UseTargetPositionForDescription ? Targets[currentTargetIndex].name : FocusTargets[currentTargetIndex].name);
            }
        }

        protected override void ChangeVisibleTarget()
        {
            if (isTransitioning)
            {
                Debug.Log("Transition in progress, skipping ChangeVisibleTarget.");
                return;
            }

            if (Items.Count == 0 || FocusTargets.Count == 0 || Targets.Count == 0) return;

            currentVisibleTargetIndex = (currentVisibleTargetIndex + 1) % Items.Count;

            for (int i = 0; i < Items.Count; i++)
            {
                int targetIndex = (i + currentVisibleTargetIndex) % Items.Count;
                GameObject targetTransform = Items[targetIndex];
                targetTransform.SetActive(i == 0);
            }

            if (Items.Count > 0)
            {
                SetDescriptionText(UseTargetPositionForDescription ? Targets[currentVisibleTargetIndex].name : Items[currentVisibleTargetIndex].name);
            }

            if (FocusOnNewTargets && !DisableCameraControl && _cameraController != null)
            {
                SetCameraToVisibleTarget();
            }
        }

        private void SetCameraToVisibleTarget()
        {
            if (currentVisibleTargetIndex < FocusTargets.Count && currentVisibleTargetIndex < Targets.Count)
            {
                float duration = GetTransitionDuration(currentVisibleTargetIndex);
               // Debug.Log($"SetCameraToVisibleTarget: Moving to Targets[{currentVisibleTargetIndex}] over {duration}s");
                MoveTargetSmoothly(Targets[currentVisibleTargetIndex].position, Targets[currentVisibleTargetIndex].rotation, duration);

                if (!_cameraController.UseManualDOF)
                {
                    // Ensure we're using global positions for both camera and target
                    Vector3 cameraPosition = _cameraController.mainCamera.transform.position;
                    Vector3 targetPosition = FocusTargets[currentVisibleTargetIndex].position;

                    // Calculate the distance between the camera and the focus target
                    float cameraTargetDistance = Vector3.Distance(cameraPosition, targetPosition);

                    float distanceRatio = Mathf.InverseLerp(minDistance, maxDistance, cameraTargetDistance);

                    //float targetAperture = Mathf.Lerp(minAperture, maxAperture, distanceRatio);
                    float adjustedRatio = Mathf.Pow(distanceRatio, 2); // Non-linear curve
                    float targetAperture = Mathf.Lerp(minAperture, maxAperture, adjustedRatio);
                    // Debug log to verify values
                   // Debug.Log($"Target: {FocusTargets[currentVisibleTargetIndex].name}, CameraPos: {cameraPosition}, TargetPos: {targetPosition}, Distance: {cameraTargetDistance}, DistanceRatio: {distanceRatio}, TargetAperture: {targetAperture}");

                    Transform previousAutoDOFTarget = _cameraController.AutoDOFTarget;
                    _cameraController.AutoDOFTarget = null;

                    if (UseTransitionDurationForDOF)
                    {
                        Vector3 cameraEndPosition = Targets[currentVisibleTargetIndex].position + dummyTargetOffset;
                        DemoScenes.Instance.SetDOFToTarget(FocusTargets[currentVisibleTargetIndex], cameraEndPosition, targetAperture, duration);
                        _cameraController.SetDOFTransitioning(true);
                        StartCoroutine(ResetDOFTransitioning(duration, previousAutoDOFTarget));
                    }
                    else
                    {
                        float focusDistance = Vector3.Distance(_cameraController.mainCamera.transform.position, FocusTargets[currentVisibleTargetIndex].position);
                        DemoScenes.Instance.SetDOFImmediate(focusDistance, targetAperture);
                        _cameraController.AutoDOFTarget = previousAutoDOFTarget;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Index {currentVisibleTargetIndex} is out of bounds for FocusTargets ({FocusTargets.Count}) or Targets ({Targets.Count}).");
            }
        }

        protected new void MoveTargetSmoothly(Vector3 newPosition, Quaternion newRotation, float duration)
        {
            if (moveTargetCoroutine != null)
            {
                StopCoroutine(moveTargetCoroutine);
            }
            moveTargetCoroutine = StartCoroutine(SmoothMoveTarget(newPosition + dummyTargetOffset, newRotation, duration));
        }

        private IEnumerator SmoothMoveTarget(Vector3 newPosition, Quaternion newRotation, float duration)
        {
            isTransitioning = true;
            Vector3 startPosition = dummyTarget.position;
            Vector3 endPosition = newPosition;
            Quaternion startRotation = _cameraController.mainCamera.transform.rotation;
            Quaternion endRotation = UseTargetTransformRotation ? newRotation : startRotation;

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                t = Mathf.SmoothStep(0f, 1f, t);
                dummyTarget.position = Vector3.Lerp(startPosition, endPosition, t);

                if (UseTargetTransformRotation)
                {
                    _cameraController.mainCamera.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            dummyTarget.position = endPosition;
            if (UseTargetTransformRotation)
            {
                _cameraController.mainCamera.transform.rotation = endRotation;
            }

            IsMovingTarget = false;
            isTransitioning = false;

            moveTargetCoroutine = null;
        }

        private IEnumerator ResetDOFTransitioning(float duration, Transform autoDOFTargetToRestore = null)
        {
            yield return new WaitForSeconds(duration);
            _cameraController.SetDOFTransitioning(false);
            if (autoDOFTargetToRestore != null)
            {
                _cameraController.AutoDOFTarget = autoDOFTargetToRestore;
            }
        }
    }
}