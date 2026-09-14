// T330 2회차 — 파츠 ID 보조 패스의 재질. 정본 web/js/scene3d.js 880~890 `idMatFor` 의 ShaderMaterial 그대로:
//   정점: vZ = -(modelView × position).z(뷰공간 앞 거리) · 프래그먼트: (uId.r, uId.g, 0, clamp(vZ / uZFar, 0, 1)).
// 파츠마다 이 재질의 인스턴스 하나가 `_PartId` 에 제 번호를 굳혀 쥔다(정본 «파츠마다 ShaderMaterial 인스턴스 · 프로그램은 하나»).
// b 채널에도 같은 깊이를 적어 둔다 — 정본은 0 이지만 알파가 사라지는 경로(후처리·포맷)를 진단할 때 b 로 견준다. 컴포짓은 정본대로 a 를 읽는다.
// 수치(idZFar)는 전역 `_EdgeIdZFar` — EdgeOutlineHost 가 표(EdgeOutlineUi.json)에서 싣는다.
Shader "Forge/EdgePartId"
{
    Properties
    {
        _PartId ("Part Id (r=low byte/255, g=high byte/255)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "EdgePartId"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On ZTest LEqual Cull Back Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _PartId;
            CBUFFER_END
            float _EdgeIdZFar;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float vz : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float3 vs = TransformWorldToView(ws);
                o.vz = -vs.z;
                o.positionCS = TransformWorldToHClip(ws);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float a = saturate(i.vz / max(_EdgeIdZFar, 1e-4));
                return half4(_PartId.x, _PartId.y, a, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
