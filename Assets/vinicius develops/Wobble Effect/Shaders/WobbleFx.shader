Shader "vdev/FX/Wobble (Multiply)"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_Intensity("Intensity", Range(0.0, 360.0)) = 4.0
		_Amplitude("Amplitude", Range(0.0, 0.1)) = 0.003
		_Brightness("Brightness", Range(0.1, 3.0)) = 1.2
		_Offset("Offset", Vector) = (0,0,0,0)
		_Speed("Speed", Range(0, 3)) = 0.0
	}
		SubShader
		{
			// No culling or depth
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
			const float PI = 3.1415927;

			fixed4 frag(v2f i) : SV_Target
			{
				// Apply small UV offset for subtle underwater motion
				i.uv += _Offset.xy;
				float timeOffset = _Time.y * _Speed;
				float sine = sin((i.uv.y + timeOffset) * _Intensity);
				i.uv.x += sine * _Amplitude;

				fixed4 col = tex2D(_MainTex, i.uv);
				col *= _Color;
				col.rgb *= _Brightness;
				return col;
			}
			ENDCG
		}
		}
}
