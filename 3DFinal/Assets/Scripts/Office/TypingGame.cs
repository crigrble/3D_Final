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
        Debug.Log("Finished one sentence.");
        sentenceIndex++;

        if (sentenceIndex < sentences.Length)
        {
            StartTyping(sentences[sentenceIndex]);
        }
        else
        {
            Debug.Log("All sentences completed!");
            targetText.text = "工作完成！";
            
            // 通知 WorkManager 減少剩餘工作
            if (WorkManager.Instance != null)
            {
                WorkManager.Instance.CompleteJob();
            }
            else
            {
                Debug.LogWarning("⚠️ WorkManager.Instance 為 null，無法減少剩餘工作數量");
            }
        }
    }
}