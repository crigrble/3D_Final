using UnityEngine;

public class PatrolScheduler : MonoBehaviour
{
    [Header("References")]
    public StarterAssets.PatrolController patrol;
    public TypingGame typingGame;
    public WorkDayClock clock;

    [Header("Check Setting")]
    public float checkInterval = 2f;

    private float timer;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = checkInterval;

        // 安全檢查
        if (patrol == null || typingGame == null || clock == null)
        {
            Debug.LogWarning("PatrolScheduler: reference missing");
            return;
        }

        if (!clock.IsWorking()) return;
        if (patrol.IsOnPatrol()) return;

        float workRatio = typingGame.GetRemainingWorkRatio();
        float timeRatio = clock.GetRemainingTimeRatio();

        float pressure = workRatio * (1f - timeRatio);
        pressure = Mathf.Clamp01(pressure + 0.05f);

        Debug.Log($"[Scheduler] work={workRatio:F2}, time={timeRatio:F2}, pressure={pressure:F2}");

        if (Random.value < pressure)
        {
            Debug.Log("[Scheduler] Start patrol!");
            patrol.StartPatrol();
        }
    }
}
