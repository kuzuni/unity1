using System;
using System.Collections.Generic;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// 전투 전리품 레인(정본 `ui.js floatLoot` · `#loot-feed` · T138)과 전투 토스트 레인(`toast(msg, lane)`)의 **수치표·셈** — `Resources/LootFeedUi.json`.
    /// 값은 전부 표에서 온다(§1) · `_rem` = 정본 rem · `_ms` = 벽시계. UnityEngine 참조 0.
    /// </summary>
    public sealed class LootFeedSpec
    {
        public int KeepMax;
        public double LifeMs, RightRem, BottomRem, GapRem, FontRem, PadYRem, PadXRem, RadiusRem, LaneWF, ToastHideMs;
        public RewardBurstSpec.Track LootPop;
        public string CombatLane, CombatBox;

        public static LootFeedSpec From(JsonObject root)
        {
            var L = J.Obj(J.Require(root, "layout"));
            Func<string, double> n = k => J.Num(J.Require(L, k));
            var s = new LootFeedSpec
            {
                KeepMax = J.Int(J.Require(L, "keep_max_n")), LifeMs = n("life_ms"), RightRem = n("right_rem"), BottomRem = n("bottom_rem"), GapRem = n("gap_rem"),
                FontRem = n("font_rem"), PadYRem = n("pad_y_rem"), PadXRem = n("pad_x_rem"), RadiusRem = n("radius_rem"), LaneWF = n("lane_w_f"), ToastHideMs = n("toast_hide_ms"),
            };
            var list = J.List(J.Require(root, "lootpop"), x => J.Obj(x));
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                var k = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException("LootFeedUi 키프레임 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                k.Num["opacity"] = J.Num(J.Require(o, "opacity"));
                k.Num["ty_rem"] = J.Num(J.Require(o, "ty_rem"));
                object e; k.Ease = o.TryGet("ease", out e) ? RewardBurstSpec.EaseOf(e) : CssEase.Linear;
                keys[i] = k;
            }
            if (keys.Length < 2) throw new FormatException("lootpop 키프레임이 둘 미만이다");
            s.LootPop = new RewardBurstSpec.Track { Keys = keys };
            var T = J.Obj(J.Require(root, "text"));
            s.CombatLane = J.Str(J.Require(T, "combat_lane"));
            s.CombatBox = J.Str(J.Require(T, "combat_box"));
            return s;
        }
    }

    /// <summary>
    /// 정본 `floatLoot(text)`(ui.js 1371~1379) 와 `toast(msg, lane)`(1435~1457) 의 규칙: 줄이 **6 을 넘으면**(= 7 이상) 맨 위 한 줄을 버리고 붙인다(그래서 동시에 보이는 최대는 7) ·
    /// 각 줄은 1.6초 `lootpop`(forwards) 뒤 사라진다 · 레인 `combat` 만 전투 상자(모달 아래)로, 나머지는 기본 상자(팝업 위).
    /// </summary>
    public static class LootFeedRules
    {
        /// <summary>붙이기 전에 맨 위를 버려야 하는가 — `children.length > keep_max`.</summary>
        public static bool DropFirst(LootFeedSpec s, int lines) { return lines > s.KeepMax; }

        /// <summary>붙인 뒤 줄 수 — 버림 규칙을 거친 값(상한 keep_max + 1).</summary>
        public static int AfterPush(LootFeedSpec s, int lines) { return (DropFirst(s, lines) ? lines - 1 : lines) + 1; }

        /// <summary>줄이 걷히는 시각(붙인 시각 기준 ms).</summary>
        public static double LifeMs(LootFeedSpec s) { return s.LifeMs; }

        /// <summary>`lootpop` — 불투명도 · translateY(rem) · 퍼센트 0~100(forwards).</summary>
        public static void LootPop(LootFeedSpec s, double percent, out double opacity, out double tyRem)
        {
            opacity = s.LootPop.Sample(percent, "opacity", null);
            tyRem = s.LootPop.Sample(percent, "ty_rem", null);
        }

        /// <summary>토스트가 어느 상자로 가는가 — `lane === 'combat'` 이면 전투 상자.</summary>
        public static bool IsCombatLane(LootFeedSpec s, string lane) { return lane != null && lane == s.CombatLane; }
        /// <summary>전투 상자의 이름(정본 `#toasts-combat`).</summary>
        public static string CombatBoxName(LootFeedSpec s) { return s.CombatBox; }
    }
}
