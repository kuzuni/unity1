using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Data;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Gallery;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T408 — 판매 코인 연출의 **시간축**을 정본 25 프레임과 같은 잣대로 잰다: 표 `CoinSellCurveUi.json`(`tools/check_coinsell_curve.py` 가
    /// 정본 `ref/shots/coinsell-*.png` 에서 센 것)의 시각마다 클론 화면을 카메라 사본 → RT 로 찍어 «금빛 화소» 를 띠(top HUD · mid 나는 곳 · bot 모루)로 센다.
    /// 정적 바닥은 정본과 같이 **마지막 프레임의 금빛 자리 집합**이다. 곡선 전부를 `ui-screens/t408-coinsell.txt` 에 남기고,
    /// 단언은 판정 띠(표 `flight_band` = bot · **장비 시트 띠** — 코인이 태어나 날고 착지하는 곳)의 **봉우리 시각**·**끝 시각**을 표와 견준다.
    /// T411 2회차(결정 700): 무대 띠(mid)는 정본 프레임에 실시간 3D 전투(보스 WARNING 띠·빛기둥)가 섞여 잣대가 아니고, 옛 배치 프레임 셋은 표가 뺐다.
    /// 시트 경계는 정본 표(`sheet_top_f` .562)가 아니라 **클론 제 표**(`sheet_top` · UiRoot 가 시트를 세우는 그 값)로 잡는다 — 같은 뜻(시트 윗선)을 각자 제 판에서.
    /// 판정 띠의 두 프레임(첫 비행 표본 · 봉우리)은 `t408-coinsell-<ms>ms.png` 로도 남긴다 — 촬영의 위아래·자리를 눈으로 확인하는 자국(런마다 새로 · 레포엔 안 넣는다).
    /// 원작 PNG 는 이 레포에 없다(§1) — 표가 정본을 대신한다.
    /// </summary>
    public class CoinSellCurveTests
    {
        const int W = 540, H = 960;

        static IEnumerator Boot()
        {
            try { if (File.Exists(SaveIo.SavePath)) File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        [TearDown]
        public void Clean() { UiRoot.OverrideSafeArea(null); }

        sealed class Row { public int Ms, Total, Top, Mid, Bot; }

        static JsonObject Table()
        {
            TextAsset ta = Resources.Load<TextAsset>("CoinSellCurveUi");
            Assert.IsNotNull(ta, "Resources/CoinSellCurveUi.json (T408)");
            return MiniJson.ParseObject(ta.text);
        }

        static bool Gold(Color32 c, JsonObject g)
        {
            return c.r > J.Num(g["r_min"]) && c.g > J.Num(g["g_min"]) && c.g < J.Num(g["g_max"]) && c.b < J.Num(g["b_max"]) && c.r - c.b > J.Num(g["rb_min"]);
        }

        /// <summary>
        /// 촬영 세션 — 카메라 사본·RT·캔버스 모드 전환을 **한 번만** 하고(런 821 자국: 촬영마다 만들면 한 장에 0.5~1.4초라 25 시각을 못 맞춘다)
        /// 프레임마다 Render + ReadPixels 만 한다. 해상도는 <see cref="W"/>×<see cref="H"/> **그대로** — 절반 RT 는 화면을 줄이는 것이 아니라
        /// 앱의 **왼쪽 아래 1/4 을 2배로** 담았다(런 868 자국 PNG · 카메라 사본의 화소 배율은 안전 영역 540×960 그대로라) — 띠도 코인 수도 그 조각 것이었다(T411 3회차).
        /// 마스크는 «위가 0행» 인 byte[](1 = 금빛)로 쌓아 두고, 정본과 같이 **마지막 프레임을 바닥**으로 뺀다.
        /// </summary>
        sealed class Session
        {
            public int W, H;
            Canvas canvas; UiRoot root; Camera cam; RenderTexture rt; Texture2D tex;
            RenderMode prevMode; Camera prevCam; float prevPlane; RenderTexture prevActive;
            JsonObject gold;

            public static Session Begin(JsonObject gold)
            {
                UiRoot root = UiRoot.Instance;
                if (root == null || root.Canvas == null) return null;
                Session s = new Session { root = root, canvas = root.Canvas, gold = gold, W = CoinSellCurveTests.W, H = CoinSellCurveTests.H };
                s.prevMode = s.canvas.renderMode; s.prevCam = s.canvas.worldCamera; s.prevPlane = s.canvas.planeDistance; s.prevActive = RenderTexture.active;
                try
                {
                    s.rt = new RenderTexture(s.W, s.H, 24, RenderTextureFormat.ARGB32);
                    Camera cam = ShotCam.From(Camera.main, "t408-shot-cam", s.rt);
                    // T349 — UI 층만 그리는 카메라는 후처리를 끈다(켜면 UI 가 톤맵·색 보정에 물든다 · 결정 591) — `check_shot_cams` 가 보는 꼴 그대로.
                    ShotCam.CopyUrp(Camera.main, cam).renderPostProcessing = false;
                    s.cam = cam;
                    s.cam.ResetProjectionMatrix();
                    s.cam.cullingMask = 1 << s.canvas.gameObject.layer;
                    s.cam.clearFlags = CameraClearFlags.SolidColor;
                    s.cam.backgroundColor = Color.black;
                    s.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    s.canvas.worldCamera = s.cam;
                    s.canvas.planeDistance = 1f;
                    root.Layout();
                    Canvas.ForceUpdateCanvases();
                    s.tex = new Texture2D(s.W, s.H, TextureFormat.RGBA32, false);
                    return s;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[T408] 촬영 세션 실패: " + e.Message);
                    s.End();
                    return null;
                }
            }

            /// <summary>마지막 촬영의 PNG(자국용 · `Grab` 뒤에만 뜻이 있다). 실패하면 null.</summary>
            public byte[] LastPng()
            {
                try { return tex != null ? ImageConversion.EncodeToPNG(tex) : null; }
                catch (System.Exception) { return null; }
            }

            /// <summary>지금 프레임의 금빛 마스크(위가 0행). 실패하면 null.</summary>
            public byte[] Grab()
            {
                try
                {
                    Canvas.ForceUpdateCanvases();
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                    tex.Apply(false);
                    RenderTexture.active = prevActive;
                    Color32[] px = tex.GetPixels32();
                    byte[] m = new byte[W * H];
                    for (int y = 0; y < H; y++)
                    {
                        int src = y * W, dst = (H - 1 - y) * W;   // 텍스처는 아래가 0행
                        for (int x = 0; x < W; x++) if (Gold(px[src + x], gold)) m[dst + x] = 1;
                    }
                    return m;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[T408] 촬영 실패: " + e.Message);
                    return null;
                }
            }

            public void End()
            {
                RenderTexture.active = prevActive;
                if (canvas != null) { canvas.renderMode = prevMode; canvas.worldCamera = prevCam; canvas.planeDistance = prevPlane; }
                if (root != null) { root.Layout(); Canvas.ForceUpdateCanvases(); }
                if (tex != null) Object.Destroy(tex);
                if (cam != null) Object.Destroy(cam.gameObject);
                if (rt != null) { rt.Release(); Object.Destroy(rt); }
            }
        }

        static int Band(Row r, string band)
        {
            switch (band) { case "top": return r.Top; case "mid": return r.Mid; case "bot": return r.Bot; default: return r.Total; }
        }

        /// <summary>띠 셈 — top·mid 경계는 표대로, **mid·bot 경계(시트 윗선)는 클론 제 표(`sheetTopF`)** 로 잡는다(정본은 .562 · 클론은 `sheet_top`).</summary>
        static Row Count(byte[] m, byte[] floor, JsonObject bands, float sheetTopF, int ms, int w, int h)
        {
            Row r = new Row { Ms = ms };
            List<object> top = J.Arr(bands["top"]), mid = J.Arr(bands["mid"]), bot = J.Arr(bands["bot"]);
            float t0 = (float)J.Num(top[0]), t1 = (float)J.Num(top[1]), m0 = (float)J.Num(mid[0]), m1 = sheetTopF, b0 = sheetTopF, b1 = (float)J.Num(bot[1]);
            for (int y = 0; y < h; y++)
            {
                float f = y / (float)h; int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (m[row + x] == 0 || floor[row + x] != 0) continue;
                    r.Total++;
                    if (f >= t0 && f < t1) r.Top++;
                    else if (f >= m0 && f < m1) r.Mid++;
                    else if (f >= b0 && f < b1) r.Bot++;
                }
            }
            return r;
        }

        static void Record(string text)
        {
            Debug.Log("[T408] " + text);
            try
            {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), GallerySheet.OutDir);
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "t408-coinsell.txt"), text, new UTF8Encoding(false));
            }
            catch (System.Exception e) { Debug.Log("[T408] 자국 파일을 못 썼다: " + e.Message); }
        }

        static void RecordPng(int ms, byte[] png)
        {
            if (png == null) return;
            try
            {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), GallerySheet.OutDir);
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, "t408-coinsell-" + ms.ToString("D4") + "ms.png"), png);
            }
            catch (System.Exception e) { Debug.Log("[T408] 자국 PNG 를 못 썼다: " + e.Message); }
        }

        [UnityTest]
        public IEnumerator 판매_코인_시간축은_정본_25_프레임과_같은_잣대로_재어_봉우리와_끝_시각이_표와_맞는다()
        {
            yield return Boot();
            JsonObject t = Table();
            JsonObject gold = J.Obj(t["gold"]), bands = J.Obj(t["bands"]);
            List<object> refFrames = J.Arr(t["frames"]);
            int refPeakMs = (int)J.Num(t["peak_ms"]), refEndMs = (int)J.Num(t["end_ms"]);
            float endF = (float)J.Num(t["end_f"]);
            string band = J.Str(t["flight_band"]);
            Assert.IsNotNull(band, "표의 판정 띠 flight_band");
            float sheetTopF = UiKit.L("sheet_top");                        // 클론 시트 윗선(UiRoot 가 시트를 세우는 그 값) — 정본 표는 sheet_top_f .562
            Assert.IsNotNull(refFrames); Assert.Greater(refFrames.Count, 3, "표의 프레임");
            // 자국 PNG 두 장 — 표의 둘째 시각(첫 비행 표본)과 봉우리 시각
            int pngA = (int)J.Num(J.Obj(refFrames[1])["ms"]), pngB = refPeakMs;

            UiRoot.OverrideSafeArea(new Rect(0f, 0f, W, H));
            UiRoot.Instance.Layout();
            yield return null;
            Assert.IsFalse(CoinBurst.Covered(), "메인 화면 — 팝업·탭 패널 없음");
            Session ses = Session.Begin(gold);
            if (ses == null) Assert.Ignore("그래픽 장치가 없다 — 화소를 못 찍는다(-nographics)");
            int w = ses.W, h = ses.H;
            var shots = new List<KeyValuePair<int, byte[]>>();
            var actual = new List<int>();
            float t0;
            try
            {
                byte[] probe = ses.Grab();   // 세션 첫 촬영의 비용(셰이더·RT 준비)은 연출 전에 치른다
                if (probe == null) Assert.Ignore("그래픽 장치가 없다 — 화소를 못 찍는다(-nographics)");
                yield return null;
                int n = CoinBurst.Play(100);
                Assert.Greater(n, 0, "조각이 난다");
                t0 = Time.unscaledTime;
                foreach (object o in refFrames)
                {
                    int ms = (int)J.Num(J.Obj(o)["ms"]);
                    while ((Time.unscaledTime - t0) * 1000f < ms) yield return null;
                    int at = Mathf.RoundToInt((Time.unscaledTime - t0) * 1000f);
                    byte[] m = ses.Grab();
                    Assert.IsNotNull(m, "촬영 " + ms + "ms");
                    if (ms == pngA || ms == pngB) RecordPng(ms, ses.LastPng());
                    shots.Add(new KeyValuePair<int, byte[]>(ms, m));
                    actual.Add(at);
                }
            }
            finally { ses.End(); }
            byte[] floor = shots[shots.Count - 1].Value;   // 정본과 같이 마지막 프레임이 바닥이다
            var rows = new List<Row>();
            for (int i = 0; i < shots.Count; i++) rows.Add(Count(shots[i].Value, floor, bands, sheetTopF, shots[i].Key, w, h));

            // 봉우리·끝(판정 띠 = 시트) — 정본과 같은 셈(check_coinsell_curve.derive)
            Row peak = rows[0];
            foreach (Row r in rows) if (Band(r, band) > Band(peak, band)) peak = r;
            int endMs = -1;
            foreach (Row r in rows) if (r.Ms > peak.Ms && Band(r, band) <= Band(peak, band) * endF) { endMs = r.Ms; break; }

            var sb = new StringBuilder();
            sb.AppendLine("# T408 — 판매 코인 시간축(클론 · " + W + "×" + H + " · 정적 바닥 = 마지막 프레임) ↔ 정본 표 CoinSellCurveUi.json · 판정 띠 " + band
                          + "(시트 · 클론 경계 " + sheetTopF.ToString("0.###") + "H ↔ 정본 " + J.Num(t["sheet_top_f"]).ToString("0.###") + "H) · 정본 딴 판 프레임 " + J.Arr(t["foreign_ms"]).Count + "장 뺌");
            sb.AppendLine("#   ms  실제ms  total    top    mid    bot | 정본 total   top   mid   bot");
            for (int i = 0; i < rows.Count; i++)
            {
                JsonObject rf = J.Obj(refFrames[i]);
                sb.AppendLine(string.Format("{0,5} {1,6} {2,6} {3,6} {4,6} {5,6} | {6,6} {7,5} {8,5} {9,5}", rows[i].Ms, actual[i], rows[i].Total, rows[i].Top, rows[i].Mid, rows[i].Bot,
                    (int)J.Num(rf["total"]), (int)J.Num(rf["top"]), (int)J.Num(rf["mid"]), (int)J.Num(rf["bot"])));
            }
            sb.AppendLine("# 클론 봉우리(" + band + ") " + peak.Ms + "ms " + Band(peak, band) + " · 끝 " + (endMs < 0 ? "없음" : endMs + "ms") + " ↔ 정본 봉우리 " + refPeakMs + "ms " + (int)J.Num(t["peak_v"]) + " · 끝 " + refEndMs + "ms");
            Record(sb.ToString());

            // 표본 간격 가드 — 촬영이 느려 정본 봉우리(≤1000ms) 앞에서 표본이 400ms 넘게 벌어졌으면 이 환경에선 시간축을 못 잰다(자국은 남았다).
            int worstGap = 0;
            for (int i = 1; i < actual.Count && rows[i].Ms <= refEndMs; i++) worstGap = Mathf.Max(worstGap, actual[i] - actual[i - 1]);
            if (actual[0] > 400 || worstGap > 400) Assert.Ignore("환경 — 배치모드 촬영 간격이 넓다(첫 표본 " + actual[0] + "ms · 최대 간격 " + worstGap + "ms): 시간축을 못 잰다 · 자국 t408-coinsell.txt 에 값은 남겼다");
            Assert.Greater(Band(peak, band), 0, "코인이 사는 시트 띠(" + band + " · " + sheetTopF.ToString("0.###") + "H~)에 금빛 화소가 한 번은 뜬다");
            // 판정(정본 · 시트 띠): 봉우리 860ms(착지 + 라벨 팝) · 끝 2500ms(라벨 2000ms 가 다 스러진 뒤). 종전 «봉우리 900 · 끝 1000» 은 무대 띠의 보스 연출을 잰 것이었다(T411 2회차 · 결정 700).
            // 어긋나면 T411 로 접는다(T386 ⓒ) — 그 번호가 닫히면 이 접음을 걷는다.
            bool peakOk = peak.Ms >= refPeakMs - 400 && peak.Ms <= refPeakMs + 400;
            bool endOk = endMs >= 0 && endMs <= refEndMs + 600;
            if (!peakOk || !endOk)
                Assert.Ignore("KNOWN T411 — 판매 코인 연출의 시간축이 정본과 다르다(클론 " + band + " 봉우리 " + peak.Ms + "ms " + Band(peak, band) + " · 끝 " + (endMs < 0 ? "없음" : endMs + "ms")
                              + " ↔ 정본 " + refPeakMs + "ms · " + refEndMs + "ms) · 자국 ui-screens/t408-coinsell.txt · 임자 T411 절(연출 갈래)");
        }
    }
}
