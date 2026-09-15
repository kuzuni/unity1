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
            // 갈래를 가르는 것은 **상태 하나**다 — 썸네일을 못 굽는 자리(CI)에서도 이 갈래가 돌아야 한다(런 675 에서 통째로 Skipped 였던 자리).
            Transform img = cell.Find("img");
            Assert.IsNotNull(img, "탄 탈것은 얼굴 자리를 그린다(정본 mountFace · 못 구우면 아이콘이 남는다)");
            Sprite baked = Forge.Game.Ui.PetFaces.Get(ms.RiddenInst().Name, Forge.Game.Gallery.GalleryKind.Mounts);
            if (baked != null) Assert.AreSame(baked, img.GetComponent<Image>().sprite, "구운 썸네일이 있으면 그것으로 갈아 끼운다");
            // T381 3회차 — 얼굴 상자는 **셀 바깥 사각형**이다(정본 1869~1877 `.equip-cell .cell-img`: 테 두께만큼 밖으로 · `100% + 2*cellb`).
            //   탈것 얼굴은 `fit-ink` 가 아니라 잉크 정규화가 없으므로 **상자 크기가 곧 얼굴 크기**다 — 종전 값은 테 두께 두 배만큼 작았다.
            //   `ApplyThumb` 는 구운 썸네일이 있을 때만 크기를 다시 잡으므로 그 갈래에서만 잰다(CI 에서 못 굽는 자리가 있다 · 2회차).
            if (baked != null)
            {
                RectTransform cellRt = (RectTransform)cell;
                RectTransform imgRt = (RectTransform)img;
                float want = cellRt.rect.height + Forge.Game.Ui.PopupKit.Line3 * 2f;
                Assert.AreEqual(want, imgRt.rect.height, 0.6f, "얼굴 상자 = 셀 높이 + 테 두께 두 배(정본 cell-img)");
                Assert.AreEqual(want, imgRt.rect.width, 0.6f, "정사각 상자다(정본도 같은 값을 폭·높이에 준다)");
                Assert.Greater(imgRt.rect.height, cellRt.rect.height, "셀보다 커야 한다 — 테 위로 걸친다");
            }
            Transform lv = cell.Find("lv");
            Assert.IsNotNull(lv, "탄 탈것은 Lv 배지를 그린다(정본 1532 .cell-lv)");
            StringAssert.Contains("Lv", lv.GetComponent<TextMeshProUGUI>().text);
            Assert.IsNull(cell.Find("slot-name"), "탄 갈래엔 «탈것» 글자가 없다(정본은 빈 칸에만 쓴다)");
        }
    }
}
