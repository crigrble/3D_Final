using UnityEngine;

/// <summary>
/// Fish (Clean Final)
/// - 只保留單一 OnTriggerEnter（避免重複方法簽名導致計分邏輯被覆蓋）
/// - 支援：只算一次 / 冷卻 / 觸碰變色 / 受驚嚇加速 / 隨機游動 / 邊界限制
/// - 可選：手可見性 gate（避免 tracking 掉時誤計分）
/// </summary>
public class Fish : MonoBehaviour
{
    [Header("🐟 縮放設定")]
    [Tooltip("世界縮放比例 (例如 0.062)。只影響速度/力道等數值，不會縮小邊界框。")]
    [SerializeField] private float worldScale = 0.062f;

    [Header("計分")]
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private string handTag = "Hand";
    [Tooltip("連續碰撞時加分冷卻（秒）。若開啟「只算一次」，冷卻主要影響觸碰特效/受驚嚇頻率。")]
    [SerializeField] private float scoreCooldown = 1.0f;

    [Header("音效")]
    [Tooltip("得分時播放的音效（getPoint）")]
    [SerializeField] private AudioClip getPointSfx = null;
    [SerializeField] [Range(0f,1f)] private float sfxVolume = 1.0f;

    [Header("計分規則")]
    [Tooltip("勾選後：同一條魚在本局只會加分一次。")]
    [SerializeField] private bool scoreOnlyOnce = false;  // 改為 false，允許每條魚多次計分
    [Tooltip("若勾選：即使已計分過，也允許觸碰特效/受驚嚇（但不加分）。")]
    [SerializeField] private bool allowTouchEffectsAfterScored = true;

    [Header("手可見性 gate（建議先關閉用來排錯）")]
    [Tooltip("勾選後：只有 HandCollisionDetector.IsHandVisible=true 才允許觸碰邏輯（避免 tracking 掉時誤計）。")]
    [SerializeField] private bool requireHandVisible = false;

    [Header("觸碰特效")]
    [SerializeField] private Color touchColor = Color.yellow;
    [SerializeField] private float colorDuration = 0.3f;

    [Header("移動設定")]
    [SerializeField] private bool enableMovement = true;
    [Tooltip("標準巡航速度")]
    [SerializeField] private float baseMoveSpeed = 2.0f;
    [Tooltip("轉彎靈敏度（越小越自然）")]
    [SerializeField] private float turnSpeed = 1.5f;
    [Tooltip("微調方向頻率（秒）- 小角度調整")]
    [SerializeField] private float microAdjustInterval = 0.5f;
    [Tooltip("大轉向頻率（秒）- 明顯改變方向")]
    [SerializeField] private float majorTurnInterval = 3.0f;
    [Tooltip("大轉向機率（0-1）")]
    [SerializeField] private float majorTurnChance = 0.4f;

    [Header("游泳範圍（Local）")]
    [Tooltip("魚缸中心點（Empty）。不填會用 parent，沒有 parent 就用自己。")]
    [SerializeField] private Transform swimAnchor;
    [SerializeField] private Vector3 minBounds = new Vector3(-12.3f, -4f, -1f);
    [SerializeField] private Vector3 maxBounds = new Vector3(14f, 5f, 8f);
    [Tooltip("碰到牆前緩衝距離")]
    [SerializeField] private float boundaryBuffer = 0.3f;

    [Header("自然感細節")]
    [SerializeField] private Vector3 forwardOffset = new Vector3(0, 180, 0);
    [SerializeField] private bool enableSpeedVariation = true;
    [SerializeField] private float maxTiltAngle = 25f;
    [Tooltip("垂直移動限制（越小越水平）")]
    [SerializeField] private float verticalDriftLimit = 0.12f;
    [Tooltip("方向平滑過渡時間")]
    [SerializeField] private float directionSmoothTime = 0.8f;
    [Tooltip("上下浮動振幅")]
    [SerializeField] private float bobbingAmplitude = 0.08f;
    [Tooltip("上下浮動速度")]
    [SerializeField] private float bobbingSpeed = 2.0f;
    [Tooltip("明顯向上/向下游動的機率（0-1）")]
    [SerializeField] private float verticalSwimChance = 0.3f;

