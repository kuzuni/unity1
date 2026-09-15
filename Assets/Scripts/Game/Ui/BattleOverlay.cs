using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forge.Core.BattleFx;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 전투 씬 위 오버레이(T39) — 원작 `#boss-warning`(css 345~ · 감광 + 붉은 점멸 3회 + 사선 위험 띠 배너 + 흐르는 WARNING + 한글 부제) · `deathFade`(17402 · 암전 커버 + «죽었습니다»/부제) ·
    /// `sceneCut`(17361 · 420ms 하드컷 커버). 게임 영역 밴드(HUD 층과 같은 0~sheet_top)에만 깔리고 HUD 바로 위 · 시트/채팅/패널/탭바 아래에 선다(원작 z 14/15/16 · 팝업 20+ 아래).
    /// 시계는 <see cref="Tick"/> 으로 전투 씬이 민다(수동 스텝 테스트가 같은 길). 글자는 <see cref="UiKit.Text"/>(TextKind) · 색은 카탈로그 키의 가장 가까운 것(전용 키는 T22/T27 · 결정 기록).
    /// </summary>
    public sealed class BattleOverlay : MonoBehaviour
    {
        public static BattleOverlay Instance { get; private set; }
        /// <summary>원작 rem = 앱높이/844 × 16 css px → 캔버스 px.</summary>
        public static float Rem { get { return UiKit.RefH / 844f * 16f; } }

        /// <summary>T368 — 보스 경고 사선 줄무늬 한 타일의 가로(캔버스 px). 정본이 `background-size: 1.556rem`(= 주기 1.1rem × √2)로 적어 둔 수를 <see cref="SurfaceArt.StripeTileWidth"/> 가 스스로 낸다.</summary>
        static float StripeTileW { get { return SurfaceArt.StripeTileWidth("bw_hazard", HazardRem("period_rem") * Rem); } }
        /// <summary>T368 4회차 — 빗금 표(`SurfaceUi.json` `stripes.bw_hazard`)의 rem 칸(`period_rem`·`dash_rem`·`height_rem`). 정본 395 가 이 자리만 rem 으로 못 박았다 — 수는 코드에 안 박는다(§1) · 칸이 없으면 던진다(기본값으로 가리지 않는다).</summary>
        static float HazardRem(string field)
        {
            float v = SurfaceArt.StripeNum("bw_hazard", field, float.NaN);
            if (float.IsNaN(v)) throw new System.Collections.Generic.KeyNotFoundException("SurfaceUi.json stripes.bw_hazard 에 «" + field + "» 이 없다 (T368)");
            return v;
        }

        RectTransform layer;
        // boss warning
        RectTransform warnRoot; Image dim, flash, bannerBg; RectTransform banner; TextMeshProUGUI marquee; RectTransform track; TextMeshProUGUI sub; RectTransform[] hazardTiles;
        double warnT = -1, warnDur = FxRules.BossWarnDur; float trackW;
        // death
        RectTransform deathRoot; Image cover; TextMeshProUGUI title; Image rule; TextMeshProUGUI dsub; RectTransform bannerGroup;
        double deathT = -1;
        // scene cut
        Image cut; double cutT = -1, cutMs;
        // 피격 붉은 비네트(T135 ⓐ)
        Image vig; double vigT = -1, vigPeak;
        // 스킬 시전 섬광(T335 ⓑ · 정본 `#skill-flash` css 1919 · ui.js 3779 `skillFlash(color)`)
        Image skf; double skfT = -1;

        public bool WarningActive { get { return warnT >= 0; } }
        public double WarningT { get { return warnT; } }
        public bool DeathActive { get { return deathT >= 0; } }
        public double DeathMs { get { return deathT * 1000; } }
        public float CoverAlpha { get { return cover != null ? cover.color.a : 0; } }
        public bool CutActive { get { return cutT >= 0; } }
        public string DeathSub { get { return dsub != null ? dsub.text : null; } }
        public bool VignetteActive { get { return vigT >= 0; } }
        /// <summary>이번 피격의 `--vig`(정점 알파).</summary>
        public double VignettePeak { get { return vigPeak; } }
        public float VignetteAlpha { get { return vig != null && vig.gameObject.activeSelf ? vig.color.a : 0f; } }
        /// <summary>비네트 면 — 테스트가 층·띠·그림을 본다.</summary>
        public Image Vignette { get { return vig; } }
        public bool SkillFlashActive { get { return skfT >= 0; } }
        public float SkillFlashAlpha { get { return skf != null && skf.gameObject.activeSelf ? skf.color.a : 0f; } }
        /// <summary>섬광 면 — 테스트가 층·색·서열을 본다.</summary>
        public Image SkillFlashImage { get { return skf; } }
        /// <summary>오버레이 띠(상단바 아래 ~ 장비 시트 위 · 정본 `#game-area`) — 테스트가 층·띠를 본다.</summary>
        public RectTransform Layer { get { return layer; } }

        /// <summary>UiRoot 아래에 한 번 세운다(없으면 null — UI 껍데기가 없는 씬).</summary>
        public static BattleOverlay Ensure()
        {
            if (Instance != null) return Instance;
            UiRoot root = UiRoot.Instance;
            if (root == null || root.App == null) return null;
            RectTransform rt = UiKit.Box(root.App, "battle-overlay");
            // 정본 `index.html`: `#topbar`(64행) 는 `#game-area`(67행) **밖**의 형제이고 사망 암전·씬컷·보스 워닝은 전부 `#game-area` 안의
            // `#fx-layer`/`#boss-warning`(z 13~16 · isolation) 에 산다 — 그래서 상단바는 어떤 전투 연출에도 덮이지 않는다.
            // 띠를 «상단바 아래 ~ 시트 위» 로 둔다(T85 · 촬영 런 141 에서 사망 암전이 상단바까지 검게 덮은 자리).
            UiKit.Band(rt, UiKit.L("topbar_h"), UiKit.L("sheet_top"));
            rt.SetSiblingIndex(root.HudLayer.GetSiblingIndex() + 1);
            var o = rt.gameObject.AddComponent<BattleOverlay>();
            o.layer = rt;
            o.StageVignette(root);
            Instance = o;
            return o;
        }

        /// <summary>
        /// T178 12회차 — 정본 `style.css` 147~150 `#game-area::after` 의 **상시 비네트**.
        /// 정본 주석: «비네트 포스트 — 화면 가장자리를 살짝 눌러 시선을 중앙 전투 라인으로 모음».
        /// 클론엔 이 겹이 통째로 없었다(T135 의 피격 `#dmg-flash` 는 **다른 자리** — 그쪽은 맞으면 켜졌다 꺼진다).
        ///
        /// 자리: 정본은 `#game-area` 안 `z-index: 1` 이라 3D(`#game3d`) **위** · `#fx-layer`·`#boss-warning` **아래**다.
        /// 그래서 오버레이 띠(`layer`)의 **자식이 아니라 그 바로 앞 형제**로 둔다 — 띠 안의 맨 아래 자리는
        /// 이미 임자가 있고(피격 비네트 `DamageVignetteTests` · 스킬 섬광 `SkillFlashTests` 가 «맨 아래» 를 단언한다),
        /// 거기 끼우면 남의 자를 깨뜨린다. 띠 밖 형제로 두면 상단바·시트를 안 덮는 것은 같은 `UiKit.Band` 가 보장한다.
        /// 값은 표(`Resources/SurfaceUi.json` `stage_vignette`)가 쥔다.
        /// </summary>
        void StageVignette(UiRoot root)
        {
            if (vignetteBand != null) return;
            RectTransform band = UiKit.Box(root.App, "stage-vignette");
            UiKit.Band(band, UiKit.L("topbar_h"), UiKit.L("sheet_top"));
            band.SetSiblingIndex(layer.GetSiblingIndex());          // 띠 바로 앞 = 3D 위 · 모든 연출 아래
            // 높이는 `rect` 대신 표에서 센다 — 방금 만든 상자는 레이아웃 전이라 `rect.height` 가 0 이다(비율만 쓰면 굽는 타원이 납작해진다).
            float w = UiKit.RefW, h = UiKit.RefH * (UiKit.L("sheet_top") - UiKit.L("topbar_h"));
            SurfaceArt.Fill(band, "vignette-grad", "stage_vignette", w, h);
            vignetteBand = band;
        }

        RectTransform vignetteBand;
        /// <summary>상시 비네트 띠 — 테스트가 자리·겹을 본다(T178 12회차).</summary>
        public RectTransform StageVignetteBand { get { return vignetteBand; } }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        static Image Full(Transform parent, string name, string colorKey, float alpha)
        {
            Image im = UiKit.Panel(parent, name, colorKey);
            UiKit.Fill(im.rectTransform);
            im.raycastTarget = false;
            var c = im.color; c.a = alpha; im.color = c;
            return im;
        }

        // ── T173 — 정본 방사형 두 겹을 굽는다(표 `BossWarnUi.json`) ──────────────────────────────
        const string BossWarnRes = "BossWarnUi";
        static JsonObject bwTable;
        static readonly Dictionary<string, Sprite> bwSprites = new Dictionary<string, Sprite>();

        /// <summary>그 겹의 방사형 그림을 깐다(<paramref name="mat"/> 가 있으면 그 재질로 — 정본 `mix-blend-mode: screen`).</summary>
        static Image Radial(Transform parent, string name, string key, Material mat)
        {
            RectTransform rt = UiKit.Box(parent, name);
            UiKit.Fill(rt);
            Image im = rt.gameObject.AddComponent<Image>();
            im.raycastTarget = false;
            im.type = Image.Type.Simple;
            im.sprite = RadialSprite(key);
            if (mat != null) im.material = mat;   // 못 찾으면 null 이 와서 여태처럼 보통 알파로 그려진다(연출이 사라지는 것보다 낫다)
            im.color = new Color(1f, 1f, 1f, 0f);
            return im;
        }

        static JsonObject BossWarnTable()
        {
            if (bwTable != null) return bwTable;
            TextAsset ta = Resources.Load<TextAsset>(BossWarnRes);
            if (ta == null) throw new KeyNotFoundException("Resources/" + BossWarnRes + ".json 이 없다");
            bwTable = J.Obj(MiniJson.Parse(ta.text));
            if (bwTable == null || bwTable.Count == 0) throw new KeyNotFoundException(BossWarnRes + ".json 을 못 읽었다");
            return bwTable;
        }

        /// <summary>
        /// 상자를 꽉 채우는 방사형 그라디언트 한 장. 중심은 표의 `cx`·`cy`(정본 `at 50% 42%`)이고 100% 지점은
        /// **가장 먼 모서리**다(CSS 기본 `farthest-corner`) — 타원 밖을 투명으로 두면 모서리가 비어 정본과 달라진다.
        /// 굽은 그림은 <c>Apply(false, false)</c> 로 **읽을 수 있게** 남긴다(PlayMode 자가 «가운데가 가장자리보다 옅은가» 를 픽셀로 잰다).
        /// </summary>
        static Sprite RadialSprite(string key)
        {
            Sprite hit;
            if (bwSprites.TryGetValue(key, out hit) && hit != null) return hit;
            JsonObject root = BossWarnTable();
            JsonObject one = J.Obj(root[key]);
            List<object> stops = one == null ? null : J.Arr(one["stops"]);
            List<object> offs = one == null ? null : J.Arr(one["offsets"]);
            if (stops == null || offs == null || stops.Count < 2 || offs.Count != stops.Count)
                throw new KeyNotFoundException(BossWarnRes + ".json 의 «" + key + "» 에 stops·offsets(길이가 같은 둘)이 없다");
            float cx = (float)J.Num(one["cx"], 0.5), cy = (float)J.Num(one["cy"], 0.5);
            int n = Mathf.Max(8, (int)J.Num(root["bake_px"], 128));

            Color[] col = new Color[stops.Count];
            float[] pos = new float[offs.Count];
            for (int i = 0; i < stops.Count; i++)
            {
                List<object> c = J.Arr(stops[i]);
                if (c == null || c.Count < 4) throw new KeyNotFoundException(BossWarnRes + ".json 의 «" + key + "» stop " + i + " 가 [r,g,b,a] 가 아니다");
                col[i] = new Color((float)J.Num(c[0]) / 255f, (float)J.Num(c[1]) / 255f, (float)J.Num(c[2]) / 255f, (float)J.Num(c[3]));
                pos[i] = (float)J.Num(offs[i]);
            }
            // farthest-corner — 중심에서 가장 먼 모서리가 100%
            float rx = Mathf.Max(cx, 1f - cx), ry = Mathf.Max(cy, 1f - cy);
            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.name = "bw-" + key;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float v = 1f - (y + 0.5f) / n;              // 텍스처는 아래가 0행이고 CSS 는 위가 0
                    float dx = (u - cx) / rx, dy = (v - cy) / ry;
                    Color c = SampleStops(col, pos, Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy)));
                    px[y * n + x] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = tex.name;
            bwSprites[key] = sp;
            return sp;
        }

        /// <summary>stop 사이 선형 — 마지막 offset 뒤는 마지막 색 그대로다(정본 `… .34) 70%` 의 뒤쪽).</summary>
        static Color SampleStops(Color[] col, float[] pos, float t)
        {
            if (t <= pos[0]) return col[0];
            for (int i = 1; i < pos.Length; i++)
            {
                if (t > pos[i]) continue;
                float span = pos[i] - pos[i - 1];
                float k = span <= 0f ? 1f : (t - pos[i - 1]) / span;
                return Color.Lerp(col[i - 1], col[i], k);
            }
            return col[col.Length - 1];
        }

        static void Alpha(Graphic g, double a)
        {
            if (g == null) return;
            var c = g.color; c.a = (float)Math.Max(0, Math.Min(1, a)); g.color = c;
        }

        // ── 보스 워닝 ──
        void BuildWarning()
        {
            if (warnRoot != null) return;
            float rem = Rem;
            warnRoot = UiKit.Box(layer, "boss-warning");
            UiKit.Fill(warnRoot);
            // T173 — 정본 368·374 는 **방사형 그라디언트 두 겹**이다(단색 판이 아니다): 감광은 가운데가 옅고 가장자리가 짙으며,
            // 점멸은 `mix-blend-mode: screen` 이라 «가산 경광등» 이다. 클론은 둘 다 단색 판 + 보통 알파라 씬을 덮어 죽이는
            // 붉은 물감이었다(런 403 `gear-detail` 의 통짜 (90,14,11)). 색·중심·비율은 `BossWarnUi.json` 이 쥔다.
            dim = Radial(warnRoot, "bw-dim", "dim", null);
            flash = Radial(warnRoot, "bw-flash", "flash", CraftFxPoly.Screen());
            banner = UiKit.Box(warnRoot, "bw-banner");
            // T368 4회차 — 빗금 띠 두께는 표 `bw_hazard.height_rem`(정본 395 `.bw-hazard { height: .5rem }`) · 전엔 .5f 가 코드에 박혀 있었다
            float hazard = HazardRem("height_rem") * rem, pad = 0.3f * rem, textH = 1.5f * rem * 1.2f;
            float bannerH = hazard * 2 + pad * 2 + textH;
            banner.anchorMin = new Vector2(0, 1 - (float)FxRules.WarnTop); banner.anchorMax = new Vector2(1, 1 - (float)FxRules.WarnTop);
            banner.pivot = new Vector2(0.5f, 0.5f); banner.sizeDelta = new Vector2(0, bannerH); banner.anchoredPosition = Vector2.zero;
            bannerBg = UiKit.Panel(banner, "bg", "pp_red_dk"); UiKit.Fill(bannerBg.rectTransform); bannerBg.raycastTarget = false;
            // T178 11회차 — 정본 style.css 390 `.bw-banner { background: linear-gradient(180deg, #1e0202, #5a0707 45%, #240303) }`.
            // 클론은 `pp_red_dk` **단색 한 장**이라 띠가 납작했다 — 정본은 가운데(45%)가 가장 밝은 핏빛이고 위·아래 끝이 거의 검정이라
            // 띠가 가운데서 부풀어 오른 것처럼 보인다. 값은 표(`Resources/SurfaceUi.json` `bw_banner`)가 쥔다(§1 — 코드에 안 박는다).
            // 단색 판은 그대로 둔다 — 겹이 불투명이라 안 비치지만, 표가 없으면(`Bake` 가 null) 그 판이 바탕으로 남는다(`shop_banner` 와 같은 길).
            SurfaceArt.Fill(banner, "bw-banner-grad", "bw_banner", UiKit.RefW, bannerH);
            // T368 2회차 — 정본 style.css 395 `.bw-hazard` 는 **−45° 되풀이 줄무늬**다:
            //   `repeating-linear-gradient(-45deg, #ffca28 0 .55rem, #16100a .55rem 1.1rem)` + `background-size: 1.556rem 100%`(= 1.1 × √2 · 가로축 환산 주기)
            //   + `@keyframes bwhazard { 0 → 1.556rem }` .62s linear infinite.
            // 클론은 **세로 대시 24개**(폭 .55rem · 피치 1.1rem)였다 — 각도가 아예 없고 색 셋(`coin`·`topbar_bg`)도 정본이 아니며 수가 코드에 박혀 있었다.
            // 이제 한 타일을 굽고(`SurfaceArt.BakeStripe` · 셈은 Core `StripeRules`) `Tiled` 로 되풀이하며, 흐름은 한 타일만큼 밀어 준다(= background-position).
            hazardTiles = new RectTransform[2];
            float tileW = StripeTileW;
            for (int row = 0; row < 2; row++)
            {
                RectTransform brt = UiKit.Box(banner, "bw-hazard" + row);
                brt.anchorMin = new Vector2(0, row == 0 ? 1 : 0); brt.anchorMax = new Vector2(1, row == 0 ? 1 : 0);
                brt.pivot = new Vector2(0.5f, row == 0 ? 1 : 0); brt.sizeDelta = new Vector2(0, hazard); brt.anchoredPosition = Vector2.zero;
                brt.gameObject.AddComponent<RectMask2D>();      // 흐르는 판이 띠 밖으로 새지 않게
                RectTransform hzrt = UiKit.Box(brt, "stripe");
                hzrt.anchorMin = new Vector2(0, 0); hzrt.anchorMax = new Vector2(1, 1);
                hzrt.offsetMin = new Vector2(-tileW, 0); hzrt.offsetMax = new Vector2(tileW, 0);   // 양옆으로 한 타일씩 더 — 밀어도 빈자리가 안 생긴다
                Image st = hzrt.gameObject.AddComponent<Image>();
                st.raycastTarget = false;
                st.type = Image.Type.Tiled;
                st.sprite = SurfaceArt.BakeStripe("bw_hazard", HazardRem("period_rem") * rem, HazardRem("dash_rem") * rem, 0f, hazard);   // T368 4회차 — 주기·대시도 표에서(전엔 1.1f·.55f 리터럴)
                hazardTiles[row] = hzrt;
            }
            RectTransform mq = UiKit.Box(banner, "bw-marquee");
            mq.anchorMin = new Vector2(0, 0); mq.anchorMax = new Vector2(1, 1); mq.offsetMin = new Vector2(0, hazard); mq.offsetMax = new Vector2(0, -hazard);
            var mask = mq.gameObject.AddComponent<RectMask2D>();
            track = UiKit.Box(mq, "bw-track");
            track.anchorMin = new Vector2(0, 0.5f); track.anchorMax = new Vector2(0, 0.5f); track.pivot = new Vector2(0, 0.5f);
            marquee = UiKit.Text(track, "text", TextKind.Title, FxRules.WarnText + FxRules.WarnText + FxRules.WarnText, "pp_paper", TextAlignmentOptions.Left);
            WrapUi.Apply(marquee, "bw_track_span");   // T361 2회차 — 정본 white-space 표(WrapUi.json) 402 `.bw-track span { nowrap }`
            marquee.overflowMode = TextOverflowModes.Overflow;
            marquee.raycastTarget = false;
            // T168 2회차 — 정본 style.css 402 `.bw-track span { letter-spacing: .14em }`. 왼쪽 정렬이라 되밀기는 없다.
            LetterSpacing.Apply(marquee, "bw_track_ls_em");
            UiKit.Outline(marquee, "pp_red", 0.2f);
            UiKit.TextShadow(marquee, "bw_marquee");   // T333 5회차 — 정본 402 `.bw-track span` 셋째 겹 `0 2px 0 rgba(0,0,0,.86)`(붉은 글로우 둘은 위 붉은 테가 근사)
            var trt = marquee.rectTransform;
            trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(0, 0.5f); trt.pivot = new Vector2(0, 0.5f); trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(UiKit.RefW * 6, textH);
            trackW = 0;
            sub = UiKit.Text(warnRoot, "bw-sub", TextKind.Body, FxRules.WarnSub, "coin", TextAlignmentOptions.Center);
            sub.raycastTarget = false;
            UiKit.TextShadow(sub, "bw_sub");   // T333 5회차 — 정본 411 `.bw-sub` 둘째 겹 `0 2px 0 rgba(0,0,0,.86)`(붉은 글로우는 근사로 뺀다)
            // T168 2회차 — 정본 411 `.bw-sub { letter-spacing: .55em; text-indent: .55em }`: 한 글자씩 벌어지는 줄이었는데 클론은 0 이라 다닥다닥 붙어 있었다.
            // 되밀기(text-indent)까지 옮긴다 — 자간이 마지막 글자 뒤에도 붙어 가운데 정렬이 왼쪽으로 쏠리기 때문이다.
            LetterSpacing.Apply(sub, "bw_sub_ls_em", "bw_sub_indent_em");
            UiKit.Outline(sub, "pp_red_dk", 0.25f);
            var srt = sub.rectTransform;
            srt.anchorMin = new Vector2(0, 1 - (float)FxRules.WarnTop); srt.anchorMax = new Vector2(1, 1 - (float)FxRules.WarnTop); srt.pivot = new Vector2(0.5f, 1);
            srt.sizeDelta = new Vector2(0, 1.4f * rem); srt.anchoredPosition = new Vector2(0, -(bannerH / 2 + 0.3f * rem));
            warnRoot.gameObject.SetActive(false);
        }

        /// <summary>`UI.bossWarning(dur)` — 연속 보스전에서도 처음부터 다시 돈다.</summary>
        public void BossWarning(double sec)
        {
            BuildWarning();
            warnDur = sec > 0 ? sec : FxRules.BossWarnDur;
            warnT = 0;
            warnRoot.gameObject.SetActive(true);
            DriveWarning();
        }

        void DriveWarning()
        {
            double u = Math.Min(1, warnT / warnDur);
            // T173 — 정본 363 `#app:has(> .modal:not(.hidden)) #boss-warning .bw-dim { display: none }`:
            // 팝업이 떠 있으면 **경고 딤만** 끈다(모달 딤과 겹쳐 화면이 통째로 검게 죽는다 · 점멸·배너는 그대로 둬야
            // 카드 옆 여백에서 «보스 온다» 가 읽힌다 — 정본 주석).
            bool modal = PopupLayer.Instance != null && PopupLayer.Instance.OpenCount > 0;
            Alpha(dim, modal ? 0 : FxRules.WarnDim(u));
            // 세기(0~1)만 여기서 준다 — 정본 rgba 의 .62·.34 는 구운 그림이 이미 쥐고 있다(예전의 `* 0.62` 는 그 값을 코드에 박은 것이었다).
            Alpha(flash, FxRules.WarnFlash(warnT));
            banner.localScale = new Vector3(1, (float)FxRules.WarnBanner(u), 1);
            Alpha(sub, FxRules.WarnSubAlpha(u));
            if (trackW <= 0 && marquee != null) trackW = marquee.preferredWidth / 3f;
            float w = trackW > 0 ? trackW : UiKit.RefW;
            float off = (float)((warnT / FxRules.WarnScrollPeriod) % 1.0) * w;
            track.anchoredPosition = new Vector2(-off, 0);
            // T368 2회차 — 정본 `@keyframes bwhazard { from 0 → to 1.556rem }`: **한 타일**만큼 흐른다(주기가 아니라 가로축 환산 폭이다).
            float hz = (float)((warnT / FxRules.WarnHazardPeriod) % 1.0) * StripeTileW;
            if (hazardTiles != null)
                for (int i = 0; i < hazardTiles.Length; i++) if (hazardTiles[i] != null) hazardTiles[i].anchoredPosition = new Vector2(hz, 0);
        }

        // ── 사망 암전 ──
        void BuildDeath()
        {
            if (deathRoot != null) return;
            float rem = Rem;
            deathRoot = UiKit.Box(layer, "death-fade");
            UiKit.Fill(deathRoot);
            cover = Full(deathRoot, "cover", "modal_dim_deep", 0);
            bannerGroup = UiKit.Box(deathRoot, "banner");
            bannerGroup.anchorMin = new Vector2(0, 0.5f); bannerGroup.anchorMax = new Vector2(1, 0.5f); bannerGroup.pivot = new Vector2(0.5f, 0.5f);
            bannerGroup.sizeDelta = new Vector2(0, 5f * rem); bannerGroup.anchoredPosition = Vector2.zero;
            title = UiKit.Text(bannerGroup, "title", TextKind.Title, FxRules.DeathTitle, "pp_paper", TextAlignmentOptions.Center);
            // T168 2회차 — 여태 코드에 박혀 있던 32f 를 표로 옮긴다(§1). 근거는 정본 `scene3d.js` 17417 의 인라인
            // `letter-spacing:.32em; padding-left:.32em` 이다 — 되밀기도 그 줄이 시켜서 같이 준다.
            LetterSpacing.Apply(title, "death_title_ls_em", "death_title_indent_em");
            title.raycastTarget = false;
            UiKit.Outline(title, "pp_red", 0.18f);
            var t = title.rectTransform; t.anchorMin = new Vector2(0, 1); t.anchorMax = new Vector2(1, 1); t.pivot = new Vector2(0.5f, 1); t.sizeDelta = new Vector2(0, 2.2f * rem); t.anchoredPosition = Vector2.zero;
            rule = UiKit.Panel(bannerGroup, "rule", "pp_paper");
            rule.raycastTarget = false;
            var r = rule.rectTransform; r.anchorMin = new Vector2(0.5f, 1); r.anchorMax = new Vector2(0.5f, 1); r.pivot = new Vector2(0.5f, 1); r.sizeDelta = new Vector2(11f * rem, Mathf.Max(1f, 0.0625f * rem)); r.anchoredPosition = new Vector2(0, -(2.2f + 0.55f) * rem);
            dsub = UiKit.Text(bannerGroup, "sub", TextKind.Body, "", "pp_muted", TextAlignmentOptions.Center);
            dsub.raycastTarget = false;
            LetterSpacing.Apply(dsub, "death_sub_ls_em");   // 정본 scene3d.js 17423 `letter-spacing:.06em`(되밀기는 정본에 없다)
            var s = dsub.rectTransform; s.anchorMin = new Vector2(0, 1); s.anchorMax = new Vector2(1, 1); s.pivot = new Vector2(0.5f, 1); s.sizeDelta = new Vector2(0, 1.4f * rem); s.anchoredPosition = new Vector2(0, -(2.2f + 1.1f) * rem);
            deathRoot.gameObject.SetActive(false);
        }

        /// <summary>`deathFade(sub)` — 순수 장식 · 진행은 Core Battle 의 벽시계가 굴린다.</summary>
        public bool DeathFade(string sub)
        {
            BuildDeath();
            dsub.text = sub ?? "";
            deathT = 0;
            deathRoot.gameObject.SetActive(true);
            DriveDeath();
            return true;
        }

        void DriveDeath()
        {
            double ms = deathT * 1000;
            Alpha(cover, FxRules.DeathCoverAlpha(ms));
            double k = FxRules.DeathBannerK(ms);
            double a = k * FxRules.DeathCoverAlpha(ms);
            Alpha(title, a); Alpha(rule, a * 0.55); Alpha(dsub, a * 0.78);
            bannerGroup.anchoredPosition = new Vector2(0, (float)(-(1 - k) * FxRules.DeathBannerRise * Rem));
        }

        // ── 피격 붉은 비네트(T135 ⓐ · 정본 `ui.js` 1360 `flashDamage` · `style.css` 438 `#dmg-flash`) ──

        /// <summary>
        /// 그라디언트+마스크를 한 장에 구운다. 셈은 전부 <see cref="FxRules"/> 가 쥐고(EditMode 가 잰다) 여기서는 칠하기만 한다.
        /// 정규화 좌표로 굽기 때문에 띠가 세로로 길어도 CSS 와 같은 그림이 나온다(CSS 도 그라디언트를 상자 기준으로 푼다).
        /// </summary>
        static Sprite vigSprite;
        static Sprite VigSprite()
        {
            if (vigSprite != null) return vigSprite;
            const int n = 256;
            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.name = "dmg-vignette";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                // 텍스처 y 는 아래가 0 이고 마스크는 «위에서부터» 재므로 뒤집는다.
                double v = (y + 0.5) / n, yFromTop = 1 - v;
                double mask = FxRules.DmgVigMask(yFromTop);
                for (int x = 0; x < n; x++)
                {
                    double u = (x + 0.5) / n;
                    double r, g, b, a;
                    FxRules.DmgVigSample(FxRules.DmgVigT(u, v), out r, out g, out b, out a);
                    a *= mask;
                    px[y * n + x] = new Color32(B(r), B(g), B(b), B(a));
                }
            }
            tex.SetPixels32(px);
            // CPU 복사본을 남긴다(`UiShapes` 는 안 남긴다) — PlayMode 단언이 구운 픽셀을 되읽어
            // «가운데 투명 · 가장자리 붉음 · 바닥 사라짐» 을 직접 재기 때문이다. 256² RGBA = 256KB.
            tex.Apply(false, false);
            vigSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            vigSprite.name = "dmg-vignette";
            return vigSprite;
        }

        static byte B(double v) { return (byte)Math.Max(0, Math.Min(255, Math.Round(v * 255))); }

        void BuildVignette()
        {
            if (vig != null) return;
            vig = UiKit.Panel(layer, "dmg-flash", "pp_paper");
            UiKit.Fill(vig.rectTransform);
            vig.sprite = VigSprite();
            vig.type = Image.Type.Simple;
            vig.color = new Color(1, 1, 1, 0);   // 색은 스프라이트가 쥐고 여기서는 세기만 곱한다(정본 `--vig`).
            vig.raycastTarget = false;
            // 정본 z-index 12 — 씬 안 HUD 위 · **사망 암전(15)·씬컷(16)·보스 워닝 아래**(css 438 의 긴 경고 그대로).
            // 이 띠 안에서는 형제 순서가 곧 서열이므로 늘 맨 아래에 둔다.
            vig.transform.SetAsFirstSibling();
            vig.gameObject.SetActive(false);
        }

        /// <summary>`UI.flashDamage(sev)` — 연타해도 처음부터 다시 돈다(정본은 리플로우 강제로 같은 일을 한다).</summary>
        public void FlashDamage(double sev)
        {
            BuildVignette();
            vigPeak = FxRules.DmgVigPeak(sev);
            vigT = 0;
            vig.transform.SetAsFirstSibling();
            vig.gameObject.SetActive(true);
            DriveVignette();
        }

        void DriveVignette() { Alpha(vig, FxRules.DmgVigAlpha(vigT * 1000, vigPeak)); }

        // ── 스킬 시전 섬광(T335 ⓑ · 정본 css 1919~1921 `#skill-flash` z 5 · ui.js 3779 `skillFlash(color)` · combat.js 377 공격 스킬만) ──

        /// <summary>
        /// 흰 방사형 한 장 — 정본 `radial-gradient(ellipse at center, ${color}33 0%, transparent 65%)` 의 **알파만** 굽고 색은 <see cref="Image.color"/> 가 곱한다
        /// (투명으로 가는 보간은 색을 안 바꾸고 알파만 줄이므로 같은 그림). 셈은 <see cref="DungeonFxSpec.FlashGlowAlpha"/>(EditMode 가 잰다).
        /// </summary>
        static Sprite skfSprite;
        static Sprite SkillFlashSprite()
        {
            if (skfSprite != null) return skfSprite;
            DungeonFxSpec sp = DungeonClearFx.Spec;
            const int n = 128;
            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.name = "skill-flash";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                double v = 1 - (y + 0.5) / n;   // 텍스처는 아래가 0행 · CSS 는 위가 0
                for (int x = 0; x < n; x++)
                {
                    double u = (x + 0.5) / n;
                    px[y * n + x] = new Color32(255, 255, 255, B(sp.FlashGlowAlpha(u, v)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            skfSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            skfSprite.name = tex.name;
            return skfSprite;
        }

        void BuildSkillFlash()
        {
            if (skf != null) return;
            skf = UiKit.Panel(layer, "skill-flash", "pp_paper");
            UiKit.Fill(skf.rectTransform);
            skf.sprite = SkillFlashSprite();
            skf.type = Image.Type.Simple;
            skf.raycastTarget = false;
            skf.color = new Color(1, 1, 1, 0);
            skf.gameObject.SetActive(false);
        }

        /// <summary>`UI.skillFlash(color)` — 스킬 색(`#rrggbb`)의 방사형 섬광 .5s. 연타하면 처음부터 다시 돈다(정본은 리플로우 강제).</summary>
        public void SkillFlash(string colorHex)
        {
            Color c;
            if (string.IsNullOrEmpty(colorHex) || !ColorUtility.TryParseHtmlString(colorHex, out c)) throw new FormatException("스킬 색 «" + colorHex + "» 을 못 읽는다(gamedata skills color)");
            BuildSkillFlash();
            skf.color = new Color(c.r, c.g, c.b, 0f);
            skfT = 0;
            skf.gameObject.SetActive(true);
            DriveSkillFlash();
        }

        void DriveSkillFlash()
        {
            // 정본 z-index 5 — 비네트(12)·사망(15)·씬컷(16)·보스 워닝 전부 아래. 비네트가 켜질 때 스스로 맨 아래로 가므로 매 프레임 되돌린다.
            if (skf.transform.GetSiblingIndex() != 0) skf.transform.SetAsFirstSibling();
            Alpha(skf, DungeonClearFx.Spec.FlashAlphaAt(skfT * 1000));
        }

        // ── 씬컷 ──
        /// <summary>`sceneCut(apply, dur)` — 호출부가 apply 를 먼저 돌린 뒤 커버가 걷힌다.</summary>
        public void SceneCut(double ms)
        {
            if (cut == null) { cut = Full(layer, "scene-cut", "modal_dim_deep", 1); cut.gameObject.SetActive(false); }
            cutMs = ms > 0 ? ms : FxRules.SceneCutMs;
            cutT = 0;
            cut.transform.SetAsLastSibling();
            cut.gameObject.SetActive(true);
            Alpha(cut, 1);
        }

        /// <summary>시계를 민다 — 전투 씬 Step 이 부른다.</summary>
        public void Tick(float dt)
        {
            if (warnT >= 0)
            {
                warnT += dt;
                if (warnT >= warnDur) { warnT = -1; warnRoot.gameObject.SetActive(false); }
                else DriveWarning();
            }
            if (deathT >= 0)
            {
                deathT += dt;
                if (deathT * 1000 >= FxRules.DeathTotalMs) { deathT = -1; deathRoot.gameObject.SetActive(false); }
                else DriveDeath();
            }
            if (vigT >= 0)
            {
                vigT += dt;
                if (vigT * 1000 >= FxRules.DmgVigMs) { vigT = -1; Alpha(vig, 0); vig.gameObject.SetActive(false); }
                else DriveVignette();
            }
            if (skfT >= 0)
            {
                skfT += dt;
                if (DungeonClearFx.Spec.FlashDone(skfT * 1000)) { skfT = -1; Alpha(skf, 0); skf.gameObject.SetActive(false); }
                else DriveSkillFlash();
            }
            if (cutT >= 0)
            {
                cutT += dt;
                double k = cutT * 1000 / cutMs;
                if (k >= 1) { cutT = -1; cut.gameObject.SetActive(false); }
                else Alpha(cut, FxRules.SceneCutAlpha(k));
            }
        }
    }
}
