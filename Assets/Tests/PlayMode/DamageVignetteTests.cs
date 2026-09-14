using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core;
using Forge.Core.Battle;
using Forge.Core.BattleFx;
using Forge.Core.Data;
using Forge.Core.Save;
using Forge.Core.Hero;
using Forge.Game;
using Forge.Game.Gallery;
using Forge.Game.Battle;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T135 ⓐ — 피격 붉은 비네트가 **화면에 실제로 선다**. 셈은 EditMode(`DmgVignetteRulesTests`)가 재고,
    /// 여기서는 ⓐ 면이 서는가 ⓑ 층이 정본 z 12(HUD 위 · 사망 암전·씬컷 아래)인가 ⓒ 그림이 «가운데 투명 · 가장자리 붉음 · 바닥 사라짐» 인가
    /// ⓓ 시계가 돌아 걷히는가 ⓔ 전투의 영웅 피격이 실제로 이것을 부르는가 를 본다.
    /// 판정 PNG: **정점 한 장**(`screen_t135-vignette.png` · 주인 눈 확인용) — 촬영 목록의 화면들은 피격 순간을 안 담으므로
    /// T104·T134·T138 과 같은 길로 이 테스트가 직접 한 장 굽는다.
    /// </summary>
    public class DamageVignetteTests
    {
        static GameData _data; static SaveDefs _defs;
        static GameData Data { get { return _data ?? (_data = GameData.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "data"))); } }
        static SaveDefs Defs { get { return _defs ?? (_defs = SaveDefs.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.streamingAssetsPath, "data", SaveDefs.FileName)))); } }
        static HeroStats StrongHero() { return new HeroStats { Atk = Big.Of(5000), Hp = Big.Of(100000), CritCh = 30, CritDmg = 100, AttacksPerSec = 1.1 }; }

        static IEnumerator Boot()
        {
            BattleScene.AutoBoot = false;
            if (BattleScene.Instance != null) UnityEngine.Object.Destroy(BattleScene.Instance.gameObject);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator 정점_한_장을_구워_눈_확인에_남긴다()
        {
            yield return Boot();
            float t = 0f;
            while (!MetaHost.Ready && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            var ov = BattleOverlay.Ensure();
            Assert.IsNotNull(ov);
            // 사망 세기(상한 .64)로 켠 뒤 정점 구간(13~44ms)에서 굽는다 — 가장 진한 프레임이 눈에 제일 잘 읽힌다.
            ov.FlashDamage(1);
            ov.Tick(0.02f);
            Assert.AreEqual((float)FxRules.DmgVigMax, ov.Vignette.color.a, 1e-3, "정점에서 굽는다");
            Capture("screen_t135-vignette");
            yield return null;
        }

        /// <summary>UI 를 한 장 그린다(T104 `OutlineTests.Grab` · T134 `RewardBurstTests.Capture` 와 같은 길) — 눈 확인용 · 실패해도 판정을 안 흔든다.</summary>
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
            GameObject camGo = new GameObject("t135-pixel-cam");
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
                try { GallerySheet.Save(tex, saveAs); } catch (System.Exception e) { Debug.Log("[T135] PNG 저장 생략: " + e.Message); }
                UnityEngine.Object.Destroy(tex);
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                root.Layout();
                cam.targetTexture = null;
                UnityEngine.Object.Destroy(camGo);
                UnityEngine.Object.Destroy(rt);
            }
        }

        static Color32 At(Sprite sp, double u, double v)
        {
            Texture2D t = sp.texture;
            int x = Mathf.Clamp((int)(u * t.width), 0, t.width - 1);
            int y = Mathf.Clamp((int)(v * t.height), 0, t.height - 1);
            return t.GetPixel(x, y);
        }

        [UnityTest]
        public IEnumerator 피격_비네트는_HUD_위_사망암전_아래에_서고_시계대로_걷힌다()
        {
            yield return Boot();
            var ov = BattleOverlay.Ensure();
            Assert.IsNotNull(ov, "UI 껍데기가 없다");
            Assert.IsFalse(ov.VignetteActive, "부팅 직후에는 꺼져 있다");

            ov.FlashDamage(0.2);
            Assert.IsTrue(ov.VignetteActive);
            Assert.AreEqual(0.53, ov.VignettePeak, 1e-9, "--vig = .32 + .2×1.05");
            Image vig = ov.Vignette;
            Assert.IsNotNull(vig, "비네트 면이 안 섰다");
            Assert.IsTrue(vig.gameObject.activeInHierarchy, "켜져 있어야 한다");
            Assert.IsFalse(vig.raycastTarget, "`pointer-events: none`");

            // 층 — 정본 css 438 은 z 12 를 «사망 암전(15)·팝업(20+) 아래» 로 못 박는다. 이 띠 안에서는 형제 순서가 서열이다.
            Assert.AreEqual(0, vig.transform.GetSiblingIndex(), "비네트는 오버레이 띠의 맨 아래 형제 — 암전·씬컷이 위로 온다");
            Assert.AreSame(ov.Layer, vig.rectTransform.parent, "`#game-area` 안이다(상단바·시트·탭바로 안 샌다)");
            Assert.AreEqual(Vector2.zero, vig.rectTransform.offsetMin);
            Assert.AreEqual(Vector2.zero, vig.rectTransform.offsetMax);

            // 시계 — 13ms 정점 · 44ms 까지 유지 · 440ms 에 걷힘.
            Assert.AreEqual(FxRules.DmgVigAlpha(0, ov.VignettePeak), vig.color.a, 1e-4, "0ms");
            ov.Tick(0.02f);
            Assert.AreEqual((float)ov.VignettePeak, vig.color.a, 1e-3, "20ms 에 정점");
            ov.Tick(0.02f);
            Assert.AreEqual((float)ov.VignettePeak, vig.color.a, 1e-3, "40ms 도 아직 정점(플래토 31ms)");
            ov.Tick(0.2f);
            Assert.Less(vig.color.a, (float)ov.VignettePeak, "240ms 에는 빠지는 중");
            Assert.Greater(vig.color.a, 0f);
            ov.Tick(0.3f);
            Assert.IsFalse(ov.VignetteActive, "440ms 를 지나면 걷힌다");
            Assert.IsFalse(vig.gameObject.activeSelf);
            Assert.AreEqual(0f, ov.VignetteAlpha, 1e-6);
        }

        [UnityTest]
        public IEnumerator 연타해도_처음부터_다시_돌고_사망은_상한_세기다()
        {
            yield return Boot();
            var ov = BattleOverlay.Ensure();
            ov.FlashDamage(0.2);
            ov.Tick(0.3f);
            float faded = ov.Vignette.color.a;
            Assert.Less(faded, (float)ov.VignettePeak, "꼬리 구간");
            ov.FlashDamage(0.2);                       // 정본은 리플로우를 강제해 애니메이션을 되감는다
            ov.Tick(0.02f);
            Assert.AreEqual((float)ov.VignettePeak, ov.Vignette.color.a, 1e-3, "다시 부르면 정점에서 시작한다");

            ov.FlashDamage(1);                          // `heroDown` 자리
            Assert.AreEqual(FxRules.DmgVigMax, ov.VignettePeak, 1e-9, "사망은 상한 .64");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 그림이_가운데는_투명하고_가장자리는_붉고_바닥은_사라진다()
        {
            yield return Boot();
            var ov = BattleOverlay.Ensure();
            ov.FlashDamage(1);
            Sprite sp = ov.Vignette.sprite;
            Assert.IsNotNull(sp, "구운 그라디언트가 없다 — 단색 면이면 화면 전체가 붉게 잠긴다");

            Color32 mid = At(sp, 0.5, 0.5);
            Assert.AreEqual(0, mid.a, "한가운데는 완전 투명 — 3D 전투가 읽혀야 한다");
            // 위 모서리(마스크가 안 깎는 자리) — 정본 rgba(255,58,44,.92).
            Color32 corner = At(sp, 0.001, 0.999);
            Assert.Greater(corner.a, 200, "위 모서리는 진하다");
            Assert.Greater(corner.r, 200, "붉다");
            Assert.Less(corner.g, 120, "붉은 쪽이지 흰색·주황이 아니다");
            Assert.Greater(corner.r - corner.b, 100, "R 이 B 보다 한참 높다");
            // 바닥 모서리 — 세로 마스크가 100% 에서 0 으로 깎는다.
            Color32 bottom = At(sp, 0.001, 0.001);
            Assert.Less(bottom.a, corner.a, "바닥은 마스크가 깎아 위 모서리보다 옅다");
            Assert.LessOrEqual(bottom.a, 8, "씬 아래 끝에서는 거의 사라진다(직선으로 잘리지 않는다)");
            // 반지름을 따라 알파가 오른다.
            Assert.Less(At(sp, 0.5, 0.72).a, At(sp, 0.5, 0.95).a, "가운데→가장자리로 갈수록 진해진다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 전투의_영웅_피격이_비네트를_부른다()
        {
            yield return Boot();
            var b = UnityEngine.Object.FindAnyObjectByType<Bootstrap>();
            var s = BattleScene.Create(b != null ? b.transform : null, false);
            s.ManualStep = true;
            s.Attach(BattleScene.MakeBattle(Data, Defs, StrongHero, 9), Data, Defs);
            var ov = BattleOverlay.Ensure();
            Assert.IsFalse(ov.VignetteActive);
            // 정본 `scene3d.js` 13351 이 영웅 피격에서 부르는 자리 — 씬이 그 이벤트를 삼키면 비네트가 켜져야 한다.
            // ⚠ 큐를 비우는 `Drain` 은 **논리 틱이 한 번 지날 때만** 돈다(`Step` 의 `while (acc >= BattleRules.Tick)`).
            //    한 프레임(16ms)만 밀면 이벤트가 큐에 남아 있어 «안 불렀다» 로 읽힌다 — 틱 하나를 통째로 민다(런 280 에서 이걸로 빨갰다).
            s.Battle.Events.Add(new BattleEvent { Kind = BattleEventKind.HeroHit, Num = 0.2 });
            s.Step((float)BattleRules.Tick);
            Assert.IsTrue(ov.VignetteActive, "영웅 피격이 비네트를 안 불렀다");
            Assert.AreEqual(0.53, ov.VignettePeak, 1e-9, "피해 비율이 그대로 세기로 들어간다");
            // 정본 13374 `UI.flashDamage(1)` — «치명타 피격보다 진하게».
            s.Battle.Events.Add(new BattleEvent { Kind = BattleEventKind.HeroDown });
            s.Step((float)BattleRules.Tick);
            Assert.AreEqual(FxRules.DmgVigMax, ov.VignettePeak, 1e-9, "사망은 상한 세기 .64");
            UnityEngine.Object.Destroy(s.gameObject);
            yield return null;
        }

    }
}
