using UnityEngine;

namespace vnc.FX
{
    /// <summary>
    /// 圖形與 URP/內建管線雙支援的 WaterCamera
    /// - 支援 Blend mode (Multiply/Overlay/Screen) 並自動切換材質 shader
    /// - 同步到 WobbleManager，並在 Built-in pipeline 下使用 OnRenderImage 作為備援
    /// </summary>
    public class WaterCamera : MonoBehaviour
    {
        [Header("Wobble 設定")]
        public Material wobbleMaterial;
        public Color underwaterColor = new Color(0.7f, 0.9f, 1f, 1f); // 淡藍色水下色調
        public BlendMode blend = BlendMode.Multiply;
        public bool enableOnMainCamera = true; // 是否只在 MainCamera 啟用
        public bool effectActive = true; // 總開關
        [Range(0f, 20f)] public float intensity = 8f; // 增加扭曲強度
        [Range(0f, 0.05f)] public float amplitude = 0.012f; // 增加扭曲幅度
        [Range(0.1f, 3f)] public float brightness = 1.1f;
        [Range(-2f, 2f)] public float speed = 0.5f; // 啟用動態速度
        
            [Header("動態扭曲效果")]
            [Tooltip("多層波動混合，產生更自然的水流效果")]
            public bool enableMultiLayerDistortion = true;
            [Range(0f, 0.02f)] public float secondaryAmplitude = 0.008f; // 第二層波動幅度
            [Range(0.1f, 5f)] public float secondarySpeed = 1.2f; // 第二層波動速度
            [Range(0f, 0.01f)] public float tertiaryAmplitude = 0.004f; // 第三層波動幅度
            [Range(0.1f, 3f)] public float tertiarySpeed = 2.5f; // 第三層波動速度
            
            [Header("Shake")]
            public bool enableCameraShake = true; // small local transform shake
            public float camShakeMagnitude = 0.05f; // 增加晃動幅度
            public float camShakeFrequency = 0.3f; // 減慢晃動頻率
            public bool enableUVJitter = true; // small UV jitter applied to shader
            public float uvJitterMagnitude = 0.015f; // 增加 UV 抖動
            public float uvJitterFrequency = 0.8f; // 加快抖動頻率
            [Tooltip("Add a subtle sinusoidal offset on top of Perlin noise (UV space)")]
            public float uvPulseMagnitude = 0.006f; // 增加脈動效果
            public float uvPulseFrequency = 0.7f;

        [Header("Shaders (optional)")]
        public Shader multiply;
        public Shader overlay;
        public Shader screen;

        [Header("進階")]
        [Tooltip("啟動時從材質讀取初始參數值")]
        public bool loadFromMaterialOnStart = true;
        [Tooltip("每幀自動同步到 WobbleManager，非必要可關閉")]
        public bool autoSync = true;
        [Tooltip("(Debug) Always force the effect active regardless of camera/main settings")]
        public bool debugForceActive = false;

        private Camera cam;
        private UnityEngine.Vector3 originalLocalPosition;
        private float shakeSeedX;
        private float shakeSeedY;

        private void Awake()
        {
            cam = GetComponent<Camera>() ?? Camera.main;
            originalLocalPosition = cam.transform.localPosition;
            shakeSeedX = UnityEngine.Random.Range(0f, 1000f);
            shakeSeedY = UnityEngine.Random.Range(0f, 1000f);
            if (wobbleMaterial != null)
            {
                Debug.Log($"Wobble material assigned: {wobbleMaterial.name}, shader: {wobbleMaterial.shader?.name}");
                if (!wobbleMaterial.HasProperty("_Amplitude"))
                {
                    Debug.LogWarning("Wobble material shader does not expose _Amplitude. Please use the Wobble shaders provided (WobbleFx, Sprite-WobbleFx, etc.)");
                }
                
                // 從材質讀取初始值（如果啟用）
                if (loadFromMaterialOnStart)
                {
                    LoadFromMaterial();
                }
            }
        }

        private void OnEnable()
        {
            SyncToManager();
        }

        private void OnDisable()
        {
            WobbleManager.EffectActive = false;
        }