    [Header("受驚嚇效果")]
    [SerializeField] private float scaredSpeedMultiplier = 2.0f;
    [SerializeField] private float scaredDuration = 1.2f;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;
    [Tooltip("只要進 Trigger 就印（用於排錯）；正式可關。")]
    [SerializeField] private bool debugLogTriggerNames = true;  // 改為 true 以便排錯

    // --- 內部狀態 ---
    private Renderer fishRenderer;
    private Color originalColor;
    private bool isTouched = false;
    private float touchTime = -1f;

    private float lastScoreTime = -999f;
    private bool hasScored = false;

    private Vector3 currentVelocity;
    private Vector3 targetDirection;
    private float nextChangeTime = 0f;
    private bool isScared = false;
    private float scaredEndTime = 0f;
    private float speedOffset;
    private float bobbingPhase; // 上下浮動相位（每條魚不同）
    private float individualSpeedMultiplier; // 每條魚的個體速度差異
    private float currentSpeedMultiplier; // 當前速度倍數（會動態變化）

    private void Start()
    {
        // 檢查 Collider 設置
        CheckColliderSetup();

        fishRenderer = GetComponent<Renderer>();
        if (fishRenderer != null) originalColor = fishRenderer.material.color;

        if (swimAnchor == null)
            swimAnchor = transform.parent != null ? transform.parent : transform;

        // 讓邊界包含初始位置（避免一開始就被夾牆/瞬移）
        Vector3 startLocalPos = swimAnchor.InverseTransformPoint(transform.position);
        bool boundsAdjusted = false;

        if (startLocalPos.x < minBounds.x + boundaryBuffer) { minBounds.x = startLocalPos.x - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.x > maxBounds.x - boundaryBuffer) { maxBounds.x = startLocalPos.x + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        if (startLocalPos.y < minBounds.y + boundaryBuffer) { minBounds.y = startLocalPos.y - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.y > maxBounds.y - boundaryBuffer) { maxBounds.y = startLocalPos.y + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        if (startLocalPos.z < minBounds.z + boundaryBuffer) { minBounds.z = startLocalPos.z - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.z > maxBounds.z - boundaryBuffer) { maxBounds.z = startLocalPos.z + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        if (boundsAdjusted && enableDebug)
            Debug.Log($"📏 {gameObject.name} 初始位置在邊界外，已自動擴展游泳範圍以包含初始點。");

        speedOffset = Random.Range(0f, 100f);
        bobbingPhase = Random.Range(0f, Mathf.PI * 2f); // 隨機相位讓每條魚浮動不同步
        
        // 每條魚有不同的基礎速度（80%-120%）
        individualSpeedMultiplier = Random.Range(0.8f, 1.2f);
        currentSpeedMultiplier = Random.Range(0.9f, 1.1f); // 初始也有些變化

        ChangeTargetDirection(true); // 初始使用大轉向
        nextChangeTime = Time.time + Random.Range(0f, microAdjustInterval);

        if (currentVelocity == Vector3.zero)
            currentVelocity = transform.forward * baseMoveSpeed * worldScale;
    }

    private void Update()
    {
        if (enableMovement) HandleMovement();

        if (isTouched && Time.time - touchTime > colorDuration)
        {
            isTouched = false;
            if (fishRenderer != null) fishRenderer.material.color = originalColor;
        }
    }

    // ✅ 單一 Trigger 入口（不要再寫第二個同簽名的方法）
    private void OnTriggerEnter(Collider other)
    {
        if (enableDebug || debugLogTriggerNames)
            Debug.Log($"[🐟 Fish Trigger] {gameObject.name} 被 {other.name} (Tag: {other.tag}) 觸碰");

        HandleHit(other.gameObject);
    }

    // 如果你未來把魚改成非 Trigger 碰撞，可保留這個
    private void OnCollisionEnter(Collision collision)
    {
        if (enableDebug)
            Debug.Log($"[🐟 Fish Collision] {gameObject.name} 與 {collision.gameObject.name} (Tag: {collision.gameObject.tag}) 碰撞");

        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject other)
    {
        if (other == null)
        {
            if (enableDebug) Debug.LogWarning($"[🐟 Fish] HandleHit: other 為 null");
            return;
        }

        string otherTag = other.tag;
        if (enableDebug)
            Debug.Log($"[🐟 Fish] {gameObject.name} 處理碰撞：{other.name} (Tag: {otherTag}, 期望: {handTag})");

        if (other.CompareTag(handTag))
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] ✅ 檢測到 Hand Tag！準備觸發 OnTouched()");
            OnTouched();
            return;
        }
        else
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ Tag 不匹配：{otherTag} != {handTag}");
        }

        // 魚魚互撞就轉向避開
        if (other.CompareTag("Fish"))
        {
            // 計算遠離對方的方向
            Vector3 awayDirection = (transform.position - other.transform.position).normalized;
            
            // 保持一些水平方向的隨機性，避免完全相反
            float randomAngle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            float currentAngle = Mathf.Atan2(awayDirection.z, awayDirection.x);
            float newAngle = currentAngle + randomAngle;
            
            float xDir = Mathf.Cos(newAngle);
            float zDir = Mathf.Sin(newAngle);
            float yDir = Random.Range(-verticalDriftLimit, verticalDriftLimit);
            
            targetDirection = new Vector3(xDir, yDir, zDir).normalized;
            
            // 立即改變當前速度方向，讓轉向更快速
            currentVelocity = Vector3.Lerp(currentVelocity, targetDirection * baseMoveSpeed * worldScale * individualSpeedMultiplier, 0.5f);
            
            if (enableDebug)
                Debug.Log($"[🐟 Fish] {gameObject.name} 與其他魚碰撞，轉向避開");
        }
    }

