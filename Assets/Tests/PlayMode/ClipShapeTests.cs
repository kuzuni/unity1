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
    /// T159 2회차 — 정본 `clip-path` 로 깎은 면이 클론에서도 **깎여 있는가**.
    ///
    /// 그림을 «눈으로» 가 아니라 **구운 픽셀**로 본다: 홈이 파인 쪽 중간 높이는 비어 있고(알파 0),
    /// 반대쪽 중간 높이는 차 있어야 한다. 민무늬 직사각형이면 양쪽 다 차 있어 이 자가 빨개진다.
    /// (자리 대조 자체는 `tools/check_clip_paths.py` 가 본다 — 이 자는 «실물이 그 모양인가» 쪽이다.)
    /// </summary>
    public class ClipShapeTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null && UiRoot.Instance.App != null) && t < 20f)
            { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        /// <summary>표가 정본 꼭짓점을 그대로 쥐는가(리그 2503~2504 · 패스 2722~2723).</summary>
        [UnityTest]
        public IEnumerator 꼬리_네_도형이_정본_꼭짓점_그대로다()
        {
            yield return Boot();

            Vector2[] ll = ClipShape.Clip("lgr_tail_l");
            Assert.AreEqual(5, ll.Length, "정본 polygon(100% 0, 0 0, 50% 50%, 0 100%, 100% 100%) 은 꼭짓점 다섯");
            Assert.AreEqual(0.5f, ll[2].x, 1e-4f, "리그 꼬리는 바깥 변에서 **절반 깊이**로 파고든다");
            Assert.AreEqual(0.5f, ll[2].y, 1e-4f, "홈은 세로 한가운데");

            Vector2[] lr = ClipShape.Clip("lgr_tail_r");
            Assert.AreEqual(0.5f, lr[2].x, 1e-4f, "거울상도 같은 깊이");

            Vector2[] pl = ClipShape.Clip("pass_tail_l");
            Assert.AreEqual(0.31f, pl[4].x, 1e-4f, "정본 2722 `31% 50%` — 실측 11px/36px = 30.5% 자리");
            Assert.AreEqual(0.5f, pl[4].y, 1e-4f);

            Vector2[] pr = ClipShape.Clip("pass_tail_r");
            Assert.AreEqual(0.69f, pr[2].x, 1e-4f, "오른쪽에서 잰 같은 깊이(1 − .31)");
            Assert.AreEqual(1f - pl[4].x, pr[2].x, 1e-4f, "두 꼬리는 서로 거울상이다");
        }

        /// <summary>홈이 실제로 «파여» 있는가 — 민무늬 사각형이면 두 점이 다 안쪽이라 빨강.</summary>
        /// <remarks>
        /// 구운 텍스처를 읽지 않는다: <see cref="CraftFxPoly"/> 는 `Apply(false, true)` 로 굽기 때문에 그림은
        /// GPU 로만 남아 `GetPixel` 이 던진다. 그래서 **같은 것을 꼭짓점으로** 잰다 — 굽는 입력이 그 다각형이므로
        /// 다각형이 파여 있으면 구운 그림도 파여 있다.
        /// </remarks>
        [UnityTest]
        public IEnumerator 홈_쪽_가운데는_도형_밖이고_반대쪽은_안이다()
        {
            yield return Boot();

            AssertNotch("lgr_tail_l", true);
            AssertNotch("pass_tail_l", true);
            AssertNotch("lgr_tail_r", false);
            AssertNotch("pass_tail_r", false);

            // 가격 페넌트 — 아래 꼭짓점은 가운데에만 있다(모서리는 82% 에서 깎인다)
            Vector2[] pen = ClipShape.Clip("pass_pennant");
            Assert.IsTrue(Inside(pen, new Vector2(0.5f, 0.99f)), "페넌트의 아래 꼭짓점은 가운데다");
            Assert.IsFalse(Inside(pen, new Vector2(0.03f, 0.99f)), "왼쪽 아래 모서리는 깎여 있다 — 둥근 사각이면 여기가 차 있다");
            Assert.IsFalse(Inside(pen, new Vector2(0.97f, 0.99f)), "오른쪽 아래 모서리도 깎인다");

            // 패스 칸 꼬리 — 빗변 반대쪽 절반만 남는다(무료는 오른쪽 · 프리미엄은 거울상)
            Vector2[] tf = ClipShape.Clip("pass_cell_tail_free");
            Assert.IsTrue(Inside(tf, new Vector2(0.9f, 0.5f)), "무료 칸 꼬리는 수직변이 오른쪽이다");
            Assert.IsFalse(Inside(tf, new Vector2(0.1f, 0.5f)));
            Vector2[] tp = ClipShape.Clip("pass_cell_tail_prem");
            Assert.IsTrue(Inside(tp, new Vector2(0.1f, 0.5f)), "프리미엄 칸 꼬리는 거울상이다");
            Assert.IsFalse(Inside(tp, new Vector2(0.9f, 0.5f)));
        }

        /// <summary>제비꼬리 홈은 %가 아니라 **절대 .8rem** 이다 — 상자가 넓어져도 깊이가 그만큼이어야 한다.</summary>
        [UnityTest]
        public IEnumerator 거래_태그의_홈은_폭이_바뀌어도_같은_길이만큼_판다()
        {
            yield return Boot();

            float rem = 10f;
            Vector2[] narrow = ClipShape.Clip("shop_deal_tag", 100f, rem);
            Vector2[] wide = ClipShape.Clip("shop_deal_tag", 200f, rem);
            Assert.AreEqual(1f - 0.8f * rem / 100f, narrow[2].x, 1e-4f, "정본 `calc(100% - .8rem)`");
            Assert.AreEqual(1f - 0.8f * rem / 200f, wide[2].x, 1e-4f, "폭이 두 배면 정규 깊이는 절반");
            Assert.AreEqual((1f - narrow[2].x) * 100f, (1f - wide[2].x) * 200f, 1e-3f, "실제 판 길이는 둘 다 .8rem");
            Assert.Less(narrow[2].x, 1f, "홈이 오른쪽 변보다 안쪽이어야 «제비꼬리» 다");
            Assert.AreEqual(0.5f, narrow[2].y, 1e-4f, "홈은 세로 한가운데");
            // rem 을 안 주면 표의 값 그대로(정규 % 도형과 같은 길)
            Assert.AreEqual(1f, ClipShape.Clip("shop_deal_tag")[2].x, 1e-4f);
        }

        private static void AssertNotch(string key, bool notchLeft)
        {
            Vector2[] p = ClipShape.Clip(key);
            Vector2 outer = new Vector2(notchLeft ? 0.05f : 0.95f, 0.5f);
            Vector2 inner = new Vector2(notchLeft ? 0.95f : 0.05f, 0.5f);
            Assert.IsFalse(Inside(p, outer), key + ": 홈 쪽 중간 높이는 도형 밖이어야 한다 — 안이면 민무늬 사각형이다");
            Assert.IsTrue(Inside(p, inner), key + ": 본체에 붙는 쪽 중간 높이는 도형 안이다");
        }

        /// <summary>점이 다각형 안인가(반직선 교차 · 굽는 자가 쓰는 것과 같은 셈).</summary>
        private static bool Inside(Vector2[] pts, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                if ((pts[i].y > p.y) == (pts[j].y > p.y)) continue;
                float x = (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x;
                if (p.x < x) inside = !inside;
            }
            return inside;
        }

        /// <summary>실물 세 화면(리그 보상 · 패스 · 상점)의 깎인 자리가 «구운 면» 인가(민무늬 판이 아니라).</summary>
        [UnityTest]
        public IEnumerator 리그_패스_상점의_깎인_자리가_구운_면이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;

            LeagueSheet.OpenRewards(h);
            yield return null;
            AssertBaked(h, LeagueSheet.RewardsName, "tail-l");
            AssertBaked(h, LeagueSheet.RewardsName, "tail-r");
            h.Popups.Hide(LeagueSheet.RewardsName);
            yield return null;

            PassPopup.Open(h);
            yield return null;
            AssertBaked(h, PassPopup.Name, "tail-l");
            AssertBaked(h, PassPopup.Name, "tail-r");
            // 가격 페넌트 두 층(검정 바깥 · 주황 안쪽)과 보상 칸 꼬리(검정 층 + 칸 색 면)
            AssertBaked(h, PassPopup.Name, "price-line");
            AssertBaked(h, PassPopup.Name, "price-face");
            AssertBaked(h, PassPopup.Name, "tail-line");
            AssertBaked(h, PassPopup.Name, "tail-face");
            h.Popups.Hide(PassPopup.Name);
            yield return null;

            ShopSheet.Open(h);
            yield return null;
            AssertBaked(h, ShopSheet.Name, "tag");
            ShopSheet.Close(h);
            yield return null;
        }

        private static void AssertBaked(MetaHost h, string popup, string name)
        {
            Popup p = h.Popups.Find(popup);
            Assert.IsNotNull(p, popup + " 가 열렸다");
            Transform tail = Find(p.Root, name);
            Assert.IsNotNull(tail, popup + " 의 " + name);
            Transform face = tail.Find("face");
            Assert.IsNotNull(face, name + ": 구운 면 «face» 가 있어야 한다(UiKit.Panel 이면 없다)");
            Image img = face.GetComponent<Image>();
            Assert.IsNotNull(img, name + ": 면 그림");
            Assert.IsNotNull(img.sprite, name + ": 구운 스프라이트 — 민무늬 판은 스프라이트가 없다");
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform hit = Find(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
