using UnityEngine;

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

    void LateUpdate()
    {
        if (!bounds) return;

        Transform bt = bounds.transform;

        // World -> Box local
        Vector3 pLocal = bt.InverseTransformPoint(transform.position);

        Vector3 half = bounds.size * 0.5f;
        Vector3 min = bounds.center - half + padding;
        Vector3 max = bounds.center + half - padding;

        if (clampX) pLocal.x = Mathf.Clamp(pLocal.x, min.x, max.x);
        if (clampY) pLocal.y = Mathf.Clamp(pLocal.y, min.y, max.y);
        if (clampZ) pLocal.z = Mathf.Clamp(pLocal.z, min.z, max.z);

        // Box local -> World
        transform.position = bt.TransformPoint(pLocal);
    }
}
