using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultPanelController : MonoBehaviour
{
    [Tooltip("要重開的遊戲場景名稱（例如 Main 或 GameScene）")]
    public string gameSceneName = "GameScene";

    public void Show()
    {
        gameObject.SetActive(true);
        // 分數文字會由 GameManager_fish 自動更新（Tag=ResultScoreUI）
    }

    public void Restart()
    {
        if (GameManager_fish.Instance != null)
            GameManager_fish.Instance.ResetScore();

        SceneManager.LoadScene(gameSceneName);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
