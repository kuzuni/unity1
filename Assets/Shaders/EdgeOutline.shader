// T147 2회차 — 캐릭터 윤곽선(후처리 깊이-엣지 아웃라인).
// 정본 web/js/scene3d.js initPost() 컴포짓 프래그먼트를 그대로 옮긴 것이고, 셈의 정본은
// Assets/Scripts/Core/Render/EdgeOutlineRules.cs 다(EditMode 가 그 셈을 잰다 · 여기는 같은 식을 HLSL 로).
// 수치는 하나도 안 박는다 — 전부 전역 유니폼이고 EdgeOutlineHost 가 EdgeOutlineUi.json 표에서 실어 준다.
// 파츠 ID 항(④)은 T330 2회차 — 보조 패스(EdgeIdPass)가 액터 파츠만 `_EdgeIdTex` 에 굽고(rgb = 16bit 번호 · a = 선형깊이/idZFar) 여기서 IdLine 을 잇는다.
Shader "Forge/EdgeOutline"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        // 🚨 **색을 읽지 않고 «위에 얹는다»** (T147 8회차 · 런 395 의 t147-feature.txt 가 준 답).
        //    정본은 `c = mix(c, 검정, edge)` 인데, 알파 혼합이 그것과 **같은 식**이다(edge 가 0/1 이라 더 그렇다).
        //    입력(`_BlitTexture`)을 안 읽으면 URP 의 색 복사본(`fetchColorBuffer`)이 필요 없어진다 —
        //    손으로 쓴 렌더러 에셋에서 그 칸만 안 실리던 자리를 **아예 안 쓰는 쪽으로** 피한다.
        //    덤: 프레임마다 전체화면 색 복사 한 번이 사라진다(§1 60fps).
        ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "EdgeOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // 🚨 전체화면 블릿의 `Vert`·`Varyings`·`_BlitTexture` 는 **core 패키지의 Runtime/Utilities** 에 있다 —
            //    `universal/ShaderLibrary/Blit.hlsl` 은 **없는 경로**다(런 343 실측: «Couldn't open include file»).
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _EdgeOn;        // 판정기의 «off 프레임» — 0 이면 네 항이 통째로 꺼진다(정본 규칙)
            float _EdgeK;         // 실루엣 상대 임계
            float _EdgeNormalK;   // 법선 불연속 임계(1 - dot)
            float _EdgeCreaseK;   // 곡률 임계(역깊이 2차 차분)
            float _EdgeCreaseHyst;// 곡률 이력 비율
            float _EdgeMaxZ;      // 지평선 컷 — 두께 판정엔 절대 안 쓴다
            float _EdgeDilate;    // 1 이면 한 칸 팽창(버퍼/CSS 비 2 이상일 때만)
            float _EdgeDiagF;     // 대각 탭 거리 환산(1/√2)
            float _EdgeR2F;       // 반경 2 탭 거리 환산(1/2)
            float4 _EdgeLineColor;
            // ④ 파츠 ID 항(T330) — 보조 패스가 굽는 ID 버퍼와 그 계수. `_EdgeIdOn` 은 «이 카메라에 ID 버퍼가 있는가»(Core EdgeIdTaps.Has).
            float _EdgeIdOn;
            float _EdgeIdZFar;    // a 채널 깊이 스케일
            float _EdgeIdTolZ;    // 가시성 검증 허용오차(뷰공간 유닛)
            float _EdgeIdLoF;     // 키 = r × 255 + g × 65280
            float _EdgeIdHiF;
            TEXTURE2D(_EdgeIdTex);
            SAMPLER(sampler_EdgeIdTex);

            float LinZ(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            // ID 화소 → 정수 키(Core EdgeOutlineRules.IdKey · 정본 idkey). 앞의 step 이 가시성 검증 — 가려진 액터 화소는 키 0(배경).
            float IdKey(float2 uv, float z)
            {
                float4 j = SAMPLE_TEXTURE2D_LOD(_EdgeIdTex, sampler_EdgeIdTex, uv, 0);
                return step(abs(j.a * _EdgeIdZFar - z), _EdgeIdTolZ) * (j.r * _EdgeIdLoF + j.g * _EdgeIdHiF);
            }

            // 깊이만으로 뷰공간 법선 복원 — n ∝ (q_u, q_v, -(q - u·q_u - v·q_v)).
            float3 PNrm(float q, float gu, float gv, float2 uv)
            {
                return normalize(float3(gu, gv, -(q - uv.x * gu - uv.y * gv)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                // 판정기의 «off 프레임» — 아무것도 안 얹는다(알파 0).
                if (_EdgeOn < 0.5) return half4(0.0, 0.0, 0.0, 0.0);

                float2 texel = _ScreenParams.zw - 1.0;
                float2 tx = float2(texel.x, 0.0), ty = float2(0.0, texel.y);

                float z0 = LinZ(uv);
                float zl = LinZ(uv - tx), zr = LinZ(uv + tx), zd = LinZ(uv - ty), zu = LinZ(uv + ty);
                float z2l = LinZ(uv - 2.0 * tx), z2r = LinZ(uv + 2.0 * tx);
                float z2d = LinZ(uv - 2.0 * ty), z2u = LinZ(uv + 2.0 * ty);
                float zdl = LinZ(uv - tx - ty), zdr = LinZ(uv + tx - ty);
                float zul = LinZ(uv - tx + ty), zur = LinZ(uv + tx + ty);

                // ① 실루엣 — 반경 1 검출 + 한 칸 팽창(깊이가 이어진 이웃에서만).
                float e0 = step(_EdgeK * z0, max(max(zl, zr), max(zd, zu)) - z0);
                float eR = step(_EdgeK * zr, max(max(z0, z2r), max(zur, zdr)) - zr);
                float eL = step(_EdgeK * zl, max(max(z0, z2l), max(zul, zdl)) - zl);
                float eU = step(_EdgeK * zu, max(max(z0, z2u), max(zul, zur)) - zu);
                float eD = step(_EdgeK * zd, max(max(z0, z2d), max(zdl, zdr)) - zd);
                float cL = 1.0 - step(_EdgeK * z0, abs(zl - z0));
                float cR = 1.0 - step(_EdgeK * z0, abs(zr - z0));
                float cU = 1.0 - step(_EdgeK * z0, abs(zu - z0));
                float cD = 1.0 - step(_EdgeK * z0, abs(zd - z0));
                float sil = max(e0, _EdgeDilate * max(max(eL * cL, eR * cR), max(eU * cU, eD * cD)));

                // 큰 계단 반경 2 억제용 — 탭거리로 나눈 화소당 기울기의 최댓값.
                float a1 = max(max(abs(zl - z0), abs(zr - z0)), max(abs(zd - z0), abs(zu - z0)));
                float ad = max(max(abs(zdl - z0), abs(zdr - z0)), max(abs(zul - z0), abs(zur - z0))) * _EdgeDiagF;
                float a2 = max(max(abs(z2l - z0), abs(z2r - z0)), max(abs(z2d - z0), abs(z2u - z0))) * _EdgeR2F;
                float amax = max(a1, max(ad, a2));

                float qc = 1.0 / z0, qL = 1.0 / zl, qR = 1.0 / zr, qD = 1.0 / zd, qU = 1.0 / zu;
                float q2L = 1.0 / z2l, q2R = 1.0 / z2r, q2D = 1.0 / z2d, q2U = 1.0 / z2u;
                float qDL = 1.0 / zdl, qDR = 1.0 / zdr, qUL = 1.0 / zul, qUR = 1.0 / zur;

                // 화면공간 → 시야평면 좌표. proj = (tan(fov/2)·aspect, tan(fov/2)) = 투영행렬 대각의 역수.
                float2 proj = float2(1.0 / UNITY_MATRIX_P._m00, 1.0 / UNITY_MATRIX_P._m11);
                float2 uv0 = (uv * 2.0 - 1.0) * proj;
                float2 duv = 2.0 * texel * proj;

                // ② 법선 불연속 — 위 12탭을 그대로 재활용한다(추가 페치 0).
                float3 n0 = PNrm(qc, (qR - qL) / (2.0 * duv.x), (qU - qD) / (2.0 * duv.y), uv0);
                float3 nR = PNrm(qR, (q2R - qc) / (2.0 * duv.x), (qUR - qDR) / (2.0 * duv.y), uv0 + float2(duv.x, 0.0));
                float3 nL = PNrm(qL, (qc - q2L) / (2.0 * duv.x), (qUL - qDL) / (2.0 * duv.y), uv0 - float2(duv.x, 0.0));
                float3 nU = PNrm(qU, (qUR - qUL) / (2.0 * duv.x), (q2U - qc) / (2.0 * duv.y), uv0 + float2(0.0, duv.y));
                float3 nD = PNrm(qD, (qDR - qDL) / (2.0 * duv.x), (qc - q2D) / (2.0 * duv.y), uv0 - float2(0.0, duv.y));
                float dmin = min(min(dot(n0, nL), dot(n0, nR)), min(dot(n0, nD), dot(n0, nU)));

                // ③ 곡률 — 법선이 나란한 작은 계단 전용. 이웃의 곡률선을 기준선으로 두고 이력으로 반대 날개를 살린다.
                float cx = abs(qL + qR - 2.0 * qc), cy = abs(qD + qU - 2.0 * qc);
                float crv0 = step(_EdgeCreaseK * qc, max(cx, cy));
                float crvL = step(_EdgeCreaseK * qL, max(abs(q2L + qc - 2.0 * qL), abs(qDL + qUL - 2.0 * qL)));
                float crvR = step(_EdgeCreaseK * qR, max(abs(qc + q2R - 2.0 * qR), abs(qDR + qUR - 2.0 * qR)));
                float crvU = step(_EdgeCreaseK * qU, max(abs(qUL + qUR - 2.0 * qU), abs(qc + q2U - 2.0 * qU)));
                float crvD = step(_EdgeCreaseK * qD, max(abs(qDL + qDR - 2.0 * qD), abs(q2D + qc - 2.0 * qD)));
                float crvNbr = max(max(crvL, crvR), max(crvU, crvD));
                float crvNear = max(crv0, crvNbr);
                float crvHy = max(crv0, _EdgeDilate * step(_EdgeCreaseK * qc * _EdgeCreaseHyst, max(cx, cy)) * crvNbr);
                // 법선항은 반경 1 안에 곡률선이 없는 자리에서만(비접촉 게이트) · 둘 다 큰 계단 안에서는 꺼진다.
                float crs = max(crvHy, step(_EdgeNormalK, 1.0 - dmin) * (1.0 - crvNear)) * (1.0 - step(_EdgeK * z0, amax));

                // ④ 파츠 ID 불연속(Core IdLine) — 깊이도 법선도 0 인 «같은 평면 파츠 경계» 전용.
                //    두께 규율은 실루엣과 같다: 팽창 off 면 키가 큰 쪽만(1px) · on 이면 양쪽(2px). 깊이가 이어진 이웃(cN)에만 건다.
                float idl = 0.0;
                if (_EdgeIdOn > 0.5)
                {
                    float k0 = IdKey(uv, z0);
                    float kl = IdKey(uv - tx, zl), kr = IdKey(uv + tx, zr), kd = IdKey(uv - ty, zd), ku = IdKey(uv + ty, zu);
                    float idOne = max(max(cL * step(0.5, k0 - kl), cR * step(0.5, k0 - kr)), max(cU * step(0.5, k0 - ku), cD * step(0.5, k0 - kd)));
                    float idTwo = max(max(cL * step(0.5, abs(k0 - kl)), cR * step(0.5, abs(k0 - kr))), max(cU * step(0.5, abs(k0 - ku)), cD * step(0.5, abs(k0 - kd))));
                    idl = lerp(idOne, idTwo, _EdgeDilate);
                }

                float edge = max(sil, max(crs, idl)) * step(z0, _EdgeMaxZ);
                return half4(_EdgeLineColor.rgb, edge);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
