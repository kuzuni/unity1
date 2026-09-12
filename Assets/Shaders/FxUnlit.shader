// T39 — 임팩트 연출용 무조명 셰이더(정본 `MeshBasicMaterial({ transparent, blending: Additive|Normal, depthWrite:false, depthTest:false|true, toneMapped:false })` 자리).
//   정점색 × _BaseColor × 텍스처(플레어·그을음) · 블렌드/깊이 검사는 재질 칸으로(가산 = One One · 보통 = SrcAlpha OneMinusSrcAlpha · depthTest false = ZTest Always).
//   안개는 안 섞는다(정본 Basic 재질도 fog 를 받지만 임팩트 쿼드는 카메라 가까이라 차이가 없다 — 결정 기록).
Shader "Forge/FxUnlit"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst blend", Float) = 10
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest [_ZTest]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex FxVert
            #pragma fragment FxFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _MainTex_ST;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings FxVert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 FxFrag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 c = _BaseColor * i.color * t;
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
