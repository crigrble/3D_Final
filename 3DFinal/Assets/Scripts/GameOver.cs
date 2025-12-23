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

        bool hasCheckedAtD = false;
        bool isGameOver = false;

        void Reset()
        {
            if (!patrol) patrol = GetComponent<PatrolController>();
            if (!mainCamera && Camera.main) mainCamera = Camera.main.transform;
        }

        void Start()
        {
            // 初始化時隱藏遊戲結束UI
            if (gameOverUI != null)
            {
                gameOverUI.SetActive(false);
            }
        }

        void Update()
        {
            // 如果已經遊戲結束，不再檢查
            if (isGameOver) return;

            if (!patrol || !mainCamera) return;

            var points = patrol.patrolPoints;
            if (points == null || points.Length == 0) return;

            // Point D = 最後一個點
            Transform pointD = points[points.Length - 1];
            if (!pointD) return;

            // 檢查 BOSS（PatrolController 所在的 GameObject）的位置
            Vector3 bossPosition = patrol.transform.position;
            Vector3 pointDPosition = pointD.position;
            bossPosition.y = 0f;
            pointDPosition.y = 0f;

            bool isBossAtD = Vector3.Distance(bossPosition, pointDPosition) <= arriveDistance;

            if (isBossAtD && !hasCheckedAtD)
            {
                hasCheckedAtD = true;

                // 檢查玩家攝影機角度（Y軸旋轉，即水平角度）
                float cameraAngle = NormalizeSignedAngle(mainCamera.eulerAngles.y); // -180~180
                bool isAngleValid = (cameraAngle >= minAngle && cameraAngle <= maxAngle); // [-30, 0]

                if (!isAngleValid)
                {
                    Lose(cameraAngle, "BOSS 到達點D時，玩家攝影機角度不在工作範圍內（摸魚被發現）");
                }
            }

            // 如果 BOSS 離開點D，重置檢查標記
            if (!isBossAtD) hasCheckedAtD = false;
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
                gameOverUI.SetActive(true);
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

        static float NormalizeSignedAngle(float angleDeg)
        {
            angleDeg %= 360f;
            if (angleDeg > 180f) angleDeg -= 360f;
            return angleDeg;
        }
    }
}
