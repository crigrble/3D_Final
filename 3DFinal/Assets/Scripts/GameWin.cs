using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameWin : MonoBehaviour
{
    [Header("References")]
    [Tooltip("工作管理器（用於檢查剩餘工作數量）")]
    public WorkManager workManager;
    
    [Tooltip("工作時鐘（用於檢查時間是否結束）")]
    public WorkDayClock workDayClock;

    [Header("Game Win UI")]
    [Tooltip("遊戲勝利時顯示的 UI")]
    public GameObject gameWinUI;

    [Header("Game Win Settings")]
    public UnityEvent onWin;
    public bool pauseTimeOnWin = true;

    [Header("重新開始設定")]
    [Tooltip("重新開始時要載入的場景名稱（留空則重新載入當前場景）")]
    public string restartSceneName = "";

    [Header("調試")]
    [Tooltip("啟用詳細的調試日誌")]
    public bool enableDebug = true;

    private bool isGameWon = false;

    void Start()
    {
        // 強制輸出，確保腳本有運行
        Debug.Log("[GameWin] ⚡ Start() 被調用！腳本已初始化");
        
        if (enableDebug)
            Debug.Log("[GameWin] 調試模式已啟用");

        // 自動查找引用
        if (workManager == null)
        {
            workManager = WorkManager.Instance;
            if (workManager == null)
            {
                workManager = FindObjectOfType<WorkManager>();
            }
        }

        if (workDayClock == null)
        {
            workDayClock = FindObjectOfType<WorkDayClock>();
        }

        // 檢查引用
        if (enableDebug)
        {
            Debug.Log($"[GameWin] 引用檢查：workManager={workManager != null}, workDayClock={workDayClock != null}, gameWinUI={gameWinUI != null}");
            if (workManager != null)
                Debug.Log($"[GameWin] 當前 remainingJobs={workManager.GetRemainingJobs()}");
            if (workDayClock != null)
                Debug.Log($"[GameWin] IsWorking={workDayClock.IsWorking()}");
        }

        // 初始化時隱藏遊戲勝利UI
        if (gameWinUI != null)
        {
            gameWinUI.SetActive(false);
            if (enableDebug)
                Debug.Log("[GameWin] ✅ gameWinUI 已隱藏（初始化）");
        }
        else if (enableDebug)
        {
            Debug.LogError("[GameWin] ❌ gameWinUI 未設定！請在 Inspector 中指定 gameWinUI。");
        }

        // 訂閱 WorkManager 的事件
        if (workManager != null)
        {
            workManager.OnJobsChanged.AddListener(OnJobsChanged);
            if (enableDebug)
                Debug.Log("[GameWin] ✅ 已訂閱 WorkManager.OnJobsChanged 事件");
        }
        else if (enableDebug)
        {
            Debug.LogError("[GameWin] ❌ WorkManager 未找到！請確認場景中有 WorkManager 組件");
        }

        if (workDayClock == null && enableDebug)
        {
            Debug.LogError("[GameWin] ❌ WorkDayClock 未找到！請確認場景中有 WorkDayClock 組件");
        }
    }

    void Update()
    {
        // 如果已經遊戲勝利，不再檢查
        if (isGameWon) return;

        // 持續檢查勝利條件
        CheckWinCondition();
    }

    void OnJobsChanged(int remainingJobs)
    {
        if (enableDebug)
            Debug.Log($"[GameWin] OnJobsChanged 被調用，remainingJobs={remainingJobs}");

        // 當工作數量改變時，檢查是否勝利
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (isGameWon)
        {
            if (enableDebug && Time.frameCount % 120 == 0)
                Debug.Log("[GameWin] 遊戲已勝利，跳過檢查");
            return;
        }

        // 檢查必要引用
        if (workManager == null)
        {
            if (enableDebug && Time.frameCount % 120 == 0)
                Debug.LogWarning("[GameWin] ⚠️ workManager 為 null，無法檢查勝利條件");
            return;
        }

        if (workDayClock == null)
        {
            if (enableDebug && Time.frameCount % 120 == 0)
                Debug.LogWarning("[GameWin] ⚠️ workDayClock 為 null，無法檢查勝利條件");
            return;
        }

        int remainingJobs = workManager.GetRemainingJobs();
        bool isTimeStillRunning = workDayClock.IsWorking();

        // 當 remainingJobs 為 0 時，立即打印詳細信息
        if (remainingJobs == 0)
        {
            // 強制輸出，確保能看到檢查過程
            Debug.Log($"[GameWin] 🔍 檢查勝利條件：remainingJobs={remainingJobs}, isTimeStillRunning={isTimeStillRunning}, isGameWon={isGameWon}");
            Debug.Log($"[GameWin] 🔍 workManager 是否為 null：{workManager == null}");
            Debug.Log($"[GameWin] 🔍 workDayClock 是否為 null：{workDayClock == null}");
            Debug.Log($"[GameWin] 🔍 gameWinUI 是否為 null：{gameWinUI == null}");
        }
        else if (enableDebug && Time.frameCount % 120 == 0) // 每120帧打印一次，避免刷屏
        {
            Debug.Log($"[GameWin] 檢查勝利條件：remainingJobs={remainingJobs}, isTimeStillRunning={isTimeStillRunning}");
        }

        // 勝利條件：remainingJobs = 0 且時間還沒結束
        if (remainingJobs == 0)
        {
            if (isTimeStillRunning)
            {
                if (enableDebug)
                    Debug.Log("[GameWin] ✅ 勝利條件達成！remainingJobs=0 且時間還沒結束");
                
                Win();
            }
            else
            {
                if (enableDebug)
                    Debug.LogWarning("[GameWin] ⚠️ remainingJobs=0 但時間已結束，不觸發勝利");
            }
        }
    }

    void Win()
    {
        if (isGameWon) return; // 避免重複觸發

        isGameWon = true;

        if (enableDebug)
            Debug.Log("[GameWin] 🎉 遊戲勝利！");

        // 更新結算畫面分數（如果 GameManager_fish 存在）
        if (GameManager_fish.Instance != null)
        {
            // 方法1：查找 Tag=ResultScoreUI 的物件
            var resultObj = GameObject.FindGameObjectWithTag("ResultScoreUI");
            if (resultObj != null)
            {
                var resultText = resultObj.GetComponent<TMPro.TextMeshProUGUI>();
                if (resultText != null)
                {
                    int finalScore = GameManager_fish.Instance.GetCurrentScore();
                    resultText.text = "Final Score: " + finalScore;
                    if (enableDebug)
                        Debug.Log($"[GameWin] ✅ 結算畫面分數已更新：{finalScore}");
                }
                else if (enableDebug)
                {
                    Debug.LogWarning("[GameWin] ⚠️ 找到 Tag=ResultScoreUI 的物件，但沒有 TextMeshProUGUI 組件");
                }
            }
            else if (enableDebug)
            {
                Debug.LogWarning("[GameWin] ⚠️ 未找到 Tag=ResultScoreUI 的物件");
            }

            // 方法2：如果 gameWinUI 有 ResultPanelController，調用其 Show() 方法
            if (gameWinUI != null)
            {
                var resultPanel = gameWinUI.GetComponent<ResultPanelController>();
                if (resultPanel != null)
                {
                    resultPanel.Show();
                    if (enableDebug)
                        Debug.Log("[GameWin] ✅ 找到 ResultPanelController，已調用 Show()");
                }
            }
        }
        else if (enableDebug)
        {
            Debug.LogWarning("[GameWin] ⚠️ GameManager_fish.Instance 為 null，無法更新分數");
        }

        // 顯示遊戲勝利UI
        if (gameWinUI != null)
        {
            // 詳細檢查 gameWinUI 的狀態
            if (enableDebug)
            {
                Debug.Log($"[GameWin] 🔍 檢查 gameWinUI 狀態：");
                Debug.Log($"[GameWin]   - gameWinUI.activeSelf = {gameWinUI.activeSelf}");
                Debug.Log($"[GameWin]   - gameWinUI.activeInHierarchy = {gameWinUI.activeInHierarchy}");
                
                // 檢查父對象
                Transform parent = gameWinUI.transform.parent;
                if (parent != null)
                {
                    Debug.Log($"[GameWin]   - 父對象名稱：{parent.name}");
                    Debug.Log($"[GameWin]   - 父對象 activeSelf = {parent.gameObject.activeSelf}");
                    Debug.Log($"[GameWin]   - 父對象 activeInHierarchy = {parent.gameObject.activeInHierarchy}");
                }
                else
                {
                    Debug.Log($"[GameWin]   - 沒有父對象（根對象）");
                }
                
                // 檢查 Canvas
                Canvas canvas = gameWinUI.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"[GameWin]   - Canvas 名稱：{canvas.name}");
                    Debug.Log($"[GameWin]   - Canvas activeSelf = {canvas.gameObject.activeSelf}");
                    Debug.Log($"[GameWin]   - Canvas activeInHierarchy = {canvas.gameObject.activeInHierarchy}");
                    Debug.Log($"[GameWin]   - Canvas Sorting Order = {canvas.sortingOrder}");
                    Debug.Log($"[GameWin]   - Canvas Render Mode = {canvas.renderMode}");
                }
                else
                {
                    Debug.LogWarning("[GameWin]   - ⚠️ 未找到 Canvas 組件！");
                }
            }
            
            // 確保父對象和 Canvas 都被激活
            Transform parentTransform = gameWinUI.transform.parent;
            if (parentTransform != null && !parentTransform.gameObject.activeSelf)
            {
                Debug.LogWarning($"[GameWin] ⚠️ 父對象 {parentTransform.name} 被禁用，正在激活...");
                parentTransform.gameObject.SetActive(true);
            }
            
            Canvas parentCanvas = gameWinUI.GetComponentInParent<Canvas>();
            if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
            {
                Debug.LogWarning($"[GameWin] ⚠️ Canvas {parentCanvas.name} 被禁用，正在激活...");
                parentCanvas.gameObject.SetActive(true);
            }
            
            // 激活 gameWinUI
            gameWinUI.SetActive(true);
            
            if (enableDebug)
            {
                Debug.Log($"[GameWin] ✅ gameWinUI.SetActive(true) 已調用");
                Debug.Log($"[GameWin]   - 激活後 activeSelf = {gameWinUI.activeSelf}");
                Debug.Log($"[GameWin]   - 激活後 activeInHierarchy = {gameWinUI.activeInHierarchy}");
            }
            
            // 如果 gameWinUI 有 ResultPanelController，調用其 Show() 方法（處理鼠標顯示等）
            var resultPanel = gameWinUI.GetComponent<ResultPanelController>();
            if (resultPanel != null)
            {
                resultPanel.Show();
                if (enableDebug)
                    Debug.Log("[GameWin] ✅ 找到 ResultPanelController，已調用 Show()");
            }
            else
            {
                // 如果沒有 ResultPanelController，手動顯示鼠標
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (enableDebug)
                    Debug.Log("[GameWin] ⚠️ 未找到 ResultPanelController，手動顯示鼠標");
            }
            
            if (enableDebug)
                Debug.Log("[GameWin] ✅ GameWin UI 已顯示");
        }
        else
        {
            Debug.LogWarning("[GameWin] ⚠️ gameWinUI 未設定！請在 Inspector 中指定 gameWinUI。");
        }

        // 觸發UnityEvent事件
        onWin?.Invoke();

        // 暫停遊戲時間
        if (pauseTimeOnWin)
        {
            Time.timeScale = 0f;
            if (enableDebug)
                Debug.Log("[GameWin] 遊戲時間已暫停");
        }
    }

    /// <summary>
    /// 重新開始遊戲
    /// 可以從 UI 按鈕調用此方法
    /// </summary>
    public void RestartGame()
    {
        // 恢復時間流速
        Time.timeScale = 1f;

        // 重置工作數量（如果 WorkManager 存在）
        if (workManager != null)
        {
            workManager.ResetJobs();
        }

        // 重置遊戲狀態
        isGameWon = false;

        // 載入場景
        if (string.IsNullOrEmpty(restartSceneName))
        {
            // 重新載入當前場景
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // 載入指定場景
            SceneManager.LoadScene(restartSceneName);
        }

        if (enableDebug)
            Debug.Log("[GameWin] 遊戲重新開始");
    }

    /// <summary>
    /// 返回主選單（如果有的話）
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        
        // 這裡可以設定主選單場景名稱
        // SceneManager.LoadScene("MainMenu");
        
        if (enableDebug)
            Debug.Log("[GameWin] 返回主選單功能待實現");
    }

    /// <summary>
    /// 退出遊戲
    /// </summary>
    public void QuitGame()
    {
        if (enableDebug)
            Debug.Log("[GameWin] 退出遊戲");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// 獲取遊戲是否已勝利
    /// </summary>
    public bool IsGameWon()
    {
        return isGameWon;
    }
    
    /// <summary>
    /// 重置遊戲狀態（用於重新開始遊戲，不重新載入場景）
    /// </summary>
    public void ResetGameState()
    {
        if (enableDebug)
            Debug.Log("[GameWin] 🔄 開始重置遊戲狀態...");
        
        isGameWon = false;
        
        // 隱藏 UI
        if (gameWinUI != null)
        {
            gameWinUI.SetActive(false);
            if (enableDebug)
                Debug.Log("[GameWin] ✅ gameWinUI 已隱藏");
        }
        
        // 確保重新查找引用（以防引用丟失）
        if (workManager == null)
        {
            workManager = WorkManager.Instance;
            if (workManager == null)
            {
                workManager = FindObjectOfType<WorkManager>();
            }
        }
        
        // 無論 workManager 是否為 null，都重新訂閱事件（確保事件訂閱正確）
        if (workManager != null)
        {
            // 先移除舊的監聽器（避免重複訂閱）
            workManager.OnJobsChanged.RemoveListener(OnJobsChanged);
            // 重新訂閱
            workManager.OnJobsChanged.AddListener(OnJobsChanged);
            if (enableDebug)
                Debug.Log("[GameWin] ✅ 已重新訂閱 WorkManager.OnJobsChanged 事件");
        }
        else if (enableDebug)
        {
            Debug.LogWarning("[GameWin] ⚠️ workManager 為 null，無法訂閱事件");
        }
        
        if (workDayClock == null)
        {
            workDayClock = FindObjectOfType<WorkDayClock>();
        }
        
        if (enableDebug)
        {
            Debug.Log("[GameWin] ✅ 遊戲狀態已重置：isGameWon = false");
            Debug.Log($"[GameWin] 引用檢查：workManager={workManager != null}, workDayClock={workDayClock != null}");
            if (workManager != null)
                Debug.Log($"[GameWin] 當前 remainingJobs={workManager.GetRemainingJobs()}");
        }
        
        // 重置後立即檢查一次勝利條件（以防 remainingJobs 已經是 0）
        // 注意：這應該不會觸發，因為 ResetGameState() 通常在工作重置後調用
        // 但為了安全起見，還是檢查一次
        if (workManager != null && workDayClock != null)
        {
            int currentJobs = workManager.GetRemainingJobs();
            if (currentJobs == 0 && workDayClock.IsWorking())
            {
                if (enableDebug)
                    Debug.LogWarning("[GameWin] ⚠️ 重置後發現 remainingJobs=0，這不應該發生（工作應該已經重置）");
            }
        }
    }

    void OnDestroy()
    {
        // 取消訂閱事件
        if (workManager != null)
        {
            workManager.OnJobsChanged.RemoveListener(OnJobsChanged);
        }
    }
}
