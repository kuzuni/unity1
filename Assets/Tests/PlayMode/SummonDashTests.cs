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
    /// T368 3회차 — 정본 style.css 4205 `#panel-skills .summon-bar::before`: 소환 바 위 **풀블리드 대시 줄**.
    /// 재는 것: ⓐ 구운 타일 한 장이 `Tiled` 로 붙었는가(조각 늘어놓기가 아니다) ⓑ 자리·크기가 표(= 정본 비율) 그대로인가(left −.027W · width 1W · top −.0242H · height .00225H)
    /// ⓒ 타일 가로가 주기 .1993W 이고 당김 −.0499W 로 **x=0 이 대시 가운데**인가(첫·끝 화소 잉크 · 주기 중간 빈틈) ⓓ 펫 패널엔 없는가(정본 4404 스코프 밖).
    /// </summary>
    public class SummonDashTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 안 섰다");
            yield return null;
        }

        /// <summary>켜져 있는(activeInHierarchy) 오브젝트만 찾는다 — 숨긴 패널의 잔해를 «있다» 로 세지 않게.</summary>
        static Transform FindActive(Transform root, string name)
        {
            if (!root.gameObject.activeInHierarchy) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindActive(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        static bool Ink(Color c, Color ink) { return c.a > 0.5f && Mathf.Abs(c.r - ink.r) < 0.02f && Mathf.Abs(c.g - ink.g) < 0.02f && Mathf.Abs(c.b - ink.b) < 0.02f; }

        [UnityTest]
        public IEnumerator 스킬_패널_소환_바_위에_정본_대시_줄이_한_타일로_깔리고_펫_패널엔_없다()
        {
            yield return Boot();
            UiRoot.Instance.TabBar.OnTab("summon");
            yield return null;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            Assert.IsNotNull(sheet, "소환 시트");
            sheet.Switch(SkillPetSheet.SubSkills);
            yield return null; yield return null;

            Transform d = FindActive(UiRoot.Instance.App, "summon-dash");
            Assert.IsNotNull(d, "정본 #panel-skills .summon-bar::before — 소환 바 위 대시 줄(summon-dash)이 없다");
            Assert.AreEqual("summon-bar", d.parent.name, "소환 바의 자식(::before)");
            Image img = d.GetComponent<Image>();
            Assert.IsNotNull(img);
            Assert.AreEqual(Image.Type.Tiled, img.type, "되풀이는 Tiled 가 맡는다(대시 조각을 늘어놓지 않는다)");
            Assert.IsNotNull(img.sprite, "줄무늬는 구워서 얹는다");
            Assert.IsFalse(img.raycastTarget, "장식은 클릭을 안 먹는다(pointer-events: none)");

            float W = UiKit.RefW, H = UiKit.RefH;
            RectTransform rt = (RectTransform)d;
            Assert.AreEqual(1.0f * W, rt.sizeDelta.x, 0.5f, "폭 = 앱 폭(정본 width: var(--app-w))");
            Assert.AreEqual(-0.027f * W, rt.anchoredPosition.x, 0.5f, "정본 left: calc(var(--app-w) * -.027) — 패딩만큼 되밀어 풀블리드");
            Assert.AreEqual(0.0242f * H, rt.anchoredPosition.y, 0.5f, "정본 top: calc(var(--app-h) * -.0242) — 바 위쪽(앵커가 위-왼쪽이라 +y)");
            Assert.AreEqual(Mathf.Max(1f, 0.00225f * H), rt.sizeDelta.y, 0.5f, "정본 height: calc(var(--app-h) * .00225)");

            Texture2D tex = img.sprite.texture;
            int tw = tex.width;
            Assert.AreEqual(0.1993f * W, tw, 1.0f, "타일 가로 = 정본 background-size .1993W(한 주기)");
            Color ink = UiKit.C("pp_line");
            Assert.IsTrue(Ink(tex.GetPixel(0, 0), ink), "당김 −.0499W(반 대시) — x=0 은 대시 안이다");
            Assert.IsTrue(Ink(tex.GetPixel(tw - 1, 0), ink), "x=0 의 왼쪽(타일 끝)도 대시 — 대시 중심이 x=0 이다");
            Assert.IsFalse(Ink(tex.GetPixel(tw / 2, 0), ink), "주기 중간은 빈틈(transparent 50%)");
            int inkCount = 0;
            for (int x = 0; x < tw; x++) if (Ink(tex.GetPixel(x, 0), ink)) inkCount++;
            Assert.AreEqual(tw * 0.5f, inkCount, 2f, "대시 = 주기의 50%");

            sheet.Switch(SkillPetSheet.SubPets);
            yield return null; yield return null;
            Assert.IsNull(FindActive(UiRoot.Instance.App, "summon-dash"), "정본 4404 #panel-pets .summon-bar 엔 ::before 가 없다 — 펫 패널은 안 그린다");
        }
    }
}
