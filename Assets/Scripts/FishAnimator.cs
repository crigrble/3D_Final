using UnityEngine;

/// <summary>
/// 魚的身體扭動動畫腳本（適用於無骨架的整體模型）
/// 使用頂點動畫實現尾巴擺動效果
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class FishAnimator : MonoBehaviour
{
    [Header("動畫設定")]
    [SerializeField] private bool enableAnimation = true; // 是否啟用動畫
    [SerializeField] private float waveAmplitude = 0.3f; // 擺動振幅（越大擺動越明顯）
    [SerializeField] private float waveFrequency = 2.0f; // 擺動頻率（越大越快）
    [SerializeField] private float waveLength = 2.0f; // 波長（影響扭動的範圍）
    [SerializeField] private bool animateOnSpeed = true; // 根據移動速度調整動畫
    [SerializeField] private float speedMultiplier = 1.0f; // 速度影響倍數
    
    [Header("扭動方向")]
    [Tooltip("彎曲軸向：魚擺動的方向，如 (0,1,0)=左右擺，(1,0,0)=上下擺")]
    [SerializeField] private Vector3 bendAxis = Vector3.up; // 彎曲軸向（通常是 Y 或 X）
    [Tooltip("前進軸向：魚身體從頭到尾的方向，如 (0,0,1)=沿Z軸，(1,0,0)=沿X軸")]
    [SerializeField] private Vector3 forwardAxis = Vector3.forward; // 魚的前進軸向
    
    [Header("進階設定")]
    [SerializeField] private float phaseOffset = 0f; // 相位偏移（讓多條魚動畫不同步）
    [SerializeField] private bool autoPhase = true; // 自動隨機相位
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 從頭到尾的強度曲線
    
    [Header("網格設定")]
    [SerializeField] private bool canAnimateMesh = false; // 網格是否可讀取（Read/Write Enable）
    [SerializeField] private bool useShaderAnimation = true; // 使用 Shader 動畫（推薦，不需要 Read/Write）
    [SerializeField] private Transform tailTransform = null; // 備用：使用 Transform 旋轉尾部（當網格不可讀且無 Shader 時）
    
    private Mesh originalMesh;
    private Mesh clonedMesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private float meshLength; // 網格長度（用於計算頂點位置權重）
    private Rigidbody rb;
    private Fish fishScript;
    private Material fishMaterial; // 魚的材質（用於 Shader 動畫）
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        fishScript = GetComponent<Fish>();
        
        if (autoPhase)
        {
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }
        
        if (useShaderAnimation)
        {
            InitializeShaderAnimation();
        }
        else if (canAnimateMesh)
        {
            InitializeMesh();
        }
        else
        {
            Debug.LogWarning($"FishAnimator on {gameObject.name}: 網格動畫已停用。如需使用頂點動畫，請在 Import Settings 中啟用 Read/Write。目前將使用 Transform fallback。");
        }
    }
    
    private void InitializeShaderAnimation()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("FishAnimator: 找不到 Renderer！");
            useShaderAnimation = false;
            return;
        }
        
        // 複製材質以避免影響其他物件
        fishMaterial = renderer.material;
        
        // 設定初始 Shader 參數
        fishMaterial.SetFloat("_WaveAmplitude", waveAmplitude);
        fishMaterial.SetFloat("_WaveFrequency", waveFrequency);
        fishMaterial.SetFloat("_WaveLength", waveLength);
        fishMaterial.SetFloat("_WaveSpeed", 1.0f);
        fishMaterial.SetVector("_BendAxis", new Vector4(bendAxis.x, bendAxis.y, bendAxis.z, 0));
        fishMaterial.SetVector("_ForwardAxis", new Vector4(forwardAxis.x, forwardAxis.y, forwardAxis.z, 0));
        
        Debug.Log($"✅ FishAnimator (Shader) 已初始化: {gameObject.name}\n" +
                  $"   WaveAmplitude={waveAmplitude}, Frequency={waveFrequency}, Length={waveLength}\n" +
                  $"   BendAxis={bendAxis}, ForwardAxis={forwardAxis}");
    }
    
    private void InitializeMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("FishAnimator: 找不到 MeshFilter！");
            canAnimateMesh = false;
            return;
        }
        
        try
        {
            // 複製原始網格
            originalMesh = meshFilter.sharedMesh;
            clonedMesh = Instantiate(originalMesh);
            meshFilter.mesh = clonedMesh;
            
            // 獲取原始頂點
            originalVertices = originalMesh.vertices;
            modifiedVertices = new Vector3[originalVertices.Length];
            
            // 計算網格長度（沿著前進軸）
            meshLength = CalculateMeshLength();
            
            Debug.Log($"✅ FishAnimator 已初始化: {gameObject.name}, 頂點數: {originalVertices.Length}, 長度: {meshLength:F2}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FishAnimator 初始化失敗: {e.Message}\n請確保模型的 Read/Write Enabled 已勾選！");
            canAnimateMesh = false;
        }
    }
    
    private float CalculateMeshLength()
    {
        if (originalVertices.Length == 0) return 1f;
        
        float minPos = float.MaxValue;
        float maxPos = float.MinValue;
        
        foreach (Vector3 vertex in originalVertices)
        {
            // 沿著前進軸向的投影
            float projection = Vector3.Dot(vertex, forwardAxis.normalized);
            minPos = Mathf.Min(minPos, projection);
            maxPos = Mathf.Max(maxPos, projection);
        }
        
        return Mathf.Max(maxPos - minPos, 0.1f); // 避免除以零
    }
    
    private void Update()
    {
        if (!enableAnimation) return;
        
        if (useShaderAnimation && fishMaterial != null)
        {
            UpdateShaderAnimation();
        }
        else if (canAnimateMesh && originalVertices != null && originalVertices.Length > 0)
        {
            AnimateMesh();
        }
        else if (tailTransform != null)
        {
            AnimateTailTransform();
        }
    }
    
    private void UpdateShaderAnimation()
    {
        // 根據速度調整動畫（如果啟用）
        float currentSpeed = 1f;
        if (animateOnSpeed && rb != null)
        {
            currentSpeed = Mathf.Max(0.3f, rb.velocity.magnitude * speedMultiplier);
        }
        
        // 更新 Shader 參數
        fishMaterial.SetFloat("_WaveAmplitude", waveAmplitude);
        fishMaterial.SetFloat("_WaveFrequency", waveFrequency);
        fishMaterial.SetFloat("_WaveLength", waveLength);
        fishMaterial.SetFloat("_WaveSpeed", currentSpeed);
        fishMaterial.SetVector("_BendAxis", new Vector4(bendAxis.x, bendAxis.y, bendAxis.z, 0));
        fishMaterial.SetVector("_ForwardAxis", new Vector4(forwardAxis.x, forwardAxis.y, forwardAxis.z, 0));
    }
    
    private void AnimateMesh()
    {
        // 計算當前速度（如果啟用速度調整）
        float currentSpeed = 1f;
        if (animateOnSpeed && rb != null)
        {
            currentSpeed = Mathf.Max(0.3f, rb.velocity.magnitude * speedMultiplier);
        }
        
        float time = Time.time * waveFrequency * currentSpeed + phaseOffset;
        
        // 更新每個頂點
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];
            
            // 計算頂點在魚身上的位置（0=頭部, 1=尾部）
            float positionAlongBody = Vector3.Dot(vertex, forwardAxis.normalized);
            float normalizedPosition = (positionAlongBody + meshLength * 0.5f) / meshLength;
            normalizedPosition = Mathf.Clamp01(normalizedPosition);
            
            // 根據位置計算擺動強度（尾部擺動最大）
            float intensity = intensityCurve.Evaluate(normalizedPosition);
            
            // 計算正弦波位移
            float wave = Mathf.Sin(time - normalizedPosition * waveLength) * waveAmplitude * intensity;
            
            // 套用位移到彎曲軸向
            Vector3 offset = bendAxis.normalized * wave;
            modifiedVertices[i] = vertex + offset;
        }
        
        // 更新網格
        clonedMesh.vertices = modifiedVertices;
        clonedMesh.RecalculateNormals();
        clonedMesh.RecalculateBounds();
    }
    
    private void AnimateTailTransform()
    {
        // 備用方案：旋轉尾部 Transform
        float currentSpeed = 1f;
        if (animateOnSpeed && rb != null)
        {
            currentSpeed = Mathf.Max(0.3f, rb.velocity.magnitude * speedMultiplier);
        }
        
        float time = Time.time * waveFrequency * currentSpeed + phaseOffset;
        float angle = Mathf.Sin(time) * waveAmplitude * 30f; // 轉換為角度
        
        tailTransform.localRotation = Quaternion.Euler(bendAxis.normalized * angle);
    }
    
    /// <summary>
    /// 設定動畫參數（可在運行時調用）
    /// </summary>
    public void SetAnimationParams(float amplitude, float frequency, float length)
    {
        waveAmplitude = amplitude;
        waveFrequency = frequency;
        waveLength = length;
    }
    
    /// <summary>
    /// 啟用/停用動畫
    /// </summary>
    public void SetAnimationEnabled(bool enabled)
    {
        enableAnimation = enabled;
        
        // 停用時恢復原始網格
        if (!enabled && canAnimateMesh && clonedMesh != null && originalVertices != null)
        {
            clonedMesh.vertices = originalVertices;
            clonedMesh.RecalculateNormals();
            clonedMesh.RecalculateBounds();
        }
    }
    
    private void OnDestroy()
    {
        // 清理複製的網格
        if (clonedMesh != null)
        {
            Destroy(clonedMesh);
        }
        
        // 清理複製的材質
        if (fishMaterial != null && useShaderAnimation)
        {
            Destroy(fishMaterial);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 在 Scene 視圖中顯示彎曲軸向和前進軸向
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(forwardAxis) * 2f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(bendAxis) * 1f);
    }
}
