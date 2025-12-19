using UnityEngine;

/// <summary>
/// 实时显示手部追踪状态的调试UI
/// 将此脚本挂载到与 HandCollisionDetector 相同的物体上
/// </summary>
public class HandTrackingDebugUI : MonoBehaviour
{
    private HandCollisionDetector detector;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    
    void Start()
    {
        detector = GetComponent<HandCollisionDetector>();
        
        // 设置样式
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;
        labelStyle.padding = new RectOffset(5, 5, 2, 2);
        
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.yellow;
        titleStyle.padding = new RectOffset(5, 5, 2, 2);
    }
    
    void OnGUI()
    {
        if (detector == null) return;
        
        // 创建半透明背景
        GUI.Box(new Rect(5, 5, 400, 200), "");
        
        int y = 10;
        GUI.Label(new Rect(10, y, 400, 25), "【手部追踪调试信息】", titleStyle);
        y += 30;
        
        // 使用反射获取私有字段
        var type = typeof(HandCollisionDetector);
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        
        var handRoot = type.GetField("handRoot", bindingFlags)?.GetValue(detector) as Transform;
        var handRenderer = type.GetField("handRenderer", bindingFlags)?.GetValue(detector) as Renderer;
        var isReceivingData = (bool)(type.GetField("isReceivingData", bindingFlags)?.GetValue(detector) ?? false);
        var isHandVisible = (bool)(type.GetField("isHandVisible", bindingFlags)?.GetValue(detector) ?? false);
        var hasNewData = (bool)(type.GetField("_hasNewData", bindingFlags)?.GetValue(detector) ?? false);
        var smoothing = (float)(type.GetField("smoothing", bindingFlags)?.GetValue(detector) ?? 0f);
        var positionScale = (float)(type.GetField("positionScale", bindingFlags)?.GetValue(detector) ?? 0f);
        
        // 显示信息
        GUI.Label(new Rect(10, y, 400, 20), 
            $"handRoot 绑定: {(handRoot != null ? "✓ 已绑定 (" + handRoot.name + ")" : "✗ 未绑定")}", 
            labelStyle);
        y += 22;
        
        GUI.Label(new Rect(10, y, 400, 20), 
            $"handRenderer 绑定: {(handRenderer != null ? "✓ 已绑定" : "✗ 未绑定")}", 
            labelStyle);
        y += 22;
        
        GUI.Label(new Rect(10, y, 400, 20), 
            $"isReceivingData: {(isReceivingData ? "✓ 接收中" : "✗ 未接收")}", 
            labelStyle);
        y += 22;
        
        GUI.Label(new Rect(10, y, 400, 20), 
            $"_hasNewData: {(hasNewData ? "✓ 有新数据" : "✗ 无新数据")}", 
            labelStyle);
        y += 22;
        
        GUI.Label(new Rect(10, y, 400, 20), 
            $"手部可见: {(isHandVisible ? "✓ 可见" : "✗ 隐藏")}", 
            labelStyle);
        y += 22;
        
        if (handRoot != null)
        {
            GUI.Label(new Rect(10, y, 400, 20), 
                $"手部位置: {handRoot.position:F2}", 
                labelStyle);
            y += 22;
            
            var rb = handRoot.GetComponent<Rigidbody>();
            GUI.Label(new Rect(10, y, 400, 20), 
                $"Rigidbody: {(rb != null ? $"有 (isKinematic={rb.isKinematic})" : "无")}", 
                labelStyle);
            y += 22;
        }
        
        GUI.Label(new Rect(10, y, 400, 20), 
            $"平滑系数: {smoothing:F2} | 位置缩放: {positionScale:F1}", 
            labelStyle);
    }
}
