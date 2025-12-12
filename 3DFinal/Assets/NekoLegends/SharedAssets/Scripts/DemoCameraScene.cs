using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NekoLegends
{
    public class DemoCameraScene : DemoScenes
    {
        [SerializeField] protected DemoCameraController _cameraController;
        [Space]
        [SerializeField] protected Button ChangeCameraBtn, NextVisibleBtn, PrevVisibleBtn;

        [SerializeField] protected bool DisableCameraControl;
        [SerializeField] protected bool FocusOnNewTargets;

        [Header("Target Configuration")]
        [SerializeField] private bool UseSceneTargetPositions;
        [SerializeField] protected bool UseTargetTransformRotation;
        [SerializeField] protected List<Transform> Targets;

        // Single scene-level switch
        [SerializeField] private bool UseSceneCamTransformWhenNoTarget = false;

        [Space]
        [SerializeField] private Button ToggleSpotLightBtn;
        [SerializeField] private Light Spotlight;

        protected int currentTargetIndex, currentVisibleTargetIndex;

        [SerializeField] protected float transitionDuration = 1.0f;
        public bool IsMovingTarget { get; set; }

        protected Coroutine moveTargetCoroutine;
        protected virtual bool ShouldSetAutoDOFTarget()
        {
            return true;
        }
        protected Transform dummyTarget;
        [SerializeField] protected Vector3 dummyTargetOffset;

        [SerializeField] private GameObject AutoHideContainer;
        private List<GameObject> items;
        private Transform currentActiveTarget;

        void Awake()
        {
            if (UseSceneCamTransformWhenNoTarget)
            {
                // No dummy, leave camera exactly where it is
                dummyTarget = null;
                if (_cameraController != null)
                {
                    _cameraController.FreePoseWhenNoTarget = true;  // tell controller not to spawn a fallback
                    _cameraController.target = null;                // ensure it's truly targetless
                }
                return;
            }

            // Existing behavior (dummy flow)
            dummyTarget = new GameObject("CameraDummyTarget").transform;
            dummyTargetOffset = Vector3.zero;

            if (!DisableCameraControl && _cameraController != null)
            {
                _cameraController.target = dummyTarget;
            }
        }

        protected override void Start()
        {
            base.Start();

            // Initialize Targets based on UseSceneTargetPositions
            if (UseSceneTargetPositions)
            {
                if (TargetPositions != null && TargetPositions.Count > 0)
                {
                    Targets = new List<Transform>(TargetPositions);
                }
                else
                {
                    Debug.LogWarning("UseSceneTargetPositions is enabled, but TargetPositions is empty or null. Falling back to Targets list.");
                    UseSceneTargetPositions = false;
                }
            }

            if (!UseSceneCamTransformWhenNoTarget)
            {
                if (Targets == null || Targets.Count == 0)
                {
                    Targets = new List<Transform>();
                    Targets.Add(dummyTarget);
                }
                else
                {
                    dummyTarget.position = Targets[currentTargetIndex].position + dummyTargetOffset;
                }

                if (!DisableCameraControl && _cameraController != null)
                {
                    _cameraController.target = dummyTarget;
                    _cameraController.RecalculateOffset();
                }
            }

            if (AutoHideContainer != null)
            {
                items = new List<GameObject>();
                foreach (Transform child in AutoHideContainer.transform)
                {
                    items.Add(child.gameObject);
                }
            }

            InitTargets();
        }

        protected virtual void OnEnable()
        {
            if (ChangeCameraBtn)
                ChangeCameraBtn.onClick.AddListener(ChangeTarget);
            //else
                //Debug.LogWarning("ChangeCameraBtn is not assigned.");

            if (NextVisibleBtn) NextVisibleBtn.onClick.AddListener(ChangeVisibleTarget);
            if (PrevVisibleBtn) PrevVisibleBtn.onClick.AddListener(ChangeVisibleTargetBackward);
            if (ToggleSpotLightBtn && Spotlight) ToggleSpotLightBtn.onClick.AddListener(ToggleSpotLightBtnHandler);

            base.OnEnable();
        }

        protected virtual void OnDisable()
        {
            if (ChangeCameraBtn) ChangeCameraBtn.onClick.RemoveListener(ChangeTarget);
            if (NextVisibleBtn) NextVisibleBtn.onClick.RemoveListener(ChangeVisibleTarget);
            if (PrevVisibleBtn) PrevVisibleBtn.onClick.RemoveListener(ChangeVisibleTargetBackward);
            if (ToggleSpotLightBtn && Spotlight) ToggleSpotLightBtn.onClick.RemoveListener(ToggleSpotLightBtnHandler);
            base.OnDisable();
        }

        protected virtual void InitTargets()
        {
            if (Targets == null || Targets.Count == 0)
            {
                Debug.LogWarning("InitTargets: Targets list is empty. Skipping initialization.");
                return;
            }

            currentVisibleTargetIndex = 0;
            currentActiveTarget = Targets[0];

            if (currentActiveTarget != null)
            {
                currentActiveTarget.gameObject.SetActive(true);
                SetDescriptionText(currentActiveTarget.name);
            }
            else
            {
                Debug.LogWarning("Initial target is null.");
            }

            // Deactivate all other targets during initialization
            for (int i = 1; i < Targets.Count; i++)
            {
                if (Targets[i] != null)
                {
                    Targets[i].gameObject.SetActive(false);
                }
            }

            UpdateItemsVisibility();
        }

        protected virtual void ChangeTarget()
        {
            if (Targets == null || Targets.Count == 0) return;

            currentTargetIndex = (currentTargetIndex + 1) % Targets.Count;

            if (UseSceneCamTransformWhenNoTarget || DisableCameraControl)
            {
                // In free mode, don’t try to move a dummy target
                if (Targets[currentTargetIndex] != null)
                    SetDescriptionText(Targets[currentTargetIndex].name);
                return;
            }

            if (Targets[currentTargetIndex] != null)
            {
                MoveTargetSmoothly(Targets[currentTargetIndex].position, Targets[currentTargetIndex].rotation, transitionDuration, currentTargetIndex);
                SetDescriptionText(Targets[currentTargetIndex].name);
            }
            else
            {
                Debug.LogWarning($"Target at index {currentTargetIndex} is null.");
            }
        }

        // ------------------- NEXT -------------------
        protected virtual void ChangeVisibleTarget()
        {
            if (Targets == null || Targets.Count == 0) return;

            if (currentActiveTarget) currentActiveTarget.gameObject.SetActive(false);

            currentVisibleTargetIndex = (currentVisibleTargetIndex + 1) % Targets.Count;
            currentTargetIndex = currentVisibleTargetIndex;

            currentActiveTarget = Targets[currentVisibleTargetIndex];
            currentActiveTarget.gameObject.SetActive(true);
            SetDescriptionText(currentActiveTarget.name);
            UpdateItemsVisibility();

            if (FocusOnNewTargets && !DisableCameraControl)
                FocusCameraOn(currentActiveTarget);
        }

        // ----------------- PREVIOUS -----------------
        protected virtual void ChangeVisibleTargetBackward()
        {
            if (Targets == null || Targets.Count == 0) return;

            if (currentActiveTarget) currentActiveTarget.gameObject.SetActive(false);

            currentVisibleTargetIndex = (currentVisibleTargetIndex - 1 + Targets.Count) % Targets.Count;
            currentTargetIndex = currentVisibleTargetIndex;

            currentActiveTarget = Targets[currentVisibleTargetIndex];
            currentActiveTarget.gameObject.SetActive(true);
            SetDescriptionText(currentActiveTarget.name);
            UpdateItemsVisibility();

            if (!DisableCameraControl)
                FocusCameraOn(currentActiveTarget);
        }

        // ---------- helper (shared by both) ---------
        private void FocusCameraOn(Transform target)
        {
            if (target == null) return;

            if (UseSceneCamTransformWhenNoTarget)
            {
                // Free mode: don’t try to move/aim via dummy; still allow DOF target
                if (_cameraController != null) _cameraController.AutoDOFTarget = target;
                return;
            }

            _cameraController.target = dummyTarget;
            _cameraController.AutoDOFTarget = target;

            MoveTargetSmoothly(target.position, target.rotation, transitionDuration, currentTargetIndex);

            // recalc after the dummy starts moving
            _cameraController.RecalculateOffset();
        }

        private void UpdateItemsVisibility()
        {
            if (items == null || items.Count == 0) return;

            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetActive(i == currentVisibleTargetIndex);
            }
        }

        private void SetNavigationEnabled(bool enabled)
        {
            if (NextVisibleBtn) NextVisibleBtn.interactable = enabled;
            if (PrevVisibleBtn) PrevVisibleBtn.interactable = enabled;
            if (ChangeCameraBtn) ChangeCameraBtn.interactable = enabled;
        }

        private IEnumerator HideOtherItemsAfterTransition()
        {
            yield return new WaitForSeconds(transitionDuration);

            for (int i = 0; i < items.Count; i++)
            {
                if (i != currentVisibleTargetIndex)
                {
                    items[i].SetActive(false);
                }
            }
        }

        private void ToggleSpotLightBtnHandler()
        {
            Spotlight.gameObject.SetActive(!Spotlight.gameObject.activeSelf);
        }

        protected void MoveTargetSmoothly(Vector3 pos, Quaternion rot, float dur, int idx)
        {
            if (UseSceneCamTransformWhenNoTarget) return; // free mode: no dummy movement

            if (IsMovingTarget) return;            // ignore extra taps
            SetNavigationEnabled(false);           // lock UI
            moveTargetCoroutine = StartCoroutine(
                SmoothMoveTarget(pos + dummyTargetOffset, rot, dur, idx));
        }

        private IEnumerator SmoothMoveTarget(Vector3 newPosition, Quaternion newRotation, float duration, int targetIndex)
        {
            if (UseSceneCamTransformWhenNoTarget) yield break; // safety

            IsMovingTarget = true;
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

            if (ShouldSetAutoDOFTarget() && targetIndex >= 0 && targetIndex < Targets.Count && Targets[targetIndex] != null)
            {
                _cameraController.AutoDOFTarget = Targets[targetIndex];
            }
            else
            {
                Debug.LogWarning($"Cannot set AutoDOFTarget: Invalid index {targetIndex} or null target.");
            }

            moveTargetCoroutine = null;
            IsMovingTarget = false;
            SetNavigationEnabled(true);
            moveTargetCoroutine = null;
        }
    }
}
