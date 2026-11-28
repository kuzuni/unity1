using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T528 — 소환 결과 격자를 **자를지**는 «잰 높이» 가 아니라 **«셀 수»** 가 정한다.
    ///
    /// 정본 `css/style.css` **6228** 주석이 그 뜻을 적어 뒀다 — «셀이 적을 때는 overflow 를 열어 **광채가
    /// 잘리지 않게** 하고(스크롤 불필요), 많을 때만 스크롤+페이드 마스크를 켠다 — `overflow-y: auto` 는
    /// **가로도 함께** 잘라낸다». 그래서 **6233** `.sr-grid { overflow: visible }` 이고 **6235** 가
    /// `.sr-grid.mid, .sr-grid.dense` 에만 `overflow-y: auto` + 페이드를 건다. 문은 `js/ui.js` **445** 의
    /// class 한 줄이고 그 잣대는 **셀 수**다(n > 24 dense · n > 10 mid · 그 아래는 빈 class).
    ///
    /// 클론은 `totalH > bodyH` 로 갈라서 **셀 9 + 주역** 판 하나에서만 어긋났다(T33 89회차 실측:
    /// 그 판이 n ≤ 10 중 유일하게 «주역 행 + 3행» 이 겹친다 · totalH 1811 > bodyH 1506).
    /// 이 자는 그 두 쪽을 **같은 창에서** 본다 — 9는 안 자르고, 11은 자르고 페이드까지 건다.
    /// </summary>
    public class SummonGridClipTests
    {
        static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20261128;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null
                     && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready); i++)
                yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;
        }

        /// <summary>n개 중 **마지막 하나만** 전설(= 정본 `Hi`)로 둔다 — 그래야 `heroIdx = n − 1 > 0` 이 된다.</summary>
        static SkillSummonResultView Open(int n)
        {
            var list = new List<SkillSummonResultView.Entry>();
            for (int i = 0; i < n; i++)
                list.Add(new SkillSummonResultView.Entry
                {
                    Key = "sk:" + i,
                    IconKey = "sk_fireball",
                    Rarity = (i == n - 1) ? "legendary" : "common",
                    Name = "가" + i,
                });
            return SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "legendary", null);
        }

        /// <summary>
        /// 이름이 `sr-grid` 인 상자. ⚠ **그 이름이 가리키는 것이 두 가지다**(1차 판정이 여기서 빨갰다 · 런 1418):
        /// 안 자르는 가지는 `UiKit.Box(body, "sr-grid")` 라 **격자 자신**이고, 자르는 가지는
        /// `PetSkillKit.Scroll(body, "sr-grid", …)` 이라 그 이름이 **뷰(마스크를 단 상자)** 에 붙고
        /// 격자는 그 아래 `content` 다(`PetSkillKit.cs` 366~387 — `RectMask2D` 는 **뷰**에 달린다).
        /// 그래서 마스크는 «이름이 sr-grid 인 상자 **또는 그 부모**» 에서 찾는다.
        /// </summary>
        static RectTransform Named(SkillSummonResultView v)
        {
            foreach (Transform x in v.GetComponentsInChildren<Transform>(true))
                if (x.name == "sr-grid") return (RectTransform)x;
            return null;
        }

        static RectMask2D MaskOf(RectTransform named)
        {
            if (named == null) return null;
            RectMask2D self = named.GetComponent<RectMask2D>();
            if (self != null) return self;
            return named.parent == null ? null : named.parent.GetComponent<RectMask2D>();
        }

        [UnityTest]
        public IEnumerator 셀_아홉에_주역이_있어도_격자를_안_자른다_정본은_셀_수로_가른다()
        {
            yield return Boot();
            SkillSummonResultView v = Open(9);
            Assert.IsNotNull(v, "소환 결과 창이 안 열렸다");
            yield return null;
            RectTransform grid = Named(v);
            Assert.IsNotNull(grid, "격자 sr-grid");
            // ⓐ 정본 6233 `.sr-grid { overflow: visible }` — n = 9 는 class 가 비어 **안 자른다**.
            //    클론에서 «자른다» 는 곧 `PetSkillKit.Scroll` 공장이 부모에 단 `RectMask2D` 다.
            Assert.IsNull(MaskOf(grid),
                "셀 9 + 주역 판에서 마스크가 붙었다 — 정본은 n ≤ 10 이면 높이가 넘쳐도 안 자른다(T528)");
            Assert.IsNull(grid.GetComponentInParent<ScrollRect>(),
                "셀 9 판에 스크롤이 붙었다 — 정본 6235 는 .mid/.dense 에만 건다");
            v.Close();
        }

        [UnityTest]
        public IEnumerator 셀_열하나부터는_자르고_페이드가_보이는_높이의_5퍼센트다()
        {
            yield return Boot();
            SkillSummonResultView v = Open(11);
            Assert.IsNotNull(v);
            yield return null;
            RectTransform grid = Named(v);
            Assert.IsNotNull(grid, "격자 sr-grid");
            // ⓑ 정본 6235 `.sr-grid.mid` — n = 11 은 mid 라 자르고 페이드를 건다.
            RectMask2D rm = MaskOf(grid);
            Assert.IsNotNull(rm, "셀 11(mid)에서는 마스크가 붙어야 한다(T528)");
            // 제품 코드가 재는 것과 **같은 몸통**을 재야 한다 — `bodyH` 는 마스크를 단 상자의 **부모**(몸통)다.
            RectTransform maskBox = (RectTransform)rm.transform;
            float bodyH = ((RectTransform)maskBox.parent).rect.height;
            float fade = PetSkillStyle.L("sr_grid_fade_f");
            Assert.Greater(rm.softness.y, 0, "페이드가 0 이다");
            Assert.AreEqual(bodyH * fade, rm.softness.y, Mathf.Max(2f, bodyH * fade * 0.25f),
                "페이드는 보이는 높이의 표값(sr_grid_fade_f) 몫이다 — T178 47회차");
            v.Close();
        }
    }
}
