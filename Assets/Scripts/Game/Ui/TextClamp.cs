using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T351 — 정본 `overflow` 축의 **글자 자르기·말줄임**을 자리 이름으로 건다. 정본은 세 꼴이다:
    /// 한 줄 말줄임(`white-space: nowrap; overflow: hidden; text-overflow: ellipsis` · `.profile-field` 3052 · `.held-name` 1005)과
    /// 두 줄 클램프(`-webkit-line-clamp: 2` · `.sr-name > span` 7048). TMP 는 <see cref="TextOverflowModes.Ellipsis"/> 가
    /// 상자 안에 든 마지막 줄 끝에 …(U+2026)을 붙이고 나머지를 버리므로 «줄 수» 는 곧 **상자 높이**다 — <see cref="BoxHeight"/> 가 그 높이를 준다.
    /// 수치(자리별 줄 수 · 줄높이 비율)는 <c>Resources/TextClampUi.json</c> 이 쥔다(§1 «코드에 숫자를 박지 않는다»).
    /// 본보기는 `Hud.cs` 212~217 채팅 미리보기(NoWrap + Ellipsis) — 이 도우미는 그 두 줄을 표 자리 이름으로 묶은 것이다.
    /// </summary>
    public static class TextClamp
    {
        public const string ResourcePath = "TextClampUi";
        static JsonObject root, sites;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T351)");
            root = MiniJson.ParseObject(ta.text);
            sites = J.Obj(root["sites"]);
            if (sites == null) throw new InvalidOperationException(ResourcePath + ".json 에 sites 절이 없다 (T351)");
        }

        public static void Reset() { root = null; sites = null; }

        /// <summary>표에 적힌 자리 이름 전부(자가 돈다).</summary>
        public static IEnumerable<string> Sites() { Load(); return sites.Keys; }

        /// <summary>자리의 허용 줄 수(1 이상). 표에 없는 자리는 던진다 — 조용히 넘치는 글자가 «원작에 없는 것» 이다.</summary>
        public static int Lines(string site)
        {
            Load();
            JsonObject s = J.Obj(sites[site]);
            if (s == null) throw new KeyNotFoundException(ResourcePath + ".json 에 자리 «" + site + "» 가 없다 (T351)");
            object v = s["lines"];
            if (!J.IsNum(v) || J.Num(v) < 1) throw new InvalidOperationException(ResourcePath + ".json «" + site + "».lines 는 1 이상이어야 한다 (T351)");
            return (int)J.Num(v);
        }

        /// <summary>글꼴 지표를 못 읽을 때의 줄높이 비율(글자 크기 × 이 값).</summary>
        public static float LineHeightF()
        {
            Load();
            object v = root["line_height_f"];
            if (!J.IsNum(v) || J.Num(v) <= 0) throw new InvalidOperationException(ResourcePath + ".json line_height_f 가 없다 (T351)");
            return (float)J.Num(v);
        }

        /// <summary>상자에 더 주는 여유 비율 — TMP 는 줄이 상자 높이에 조금이라도 안 들면 그 줄을 통째로 버린다(런 528 실측 characterCount 0).</summary>
        public static float SlackF()
        {
            Load();
            object v = root["slack_f"];
            if (!J.IsNum(v) || J.Num(v) < 0) throw new InvalidOperationException(ResourcePath + ".json slack_f 가 없다 (T351)");
            return (float)J.Num(v);
        }

        /// <summary>이 글자의 실제 한 줄 높이 — 글꼴 faceInfo(lineHeight / pointSize) × 글자 크기. 지표가 비어 있으면 표의 비율로.</summary>
        public static float LineHeight(TMP_Text t)
        {
            if (t == null) throw new ArgumentNullException("t");
            float f = 0f;
            if (t.font != null && t.font.faceInfo.pointSize > 0) f = t.font.faceInfo.lineHeight / t.font.faceInfo.pointSize;
            if (f <= 0f) f = LineHeightF();
            return t.fontSize * f;
        }

        /// <summary>
        /// 그 글자의 **줄과 줄 사이 내려감**(pitch · px) — 첫 줄이 먹는 <see cref="LineHeight"/>(face)와 다르다.
        /// TMP 는 «자산 줄높이 × baseScale + <c>lineSpacing</c> × em/100» 으로 내려가므로 pitch = (face비율 + lineSpacing/100) × 글자 크기다.
        /// 줄높이를 안 건드린 글자는 <c>lineSpacing</c> 이 0 이라 pitch = face 로 종전과 같다(T423 1회차).
        /// </summary>
        public static float Pitch(TMP_Text t)
        {
            if (t == null) throw new ArgumentNullException("t");
            return LineHeight(t) + t.lineSpacing * 0.01f * t.fontSize;
        }

        /// <summary>
        /// 그 자리의 글자 상자 높이 — **첫 줄은 face, 둘째 줄부터 pitch**: `face + (줄 수 − 1) × pitch`, 거기에 여유를 곱한다.
        ///
        /// ⚠ 종전 셈은 `줄 수 × face` 였다. 줄높이를 표에서 받는 자리(T354)가 늘면서 그 둘이 갈라졌다 —
        /// CSS 줄상자는 «`line-height` × 줄 수» 지만 **TMP 의 첫 줄은 pitch 가 아니라 face(NotoSansKR 1.448em)를 먹는다**.
        /// 그래서 정본이 «두 줄이 딱 드는 높이» 로 적어 둔 값(`.sr-name` 2.36em = 1.18 × 2)을 그대로 상자에 주면
        /// TMP 는 둘째 줄을 통째로 버린다(필요 높이 = 1.448 + 1.18 = 2.628em · T423 등재 실측).
        /// 한 줄 자리(`lines: 1`)와 줄높이를 안 건 자리는 값이 **종전과 같다**.
        /// </summary>
        public static float BoxHeight(TMP_Text t, string site)
        {
            int n = Lines(site);
            return (LineHeight(t) + (n - 1) * Pitch(t)) * (1f + SlackF());
        }

        /// <summary>정본의 자르기 규칙을 건다: 한 줄이면 NoWrap + Ellipsis · 여러 줄이면 Normal(줄바꿈) + Ellipsis(상자 밖 줄은 버리고 …).</summary>
        public static TMP_Text Apply(TMP_Text t, string site)
        {
            if (t == null) throw new ArgumentNullException("t");
            int n = Lines(site);
            t.textWrappingMode = n <= 1 ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }
    }
}
