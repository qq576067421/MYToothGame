Shader "YX/UI/MaskFlashBlend"
{
  Properties
  {
    _MainTex ("Sprite Texture", 2D) = "white" {}
    _RotateSpeedMain ("Main Rotate Degree", float) = 0
    _Color ("Tint", Color) = (1,1,1,1)
    _FlowlightTex ("Add Move Texture", 2D) = "white" {}
    _RotateSpeedAdd ("Add  Rotate Degree", float) = 0
    _MaskTex ("Mask Map", 2D) = "white" {}
    _RotateSpeedMask ("Mask Rotate Degree", float) = 0
    _FlowlightColor ("Flowlight Color", Color) = (0,0,0,1)
    _MaskColor ("Mask Color", Color) = (1,1,1,1)
    _Power ("Power", float) = 1
    _SpeedX ("SpeedX", float) = 1
    _SpeedY ("SpeedY", float) = 0
    _MaskSpeedX ("MaskSpeedX", float) = 0
    _MaskSpeedY ("MaskSpeedY", float) = 0
    _BackSpeedX ("BackSpeedX", float) = 0
    _BackSpeedY ("BackSpeedY", float) = 0
    _StencilComp ("Stencil Comparison", float) = 8
    _Stencil ("Stencil ID", float) = 0
    _StencilOp ("Stencil Operation", float) = 0
    _StencilWriteMask ("Stencil Write Mask", float) = 255
    _StencilReadMask ("Stencil Read Mask", float) = 255
    [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Source Blend Mode", float) = 1
    [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Destination Blend Mode", float) = 1
  }
  SubShader
  {
    Tags
    { 
      "CanUseSpriteAtlas" = "true"
      "IGNOREPROJECTOR" = "true"
      "PreviewType" = "Plane"
      "QUEUE" = "Transparent"
      "RenderType" = "Transparent"
    }
    Pass // ind: 1, name: 
    {
      Tags
      { 
        "CanUseSpriteAtlas" = "true"
        "IGNOREPROJECTOR" = "true"
        "PreviewType" = "Plane"
        "QUEUE" = "Transparent"
        "RenderType" = "Transparent"
      }
      ZWrite Off
      Cull Off
      Blend Zero Zero
      ColorMask RGB
      // m_ProgramMask = 6
      CGPROGRAM
      //#pragma target 4.0
      
      #pragma vertex vert
      #pragma fragment frag
      
      #include "UnityCG.cginc"
      
      
      #define CODE_BLOCK_VERTEX
      //uniform float4 _Time;
      //uniform float4x4 unity_ObjectToWorld;
      //uniform float4x4 unity_MatrixVP;
      uniform float4 _MaskTex_ST;
      uniform float4 _FlowlightTex_ST;
      uniform float _SpeedX;
      uniform float _SpeedY;
      uniform float _MaskSpeedX;
      uniform float _MaskSpeedY;
      uniform float _BackSpeedX;
      uniform float _BackSpeedY;
      uniform float4 _MainTex_ST;
      uniform float4 _FlowlightColor;
      uniform float _Power;
      uniform float4 _MaskColor;
      uniform float _RotateSpeedMain;
      uniform float _RotateSpeedAdd;
      uniform float _RotateSpeedMask;
      uniform sampler2D _MainTex;
      uniform sampler2D _MaskTex;
      uniform sampler2D _FlowlightTex;
      struct appdata_t
      {
          float4 vertex :POSITION0;
          float4 color :COLOR0;
          float2 texcoord :TEXCOORD0;
      };
      
      struct OUT_Data_Vert
      {
          float4 color :COLOR0;
          float2 texcoord :TEXCOORD0;
          float2 texcoord1 :TEXCOORD1;
          float2 texcoord2 :TEXCOORD2;
          float2 texcoord3 :TEXCOORD3;
          float4 vertex :SV_POSITION;
      };
      
      struct v2f
      {
          float4 color :COLOR0;
          float2 texcoord1 :TEXCOORD1;
          float2 texcoord2 :TEXCOORD2;
          float2 texcoord3 :TEXCOORD3;
      };
      
      struct OUT_Data_Frag
      {
          float4 color :SV_Target0;
      };
      
      float4 phase0_Output0_2;
      float4 phase0_Output0_3;
      float4 u_xlat0;
      float4 u_xlat1;
      float2 u_xlat4;
      OUT_Data_Vert vert(appdata_t in_v)
      {
          OUT_Data_Vert out_v;
          out_v.vertex = UnityObjectToClipPos(in_v.vertex);
          out_v.color = in_v.color;
          u_xlat0.xy = TRANSFORM_TEX(in_v.texcoord.xy, _FlowlightTex);
          u_xlat1 = (_Time.yyyy * float4(_SpeedX, _SpeedY, _MaskSpeedX, _MaskSpeedY));
          u_xlat1 = frac(u_xlat1);
          u_xlat0.zw = (u_xlat0.xy + u_xlat1.xy);
          u_xlat0.xy = TRANSFORM_TEX(in_v.texcoord.xy, _MainTex);
          phase0_Output0_2 = u_xlat0;
          u_xlat4.xy = (_Time.yy * float2(_BackSpeedX, _BackSpeedY));
          u_xlat4.xy = frac(u_xlat4.xy);
          u_xlat0.zw = (u_xlat4.xy + u_xlat0.xy);
          u_xlat1.xy = TRANSFORM_TEX(in_v.texcoord.xy, _MaskTex);
          u_xlat0.xy = (u_xlat1.zw + u_xlat1.xy);
          phase0_Output0_3 = u_xlat0;
          out_v.texcoord = phase0_Output0_2.xy;
          out_v.texcoord1 = phase0_Output0_2.zw;
          out_v.texcoord2 = phase0_Output0_3.xy;
          out_v.texcoord3 = phase0_Output0_3.zw;
          return out_v;
      }
      
      #define CODE_BLOCK_FRAGMENT
      float4 phase0_Input0_3;
      float4 u_xlat0_d;
      float4 u_xlat16_0;
      float4 u_xlat10_0;
      float4 u_xlat1_d;
      float4 u_xlat10_1;
      float4 u_xlat16_2;
      float2 u_xlat3;
      float u_xlat4_d;
      float u_xlat5;
      float2 u_xlat6;
      float2 u_xlat16_7;
      float u_xlat24;
      OUT_Data_Frag frag(v2f in_f)
      {
          OUT_Data_Frag out_f;
          phase0_Input0_3 = float4(in_f.texcoord2, in_f.texcoord3);
          u_xlat0_d.xyz = float3((float3(_RotateSpeedMain, _RotateSpeedMask, _RotateSpeedAdd) * float3(0.0174533334, 0.0174533334, 0.0174533334)));
          u_xlat1_d.x = cos(u_xlat0_d.x);
          u_xlat0_d.x = sin(u_xlat0_d.x);
          u_xlat16_2 = (phase0_Input0_3.zwxy + float4(-0.5, (-0.5), (-0.5), (-0.5)));
          u_xlat0_d.xw = (u_xlat0_d.xx * u_xlat16_2.yx);
          u_xlat3.x = ((u_xlat16_2.x * u_xlat1_d.x) + (-u_xlat0_d.x));
          u_xlat3.y = ((u_xlat16_2.y * u_xlat1_d.x) + u_xlat0_d.w);
          u_xlat16_2.xy = (u_xlat3.xy + float2(0.5, 0.5));
          u_xlat10_1 = tex2D(_MainTex, u_xlat16_2.xy);
          u_xlat3.x = cos(u_xlat0_d.y);
          u_xlat0_d.x = sin(u_xlat0_d.y);
          u_xlat4_d = sin(u_xlat0_d.z);
          u_xlat5 = cos(u_xlat0_d.z);
          u_xlat0_d.xy = (u_xlat0_d.xx * u_xlat16_2.wz);
          u_xlat6.x = ((u_xlat16_2.z * u_xlat3.x) + (-u_xlat0_d.x));
          u_xlat6.y = ((u_xlat16_2.w * u_xlat3.x) + u_xlat0_d.y);
          u_xlat16_2.xy = (u_xlat6.xy + float2(0.5, 0.5));
          u_xlat10_0 = tex2D(_MaskTex, u_xlat16_2.xy);
          u_xlat16_0 = (u_xlat10_1 * u_xlat10_0);
          u_xlat16_0 = (u_xlat16_0 * _MaskColor);
          u_xlat16_2.xyz = ((u_xlat16_0.xyz * u_xlat16_0.www) + _FlowlightColor.xyz);
          u_xlat16_7.xy = (in_f.texcoord1.yx + float2(-0.5, (-0.5)));
          u_xlat0_d.xy = (float2(u_xlat4_d, u_xlat4_d) * u_xlat16_7.xy);
          u_xlat1_d.x = ((u_xlat16_7.y * u_xlat5) + (-u_xlat0_d.x));
          u_xlat1_d.y = ((u_xlat16_7.x * u_xlat5) + u_xlat0_d.y);
          u_xlat16_7.xy = (u_xlat1_d.xy + float2(0.5, 0.5));
          u_xlat10_1 = tex2D(_FlowlightTex, u_xlat16_7.xy);
          u_xlat1_d = (u_xlat10_1 * float4(_Power, _Power, _Power, _Power));
          u_xlat0_d.xyz = (u_xlat16_2.xyz * u_xlat1_d.xyz);
          u_xlat24 = (u_xlat16_0.w * u_xlat1_d.w);
          out_f.color.w = (u_xlat24 * in_f.color.w);
          out_f.color.xyz = u_xlat0_d.xyz;
          return out_f;
      }
      
      
      ENDCG
      
    } // end phase
  }
  FallBack Off
}
