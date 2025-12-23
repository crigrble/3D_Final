using UnityEngine;

/// <summary>
/// 把「追蹤寫進 Wrist 的位移」轉移到 HandRig（父物件），修正左右/前後軸亂掉的問題。
/// 用法：掛在 Wrist 上，指定 rigRoot = HandRig。
/// 原理：
/// 1) 讀取 Wrist 被追蹤寫入的 localPosition（raw）
///— 2) 依需求做軸交換 / 反向 / 縮放
/// 3) 把結果套到 HandRig.localPosition
/// 4) 把 Wrist.localPosition 拉回初始值，避免骨架被拉扯變形
/// </summary>
public class WristToHandRigRemap : MonoBehaviour
{
    [Header("Assign")]
    [Tooltip("HandRig（Wrist 的父物件或更上層的手 root）。")]
    public Transform rigRoot;

    [Header("Scale")]
    [Tooltip("左右移動倍率（對應 tracker 的 X）。")]
    public float scaleX = 1f;

    [Tooltip("上下移動倍率（對應 tracker 的 Y）。")]
    public float scaleY = 1f;

    [Tooltip("前後/深度倍率（對應 tracker 的 Z）。負數可反轉深度方向。")]
    public float scaleZ = 1f;

    [Header("Axis Remap (最常用修正)")]
    [Tooltip("勾選後：把 tracker 的 X/Z 對調（修『左右變前後』最常用）。")]
    public bool swapXZ = true;

    [Tooltip("勾選後：反轉 X（左右反向）。")]
    public bool invertX = false;

    [Tooltip("勾選後：反轉 Y（上下反向）。")]
    public bool invertY = false;

    [Tooltip("勾選後：反轉 Z（深度反向）。也可用 scaleZ 負號達成。")]
    public bool invertZ = false;

    [Header("Debug")]
    public bool debugLog = false;
    public float logInterval = 0.5f;

    Vector3 wristLocalOrigin;
    Vector3 rigLocalOrigin;
    float t;

    void Reset()
    {
        // 預設抓父物件當 rigRoot（通常就是 HandRig）
        if (transform.parent != null) rigRoot = transform.parent;
        // 你現在描述「左右變前後」最常見就是 swapXZ 要打開
        swapXZ = true;
    }

    void Start()
    {
        if (!rigRoot)
        {
            Debug.LogError("[WristToHandRigRemap] rigRoot 沒指定。請把 HandRig 拖進去。");
            enabled = false;
            return;
        }

        wristLocalOrigin = transform.localPosition;
        rigLocalOrigin = rigRoot.localPosition;
    }

    void LateUpdate()
    {
        // 追蹤通常會在 Update 把 Wrist.localPosition 改掉
        // 我們在 LateUpdate 讀「追蹤結果」，轉移到 HandRig
        Vector3 raw = transform.localPosition - wristLocalOrigin;

        // 1) 先做縮放（讓移動量正常）
        raw = new Vector3(raw.x * scaleX, raw.y * scaleY, raw.z * scaleZ);

        // 2) 軸交換（修左右變前後）
        Vector3 mapped = raw;
        if (swapXZ)
            mapped = new Vector3(raw.z, raw.y, raw.x);

        // 3) 反向（必要時）
        if (invertX) mapped.x *= -1f;
        if (invertY) mapped.y *= -1f;
        if (invertZ) mapped.z *= -1f;

        // 4) 把位移加到 HandRig 上
        rigRoot.localPosition = rigLocalOrigin + mapped;

        // 5) 把 Wrist 拉回原位，避免骨架被拉扯/變形
        transform.localPosition = wristLocalOrigin;

        // Debug
        if (debugLog)
        {
            t += Time.deltaTime;
            if (t >= logInterval)
            {
                t = 0f;
                Debug.Log($"[Remap] raw={raw} mapped={mapped} rigPos={rigRoot.localPosition}");
            }
        }
    }
}
