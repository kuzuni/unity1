using System.IO;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T352 4회차 — **굵기는 색이 아니라 «폭» 도 바꾼다.**
    /// 진짜 굵은 판이 없는 글꼴에 <see cref="FontStyles.Bold"/> 를 주면 TMP 는 획을 두껍게 하고(`boldStyle`)
    /// **글자마다 `boldSpacing`/100 em 만큼 자간을 더한다**. 브라우저(정본)는 굵기를 흉내 낼 때 **자폭을 안 늘린다** —
    /// 그러니 그 자간은 **클론만 무는 폭 비용**이고, 정본이 `font-weight` 225 자리를 bold 로 두는 이 이식에서는
    /// 글자 하한(T136)과 **겹쳐 쌓인다**.
    ///
    /// 이 자는 그 비용을 **수로 못박는다**(고치는 자가 아니다 — 고침은 «진짜 굵은 판을 붙인다» 또는 «boldSpacing 0»
    /// 이고 그것은 굵기 공장을 여는 회차 몫이다). 실제로 물린 자리 하나: T397(리그 도전 행 상대 이름) —
    /// «BlandBuddy22667» 은 민 자폭 합 8.534em 이라 36px 에서 307.2px 로 칸 330.2 에 **드는데**,
    /// 굵기 자간이 붙어 **342.5px**(런 809 실측)이 되어 접힌다.
    /// </summary>
    public class BoldWidthTests
    {
        const string Sample = "BlandBuddy22667";   // T397 이 물린 그 이름(15자 · 라틴+숫자)
        const float Size = 36f;                    // 하한 `TextKind.Sub`

        static TextMeshProUGUI Label(Transform parent, string name, bool bold)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.font = UiFont.Primary;
            t.fontSize = Size;
            t.richText = false;
            t.enableWordWrapping = false;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.text = Sample;
            return t;
        }

        [Test]
        public void 굵게_찍으면_글자마다_boldSpacing_만큼_넓어진다()
        {
            TMP_FontAsset fa = UiFont.Primary;
            Assert.IsNotNull(fa, "런타임 글꼴 애셋");

            GameObject root = new GameObject("bold-width", typeof(RectTransform), typeof(Canvas));
            try
            {
                TextMeshProUGUI plain = Label(root.transform, "plain", false);
                TextMeshProUGUI bold = Label(root.transform, "bold", true);
                plain.ForceMeshUpdate();
                bold.ForceMeshUpdate();

                float wPlain = plain.preferredWidth, wBold = bold.preferredWidth;
                float gap = wBold - wPlain;
                // TMP 는 `boldSpacing` 을 «em 의 1/100» 으로 읽어 **글자 사이**에 더한다 — 마지막 글자 뒤엔 안 붙으므로 **n−1** 이다.
                //   (런 809 실측이 그것을 말한다: 15자에서 차 35.3 ↔ n×2.52 = 37.8 이 아니라 (n−1)×2.52 = 35.3.)
                float expect = fa.boldSpacing / 100f * Size * (Sample.Length - 1);

                StringBuilder log = new StringBuilder();
                log.Append("# T352 4회차 — 굵기가 무는 폭\n");
                log.Append("글꼴 boldSpacing ").Append(fa.boldSpacing.ToString("0.###"))
                   .Append(" · boldStyle ").Append(fa.boldStyle.ToString("0.###")).Append('\n');
                log.Append("«").Append(Sample).Append("» ").Append(Size.ToString("0")).Append("px — 민 ")
                   .Append(wPlain.ToString("0.0")).Append(" · 굵게 ").Append(wBold.ToString("0.0"))
                   .Append(" · 차 ").Append(gap.ToString("0.0")).Append("(기대 ").Append(expect.ToString("0.0")).Append(")\n");
                log.Append("배율 ").Append((wBold / Mathf.Max(1f, wPlain)).ToString("0.000"))
                   .Append(" · 글자당 ").Append((gap / Sample.Length).ToString("0.00")).Append("px\n");
                log.Append("T397: 리그 도전 행 이름 칸 330.2px — 민 ").Append(wPlain.ToString("0.0"))
                   .Append(" 은 들고 굵게 ").Append(wBold.ToString("0.0")).Append(" 은 ")
                   .Append(wBold > 330.2f ? "넘는다(접힌다)" : "든다").Append('\n');
                Write(log.ToString());

                Assert.Greater(fa.boldSpacing, 0f, "이 글꼴엔 진짜 굵은 판이 없어 TMP 가 자간으로 굵기를 낸다");
                Assert.Greater(gap, 0f, "굵게 찍으면 폭이 늘어난다 — 정본(브라우저)은 안 늘어나는 몫이다");
                Assert.AreEqual(expect, gap, expect * 0.05f,
                    "늘어난 폭이 boldSpacing 이 말하는 값이다(글자 사이마다 boldSpacing/100 em · n−1) — 이 셈이 깨지면 TMP 판이 바뀐 것이다");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void Write(string text)
        {
            Debug.Log("[T352] " + text);
            try
            {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), GallerySheet.OutDir);
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "t352-bold.txt"), text, new UTF8Encoding(false));
            }
            catch (System.Exception e) { Debug.Log("[T352] 진단 파일을 못 썼다: " + e.Message); }
        }
    }
}
