using UnityEngine;

/// <summary>
/// 手部追踪调试辅助脚本
/// 将此脚本挂载到与 HandCollisionDetector 相同的物体上
/// </summary>
public class HandDebugHelper : MonoBehaviour
{
    private HandCollisionDetector handCollisionDetector;
    private int frameCount = 0;
    
    void Start()
    {
        handCollisionDetector = GetComponent<HandCollisionDetector>();
        
        if (handCollisionDetector == null)
        {
            Debug.LogError("❌ HandDebugHelper: 找不到 HandCollisionDetector 组件！");
        }
        else
        {
            Debug.Log("✅ HandDebugHelper: 已找到 HandCollisionDetector");
        }
        
        // 检查关键绑定
        var handRoot = handCollisionDetector.GetType()
            .GetField("handRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(handCollisionDetector) as Transform;
            
        var handRenderer = handCollisionDetector.GetType()
            .GetField("handRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(handCollisionDetector) as Renderer;
        
        Debug.Log($"🔍 handRoot 绑定状态: {(handRoot != null ? $"✅ 已绑定 ({handRoot.name})" : "❌ 未绑定")}");
        Debug.Log($"🔍 handRenderer 绑定状态: {(handRenderer != null ? $"✅ 已绑定 ({handRenderer.name})" : "⚠️ 未绑定")}");
        
        if (handRoot != null)
        {
            var rb = handRoot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"🔍 Rigidbody 状态: isKinematic={rb.isKinematic}, mass={rb.mass}");
            }
            else
            {
                Debug.Log("🔍 handRoot 没有 Rigidbody 组件");
            }
        }
    }
    
    void Update()
    {
        frameCount++;
        
        // 每60帧输出一次位置信息
        if (frameCount % 60 == 0 && handCollisionDetector != null)
        {
            var handRoot = handCollisionDetector.GetType()
                .GetField("handRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(handCollisionDetector) as Transform;
            
            var isReceivingData = (bool)(handCollisionDetector.GetType()
                .GetField("isReceivingData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(handCollisionDetector) ?? false);
            
            var hasNewData = (bool)(handCollisionDetector.GetType()
                .GetField("_hasNewData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(handCollisionDetector) ?? false);
            
            Debug.Log($"📊 Frame {frameCount}: isReceivingData={isReceivingData}, _hasNewData={hasNewData}, " +
                     $"handRoot位置={(handRoot != null ? handRoot.position.ToString("F2") : "null")}, " +
                     $"IsHandVisible={HandCollisionDetector.IsHandVisible}");
        }
    }
}