    public void OnTouched()
    {
        if (enableDebug) Debug.Log($"[🐟 Fish] OnTouched() 被調用 - {gameObject.name}");

        // 1) 手可見性 gate（排錯時建議先關）
        if (requireHandVisible)
        {
            bool handVisible = HandCollisionDetector.IsHandVisible;
            if (enableDebug) Debug.Log($"[🐟 Fish] 手可見性檢查：{handVisible}");
            if (!handVisible)
            {
                if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ 手不可見，跳過計分");
                return;
            }
        }

        // 2) 冷卻：避免事件洗爆
        float timeSinceLastScore = Time.time - lastScoreTime;
        if (timeSinceLastScore < scoreCooldown)
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ 冷卻中：還需等待 {scoreCooldown - timeSinceLastScore:F2} 秒");
            return;
        }
        lastScoreTime = Time.time;

        // 3) 計分判斷：只算一次
        bool canScore = true;
        if (scoreOnlyOnce && hasScored)
        {
            canScore = false;
            if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ 此魚已計過分（scoreOnlyOnce = true）");
        }

        // 全局條件：如果手模型不可見、或攝影機處於 Office 模式，則不加分（但可保留特效）
        if (!HandCollisionDetector.IsHandVisible || CameraSwitch.IsInOffice)
        {
            canScore = false;
            if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ 由於手不可見或攝影機在 Office 模式，跳過計分 (HandVisible={HandCollisionDetector.IsHandVisible}, InOffice={CameraSwitch.IsInOffice})");
        }

