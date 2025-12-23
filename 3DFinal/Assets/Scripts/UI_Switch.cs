using UnityEngine;

public class UI_Switch : MonoBehaviour
{
    [Header("Assign")]
    public Transform cam;          // Main Camera transform
    public GameObject ui0;         // 0-degree UI
    public GameObject ui90;        // 90-degree UI

    [Header("Angle Detect")]
    public float tolerance = 6f;   // 允許誤差（角度接近多少算到位）

    private int lastState = -1;    // 0 = ui0, 1 = ui90

    void Start()
    {
        // 沒拖 Camera 的話，嘗試自動抓 MainCamera
        if (!cam && Camera.main) cam = Camera.main.transform;

        // 啟動時先刷新一次（避免一開始顯示錯）
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (!cam || !ui0 || !ui90) return;

        float y = NormalizeAngle(cam.eulerAngles.y);

        int state = GetState(y);
        if (state == -1) return; // 不在 0/90 附近就不切（避免轉動中一直跳）

        if (state != lastState)
        {
            lastState = state;
            ui0.SetActive(state == 0);
            ui90.SetActive(state == 1);
        }
    }

    int GetState(float y)
    {
        // 0 度附近
        if (Mathf.Abs(y - 0f) <= tolerance || Mathf.Abs(y - 360f) <= tolerance) return 0;

        // 90 度附近
        if (Mathf.Abs(y - 270f) <= tolerance) return 1;

        return -1;
    }

    float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a < 0f) a += 360f;
        return a;
    }
}
