Shader "Neko Legends/Free/Foilage Shader"
{

    Properties
    {
        [Header(Texture)]
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Header(Wave)]
        _Amplitude ("Wave Amplitude", Float) = 0.1
        _Frequency ("Wave Frequency", Float) = 1.0
        _WaveLength ("Wave Length", Float) = 1.0

        [Header(Gradient)]
        [Toggle] _GradientEnabled ("Enable Gradient", Float) = 0
        _GradientColorA ("Gradient Color A (Bottom)", Color) = (1,1,1,1)
        _GradientColorB ("Gradient Color B (Top)", Color) = (1,1,1,1)
        _GradientAmount ("Gradient Amount", Range(0,1)) = 0.0
        _GradientBlendWidth ("Gradient Blend Width", Range(0.01,0.5)) = 0.1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0 // 0 = Off (double-sided) by default
        [Toggle] _BillboardEnabled ("Enable Billboard", Float) = 0
        _BillboardStrength ("Billboard Strength", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Texture
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            // Wave
            float _Amplitude;
            float _Frequency;
            float _WaveLength;

            // Gradient
            float _GradientEnabled;
            float4 _GradientColorA;
            float4 _GradientColorB;
            float _GradientAmount;
            float _GradientBlendWidth;

            // Rendering
            float _BillboardEnabled;
            float _BillboardStrength;

            v2f vert (appdata v)
            {
                v2f o;

                // Billboard effect
                if (_BillboardEnabled > 0.5)
                {
                    // Get the model matrix
                    float4x4 modelMatrix = UNITY_MATRIX_M;
                    // Reset rotation to face camera
                    float3 forward = normalize(GetWorldSpaceViewDir(mul(modelMatrix, v.vertex).xyz));
                    float3 up = float3(0, 1, 0);
                    float3 right = normalize(cross(up, forward));
                    up = normalize(cross(forward, right));
                    // Apply billboard rotation with strength
                    float3 vertexPos = v.vertex.xyz;
                    vertexPos = lerp(vertexPos, right * vertexPos.x + up * vertexPos.y, _BillboardStrength);
                    v.vertex.xyz = vertexPos;
                }

                // Wave displacement
                float displacement = v.uv.y * _Amplitude * sin(_Frequency * _Time.y + v.uv.x * _WaveLength);
                v.vertex.x += displacement;

                // Transform to clip space
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Sample texture
                half4 color = tex2D(_MainTex, i.uv);
                clip(color.a - _Cutoff);

                // Apply gradient if enabled
                if (_GradientEnabled > 0.5)
                {
                    float gradientFactor = smoothstep(1 - _GradientAmount - _GradientBlendWidth, 
                                                     1 - _GradientAmount + _GradientBlendWidth, 
                                                     i.uv.y);
                    half3 gradientColor = lerp(_GradientColorA.rgb, _GradientColorB.rgb, gradientFactor);
                    color.rgb = lerp(color.rgb, gradientColor, gradientFactor);
                }

                return color;
            }
            ENDHLSL
        }
    }
}