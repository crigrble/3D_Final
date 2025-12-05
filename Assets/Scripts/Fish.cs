using UnityEngine;

/// <summary>
/// 魚的碰撞偵測腳本
/// 掛載在每條魚的物件上
/// </summary>
public class Fish : MonoBehaviour
{
    [Header("魚的屬性")]
    [SerializeField] private int scoreValue = 10; // 碰到這條魚的分數
    [SerializeField] private string handTag = "Hand"; // 手的 Tag
    
    [Header("視覺效果")]
    [SerializeField] private Color touchColor = Color.yellow; // 被碰到時的顏色
    [SerializeField] private float colorDuration = 0.3f; // 顏色持續時間
    
    [Header("加分冷卻")]
    [SerializeField] private float scoreCooldown = 1.0f; // 加分冷卻時間（秒）
    
    [Header("移動設定")]
    [SerializeField] private bool enableMovement = true; // 啟用隨機移動
    [SerializeField] private float moveSpeed = 2.0f; // 移動速度
    [SerializeField] private float changeDirectionTime = 3.0f; // 改變方向的時間間隔
    [SerializeField] private float rotationSpeed = 5.0f; // 旋轉速度
    [SerializeField] private string boundTag = "Bound"; // 邊界的 Tag
    [SerializeField] private Vector3 forwardOffset = new Vector3(0, 180, 0); // 魚的前進方向偏移（預設180度因為模型尾巴朝前）
    [SerializeField] private Vector3 minBounds = new Vector3(-10f, -5f, -10f); // 邊界最小值
    [SerializeField] private Vector3 maxBounds = new Vector3(10f, 5f, 10f); // 邊界最大值
    [SerializeField] private float boundaryBuffer = 0.5f; // 邊界緩衝區，避免卡在邊界抖動
    [SerializeField] private float maxTiltAngle = 15f; // X和Z軸最大傾斜角度
    
    [Header("受驚嚇效果")]
    [SerializeField] private float scaredSpeedMultiplier = 3.0f; // 受驚時的速度倍數
    [SerializeField] private float scaredDuration = 1.5f; // 受驚持續時間（秒）
    
    [Header("調試設定")]
    [SerializeField] private bool enableDebug = true;
    
    private Renderer fishRenderer;
    private Color originalColor;
    private float touchTime = -1f;
    private bool isTouched = false;
    private float lastScoreTime = -999f; // 上次加分的時間
    
    // 移動相關
    private Vector3 moveDirection;
    private float nextDirectionChangeTime = 0f;
    private bool isScared = false; // 是否處於受驚狀態
    private float scaredEndTime = 0f; // 受驚結束時間
    
    private void Start()
    {
        // 獲取 Renderer
        fishRenderer = GetComponent<Renderer>();
        if (fishRenderer != null)
        {
            originalColor = fishRenderer.material.color;
        }
        
        // 驗證設定
        ValidateSetup();
        
        // 初始化隨機移動方向
        if (enableMovement)
        {
            ChangeDirection();
        }
    }
    
    private void Update()
    {
        // 隨機移動
        if (enableMovement)
        {
            MoveRandomly();
        }
        
        // 恢復原始顏色
        if (isTouched && Time.time - touchTime > colorDuration)
        {
            isTouched = false;
            if (fishRenderer != null)
            {
                fishRenderer.material.color = originalColor;
            }
        }
    }
    
