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
    [SerializeField] private bool enableTailWag = true; // 是否啟用尾巴擺動
    [SerializeField] private float tailAmplitude = 0.02f; // 尾擺幅度
    [SerializeField] private float tailSpeed = 2.0f; // 尾擺速度
    
    [Header("自然游泳設定")]
    [SerializeField] private float verticalDriftLimit = 0.15f; // 垂直移動限制（越小越水平）
    [SerializeField] private float directionSmoothTime = 1.5f; // 方向平滑過渡時間
    [SerializeField] private float bobbingAmplitude = 0.1f; // 上下浮動振幅
    [SerializeField] private float bobbingSpeed = 1.5f; // 上下浮動速度
    
    [Header("調試設定")]
    [SerializeField] private bool enableDebug = true;
    
    private Renderer fishRenderer;
    private Color originalColor;
    private float touchTime = -1f;
    private bool isTouched = false;
    private float lastScoreTime = -999f; // 上次加分的時間
    
    // 移動相關
    private Vector3 moveDirection;
    private Vector3 targetDirection; // 目標方向（用於平滑過渡）
    private float nextDirectionChangeTime = 0f;
    private bool isScared = false; // 是否處於受驚狀態
    private float scaredEndTime = 0f; // 受驚結束時間
    private float bobbingPhase; // 上下浮動相位（每條魚不同）
    
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
            moveDirection = targetDirection; // 初始時直接使用目標方向
            bobbingPhase = Random.Range(0f, Mathf.PI * 2f); // 隨機相位讓每條魚浮動不同步
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
            nextDirectionChangeTime = Time.time + changeDirectionTime + Random.Range(-0.5f, 0.5f);
        }
        
        // 平滑過渡到目標方向（自然轉彎）
        if (!isScared)
        {
            moveDirection = Vector3.Lerp(moveDirection, targetDirection, Time.deltaTime / directionSmoothTime);
            moveDirection.Normalize();
        }
        
        // 使用 Rigidbody.MovePosition 來移動（防止穿牆）
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 newPosition;
        
        // 計算當前速度（受驚時加速）
        float currentSpeed = isScared ? moveSpeed * scaredSpeedMultiplier : moveSpeed;
        
        // 計算基本移動
        Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
        
        // 添加自然的上下浮動（受驚時減弱）
        if (!isScared)
        {
            float bobbing = Mathf.Sin(Time.time * bobbingSpeed + bobbingPhase) * bobbingAmplitude * Time.deltaTime;
            movement.y += bobbing;
        }
        
        if (rb != null && rb.isKinematic)
        {
            newPosition = rb.position + movement;
        }
        else
        {
            newPosition = transform.position + movement;
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
                targetDirection.x = Mathf.Abs(targetDirection.x);
                bounced = true;
            }
        }
        else if (newPosition.x >= maxBounds.x - boundaryBuffer)
        {
            newPosition.x = maxBounds.x - boundaryBuffer;
            if (moveDirection.x > 0)
            {
                moveDirection.x = -moveDirection.x;
                targetDirection.x = -Mathf.Abs(targetDirection.x);
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
                targetDirection.y = Mathf.Abs(targetDirection.y);
                bounced = true;
            }
        }
        else if (newPosition.y >= maxBounds.y - boundaryBuffer)
        {
            newPosition.y = maxBounds.y - boundaryBuffer;
            if (moveDirection.y > 0)
            {
                moveDirection.y = -moveDirection.y;
                targetDirection.y = -Mathf.Abs(targetDirection.y);
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
                targetDirection.z = Mathf.Abs(targetDirection.z);
                bounced = true;
            }
        }
        else if (newPosition.z >= maxBounds.z - boundaryBuffer)
        {
            newPosition.z = maxBounds.z - boundaryBuffer;
            if (moveDirection.z > 0)
            {
                moveDirection.z = -moveDirection.z;
                targetDirection.z = -Mathf.Abs(targetDirection.z);
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
        
        // 讓魚頭朝向移動方向（使用水平方向為主）
        if (moveDirection != Vector3.zero)
        {
            // 計算水平移動方向（用於旋轉）
            Vector3 horizontalDir = new Vector3(moveDirection.x, 0, moveDirection.z);
            if (horizontalDir.sqrMagnitude < 0.001f)
            {
                horizontalDir = transform.forward;
            }
            horizontalDir.Normalize();
            
            // 計算目標旋轉（魚頭朝向移動方向）
            // 注意：使用正向 horizontalDir，配合 forwardOffset 來調整模型朝向
            Quaternion baseRotation = Quaternion.LookRotation(horizontalDir);
            Quaternion offsetRotation = Quaternion.Euler(forwardOffset);
            Quaternion targetRotation = baseRotation * offsetRotation;
            
            // 根據垂直移動添加輕微俯仰角
            float pitchAngle = moveDirection.y * maxTiltAngle * 0.5f;
            pitchAngle = Mathf.Clamp(pitchAngle, -maxTiltAngle, maxTiltAngle);
            
            // 添加輕微的搖擺感
            float rollAngle = Mathf.Sin(Time.time * 2f + bobbingPhase) * 3f;
            
            // 套用俯仰和搖擺
            Vector3 finalEuler = targetRotation.eulerAngles;
            finalEuler.x = pitchAngle;
            finalEuler.z = rollAngle;
            targetRotation = Quaternion.Euler(finalEuler);
            
            // 平滑旋轉
            float smoothRotSpeed = isScared ? rotationSpeed * 2f : rotationSpeed;
            if (rb != null && rb.isKinematic)
            {
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothRotSpeed));
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothRotSpeed);
            }
        }
    }
    
    /// <summary>
    /// 改變移動方向（生成新的目標方向）
    /// </summary>
    private void ChangeDirection()
    {
        // 水平方向隨機（主要移動方向）
        float horizontalAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float xDir = Mathf.Cos(horizontalAngle);
        float zDir = Mathf.Sin(horizontalAngle);
        
        // 垂直方向限制（魚主要水平游泳，偶爾輕微上下）
        float yDir = Random.Range(-verticalDriftLimit, verticalDriftLimit);
        
        targetDirection = new Vector3(xDir, yDir, zDir).normalized;
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
            // 反轉移動方向和目標方向
            moveDirection = -moveDirection;
            targetDirection = moveDirection;
        }
        // 檢查是否碰到其他魚 - 隨機轉向
        else if (other.CompareTag("Fish"))
        {
            ChangeDirection();
            // 立即更新移動方向以避免持續碰撞
            moveDirection = Vector3.Lerp(moveDirection, targetDirection, 0.3f);
            moveDirection.Normalize();
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
            // 反轉移動方向和目標方向
            moveDirection = -moveDirection;
            targetDirection = moveDirection;
        }
        // 檢查是否碰到其他魚 - 隨機轉向
        else if (collision.gameObject.CompareTag("Fish"))
        {
            ChangeDirection();
            // 立即更新移動方向以避免持續碰撞
            moveDirection = Vector3.Lerp(moveDirection, targetDirection, 0.3f);
            moveDirection.Normalize();
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
        // 如果手部模型處於隱藏狀態，忽略此觸碰（不加分）
        if (!HandCollisionDetector.IsHandVisible)
        {
            if (enableDebug)
            {
                Debug.Log($" {gameObject.name} 忽略碰觸：手部模型目前為 off");
            }
            return;
        }
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
