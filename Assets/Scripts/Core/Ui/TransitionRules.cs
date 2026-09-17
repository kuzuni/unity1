using System;
using System.Collections.Generic;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>T454 — 정본 `transition` 한 자리(`Resources/TransitionUi.json` `transitions` 한 칸): 길이(ms) · 타이밍 · 시작 translateY(rem · 아래가 +).</summary>
    public sealed class TransitionSpec
    {
        public string Key;
        /// <summary>전이 길이(ms · 정본 transition 길이).</summary>
        public double Ms;
        public CssEase Ease;
        /// <summary>시작 translateY(rem · 아래가 + · 끝은 언제나 0 = 정본 `transform: none`).</summary>
        public double DyRem;
        public string Note;
    }

    /// <summary>`TransitionUi.json` 의 `transitions` 표 — 키 → <see cref="TransitionSpec"/>. UnityEngine 참조 0.</summary>
    public sealed class TransitionTable
    {
        readonly Dictionary<string, TransitionSpec> map = new Dictionary<string, TransitionSpec>();
        readonly List<string> order = new List<string>();

        public IReadOnlyList<string> Keys { get { return order; } }
        public bool Has(string key) { return map.ContainsKey(key); }

        public TransitionSpec Get(string key)
        {
            TransitionSpec s;
            if (!map.TryGetValue(key, out s)) throw new KeyNotFoundException("TransitionUi: 전이 자리 «" + key + "» 이 표에 없다");
            return s;
        }

        public static TransitionTable From(JsonObject root)
        {
            JsonObject P = J.Obj(J.Require(root, "transitions"));
            TransitionTable t = new TransitionTable();
            foreach (KeyValuePair<string, object> kv in P)
            {
                if (kv.Key == "_") continue;
                JsonObject o = J.Obj(kv.Value);
                if (o == null) throw new FormatException("TransitionUi: «" + kv.Key + "» 은 객체여야 한다");
                TransitionSpec s = new TransitionSpec
                {
                    Key = kv.Key,
                    Ms = J.Num(J.Require(o, "ms")),
                    DyRem = J.Num(J.Require(o, "dy_rem")),
                };
                object e;
                // CSS 는 timing-function 을 안 적으면 `ease` 다 — 표는 그래도 적는다(빈 칸이면 CSS 기본값).
                s.Ease = o.TryGet("ease", out e) ? RewardBurstSpec.EaseOf(e) : RewardBurstSpec.EaseOf("ease");
                object n;
                s.Note = o.TryGet("_", out n) ? n as string : null;
                if (s.Ms <= 0) throw new FormatException("TransitionUi: «" + kv.Key + "» ms 는 0보다 커야 한다");
                t.map[kv.Key] = s;
                t.order.Add(kv.Key);
            }
            if (t.order.Count == 0) throw new FormatException("TransitionUi: transitions 표가 비었다");
            return t;
        }
    }

    /// <summary>T454 — 정본 CSS `transition` 의 셈(순수 함수 · UnityEngine 0): 시작 상태에서 끝 상태(정본 `.show`/`.on`/`none`)로 표의 ms·ease 를 따라 간다.</summary>
    public static class TransitionRules
    {
        /// <summary>진행도 0(시작)~1(끝) — 지난 시간을 표 길이로 나눠 이징한다.</summary>
        public static double Progress(TransitionSpec s, double elapsedMs)
        {
            if (elapsedMs <= 0) return 0;
            double t = elapsedMs / s.Ms;
            if (t >= 1) return 1;
            double k = s.Ease.Ease(t);
            return k < 0 ? 0 : (k > 1 ? 1 : k);
        }

        /// <summary>끝났는가(길이를 다 지났다).</summary>
        public static bool Done(TransitionSpec s, double elapsedMs) { return elapsedMs >= s.Ms; }

        /// <summary>그 진행도에서 «아직 남은 translateY»(rem · 아래가 +) — 시작 dy_rem 에서 0 으로.</summary>
        public static double DyRem(TransitionSpec s, double progress) { return s.DyRem * (1.0 - Clamp01(progress)); }

        /// <summary>그 진행도에서 두 값 사이(a = 시작 · b = 끝) — 손잡이 자리·불투명도가 같은 식이다.</summary>
        public static double Lerp(double a, double b, double progress) { return a + (b - a) * Clamp01(progress); }

        static double Clamp01(double v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }
    }
}