        private void Update()
        {
            // If shader swappnig is configured, set the shader on the material
            if (wobbleMaterial != null)
            {
                switch (blend)
                {
                    case BlendMode.Multiply:
                        if (multiply != null) wobbleMaterial.shader = multiply;
                        break;
                    case BlendMode.Overlay:
                        if (overlay != null) wobbleMaterial.shader = overlay;
                        break;
                    case BlendMode.Screen:
                        if (screen != null) wobbleMaterial.shader = screen;
                        break;
                    default:
                        break;
                }
            }

            if (autoSync)
            {
                SyncToManager();
            }

                // Runtime debug shortcuts: '[' / ']' to change amplitude, 'P' to toggle effect force
                if (Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    amplitude = Mathf.Max(0f, amplitude - 0.001f);
                    ApplyInspectorToMaterial();
                }
                if (Input.GetKeyDown(KeyCode.RightBracket))
                {
                    amplitude = Mathf.Min(0.03f, amplitude + 0.001f);
                    ApplyInspectorToMaterial();
                }
                if (Input.GetKeyDown(KeyCode.P))
                {
                    debugForceActive = !debugForceActive;
                    SyncToManager();
                }

            // Camera transform and UV jitter
            if (WobbleManager.EffectActive)
            {
                // Compute perlin-based UV jitter with multi-layer distortion
                if (enableUVJitter)
                {
                    float t = Time.time * uvJitterFrequency;
                    float ox = (Mathf.PerlinNoise(t + shakeSeedX, 0f) - 0.5f) * 2f * uvJitterMagnitude;
                    float oy = (Mathf.PerlinNoise(0f, t + shakeSeedY) - 0.5f) * 2f * uvJitterMagnitude;
                    
                    // 多層波動混合
                    if (enableMultiLayerDistortion)
                    {
                        // 第二層：較快的小波動
                        float t2 = Time.time * secondarySpeed;
                        ox += Mathf.Sin(t2 + shakeSeedX) * secondaryAmplitude;
                        oy += Mathf.Cos(t2 + shakeSeedY) * secondaryAmplitude;
                        
                        // 第三層：最快的微波動
                        float t3 = Time.time * tertiarySpeed;
                        ox += Mathf.Sin(t3 * 1.5f + shakeSeedX * 2f) * tertiaryAmplitude;
                        oy += Mathf.Cos(t3 * 1.7f + shakeSeedY * 2f) * tertiaryAmplitude;
                    }
                    
                    // 主脈動
                    float pulse = Mathf.Sin(Time.time * uvPulseFrequency) * uvPulseMagnitude;
                    float pulseY = Mathf.Cos(Time.time * uvPulseFrequency * 1.3f) * uvPulseMagnitude * 0.7f;
                    
                    WobbleManager.Offset = new UnityEngine.Vector2(ox + pulse, oy + pulseY);
                }
                else
                {
                    WobbleManager.Offset = Vector2.zero;
                }

                // Small camera local position shake if enabled
                if (enableCameraShake && cam != null)
                {
                    float t2 = Time.time * camShakeFrequency;
                    float cx = (Mathf.PerlinNoise(t2 + shakeSeedX, 0f) - 0.5f) * 2f * camShakeMagnitude;
                    float cy = (Mathf.PerlinNoise(0f, t2 + shakeSeedY) - 0.5f) * 2f * camShakeMagnitude;
                    cam.transform.localPosition = originalLocalPosition + new Vector3(cx, cy, 0f);
                }
            }
            else
            {
                // Reset camera/offset when not active
                if (cam != null)
                {
                    cam.transform.localPosition = originalLocalPosition;
                }
                WobbleManager.Offset = Vector2.zero;
            }
        }

        private void OnValidate()
        {
            if (cam == null) cam = GetComponent<Camera>() ?? Camera.main;
            SyncToManager();
            // Also push inspector values to the wobble material for quick preview in editor
            ApplyInspectorToMaterial();
        }

        [ContextMenu("從材質載入參數 (Load From Material)")]
        public void LoadFromMaterialMenu()
        {
            LoadFromMaterial();
        }
        
