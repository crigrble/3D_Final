Shader "Custom/FishSwimShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        [Header(Swim Animation)]
        _WaveAmplitude ("Wave Amplitude", Range(0, 2)) = 0.5
        _WaveFrequency ("Wave Frequency", Range(0, 10)) = 3.0
        _WaveLength ("Wave Length", Range(0.1, 10)) = 3.0
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.0
        _BendAxis ("Bend Axis", Vector) = (0, 1, 0, 0)
        _ForwardAxis ("Forward Axis", Vector) = (0, 0, 1, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                SHADOW_COORDS(3)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveLength;
            float _WaveSpeed;
            float4 _BendAxis;
            float4 _ForwardAxis;

            v2f vert (appdata v)
            {
                v2f o;
                
                // 計算頂點在物件空間中的位置
                float3 localPos = v.vertex.xyz;
                
                // 使用點積計算頂點沿前進軸的位置
                float3 forwardDir = normalize(_ForwardAxis.xyz);
                float positionAlongBody = dot(localPos, forwardDir);
                
                // 計算時間相關的波動
                float time = _Time.y * _WaveFrequency * _WaveSpeed;
                
                // 計算正弦波
                float wave = sin(time - positionAlongBody * _WaveLength);
                
                // 計算強度（從頭到尾逐漸增強）
                float normalizedPos = saturate(positionAlongBody * 2.0 + 0.5);
                float intensity = normalizedPos * normalizedPos * normalizedPos;
                
                // 套用擺動
                float3 bendDir = normalize(_BendAxis.xyz);
                float3 offset = bendDir * wave * _WaveAmplitude * intensity;
                v.vertex.xyz += offset;
                
                // 轉換到世界空間
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 基礎顏色
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // 簡單的光照
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 normal = normalize(i.worldNormal);
                float ndotl = max(0, dot(normal, lightDir));
                
                // 環境光 + 主光源
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;
                float3 diffuse = _LightColor0.rgb * ndotl;
                
                // 陰影
                float shadow = SHADOW_ATTENUATION(i);
                
                col.rgb *= (ambient + diffuse * shadow);
                
                return col;
            }
            ENDCG
        }
        
        // 陰影投射 Pass
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveLength;
            float _WaveSpeed;
            float4 _BendAxis;
            float4 _ForwardAxis;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata v)
            {
                v2f o;
                
                // 與主 Pass 相同的頂點動畫
                float3 localPos = v.vertex.xyz;
                float3 forwardDir = normalize(_ForwardAxis.xyz);
                float positionAlongBody = dot(localPos, forwardDir);
                float time = _Time.y * _WaveFrequency * _WaveSpeed;
                float wave = sin(time - positionAlongBody * _WaveLength);
                float normalizedPos = saturate(positionAlongBody * 2.0 + 0.5);
                float intensity = normalizedPos * normalizedPos * normalizedPos;
                float3 bendDir = normalize(_BendAxis.xyz);
                float3 offset = bendDir * wave * _WaveAmplitude * intensity;
                v.vertex.xyz += offset;
                
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
