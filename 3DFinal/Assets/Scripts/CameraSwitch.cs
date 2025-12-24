using UnityEngine;
using System.Collections;

public class CameraSwitch : MonoBehaviour
{
    [Header("Target")]
    public Transform mainCamera;     // �� Main Camera �� Transform

    [Header("Angles (degrees)")]
    public float angleA = 0f;        // �_�l��
    public float angleB = -90f;      // ������

    [Header("Smooth")]
    public float duration = 0.25f;   // 0 = ����F>0 = ����

    bool toggled = false;            // false => angleA, true => angleB
    bool isRotating = false;
    
    // 靜態屬性：用於外部檢查是否處於 Office 模式
    public static bool IsInOffice { get; private set; } = false;

    void Reset()
    {
        // �۰ʧ�D�۾��]�p�G���������^
        if (Camera.main != null) mainCamera = Camera.main.transform;
    }
    
    void Awake()
    {
        // 初始化靜態屬性：0度時手消失，不計分
        IsInOffice = !toggled;
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
        IsInOffice = !toggled;  // 更新靜態屬性：0度時手消失，不計分
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