        if (canScore)
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] ✨ {gameObject.name} 獲得 {scoreValue} 分");

            if (GameManager_fish.Instance != null)
            {
                GameManager_fish.Instance.AddScore(scoreValue);
                if (enableDebug) Debug.Log($"[🐟 Fish] ✅ 分數已添加到 GameManager");
            }
            else
            {
                Debug.LogError($"[🐟 Fish] ❌ GameManager_fish.Instance 為 null！請確認場景中有 GameManager_fish 物件");
            }

            // 播放得分音效（如果有設定）
            if (getPointSfx != null)
            {
                AudioSource.PlayClipAtPoint(getPointSfx, transform.position, sfxVolume);
            }

            hasScored = true;
        }
        else
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] (不計分) {gameObject.name} 已經計過分");
        }

        // 4) 已得分後要不要還有特效/受驚嚇
        if (!canScore && !allowTouchEffectsAfterScored)
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] 跳過特效（已計分且 allowTouchEffectsAfterScored = false）");
            return;
        }

        // 5) 觸碰變色
        if (fishRenderer != null)
        {
            fishRenderer.material.color = touchColor;
            isTouched = true;
            touchTime = Time.time;
            if (enableDebug) Debug.Log($"[🐟 Fish] ✅ 魚變色效果已觸發");
        }
        else
        {
            if (enableDebug) Debug.LogWarning($"[🐟 Fish] ⚠️ fishRenderer 為 null，無法變色");
        }

        // 6) 受驚嚇：加速 + 換方向（但也適度限制垂直移動）
        isScared = true;
        scaredEndTime = Time.time + scaredDuration;

        // 生成主要水平的逃跑方向，但允許一定的垂直移動
        float escapeAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float escapeX = Mathf.Cos(escapeAngle);
        float escapeZ = Mathf.Sin(escapeAngle);
        float escapeY = Random.Range(-verticalDriftLimit * 1.5f, verticalDriftLimit * 1.5f);
        
        targetDirection = new Vector3(escapeX, escapeY, escapeZ).normalized;
        currentVelocity = targetDirection * baseMoveSpeed * scaredSpeedMultiplier * worldScale;
        
        if (enableDebug) Debug.Log($"[🐟 Fish] ✅ 受驚嚇效果已觸發，加速逃離");
    }

    private void HandleMovement()
    {
        float dt = Time.deltaTime;

        if (isScared && Time.time >= scaredEndTime) isScared = false;

        if (!isScared && Time.time >= nextChangeTime)
        {
            // 決定是大轉向還是小微調
            bool doMajorTurn = Random.value < majorTurnChance;
            ChangeTargetDirection(doMajorTurn);
            
            // 大轉向時有較高機會改變速度節奏，小微調時也有機會
            if ((doMajorTurn && Random.value < 0.6f) || Random.value < 0.15f)
            {
                currentSpeedMultiplier = Random.Range(0.75f, 1.4f);
            }
            
            // 設定下次轉向時間（微調較頻繁）
            float interval = doMajorTurn ? majorTurnInterval : microAdjustInterval;
            nextChangeTime = Time.time + interval + Random.Range(-0.3f, 0.3f);
        }

        // 計算目標速度（包含多層隨機變化）
        float targetSpeed = baseMoveSpeed * worldScale * individualSpeedMultiplier * currentSpeedMultiplier;
        if (isScared) targetSpeed *= scaredSpeedMultiplier;

        if (enableSpeedVariation && !isScared)
        {
            // 基礎波動（正弦波）
            float wave = Mathf.Sin(Time.time * 3f + speedOffset);
            float waveMultiplier = 1.0f + wave * 0.4f; // 增加到 ±40%
            
            // 添加額外的隨機微調（Perlin noise 效果）
            float noise = Mathf.PerlinNoise(Time.time * 0.5f + speedOffset, 0);
            float noiseMultiplier = 0.9f + noise * 0.3f; // 90%-120%
            
            targetSpeed *= waveMultiplier * noiseMultiplier;
        }

        Vector3 desiredVelocity = targetDirection * targetSpeed;

        // 平滑過渡到目標速度（自然轉彎）
        float steerRate = turnSpeed;
        if (isScared) steerRate *= 2f;

        currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, dt * steerRate);
        
        // 限制垂直速度在合理範圍內
        if (!isScared)
        {
            currentVelocity.y = Mathf.Clamp(currentVelocity.y, -0.06f, 0.06f);
        }
        else
        {
            currentVelocity.y = Mathf.Clamp(currentVelocity.y, -0.08f, 0.08f);
        }

        if (currentVelocity.sqrMagnitude < 0.0001f)
            currentVelocity = Random.onUnitSphere * targetSpeed * 0.1f;

        // 計算實際移動位置（主要速度 + 漂浮效果）
        Vector3 movementVelocity = currentVelocity;
        
        // 添加自然的上下漂浮（不影響頭部方向）
        if (!isScared)
        {
            float bobbing = Mathf.Sin(Time.time * bobbingSpeed + bobbingPhase) * bobbingAmplitude * worldScale;
            movementVelocity.y += bobbing;
        }

        Vector3 nextPos = transform.position + movementVelocity * dt;

        Vector3 localPos = swimAnchor.InverseTransformPoint(nextPos);
        bool hitBound = CheckBounds(ref localPos, ref currentVelocity);

        if (hitBound)
            targetDirection = currentVelocity.normalized;

        transform.position = swimAnchor.TransformPoint(localPos);

        // 面向速度方向（使用主要移動方向，不含漂浮效果）
        if (currentVelocity.sqrMagnitude > 0.0001f)
        {
            // 使用主要移動方向來計算旋轉（不包含bobbing的微小波動）
            Vector3 lookDirection = currentVelocity.normalized;
            
            // 計算目標旋轉（魚頭朝向移動方向）
            Quaternion baseRotation = Quaternion.LookRotation(lookDirection);
            Quaternion offsetRotation = Quaternion.Euler(forwardOffset);
            Quaternion targetRotation = baseRotation * offsetRotation;

            // 添加輕微的搖擺感（模擬魚身擺動）
            float rollAngle = Mathf.Sin(Time.time * 2f + bobbingPhase) * 7f;

            // 套用搖擺
            Vector3 finalEuler = targetRotation.eulerAngles;
            finalEuler.z += rollAngle;
            targetRotation = Quaternion.Euler(finalEuler);

            // 平滑旋轉（受驚時旋轉更快）
            float smoothRotSpeed = isScared ? turnSpeed * 2f : turnSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, dt * smoothRotSpeed);
        }
    }

    private bool CheckBounds(ref Vector3 localPos, ref Vector3 velocity)
    {
        bool hit = false;

        float minX = minBounds.x;
        float maxX = maxBounds.x;
        float minY = minBounds.y;
        float maxY = maxBounds.y;
        float minZ = minBounds.z;
        float maxZ = maxBounds.z;
        float buffer = boundaryBuffer;

        Vector3 localVel = swimAnchor.InverseTransformDirection(velocity);

        if (localPos.x < minX + buffer)
        {
            localPos.x = minX + buffer;
            if (localVel.x < 0) localVel.x *= -1;
            hit = true;
        }
        else if (localPos.x > maxX - buffer)
        {
            localPos.x = maxX - buffer;
            if (localVel.x > 0) localVel.x *= -1;
            hit = true;
        }

        if (localPos.y < minY + buffer)
        {
            localPos.y = minY + buffer;
            if (localVel.y < 0) localVel.y *= -1;
            hit = true;
        }
        else if (localPos.y > maxY - buffer)
        {
            localPos.y = maxY - buffer;
            if (localVel.y > 0) localVel.y *= -1;
            hit = true;
        }

        if (localPos.z < minZ + buffer)
        {
            localPos.z = minZ + buffer;
            if (localVel.z < 0) localVel.z *= -1;
            hit = true;
        }
        else if (localPos.z > maxZ - buffer)
        {
            localPos.z = maxZ - buffer;
            if (localVel.z > 0) localVel.z *= -1;
            hit = true;
        }

        if (hit)
            velocity = swimAnchor.TransformDirection(localVel);

        return hit;
    }

    private void ChangeTargetDirection(bool majorTurn = false)
    {
        if (majorTurn)
        {
            // 大轉向：完全隨機的新方向
            float horizontalAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float xDir = Mathf.Cos(horizontalAngle);
            float zDir = Mathf.Sin(horizontalAngle);
            
            // 決定垂直方向：有一定機率選擇明顯的向上或向下游動
            float yDir;
            if (Random.value < verticalSwimChance)
            {
                // 明顯向上或向下游動
                if (Random.value < 0.5f)
                {
                    // 向上游
                    yDir = Random.Range(0.3f, 0.6f);
                }
                else
                {
                    // 向下游
                    yDir = Random.Range(-0.6f, -0.3f);
                }
            }
            else
            {
                // 輕微垂直移動（主要水平游動）
                yDir = Random.Range(-verticalDriftLimit, verticalDriftLimit);
            }
            
            targetDirection = new Vector3(xDir, yDir, zDir).normalized;
        }
        else
        {
            // 小微調：基於當前方向做小角度調整（20-40度）
            Vector3 currentDir = targetDirection;
            if (currentDir.sqrMagnitude < 0.01f)
                currentDir = transform.forward;
            
            // 水平面上的小角度偏移
            float currentAngle = Mathf.Atan2(currentDir.z, currentDir.x);
            float angleOffset = Random.Range(-30f, 30f) * Mathf.Deg2Rad; // ±30度微調
            float newAngle = currentAngle + angleOffset;
            
            float xDir = Mathf.Cos(newAngle);
            float zDir = Mathf.Sin(newAngle);
            
            // 垂直方向也做小調整
            float yDir = currentDir.y + Random.Range(-0.05f, 0.05f);
            yDir = Mathf.Clamp(yDir, -verticalDriftLimit * 2f, verticalDriftLimit * 2f);
            
            targetDirection = new Vector3(xDir, yDir, zDir).normalized;
        }
    }

    private void CheckColliderSetup()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[🐟 Fish] ❌ {gameObject.name} 沒有 Collider 組件！請添加 Collider（BoxCollider/SphereCollider 等）");
            return;
        }

        if (!col.isTrigger)
        {
            Debug.LogWarning($"[🐟 Fish] ⚠️ {gameObject.name} 的 Collider 沒有勾選 Is Trigger！OnTriggerEnter 不會觸發。");
            if (enableDebug)
            {
                Debug.Log($"[🐟 Fish] 💡 建議：勾選 Collider 的 Is Trigger 選項");
            }
        }
        else
        {
            if (enableDebug)
                Debug.Log($"[🐟 Fish] ✅ {gameObject.name} Collider 設置正確（Is Trigger = true）");
        }

        // 檢查是否有 Rigidbody（Trigger 需要至少一方有 Rigidbody）
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            if (enableDebug)
                Debug.Log($"[🐟 Fish] ℹ️ {gameObject.name} 沒有 Rigidbody（這是正常的，只要碰撞的另一方有 Rigidbody 即可）");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (swimAnchor == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.matrix = swimAnchor.localToWorldMatrix;

        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            maxBounds.z - minBounds.z
        );

        Vector3 center = new Vector3(
            (maxBounds.x + minBounds.x) * 0.5f,
            (maxBounds.y + minBounds.y) * 0.5f,
            (maxBounds.z + minBounds.z) * 0.5f
        );

        Gizmos.DrawWireCube(center, size);

        // 繪製 Collider 範圍（如果有的話）
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.yellow;
            if (col is BoxCollider boxCol)
            {
                Gizmos.DrawWireCube(transform.position + boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
        }
    }
}
