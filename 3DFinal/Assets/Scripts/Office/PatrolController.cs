using UnityEngine;

namespace StarterAssets
{
    public class PatrolController : MonoBehaviour
    {
        [Header("Patrol Path")]
        public Transform[] patrolPoints;

        [Header("Wait Setting")]
        public float waitTimeAtA = 1.5f;
        public float waitTimeAtD = 2.0f;

        [Header("Turn Setting")]
        public float turnSpeed = 360f;

        [Header("Freeze Check")]
        public Transform cameraTransform;

        [Header("Light")]
        public Light playerLight;

        private StarterAssetsInputs _input;

        private int currentIndex = 0;
        private int direction = 1;

        private bool isOnPatrol = false;
        private bool isReturning = false;

        // 停頓
        private bool isWaiting = false;
        private bool isWaitingAtD = false;
        private float waitTimer;

        // A 點轉身
        private bool isTurningAtA = false;
        private Quaternion targetRotation;

        void Start()
        {
            _input = GetComponent<StarterAssetsInputs>();
        }

        void Update()
        {
            // 待命
            if (!isOnPatrol)
            {
                _input.move = Vector2.zero;
                return;
            }

            // 回程關燈（你原本的設計）
            if (playerLight != null)
                playerLight.enabled = !isReturning;

            // === 停頓中 ===
            if (isWaiting)
            {
                _input.move = Vector2.zero;

                // ⭐ A 點轉身只在停頓時進行
                if (isTurningAtA)
                {
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        turnSpeed * Time.deltaTime
                    );
                }

                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;

                    // D → 開始回程
                    if (isWaitingAtD)
                    {
                        isWaitingAtD = false;
                        isReturning = true;
                        direction = -1;
                        currentIndex += direction;
                    }
                    else
                    {
                        // A → 巡邏結束
                        isTurningAtA = false;
                        isOnPatrol = false;
                        isReturning = false;
                        direction = 1;
                    }
                }
                return;
            }

            // === 世界座標方向 ===
            Transform target = patrolPoints[currentIndex];
            Vector3 worldDir = target.position - transform.position;
            worldDir.y = 0f;

            if (worldDir.magnitude < 0.25f)
            {
                HandleArriveAtPoint();
                _input.move = Vector2.zero;
                return;
            }

            // === 世界方向 → camera-relative input ===
            Transform cam = cameraTransform != null
                ? cameraTransform
                : Camera.main.transform;

            Vector3 camForward = cam.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cam.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 dir = worldDir.normalized;

            float x = Vector3.Dot(dir, camRight);
            float y = Vector3.Dot(dir, camForward);

            _input.move = new Vector2(x, y);
        }

        void HandleArriveAtPoint()
        {
            // 去程到 D
            if (!isReturning && currentIndex == patrolPoints.Length - 1)
            {
                isWaiting = true;
                isWaitingAtD = true;
                waitTimer = waitTimeAtD;

                // D 點鏡頭 -90° → 凍結
                if (cameraTransform != null)
                {
                    float camY = cameraTransform.eulerAngles.y;
                    bool isCameraAtMinus90 =
                        Mathf.Abs(camY - 270f) < 1f || Mathf.Abs(camY + 90f) < 1f;

                    if (isCameraAtMinus90)
                    {
                        Debug.Log("Freeze game: camera at -90° when waiting at D");
                        Time.timeScale = 0f;
                    }
                }
                return;
            }

            // 回程到 A → 停頓 + 轉身
            if (isReturning && currentIndex == 0)
            {
                isWaiting = true;
                waitTimer = waitTimeAtA;

                isTurningAtA = true;
                targetRotation = Quaternion.LookRotation(
                    -transform.forward,
                    Vector3.up
                );
                return;
            }

            currentIndex += direction;
        }

        // 外部接口
        public void StartPatrol()
        {
            if (isOnPatrol) return;

            isOnPatrol = true;
            isReturning = false;
            isWaiting = false;
            isWaitingAtD = false;
            isTurningAtA = false;
            currentIndex = 0;
            direction = 1;
        }

        public bool IsOnPatrol() => isOnPatrol;
    }
}
