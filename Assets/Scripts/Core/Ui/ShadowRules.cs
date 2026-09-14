using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T331 — 정본 `box-shadow` **바깥 그림자** 한 자리의 수치(`Resources/ShadowUi.json` 의 `spots` 한 칸).
    ///
    /// CSS 는 `box-shadow: <dx> <dy> <blur> <spread> <color>` 다. 정본의 딱딱한 턱은 `blur`·`spread` 가 0 이라
    /// **같은 모양을 한 겹 뒤에 깔고 내리기만** 하면 화면이 같아진다 — 구울 것이 없다(흐린 자리는 굽는 길이 따로 필요하다).
    ///
    /// ⚠ 부호: CSS 의 +y 는 **아래**고 UGUI 의 +y 는 **위**다. 이 표는 **정본 그대로**(아래가 양수) 담고,
    ///    화면에 거는 쪽(`UiShadow`)이 한 자리에서 뒤집는다 — 표를 뒤집어 담으면 정본과 대조할 때마다 헷갈린다.
    /// UnityEngine 참조 0.
    /// </summary>
    public struct ShadowSpec
    {
        /// <summary>정본 좌표의 치우침(rem · +y 는 **아래**).</summary>
        public double DxRem, DyRem;
        /// <summary>흐림·번짐(rem · 딱딱한 턱은 둘 다 0).</summary>
        public double BlurRem, SpreadRem;
        /// <summary>색(0~1).</summary>
        public double R, G, B, A;

        /// <summary>흐림도 번짐도 없는가 — 그러면 같은 모양 한 겹으로 끝난다(굽지 않는다).</summary>
        public bool IsHard { get { return BlurRem <= 0 && SpreadRem <= 0; } }
    }

    /// <summary>정본 그림자 자리 표 — 키는 자(`tools/check_box_shadows.py`)가 세는 키와 같다.</summary>
    public sealed class ShadowTable
    {
        readonly Dictionary<string, ShadowSpec> spots = new Dictionary<string, ShadowSpec>(StringComparer.Ordinal);

        public int Count { get { return spots.Count; } }
        public IEnumerable<string> Keys { get { return spots.Keys; } }

        public static ShadowTable From(JsonObject root)
        {
            JsonObject s = J.Obj(J.Require(root, "spots"));
            var t = new ShadowTable();
            foreach (var kv in s)
            {
                JsonObject o = J.Obj(kv.Value);
                double[] rgba = J.NumArr(J.Require(o, "rgba"));
                if (rgba == null || rgba.Length != 4) throw new FormatException("ShadowUi «" + kv.Key + "»: rgba 는 넷이어야 한다");
                foreach (double c in rgba) if (c < 0 || c > 1) throw new FormatException("ShadowUi «" + kv.Key + "»: rgba 는 0~1 이다(CSS 의 0~255 를 그대로 옮기지 마라)");
                var sp = new ShadowSpec
                {
                    DxRem = J.Num(J.Require(o, "dx_rem")),
                    DyRem = J.Num(J.Require(o, "dy_rem")),
                    BlurRem = J.Num(J.Require(o, "blur_rem")),
                    SpreadRem = J.Num(J.Require(o, "spread_rem")),
                    R = rgba[0], G = rgba[1], B = rgba[2], A = rgba[3],
                };
                if (sp.BlurRem < 0 || sp.SpreadRem < 0) throw new FormatException("ShadowUi «" + kv.Key + "»: blur·spread 는 음수가 아니다");
                if (sp.DxRem == 0 && sp.DyRem == 0 && sp.IsHard) throw new FormatException("ShadowUi «" + kv.Key + "»: 치우침도 흐림도 0 이면 그림자가 아니다");
                t.spots[kv.Key] = sp;
            }
            if (t.spots.Count == 0) throw new FormatException("ShadowUi: spots 가 비었다");
            return t;
        }

        public bool Has(string key) { return spots.ContainsKey(key); }

        public ShadowSpec Get(string key)
        {
            ShadowSpec s;
            if (!spots.TryGetValue(key, out s)) throw new KeyNotFoundException("ShadowUi.json 의 «spots» 에 «" + key + "» 이 없다");
            return s;
        }

        /// <summary>화면 좌표(UGUI)의 치우침 — CSS 의 아래(+y)를 여기서 한 번만 뒤집는다.</summary>
        public void OffsetPx(string key, double remPx, out double x, out double y)
        {
            ShadowSpec s = Get(key);
            x = s.DxRem * remPx;
            y = -s.DyRem * remPx;
        }
    }
}
