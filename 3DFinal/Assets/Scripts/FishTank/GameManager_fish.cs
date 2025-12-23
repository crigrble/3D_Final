using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager_fish : MonoBehaviour
{
    public static GameManager_fish Instance { get; private set; }

    [Header("分數設定")]
    [SerializeField] private int currentScore = 0;

    [Header("UI (自動綁定)")]
    [Tooltip("遊戲中顯示分數的 TMP 文字（Tag=ScoreUI 會自動抓）")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("結算畫面顯示最終分數的 TMP 文字（Tag=ResultScoreUI 會自動抓）")]
    [SerializeField] private TextMeshProUGUI resultScoreText;

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
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        AutoBindUI();
        UpdateScoreUI();
        UpdateResultUI();

        if (enableDebug) Debug.Log("✅ GameManager_fish 初始化完成");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AutoBindUI();
        UpdateScoreUI();
        UpdateResultUI();
    }

    private void AutoBindUI()
    {
        // 遊戲中分數 UI
        var scoreObj = GameObject.FindGameObjectWithTag("ScoreUI");
        if (scoreObj != null)
            scoreText = scoreObj.GetComponent<TextMeshProUGUI>();

        // 結算分數 UI
        var resultObj = GameObject.FindGameObjectWithTag("ResultScoreUI");
        if (resultObj != null)
            resultScoreText = resultObj.GetComponent<TextMeshProUGUI>();

        if (enableDebug)
        {
            Debug.Log($"🔎 AutoBindUI：scoreText={(scoreText ? scoreText.name : "null")} / resultScoreText={(resultScoreText ? resultScoreText.name : "null")}");
        }
    }

    public void AddScore(int points)
    {
        currentScore += points;

        if (enableDebug)
            Debug.Log($"💰 加分 +{points}！當前分數: {currentScore}");

        UpdateScoreUI();
        UpdateResultUI(); // 如果結算畫面已經開著，也會同步更新
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
        UpdateResultUI();
    }

    public int GetCurrentScore() => currentScore;

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + currentScore;
    }

    private void UpdateResultUI()
    {
        if (resultScoreText != null)
            resultScoreText.text = "Final Score: " + currentScore;
    }
}
