using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T450 — 플레이어 정보 «보유 옵션» 목록의 줄 피치는 정본 `.pinfo-subs-list`(style.css 5600) `font-size: .88rem; line-height: 1.14` 그대로
    /// **글자 × 배수** 뿐이어야 한다(블록 안 글줄 · 줄 사이 추가 틈 0). 전엔 글자가 `Sub` 하한 36(+13%)이고 줄마다 rem*0.1 을 더해
    /// 피치가 2.23%H(정본 1.90%H) 였다. 자는 세 가지를 본다 — ⓐ 피치가 정본 1.8%H ±0.15 안 ⓑ 목록 틈 0
    /// ⓒ 고침이 **이 자리만**(Micro 예외 칸 + 크기표)이라 종류 `Sub` 의 단은 안 움직였다.
    /// </summary>
    public class PlayerInfoSubsSizeTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready || UiRoot.Instance == null)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost/UiRoot 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [UnityTest]
        public IEnumerator 보유_옵션_줄은_정본_88rem_글자에_틈_0_이라_피치가_1_8퍼센트H_안이다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(MetaHost.Ready && UiRoot.Instance != null); i++) yield return null;
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            MetaHost h = MetaHost.Instance;
            var saved = PlayerInfoPopup.SubLines;
            PlayerInfoPopup.SubLines = () => new List<string>
                { "공격력 +12.5%", "치명타 확률 +3.1%", "체력 +8.0%", "골드 획득 +4.4%", "치명타 피해 +6.2%" };
            try
            {
                PlayerInfoPopup.Open(h);
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                Popup p = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
                Assert.IsNotNull(p, "플레이어 정보 팝업");
                Transform subs = Find(p.Root, "subs");
                Assert.IsNotNull(subs, "보유 옵션 상자(subs)");
                Transform line = Find(subs, "sub");
                Assert.IsNotNull(line, "보유 옵션 줄(sub)");
                TextMeshProUGUI t = line.GetComponent<TextMeshProUGUI>();
                Assert.IsNotNull(t, "그 줄은 글자 하나다");

                // ⓒ 이 자리만 — 종류 표식은 §1 예외 칸 Micro 이고, 크기는 크기표의 정본 .88rem 이다. 종류 `Sub` 의 단은 그대로다.
                UiTextKindTag tag = line.GetComponent<UiTextKindTag>();
                Assert.IsNotNull(tag, "UiKit 종류 표식");
                Assert.AreEqual(TextKind.Micro, tag.Kind, "정본 .88rem 은 Sub 하한 36 아래라 §1 예외 칸(Micro)을 쓴다 — 결정 633");
                float want = TextSizeUi.Px("pinfo_subs_list");
                Assert.AreEqual(0.88f * PopupKit.Rem, want, 0.01f, "크기표 pinfo_subs_list = 정본 5600 .88rem");
                Assert.AreEqual(want, t.fontSize, 0.01f, "보유 옵션 글자 = 크기표의 정본 크기");
                float subSize = UiCatalog.Instance.Kind(TextKind.Sub).size;
                Assert.GreaterOrEqual(subSize, 36f, "종류 Sub 의 단은 §1 하한 그대로다 — 이 고침은 전역 종류표를 안 건드렸다");
                Assert.Less(t.fontSize, subSize, "이 자리 글자는 종류 Sub 의 단이 아니라 자리 크기다(전엔 Sub 36 = +13%)");

                // ⓑ 목록 틈 0 — 정본은 블록 안 글줄이라 줄 사이 추가 틈이 없다.
                VerticalLayoutGroup g = subs.GetComponentInChildren<VerticalLayoutGroup>(true);
                Assert.IsNotNull(g, "목록 세로 배치");
                Assert.AreEqual(0f, g.spacing, 1e-4f, "보유 옵션 목록 틈 = 0(전엔 rem*0.1 = 1.8px 을 줄마다 더했다)");

                // ⓐ 피치 = 글자 × 1.14 만 — 기준 캔버스 높이 대비 정본 1.8%H ±0.15(정본 셈 32.0 × 1.14 = 36.5px = 1.90%H).
                LayoutElement le = line.GetComponent<LayoutElement>();
                Assert.IsNotNull(le, "줄 상자 높이를 쥔 LayoutElement");
                double ratio = LineHeight.Table.Get("pinfo_subs_list_2_lh");
                Assert.AreEqual((float)(ratio * t.fontSize), le.preferredHeight, 0.01f, "줄 상자 = 표 배수 × 글자 크기");
                float pitchPctH = (le.preferredHeight + g.spacing) / UiKit.RefH * 100f;
                Assert.AreEqual(1.8f, pitchPctH, 0.15f, "보유 옵션 줄 피치(%H) — 정본이 못 박은 1.8%H ±0.15(전엔 2.23)");
                Debug.Log("[T450] 보유 옵션 글자 " + t.fontSize.ToString("0.00") + "px · 줄 상자 " + le.preferredHeight.ToString("0.00") + "px · 틈 " + g.spacing + " · 피치 " + pitchPctH.ToString("0.000") + "%H");
            }
            finally { PlayerInfoPopup.SubLines = saved; }
        }
    }
}
