using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameObject gameOverPanel;

    void Awake()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void ShowLoseUI()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);

        // 額外：解鎖滑鼠，方便點按鈕（第一人稱很常需要）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
