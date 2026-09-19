using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T415 17회차 — 둥근 모서리 셋: 던전 상세 영웅 판(정본 2060 `.dg-detail-hero { border-radius: .6rem }` → 상자 자신이 둥근 마스크) ·
    /// 스킬 상세 조각 게이지(5244 `.skd-orbcol .sk-shard` .5rem · 표 `skd_shard_r_rem`) · 장착 오브 어둠 막(4087 `.sk-orb.equipped::after` 50% = `PetSkillKit.Disc` 원판).
    /// </summary>
    public class SkillRadiusTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindDeep(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [Test]
        public void 표는_영웅_판_6rem_상세_조각_게이지_5rem_버튼_7rem_을_정본_값_그대로_쥔다()
        {
            float rem = RadiusUi.PxPerRem;
            Assert.AreEqual(0.6f, RadiusUi.Px("dgd_hero_r_rem") / rem, 1e-4f, "style.css 2060 .dg-detail-hero .6rem");
            Assert.AreEqual(0.5f, RadiusUi.Px("skd_shard_r_rem") / rem, 1e-4f, "style.css 5244 .skd-orbcol .sk-shard .5rem");
            Assert.AreEqual(0.7f, PetSkillStyle.L("btn_r_rem"), 1e-6f, "style.css 5209 .summon-btn · 5259 .skd-btn .7rem = PaperButton 기본");
        }

        [UnityTest]
        public IEnumerator 던전_상세_영웅_판은_표_반지름의_둥근_마스크이고_장착_어둠_막은_원판이다()
        {
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
            Transform overlay = FindDeep(UiRoot.Instance.App, "modal-dungeon-detail");
            Assert.IsNotNull(overlay, "던전 상세 팝업");
            RectTransform hero = overlay.Find("card/hero") as RectTransform;
            Assert.IsNotNull(hero, "영웅 판(hero)");
            Image mask = hero.GetComponent<Image>();
            Assert.IsNotNull(mask, "상자 자신이 둥근 마스크 그림을 갖는다");
            Assert.AreSame(UiShapes.Rounded, mask.sprite, "둥근 스프라이트");
            Assert.AreEqual(UiShapes.RoundedMultiplier(RadiusUi.Px("dgd_hero_r_rem")), mask.pixelsPerUnitMultiplier, 1e-4f, "반지름 = 표 dgd_hero_r_rem(.6rem)");
            Mask m = hero.GetComponent<Mask>();
            Assert.IsNotNull(m, "Mask — 바탕 겹·일러스트·제목이 둥근 상자 안에서 잘린다");
            Assert.IsFalse(m.showMaskGraphic, "마스크 그림 자체는 안 보인다(그림은 바탕 겹이 그린다)");
            Assert.IsFalse(mask.raycastTarget, "입력 안 받음");
            Transform bg = hero.Find("bg-grad");
            Assert.IsNotNull(bg, "바탕 겹은 그대로 hero 의 자식");
            Assert.AreEqual(0, bg.GetSiblingIndex(), "바탕 겹은 여전히 첫 자식");
            DungeonDetailPopup.Close();
            yield return null;

            // 장착 오브 어둠 막 — 정본 4087 50% = 원판. 공장이 주는 스프라이트가 원이면 그 자리는 원이다(`#` 창이 못 지키는 자리 · 자 TABLE ✓ 의 근거).
            Transform tmp = new GameObject("tmp-orb", typeof(RectTransform)).transform;
            tmp.SetParent(UiRoot.Instance.App, false);
            Image dimm = PetSkillKit.Disc(tmp, "equipped", Color.black);
            Assert.AreSame(UiShapes.Circle, dimm.sprite, "PetSkillKit.Disc = UiKit.Circle 원판 스프라이트");
            Assert.IsTrue(dimm.preserveAspect, "원은 비율을 지킨다");
            Object.Destroy(tmp.gameObject);
            yield return null;
        }
    }
}
