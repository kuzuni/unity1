using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T385 — `Resources/OrbIconUi.json` 읽기. 셈은 <see cref="OrbIconRules"/>, 값은 이 표다.
    /// 배선(구슬 위에 실제로 얹기)은 `SkillSummonResult`·`SummonFx` 가 남의 lock 에서 풀리는 회차 몫이다.
    /// </summary>
    public static class OrbIconUi
    {
        public const string ResourcePath = "OrbIconUi";

        /// <summary>겹 한 장 — 정본 `radial-gradient(rx ry at x y, 색, 투명 end)` 한 줄.</summary>
        public struct Layer
        {
            public double Rx, Ry, AtX, AtY, End, A;
            public Color Hex;
        }

        static JsonObject root;
        static Layer[] layers;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T385)");
            root = MiniJson.ParseObject(ta.text);
        }
        public static void Reset() { root = null; layers = null; }

        static double Num(JsonObject o, string key)
        {
            object v = o != null ? o[key] : null;
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 에 «" + key + "» 이 없다");
            return J.Num(v);
        }

        /// <summary>⑵ 배럴 배율 — 정본 `.sr-ico { transform: scale(1.04) }`.</summary>
        public static float BarrelScale { get { Load(); return (float)Num(root, "barrel_scale"); } }

        /// <summary>정본에서 아이콘이 구체 한 변에서 차지하는 비율(1/2.4) — 겹을 구체 기준으로 옮길 때 쓴다.</summary>
        public static double IconFracOfOrb { get { Load(); return Num(root, "icon_frac_of_orb"); } }

        static JsonObject Overlay()
        {
            Load();
            JsonObject o = J.Obj(root["overlay"]);
            if (o == null) throw new KeyNotFoundException(ResourcePath + ".json 에 «overlay» 가 없다");
            return o;
        }

        /// <summary>⑶ 스페큘러 겹의 상자 — 아이콘 한 변을 1 로 본 비율.</summary>
        public static OrbIconRules.OverlayBox Box
        {
            get
            {
                JsonObject o = Overlay();
                return new OrbIconRules.OverlayBox(Num(o, "left"), Num(o, "top"), Num(o, "w"), Num(o, "h"));
            }
        }

        /// <summary>겹 전체에 걸리는 알파(정본 `opacity: .35`).</summary>
        public static float Alpha { get { return (float)Num(Overlay(), "alpha"); } }

        /// <summary>겹 두 장 — 정본 차례 그대로(먼저가 위).</summary>
        public static Layer[] Layers
        {
            get
            {
                if (layers != null) return layers;
                List<object> a = J.Arr(Overlay()["layers"]);
                if (a == null || a.Count == 0) throw new KeyNotFoundException(ResourcePath + ".json «overlay» 에 layers 가 없다");
                Layer[] ls = new Layer[a.Count];
                for (int i = 0; i < a.Count; i++)
                {
                    JsonObject o = J.Obj(a[i]);
                    if (o == null) throw new System.FormatException(ResourcePath + ".json layers[" + i + "] 가 객체가 아니다");
                    ls[i].Rx = Num(o, "rx"); ls[i].Ry = Num(o, "ry");
                    ls[i].AtX = Num(o, "at_x"); ls[i].AtY = Num(o, "at_y");
                    ls[i].End = Num(o, "end"); ls[i].A = Num(o, "a");
                    double hr, hg, hb;
                    string hex = J.Str(o["hex"]);
                    if (!ColorMixRules.ParseHex(hex, out hr, out hg, out hb))
                        throw new System.FormatException(ResourcePath + ".json layers[" + i + "] 의 hex «" + hex + "» 를 못 읽었다");
                    ls[i].Hex = new Color((float)(hr / 255.0), (float)(hg / 255.0), (float)(hb / 255.0), 1f);
                }
                layers = ls;
                return layers;
            }
        }
    }
}
