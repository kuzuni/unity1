using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T345 — 정본 `border-radius` 의 **셈**. 자리마다의 값은 표(`Resources/RadiusUi.json`)가 쥐고, 여기에는 단위 규약과 환산만 있다.
    /// UnityEngine 참조 0.
    ///
    /// 단위 규약(키 꼬리가 단위다 · `tools/check_border_radius.py` 가 같은 규약으로 정본 CSS 와 표를 견준다):
    ///   `…_r_rem` — 정본이 `Nrem` 으로 준 반지름. 정본 `:root` 글꼴은 높이 기준(`16/844 H` · 클론 catalog `rem_h`)이라 «1rem 이 몇 px 인가» 를 곱한다.
    ///   `…_r_w`   — 정본이 `calc(var(--app-w) * k)` 로 준 반지름(뒤로 버튼). 앱 폭을 곱한다 — rem 으로 바꿔 적으면 비율이 다른 화면에서 어긋난다.
    ///   `…_r_px`  — 정본이 **절대 CSS px** 로 준 반지름(보스 핍 2px 꼴). CSS px → 기준 캔버스 px 는 `KeylineUi.CssPx` 한 군데다(T104·T109 규약) —
    ///               rem 으로 바꿔 적으면 안 된다(정본이 rem 을 안 쓴 까닭은 «크기를 안 따라가는 모서리» 라서다 · T345 18회차).
    /// 정본의 `50%` 자리는 표에 안 둔다 — 그것은 «원» 이고 클론은 `UiKit.Circle` 이 그린다.
    /// </summary>
    public static class RadiusRules
    {
        public const string RemSuffix = "_r_rem";
        public const string AppWSuffix = "_r_w";
        public const string CssPxSuffix = "_r_px";

        public static bool IsRemKey(string key) { return key != null && key.EndsWith(RemSuffix, StringComparison.Ordinal); }
        public static bool IsAppWKey(string key) { return key != null && key.EndsWith(AppWSuffix, StringComparison.Ordinal); }
        public static bool IsCssPxKey(string key) { return key != null && key.EndsWith(CssPxSuffix, StringComparison.Ordinal); }
        public static bool IsRadiusKey(string key) { return IsRemKey(key) || IsAppWKey(key) || IsCssPxKey(key); }

        /// <summary>표값 → 화면 px. rem 키는 <paramref name="pxPerRem"/> 을, app-w 키는 <paramref name="appWidthPx"/> 를 곱한다. CSS px 키는 이 묶음으로 못 잰다 — <see cref="Px(double, string, double, double, double)"/> 를 쓴다.</summary>
        public static double Px(double value, string key, double pxPerRem, double appWidthPx)
        {
            if (IsCssPxKey(key)) throw new FormatException("CSS px 반지름은 환산비가 있어야 한다(cssPx 를 준 묶음을 써라): " + key);
            if (IsRemKey(key)) return value * pxPerRem;
            if (IsAppWKey(key)) return value * appWidthPx;
            throw new FormatException("반지름 키는 «" + RemSuffix + "» · «" + AppWSuffix + "» · «" + CssPxSuffix + "» 로 끝나야 한다: " + key);
        }

        /// <summary>표값 → 화면 px. <paramref name="cssPx"/> 는 «정본 CSS px 하나가 기준 캔버스 px 로 몇 인가»(`KeylineUi.CssPx`).</summary>
        public static double Px(double value, string key, double pxPerRem, double appWidthPx, double cssPx)
        {
            if (IsCssPxKey(key)) return value * cssPx;
            return Px(value, key, pxPerRem, appWidthPx);
        }

        /// <summary>CSS 는 반지름이 짧은 변의 반을 넘으면 그만큼으로 줄인다 — 한 값 반지름은 min(r, w/2, h/2).</summary>
        public static double Clamp(double radiusPx, double widthPx, double heightPx)
        {
            double half = Math.Min(widthPx, heightPx) * 0.5;
            if (half < 0) half = 0;
            return radiusPx > half ? half : (radiusPx < 0 ? 0 : radiusPx);
        }

        /// <summary>알약 동치(결정 543): 반지름이 높이의 반 이상이면 «높이의 반» 과 같은 그림이다.</summary>
        public static bool IsPill(double radiusPx, double heightPx) { return radiusPx + 1e-6 >= heightPx * 0.5; }
    }

    /// <summary>`RadiusUi.json` — 자리 키 → 값(단위는 키 꼬리). `_` 로 시작하는 칸은 설명이다.</summary>
    public sealed class RadiusTable
    {
        readonly Dictionary<string, double> map = new Dictionary<string, double>(StringComparer.Ordinal);

        public int Count { get { return map.Count; } }
        public IEnumerable<string> Keys { get { return map.Keys; } }
        public bool Has(string key) { return map.ContainsKey(key); }

        public double Get(string key)
        {
            double v;
            if (!map.TryGetValue(key, out v)) throw new FormatException("RadiusUi 에 없는 자리다: " + key);
            return v;
        }

        public static RadiusTable From(JsonObject root)
        {
            if (root == null) throw new FormatException("RadiusUi 의 최상위가 «상자» 가 아니다");
            var t = new RadiusTable();
            foreach (var kv in root)
            {
                string k = kv.Key;
                if (k.Length > 0 && k[0] == '_') continue;
                if (!RadiusRules.IsRadiusKey(k))
                    throw new FormatException("RadiusUi 키는 «" + RadiusRules.RemSuffix + "» · «" + RadiusRules.AppWSuffix + "» · «" + RadiusRules.CssPxSuffix + "» 로 끝나야 한다: " + k);
                if (!J.IsNum(kv.Value)) throw new FormatException("RadiusUi «" + k + "» 가 수가 아니다");
                double v = J.Num(kv.Value);
                if (v < 0) throw new FormatException("RadiusUi «" + k + "» 가 음수다: " + v);
                t.map[k] = v;
            }
            if (t.map.Count == 0) throw new FormatException("RadiusUi 에 자리가 하나도 없다");
            return t;
        }
    }
}
