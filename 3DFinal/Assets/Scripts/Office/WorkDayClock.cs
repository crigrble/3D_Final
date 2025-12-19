using UnityEngine;
using TMPro;

public class WorkDayClock : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI clockText;

    [Header("Work Time Setting")]
    public int startHour = 10;
    public int endHour = 18;

    [Header("Game Time")]
    [Tooltip("現實幾秒 = 遊戲中 1 分鐘")]
    public float realSecondsPerGameMinute = 1.5f;

    [Header("Warning Setting")]
    public int warningMinutes = 10;
    public Color normalColor = Color.green; // 09FF56
    public Color warningColor = Color.red;

    private float currentGameMinutes;
    private float totalWorkMinutes;
    private bool isRunning = true;

    void Start()
    {
        currentGameMinutes = startHour * 60;
        totalWorkMinutes = (endHour - startHour) * 60f;
        clockText.color = normalColor;
        UpdateClockUI();
    }

    void Update()
    {
        if (!isRunning) return;

        // 推進遊戲時間
        currentGameMinutes += Time.deltaTime / realSecondsPerGameMinute;

        // 下班判定
        if (currentGameMinutes >= endHour * 60)
        {
            currentGameMinutes = endHour * 60;
            isRunning = false;
            UpdateClockUI();
            OnWorkFinished();
            return;
        }

        UpdateClockUI();
    }

    void UpdateClockUI()
    {
        int hour = Mathf.FloorToInt(currentGameMinutes / 60);
        int minute = Mathf.FloorToInt(currentGameMinutes % 60);

        bool isWarningTime = currentGameMinutes >= (endHour * 60 - warningMinutes);

        // 顏色切換
        clockText.color = isWarningTime ? warningColor : normalColor;

        // 冒號閃爍
        bool showColon = !isWarningTime || Mathf.FloorToInt(Time.time * 2) % 2 == 0;
        string colon = showColon ? ":" : " ";

        clockText.text = $"{hour:00}{colon}{minute:00}";
    }

    void OnWorkFinished()
    {
        Debug.Log("遊戲結束");
    }


    /// 剩餘上班時間比例（1 = 剛上班，0 = 下班）
    public float GetRemainingTimeRatio()
    {
        float remaining = endHour * 60f - currentGameMinutes;
        return Mathf.Clamp01(remaining / totalWorkMinutes);
    }

    public bool IsWorking()
    {
        return isRunning;
    }
}
