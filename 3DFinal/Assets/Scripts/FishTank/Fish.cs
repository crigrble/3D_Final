using UnityEngine;

/// <summary>
/// 魚的碰撞偵測腳本 (最終完美版)
/// 修正：
/// 1. 加入「初始位置自動適應」：魚會從您擺放的地方開始游，不會再被強制傳送
/// 2. 移除互斥力，保持單純隨機轉向
/// 3. 保留所有自然游動優化
///
/// 【新增】
/// 4. 支援「同一條魚只算一次分」（可開關）
/// </summary>
public class Fish : MonoBehaviour
{
    [Header("🐟 縮放設定")]
    [Tooltip("世界縮放比例 (例如 0.062)。這只會影響「速度」和「物理力道」，不會再縮小您的邊界框。")]
    [SerializeField] private float worldScale = 0.062f;

    [Header("基礎屬性")]
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private string handTag = "Hand";
    [SerializeField] private Color touchColor = Color.yellow;
    [SerializeField] private float colorDuration = 0.3f;

    [Tooltip("連續碰撞時的加分冷卻（秒）。若你開啟「只算一次」，此冷卻主要只影響觸碰特效/受驚嚇的觸發頻率。")]
    [SerializeField] private float scoreCooldown = 1.0f;

    [Header("計分規則（新增）")]
    [Tooltip("勾選後：同一條魚在本局遊戲只會加分一次。")]
    [SerializeField] private bool scoreOnlyOnce = true;

    [Tooltip("如果勾選：即使已經得過分，仍可觸發變色/受驚嚇（但不加分）。")]
    [SerializeField] private bool allowTouchEffectsAfterScored = true;

    [Header("移動設定")]
    [SerializeField] private bool enableMovement = true;

    [Tooltip("標準巡航速度")]
    [SerializeField] private float baseMoveSpeed = 2.5f;

    [Tooltip("轉彎靈敏度 (數值越小，轉彎半徑越大，看起來越自然)")]
    [SerializeField] private float turnSpeed = 0.6f;

    [Tooltip("改變方向的頻率 (秒)")]
    [SerializeField] private float changeDirectionInterval = 3.0f;

    [Header("游泳範圍 (絕對 Local 座標)")]
    [Tooltip("請拖入魚缸中心點 (Empty Object)")]
    [SerializeField] private Transform swimAnchor;

    [Tooltip("紅框的最小角落 (請看 Scene 視窗調整)")]
    [SerializeField] private Vector3 minBounds = new Vector3(-5f, -2f, -5f);

    [Tooltip("紅框的最大角落 (請看 Scene 視窗調整)")]
    [SerializeField] private Vector3 maxBounds = new Vector3(5f, 2f, 5f);

    [Tooltip("碰到牆壁前的緩衝距離")]
    [SerializeField] private float boundaryBuffer = 0.1f;

    [Header("自然感細節")]
    [SerializeField] private Vector3 forwardOffset = new Vector3(0, 180, 0);
    [SerializeField] private bool enableSpeedVariation = true;
    [SerializeField] private float maxTiltAngle = 8f;

    [Header("受驚嚇效果")]
    [SerializeField] private float scaredSpeedMultiplier = 2.5f;
    [SerializeField] private float scaredDuration = 1.5f;

    [Header("調試設定")]
    [SerializeField] private bool enableDebug = true;

    // --- 內部變數 ---
    private Renderer fishRenderer;
    private Color originalColor;
    private float touchTime = -1f;
    private bool isTouched = false;
    private float lastScoreTime = -999f;

    // 【新增】這條魚是否已經給過分數
    private bool hasScored = false;

    private Vector3 currentVelocity;
    private Vector3 targetDirection;
    private float nextChangeTime = 0f;
    private bool isScared = false;
    private float scaredEndTime = 0f;
    private float speedOffset;

