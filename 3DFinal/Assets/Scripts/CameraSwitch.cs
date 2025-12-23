using UnityEngine;
using System.Collections;

public class CameraSwitch : MonoBehaviour
{
    [Header("Target")]
    public Transform mainCamera;     // 拖 Main Camera 的 Transform

    [Header("Angles (degrees)")]
    public float angleA = 0f;        // 起始角
    public float angleB = -90f;      // 切換角

    [Header("Smooth")]
    public float duration = 0.25f;   // 0 = 瞬轉；>0 = 平滑

    bool toggled = false;            // false => angleA, true => angleB
    bool isRotating = false;

    void Reset()
    {
        // 自動抓主相機（如果場景中有）
        if (Camera.main != null) mainCamera = Camera.main.transform;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (!mainCamera || isRotating) return;

        toggled = !toggled;
        float targetYaw = toggled ? angleB : angleA;

        if (duration <= 0f)
        {
            SetYawInstant(targetYaw);
        }
        else
        {
            StartCoroutine(RotateYawSmooth(targetYaw));
        }
    }

    void SetYawInstant(float targetYaw)
    {
        Vector3 e = mainCamera.eulerAngles;
        mainCamera.rotation = Quaternion.Euler(e.x, targetYaw, e.z);
    }

    IEnumerator RotateYawSmooth(float targetYaw)
    {
        isRotating = true;

        Quaternion start = mainCamera.rotation;
        Quaternion end = Quaternion.Euler(
            mainCamera.eulerAngles.x,
            targetYaw,
            mainCamera.eulerAngles.z
        );

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(duration, 0.0001f);
            mainCamera.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }

        mainCamera.rotation = end;
        isRotating = false;
    }
}