        [ContextMenu("儲存參數到材質 (Apply Inspector To Material)")]
        public void ApplyInspectorToMaterial()
        {
            if (wobbleMaterial == null) return;
            if (wobbleMaterial.HasProperty("_Color")) wobbleMaterial.SetColor("_Color", underwaterColor);
            if (wobbleMaterial.HasProperty("_Intensity")) wobbleMaterial.SetFloat("_Intensity", intensity);
            if (wobbleMaterial.HasProperty("_Amplitude")) wobbleMaterial.SetFloat("_Amplitude", amplitude);
            if (wobbleMaterial.HasProperty("_Brightness")) wobbleMaterial.SetFloat("_Brightness", brightness);
            if (wobbleMaterial.HasProperty("_Speed")) wobbleMaterial.SetFloat("_Speed", speed);
            if (wobbleMaterial.HasProperty("_Offset")) wobbleMaterial.SetVector("_Offset", new UnityEngine.Vector4(WobbleManager.Offset.x, WobbleManager.Offset.y, 0f, 0f));
            WobbleManager.WobbleMaterial = wobbleMaterial;
            Debug.Log($"Applied wobble to material: intensity={intensity}, amplitude={amplitude}, brightness={brightness}");
        }

        /// <summary>
        /// 從材質讀取參數到 Inspector（保留材質設定）
        /// </summary>
        private void LoadFromMaterial()
        {
            if (wobbleMaterial == null) return;
            
            if (wobbleMaterial.HasProperty("_Intensity"))
            {
                intensity = wobbleMaterial.GetFloat("_Intensity");
            }
            if (wobbleMaterial.HasProperty("_Amplitude"))
            {
                amplitude = wobbleMaterial.GetFloat("_Amplitude");
            }
            if (wobbleMaterial.HasProperty("_Brightness"))
            {
                brightness = wobbleMaterial.GetFloat("_Brightness");
            }
            if (wobbleMaterial.HasProperty("_Speed"))
            {
                speed = wobbleMaterial.GetFloat("_Speed");
            }
            if (wobbleMaterial.HasProperty("_Color"))
            {
                underwaterColor = wobbleMaterial.GetColor("_Color");
            }
            
            Debug.Log($"✅ 已從材質載入參數: Intensity={intensity}, Amplitude={amplitude}, Speed={speed}");
        }
        
        private void SyncToManager()
        {
            if (wobbleMaterial != null)
            {
                WobbleManager.WobbleMaterial = wobbleMaterial;
            }
            WobbleManager.Intensity = intensity;
            WobbleManager.Amplitude = amplitude;
            WobbleManager.Brightness = brightness;
            WobbleManager.Speed = speed;
            // Reset offset initially; Update() will animate it
            WobbleManager.Offset = Vector2.zero;
            WobbleManager.UnderwaterColor = underwaterColor;
            WobbleManager.BlendMode = (int)blend;
            // If debug forcing is enabled, keep the effect on regardless of camera selection
            bool active = debugForceActive || (effectActive && (!enableOnMainCamera || cam == Camera.main));
            WobbleManager.EffectActive = active;
            // Also push inspector values directly to the material for quick preview
            ApplyInspectorToMaterial();
        }

        /// <summary>
        /// 開啟效果 (會同步到 WobbleManager)
        /// </summary>
        public void EnableEffect()
        {
            effectActive = true;
            SyncToManager();
        }

        /// <summary>
        /// 關閉效果 (會同步到 WobbleManager)
        /// </summary>
        public void DisableEffect()
        {
            effectActive = false;
            SyncToManager();
        }

        // Legacy: for built-in pipeline, still perform OnRenderImage blit
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (wobbleMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (effectActive && (!enableOnMainCamera || cam == Camera.main))
            {
                if (wobbleMaterial.HasProperty("_Color"))
                {
                    wobbleMaterial.SetColor("_Color", underwaterColor);
                }
                if (wobbleMaterial.HasProperty("_Intensity")) wobbleMaterial.SetFloat("_Intensity", intensity);
                if (wobbleMaterial.HasProperty("_Amplitude")) wobbleMaterial.SetFloat("_Amplitude", amplitude);
                if (wobbleMaterial.HasProperty("_Brightness")) wobbleMaterial.SetFloat("_Brightness", brightness);
                if (wobbleMaterial.HasProperty("_Speed")) wobbleMaterial.SetFloat("_Speed", speed);
                if (wobbleMaterial.HasProperty("_Offset")) wobbleMaterial.SetVector("_Offset", new UnityEngine.Vector4(WobbleManager.Offset.x, WobbleManager.Offset.y, 0f, 0f));
                Graphics.Blit(source, destination, wobbleMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }

    public enum BlendMode
    {
        Multiply,
        Overlay,
        Screen
    }
}
