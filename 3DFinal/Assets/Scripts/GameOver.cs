using UnityEngine;
using UnityEngine.Events;

namespace StarterAssets
{
    public class GameOver: MonoBehaviour
    {
        [Header("References")]
        public PatrolController patrol;     // 拖你的 PatrolController
        public Transform mainCamera;        // 拖 Main Camera (Transform)

        [Header("Point D Check")]
        [Tooltip("到點判定距離。想跟 PatrolController 一致就用 0.25")]
        public float arriveDistance = 0.25f;

        [Header("Yaw Range (degrees)")]
        [Tooltip("允許角度範圍：0 ~ -45（包含端點）。等同於 [-45, 0]")]
        public float minYaw = -45f;
        public float maxYaw = 0f;

        [Header("Game Over")]
        public UnityEvent onLose;
        public bool pauseTimeOnLose = true;

        bool hasCheckedAtD = false;

        void Reset()
        {
            if (!patrol) patrol = GetComponent<PatrolController>();
            if (!mainCamera && Camera.main) mainCamera = Camera.main.transform;
        }

        void Update()
        {
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

                float yaw = NormalizeSignedAngle(mainCamera.eulerAngles.y); // -180~180
                bool ok = (yaw >= minYaw && yaw <= maxYaw);                 // [-45, 0]

                if (!ok)
                    Lose(yaw);
            }

            // 若你只想判一次，就把這段移除
            if (!isAtD) hasCheckedAtD = false;
        }

        void Lose(float yaw)
        {
            Debug.Log($"[Lose] Reached Point D, but camera yaw={yaw:F1} not in [{minYaw}, {maxYaw}]");

            onLose?.Invoke();

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
