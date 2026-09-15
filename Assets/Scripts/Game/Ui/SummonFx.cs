using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.CraftFx;
using Forge.Core.Ui;
using Forge.Game.Gallery;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T179 — 소환 결과 팝업의 연출 겹(정본 `.sr-canopy`(아치 + 빛발 3 + 스필) · `.sr-rays`(배경 광선) · `.sr-stars`(별)). 수치는 `Resources/SummonFxUi.json`.
    /// 정본은 CSS 그라디언트·마스크·blur 로 그리는데 UGUI 에는 그것이 없어 **한 장씩 굽는다**(<see cref="CraftFxPoly"/>·<see cref="AgePattern"/> 과 같은 길 · 결정 223 «맨 Graphic 은 안 칠해진다»).
    /// 층 사다리(정본 5692~5697): 광선 0 · 바닥 10 · 반사 12 · 별 15 · 천개 20 · 그리드 40 — 형제 순서로 지킨다.
    /// 바닥 반사(`.sr-reflect` · 3회차): 정본 `buildSummonReflection`(ui.js 771~790)은 done 에서 그리드를 **복제**해 이름·배지를 떼고 뒤집어(scaleY −1.22 · 위 변 고정) blur 4px + 세로 마스크로 깐다.
    /// UGUI 엔 blur 도 소프트 마스크도 없고, 정본 주석대로 «blur 가 약하면 거꾸로 놓인 아이콘 줄로 읽힌다» 라 겹 복제로는 못 옮긴다 — 그래서 <see cref="BakeReflection"/> 이 복제 그리드를 임시 월드 캔버스에 세워
    /// «1 픽셀 = blur 4px» 해상도의 RT 에 한 번 찍고(다운샘플이 곧 흐림) 상자 흐림·채도·밝기·마스크를 픽셀로 얹은 **한 장**을 뒤집힌 Image(피벗 아래 가운데 = 이음선 · scaleY −1.22) 에 건다. done **다음** 프레임에 한다(결정 516 과 같은 까닭).
    /// ⚠ 굽기는 <see cref="Build"/> 가 아니라 **첫 Update 에서** 한다(<see cref="Bake"/>) — 팝업을 연 프레임에 픽셀 루프를 얹으면 그 프레임이 연출 창(`sr_charge_ms + sr_tail_ms` = 390ms)을
    /// 넘겨 결과가 탭보다 먼저 끝난다(런 438·444 `PetUiTests.스킬_소환…` · 결정 516). 그 전까지 Image 는 꺼 둔다(스프라이트 없는 Image 는 흰 네모를 그린다).
    /// </summary>
    public sealed class SummonFx : MonoBehaviour
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        RectTransform canopy, rays, floor;
        // ---- 바닥 반사(3회차) ----
        RectTransform grid, reflect;
        Image reflectImg;
        Texture2D reflectTex;
        Sprite reflectSprite;
        readonly List<float> orbScales = new List<float>();
        float reflectAt = -1f;
        int doneFrame = -1;
        /// <summary>반사 무대(임시 월드 캔버스)를 세우는 자리 — 전장(원점 근처)·PetFaces 무대(0,−500,0)와 겹치지 않게 멀리.</summary>
        static readonly Vector3 ReflectAway = new Vector3(0f, -3000f, 0f);
        /// <summary>복제본에서 떼는 것(정본 ui.js 777: 이름·등급·배지 — 거울상 글자는 노이즈). `.sr-ray/.sr-beam/.sr-ghost/.sr-spark` 는 클론에 없다.</summary>
        static readonly string[] ReflectStrip = { "sr-name", "sr-sub", "sr-qty", "sr-dup", "sr-new" };
        CanvasGroup canopyGroup, starsGroup;
        Image raysImg;
        Sprite raysIdle, raysDone;
        readonly List<Image> rayBars = new List<Image>();
        readonly List<float> rayPeriod = new List<float>(), rayDelay = new List<float>();
        Image spill;
        readonly List<RectTransform> stars = new List<RectTransform>();
        readonly List<CanvasGroup> starGroups = new List<CanvasGroup>();
        readonly List<float> starDur = new List<float>(), starDelay = new List<float>();
        float t0, doneAt = -1f;
        bool done, baked;
        readonly List<Action> pending = new List<Action>();
        readonly List<Image> pendingImgs = new List<Image>();

        public RectTransform Canopy { get { return canopy; } }
        public RectTransform Rays { get { return rays; } }
        public int StarCount { get { return stars.Count; } }
        public int RayBarCount { get { return rayBars.Count; } }
        public bool Done { get { return done; } }
        public float StarsAlpha { get { return starsGroup != null ? starsGroup.alpha : 0f; } }
        /// <summary>스프라이트를 다 구웠는가 — Build 직후엔 false · 첫 Update 뒤 true.</summary>
        public bool Baked { get { return baked; } }
        /// <summary>바닥 반사(`.sr-reflect`) — done 다음 프레임 전엔 null.</summary>
        public RectTransform Reflect { get { return reflect; } }
        /// <summary>반사 그림이 찍혔는가(그래픽 장치가 없으면 상자만 서고 false).</summary>
        public bool ReflectBaked { get { return reflectImg != null && reflectImg.sprite != null; } }

        /// <summary>
        /// 무대(`stage`)판의 몸(`sr-body`)에 세 겹을 세운다. <paramref name="gridTop"/>·<paramref name="gridW"/> 는 그리드의 몸 안 자리(위에서 · 폭) · <paramref name="floor"/> 는 이미 선 소환진(그 위에 별·천개를 끼운다).
        /// </summary>
        public static SummonFx Build(RectTransform body, RectTransform floor, float gridTop, float gridW, bool one, bool compact)
        {
            SummonFx fx = body.gameObject.AddComponent<SummonFx>();
            fx.t0 = Time.unscaledTime;
            fx.floor = floor;
            float W = body.rect.width, H = body.rect.height, rem = PetSkillStyle.RemPx;
            var s = SummonFxStyle.Root;

            // ---- 배경 광선(z 0) — 몸 가운데 · 폭 190% 정사각 · 도입 수축 · 20s 회전 ----
            float rw = W * L("rays_w_f");
            fx.rays = UiKit.Box(body, "sr-rays");
            UiKit.Anchor(fx.rays, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rw, rw);
            fx.raysImg = fx.rays.gameObject.AddComponent<Image>();
            Image raysImg = fx.raysImg;
            fx.Defer(raysImg, () => { fx.raysIdle = BakeRays("sr-rays", "rays_mask", "rays_mask_a"); raysImg.sprite = fx.raysIdle; });   // done 마스크는 SetDone 에서
            fx.raysImg.raycastTarget = false;
            fx.raysImg.color = new Color(1f, 1f, 1f, L("rays_a"));
            fx.rays.SetAsFirstSibling();

            // ---- 별(z 15) — done 에서만 켠다 ----
            RectTransform starsRt = UiKit.Box(body, "sr-stars");
            UiKit.Fill(starsRt);
            fx.starsGroup = starsRt.gameObject.AddComponent<CanvasGroup>();
            fx.starsGroup.alpha = 0f;
            fx.starsGroup.blocksRaycasts = false;
            int n = Mathf.RoundToInt(L("stars_n")), topN = Mathf.RoundToInt(L("stars_top_n"));
            float cssPx = L("css_px");
            for (int i = 0; i < n; i++)
            {
                bool top = i < topN;
                float x = (L("star_x0") + (i * L("star_x_step")) % L("star_x_mod")) / 100f;
                float y = (top ? L("star_top_y0") + (i * L("star_top_y_step")) % L("star_top_y_mod") : L("star_bot_y0") + (i * L("star_bot_y_step")) % L("star_bot_y_mod")) / 100f;
                float sz = (L("star_s0_px") + (i * L("star_s_step")) % L("star_s_mod")) * cssPx;
                float box = sz * L("star_lobe_len_f");
                RectTransform st = UiKit.Box(starsRt, "star-" + i);
                UiKit.Anchor(st, new Vector2(x, 1f - y), new Vector2(0.5f, 0.5f), Vector2.zero, box, box);
                Image si = st.gameObject.AddComponent<Image>();
                string starName = "sr-star-" + Mathf.RoundToInt(sz);
                fx.Defer(si, () => si.sprite = BakeStar(starName, sz, box));
                si.raycastTarget = false;
                fx.stars.Add(st);
                fx.starGroups.Add(st.gameObject.AddComponent<CanvasGroup>());
                fx.starDur.Add(L("star_dur0_s") + (i % Mathf.RoundToInt(L("star_dur_mod"))) * L("star_dur_step_s"));
                fx.starDelay.Add(((i * L("star_d_step")) % L("star_d_mod_ms")) / 1000f);
            }
            starsRt.SetSiblingIndex(floor != null ? floor.GetSiblingIndex() + 1 : 1);

            // ---- 천개(z 20) — 그리드 위 밴드 · 아치 + 빛발 3 + 스필 ----
            string k = one ? "canopy_one_" : compact ? "canopy_compact_" : "canopy_";
            float cw = gridW * L(k + "w_f"), ch = cw / L(k + "aspect"), mb = L(k + "mb_rem") * rem;
            fx.canopy = UiKit.Box(body, "sr-canopy");
            UiKit.Place(fx.canopy, (W - cw) * 0.5f, gridTop + mb - ch, cw, ch);
            fx.canopyGroup = fx.canopy.gameObject.AddComponent<CanvasGroup>();
            fx.canopyGroup.alpha = 0f;
            fx.canopyGroup.blocksRaycasts = false;
            Image arch = UiKit.Box(fx.canopy, "arch").gameObject.AddComponent<Image>();
            UiKit.Fill(arch.rectTransform);
            string archName = "sr-arch-" + (one ? "one" : compact ? "compact" : "stage");
            fx.Defer(arch, () => arch.sprite = BakeArch(archName, cw, ch, rem));
            arch.raycastTarget = false;
            // 스필(b) — 아치 위로 넓게 새는 빛무리
            float sw = cw * L("spill_w_f"), sh = (compact ? L("spill_compact_h_rem") : L("spill_h_rem")) * rem;
            RectTransform spRt = UiKit.Box(fx.canopy, "spill");
            UiKit.Anchor(spRt, new Vector2(0.5f, L("spill_bottom_f")), new Vector2(0.5f, 0f), Vector2.zero, sw, sh);
            fx.spill = spRt.gameObject.AddComponent<Image>();
            Image spill = fx.spill;
            fx.Defer(spill, () => spill.sprite = BakeSpill("sr-spill"));
            fx.spill.raycastTarget = false;
            // 빛발 3 — 아치에서 위로
            float bw = L("ray_w_rem") * rem;
            float hMid = (compact ? L("ray_compact_h_rem") : L("ray_h_rem")) * rem, hSide = (compact ? L("ray_compact_side_h_rem") : L("ray_side_h_rem")) * rem;
            float dx = (compact ? L("ray_compact_side_dx_rem") : L("ray_side_dx_rem")) * rem;
            float[] xs = { -dx, 0f, dx }; float[] hs = { hSide, hMid, hSide };
            float[] per = { L("ray_side1_period_s"), L("ray_period_s"), L("ray_side3_period_s") };
            float[] del = { 0f, 0f, L("ray_side3_delay_s") };
            for (int i = 0; i < 3; i++)
            {
                RectTransform r = UiKit.Box(fx.canopy, "ray-" + (i + 1));
                UiKit.Anchor(r, new Vector2(0.5f, L("ray_bottom_f")), new Vector2(0.5f, 0f), new Vector2(xs[i], 0f), bw, hs[i]);
                Image ri = r.gameObject.AddComponent<Image>();
                fx.Defer(ri, () => ri.sprite = BakeRayBar("sr-ray"));   // 셋이 한 장을 나눠 쓴다(cache)
                ri.raycastTarget = false;
                fx.rayBars.Add(ri); fx.rayPeriod.Add(per[i]); fx.rayDelay.Add(del[i]);
            }
            fx.canopy.SetSiblingIndex(starsRt.GetSiblingIndex() + 1);
            return fx;
        }

        /// <summary>결과가 안착한 뒤(정본 `#summon-result-modal.done`): 별을 켜고 광선을 done 마스크·느린 회전·호흡으로.</summary>
        public void SetDone()
        {
            if (done) return;
            done = true;
            doneAt = Time.unscaledTime;
            doneFrame = Time.frameCount;
            if (raysDone == null) raysDone = BakeRays("sr-rays-done", "rays_done_mask", "rays_done_mask_a");
            if (raysImg != null) raysImg.sprite = raysDone;
        }

        /// <summary>미룬 굽기 하나 — 스프라이트가 올 때까지 Image 를 꺼 둔다.</summary>
        void Defer(Image img, Action bake)
        {
            img.enabled = false;
            pendingImgs.Add(img);
            pending.Add(bake);
        }

        /// <summary>미룬 굽기를 전부 한다(첫 Update · 테스트가 앞당길 수도 있다). 두 번 불러도 한 번만.</summary>
        public void Bake()
        {
            if (baked) return;
            baked = true;
            for (int i = 0; i < pending.Count; i++) pending[i]();
            for (int i = 0; i < pendingImgs.Count; i++) if (pendingImgs[i] != null) pendingImgs[i].enabled = true;
            pending.Clear(); pendingImgs.Clear();
        }

        static float L(string key) { return SummonFxStyle.L(key); }

        static float Veil(float t, float period, float delay, float lo, float hi)
        {
            float ph = Mathf.Repeat((t - delay) / Mathf.Max(0.01f, period), 1f);
            float k = 0.5f - 0.5f * Mathf.Cos(ph * Mathf.PI * 2f);   // ease-in-out 왕복
            return Mathf.Lerp(lo, hi, k);
        }

        void Update()
        {
            if (!baked) Bake();
            // 바닥 반사 — 정본 finishSummonResult → buildSummonReflection. done 을 받은 **다음** 프레임에 한 번(연 프레임·탭 프레임을 안 늘린다)
            if (done && reflect == null && grid != null && Time.frameCount > doneFrame) BakeReflection();
            float t = Time.unscaledTime - t0;
            // 천개 도입 — srcanopy .45s(.05s 뒤) scale(.8)·translateY(-.5rem) → 1
            if (canopyGroup != null)
            {
                float k = Mathf.Clamp01((t - L("canopy_in_delay_ms") / 1000f) / (L("canopy_in_ms") / 1000f));
                float e = 1f - Mathf.Pow(1f - k, 3f);
                canopyGroup.alpha = e;
                canopy.localScale = Vector3.one * Mathf.Lerp(L("canopy_in_scale"), 1f, e);
            }
            // 빛발·스필 호흡 — srveil α .42↔.85 · scaleY .84↔1.12
            for (int i = 0; i < rayBars.Count; i++)
            {
                float a = Veil(t, rayPeriod[i], rayDelay[i], L("veil_a_lo"), L("veil_a_hi"));
                float sy = Veil(t, rayPeriod[i], rayDelay[i], L("veil_sy_lo"), L("veil_sy_hi"));
                rayBars[i].color = new Color(1f, 1f, 1f, a);
                rayBars[i].rectTransform.localScale = new Vector3(1f, sy, 1f);
            }
            if (spill != null) spill.color = new Color(1f, 1f, 1f, Veil(t, L("spill_period_s"), 0f, L("veil_a_lo"), L("veil_a_hi")));
            // 광선 — 도입 수축(srintro .24s 1.3→1 · α 0→) + 회전(20s · done 26s) + done 호흡
            if (rays != null)
            {
                float ik = Mathf.Clamp01(t / (L("rays_intro_ms") / 1000f));
                float ie = ik * ik;   // ease-in
                float spin = done ? L("rays_done_spin_s") : L("rays_spin_s");
                rays.localRotation = Quaternion.Euler(0f, 0f, -360f * Mathf.Repeat(t / spin, 1f));
                rays.localScale = Vector3.one * Mathf.Lerp(L("rays_intro_scale"), 1f, ie);
                float a = done ? Veil(Time.unscaledTime - doneAt, L("rays_done_breath_s"), 0f, L("rays_done_a_lo"), L("rays_done_a_hi")) : L("rays_a") * ie;
                raysImg.color = new Color(1f, 1f, 1f, a);
            }
            // 반사 — srreflect .5s ease-out 로 α 0 → .88
            if (reflectImg != null && reflectImg.sprite != null)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - reflectAt) / (L("reflect_in_ms") / 1000f));
                float e = 1f - (1f - k) * (1f - k);
                reflectImg.color = new Color(1f, 1f, 1f, L("reflect_a") * e);
            }
            // 별 — done 뒤 .6s 로 켜고 각자 호흡(srstar α .9↔1 · scale .97↔1.2 · dur·delay 는 표 수열)
            if (starsGroup != null && done)
            {
                float dt = Time.unscaledTime - doneAt;
                starsGroup.alpha = Mathf.Clamp01(dt / (L("stars_fade_ms") / 1000f));
                for (int i = 0; i < stars.Count; i++)
                {
                    float sc = Veil(dt, starDur[i], starDelay[i], L("star_s_lo"), L("star_s_hi"));
                    stars[i].localScale = Vector3.one * sc;
                    starGroups[i].alpha = Veil(dt, starDur[i], starDelay[i], L("star_a_lo"), 1f);
                }
            }
        }

        // ================= 바닥 반사(3회차) =================

        /// <summary>반사의 원본 — 그리드와 셀별 구체 배율(정본 `--sz` · heroic 확대는 복제본에서 뗀다 · ui.js 776). 셀을 다 세운 뒤 한 번 부른다.</summary>
        public void SetReflectSource(RectTransform gridRt, IList<float> cellOrbScales)
        {
            grid = gridRt;
            orbScales.Clear();
            if (cellOrbScales != null) orbScales.AddRange(cellOrbScales);
        }

        /// <summary>
        /// `.sr-reflect` 를 세운다(정본 6957~6978 · ui.js 771~790): 몸 폭 · 이음선 = 그리드 아래 − 8px(위로 겹침) · 높이 = 그리드 높이 · scaleY(−1.22)(정본 transform-origin 55% = 위 변 고정 · 아래로만 자람) · z 12(바닥 다음 형제).
        /// ⚠ 피벗은 **아래 가운데**(0.5, 0)를 이음선에 둔다 — 음수 배율은 피벗을 중심으로 거울을 대는 것이라, 피벗을 위에 두면 상자가 **위로** 뒤집혀 천개 위에 뜬다(런 459·463: 위 변이 그리드 아래보다 749px 위). 피벗이 아래면 원본의 위(구체)가 이음선 아래 1.22배 자리로 간다 = 정본.
        /// 그림은 <see cref="BakeReflectSprite"/> 한 장 — 그래픽 장치가 없으면 상자만 서고 Image 는 꺼진 채다. 두 번 불러도 한 번만.
        /// </summary>
        public void BakeReflection()
        {
            if (reflect != null || grid == null) return;
            RectTransform body = (RectTransform)transform;
            float cssPx = L("css_px");
            float gw = grid.rect.width, gh = grid.rect.height;
            float gridTop = -grid.anchoredPosition.y;   // UiKit.Place: 위에서 잰 자리
            reflect = UiKit.Box(body, "sr-reflect");
            reflect.anchorMin = reflect.anchorMax = new Vector2(0f, 1f);
            reflect.pivot = new Vector2(0.5f, 0f);   // 아래 가운데 = 이음선(위 참조)
            reflect.sizeDelta = new Vector2(body.rect.width, gh);
            reflect.anchoredPosition = new Vector2(body.rect.width * 0.5f, -(gridTop + gh - L("reflect_top_px") * cssPx));
            reflect.localScale = new Vector3(1f, -L("reflect_sy"), 1f);
            reflect.SetSiblingIndex(floor != null ? floor.GetSiblingIndex() + 1 : 1);
            reflectImg = reflect.gameObject.AddComponent<Image>();
            reflectImg.raycastTarget = false;
            reflectImg.color = new Color(1f, 1f, 1f, 0f);
            reflectImg.enabled = false;
            reflectSprite = BakeReflectSprite(gw, gh);
            if (reflectSprite == null) return;
            reflectImg.sprite = reflectSprite;
            reflectImg.enabled = true;
            reflectAt = Time.unscaledTime;
        }

        /// <summary>
        /// 복제 그리드를 임시 월드 캔버스(멀리)에 세워 정사영 카메라로 «1 픽셀 = blur 4px» 해상도 RT 에 찍고, 읽은 픽셀에 상자 흐림(반지름 `reflect_blur_r`)·채도 .9·밝기 1.1·세로 마스크(원본 좌표 · 위 1 → 56% .6 → 96% 0)를 얹는다.
        /// 뒤집기는 그림이 아니라 <see cref="Reflect"/> 의 배율이 한다(정본도 mask·filter 뒤에 transform). 실패하면 경고 한 줄 · null(반사 없이 간다 — PetFaces 와 같은 태도).
        /// </summary>
        Sprite BakeReflectSprite(float gw, float gh)
        {
            if (!GallerySheet.GraphicsAvailable || gw < 1f || gh < 1f) return null;
            float blurPx = Mathf.Max(1f, L("reflect_blur_px") * L("css_px"));   // 캔버스 px
            int W = Mathf.Clamp(Mathf.RoundToInt(gw / blurPx), 8, 512), H = Mathf.Clamp(Mathf.RoundToInt(gh / blurPx), 4, 512);
            GameObject stageGo = new GameObject("sr-reflect-stage", typeof(RectTransform), typeof(Canvas));
            stageGo.layer = gameObject.layer;
            GameObject camGo = new GameObject("sr-reflect-cam");
            RenderTexture rt = null, prev = RenderTexture.active;
            Texture2D tex = null;
            try
            {
                Canvas sc = stageGo.GetComponent<Canvas>();
                sc.renderMode = RenderMode.WorldSpace;
                RectTransform srt = (RectTransform)stageGo.transform;
                srt.position = ReflectAway;
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(gw, gh);
                GameObject clone = Instantiate(grid.gameObject, srt, false);
                clone.name = "sr-grid";
                RectTransform crt = (RectTransform)clone.transform;
                UiKit.Fill(crt);
                NormalizeReflectClone(crt);
                SetLayerDeep(clone.transform, stageGo.layer);
                Camera cam = camGo.AddComponent<Camera>();
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                cam.enabled = false;
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.ResetProjectionMatrix();
                cam.orthographic = true;
                cam.orthographicSize = gh * 0.5f;
                cam.aspect = gw / gh;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.useOcclusionCulling = false;
                cam.cullingMask = 1 << stageGo.layer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.transform.position = ReflectAway + new Vector3(0f, 0f, -10f);
                cam.transform.rotation = Quaternion.identity;
                Canvas.ForceUpdateCanvases();
                rt = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
                RenderTexture.active = rt;
                tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.name = "sr-reflect";
                tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
                tex.ReadPixels(new Rect(0f, 0f, W, H), 0, 0);
                ReflectPixels(tex);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SummonFx] 바닥 반사 굽기 실패 — 반사 없이: " + ex.Message);
                if (tex != null) Destroy(tex);
                return null;
            }
            finally
            {
                RenderTexture.active = prev;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                stageGo.SetActive(false);
                Destroy(camGo);
                Destroy(stageGo);
            }
            reflectTex = tex;
            Sprite sp = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sp.name = "sr-reflect";
            return sp;
        }

        /// <summary>읽은 RT 픽셀에 상자 흐림(사전곱 평균) · 채도 · 밝기 · 세로 마스크(원본 좌표 · 위 = 0%). 표 조회는 루프 밖(결정 516).</summary>
        void ReflectPixels(Texture2D tex)
        {
            int W = tex.width, H = tex.height;
            Color32[] src = tex.GetPixels32();
            var dst = new Color32[W * H];
            int r = Mathf.Max(0, Mathf.RoundToInt(L("reflect_blur_r")));
            float sat = L("reflect_saturate"), bri = L("reflect_brightness");
            float[] ms = SummonFxStyle.Arr("reflect_mask"), ma = SummonFxStyle.Arr("reflect_mask_a");
            for (int y = 0; y < H; y++)
            {
                float fromTop = 1f - (y + 0.5f) / H;
                float mask = Stops(ms, ma, fromTop);
                for (int x = 0; x < W; x++)
                {
                    float sr = 0f, sg = 0f, sb = 0f, sa = 0f; int n = 0;
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int yy = y + dy; if (yy < 0 || yy >= H) continue;
                        for (int dx = -r; dx <= r; dx++)
                        {
                            int xx = x + dx; if (xx < 0 || xx >= W) continue;
                            Color32 c = src[yy * W + xx];
                            float a = c.a / 255f;
                            sr += c.r / 255f * a; sg += c.g / 255f * a; sb += c.b / 255f * a; sa += a; n++;
                        }
                    }
                    float outA = n > 0 ? sa / n : 0f;
                    float rr = 0f, gg = 0f, bb = 0f;
                    if (sa > 1e-5f) { rr = sr / sa; gg = sg / sa; bb = sb / sa; }
                    float gray = 0.299f * rr + 0.587f * gg + 0.114f * bb;
                    rr = Mathf.Clamp01(Mathf.Lerp(gray, rr, sat) * bri);
                    gg = Mathf.Clamp01(Mathf.Lerp(gray, gg, sat) * bri);
                    bb = Mathf.Clamp01(Mathf.Lerp(gray, bb, sat) * bri);
                    dst[y * W + x] = new Color(rr, gg, bb, outA * mask);
                }
            }
            tex.SetPixels32(dst);
            tex.Apply(false, false);   // 읽을 수 있게 둔다(자가 마스크·구체를 본다)
        }

        /// <summary>정본 ui.js 775~777 + CSS 6977~6978: 셀 전부 on(α 1 · transform none) · heroic 뗌(구체 배율 = --sz 만) · 이름·등급·배지 제거. Destroy 는 프레임 끝이라 **끈다**(이 프레임에 찍는다).</summary>
        void NormalizeReflectClone(RectTransform gridClone)
        {
            for (int i = 0; i < gridClone.childCount; i++)
            {
                Transform cell = gridClone.GetChild(i);
                cell.localScale = Vector3.one;
                CanvasGroup cg = cell.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
                Transform wrap = cell.Find("sr-orbwrap");
                if (wrap != null) wrap.localScale = Vector3.one * (i < orbScales.Count ? orbScales[i] : 1f);
                Transform[] all = cell.GetComponentsInChildren<Transform>(true);
                for (int k = 0; k < all.Length; k++)
                    if (Array.IndexOf(ReflectStrip, all[k].name) >= 0) all[k].gameObject.SetActive(false);
            }
        }

        static void SetLayerDeep(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerDeep(t.GetChild(i), layer);
        }

        /// <summary>CSS 그라디언트 스톱(xs 오름차순 · ys 값) — 밖은 끝값.</summary>
        static float Stops(float[] xs, float[] ys, float x)
        {
            if (xs == null || xs.Length == 0) return 0f;
            if (x <= xs[0]) return ys[0];
            for (int i = 0; i + 1 < xs.Length; i++)
                if (x < xs[i + 1]) return Mathf.Lerp(ys[i], ys[i + 1], (x - xs[i]) / Mathf.Max(1e-6f, xs[i + 1] - xs[i]));
            return ys[ys.Length - 1];
        }

        void OnDestroy()
        {
            if (reflectSprite != null) Destroy(reflectSprite);
            if (reflectTex != null) Destroy(reflectTex);
        }

        // ================= 굽기 =================
        static Sprite Finish(string name, Texture2D tex, Color32[] px)
        {
            tex.SetPixels32(px); tex.Apply(false, true);
            Sprite sp = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sp.name = name;
            cache[name] = sp;
            return sp;
        }

        static Texture2D NewTex(string name, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name; tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static float Ramp(float x, float x0, float y0, float x1, float y1)
        {
            if (x <= x0) return y0; if (x >= x1) return y1;
            return Mathf.Lerp(y0, y1, (x - x0) / (x1 - x0));
        }

        /// <summary>아치 — 타원 테(.08rem) + 안쪽 방사 띠(68→88→100%) + 룬 눈금(1.1°/9° · r 86~99%) · 세로 마스크(위에서 42% 까지 1 → 78% 0 · 눈금은 40→62%). 정본 5855~5878.</summary>
        public static Sprite BakeArch(string name, float w, float h, float rem)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int W = Mathf.RoundToInt(L("bake_px")), H = Mathf.Max(8, Mathf.RoundToInt(W * h / Mathf.Max(1f, w)));
            Color line = SummonFxStyle.C("line"), fill = SummonFxStyle.C("fill");
            float strokeN = (L("arch_stroke_rem") * rem) / (w * 0.5f);   // 반지름 단위 두께
            // 표 값은 루프 밖에서 한 번만 읽는다(픽셀마다 사전을 뒤지면 굽기가 수백 ms — 결정 516)
            float mSolid = L("arch_mask_solid_f"), mEnd = L("arch_mask_end_f"), tmSolid = L("tick_mask_solid_f"), tmEnd = L("tick_mask_end_f");
            float fr0 = L("arch_fill_r0"), fr1 = L("arch_fill_r1"), tEvery = L("tick_every_deg"), tDeg = L("tick_deg"), tr0 = L("tick_r0"), tr1 = L("tick_r1"), tr2 = L("tick_r2");
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;                 // 0 = 아래 · 1 = 위
                float fromTop = 1f - v;
                float mask = Ramp(fromTop, mSolid, 1f, mEnd, 0f);
                float tmask = Ramp(fromTop, tmSolid, 1f, tmEnd, 0f);
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f, vv = v * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + vv * vv);
                    float ring = 1f - Mathf.Clamp01((Mathf.Abs(r - 1f + strokeN * 0.5f) - strokeN * 0.5f) / (1.5f / W * 2f));
                    float band = r < fr0 ? 0f : r < fr1 ? Ramp(r, fr0, 0f, fr1, 1f) : Ramp(r, fr1, 1f, 1f, 0f);
                    bool tick = r >= tr0 && r <= tr2 && Mathf.Repeat(Mathf.Repeat(Mathf.Atan2(vv, u) * Mathf.Rad2Deg, 360f), tEvery) < tDeg;
                    float tickA = tick ? (r < tr1 ? Ramp(r, tr0, 0f, tr1, 1f) : 1f) * tmask : 0f;
                    float a = Mathf.Max(ring * line.a * mask, Mathf.Max(band * fill.a * mask, tickA * line.a));
                    Color c = ring * mask > 0.01f || tickA > 0.01f ? line : fill;
                    px[y * W + x] = new Color(c.r, c.g, c.b, a);
                }
            }
            return Finish(name, NewTex(name, W, H), px);
        }

        /// <summary>빛발 한 가닥 — 세로 그라디언트(0% .5 · 46% .34 · 80% .14 · 100% 0) 을 둥근 막대(border-radius 50%) 에 · 가장자리 blur 2.2px 는 알파 경사로. 정본 5880~5892.</summary>
        public static Sprite BakeRayBar(string name)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int W = 32, H = 256;
            Color c = SummonFxStyle.C("ray");
            float a0 = L("ray_a0"), a46 = L("ray_a46"), a80 = L("ray_a80");
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;   // 0 = 밑동
                float ga = v < 0.46f ? Ramp(v, 0f, a0, 0.46f, a46) : v < 0.8f ? Ramp(v, 0.46f, a46, 0.8f, a80) : Ramp(v, 0.8f, a80, 1f, 0f);
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f, vv = v * 2f - 1f;
                    float d = u * u + vv * vv;                          // 타원(둥근 막대) 안 = 1
                    float edge = 1f - Mathf.Clamp01((d - 0.72f) / 0.28f);   // blur — 가장자리를 부드럽게
                    px[y * W + x] = new Color(c.r, c.g, c.b, ga * edge);
                }
            }
            return Finish(name, NewTex(name, W, H), px);
        }

        /// <summary>스필 — 바닥 가운데의 타원 방사(58%×100%) .5→.34@42%→0 · 세로 마스크(아래 44% 1 → 74% .55 → 100% 0). 정본 5894~5903.</summary>
        public static Sprite BakeSpill(string name)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int W = 256, H = 128;
            Color c = SummonFxStyle.C("spill");
            float mMid = L("spill_mask_mid_f"), mSolid = L("spill_mask_solid_f"), mMidA = L("spill_mask_mid_a"), rx = L("spill_rx_f"), sa0 = L("spill_a0"), sa42 = L("spill_a42");
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;   // 0 = 아래
                float mask = v < mMid ? Ramp(v, mSolid, 1f, mMid, mMidA) : Ramp(v, mMid, mMidA, 1f, 0f);
                for (int x = 0; x < W; x++)
                {
                    float u = ((x + 0.5f) / W * 2f - 1f) / rx;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = r < 0.42f ? Ramp(r, 0f, sa0, 0.42f, sa42) : Ramp(r, 0.42f, sa42, 1f, 0f);
                    px[y * W + x] = new Color(c.r, c.g, c.b, a * mask);
                }
            }
            return Finish(name, NewTex(name, W, H), px);
        }

        /// <summary>배경 광선 — conic 스포크 두 겹(.7°/9° · 2.6°/31°) × 방사 마스크(표 스톱). 정본 5931~5945 · done 7152~7160.</summary>
        public static Sprite BakeRays(string name, string maskKey, string maskAKey)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int S = Mathf.RoundToInt(L("bake_px"));
            Color c1 = SummonFxStyle.C("rays1"), c2 = SummonFxStyle.C("rays2");
            float[] ms = SummonFxStyle.Arr(maskKey), ma = SummonFxStyle.Arr(maskAKey);
            float s1Every = L("rays_spoke1_every_deg"), s1Deg = L("rays_spoke1_deg"), s2Every = L("rays_spoke2_every_deg"), s2Deg = L("rays_spoke2_deg");
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S * 2f - 1f, v = (y + 0.5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float mask = 0f;
                    for (int i = 0; i + 1 < ms.Length; i++) if (r >= ms[i] && r < ms[i + 1]) { mask = Mathf.Lerp(ma[i], ma[i + 1], (r - ms[i]) / (ms[i + 1] - ms[i])); break; }
                    if (r < ms[0]) mask = ma[0];
                    float deg = Mathf.Repeat(Mathf.Atan2(v, u) * Mathf.Rad2Deg, 360f);
                    float a1 = Mathf.Repeat(deg, s1Every) < s1Deg ? c1.a : 0f;
                    float a2 = Mathf.Repeat(deg, s2Every) < s2Deg ? c2.a : 0f;
                    Color c = a2 > a1 ? c2 : c1;
                    px[y * S + x] = new Color(c.r, c.g, c.b, Mathf.Max(a1, a2) * mask);
                }
            return Finish(name, NewTex(name, S, S), px);
        }

        /// <summary>별 하나 — 방사 점(#fff → 38% (210,230,255,.85) → 74% 0) + 십자 로브(2.6px × 3.1s · 가운데 .95). 정본 5970~5984.</summary>
        /// <summary>주역 와이프 한 장 — 정본 `.sr-wipe`(style.css 6184~6190)의 방사 그라디언트 그대로:
        /// 흰 0%% → **등급색** 24%% → 흰 .34 52%% → 투명 82%%. 등급색이 들어가므로 등급마다 한 장씩 굽는다(최대 여섯).
        /// 가운데는 정본이 `--fx`/`--fy` 로 옮기지만 여기서는 **가운데로 굽고 자리는 RectTransform 이 잡는다**
        /// (스프라이트를 자리마다 다시 구우면 장수가 셀 수만큼 는다).</summary>
        public static Sprite BakeWipe(string name, Color rc)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int W = 256, H = 256;
            float s0 = L("wipe_stop0_f"), s1 = L("wipe_stop1_f"), s2 = L("wipe_stop2_f"), s3 = L("wipe_stop3_f");
            float a0 = L("wipe_a0"), a1 = L("wipe_a1"), a2 = L("wipe_a2"), a3 = L("wipe_a3");
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H * 2f - 1f;
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);          // 0 = 가운데 · 1 = 변의 한가운데
                    float a;
                    Color c;
                    if (r <= s1) { a = Ramp(r, s0, a0, s1, a1); c = Color.Lerp(Color.white, rc, s1 <= s0 ? 1f : Mathf.Clamp01((r - s0) / (s1 - s0))); }
                    else if (r <= s2) { a = Ramp(r, s1, a1, s2, a2); c = Color.Lerp(rc, Color.white, Mathf.Clamp01((r - s1) / Mathf.Max(1e-4f, s2 - s1))); }
                    else { a = Ramp(r, s2, a2, s3, a3); c = Color.white; }
                    if (r > s3) a = 0f;
                    px[y * W + x] = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
                }
            }
            return Finish(name, NewTex(name, W, H), px);
        }

        /// <summary>
        /// 충전 비네트(정본 `.sr-wrap::before` 5744) — `radial-gradient(82% 51% at 50% 44%, rgba(0,0,0,0) 12%, rgba(0,0,0,.90) 100%)` 한 장.
        /// 정본이 못 박은 대로 **그라디언트는 고정**이고 움직이는 것은 이 판의 불투명도·배율뿐이다(배경을 키프레임으로 만들면 화면에서 계단이 된다).
        /// </summary>
        public static Sprite BakeVig(string name)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            SummonChargeSpec sp = SummonFxStyle.Charge;
            int N = Mathf.Max(8, Mathf.RoundToInt((float)sp.VigBakePx));
            float rx = (float)sp.VigRxF, ry = (float)sp.VigRyF, cy = (float)sp.VigCyF;
            float inner = (float)sp.VigInnerF, outA = (float)sp.VigOuterA;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                // CSS 는 위에서 아래로 — 구운 판은 아래에서 위로(UGUI) 라 y 를 뒤집는다.
                float fy = 1f - (y + 0.5f) / N;
                for (int x = 0; x < N; x++)
                {
                    float fx2 = (x + 0.5f) / N;
                    float u = (fx2 - 0.5f) / rx, v = (fy - cy) / ry;
                    float r = Mathf.Sqrt(u * u + v * v);           // 1 = 그라디언트의 바깥 끝
                    float a = r <= inner ? 0f : Mathf.Clamp01((r - inner) / Mathf.Max(1e-4f, 1f - inner)) * outA;
                    px[y * N + x] = new Color(0f, 0f, 0f, a);
                }
            }
            return Finish(name, NewTex(name, N, N), px);
        }


        /// <summary>
        /// 소환진의 룬 눈금 띠(정본 `.sr-floor::after` 5822~5830) — 9°마다 1.1° 짜리 선을 **링 바로 위 좁은 띠**에만 얹는다.
        ///
        /// 정본은 원뿔 그라디언트에 방사 마스크를 씌운다. 여기서는 굽는 판 한 장에 둘을 한꺼번에 푼다 —
        /// 화소마다 ⓐ 타원 좌표의 각도로 «눈금 안인가» 를 보고 ⓑ 같은 좌표의 반지름으로 마스크(87→93→99→100%)를 곱한다.
        /// 판은 소환진과 같은 비율(타원)로 굽는다 — 원판을 늘려 쓰면 눈금이 옆으로 퍼져 굵기가 각도마다 달라진다.
        ///
        /// ⚠ 정본 주석이 못 박은 것: 이 띠를 **넓히거나 돌리지 말 것**(«긁힌 자국» · «타원 위를 도는 붓질»). 움직임은 호흡뿐이다.
        /// 색·알파는 부르는 쪽이 준다(`.done` 에서 등급 파생색으로 승격한다 · ui.js 550).
        /// </summary>
        public static Sprite BakeFloorTicks(string name, float w, float h)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int W = Mathf.Max(16, Mathf.RoundToInt(L("bake_px") * Mathf.Max(1f, w / Mathf.Max(1f, h))));
            int H = Mathf.Max(16, Mathf.RoundToInt(L("bake_px")));
            float every = L("floor_tick_every_deg"), wide = L("floor_tick_deg");
            float m0 = L("floor_tick_mask0"), m1 = L("floor_tick_mask1"), m2 = L("floor_tick_mask2"), m3 = L("floor_tick_mask3");
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H * 2f - 1f;
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);                  // 타원 좌표라 1 이 곧 가장자리다
                    float mask = r <= m0 ? 0f
                        : r < m1 ? Ramp(r, m0, 0f, m1, 1f)
                        : r <= m2 ? 1f
                        : Ramp(r, m2, 1f, m3, 0f);
                    if (r > m3) mask = 0f;
                    float deg = Mathf.Repeat(Mathf.Atan2(v, u) * Mathf.Rad2Deg, 360f);
                    float a = Mathf.Repeat(deg, every) < wide ? mask : 0f;
                    px[y * W + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            }
            return Finish(name, NewTex(name, W, H), px);
        }


        /// <summary>
        /// 주역 충격파 링(정본 `#summon-result-modal.hero .sr-cell.heroic::after` · style.css 6754~6760).
        /// `radial-gradient(closest-side, 투명 58%, 등급색 74%, 흰 .85 82%, 투명 94%)` — 가운데가 빈 **고리**다.
        /// 정본은 `mix-blend-mode: screen`(가산)이라 거는 쪽이 가산 재질을 준다(와이프와 같은 길).
        /// </summary>
        public static Sprite BakeHeroRing(string name, Color rc)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int N = Mathf.Max(16, Mathf.RoundToInt(L("bake_px")));
            float s0 = HL("heroring_stop0"), s1 = HL("heroring_stop1"), s2 = HL("heroring_stop2"), s3 = HL("heroring_stop3");
            float wa = HL("heroring_white_a");
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                float v = (y + 0.5f) / N * 2f - 1f;
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a; Color c;
                    if (r <= s0) { a = 0f; c = rc; }
                    else if (r <= s1) { a = Ramp(r, s0, 0f, s1, 1f); c = rc; }
                    else if (r <= s2) { a = Ramp(r, s1, 1f, s2, wa); c = Color.Lerp(rc, Color.white, Ramp(r, s1, 0f, s2, 1f)); }
                    else { a = Ramp(r, s2, wa, s3, 0f); c = Color.white; }
                    if (r > s3) a = 0f;
                    px[y * N + x] = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
                }
            }
            return Finish(name, NewTex(name, N, N), px);
        }

        /// <summary>표의 `hero` 절 수치 하나(그 절은 `layout` 밖에 있다).</summary>
        static float HL(string key) { return (float)J.Num(J.Require(J.Obj(SummonFxStyle.Root["hero"]), key)); }


        /// <summary>
        /// 주역 광창(정본 `.sr-cell.heroic .sr-beam` · style.css 6740~6746) — **십자 광선**.
        /// 가로는 흰 선, 세로는 등급 하이라이트(`--rc-lite`) 선이고 둘 다 47%→50%→53% 로 좁다.
        /// 거기에 `radial-gradient(closest-side, 검정 0%, rgba(0,0,0,.5) 42%, 투명 76%)` 마스크를 씌워 가운데만 남긴다.
        /// 두 선은 정본에서 배경 두 겹이 겹치는 것이라 **더해서** 굽는다(가산 혼합은 거는 쪽이 준다).
        /// </summary>
        public static Sprite BakeBeam(string name, Color lite)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int N = Mathf.Max(16, Mathf.RoundToInt(L("bake_px")));
            float l0 = HL("beam_line0"), l1 = HL("beam_line1"), l2 = HL("beam_line2");
            float m0 = HL("beam_mask0"), m1 = HL("beam_mask1"), m1a = HL("beam_mask1_a"), m2 = HL("beam_mask2");
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                float fy = (y + 0.5f) / N;
                for (int x = 0; x < N; x++)
                {
                    float fx = (x + 0.5f) / N;
                    // 가로 선(세로 좌표가 가운데일 때 진하다) · 세로 선(가로 좌표가 가운데일 때)
                    float hA = Band(fy, l0, l1, l2), vA = Band(fx, l0, l1, l2);
                    float u = fx * 2f - 1f, v = fy * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float mask = r <= m0 ? 1f : r < m1 ? Ramp(r, m0, 1f, m1, m1a) : Ramp(r, m1, m1a, m2, 0f);
                    if (r > m2) mask = 0f;
                    // 두 선을 더한다(정본은 배경 두 겹 · 겹치는 가운데가 가장 밝다)
                    float a = Mathf.Clamp01(hA + vA) * mask;
                    Color c = hA + vA <= 0.0001f ? Color.white : Color.Lerp(lite, Color.white, hA / Mathf.Max(0.0001f, hA + vA));
                    px[y * N + x] = new Color(c.r, c.g, c.b, a);
                }
            }
            return Finish(name, NewTex(name, N, N), px);
        }

        /// <summary>
        /// 셀별 광원 재점화 판(정본 `.sr-relight` 6377~6383) —
        /// `radial-gradient(closest-side, var(--rc-lite) 0%, var(--rc) 30%, rgba(0,0,0,0) 70%)` 한 장.
        ///
        /// CSS 가 투명으로 잇는 마지막 구간은 **미리 곱한 알파**로 보간한다 — 색은 등급색 그대로 두고 알파만 떨어뜨린다
        /// (색을 검정으로 끌면 가산 혼합에서 «까맣게 죽은 테» 가 한 겹 생긴다). 정사각 판이라 `closest-side` = 반지름 = 반 변.
        /// 정본 `filter: blur(3px)` 는 따로 안 먹인다 — 이 감쇠가 이미 매끈해 3px 가우시안이 프로필을 1% 미만으로 바꾼다(표 `_` 참조).
        /// </summary>
        public static Sprite BakeRelight(string name, Color rc, Color lite)
        {
            SummonRelightSpec sp = SummonFxStyle.Relight;
            return BakeRadial(name, rc, lite, (float)sp.StopLite, (float)sp.StopRc, (float)sp.StopOut);
        }

        /// <summary>
        /// 비행 잔상 판(정본 `.sr-ghost` 6448~6452) — 같은 `radial-gradient(closest-side …)` 문법에 정지점만 다르다(0 / 52% / 74%).
        /// 정본 `filter: blur(6px)` 는 따로 안 먹인다(재점화와 같은 까닭 — 감쇠가 이미 매끈하다 · 표 `_` 참조).
        /// </summary>
        public static Sprite BakeGhost(string name, Color rc, Color lite)
        {
            SummonGhostSpec sp = SummonFxStyle.Ghost;
            return BakeRadial(name, rc, lite, (float)sp.StopLite, (float)sp.StopRc, (float)sp.StopOut);
        }

        /// <summary>
        /// 등급 챕터 링 한 단계(정본 `.sr-tierflash` 6417~6427) — **테 굵기·번짐이 다른 판을 단계마다 따로 굽는다.**
        ///
        /// 정본 주석이 압력파 문법을 못 박았다: «퍼질수록 얇아지고(border-width 내림) 번진다(blur 오름).
        /// 하드엣지 고정 굵기는 «그래픽 스탬프» 다». 그런데 `transform: scale` 은 테 굵기까지 같이 키우므로
        /// **한 장을 배율로 날려서는 그 문법을 못 옮긴다** — 그래서 단계마다 판을 갈아 끼운다(결정 663 과 같은 길).
        ///
        /// <paramref name="bandF"/> 는 판 반지름에 대한 테 굵기 비율 · <paramref name="softF"/> 는 같은 자로 잰 번짐 폭이다.
        /// 링 둘레의 `box-shadow 0 0 .9rem` 은 바깥쪽 감쇠 꼬리로 같이 굽는다(따로 한 겹을 두면 두 번 가산된다).
        /// </summary>
        public static Sprite BakeTierRing(string name, Color rc, float bandF, float softF, float glowF)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int N = Mathf.Max(16, Mathf.RoundToInt(L("bake_px")));
            float outer = 1f;                                   // 테의 바깥 끝 = 판의 변 한가운데
            float inner = Mathf.Max(0.02f, outer - Mathf.Max(1e-4f, bandF));
            float soft = Mathf.Max(0.5f / N, softF);            // 적어도 한 화소는 잇는다
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                float v = (y + 0.5f) / N * 2f - 1f;
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a;
                    if (r < inner - soft) a = 0f;                               // 고리 안쪽은 비어 있다
                    else if (r < inner) a = Ramp(r, inner - soft, 0f, inner, 1f);
                    else if (r <= outer) a = 1f;
                    else a = Ramp(r, outer, 1f, outer + glowF, 0f);             // box-shadow 의 바깥 꼬리
                    if (r > outer + glowF) a = 0f;
                    px[y * N + x] = new Color(rc.r, rc.g, rc.b, Mathf.Clamp01(a));
                }
            }
            return Finish(name, NewTex(name, N, N), px);
        }

        /// <summary>
        /// 챕터 링의 **심지**(정본 `.sr-tierflash::after` 6434~6441) — 정지점만 다른 같은 방사 문법이라 <see cref="BakeRadial"/> 을 나눠 쓴다.
        /// 정본 주석: «링만 있으면 «테두리 원» 이고, 중심이 그 등급색으로 한 번 달아올라야 광원 문법에 앉는다».
        /// </summary>
        public static Sprite BakeTierWick(string name, Color rc, Color lite)
        {
            SummonTierBreakSpec sp = SummonFxStyle.TierBreak;
            return BakeRadial(name, rc, lite, (float)sp.WickStopLite, (float)sp.WickStopRc, (float)sp.WickStopOut);
        }

        /// <summary>
        /// 등급 챕터 펄스 판(정본 `.sr-tierpulse` 6402~6412) —
        /// `radial-gradient(120% 90% at 50% 42%, var(--rc-lite) 0%, var(--rc) 30%, rgba(0,0,0,0) 64%)` 한 장.
        ///
        /// 재점화·잔상과 달리 **타원**이고 중심이 위쪽(42%)이라 `BakeRadial` 과 따로 굽는다.
        /// CSS 는 위에서 아래로 — 구운 판은 아래에서 위로(UGUI)라 y 를 뒤집는다(`BakeVig` 와 같은 자리).
        /// 마지막 구간은 색을 등급색으로 둔 채 알파만 떨어뜨린다(미리 곱한 알파 · `BakeRadial` 과 같은 까닭).
        /// </summary>
        public static Sprite BakeTierPulse(string name, Color rc, Color lite)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            SummonTierBreakSpec sp = SummonFxStyle.TierBreak;
            int N = Mathf.Max(16, Mathf.RoundToInt(L("bake_px")));
            float rx = (float)sp.Rx, ry = (float)sp.Ry, cx = (float)sp.Cx, cy = (float)sp.Cy;
            float s0 = (float)sp.StopLite, s1 = (float)sp.StopRc, s2 = (float)sp.StopOut;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                float fy = 1f - (y + 0.5f) / N;                 // CSS 의 +y 는 아래
                for (int x = 0; x < N; x++)
                {
                    float fx = (x + 0.5f) / N;
                    float u = (fx - cx) / rx, v = (fy - cy) / ry;
                    float r = Mathf.Sqrt(u * u + v * v);        // 1 = 그라디언트의 바깥 끝
                    Color c; float a;
                    if (r <= s1) { c = Color.Lerp(lite, rc, s1 <= s0 ? 1f : Mathf.Clamp01((r - s0) / (s1 - s0))); a = 1f; }
                    else { c = rc; a = Ramp(r, s1, 1f, s2, 0f); }
                    if (r > s2) a = 0f;
                    px[y * N + x] = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
                }
            }
            return Finish(name, NewTex(name, N, N), px);
        }

        /// <summary>
        /// `radial-gradient(closest-side, lite 0%, rc <s1>, rgba(0,0,0,0) <s2>)` 한 장 — 재점화·잔상이 같이 쓴다.
        ///
        /// CSS 가 투명으로 잇는 마지막 구간은 **미리 곱한 알파**로 보간한다 — 색은 등급색 그대로 두고 알파만 떨어뜨린다
        /// (색을 검정으로 끌면 가산 혼합에서 «까맣게 죽은 테» 가 한 겹 생긴다). 정사각 판이라 `closest-side` = 반지름 = 반 변.
        /// </summary>
        static Sprite BakeRadial(string name, Color rc, Color lite, float s0, float s1, float s2)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int N = Mathf.Max(16, Mathf.RoundToInt(L("bake_px")));
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                float v = (y + 0.5f) / N * 2f - 1f;
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);          // 1 = 변의 한가운데(= closest-side 반지름)
                    Color c; float a;
                    if (r <= s1) { c = Color.Lerp(lite, rc, s1 <= s0 ? 1f : Mathf.Clamp01((r - s0) / (s1 - s0))); a = 1f; }
                    else { c = rc; a = Ramp(r, s1, 1f, s2, 0f); }
                    if (r > s2) a = 0f;
                    px[y * N + x] = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
                }
            }
            return Finish(name, NewTex(name, N, N), px);
        }

        /// <summary>
        /// 착지 스파크 판(정본 `.sr-spark` 6463~6476) — 심 하나 + `box-shadow` 복제 여덟을 **한 장에** 굽는다.
        ///
        /// 축 넷은 등급색(1.5rem) · 대각 넷은 흰색(1.06rem)이고, 음수 퍼짐(spread)은 복제의 반지름을 그만큼 깎는다.
        /// 정본이 `transform: scale` 로 «오프셋도 함께 확대» 하므로 이 판은 **제자리 배치만** 굽고 날리기는 부르는 쪽의 배율이 한다.
        /// 가장자리는 한 화소 안에서 잇는다 — 안 그러면 지름 7~8 화소짜리 점이 사각형으로 보인다.
        /// </summary>
        public static Sprite BakeSpark(string name, Color rc)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            SummonSparkSpec sp = SummonFxStyle.Spark;
            int N = Mathf.Max(16, Mathf.RoundToInt(L("bake_px")));
            float half = (float)sp.BoxRem * 0.5f;                 // 판 한 변의 절반(rem)
            float perRem = N * 0.5f / half;                       // rem → 화소
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                float vy = ((y + 0.5f) / N * 2f - 1f) * half;     // rem · +y 는 위(UGUI)
                for (int x = 0; x < N; x++)
                {
                    float vx = ((x + 0.5f) / N * 2f - 1f) * half;
                    float bestA = 0f; Color bestC = Color.white;
                    for (int i = 0; i <= 8; i++)
                    {
                        double dx0, dy0, rr; bool rarity;
                        sp.Dot(i, out dx0, out dy0, out rr, out rarity);
                        float d = Mathf.Sqrt((vx - (float)dx0) * (vx - (float)dx0) + (vy - (float)dy0) * (vy - (float)dy0));
                        float edge = 0.5f / perRem;               // 한 화소 폭으로 잇는다
                        float a = d <= (float)rr - edge ? 1f : d >= (float)rr + edge ? 0f : 1f - (d - ((float)rr - edge)) / (2f * edge);
                        if (a > bestA) { bestA = a; bestC = rarity ? rc : Color.white; }
                    }
                    px[y * N + x] = new Color(bestC.r, bestC.g, bestC.b, Mathf.Clamp01(bestA));
                }
            }
            return Finish(name, NewTex(name, N, N), px);
        }

        /// <summary>좁은 선 하나 — <paramref name="mid"/> 에서 1 이고 양옆 <paramref name="a"/>·<paramref name="b"/> 에서 0.</summary>
        static float Band(float t, float a, float mid, float b)
        {
            if (t <= a || t >= b) return 0f;
            return t < mid ? Ramp(t, a, 0f, mid, 1f) : Ramp(t, mid, 1f, b, 0f);
        }

        public static Sprite BakeStar(string name, float sz, float box)
        {
            Sprite hit; if (cache.TryGetValue(name, out hit) && hit != null) return hit;
            int S = Mathf.Max(8, Mathf.RoundToInt(box));
            Color core = SummonFxStyle.C("star_core"), mid = SummonFxStyle.C("star_mid"), lobe = SummonFxStyle.C("star_lobe");
            float lobeW = L("star_lobe_px") * L("css_px") / box * S;
            float midStop = L("star_mid_stop"), edgeStop = L("star_edge_stop");
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float cx = x + 0.5f - S * 0.5f, cy = y + 0.5f - S * 0.5f;
                    float r = Mathf.Sqrt(cx * cx + cy * cy) / (sz / box * S * 0.5f);   // 1 = 점의 반지름
                    float a = r < midStop ? Ramp(r, 0f, core.a, midStop, mid.a) : Ramp(r, midStop, mid.a, edgeStop, 0f);
                    Color c = r < midStop ? Color.Lerp(core, mid, r / midStop) : mid;
                    // 로브 — 세로·가로 막대(폭 lobeW · 길이 = 상자) · 가운데 .95 → 끝 0
                    float lv = Mathf.Abs(cx) <= lobeW * 0.5f ? 1f - Mathf.Abs(cy) / (S * 0.5f) : 0f;
                    float lh = Mathf.Abs(cy) <= lobeW * 0.5f ? 1f - Mathf.Abs(cx) / (S * 0.5f) : 0f;
                    float la = Mathf.Max(lv, lh) * lobe.a;
                    if (la > a) { c = lobe; a = la; }
                    px[y * S + x] = new Color(c.r, c.g, c.b, a);
                }
            return Finish(name, NewTex(name, S, S), px);
        }
    }

    /// <summary>`Resources/SummonFxUi.json` — 색·배치 표(T65·T134 꼴).</summary>
    public static class SummonFxStyle
    {
        public const string ResourcePath = "SummonFxUi";
        static JsonObject root, colors, layout;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        public static JsonObject Root { get { Load(); return root; } }

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T179)");
            root = MiniJson.ParseObject(ta.text);
            colors = J.Obj(root["colors"]);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; colorCache.Clear(); charge = null; idle = null; hero = null; relight = null; spark = null; ghost = null; tierBreak = null; heroRingEase = null; }

        static SummonChargeSpec charge;
        static SummonIdleSpec idle;
        static SummonHeroSpec hero;
        static SummonRelightSpec relight;
        static SummonSparkSpec spark;
        static SummonGhostSpec ghost;
        static SummonTierBreakSpec tierBreak;
        /// <summary>T334 6회차 — 주역 착지의 화면 킥(표의 `hero` 절).</summary>
        /// <summary>표의 `hero` 절 수치 하나(그 절은 `layout` 밖이다).</summary>
        public static float H(string key) { Load(); return (float)J.Num(J.Require(J.Obj(root["hero"]), key)); }

        static CssEase heroRingEase;
        /// <summary>충격파의 이징(정본 `cubic-bezier(.08,.72,.3,1)`).</summary>
        public static CssEase HeroRingEase
        {
            get
            {
                if (heroRingEase == null)
                {
                    double[] e = J.NumArr(J.Require(J.Obj(Root["hero"]), "heroring_ease"));
                    heroRingEase = new CssEase(e[0], e[1], e[2], e[3]);
                }
                return heroRingEase;
            }
        }

        public static SummonHeroSpec Hero { get { Load(); if (hero == null) hero = SummonHeroSpec.From(root); return hero; } }
        /// <summary>T334 5회차 — 완료 뒤 아이들 호흡(표의 `idle` 절).</summary>
        public static SummonIdleSpec Idle { get { Load(); if (idle == null) idle = SummonIdleSpec.From(root); return idle; } }
        /// <summary>T334 3회차 ⓑ — 충전 구간 키프레임 넷(Core 가 쥔 셈 · 표의 `charge` 절).</summary>
        public static SummonChargeSpec Charge { get { Load(); if (charge == null) charge = SummonChargeSpec.From(root); return charge; } }

        /// <summary>T334 11회차 — 셀별 광원 재점화 규칙(정본 `.sr-relight`).</summary>
        public static SummonRelightSpec Relight { get { Load(); if (relight == null) relight = SummonRelightSpec.From(root); return relight; } }

        /// <summary>T334 12회차 — 착지 스파크 규칙(정본 `.sr-spark`).</summary>
        public static SummonSparkSpec Spark { get { Load(); if (spark == null) spark = SummonSparkSpec.From(root); return spark; } }

        /// <summary>T334 15회차 — 비행 잔상 규칙(정본 `.sr-ghost`).</summary>
        public static SummonGhostSpec Ghost { get { Load(); if (ghost == null) ghost = SummonGhostSpec.From(root); return ghost; } }

        /// <summary>T334 16회차 — 등급 챕터 펄스 규칙(정본 `.sr-tierpulse`).</summary>
        public static SummonTierBreakSpec TierBreak { get { Load(); if (tierBreak == null) tierBreak = SummonTierBreakSpec.From(root); return tierBreak; } }

        public static Color C(string key)
        {
            Load();
            Color c;
            if (colorCache.TryGetValue(key, out c)) return c;
            string hex = J.Str(colors[key]);
            if (hex == null || !ColorUtility.TryParseHtmlString(hex, out c)) throw new KeyNotFoundException("SummonFxUi.json 의 «colors» 에 «" + key + "» 이 없다");
            colorCache[key] = c;
            return c;
        }

        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException("SummonFxUi.json 의 «layout» 에 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        public static float[] Arr(string key)
        {
            Load();
            double[] d = J.NumArr(layout[key]);
            if (d == null) throw new KeyNotFoundException("SummonFxUi.json 의 «layout» 에 배열 «" + key + "» 이 없다");
            var f = new float[d.Length];
            for (int i = 0; i < d.Length; i++) f[i] = (float)d[i];
            return f;
        }
    }
}
