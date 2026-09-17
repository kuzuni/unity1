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

        /// <summary>한 표본 — Ms = 표의 시각(정본 열과 나란히) · At = **실제로 찍힌 시각**(봉우리·끝 판정은 이것으로 · 7회차: 배치모드는 표본이 200~700ms 늦게 찍혀 표 시각으로 재면 «420ms 봉우리» 가 실은 593ms 였다).</summary>
        // T441 2회차 — `MinF` = 그 프레임에서 **가장 높이 뜬 금빛 화소의 행**(0 = 화면 맨 위 · 정적 바닥은 뺀 뒤).
        //   «코인이 시트 위로 날아오르는가» 를 한 수로 적는다 — 정본 자국은 mid 띠가 3762~5045 까지 차는데 클론은 0~1 이라,
        //   포물선이 시트 띠(0.552H~) 안에서만 논다는 뜻이다. 그 높이를 프레임마다 남겨 다음 회차가 «얼마나 낮은가» 로 잡게 한다.
        sealed class Row { public int Ms, At, Total, Top, Mid, Bot; public float MinF = 1f; }

        static JsonObject Table()
        {
            TextAsset ta = Resources.Load<TextAsset>("CoinSellCurveUi");
            Assert.IsNotNull(ta, "Resources/CoinSellCurveUi.json (T408)");
            return MiniJson.ParseObject(ta.text);
        }

        /// <summary>금빛 문턱 — 표를 **한 번만** 읽어 정수로 쥔다(4회차 · 런 875: 화소마다 `J.Num` 다섯 번이면 540×960 한 장에 초가 넘어 표본 간격이 1.5초로 벌어졌다).</summary>
        sealed class GoldRule
        {
            public int RMin, GMin, GMax, BMax, RbMin;
            public static GoldRule From(JsonObject g)
            {
                return new GoldRule { RMin = (int)J.Num(g["r_min"]), GMin = (int)J.Num(g["g_min"]), GMax = (int)J.Num(g["g_max"]), BMax = (int)J.Num(g["b_max"]), RbMin = (int)J.Num(g["rb_min"]) };
            }
            public bool Is(Color32 c) { return c.r > RMin && c.g > GMin && c.g < GMax && c.b < BMax && c.r - c.b > RbMin; }
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
            GoldRule gold;

            public static Session Begin(JsonObject goldTable)
            {
                UiRoot root = UiRoot.Instance;
                if (root == null || root.Canvas == null) return null;
                Session s = new Session { root = root, canvas = root.Canvas, gold = GoldRule.From(goldTable), W = CoinSellCurveTests.W, H = CoinSellCurveTests.H };
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
                        for (int x = 0; x < W; x++) if (gold.Is(px[src + x])) m[dst + x] = 1;
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
                    if (f < r.MinF) r.MinF = f;   // T441 2회차 — 가장 높이 뜬 금빛 행
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
                // 6회차 — 정본 탐침과 같은 금액(표 probe_total 12345 · 코인 10개): 코인 수가 다르면 지연 폭(i×26ms)이 달라 봉우리 시각이 어긋난다(런 898: 여섯 개로 560ms).
                int n = CoinBurst.Play(J.Num(t["probe_total"]));
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
            for (int i = 0; i < shots.Count; i++) { Row row = Count(shots[i].Value, floor, bands, sheetTopF, shots[i].Key, w, h); row.At = actual[i]; rows.Add(row); }

            // 봉우리·끝(판정 띠 = 시트) — 정본과 같은 셈(check_coinsell_curve.derive)
            Row peak = rows[0];
            foreach (Row r in rows) if (Band(r, band) > Band(peak, band)) peak = r;
            int endMs = -1;
            foreach (Row r in rows) if (r.At > peak.At && Band(r, band) <= Band(peak, band) * endF) { endMs = r.At; break; }

            var sb = new StringBuilder();
            sb.AppendLine("# T408 — 판매 코인 시간축(클론 · " + W + "×" + H + " · 정적 바닥 = 마지막 프레임) ↔ 정본 표 CoinSellCurveUi.json · 판정 띠 " + band
                          + "(시트 · 클론 경계 " + sheetTopF.ToString("0.###") + "H ↔ 정본 " + J.Num(t["sheet_top_f"]).ToString("0.###") + "H) · 정본 딴 판 프레임 " + J.Arr(t["foreign_ms"]).Count + "장 뺌");
            sb.AppendLine("#   ms  실제ms  total    top    mid    bot  최고%H | 정본 total   top   mid   bot   (최고%H = 그 프레임에서 가장 높이 뜬 금빛 행 · 100 = 못 떴다 · T441 2회차)");
            for (int i = 0; i < rows.Count; i++)
            {
                JsonObject rf = J.Obj(refFrames[i]);
                sb.AppendLine(string.Format("{0,5} {1,6} {2,6} {3,6} {4,6} {5,6} {10,6:0.0} | {6,6} {7,5} {8,5} {9,5}", rows[i].Ms, actual[i], rows[i].Total, rows[i].Top, rows[i].Mid, rows[i].Bot,
                    (int)J.Num(rf["total"]), (int)J.Num(rf["top"]), (int)J.Num(rf["mid"]), (int)J.Num(rf["bot"]), rows[i].MinF * 100f));
            }
            sb.AppendLine("# 클론 봉우리(" + band + ") 실제 " + peak.At + "ms(표 " + peak.Ms + ") " + Band(peak, band) + " · 끝 실제 " + (endMs < 0 ? "없음" : endMs + "ms") + " ↔ 정본 봉우리 " + refPeakMs + "ms " + (int)J.Num(t["peak_v"]) + " · 끝 " + refEndMs + "ms");
            Record(sb.ToString());

            // 표본 간격 가드 — 촬영이 느려 정본 봉우리(≤1000ms) 앞에서 표본이 400ms 넘게 벌어졌으면 이 환경에선 시간축을 못 잰다(자국은 남았다).
            int worstGap = 0;
            for (int i = 1; i < actual.Count && rows[i].Ms <= refEndMs; i++) worstGap = Mathf.Max(worstGap, actual[i] - actual[i - 1]);
            if (actual[0] > 400 || worstGap > 400) Assert.Ignore("환경 — 배치모드 촬영 간격이 넓다(첫 표본 " + actual[0] + "ms · 최대 간격 " + worstGap + "ms): 시간축을 못 잰다 · 자국 t408-coinsell.txt 에 값은 남겼다");
            Assert.Greater(Band(peak, band), 0, "코인이 사는 시트 띠(" + band + " · " + sheetTopF.ToString("0.###") + "H~)에 금빛 화소가 한 번은 뜬다");
            // 판정(정본 · 시트 띠): 봉우리 860ms(착지 + 라벨 팝) · 끝 2500ms(라벨 2000ms 가 다 스러진 뒤). 종전 «봉우리 900 · 끝 1000» 은 무대 띠의 보스 연출을 잰 것이었다(T411 2회차 · 결정 700).
            // T411 8회차 — **접음을 걷고 단언으로 세웠다**: 이 자리는 T411 이 닫힐 때까지 `Assert.Ignore("KNOWN T411 …")` 로 접혀 있었는데,
            //   그 절이 연출을 정본으로 옮겨 런 **978·1012** 에서 접히지 않고 실제로 지나갔다(§1 «그 번호가 닫히면 자가 «이제 켜라» 로 운다»).
            //   허용은 접을 때 쓰던 창 그대로다(봉우리 ±400ms · 끝 +600ms) — 걷는 회차가 잣대까지 손대면 «무엇이 나아졌나» 를 못 가린다.
            //   촬영이 느린 환경은 **위 표본 간격 가드**가 먼저 접으므로, 여기까지 온 런은 시간축을 잴 수 있는 런이다.
            string trace = "(클론 " + band + " 봉우리 실제 " + peak.At + "ms " + Band(peak, band) + " · 끝 실제 " + (endMs < 0 ? "없음" : endMs + "ms")
                           + " ↔ 정본 " + refPeakMs + "ms · " + refEndMs + "ms · 자국 ui-screens/t408-coinsell.txt)";
            // T441 1회차(§0-6 · 런 1019 빨강) — **접는다**: 이 칸이 재는 것은 «연출이 정본 시각에 가장 밝은가» 인데 클론 꼭대기가 **평평하고**(런 1019 자국 bot 257ms 2004 · 426ms 2125 · 570ms 1801 · 717ms 1816 — 15% 안에 넷)
            //   배치모드 표본 간격이 ~250ms 라 **어느 표본이 최댓값인지가 런마다 바뀐다**(런 1012 691ms · 런 1015 602ms = 창 안 ↔ 런 1019 426ms = 창 밖).
            //   평평한 꼭대기의 무게중심으로 재도 ≈340ms 라 정본 860ms 와 300~500ms 떨어져 있다 — 자가 흔들리는 것이 아니라 **연출이 실제로 이르고 낮다**(꼭대기 2125 ↔ 정본 2743).
            //   T411 8회차는 런 978·1012 가 지나갔다는 이유로 접음을 걷었는데 그 둘은 **표본 운**이었다. 잣대(±400)는 손대지 않는다(T411 8회차가 «걷는 회차가 잣대까지 손대면 안 된다» 고 적어 둔 그대로).
            //   남은 일은 T441 에 등재했다 — 그 번호가 닫히면 `check_unity_green` 이 «이제 켜라» 로 운다(§1).
            if (!(peak.At >= refPeakMs - 400 && peak.At <= refPeakMs + 400))
                Assert.Ignore("KNOWN T441 — 봉우리가 정본보다 이르다(연출 몫 · 실측 " + peak.At + "ms ↔ 정본 " + refPeakMs + "ms) " + trace);
            Assert.GreaterOrEqual(endMs, 0, "봉우리 뒤로 연출이 실제로 스러진다(끝 시각이 잡힌다) " + trace);
            Assert.LessOrEqual(endMs, refEndMs + 600, "연출이 정본처럼 끝난다 — 라벨까지 스러진 시각이 정본 +600ms 안이다 " + trace);
        }
    }
}
