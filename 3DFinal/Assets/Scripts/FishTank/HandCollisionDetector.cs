using UnityEngine;
using Mediapipe.Unity;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System.Collections.Generic;

public class HandCollisionDetector : MonoBehaviour
{
    [Header("相機設定")]
    [SerializeField] private Camera mainCamera;

    [Header("手部模型設定")]
    [SerializeField] private Transform handRoot;
    [SerializeField] private Renderer handRenderer;
    [SerializeField] private float hideHandDelay = 0.2f;

    [Header("碰撞偵測點（可選）")]
    [SerializeField] private bool useMultipleColliders = false;
    [SerializeField] private Transform[] colliderPoints;

    [Header("移動範圍限制")]
    public BoxCollider movementBounds;   // ⚠️ 請確認這個 BoxCollider 的 Size Z 夠大 (例如 10)
    public bool clampToBounds = true;
    public float boundsPadding = 0.02f;

    [Header("位置設定")]
    [SerializeField] private float positionScaleX = 8.0f;
    [SerializeField] private float positionScaleY = 8.0f;

    // ⚠️ 如果校正後移動太慢，把這個改大 (例如 20 或 30)
    [SerializeField] private float depthScale = 15.0f;

    [SerializeField] private float baseHandSize = 0.15f; // 會被校正功能覆蓋
    [SerializeField] private Vector3 handOffset = Vector3.zero;
    [SerializeField] private float smoothing = 0.5f;

