using UnityEngine;

/// <summary>
/// Fish (Clean Final)
/// - 只保留單一 OnTriggerEnter（避免重複方法簽名導致計分邏輯被覆蓋）
/// - 支援：只算一次 / 冷卻 / 觸碰變色 / 受驚嚇加速 / 隨機游動 / 邊界限制
/// - 可選：手可見性 gate（避免 tracking 掉時誤計分）
/// - 可選：相機角度檢查（確保玩家在摸魚狀態才能計分）
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

    [Header("計分規則")]
    [Tooltip("勾選後：同一條魚在本局只會加分一次。")]
    [SerializeField] private bool scoreOnlyOnce = false;  // 改為 false，允許每條魚多次計分
    [Tooltip("若勾選：即使已計分過，也允許觸碰特效/受驚嚇（但不加分）。")]
    [SerializeField] private bool allowTouchEffectsAfterScored = true;

    [Header("相機角度檢查")]
    [Tooltip("勾選後：只有當主攝影機 rotationY < -50 度時才能計分（確保玩家在摸魚狀態）")]
    [SerializeField] private bool requireCameraAngle = true;
    [Tooltip("允許計分的最大相機角度（rotationY 必須小於此值才能計分）")]
    [SerializeField] private float maxCameraAngleForScore = -50f;
    [Tooltip("主攝影機 Transform（留空會自動尋找 Camera.main）")]
    [SerializeField] private Transform mainCamera;

    [Header("手可見性 gate（建議先關閉用來排錯）")]
    [Tooltip("勾選後：只有 HandCollisionDetector.IsHandVisible=true 才允許觸碰邏輯（避免 tracking 掉時誤計）。")]
    [SerializeField] private bool requireHandVisible = false;

    [Header("觸碰特效")]
    [SerializeField] private Color touchColor = Color.yellow;
    [SerializeField] private float colorDuration = 0.3f;

    [Header("移動設定")]
    [SerializeField] private bool enableMovement = true;
    [Tooltip("標準巡航速度")]
    [SerializeField] private float baseMoveSpeed = 2.5f;
    [Tooltip("轉彎靈敏度（越小越自然）")]
    [SerializeField] private float turnSpeed = 0.6f;
    [Tooltip("改變方向頻率（秒）")]
    [SerializeField] private float changeDirectionInterval = 3.0f;

    [Header("游泳範圍（Local）")]
    [Tooltip("魚缸中心點（Empty）。不填會用 parent，沒有 parent 就用自己。")]
    [SerializeField] private Transform swimAnchor;
    [SerializeField] private Vector3 minBounds = new Vector3(-5f, -2f, -5f);
    [SerializeField] private Vector3 maxBounds = new Vector3(5f, 2f, 5f);
    [Tooltip("碰到牆前緩衝距離")]
    [SerializeField] private float boundaryBuffer = 0.1f;

    [Header("自然感細節")]
    [SerializeField] private Vector3 forwardOffset = new Vector3(0, 180, 0);
    [SerializeField] private bool enableSpeedVariation = true;
    [SerializeField] private float maxTiltAngle = 8f;

    [Header("受驚嚇效果")]
    [SerializeField] private float scaredSpeedMultiplier = 2.5f;
    [SerializeField] private float scaredDuration = 1.5f;

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

    private void Start()
    {
        // 檢查 Collider 設置
        CheckColliderSetup();

        // 自動尋找主攝影機
        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

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

        ChangeTargetDirection();
        nextChangeTime = Time.time + Random.Range(0f, changeDirectionInterval);

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

        // 可選：魚魚互撞就換方向
        if (other.CompareTag("Fish"))
        {
            ChangeTargetDirection();
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

        // 2) 相機角度檢查：只有摸魚狀態才能計分
        if (requireCameraAngle)
        {
            if (mainCamera == null)
            {
                if (Camera.main != null)
                {
                    mainCamera = Camera.main.transform;
                }
                else
                {
                    if (enableDebug) Debug.LogWarning($"[🐟 Fish] ⚠️ 找不到主攝影機，跳過角度檢查");
                }
            }

            if (mainCamera != null)
            {
                float cameraAngleY = NormalizeSignedAngle(mainCamera.eulerAngles.y);
                bool isSlacking = cameraAngleY < maxCameraAngleForScore;

                if (enableDebug)
                {
                    Debug.Log($"[🐟 Fish] 相機角度檢查：rotationY = {cameraAngleY:F1}° (需要 < {maxCameraAngleForScore}°)");
                }

                if (!isSlacking)
                {
                    if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ 相機角度不符合摸魚條件（{cameraAngleY:F1}° >= {maxCameraAngleForScore}°），無法計分");
                    // 注意：這裡不 return，因為可能還需要觸發特效
                }
            }
        }

        // 3) 冷卻：避免事件洗爆
        float timeSinceLastScore = Time.time - lastScoreTime;
        if (timeSinceLastScore < scoreCooldown)
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ 冷卻中：還需等待 {scoreCooldown - timeSinceLastScore:F2} 秒");
            return;
        }
        lastScoreTime = Time.time;

        // 4) 計分判斷：只算一次
        bool canScore = true;
        if (scoreOnlyOnce && hasScored)
        {
            canScore = false;
            if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ 此魚已計過分（scoreOnlyOnce = true）");
        }

        // 5) 檢查是否滿足計分條件（相機角度 + 只算一次檢查）
        bool canActuallyScore = canScore;
        
        // 如果啟用了相機角度檢查，需要再次確認
        if (requireCameraAngle && mainCamera != null)
        {
            float cameraAngleY = NormalizeSignedAngle(mainCamera.eulerAngles.y);
            canActuallyScore = canScore && (cameraAngleY < maxCameraAngleForScore);
            
            if (!canActuallyScore && canScore)
            {
                if (enableDebug) Debug.Log($"[🐟 Fish] ⚠️ 相機角度不符合摸魚條件，無法計分");
            }
        }

        if (canActuallyScore)
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

            // 只有在 scoreOnlyOnce = true 時才設置 hasScored
            // 這樣即使 prefab 中設置錯誤，也不會影響多次計分
            if (scoreOnlyOnce)
            {
                hasScored = true;
            }
        }
        else
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] (不計分) {gameObject.name} 已經計過分");
        }

        // 6) 已得分後要不要還有特效/受驚嚇
        if (!canActuallyScore && !allowTouchEffectsAfterScored)
        {
            if (enableDebug) Debug.Log($"[🐟 Fish] 跳過特效（已計分且 allowTouchEffectsAfterScored = false）");
            return;
        }

        // 7) 觸碰變色
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

        // 8) 受驚嚇：加速 + 換方向
        isScared = true;
        scaredEndTime = Time.time + scaredDuration;

        targetDirection = Random.onUnitSphere;
        currentVelocity = targetDirection * baseMoveSpeed * scaredSpeedMultiplier * worldScale;
        
        if (enableDebug) Debug.Log($"[🐟 Fish] ✅ 受驚嚇效果已觸發，加速逃離");
    }

    private void HandleMovement()
    {
        float dt = Time.deltaTime;

        if (isScared && Time.time >= scaredEndTime) isScared = false;

        if (!isScared && Time.time >= nextChangeTime)
        {
            ChangeTargetDirection();
            nextChangeTime = Time.time + changeDirectionInterval + Random.Range(-1.0f, 1.0f);
        }

        float targetSpeed = baseMoveSpeed * worldScale;
        if (isScared) targetSpeed *= scaredSpeedMultiplier;

        if (enableSpeedVariation && !isScared)
        {
            float wave = Mathf.Sin(Time.time * 3f + speedOffset);
            targetSpeed *= (1.0f + wave * 0.2f);
        }

        Vector3 desiredVelocity = targetDirection * targetSpeed;

        float steerRate = turnSpeed;
        if (isScared) steerRate *= 2f;

        currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, dt * steerRate);

        if (currentVelocity.sqrMagnitude < 0.0001f)
            currentVelocity = Random.onUnitSphere * targetSpeed * 0.1f;

        Vector3 nextPos = transform.position + currentVelocity * dt;

        Vector3 localPos = swimAnchor.InverseTransformPoint(nextPos);
        bool hitBound = CheckBounds(ref localPos, ref currentVelocity);

        if (hitBound)
            targetDirection = currentVelocity.normalized;

        transform.position = swimAnchor.TransformPoint(localPos);

        // 面向速度方向（含自然傾斜）
        if (currentVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 horizontalDir = currentVelocity;
            horizontalDir.y *= 0.5f;

            if (horizontalDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalDir.normalized) * Quaternion.Euler(forwardOffset);

                float turnBanking = -Vector3.SignedAngle(transform.forward, currentVelocity.normalized, Vector3.up);
                turnBanking = Mathf.Clamp(turnBanking, -maxTiltAngle, maxTiltAngle);

                float pitch = -currentVelocity.y * 200f * worldScale;
                pitch = Mathf.Clamp(pitch, -maxTiltAngle, maxTiltAngle);

                Quaternion tiltRot = Quaternion.Euler(pitch, 0, turnBanking);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot * tiltRot, dt * turnSpeed);
            }
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

    private void ChangeTargetDirection()
    {
        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y *= 0.3f;
        targetDirection = randomDir.normalized;
    }

    /// <summary>
    /// 將角度正規化到 -180~180 度範圍
    /// </summary>
    static float NormalizeSignedAngle(float angleDeg)
    {
        angleDeg %= 360f;
        if (angleDeg > 180f) angleDeg -= 360f;
        return angleDeg;
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
