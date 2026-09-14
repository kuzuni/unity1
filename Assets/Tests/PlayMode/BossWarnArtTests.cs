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
    /// T173 — 보스 경고 두 겹이 정본의 **방사형 그라디언트 + screen 합성**인가, 그리고 팝업이 떠 있을 때 경고 딤이 꺼지는가.
    ///
    /// 클론은 여태 `.bw-dim`·`.bw-flash` 가 **단색 판 한 장**이고 점멸이 보통 알파라, 연출이 도는 순간에 찍힌 화면이
    /// 씬 대역 통째로 한 색이 됐다(런 403 `gear-detail` 의 `(90,14,11)`). 여기서는 **구운 픽셀**로 그 셋을 잰다.
    /// </summary>
    public class BossWarnArtTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready) && t < 20f)
            { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 20초 안에 안 섰다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 감광은_가운데가_옅고_가장자리가_짙다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            o.BossWarning(2.0);
            yield return null;

            Image dim = Find(o.Layer, "bw-dim");
            Assert.IsNotNull(dim, "bw-dim");
            Assert.IsNotNull(dim.sprite, "방사형이 구워졌다 — 단색 판이면 스프라이트가 없다");
            Texture2D tex = dim.sprite.texture;

            // 정본 `radial-gradient(ellipse at 50% 42%, rgba(72,4,4,.34), rgba(6,2,4,.76))`
            // — 중심(위에서 42%)이 .34 · 가장 먼 모서리가 .76.
            int cx = Mathf.RoundToInt(tex.width * 0.5f);
            int cy = Mathf.RoundToInt(tex.height * (1f - 0.42f));   // 텍스처는 아래가 0행
            float mid = tex.GetPixel(cx, cy).a;
            float corner = tex.GetPixel(1, 1).a;
            Assert.AreEqual(0.34f, mid, 0.02f, "가운데 알파 = 정본 rgba(72,4,4,**.34**)");
            Assert.AreEqual(0.76f, corner, 0.03f, "가장 먼 모서리 = rgba(6,2,4,**.76**)");
            Assert.Less(mid, corner, "가운데가 가장자리보다 **옅다** — 단색 판이면 둘이 같다");
            // 모서리가 비어 있으면(타원 밖 투명) 정본과 다르다 — farthest-corner 로 상자를 꽉 채워야 한다
            Assert.Greater(tex.GetPixel(tex.width - 2, tex.height - 2).a, 0.5f, "네 모서리 다 칠해져 있다");
        }

        [UnityTest]
        public IEnumerator 점멸은_가산_합성이고_가운데가_가장_밝다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            o.BossWarning(2.0);
            yield return null;

            Image flash = Find(o.Layer, "bw-flash");
            Assert.IsNotNull(flash, "bw-flash");
            Assert.IsNotNull(flash.sprite, "방사형이 구워졌다");
            Texture2D tex = flash.sprite.texture;
            int cx = Mathf.RoundToInt(tex.width * 0.5f);
            int cy = Mathf.RoundToInt(tex.height * (1f - 0.42f));
            Color mid = tex.GetPixel(cx, cy);
            Color far = tex.GetPixel(1, 1);
            Assert.AreEqual(0.62f, mid.a, 0.02f, "정본 rgba(255,72,48,**.62**)");
            Assert.AreEqual(0.34f, far.a, 0.02f, "70% 뒤는 마지막 색 그대로 — rgba(190,10,10,**.34**)");
            Assert.Greater(mid.r + mid.g + mid.b, far.r + far.g + far.b, "가운데가 더 밝다(경광등)");

            // 정본 `mix-blend-mode: screen` — 이 레포의 그 재질은 T87 이 세운 `Forge/UiScreen` 이다.
            // 셰이더를 못 찾으면 재질이 null 이고 여태처럼 보통 알파로 그려진다(연출이 사라지는 것보다 낫다 · CraftFxPoly.Screen 주석).
            if (CraftFxPoly.Screen() != null)
            {
                Assert.IsNotNull(flash.material, "점멸은 screen 재질로 그린다");
                Assert.AreEqual(CraftFxPoly.ScreenShaderName, flash.material.shader.name, "정본 mix-blend-mode: screen");
            }
        }

        [UnityTest]
        public IEnumerator 팝업이_떠_있으면_경고_딤만_꺼진다()
        {
            yield return Boot();
            BattleOverlay o = BattleOverlay.Ensure();
            MetaHost h = MetaHost.Instance;

            o.BossWarning(2.0);
            o.Tick(0.5f);                 // 감광은 앞 8% 에 차오른다(FxRules.WarnDimIn) — 한가운데 프레임으로 민다
            Image dim = Find(o.Layer, "bw-dim");
            Assert.Greater(dim.color.a, 0.5f, "팝업이 없을 때는 딤이 보인다");

            PassPopup.Open(h);
            yield return null;
            o.Tick(0.1f);                 // 팝업이 뜬 채로 한 박자 더 — 정본은 그 순간 딤을 끈다
            yield return null;
            Assert.AreEqual(0f, dim.color.a, 1e-4f, "정본 363 — 팝업이 떠 있으면 경고 딤은 끈다(모달 딤과 겹쳐 화면이 검게 죽는다)");
            Assert.IsNotNull(Find(o.Layer, "bw-banner"), "배너는 그대로 둔다 — 카드 옆 여백에서 «보스 온다» 가 읽혀야 한다");

            h.Popups.Hide(PassPopup.Name);
            yield return null;
        }

        private static Image Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root.GetComponent<Image>() ?? root.GetComponentInChildren<Image>(true);
            for (int i = 0; i < root.childCount; i++)
            {
                Image hit = Find(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
