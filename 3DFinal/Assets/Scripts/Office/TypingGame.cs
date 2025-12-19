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
        if (sentences.Length > 0)
            StartTyping(sentences[0]);

    }

    public void StartTyping(string text)
    {
        currentSentence = text.ToLower();
        index = 0;
        isTyping = true;

        targetText.text = currentSentence;
    }
    public float GetRemainingWorkRatio()
    {
        // 沒有句子就當作沒有工作
        if (sentences == null || sentences.Length == 0) return 0f;

        // 全部做完
        if (sentenceIndex >= sentences.Length) return 0f;

        // 計算總字數（全部句子的長度加總）
        int totalChars = 0;
        for (int i = 0; i < sentences.Length; i++)
        {
            if (!string.IsNullOrEmpty(sentences[i]))
                totalChars += sentences[i].Length;
        }
        if (totalChars <= 0) return 0f;

        // 計算已完成字數：
        // 1) 完成的句子：整句長度都算完成
        int doneChars = 0;
        for (int i = 0; i < sentenceIndex; i++)
        {
            if (!string.IsNullOrEmpty(sentences[i]))
                doneChars += sentences[i].Length;
        }

        // 2) 目前正在打的句子：用 index 代表已打對的字數
        // index 是你現在句子的進度（0 ~ currentSentence.Length）
        doneChars += Mathf.Clamp(index, 0, currentSentence != null ? currentSentence.Length : 0);

        // 剩餘比例 = 1 - 完成比例
        float doneRatio = (float)doneChars / totalChars;
        return Mathf.Clamp01(1f - doneRatio);
    }

    void Update()
    {
        if (!isTyping) return;

        foreach (char c in Input.inputString)
        {
            if (index >= currentSentence.Length) return;

            if (c == currentSentence[index])
            {
                index++;
                UpdateTextColor();

                if (index == currentSentence.Length)
                {
                    isTyping = false;
                    OnSentenceComplete();
                }
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
        }
    }
}
