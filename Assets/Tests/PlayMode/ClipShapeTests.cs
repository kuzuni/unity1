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

        /// <summary>구운 픽셀이 실제로 파여 있는가 — 민무늬 사각형이면 빨강.</summary>
        [UnityTest]
        public IEnumerator 구운_면은_홈_쪽이_비고_반대쪽이_찬다()
        {
            yield return Boot();

            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t159-host");
            try
            {
                // 홈이 왼쪽인 둘 · 오른쪽인 둘
                AssertNotch("lgr_tail_l", host, true);
                AssertNotch("pass_tail_l", host, true);
                AssertNotch("lgr_tail_r", host, false);
                AssertNotch("pass_tail_r", host, false);
            }
            finally { Object.Destroy(host.gameObject); }
            yield return null;
        }

        private static void AssertNotch(string key, RectTransform host, bool notchLeft)
        {
            RectTransform box = ClipShape.Face(host, key, key, 0f, 0f, 60f, 64f, "pp_line");
            Image img = box.Find("face").GetComponent<Image>();
            Assert.IsNotNull(img.sprite, key + ": 폴리곤이 구워졌다");
            Texture2D tex = img.sprite.texture;
            int mid = tex.height / 2;
            int nearLeft = Mathf.Max(0, Mathf.RoundToInt(tex.width * 0.05f));
            int nearRight = Mathf.Min(tex.width - 1, Mathf.RoundToInt(tex.width * 0.95f));
            float outside = tex.GetPixel(notchLeft ? nearLeft : nearRight, mid).a;
            float inside = tex.GetPixel(notchLeft ? nearRight : nearLeft, mid).a;
            Assert.Less(outside, 0.05f, key + ": 홈 쪽 중간 높이는 비어야 한다 — 차 있으면 민무늬 사각형이다");
            Assert.Greater(inside, 0.95f, key + ": 본체에 붙는 쪽 중간 높이는 차 있어야 한다");
        }

        /// <summary>실물 두 화면의 꼬리 넷이 «구운 면» 인가(민무늬 판이 아니라).</summary>
        [UnityTest]
        public IEnumerator 리그_보상과_패스의_꼬리가_구운_면이다()
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
            h.Popups.Hide(PassPopup.Name);
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
