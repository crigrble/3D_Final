using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

/// <summary>
/// 結果面板控制器
/// 用於控制遊戲結果 UI（勝利/失敗）的顯示和操作
/// </summary>
public class ResultPanelController : MonoBehaviour
{
    [Header("場景設定")]
    [Tooltip("重新開始時要載入的場景名稱（留空則重新載入當前場景）")]
    public string gameSceneName = "GameScene";

    [Tooltip("主選單場景名稱（如果有的話）")]
    public string mainMenuSceneName = "";

    [Header("調試設定")]
    [Tooltip("啟用詳細的調試日誌")]
    public bool enableDebug = true;

    private bool isInitialized = false;
    private bool isPanelVisible = false;
    
    void Start()
    {
        // 標記為已初始化
        isInitialized = true;
        
        // 初始化時隱藏面板
        // 重要：如果 GameObject 在 Start 時是激活的，說明它可能是被外部腳本（如 GameWin）剛剛激活的
        // 在這種情況下，不應該隱藏，否則會導致 UI 無法顯示
        // 
        // Unity 的執行順序：
        // 1. GameWin.Start() -> gameWinUI.SetActive(false) -> ResultPanelController.Start() 不會執行（因為 GameObject 未激活）
        // 2. GameWin.Win() -> gameWinUI.SetActive(true) -> ResultPanelController.Start() 現在會執行
        // 3. 如果 ResultPanelController.Start() 調用 Hide()，會再次禁用 gameWinUI，導致 UI 無法顯示
        //
        // 解決方案：如果 GameObject 在 Start 時是激活的，說明它應該被顯示，不應該隱藏
        // 只有在 GameObject 一開始就是未激活狀態時，才保持未激活狀態
        if (gameObject.activeSelf)
        {
            // GameObject 是激活的，說明它應該被顯示
            // 可能是被 GameWin/GameOver 剛剛激活的，或者是場景中默認激活的
            // 無論如何，如果它是激活的，就不應該隱藏
            if (enableDebug)
            {
                // 檢查是否有 GameWin 或 GameOver 腳本在場景中（僅用於調試）
                GameWin gameWin = FindObjectOfType<GameWin>();
                GameOver gameOver = FindObjectOfType<GameOver>();
                
                if (gameWin != null || gameOver != null)
                {
                    Debug.Log($"[ResultPanelController] ✅ GameObject 是激活的，且找到 GameWin 或 GameOver 腳本，跳過 Start() 中的 Hide()");
                }
                else
                {
                    Debug.Log($"[ResultPanelController] ✅ GameObject 是激活的，跳過 Start() 中的 Hide()（即使未找到 GameWin/GameOver，也不隱藏已激活的 UI）");
                }
            }
            // 不調用 Hide()，保持激活狀態
        }
        else
        {
            // GameObject 已經是未激活狀態，不需要隱藏
            if (enableDebug)
            {
                Debug.Log($"[ResultPanelController] GameObject '{gameObject.name}' 已經是未激活狀態，跳過 Hide()");
            }
        }
    }

