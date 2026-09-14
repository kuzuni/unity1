using System.Collections;
using NUnit.Framework;
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
    /// T138 — 전투 전리품 레인(정본 `floatLoot`) + 전투 토스트 레인(`toast(msg,'combat')`). 1회차는 레인을 직접 부른다(전투 호출부는 T135 lock 뒤):
    /// 줄이 아래에서 위로 쌓이고 6 을 넘으면 맨 위가 버려진다(동시에 최대 7) · 줄마다 아이콘(🪙→coin)+글자 · 1.6초 뒤 다 걷힌다 · 레인은 무대 띠 오른쪽 아래 ·
    /// 전투 토스트는 모달 **아래** 상자에, 기본 토스트는 그대로 팝업 위 상자에. 판정 PNG: 줄 셋이 보이는 순간 한 장(`screen_t138-loot.png`).
    /// </summary>
    public class LootFeedTests
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

        [UnityTest]
        public IEnumerator 전리품_줄은_아이콘_글자로_쌓이고_6을_넘으면_맨_위가_버려지며_1_6초_뒤_걷힌다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            LootFeedSpec s = LootFeed.Spec;
            Assert.AreEqual(1, LootFeed.Push("🪙 +3"));
            LootFeed f = LootFeed.Instance;
            Assert.IsNotNull(f);
            RectTransform layer = (RectTransform)f.transform;
            Assert.AreEqual(root.App, layer.parent, "층은 앱 상자 안");
            Assert.AreEqual(new Vector2(0f, 1f - UiKit.L("sheet_top")), layer.anchorMin, "무대 띠(정본 #game-area) — 아래 = 장비 시트 위");
            Assert.AreEqual(new Vector2(1f, 1f - UiKit.L("topbar_h")), layer.anchorMax, "위 = 상단바 아래");
            Assert.AreEqual(new Vector2(1f, 0f), f.Lane.anchorMin, "레인은 오른쪽 아래 앵커"); Assert.AreEqual(new Vector2(1f, 0f), f.Lane.pivot, "피벗도 오른쪽 아래 — 줄이 늘면 위로 자란다");
            float rem = PopupKit.Rem;
            Assert.AreEqual(-(float)s.RightRem * rem, f.Lane.anchoredPosition.x, 0.01f, "right .6rem"); Assert.AreEqual((float)s.BottomRem * rem, f.Lane.anchoredPosition.y, 0.01f, "bottom 3.4rem");
            yield return null;
            RectTransform line = (RectTransform)f.Lane.GetChild(0);
            Assert.AreEqual(1, line.GetComponentsInChildren<Image>(true).Length - 1, "아이콘 하나(🪙 → coin · 알약 배경 제외)");
            Assert.AreEqual(UiKit.Icon(line, "probe", "coin").sprite, line.Find("row").GetComponentInChildren<Image>(true).sprite, "아이콘은 T31 코인 스프라이트");
            Object.Destroy(line.Find("probe").gameObject);
            Assert.AreEqual(1, UiKit.RowTexts((RectTransform)line.Find("row")).Length); Assert.AreEqual("+3", UiKit.RowTexts((RectTransform)line.Find("row"))[0].text.Trim());
            // 런 292 PNG: 줄이 패딩만 한 조각이었다(중첩 레이아웃 선호값 0) — 줄 크기가 아이콘+글자를 담는지 못박는다
            float kind = PopupKit.FontSize(TextKind.Sub);
            Assert.Greater(line.rect.width, kind * 2f, "줄 폭은 아이콘 + 글자(«+3»)보다 넓다 — 지금 " + line.rect.width);
            Assert.Less(line.rect.width, f.Lane.rect.width, "줄은 레인 안");
            Assert.GreaterOrEqual(line.rect.height, kind, "줄 높이 ≥ 글자 크기");
            Assert.Less(line.rect.height, kind * 2.5f, "줄은 한 줄이다(줄바꿈 없음)");
            for (int i = 2; i <= 9; i++) LootFeed.Push("🔨 +" + i);
            yield return null;
            Assert.AreEqual(7, f.Lines, "9 줄을 붙이면 6 을 넘는 순간부터 맨 위를 버려 7 이 남는다(정본 `> 6`)");
            Assert.AreEqual(2, f.Dropped, "버린 줄 둘(«🪙 +3»·«🔨 +2»)");
            Assert.AreEqual(9, f.Pushed.Count);
            Assert.AreEqual("+3", UiKit.RowTexts((RectTransform)f.Lane.GetChild(0).Find("row"))[0].text.Trim(), "맨 위에 남은 줄은 세 번째로 붙인 «🔨 +3»(첫 둘은 버려졌다)");
            Capture("screen_t138-loot");
            yield return WaitMs(LootFeedRules.LifeMs(s) + 400);
            Assert.AreEqual(0, f.Lines, "1.6초 뒤 다 걷힌다(정본 el.remove)");
        }

        [UnityTest]
        public IEnumerator 전투_토스트는_모달_아래_레인에_기본_토스트는_팝업_위_레인에_선다()
        {
            yield return Boot();
            UiRoot root = UiRoot.Instance;
            PopupLayer pl = PopupLayer.Instance;
            Assert.IsNotNull(pl.CombatLane, "전투 레인 상자(정본 #toasts-combat)");
            RectTransform modals = (RectTransform)root.App.Find("modals");
            Assert.IsNotNull(modals);
            Assert.Less(pl.CombatLane.GetSiblingIndex(), modals.GetSiblingIndex(), "전투 레인은 모달 아래 — 팝업을 읽는 중에 안 끼어든다(정본 z 19 < 20)");
            Assert.Greater(root.App.Find("toasts").GetSiblingIndex(), modals.GetSiblingIndex(), "기본 레인은 팝업 위(정본 z 30)");
            int before = pl.CombatLane.childCount, before0 = root.App.Find("toasts").childCount;
            pl.Toast("💀 쓰러졌다... 회복 후 다시 도전!", "combat");
            yield return null;
            Assert.AreEqual(before + 1, pl.CombatLane.childCount, "combat 레인으로 갔다");
            Assert.AreEqual("combat", pl.LastToastLane);
            pl.Toast("🪙 +10");
            yield return null;
            Assert.AreEqual(before0 + 1, root.App.Find("toasts").childCount, "레인 없음 = 기본 상자");
            Assert.IsNull(pl.LastToastLane);
            Assert.AreEqual(before + 1, pl.CombatLane.childCount, "기본 토스트는 전투 레인에 안 간다");
            pl.Toast("x", "other");
            yield return null;
            Assert.AreEqual(before0 + 2, root.App.Find("toasts").childCount, "모르는 레인은 기본 상자(정본 `lane === 'combat'` 만 갈래)");
        }

        /// <summary>UI 를 한 장 그린다(T134 `RewardBurstTests.Capture` 와 같은 길) — 눈 확인용 · 실패해도 판정을 안 흔든다.</summary>
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
            GameObject camGo = new GameObject("t138-pixel-cam");
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
                try { GallerySheet.Save(tex, saveAs); } catch (System.Exception e) { Debug.Log("[T138] PNG 저장 생략: " + e.Message); }
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
