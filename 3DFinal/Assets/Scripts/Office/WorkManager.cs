using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class WorkManager : MonoBehaviour
{
    public static WorkManager Instance { get; private set; }

    [Header("工作設定")]
    [Tooltip("初始剩餘工作數量（可在 Inspector 中設定）")]
    [SerializeField] private int remainingJobs = 7;  // 剩餘工作數量
    
    private int initialJobs;  // 保存初始值，用於重置

    [Header("UI (自動綁定)")]
    [Tooltip("顯示剩餘工作數量的 TMP 文字（Tag=RemainingJobsUI 會自動抓）")]
    [SerializeField] private TextMeshProUGUI remainingJobsText;

    [Header("事件")]
    public UnityEvent<int> OnJobsChanged;  // 當工作數量改變時觸發

    [Header("調試設定")]
    [SerializeField] private bool enableDebug = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 保存初始值，用於重置功能
        initialJobs = remainingJobs;
    }

    private void Start()
    {
        AutoBindUI();
        UpdateUI();
    }

    private void AutoBindUI()
    {
        // 自動綁定 UI
        var jobsObj = GameObject.FindGameObjectWithTag("RemainingJobsUI");
        if (jobsObj != null)
        {
            remainingJobsText = jobsObj.GetComponent<TextMeshProUGUI>();
            if (enableDebug) Debug.Log("✅ RemainingJobsUI 已自動綁定");
        }
        else if (enableDebug)
        {
            Debug.LogWarning("⚠️ 未找到 Tag=RemainingJobsUI 的物件");
        }
    }

    /// <summary>
    /// 完成一份工作，減少剩餘工作數量
    /// </summary>
    public void CompleteJob()
    {
        if (remainingJobs > 0)
        {
            remainingJobs--;
            UpdateUI();
            OnJobsChanged?.Invoke(remainingJobs);

            if (enableDebug)
                Debug.Log($"✅ 工作完成！剩餘工作：{remainingJobs}");
        }
    }

    /// <summary>
    /// 重置工作數量（重置為初始值）
    /// </summary>
    /// <param name="resetValue">重置為的值（如果不提供，則使用初始值）</param>
    public void ResetJobs(int resetValue = -1)
    {
        if (resetValue >= 0)
        {
            remainingJobs = resetValue;
        }
        else
        {
            remainingJobs = initialJobs;  // 重置為初始值
        }
        UpdateUI();
        OnJobsChanged?.Invoke(remainingJobs);

        if (enableDebug)
            Debug.Log($"🔄 工作已重置：{remainingJobs}");
    }

    /// <summary>
    /// 獲取剩餘工作數量
    /// </summary>
    public int GetRemainingJobs() => remainingJobs;


    /// <summary>
    /// 檢查是否還有剩餘工作
    /// </summary>
    public bool HasRemainingJobs() => remainingJobs > 0;

    private void UpdateUI()
    {
        if (remainingJobsText != null)
        {
            remainingJobsText.text = "remain jobs : " + remainingJobs;
        }
    }
}

