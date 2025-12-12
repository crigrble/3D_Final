using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // Added for Button
using System.Reflection; // Added for better reflection handling
using UnityEngine.EventSystems;

namespace NekoLegends
{
    public class DemoCameraController : MonoBehaviour
    {
        [SerializeField] public Transform AutoDOFTarget;  // The target you want to keep in focus used for auto DOF

        // Scene drives this (not shown in Inspector)
        [NonSerialized] public bool FreePoseWhenNoTarget = false;

        [Header("Manual Settings")]
        [SerializeField] public bool UseManualDOF; // Toggle for manual DOF
        [SerializeField] public List<float> ManualFocusDistances = new List<float> { .4f, 5f }; // Editable list of focus distances
        [SerializeField] public List<float> ManualApertures = new List<float> { 8f, 10f }; // Editable list of aperture values
        [SerializeField] public Button ManualFocusBtn; // Button for toggling manual DOF
        private int currentFocusIndex = 0; // Tracks current manual focus/aperture index

        [Header("Camera Settings")]
        public Camera mainCamera;
        public Transform target;
        public float rotationSpeed = 1.0f;
        public float zoomSpeed = .75f;
        public float panSpeed = 1.0f;
        public float zoomInMax = 0.25f;
        public float zoomOutMax = 1f;
        public bool invertAxisY = false;

        public Vector3 cameraOffsetOverride;
        private Vector3 cameraOffset;
        private Vector3 panLastPosition;
        private int previousTouchCount = 0;

        public float touchRotationMultiplier = 0.2f;

        private bool isTransitioning = false;
        private Vector3 transitionStartOffset;
        private Vector3 transitionEndOffset;
        private float transitionDuration = 1.0f;
        private float transitionElapsed = 0f;

        [Header("Pan Limits")]
        [SerializeField] private bool enablePanLimits = false;
        [SerializeField] private float upperPanLimit = 10f;
        [SerializeField] private float lowerPanLimit = -10f;

        private bool isDOFTransitioning = false;

        // Input system detection
        private bool useNewInputSystem = false;
        private Type mouseType;
        private Type touchscreenType;
        // --- Free-pose zoom clamp state ---
        private bool freeZoomInitialized = false;
        private Vector3 freeZoomPivot;      // virtual pivot to measure zoom distance against
        private float freeZoomDistance;     // current clamped distance along camera forward to the pivot

        // Custom Touch struct to mimic old Input.Touch
        private struct CustomTouch
        {
            public TouchPhase phase;
            public Vector2 position;
            public Vector2 deltaPosition;
        }

        public void SetDOFTransitioning(bool transitioning)
        {
            isDOFTransitioning = transitioning;
        }
        private void InitFreeZoom()
        {
            // If we previously had a target, prefer that distance; otherwise pick midpoint.
            float initialDist = (cameraOffset.sqrMagnitude > 1e-6f)
                ? cameraOffset.magnitude
                : (zoomInMax + zoomOutMax) * 0.5f;

            freeZoomDistance = Mathf.Clamp(initialDist, zoomInMax, zoomOutMax);
            // Place the pivot so that the current camera position sits at 'freeZoomDistance' along forward
            freeZoomPivot = mainCamera.transform.position - mainCamera.transform.forward * freeZoomDistance;
            freeZoomInitialized = true;
        }

