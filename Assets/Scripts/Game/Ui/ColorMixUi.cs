using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T371 — 정본 `color-mix(in srgb, A p%, B)` 자리의 **비율과 상대색**을 표(`Assets/Forge/Resources/ColorMixUi.json`)에서 읽어 섞는다.
    /// 셈은 Core <see cref="ColorMixRules"/>(sRGB · 알파 미리 곱하기)가 쥐고, 여기는 표 읽기와 Unity <see cref="Color"/> 변환만 한다.
    /// 부르는 쪽은 앞 색(등급색·가지색)만 준다 — 비율도 뒤 색도 코드에 안 적는다(§1).
    /// </summary>
    public static class ColorMixUi
    {
        public const string ResourcePath = "ColorMixUi";

        struct Row { public double F; public double R, G, B, A; }

        static Dictionary<string, Row> rows;

        static Dictionary<string, Row> Rows
        {
            get
            {
                if (rows != null) return rows;
                TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
                JsonObject root = J.Obj(MiniJson.Parse(ta.text));
                if (root == null || root.Count == 0) throw new KeyNotFoundException(ResourcePath + ".json 을 못 읽었다");
                rows = new Dictionary<string, Row>();
                foreach (KeyValuePair<string, object> kv in root)
                {
                    if (kv.Key.Length > 0 && kv.Key[0] == '_') continue;     // 설명 칸
                    JsonObject o = J.Obj(kv.Value);
                    if (o == null) continue;
                    Row r = new Row();
                    r.F = J.Num(o["mix_f"]);
                    string with = J.Str(o["with"]);
                    if (ColorMixRules.IsTransparent(with)) { r.R = 0; r.G = 0; r.B = 0; r.A = 0; }
                    else
                    {
                        double cr, cg, cb;
                        if (!ColorMixRules.ParseHex(with, out cr, out cg, out cb))
                            throw new KeyNotFoundException("ColorMixUi.json 의 «" + kv.Key + "» with 를 못 읽었다: " + with);
                        r.R = cr; r.G = cg; r.B = cb; r.A = 1;
                    }
                    rows[kv.Key] = r;
                }
                return rows;
            }
        }

        static Row Get(string key)
        {
            Row r;
            if (!Rows.TryGetValue(key, out r)) throw new KeyNotFoundException("ColorMixUi.json 에 «" + key + "» 이 없다");
            return r;
        }

        /// <summary>표의 비율(앞 색의 몫)로 <paramref name="a"/> 와 표의 뒤 색을 섞는다 — 정본 `color-mix(in srgb, a p%, with)`.</summary>
        public static Color Mix(string key, Color a)
        {
            Row w = Get(key);
            double r, g, b, al;
            ColorMixRules.Srgb(a.r * 255.0, a.g * 255.0, a.b * 255.0, a.a, w.R, w.G, w.B, w.A, w.F, out r, out g, out b, out al);
            return new Color((float)(r / 255.0), (float)(g / 255.0), (float)(b / 255.0), (float)al);
        }

        /// <summary>표의 비율(자만 읽는다 · 자리 배선은 <see cref="Mix"/> 를 쓴다).</summary>
        public static float F(string key) { return (float)Get(key).F; }
    }
}
