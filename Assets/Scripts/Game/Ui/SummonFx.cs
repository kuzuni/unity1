using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Game.Gallery;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T179 — 소환 결과 팝업의 연출 겹(정본 `.sr-canopy`(아치 + 빛발 3 + 스필) · `.sr-rays`(배경 광선) · `.sr-stars`(별)). 수치는 `Resources/SummonFxUi.json`.
    /// 정본은 CSS 그라디언트·마스크·blur 로 그리는데 UGUI 에는 그것이 없어 **한 장씩 굽는다**(<see cref="CraftFxPoly"/>·<see cref="AgePattern"/> 과 같은 길 · 결정 223 «맨 Graphic 은 안 칠해진다»).
    /// 층 사다리(정본 5692~5697): 광선 0 · 바닥 10 · 반사 12 · 별 15 · 천개 20 · 그리드 40 — 형제 순서로 지킨다.
    /// 바닥 반사(`.sr-reflect` · 3회차): 정본 `buildSummonReflection`(ui.js 771~790)은 done 에서 그리드를 **복제**해 이름·배지를 떼고 뒤집어(scaleY −1.22 · 위 변 고정) blur 4px + 세로 마스크로 깐다.
    /// UGUI 엔 blur 도 소프트 마스크도 없고, 정본 주석대로 «blur 가 약하면 거꾸로 놓인 아이콘 줄로 읽힌다» 라 겹 복제로는 못 옮긴다 — 그래서 <see cref="BakeReflection"/> 이 복제 그리드를 임시 월드 캔버스에 세워
    /// «1 픽셀 = blur 4px» 해상도의 RT 에 한 번 찍고(다운샘플이 곧 흐림) 상자 흐림·채도·밝기·마스크를 픽셀로 얹은 **한 장**을 뒤집힌 Image 에 건다. done **다음** 프레임에 한다(결정 516 과 같은 까닭).
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
        /// `.sr-reflect` 를 세운다(정본 6957~6978 · ui.js 771~790): 몸 폭 · 위 변 = 그리드 아래 − 8px(위로 겹침) · 높이 = 그리드 높이 · 피벗 위 가운데에서 scaleY(−1.22)(정본 transform-origin 55% = 위 변 고정) · z 12(바닥 다음 형제).
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
            reflect.pivot = new Vector2(0.5f, 1f);
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

        public static void Reset() { root = null; colorCache.Clear(); }

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
