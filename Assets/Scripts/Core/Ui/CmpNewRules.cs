using System;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 제작 비교 팝업의 «새로운!» 카드 강조(T135 ⓒ · 정본 `style.css` 1833~1846)의 **수치표·셈** — `Resources/CmpNewUi.json`.
    /// 정본은 둘을 겹쳐 돌린다: ⓐ `newpulse` 1.3s — 카드 테두리 글로우의 **번짐 반지름**만 6px ↔ 16px 로 숨쉰다(색은 희귀도색).
    /// ⓑ `shinesweep` 1.7s — 카드 폭 45%·높이 220%·15° 기운 흰 띠가 왼쪽(−80%) 밖에서 오른쪽(130%) 밖으로 쓸고 **55~100% 는 멈춰 쉰다**.
    /// 값은 전부 표에서 온다(§1) · UnityEngine 참조 0 · 시계는 `RewardBurstSpec.Track`(CSS 키프레임 공용 기계)을 그대로 쓴다.
    /// </summary>
    public sealed class CmpNewSpec
    {
        /// <summary>한 바퀴(ms) — 둘은 주기가 달라 따로 돈다(정본도 애니메이션 둘이다).</summary>
        public double PulseMs, SweepMs;
        /// <summary>띠 기하 — 카드 크기 분수와 각(정본 css 1835~1837).</summary>
        public double SweepWF, SweepHF, SweepTopF, SweepRotDeg, SweepAngleDeg, SweepPeakA;
        public RewardBurstSpec.Track Pulse, Sweep;

        public static CmpNewSpec From(JsonObject root)
        {
            JsonObject L = J.Obj(J.Require(root, "layout"));
            Func<string, double> n = k => J.Num(J.Require(L, k));
            CmpNewSpec s = new CmpNewSpec
            {
                PulseMs = n("pulse_ms"),
                SweepMs = n("sweep_ms"),
                SweepWF = n("sweep_w_f"),
                SweepHF = n("sweep_h_f"),
                SweepTopF = n("sweep_top_f"),
                SweepRotDeg = n("sweep_rot_deg"),
                SweepAngleDeg = n("sweep_angle_deg"),
                SweepPeakA = n("sweep_peak_a"),
            };
            if (s.PulseMs <= 0 || s.SweepMs <= 0) throw new FormatException("CmpNewUi: 주기는 0보다 커야 한다");
            s.Pulse = Track(root, "newpulse", "glow_px");
            s.Sweep = Track(root, "shinesweep", "left_f");
            return s;
        }

        /// <summary>키프레임 배열 하나 → 공용 트랙(퍼센트 오름차순 · 칸 이름 하나).</summary>
        static RewardBurstSpec.Track Track(JsonObject root, string name, string field)
        {
            var list = J.List(J.Require(root, name), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("CmpNewUi: " + name + " 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject o = list[i];
                var k = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException("CmpNewUi: " + name + " 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                k.Num[field] = J.Num(J.Require(o, field));
                object e;
                k.Ease = o.TryGet("ease", out e) ? RewardBurstSpec.EaseOf(e) : CssEase.Linear;
                keys[i] = k;
            }
            return new RewardBurstSpec.Track { Keys = keys };
        }

        /// <summary>`infinite` — 벽시계 ms 를 한 바퀴 안 퍼센트(0~100)로.</summary>
        public static double Percent(double ms, double periodMs)
        {
            if (periodMs <= 0) return 0;
            double u = ms / periodMs;
            u -= Math.Floor(u);
            return u * 100;
        }

        /// <summary>`box-shadow: 0 0 Npx` 의 N — 정본 CSS px(호출부가 앱 배율로 환산한다).</summary>
        public double GlowPx(double ms) { return Pulse.Sample(Percent(ms, PulseMs), "glow_px", null); }

        /// <summary>띠의 `left` — 카드 폭 분수(−0.8 = 왼쪽 밖 · 1.3 = 오른쪽 밖).</summary>
        public double SweepLeftF(double ms) { return Sweep.Sample(Percent(ms, SweepMs), "left_f", null); }

        /// <summary>쓸고 난 뒤 «멈춰 쉬는» 구간인가(정본 55~100%) — 마지막 두 키가 같은 값이면 그 구간이다.</summary>
        public bool SweepResting(double ms)
        {
            int last = Sweep.Keys.Length - 1;
            double at = Sweep.Keys[last - 1].At;
            return Percent(ms, SweepMs) >= at
                && Math.Abs(Sweep.Keys[last].Num["left_f"] - Sweep.Keys[last - 1].Num["left_f"]) < 1e-9;
        }
    }
}
