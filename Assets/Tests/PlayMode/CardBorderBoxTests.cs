using System.Collections;
using NUnit.Framework;
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
    /// T465 — 클론이 CSS `padding` 을 테두리 **바깥**에서 재던 것을 바로잡았다: 팝업 카드(`PopupKit.Card` · `DungeonPopups.Card`)의 rect 는
    /// 이제 CSS 의 **패딩 상자**이고 테(`line`/`bg`)·면·그늘은 그 rect 보다 위·아래로 `line3_px` 씩 **밖에** 선다(`PopupKit.GrowY`).
    /// 곧 호출부가 `pad` 로 놓는 첫 자식은 테 안쪽에서 `pad` 만큼 떨어지고(정본 1752 `.modal-card { padding: 1.1rem }` + 3542 `border: var(--ol3)`),
    /// 카드 몸(border-box)은 rect 보다 `2 × line3` 높다. ✕ 는 그 몸의 아래변(rect 아래변 − line3)에 걸린다.
    /// 장비 시트(정본 816·3629 `#equip-sheet { padding: .55rem .6rem; border-top: var(--ol3) solid }`)는 위 테 한 겹만큼 첫 칸이 내려간다.
    /// </summary>
    public class CardBorderBoxTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready && UiRoot.Instance != null && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready && MetaHost.Ready && DungeonUiHost.Ready, "호스트 셋이 20초 안에 준비되지 않았다");
            yield return null;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        /// <summary>테 상자가 «부모를 채우되 위·아래로 line3 만큼 밖» 인지 — 좌우는 옛 그대로(0)다.</summary>
        static void AssertGrownY(RectTransform rt, float line3, string what)
        {
            Assert.AreEqual(0f, rt.anchorMin.x, 1e-4f, what + " 앵커 가로 0"); Assert.AreEqual(1f, rt.anchorMax.x, 1e-4f, what + " 앵커 가로 1");
            Assert.AreEqual(0f, rt.anchorMin.y, 1e-4f, what + " 앵커 세로 0"); Assert.AreEqual(1f, rt.anchorMax.y, 1e-4f, what + " 앵커 세로 1");
            Assert.AreEqual(-line3, rt.offsetMin.y, 0.01f, what + " 아래로 line3 만큼 밖");
            Assert.AreEqual(line3, rt.offsetMax.y, 0.01f, what + " 위로 line3 만큼 밖");
            Assert.AreEqual(0f, rt.offsetMin.x, 0.01f, what + " 좌우는 rect 그대로(남은 몫)");
            Assert.AreEqual(0f, rt.offsetMax.x, 0.01f, what + " 좌우는 rect 그대로(남은 몫)");
        }

        [UnityTest]
        public IEnumerator 공용_카드는_rect_가_패딩_상자고_테_면_그늘이_위아래로_line3_만큼_밖에_서며_X_는_그_몸의_아래변에_걸린다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            CardPop.SettleAll();
            Popup p = h.Popups.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업");
            RectTransform card = (RectTransform)Find(p.Root, "card");
            Assert.IsNotNull(card, "카드");
            float line3 = PopupKit.Line3;
            Assert.AreEqual(UiKit.L("line3_px"), line3, 1e-4f, "테 두께는 표 line3_px");
            Assert.Greater(line3, 0f, "테 두께 > 0");

            RectTransform line = (RectTransform)card.Find("line");
            RectTransform face = (RectTransform)card.Find("face");
            Assert.IsNotNull(line, "테(line)"); Assert.IsNotNull(face, "면(face)");
            AssertGrownY(line, line3, "테");
            // 면 = 테 안쪽: 세로는 rect 그대로(위끝 = 카드 rect 위끝) · 가로만 옛 테 몫(line3)
            Assert.AreEqual(0f, face.offsetMin.y, 0.01f, "면 아래끝 = rect 아래끝"); Assert.AreEqual(0f, face.offsetMax.y, 0.01f, "면 위끝 = rect 위끝");
            Assert.AreEqual(line3, face.offsetMin.x, 0.01f, "면 왼끝 = 테 안쪽"); Assert.AreEqual(-line3, face.offsetMax.x, 0.01f, "면 오른끝 = 테 안쪽");
            Rect rc = World(card), rl = World(line), rf = World(face);
            Assert.AreEqual(rc.height, rf.height, rc.height * 0.002f, "면 높이 = rect 높이");
            Assert.AreEqual(rc.height * (1f + 2f * line3 / card.rect.height), rl.height, rc.height * 0.002f, "카드 몸(테) 높이 = rect + 2·line3");
            Assert.Greater(rl.yMax, rc.yMax, "테 위끝이 rect 위끝보다 위"); Assert.Less(rl.yMin, rc.yMin, "테 아래끝이 rect 아래끝보다 아래");

            // 그늘도 카드 몸에 진다 — 딱딱한 턱은 세로로 2·line3 만큼 더 크다
            Transform lip = UiShadow.Find(card, "card_lip");
            Assert.IsNotNull(lip, "공용 아래턱");
            RectTransform lipRt = (RectTransform)lip;
            Assert.AreEqual(2f * line3, lipRt.offsetMax.y - lipRt.offsetMin.y, 0.01f, "턱의 세로 여분 = 2·line3(위·아래 line3 씩)");

            // ✕ 는 카드 몸의 아래변에 걸린다 — 앵커는 rect 아래변(0) · 오프셋이 line3 만큼 더 내려간다
            RectTransform x = (RectTransform)Find(card, "x-btn");
            Assert.IsNotNull(x, "✕");
            Assert.AreEqual(0f, x.anchorMin.y, 1e-4f, "✕ 앵커 = rect 아래변");
            float d = UiKit.H("xbtn");
            Assert.AreEqual(d * (0.5f - UiKit.L("xbtn_over")) - line3, x.anchoredPosition.y, 0.01f, "✕ 가운데 = 몸의 아래변 + 지름 × (.5 − 걸침)");

            // 표값(profile_h · 원작 카드 몸)은 그대로고 rect 만 2·line3 작다 — 화면의 카드 몸은 표 그대로다
            Assert.AreEqual(UiKit.L("profile_h") * UiKit.RefH, card.rect.height + 2f * line3, 0.5f, "카드 몸 = 표 profile_h");
            Debug.Log("[T465] 공용 카드 rect " + card.rect.height.ToString("0.0") + " · 몸 " + (card.rect.height + 2f * line3).ToString("0.0") + " · line3 " + line3.ToString("0.0"));
            ProfilePopup.Close(h);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 던전_계열_카드도_테가_rect_밖에_서고_첫_자식은_테_안쪽에서_pad_만큼_떨어진다()
        {
            yield return Boot();
            AscendPopup.Open();
            yield return null;
            Transform root = AscendPopup.Root;   // T414 — 앱 뿌리가 아니라 이 팝업의 뿌리에서 찾는다
            Assert.IsNotNull(root, "승천 팝업 뿌리");
            RectTransform card = (RectTransform)Find(root, "card");
            Assert.IsNotNull(card, "승천 카드");
            float line3 = DungeonPopups.Line3;
            RectTransform bg = (RectTransform)card.Find("bg");
            Assert.IsNotNull(bg, "테(bg)");
            AssertGrownY(bg, line3, "테");
            RectTransform face = (RectTransform)bg.Find("face");
            Assert.IsNotNull(face, "면(bg/face)");
            Rect rc = World(card), rf = World(face);
            Assert.AreEqual(rc.yMax, rf.yMax, rc.height * 0.002f, "면 위끝 = rect 위끝(테 안쪽)");
            Assert.AreEqual(rc.yMin, rf.yMin, rc.height * 0.002f, "면 아래끝 = rect 아래끝");
            // 첫 자식(제목 줄 `title-row` · 안의 "title" 은 HorizontalLayoutGroup 이 놓는다)은 rect 위끝에서 pad — 곧 테 안쪽에서 pad(정본 padding)
            RectTransform title = (RectTransform)Find(card, "title-row");
            Assert.IsNotNull(title, "제목 줄(title-row)");
            float pad = DungeonPopups.RemL("card_pad_rem");
            Assert.AreEqual(-pad, title.anchoredPosition.y, 0.5f, "제목 위끝 = rect(테 안쪽) 위끝 + pad");
            RectTransform x = (RectTransform)Find(card, "x-btn");
            Assert.IsNotNull(x, "✕");
            Assert.AreEqual(-line3, x.anchoredPosition.y, 0.01f, "✕ 가운데 = 카드 몸의 아래변(rect 아래변 − line3)");
            Rect rx = World(x);
            Assert.AreEqual(rc.yMin - line3 * (rc.height / card.rect.height), rx.center.y, rc.height * 0.002f, "✕ 가운데가 몸의 아래변에 있다(월드)");
            AscendPopup.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator 장비_시트의_첫_칸은_위_테_한_겹에_패딩_55rem_을_더한_자리다()
        {
            yield return Boot();
            RectTransform grid = (RectTransform)Find(UiRoot.Instance.App, "equip-grid");
            Assert.IsNotNull(grid, "장비 격자");
            float rem = PopupKit.Rem;
            Assert.AreEqual(-(rem * 0.55f + PopupKit.Line3), grid.anchoredPosition.y, 0.01f, "격자 위끝 = 위 테(line3) + .55rem — 정본 816 padding 은 border-top 안쪽부터");
            Debug.Log("[T465] 장비 격자 위끝 " + (-grid.anchoredPosition.y).ToString("0.0") + "px = line3 " + PopupKit.Line3.ToString("0.0") + " + .55rem " + (rem * 0.55f).ToString("0.0"));
        }
    }
}
