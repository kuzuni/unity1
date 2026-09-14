using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>T18 — 모든 활성 글자가 UiKit 을 거쳤고(종류 표식) 그 종류의 하한 이상인가(ROUTINE §1: 본문 40 · 버튼 44 · 보조 36 · 제목 60).</summary>
    public class TextSizeGateTests
    {
        [UnityTest]
        public IEnumerator 모든_활성_글자가_종류_하한_이상이다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            UiCatalog cat = UiCatalog.Instance;

            TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            int seen = 0;
            foreach (TMP_Text t in texts)
            {
                if (!t.gameObject.activeInHierarchy) continue;
                seen++;
                UiTextKindTag tag = t.GetComponent<UiTextKindTag>();
                Assert.IsNotNull(tag, Path(t.transform) + " 은 UiKit.Text 를 거치지 않았다(종류 표식 없음)");
                float min = cat.Kind(tag.Kind).min;
                Assert.GreaterOrEqual(t.fontSize, min, Path(t.transform) + " 글자 " + t.fontSize + " < 종류 " + tag.Kind + " 하한 " + min);
                Assert.AreSame(UiFont.Primary, t.font, Path(t.transform) + " 은 주인 글꼴(NotoSans)이 아니다");
            }
            Assert.Greater(seen, 0, "활성 글자가 하나도 없다 — HUD 가 안 섰다");
        }

        [Test]
        public void 카탈로그의_종류_하한은_ROUTINE_규칙_그대로다()
        {
            UiCatalog cat = UiCatalog.Instance;
            Assert.GreaterOrEqual(cat.Kind(TextKind.Body).min, 40f);
            Assert.GreaterOrEqual(cat.Kind(TextKind.Button).min, 44f);
            Assert.GreaterOrEqual(cat.Kind(TextKind.Sub).min, 36f);
            // T136 — `Micro` 는 §1 하한의 **예외 한 자리**다: 정본이 `.5rem` 로 못 박은 배지 글자(기준 캔버스 18.2px).
            //   그 값이 커지면 배지가 제 그릇보다 넓어지고, 더 작아지면 정본보다 작아진다 — 양쪽을 다 막는다.
            Assert.AreEqual(18f, cat.Kind(TextKind.Micro).size, 0.5f, "Micro = 정본 .5rem(= 18.2px 기준 캔버스)");
            Assert.AreEqual(cat.Kind(TextKind.Micro).size, cat.Kind(TextKind.Micro).min, 1e-3f, "Micro 는 크기 = 하한");
            Assert.GreaterOrEqual(cat.Kind(TextKind.Title).min, 60f);
            foreach (TextKind k in System.Enum.GetValues(typeof(TextKind)))
                Assert.GreaterOrEqual(cat.Kind(k).size, cat.Kind(k).min, k + " 의 크기가 제 하한보다 작다");
        }

        /// <summary>T53 — 화면에 실제로 서는 한글이 글꼴에 있는가(두부 □ 막이).
        /// 폰트 애셋이 글리프를 못 찾으면 TMP 는 조용히 네모를 그린다 — 그래서 문자 단위로 묻는다.</summary>
        [UnityTest]
        public IEnumerator 화면_한글이_글꼴에_있다_두부가_없다()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            TMP_FontAsset fa = UiFont.Primary;
            System.Collections.Generic.HashSet<char> missing = new System.Collections.Generic.HashSet<char>();
            foreach (TextMeshProUGUI t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) continue;
                string text = t.text;
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    // T100 — 한글 음절만 보던 자리를 «글꼴이 그려야 할 모든 글자» 로 넓힌다. 종전 거르개는
                    // 화면에 실제로 선 `↻`·`😭`·`ㅠ` 를 하나도 안 세서 «전부 초록인데 화면엔 □» 가 났다.
                    if (c < 0x80) continue;                          // ASCII 는 어느 글꼴에나 있다
                    if (char.IsWhiteSpace(c) || char.IsControl(c)) continue;
                    if (KnownTofu.IndexOf(c) >= 0) continue;         // 임자가 정해진 아는 자리(tools/check_text_glyphs.py 의 KNOWN 과 같은 목록)
                    // T106 — 이모지(BMP 밖은 서리게이트 짝)는 카탈로그의 이모지 폴백 글꼴이 쥔다 → 그 글자만 «폴백 포함» 으로 묻는다.
                    //        짝을 한 코드포인트로 합쳐 묻는다(HasCharacter 는 char 만 받으므로 TryAddCharacters + characterLookupTable 로 · 런 409). 주 글꼴 혼자 판정하는 규칙(T112)은 그대로 — 폴백도 배포판에 실리는 파일이다.
                    if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        uint cp = (uint)char.ConvertToUtf32(c, text[i + 1]); i++;
                        if (!HasWithEmoji(fa, cp)) { missing.Add(c); missing.Add(text[i]); }
                        continue;
                    }
                    if (EmojiChars.IndexOf(c) >= 0) { if (!HasWithEmoji(fa, c)) missing.Add(c); continue; }
                    // T112 — 두 인자의 뜻이 다르다. `searchFallbacks`(둘째)는 **끈다**: 배포판(리눅스 CI·WebGL)에
                    // OS 폴백이 없으므로 «주인 글꼴 혼자» 가 판정 기준이다. `tryAddCharacter`(셋째)는 **켠다**:
                    // 런타임 애셋은 Dynamic 이라 이것을 끄면 «글꼴에 있는가» 가 아니라 «이 순간까지 아틀라스에
                    // 구워졌는가» 를 묻게 된다 — 아래 «카탈로그 글꼴이…» 가 그것 때문에 런 216 에 빨개졌다.
                    // 이 자리는 «화면에 이미 선 글자» 만 봐서 지금껏 맞게 돌았지만 같은 함정이라 함께 켠다
                    // (글꼴에 없는 글자는 셋째를 켜도 false 라 막이는 안 느슨해진다).
                    if (!fa.HasCharacter(c, false, true)) missing.Add(c);
                }
            }
            Assert.IsEmpty(missing, "글꼴에 없는 글자(화면에 □ 로 나온다): " + new string(System.Linq.Enumerable.ToArray(missing)));
        }

        /// <summary>
        /// T100 — «아는 두부»: 임자가 정해져 있고 지금 못 고치는 글자(정본도 글자로 쓰거나 남의 lock 이 쥔 자리).
        /// `tools/check_text_glyphs.py` 의 `KNOWN` 과 **같은 목록**이다 — 한쪽만 지우면 다른 쪽이 잡는다.
        /// 새 글자는 여기 없으니 이 단언이 빨개진다(그것이 이 막이의 일이다).
        /// </summary>
        /// T137 — Core 토스트 둘(🚪 U+1F6AA Dungeons.cs:237 · 🔥 U+1F525 Battle.cs:578)도 같은 목록 — 정본도 글자(dungeons.js 156 · combat.js 499) · T106.
        /// T106 — 이모지 여덟은 폴백 글꼴(NotoEmoji-Forge)이 쥐어 «아는 두부» 에서 뺐다 — 남은 것은 `↻`(T108 이 아이콘으로 바꿨다 · 다시 글자가 되면 빨강) 하나.
        private const string KnownTofu = "\u21BB";
        /// <summary>T106 — 정본이 글자로 쓰는 이모지 여덟 중 BMP 안의 둘(⏱ ⏹) — 폴백 포함으로 묻는다(BMP 밖 여섯은 서리게이트 짝으로 HasCharacters 가 본다).</summary>
        private const string EmojiChars = "\u23F1\u23F9";

        /// <summary>T106 — 이모지 폴백 글꼴이 카탈로그에 꽂혀 있고, 정본이 글자로 쓰는 여덟을 (주 글꼴이 아니라) 그것이 직접 쥔다.</summary>
        [Test]
        public void 이모지_여덟은_폴백_글꼴이_직접_쥔다()
        {
            TMP_FontAsset fa = UiFont.Primary;
            TMP_FontAsset em = UiFont.EmojiFallback;
            Assert.IsNotNull(em, "카탈로그 emojiFont(NotoEmoji-Forge.ttf)로 만든 폴백 애셋이 없다 — 이모지 자리가 □ 다");
            Assert.IsTrue(fa.fallbackFontAssetTable != null && fa.fallbackFontAssetTable.Contains(em), "폴백 표에 이모지 애셋이 걸려 있어야 TMP 가 찾는다");
            foreach (uint cp in new uint[] { 0x23F1, 0x23F9, 0x1F62D, 0x1F434, 0x1F43E, 0x1F6AA, 0x1F525, 0x1F6E1 })
            {
                string e = char.ConvertFromUtf32((int)cp);
                Assert.IsTrue(Has(em, cp), "이모지 폴백 글꼴에 «" + e + "»(U+" + cp.ToString("X") + ") 가 없다 — 서브셋을 다시 뽑는다(docs/assets-map.md)");
                Assert.IsFalse(Has(fa, cp), "주 글꼴이 «" + e + "» 를 직접 쥘 리 없다(서브셋 밖) — 폴백이 그린다");
            }
            Assert.IsFalse(Has(em, '가'), "이모지 글꼴은 한글을 안 쥔다(서브셋이 이모지뿐)");
        }

        /// <summary>글꼴 애셋이 코드포인트 하나를 쥐는가 — 동적 애셋이라 먼저 올려 보고(TryAddCharacters) 표에서 찾는다. `HasCharacter` 는 char 만 받아 BMP 밖(이모지)을 못 묻는다(런 409 · 실제 TMP 에 uint 오버로드가 없다).</summary>
        private static bool Has(TMP_FontAsset fa, uint cp)
        {
            if (fa == null) return false;
            fa.TryAddCharacters(char.ConvertFromUtf32((int)cp));
            return fa.characterLookupTable != null && fa.characterLookupTable.ContainsKey(cp);
        }

        /// <summary>주 글꼴 또는 이모지 폴백(`UiFont.EmojiFallback`)이 쥐는가 — 정본이 글자로 쓰는 이모지 여덟만 이 물음을 쓴다(그 밖은 «주 글꼴 혼자» · T112).</summary>
        private static bool HasWithEmoji(TMP_FontAsset fa, uint cp)
        {
            return Has(fa, cp) || Has(UiFont.EmojiFallback, cp);
        }

        /// <summary>카탈로그 글꼴 자체가 한글을 쥐고 있는가(OS 폴백에 기대지 않는다 — 리눅스 CI·WebGL 에는 없다).</summary>
        [Test]
        public void 카탈로그_글꼴이_한글을_직접_쥔다()
        {
            TMP_FontAsset fa = UiFont.Primary;
            foreach (char c in "대장간던전소환퀘스트상점펫스킬탈것승천장비제작판매설정프로필리그채팅ㅋㅠ")   // T100 2회차 — 자모(ㅋ·ㅠ)도 직접 쥔다(서브셋에 U+3130~318F 를 더했다 · 빼면 채팅이 □)
                // tryAddCharacter=true — 런타임 폰트 애셋은 «동적» 이라 아직 아틀라스에 안 올라간 글자는 false 다(런 216 · ㅋ).
                // 앞선 테스트가 그 글자를 그렸느냐에 따라 흔들리지 않게, 원본 글꼴에서 찾아 올려 본다(폴백은 여전히 안 본다).
                Assert.IsTrue(fa.HasCharacter(c, false, true), "카탈로그 글꼴에 '" + c + "' 가 없다 — OS 폴백이 없는 환경에서 두부가 된다");
        }

        private static string Path(Transform t)
        {
            string s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }
    }
}
