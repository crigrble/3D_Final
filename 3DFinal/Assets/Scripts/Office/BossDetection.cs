using UnityEngine;

namespace StarterAssets
{
    /// <summary>
    /// BOSS 檢測系統
    /// 當 BOSS 看到玩家在摸魚時，觸發遊戲失敗
    /// </summary>
    public class BossDetection : MonoBehaviour
    {
        [Header("BOSS 設定")]
        [Tooltip("BOSS 的 Transform（PlayerArmature）")]
        public Transform bossTransform;
        
        [Tooltip("BOSS 的視覺檢測點（通常是頭部或眼睛位置）")]
        public Transform bossVisionPoint;
        
        [Header("玩家設定")]
        [Tooltip("玩家的主攝影機")]
        public Transform playerCamera;
        
        [Tooltip("玩家的位置（通常是玩家角色的 Transform）")]
        public Transform playerTransform;

        [Header("摸魚判定")]
        [Tooltip("上班時允許的攝影機角度範圍：-30 ~ 0 度")]
        public float minWorkingAngle = -30f;
        public float maxWorkingAngle = 0f;

        [Header("視覺檢測設定")]
        [Tooltip("BOSS 的視覺範圍（距離）")]
        public float visionRange = 10f;
        
        [Tooltip("BOSS 的視覺角度（度）")]
        [Range(0f, 180f)]
        public float visionAngle = 60f;
        
        [Tooltip("檢測間隔（秒），降低性能消耗")]
        public float checkInterval = 0.1f;

        [Header("射線檢測設定")]
        [Tooltip("是否使用射線檢測（檢查是否有障礙物阻擋視線）")]
        public bool useRaycastCheck = true;
        
        [Tooltip("射線檢測的 Layer Mask（哪些層會被視為障礙物）")]
        public LayerMask obstacleLayers = -1;

        [Header("遊戲失敗")]
        public GameOver gameOverController;
        public bool enableDetection = true;

        [Header("調試")]
        public bool enableDebug = true;
        public bool showGizmos = true;
        public Color gizmoColor = Color.red;

        private float lastCheckTime = 0f;
        private bool isPlayerInVision = false;
        private bool isPlayerSlacking = false;

        void Start()
        {
            // 自動查找引用
            if (bossTransform == null)
            {
                GameObject bossObj = GameObject.Find("PlayerArmature");
                if (bossObj != null) bossTransform = bossObj.transform;
            }

            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }

            if (gameOverController == null)
            {
                gameOverController = FindObjectOfType<GameOver>();
            }

            // 如果沒有指定視覺點，使用 BOSS 的位置
            if (bossVisionPoint == null && bossTransform != null)
            {
                bossVisionPoint = bossTransform;
            }

            if (enableDebug)
            {
                Debug.Log($"[BOSS 檢測] 初始化完成 - BOSS: {(bossTransform ? bossTransform.name : "null")}, 玩家攝影機: {(playerCamera ? playerCamera.name : "null")}");
            }
        }

        void Update()
        {
            if (!enableDetection || isGameOver()) return;
            if (Time.time - lastCheckTime < checkInterval) return;

            lastCheckTime = Time.time;
            CheckBossVision();
        }

        void CheckBossVision()
        {
            if (bossTransform == null || bossVisionPoint == null || playerCamera == null)
            {
                if (enableDebug && Time.frameCount % 60 == 0) // 每60帧打印一次，避免刷屏
                {
                    Debug.LogWarning("[BOSS 檢測] 缺少必要的引用！");
                }
                return;
            }

            // 1. 檢查玩家是否在摸魚
            isPlayerSlacking = IsPlayerSlacking();

            // 2. 檢查玩家是否在 BOSS 視覺範圍內
            isPlayerInVision = IsPlayerInVisionRange();

            // 3. 如果玩家在摸魚且 BOSS 能看到玩家，觸發遊戲失敗
            if (isPlayerSlacking && isPlayerInVision)
            {
                if (enableDebug)
                {
                    Debug.LogWarning($"[BOSS 檢測] ⚠️ BOSS 發現玩家在摸魚！");
                }
                TriggerGameOver();
            }
        }

