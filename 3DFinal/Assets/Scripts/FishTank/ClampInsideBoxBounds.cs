using UnityEngine;

[DefaultExecutionOrder(10000)] // 讓 Clamp 盡量最後跑（避免被追蹤腳本蓋回去）
public class ClampInsideOrientedBox : MonoBehaviour
{
    [Header("Bounds")]
    public BoxCollider bounds;

    [Header("Padding (local space)")]
    public Vector3 padding = Vector3.zero;

    [Header("Clamp Axes (Box local space)")]
    public bool clampX = true;
    public bool clampY = true;
    public bool clampZ = true;

    [Header("Debug")]
    public bool debugLog = false;
    public float logInterval = 0.5f;
    float t;

    void LateUpdate()
    {
        if (!bounds) return;

        Transform bt = bounds.transform;

        // World -> Box local
        Vector3 pLocal = bt.InverseTransformPoint(transform.position);

        Vector3 half = bounds.size * 0.5f;
        Vector3 min = bounds.center - half + padding;
        Vector3 max = bounds.center + half - padding;

        Vector3 before = pLocal;

        if (clampX) pLocal.x = Mathf.Clamp(pLocal.x, min.x, max.x);
        if (clampY) pLocal.y = Mathf.Clamp(pLocal.y, min.y, max.y);
        if (clampZ) pLocal.z = Mathf.Clamp(pLocal.z, min.z, max.z);

        // Box local -> World
        transform.position = bt.TransformPoint(pLocal);

        if (debugLog)
        {
            t += Time.deltaTime;
            if (t >= logInterval)
            {
                t = 0f;
                Debug.Log(
                    $"[ClampInsideOrientedBox]\n" +
                    $"- using bounds: {bounds.name} (path: {GetPath(bounds.transform)})\n" +
                    $"- bounds.center={bounds.center}, bounds.size={bounds.size}, lossyScale={bt.lossyScale}\n" +
                    $"- min={min}, max={max}\n" +
                    $"- pLocal before={before}, after={pLocal}"
                );
            }
        }
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
