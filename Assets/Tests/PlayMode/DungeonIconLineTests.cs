using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;
using TMPro;

namespace Forge.Tests.PlayMode
{
    /// <summary>T433 — 인라인 아이콘이 든 줄의 상자: 정본 `.ico` 기본 1.45em(863·1763·2597)이 줄 상자를 민다.
    /// 던전 상세의 보상 알약(아이콘 둘)과 열쇠 줄(아이콘 + 숫자)이 «글자 줄 상자» 가 아니라 «아이콘 줄 상자 + 패딩» 으로 서는가.
    /// 여는 채비는 `DungeonStageRowTests` 그대로(새 세이브는 hammer 가 잠겨 해금 뒤 연다).
    /// T468 — 그 줄 상자의 글자 크기는 종류 칸(Sub 36 · Title 60)이 아니라 정본 .88rem(5331) · 1.5rem(5342)이다 — 종류 칸을 넣으면 1.45 배가 그대로 커져 알약 +4.2px · 열쇠 +6.0px 였다.</summary>
    public class DungeonIconLineTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost 가 20초 안에 Ready 되지 않았다");
                yield return null;
            }
            yield return null;
        }

        static Transform FindDeep(Transform t, string name)
        {
            foreach (Transform c in t.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c;
            return null;
        }

        [UnityTest]
        public IEnumerator 던전_상세_보상_알약과_열쇠_줄은_아이콘_줄_상자_1_45em_으로_선다()
        {
            Assert.AreEqual(1.45f, DungeonStyle.L("ico_em"), 1e-6f, "정본 .ico 기본 1.45em(863·1763·2597)");

            yield return Boot();
            DungeonUiHost.Instance.S.BestChapter = 5;
            DungeonUiHost.Instance.S.BestStage = 1;
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null;
            yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 열린다(해금 뒤)");
            Canvas.ForceUpdateCanvases();
            RectTransform app = UiRoot.Instance.App;
            Transform overlay = FindDeep(app, "modal-dungeon-detail");
            Assert.IsNotNull(overlay, "던전 상세 팝업");
            RectTransform card = overlay.Find("card") as RectTransform;
            Assert.IsNotNull(card, "카드");
            RectTransform pill = card.Find("reward-pill") as RectTransform;
            RectTransform keys = FindDeep(card, "keys") as RectTransform;
            Assert.IsNotNull(pill, "보상 알약"); Assert.IsNotNull(keys, "열쇠 줄 글자");

            // T468 — 두 줄의 글자 크기는 정본 그대로(5331 .88rem · 5342 1.5rem) · 종류 칸(Sub 36 · Title 60)이 아니다
            float pillFs = TextSizeUi.Px("dgd_pill"), keysFs = DungeonPopups.Rem(DungeonStyle.L("dgd_keys_font_rem"));
            Assert.AreEqual(0.88f, TextSizeUi.Rem("dgd_pill"), 1e-6f, "정본 5331 .dgd-reward-pill .88rem");
            Assert.AreEqual(1.5f, DungeonStyle.L("dgd_keys_font_rem"), 1e-6f, "정본 5342 .dgd-keys 1.5rem");
            Assert.Less(pillFs, DungeonPopups.Kind(TextKind.Sub), "알약 글자는 하한 Sub 아래(예외 칸 Micro 의 자리)");
            Assert.Less(keysFs, DungeonPopups.Kind(TextKind.Title), "열쇠 글자는 Title 60 보다 작다(정본 54.6)");
            Assert.GreaterOrEqual(keysFs, DungeonPopups.Kind(TextKind.Head), "열쇠 글자는 Head 하한 48 위 — 예외 칸이 아니다");
            TextMeshProUGUI kt = keys.GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(keysFs, kt.fontSize, 0.01f, "열쇠 글자 = 1.5rem");
            Assert.AreEqual(TextKind.Head, kt.GetComponent<UiTextKindTag>().Kind, "열쇠 글자 종류 = Head(하한 48 ≤ 54.6)");
            TextMeshProUGUI label = FindDeep(pill, "label").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(pillFs, label.fontSize, 0.01f, "알약 라벨 = .88rem");
            Assert.AreEqual(TextKind.Micro, label.GetComponent<UiTextKindTag>().Kind, "알약 글자 종류 = Micro(§1 예외 열두째 자리)");
            int vals = 0;
            foreach (TextMeshProUGUI t in pill.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name.StartsWith("val-", System.StringComparison.Ordinal)) { vals++; Assert.AreEqual(pillFs, t.fontSize, 0.01f, t.name + " = .88rem"); }
            Assert.Greater(vals, 0, "보상 숫자가 하나는 있다");

            // 셈 ↔ 도우미: 아이콘 줄 상자 = 글자 x 1.45 + 기준선 아래 몫(글꼴 descender) · 글자 줄 상자(x 1.25)보다 크다
            var f = UiFont.Primary.faceInfo;
            float pillIco = pillFs * 1.45f + Mathf.Abs(f.descentLine) / f.pointSize * pillFs;
            float keysIco = keysFs * 1.45f + Mathf.Abs(f.descentLine) / f.pointSize * keysFs;
            Assert.AreEqual(pillIco, DungeonDetailPopup.IconLineH(pillFs), 1e-3f, "알약 아이콘 줄 상자");
            Assert.AreEqual(keysIco, DungeonDetailPopup.IconLineH(keysFs), 1e-3f, "열쇠 아이콘 줄 상자");
            Assert.Greater(pillIco, pillFs * 1.25f, "아이콘 줄 상자 > 글자 줄 상자(알약)");
            Assert.Greater(keysIco, keysFs * 1.25f, "아이콘 줄 상자 > 글자 줄 상자(열쇠)");

            float pad = DungeonPopups.RemL("dgd_pill_pad_rem");
            Assert.AreEqual(pillIco + pad * 2f, pill.rect.height, 0.5f, "알약 높이 = 아이콘 줄 상자 + 패딩 x 2(정본 셈 · 글자 줄이 아니다)");
            Assert.Greater(pill.rect.height, pillFs * 1.25f + pad * 2f + 1f, "옛 값(글자 줄 + 패딩)보다 크다");
            Assert.AreEqual(keysIco, keys.rect.height, 0.5f, "열쇠 줄 상자 = 아이콘 줄 상자(1.5rem)");
            // T468 — 종류 칸으로 세던 옛 상자(Sub 36 · Title 60 에 1.45)보다 작다: 알약 −4.2px · 열쇠 −6.0px(등재문)
            float oldPill = DungeonDetailPopup.IconLineH(DungeonPopups.Kind(TextKind.Sub)) + pad * 2f, oldKeys = DungeonDetailPopup.IconLineH(DungeonPopups.Kind(TextKind.Title));
            Assert.Less(pill.rect.height, oldPill - 3f, "알약이 종류 칸(Sub)으로 세던 옛 값보다 작다 · 실측 " + pill.rect.height.ToString("0.0") + " ↔ 옛 " + oldPill.ToString("0.0"));
            Assert.Less(keys.rect.height, oldKeys - 5f, "열쇠 줄이 종류 칸(Title)으로 세던 옛 값보다 작다 · 실측 " + keys.rect.height.ToString("0.0") + " ↔ 옛 " + oldKeys.ToString("0.0"));
            Debug.Log("[T468] 알약 " + pill.rect.height.ToString("0.0") + "px(옛 " + oldPill.ToString("0.0") + ") · 열쇠 줄 " + keys.rect.height.ToString("0.0") + "px(옛 " + oldKeys.ToString("0.0") + ") · 카드 " + card.rect.height.ToString("0.0"));

            DungeonDetailPopup.Close();
            yield return null;
        }
    }
}