        bool IsPlayerSlacking()
        {
            if (playerCamera == null) return false;

            // 檢查攝影機的 Y 軸旋轉角度
            float cameraAngle = NormalizeSignedAngle(playerCamera.eulerAngles.y);
            bool isWorking = (cameraAngle >= minWorkingAngle && cameraAngle <= maxWorkingAngle);

            if (enableDebug && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[BOSS 檢測] 玩家攝影機角度: {cameraAngle:F1}° (上班: {isWorking})");
            }

            return !isWorking; // 不在工作範圍 = 在摸魚
        }

        bool IsPlayerInVisionRange()
        {
            if (bossVisionPoint == null || playerCamera == null) return false;

            Vector3 bossPosition = bossVisionPoint.position;
            Vector3 playerPosition = playerCamera.position;
            Vector3 directionToPlayer = playerPosition - bossPosition;
            float distanceToPlayer = directionToPlayer.magnitude;

            // 1. 檢查距離
            if (distanceToPlayer > visionRange)
            {
                return false;
            }

            // 2. 檢查角度（BOSS 的朝向）
            Vector3 bossForward = bossTransform.forward;
            directionToPlayer.Normalize();
            float angleToPlayer = Vector3.Angle(bossForward, directionToPlayer);

            if (angleToPlayer > visionAngle / 2f)
            {
                return false; // 玩家不在 BOSS 的視覺角度內
            }

            // 3. 射線檢測（檢查是否有障礙物）
            if (useRaycastCheck)
            {
                RaycastHit hit;
                Vector3 rayOrigin = bossPosition;
                Vector3 rayDirection = directionToPlayer;
                float rayDistance = distanceToPlayer;

                if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, obstacleLayers))
                {
                    // 如果射線擊中的不是玩家，表示有障礙物阻擋
                    if (hit.collider.transform != playerCamera && 
                        hit.collider.transform != playerTransform &&
                        !hit.collider.transform.IsChildOf(playerCamera) &&
                        !hit.collider.transform.IsChildOf(playerTransform))
                    {
                        if (enableDebug && Time.frameCount % 60 == 0)
                        {
                            Debug.Log($"[BOSS 檢測] 視線被 {hit.collider.name} 阻擋");
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        void TriggerGameOver()
        {
            if (gameOverController != null)
            {
                float currentAngle = NormalizeSignedAngle(playerCamera.eulerAngles.y);
                string reason = $"BOSS 發現玩家在摸魚（攝影機角度={currentAngle:F1}度，不在工作範圍 [{minWorkingAngle}, {maxWorkingAngle}] 內）";
                gameOverController.TriggerGameOver(reason);
            }
            else
            {
                Debug.LogError("[BOSS 檢測] GameOver 控制器未設定！");
            }
        }

        bool isGameOver()
        {
            if (gameOverController != null)
            {
                return gameOverController.IsGameOver();
            }
            return false;
        }

        static float NormalizeSignedAngle(float angleDeg)
        {
            angleDeg %= 360f;
            if (angleDeg > 180f) angleDeg -= 360f;
            return angleDeg;
        }

        void OnDrawGizmos()
        {
            if (!showGizmos || !Application.isPlaying) return;
            if (bossTransform == null || bossVisionPoint == null) return;

            Gizmos.color = gizmoColor;

            // 繪製視覺範圍
            Vector3 bossPos = bossVisionPoint.position;
            Vector3 bossForward = bossTransform.forward;

            // 繪製視覺錐形
            float halfAngle = visionAngle / 2f;
            Vector3 leftBoundary = Quaternion.Euler(0, -halfAngle, 0) * bossForward;
            Vector3 rightBoundary = Quaternion.Euler(0, halfAngle, 0) * bossForward;

            Gizmos.DrawRay(bossPos, leftBoundary * visionRange);
            Gizmos.DrawRay(bossPos, rightBoundary * visionRange);
            Gizmos.DrawRay(bossPos, bossForward * visionRange);

            // 繪製到玩家的射線（如果玩家在視覺範圍內）
            if (playerCamera != null && isPlayerInVision)
            {
                Gizmos.color = isPlayerSlacking ? Color.red : Color.yellow;
                Gizmos.DrawLine(bossPos, playerCamera.position);
            }
        }
    }
}

