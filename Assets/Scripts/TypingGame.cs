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
