using UnityEngine;
using UnityEngine.Events;

namespace StarterAssets
{
    public class GameOver: MonoBehaviour
    {
        [Header("References")]
        public PatrolController patrol;     // 巡邏控制器 PatrolController
        public Transform mainCamera;        // 主攝影機 Main Camera (Transform)
        public GameObject gameOverUI;       // 遊戲結束UI

        [Header("Point D Check")]
        [Tooltip("到達點D的距離判定，建議與 PatrolController 一致，預設 0.25")]
        public float arriveDistance = 0.25f;

        [Header("Camera Angle Range (degrees)")]
        [Tooltip("允許的攝影機角度範圍：-30 ~ 0 度")]
        public float minAngle = -30f;
        public float maxAngle = 0f;

        [Header("Game Over")]
        public UnityEvent onLose;
        public bool pauseTimeOnLose = true;

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

            Vector3 p = transform.position;
            Vector3 d = pointD.position;
            p.y = 0f; d.y = 0f;

            bool isAtD = Vector3.Distance(p, d) <= arriveDistance;

            if (isAtD && !hasCheckedAtD)
            {
                hasCheckedAtD = true;

                // 檢查攝影機角度（Y軸旋轉，即水平角度）
                float cameraAngle = NormalizeSignedAngle(mainCamera.eulerAngles.y); // -180~180
                bool isAngleValid = (cameraAngle >= minAngle && cameraAngle <= maxAngle); // [-30, 0]

                if (!isAngleValid)
                {
                    Lose(cameraAngle);
                }
            }

            // 如果玩家離開點D，重置檢查標記
            if (!isAtD) hasCheckedAtD = false;
        }

        void Lose(float cameraAngle)
        {
            isGameOver = true;

            Debug.Log($"[遊戲失敗] 到達點D時，攝影機角度={cameraAngle:F1}度，不在允許範圍 [{minAngle}, {maxAngle}] 內");

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

        static float NormalizeSignedAngle(float angleDeg)
        {
            angleDeg %= 360f;
            if (angleDeg > 180f) angleDeg -= 360f;
            return angleDeg;
        }
    }
}
