using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T104 — 글자 키라인을 **픽셀로** 잰다(값 단언만으로는 «칠해졌다» 를 모른다 · T87 ⓔ). 흰 판 위에 «I» 셋을 세운다:
    /// 검정 민글자(대조 · 줄기 폭) · 옛 갈래 <c>UiKit.Outline(0.25)</c> · 새 갈래 <c>UiKit.OutlinePx</c>. UI 를 한 장 그려
    /// (카메라 사본 → RenderTexture → ReadPixels · T27 `UiShotsTests.Capture` 와 같은 길) 줄기 한가운데 행을 훑어
    /// 바깥 검정 띠와 흰 코어를 센다. 정본 `-webkit-text-stroke: N` + `paint-order: stroke fill` 의 그림 = **바깥 N/2 · 채움 그대로**.
    /// 식(<see cref="OutlineSdf"/>)은 모바일 SDF 셰이더에서 읽은 것이라 이 자가 실제 두께로 검산한다 — 1회차 허용은 ±40% · 채움 ±2.5px.
    /// </summary>
    public class OutlineTests
    {
        // 자의 눈금(게임 크기가 아니다): 런 195 실측 캡처 배율 0.262(화면 px / 캔버스 px)에서 96px 글자는 줄기 1px 라 못 쟀다 —
        // 글자를 앱 상자 폭의 1/3.6(≤300px)로, 획은 글자의 13.3%(W = D = 0.667 · 여백 안)로 잡아 바깥 띠가 화면 5px 안팎이 되게 한다.
        const float FontFrac = 1f / 3.6f, FontMaxPx = 300f, StrokeFrac = 0.1333f;
        const float Legacy01 = 0.25f; // 옛 호출부의 대표 상수(Hud 스테이지 라벨 · PetSkillKit 버튼)

        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.Canvas != null && UiRoot.Instance.App != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 20초 안에 안 섰다");
            yield return null;
        }

        private static bool NoGraphics()
        {
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
        }

        [UnityTest]
        public IEnumerator px_갈래는_바깥_띠가_획의_절반이고_채움은_민글자_줄기_그대로다()
        {
            yield return Boot();
            if (NoGraphics()) Assert.Ignore("그래픽 장치가 없다 — 픽셀은 CI 의 유니티 잡이 본다");
            UiRoot root = UiRoot.Instance;
            RectTransform host = UiKit.Box(root.App, "t104-host");
            OutlineSdf o;
            float fontPx = Mathf.Min(FontMaxPx, root.App.rect.width * FontFrac);
            float strokePx = fontPx * StrokeFrac;
            float slot = fontPx * 1.1f;
            try
            {
                Place(host, 0f, slot * 3f + 40f, fontPx * 1.4f);
                Image bg = UiKit.Panel(host, "bg", "white");
                RectTransform b = bg.rectTransform;
                b.anchorMin = Vector2.zero; b.anchorMax = Vector2.one; b.offsetMin = Vector2.zero; b.offsetMax = Vector2.zero;

                // 세 라벨을 가로로: 검정 민글자(대조) · 옛 갈래 · px 갈래
                TextMeshProUGUI plain = UiKit.Text(host, "plain", TextKind.Title, "I", "pp_line");
                plain.fontSize = fontPx; PlaceX((RectTransform)plain.transform, -slot, slot, fontPx * 1.3f);
                TextMeshProUGUI legacy = UiKit.Text(host, "legacy", TextKind.Title, "I", "white");
                legacy.fontSize = fontPx; PlaceX((RectTransform)legacy.transform, 0f, slot, fontPx * 1.3f);
                UiKit.Outline(legacy, "pp_line", Legacy01);
                TextMeshProUGUI px = UiKit.Text(host, "px", TextKind.Title, "I", "white");
                px.fontSize = fontPx; PlaceX((RectTransform)px.transform, slot, slot, fontPx * 1.3f);
                o = UiKit.OutlinePx(px, "pp_line", strokePx);   // 글자 크기를 정한 뒤에 부른다(그 순간의 fontSize 로 환산)
                Assert.IsFalse(o.Clipped, "글자 " + fontPx + "px 에서 획 " + strokePx + " 은 여백 안이어야 한다: " + o.VisiblePx + "/" + o.WantedPx);
                Assert.AreEqual(strokePx * 0.5, o.VisiblePx, 1e-2, "식: 보이는 띠 = 획/2");
                yield return null;
                yield return null;

                Shot shot = Grab(new RectTransform[] { (RectTransform)plain.transform, (RectTransform)legacy.transform, (RectTransform)px.transform }, "screen_t104-outline");
                try
                {
                    float scale = shot.Rects[2].height / ((RectTransform)px.transform).rect.height;   // 화면 px / 캔버스 px
                    Assert.Greater(scale, 0.05f, "캡처 배율");
                    int stem = Stem(shot, shot.Rects[0]);
                    int legL, legCore, legR; Bands(shot, shot.Rects[1], out legL, out legCore, out legR);
                    int pxL, pxCore, pxR; Bands(shot, shot.Rects[2], out pxL, out pxCore, out pxR);
                    float want = strokePx * 0.5f * scale;
                    if (want < 3f) Assert.Ignore("캡처 배율 " + scale.ToString("0.000") + " 에서 기대 띠가 " + want.ToString("0.0") + "px 라 ±40% 를 못 잰다(런 195 꼴) — 글자 " + fontPx + "px · 앱 폭 " + root.App.rect.width);
                    string info = "글자 " + fontPx.ToString("0") + "px 획 " + strokePx.ToString("0.0") + " · 배율 " + scale.ToString("0.000") + " · 민글자 줄기 " + stem + " · 옛 띠 " + legL + "/" + legR + " 코어 " + legCore
                        + " · px 띠 " + pxL + "/" + pxR + " 코어 " + pxCore + " · 기대 띠 " + want.ToString("0.00") + " (W=" + o.Width01.ToString("0.000") + " D=" + o.Dilate.ToString("0.000")
                        + " · 재질 G=" + Mat(px, "_GradientScale") + " R=" + Mat(px, "_ScaleRatioA") + " dilate=" + Mat(px, "_FaceDilate") + " outline=" + Mat(px, "_OutlineWidth")
                        + " 샘플링 " + (px.font != null ? px.font.faceInfo.pointSize.ToString() : "?") + "pt)";
                    Debug.Log("[T104] " + info);
                    Assert.Greater(stem, 2, "민글자 «I» 줄기가 안 보인다 — " + info);
                    Assert.Greater(pxL, 1, "px 갈래 왼쪽 띠가 없다 — " + info);
                    Assert.Greater(pxR, 1, "px 갈래 오른쪽 띠가 없다 — " + info);
                    Assert.GreaterOrEqual(pxL, legL + 2, "px 갈래 띠가 옛 갈래보다 2px 이상 두꺼워야 한다(등재 진단 · 런 200 옛 3/2 ↔ px 5/6) — " + info);
                    Assert.GreaterOrEqual(pxL, want * 0.6f, "띠가 식보다 얇다 — " + info);
                    Assert.LessOrEqual(pxL, want * 1.4f + 1f, "띠가 식보다 두껍다 — " + info);
                    Assert.LessOrEqual(Mathf.Abs(pxL - pxR), 2, "띠가 좌우 비대칭 — " + info);
                    // 채움이 그대로인가: 흰 코어 폭 ≈ 검정 줄기 폭(AA 문턱 차이로 코어가 1~2px 좁게 읽힌다 · 띠가 채움을 먹었으면 2×띠만큼 좁다)
                    Assert.GreaterOrEqual(pxCore, stem - 2f, "px 갈래가 채움을 먹었다(_FaceDilate 보정 실패) — " + info);
                    Assert.LessOrEqual(pxCore, stem + 2f, "px 갈래가 채움을 부풀렸다 — " + info);
                }
                finally { shot.Dispose(); }
            }
            finally
            {
                Object.Destroy(host.gameObject);
            }
            yield return null;
        }

        /// <summary>
        /// T104 2회차 — 호출부가 정본 폭표(<see cref="KeylineUi"/>)를 **캔버스 px 로 환산해** <see cref="UiKit.OutlinePx"/> 에 넘기는가.
        /// ⓐ 표: px 절은 ×css_px(정본 499 ↔ 앱 상자 1080 = 2.164) · em 절의 {em, min_px} 는 정본 `max(Npx, .Mem)` ⓑ 화면: 소환 시트 제목(정본 h2.sheet-title .11em)이
        /// `_FaceDilate` &gt; 0(채움을 안 먹는 갈래)이고 `outlineWidth` 가 그 글자 크기·재질로 환산한 식과 같다 — 옛 `Outline(0.3)` 이었으면 dilate 0.
        /// 두께 자체의 픽셀 검산은 위 픽셀 자가 이미 했다(런 200·204).
        /// </summary>
        [UnityTest]
        public IEnumerator 호출부는_정본_폭표를_캔버스_px_로_환산해_받는다()
        {
            yield return Boot();
            float css = KeylineUi.CssPx;
            Assert.Greater(css, 1.5f, "css_px(정본 CSS px → 캔버스 px)가 표에 있어야 한다: " + css);
            Assert.AreEqual(0.5f * css, KeylineUi.Px("chat_time"), 1e-4f, "px 절은 css_px 를 곱한다(2ae9903 보고 · .chat-time .5px)");
            Assert.AreEqual(2f * css, KeylineUi.Stroke("sk_lv", 40f), 1e-4f, "Stroke 는 px 절을 먼저 본다(#panel-skills .sk-lv 2px)");
            Assert.AreEqual(0.11f * 40f, KeylineUi.Stroke("sheet_title", 40f), 1e-4f, "em 절은 글자 크기에 곱한다(.11em)");
            Assert.AreEqual(1.2f * css, KeylineUi.Stroke("petd_name", 10f), 1e-4f, "max(1.2px, .125em): 작은 글자는 1.2 CSS px 바닥");
            Assert.AreEqual(0.125f * 100f, KeylineUi.Stroke("petd_name", 100f), 1e-4f, "max(1.2px, .125em): 큰 글자는 em");
            Assert.Throws<KeyNotFoundException>(() => KeylineUi.Stroke("이런_키_없다", 10f));

            UiRoot root = UiRoot.Instance;
            root.TabBar.OnTab("summon");
            for (int i = 0; i < 4; i++) yield return null;
            TextMeshProUGUI title = null;
            foreach (TextMeshProUGUI t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                if (t.name == "sheet-title" && t.gameObject.activeInHierarchy) { title = t; break; }
            Assert.IsNotNull(title, "소환 시트 제목(sheet-title)이 안 섰다");
            Material m = title.fontMaterial;
            float g = m.GetFloat("_GradientScale"), r = m.GetFloat("_ScaleRatioA"), ps = title.font.faceInfo.pointSize;
            OutlineSdf want = OutlineSdf.FromStroke(KeylineUi.Stroke("sheet_title", title.fontSize), title.fontSize, g, r, ps);
            Assert.Greater(m.GetFloat("_FaceDilate"), 0f, "px 갈래는 _FaceDilate 로 채움을 지킨다 — 옛 Outline(width01) 이면 0 이다");
            Assert.AreEqual((double)want.Width01, (double)title.outlineWidth, 1e-3, "outlineWidth = FromStroke(.11em × 글자 " + title.fontSize + "px) 의 Width01");
            Assert.AreEqual((double)want.Dilate, (double)m.GetFloat("_FaceDilate"), 1e-3, "_FaceDilate = 같은 식의 Dilate(D = W)");
            Assert.IsFalse(want.Clipped, "제목 .11em 은 이 글꼴 여백 안이어야 한다(잘리면 T121 의 몫): 보이는 " + want.VisiblePx + " / 원한 " + want.WantedPx);
            yield return null;
        }

        // ---- 도우미 ----

        private static void Place(RectTransform rt, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, y);
        }

        private static string Mat(TextMeshProUGUI t, string prop)
        {
            Material m = t.fontMaterial;
            return m != null && m.HasProperty(prop) ? m.GetFloat(prop).ToString("0.###") : "?";
        }

        private static void PlaceX(RectTransform rt, float x, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, 0f);
        }

        /// <summary>검정↔흰 의 50% 덮임 문턱 — 줄기(검정)와 코어(흰)를 같은 문턱으로 세야 AA 편향이 상쇄된다(런 200: 어두움 100 미만·밝음 190 초과의 두 문턱은 +1.7px 를 만들었다).</summary>
        const int Mid = 128;

        private static int Lum(Color32 c) { return (c.r * 299 + c.g * 587 + c.b * 114) / 1000; }

        /// <summary>줄기(검정 «I»)의 한가운데 행에서 가장 긴 어두운 구간.</summary>
        private static int Stem(Shot s, RectInt r)
        {
            int[] row = MidRow(s, r);
            int best = 0, run = 0;
            for (int i = 0; i < row.Length; i++) { if (row[i] < Mid) { run++; if (run > best) best = run; } else run = 0; }
            return best;
        }

        /// <summary>흰 글자 + 검정 띠: 한가운데 행을 왼쪽부터 훑어 «첫 어두운 구간(왼 띠) · 그 뒤 밝은 구간(코어) · 다음 어두운 구간(오른 띠)».</summary>
        private static void Bands(Shot s, RectInt r, out int left, out int core, out int right)
        {
            int[] row = MidRow(s, r);
            left = core = right = 0;
            int i = 0, n = row.Length;
            while (i < n && row[i] >= Mid) i++;                // 배경(흰) 지나기
            while (i < n && row[i] < Mid) { left++; i++; }    // 왼 띠
            while (i < n && row[i] >= Mid) { core++; i++; }   // 코어(흰) — 50% 덮임 한 문턱이라 줄기와 같은 자로 잰다
            while (i < n && row[i] < Mid) { right++; i++; }   // 오른 띠
        }

        /// <summary>어두운 픽셀이 있는 행들의 한가운데 행(밝기 배열 · 왼→오른).</summary>
        private static int[] MidRow(Shot s, RectInt r)
        {
            List<int> dark = new List<int>();
            for (int y = r.yMin; y < r.yMax; y++)
                for (int x = r.xMin; x < r.xMax; x++)
                    if (Lum(s.Px[y * s.W + x]) < Mid) { dark.Add(y); break; }
            Assert.Greater(dark.Count, 0, "칸 안에 어두운 픽셀이 하나도 없다(" + r + ")");
            int mid = dark[dark.Count / 2];
            int[] row = new int[r.width];
            for (int x = 0; x < r.width; x++) row[x] = Lum(s.Px[mid * s.W + r.xMin + x]);
            return row;
        }

        private sealed class Shot
        {
            public Color32[] Px; public int W, H; public RectInt[] Rects; public Texture2D Tex;
            public void Dispose() { if (Tex != null) Object.Destroy(Tex); }
        }

        /// <summary>UI 를 한 장 그린다(T87 `CountPixels`·T27 `Capture` 와 같은 길) — 카메라 사본 → RT → ReadPixels · 칸들의 화면 사각도 같이 잰다.</summary>
        private static Shot Grab(RectTransform[] targets, string saveAs)
        {
            UiRoot root = UiRoot.Instance;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("t104-pixel-cam");
            Camera cam = camGo.AddComponent<Camera>();
            Shot s = new Shot { W = w, H = h, Rects = new RectInt[targets.Length] };
            try
            {
                int uiLayer = canvas.gameObject.layer;
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                // T349 — `CopyFrom` 은 URP 추가 데이터(renderPostProcessing·volumeLayerMask·antialiasing·renderShadows)를
                //        **안 옮긴다** — 그래서 촬영 PNG 의 3D 띠가 톤맵·노출·색 보정 없이 찍혔다(런 503 실측).
                ShotCam.CopyUrp(Camera.main, cam);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                cam.ResetProjectionMatrix();
                cam.cullingMask = 1 << uiLayer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.Render();

                RenderTexture.active = rt;
                s.Tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                s.Tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
                s.Tex.Apply(false);
                s.Px = s.Tex.GetPixels32();

                Vector3[] corners = new Vector3[4];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i].GetWorldCorners(corners);
                    Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                    Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x)), 0, w - 1);
                    int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x)), 0, w - 1);
                    int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y)), 0, h - 1);
                    int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y)), 0, h - 1);
                    s.Rects[i] = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
                }
                if (!string.IsNullOrEmpty(saveAs))
                {
                    try { GallerySheet.Save(s.Tex, saveAs); }
                    catch (System.Exception e) { Debug.LogWarning("[T104] 그림 저장 실패(단언은 계속): " + e.Message); }
                }
                return s;
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.targetTexture = null;
                Object.Destroy(camGo);
                Object.Destroy(rt);
            }
        }
    }
}
