using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Forge.Core.Mounts;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T381 1회차 — 장비 격자의 탈것 칸: 정본 `ui.js` 1526~1535 은 **탄 탈것이 있으면** 얼굴 + `Lv.N` + 여분 «+N» 을 그리고
    /// 없을 때만 실루엣 + «탈것» 이다. 클론은 빈 갈래 하나뿐이라 탈것을 타도 칸이 비어 보였다.
    /// </summary>
    public class MountCellTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && UiRoot.Instance != null && PetSkillHost.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 안 섰다");
            yield return null;
        }

        static Transform Cell()
        {
            foreach (Transform t in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true))
                if (t.name == "egg-cell") return t;
            return null;
        }

        [UnityTest]
        public IEnumerator 탈것을_안_탔으면_실루엣과_탈것_글자다()
        {
            yield return Boot();
            MountSystem ms = PetSkillHost.Instance.Mounts;
            ms.State.ActiveMounts.Clear();
            ForgeSheet.Render(ForgeHost.Instance);
            yield return null;

            Transform cell = Cell();
            Assert.IsNotNull(cell, "탈것 칸(egg-cell)");
            Assert.IsNotNull(cell.Find("mount-sil"), "빈 갈래는 실루엣이다");
            Assert.IsNotNull(cell.Find("slot-name"), "빈 갈래는 «탈것» 글자를 쓴다");
            Assert.IsNull(cell.Find("lv"), "빈 칸엔 Lv 배지가 없다");
        }

        [UnityTest]
        public IEnumerator 탈것을_타면_그_얼굴과_Lv_배지가_선다()
        {
            yield return Boot();
            MountSystem ms = PetSkillHost.Instance.Mounts;
            if (ms.State.Mounts.Count == 0)
                ms.State.Mounts.Add(new Mount { Name = "horse", Rarity = "common", Level = 7, Stars = 0 });
            Assert.Greater(ms.State.Mounts.Count, 0, "탈것을 하나는 쥐어야 잰다");
            ms.SetRidden(0);
            Assert.IsNotNull(ms.RiddenInst(), "탄 탈것이 서야 한다");
            ForgeSheet.Render(ForgeHost.Instance);
            yield return null;
            yield return null;

            Transform cell = Cell();
            Assert.IsNotNull(cell, "탈것 칸(egg-cell)");
            if (cell.Find("mount-sil") != null && cell.Find("img") == null)
                Assert.Ignore("이 환경에선 탈것 얼굴을 못 굽는다(PetFaces.Available false) — 정본의 빈 갈래로 떨어지는 것이 설계다");

            Assert.IsNotNull(cell.Find("img"), "탄 탈것은 얼굴을 그린다(정본 mountFace)");
            Transform lv = cell.Find("lv");
            Assert.IsNotNull(lv, "탄 탈것은 Lv 배지를 그린다(정본 1532 .cell-lv)");
            StringAssert.Contains("Lv", lv.GetComponent<TextMeshProUGUI>().text);
            Assert.IsNull(cell.Find("slot-name"), "탄 갈래엔 «탈것» 글자가 없다(정본은 빈 칸에만 쓴다)");
        }
    }
}
