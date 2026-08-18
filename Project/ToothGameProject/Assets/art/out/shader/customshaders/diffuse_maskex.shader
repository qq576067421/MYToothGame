// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Mobile/Diffuse_MaskEx" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}

		//-------------------add----------------------
		_MinX("Min X", Float) = -10
		_MaxX("Max X", Float) = 10
		_MinY("Min Y", Float) = -10
		_MaxY("Max Y", Float) = 10
		//-------------------add----------------------

	}

		Category{
			Tags{ "RenderType" = "Opaque" }


		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag


#include "UnityCG.cginc"

		sampler2D _MainTex;

		//-------------------add----------------------
		float _MinX;
		float _MaxX;
		float _MinY;
		float _MaxY;
		//-------------------add----------------------

		struct appdata_t {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			float2 texcoord : TEXCOORD0;
			//-------------------add----------------------
			float3 vpos : TEXCOORD1;
			//-------------------add----------------------
		};

		float4 _MainTex_ST;

		v2f vert(appdata_t v)
		{
			v2f o;
			//-------------------add----------------------
			o.vpos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0));
			//-------------------add----------------------
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
			return o;
		}



		fixed4 frag(v2f i) : SV_Target
		{
			//-------------------add----------------------
			fixed4 c = tex2D(_MainTex, i.texcoord);
			c.a *= (i.vpos.x >= _MinX);
			c.a *= (i.vpos.x <= _MaxX);
			c.a *= (i.vpos.y >= _MinY);
			c.a *= (i.vpos.y <= _MaxY);
			if (c.a < 0.1)
			{
				discard;
			}
			return c;
			//-------------------add----------------------
		}
		ENDCG
	}
	}
	}
}