using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

namespace StarterAssets
{
    public class GameOver: MonoBehaviour
    {
        [Header("References")]
        [Tooltip("BOSS 的巡邏控制器（PlayerArmature 上的 PatrolController）")]
        public PatrolController patrol;     // 巡邏控制器 PatrolController
        [Tooltip("玩家的主攝影機（用於檢測是否在摸魚）")]
        public Transform mainCamera;        // 主攝影機 Main Camera (Transform)
        [Tooltip("遊戲結束時顯示的 UI")]
        public GameObject gameOverUI;       // 遊戲結束UI

        [Header("Point D Check")]
        [Tooltip("BOSS 到達點D的距離判定，建議與 PatrolController 一致，預設 0.25")]
        public float arriveDistance = 0.25f;

        [Header("Camera Angle Range (degrees)")]
        [Tooltip("允許的攝影機角度範圍：-30 ~ 0 度")]
        public float minAngle = -30f;
        public float maxAngle = 0f;

        [Header("Game Over")]
        public UnityEvent onLose;
        public bool pauseTimeOnLose = true;

        [Header("重新開始設定")]
        [Tooltip("重新開始時要載入的場景名稱（留空則重新載入當前場景）")]
        public string restartSceneName = "";

        [Header("調試")]
        [Tooltip("啟用詳細的調試日誌")]
        public bool enableDebug = true;

        bool hasCheckedAtD = false;
        bool isGameOver = false;

        void Reset()
        {
            if (!patrol) patrol = GetComponent<PatrolController>();
            if (!mainCamera && Camera.main) mainCamera = Camera.main.transform;
        }

        void Start()
        {
            // 強制輸出，確保腳本有運行
            Debug.Log("[GameOver] ⚡ Start() 被調用！腳本已初始化");
            
            if (enableDebug)
                Debug.Log("[GameOver] 調試模式已啟用");

            // 檢查引用
            if (enableDebug)
            {
                Debug.Log($"[GameOver] 引用檢查：patrol={patrol != null}, mainCamera={mainCamera != null}, gameOverUI={gameOverUI != null}");
            }

            // 初始化時隱藏遊戲結束UI
            if (gameOverUI != null)
            {
                gameOverUI.SetActive(false);
                if (enableDebug)
                    Debug.Log("[GameOver] ✅ gameOverUI 已隱藏（初始化）");
            }
            else if (enableDebug)
            {
                Debug.LogError("[GameOver] ❌ gameOverUI 未設定！請在 Inspector 中指定 gameOverUI。");
            }
        }

        void Update()
        {
            // 如果已經遊戲結束，不再檢查
            if (isGameOver) return;

            if (!patrol || !mainCamera)
            {
                if (enableDebug && Time.frameCount % 60 == 0) // 每60帧打印一次，避免刷屏
                {
                    Debug.LogWarning($"[GameOver] 缺少必要引用：patrol={patrol != null}, mainCamera={mainCamera != null}");
                }
                return;
            }

            var points = patrol.patrolPoints;
            if (points == null || points.Length == 0)
            {
                if (enableDebug && Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning("[GameOver] patrolPoints 為空或 null");
                }
                return;
            }

            // Point D = 最後一個點
            Transform pointD = points[points.Length - 1];
            if (!pointD)
            {
                if (enableDebug) Debug.LogError("[GameOver] pointD 為 null！");
                return;
            }

            // 方法1：檢查 BOSS（PatrolController 所在的 GameObject）的位置
            Vector3 bossPosition = patrol.transform.position;
            Vector3 pointDPosition = pointD.position;
            bossPosition.y = 0f;
            pointDPosition.y = 0f;

            float distanceToD = Vector3.Distance(bossPosition, pointDPosition);
            bool isBossAtDByDistance = distanceToD <= arriveDistance;

            // 方法2：使用 PatrolController 的公開方法檢查狀態（更可靠）
            bool isBossAtDByState = patrol.IsWaitingAtPointD();

            if (enableDebug && Time.frameCount % 30 == 0)
            {
                int currentIndex = patrol.GetCurrentPatrolIndex();
                Debug.Log($"[GameOver] Patrol狀態: isOnPatrol={patrol.IsOnPatrol()}, currentIndex={currentIndex}/{points.Length-1}, isWaitingAtD={isBossAtDByState}");
            }

            // 使用兩種方法中的任一種來判斷（優先使用狀態檢查）
            bool isBossAtD = isBossAtDByState || isBossAtDByDistance;

            // 調試信息
            if (enableDebug && Time.frameCount % 30 == 0) // 每30帧打印一次
            {
                float cameraAngle = NormalizeSignedAngle(mainCamera.eulerAngles.y);
                Debug.Log($"[GameOver] BOSS距離點D: {distanceToD:F3}m (需要 <= {arriveDistance}m), 相機角度: {cameraAngle:F1}°, 已檢查: {hasCheckedAtD}, 在點D: {isBossAtD}");
            }

            if (isBossAtD && !hasCheckedAtD)
            {
                hasCheckedAtD = true;

                // 檢查玩家攝影機角度（Y軸旋轉，即水平角度）
                float cameraAngle = NormalizeSignedAngle(mainCamera.eulerAngles.y); // -180~180
                bool isAngleValid = (cameraAngle >= minAngle && cameraAngle <= maxAngle); // [-30, 0]

                // 強制輸出，確保能看到檢查過程
                Debug.Log($"[GameOver] ✅ BOSS 到達點D！相機角度: {cameraAngle:F1}° (允許範圍: [{minAngle}, {maxAngle}]°), 有效: {isAngleValid}, isGameOver={isGameOver}");

                if (!isAngleValid)
                {
                    Debug.LogWarning($"[GameOver] ⚠️ 觸發遊戲失敗：相機角度 {cameraAngle:F1}° 不在工作範圍內！");
                    Lose(cameraAngle, "BOSS 到達點D時，玩家攝影機角度不在工作範圍內（摸魚被發現）");
                }
                else
                {
                    if (enableDebug)
                    {
                        Debug.Log($"[GameOver] ✅ 相機角度在允許範圍內，遊戲繼續");
                    }
                }
            }

            // 如果 BOSS 離開點D，重置檢查標記
            if (!isBossAtD && hasCheckedAtD)
            {
                hasCheckedAtD = false;
                if (enableDebug)
                {
                    Debug.Log("[GameOver] BOSS 離開點D，重置檢查標記");
                }
            }
        }

