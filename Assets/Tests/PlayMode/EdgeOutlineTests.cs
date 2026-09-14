using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Forge.Core.Render;
using Forge.Game.Gallery;
using Forge.Game.Render;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T147 2회차 — 캐릭터 윤곽선(후처리 깊이-엣지)이 **화면에 실제로 그려지는가**.
    /// 셈은 EditMode(`EdgeOutlineRulesTests`)가 표와 함께 잰다. 여기서는 배선을 잰다:
    /// ⓐ 표의 수치가 전역 유니폼으로 실리는가 ⓑ 판정기의 on/off 가 **네 항을 한꺼번에** 여닫는가
    /// ⓒ 깊이 계단 하나를 세운 장면에서 **on 프레임에만** 검정 띠가 생기는가(정본 판정기와 같은 차분).
    /// </summary>
    public class EdgeOutlineTests
    {
        const float DarkMax = 0.15f;   // 이보다 어두우면 «선» 으로 센다(바탕·물체는 아래에서 밝게 칠한다)
        const int Size = 256;

        private static bool NoGraphics()
        {
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
        }

        [Test]
        public void 표의_수치가_전역_유니폼으로_실린다()
        {
            EdgeOutlineSpec s = EdgeOutlineHost.Spec;
            var go = new GameObject("t147-host");
            try
            {
                EdgeOutlineHost host = go.AddComponent<EdgeOutlineHost>();
                host.Apply();
                Assert.AreEqual((float)s.EdgeK, Shader.GetGlobalFloat(EdgeOutlineHost.EdgeKProp), 1e-6f, "_EdgeK");
                Assert.AreEqual((float)s.NormalK, Shader.GetGlobalFloat(EdgeOutlineHost.NormalKProp), 1e-6f, "_EdgeNormalK");
                Assert.AreEqual((float)s.CreaseK, Shader.GetGlobalFloat(EdgeOutlineHost.CreaseKProp), 1e-6f, "_EdgeCreaseK");
                Assert.AreEqual((float)s.CreaseHystF, Shader.GetGlobalFloat(EdgeOutlineHost.CreaseHystProp), 1e-6f, "_EdgeCreaseHyst");
                Assert.AreEqual((float)s.EdgeMaxZ, Shader.GetGlobalFloat(EdgeOutlineHost.MaxZProp), 1e-6f, "_EdgeMaxZ");
                Assert.AreEqual((float)s.DiagF, Shader.GetGlobalFloat(EdgeOutlineHost.DiagFProp), 1e-6f, "_EdgeDiagF");
                Assert.AreEqual((float)s.R2F, Shader.GetGlobalFloat(EdgeOutlineHost.R2FProp), 1e-6f, "_EdgeR2F");
                // 두께 스위치는 그 화면 폭이 정한다 — 정본 devicePixelRatio 자리(1 이면 팽창 없음 · 2 이상이면 한 칸).
                bool want = EdgeOutlineRules.DilateOn(s, EdgeOutlineRules.BufScale(s, Screen.width));
                Assert.AreEqual(want ? 1f : 0f, Shader.GetGlobalFloat(EdgeOutlineHost.DilateProp), 1e-6f,
                    "화면 폭 " + Screen.width + "px → 버퍼/CSS 비 " + EdgeOutlineRules.BufScale(s, Screen.width).ToString("0.00"));
                Assert.AreEqual(Color.black, EdgeOutlineHost.LineColor(s), "정본 선 색은 검정");
            }
            finally { EdgeOutlineHost.SetOn(true); Object.DestroyImmediate(go); }
        }

        [Test]
        public void off_프레임은_네_항을_한꺼번에_끈다()
        {
            try
            {
                EdgeOutlineHost.SetOn(false);
                Assert.AreEqual(0f, Shader.GetGlobalFloat(EdgeOutlineHost.OnProp), 1e-6f, "off 프레임에 항이 남으면 차분 마스크가 선을 통째로 지운다");
                Assert.IsFalse(EdgeOutlineHost.On);
                EdgeOutlineHost.SetOn(true);
                Assert.AreEqual(1f, Shader.GetGlobalFloat(EdgeOutlineHost.OnProp), 1e-6f);
                Assert.IsTrue(EdgeOutlineHost.On);
            }
            finally { EdgeOutlineHost.SetOn(true); }
        }

        [UnityTest]
        public IEnumerator 깊이_계단에_검정_띠가_생기고_off_프레임엔_없다()
        {
            if (NoGraphics()) { Assert.Ignore("그래픽 장치가 없다 — 픽셀은 CI 의 유니티 잡이 본다"); yield break; }
            Assert.IsNotNull(Shader.Find(EdgeOutlineHost.ShaderName), "엣지 셰이더가 없다 — 렌더러 기능이 그릴 것이 없다");

            GameObject rig = new GameObject("t147-rig");
            RenderTexture rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevActive = RenderTexture.active;
            Texture2D on = null, off = null;
            try
            {
                var hostGo = new GameObject("t147-host");
                hostGo.transform.SetParent(rig.transform, false);
                hostGo.AddComponent<EdgeOutlineHost>().Apply();

                Camera cam = new GameObject("t147-cam").AddComponent<Camera>();
                cam.transform.SetParent(rig.transform, false);
                cam.transform.position = Vector3.zero;
                cam.transform.rotation = Quaternion.identity;
                cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f; cam.fieldOfView = 45f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.6f, 0.6f, 0.65f);
                cam.targetTexture = rt;

                // 뒷벽(z = 20) ↔ 앞 상자(z = 10): 계단 10 유닛 ≫ edgeK×10 = 0.28 이라 실루엣 항이 **가까운 쪽**을 칠한다.
                Cube(rig.transform, new Vector3(0f, 0f, 20f), new Vector3(40f, 40f, 0.2f), new Color(0.72f, 0.74f, 0.78f));
                Cube(rig.transform, new Vector3(0f, 0f, 10f), new Vector3(3f, 3f, 3f), new Color(0.85f, 0.80f, 0.55f));
                yield return null;

                EdgeOutlineHost.SetOn(true);
                yield return null;
                on = Shoot(cam, rt);
                EdgeOutlineHost.SetOn(false);
                yield return null;
                off = Shoot(cam, rt);

                int darkOn = Dark(on), darkOff = Dark(off);
                try { GallerySheet.Save(on, "screen_t147-edge-on"); GallerySheet.Save(off, "screen_t147-edge-off"); }
                catch (System.Exception e) { Debug.LogWarning("[T147] 그림 저장 실패(단언은 계속): " + e.Message); }

                Assert.AreEqual(0, darkOff, "off 프레임에 검정 화소가 있다 — 네 항이 다 꺼지지 않았거나 장면이 원래 어둡다");
                Assert.Greater(darkOn, 0, "on 프레임에 검정 화소가 0 — 엣지 패스가 안 돈다(렌더러 기능 등록·머티리얼·깊이 요구 확인)");
                // 상자 둘레는 2 × (3유닛 상자의 화면 폭 + 높이) 남짓이고 선은 1~2px 이라, 화면의 한 줌이어야 한다(면이 통째로 칠해지면 임계가 틀린 것이다).
                Assert.Less(darkOn, Size * Size / 8, "검정이 화면의 1/8 을 넘는다 — 선이 아니라 면이 칠해졌다");
            }
            finally
            {
                EdgeOutlineHost.SetOn(true);
                RenderTexture.active = prevActive;
                if (on != null) Object.DestroyImmediate(on);
                if (off != null) Object.DestroyImmediate(off);
                Object.DestroyImmediate(rig);
                rt.Release(); Object.DestroyImmediate(rt);
            }
        }

        static void Cube(Transform parent, Vector3 pos, Vector3 scale, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Renderer r = go.GetComponent<Renderer>();
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh);
            m.color = c;
            r.sharedMaterial = m;
        }

        static Texture2D Shoot(Camera cam, RenderTexture rt)
        {
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            tex.Apply(false);
            return tex;
        }

        static int Dark(Texture2D tex)
        {
            Color32[] px = tex.GetPixels32();
            int n = 0;
            for (int i = 0; i < px.Length; i++)
            {
                float l = (px[i].r * 0.299f + px[i].g * 0.587f + px[i].b * 0.114f) / 255f;
                if (l < DarkMax) n++;
            }
            return n;
        }
    }
}
