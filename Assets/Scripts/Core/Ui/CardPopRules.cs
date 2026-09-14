using System;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 모달 열림 카드 팝(T135 ⓑ · 정본 `style.css` 1758~1759 `cardpop`)의 **수치표·셈** — `Resources/CardPopUi.json`.
    /// 정본은 모달을 **처음** 열 때만 `.opening` 을 붙여 카드를 scale .7·α 0 에서 1·1 로 .25s ease-out 으로 키우고,
    /// 열린 채 다시 그리는(`ui.js` 1156) 재호출엔 안 붙인다. 값은 전부 표에서 온다(§1) · UnityEngine 참조 0 ·
    /// 시계는 <see cref="RewardBurstSpec.Track"/>(CSS 키프레임 공용 기계)을 그대로 쓴다.
    /// </summary>
    public sealed class CardPopSpec
    {
        /// <summary>한 번 도는 길이(ms · 정본 `.25s`).</summary>
        public double DurationMs;
        /// <summary>`opening` 이 붙어 있는 시간(ms · 정본 300) — 재호출이 팝을 다시 안 여는 창.</summary>
        public double HoldMs;
        public RewardBurstSpec.Track Pop;

        public static CardPopSpec From(JsonObject root)
        {
            JsonObject L = J.Obj(J.Require(root, "layout"));
            CardPopSpec s = new CardPopSpec
            {
                DurationMs = J.Num(J.Require(L, "duration_ms")),
                HoldMs = J.Num(J.Require(L, "opening_hold_ms")),
            };
            if (s.DurationMs <= 0) throw new FormatException("CardPopUi: duration_ms 는 0보다 커야 한다");
            if (s.HoldMs < s.DurationMs) throw new FormatException("CardPopUi: opening_hold_ms 는 duration_ms 이상이어야 한다(정본 300 ≥ 250 — 팝이 끝나기 전에 창이 닫히면 재렌더가 다시 튄다)");
            s.Pop = Track(root, "cardpop");
            return s;
        }

        /// <summary>키프레임 배열 → 공용 트랙(퍼센트 오름차순 · 칸 `scale`·`alpha` 둘 다 있어야 한다).</summary>
        static RewardBurstSpec.Track Track(JsonObject root, string name)
        {
            var list = J.List(J.Require(root, name), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("CardPopUi: " + name + " 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject o = list[i];
                var k = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException("CardPopUi: " + name + " 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                k.Num["scale"] = J.Num(J.Require(o, "scale"));
                k.Num["alpha"] = J.Num(J.Require(o, "alpha"));
                if (k.Num["scale"] <= 0 || k.Num["alpha"] < 0 || k.Num["alpha"] > 1) throw new FormatException("CardPopUi: " + name + " 의 scale 은 양수 · alpha 는 0~1 이어야 한다");
                object e;
                k.Ease = o.TryGet("ease", out e) ? RewardBurstSpec.EaseOf(e) : CssEase.Linear;
                keys[i] = k;
            }
            return new RewardBurstSpec.Track { Keys = keys };
        }

        /// <summary>한 번만 도는 애니메이션 — 벽시계 ms 를 0~100 으로(끝나면 100 에 머문다 · `animation-fill-mode` 기본이지만 마지막 키가 «없음» 상태와 같다).</summary>
        public double Percent(double ms)
        {
            if (ms <= 0) return 0;
            if (ms >= DurationMs) return 100;
            return ms / DurationMs * 100;
        }

        public double ScaleAt(double ms) { return Pop.Sample(Percent(ms), "scale", null); }
        public double AlphaAt(double ms) { return Pop.Sample(Percent(ms), "alpha", null); }
        /// <summary>팝이 끝났는가(정본 `animation` 한 번 · 그 뒤 카드는 원래 모습).</summary>
        public bool Done(double ms) { return ms >= DurationMs; }
        /// <summary>«처음 열림» 창 안인가 — 이 안의 재호출은 팝을 다시 열지 않는다(정본 1156·1247).</summary>
        public bool Opening(double ms) { return ms < HoldMs; }
    }
}
