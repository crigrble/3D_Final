using UnityEngine;
using TMPro;

/// <summary>
/// 文字呼吸燈效果
/// 讓文字透明度像呼吸燈一樣時高時低
/// </summary>
public class TextBreathingEffect : MonoBehaviour
{
    [Header("呼吸效果設定")]
    [Tooltip("呼吸速度（每秒循環次數）")]
    [Range(0.1f, 5f)]
    public float breathingSpeed = 1f;
    
    [Tooltip("最小透明度（0 = 完全透明，1 = 完全不透明）")]
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;
    
    [Tooltip("最大透明度（0 = 完全透明，1 = 完全不透明）")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;
    
    [Tooltip("是否在開始時啟用效果")]
    public bool playOnStart = true;
    
    [Header("調試")]
    [Tooltip("啟用調試日誌")]
    public bool enableDebug = false;
    
    private TextMeshProUGUI tmpText;
    private UnityEngine.UI.Text legacyText;
    private bool isPlaying = false;
    private float currentTime = 0f;
    
    void Start()
    {
        // 嘗試獲取 TextMeshPro 組件
        tmpText = GetComponent<TextMeshProUGUI>();
        
        // 如果沒有 TextMeshPro，嘗試獲取普通 Text 組件
        if (tmpText == null)
        {
            legacyText = GetComponent<UnityEngine.UI.Text>();
        }
        
        // 檢查是否找到文字組件
        if (tmpText == null && legacyText == null)
        {
            Debug.LogError($"[TextBreathingEffect] ❌ GameObject '{gameObject.name}' 上沒有找到 TextMeshProUGUI 或 Text 組件！");
            enabled = false;
            return;
        }
        
        if (enableDebug)
        {
            string textType = tmpText != null ? "TextMeshProUGUI" : "Text";
            Debug.Log($"[TextBreathingEffect] ✅ 找到 {textType} 組件，呼吸效果已準備就緒");
            Debug.Log($"[TextBreathingEffect] 參數：速度={breathingSpeed}, 透明度範圍={minAlpha}-{maxAlpha}, 自動播放={playOnStart}");
        }
        
        // 如果設置為開始時播放，立即啟用
        if (playOnStart)
        {
            isPlaying = true;
            if (enableDebug)
                Debug.Log("[TextBreathingEffect] ▶️ 自動開始呼吸效果");
        }
    }
    
    void Update()
    {
        if (!isPlaying) return;
        
        // 使用 unscaledDeltaTime，這樣即使 Time.timeScale = 0 時也能正常工作
        currentTime += Time.unscaledDeltaTime * breathingSpeed;
        
        // 使用正弦波計算透明度（範圍從 -1 到 1，然後映射到 minAlpha 到 maxAlpha）
        float normalizedValue = (Mathf.Sin(currentTime * 2f * Mathf.PI) + 1f) * 0.5f; // 0 到 1
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedValue);
        
        // 更新文字透明度
        if (tmpText != null)
        {
            Color color = tmpText.color;
            color.a = alpha;
            tmpText.color = color;
        }
        else if (legacyText != null)
        {
            Color color = legacyText.color;
            color.a = alpha;
            legacyText.color = color;
        }
        
        // 調試輸出（每 60 幀輸出一次，避免刷屏）
        if (enableDebug && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[TextBreathingEffect] 當前透明度: {alpha:F2}, 時間: {currentTime:F2}");
        }
    }
    
    /// <summary>
    /// 開始呼吸效果
    /// </summary>
    public void Play()
    {
        isPlaying = true;
        currentTime = 0f;
        
        if (enableDebug)
            Debug.Log("[TextBreathingEffect] ▶️ 開始呼吸效果");
    }
    
    /// <summary>
    /// 停止呼吸效果
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        
        // 恢復到最大透明度
        float alpha = maxAlpha;
        if (tmpText != null)
        {
            Color color = tmpText.color;
            color.a = alpha;
            tmpText.color = color;
        }
        else if (legacyText != null)
        {
            Color color = legacyText.color;
            color.a = alpha;
            legacyText.color = color;
        }
        
        if (enableDebug)
            Debug.Log("[TextBreathingEffect] ⏹️ 停止呼吸效果");
    }
    
    /// <summary>
    /// 暫停/恢復呼吸效果
    /// </summary>
    public void Toggle()
    {
        if (isPlaying)
            Stop();
        else
            Play();
    }
    
    /// <summary>
    /// 設置呼吸速度
    /// </summary>
    /// <param name="speed">呼吸速度（每秒循環次數）</param>
    public void SetBreathingSpeed(float speed)
    {
        breathingSpeed = Mathf.Clamp(speed, 0.1f, 5f);
        
        if (enableDebug)
            Debug.Log($"[TextBreathingEffect] 🔧 呼吸速度設置為: {breathingSpeed}");
    }
    
    /// <summary>
    /// 設置透明度範圍
    /// </summary>
    /// <param name="min">最小透明度</param>
    /// <param name="max">最大透明度</param>
    public void SetAlphaRange(float min, float max)
    {
        minAlpha = Mathf.Clamp01(min);
        maxAlpha = Mathf.Clamp01(max);
        
        // 確保 min <= max
        if (minAlpha > maxAlpha)
        {
            float temp = minAlpha;
            minAlpha = maxAlpha;
            maxAlpha = temp;
        }
        
        if (enableDebug)
            Debug.Log($"[TextBreathingEffect] 🔧 透明度範圍設置為: {minAlpha} - {maxAlpha}");
    }
}

