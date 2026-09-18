using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 소환 확률 팝업(원작 ui.js openSummonRates · stepSummonRates · renderSummonRates · UI-SPEC 48 · shot-042521 · 스킬·펫 공용 — 탈것은 T34 Core 가 서면 같은 자리):
    /// ◀ «레벨 N / 소환 확률» ▶ · i · 등급별 색 막대(0% 등급도 표시 · 별 = 그 라인 승천 횟수) · 안내 문구 · 소환 경험치 게이지(n/5 · MAX).
    /// </summary>
    public static class SkillRatesPopup
    {
        public const string ModalName = "summon-rates";
        static SkillPetSheet sheet;
        static string kind = "skill";
        static int? level;

        public static string KindNow { get { return kind; } }
        public static int LevelNow { get; private set; }
        public static Button PrevButton { get; private set; }
        public static Button NextButton { get; private set; }

        static PetSkillHost H { get { return sheet.Host; } }
        static GameDefs Defs { get { return H.Data.Defs; } }
        static int Max { get { return kind == "mount" ? H.Mounts.Rules.MaxLevel : H.Data.Balance.Skills.MaxLevel; } }
        static int CurLevel { get { return kind == "pet" ? H.Pets.SummonLevel() : kind == "mount" ? H.Mounts.Level() : H.Skills.SummonLevel(); } }

        public static void Open(SkillPetSheet s, string k)
        {
            sheet = s;
            kind = k ?? kind;
            level = null;
            Render();
        }

        /// <summary>
        /// T332 별 둘 — 정본 `.rate-star { … filter: drop-shadow(0 1px 0 rgba(0,0,0,.35)) }`(style.css 4602).
        /// 정본은 별 `<img>` 들과 뒤따르는 수를 **한 `<i>` 로 묶어** 거는데(ui.js 4362~4363) 클론은 그것이 `Image` 여럿 + 글자 하나라
        /// **기구가 갈린다**: 그림은 `ForgeUi.ImageShadow`(메시 오프셋) · 글자는 `UiKit.TextShadow`(TMP 언더레이 · T333).
        /// `UnityEngine.UI.Shadow` 가 `IMeshModifier` 라 TMP 메시를 안 잡기 때문이다(T332 7회차). 값은 둘 다 표 `TextShadowUi.json` 의 같은 키에서 읽는다.
        /// </summary>
        const string StarShadowKey = "rate_star";

        static void StarShadow(Image star)
        {
            ForgeUi.ImageShadow(star, TextShadowUi.Px(StarShadowKey, "dx_px"), TextShadowUi.Px(StarShadowKey, "dy_px"), TextShadowUi.C(StarShadowKey));
        }

        public static void Step(int d)
        {
            int cur = level.HasValue ? level.Value : CurLevel;
            level = Mathf.Clamp(cur + d, 1, Max);
            Render();
        }

        public static void Render()
        {
            int lvl = level.HasValue ? level.Value : CurLevel;
            LevelNow = lvl;
            // 탈것 확률표는 분수(0~1) + needed 필드 — 원작 renderSummonRates 의 isMount 갈래(레벨 상한 MAX_LEVEL 50 · 게이지 = (오픈 − prev)/(need − prev))
            OrderedMap<double> rates = kind == "pet" ? H.Pets.Rates(lvl) : kind == "mount" ? H.Data.Balance.Mounts.SummonAt(Mathf.Clamp(lvl, 1, Max)).Rates : H.Skills.Rates(lvl);
            string line = kind == "pet" ? "pet" : kind == "mount" ? "mount" : "skill";
            int ascN = H.AscendCount(line);
            int cnt = kind == "pet" ? H.Pets.State.PetSummonCount : kind == "mount" ? H.Mounts.State.MountOpens : H.Skills.State.SummonCount;
            bool capped = CurLevel >= Max;
            float gRatio = capped ? 1f : (cnt % 5) / 5f;
            string gText = capped ? PetSkillStyle.T("gauge_max") : PetSkillStyle.T("gauge", cnt % 5, 5);
            if (kind == "mount")
            {
                double? need = H.Mounts.NextNeeded();
                double prev = H.Mounts.PrevNeeded();
                gRatio = need.HasValue ? Mathf.Clamp01((float)((cnt - prev) / (need.Value - prev))) : 1f;
                gText = need.HasValue ? PetSkillStyle.T("gauge", JsNum.ToString(cnt - prev), JsNum.ToString(need.Value - prev)) : PetSkillStyle.T("gauge_max");
            }

            float wf = PetSkillStyle.L("rates_w_f");
            float w = wf * UiKit.RefW;
            float padX = w * PetSkillStyle.L("rates_pad_x_f"), padT = PetSkillStyle.Px("rates_pad_top_rem"), padB = PetSkillStyle.Px("rates_pad_bottom_rem");
            float gap = PetSkillStyle.Px("rates_gap_rem");
            float inner = w - padX * 2f;
            float title = UiCatalog.Instance.Kind(TextKind.Head).size, sub = UiCatalog.Instance.Kind(TextKind.Sub).size;   // T391 ⓑ — 정본 4580 `.rates-head h3 { 1.3rem }` = 47.3px → Head 48(전엔 Title 60)
            float headH = Mathf.Max(PetSkillStyle.Px("tri_h_rem"), title * 1.15f + sub * 1.2f);
            float iH = PetSkillStyle.Px("rates_i_rem");
            float barH = PetSkillStyle.Px("rate_bar_h_rem");
            string[] rar = Defs.Rarities;
            float listH = rar.Length * barH + (rar.Length - 1) * gap;
            float tipMy = PetSkillStyle.Px("rates_tip_my_rem");
            float tipH = sub * 1.4f;
            float progH = PetSkillStyle.Px("rates_prog_h_rem");
            float h = padT + headH + gap + iH + gap + listH + gap + tipMy + tipH + tipMy + progH + padB;
            // T410 — 정본 4575 `.rates-card { width: 74.35% }` 는 **흰 면**(원본 shot-042521 실측 371/499 · 검정 테는 그 바깥)이다.
            //   `Modal.Open` 의 widthFrac 은 테(line3)까지 품은 바깥 상자라 흰 면이 좌우 line 씩(합 1.39%p) 좁았다(런 827 실측 x73~467) →
            //   바깥 상자 = 흰 면 + 2·테 로 열고, 내용 상자는 흰 면에 맞춰 안으로 line 만큼 들인다(x 셈은 그대로 흰 면 기준 · 가운데 50%W 불변).
            float frame = PetSkillKit.Line3;
            PetSkillModal.Handle m = sheet.Modal.Open(ModalName, (w + frame * 2f) / UiKit.RefW, h, PetSkillStyle.L("rates_top_rem"));
            RectTransform c = m.Content;
            c.offsetMin = new Vector2(frame, c.offsetMin.y);
            c.offsetMax = new Vector2(-frame, c.offsetMax.y);
            float y = padT;
            // head
            float tw = PetSkillStyle.Px("tri_w_rem"), th = PetSkillStyle.Px("tri_h_rem"), ti = PetSkillStyle.Px("tri_icon_h");
            PrevButton = TriButton(c, "tri-prev", "tri_left", padX, y + (headH - th) * 0.5f, tw, th, ti, () => Step(-1));
            NextButton = TriButton(c, "tri-next", "tri_right", w - padX - tw, y + (headH - th) * 0.5f, tw, th, ti, () => Step(1));
            TextMeshProUGUI ht = PetSkillKit.Stroked(c, "rates-h3", TextKind.Head, PetSkillStyle.T("rates_level", lvl), PetSkillStyle.C("white"), "sheet_title");   // 정본 .rates-head h3 .11em
            UiKit.Place(ht.rectTransform, padX + tw, y, inner - tw * 2f, title * 1.15f);
            TextMeshProUGUI st = PetSkillKit.Text(c, "rates-sub", TextKind.Sub, PetSkillStyle.T("rates_sub"), PetSkillStyle.C("ink"));
            UiKit.Place(st.rectTransform, padX + tw, y + title * 1.15f, inner - tw * 2f, sub * 1.2f);
            y += headH + gap;
            // i (오른쪽 끝)
            Button ib = UiKit.Button(c, "rates-i", () => PetSkillHost.Say(PetSkillStyle.T("rates_i_toast")));
            RectTransform ir = ib.GetComponent<RectTransform>();
            UiKit.Place(ir, w - padX - iH, y, iH, iH);
            Image disc = PetSkillKit.Disc(ir, "bg", PetSkillStyle.C("ink"));
            UiKit.Fill(disc.rectTransform);
            TextMeshProUGUI it = PetSkillKit.Text(ir, "t", TextKind.Sub, PetSkillStyle.T("info_i"), PetSkillStyle.C("white"));
            LineHeight.Apply(it, "rates_i_lh");   // T354 25회차 — 정본 4648 `.rates-i { line-height: 1 }`
            // T439 — 정본 style.css 4647 `.rates-i { font-weight: 900; font-style: **italic**; font-family: Georgia, serif }`.
            //   `PetSkillKit.Text` 이 이미 900 몫의 Bold 를 준다 — 여기서 **기울임 비트만 더한다**(`|=` 라 굵기가 안 지워진다).
            //   ⚠ 세리프(Georgia)는 **못 낸다** — 이 레포의 글꼴은 NotoSansKR 하나뿐이고(T53) 새 글꼴을 들이는 것은 §1 이 막는다.
            //   그래서 «기울인 산세리프 i» 까지가 이 자리가 낼 수 있는 전부다(정본 세 조건 중 둘).
            //   TMP 가짜 기울임은 획을 기울일 뿐 자폭을 안 늘린다(가짜 굵기의 `boldSpacing` 과 다르다 · T352) — 한 글자라 이웃 자간도 없다.
            it.fontStyle |= FontStyles.Italic;
            UiKit.Fill(it.rectTransform);
            y += iH + gap;
            // rate-list
            float barPad = PetSkillStyle.Px("rate_bar_pad_rem");
            for (int i = 0; i < rar.Length; i++)
            {
                string r = rar[i];
                RectTransform bar = PetSkillKit.Framed(c, "rate-bar-" + r, PetSkillStyle.Rarity(Defs, r), PetSkillStyle.Px("rate_bar_r_rem"), PetSkillKit.Line3);
                UiKit.Place(bar, padX, y, inner, barH);
                // T331 41회차 — 정본 8584 `.rate-bar` 의 드리운 그림자 `0 .08rem .1rem rgba(0,0,0,.22)`(표 ratebar_drop · «막대가 종이 위에 얹힌 판으로») · 틀 안 맨 뒤(면·테 뒤).
                UiShadow.Drop(bar, "ratebar_drop", PetSkillStyle.Px("rate_bar_r_rem"), inner, barH);
                // T178 4회차 — 정본 8577 `.rate-bar { background-image: … }` 등급색 면 위 겹 둘: 에나멜 하이라이트(28% 하드 스톱) + 위 1px 림. 둥근 면이라 Mask 로(3회차 길).
                Image rateFace = bar.Find("face").GetComponent<Image>();
                SurfaceArt.FillMasked(rateFace, "rate-enamel", "rate_bar_enamel", inner - PetSkillKit.Line3 * 2f, barH - PetSkillKit.Line3 * 2f);
                SurfaceArt.FillMasked(rateFace, "rate-rim", "rate_bar_rim", inner - PetSkillKit.Line3 * 2f, barH - PetSkillKit.Line3 * 2f);
                string name = Defs.RarityKr.Get(r, r);
                TextMeshProUGUI nt = PetSkillKit.Text(bar, "rate-name", TextKind.Sub, name, PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
                float nw = PetSkillKit.TextWidth(TextKind.Sub, name);
                UiKit.Place(nt.rectTransform, barPad, 0f, inner * 0.5f, barH);
                if (ascN > 0)
                {
                    float sx = barPad + nw + PetSkillStyle.Rem(0.2f);
                    float ss = sub * 0.8f;
                    int shown = ascN <= 5 ? ascN : 1;
                    for (int k = 0; k < shown; k++)
                    {
                        Image star = UiKit.Icon(bar, "rate-star-" + k, "star");
                        UiKit.Place(star.rectTransform, sx + k * ss, (barH - ss) * 0.5f, ss, ss);
                        StarShadow(star);
                    }
                    if (ascN > 5)
                    {
                        TextMeshProUGUI sn = PetSkillKit.Text(bar, "rate-star-n", TextKind.Sub, ascN.ToString(), PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
                        UiKit.TextShadow(sn, StarShadowKey);   // 정본은 별과 수를 `<i class="rate-star">` **하나로 묶어** 걸므로 수도 같은 그림자를 진다(ui.js 4363)
                        UiKit.Place(sn.rectTransform, sx + ss, 0f, inner * 0.3f, barH);
                    }
                }
                double pct = rates.Get(r, 0);
                TextMeshProUGUI pt = PetSkillKit.Text(bar, "rate-pct", TextKind.Sub, PetSkillStyle.T("rates_pct", JsNum.ToFixed(pct, 2)), PetSkillStyle.C("ink"), TextAlignmentOptions.Right);
                UiKit.Place(pt.rectTransform, inner * 0.5f, 0f, inner * 0.5f - barPad, barH);
                // T352 2회차 — 정본 8635 `.rate-bar { font-variant-numeric: tabular-nums }`: 등급 여섯 줄의 확률이 **세로로 열을 이룬다**.
                //   등폭이 아니면 줄마다 «1» 과 «8» 의 폭이 달라 소수점이 좌우로 흔들린다(정본 주석 «행마다 좌우로 흔들리던 자리»).
                TabularText.Apply(pt);
                y += barH + gap;
            }
            y += tipMy - gap;
            TextMeshProUGUI tip = PetSkillKit.Text(c, "rates-tip", TextKind.Sub, PetSkillStyle.T(kind == "pet" ? "rates_tip_pet" : kind == "mount" ? "rates_tip_mount" : "rates_tip_skill"), PetSkillStyle.C("ink"), TextAlignmentOptions.Center, false);
            UiKit.Place(tip.rectTransform, padX, y, inner, tipH);
            LineHeight.Apply(tip, "rates_tip_lh");   // T354 — 정본 4600 .rates-tip { line-height: 1.4 }
            y += tipH + tipMy;
            float pmx = PetSkillStyle.Px("rates_prog_mx_rem");
            RectTransform prog = PetSkillKit.Gauge(c, "rates-prog", inner - pmx * 2f, progH, gRatio, gText, PetSkillStyle.C("shard_bg"), PetSkillStyle.Px("rates_prog_r_rem"), PetSkillKit.Line3, TextKind.Sub);
            UiKit.Place(prog, padX + pmx, y, inner - pmx * 2f, progH);
            // T352 2회차 — 정본 8635 `.rates-prog span` 도 등폭이다(«12/50» 꼴 숫자가 채워질수록 흔들린다).
            //   게이지 글자는 `PetSkillKit.Gauge` 가 «t» 로 세우는데 그 파일은 T332·T355 lock 이라 **안 연다** — 세워진 글자에 도우미만 건다.
            Transform gaugeLabel = prog.Find("t");
            if (gaugeLabel != null) TabularText.Apply(gaugeLabel.GetComponent<TextMeshProUGUI>());
        }

        static Button TriButton(RectTransform parent, string name, string icon, float x, float y, float w, float h, float iconH, UnityEngine.Events.UnityAction onClick)
        {
            Button b = UiKit.Button(parent, name, onClick);
            RectTransform br = b.GetComponent<RectTransform>();
            UiKit.Place(br, x, y, w, h);
            Image ico = UiKit.Icon(br, "ico", icon, TriTint);
            UiKit.Anchor(ico.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, iconH, iconH);
            return b;
        }

        /// <summary>원작 ui.js `TRI_BLUE = { tint: '#005dff' }` — 아틀라스에 그 tint 변형이 굽혀 있다(T31). 색 값은 표(pp_blue)에서 온다.</summary>
        static string TriTint { get { return "#" + ColorUtility.ToHtmlStringRGB(PetSkillStyle.C("pp_blue")).ToLowerInvariant(); } }
    }
}
