Shader "vdev/FX/Wobble Enhanced (Underwater)"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (0.7, 0.9, 1, 1)
		_Intensity("Intensity", Range(0.0, 360.0)) = 8.0
		_Amplitude("Amplitude", Range(0.0, 0.1)) = 0.015
		_Brightness("Brightness", Range(0.1, 3.0)) = 1.1
		_Offset("Offset", Vector) = (0,0,0,0)
		_Speed("Speed", Range(-3, 3)) = 0.5
		
		[Header(Enhanced Distortion)]
		_DistortionX("Horizontal Distortion", Range(0.0, 0.05)) = 0.015
		_DistortionY("Vertical Distortion", Range(0.0, 0.05)) = 0.01
		_WaveFrequencyX("Wave Frequency X", Range(1, 50)) = 10
		_WaveFrequencyY("Wave Frequency Y", Range(1, 50)) = 8
		_SecondaryWaveScale("Secondary Wave Scale", Range(0, 2)) = 0.5
		_ChromaticAmount("Chromatic Aberration", Range(0, 0.01)) = 0.002
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			uniform float4 _Color;
			float _Intensity;
			float _Amplitude;
			float _Brightness;
			float4 _Offset;
			float _Speed;
			float _DistortionX;
			float _DistortionY;
			float _WaveFrequencyX;
			float _WaveFrequencyY;
			float _SecondaryWaveScale;
			float _ChromaticAmount;
			const float PI = 3.1415927;

			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				float time = _Time.y * _Speed;
				
				// 主波動：雙向正弦波扭曲
				float waveX = sin((uv.y * _WaveFrequencyY + time) * _Intensity * 0.1) * _DistortionX;
				float waveY = cos((uv.x * _WaveFrequencyX + time * 1.3) * _Intensity * 0.1) * _DistortionY;
				
				// 次要波動：增加複雜度
				float waveX2 = sin((uv.y * _WaveFrequencyY * 2.5 - time * 1.5) * _Intensity * 0.05) * _DistortionX * _SecondaryWaveScale;
				float waveY2 = cos((uv.x * _WaveFrequencyX * 2.2 + time * 1.8) * _Intensity * 0.05) * _DistortionY * _SecondaryWaveScale;
				
				// 組合波動
				uv.x += waveX + waveX2;
				uv.y += waveY + waveY2;
				
				// 添加 Offset (從 WaterCamera 的 UV jitter)
				uv += _Offset.xy;
				
				// 色散效果（模擬水下光線折射）
				float2 uvR = uv + float2(_ChromaticAmount, 0);
				float2 uvG = uv;
				float2 uvB = uv - float2(_ChromaticAmount, 0);
				
				fixed4 col;
				col.r = tex2D(_MainTex, uvR).r;
				col.g = tex2D(_MainTex, uvG).g;
				col.b = tex2D(_MainTex, uvB).b;
				col.a = 1.0;
				
				// 應用水下色調
				col *= _Color;
				col.rgb *= _Brightness;
				
				return col;
			}
			ENDCG
		}
	}
}
