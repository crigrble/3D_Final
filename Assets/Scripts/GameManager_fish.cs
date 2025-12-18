using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 摸魚遊戲管理器
/// 管理分數、UI 顯示等遊戲邏輯
/// </summary>
public class GameManager_fish : MonoBehaviour
{
    // Singleton 實例
    public static GameManager_fish Instance { get; private set; }
    
    [Header("分數設定")]
    [SerializeField] private int currentScore = 0;
    
    [Header("UI 參考")]
    [SerializeField] private TextMeshProUGUI scoreText; // TMP 文字
    [SerializeField] private Text scoreTextLegacy; // 舊版 UI Text（備用）
    
    [Header("調試設定")]
    [SerializeField] private bool enableDebug = true;
    
    private void Awake()
    {
        // Singleton 設定
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // 切換場景時不銷毀
    }
    
    private void Start()
    {
        // 初始化 UI
        UpdateScoreUI();
        
        if (enableDebug)
        {
            Debug.Log("✅ GameManager_fish 初始化完成");
        }
    }
    
    /// <summary>
    /// 增加分數
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        
        if (enableDebug)
        {
            Debug.Log($"💰 加分 +{points}！當前分數: {currentScore}");
        }
        
        UpdateScoreUI();
    }
    
    /// <summary>
    /// 重置分數
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
    }
    
    /// <summary>
    /// 取得當前分數
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    /// <summary>
    /// 更新分數 UI
    /// </summary>
    private void UpdateScoreUI()
    {
        string scoreString = "Score:" + currentScore.ToString();
        
        // 使用 TextMeshPro
        if (scoreText != null)
        {
            scoreText.text = scoreString;
        }
        
        // 備用：舊版 UI Text
        if (scoreTextLegacy != null)
        {
            scoreTextLegacy.text = scoreString;
        }
    }
}
