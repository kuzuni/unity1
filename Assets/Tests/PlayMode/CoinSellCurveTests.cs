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
    /// 단언은 등재의 판정 셋 중 시각 둘 — **봉우리(mid) 시각**·**끝(mid) 시각** — 을 표와 견준다(두 물결 꼴은 자국으로 남겨 다음 회차가 읽는다).
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

        /// <summary>UI 층만 검정 위에 한 번 찍어 금빛 화소 자리를 돌려준다(UiShotsTests 의 ② 와 같은 길). 그래픽 장치가 없으면 null.</summary>
        static HashSet<int> Shoot(JsonObject gold, out int w, out int h)
        {
            w = W; h = H;
            UiRoot root = UiRoot.Instance;
            if (root == null || root.Canvas == null) return null;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode; Camera prevCam = canvas.worldCamera; float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            Camera cam = ShotCam.From(Camera.main, "t408-shot-cam", rt);
            Texture2D tex = null;
            try
            {
                int uiLayer = canvas.gameObject.layer;
                UniversalAdditionalCameraData camUrp = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camUrp != null) camUrp.renderPostProcessing = false;
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
                tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply(false);
                Color32[] px = tex.GetPixels32();
                var set = new HashSet<int>();
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        if (Gold(px[y * W + x], gold)) set.Add((H - 1 - y) * W + x);   // 텍스처는 아래가 0행 — 위가 0 인 좌표로 저장
                return set;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[T408] 촬영 실패: " + e.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode; canvas.worldCamera = prevCam; canvas.planeDistance = prevPlane;
                root.Layout(); Canvas.ForceUpdateCanvases();
                if (tex != null) Object.Destroy(tex);
                if (cam != null) Object.Destroy(cam.gameObject);
                rt.Release(); Object.Destroy(rt);
            }
        }

        static Row Count(HashSet<int> pts, HashSet<int> floor, JsonObject bands, int ms, int w, int h)
        {
            Row r = new Row { Ms = ms };
            List<object> top = J.Arr(bands["top"]), mid = J.Arr(bands["mid"]), bot = J.Arr(bands["bot"]);
            foreach (int p in pts)
            {
                if (floor.Contains(p)) continue;
                r.Total++;
                float f = (p / w) / (float)h;
                if (f >= J.Num(top[0]) && f < J.Num(top[1])) r.Top++;
                else if (f >= J.Num(mid[0]) && f < J.Num(mid[1])) r.Mid++;
                else if (f >= J.Num(bot[0]) && f < J.Num(bot[1])) r.Bot++;
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

        [UnityTest]
        public IEnumerator 판매_코인_시간축은_정본_25_프레임과_같은_잣대로_재어_봉우리와_끝_시각이_표와_맞는다()
        {
            yield return Boot();
            JsonObject t = Table();
            JsonObject gold = J.Obj(t["gold"]), bands = J.Obj(t["bands"]);
            List<object> refFrames = J.Arr(t["frames"]);
            int refPeakMs = (int)J.Num(t["peak_ms"]), refEndMs = (int)J.Num(t["end_ms"]);
            float endF = (float)J.Num(t["end_f"]);
            Assert.IsNotNull(refFrames); Assert.Greater(refFrames.Count, 3, "표의 프레임");

            UiRoot.OverrideSafeArea(new Rect(0f, 0f, W, H));
            UiRoot.Instance.Layout();
            yield return null;
            Assert.IsFalse(CoinBurst.Covered(), "메인 화면 — 팝업·탭 패널 없음");
            int w, h;
            HashSet<int> probe = Shoot(gold, out w, out h);
            if (probe == null) Assert.Ignore("그래픽 장치가 없다 — 화소를 못 찍는다(-nographics)");

            int n = CoinBurst.Play(100);
            Assert.Greater(n, 0, "조각이 난다");
            float t0 = Time.unscaledTime;
            var shots = new List<KeyValuePair<int, HashSet<int>>>();
            var actual = new List<int>();
            foreach (object o in refFrames)
            {
                int ms = (int)J.Num(J.Obj(o)["ms"]);
                while ((Time.unscaledTime - t0) * 1000f < ms) yield return null;
                HashSet<int> s = Shoot(gold, out w, out h);
                Assert.IsNotNull(s, "촬영 " + ms + "ms");
                shots.Add(new KeyValuePair<int, HashSet<int>>(ms, s));
                actual.Add(Mathf.RoundToInt((Time.unscaledTime - t0) * 1000f));
            }
            HashSet<int> floor = shots[shots.Count - 1].Value;   // 정본과 같이 마지막 프레임이 바닥이다
            var rows = new List<Row>();
            for (int i = 0; i < shots.Count; i++) rows.Add(Count(shots[i].Value, floor, bands, shots[i].Key, w, h));

            // 봉우리·끝(mid 띠) — 정본과 같은 셈(check_coinsell_curve.derive)
            Row peak = rows[0];
            foreach (Row r in rows) if (r.Mid > peak.Mid) peak = r;
            int endMs = -1;
            foreach (Row r in rows) if (r.Ms > peak.Ms && r.Mid <= peak.Mid * endF) { endMs = r.Ms; break; }

            var sb = new StringBuilder();
            sb.AppendLine("# T408 — 판매 코인 시간축(클론 · 540×960 · 정적 바닥 = 마지막 프레임) ↔ 정본 표 CoinSellCurveUi.json");
            sb.AppendLine("#   ms  실제ms  total    top    mid    bot | 정본 total   top   mid   bot");
            for (int i = 0; i < rows.Count; i++)
            {
                JsonObject rf = J.Obj(refFrames[i]);
                sb.AppendLine(string.Format("{0,5} {1,6} {2,6} {3,6} {4,6} {5,6} | {6,6} {7,5} {8,5} {9,5}", rows[i].Ms, actual[i], rows[i].Total, rows[i].Top, rows[i].Mid, rows[i].Bot,
                    (int)J.Num(rf["total"]), (int)J.Num(rf["top"]), (int)J.Num(rf["mid"]), (int)J.Num(rf["bot"])));
            }
            sb.AppendLine("# 클론 봉우리(mid) " + peak.Ms + "ms " + peak.Mid + " · 끝 " + (endMs < 0 ? "없음" : endMs + "ms") + " ↔ 정본 봉우리 " + refPeakMs + "ms " + (int)J.Num(t["peak_mid"]) + " · 끝 " + refEndMs + "ms");
            Record(sb.ToString());

            Assert.Greater(peak.Mid, 0, "코인이 나는 곳(mid 10~60%H)에 금빛 화소가 한 번은 뜬다");
            Assert.That(peak.Ms, Is.InRange(refPeakMs - 400, refPeakMs + 400), "mid 봉우리 시각 ≈ 정본 " + refPeakMs + "ms(±400 · 프레임 간격 최대 200ms + 배치모드 여유) — 실제 " + peak.Ms);
            Assert.GreaterOrEqual(endMs, 0, "봉우리 뒤 mid 가 봉우리의 " + (endF * 100) + "% 아래로 내려온다(연출이 끝난다)");
            Assert.LessOrEqual(endMs, refEndMs + 600, "mid 끝 시각 ≤ 정본 " + refEndMs + "ms + 600 — 실제 " + endMs);
        }
    }
}