    void Update()
    {
        // 當面板顯示時，檢測空格鍵按下以重新開始遊戲
        // 注意：即使 isPanelVisible 為 false，如果 GameObject 是激活的，也應該能檢測到空格鍵
        // 這樣可以確保在場景重新載入後，如果面板被激活，空格鍵仍然有效
        if (gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Space))
        {
            // 只有在面板可見時才響應空格鍵（避免遊戲進行中誤觸）
            // 但也要檢查面板是否真的顯示（通過 activeInHierarchy 和 isPanelVisible）
            if (isPanelVisible || gameObject.activeSelf)
            {
                if (enableDebug)
                    Debug.Log("[ResultPanelController] 🔘 空格鍵被按下，重新開始遊戲");
                
                // 防止重複調用（在場景載入過程中）
                if (Time.timeScale > 0 || Time.unscaledTime > 0)
                {
                    RestartGame();
                }
            }
        }
    }
    
    /// <summary>
    /// 顯示結果面板
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        isPanelVisible = true;
        
        // 顯示鼠標並解鎖（讓玩家可以點擊按鈕）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // 確保 EventSystem 存在且激活（UI 按鈕點擊需要 EventSystem）
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            // 如果沒有 EventSystem，嘗試查找或創建一個
            eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                // 創建新的 EventSystem
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (enableDebug)
                    Debug.LogWarning("[ResultPanelController] ⚠️ 場景中沒有 EventSystem，已自動創建一個");
            }
            else
            {
                UnityEngine.EventSystems.EventSystem.current = eventSystem;
            }
        }
        
        if (eventSystem != null && !eventSystem.gameObject.activeSelf)
        {
            eventSystem.gameObject.SetActive(true);
            if (enableDebug)
                Debug.LogWarning("[ResultPanelController] ⚠️ EventSystem 被禁用，已重新激活");
        }
        
        // 確保 Canvas 有 GraphicRaycaster（用於檢測 UI 點擊）
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            UnityEngine.UI.GraphicRaycaster raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                if (enableDebug)
                    Debug.LogWarning("[ResultPanelController] ⚠️ Canvas 沒有 GraphicRaycaster，已自動添加");
            }
            else if (!raycaster.enabled)
            {
                raycaster.enabled = true;
                if (enableDebug)
                    Debug.LogWarning("[ResultPanelController] ⚠️ GraphicRaycaster 被禁用，已重新啟用");
            }
            
            // 確保 Canvas 的 Sorting Order 足夠高，避免被其他 UI 遮擋
            if (canvas.sortingOrder < 100)
            {
                canvas.sortingOrder = 100;
                if (enableDebug)
                    Debug.Log($"[ResultPanelController] 🔧 Canvas Sorting Order 已設置為 100，確保 UI 顯示在最上層");
            }
        }
        else if (enableDebug)
        {
            Debug.LogWarning("[ResultPanelController] ⚠️ 未找到 Canvas 組件");
        }
        
        // 確保所有按鈕都是可交互的，並檢查按鈕狀態
        UnityEngine.UI.Button[] buttons = GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var button in buttons)
        {
            if (!button.interactable)
            {
                button.interactable = true;
                if (enableDebug)
                    Debug.LogWarning($"[ResultPanelController] ⚠️ 按鈕 '{button.name}' 的 interactable 被禁用，已重新啟用");
            }
            
            // 詳細檢查按鈕狀態
            if (enableDebug)
            {
                RectTransform rectTransform = button.GetComponent<RectTransform>();
                UnityEngine.UI.Image image = button.GetComponent<UnityEngine.UI.Image>();
                
                Debug.Log($"[ResultPanelController] 🔍 按鈕 '{button.name}' 詳細信息：");
                Debug.Log($"  - interactable = {button.interactable}");
                Debug.Log($"  - enabled = {button.enabled}");
                Debug.Log($"  - activeSelf = {button.gameObject.activeSelf}");
                Debug.Log($"  - activeInHierarchy = {button.gameObject.activeInHierarchy}");
                
                if (rectTransform != null)
                {
                    Debug.Log($"  - RectTransform 位置: {rectTransform.position}");
                    Debug.Log($"  - RectTransform 大小: {rectTransform.sizeDelta}");
                    Debug.Log($"  - RectTransform anchoredPosition: {rectTransform.anchoredPosition}");
                    Debug.Log($"  - RectTransform localPosition: {rectTransform.localPosition}");
                    
                    // 檢查按鈕是否在屏幕可見範圍內
                    Vector3[] corners = new Vector3[4];
                    rectTransform.GetWorldCorners(corners);
                    Debug.Log($"  - RectTransform 世界座標角落: 左下={corners[0]}, 右下={corners[1]}, 右上={corners[2]}, 左上={corners[3]}");
                    
                    // 檢查按鈕是否在 Canvas 的渲染範圍內
                    if (canvas != null)
                    {
                        Rect canvasRect = canvas.pixelRect;
                        Debug.Log($"  - Canvas 像素範圍: {canvasRect}");
                        
                        // 將世界座標轉換為屏幕座標
                        Camera canvasCamera = canvas.worldCamera ?? Camera.main;
                        if (canvasCamera != null)
                        {
                            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]);
                            Debug.Log($"  - 按鈕左下角屏幕座標: {screenPos}");
                            
                            // 檢查是否在屏幕範圍內
                            bool isOnScreen = screenPos.x >= 0 && screenPos.x <= Screen.width && 
                                            screenPos.y >= 0 && screenPos.y <= Screen.height;
                            Debug.Log($"  - 按鈕是否在屏幕範圍內: {isOnScreen}");
                            
                            if (!isOnScreen)
                            {
                                Debug.LogWarning($"[ResultPanelController] ⚠️ 按鈕 '{button.name}' 不在屏幕可見範圍內！正在嘗試修復位置...");
                                
                                // 嘗試修復按鈕位置：將按鈕移動到屏幕可見範圍內
                                RectTransform buttonRect = button.GetComponent<RectTransform>();
                                if (buttonRect != null && canvas != null)
                                {
                                    // 獲取 Canvas 的 RectTransform
                                    RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
                                    if (canvasRectTransform != null)
                                    {
                                        // 根據 Canvas 的 Render Mode 使用不同的修復策略
                                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                                        {
                                            // Screen Space - Overlay: 使用 anchoredPosition
                                            // 將按鈕設置為 Canvas 下方中央位置
                                            buttonRect.anchorMin = new Vector2(0.5f, 0f);
                                            buttonRect.anchorMax = new Vector2(0.5f, 0f);
                                            buttonRect.pivot = new Vector2(0.5f, 0.5f);
                                            buttonRect.anchoredPosition = new Vector2(0, 100); // 距離底部 100 像素
                                            
                                            // 確保 Z 軸為 0
                                            Vector3 localPos = buttonRect.localPosition;
                                            localPos.z = 0;
                                            buttonRect.localPosition = localPos;
                                        }
                                        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                                        {
                                            // Screen Space - Camera: 也需要使用 anchoredPosition
                                            buttonRect.anchorMin = new Vector2(0.5f, 0f);
                                            buttonRect.anchorMax = new Vector2(0.5f, 0f);
                                            buttonRect.pivot = new Vector2(0.5f, 0.5f);
                                            buttonRect.anchoredPosition = new Vector2(0, 100);
                                            
                                            // 確保 Z 軸在 Canvas 的 planeDistance 範圍內
                                            if (canvas.worldCamera != null)
                                            {
                                                Vector3 localPos = buttonRect.localPosition;
                                                localPos.z = 0;
                                                buttonRect.localPosition = localPos;
                                            }
                                        }
                                        else
                                        {
                                            // World Space: 使用世界座標
                                            // 這種情況下，需要根據相機位置計算
                                            if (canvas.worldCamera != null)
                                            {
                                                // 將按鈕放在相機前方
                                                Vector3 worldPos = canvas.worldCamera.transform.position + canvas.worldCamera.transform.forward * canvas.planeDistance;
                                                buttonRect.position = worldPos;
                                            }
                                        }
                                        
                                        Debug.LogWarning($"[ResultPanelController] 🔧 已嘗試修復按鈕 '{button.name}' 位置 (Canvas Render Mode: {canvas.renderMode})");
                                        
                                        // 強制更新 RectTransform
                                        Canvas.ForceUpdateCanvases();
                                        
                                        // 重新檢查是否在屏幕範圍內
                                        buttonRect.GetWorldCorners(corners);
                                        if (canvasCamera != null)
                                        {
                                            screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]);
                                            isOnScreen = screenPos.x >= 0 && screenPos.x <= Screen.width && 
                                                        screenPos.y >= 0 && screenPos.y <= Screen.height;
                                            
                                            if (isOnScreen)
                                            {
                                                Debug.Log($"[ResultPanelController] ✅ 按鈕 '{button.name}' 現在在屏幕範圍內！新位置: anchoredPosition={buttonRect.anchoredPosition}");
                                            }
                                            else
                                            {
                                                Debug.LogError($"[ResultPanelController] ❌ 自動修復失敗。按鈕屏幕座標: {screenPos}，請在 Unity Editor 中手動調整按鈕的 RectTransform 設置。");
                                                Debug.LogError($"[ResultPanelController] 💡 建議：將按鈕的 Anchor Presets 設置為 'Bottom Center'，Pos Y 設置為 100-200");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                if (image != null)
                {
                    Debug.Log($"  - Image raycastTarget = {image.raycastTarget}");
                    Debug.Log($"  - Image color alpha = {image.color.a}");
                }
                
                // 檢查按鈕的 OnClick 事件是否有監聽器
                int listenerCount = button.onClick.GetPersistentEventCount();
                Debug.Log($"  - OnClick 事件監聽器數量 = {listenerCount}");
                if (listenerCount == 0)
                {
                    Debug.LogWarning($"[ResultPanelController] ⚠️ 按鈕 '{button.name}' 的 OnClick 事件沒有監聽器！請在 Inspector 中添加事件。");
                }
            }
        }
        
        if (enableDebug && buttons.Length > 0)
        {
            Debug.Log($"[ResultPanelController] ✅ 找到 {buttons.Length} 個按鈕，已確保所有按鈕可交互");
        }
        else if (enableDebug && buttons.Length == 0)
        {
            Debug.LogWarning("[ResultPanelController] ⚠️ 未找到任何按鈕！請確認按鈕是 gameWinUI/gameOverUI 的子對象。");
        }
        
        // 更新結算畫面分數（如果 GameManager_fish 存在）
        if (GameManager_fish.Instance != null)
        {
            var resultObj = GameObject.FindGameObjectWithTag("ResultScoreUI");
            if (resultObj != null)
            {
                var resultText = resultObj.GetComponent<TMPro.TextMeshProUGUI>();
                if (resultText != null)
                {
                    int finalScore = GameManager_fish.Instance.GetCurrentScore();
                    resultText.text = "Final Score: " + finalScore;
                    if (enableDebug)
                        Debug.Log($"[ResultPanelController] ✅ 結算畫面分數已更新：{finalScore}");
                }
            }
            else if (enableDebug)
            {
                Debug.LogWarning("[ResultPanelController] ⚠️ 未找到 Tag=ResultScoreUI 的物件");
            }
        }
        
        if (enableDebug)
        {
            Debug.Log("[ResultPanelController] ✅ 結果面板已顯示，鼠標已解鎖");
            Debug.Log($"[ResultPanelController] ✅ EventSystem 狀態：{(eventSystem != null ? "存在且激活" : "不存在")}");
            Debug.Log($"[ResultPanelController] ✅ Canvas 狀態：{(canvas != null ? $"存在，Sorting Order = {canvas.sortingOrder}" : "不存在")}");
        }
    }

    /// <summary>
    /// 隱藏結果面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        isPanelVisible = false;
        // 注意：隱藏時不鎖定鼠標，因為可能還在遊戲中
        if (enableDebug)
            Debug.Log("[ResultPanelController] 結果面板已隱藏");
    }

    /// <summary>
    /// 重新開始遊戲
    /// 可以從 UI 按鈕調用此方法
    /// </summary>
    public void Restart()
    {
        if (enableDebug)
            Debug.Log("[ResultPanelController] 🔘 Restart() 按鈕被點擊！");
        RestartGame();
    }
    
    /// <summary>
    /// 測試按鈕點擊（用於調試）
    /// 如果這個方法被調用，說明按鈕點擊事件正常工作
    /// </summary>
    public void TestButtonClick()
    {
        Debug.Log("[ResultPanelController] ✅✅✅ 測試按鈕被點擊！按鈕事件正常工作！");
    }

    /// <summary>
    /// 重新開始遊戲（完整版本）
    /// 這個方法會重置所有遊戲狀態，但不重新載入場景
    /// </summary>
    public void RestartGame()
    {
        if (enableDebug)
            Debug.Log("[ResultPanelController] 🔄 開始重新遊戲（不重新載入場景）...");

        // 立即設置標誌，防止重複調用
        isPanelVisible = false;

        // 恢復時間流速
        Time.timeScale = 1f;

        // 重置分數（如果 GameManager_fish 存在）
        if (GameManager_fish.Instance != null)
        {
            GameManager_fish.Instance.ResetScore();
            if (enableDebug)
                Debug.Log("[ResultPanelController] ✅ 分數已重置");
        }

        // 重要：先重置遊戲狀態，再重置工作數量
        // 這樣當 ResetJobs() 觸發 OnJobsChanged 事件時，GameWin 已經準備好接收事件了
        GameWin gameWin = FindObjectOfType<GameWin>();
        if (gameWin != null)
        {
            gameWin.ResetGameState();
            if (enableDebug)
                Debug.Log("[ResultPanelController] ✅ GameWin 狀態已重置（在重置工作數量之前）");
        }

        GameOver gameOver = FindObjectOfType<GameOver>();
        if (gameOver != null)
        {
            gameOver.ResetGameState();
            if (enableDebug)
                Debug.Log("[ResultPanelController] ✅ GameOver 狀態已重置（在重置工作數量之前）");
        }

        // 重置工作數量（如果 WorkManager 存在）
        // 注意：這會觸發 OnJobsChanged 事件，所以必須在 GameWin/GameOver 狀態重置之後
        if (WorkManager.Instance != null)
        {
            int jobsBeforeReset = WorkManager.Instance.GetRemainingJobs();
            WorkManager.Instance.ResetJobs();
            int jobsAfterReset = WorkManager.Instance.GetRemainingJobs();
            
            if (enableDebug)
            {
                Debug.Log($"[ResultPanelController] ✅ 工作數量已重置：{jobsBeforeReset} -> {jobsAfterReset}");
                Debug.Log($"[ResultPanelController] WorkManager.OnJobsChanged 事件應該已觸發，GameWin 應該能接收事件");
            }
        }

        // 隱藏當前 UI
        Hide();

        if (enableDebug)
            Debug.Log("[ResultPanelController] ✅ 遊戲狀態已重置，可以重新開始遊戲");
    }

    /// <summary>
    /// 返回主選單
    /// 可以從 UI 按鈕調用此方法
    /// </summary>
    public void ReturnToMainMenu()
    {
        // 恢復時間流速
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            if (enableDebug)
                Debug.LogWarning("[ResultPanelController] ⚠️ 主選單場景名稱未設定！");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
        if (enableDebug)
            Debug.Log($"[ResultPanelController] 返回主選單：{mainMenuSceneName}");
    }

    /// <summary>
    /// 退出遊戲
    /// 可以從 UI 按鈕調用此方法
    /// </summary>
    public void QuitGame()
    {
        if (enableDebug)
            Debug.Log("[ResultPanelController] 退出遊戲");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