        /// <summary>
        /// 觸發遊戲失敗（公開方法，可被外部調用）
        /// </summary>
        /// <param name="reason">失敗原因描述</param>
        public void TriggerGameOver(string reason = "未知原因")
        {
            if (isGameOver) return; // 避免重複觸發

            float cameraAngle = mainCamera != null ? NormalizeSignedAngle(mainCamera.eulerAngles.y) : 0f;
            Lose(cameraAngle, reason);
        }

        void Lose(float cameraAngle, string reason = "")
        {
            isGameOver = true;

            string logMessage = string.IsNullOrEmpty(reason) 
                ? $"[遊戲失敗] 到達點D時，攝影機角度={cameraAngle:F1}度，不在允許範圍 [{minAngle}, {maxAngle}] 內"
                : $"[遊戲失敗] {reason} (攝影機角度={cameraAngle:F1}度)";
            
            Debug.Log(logMessage);

            // 更新結算畫面分數（如果 GameManager_fish 存在）
            if (GameManager_fish.Instance != null)
            {
                // 強制更新結算UI，確保分數顯示在遊戲結束畫面中
                var resultObj = GameObject.FindGameObjectWithTag("ResultScoreUI");
                if (resultObj != null)
                {
                    var resultText = resultObj.GetComponent<TMPro.TextMeshProUGUI>();
                    if (resultText != null)
                    {
                        int finalScore = GameManager_fish.Instance.GetCurrentScore();
                        resultText.text = "Final Score: " + finalScore;
                        Debug.Log($"✅ 結算畫面分數已更新：{finalScore}");
                    }
                }
            }

            // 顯示遊戲結束UI
            if (gameOverUI != null)
            {
                // 詳細檢查 gameOverUI 的狀態
                if (enableDebug)
                {
                    Debug.Log($"[GameOver] 🔍 檢查 gameOverUI 狀態：");
                    Debug.Log($"[GameOver]   - gameOverUI.activeSelf = {gameOverUI.activeSelf}");
                    Debug.Log($"[GameOver]   - gameOverUI.activeInHierarchy = {gameOverUI.activeInHierarchy}");
                    
                    // 檢查父對象
                    Transform parent = gameOverUI.transform.parent;
                    if (parent != null)
                    {
                        Debug.Log($"[GameOver]   - 父對象名稱：{parent.name}");
                        Debug.Log($"[GameOver]   - 父對象 activeSelf = {parent.gameObject.activeSelf}");
                        Debug.Log($"[GameOver]   - 父對象 activeInHierarchy = {parent.gameObject.activeInHierarchy}");
                    }
                    else
                    {
                        Debug.Log($"[GameOver]   - 沒有父對象（根對象）");
                    }
                    
                    // 檢查 Canvas
                    Canvas canvas = gameOverUI.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        Debug.Log($"[GameOver]   - Canvas 名稱：{canvas.name}");
                        Debug.Log($"[GameOver]   - Canvas activeSelf = {canvas.gameObject.activeSelf}");
                        Debug.Log($"[GameOver]   - Canvas activeInHierarchy = {canvas.gameObject.activeInHierarchy}");
                        Debug.Log($"[GameOver]   - Canvas Sorting Order = {canvas.sortingOrder}");
                        Debug.Log($"[GameOver]   - Canvas Render Mode = {canvas.renderMode}");
                    }
                    else
                    {
                        Debug.LogWarning("[GameOver]   - ⚠️ 未找到 Canvas 組件！");
                    }
                }
                
                // 確保父對象和 Canvas 都被激活
                Transform parentTransform = gameOverUI.transform.parent;
                if (parentTransform != null && !parentTransform.gameObject.activeSelf)
                {
                    Debug.LogWarning($"[GameOver] ⚠️ 父對象 {parentTransform.name} 被禁用，正在激活...");
                    parentTransform.gameObject.SetActive(true);
                }
                
                Canvas parentCanvas = gameOverUI.GetComponentInParent<Canvas>();
                if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
                {
                    Debug.LogWarning($"[GameOver] ⚠️ Canvas {parentCanvas.name} 被禁用，正在激活...");
                    parentCanvas.gameObject.SetActive(true);
                }
                
                // 激活 gameOverUI
                gameOverUI.SetActive(true);
                
                if (enableDebug)
                {
                    Debug.Log($"[GameOver] ✅ gameOverUI.SetActive(true) 已調用");
                    Debug.Log($"[GameOver]   - 激活後 activeSelf = {gameOverUI.activeSelf}");
                    Debug.Log($"[GameOver]   - 激活後 activeInHierarchy = {gameOverUI.activeInHierarchy}");
                }
                
                // 如果 gameOverUI 有 ResultPanelController，調用其 Show() 方法（處理鼠標顯示等）
                var resultPanel = gameOverUI.GetComponent<ResultPanelController>();
                if (resultPanel != null)
                {
                    resultPanel.Show();
                    if (enableDebug)
                        Debug.Log("[GameOver] ✅ 找到 ResultPanelController，已調用 Show()");
                }
                else
                {
                    // 如果沒有 ResultPanelController，手動顯示鼠標
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    if (enableDebug)
                        Debug.Log("[GameOver] ⚠️ 未找到 ResultPanelController，手動顯示鼠標");
                }
                
                if (enableDebug)
                    Debug.Log("[GameOver] ✅ GameOver UI 已顯示");
            }
            else
            {
                Debug.LogWarning("遊戲結束UI未設定！請在Inspector中指定gameOverUI。");
            }