    private void Start()
    {
        fishRenderer = GetComponent<Renderer>();
        if (fishRenderer != null) originalColor = fishRenderer.material.color;

        if (swimAnchor == null)
        {
            swimAnchor = transform.parent != null ? transform.parent : transform;
        }

        // --- 關鍵修正：自動調整邊界以包含初始位置 ---
        Vector3 startLocalPos = swimAnchor.InverseTransformPoint(transform.position);
        bool boundsAdjusted = false;

        if (startLocalPos.x < minBounds.x + boundaryBuffer) { minBounds.x = startLocalPos.x - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.x > maxBounds.x - boundaryBuffer) { maxBounds.x = startLocalPos.x + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        if (startLocalPos.y < minBounds.y + boundaryBuffer) { minBounds.y = startLocalPos.y - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.y > maxBounds.y - boundaryBuffer) { maxBounds.y = startLocalPos.y + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        if (startLocalPos.z < minBounds.z + boundaryBuffer) { minBounds.z = startLocalPos.z - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.z > maxBounds.z - boundaryBuffer) { maxBounds.z = startLocalPos.z + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        if (boundsAdjusted && enableDebug)
        {
            Debug.Log($"📏 {gameObject.name} 初始位置在邊界外，已自動擴展游泳範圍以包含初始點。");
        }
        // ---------------------------------------------

        speedOffset = Random.Range(0f, 100f);

        ChangeTargetDirection();
        nextChangeTime = Time.time + Random.Range(0f, changeDirectionInterval);

        if (currentVelocity == Vector3.zero)
        {
            currentVelocity = transform.forward * baseMoveSpeed * worldScale;
        }
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
        {
            currentVelocity = Random.onUnitSphere * targetSpeed * 0.1f;
        }

        Vector3 nextPos = transform.position + currentVelocity * dt;

        Vector3 localPos = swimAnchor.InverseTransformPoint(nextPos);
        bool hitBound = CheckBounds(ref localPos, ref currentVelocity);

        if (hitBound)
        {
            targetDirection = currentVelocity.normalized;
        }

        Vector3 finalWorldPos = swimAnchor.TransformPoint(localPos);
        transform.position = finalWorldPos;

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
        {
            velocity = swimAnchor.TransformDirection(localVel);
        }

        return hit;
    }

    private void ChangeTargetDirection()
    {
        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y *= 0.3f;
        targetDirection = randomDir.normalized;
    }

    private void OnTriggerEnter(Collider other) { HandleCollision(other.gameObject); }
    private void OnCollisionEnter(Collision collision) { HandleCollision(collision.gameObject); }

    private void HandleCollision(GameObject other)
    {
        if (other.CompareTag(handTag))
        {
            OnTouched();
        }
        else if (other.CompareTag("Fish"))
        {
            ChangeTargetDirection();
        }
    }

    public void OnTouched()
    {
        if (!HandCollisionDetector.IsHandVisible) return;

        // 冷卻（避免連續碰撞洗事件）
        if (Time.time - lastScoreTime < scoreCooldown) return;
        lastScoreTime = Time.time;

        // --- 計分邏輯（新增）---
        bool canScore = true;
        if (scoreOnlyOnce && hasScored) canScore = false;

        if (canScore)
        {
            if (enableDebug) Debug.Log($"✨ {gameObject.name} 獲得 {scoreValue} 分");

            if (GameManager_fish.Instance != null)
                GameManager_fish.Instance.AddScore(scoreValue);

            hasScored = true;
        }
        else
        {
            if (enableDebug) Debug.Log($"(不計分) {gameObject.name} 已經計過分");
        }

        // --- 觸碰特效/受驚嚇（可選：已得分後仍要不要做特效）---
        if (!canScore && !allowTouchEffectsAfterScored)
            return;

        if (fishRenderer != null)
        {
            fishRenderer.material.color = touchColor;
            isTouched = true;
            touchTime = Time.time;
        }

        isScared = true;
        scaredEndTime = Time.time + scaredDuration;

        targetDirection = Random.onUnitSphere;
        currentVelocity = targetDirection * baseMoveSpeed * scaredSpeedMultiplier * worldScale;
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
    }
}
