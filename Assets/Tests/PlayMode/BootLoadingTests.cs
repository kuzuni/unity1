using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>T142 2회차 — 부팅 로딩 오버레이가 표대로 서고 진행률·단계 글자·페이드가 정본 규칙을 따르는가.
    /// 진행률을 미는 쪽(부팅 절차)은 3회차가 단다 — 여기서는 손으로 밀어 본다.</summary>
    public class BootLoadingTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        private static BootLoading Open()
        {
            // ⚠ `Ensure` 는 이미 선 것이 있으면 그것을 돌려준다 — 진짜 부팅이 세운(따라가는) 오버레이를
            //   물려받으면 이 테스트가 민 진행률을 다음 프레임에 덮어쓴다(런 336 실측). 먼저 치운다.
            if (BootLoading.Instance != null) Object.DestroyImmediate(BootLoading.Instance.gameObject);
            BootLoading.ResetCache();
            return BootLoading.Ensure(UiRoot.Instance.App);
        }

        [UnityTest]
        public IEnumerator 오버레이가_표대로_선다()
        {
            yield return Boot();
            BootLoading bl = Open();
            yield return null;
            Assert.IsNotNull(bl, "오버레이가 안 섰다");

            Transform root = bl.transform;
            Assert.IsNotNull(root.Find("bg"), "바탕이 없다");
            Transform box = root.Find("bl-box");
            Assert.IsNotNull(box, "가운데 상자가 없다");
            Assert.IsNotNull(box.Find("bl-forge/bl-anvil"), "모루가 없다");
            Assert.IsNotNull(box.Find("bl-forge/bl-hammer"), "망치가 없다");
            Assert.IsNotNull(box.Find("bl-title"), "제목이 없다");
            Assert.IsNotNull(box.Find("bl-track/bl-fill"), "진행 막대 채움이 없다");
            Assert.IsNotNull(box.Find("bl-stage"), "단계 글자가 없다");
            for (int i = 1; i <= 3; i++)
                Assert.IsNotNull(box.Find("bl-forge/bl-spark-" + i), "불티 " + i + " 이 없다");

            Assert.AreEqual("포지 클론", box.Find("bl-title").GetComponent<TextMeshProUGUI>().text);
            Object.Destroy(bl.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 진행률을_밀면_채움_폭과_단계_글자가_같이_간다()
        {
            yield return Boot();
            BootLoading bl = Open();
            yield return null;
            var spec = BootLoading.Spec;
            RectTransform fill = (RectTransform)bl.transform.Find("bl-box/bl-track/bl-fill");
            TextMeshProUGUI stage = bl.transform.Find("bl-box/bl-stage").GetComponent<TextMeshProUGUI>();

            bl.Set(8);
            yield return null;
            Assert.AreEqual(spec.FillWidthPx(8), fill.sizeDelta.x, 0.01, "8% 채움 폭");
            Assert.AreEqual("세이브 불러오는 중…", stage.text);

            bl.Set(42);
            yield return null;
            Assert.AreEqual(spec.FillWidthPx(42), fill.sizeDelta.x, 0.01, "42% 채움 폭");
            Assert.AreEqual("전장 짓는 중…", stage.text);

            // 마지막 단계(100%)는 정본 blSet 이 label 없이 부른다 — 글자를 안 건드린다
            bl.Set(96);
            yield return null;
            Assert.AreEqual("마무리 중…", stage.text);
            bl.Set(100);
            yield return null;
            Assert.AreEqual("마무리 중…", stage.text, "100% 는 글자를 안 바꾼다(정본 blSet 의 `if (s && label)`)");
            Assert.AreEqual(spec.TrackWPx, fill.sizeDelta.x, 0.01, "100% 면 막대가 꽉 찬다");

            Object.Destroy(bl.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 망치는_돌고_불티는_깜박인다()
        {
            yield return Boot();
            BootLoading bl = Open();
            yield return null;
            RectTransform hammer = (RectTransform)bl.transform.Find("bl-box/bl-forge/bl-hammer");
            // 피벗이 «머리 쪽 끝»(정본 transform-origin: 88% 88%)이라야 내리치는 그림이 된다
            Assert.AreEqual(0.88f, hammer.pivot.x, 0.001f);
            Assert.AreEqual(0.88f, hammer.pivot.y, 0.001f);

            float first = hammer.localEulerAngles.z;
            for (int i = 0; i < 20; i++) yield return null;
            float later = hammer.localEulerAngles.z;
            Assert.AreNotEqual(first, later, "망치가 멈춰 있다");

            Object.Destroy(bl.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 부팅이_끝난_씬에서는_스스로_100까지_가고_사라진다()
        {
            yield return Boot();
            // SampleScene 이 다 선 뒤(= 여섯 신호가 전부 참)에 오버레이를 띄우면, 아무도 밀어 주지 않아도
            // 제 눈으로 «다 섰다» 를 읽고 100% 로 가서 스스로 치워져야 한다(T142 4회차 배선).
            float t0 = 0f;
            while (!MetaHost.Ready && t0 < 10f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "부팅이 안 끝났다 — 이 단언의 전제가 없다");

            if (BootLoading.Instance != null) Object.DestroyImmediate(BootLoading.Instance.gameObject);
            BootLoading.ResetCache();
            BootLoading bl = BootLoading.Ensure(UiRoot.Instance.App, true);   // 진짜 부팅처럼 «따라가기» 를 켠다
            yield return null;
            yield return null;
            GameObject go = bl.gameObject;
            float t = 0f;
            while (go != null && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(go == null, "다 선 씬에서는 스스로 100% 로 가서 사라져야 한다");
        }

        /// <summary>덮개 캔버스를 한 장 그려 `ui-screens/&lt;name&gt;.png` 로 남긴다 — §1 «실제 화면을 본다».
        /// 오버레이 캔버스는 카메라 렌더에 안 들어가므로(그것이 8회차가 노린 성질이다) 찍을 때만 잠깐
        /// `ScreenSpaceCamera` 로 돌린다 — `ForgeUiTests.CountPixels` 와 같은 길이다.</summary>
        private static void Shoot(string name)
        {
            Canvas c = BootLoading.Canvas;
            if (c == null) return;
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            RenderMode prevMode = c.renderMode;
            Camera prevCam = c.worldCamera;
            float prevPlane = c.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("boot-shot-cam");
            Camera cam = camGo.AddComponent<Camera>();
            Texture2D shot = null;
            try
            {
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.ResetProjectionMatrix();
                cam.targetTexture = rt;
                cam.cullingMask = 1 << c.gameObject.layer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                shot = new Texture2D(w, h, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
                shot.Apply(false);
                GallerySheet.Save(shot, name);
            }
            finally
            {
                RenderTexture.active = prevActive;
                c.renderMode = prevMode;
                c.worldCamera = prevCam;
                c.planeDistance = prevPlane;
                if (shot != null) Object.Destroy(shot);
                Object.Destroy(camGo);
                Object.Destroy(rt);
            }
        }

        /// <summary>T142 8회차 — 배선. 부팅 뿌리 한 줄(`Bootstrap.Awake` → `BootLoading.Begin`)이 덮개를 띄우고,
        /// 덮개는 **앱 캔버스 밖 제 캔버스**에 서며(정본 `#boot-loading` 은 `#app` 밖 · `z-index: 200`),
        /// 여섯 신호가 다 서면 스스로 사라진다.
        ///
        /// «앱 캔버스 밖» 이 이 작업의 핵심이다 — 4·6회차는 `UiRoot.App` 아래에 세웠고, 그래서 이 레포의
        /// 픽셀·촬영 자 여덟(`UiRoot.Canvas` 를 제 카메라로 그려 `ReadPixels` 한다)이 덮개까지 같이 그려
        /// «맞는 색 0개» 로 빨개졌다(런 336·354). 그 자들을 한 줄도 안 고치고 푸는 길이 여기다.</summary>
        [UnityTest]
        public IEnumerator 부팅_뿌리가_덮개를_띄우고_앱_캔버스_밖에_세우고_다_서면_치운다()
        {
            yield return Boot();

            BootLoading bl = BootLoading.Instance;
            Assert.IsNotNull(bl, "부팅 뿌리(Bootstrap.Awake → BootLoading.Begin)가 덮개를 안 띄웠다");
            Canvas c = BootLoading.Canvas;
            Assert.IsNotNull(c, "덮개가 제 캔버스에 서지 않았다");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, c.renderMode, "정본 position: fixed — 화면 전체를 덮는 오버레이 캔버스다");
            Assert.AreEqual(BootLoading.Spec.ZIndex, c.sortingOrder, "정본 인라인 CSS z-index: 200");
            Assert.IsNull(c.GetComponent<GraphicRaycaster>(), "덮개는 입력을 안 먹는다(결정 323 조건 ⓐ) — 레이캐스터가 없어야 한다");

            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 안 섰다 — 이 단언의 전제가 없다");
            Assert.IsFalse(bl.transform.IsChildOf(UiRoot.Instance.transform),
                           "덮개가 앱 캔버스 아래 있다 — 정본은 #app **밖**이다(index.html 21 «마크업도 #app 앞»). " +
                           "여기 있으면 픽셀·촬영 자 여덟이 덮개를 같이 그린다(런 336·354)");

            // §1 «실제 화면을 본다» — 아직 덮개가 서 있는 지금 한 장 남긴다.
            Shoot("screen_boot-loading");

            GameObject go = bl.gameObject;
            double lastPct = -1;
            string lastStage = null;
            float t = 0f;
            while (go != null && t < 40f)
            {
                if (bl != null) { lastPct = bl.Pct; lastStage = bl.Stage; }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(go == null,
                          "부팅이 다 선 뒤에는 덮개가 스스로 사라져야 한다(정본 blDone) — " +
                          "40초 뒤에도 " + lastPct.ToString("0.#") + "% «" + lastStage + "» 에 멈춰 있다");
            Assert.IsNull(BootLoading.Canvas, "덮개가 치워지면 제 캔버스도 같이 치워져야 한다");
        }

        [UnityTest]
        public IEnumerator 끝나면_페이드하고_스스로_사라진다()
        {
            yield return Boot();
            BootLoading bl = Open();
            yield return null;
            GameObject go = bl.gameObject;
            CanvasGroup g = go.GetComponent<CanvasGroup>();
            Assert.IsNotNull(g, "페이드용 CanvasGroup 이 없다");
            Assert.AreEqual(1f, g.alpha, 0.001f);

            bl.Done();
            Assert.IsTrue(bl.Fading);
            float t = 0f;
            while (go != null && t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(go == null, "정본 blDone 처럼 스스로 치워져야 한다(450ms)");
        }
    }
}
