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
    [SerializeField] private Transform handRoot; // 手部模型根節點（整個手掌）
    [SerializeField] private Renderer handRenderer; // 手部模型渲染器（用於控制可見性）
    [SerializeField] private float hideHandDelay = 0.2f; // 沒有檢測到手後多久隱藏模型
    
    [Header("碰撞偵測點（可選）")]
    [SerializeField] private bool useMultipleColliders = false; // 使用多個碰撞點
    [SerializeField] private Transform[] colliderPoints; // 額外的碰撞偵測點（例如：指尖）
    // 建議設定：[4, 8, 12, 16, 20] = 五根手指的指尖
    
    [Header("位置設定")]
    [SerializeField] private float positionScale = 25.0f; // 位置縮放比例
    [SerializeField] private float depthScale = 30.0f; // 基於手掌大小的深度縮放
    [SerializeField] private float baseHandSize = 0.15f; // 參考手掌大小（手腕到中指根部距離）
    [SerializeField] private Vector3 handOffset = Vector3.zero; // 手部位置偏移
    [SerializeField] private float smoothing = 0.5f; // 位置平滑係數（0-1，越大越快）
    
    [Header("旋轉設定")]
    [SerializeField] private bool enableRotation = true; // 啟用旋轉
    [SerializeField] private bool onlyZAxisRotation = true; // 只啟用 Z 軸旋轉（左右）
    [SerializeField] private float rotationSmoothness = 0.3f; // 旋轉平滑係數
    [SerializeField] private Vector3 rotationOffset = new Vector3(90f, 180f, 0f); // 旋轉偏移
    [SerializeField] private Vector3 rotationClamp = new Vector3(15f, 15f, 360f); // X, Y, Z 軸旋轉限制角度
    
    [Header("調試設定")]
    [SerializeField] private bool enableDebug = true; // 啟用調試模式
    [SerializeField] private bool showGizmos = true; // 顯示 Gizmos
    [SerializeField] private bool logPositions = true; // 輸出位置資訊
    [SerializeField] private int debugLandmarkIndex = 0; // 要調試的關鍵點索引（0=手腕）
    [SerializeField] private Color gizmoColor = Color.cyan;
    
    private HandLandmarkerRunner _handLandmarkerRunner;
    private bool isReceivingData = false;
    private float lastDataTime = 0f;
    private float lastHandDetectedTime = 0f;
    private bool isHandVisible = false;
    // Public static accessor other scripts can query to know whether the hand model is shown
    public static bool IsHandVisible { get; private set; } = false;
    
    // 儲存 21 個 Landmark 的世界座標
    private Vector3[] landmarkWorldPositions = new Vector3[21];
    
    // 線程安全：用於緩衝 MediaPipe 的背景線程數據
    private Mediapipe.Tasks.Components.Containers.NormalizedLandmarks _latestHandLandmarks;
    private bool _hasNewData = false;
    private readonly object _dataLock = new object();
    
    private void Start()
    {
        // 先嘗試從同一個物件取得
        _handLandmarkerRunner = GetComponent<HandLandmarkerRunner>();
        
        // 如果找不到，從場景中搜尋
        if (_handLandmarkerRunner == null)
        {
            _handLandmarkerRunner = FindObjectOfType<HandLandmarkerRunner>();
        }
        
        if (_handLandmarkerRunner == null)
        {
            Debug.LogError("❌ 找不到 HandLandmarkerRunner 組件！請確認場景中有 MediaPipe 手部追蹤物件");
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // 驗證骨骼設定
        ValidateBoneSetup();
        
        // 驗證碰撞器設定
        ValidateColliderSetup();
        
        if (enableDebug)
        {
            Debug.Log("✅ 手部骨架控制系統初始化完成");
        }
    }
    
    private void ValidateColliderSetup()
    {
        // 簡化驗證，移除多餘 log
    }
    
    private void Update()
    {
        // 在主線程處理 MediaPipe 數據
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
        
        // 控制手部模型可見性
        if (handRenderer != null)
        {
            bool shouldBeVisible = (Time.time - lastHandDetectedTime) < hideHandDelay;
            if (shouldBeVisible != isHandVisible)
            {
                isHandVisible = shouldBeVisible;
                handRenderer.enabled = isHandVisible;
            }
            // Keep static accessor in sync for other scripts like Fish
            IsHandVisible = isHandVisible;
        }
        
        // 檢測是否持續接收數據
        if (isReceivingData && Time.time - lastDataTime > 2f)
        {
            isReceivingData = false;
        }
    }
    
    private void ValidateBoneSetup()
    {
        if (handRoot == null)
        {
            Debug.LogError("❌ Hand Root 未綁定！");
        }
    }
    
    // 由 HandLandmarkerRunner 調用：接收 MediaPipe 手部數據（背景線程）
    public void CheckHandCollision(Mediapipe.Tasks.Components.Containers.NormalizedLandmarks handLandmarks)
    {
        if (handLandmarks.landmarks == null) return;
        
        // 線程安全：只儲存數據，不處理
        lock (_dataLock)
        {
            _latestHandLandmarks = handLandmarks;
            _hasNewData = true;
        }
    }
    
    // 在主線程處理手部數據
    private void ProcessHandData(Mediapipe.Tasks.Components.Containers.NormalizedLandmarks handLandmarks)
    {
        // 驗證 landmarks 數量
        if (handLandmarks.landmarks == null || handLandmarks.landmarks.Count < 21)
        {
            return;
        }
        
        // 標記接收到數據
        if (!isReceivingData)
        {
            isReceivingData = true;
            if (enableDebug)
            {
                Debug.Log("✅ 開始接收手部追蹤數據");
            }
        }
        lastDataTime = Time.time;
        lastHandDetectedTime = Time.time; // 更新檢測到手的時間
        
        // 顯示手部模型
        if (handRenderer != null && !isHandVisible)
        {
            handRenderer.enabled = true;
            isHandVisible = true;
        }
        
        // 只轉換需要的 Landmark
        if (useMultipleColliders && colliderPoints != null)
        {
            // 多點模式：轉換所有 21 個點
            ConvertLandmarksToWorld(handLandmarks);
            UpdateHandWithColliders();
        }
        else
        {
            // 單點模式：計算手掌大小並更新位置和旋轉
            var wristLandmark = handLandmarks.landmarks[0];  // 手腕
            var middleBaseLandmark = handLandmarks.landmarks[9]; // 中指根部
            
            // 計算手掌大小（用於深度估算）
            float handSize = CalculateHandSize(wristLandmark, middleBaseLandmark);
            
            // 基於手掌大小計算深度偏移
            float depthOffset = CalculateDepthFromHandSize(handSize);
            
            // 計算手部位置（使用中指根部）
            Vector3 targetPos = LandmarkToWorldPosition(middleBaseLandmark, depthOffset);
            UpdateHandRootPosition(targetPos);
            
            // 計算並更新旋轉
            if (enableRotation)
            {
                Quaternion targetRotation = CalculateHandRotation(handLandmarks);
                UpdateHandRotation(targetRotation);
            }
        }
        
        // 移除多餘的調試 log
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
        // 直接使用正規化座標映射到世界空間
        // x, y: 0-1 → 轉換成以中心為原點的座標 (-0.5 ~ 0.5)
        float x = (landmark.x - 0.5f) * positionScale;
        float y = (landmark.y - 0.5f) * positionScale;
        
        // z: 使用深度偏移（基於手掌大小計算）
        float z = depthOffset;
        
        // 返回世界座標，Z 軸基準為 0
        // 修正：x 不反轉（左右正確），y 反轉（上下正確），z 反轉（前後正確）
        return new Vector3(x, -y, -z) + handOffset;
    }
    
    private void UpdateHandRootPosition(Vector3 targetPos)
    {
        if (handRoot == null) return;
        
        Vector3 oldPos = handRoot.position;
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
        
        // 移除位置移動的 log
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
        // handSize 越大表示離相機越近（Z 應該越小，往前）
        // handSize 越小表示離相機越遠（Z 應該越大，往後）
        float depthRatio = baseHandSize / handSize; // 比例：1.0 = 基準距離
        float depth = (depthRatio - 1.0f) * depthScale; // 轉換為深度偏移
        return depth;
    }
    
    // 計算手部旋轉
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
        // fingerDirection 應該對應 Unity 的 Up 軸（因為手指朝上）
        // palmNormal 應該對應 Unity 的 Forward 軸（手掌面向方向）
        Vector3 unityUp = new Vector3(fingerDirection.x, -fingerDirection.y, -fingerDirection.z);
        Vector3 unityForward = new Vector3(-palmNormal.x, palmNormal.y, -palmNormal.z);
        
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
            
            // 只更新 Z 軸，保持 X 和 Y 不變
            float targetZ = Mathf.LerpAngle(currentEuler.z, targetEuler.z, rotationSmoothness);
            newRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, targetZ);
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
    
    private void UpdateHandWithColliders()
    {
        // 更新手腕位置
        handRoot.position = Vector3.Lerp(handRoot.position, landmarkWorldPositions[0], smoothing);
        
        // 更新額外的碰撞點位置（如果有的話）
        // 這些點可以是獨立的碰撞器，不影響手部模型本身
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
    
    // 視覺化調試
    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;
        
        // 繪製手腕位置
        if (handRoot != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(handRoot.position, 0.05f);
        }
        
        // 如果啟用多碰撞點，繪製所有 Landmark
        if (useMultipleColliders)
        {
            Gizmos.color = gizmoColor;
            for (int i = 0; i < 21; i++)
            {
                Vector3 pos = landmarkWorldPositions[i];
                if (pos == Vector3.zero) continue;
                Gizmos.DrawSphere(pos, 0.02f);
            }
            
            // 繪製手指連線
            DrawFingerBones(2, 4, Color.red);      // 大拇指
            DrawFingerBones(5, 8, Color.green);    // 食指
            DrawFingerBones(9, 12, Color.blue);    // 中指
            DrawFingerBones(13, 16, Color.yellow); // 無名指
            DrawFingerBones(17, 20, Color.magenta); // 小指
        }
        
        // 繪製額外的碰撞點
        if (colliderPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var point in colliderPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, 0.03f);
            }
        }
    }
    
    private void DrawFingerBones(int startIndex, int endIndex, Color color)
    {
        Gizmos.color = color;
        for (int i = startIndex; i < endIndex; i++)
        {
            if (landmarkWorldPositions[i] == Vector3.zero || landmarkWorldPositions[i + 1] == Vector3.zero) continue;
            Gizmos.DrawLine(landmarkWorldPositions[i], landmarkWorldPositions[i + 1]);
        }
    }
}