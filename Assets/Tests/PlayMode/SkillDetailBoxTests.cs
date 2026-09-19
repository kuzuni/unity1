using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T483 — 스킬 상세 팝업: 정본 5237 `.skd-card { width: 74.95% }` 는 «원본 흰 면 371/495» 라 흰 면 = `skd_w_f`(상자 = 흰 면 + 테 둘) ·
    /// `min-height: 20rem` 은 최솟값 · 5259 `.skd-btn` 은 높이 선언이 없어 내용(글자 1.02rem × normal + 패딩 .68 × 2 + 키라인 둘)이 정한다 — 표의 근거 없는 `skd_btn_h_rem` 2.75 를 걷었다.
    /// </summary>
    public class SkillDetailBoxTests
    {
        static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260912;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready && SkillBar.Instance != null); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;
        }

        static SkillPetSheet Sheet { get { return SkillPetSheet.Instance; } }
        static PetSkillHost Host { get { return PetSkillHost.Instance; } }

        [Test]
        public void 표는_버튼_글자_1_02rem_과_세로_패딩_68rem_을_쥐고_옛_고정_높이는_없다()
        {
            Assert.AreEqual(1.02f, PetSkillStyle.L("skd_btn_font_rem"), 1e-6f, "정본 5259 .skd-btn 1.02rem");
            Assert.AreEqual(0.68f, PetSkillStyle.L("skd_btn_pad_y_rem"), 1e-6f, "정본 5259 padding .68rem 0");
            Assert.AreEqual(20f, PetSkillStyle.L("skd_min_h_rem"), 1e-6f, "정본 5237 min-height 20rem 은 최솟값으로 남는다");
            Assert.AreEqual(0.7495f, PetSkillStyle.L("skd_w_f"), 1e-6f, "정본 5237 width 74.95% — 흰 면의 수");
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => PetSkillStyle.L("skd_btn_h_rem"), "정본에 없던 고정 높이 2.75 는 표에서 걷었다");
        }

        [UnityTest]
        public IEnumerator 스킬_상세_카드는_흰_면이_표값이고_버튼_둘은_내용_높이로_같으며_카드는_20rem_이상이다()
        {
            yield return Boot();
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            Sheet.Switch(SkillPetSheet.SubSkills);
            yield return null;
            string id = Host.Skills.State.Skills.KeyAt(0);
            Sheet.Skills.OpenSkillDetail(id);
            yield return null; yield return null;
            Assert.IsTrue(Sheet.Modal.IsOpen(SkillPanel.DetailModal), "스킬 상세가 열린다");
            Canvas.ForceUpdateCanvases();
            PetSkillModal.Handle h = Sheet.Modal.Find(SkillPanel.DetailModal);
            RectTransform card = h.Card;
            float line3 = PetSkillKit.Line3;
            Assert.AreEqual(PetSkillStyle.L("skd_w_f") * UiKit.RefW, card.rect.width - line3 * 2f, 0.5f, "카드 흰 면(상자 − 테 둘) = skd_w_f × 앱 폭(정본 «원본 흰 면 371/495»)");
            Assert.GreaterOrEqual(card.rect.height, PetSkillStyle.Px("skd_min_h_rem") - 0.5f, "카드 높이 ≥ 20rem(최솟값)");
            RectTransform up = h.Content.Find("btn-upgrade") as RectTransform, eq = h.Content.Find("btn-equip") as RectTransform;
            Assert.IsNotNull(up, "업그레이드 버튼"); Assert.IsNotNull(eq, "장착 버튼");
            var face = UiFont.Primary.faceInfo;
            float one = PetSkillStyle.Px("skd_btn_font_rem") * (face.lineHeight / face.pointSize) + PetSkillStyle.Px("skd_btn_pad_y_rem") * 2f + line3 * 2f;
            bool maxed = up.Find("sub") != null;
            float want = (maxed ? 2f : 1f) * PetSkillStyle.Px("skd_btn_font_rem") * (face.lineHeight / face.pointSize) + PetSkillStyle.Px("skd_btn_pad_y_rem") * 2f + line3 * 2f;
            Assert.AreEqual(want, up.rect.height, 0.5f, "업그레이드 버튼 높이 = 줄 수 × 1.02rem × normal + .68rem × 2 + 키라인 둘(만렙이면 두 줄)");
            Assert.AreEqual(up.rect.height, eq.rect.height, 0.5f, "두 버튼은 같은 높이(정본 .skd-btns flex 행)");
            Assert.AreEqual(up.anchoredPosition.y, eq.anchoredPosition.y, 0.5f, "두 버튼은 같은 줄");
            Assert.Greater(one, 2.75f * PetSkillStyle.RemPx + 0.5f, "한 줄 내용 높이가 옛 고정값 2.75rem 보다 크다(정본 계약 ≈3.17rem)");
            // 패시브 알약·라벨이 버튼 위에 있고 머리와 안 겹친다
            RectTransform pill = h.Content.Find("skd-passive") as RectTransform, label = h.Content.Find("skd-passive-label") as RectTransform, orbcol = h.Content.Find("skd-orbcol") as RectTransform;
            Assert.IsNotNull(pill); Assert.IsNotNull(label); Assert.IsNotNull(orbcol);
            float pillBottom = -pill.anchoredPosition.y + pill.rect.height, btnTop = -up.anchoredPosition.y;
            Assert.LessOrEqual(pillBottom, btnTop + 0.5f, "패시브 알약은 버튼 줄 위에 있다(안 겹친다)");
            float headBottom = -orbcol.anchoredPosition.y + orbcol.rect.height, labelTop = -label.anchoredPosition.y;
            Assert.LessOrEqual(headBottom, labelTop + 0.5f, "머리(오브 열)와 패시브 라벨이 안 겹친다");
            Debug.Log("[T483] 카드 " + card.rect.width.ToString("0.0") + "×" + card.rect.height.ToString("0.0") + " · 흰 면 " + ((card.rect.width - line3 * 2f) / UiKit.RefW * 100f).ToString("0.00") + "%W · 버튼 " + up.rect.height.ToString("0.0") + "px(" + (up.rect.height / PetSkillStyle.RemPx).ToString("0.00") + "rem · " + (up.rect.height / UiKit.RefH * 100f).ToString("0.00") + "%H) · 만렙 " + maxed);
            h.XButton.onClick.Invoke();
            yield return null;
        }
    }
}