            // 觸發UnityEvent事件
            onLose?.Invoke();

            // 暫停遊戲時間
            if (pauseTimeOnLose)
                Time.timeScale = 0f;
        }

        /// <summary>
        /// 重新開始遊戲
        /// 可以從 UI 按鈕調用此方法
        /// </summary>
        public void RestartGame()
        {
            // 恢復時間流速
            Time.timeScale = 1f;

            // 重置分數（如果 GameManager 存在）
            if (GameManager_fish.Instance != null)
            {
                GameManager_fish.Instance.ResetScore();
            }

            // 重置遊戲狀態
            isGameOver = false;
            hasCheckedAtD = false;

            // 載入場景
            if (string.IsNullOrEmpty(restartSceneName))
            {
                // 重新載入當前場景
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                // 載入指定場景
                SceneManager.LoadScene(restartSceneName);
            }

            Debug.Log("[遊戲重新開始] 場景已重新載入");
        }

        /// <summary>
        /// 返回主選單（如果有的話）
        /// </summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            
            // 這裡可以設定主選單場景名稱
            // SceneManager.LoadScene("MainMenu");
            
            Debug.Log("[返回主選單] 功能待實現");
        }

        /// <summary>
        /// 退出遊戲
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[退出遊戲]");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        /// <summary>
        /// 獲取遊戲是否已結束
        /// </summary>
        public bool IsGameOver()
        {
            return isGameOver;
        }
        
        /// <summary>
        /// 重置遊戲狀態（用於重新開始遊戲，不重新載入場景）
        /// </summary>
        public void ResetGameState()
        {
            if (enableDebug)
                Debug.Log("[GameOver] 🔄 開始重置遊戲狀態...");
            
            isGameOver = false;
            hasCheckedAtD = false;
            
            // 隱藏 UI
            if (gameOverUI != null)
            {
                gameOverUI.SetActive(false);
                if (enableDebug)
                    Debug.Log("[GameOver] ✅ gameOverUI 已隱藏");
            }
            
            // 確保重新查找引用（以防引用丟失）
            if (patrol == null)
            {
                patrol = FindObjectOfType<PatrolController>();
            }
            
            if (mainCamera == null)
            {
                if (Camera.main != null)
                {
                    mainCamera = Camera.main.transform;
                }
            }
            
            if (enableDebug)
            {
                Debug.Log("[GameOver] ✅ 遊戲狀態已重置：isGameOver = false, hasCheckedAtD = false");
                Debug.Log($"[GameOver] 引用檢查：patrol={patrol != null}, mainCamera={mainCamera != null}");
            }
        }

        static float NormalizeSignedAngle(float angleDeg)
        {
            angleDeg %= 360f;
            if (angleDeg > 180f) angleDeg -= 360f;
            return angleDeg;
        }
    }
}