        void Start()
        {
            if (mainCamera == null)
            {
                Debug.LogError("Camera not assigned!");
                return;
            }

            // Only auto-create a pivot if not in free mode
            if (!target && !FreePoseWhenNoTarget)
            {
                GameObject newGameObject = new GameObject("DefaultCameraObject");
                target = newGameObject.transform;
                target.position = mainCamera.transform.position;
            }

            if (target != null)
            {
                cameraOffset = mainCamera.transform.position - target.position;
            }

            if (cameraOffsetOverride.x != 0 || cameraOffsetOverride.y != 0 || cameraOffsetOverride.z != 0)
            {
                cameraOffset = cameraOffsetOverride;
            }

            // Initialize manual DOF if no distances or apertures are set
            if (ManualFocusDistances.Count == 0) ManualFocusDistances.Add(5f);
            if (ManualApertures.Count == 0) ManualApertures.Add(5.6f);

            // Ensure focus distances and apertures lists are synchronized
            SyncManualDOFLists();

            // Initialize manual DOF state in DemoScenes
            DemoScenes.Instance.SetManualDOFState(UseManualDOF);

            // Assign button listener if ManualFocusBtn is set
            if (ManualFocusBtn != null)
            {
                ManualFocusBtn.onClick.AddListener(ToggleManualDOF);
            }

            // Detect input system
            mouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (mouseType != null)
            {
                PropertyInfo currentProp = mouseType.GetProperty("current");
                if (currentProp != null)
                {
                    object mouseInstance = currentProp.GetValue(null);
                    useNewInputSystem = mouseInstance != null;
                }
            }

            if (useNewInputSystem)
            {
                touchscreenType = Type.GetType("UnityEngine.InputSystem.Touchscreen, Unity.InputSystem");
            }
        }

        void OnDestroy()
        {
            // Clean up button listener to prevent memory leaks
            if (ManualFocusBtn != null)
            {
                ManualFocusBtn.onClick.RemoveListener(ToggleManualDOF);
            }
        }