    [Header("旋轉設定")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private bool onlyZAxisRotation = false;
    [SerializeField] private float rotationSmoothness = 0.3f;
    [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 rotationClamp = new Vector3(180f, 180f, 180f);

    [Header("調試設定")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool logPositions = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    private HandLandmarkerRunner _handLandmarkerRunner;
    private bool isReceivingData = false;
    private float lastDataTime = 0f;
    private float lastHandDetectedTime = 0f;
    private bool isHandVisible = false;
    public static bool IsHandVisible { get; private set; } = false;

    private Vector3[] landmarkWorldPositions = new Vector3[21];
    private Mediapipe.Tasks.Components.Containers.NormalizedLandmarks _latestHandLandmarks;
    private bool _hasNewData = false;
    private readonly object _dataLock = new object();

    private void Start()
    {
        _handLandmarkerRunner = GetComponent<HandLandmarkerRunner>();
        if (_handLandmarkerRunner == null) _handLandmarkerRunner = FindObjectOfType<HandLandmarkerRunner>();
        if (mainCamera == null) mainCamera = Camera.main;

        //if (handRoot != null)
        //{
        //    handOffset = handRoot.position;
        //    Debug.Log($"✅ 已自動校正手部起始點為: {handOffset}");
        //}

        ValidateBoneSetup();

        // 提醒
        if (movementBounds != null && movementBounds.size.z < 1.0f)
        {
            Debug.LogWarning("⚠️ 警告：你的水箱範圍 (BoxCollider) Z 軸太薄了！這會導致無法前後移動。請加大 BoxCollider 的 Size Z。");
        }
    }

    private void Update()
    {
        // ✅ 新增：按空白鍵校正基準深度
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CalibrateDepth();
        }

        if (_hasNewData)
        {
            lock (_dataLock)
            {
                if (_hasNewData && _latestHandLandmarks.landmarks != null)
                {
                    ProcessHandData(_latestHandLandmarks);
                    _hasNewData = false;
                }
            }
        }

        if (handRenderer != null)
        {
            bool shouldBeVisible = (Time.time - lastHandDetectedTime) < hideHandDelay;
            if (shouldBeVisible != isHandVisible)
            {
                isHandVisible = shouldBeVisible;
                handRenderer.enabled = isHandVisible;
            }
            IsHandVisible = isHandVisible;
        }

        if (isReceivingData && Time.time - lastDataTime > 2f)
        {
            isReceivingData = false;
        }
    }

    // ✅ 校正函數實作
    private void CalibrateDepth()
    {
      
        if ( _latestHandLandmarks.landmarks == null || _latestHandLandmarks.landmarks.Count < 21)

        {
            Debug.LogWarning("⚠️ 無法校正：沒有偵測到手");
            return;
        }

        var wrist = _latestHandLandmarks.landmarks[0];
        var middle = _latestHandLandmarks.landmarks[9];

        // 將當前手掌大小設為「基準值」
        baseHandSize = CalculateHandSize(wrist, middle);
        Debug.Log($"✅ 深度基準已校正！現在手掌大小 {baseHandSize:F4} 被視為「原點深度」。");
    }

    private void ValidateBoneSetup()
    {
        if (handRoot == null) Debug.LogError("❌ Hand Root 未綁定！");
    }

    public void CheckHandCollision(Mediapipe.Tasks.Components.Containers.NormalizedLandmarks handLandmarks)
    {
        if (handLandmarks.landmarks == null) return;
        lock (_dataLock)
        {
            _latestHandLandmarks = handLandmarks;
            _hasNewData = true;
        }
    }

    private void ProcessHandData(Mediapipe.Tasks.Components.Containers.NormalizedLandmarks handLandmarks)
    {
        if (handLandmarks.landmarks == null || handLandmarks.landmarks.Count < 21) return;

        if (!isReceivingData)
        {
            isReceivingData = true;
            if (enableDebug) Debug.Log("✅ 開始接收手部追蹤數據");
        }
        lastDataTime = Time.time;
        lastHandDetectedTime = Time.time;

        if (useMultipleColliders && colliderPoints != null)
        {
            ConvertLandmarksToWorld(handLandmarks);
            UpdateHandWithColliders();
        }
        else
        {
            var wristLandmark = handLandmarks.landmarks[0];
            var middleBaseLandmark = handLandmarks.landmarks[9];

            float handSize = CalculateHandSize(wristLandmark, middleBaseLandmark);
            float depthOffset = CalculateDepthFromHandSize(handSize);

            // ✅ Debug 深度資訊 (如果你發現還是不動，觀察這裡的數值)
            // if (enableDebug) Debug.Log($"Size: {handSize:F3} / DepthOffset: {depthOffset:F2}");

            Vector3 targetPos = LandmarkToWorldPosition(middleBaseLandmark, depthOffset);

            UpdateHandRootPosition(targetPos);

            if (enableRotation)
            {
                Quaternion targetRotation = CalculateHandRotation(handLandmarks);
                UpdateHandRotation(targetRotation);
            }
        }
    }

    private void ConvertLandmarksToWorld(Mediapipe.Tasks.Components.Containers.NormalizedLandmarks handLandmarks)
    {
        for (int i = 0; i < Mathf.Min(21, handLandmarks.landmarks.Count); i++)
        {
            var landmark = handLandmarks.landmarks[i];
            landmarkWorldPositions[i] = LandmarkToWorldPosition(landmark);
        }
    }

    private Vector3 LandmarkToWorldPosition(Mediapipe.Tasks.Components.Containers.NormalizedLandmark landmark, float depthOffset = 0f)
    {
        float rawX = (landmark.x - 0.5f);
        float rawY = (landmark.y - 0.5f);

        Vector3 moveX = mainCamera.transform.right * (rawX * positionScaleX);
        Vector3 moveY = mainCamera.transform.up * (-rawY * positionScaleY);
        // 強制使用世界座標的 Z 軸 (0, 0, 1) 來控制深度
        // 如果你的水箱是橫的，這裡可能要改成 Vector3.right 或 Vector3.left，先試試 forward
        Vector3 moveZ = Vector3.right * depthOffset; // 深度移動

        return handOffset + moveX + moveY + moveZ;
    }

    private void UpdateHandRootPosition(Vector3 targetPos)
    {
        if (handRoot == null) return;

        if (clampToBounds && movementBounds != null)
        {
            targetPos = ClampToBoxBounds(targetPos, movementBounds.bounds, boundsPadding);
        }

        Vector3 newPos = Vector3.Lerp(handRoot.position, targetPos, smoothing);

        Rigidbody rb = handRoot.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(newPos);
        }
        else
        {
            handRoot.position = newPos;
        }
    }

    private void UpdateHandWithColliders()
    {
        Vector3 targetWrist = landmarkWorldPositions[0];
        if (clampToBounds && movementBounds != null)
        {
            targetWrist = ClampToBoxBounds(targetWrist, movementBounds.bounds, boundsPadding);
        }
        handRoot.position = Vector3.Lerp(handRoot.position, targetWrist, smoothing);

        if (colliderPoints != null)
        {
            for (int i = 0; i < colliderPoints.Length; i++)
            {
                if (colliderPoints[i] != null && i < landmarkWorldPositions.Length)
                {
                    Vector3 p = landmarkWorldPositions[i];
                    if (clampToBounds && movementBounds != null)
                    {
                        p = ClampToBoxBounds(p, movementBounds.bounds, boundsPadding);
                    }
                    colliderPoints[i].position = p;
                }
            }
        }
    }

    private Vector3 ClampToBoxBounds(Vector3 pos, Bounds b, float pad)
    {
        return new Vector3(
            Mathf.Clamp(pos.x, b.min.x + pad, b.max.x - pad),
            Mathf.Clamp(pos.y, b.min.y + pad, b.max.y - pad),
            Mathf.Clamp(pos.z, b.min.z + pad, b.max.z - pad) // 確保這裡沒把 Z 鎖死
        );
    }

    private float CalculateHandSize(Mediapipe.Tasks.Components.Containers.NormalizedLandmark wrist,
                                    Mediapipe.Tasks.Components.Containers.NormalizedLandmark middleBase)
    {
        float dx = middleBase.x - wrist.x;
        float dy = middleBase.y - wrist.y;
        float dz = middleBase.z - wrist.z;
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private float CalculateDepthFromHandSize(float handSize)
    {
        // 如果手比基準大 (closer)，ratio < 1 -> depth < 0 (移向相機)
        // 如果手比基準小 (further)，ratio > 1 -> depth > 0 (遠離相機)
        float depthRatio = baseHandSize / handSize;
        float depth = (depthRatio - 1.0f) * depthScale;
        return depth;
    }

    private Quaternion CalculateHandRotation(Mediapipe.Tasks.Components.Containers.NormalizedLandmarks handLandmarks)
    {
        var wrist = handLandmarks.landmarks[0];
        var middleBase = handLandmarks.landmarks[9];
        var indexBase = handLandmarks.landmarks[5];

        Vector3 fingerDirection = new Vector3(middleBase.x - wrist.x, middleBase.y - wrist.y, middleBase.z - wrist.z).normalized;
        Vector3 palmSide = new Vector3(indexBase.x - wrist.x, indexBase.y - wrist.y, indexBase.z - wrist.z).normalized;
        Vector3 palmNormal = Vector3.Cross(fingerDirection, palmSide).normalized;
        palmSide = Vector3.Cross(palmNormal, fingerDirection).normalized;

        Vector3 unityUp = new Vector3(fingerDirection.x, -fingerDirection.y, -fingerDirection.z);
        Vector3 unityForward = new Vector3(-palmNormal.x, palmNormal.y, -palmNormal.z);

        if (unityForward.sqrMagnitude > 0.01f && unityUp.sqrMagnitude > 0.01f)
        {
            Quaternion cameraRot = mainCamera.transform.rotation;
            Quaternion handRot = Quaternion.LookRotation(unityForward, unityUp);
            return cameraRot * handRot * Quaternion.Euler(rotationOffset);
        }

        return handRoot.rotation;
    }

    private void UpdateHandRotation(Quaternion targetRotation)
    {
        if (handRoot == null) return;
        Quaternion newRotation;
        if (onlyZAxisRotation)
        {
            Vector3 targetEuler = targetRotation.eulerAngles;
            Vector3 currentEuler = handRoot.rotation.eulerAngles;
            float targetZ = Mathf.LerpAngle(currentEuler.z, targetEuler.z, rotationSmoothness);
            newRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, targetZ);
        }
        else
        {
            newRotation = Quaternion.Slerp(handRoot.rotation, targetRotation, rotationSmoothness);
        }

        Rigidbody rb = handRoot.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic) rb.MoveRotation(newRotation);
        else handRoot.rotation = newRotation;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        if (handRoot != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(handRoot.position, 0.05f);
        }

        if (useMultipleColliders)
        {
            Gizmos.color = gizmoColor;
            for (int i = 0; i < 21; i++)
            {
                Vector3 pos = landmarkWorldPositions[i];
                if (pos == Vector3.zero) continue;
                Gizmos.DrawSphere(pos, 0.02f);
            }
        }
    }
}