using UnityEngine;
using UnityEngine.UI;

public class ResultUI : MonoBehaviour
{
    [Header("UI Refs")]
    public CanvasGroup root;     // 整個面板（建議加 CanvasGroup）
    public Text titleText;       // 大字：CAUGHT YOU! / YOU ESCAPED!
    public Text detailText;      // 小字：YOU'RE FIRED... / GOOD JOB.
    public Button replayButton;  // 可選

    [Header("Success Text")]
    public string successTitle = "YOU ESCAPED!";
    public string successDetail = "GOOD JOB.";

    [Header("Fail Text")]
    public string failTitle = "CAUGHT YOU!";
    public string failDetail = "YOU'RE FIRED, LOSER.";

    void Awake()
    {
        HideInstant(); // 避免一開始閃一下
    }

    public void ShowResult(bool success)
    {
        if (titleText) titleText.text = success ? successTitle : failTitle;
        if (detailText) detailText.text = success ? successDetail : failDetail;

        Show();
    }

    public void Show()
    {
        if (!root) return;
        root.alpha = 1;
        root.blocksRaycasts = true;
        root.interactable = true;
    }

    public void HideInstant()
    {
        if (!root) return;
        root.alpha = 0;
        root.blocksRaycasts = false;
        root.interactable = false;
    }
}
