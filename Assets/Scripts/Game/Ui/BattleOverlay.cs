using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forge.Core.BattleFx;

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

        RectTransform layer;
        // boss warning
        RectTransform warnRoot; Image dim, flash, bannerBg; RectTransform banner; TextMeshProUGUI marquee; RectTransform track; TextMeshProUGUI sub; Image[] hazardDashes;
        double warnT = -1, warnDur = FxRules.BossWarnDur; float trackW;
        // death
        RectTransform deathRoot; Image cover; TextMeshProUGUI title; Image rule; TextMeshProUGUI dsub; RectTransform bannerGroup;
        double deathT = -1;
        // scene cut
        Image cut; double cutT = -1, cutMs;
        // 피격 붉은 비네트(T135 ⓐ)
        Image vig; double vigT = -1, vigPeak;

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
            Instance = o;
            return o;
        }

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
            dim = Full(warnRoot, "bw-dim", "modal_dim_deep", 0);
            flash = Full(warnRoot, "bw-flash", "pp_red", 0);
            banner = UiKit.Box(warnRoot, "bw-banner");
            float hazard = 0.5f * rem, pad = 0.3f * rem, textH = 1.5f * rem * 1.2f;
            float bannerH = hazard * 2 + pad * 2 + textH;
            banner.anchorMin = new Vector2(0, 1 - (float)FxRules.WarnTop); banner.anchorMax = new Vector2(1, 1 - (float)FxRules.WarnTop);
            banner.pivot = new Vector2(0.5f, 0.5f); banner.sizeDelta = new Vector2(0, bannerH); banner.anchoredPosition = Vector2.zero;
            bannerBg = UiKit.Panel(banner, "bg", "pp_red_dk"); UiKit.Fill(bannerBg.rectTransform); bannerBg.raycastTarget = false;
            hazardDashes = new Image[24];
            for (int row = 0; row < 2; row++)
            {
                Image band = UiKit.Panel(banner, "bw-hazard" + row, "coin");
                band.raycastTarget = false;
                var brt = band.rectTransform;
                brt.anchorMin = new Vector2(0, row == 0 ? 1 : 0); brt.anchorMax = new Vector2(1, row == 0 ? 1 : 0);
                brt.pivot = new Vector2(0.5f, row == 0 ? 1 : 0); brt.sizeDelta = new Vector2(0, hazard); brt.anchoredPosition = Vector2.zero;
                for (int i = 0; i < 12; i++)
                {
                    Image dash = UiKit.Panel(band.transform, "dash" + i, "topbar_bg");
                    dash.raycastTarget = false;
                    var drt = dash.rectTransform;
                    drt.anchorMin = new Vector2(0, 0); drt.anchorMax = new Vector2(0, 1); drt.pivot = new Vector2(0, 0.5f);
                    drt.sizeDelta = new Vector2(0.55f * rem, 0); drt.anchoredPosition = new Vector2(i * 1.1f * rem, 0);
                    hazardDashes[row * 12 + i] = dash;
                }
            }
            RectTransform mq = UiKit.Box(banner, "bw-marquee");
            mq.anchorMin = new Vector2(0, 0); mq.anchorMax = new Vector2(1, 1); mq.offsetMin = new Vector2(0, hazard); mq.offsetMax = new Vector2(0, -hazard);
            var mask = mq.gameObject.AddComponent<RectMask2D>();
            track = UiKit.Box(mq, "bw-track");
            track.anchorMin = new Vector2(0, 0.5f); track.anchorMax = new Vector2(0, 0.5f); track.pivot = new Vector2(0, 0.5f);
            marquee = UiKit.Text(track, "text", TextKind.Title, FxRules.WarnText + FxRules.WarnText + FxRules.WarnText, "pp_paper", TextAlignmentOptions.Left);
            marquee.enableWordWrapping = false;
            marquee.overflowMode = TextOverflowModes.Overflow;
            marquee.raycastTarget = false;
            // T168 2회차 — 정본 style.css 402 `.bw-track span { letter-spacing: .14em }`. 왼쪽 정렬이라 되밀기는 없다.
            LetterSpacing.Apply(marquee, "bw_track_ls_em");
            UiKit.Outline(marquee, "pp_red", 0.2f);
            var trt = marquee.rectTransform;
            trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(0, 0.5f); trt.pivot = new Vector2(0, 0.5f); trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(UiKit.RefW * 6, textH);
            trackW = 0;
            sub = UiKit.Text(warnRoot, "bw-sub", TextKind.Body, FxRules.WarnSub, "coin", TextAlignmentOptions.Center);
            sub.raycastTarget = false;
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
            Alpha(dim, FxRules.WarnDim(u));
            Alpha(flash, FxRules.WarnFlash(warnT) * 0.62);
            banner.localScale = new Vector3(1, (float)FxRules.WarnBanner(u), 1);
            Alpha(sub, FxRules.WarnSubAlpha(u));
            if (trackW <= 0 && marquee != null) trackW = marquee.preferredWidth / 3f;
            float w = trackW > 0 ? trackW : UiKit.RefW;
            float off = (float)((warnT / FxRules.WarnScrollPeriod) % 1.0) * w;
            track.anchoredPosition = new Vector2(-off, 0);
            float hz = (float)((warnT / FxRules.WarnHazardPeriod) % 1.0) * 1.1f * Rem;
            if (hazardDashes != null)
                for (int i = 0; i < hazardDashes.Length; i++) { int col = i % 12; hazardDashes[i].rectTransform.anchoredPosition = new Vector2(col * 1.1f * Rem + hz - 1.1f * Rem, 0); }
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
