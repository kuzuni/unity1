using System;
using System.Collections.Generic;
using Forge.Core.CraftFx;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>눌림 피드백 한 자리(T355 · `Resources/PressFxUi.json` `press` 한 칸) — 정본 `:active { translateY(dy) scale(s); filter: brightness(b) }` + `transition: transform ms ease`.</summary>
    public sealed class PressSpec
    {
        public string Key;
        /// <summary>눌렸을 때 아래로 미는 거리(rem · 정본 translateY · 아래가 +).</summary>
        public double DyRem;
        /// <summary>눌렸을 때 배율(정본 scale · 없으면 1).</summary>
        public double Scale;
        /// <summary>눌렸을 때 밝기 배율(정본 filter: brightness · 없으면 1).</summary>
        public double Brightness;
        /// <summary>
        /// 오가는 시간(ms · 정본 transition 길이 · 눌림·뗌 같다). **0 이면 즉시**다 —
        /// T515 가 센 열두 자리엔 정본에 `transition` 이 **없다**(CSS 에서 transition 없는 속성 변화는 한 프레임에 끝난다).
        /// </summary>
        public double Ms;
        /// <summary>
        /// T515 — 눌림에 **아래턱을 걷는가**(정본 `:active { box-shadow: none }`). 정본의 턱은 `inset 0 -.22rem 0` 그늘이라
        /// 그것이 사라지면 **면이 그 몫만큼 아래로 자란다** — 거는 쪽이 턱 그림을 끄고 면·글자를 내린다.
        /// ⚠ 이 갈래엔 `transition` 이 없어 **이산**이다(위상이 0 보다 크면 걷힌 모습 · 0 이면 제 모습).
        /// </summary>
        public bool LipOff;
        /// <summary>
        /// T515 5회차 — 눌림에 **면 색을 납작하게 갈아입는가**(정본 4802 `.af-dd-list button:active { background: #2c313a }`).
        /// 밝기 채널(<see cref="Brightness"/>)과 **다른 물건**이다: 그쪽은 제 색에 곱하기고 이쪽은 **딴 색으로 교체**다 —
        /// 정본이 `filter: brightness()` 와 `background:` 를 따로 쓰는 것과 같다. 갈아입을 색은 자리마다 달라
        /// **부르는 쪽**이 준다(`faceKey`·`lipKey` 와 같은 길 · 값은 `Resources/PinnedColorUi.json` 이 쥐고
        /// `tools/check_pinned_colors.py` 가 정본 리터럴과 같은지 지킨다) — 이 칸은 «그 자리가 갈아입는가» 만 쥔다.
        /// ⚠ 그 선언엔 `transition` 이 없어 **이산**이다(표 `ms: 0`).
        /// </summary>
        public bool FaceSwap;
        public CssEase Ease;
        public string Note;
    }

    /// <summary>`PressFxUi.json` 의 `press` 표 — 키 → <see cref="PressSpec"/>. UnityEngine 참조 0.</summary>
    public sealed class PressTable
    {
        readonly Dictionary<string, PressSpec> map = new Dictionary<string, PressSpec>();
        readonly List<string> order = new List<string>();

        public IReadOnlyList<string> Keys { get { return order; } }
        public bool Has(string key) { return map.ContainsKey(key); }

        public PressSpec Get(string key)
        {
            PressSpec s;
            if (!map.TryGetValue(key, out s)) throw new KeyNotFoundException("PressFxUi: 눌림 자리 «" + key + "» 이 표에 없다");
            return s;
        }

        public static PressTable From(JsonObject root)
        {
            JsonObject P = J.Obj(J.Require(root, "press"));
            PressTable t = new PressTable();
            foreach (KeyValuePair<string, object> kv in P)
            {
                if (kv.Key == "_") continue;
                JsonObject o = J.Obj(kv.Value);
                if (o == null) throw new FormatException("PressFxUi: «" + kv.Key + "» 은 객체여야 한다");
                PressSpec s = new PressSpec
                {
                    Key = kv.Key,
                    DyRem = J.Num(J.Require(o, "dy_rem")),
                    Scale = J.Num(J.Require(o, "scale")),
                    Brightness = J.Num(J.Require(o, "brightness")),
                    Ms = J.Num(J.Require(o, "ms")),
                };
                object lo;
                s.LipOff = o.TryGet("lip_off", out lo) && lo is bool && (bool)lo;
                object fsw;
                s.FaceSwap = o.TryGet("face_swap", out fsw) && fsw is bool && (bool)fsw;
                object e;
                s.Ease = o.TryGet("ease", out e) ? RewardBurstSpec.EaseOf(e) : CssEase.Linear;
                object n;
                s.Note = o.TryGet("_", out n) ? n as string : null;
                // T515 — 종전엔 `ms <= 0` 을 막았다. 그런데 정본이 센 `:active` 열두 자리엔 `transition` 이 **없다** —
                //   그 자리를 표에 적으려면 «즉시»(ms 0)가 적을 수 있는 값이어야 한다. 음수만 막는다.
                if (s.Ms < 0) throw new FormatException("PressFxUi: «" + kv.Key + "» ms 는 0 이상이어야 한다(0 = 즉시 · 정본에 transition 이 없는 자리)");
                if (s.Scale <= 0) throw new FormatException("PressFxUi: «" + kv.Key + "» scale 은 0보다 커야 한다");
                if (s.Brightness <= 0) throw new FormatException("PressFxUi: «" + kv.Key + "» brightness 는 0보다 커야 한다");
                if (s.DyRem < 0) throw new FormatException("PressFxUi: «" + kv.Key + "» dy_rem 은 0 이상(아래가 +)이어야 한다");
                t.map[kv.Key] = s;
                t.order.Add(kv.Key);
            }
            if (t.order.Count == 0) throw new FormatException("PressFxUi: press 표가 비었다");
            return t;
        }
    }

    /// <summary>탭 패널 슬라이드(정본 `.panel { transform: translateY(105%); transition: transform .22s ease-out }` · `.open { transform: none }`) — 표 `panel_slide`.</summary>
    public sealed class PanelSlideSpec
    {
        /// <summary>닫힌 자리 = 높이의 몇 %(아래가 +).</summary>
        public double DyPct;
        /// <summary>슬라이드 길이(ms).</summary>
        public double Ms;
        public CssEase Ease;
        public string Note;

        public static PanelSlideSpec From(JsonObject root)
        {
            JsonObject o = J.Obj(J.Require(root, "panel_slide"));
            if (o == null) throw new FormatException("PressFxUi: panel_slide 는 객체여야 한다");
            PanelSlideSpec s = new PanelSlideSpec { DyPct = J.Num(J.Require(o, "dy_pct")), Ms = J.Num(J.Require(o, "ms")) };
            object e;
            s.Ease = o.TryGet("ease", out e) ? RewardBurstSpec.EaseOf(e) : CssEase.Linear;
            object n;
            s.Note = o.TryGet("_", out n) ? n as string : null;
            if (s.Ms <= 0) throw new FormatException("PressFxUi: panel_slide ms 는 0보다 커야 한다");
            if (s.DyPct <= 0) throw new FormatException("PressFxUi: panel_slide dy_pct 는 0보다 커야 한다");
            return s;
        }
    }

    /// <summary>
    /// 눌림 피드백의 셈(T355). CSS `transition` 은 속성이 바뀌면 «지금 값 → 목표 값» 을 같은 길이·같은 이징으로 오가므로
    /// 눌림(0→1)과 뗌(1→0)을 한 위상(phase 0~1)으로 둔다 — 중간에 떼면 그 위상에서 되돌아간다.
    /// </summary>
    public static class PressRules
    {
        /// <summary>위상 — <paramref name="from"/>(전이가 시작될 때의 위상)에서 목표(눌림 1 · 뗌 0)로, <paramref name="elapsedMs"/> 동안 표의 ms·ease 로 간다.</summary>
        public static double Phase(PressSpec s, double from, double elapsedMs, bool pressed)
        {
            double target = pressed ? 1.0 : 0.0;
            // T515 — `ms` 0 은 «정본에 transition 이 없다» 는 뜻이다: 한 프레임도 머물지 않고 목표로 간다
            //   (종전엔 첫 프레임에 `from` 을 돌려줘 눌림이 한 프레임 늦었다 · 지금은 그 자리가 아예 없다).
            if (s.Ms <= 0) return target;
            if (elapsedMs <= 0) return Clamp01(from);
            double t = elapsedMs / s.Ms;
            if (t >= 1) return target;
            double k = s.Ease.Ease(t);
            return Clamp01(from + (target - from) * k);
        }

        /// <summary>그 위상에서 아래로 민 거리(rem).</summary>
        public static double DyRem(PressSpec s, double phase) { return s.DyRem * Clamp01(phase); }
        /// <summary>그 위상에서의 배율(1 ↔ 표 scale).</summary>
        public static double ScaleAt(PressSpec s, double phase) { return 1.0 + (s.Scale - 1.0) * Clamp01(phase); }
        /// <summary>
        /// T515 5회차 — 그 위상에서 면 색이 **갈아입은 상태인가**(정본 4802 `:active { background: #2c313a }`).
        /// 그 선언엔 `transition` 이 없어 **이산**이다 — 위상이 0 을 넘으면 갈아입은 색이고 0 이면 제 색이다
        /// (턱 걷기 <see cref="PressSpec.LipOff"/> 와 같은 잣대 · 표 `ms: 0` 이면 그 넘김이 첫 프레임에 든다).
        /// </summary>
        public static bool FaceSwapped(PressSpec s, double phase) { return s.FaceSwap && phase > 0.0; }

        /// <summary>그 위상에서의 밝기 배율(1 ↔ 표 brightness).</summary>
        public static double BrightnessAt(PressSpec s, double phase) { return 1.0 + (s.Brightness - 1.0) * Clamp01(phase); }

        /// <summary>전이가 끝나 더 갱신할 것이 없는가(위상이 목표에 닿았다).</summary>
        public static bool Settled(double phase, bool pressed) { return pressed ? phase >= 1.0 : phase <= 0.0; }

        /// <summary>패널 슬라이드 — 열린 지 <paramref name="elapsedMs"/> 에서 «아직 아래에 남은 거리»(높이 단위 · 0 이면 제자리). 시작은 높이 × dy_pct/100 · ease-out 으로 0 에 닿는다.</summary>
        public static double SlideOffset(PanelSlideSpec s, double elapsedMs, double height)
        {
            double full = height * s.DyPct / 100.0;
            if (elapsedMs <= 0) return full;
            double t = elapsedMs / s.Ms;
            if (t >= 1) return 0;
            return full * (1.0 - Clamp01(s.Ease.Ease(t)));
        }

        /// <summary>슬라이드가 끝났는가.</summary>
        public static bool SlideDone(PanelSlideSpec s, double elapsedMs) { return elapsedMs >= s.Ms; }

        static double Clamp01(double v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }
    }
}