    /// <summary>
    /// 隨機移動魚
    /// </summary>
    private void MoveRandomly()
    {
        // 檢查受驚狀態是否結束
        if (isScared && Time.time >= scaredEndTime)
        {
            isScared = false;
        }
        
        // 檢查是否需要改變方向（受驚時不改變方向）
        if (!isScared && Time.time >= nextDirectionChangeTime)
        {
            ChangeDirection();
            nextDirectionChangeTime = Time.time + changeDirectionTime;
        }
        
        // 使用 Rigidbody.MovePosition 來移動（防止穿牆）
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 newPosition;
        
        // 計算當前速度（受驚時加速）
        float currentSpeed = isScared ? moveSpeed * scaredSpeedMultiplier : moveSpeed;
        
        if (rb != null && rb.isKinematic)
        {
            newPosition = rb.position + moveDirection * currentSpeed * Time.deltaTime;
        }
        else
        {
            newPosition = transform.position + moveDirection * currentSpeed * Time.deltaTime;
        }
        
        // 檢查邊界並反彈（使用緩衝區避免抖動）
        Vector3 currentPos = rb != null && rb.isKinematic ? rb.position : transform.position;
        bool bounced = false;
        
        // X軸邊界檢查
        if (newPosition.x <= minBounds.x + boundaryBuffer)
        {
            newPosition.x = minBounds.x + boundaryBuffer;
            if (moveDirection.x < 0)
            {
                moveDirection.x = -moveDirection.x;
                bounced = true;
            }
        }
        else if (newPosition.x >= maxBounds.x - boundaryBuffer)
        {
            newPosition.x = maxBounds.x - boundaryBuffer;
            if (moveDirection.x > 0)
            {
                moveDirection.x = -moveDirection.x;
                bounced = true;
            }
        }
        
        // Y軸邊界檢查
        if (newPosition.y <= minBounds.y + boundaryBuffer)
        {
            newPosition.y = minBounds.y + boundaryBuffer;
            if (moveDirection.y < 0)
            {
                moveDirection.y = -moveDirection.y;
                bounced = true;
            }
        }
        else if (newPosition.y >= maxBounds.y - boundaryBuffer)
        {
            newPosition.y = maxBounds.y - boundaryBuffer;
            if (moveDirection.y > 0)
            {
                moveDirection.y = -moveDirection.y;
                bounced = true;
            }
        }
        
        // Z軸邊界檢查
        if (newPosition.z <= minBounds.z + boundaryBuffer)
        {
            newPosition.z = minBounds.z + boundaryBuffer;
            if (moveDirection.z < 0)
            {
                moveDirection.z = -moveDirection.z;
                bounced = true;
            }
        }
        else if (newPosition.z >= maxBounds.z - boundaryBuffer)
        {
            newPosition.z = maxBounds.z - boundaryBuffer;
            if (moveDirection.z > 0)
            {
                moveDirection.z = -moveDirection.z;
                bounced = true;
            }
        }
        
        // 套用位置
        if (rb != null && rb.isKinematic)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
        
        // 讓魚頭朝向移動方向
        if (moveDirection != Vector3.zero)
        {
            // 反轉移動方向來計算旋轉（因為要魚頭朝前，不是尾巴）
            Vector3 lookDirection = -moveDirection; // 關鍵：反轉方向
            Quaternion baseRotation = Quaternion.LookRotation(lookDirection);
            Quaternion offsetRotation = Quaternion.Euler(forwardOffset);
            Quaternion targetRotation = baseRotation * offsetRotation;
            
            // 限制X和Z軸的旋轉角度
            Vector3 eulerAngles = targetRotation.eulerAngles;
            
            // 將角度轉換為 -180 到 180 的範圍
            float xAngle = eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x;
            float zAngle = eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z;
            
            // 限制X和Z軸角度在 ±maxTiltAngle 範圍內
            xAngle = Mathf.Clamp(xAngle, -maxTiltAngle, maxTiltAngle);
            zAngle = Mathf.Clamp(zAngle, -maxTiltAngle, maxTiltAngle);
            
            // 重建旋轉（Y軸不限制，保持原本的轉向）
            targetRotation = Quaternion.Euler(xAngle, eulerAngles.y, zAngle);
            
            if (rb != null && rb.isKinematic)
            {
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed));
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }
    
    /// <summary>
    /// 改變移動方向
    /// </summary>
    private void ChangeDirection()
    {
        moveDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
    }
    
    private void ValidateSetup()
    {
        if (enableDebug)
        {
            Debug.Log($"🐟 Fish.cs 初始化完成: {gameObject.name}, 分數值: {scoreValue}");
        }
    }
    
    // Trigger 碰撞偵測（魚的 Collider 是 Trigger 時）
    private void OnTriggerEnter(Collider other)
    {
        // 檢查是否碰到邊界 - 反彈回來
        if (other.CompareTag(boundTag))
        {
            // 反轉移動方向
            moveDirection = -moveDirection;
        }
        // 檢查是否是手（透過 Tag）
        else if (other.CompareTag(handTag))
        {
            OnTouched();
        }
    }
    
    // 物理碰撞偵測（魚的 Collider 不是 Trigger 時）
    private void OnCollisionEnter(Collision collision)
    {
        // 檢查是否碰到邊界 - 反彈回來
        if (collision.gameObject.CompareTag(boundTag))
        {
            // 反轉移動方向
            moveDirection = -moveDirection;
        }
        // 檢查是否是手（透過 Tag）
        else if (collision.gameObject.CompareTag(handTag))
        {
            OnTouched();
        }
    }
    
    /// <summary>
    /// 當被手碰到時的處理邏輯
    /// </summary>
    public void OnTouched()
    {
        // 檢查冷卻時間
        if (Time.time - lastScoreTime < scoreCooldown)
        {
            if (enableDebug)
            {
                float remainingTime = scoreCooldown - (Time.time - lastScoreTime);
                Debug.Log($"⏳ {gameObject.name} 冷卻中，還需 {remainingTime:F1} 秒");
            }
            return;
        }
        
        // 更新加分時間
        lastScoreTime = Time.time;
        
        Debug.Log($"✨ {gameObject.name} 被摸到了！獲得 {scoreValue} 分");
        
        // 加分
        if (GameManager_fish.Instance != null)
        {
            GameManager_fish.Instance.AddScore(scoreValue);
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 GameManager_fish！無法加分");
        }
        
        // 改變顏色
        if (fishRenderer != null)
        {
            fishRenderer.material.color = touchColor;
            isTouched = true;
            touchTime = Time.time;
        }
        
        // 受到驚嚇，向前加速逃跑
        isScared = true;
        scaredEndTime = Time.time + scaredDuration;
        
        // TODO: 實作其他遊戲邏輯
        // - 播放音效
        // - 播放粒子特效
        
        // 範例：
        // AudioManager.Instance?.PlayTouchSound();
        // ParticleManager.Instance?.PlayTouchEffect(transform.position);
    }
}
