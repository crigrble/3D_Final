using UnityEngine;

/// <summary>
/// 魚的碰撞偵測腳本 (最終完美版)
/// 修正：
/// 1. 加入「初始位置自動適應」：魚會從您擺放的地方開始游，不會再被強制傳送
/// 2. 移除互斥力，保持單純隨機轉向
/// 3. 保留所有自然游動優化
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
    [SerializeField] private float scoreCooldown = 1.0f;

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
        // 防止魚一開始就因為超出範圍被強制傳送
        Vector3 startLocalPos = swimAnchor.InverseTransformPoint(transform.position);
        bool boundsAdjusted = false;

        // X 軸檢查與擴展
        if (startLocalPos.x < minBounds.x + boundaryBuffer) { minBounds.x = startLocalPos.x - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.x > maxBounds.x - boundaryBuffer) { maxBounds.x = startLocalPos.x + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        // Y 軸檢查與擴展
        if (startLocalPos.y < minBounds.y + boundaryBuffer) { minBounds.y = startLocalPos.y - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.y > maxBounds.y - boundaryBuffer) { maxBounds.y = startLocalPos.y + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        // Z 軸檢查與擴展
        if (startLocalPos.z < minBounds.z + boundaryBuffer) { minBounds.z = startLocalPos.z - boundaryBuffer - 0.1f; boundsAdjusted = true; }
        if (startLocalPos.z > maxBounds.z - boundaryBuffer) { maxBounds.z = startLocalPos.z + boundaryBuffer + 0.1f; boundsAdjusted = true; }

        if (boundsAdjusted && enableDebug)
        {
            Debug.Log($"📏 {gameObject.name} 初始位置在邊界外，已自動擴展游泳範圍以包含初始點。");
        }
        // ---------------------------------------------

        // 1. 初始化隨機參數
        speedOffset = Random.Range(0f, 100f);

        // 2. 隨機初始方向
        ChangeTargetDirection();

        // 3. 錯開轉向時間
        nextChangeTime = Time.time + Random.Range(0f, changeDirectionInterval);

        // 4. 初始速度
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

        // 定時改變目標方向
        if (!isScared && Time.time >= nextChangeTime)
        {
            ChangeTargetDirection();
            nextChangeTime = Time.time + changeDirectionInterval + Random.Range(-1.0f, 1.0f);
        }

        // 計算目標速度
        float targetSpeed = baseMoveSpeed * worldScale;
        if (isScared) targetSpeed *= scaredSpeedMultiplier;

        if (enableSpeedVariation && !isScared)
        {
            float wave = Mathf.Sin(Time.time * 3f + speedOffset);
            targetSpeed *= (1.0f + wave * 0.2f);
        }

        Vector3 desiredVelocity = targetDirection * targetSpeed;

        // 慣性轉向
        float steerRate = turnSpeed;
        if (isScared) steerRate *= 2f;

        currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, dt * steerRate);

        // 防止速度過低
        if (currentVelocity.sqrMagnitude < 0.0001f)
        {
            currentVelocity = Random.onUnitSphere * targetSpeed * 0.1f;
        }

        // 計算位移
        Vector3 nextPos = transform.position + currentVelocity * dt;

        // 邊界檢查
        Vector3 localPos = swimAnchor.InverseTransformPoint(nextPos);
        bool hitBound = CheckBounds(ref localPos, ref currentVelocity);

        if (hitBound)
        {
            targetDirection = currentVelocity.normalized;
        }

        // 應用位置
        Vector3 finalWorldPos = swimAnchor.TransformPoint(localPos);
        transform.position = finalWorldPos;

        // 旋轉
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

    // 檢查邊界
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

        // X 軸
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

        // Y 軸
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

        // Z 軸
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
        else if (other.CompareTag("Fish")) // 魚撞魚
        {
            // 修改：移除互斥力，只做單純的隨機轉向
            ChangeTargetDirection();
        }
    }

    public void OnTouched()
    {
        if (!HandCollisionDetector.IsHandVisible) return;
        if (Time.time - lastScoreTime < scoreCooldown) return;

        lastScoreTime = Time.time;
        if (enableDebug) Debug.Log($"✨ {gameObject.name} 獲得 {scoreValue} 分");

        if (GameManager_fish.Instance != null) GameManager_fish.Instance.AddScore(scoreValue);

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