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
    
    [Header("位置設定")]
    [SerializeField] private float depthScale = 0.6f;  // 深度變化範圍（增大以覆蓋整個魚缸）
    [SerializeField] private float baseHandSize = 0.22f;  // 基準手掌大小（調高使深度變化更早開始）
    [SerializeField] private float smoothing = 0.5f;  // 平滑度（提高以增加靈敏度）
    
    [Header("旋轉設定")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private bool onlyZAxisRotation = true;
    [SerializeField] private float rotationSmoothness = 0.1f;  // 旋轉平滑度（降低以減少灵敏度）
    [SerializeField] private float rotationDeadzone = 3f;  // 旋轉死區（度），小於此角度的變化會被忽略
    [SerializeField] private Vector3 rotationOffset = new Vector3(90f, 180f, 0f);
    [SerializeField] private Vector3 rotationClamp = new Vector3(15f, 15f, 60f);  // Z轴旋转限制角度

    [Header("調試設定")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private bool showGizmos = true;
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

        ValidateBoneSetup();
    }

    private void Update()
    {
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
            // 使用中指根部位置，計算手掌大小
            var wristLandmark = handLandmarks.landmarks[0];  // 手腕
            var middleBaseLandmark = handLandmarks.landmarks[9]; // 中指根部
            
            // 計算手掌大小（用於深度估算）
            float handSize = CalculateHandSize(wristLandmark, middleBaseLandmark);
            
            // 基於手掌大小計算深度偏移
            float depthOffset = CalculateDepthFromHandSize(handSize);
            
            // 計算手部位置（使用中指根部）
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
    
    // 計算手掌大小（手腕到中指根部的距離）
    private float CalculateHandSize(Mediapipe.Tasks.Components.Containers.NormalizedLandmark wrist,
                                    Mediapipe.Tasks.Components.Containers.NormalizedLandmark middleBase)
    {
        float dx = middleBase.x - wrist.x;
        float dy = middleBase.y - wrist.y;
        float dz = middleBase.z - wrist.z;
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    // 基於手掌大小計算深度（手掌越大=越近，越小=越遠）
    private float CalculateDepthFromHandSize(float handSize)
    {
        // 手掌大小比例轉換為深度偏移
        float depthRatio = baseHandSize / handSize; // 比例：1.0 = 基準距離
        
        // 限制比例範圍使變化更平滑（0.5 到 2.0 之間）
        depthRatio = Mathf.Clamp(depthRatio, 0.5f, 2.0f);
        
        // 手大（近）→ depth < 0 → 往前（淺），手小（遠）→ depth > 0 → 往後（深）
        float depth = (depthRatio - 1.0f) * depthScale;
        return depth;
    }

    private Vector3 LandmarkToWorldPosition(Mediapipe.Tasks.Components.Containers.NormalizedLandmark landmark, float depthOffset = 0f)
    {
        // HandZone 舞台範圍（寫死）
        float stageMinX = 4.005f;
        float stageMaxX = 4.893f;
        float stageCenterX = 4.499f;  // 舞台中心X（預設在中央）
        float stageMinY = 0f;
        float stageMaxY = 1.361f;
        float stageMinZ = -2.461f;
        float stageMaxZ = -1.653f;
        
        // 正確映射：
        // 手左右(landmark.x) → worldZ 左右（反向）
        // 手上下(landmark.y) → worldY 上下（反向）
        // 手前後(depthOffset) → worldX 前後
        float worldX = stageCenterX + depthOffset;                             // 手大小 → 前後深度
        float worldY = Mathf.Lerp(stageMaxY, stageMinY, landmark.y);          // 手上下 → 上下（反向）
        float worldZ = Mathf.Lerp(stageMinZ, stageMaxZ, landmark.x);          // 手左右 → 左右（反向）
        
        return new Vector3(worldX, worldY, worldZ);
    }

    private void UpdateHandRootPosition(Vector3 targetPos)
    {
        if (handRoot == null) return;
        
        // 直接寫死邊界限制（根據 HandZone 的 bounds）
        // 世界座標：X=左右, Y=前後(深度), Z=上下
        targetPos = new Vector3(
            Mathf.Clamp(targetPos.x, 4.105f, 4.893f),   // X: 左右
            Mathf.Clamp(targetPos.y, 0.933f, 1.361f),   // Y: 前後(深度)
            Mathf.Clamp(targetPos.z, -2.461f, -1.653f)  // Z: 上下
        );
        
        Vector3 newPos = Vector3.Lerp(handRoot.position, targetPos, smoothing);

        // 使用 Rigidbody.MovePosition 以支持物理碰撞
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
        // 更新手腕位置
        handRoot.position = Vector3.Lerp(handRoot.position, landmarkWorldPositions[0], smoothing);
        
        // 更新額外的碰撞點位置（如果有的話）
        if (colliderPoints != null)
        {
            for (int i = 0; i < colliderPoints.Length; i++)
            {
                if (colliderPoints[i] != null && i < landmarkWorldPositions.Length)
                {
                    colliderPoints[i].position = landmarkWorldPositions[i];
                }
            }
        }
    }

    private Quaternion CalculateHandRotation(Mediapipe.Tasks.Components.Containers.NormalizedLandmarks handLandmarks)
    {
        // 使用三個關鍵點定義手掌平面
        var wrist = handLandmarks.landmarks[0];      // 手腕
        var middleBase = handLandmarks.landmarks[9]; // 中指根部
        var indexBase = handLandmarks.landmarks[5];  // 食指根部
        
        // 計算手指方向（手腕 → 中指根部）= 手指朝向
        Vector3 fingerDirection = new Vector3(
            middleBase.x - wrist.x,
            middleBase.y - wrist.y,
            middleBase.z - wrist.z
        ).normalized;
        
        // 計算手掌橫向（手腕 → 食指根部）
        Vector3 palmSide = new Vector3(
            indexBase.x - wrist.x,
            indexBase.y - wrist.y,
            indexBase.z - wrist.z
        ).normalized;
        
        // 計算手掌法向量（垂直於手掌平面）
        Vector3 palmNormal = Vector3.Cross(fingerDirection, palmSide).normalized;
        
        // 重新計算正交的橫向向量
        palmSide = Vector3.Cross(palmNormal, fingerDirection).normalized;
        
        // 轉換到 Unity 座標系（考慮 Y 軸反轉）
        Vector3 unityUp = new Vector3(fingerDirection.x, -fingerDirection.y, -fingerDirection.z);
        Vector3 unityForward = new Vector3(-palmNormal.x, palmNormal.y, -palmNormal.z);  // 修正左右方向
        
        // 建立旋轉（使用 LookRotation，Forward 和 Up）
        if (unityForward.sqrMagnitude > 0.01f && unityUp.sqrMagnitude > 0.01f)
        {
            Quaternion rotation = Quaternion.LookRotation(unityForward, unityUp);
            return rotation * Quaternion.Euler(rotationOffset);
        }
        
        return handRoot.rotation; // 如果計算失敗，保持當前旋轉
    }

    // 更新手部旋轉（平滑過渡）
    private void UpdateHandRotation(Quaternion targetRotation)
    {
        if (handRoot == null) return;
        
        Quaternion newRotation;
        
        if (onlyZAxisRotation)
        {
            // 只保留 Z 軸旋轉
            Vector3 targetEuler = targetRotation.eulerAngles;
            Vector3 currentEuler = handRoot.rotation.eulerAngles;
            
            // 反轉目標 Z 軸角度
            float invertedTargetZ = 360f - targetEuler.z;
            
            // 計算角度差異（死區檢查）
            float angleDiff = Mathf.DeltaAngle(currentEuler.z, invertedTargetZ);
            
            // 如果角度變化小於死區，不更新旋轉
            if (Mathf.Abs(angleDiff) < rotationDeadzone)
            {
                return;
            }
            
            float targetZ = Mathf.LerpAngle(currentEuler.z, invertedTargetZ, rotationSmoothness);
            
            // 將角度轉換到 -180 ~ 180 範圍
            float z = targetZ > 180f ? targetZ - 360f : targetZ;
            
            // 限制 Z 軸旋轉幅度
            z = Mathf.Clamp(z, -rotationClamp.z, rotationClamp.z);
            
            newRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, z);
        }
        else
        {
            // 完整旋轉但限制角度範圍
            Quaternion smoothRotation = Quaternion.Slerp(handRoot.rotation, targetRotation, rotationSmoothness);
            Vector3 euler = smoothRotation.eulerAngles;
            
            // 將角度轉換到 -180 ~ 180 範圍
            float x = euler.x > 180f ? euler.x - 360f : euler.x;
            float y = euler.y > 180f ? euler.y - 360f : euler.y;
            float z = euler.z > 180f ? euler.z - 360f : euler.z;
            
            // 限制旋轉幅度
            x = Mathf.Clamp(x, -rotationClamp.x, rotationClamp.x);
            y = Mathf.Clamp(y, -rotationClamp.y, rotationClamp.y);
            z = z; // Z 軸不限制
            
            newRotation = Quaternion.Euler(x, y, z);
        }
        
        // 使用 Rigidbody.MoveRotation 以支持物理碰撞
        Rigidbody rb = handRoot.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.MoveRotation(newRotation);
        }
        else
        {
            handRoot.rotation = newRotation;
        }
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
