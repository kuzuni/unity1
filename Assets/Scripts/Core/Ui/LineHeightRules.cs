using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Ui
{
    /// <summary>
    /// T354 — 정본 `line-height` 의 **셈**. 자리마다의 값은 표(`Resources/LineHeightUi.json`)가 쥐고, 여기에는 단위 규약과 환산만 있다.
    /// UnityEngine 참조 0.
    ///
    /// 왜 셈이 필요한가: CSS `line-height` 는 «글자 크기의 배수» 인데 TMP `lineSpacing` 은 «**글꼴 자산 단위의 덧붙임**» 이다.
    /// TMP 의 줄 넘김은 `(faceInfo.lineHeight + lineSpacing) * baseScale` 이므로, 배수 r 을 내려면
    /// `lineSpacing = r * pointSize - lineHeight` 를 준다(<see cref="Spacing"/>). 크기를 바꿔도 비율이 유지된다 —
    /// baseScale 이 `fontSize / pointSize` 라 양쪽에 같이 걸리기 때문이다.
    ///
    /// 단위 규약(키 꼬리가 단위다):
    ///   `…_lh`     — 정본이 준 **배수**(`line-height: 1.25`). 그대로 <see cref="Spacing"/> 에 넣는다.
    ///   `…_lh_rem` — 정본이 `Nrem` 으로 준 절대 줄높이. 배수로 바꾸려면 «그 자리 글자 크기» 로 나눈다(<see cref="RatioFromPx"/>).
    ///   `…_lh_w`   — 정본이 `calc(var(--app-w) * k)` 로 준 절대 줄높이. 앱 폭을 곱한 뒤 같은 길로 간다.
    /// </summary>
    public static class LineHeightRules
    {
        public const string RatioSuffix = "_lh";
        public const string RemSuffix = "_lh_rem";
        public const string AppWSuffix = "_lh_w";

        public static bool IsRemKey(string key) { return key != null && key.EndsWith(RemSuffix, StringComparison.Ordinal); }
        public static bool IsAppWKey(string key) { return key != null && key.EndsWith(AppWSuffix, StringComparison.Ordinal); }
        /// <summary>배수 키인가 — rem·app-w 꼬리가 `_lh` 로도 끝나므로 **그 둘을 먼저 걸러야** 한다.</summary>
        public static bool IsRatioKey(string key)
        {
            return key != null && key.EndsWith(RatioSuffix, StringComparison.Ordinal) && !IsRemKey(key) && !IsAppWKey(key);
        }
        public static bool IsLineHeightKey(string key) { return IsRatioKey(key) || IsRemKey(key) || IsAppWKey(key); }

        /// <summary>글꼴이 제 힘으로 내는 줄높이 배수 — `faceInfo.lineHeight / faceInfo.pointSize`(NotoSansKR-Forge 는 1.448).</summary>
        public static double FaceRatio(double faceLineHeight, double facePointSize)
        {
            if (facePointSize <= 0) throw new FormatException("글꼴 pointSize 가 0 이하다: " + facePointSize);
            return faceLineHeight / facePointSize;
        }

        /// <summary>배수 r → TMP `lineSpacing`(글꼴 단위). 글꼴이 이미 내는 만큼을 뺀 **덧붙임**이다(음수가 정상이다).</summary>
        public static double Spacing(double ratio, double faceLineHeight, double facePointSize)
        {
            if (facePointSize <= 0) throw new FormatException("글꼴 pointSize 가 0 이하다: " + facePointSize);
            if (ratio < 0) throw new FormatException("줄높이 배수가 음수다: " + ratio);
            return ratio * facePointSize - faceLineHeight;
        }

        /// <summary>절대 줄높이(px) → 배수. `…_lh_rem`·`…_lh_w` 자리가 이 길로 온다.</summary>
        public static double RatioFromPx(double lineHeightPx, double fontSizePx)
        {
            if (fontSizePx <= 0) throw new FormatException("글자 크기가 0 이하다: " + fontSizePx);
            return lineHeightPx / fontSizePx;
        }

        /// <summary>표값 → 배수. rem 키는 <paramref name="pxPerRem"/> 를, app-w 키는 <paramref name="appWidthPx"/> 를 곱해 px 로 만든 뒤 나눈다.</summary>
        public static double Ratio(double value, string key, double fontSizePx, double pxPerRem, double appWidthPx)
        {
            if (IsRatioKey(key)) return value;
            if (IsRemKey(key)) return RatioFromPx(value * pxPerRem, fontSizePx);
            if (IsAppWKey(key)) return RatioFromPx(value * appWidthPx, fontSizePx);
            throw new FormatException("줄높이 키는 «" + RatioSuffix + "»·«" + RemSuffix + "»·«" + AppWSuffix + "» 중 하나로 끝나야 한다: " + key);
        }
    }

    /// <summary>`LineHeightUi.json` — 자리 키 → 값(단위는 키 꼬리). `_` 로 시작하는 칸은 설명이다.</summary>
    public sealed class LineHeightTable
    {
        readonly Dictionary<string, double> map = new Dictionary<string, double>(StringComparer.Ordinal);

        public int Count { get { return map.Count; } }
        public IEnumerable<string> Keys { get { return map.Keys; } }
        public bool Has(string key) { return map.ContainsKey(key); }

        public double Get(string key)
        {
            double v;
            if (!map.TryGetValue(key, out v)) throw new FormatException("LineHeightUi 에 없는 자리다: " + key);
            return v;
        }

        public static LineHeightTable From(JsonObject root)
        {
            if (root == null) throw new FormatException("LineHeightUi 의 최상위가 «상자» 가 아니다");
            var t = new LineHeightTable();
            foreach (var kv in root)
            {
                string k = kv.Key;
                if (k.Length > 0 && k[0] == '_') continue;
                if (!LineHeightRules.IsLineHeightKey(k))
                    throw new FormatException("LineHeightUi 키의 꼬리가 단위가 아니다: " + k);
                if (!J.IsNum(kv.Value)) throw new FormatException("LineHeightUi «" + k + "» 가 수가 아니다");
                double v = J.Num(kv.Value);
                if (v < 0) throw new FormatException("LineHeightUi «" + k + "» 가 음수다: " + v);
                t.map[k] = v;
            }
            if (t.map.Count == 0) throw new FormatException("LineHeightUi 에 자리가 하나도 없다");
            return t;
        }
    }
}
