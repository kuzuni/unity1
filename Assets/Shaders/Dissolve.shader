// T39 — 적 몸 셰이더(정본 scene3d.js 의 세 훅을 URP 로): ⓐ `applyRimLight`(1375 · 다크 컨투어 + 밝은 림 · ENEMY_RIM) ⓑ `flashMesh`(12768 · 피격 emissive 가산)
//   ⓒ `installDissolve/setDissolve`(12494 · 값 노이즈 2옥타브 알파 클립 + 잔불). 정점색(T4 팔레트) × _BaseColor × _Tint(보스 재질 ×0.68) 를 URP PBR 로 비추고
//   림 → 플래시 → 안개 순으로 얹는다. 디졸브 discard 는 조명 전에(정본 clipping_planes_fragment 자리). 살아 있는 동안 _Dissolve = 0 이라 화면은 한 픽셀도 안 바뀐다.
Shader "Forge/EnemyBody"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Tint ("Boss tint (bossMaterialTell)", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _Metallic ("Metallic", Range(0, 1)) = 0
        _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _RimColor ("Rim color", Color) = (0.863, 0.937, 1, 1)
        _RimStr ("Rim strength", Float) = 0
        _RimPow ("Rim power", Float) = 5
        _RimDark ("Dark contour color", Color) = (0.039, 0.067, 0.098, 1)
        _RimDarkStr ("Dark contour strength", Float) = 0.98
        _RimDarkPow ("Dark contour power", Float) = 0.85
        _FlashColor ("Flash color", Color) = (1, 1, 1, 1)
        _Flash ("Flash intensity", Float) = 0
        _Dissolve ("Dissolve threshold", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "IgnoreProjector" = "True" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _Tint;
            half _Smoothness;
            half _Metallic;
            half4 _EmissionColor;
            half4 _RimColor;
            half _RimStr;
            half _RimPow;
            half4 _RimDark;
            half _RimDarkStr;
            half _RimDarkPow;
            half4 _FlashColor;
            half _Flash;
            half _Dissolve;
        CBUFFER_END

        // 정본 DISSOLVE_NOISE — 값 노이즈(해시 + 스무스 보간)
        float DsvHash(float3 p)
        {
            p = frac(p * 0.3183099 + float3(0.71, 0.113, 0.419));
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }
        float DsvNoise(float3 x)
        {
            float3 i = floor(x), f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(lerp(DsvHash(i), DsvHash(i + float3(1, 0, 0)), f.x), lerp(DsvHash(i + float3(0, 1, 0)), DsvHash(i + float3(1, 1, 0)), f.x), f.y),
                        lerp(lerp(DsvHash(i + float3(0, 0, 1)), DsvHash(i + float3(1, 0, 1)), f.x), lerp(DsvHash(i + float3(0, 1, 1)), DsvHash(i + float3(1, 1, 1)), f.x), f.y), f.z);
        }
        // 두 옥타브 + 아래를 조금 오래 남긴다(발밑부터 사라지면 시체가 떠오르는 것처럼 보인다)
        float DsvField(float3 posOS)
        {
            float n = DsvNoise(posOS * 6.5) * 0.62 + DsvNoise(posOS * 15.0) * 0.38;
            n += clamp(posOS.y, -0.6, 1.2) * 0.10;
            return n;
        }
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
            #pragma vertex BodyVert
            #pragma fragment BodyFrag

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
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                half4  color      : COLOR;
                half   fogFactor  : TEXCOORD3;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD4;
                #endif
            };

            Varyings BodyVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                VertexPositionInputs vi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs ni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vi.positionCS;
                o.positionWS = vi.positionWS;
                o.positionOS = v.positionOS.xyz;
                o.normalWS = ni.normalWS;
                o.color = v.color;
                o.fogFactor = ComputeFogFactor(vi.positionCS.z);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                o.shadowCoord = GetShadowCoord(vi);
                #endif
                return o;
            }

            half4 BodyFrag(Varyings i) : SV_Target
            {
                // ⓒ 디졸브 — 버리는 건 조명 계산 전에
                float dsvN = 0.0;
                if (_Dissolve > 0.0)
                {
                    dsvN = DsvField(i.positionOS);
                    clip(dsvN - _Dissolve);
                }

                half3 albedo = _BaseColor.rgb * _Tint.rgb * i.color.rgb;
                half3 normalWS = normalize(i.normalWS);

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
                surfaceData.emission = _EmissionColor.rgb;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // ⓐ 림(applyRimLight) — 정본은 뷰 공간 법선·시선으로 프레넬을 잰다(같은 내적) · 위/뒤쪽 가중은 법선 y
                half fres = 1.0 - saturate(dot(normalWS, inputData.viewDirectionWS));
                half df = pow(fres, _RimDarkPow) * _RimDarkStr;
                color.rgb = lerp(color.rgb, _RimDark.rgb, saturate(df));
                half rf = pow(fres, _RimPow);
                rf *= lerp(0.3, 1.0, saturate(normalWS.y * 0.5 + 0.62));
                color.rgb += _RimColor.rgb * (rf * _RimStr);

                // ⓑ 피격 플래시 — emissive 가산(호출부가 세기·색을 준다)
                color.rgb += _FlashColor.rgb * _Flash;

                // ⓒ 잔불 — 최종 색이 나온 뒤 경계만 태운다
                if (_Dissolve > 0.0)
                {
                    half dsvE = 1.0 - smoothstep(_Dissolve, _Dissolve + 0.13, dsvN);
                    color.rgb = lerp(color.rgb, half3(1.0, 0.52, 0.16) * 1.6, dsvE * 0.92);
                }

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
            struct DepthVaryings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            DepthVaryings DepthVert(DepthAttributes v)
            {
                DepthVaryings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half DepthFrag(DepthVaryings i) : SV_Target
            {
                if (_Dissolve > 0.0) clip(DsvField(i.positionOS) - _Dissolve);
                return i.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct ShadowVaryings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            ShadowVaryings ShadowVert(ShadowAttributes v)
            {
                ShadowVaryings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
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
                o.positionCS = positionCS;
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                if (_Dissolve > 0.0) clip(DsvField(i.positionOS) - _Dissolve);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
