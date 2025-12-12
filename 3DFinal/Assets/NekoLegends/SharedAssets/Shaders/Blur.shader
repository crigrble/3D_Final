Shader "Neko Legends/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurAmount ("Blur Amount", Range(0, 5)) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }
        LOD 100

        // Horizontal Blur Pass
        Pass
        {
            Name "HorizontalBlur"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5 // WebGL2 compatibility

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _BlurAmount;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float blurSize = _BlurAmount * 0.001;
                half4 col = 0;
                float2 uv = input.uv;

                // 9-tap Gaussian weights
                float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 }; // Normalized Gaussian coefficients

                // Center sample
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * weights[0];

                // Horizontal samples
                for (int i = 1; i < 5; i++)
                {
                    float offset = i * blurSize;
                    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset, 0)) * weights[i];
                    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(offset, 0)) * weights[i];
                }

                return col;
            }
            ENDHLSL
        }

        // Vertical Blur Pass
        Pass
        {
            Name "VerticalBlur"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5 // WebGL2 compatibility

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _BlurAmount;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float blurSize = _BlurAmount * 0.001;
                half4 col = 0;
                float2 uv = input.uv;

                // 9-tap Gaussian weights
                float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 }; // Normalized Gaussian coefficients

                // Center sample
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * weights[0];

                // Vertical samples
                for (int i = 1; i < 5; i++)
                {
                    float offset = i * blurSize;
                    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, offset)) * weights[i];
                    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, offset)) * weights[i];
                }

                return col;
            }
            ENDHLSL
        }
    }
}