using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.World;
using Forge.Game.Map;

namespace Forge.Tests.PlayMode
{
    /// <summary>T9 — 부팅 씬에 세계(지면 타일·안개·광원)가 서고 챕터 테마 25종을 순회해도 콘솔 빨강 0 인가. 빨간 로그는 러너가 실패시킨다.</summary>
    public class WorldTests
    {
        private static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t0 = Time.realtimeSinceStartup;
            while ((World.Instance == null || !World.Instance.Ready) && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            Assert.IsNotNull(World.Instance, "World 가 Bootstrap 아래에 서지 않았다");
            Assert.IsTrue(World.Instance.Ready, "World 가 20초 안에 준비되지 않았다(StreamingAssets/data 읽기)");
        }

        private static Color C(Col c) { return new Color((float)c.R, (float)c.G, (float)c.B, 1f); }

        private static void AssertColor(string what, Color e, Color g, float eps)
        {
            Assert.AreEqual(e.r, g.r, eps, what + " r"); Assert.AreEqual(e.g, g.g, eps, what + " g"); Assert.AreEqual(e.b, g.b, eps, what + " b");
        }

        [UnityTest]
        public IEnumerator 부팅하면_지면_타일이_서고_테마_25종을_순회해도_빨강_0()
        {
            yield return Boot();
            World w = World.Instance;
            Assert.IsTrue(w.Defs.SimpleBg, "정본 배포값 SIMPLE_BG");
            Assert.AreEqual(25, w.Themes.Count);
            Assert.IsNotNull(w.Ground);
            Assert.AreEqual(80 * 80 * 6, w.GroundMesh.vertexCount, "60×60 / 0.75 = 80² 셀 × 6 (벽 없음)");
            var renderers = w.GetComponentsInChildren<Renderer>();
            Assert.AreEqual(1, renderers.Length, "배경 제거 모드의 세계는 지면 한 메시 = 드로우콜 1");
            Assert.AreEqual(0, w.ThemeIndex, "부팅 = 1챕터");

            Light sun = GameObject.Find(World.SunName).GetComponent<Light>();
            Camera cam = Camera.main;
            for (int i = 0; i < w.Themes.Count; i++)
            {
                w.SetTheme(i);
                yield return null;
                ThemeLook L = w.Look;
                string what = "테마 " + (i + 1) + "(" + L.Biome + ") ";
                Assert.AreEqual(i, w.ThemeIndex);
                Assert.IsTrue(RenderSettings.fog);
                Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode);
                AssertColor(what + "안개색", C(L.FogColor), RenderSettings.fogColor, 1e-3f);
                Assert.AreEqual((float)L.FogNear, RenderSettings.fogStartDistance, 1e-4f, what + "fog near");
                Assert.AreEqual((float)L.FogFar, RenderSettings.fogEndDistance, 1e-4f, what + "fog far");
                AssertColor(what + "배경", C(L.Background), cam.backgroundColor, 1e-3f);
                Assert.AreEqual((float)L.SunIntensity, sun.intensity, 1e-4f, what + "태양 세기");
                AssertColor(what + "태양색", C(L.SunColor), sun.color, 1e-3f);
                Assert.AreEqual(UnityEngine.Rendering.AmbientMode.Trilight, RenderSettings.ambientMode);
                AssertColor(what + "앰비언트 하늘", C(L.HemiColor) * (float)L.HemiIntensity, RenderSettings.ambientSkyColor, 1e-3f);
                Vector3 dir = sun.transform.forward;
                Vector3 expect = -new Vector3((float)L.SunPos[0], (float)L.SunPos[1], -(float)L.SunPos[2]).normalized;
                Assert.Less(Vector3.Angle(dir, expect), 0.1f, what + "태양 방향(원점을 본다 · z 반전)");
            }
            w.SetTheme(0);
            yield return null;
            // 1챕터 = T1 이 씬에 구워 둔 값과 같아야 한다(안개 #ade2c8 · 선형 13~35)
            Assert.AreEqual(13f, RenderSettings.fogStartDistance, 1e-4f);
            Assert.AreEqual(35f, RenderSettings.fogEndDistance, 1e-4f);
            Assert.AreEqual(0xade2c8, w.Look.FogColor.Hex, "1챕터 안개색 = T1 씬 값 #ade2c8");
        }

        [UnityTest]
        public IEnumerator 지면_타일은_영웅이_15_앞서면_30_씩_순환한다()
        {
            yield return Boot();
            World w = World.Instance;
            Assert.AreEqual(0, w.GroundX, 1e-9);
            w.SetWorldX(14.9); Assert.AreEqual(0, w.GroundX, 1e-9, "15 이하는 그대로");
            w.SetWorldX(15.1); Assert.AreEqual(30, w.GroundX, 1e-9, "15 를 넘으면 +30");
            w.SetWorldX(44.9); Assert.AreEqual(30, w.GroundX, 1e-9);
            w.SetWorldX(46); Assert.AreEqual(60, w.GroundX, 1e-9);
            w.SetWorldX(100); Assert.AreEqual(90, w.GroundX, 1e-9, "한 번에 여러 주기를 뛰어도 따라간다");
            yield return null;
            Assert.AreEqual(90f, w.Ground.transform.localPosition.x, 1e-4f);
            Assert.AreEqual(0, w.HeightAt(3, -7), 1e-12, "SIMPLE_BG 높이 0");
            w.SetWorldX(0);
        }
    }
}
