using System.Collections.Generic;
using TMPro;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T352 ⓒ — 그 글자의 **숫자 구간**을 정본 `tabular-nums` 처럼 등폭으로 그린다(<see cref="Forge.Core.Ui.TabularNums"/>).
    /// <para>
    /// 칸 폭은 글꼴에서 읽는다: 숫자 열 개 중 가장 넓은 advance ÷ 샘플링 크기 = em (tabular figures 의 정의 — 가장 넓은 숫자에 맞춘 칸).
    /// 글꼴마다 한 번만 재고, 숫자 글리프가 아직 아틀라스에 없으면 <c>TryAddCharacters</c> 로 먼저 굽는다(동적 아틀라스 · UiKit.MeasureAlphaTexels 와 같은 길).
    /// 못 읽으면(글꼴 없음 · 글리프 없음) 0 을 돌려주고 <see cref="Apply"/> 는 아무것도 안 한다 — 빨간 줄은 안 남긴다(정본도 폰트가 등폭 숫자를 못 주면 그냥 비례로 그린다).
    /// </para>
    /// 태그를 쓰므로 그 글자만 <c>richText</c> 를 켠다(<c>UiKit.Text</c> 의 기본은 끔). 켜는 조건이 곧 보호다: <see cref="Forge.Core.Ui.TabularNums.Wrap"/> 은
    /// **꺾쇠(`&lt;` `&gt;`)가 든 문구를 감싸지 않으므로** 감싸진 뒤에만 켜는 이 글자에는 태그가 될 수 있는 글자가 없다 — 정본 `U.escapeHtml` 이 막던 구멍(T175 · check_richtext)을
    /// 같은 자리에서 막는다(결정 579 · 그 자의 ALLOW 에 이 파일이 있다 — 워커 B 의 §0-6 급 수리 02f7326b · 결정 578).
    /// </summary>
    public static class TabularText
    {
        public const string Digits = "0123456789";

        static readonly Dictionary<int, float> emByFont = new Dictionary<int, float>();

        /// <summary>가장 넓은 숫자의 advance(em). 못 읽으면 0.</summary>
        public static float DigitEm(TMP_FontAsset fa)
        {
            if (fa == null) return 0f;
            int id = fa.GetInstanceID();
            float em;
            if (emByFont.TryGetValue(id, out em)) return em;
            em = MeasureDigitEm(fa);
            if (em > 0f) emByFont[id] = em;
            return em;
        }

        static float MeasureDigitEm(TMP_FontAsset fa)
        {
            float pt = fa.faceInfo.pointSize;
            if (pt <= 0f) return 0f;
            fa.TryAddCharacters(Digits);
            var table = fa.characterLookupTable;
            if (table == null) return 0f;
            float widest = 0f;
            foreach (char c in Digits)
            {
                TMP_Character ch;
                if (!table.TryGetValue(c, out ch) || ch == null || ch.glyph == null) continue;
                float adv = ch.glyph.metrics.horizontalAdvance;
                if (adv > widest) widest = adv;
            }
            return widest > 0f ? widest / pt : 0f;
        }

        /// <summary>그 글자의 숫자 구간을 등폭으로. 글자가 없거나 폭을 못 읽으면 그대로 둔다.</summary>
        public static void Apply(TMP_Text t)
        {
            if (t == null) return;
            float em = DigitEm(t.font);
            if (em <= 0f) return;
            string raw = t.text;
            string wrapped = Forge.Core.Ui.TabularNums.Wrap(raw, em);
            if (ReferenceEquals(wrapped, raw)) return;                 // 숫자 없음 · 이미 감쌈 · 꺾쇠 든 글(플레이어 글) — 켜지 않는다
            t.text = wrapped;
            t.richText = true;                                          // check_richtext ALLOW — 꺾쇠 없는 문구에만 닿는다(위 주석)
        }

        /// <summary>자용 — 글꼴 폭 캐시를 비운다.</summary>
        public static void ResetCache() { emByFont.Clear(); }
    }
}
