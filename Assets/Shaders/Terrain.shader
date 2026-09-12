// T38 — 지면 셰이더(정본 scene3d.js `terrainShade` + `applyShadeLift` 의 지면 갈래를 URP 로).
//   ⓐ 매크로 변조: 같은 알베도를 uv × _MacroScale(1/6 = 월드 30 주기)로 한 번 더 떠서 밝기 ±(uMacro) — 타일 동일성을 깬다.
//   ⓑ 거리 LOD: 카메라 거리 _LodNear~_LodFar 에서 저주파(매크로) 쪽으로 _Lod 만큼 섞는다.
//   ⓒ 눈 수광면 탈색(_Snow>0 · 눈 바이옴만): 밝을수록 무채(=백색)로 — smoothstep(0.06, 0.40, 휘도) × _Snow.
//   ⓓ 암부 리프트(applyShadeLift): 그늘(면이 태양을 등짐 · 남이 드리움) 화소에 _ShadeTint × _ShadeStr 을 더한다 — 밝은 면은 그대로.
//   노면 회랑(uRoad)은 T9 가 정점색 배율로 이미 낸다(정점색을 곱한다). 정점색·알베도·노멀맵·발광맵·안개·주광 그림자·추가광(림)을 URP Lit 규약으로.
//   값은 Core `TerrainShade`(정본 TERRAIN·SHADE·setTheme)가 계산하고 `GroundTextures.Apply` 가 넣는다.
Shader "Forge/Terrain"
{
    Properties
    {
        _BaseMap ("Albedo (지면 소재 · 12x6)", 2D) = "white" {}
        _BaseColor ("Color (흙 보정색)", Color) = (1, 1, 1, 1)
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 0.7
        _EmissionMap ("Emission (용암 균열)", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Macro ("Macro (타일 동일성 깨기)", Float) = 0.30
        _MacroScale ("Macro uv scale (1/6)", Float) = 0.1666667
        _Lod ("Distance LOD mix", Float) = 0.45
        _LodNear ("LOD near", Float) = 14
        _LodFar ("LOD far", Float) = 34
        _Snow ("Snow bleach (눈 바이옴만)", Float) = 0
        _ShadeTint ("Shade lift tint", Color) = (0, 0, 0, 1)
        _ShadeStr ("Shade lift strength", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "IgnoreProjector" = "True" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BumpMap_ST;
            float4 _EmissionMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half4 _EmissionColor;
            half _Smoothness;
            half _Metallic;
            half _Macro;
            half _MacroScale;
            half _Lod;
            half _LodNear;
            half _LodFar;
            half _Snow;
            half4 _ShadeTint;
            half _ShadeStr;
        CBUFFER_END

        TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TerrainVert
            #pragma fragment TerrainFrag

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _EMISSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                half4  tangentWS  : TEXCOORD3;
                half4  color      : COLOR;
                half   fogFactor  : TEXCOORD4;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD5;
                #endif
            };

            // 정본 GLSL smoothstep(e0 > e1 도 그대로 식) — HLSL 은 e0 > e1 을 보장하지 않으니 식으로 쓴다.
            half SmoothStepAny(half e0, half e1, half x)
            {
                half t = saturate((x - e0) / (e1 - e0));
                return t * t * (3.0 - 2.0 * t);
            }

            Varyings TerrainVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                VertexPositionInputs vi = GetVertexPositionInputs(v.positionOS.xyz);
                // 지면 메시(T9)는 접선이 없다(0 벡터) — 평면이 +y 를 보고 u 가 +x 라 (1,0,0,1) 로 대신한다.
                float4 tangentOS = v.tangentOS;
                if (dot(tangentOS.xyz, tangentOS.xyz) < 1e-8) tangentOS = float4(1.0, 0.0, 0.0, 1.0);
                VertexNormalInputs ni = GetVertexNormalInputs(v.normalOS, tangentOS);
                o.positionCS = vi.positionCS;
                o.positionWS = vi.positionWS;
                o.normalWS = ni.normalWS;
                o.tangentWS = half4(ni.tangentWS, tangentOS.w);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                o.fogFactor = ComputeFogFactor(vi.positionCS.z);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                o.shadowCoord = GetShadowCoord(vi);
                #endif
                return o;
            }

            half4 TerrainFrag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                // ⓐ 매크로 변조 + ⓑ 거리 LOD (정본 map_fragment 치환)
                half4 tDet = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half4 tMac = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * _MacroScale);
                half3 texel = tDet.rgb;
                texel *= 1.0 + (dot(tMac.rgb, half3(0.3333, 0.3333, 0.3333)) - 0.5) * _Macro;
                float dist = length(i.positionWS - GetCameraPositionWS());
                texel = lerp(texel, tMac.rgb, smoothstep(_LodNear, _LodFar, (half)dist) * _Lod);
                half3 albedo = _BaseColor.rgb * i.color.rgb * texel;

                half3 normalWS = normalize(i.normalWS);
                #if defined(_NORMALMAP)
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                half3 tangentWS = normalize(i.tangentWS.xyz);
                half3 bitangentWS = cross(normalWS, tangentWS) * i.tangentWS.w;
                normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(tangentWS, bitangentWS, normalWS)));
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = i.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                inputData.fogCoord = i.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.emission = half3(0, 0, 0);
                #if defined(_EMISSION)
                // three r128 은 map 의 uvTransform 을 emissiveMap 에도 건다 — 같은 uv
                surfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
                #endif

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // ⓒ 눈 수광면 탈색 — 임계는 선형 공간 값(정본 주석)
                if (_Snow > 0.0)
                {
                    half snowL = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
                    color.rgb = lerp(color.rgb, half3(snowL, snowL, snowL), smoothstep(0.06, 0.40, snowL) * _Snow);
                }

                // ⓓ 암부 리프트 — 그늘은 두 갈래: 면이 태양을 등졌거나(N·L) · 남이 드리웠거나(주광 그림자)
                Light mainLight = GetMainLight(inputData.shadowCoord);
                half ndl = dot(normalWS, mainLight.direction);
                half kSelf = SmoothStepAny(0.25, -0.35, ndl);
                half kCast = 1.0 - mainLight.shadowAttenuation;
                half k = max(kSelf, kCast);
                color.rgb += _ShadeTint.rgb * (k * _ShadeStr);

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes { float4 positionOS : POSITION; };
            struct DepthVaryings { float4 positionCS : SV_POSITION; };

            DepthVaryings DepthVert(DepthAttributes v)
            {
                DepthVaryings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half DepthFrag(DepthVaryings i) : SV_Target
            {
                return i.positionCS.z;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
