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

            // 移動
            Transform target = patrolPoints[currentIndex];
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;

            _input.move = new Vector2(dir.normalized.x, dir.normalized.z);

            if (dir.magnitude < 0.25f)
            {
                HandleArriveAtPoint();
            }
        }

        void HandleArriveAtPoint()
        {
            // 去程到D停頓
            if (!isReturning && currentIndex == patrolPoints.Length - 1)
            {
                isWaiting = true;
                isWaitingAtD = true;
                waitTimer = waitTimeAtD;
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
    }
}
