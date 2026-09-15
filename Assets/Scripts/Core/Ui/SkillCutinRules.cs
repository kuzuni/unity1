using System;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T384 — 정본 `#skill-cutin`(style.css 1897~1919 · `@keyframes cutin .8s ease-out forwards`)의 셈. 값은 전부 표(`SkillCutinUi.json`)에서 온다 — UnityEngine 0.
    /// CSS 키프레임은 **속성마다 따로** 보간되고 타이밍 함수는 **구간마다** 다시 걸린다 — 그래서 불투명도·배율·상승을 세 트랙으로 나눠 <see cref="CssTrack.SampleEased"/> 로 읽는다.
    /// </summary>
    public sealed class SkillCutinSpec
    {
        public double TopF, FontRem, LsEm, IconEm, IconGapEm, RiseRem;
        public double DurMs, TextBlurPx, IconBlurPx;
        public CssEase Ease;
        public CssTrack Opacity, Scale, Rise;

        public static SkillCutinSpec From(JsonObject root)
        {
            if (root == null) throw new ArgumentNullException("root");
            JsonObject lay = J.Obj(J.Require(root, "layout")), anim = J.Obj(J.Require(root, "anim")), glow = J.Obj(J.Require(root, "glow"));
            var s = new SkillCutinSpec
            {
                TopF = J.Num(J.Require(lay, "top_f")),
                FontRem = J.Num(J.Require(lay, "font_rem")),
                LsEm = J.Num(J.Require(lay, "skill_cutin_ls_em")),
                IconEm = J.Num(J.Require(lay, "icon_em")),
                IconGapEm = J.Num(J.Require(lay, "icon_gap_em")),
                RiseRem = J.Num(J.Require(lay, "rise_rem")),
                DurMs = J.Num(J.Require(anim, "dur_ms")),
                Ease = RewardBurstSpec.EaseOf(J.Require(anim, "ease")),
                Opacity = Track(anim, "opacity"),
                Scale = Track(anim, "scale"),
                Rise = Track(anim, "rise_f"),
                TextBlurPx = J.Num(J.Require(glow, "text_blur_px")),
                IconBlurPx = J.Num(J.Require(glow, "icon_blur_px")),
            };
            if (s.DurMs <= 0) throw new FormatException("SkillCutinUi anim.dur_ms 는 0보다 커야 한다");
            if (s.TopF < 0 || s.TopF > 1) throw new FormatException("SkillCutinUi layout.top_f 는 0~1 비율이다");
            return s;
        }

        /// <summary>`[{at, v}, …]` 한 채널 키프레임 → 트랙. 퍼센트는 오름차순.</summary>
        static CssTrack Track(JsonObject anim, string name)
        {
            var list = J.List(J.Require(anim, name), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("SkillCutinUi anim." + name + " 키프레임이 둘 미만이다");
            var stops = new double[list.Count];
            var vals = new double[list.Count][];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                stops[i] = J.Num(J.Require(list[i], "at"));
                if (stops[i] < prev) throw new FormatException("SkillCutinUi anim." + name + " 퍼센트는 오름차순이어야 한다");
                prev = stops[i];
                vals[i] = new[] { J.Num(J.Require(list[i], "v")) };
            }
            return new CssTrack(stops, vals);
        }

        /// <summary>시작 뒤 <paramref name="ms"/> 에서의 불투명도 · 배율 · 상승(rem). 끝(≥ dur)에서는 마지막 키 값(`forwards`).</summary>
        public void Sample(double ms, out double alpha, out double scale, out double riseRem)
        {
            double pct = ms <= 0 ? 0 : ms >= DurMs ? 100 : ms / DurMs * 100.0;
            var one = new double[1];
            Opacity.SampleEased(pct, Ease, one); alpha = one[0];
            Scale.SampleEased(pct, Ease, one); scale = one[0];
            Rise.SampleEased(pct, Ease, one); riseRem = one[0] * RiseRem;
        }

        public bool Done(double ms) { return ms >= DurMs; }
    }
}
