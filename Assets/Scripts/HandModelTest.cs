using UnityEngine;

/// <summary>
/// 測試手部模型骨骼是否可以正常控制
/// 掛在手部模型的根節點上，會讓手指做循環動作
/// </summary>
public class HandModelTest : MonoBehaviour
{
    [Header("測試設定")]
    [SerializeField] private bool enableTest = true;
    [SerializeField] private float rotationSpeed = 50f; // 旋轉速度（度/秒）
    [SerializeField] private float moveSpeed = 0.5f; // 移動速度
    [SerializeField] private float moveDistance = 0.1f; // 移動距離
    
    [Header("手指節點（拖入要測試的骨骼）")]
    [SerializeField] private Transform[] thumbJoints = new Transform[3]; // 拇指 3 個關節
    [SerializeField] private Transform[] indexJoints = new Transform[3]; // 食指 3 個關節
    [SerializeField] private Transform[] middleJoints = new Transform[3]; // 中指 3 個關節
    [SerializeField] private Transform[] ringJoints = new Transform[3]; // 無名指 3 個關節
    [SerializeField] private Transform[] pinkyJoints = new Transform[3]; // 小指 3 個關節
    
    [Header("手腕測試")]
    [SerializeField] private Transform wrist; // 手腕
    
    private float time = 0f;
    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;
    
    private void Start()
    {
        // 記錄所有骨骼的初始位置和旋轉
        int totalJoints = 3 * 5 + 1; // 5根手指 x 3關節 + 1手腕
        originalPositions = new Vector3[totalJoints];
        originalRotations = new Quaternion[totalJoints];
        
        int index = 0;
        SaveOriginalTransform(thumbJoints, ref index);
        SaveOriginalTransform(indexJoints, ref index);
        SaveOriginalTransform(middleJoints, ref index);
        SaveOriginalTransform(ringJoints, ref index);
        SaveOriginalTransform(pinkyJoints, ref index);
        
        if (wrist != null)
        {
            originalPositions[index] = wrist.localPosition;
            originalRotations[index] = wrist.localRotation;
        }
        
        Debug.Log("✅ HandModelTest 初始化完成");
        Debug.Log($"📍 拇指關節: {CountNonNull(thumbJoints)} 個");
        Debug.Log($"📍 食指關節: {CountNonNull(indexJoints)} 個");
        Debug.Log($"📍 中指關節: {CountNonNull(middleJoints)} 個");
        Debug.Log($"📍 無名指關節: {CountNonNull(ringJoints)} 個");
        Debug.Log($"📍 小指關節: {CountNonNull(pinkyJoints)} 個");
        Debug.Log($"📍 手腕: {(wrist != null ? "已設定" : "未設定")}");
    }
    
    private void Update()
    {
        if (!enableTest) return;
        
        time += Time.deltaTime;
        
        // 測試 1: 旋轉測試
        TestRotation();
        
        // 測試 2: 位置測試
        TestPosition();
        
        // 測試 3: 縮放測試
        TestScale();
    }
    
