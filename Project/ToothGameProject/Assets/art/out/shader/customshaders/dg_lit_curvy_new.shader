// Toony Colors Pro+Mobile 2
// (c) 2014-2025 Jean Moreno

Shader "dg/dg_lit_curvy_new"
{
	Properties
	{
		[Enum(Front, 2, Back, 1, Both, 0)] _Cull ("Render Face", Float) = 2.0
		[TCP2ToggleNoKeyword] _ZWrite ("Depth Write", Float) = 1.0
		[HideInInspector] _RenderingMode ("rendering mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("blending source", Float) = 1.0
		[HideInInspector] _DstBlend ("blending destination", Float) = 0.0
		[TCP2Separator]

		[TCP2HeaderHelp(Base)]
		_BaseColor ("Color", Color) = (1,1,1,1)
		[TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (0.75,0.75,0.75,1)
		[TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
		_MainTex ("Albedo", 2D) = "white" {}
		[TCP2Separator]

		[TCP2Header(Ramp Shading)]
		
		_RampThreshold ("Threshold", Range(0.01,1)) = 0.5
		_RampSmoothing ("Smoothing", Range(0.001,1)) = 0.5
		[TCP2Separator]
		
		[TCP2HeaderHelp(Specular)]
		[Toggle(TCP2_SPECULAR)] _UseSpecular ("Enable Specular", Float) = 0
		[TCP2ColorNoAlpha] _SpecularColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
		_SpecularRoughnessPBR ("Roughness", Range(0,1)) = 0.5
		[TCP2Separator]
		
		[TCP2HeaderHelp(Rim Lighting)]
		[TCP2ColorNoAlpha] _RimColor ("Rim Color", Color) = (0.8,0.8,0.8,0.5)
		_RimMin ("Rim Min", Range(0,2)) = 0.5
		_RimMax ("Rim Max", Range(0,2)) = 1
		[TCP2Separator]
		
		[TCP2HeaderHelp(MatCap)]
		[Toggle(TCP2_MATCAP)] _UseMatCap ("Enable MatCap", Float) = 0
		[NoScaleOffset] [NoScaleOffset] _MatCapTex ("MatCap (RGB)", 2D) = "gray" {}
		[TCP2ColorNoAlpha] [HDR] _MatCapColor ("MatCap Color", Color) = (1,1,1,1)
		[TCP2Separator]
		
		[TCP2HeaderHelp(Dissolve)]
		[Toggle(TCP2_DISSOLVE)] _UseDissolve ("Enable Dissolve", Float) = 0
		[NoScaleOffset] _DissolveMap ("Map", 2D) = "gray" {}
		_DissolveValue ("Value", Range(0,1)) = 0.5
		[HDR] _DissolveGradientTexture ("Gradient Texture", Color) = (1,0.5064471,0,1)
		_DissolveGradientWidth ("Ramp Width", Range(0,1)) = 0.2
		[TCP2Separator]
		
		[TCP2HeaderHelp(Curved World 2020)]
		[CurvedWorldBendSettings] _CurvedWorldBendSettings("0,10|1|1", Vector) = (0, 0, 0, 0)

		[ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadowsOff ("Receive Shadows", Float) = 1

		// Avoid compile error if the properties are ending with a drawer
		[HideInInspector] __dummy__ ("unused", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType"="TransparentCutout"
			"Queue"="AlphaTest"
		}

		HLSLINCLUDE
		#define fixed half
		#define fixed2 half2
		#define fixed3 half3
		#define fixed4 half4

		#if UNITY_VERSION >= 202020
			#define URP_10_OR_NEWER
		#endif
		#if UNITY_VERSION >= 202120
			#define URP_12_OR_NEWER
		#endif
		#if UNITY_VERSION >= 202220
			#define URP_14_OR_NEWER
		#endif

		// Texture/Sampler abstraction
		#define TCP2_TEX2D_WITH_SAMPLER(tex)						TEXTURE2D(tex); SAMPLER(sampler##tex)
		#define TCP2_TEX2D_NO_SAMPLER(tex)							TEXTURE2D(tex)
		#define TCP2_TEX2D_SAMPLE(tex, samplertex, coord)			SAMPLE_TEXTURE2D(tex, sampler##samplertex, coord)
		#define TCP2_TEX2D_SAMPLE_LOD(tex, samplertex, coord, lod)	SAMPLE_TEXTURE2D_LOD(tex, sampler##samplertex, coord, lod)

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		// Uniforms

		// Shader Properties
		TCP2_TEX2D_WITH_SAMPLER(_MainTex);
		TCP2_TEX2D_WITH_SAMPLER(_DissolveMap);
		TCP2_TEX2D_WITH_SAMPLER(_MatCapTex);

		CBUFFER_START(UnityPerMaterial)
			
			// Shader Properties
			float4 _MainTex_ST;
			float _DissolveValue;
			float _DissolveGradientWidth;
			half4 _DissolveGradientTexture;
			fixed4 _BaseColor;
			half4 _MatCapColor;
			float _RampThreshold;
			float _RampSmoothing;
			fixed4 _SColor;
			fixed4 _HColor;
			float _RimMin;
			float _RimMax;
			fixed4 _RimColor;
			float _SpecularRoughnessPBR;
			fixed4 _SpecularColor;
		CBUFFER_END

		static const float DITHER_THRESHOLD_8x8[64] =
		{
			0.2627,0.7725,0.3843,0.9529,0.2941,0.8196,0.4118,1.0000,
			0.5020,0.0392,0.6235,0.1412,0.5333,0.0510,0.6549,0.1725,
			0.3216,0.8627,0.2039,0.6824,0.3529,0.9098,0.2314,0.7294,
			0.5647,0.0824,0.4431,0.0078,0.5922,0.1137,0.4745,0.0235,
			0.2784,0.7961,0.4000,0.9765,0.2471,0.7529,0.3686,0.9333,
			0.5176,0.0471,0.6392,0.1569,0.4902,0.0314,0.6078,0.1294,
			0.3373,0.8863,0.2196,0.7059,0.3098,0.8431,0.1882,0.6706,
			0.5804,0.0980,0.4588,0.0157,0.5490,0.0667,0.4275,0.0001
		};
		float Dither8x8(float2 positionCS)
		{
			uint index = (uint(positionCS.x) % 8) * 8 + uint(positionCS.y) % 8;
			return DITHER_THRESHOLD_8x8[index];
		}
		
		static const float DITHER_THRESHOLDS_4x4[16] =
		{
			0.0588, 0.5294, 0.1765, 0.6471,
			0.7647, 0.2941, 0.8823, 0.4118,
			0.2353, 0.7059, 0.1176, 0.5882,
			0.9412, 0.4706, 0.8235, 0.3529
		};
		float Dither4x4(float2 positionCS)
		{
			uint index = (uint(positionCS.x) % 4) * 4 + uint(positionCS.y) % 4;
			return DITHER_THRESHOLDS_4x4[index];
		}
		
		//Specular help functions (from UnityStandardBRDF.cginc)
		inline float3 SpecSafeNormalize(float3 inVec)
		{
			half dp3 = max(0.001f, dot(inVec, inVec));
			return inVec * rsqrt(dp3);
		}
		
			//GGX
			#define TCP2_PI			3.14159265359
			#define TCP2_INV_PI		0.31830988618f
			#if defined(SHADER_API_MOBILE)
				#define TCP2_EPSILON 1e-4f
			#else
				#define TCP2_EPSILON 1e-7f
			#endif
			inline half GGX(half NdotH, half roughness)
			{
				half a2 = roughness * roughness;
				half d = (NdotH * a2 - NdotH) * NdotH + 1.0f;
				return TCP2_INV_PI * a2 / (d * d + TCP2_EPSILON);
			}
		
		// Built-in renderer (CG) to SRP (HLSL) bindings
		#define UnityObjectToClipPos TransformObjectToHClip
		#define _WorldSpaceLightPos0 _MainLightPosition
		
		// Curved World 2020
#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE CURVEDWORLD_BEND_TYPE_CYLINDRICALROLLOFF_Z
#define CURVEDWORLD_BEND_ID_1
		#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
		#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
		#include "Assets/PluginAssets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"

		ENDHLSL

		Pass
		{
			Name "Main"
			Tags
			{
				"LightMode"="UniversalForward"
			}
		Blend [_SrcBlend] [_DstBlend]
		Cull [_Cull]
		ZWrite [_ZWrite]

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard SRP library
			// All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			// -------------------------------------
			// Material keywords
			#pragma shader_feature_local _ _RECEIVE_SHADOWS_OFF

			// -------------------------------------
			// Universal Render Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile _ _FORWARD_PLUS
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"

			// -------------------------------------
			#pragma multi_compile_fog

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#pragma vertex Vertex
			#pragma fragment Fragment

			//--------------------------------------
			// Toony Colors Pro 2 keywords
			#pragma shader_feature_local_fragment TCP2_SPECULAR
		#pragma shader_feature_local _ _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local TCP2_MATCAP
			#pragma shader_feature_local_fragment TCP2_DISSOLVE

			// vertex input
			struct Attributes
			{
				float4 vertex       : POSITION;
				float3 normal       : NORMAL;
				float4 tangent      : TANGENT;
				float4 texcoord0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// vertex output / fragment input
			struct Varyings
			{
				float4 positionCS     : SV_POSITION;
				float3 normal         : NORMAL;
				float4 worldPosAndFog : TEXCOORD0;
			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord    : TEXCOORD1; // compute shadow coord per-vertex for the main light
			#endif
			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				half3 vertexLights : TEXCOORD2;
			#endif
				float4 pack0 : TEXCOORD3; /* pack0.xy = texcoord0  pack0.zw = matcap */
				float pack1 : TEXCOORD4; /* pack1.x = fogFactor */
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
				// Fake InputData struct needed for Forward+ macro
				struct InputDataForwardPlusDummy
				{
					float3  positionWS;
					float2  normalizedScreenSpaceUV;
				};
			#endif

			Varyings Vertex(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				// Texture Coordinates
				output.pack0.xy.xy = input.texcoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

				#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
					#ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
						CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(input.vertex, input.normal, input.tangent)
					#else
						CURVEDWORLD_TRANSFORM_VERTEX(input.vertex)
					#endif
				#endif

				float3 worldPos = mul(UNITY_MATRIX_M, input.vertex).xyz;
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				output.shadowCoord = GetShadowCoord(vertexInput);
			#endif

				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normal, input.tangent);
			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				// Vertex lighting
				output.vertexLights = VertexLighting(vertexInput.positionWS, vertexNormalInput.normalWS);
			#endif

				// world position
				output.worldPosAndFog = float4(vertexInput.positionWS.xyz, 0);

				// Computes fog factor per-vertex
				output.worldPosAndFog.w = ComputeFogFactor(vertexInput.positionCS.z);

				// normal
				output.normal = normalize(vertexNormalInput.normalWS);

				// clip position
				output.positionCS = vertexInput.positionCS;

				#if defined(TCP2_MATCAP)
				//MatCap
				float3 worldNorm = normalize(UNITY_MATRIX_I_M[0].xyz * input.normal.x + UNITY_MATRIX_I_M[1].xyz * input.normal.y + UNITY_MATRIX_I_M[2].xyz * input.normal.z);
				worldNorm = mul((float3x3)UNITY_MATRIX_V, worldNorm);
				output.pack0.zw = worldNorm.xy * 0.5 + 0.5;
				#endif

				return output;
			}

			half4 Fragment(Varyings input
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float3 positionWS = input.worldPosAndFog.xyz;
				float3 normalWS = normalize(input.normal);
				half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);

				// Shader Properties Sampling
				float4 __albedo = ( TCP2_TEX2D_SAMPLE(_MainTex, _MainTex, input.pack0.xy).rgba );
				float4 __mainColor = ( _BaseColor.rgba );
				float __alpha = ( __albedo.a * __mainColor.a );
				float __dissolveMap = ( TCP2_TEX2D_SAMPLE(_DissolveMap, _DissolveMap, input.pack0.xy).r );
				float __dissolveValue = ( _DissolveValue );
				float __dissolveGradientWidth = ( _DissolveGradientWidth );
				float __dissolveGradientStrength = ( 2.0 );
				float __ambientIntensity = ( 1.0 );
				float3 __matcapColor = ( _MatCapColor.rgb * __albedo.rgb );
				float __rampThreshold = ( _RampThreshold );
				float __rampSmoothing = ( _RampSmoothing );
				float3 __shadowColor = ( _SColor.rgb );
				float3 __highlightColor = ( _HColor.rgb );
				float __rimMin = ( _RimMin );
				float __rimMax = ( _RimMax );
				float3 __rimColor = ( _RimColor.rgb );
				float __rimStrength = ( 1.0 );
				float __specularRoughnessPbr = ( _SpecularRoughnessPBR );
				float3 __specularColor = ( _SpecularColor.rgb );

				half ndv = abs(dot(viewDirWS, normalWS));
				half ndvRaw = ndv;

				// main texture
				half3 albedo = __albedo.rgb;
				half alpha = __alpha;

				half3 emission = half3(0,0,0);
				
				//Dissolve
				#if defined(TCP2_DISSOLVE)
				half dissolveMap = __dissolveMap;
				half dissolveValue = __dissolveValue;
				half gradientWidth = __dissolveGradientWidth;
				float dissValue = dissolveValue*(1+2*gradientWidth) - gradientWidth;
				float dissolveUV = smoothstep(dissolveMap - gradientWidth, dissolveMap + gradientWidth, dissValue);
				clip((1-dissolveUV) - 0.001);
				half4 dissolveColor = ( _DissolveGradientTexture.rgba );
				dissolveColor *= __dissolveGradientStrength * dissolveUV;
				emission += dissolveColor.rgb;
				#endif
				// Alpha Testing
				half cutoffValue = Dither4x4(input.positionCS.xy);
				clip(alpha - cutoffValue);
				
				albedo *= __mainColor.rgb;

				// main light: direction, color, distanceAttenuation, shadowAttenuation
			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord = input.shadowCoord;
			#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
			#else
				float4 shadowCoord = float4(0, 0, 0, 0);
			#endif

			#if defined(URP_10_OR_NEWER)
				#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
					half4 shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
				#elif !defined (LIGHTMAP_ON)
					half4 shadowMask = unity_ProbesOcclusion;
				#else
					half4 shadowMask = half4(1, 1, 1, 1);
				#endif

				Light mainLight = GetMainLight(shadowCoord, positionWS, shadowMask);
			#else
				Light mainLight = GetMainLight(shadowCoord);
			#endif

			#if defined(_SCREEN_SPACE_OCCLUSION) || defined(USE_FORWARD_PLUS) || defined(USE_CLUSTER_LIGHT_LOOP)
				float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
			#endif

				// ambient or lightmap
				// Samples SH fully per-pixel. SampleSHVertex and SampleSHPixel functions
				// are also defined in case you want to sample some terms per-vertex.
				half3 bakedGI = SampleSH(normalWS);
				half occlusion = 1;

				half3 indirectDiffuse = bakedGI;
				indirectDiffuse *= occlusion * albedo * __ambientIntensity;

				//MatCap
				#if defined(TCP2_MATCAP)
				half2 capCoord = input.pack0.zw;
				half3 matcap = ( TCP2_TEX2D_SAMPLE(_MatCapTex, _MatCapTex, capCoord).rgb ) * __matcapColor;
				emission += matcap;
				#endif

				half3 lightDir = mainLight.direction;
				half3 lightColor = mainLight.color.rgb;

				half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

				half ndl = dot(normalWS, lightDir);
				half3 ramp;
				
				half rampThreshold = __rampThreshold;
				half rampSmooth = __rampSmoothing * 0.5;
				ndl = saturate(ndl);
				ramp = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndl);

				// apply attenuation
				ramp *= atten;

				// highlight/shadow colors
				ramp = lerp(__shadowColor, __highlightColor, ramp);
				
				// output color
				half3 color = half3(0,0,0);
				// Rim Lighting
				half rim = 1 - ndvRaw;
				rim = ( rim );
				half rimMin = __rimMin;
				half rimMax = __rimMax;
				rim = smoothstep(rimMin, rimMax, rim);
				half3 rimColor = __rimColor;
				half rimStrength = __rimStrength;
				//Rim light mask
				emission.rgb += ndl * saturate(atten) * rim * rimColor * rimStrength;
				color += albedo * lightColor.rgb * ramp;

				half3 halfDir = SpecSafeNormalize(float3(lightDir) + float3(viewDirWS));
				
				#if defined(TCP2_SPECULAR)
				//Specular: GGX
				half roughness = __specularRoughnessPbr*__specularRoughnessPbr;
				half nh = saturate(dot(normalWS, halfDir));
				half spec = GGX(nh, saturate(roughness));
				spec *= TCP2_PI * 0.05;
				#ifdef UNITY_COLORSPACE_GAMMA
					spec = max(0, sqrt(max(1e-4h, spec)));
					half surfaceReduction = 1.0 - 0.28 * roughness * __specularRoughnessPbr;
				#else
					half surfaceReduction = 1.0 / (roughness*roughness + 1.0);
				#endif
				spec = max(0, spec * ndl);
				spec *= surfaceReduction;
				spec *= atten;
				
				//Apply specular
				emission.rgb += spec * lightColor.rgb * __specularColor;
				#endif

				// Additional lights loop
			#ifdef _ADDITIONAL_LIGHTS
				uint pixelLightCount = GetAdditionalLightsCount();

				#if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
					// Additional directional lights in Forward+
					for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
					{
						FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

						Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);

						#if defined(_LIGHT_LAYERS)
							if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
						#endif
						{
							half atten = light.shadowAttenuation * light.distanceAttenuation;

							#if defined(_LIGHT_LAYERS)
								half3 lightDir = half3(0, 1, 0);
								half3 lightColor = half3(0, 0, 0);
								if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
								{
									lightColor = light.color.rgb;
									lightDir = light.direction;
								}
							#else
								half3 lightColor = light.color.rgb;
								half3 lightDir = light.direction;
							#endif

							half ndl = dot(normalWS, lightDir);
							half3 ramp;
							
							ndl = saturate(ndl);
							ramp = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndl);

							// apply attenuation (shadowmaps & point/spot lights attenuation)
							ramp *= atten;

							// apply highlight color
							ramp = lerp(half3(0,0,0), __highlightColor, ramp);
							
							// output color
							color += albedo * lightColor.rgb * ramp;

							half3 halfDir = SpecSafeNormalize(float3(lightDir) + float3(viewDirWS));
							
							#if defined(TCP2_SPECULAR)
							//Specular: GGX
							half roughness = __specularRoughnessPbr*__specularRoughnessPbr;
							half nh = saturate(dot(normalWS, halfDir));
							half spec = GGX(nh, saturate(roughness));
							spec *= TCP2_PI * 0.05;
							#ifdef UNITY_COLORSPACE_GAMMA
								spec = max(0, sqrt(max(1e-4h, spec)));
								half surfaceReduction = 1.0 - 0.28 * roughness * __specularRoughnessPbr;
							#else
								half surfaceReduction = 1.0 / (roughness*roughness + 1.0);
							#endif
							spec = max(0, spec * ndl);
							spec *= surfaceReduction;
							spec *= atten;
							
							//Apply specular
							emission.rgb += spec * lightColor.rgb * __specularColor;
							#endif
							// Rim light mask
							half3 rimColor = __rimColor;
							half rimStrength = __rimStrength;
							emission.rgb += ndl * saturate(atten) * rim * rimColor * rimStrength;
						}
					}

					// Data with dummy struct used in Forward+ macro (LIGHT_LOOP_BEGIN)
					InputDataForwardPlusDummy inputData;
					inputData.normalizedScreenSpaceUV = normalizedScreenSpaceUV;
					inputData.positionWS = positionWS;
				#endif

				LIGHT_LOOP_BEGIN(pixelLightCount)
				{
					#if defined(URP_10_OR_NEWER)
						Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
					#else
						Light light = GetAdditionalLight(lightIndex, positionWS);
					#endif
					half atten = light.shadowAttenuation * light.distanceAttenuation;

					#if defined(_LIGHT_LAYERS)
						half3 lightDir = half3(0, 1, 0);
						half3 lightColor = half3(0, 0, 0);
						if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
						{
							lightColor = light.color.rgb;
							lightDir = light.direction;
						}
					#else
						half3 lightColor = light.color.rgb;
						half3 lightDir = light.direction;
					#endif

					half ndl = dot(normalWS, lightDir);
					half3 ramp;
					
					ndl = saturate(ndl);
					ramp = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndl);

					// apply attenuation (shadowmaps & point/spot lights attenuation)
					ramp *= atten;

					// apply highlight color
					ramp = lerp(half3(0,0,0), __highlightColor, ramp);
					
					// output color
					color += albedo * lightColor.rgb * ramp;

					half3 halfDir = SpecSafeNormalize(float3(lightDir) + float3(viewDirWS));
					
					#if defined(TCP2_SPECULAR)
					//Specular: GGX
					half roughness = __specularRoughnessPbr*__specularRoughnessPbr;
					half nh = saturate(dot(normalWS, halfDir));
					half spec = GGX(nh, saturate(roughness));
					spec *= TCP2_PI * 0.05;
					#ifdef UNITY_COLORSPACE_GAMMA
						spec = max(0, sqrt(max(1e-4h, spec)));
						half surfaceReduction = 1.0 - 0.28 * roughness * __specularRoughnessPbr;
					#else
						half surfaceReduction = 1.0 / (roughness*roughness + 1.0);
					#endif
					spec = max(0, spec * ndl);
					spec *= surfaceReduction;
					spec *= atten;
					
					//Apply specular
					emission.rgb += spec * lightColor.rgb * __specularColor;
					#endif
					// Rim light mask
					half3 rimColor = __rimColor;
					half rimStrength = __rimStrength;
					emission.rgb += ndl * saturate(atten) * rim * rimColor * rimStrength;
				}
				LIGHT_LOOP_END
			#endif
			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				color += input.vertexLights * albedo;
			#endif

				// apply ambient
				color += indirectDiffuse;

				// Premultiply blending
				#if defined(_ALPHAPREMULTIPLY_ON)
					color.rgb *= alpha;
				#endif

				color += emission;

				// Mix the pixel color with fogColor. You can optionally use MixFogColor to override the fogColor with a custom one.
				float fogFactor = input.worldPosAndFog.w;
				color = MixFog(color, fogFactor);

				return half4(color, alpha);
			}
			ENDHLSL
		}

		// Depth & Shadow Caster Passes
		HLSLINCLUDE

		#if defined(SHADOW_CASTER_PASS) || defined(DEPTH_ONLY_PASS)

			#define fixed half
			#define fixed2 half2
			#define fixed3 half3
			#define fixed4 half4

			float3 _LightDirection;
			float3 _LightPosition;

			struct Attributes
			{
				float4 vertex   : POSITION;
				float3 normal   : NORMAL;
				float4 tangent : TANGENT;
				float4 texcoord0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS     : SV_POSITION;
				float3 normal         : NORMAL;
				float3 pack0 : TEXCOORD1; /* pack0.xyz = positionWS */
				float4 pack1 : TEXCOORD2; /* pack1.xy = texcoord0  pack1.zw = matcap */
			#if defined(DEPTH_ONLY_PASS)
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			#endif
			};

			float4 GetShadowPositionHClip(Attributes input)
			{
				float3 positionWS = TransformObjectToWorld(input.vertex.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normal);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif
				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
					positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif

				return positionCS;
			}

			Varyings ShadowDepthPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				#if defined(DEPTH_ONLY_PASS)
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				#endif

				float3 worldNormalUv = mul(UNITY_MATRIX_M, float4(input.normal, 1.0)).xyz;

				// Texture Coordinates
				output.pack1.xy.xy = input.texcoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

				#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
					#ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
						CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(input.vertex, input.normal, input.tangent)
					#else
						CURVEDWORLD_TRANSFORM_VERTEX(input.vertex)
					#endif
				#endif

				float3 worldPos = mul(UNITY_MATRIX_M, input.vertex).xyz;
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
				output.normal = normalize(worldNormalUv);
				output.pack0.xyz = vertexInput.positionWS;

				#if defined(DEPTH_ONLY_PASS)
					output.positionCS = TransformObjectToHClip(input.vertex.xyz);
				#elif defined(SHADOW_CASTER_PASS)
					output.positionCS = GetShadowPositionHClip(input);
				#else
					output.positionCS = float4(0,0,0,0);
				#endif

				return output;
			}

			half4 ShadowDepthPassFragment(
				Varyings input
			) : SV_TARGET
			{
				#if defined(DEPTH_ONLY_PASS)
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				#endif

				float3 positionWS = input.pack0.xyz;
				float3 normalWS = normalize(input.normal);

				// Shader Properties Sampling
				float4 __albedo = ( TCP2_TEX2D_SAMPLE(_MainTex, _MainTex, input.pack1.xy).rgba );
				float4 __mainColor = ( _BaseColor.rgba );
				float __alpha = ( __albedo.a * __mainColor.a );
				float __dissolveMap = ( TCP2_TEX2D_SAMPLE(_DissolveMap, _DissolveMap, input.pack1.xy).r );
				float __dissolveValue = ( _DissolveValue );
				float __dissolveGradientWidth = ( _DissolveGradientWidth );
				float __dissolveGradientStrength = ( 2.0 );

				half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
				half ndv = abs(dot(viewDirWS, normalWS));
				half ndvRaw = ndv;

				half3 albedo = half3(1,1,1);
				half alpha = __alpha;
				half3 emission = half3(0,0,0);
				
				//Dissolve
				#if defined(TCP2_DISSOLVE)
				half dissolveMap = __dissolveMap;
				half dissolveValue = __dissolveValue;
				half gradientWidth = __dissolveGradientWidth;
				float dissValue = dissolveValue*(1+2*gradientWidth) - gradientWidth;
				float dissolveUV = smoothstep(dissolveMap - gradientWidth, dissolveMap + gradientWidth, dissValue);
				clip((1-dissolveUV) - 0.001);
				half4 dissolveColor = ( _DissolveGradientTexture.rgba );
				dissolveColor *= __dissolveGradientStrength * dissolveUV;
				emission += dissolveColor.rgb;
				#endif
				// Alpha Testing
				half cutoffValue = Dither4x4(input.positionCS.xy);
				clip(alpha - cutoffValue);

				return 0;
			}

		#endif
		ENDHLSL

		Pass
		{
			Name "ShadowCaster"
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			// using simple #define doesn't work, we have to use this instead
			#pragma multi_compile SHADOW_CASTER_PASS

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#pragma vertex ShadowDepthPassVertex
			#pragma fragment ShadowDepthPassFragment

			//--------------------------------------
			// Toony Colors Pro 2 keywords
			#pragma shader_feature_local_fragment TCP2_DISSOLVE

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags
			{
				"LightMode" = "DepthOnly"
			}

			ZWrite On
			ColorMask 0
			Cull [_Cull]

			HLSLPROGRAM

			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			// using simple #define doesn't work, we have to use this instead
			#pragma multi_compile DEPTH_ONLY_PASS

			#pragma vertex ShadowDepthPassVertex
			#pragma fragment ShadowDepthPassFragment

			//--------------------------------------
			// Toony Colors Pro 2 keywords
			#pragma shader_feature_local_fragment TCP2_DISSOLVE

			ENDHLSL
		}

		// Curved World 2020 - Scene Picking passes
		Pass
		{
			Name "ScenePickingPass"
			Tags { "LightMode" = "Picking" }

			BlendOp Add
			Blend One Zero
			ZWrite On
			Cull Off

			CGPROGRAM

			#include "HLSLSupport.cginc"
			#include "UnityShaderVariables.cginc"
			#include "UnityShaderUtilities.cginc"

			#pragma target 3.0

			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature_local _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_instancing

#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE CURVEDWORLD_BEND_TYPE_CYLINDRICALROLLOFF_Z
#define CURVEDWORLD_BEND_ID_1
			#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#include "Assets/PluginAssets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"

			#pragma vertex vertEditorPass
			#pragma fragment fragScenePickingPass

			#include "Assets/PluginAssets/Amazing Assets/Curved World/Shaders/Core/SceneSelection.cginc"

			ENDCG
		}

		Pass
		{
			Name "SceneSelectionPass"
			Tags { "LightMode" = "SceneSelectionPass" }

			BlendOp Add
			Blend One Zero
			ZWrite On
			Cull Off

			CGPROGRAM

			#include "HLSLSupport.cginc"
			#include "UnityShaderVariables.cginc"
			#include "UnityShaderUtilities.cginc"

			#pragma target 3.0

			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature_local _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_instancing

#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE CURVEDWORLD_BEND_TYPE_CYLINDRICALROLLOFF_Z
#define CURVEDWORLD_BEND_ID_1
			#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#include "Assets/PluginAssets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"

			#pragma vertex vertEditorPass
			#pragma fragment fragSceneHighlightPass

			#include "Assets/PluginAssets/Amazing Assets/Curved World/Shaders/Core/SceneSelection.cginc"

			ENDCG
		}
	}

	FallBack "Hidden/InternalErrorShader"
	CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_SG2"
}