        void Update()
        {
            // 1) detect if we’re currently over a UI element
            bool isOverUI = false;
#if !USE_NEW_INPUT
            if (Input.GetMouseButton(0) && EventSystem.current.IsPointerOverGameObject())
                isOverUI = true;
            else if (Input.touchCount > 0
                    && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                isOverUI = true;
#endif

            bool free = FreePoseWhenNoTarget && target == null;
            if (free && !freeZoomInitialized) InitFreeZoom();
            if (!free) freeZoomInitialized = false; // reset when you return to targeted mode

            // 2) Input
            if (!isOverUI)
            {
                if (free)
                {
                    // Free-pose: rotate/pan/zoom camera directly
                    HandleMouseInputFree();
                }
                else
                {
                    // Original behavior (targeted orbit/pan/zoom)
                    int touchCount = 0;

                    if (useNewInputSystem)
                    {
                        List<CustomTouch> touches = GetActiveTouches();
                        touchCount = touches.Count;
                        if (touchCount > 0)
                        {
                            HandleTouchInputNew(touches);
                        }
                        else
                        {
                            HandleMouseInput();
                        }
                    }
                    else
                    {
                        touchCount = Input.touchCount;
                        if (touchCount > 0)
                        {
                            HandleTouchInputOld();
                        }
                        else
                        {
                            HandleMouseInput();
                        }
                    }
                }
            }

            // 3) DOF
            if (!isDOFTransitioning)
            {
                if (UseManualDOF)
                {
                    ApplyManualDOF();
                }
                else
                {
                    DemoScenes.Instance.SetManualDOFState(false); // Notify DemoScenes of auto mode
                    if (AutoDOFTarget)
                    {
                        AdjustDOFForTarget(AutoDOFTarget);
                    }
                }
            }

            // 4) Place/aim camera from target (only in non-free mode)
            if (!free && target != null)
            {
                mainCamera.transform.position = target.position + cameraOffset;
                mainCamera.transform.LookAt(target.position);
            }
        }

        // Free-pose mouse input: RMB rotate, LMB pan (viewport-normalized), scroll/MMB zoom
        private void HandleMouseInputFree()
        {
            // --- ROTATE + ZOOM use mouse delta / scroll like before ---
            Vector2 mouseDelta;
            float scrollData;

            if (useNewInputSystem)
            {
                var currentProp = mouseType.GetProperty("current");
                var mouseInstance = currentProp.GetValue(null);

                var deltaProp = mouseType.GetProperty("delta");
                var deltaCtrl = deltaProp.GetValue(mouseInstance);
                var deltaRead = deltaCtrl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                mouseDelta = (Vector2)deltaRead.Invoke(deltaCtrl, null);

                var scrollProp = mouseType.GetProperty("scroll");
                var scrollCtrl = scrollProp.GetValue(mouseInstance);
                var scrollRead = scrollCtrl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                Vector2 scrollVec = (Vector2)scrollRead.Invoke(scrollCtrl, null);
                scrollData = scrollVec.y / 120f;
            }
            else
            {
                mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                scrollData = Input.GetAxis("Mouse ScrollWheel");
            }

            // --- ROTATE (RMB): yaw around world up, pitch around camera right ---
            if (IsRightMouseButtonPressed())
            {
                mainCamera.transform.Rotate(Vector3.up, mouseDelta.x * rotationSpeed, Space.World);
                mainCamera.transform.Rotate(mainCamera.transform.right, -mouseDelta.y * rotationSpeed, Space.World);
            }

            // --- ZOOM (Scroll or MMB drag up/down) ---
            float zoomAmount = scrollData * zoomSpeed;
            if (IsMiddleMouseButtonPressed())
            {
                zoomAmount += -mouseDelta.y * zoomSpeed * 0.1f;
            }
            if (Mathf.Abs(zoomAmount) > Mathf.Epsilon)
            {
                if (!freeZoomInitialized) InitFreeZoom();

                // Desired clamped distance after this zoom step
                float desired = Mathf.Clamp(freeZoomDistance + zoomAmount, zoomInMax, zoomOutMax);

                // Move only along current forward, keeping lateral position, and avoid snapping on rotation
                Vector3 P = freeZoomPivot;
                Vector3 F = mainCamera.transform.forward; // unit length
                float currentAlongFwd = Vector3.Dot(mainCamera.transform.position - P, F);
                float delta = desired - currentAlongFwd;

                mainCamera.transform.position += F * delta;
                freeZoomDistance = desired;
            }

            // --- PAN (LMB): use viewport-normalized delta (resolution independent) ---
            if (IsLeftMouseButtonPressed())
            {
                // edge detect: first frame of press sets baseline
                bool downThisFrame = useNewInputSystem
                    ? IsLeftMouseButtonDownNew()
                    : Input.GetMouseButtonDown(0);

                if (downThisFrame)
                {
                    var startPos = useNewInputSystem ? GetMousePositionNew() : (Vector3)Input.mousePosition;
                    panLastPosition = mainCamera.ScreenToViewportPoint(startPos);
                }

                var curPos = useNewInputSystem ? GetMousePositionNew() : (Vector3)Input.mousePosition;
                Vector3 currentViewport = mainCamera.ScreenToViewportPoint(curPos);
                Vector3 deltaViewport = currentViewport - panLastPosition; // ~[-1..1] across screen

                // move along camera-right and world-up (to match your original feel)
                Vector3 horiz = Vector3.Cross(mainCamera.transform.forward, Vector3.up).normalized;
                Vector3 moveDir = (deltaViewport.x * horiz) + (-deltaViewport.y * Vector3.up);
                Vector3 move = moveDir * panSpeed;

                Vector3 newPos = mainCamera.transform.position + move;
                if (enablePanLimits)
                    newPos.y = Mathf.Clamp(newPos.y, lowerPanLimit, upperPanLimit);

                Vector3 appliedMove = newPos - mainCamera.transform.position;
                mainCamera.transform.position = newPos;

                // keep the virtual pivot traveling with lateral pans
                if (freeZoomInitialized) freeZoomPivot += appliedMove;

                panLastPosition = currentViewport;

            }
        }

        public void RecalculateOffset()
        {
            if (target == null) return;
            cameraOffset = mainCamera.transform.position - target.position;
            if (cameraOffsetOverride != Vector3.zero)
                cameraOffset = cameraOffsetOverride;
        }

        // Synchronize ManualFocusDistances and ManualApertures lists
        private void SyncManualDOFLists()
        {
            int maxLength = Mathf.Min(ManualFocusDistances.Count, ManualApertures.Count);
            if (ManualFocusDistances.Count > maxLength)
            {
                ManualFocusDistances.RemoveRange(maxLength, ManualFocusDistances.Count - maxLength);
                Debug.LogWarning("ManualFocusDistances list truncated to match ManualApertures length.");
            }
            else if (ManualApertures.Count > maxLength)
            {
                ManualApertures.RemoveRange(maxLength, ManualApertures.Count - maxLength);
                Debug.LogWarning("ManualApertures list truncated to match ManualFocusDistances length.");
            }
        }

        public void ToggleManualDOF()
        {
            if (!UseManualDOF || ManualFocusDistances.Count == 0) return;

            currentFocusIndex = (currentFocusIndex + 1) % ManualFocusDistances.Count; // Loop through distances
            ApplyManualDOF();
            DemoScenes.Instance.SetManualDOFState(true); // Notify DemoScenes of manual mode
        }

        private void ApplyManualDOF()
        {
            float focusDistance = ManualFocusDistances[currentFocusIndex];
            float aperture = ManualApertures[currentFocusIndex]; // Use manual aperture
            DemoScenes.Instance.SetDOFImmediate(focusDistance, aperture);
        }

        #region Transition Logic
        public void SmoothTransitionToTargetX(Transform newTarget, float duration = 1.0f)
        {
            if (newTarget == null)
            {
                Debug.LogError("New target is null!");
                return;
            }

            if (isTransitioning)
            {
                StopCoroutine("CameraTransitionCoroutine");
            }

            transitionStartOffset = cameraOffset;
            Vector3 newOffset = new Vector3(newTarget.position.x - target.position.x, cameraOffset.y, cameraOffset.z);
            transitionEndOffset = newOffset;
            transitionDuration = duration;
            transitionElapsed = 0f;
            isTransitioning = true;
            StartCoroutine("CameraTransitionCoroutine");
        }

        private IEnumerator CameraTransitionCoroutine()
        {
            while (transitionElapsed < transitionDuration)
            {
                float t = transitionElapsed / transitionDuration;
                t = Mathf.SmoothStep(0f, 1f, t);

                cameraOffset.x = Mathf.Lerp(transitionStartOffset.x, transitionEndOffset.x, t);

                transitionElapsed += Time.deltaTime;
                yield return null;
            }

            cameraOffset.x = transitionEndOffset.x;
            isTransitioning = false;
        }
        #endregion

        public void MoveCameraSmoothly(Transform newTarget)
        {
            SmoothTransitionToTargetX(newTarget, transitionDuration);
        }

        private List<CustomTouch> GetActiveTouches()
        {
            List<CustomTouch> activeTouches = new List<CustomTouch>();
            if (touchscreenType == null) return activeTouches;

            PropertyInfo currentProp = touchscreenType.GetProperty("current");
            object tsInstance = currentProp.GetValue(null);
            if (tsInstance == null) return activeTouches;

            PropertyInfo touchesProp = touchscreenType.GetProperty("touches");
            object touchesValue = touchesProp.GetValue(tsInstance);
            Type roaType = touchesValue.GetType();
            PropertyInfo countProp = roaType.GetProperty("Count");
            int count = (int)countProp.GetValue(touchesValue);

            MethodInfo getItem = roaType.GetMethod("get_Item");

            for (int i = 0; i < count; i++)
            {
                object touchControl = getItem.Invoke(touchesValue, new object[] { i });

                // Get phase
                PropertyInfo phaseProp = touchControl.GetType().GetProperty("phase");
                object phaseControl = phaseProp.GetValue(touchControl);
                MethodInfo phaseRead = phaseControl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                int phaseInt = Convert.ToInt32(phaseRead.Invoke(phaseControl, null));
                if (phaseInt == 0) continue; // None

                TouchPhase mappedPhase;
                switch (phaseInt)
                {
                    case 1: mappedPhase = TouchPhase.Began; break;
                    case 2: mappedPhase = TouchPhase.Moved; break;
                    case 3: mappedPhase = TouchPhase.Ended; break;
                    case 4: mappedPhase = TouchPhase.Canceled; break;
                    case 5: mappedPhase = TouchPhase.Stationary; break;
                    default: continue;
                }

                // Get position
                PropertyInfo posProp = touchControl.GetType().GetProperty("position");
                object posControl = posProp.GetValue(touchControl);
                MethodInfo posRead = posControl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                Vector2 position = (Vector2)posRead.Invoke(posControl, null);

                // Get delta
                PropertyInfo deltaProp = touchControl.GetType().GetProperty("delta");
                object deltaControl = deltaProp.GetValue(touchControl);
                MethodInfo deltaRead = deltaControl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                Vector2 delta = (Vector2)deltaRead.Invoke(deltaControl, null);

                activeTouches.Add(new CustomTouch
                {
                    phase = mappedPhase,
                    position = position,
                    deltaPosition = delta
                });
            }

            return activeTouches;
        }

        private void HandleTouchInputNew(List<CustomTouch> touches)
        {
            if (touches.Count == 1)
            {
                CustomTouch touch = touches[0];

                if (previousTouchCount != 1 || touch.phase == TouchPhase.Began)
                {
                    panLastPosition = mainCamera.ScreenToViewportPoint(touch.position);
                }

                if (touch.phase == TouchPhase.Moved)
                {
                    Vector3 currentPosition = mainCamera.ScreenToViewportPoint(touch.position);
                    Vector3 deltaPosition = currentPosition - panLastPosition;

                    Vector3 horizontalDirection = Vector3.Cross(mainCamera.transform.forward, Vector3.up).normalized;

                    float adjustedDeltaY = invertAxisY ? -deltaPosition.y : deltaPosition.y;
                    Vector3 moveDirection = deltaPosition.x * horizontalDirection + adjustedDeltaY * Vector3.up;
                    Vector3 move = moveDirection * panSpeed;

                    Vector3 newPosition = target.position + move;
                    if (enablePanLimits)
                    {
                        newPosition.y = Mathf.Clamp(newPosition.y, lowerPanLimit, upperPanLimit);
                    }
                    target.position = newPosition;

                    panLastPosition = currentPosition;
                }
            }
            else if (touches.Count == 2)
            {
                CustomTouch touchZero = touches[0];
                CustomTouch touchOne = touches[1];

                if (previousTouchCount != 2)
                {
                    panLastPosition = Vector3.zero;
                }

                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

                float cameraDistance = cameraOffset.magnitude;
                cameraDistance -= (deltaMagnitudeDiff * zoomSpeed * 0.01f);
                cameraDistance = Mathf.Clamp(cameraDistance, zoomInMax, zoomOutMax);
                cameraOffset = cameraOffset.normalized * cameraDistance;

                float prevAngle = AngleBetweenTouches(touchZeroPrevPos, touchOnePrevPos);
                float currentAngle = AngleBetweenTouches(touchZero.position, touchOne.position);
                float deltaAngle = Mathf.DeltaAngle(prevAngle, currentAngle);

                float touchRotationSpeed = rotationSpeed * touchRotationMultiplier;
                Quaternion rotation = Quaternion.Euler(0, -deltaAngle * touchRotationSpeed, 0);
                cameraOffset = rotation * cameraOffset;
            }

            previousTouchCount = touches.Count;
        }

        private void HandleTouchInputOld()
        {
            if (Input.touchCount == 1)
            {
                UnityEngine.Touch touch = Input.GetTouch(0);

                if (previousTouchCount != 1 || touch.phase == TouchPhase.Began)
                {
                    panLastPosition = mainCamera.ScreenToViewportPoint(touch.position);
                }

                if (touch.phase == TouchPhase.Moved)
                {
                    Vector3 currentPosition = mainCamera.ScreenToViewportPoint(touch.position);
                    Vector3 deltaPosition = currentPosition - panLastPosition;

                    Vector3 horizontalDirection = Vector3.Cross(mainCamera.transform.forward, Vector3.up).normalized;

                    float adjustedDeltaY = invertAxisY ? -deltaPosition.y : deltaPosition.y;
                    Vector3 moveDirection = deltaPosition.x * horizontalDirection + adjustedDeltaY * Vector3.up;
                    Vector3 move = moveDirection * panSpeed;

                    Vector3 newPosition = target.position + move;
                    if (enablePanLimits)
                    {
                        newPosition.y = Mathf.Clamp(newPosition.y, lowerPanLimit, upperPanLimit);
                    }
                    target.position = newPosition;

                    panLastPosition = currentPosition;
                }
            }
            else if (Input.touchCount == 2)
            {
                UnityEngine.Touch touchZero = Input.GetTouch(0);
                UnityEngine.Touch touchOne = Input.GetTouch(1);

                if (previousTouchCount != 2)
                {
                    panLastPosition = Vector3.zero;
                }

                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

                float cameraDistance = cameraOffset.magnitude;
                cameraDistance -= (deltaMagnitudeDiff * zoomSpeed * 0.01f);
                cameraDistance = Mathf.Clamp(cameraDistance, zoomInMax, zoomOutMax);
                cameraOffset = cameraOffset.normalized * cameraDistance;

                float prevAngle = AngleBetweenTouches(touchZeroPrevPos, touchOnePrevPos);
                float currentAngle = AngleBetweenTouches(touchZero.position, touchOne.position);
                float deltaAngle = Mathf.DeltaAngle(prevAngle, currentAngle);

                float touchRotationSpeed = rotationSpeed * touchRotationMultiplier;
                Quaternion rotation = Quaternion.Euler(0, -deltaAngle * touchRotationSpeed, 0);
                cameraOffset = rotation * cameraOffset;
            }

            previousTouchCount = Input.touchCount;
        }

        private float AngleBetweenTouches(Vector2 touch1, Vector2 touch2)
        {
            return Mathf.Atan2(touch2.y - touch1.y, touch2.x - touch1.x) * Mathf.Rad2Deg;
        }

        private void HandleMouseInput()
        {
            Vector2 mouseDelta = Vector2.zero;
            float scrollData = 0f;

            if (useNewInputSystem)
            {
                PropertyInfo currentProp = mouseType.GetProperty("current");
                object mouseInstance = currentProp.GetValue(null);

                // Delta
                PropertyInfo deltaProp = mouseType.GetProperty("delta");
                object deltaControl = deltaProp.GetValue(mouseInstance);
                MethodInfo deltaRead = deltaControl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                mouseDelta = (Vector2)deltaRead.Invoke(deltaControl, null);

                // Scroll
                PropertyInfo scrollProp = mouseType.GetProperty("scroll");
                object scrollControl = scrollProp.GetValue(mouseInstance);
                MethodInfo scrollRead = scrollControl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                Vector2 scrollVec = (Vector2)scrollRead.Invoke(scrollControl, null);
                scrollData = scrollVec.y / 120f; // Normalize to approximate old system values
            }
            else
            {
                mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                scrollData = Input.GetAxis("Mouse ScrollWheel");
            }

            if (IsRightMouseButtonPressed())
            {
                Quaternion camTurnAngle = Quaternion.Euler(mouseDelta.y * rotationSpeed, mouseDelta.x * rotationSpeed, 0);
                cameraOffset = camTurnAngle * cameraOffset;
            }

            float zoomAmount = scrollData * zoomSpeed;
            if (IsMiddleMouseButtonPressed())
            {
                zoomAmount = -mouseDelta.y * zoomSpeed * 0.1f;
            }

            float cameraDistance = cameraOffset.magnitude;
            cameraDistance += zoomAmount;
            cameraDistance = Mathf.Clamp(cameraDistance, zoomInMax, zoomOutMax);
            cameraOffset = cameraOffset.normalized * cameraDistance;

            if (IsLeftMouseButtonPressed())
            {
                if ((useNewInputSystem && IsLeftMouseButtonDownNew()) || (!useNewInputSystem && Input.GetMouseButtonDown(0)))
                {
                    panLastPosition = mainCamera.ScreenToViewportPoint(useNewInputSystem ? GetMousePositionNew() : Input.mousePosition);
                }

                Vector3 currentPosition = mainCamera.ScreenToViewportPoint(useNewInputSystem ? GetMousePositionNew() : Input.mousePosition);
                Vector3 deltaPosition = currentPosition - panLastPosition;

                Vector3 horizontalDirection = Vector3.Cross(mainCamera.transform.forward, Vector3.up).normalized;

                Vector3 moveDirection = deltaPosition.x * horizontalDirection + -deltaPosition.y * Vector3.up;
                Vector3 move = moveDirection * panSpeed;

                Vector3 newPosition = target.position + move;
                if (enablePanLimits)
                {
                    newPosition.y = Mathf.Clamp(newPosition.y, lowerPanLimit, upperPanLimit);
                }
                target.position = newPosition;

                panLastPosition = currentPosition;
            }
        }

        private bool IsLeftMouseButtonDownNew()
        {
            PropertyInfo currentProp = mouseType.GetProperty("current");
            object mouseInstance = currentProp.GetValue(null);
            PropertyInfo leftButtonProp = mouseType.GetProperty("leftButton");
            object leftButton = leftButtonProp.GetValue(mouseInstance);
            PropertyInfo wasPressedThisFrameProp = leftButton.GetType().GetProperty("wasPressedThisFrame");
            return (bool)wasPressedThisFrameProp.GetValue(leftButton);
        }

        private Vector3 GetMousePositionNew()
        {
            PropertyInfo currentProp = mouseType.GetProperty("current");
            object mouseInstance = currentProp.GetValue(null);
            PropertyInfo positionProp = mouseType.GetProperty("position");
            object positionControl = positionProp.GetValue(mouseInstance);
            MethodInfo readMethod = positionControl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
            Vector2 pos = (Vector2)readMethod.Invoke(positionControl, null);
            return new Vector3(pos.x, pos.y, 0);
        }

        public void AdjustDOFForTarget(Transform autoDOFTarget)
        {
            float dynamicFocusDistance = Vector3.Distance(mainCamera.transform.position, autoDOFTarget.position);

            bool free = FreePoseWhenNoTarget && target == null;
            float distanceForRatio = free && freeZoomInitialized ? freeZoomDistance : cameraOffset.magnitude;

            // Safe, normalized ratio
            float zoomRatio = Mathf.InverseLerp(zoomInMax, zoomOutMax,
                                Mathf.Clamp(distanceForRatio, zoomInMax, zoomOutMax));

            float minAperture = 1.4f;
            float maxAperture = 16.0f;
            float currentAperture = Mathf.Lerp(minAperture, maxAperture, zoomRatio);
            DemoScenes.Instance.SetDOFImmediate(dynamicFocusDistance, currentAperture);
        }


        bool IsLeftMouseButtonPressed()
        {
            if (useNewInputSystem)
            {
                PropertyInfo currentProp = mouseType.GetProperty("current");
                object mouseInstance = currentProp.GetValue(null);
                PropertyInfo buttonProp = mouseType.GetProperty("leftButton");
                object buttonInstance = buttonProp.GetValue(mouseInstance);
                PropertyInfo isPressedProp = buttonInstance.GetType().GetProperty("isPressed");
                return (bool)isPressedProp.GetValue(buttonInstance);
            }
            else
            {
                return Input.GetMouseButton(0);
            }
        }

        bool IsMiddleMouseButtonPressed()
        {
            if (useNewInputSystem)
            {
                PropertyInfo currentProp = mouseType.GetProperty("current");
                object mouseInstance = currentProp.GetValue(null);
                PropertyInfo buttonProp = mouseType.GetProperty("middleButton");
                object buttonInstance = buttonProp.GetValue(mouseInstance);
                PropertyInfo isPressedProp = buttonInstance.GetType().GetProperty("isPressed");
                return (bool)isPressedProp.GetValue(buttonInstance);
            }
            else
            {
                return Input.GetMouseButton(2);
            }
        }

        bool IsRightMouseButtonPressed()
        {
            if (useNewInputSystem)
            {
                PropertyInfo currentProp = mouseType.GetProperty("current");
                object mouseInstance = currentProp.GetValue(null);
                PropertyInfo buttonProp = mouseType.GetProperty("rightButton");
                object buttonInstance = buttonProp.GetValue(mouseInstance);
                PropertyInfo isPressedProp = buttonInstance.GetType().GetProperty("isPressed");
                return (bool)isPressedProp.GetValue(buttonInstance);
            }
            else
            {
                return Input.GetMouseButton(1);
            }
        }
    }
}
