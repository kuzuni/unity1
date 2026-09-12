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
            float title = UiCatalog.Instance.Kind(TextKind.Title).size, sub = UiCatalog.Instance.Kind(TextKind.Sub).size;
            float headH = Mathf.Max(PetSkillStyle.Px("tri_h_rem"), title * 1.15f + sub * 1.2f);
            float iH = PetSkillStyle.Px("rates_i_rem");
            float barH = PetSkillStyle.Px("rate_bar_h_rem");
            string[] rar = Defs.Rarities;
            float listH = rar.Length * barH + (rar.Length - 1) * gap;
            float tipMy = PetSkillStyle.Px("rates_tip_my_rem");
            float tipH = sub * 1.4f;
            float progH = PetSkillStyle.Px("rates_prog_h_rem");
            float h = padT + headH + gap + iH + gap + listH + gap + tipMy + tipH + tipMy + progH + padB;
            PetSkillModal.Handle m = sheet.Modal.Open(ModalName, wf, h, PetSkillStyle.L("rates_top_rem"));
            RectTransform c = m.Content;
            float y = padT;
            // head
            float tw = PetSkillStyle.Px("tri_w_rem"), th = PetSkillStyle.Px("tri_h_rem"), ti = PetSkillStyle.Px("tri_icon_h");
            PrevButton = TriButton(c, "tri-prev", "tri_left", padX, y + (headH - th) * 0.5f, tw, th, ti, () => Step(-1));
            NextButton = TriButton(c, "tri-next", "tri_right", w - padX - tw, y + (headH - th) * 0.5f, tw, th, ti, () => Step(1));
            TextMeshProUGUI ht = PetSkillKit.Stroked(c, "rates-h3", TextKind.Title, PetSkillStyle.T("rates_level", lvl), PetSkillStyle.C("white"), 0.3f);
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
            UiKit.Fill(it.rectTransform);
            y += iH + gap;
            // rate-list
            float barPad = PetSkillStyle.Px("rate_bar_pad_rem");
            for (int i = 0; i < rar.Length; i++)
            {
                string r = rar[i];
                RectTransform bar = PetSkillKit.Framed(c, "rate-bar-" + r, PetSkillStyle.Rarity(Defs, r), PetSkillStyle.Px("rate_bar_r_rem"), PetSkillKit.Line3);
                UiKit.Place(bar, padX, y, inner, barH);
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
                    }
                    if (ascN > 5)
                    {
                        TextMeshProUGUI sn = PetSkillKit.Text(bar, "rate-star-n", TextKind.Sub, ascN.ToString(), PetSkillStyle.C("ink"), TextAlignmentOptions.Left);
                        UiKit.Place(sn.rectTransform, sx + ss, 0f, inner * 0.3f, barH);
                    }
                }
                double pct = rates.Get(r, 0);
                TextMeshProUGUI pt = PetSkillKit.Text(bar, "rate-pct", TextKind.Sub, PetSkillStyle.T("rates_pct", JsNum.ToFixed(pct, 2)), PetSkillStyle.C("ink"), TextAlignmentOptions.Right);
                UiKit.Place(pt.rectTransform, inner * 0.5f, 0f, inner * 0.5f - barPad, barH);
                y += barH + gap;
            }
            y += tipMy - gap;
            TextMeshProUGUI tip = PetSkillKit.Text(c, "rates-tip", TextKind.Sub, PetSkillStyle.T(kind == "pet" ? "rates_tip_pet" : kind == "mount" ? "rates_tip_mount" : "rates_tip_skill"), PetSkillStyle.C("ink"), TextAlignmentOptions.Center, false);
            UiKit.Place(tip.rectTransform, padX, y, inner, tipH);
            y += tipH + tipMy;
            float pmx = PetSkillStyle.Px("rates_prog_mx_rem");
            RectTransform prog = PetSkillKit.Gauge(c, "rates-prog", inner - pmx * 2f, progH, gRatio, gText, PetSkillStyle.C("shard_bg"), PetSkillStyle.Px("rates_prog_r_rem"), PetSkillKit.Line3, TextKind.Sub);
            UiKit.Place(prog, padX + pmx, y, inner - pmx * 2f, progH);
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
