using UnityEngine;
using TMPro;

public class TypingGame : MonoBehaviour
{
    public TextMeshProUGUI targetText;

    [TextArea]
    public string[] sentences;
    private int sentenceIndex = 0;

    private string currentSentence;
    private int index = 0;
    private bool isTyping = false;

    void Start()
    {
        if (targetText != null)
        {
            targetText.enableWordWrapping = true;
        }

        if (sentences.Length > 0)
            StartTyping(sentences[0]);
    }

    public void StartTyping(string text)
    {
        // 1. 資料層：保留換行，但統一格式為 \n (Code 10)
        string cleanText = text;
        cleanText = cleanText.Replace("\r\n", "\n").Replace("\r", "\n");
        cleanText = cleanText.Trim();
        currentSentence = cleanText.ToLower(); // 依需求轉小寫

        index = 0;
        isTyping = true;
        targetText.text = currentSentence;

        // Debug: 印出題目中每個字的編碼
        // string debugCodes = "";
        // foreach (char c in currentSentence)
        // {
        //     if (c == '\n') debugCodes += "[Enter/10]";
        //     else debugCodes += $"{(int)c}|";
        // }
        // Debug.Log($"[題目載入] 編碼檢查: {debugCodes}");
    }

    public float GetRemainingWorkRatio()
    {
        if (sentences == null || sentences.Length == 0) return 0f;
        if (sentenceIndex >= sentences.Length) return 0f;

        int totalChars = 0;
        for (int i = 0; i < sentences.Length; i++)
        {
            if (!string.IsNullOrEmpty(sentences[i]))
                totalChars += sentences[i].Length;
        }
        if (totalChars <= 0) return 0f;

        int doneChars = 0;
        for (int i = 0; i < sentenceIndex; i++)
        {
            if (!string.IsNullOrEmpty(sentences[i]))
                doneChars += sentences[i].Length;
        }

        doneChars += Mathf.Clamp(index, 0, currentSentence != null ? currentSentence.Length : 0);

        float doneRatio = (float)doneChars / totalChars;
        return Mathf.Clamp01(1f - doneRatio);
    }

    void Update()
    {
        // 快速完成熱鍵：按下 ALT 鍵時，完成當前句子並減少 remainingJobs
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            if (isTyping && currentSentence != null && index < currentSentence.Length)
            {
                Debug.Log("[TypingGame] ⚡ ALT 鍵被按下，快速完成當前句子");
                // 直接完成當前句子
                index = currentSentence.Length;
                UpdateTextColor();
                isTyping = false;
                OnSentenceComplete();
                return;
            }
        }

        if (!isTyping) return;

        foreach (char inputChar in Input.inputString)
        {
            // 輸入的ASCII碼
            Debug.Log($"[輸入偵測] 字元: '{(inputChar == '\r' ? "\\r" : inputChar == '\n' ? "\\n" : inputChar.ToString())}' | ASCII: {(int)inputChar}");

            if (index >= currentSentence.Length) return;

            char targetChar = currentSentence[index];
            char playerChar = inputChar;

            // 將 Enter 鍵統一視為 \n
            if (playerChar == '\r' || playerChar == '\n')
            {
                playerChar = '\n';
            }

            if (playerChar == targetChar)
            {
                index++;
                UpdateTextColor();

                if (index == currentSentence.Length)
                {
                    isTyping = false;
                    OnSentenceComplete();
                }
            }
            else
            {
                // 加碼：如果你按錯了，告訴你錯在哪
                // Debug.Log($"[打錯了] 系統在等 ASCII:{(int)targetChar} ('{targetChar}')");
            }
        }
    }

    void UpdateTextColor()
    {
        string correct = $"<color=#2EFF2E>{currentSentence.Substring(0, index)}</color>";
        string remain = currentSentence.Substring(index);

        targetText.text = correct + remain;
    }

    void OnSentenceComplete()
    {
        Debug.Log($"[TypingGame] OnSentenceComplete() 被調用，當前 sentenceIndex={sentenceIndex}，sentences.Length={sentences.Length}");
        sentenceIndex++;
        Debug.Log($"[TypingGame] sentenceIndex 已遞增為 {sentenceIndex}");

        // 每完成一個句子（切換句子），就減少一次 remaining jobs
        Debug.Log($"[TypingGame] ✅ 完成一個句子，準備通知 WorkManager 減少剩餘工作");
        Debug.Log($"[TypingGame] WorkManager.Instance 是否為 null：{WorkManager.Instance == null}");
        
        if (WorkManager.Instance != null)
        {
            Debug.Log("[TypingGame] ✅ WorkManager.Instance 存在，調用 CompleteJob()");
            WorkManager.Instance.CompleteJob();
        }
        else
        {
            Debug.LogError("[TypingGame] ⚠️ WorkManager.Instance 為 null！請確認場景中有 WorkManager 組件");
            Debug.LogError("[TypingGame] 提示：請在場景中添加一個 GameObject，並添加 WorkManager 組件");
        }

        if (sentenceIndex < sentences.Length)
        {
            Debug.Log($"[TypingGame] 還有更多句子，開始下一個句子");
            StartTyping(sentences[sentenceIndex]);
        }
        else
        {
            Debug.Log($"[TypingGame] ✅ All sentences completed! sentenceIndex={sentenceIndex}, sentences.Length={sentences.Length}");
            targetText.text = "工作完成！";
        }
    }
}