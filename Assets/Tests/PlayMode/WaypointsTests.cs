using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T139 — 맵 위 이정표 둘(정본 <c>#waypoint-mystery</c>·<c>#waypoint-pass</c>): 부팅 때 무대 띠 오른쪽 정본 자리에 서고(HUD 아래) · 아이콘은 T31 아틀라스 <c>wp_mystery</c>·<c>power</c> ·
    /// 미스터리 아래엔 09:00 까지 카운트다운이 매초 다시 써지고 · 누르면 미스터리 = 준비 중 팝업(<c>openStub</c>) · 패스 = 패스 팝업(<c>openPass</c>) · 리그 이정표는 없다.
    /// 판정 PNG: 이정표 둘이 보이는 메인 한 장(<c>screen_t139-waypoints.png</c> · 정기 촬영 <c>screen_main.png</c> 에도 찍힌다).
    /// </summary>
    public class WaypointsTests
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
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        static IEnumerator WaitMs(double ms) { float t = 0f; while (t * 1000f < ms) { t += Time.unscaledDeltaTime; yield return null; } }

        static bool IsCountdownNow(string txt)
        {
            double sec = WaypointsRules.CountdownSec(System.DateTime.Now);
            for (int d = -3; d <= 3; d++) if (txt == PopupKit.FmtTime(sec + d)) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator 이정표_둘이_무대_띠_오른쪽_정본_자리에_서고_아이콘은_아틀라스이며_카운트다운이_매초_다시_써진다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            Waypoints w = Waypoints.Instance;
            Assert.IsNotNull(w, "UiRoot.Build 가 세운다(정본 index.html 정적 마크업 · paintWaypointIcons)");
            WaypointsSpec s = Waypoints.Spec;
            float rem = PopupKit.Rem;
            Assert.AreEqual(root.App, w.Layer.parent, "층은 앱 상자 안");
            Assert.AreEqual(new Vector2(0f, 1f - UiKit.L("sheet_top")), w.Layer.anchorMin, "무대 띠(정본 #game-area) — 아래 = 장비 시트 위");
            Assert.AreEqual(new Vector2(1f, 1f - UiKit.L("topbar_h")), w.Layer.anchorMax, "위 = 상단바 아래");
            Assert.Less(w.Layer.GetSiblingIndex(), root.HudLayer.GetSiblingIndex(), "HUD 아래(정본 .waypoint z3 < #topbar z5)");
            Assert.IsNull(w.Layer.GetComponent<Image>(), "층은 입력을 안 막는다(그림만)");
            Assert.AreEqual(2, w.Buttons.Count, "정본 이정표 둘"); Assert.AreEqual(2, w.Layer.childCount);
            Assert.IsNull(w.Layer.Find(WaypointsSpec.RemovedLeague), "리그 보상 이정표는 정본이 지웠다(주인 지시 2026-08-19)");
            foreach (WaypointSpec p in s.Points)
            {
                Button b = w.Buttons[p.Id];
                RectTransform rt = (RectTransform)b.transform;
                Assert.AreEqual(w.Layer, rt.parent);
                Assert.AreEqual(new Vector2(1f - (float)p.RightF, 1f - (float)p.TopF), rt.anchorMin, p.Id + " 자리 = 정본 top/right %");
                Assert.AreEqual(rt.anchorMin, rt.anchorMax); Assert.AreEqual(Vector2.one, rt.pivot, "오른쪽 위 모서리가 그 자리");
                RectTransform iconBox = (RectTransform)rt.Find(WaypointsStyle.T("icon"));
                Assert.IsNotNull(iconBox, p.Id + " 아이콘 칸");
                Assert.AreEqual((float)s.IconRem * rem, iconBox.sizeDelta.x, 0.01f, "아이콘 칸 2.3rem"); Assert.AreEqual((float)s.IconRem * rem, iconBox.sizeDelta.y, 0.01f);
                Image ico = iconBox.GetComponentInChildren<Image>(true);
                Assert.IsNotNull(ico);
                Assert.AreSame(UiIcons.Get(p.Icon), ico.sprite, p.Id + " 아이콘 = 정본 WP_ICON «" + p.Icon + "»(T31 아틀라스)");
                Assert.IsNotNull(ico.GetComponent<Shadow>(), "정본 drop-shadow — 그림에 그림자");
                Assert.IsFalse(ico.raycastTarget); Assert.IsTrue(b.targetGraphic.raycastTarget, "누르는 면은 버튼 것");
                Assert.GreaterOrEqual(rt.sizeDelta.x, (float)s.IconRem * rem - 0.01f, "상자 폭 ≥ 아이콘(정본 flex column · 알약이 더 넓으면 그만큼)");
                Transform time = rt.Find(WaypointsStyle.T("time"));
                if (p.Countdown)
                {
                    Assert.IsNotNull(time, p.Id + " 카운트다운 알약");
                    TextMeshProUGUI t = time.GetComponentInChildren<TextMeshProUGUI>(true);
                    Assert.IsNotNull(t); Assert.IsNotEmpty(t.text);
                    Assert.AreEqual(w.LastCountdown, t.text);
                    Assert.IsTrue(IsCountdownNow(t.text), "다음 09:00 까지(정본 msUntilDailyReset) — 지금 «" + t.text + "»");
                    Assert.AreEqual(WaypointsStyle.C("time_ink"), t.color, "#ffd54f");
                    Assert.AreEqual(WaypointsStyle.C("time_bg"), time.GetComponent<Image>().color, "rgba(0,0,0,.65)");
                    Assert.Greater(((RectTransform)time).sizeDelta.x, 0f); Assert.Greater(((RectTransform)time).sizeDelta.y, 0f);
                    Assert.AreEqual(-(float)(s.IconRem + s.GapRem) * rem, ((RectTransform)time).anchoredPosition.y, 0.01f, "아이콘 아래 gap .15rem");
                    Assert.AreEqual(rt.sizeDelta.x, Mathf.Max((float)s.IconRem * rem, ((RectTransform)time).sizeDelta.x), 0.01f, "상자 폭 = max(아이콘, 알약)");
                }
                else Assert.IsNull(time, p.Id + " 엔 카운트다운이 없다(정본 #waypoint-pass 는 아이콘뿐)");
            }
            int ticks = w.Ticks;
            yield return WaitMs(s.TickMs + 300);
            Assert.Greater(w.Ticks, ticks, "정본 매초 tick 이 글자를 다시 쓴다");
            Assert.IsTrue(IsCountdownNow(w.LastCountdown));
            Capture("screen_t139-waypoints");
        }

        [UnityTest]
        public IEnumerator 누르면_미스터리는_준비_중_팝업_패스는_패스_팝업이_열린다()
        {
            yield return Boot();
            Waypoints w = Waypoints.Instance;
            Assert.IsNotNull(w);
            PopupLayer pl = PopupLayer.Instance;
            MetaHost h = MetaHost.Instance;
            Assert.IsNull(pl.Find("stub")); Assert.IsNull(pl.Find(PassPopup.Name));
            w.Buttons["waypoint-mystery"].onClick.Invoke();
            yield return null;
            Assert.AreEqual("waypoint-mystery", w.LastTap);
            Popup stub = pl.Find("stub");
            Assert.IsNotNull(stub, "정본 onWaypointMystery → openStub('미스터리 상자', …)");
            bool title = false, desc = false;
            foreach (TextMeshProUGUI t in stub.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.text == WaypointsStyle.T("stub_title")) title = true;
                if (t.text == WaypointsStyle.T("stub_desc")) desc = true;
            }
            Assert.IsTrue(title, "제목 «미스터리 상자»"); Assert.IsTrue(desc, "문구 «특별 이벤트 상자는 준비 중입니다.»");
            pl.Hide("stub");
            yield return null;
            Assert.IsNull(pl.Find("stub"));
            w.Buttons["waypoint-pass"].onClick.Invoke();
            yield return null;
            Assert.AreEqual("waypoint-pass", w.LastTap);
            Assert.IsNotNull(pl.Find(PassPopup.Name), "정본 UI.openPass()");
            PassPopup.Close(h);
            yield return null;
            Assert.IsNull(pl.Find(PassPopup.Name));
        }

        /// <summary>UI 를 한 장 그린다(T138 `LootFeedTests.Capture` 와 같은 길) — 눈 확인용 · 실패해도 판정을 안 흔든다.</summary>
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
            GameObject camGo = new GameObject("t139-pixel-cam");
            Camera cam = camGo.AddComponent<Camera>();
            try
            {
                if (Camera.main != null) cam.CopyFrom(Camera.main);
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
                try { GallerySheet.Save(tex, saveAs); } catch (System.Exception e) { Debug.Log("[T139] PNG 저장 생략: " + e.Message); }
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
