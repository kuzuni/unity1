using System;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T335 ⓐⓒ — 던전 클리어 팝업의 작은 연출 셋(보상 칸 팝 · 카드 가라앉기 · 딤 페이드)과 기술 노드 «연구 완료» 맥동의 **수치표·셈** —
    /// `Resources/DungeonFxUi.json`(정본 `style.css` 5390~5398 · 4622~4626). 값은 전부 표에서 온다(§1) · UnityEngine 참조 0 ·
    /// 시계는 <see cref="RewardBurstSpec.Track"/>(CSS 키프레임 공용 기계)을 그대로 쓴다(T135 <see cref="CardPopSpec"/> 과 같은 꼴).
    /// </summary>
    public sealed class DungeonFxSpec
    {
        /// <summary>칸 팝 한 번(ms · 정본 .38s).</summary>
        public double PopMs;
        /// <summary>칸마다 늦는 간격(ms · 정본 nth-child .09s).</summary>
        public double PopStaggerMs;
        /// <summary>[보상 수령] 뒤 카드가 가라앉기 시작하기까지(ms · 정본 animation-delay .12s).</summary>
        public double SinkDelayMs;
        /// <summary>가라앉기 길이(ms · 정본 .45s).</summary>
        public double SinkMs;
        /// <summary>딤이 걷히는 길이(ms · 정본 transition .55s).</summary>
        public double DimOutMs;
        /// <summary>«연구 완료» 맥동 반 주기(ms · 정본 1.1s alternate).</summary>
        public double ReadyMs;
        /// <summary>스킬 시전 섬광 한 번(정본 skflash .5s).</summary>
        public double FlashMs;
        /// <summary>섬광 방사형: 가운데 알파(정본 `${color}33` = .2) · 투명이 되는 반지름(정본 65% · farthest-corner 기준) · 중심.</summary>
        public double FlashCoreAlpha, FlashEdge, FlashCx, FlashCy;
        public string ReadyFromKey, ReadyToKey;
        public CssEase ReadyEase;
        public RewardBurstSpec.Track Pop, Sink, Dim, Flash;

        public static DungeonFxSpec From(JsonObject root)
        {
            JsonObject L = J.Obj(J.Require(root, "layout"));
            DungeonFxSpec s = new DungeonFxSpec
            {
                PopMs = J.Num(J.Require(L, "pop_ms")),
                PopStaggerMs = J.Num(J.Require(L, "pop_stagger_ms")),
                SinkDelayMs = J.Num(J.Require(L, "sink_delay_ms")),
                SinkMs = J.Num(J.Require(L, "sink_ms")),
                DimOutMs = J.Num(J.Require(L, "dim_out_ms")),
                ReadyMs = J.Num(J.Require(L, "ready_ms")),
                FlashMs = J.Num(J.Require(L, "flash_ms")),
            };
            if (s.PopMs <= 0 || s.SinkMs <= 0 || s.DimOutMs <= 0 || s.ReadyMs <= 0 || s.FlashMs <= 0) throw new FormatException("DungeonFxUi: 길이(ms)는 0보다 커야 한다");
            if (s.PopStaggerMs < 0 || s.SinkDelayMs < 0) throw new FormatException("DungeonFxUi: 지연(ms)은 음수일 수 없다");
            s.Pop = Track(root, "dgcpop");
            s.Sink = Track(root, "dgsink");
            s.Dim = Track(root, "dimout");
            s.Flash = Track(root, "skflash");
            JsonObject g = J.Obj(J.Require(root, "skflash_glow"));
            s.FlashCoreAlpha = J.Num(J.Require(g, "core_alpha"));
            s.FlashEdge = J.Num(J.Require(g, "edge"));
            s.FlashCx = J.Num(J.Require(g, "cx"));
            s.FlashCy = J.Num(J.Require(g, "cy"));
            if (s.FlashCoreAlpha < 0 || s.FlashCoreAlpha > 1 || s.FlashEdge <= 0 || s.FlashEdge > 1) throw new FormatException("DungeonFxUi: skflash_glow 의 core_alpha 는 0~1 · edge 는 0 초과 1 이하여야 한다");
            JsonObject r = J.Obj(J.Require(root, "ttready"));
            s.ReadyFromKey = J.Str(J.Require(r, "from_key"));
            s.ReadyToKey = J.Str(J.Require(r, "to_key"));
            object e;
            s.ReadyEase = r.TryGet("ease", out e) ? RewardBurstSpec.EaseOf(e) : CssEase.Linear;
            return s;
        }

        /// <summary>키프레임 배열 → 공용 트랙(퍼센트 오름차순 · 칸 `scale`·`alpha` 둘 다 있어야 한다).</summary>
        static RewardBurstSpec.Track Track(JsonObject root, string name)
        {
            var list = J.List(J.Require(root, name), x => J.Obj(x));
            if (list.Count < 2) throw new FormatException("DungeonFxUi: " + name + " 키프레임이 둘 미만이다");
            var keys = new RewardBurstSpec.KeyStop[list.Count];
            double prev = -1;
            for (int i = 0; i < list.Count; i++)
            {
                JsonObject o = list[i];
                var k = new RewardBurstSpec.KeyStop { At = J.Num(J.Require(o, "at")) };
                if (k.At < prev) throw new FormatException("DungeonFxUi: " + name + " 퍼센트는 오름차순이어야 한다");
                prev = k.At;
                k.Num["scale"] = J.Num(J.Require(o, "scale"));
                k.Num["alpha"] = J.Num(J.Require(o, "alpha"));
                if (k.Num["scale"] <= 0 || k.Num["alpha"] < 0 || k.Num["alpha"] > 1) throw new FormatException("DungeonFxUi: " + name + " 의 scale 은 양수 · alpha 는 0~1 이어야 한다");
                object e;
                k.Ease = o.TryGet("ease", out e) ? RewardBurstSpec.EaseOf(e) : CssEase.Linear;
                keys[i] = k;
            }
            return new RewardBurstSpec.Track { Keys = keys };
        }

        static double Pct(double ms, double delay, double dur)
        {
            double t = ms - delay;
            if (t <= 0) return 0;
            if (t >= dur) return 100;
            return t / dur * 100;
        }

        // ---- 칸 팝(정본 backwards · 시작 전에도 from 모습 · 끝나면 원래 모습) ----
        public double PopPercent(double ms, int index) { return Pct(ms, index * PopStaggerMs, PopMs); }
        public double PopScaleAt(double ms, int index) { return Pop.Sample(PopPercent(ms, index), "scale", null); }
        public double PopAlphaAt(double ms, int index) { return Pop.Sample(PopPercent(ms, index), "alpha", null); }
        /// <summary>칸 count 개의 팝이 전부 끝났는가(마지막 칸의 지연 + 한 번 길이).</summary>
        public bool PopDone(double ms, int count) { return ms >= Math.Max(0, count - 1) * PopStaggerMs + PopMs; }

        // ---- 가라앉기(정본 forwards · 지연 동안은 원래 모습 · 끝값에 머문다) + 딤 페이드 ----
        public double SinkPercent(double ms) { return Pct(ms, SinkDelayMs, SinkMs); }
        public double SinkScaleAt(double ms) { return Sink.Sample(SinkPercent(ms), "scale", null); }
        public double SinkAlphaAt(double ms) { return Sink.Sample(SinkPercent(ms), "alpha", null); }
        public double DimPercent(double ms) { return Pct(ms, 0, DimOutMs); }
        /// <summary>딤 알파 배율(1 → 0) — 시작 알파에 곱한다.</summary>
        public double DimFactorAt(double ms) { return Dim.Sample(DimPercent(ms), "alpha", null); }
        /// <summary>카드도 딤도 끝났는가 — 그때 팝업 뿌리를 걷는다(정본 setTimeout → hidden).</summary>
        public bool LeaveDone(double ms) { return ms >= Math.Max(SinkDelayMs + SinkMs, DimOutMs); }

        // ---- 스킬 시전 섬광(정본 skflash .5s ease-out · 0%→25% 켜지고 100% 에 꺼진다 · 연타는 처음부터) ----
        public double FlashPercent(double ms) { return Pct(ms, 0, FlashMs); }
        public double FlashAlphaAt(double ms) { return Flash.Sample(FlashPercent(ms), "alpha", null); }
        public bool FlashDone(double ms) { return ms >= FlashMs; }
        /// <summary>
        /// 방사형 한 장의 알파 — u·v 는 상자 정규 좌표(0~1 · 위가 0). 정본 `radial-gradient(ellipse at center, c33 0%, transparent 65%)`:
        /// 타원의 100% 는 **가장 먼 모서리**(CSS 기본 farthest-corner)라 중심 기준 반지름을 그 모서리로 나눈다. 투명으로 가는 보간은 알파만 줄인다.
        /// </summary>
        public double FlashGlowAlpha(double u, double v)
        {
            double rx = Math.Max(FlashCx, 1 - FlashCx), ry = Math.Max(FlashCy, 1 - FlashCy);
            double dx = (u - FlashCx) / rx, dy = (v - FlashCy) / ry;
            double r = Math.Sqrt(dx * dx + dy * dy);
            if (r >= FlashEdge) return 0;
            return FlashCoreAlpha * (1 - r / FlashEdge);
        }

        // ---- «연구 완료» 맥동(정본 infinite alternate · 반 주기 ReadyMs · ease-in-out) ----
        /// <summary>0(from 색) ~ 1(to 색) — 벽시계 ms 를 왕복 한 번(2·ReadyMs)에 접어 구간 이징을 건다.</summary>
        public double ReadyT(double ms)
        {
            if (ms < 0) ms = 0;
            double k = (ms / ReadyMs) % 2.0;
            double t = k <= 1 ? k : 2 - k;
            return ReadyEase.Ease(t);
        }
    }
}
