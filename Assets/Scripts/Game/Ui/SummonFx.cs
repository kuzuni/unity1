using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T179 — 소환 결과 팝업의 연출 겹(정본 `.sr-canopy`(아치 + 빛발 3 + 스필) · `.sr-rays`(배경 광선) · `.sr-stars`(별)). 수치는 `Resources/SummonFxUi.json`.
    /// 정본은 CSS 그라디언트·마스크·blur 로 그리는데 UGUI 에는 그것이 없어 **한 장씩 굽는다**(<see cref="CraftFxPoly"/>·<see cref="AgePattern"/> 과 같은 길 · 결정 223 «맨 Graphic 은 안 칠해진다»).
    /// 층 사다리(정본 5692~5697): 광선 0 · 바닥 10 · 별 15 · 천개 20 · 그리드 40 — 형제 순서로 지킨다. 바닥 반사(`.sr-reflect` 12)는 2회차.
    /// </summary>
    public sealed class SummonFx : MonoBehaviour
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        RectTransform canopy, rays;
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
        bool done;

        public RectTransform Canopy { get { return canopy; } }
        public RectTransform Rays { get { return rays; } }
        public int StarCount { get { return stars.Count; } }
        public int RayBarCount { get { return rayBars.Count; } }
        public bool Done { get { return done; } }
        public float StarsAlpha { get { return starsGroup != null ? starsGroup.alpha : 0f; } }

        /// <summary>
        /// 무대(`stage`)판의 몸(`sr-body`)에 세 겹을 세운다. <paramref name="gridTop"/>·<paramref name="gridW"/> 는 그리드의 몸 안 자리(위에서 · 폭) · <paramref name="floor"/> 는 이미 선 소환진(그 위에 별·천개를 끼운다).
        /// </summary>
        public static SummonFx Build(RectTransform body, RectTransform floor, float gridTop, float gridW, bool one, bool compact)
        {
            SummonFx fx = body.gameObject.AddComponent<SummonFx>();
            fx.t0 = Time.unscaledTime;
            float W = body.rect.width, H = body.rect.height, rem = PetSkillStyle.RemPx;
            var s = SummonFxStyle.Root;

            // ---- 배경 광선(z 0) — 몸 가운데 · 폭 190% 정사각 · 도입 수축 · 20s 회전 ----
            float rw = W * L("rays_w_f");
            fx.rays = UiKit.Box(body, "sr-rays");
            UiKit.Anchor(fx.rays, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rw, rw);
            fx.raysImg = fx.rays.gameObject.AddComponent<Image>();
            fx.raysIdle = BakeRays("sr-rays", "rays_mask", "rays_mask_a");
            fx.raysDone = BakeRays("sr-rays-done", "rays_done_mask", "rays_done_mask_a");
            fx.raysImg.sprite = fx.raysIdle;
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
                si.sprite = BakeStar("sr-star-" + Mathf.RoundToInt(sz), sz, box);
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
            arch.sprite = BakeArch("sr-arch-" + (one ? "one" : compact ? "compact" : "stage"), cw, ch, rem);
            arch.raycastTarget = false;
            // 스필(b) — 아치 위로 넓게 새는 빛무리
            float sw = cw * L("spill_w_f"), sh = (compact ? L("spill_compact_h_rem") : L("spill_h_rem")) * rem;
            RectTransform spRt = UiKit.Box(fx.canopy, "spill");
            UiKit.Anchor(spRt, new Vector2(0.5f, L("spill_bottom_f")), new Vector2(0.5f, 0f), Vector2.zero, sw, sh);
            fx.spill = spRt.gameObject.AddComponent<Image>();
            fx.spill.sprite = BakeSpill("sr-spill");
            fx.spill.raycastTarget = false;
            // 빛발 3 — 아치에서 위로
            float bw = L("ray_w_rem") * rem;
            float hMid = (compact ? L("ray_compact_h_rem") : L("ray_h_rem")) * rem, hSide = (compact ? L("ray_compact_side_h_rem") : L("ray_side_h_rem")) * rem;
            float dx = (compact ? L("ray_compact_side_dx_rem") : L("ray_side_dx_rem")) * rem;
            float[] xs = { -dx, 0f, dx }; float[] hs = { hSide, hMid, hSide };
            float[] per = { L("ray_side1_period_s"), L("ray_period_s"), L("ray_side3_period_s") };
            float[] del = { 0f, 0f, L("ray_side3_delay_s") };
            Sprite bar = BakeRayBar("sr-ray");
            for (int i = 0; i < 3; i++)
            {
                RectTransform r = UiKit.Box(fx.canopy, "ray-" + (i + 1));
                UiKit.Anchor(r, new Vector2(0.5f, L("ray_bottom_f")), new Vector2(0.5f, 0f), new Vector2(xs[i], 0f), bw, hs[i]);
                Image ri = r.gameObject.AddComponent<Image>();
                ri.sprite = bar; ri.raycastTarget = false;
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
            if (raysImg != null) raysImg.sprite = raysDone;
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
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;                 // 0 = 아래 · 1 = 위
                float fromTop = 1f - v;
                float mask = Ramp(fromTop, L("arch_mask_solid_f"), 1f, L("arch_mask_end_f"), 0f);
                float tmask = Ramp(fromTop, L("tick_mask_solid_f"), 1f, L("tick_mask_end_f"), 0f);
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f, vv = v * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + vv * vv);
                    float ring = 1f - Mathf.Clamp01((Mathf.Abs(r - 1f + strokeN * 0.5f) - strokeN * 0.5f) / (1.5f / W * 2f));
                    float band = r < L("arch_fill_r0") ? 0f : r < L("arch_fill_r1") ? Ramp(r, L("arch_fill_r0"), 0f, L("arch_fill_r1"), 1f) : Ramp(r, L("arch_fill_r1"), 1f, 1f, 0f);
                    float deg = Mathf.Repeat(Mathf.Atan2(vv, u) * Mathf.Rad2Deg, 360f);
                    bool tick = Mathf.Repeat(deg, L("tick_every_deg")) < L("tick_deg") && r >= L("tick_r0") && r <= L("tick_r2");
                    float tickA = tick ? (r < L("tick_r1") ? Ramp(r, L("tick_r0"), 0f, L("tick_r1"), 1f) : 1f) * tmask : 0f;
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
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;   // 0 = 밑동
                float ga = v < 0.46f ? Ramp(v, 0f, L("ray_a0"), 0.46f, L("ray_a46")) : v < 0.8f ? Ramp(v, 0.46f, L("ray_a46"), 0.8f, L("ray_a80")) : Ramp(v, 0.8f, L("ray_a80"), 1f, 0f);
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
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;   // 0 = 아래
                float mask = v < L("spill_mask_mid_f") ? Ramp(v, L("spill_mask_solid_f"), 1f, L("spill_mask_mid_f"), L("spill_mask_mid_a")) : Ramp(v, L("spill_mask_mid_f"), L("spill_mask_mid_a"), 1f, 0f);
                for (int x = 0; x < W; x++)
                {
                    float u = ((x + 0.5f) / W * 2f - 1f) / L("spill_rx_f");
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = r < 0.42f ? Ramp(r, 0f, L("spill_a0"), 0.42f, L("spill_a42")) : Ramp(r, 0.42f, L("spill_a42"), 1f, 0f);
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
                    float a1 = Mathf.Repeat(deg, L("rays_spoke1_every_deg")) < L("rays_spoke1_deg") ? c1.a : 0f;
                    float a2 = Mathf.Repeat(deg, L("rays_spoke2_every_deg")) < L("rays_spoke2_deg") ? c2.a : 0f;
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
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float cx = x + 0.5f - S * 0.5f, cy = y + 0.5f - S * 0.5f;
                    float r = Mathf.Sqrt(cx * cx + cy * cy) / (sz / box * S * 0.5f);   // 1 = 점의 반지름
                    float a = r < L("star_mid_stop") ? Ramp(r, 0f, core.a, L("star_mid_stop"), mid.a) : Ramp(r, L("star_mid_stop"), mid.a, L("star_edge_stop"), 0f);
                    Color c = r < L("star_mid_stop") ? Color.Lerp(core, mid, r / L("star_mid_stop")) : mid;
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
