using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T460 — 피해 숫자 아크 키프레임(표 <c>Resources/DmgArcUi.json</c>)을 강타입으로. 정본 `style.css` 507 `.float-dmg { animation: dmgrise .55s cubic-bezier(.16,.86,.35,1) forwards }`
    /// · 510 `.dmg-crit { animation-name: dmgcrit }` · 524 `.dmg-kill { animation-name: dmgkill }` — 세 아크는 이름만 다르고 길이·곡선은 같다.
    /// 전엔 Game `DamageNumbers.cs` 의 `Frame[]` 상수 셋(값은 정본과 같았다)이 **수명 900ms 에 늘어져** 돌았고 구간 곡선이 없었다(선형).
    /// CSS 규약대로 timing-function 은 **키프레임 구간마다** 걸린다 — <see cref="Sample"/> 이 그렇게 잰다. UnityEngine 0(§1).
    /// </summary>
    public sealed class DmgArcSpec
    {
        /// <summary>키프레임 하나 — 정본 퍼센트(<see cref="At"/> 0~1)와 그 시점의 `--dx`·`--rise`·`--pop` 배율 · 불투명도 · rotate(deg).</summary>
        public sealed class Frame
        {
            public double At, DxF, RiseF, Scale, Opacity, Rot;
        }

        /// <summary>아크 길이(ms) — 정본 .55s. 수명(HitRules.DmgLifeMs 900)과 다르다: 끝난 뒤 마지막 프레임으로 머문다(forwards).</summary>
        public double DurationMs;
        /// <summary>구간 곡선 cubic-bezier(x1, y1, x2, y2).</summary>
        public double[] Ease = new double[4];

        readonly Dictionary<string, Frame[]> arcs = new Dictionary<string, Frame[]>();

        public IEnumerable<string> Names { get { return arcs.Keys; } }
        public bool Has(string name) { return name != null && arcs.ContainsKey(name); }

        public Frame[] Frames(string name)
        {
            Frame[] f;
            if (name == null || !arcs.TryGetValue(name, out f)) throw new KeyNotFoundException("DmgArcUi: 아크 «" + name + "» 이 표에 없다");
            return f;
        }

        public static DmgArcSpec From(JsonObject root)
        {
            var s = new DmgArcSpec();
            s.DurationMs = J.Num(J.Require(root, "duration_ms"));
            if (s.DurationMs <= 0) throw new InvalidOperationException("DmgArcUi: duration_ms 는 0 보다 커야 한다");
            List<object> e = J.Arr(J.Require(root, "ease"));
            if (e == null || e.Count != 4) throw new InvalidOperationException("DmgArcUi: ease 는 cubic-bezier 네 수여야 한다");
            for (int i = 0; i < 4; i++)
            {
                if (!J.IsNum(e[i])) throw new InvalidOperationException("DmgArcUi: ease[" + i + "] 가 수가 아니다");
                s.Ease[i] = J.Num(e[i]);
            }
            if (s.Ease[0] < 0 || s.Ease[0] > 1 || s.Ease[2] < 0 || s.Ease[2] > 1) throw new InvalidOperationException("DmgArcUi: ease 의 x1·x2 는 0~1 이어야 한다(CSS cubic-bezier)");
            JsonObject a = J.Obj(J.Require(root, "arcs"));
            if (a == null) throw new InvalidOperationException("DmgArcUi: arcs 가 객체가 아니다");
            foreach (var kv in a)
            {
                List<object> arr = J.Arr(kv.Value);
                if (arr == null || arr.Count < 2) throw new InvalidOperationException("DmgArcUi: 아크 «" + kv.Key + "» 의 키프레임이 둘 미만이다");
                var frames = new Frame[arr.Count];
                double prev = -1;
                for (int i = 0; i < arr.Count; i++)
                {
                    JsonObject o = J.Obj(arr[i]);
                    if (o == null) throw new InvalidOperationException("DmgArcUi: 아크 «" + kv.Key + "» 키프레임 " + i + " 이 객체가 아니다");
                    var f = new Frame
                    {
                        At = J.Num(J.Require(o, "at")),
                        DxF = J.Num(J.Require(o, "dx_f")),
                        RiseF = J.Num(J.Require(o, "rise_f")),
                        Scale = J.Num(J.Require(o, "scale")),
                        Opacity = J.Num(J.Require(o, "opacity")),
                        Rot = J.Num(J.Require(o, "rot")),
                    };
                    if (f.At <= prev || f.At < 0 || f.At > 1) throw new InvalidOperationException("DmgArcUi: 아크 «" + kv.Key + "» 키프레임 " + i + " 의 at 이 0~1 오름차순이 아니다");
                    if (f.Opacity < 0 || f.Opacity > 1) throw new InvalidOperationException("DmgArcUi: 아크 «" + kv.Key + "» 키프레임 " + i + " 의 opacity 가 0~1 밖이다");
                    prev = f.At; frames[i] = f;
                }
                if (frames[0].At != 0 || frames[frames.Length - 1].At != 1) throw new InvalidOperationException("DmgArcUi: 아크 «" + kv.Key + "» 는 0% 와 100% 키프레임이 있어야 한다");
                s.arcs[kv.Key] = frames;
            }
            if (!s.arcs.ContainsKey("dmg")) throw new InvalidOperationException("DmgArcUi: 기본 아크 «dmg» 가 없다(정본 553 dmgrise)");
            return s;
        }

        /// <summary>
        /// 아크 시각 <paramref name="u"/>(0~1 · 아크 길이 기준 · 1 을 넘으면 마지막 프레임에 머문다 = forwards)의 값. 키프레임 사이는 CSS 규약대로
        /// 구간 진행률에 <see cref="Ease"/> 곡선을 걸어 선형 보간한다.
        /// </summary>
        public void Sample(string name, double u, out double dxF, out double riseF, out double scale, out double opacity, out double rot)
        {
            Frame[] f = Frames(name);
            if (u <= 0) { Set(f[0], out dxF, out riseF, out scale, out opacity, out rot); return; }
            if (u >= 1) { Set(f[f.Length - 1], out dxF, out riseF, out scale, out opacity, out rot); return; }
            Frame a = f[0], b = f[f.Length - 1];
            for (int i = 0; i < f.Length - 1; i++) if (u >= f[i].At && u <= f[i + 1].At) { a = f[i]; b = f[i + 1]; break; }
            double p = b.At > a.At ? (u - a.At) / (b.At - a.At) : 0;
            double w = CssBezier.Y(p, Ease[0], Ease[1], Ease[2], Ease[3]);
            dxF = a.DxF + (b.DxF - a.DxF) * w;
            riseF = a.RiseF + (b.RiseF - a.RiseF) * w;
            scale = a.Scale + (b.Scale - a.Scale) * w;
            opacity = a.Opacity + (b.Opacity - a.Opacity) * w;
            rot = a.Rot + (b.Rot - a.Rot) * w;
        }

        static void Set(Frame f, out double dxF, out double riseF, out double scale, out double opacity, out double rot)
        {
            dxF = f.DxF; riseF = f.RiseF; scale = f.Scale; opacity = f.Opacity; rot = f.Rot;
        }
    }

    /// <summary>CSS `cubic-bezier(x1, y1, x2, y2)` — 진행률 x(0~1)에서 값 y. 끝점 (0,0)·(1,1). x→t 는 이분법(정본 브라우저와 같은 뜻 · 1e-6).</summary>
    public static class CssBezier
    {
        public static double Y(double x, double x1, double y1, double x2, double y2)
        {
            if (x <= 0) return 0;
            if (x >= 1) return 1;
            double lo = 0, hi = 1, t = x;
            for (int i = 0; i < 40; i++)
            {
                t = (lo + hi) * 0.5;
                double cx = Curve(t, x1, x2);
                if (Math.Abs(cx - x) < 1e-6) break;
                if (cx < x) lo = t; else hi = t;
            }
            return Curve(t, y1, y2);
        }

        static double Curve(double t, double p1, double p2)
        {
            double mt = 1 - t;
            return 3 * mt * mt * t * p1 + 3 * mt * t * t * p2 + t * t * t;
        }
    }
}
