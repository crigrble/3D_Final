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

        private StarterAssetsInputs _input;
        private Light playerLight;

        private int currentIndex;
        private int direction;

        private bool isOnPatrol = false;
        private bool isReturning = false;

        // 停頓
        private bool isWaiting = false;
        private bool isWaitingAtD = false;
        private float waitTimer;

        // 轉身
        private bool isTurningAtA = false;
        private Quaternion targetRotation;

        void Start()
        {
            _input = GetComponent<StarterAssetsInputs>();
            playerLight = GetComponentInChildren<Light>();

            // 如果沒在 Inspector 拉攝影機，自動抓 MainCamera
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            currentIndex = 0;
            direction = 1;
            isOnPatrol = false;
        }

        void Update()
        {
            // 待命
            if (!isOnPatrol)
            {
                _input.move = Vector2.zero;
                return;
            }

            // 燈光
            if (playerLight != null)
                playerLight.enabled = !isReturning;

            // 停頓處理
            if (isWaiting)
            {
                _input.move = Vector2.zero;

                // A點轉身
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

                    // 開始回程
                    if (isWaitingAtD)
                    {
                        isWaitingAtD = false;
                        isReturning = true;
                        direction = -1;
                        currentIndex += direction;
                        return;
                    }

                    // 巡邏結束
                    isTurningAtA = false;
                    isOnPatrol = false;
                    direction = 1;
                    isReturning = false;
                }
                return;
            }

            // --- 移動邏輯修正開始 ---
            Transform target = patrolPoints[currentIndex];
            Vector3 worldDir = target.position - transform.position;
            worldDir.y = 0f;
            worldDir.Normalize(); // 取得標準化的世界方向

            // 核心修正：將「世界方向」轉換為「相對於攝影機的方向」
            // 這樣當 ThirdPersonController 再次加上攝影機角度時，就會剛好抵銷
            Vector2 finalInput = Vector2.zero;

            if (cameraTransform != null)
            {
                // 1. 取得世界方向的角度
                float targetAngle = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
                // 2. 扣掉攝影機目前的角度
                float relativeAngle = targetAngle - cameraTransform.eulerAngles.y;
                // 3. 轉回 Vector2 輸入 (Sin, Cos)
                // 注意：Mathf.Sin/Cos 吃的是弧度 (Rad)
                float rad = relativeAngle * Mathf.Deg2Rad;
                finalInput = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            }
            else
            {
                // 如果沒有攝影機，就直接用世界座標
                finalInput = new Vector2(worldDir.x, worldDir.z);
            }

            // 寫入 Input
            _input.move = finalInput;

            // 判斷到達距離 (用未修正的原始距離)
            float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                                          new Vector3(target.position.x, 0, target.position.z));
            
            if (dist < 0.25f)
            {
                HandleArriveAtPoint();
            }
            // --- 移動邏輯修正結束 ---
        }

        void HandleArriveAtPoint()
        {
            // 去程到D停頓
            if (!isReturning && currentIndex == patrolPoints.Length - 1)
            {
                isWaiting = true;
                isWaitingAtD = true;
                waitTimer = waitTimeAtD;

                // 檢查攝影機角度並凍結
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


            // 回程到A停頓 + 轉身
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
            currentIndex = 0;
            direction = 1;
        }

        public bool IsOnPatrol()
        {
            return isOnPatrol;
        }

        public bool IsWaitingAtPointD()
        {
            if (!isOnPatrol || isReturning) return false;
            if (patrolPoints == null || patrolPoints.Length == 0) return false;
            return currentIndex == patrolPoints.Length - 1 && isWaitingAtD;
        }

        public int GetCurrentPatrolIndex()
        {
            return currentIndex;
        }
    }
}