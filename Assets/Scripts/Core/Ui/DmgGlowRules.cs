using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T333 10회차 — 데미지 숫자의 **글로우 한 겹**(표 <c>Resources/DmgGlowUi.json</c>)을 강타입으로. 정본은 `text-shadow` 를 여러 겹 쌓고
    /// `@keyframes dmgcrit`(565·567)·`dmgkill`(578·580)이 **태어나는 프레임**에서 그 겹을 갈아 끼운다 — TMP 언더레이는 한 겹뿐이라
    /// 겹은 «읽히게 만드는 한 겹»만 옮기고(표 `_` 에 뺀 겹을 적는다), **시간에 따른 갈아 끼움**은 여기 `Phase` 가 고른다.
    /// 값→재질 환산은 <see cref="UnderlaySdf"/> 가 그대로 쓴다. UnityEngine 0(§1).
    /// </summary>
    public sealed class DmgGlowSpec
    {
        /// <summary>글로우 한 겹 — 정본 `text-shadow: dx dy blur color` 의 CSS px·색.</summary>
        public sealed class Glow
        {
            public string Key;
            public double DxPx, DyPx, BlurPx, Alpha;
            /// <summary>#RRGGBB(알파는 <see cref="Alpha"/> 가 따로 쥔다 — 표가 정본 rgba 를 그대로 읽히게).</summary>
            public string Color;
        }

        /// <summary>«수명의 몇 할까지 이 겹» — 정본 키프레임 퍼센트 그대로(0&lt;u≤1).</summary>
        public sealed class Phase
        {
            public double UntilF;
            public string Key;
        }

        readonly Dictionary<string, Glow> glows = new Dictionary<string, Glow>();
        readonly Dictionary<string, Phase[]> classes = new Dictionary<string, Phase[]>();

        public static DmgGlowSpec From(JsonObject root)
        {
            var s = new DmgGlowSpec();
            JsonObject g = J.Obj(J.Require(root, "glows"));
            if (g == null) throw new InvalidOperationException("DmgGlowUi: glows 가 객체가 아니다");
            foreach (var kv in g)
            {
                JsonObject o = J.Obj(kv.Value);
                if (o == null) continue;
                s.glows[kv.Key] = new Glow
                {
                    Key = kv.Key,
                    DxPx = J.Num(J.Require(o, "dx_px")),
                    DyPx = J.Num(J.Require(o, "dy_px")),
                    BlurPx = J.Num(J.Require(o, "blur_px")),
                    Color = J.Str(J.Require(o, "color")),
                    Alpha = J.Num(J.Require(o, "alpha")),
                };
            }
            JsonObject c = J.Obj(J.Require(root, "classes"));
            if (c == null) throw new InvalidOperationException("DmgGlowUi: classes 가 객체가 아니다");
            foreach (var kv in c)
            {
                List<object> arr = J.Arr(kv.Value);
                if (arr == null || arr.Count == 0) throw new InvalidOperationException("DmgGlowUi: 종류 «" + kv.Key + "» 의 단계가 비었다");
                var ph = new Phase[arr.Count];
                double prev = 0;
                for (int i = 0; i < arr.Count; i++)
                {
                    JsonObject o = J.Obj(arr[i]);
                    if (o == null) throw new InvalidOperationException("DmgGlowUi: 종류 «" + kv.Key + "» 의 단계 " + i + " 가 객체가 아니다");
                    double until = J.Num(J.Require(o, "until_f"));
                    string key = J.Str(J.Require(o, "key"));
                    if (until <= prev) throw new InvalidOperationException("DmgGlowUi: 종류 «" + kv.Key + "» 의 until_f 가 커지지 않는다(" + prev + " → " + until + ") — 정본 키프레임 순서다");
                    if (!s.glows.ContainsKey(key)) throw new InvalidOperationException("DmgGlowUi: 종류 «" + kv.Key + "» 가 표에 없는 겹 «" + key + "» 을 부른다");
                    ph[i] = new Phase { UntilF = until, Key = key };
                    prev = until;
                }
                if (ph[ph.Length - 1].UntilF < 1) throw new InvalidOperationException("DmgGlowUi: 종류 «" + kv.Key + "» 의 마지막 단계가 수명 끝(1)까지 안 간다 — 정본은 마지막 키프레임 값이 끝까지 간다");
                s.classes[kv.Key] = ph;
            }
            return s;
        }

        /// <summary>그 종류에 글로우가 있나(정본에 없는 종류는 없다 — 지어내지 않는다).</summary>
        public bool Has(string cls) { return cls != null && classes.ContainsKey(cls); }

        public Glow Get(string key)
        {
            Glow g;
            if (key == null || !glows.TryGetValue(key, out g)) throw new KeyNotFoundException("DmgGlowUi.json 에 겹 «" + key + "» 이 없다");
            return g;
        }

        /// <summary>단계 목록(자가 «몇 단인가» 를 묻는다).</summary>
        public Phase[] Phases(string cls)
        {
            Phase[] p;
            return cls != null && classes.TryGetValue(cls, out p) ? p : null;
        }

        /// <summary>수명 비율 <paramref name="u"/>(0~1)에 서는 겹 키 — 없는 종류면 null. 정본 키프레임과 같이 «그 퍼센트까지» 가 그 겹이고 마지막 겹은 끝까지 간다.</summary>
        public string KeyAt(string cls, double u)
        {
            Phase[] p = Phases(cls);
            if (p == null) return null;
            for (int i = 0; i < p.Length; i++) if (u < p[i].UntilF) return p[i].Key;
            return p[p.Length - 1].Key;
        }
    }
}
