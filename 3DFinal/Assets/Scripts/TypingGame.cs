using UnityEngine;
using TMPro;
using System.Linq;

public class TypingGame : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI remainingWorkText;

    [Header("Text Pool")]
    [TextArea]
    public string[] sentences;

    [Header("Game Setting")]
    public int jobsPerGame = 7;

    // 本局資料
    private string[] selectedSentences;
    private int sentenceIndex = 0;

    // 打字狀態
    private string currentSentence;
    private int index = 0;
    private bool isTyping = false;
    private bool hasError = false;

    void Start()
    {
        PrepareNewGame();
    }

    // 初始化
    void PrepareNewGame()
    {
        if (sentences == null || sentences.Length < jobsPerGame)
        {
            Debug.LogError("Not enough sentences for this game.");
            enabled = false;
            return;
        }

        // 隨機抽工作內容
        selectedSentences = sentences
            .OrderBy(x => Random.value)
            .Take(jobsPerGame)
            .ToArray();

        sentenceIndex = 0;
        UpdateRemainingWorkUI();

        StartTyping(selectedSentences[sentenceIndex]);
    }

    void StartTyping(string text)
    {
        // 統一換行格式
        currentSentence = text.Replace("\r\n", "\n").Replace("\r", "\n");

        index = 0;
        hasError = false;
        isTyping = true;

        SkipNewLines();
        UpdateTextDisplay();
    }

    void Update()
    {
        if (!isTyping) return;

        SkipNewLines();

        foreach (char inputChar in Input.inputString)
        {
            // Backspace
            if (inputChar == '\b')
            {
                HandleBackspace();
                continue;
            }

            if (index >= currentSentence.Length) continue;

            char targetChar = currentSentence[index];

            // 比對（大小寫沒差）
            if (char.ToLower(inputChar) == char.ToLower(targetChar))
            {
                index++;
                hasError = false;

                SkipNewLines();
                UpdateTextDisplay();

                if (index == currentSentence.Length)
                {
                    isTyping = false;
                    OnSentenceComplete();
                    return;
                }
            }
            else
            {
                hasError = true;
                UpdateTextDisplay();
            }
        }
    }

    void HandleBackspace()
    {
        if (hasError)
        {
            hasError = false;
        }
        else if (index > 0)
        {
            index--;
        }

        UpdateTextDisplay();
    }

    // 顯示文字
    void UpdateTextDisplay()
    {
        string correct = $"<color=#2EFF2E>{currentSentence.Substring(0, index)}</color>";

        string error = "";
        if (hasError && index < currentSentence.Length)
        {
            error = $"<color=#FF3B3B>{currentSentence[index]}</color>";
        }

        int remainStart = hasError ? index + 1 : index;
        string remain = remainStart < currentSentence.Length
            ? currentSentence.Substring(remainStart)
            : "";

        string cursor = "<color=#FFFFFF>|</color>";

        targetText.text = correct + error + cursor + remain;
    }

    // 完成一份工作
    void OnSentenceComplete()
    {
        sentenceIndex++;
        UpdateRemainingWorkUI();

        if (sentenceIndex < selectedSentences.Length)
        {
            StartTyping(selectedSentences[sentenceIndex]);
        }
        else
        {
            Debug.Log("All jobs completed!");
        }
    }

    void SkipNewLines()
    {
        while (index < currentSentence.Length && currentSentence[index] == '\n')
        {
            index++;
        }
    }


    /// 剩餘工作比例（0 ~ 1）
    public float GetRemainingWorkRatio()
    {
        if (jobsPerGame == 0) return 0f;
        return (float)(jobsPerGame - sentenceIndex) / jobsPerGame;
    }

    /// 剩餘工作數量
    public int GetRemainingJobs()
    {
        return jobsPerGame - sentenceIndex;
    }

    // UI
    void UpdateRemainingWorkUI()
    {
        int remaining = GetRemainingJobs();
        remainingWorkText.text = $"Remaining Tasks: {remaining}";
    }
}
