using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T134 — 공통 수령 연출(정본 `rewardBurst`). 1회차는 연출 자체를 직접 부른다(호출 여덟 자리는 각 파일 lock 뒤):
    /// 재화별 아이콘 수가 로그 눈금대로 · 층이 앱 상자의 마지막 형제(팝업 위 · 정본 z 70) · 메인 화면에서는 앵커 0 · 누적 카운터의 마지막 값 = 양 ·
    /// 수명 뒤 층이 빈다 · 시트(퀘스트)가 상단바를 덮으면 도착점이 시트 상단으로 승격되고 앵커 배지가 선다 · 빈 목록은 0.
    /// 판정 PNG: 비행 중간 한 장(`screen_t134-reward.png` · 눈 확인용).
    /// </summary>
    public class RewardBurstTests
    {
        static IEnumerator Boot()
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

        static IEnumerator WaitMs(double ms) { float t = 0f; while (t * 1000f < ms) { t += Time.unscaledDeltaTime; yield return null; } }

        static Dictionary<string, double> R(params object[] kv)
        {
            var d = new Dictionary<string, double>();
            for (int i = 0; i < kv.Length; i += 2) d[(string)kv[i]] = System.Convert.ToDouble(kv[i + 1]);
            return d;
        }

        [UnityTest]
        public IEnumerator 메인_화면에서_재화_둘이_눈금대로_터져_pill과_상단바로_날아가고_수명_뒤_층이_빈다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            Assert.IsNotNull(root);
            RewardBurstSpec s = RewardBurst.Spec;
            RectTransform from = root.Sheet;
            int n = RewardBurst.Play(R("coins", 100, "hammers", 5), from);
            Assert.AreEqual(5 + 3, n, "코인 100 → 2+round(2.6)=5 · 해머 5 → 2+round(.91)=3");
            RewardBurst rb = RewardBurst.Instance;
            Assert.IsNotNull(rb);
            Assert.AreEqual(root.App.childCount - 1, rb.Layer.GetSiblingIndex(), "층은 앱 상자의 마지막 형제(팝업 위 · 정본 #reward-burst z 70)");
            Assert.AreEqual(2, rb.LastEntries.Count); Assert.AreEqual(5, rb.LastCounts[0]); Assert.AreEqual(3, rb.LastCounts[1]);
            Assert.IsFalse(rb.LastTargets[0].Covered, "메인 화면 — 코인 pill 이 보인다");
            Assert.IsFalse(rb.LastTargets[1].Covered, "해머는 pill 이 없어 상단바 밴드로(가려지지 않음)");
            bool band; RectTransform pill = RewardBurst.TargetOf("coins", out band);
            Assert.IsNotNull(pill); Assert.AreEqual("pill-coin", pill.name); Assert.IsFalse(band);
            RewardBurst.TargetOf("hammers", out band); Assert.IsTrue(band, "해머 → 상단바 폴백(rw-pulse-band)");
            yield return null;
            Assert.AreEqual(8, rb.Flying, "아이콘 여덟이 층에 섰다(지연 중은 감춤)");
            Assert.AreEqual(2, rb.Impacts, "글로우 + 링");
            Assert.AreEqual(2, rb.Amts, "재화마다 «+획득량» 하나");
            Assert.AreEqual(0, rb.Anchors, "안 가려졌으니 앵커 없음");
            // 런 278 PNG: «+5» 라벨이 앱 상자 왼쪽 밖에 찍혔다 — Box 의 늘림 앵커 위에 sizeDelta 만 줘서 rect 폭이 «부모 폭 + w» 였다. 폭·피벗·자리를 못박는다.
            int rows = 0, flies = 0;
            foreach (RectTransform ch in rb.Layer)
            {
                if (ch.name == "rw-amt")
                {
                    rows++;
                    Assert.Less(ch.rect.width, rb.Layer.rect.width * 0.5f, "«+획득량» 상자 폭은 제 글자 폭(부모 폭이 더해지면 안 된다)");
                    Assert.AreEqual(new Vector2(0.5f, 0.5f), ch.pivot, "피벗 가운데(CSS transform-origin)");
                    float cx = ch.anchoredPosition.x;
                    Assert.IsTrue(cx >= 0f && cx <= rb.Layer.rect.width, "라벨 가운데가 앱 상자 안 — 지금 " + cx);
                }
                if (ch.name == "rw-fly") { flies++; Assert.AreEqual(new Vector2(0.5f, 0.5f), ch.pivot, "아이콘은 중심을 돌며 회전한다"); Assert.Less(ch.rect.width, rb.Layer.rect.width * 0.2f); }
            }
            Assert.AreEqual(2, rows); Assert.AreEqual(8, flies);
            // 비행 중간 — 눈 확인용 한 장
            double lastDelay = RewardBurstRules.LastDelayMs(s, 1, 5);
            yield return WaitMs(lastDelay + s.FlyMs * 0.45);
            Assert.GreaterOrEqual(rb.Layer.childCount, 8, "아이콘 여덟은 아직 층에 있다(임팩트·라벨은 걷혔을 수 있다)");
            Capture("screen_t134-reward");
            // 다 걷힌 뒤
            yield return WaitMs(rb.LastTotalMs + 400);
            Assert.AreEqual(0, rb.Flying); Assert.AreEqual(0, rb.Impacts); Assert.AreEqual(0, rb.Amts); Assert.AreEqual(0, rb.Ticks); Assert.AreEqual(0, rb.Anchors);
            Assert.AreEqual(0, rb.Layer.childCount, "층은 비었다(정본 el.remove)");
            Assert.AreEqual(8, rb.LastTickLabels.Count, "착지마다 카운터 갱신 한 번");
            CollectionAssert.Contains(rb.LastTickLabels, "+100", "코인 마지막 착지 = 양 그대로");
            CollectionAssert.Contains(rb.LastTickLabels, "+5", "해머 마지막 착지");
            CollectionAssert.Contains(rb.LastTickLabels, "+20", "코인 첫 착지 = round(100·1/5)");
            Assert.AreEqual(Vector3.one, pill.localScale, "박동 뒤 pill 크기는 원값");
            Assert.AreEqual(0, RewardBurst.Play(R("coins", 0, "gems", -1), from), "양이 0 이하뿐이면 연출 없음");
            Assert.AreEqual(0, RewardBurst.Play(null, from));
        }

        [UnityTest]
        public IEnumerator 시트가_상단바를_덮으면_도착점이_시트_상단으로_승격되고_앵커_배지가_선다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            MetaHost h = MetaHost.Instance;
            RewardBurstSpec s = RewardBurst.Spec;
            QuestSheet.Open(h);
            yield return null;
            RewardBurst rb = RewardBurst.Ensure();
            bool band; RectTransform pill = RewardBurst.TargetOf("coins", out band);
            RectTransform card;
            Vector3[] c = new Vector3[4]; pill.GetWorldCorners(c);
            Vector3 mid = rb.Layer.InverseTransformPoint((c[0] + c[2]) * 0.5f);
            Assert.IsTrue(rb.CoveredAt(mid.x + rb.Layer.rect.width * 0.5f, rb.Layer.rect.height * 0.5f - mid.y, out card), "퀘스트 시트가 코인 pill 을 덮는다");
            Assert.IsNotNull(card, "덮은 것은 딤이 아니라 시트 카드");
            int n = RewardBurst.Play(R("coins", 1000), root.App);
            Assert.AreEqual(2 + 4, n, "1000 → 2 + round(3.9) = 6");
            RewardTarget t = rb.LastTargets[0];
            Assert.IsTrue(t.Covered); Assert.IsTrue(t.Promoted, "카드 상단 모서리 안쪽으로 승격");
            double cssPx = UiKit.L("anvil_fx_px");
            Assert.LessOrEqual(t.Y, s.CardTopPx * cssPx + s.CardMinYPx * cssPx + 1, "시트 상단 + 18px(시트는 앱 위에서 시작)");
            yield return null;
            Assert.AreEqual(1, rb.Anchors, "재화 하나 → 앵커 배지 하나");
            Assert.AreEqual(6, rb.Flying);
            yield return WaitMs(rb.LastTotalMs + 400);
            Assert.AreEqual(0, rb.Anchors, "앵커는 마지막 아이콘 + 460ms 뒤 340ms 페이드로 걷힌다");
            Assert.AreEqual(0, rb.Layer.childCount);
            QuestSheet.Close(h);
            yield return null;
            Assert.IsFalse(rb.CoveredAt(mid.x + rb.Layer.rect.width * 0.5f, rb.Layer.rect.height * 0.5f - mid.y, out card), "시트를 닫으면 pill 이 보인다");
        }

        /// <summary>UI 를 한 장 그린다(T104 `OutlineTests.Grab` 과 같은 길) — 눈 확인용 · 실패해도 판정을 안 흔든다.</summary>
        static void Capture(string saveAs)
        {
            UiRoot root = UiRoot.Instance;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("t134-pixel-cam");
            Camera cam = camGo.AddComponent<Camera>();
            try
            {
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                // T349 4회차 — 이 카메라는 `cullingMask` 가 **UI 층 하나**다(3D 를 한 화소도 안 그린다).
                //        여기에 URP 포스트를 켜면 **UI 가 톤맵·색 보정에 물든다** — 정본은 `filter` 를 `#game3d` 에만 걸고
                //        HUD·패널·팝업에는 안 건다(T357). 3회차에 켰다가 순백 코어가 rgb 215 로 내려가 이 자가 빨갰다.
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                cam.ResetProjectionMatrix();
                cam.cullingMask = 1 << canvas.gameObject.layer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
                tex.Apply(false);
                try { GallerySheet.Save(tex, saveAs); } catch (System.Exception e) { Debug.Log("[T134] PNG 저장 생략: " + e.Message); }
                Object.Destroy(tex);
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                root.Layout();
                cam.targetTexture = null;
                Object.Destroy(camGo);
                Object.Destroy(rt);
            }
        }
    }
}
