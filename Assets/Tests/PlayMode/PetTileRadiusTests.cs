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
    /// T345 19회차 — 펫 타일의 모서리는 **자리마다 다르다**: 격자 타일은 정본 4262 `.pet-tile .tile-face` **.5rem**,
    /// 상세 팝업 타일은 5536 `.petd-tile` **.55rem**(그 규칙은 펫·알·탈것 상세 셋 다에 걸린다).
    /// 클론은 셋 다 `tile_r_rem` .5 로 그렸고 표에 있던 `petd_tile_r_rem` .55 는 부르는 자리가 0 이었다.
    /// 새 세이브 → 알 소환 → 즉시 부화 → 펫 하나(`ColorMixSitesTests` 와 같은 길).
    /// </summary>
    public class PetTileRadiusTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static float Mult(string key) { return UiShapes.RoundedMultiplier(PetSkillStyle.Px(key)); }

        static Transform FindFace(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "tile-face" && t.Find("face") != null) return t;
            return null;
        }

        [Test]
        public void 표는_격자_5rem_과_상세_55rem_을_따로_쥔다()
        {
            Assert.AreEqual(0.5f * PetSkillStyle.Rem(1f), PetSkillStyle.Px("tile_r_rem"), 1e-3f,
                "정본 4262 `.pet-tile .tile-face { border-radius: .5rem }`");
            Assert.AreEqual(0.55f * PetSkillStyle.Rem(1f), PetSkillStyle.Px("petd_tile_r_rem"), 1e-3f,
                "정본 5536 `.petd-tile { border-radius: .55rem }`");
            Assert.Greater(PetSkillStyle.Px("petd_tile_r_rem"), PetSkillStyle.Px("tile_r_rem"),
                "상세 타일이 격자 타일보다 더 둥글다 — 둘이 같으면 이 회차의 고침이 되돌려진 것이다");
        }

        [UnityTest]
        public IEnumerator 격자_타일은_5rem_상세_타일은_55rem_으로_선다()
        {
            yield return Boot();
            for (int i = 0; i < 600 && !(PetSkillHost.Ready && SkillPetSheet.Instance != null); i++) yield return null;
            Assert.IsTrue(PetSkillHost.Ready, "PetSkillHost");
            PetSkillHost host = PetSkillHost.Instance;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            yield return null;
            sheet.Switch(SkillPetSheet.SubPets);
            yield return null;
            while (host.SummonMult("pet") != 1) host.CycleSummonMult("pet");
            host.EggCurrency = 100000; host.Gems = 100000; host.Sync();
            yield return null;
            sheet.Pets.SummonButton.onClick.Invoke();
            yield return null;
            for (int k = 0; k < 4 && SkillSummonResultView.Current != null; k++) { SkillSummonResultView.Current.OnTap(); yield return null; }
            Assert.GreaterOrEqual(host.Pets.State.Eggs.Count, 1, "x1 소환 = 알 하나 이상");
            int hatching = host.Pets.State.Hatching.Count;
            sheet.Pets.OpenEggDetail(0);
            yield return null;
            sheet.Modal.Find(PetPanel.DetailModal).Content.Find("btn-hatch").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(sheet.Pets.SkipButton(hatching), "부화 칸의 스킵");
            sheet.Pets.SkipButton(hatching).onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, host.Pets.State.Pets.Count, "즉시 부화 → 펫 하나");

            // 격자 타일 — 모달 밖의 tile-face
            Canvas.ForceUpdateCanvases();
            Transform grid = null;
            foreach (Transform t in sheet.GetComponentsInChildren<Transform>(true))
                if (t.name == "tile-face" && t.Find("face") != null && !t.IsChildOf(sheet.Modal.transform)) { grid = t; break; }
            Assert.IsNotNull(grid, "격자의 펫 타일(tile-face)");
            // `PetSkillKit.Framed` 는 테(line)에 표값을, 안쪽 면(face)에 «표값 − 테 폭» 을 준다 — 자리 값을 쥔 쪽은 테다.
            Image gLine = grid.Find("line").GetComponent<Image>();
            Assert.AreEqual(UiShapes.Rounded, gLine.sprite, "격자 타일 테: 둥근 9-슬라이스");
            Assert.AreEqual(Mult("tile_r_rem"), gLine.pixelsPerUnitMultiplier, 1e-3f,
                "격자 타일 테: 정본 4262 .5rem(표 tile_r_rem)");

            // 펫 상세 타일 — 같은 그림이지만 모서리는 .55rem 이다
            sheet.Pets.OpenPetDetail(0);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Transform detail = FindFace(sheet.Modal.Find(PetPanel.DetailModal).Content);
            Assert.IsNotNull(detail, "펫 상세의 타일(tile-face)");
            Image dLine = detail.Find("line").GetComponent<Image>();
            Assert.AreEqual(Mult("petd_tile_r_rem"), dLine.pixelsPerUnitMultiplier, 1e-3f,
                "펫 상세 타일 테: 정본 5536 .55rem(표 petd_tile_r_rem)");
            Assert.AreNotEqual(Mult("tile_r_rem"), dLine.pixelsPerUnitMultiplier,
                "상세 타일이 격자 타일과 같은 모서리로 서면 19회차 전으로 돌아간 것이다");
        }
    }
}