/* TCP_DATA u config(ver:"2.9.21";unity:"2022.3.62t7";tmplt:"SG2_Template_URP";features:list["ENABLE_FORWARD_PLUS","UNITY_5_4","UNITY_5_5","UNITY_5_6","UNITY_2017_1","UNITY_2018_1","UNITY_2018_2","UNITY_2018_3","UNITY_2019_1","SPECULAR","RIM","RIM_LIGHTMASK","SHADOW_COLOR_MAIN_DIR","UNITY_2019_2","UNITY_2019_3","UNITY_2019_4","UNITY_2020_1","UNITY_2021_1","UNITY_2021_2","UNITY_2022_2","SPEC_PBR_GGX","MATCAP","CURVED_WORLD_2020","FOG","MATCAP_ADD","MATCAP_SHADER_FEATURE","AUTO_TRANSPARENT_BLENDING","SPECULAR_SHADER_FEATURE","DISSOLVE","DISSOLVE_SHADER_FEATURE","DISSOLVE_CLIP","DISSOLVE_GRADIENT","ALPHA_TESTING","ALPHA_TESTING_DITHERING","TEMPLATE_LWRP"];flags:list[];flags_extra:dict[];keywords:dict[RampTextureDrawer="[TCP2Gradient]",RampTextureLabel="Ramp Texture",SHADER_TARGET="3.0",RIM_LABEL="Rim Lighting",RENDER_TYPE="TransparentCutout",CURVED_WORLD_2020_INCLUDE="Assets/PluginAssets/Amazing Assets/Curved World/Shaders/Core"];shaderProperties:list[sp(name:"Albedo";imps:list[imp_mp_texture(uto:True;tov:"";tov_lbl:"";gto:True;sbt:False;scr:False;scv:"";scv_lbl:"";gsc:False;roff:False;goff:False;sin_anm:False;sin_anmv:"";sin_anmv_lbl:"";gsin:False;notile:False;triplanar_local:False;def:"white";locked_uv:False;uv:0;cc:4;chan:"RGBA";mip:-1;mipprop:False;ssuv_vert:False;ssuv_obj:False;uv_type:Texcoord;uv_chan:"XZ";tpln_scale:1;uv_shaderproperty:__NULL__;uv_cmp:__NULL__;sep_sampler:__NULL__;prop:"_MainTex";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"4030e6dc-614f-4a02-b744-6c40b441e248";op:Multiply;lbl:"Albedo";gpu_inst:False;dots_inst:False;locked:False;impl_index:-1)];layers:list[];unlocked:list[];layer_blend:dict[];custom_blend:dict[];clones:dict[];isClone:False),,,,,,,,,,,,,,,sp(name:"MatCap Color";imps:list[imp_mp_color(def:RGBA(1, 1, 1, 1);hdr:True;cc:3;chan:"RGB";prop:"_MatCapColor";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"49380e92-5f3d-4873-bd83-c6b1b77b560c";op:Multiply;lbl:"MatCap Color";gpu_inst:False;dots_inst:False;locked:False;impl_index:0),imp_spref(cc:3;chan:"RGB";lsp:"Albedo";guid:"1b9937a0-7ad1-47f1-8d2a-abff46ed406c";op:Multiply;lbl:"MatCap Color";gpu_inst:False;dots_inst:False;locked:False;impl_index:-1)];layers:list[];unlocked:list[];layer_blend:dict[];custom_blend:dict[];clones:dict[];isClone:False),,,,sp(name:"Dissolve Gradient Texture";imps:list[imp_mp_color(def:RGBA(1, 0.5064471, 0, 1);hdr:True;cc:4;chan:"RGBA";prop:"_DissolveGradientTexture";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"8142c3f1-4dcf-4f4f-8a89-4e510095acee";op:Multiply;lbl:"Gradient Texture";gpu_inst:False;dots_inst:False;locked:False;impl_index:-1)];layers:list[];unlocked:list[];layer_blend:dict[];custom_blend:dict[];clones:dict[];isClone:False),,,,,,,,,,,,,,,,,sp(name:"Emission";imps:list[imp_mp_color(def:RGBA(0, 0, 0, 1);hdr:True;cc:3;chan:"RGB";prop:"_Emission";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"84301501-636e-40de-a68e-52353fa81b30";op:Multiply;lbl:"Emission Color";gpu_inst:False;dots_inst:False;locked:False;impl_index:-1),imp_vcolors(cc:3;chan:"AAA";linear:False;guid:"3756428c-99e2-4293-92a4-317d82550f48";op:Multiply;lbl:"Emission";gpu_inst:False;dots_inst:False;locked:False;impl_index:-1)];layers:list[];unlocked:list[];layer_blend:dict[];custom_blend:dict[];clones:dict[];isClone:False)];customTextures:list[];codeInjection:codeInjection(injectedFiles:list[];mark:False);matLayers:list[]) */
/* TCP_HASH f898fe7a8ce558a1a627c0d156e32d10 */