    private void TestRotation()
    {
        // 讓每根手指的關節繞 Z 軸旋轉（彎曲動作）
        float angle = Mathf.Sin(time * 2f) * 45f; // -45° 到 +45° 的正弦波
        
        RotateJoints(thumbJoints, angle);
        RotateJoints(indexJoints, angle);
        RotateJoints(middleJoints, angle);
        RotateJoints(ringJoints, angle);
        RotateJoints(pinkyJoints, angle);
        
        // 手腕旋轉測試
        if (wrist != null)
        {
            wrist.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time) * 30f);
        }
    }
    
    private void TestPosition()
    {
        // 測試位置移動（上下移動）
        float yOffset = Mathf.Sin(time * 3f) * moveDistance;
        
        MoveJoints(thumbJoints, new Vector3(0, yOffset, 0));
        MoveJoints(indexJoints, new Vector3(0, yOffset, 0));
        MoveJoints(middleJoints, new Vector3(0, yOffset, 0));
        MoveJoints(ringJoints, new Vector3(0, yOffset, 0));
        MoveJoints(pinkyJoints, new Vector3(0, yOffset, 0));
    }
    
    private void TestScale()
    {
        // 測試縮放（呼吸效果）
        float scale = 1f + Mathf.Sin(time * 1.5f) * 0.1f;
        
        ScaleJoints(thumbJoints, scale);
        ScaleJoints(indexJoints, scale);
        ScaleJoints(middleJoints, scale);
        ScaleJoints(ringJoints, scale);
        ScaleJoints(pinkyJoints, scale);
    }
    
    private void RotateJoints(Transform[] joints, float angle)
    {
        foreach (var joint in joints)
        {
            if (joint == null) continue;
            
            // 嘗試不同的旋轉軸，看哪個能讓手指彎曲
            joint.localRotation = Quaternion.Euler(0, 0, angle); // Z 軸
            // 如果 Z 軸不對，可以試試：
            // joint.localRotation = Quaternion.Euler(angle, 0, 0); // X 軸
            // joint.localRotation = Quaternion.Euler(0, angle, 0); // Y 軸
        }
    }
    
    private void MoveJoints(Transform[] joints, Vector3 offset)
    {
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null) continue;
            
            // 測試直接設定位置
            joints[i].localPosition += offset * Time.deltaTime;
        }
    }
    
    private void ScaleJoints(Transform[] joints, float scale)
    {
        foreach (var joint in joints)
        {
            if (joint == null) continue;
            
            joint.localScale = Vector3.one * scale;
        }
    }
    
    private void SaveOriginalTransform(Transform[] joints, ref int index)
    {
        foreach (var joint in joints)
        {
            if (joint != null)
            {
                originalPositions[index] = joint.localPosition;
                originalRotations[index] = joint.localRotation;
            }
            index++;
        }
    }
    
    private int CountNonNull(Transform[] transforms)
    {
        int count = 0;
        foreach (var t in transforms)
        {
            if (t != null) count++;
        }
        return count;
    }
    
    private void OnDisable()
    {
        // 恢復原始狀態
        if (originalPositions == null) return;
        
        int index = 0;
        RestoreOriginalTransform(thumbJoints, ref index);
        RestoreOriginalTransform(indexJoints, ref index);
        RestoreOriginalTransform(middleJoints, ref index);
        RestoreOriginalTransform(ringJoints, ref index);
        RestoreOriginalTransform(pinkyJoints, ref index);
        
        if (wrist != null)
        {
            wrist.localPosition = originalPositions[index];
            wrist.localRotation = originalRotations[index];
        }
    }
    
    private void RestoreOriginalTransform(Transform[] joints, ref int index)
    {
        foreach (var joint in joints)
        {
            if (joint != null)
            {
                joint.localPosition = originalPositions[index];
                joint.localRotation = originalRotations[index];
            }
            index++;
        }
    }
    
    // Gizmos 顯示測試中的關節
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.green;
        DrawJointGizmos(thumbJoints);
        
        Gizmos.color = Color.blue;
        DrawJointGizmos(indexJoints);
        
        Gizmos.color = Color.yellow;
        DrawJointGizmos(middleJoints);
        
        Gizmos.color = Color.magenta;
        DrawJointGizmos(ringJoints);
        
        Gizmos.color = Color.red;
        DrawJointGizmos(pinkyJoints);
        
        if (wrist != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(wrist.position, 0.02f);
        }
    }
    
    private void DrawJointGizmos(Transform[] joints)
    {
        foreach (var joint in joints)
        {
            if (joint == null) continue;
            
            Gizmos.DrawSphere(joint.position, 0.01f);
        }
        
        // 畫連線
        for (int i = 0; i < joints.Length - 1; i++)
        {
            if (joints[i] != null && joints[i + 1] != null)
            {
                Gizmos.DrawLine(joints[i].position, joints[i + 1].position);
            }
        }
    }
}
