using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
            // 🚨 렌더러의 엣지 기능은 **꺼 둔 채** 실린다(결정 331) — 런 350~355 에서 이 패스가 모든 카메라의
            //    화면을 통째로 회색 128 로 씻어 남의 픽셀 자 넷을 깨뜨렸다. 그래서 이 자만 켰다 끈다.
            object feature = Feature();
            if (feature == null) { Assert.Ignore("렌더러에서 EdgeOutline 기능을 못 찾았다(파이프라인 꼴이 바뀌었다) — 켤 수 없으니 판정도 못 한다"); yield break; }
            Shader sh = Shader.Find(EdgeOutlineHost.ShaderName);
            Assert.IsNotNull(sh, "엣지 셰이더가 없다 — 렌더러 기능이 그릴 것이 없다");
            // 🚨 컴파일 에러가 난 셰이더는 `isSupported` 가 false 다. 이 한 줄이 없으면 런 343 처럼
            //    «검정 화소 0» 이라는 **증상**만 남아 배선을 뒤지게 된다(진짜 원인은 include 경로였다).
            Assert.IsTrue(sh.isSupported, "엣지 셰이더가 컴파일에 실패했다 — 에디터 로그의 «Shader error in Forge/EdgeOutline» 줄을 보라");

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

                // 🚨 런 364 실측: off 프레임이 **통째로 회색 128** 이었다 = 패스는 도는데 `_BlitTexture` 가 안 물렸다.
                //    가장 그럴듯한 갈래는 «손으로 쓴 렌더러 에셋의 `fetchColorBuffer` 가 실제로는 안 실렸다» 이다
                //    (그 칸이 꺼져 있으면 URP 는 색 복사본을 만들지 않아 재질이 안 물린 텍스처를 읽는다).
                //    그래서 이 회차는 **그 칸을 리플렉션으로 켜 놓고** 재고, 원래 읽힌 값을 단언 메시지에 실어 보낸다 —
                //    다음 런 하나로 «표가 안 실렸다» 인지 «주입점이 틀렸다» 인지 갈린다. 잰 뒤에는 원래대로 되돌린다.
                string before = Describe(feature);
                object savedFetch = Get(feature, "fetchColorBuffer");
                Set(feature, "fetchColorBuffer", true);
                SetActive(feature, true);
                EdgeOutlineHost.SetOn(true);
                yield return null;
                on = Shoot(cam, rt);
                EdgeOutlineHost.SetOn(false);
                yield return null;
                off = Shoot(cam, rt);
                SetActive(feature, false);
                if (savedFetch != null) Set(feature, "fetchColorBuffer", savedFetch);

                // 한 런에서 둘째 갈래까지 같이 가른다: 단색이면 **주입점을 500(투명 뒤)으로 내려** 한 장 더 찍는다.
                //    거기서 그림이 살면 «600(후처리 뒤)에서는 활성 색이 최종 타깃과 갈린다» 가 답이다.
                string alt = "(안 쟀다)";
                if (Uniform(off))
                {
                    object savedInj = Get(feature, "injectionPoint");
                    Set(feature, "injectionPoint", 500);
                    SetActive(feature, true);
                    EdgeOutlineHost.SetOn(false);
                    yield return null;
                    Texture2D probe = Shoot(cam, rt);
                    SetActive(feature, false);
                    if (savedInj != null) Set(feature, "injectionPoint", savedInj);
                    alt = Uniform(probe) ? "주입점 500 에서도 단색(" + Mid(probe) + ")" : "주입점 500 에서는 장면이 산다(" + Mid(probe) + ") — 600 이 범인";
                    try { GallerySheet.Save(probe, "screen_t147-edge-probe500"); } catch (System.Exception e) { Debug.LogWarning("[T147] 진단 그림 저장 실패: " + e.Message); }
                    Object.DestroyImmediate(probe);
                }

                int darkOn = Dark(on), darkOff = Dark(off);
                try { GallerySheet.Save(on, "screen_t147-edge-on"); GallerySheet.Save(off, "screen_t147-edge-off"); }
                catch (System.Exception e) { Debug.LogWarning("[T147] 그림 저장 실패(단언은 계속): " + e.Message); }

                // 🚨 먼저 «입력이 물렸는가» 를 가른다. off 프레임의 셰이더는 `src` 를 그대로 돌려주므로,
                //    그 그림이 **단색**이면 장면이 아니라 `_BlitTexture` 를 못 받은 것이다(유니티는 안 물린 텍스처에 회색 128 을 물린다).
                //    이 한 줄이 없으면 «검정 화소 0» 이라는 증상만 남아 판정식을 뒤지게 된다(런 354 실측).
                Assert.IsFalse(Uniform(off), "off 프레임이 통째로 단색이다(" + Mid(off) + ") — 패스는 도는데 입력(_BlitTexture)이 안 물렸다. 에셋에 실린 값 «" + before + "» · 이 판은 fetchColorBuffer 를 켜 놓고 쟀다 · " + alt);
                Assert.IsFalse(Uniform(on), "on 프레임이 통째로 단색이다(" + Mid(on) + ") — 위와 같은 갈래(입력 없음) · 에셋 값 «" + before + "»");
                Assert.AreEqual(0, darkOff, "off 프레임에 검정 화소가 있다 — 네 항이 다 꺼지지 않았거나 장면이 원래 어둡다");
                Assert.Greater(darkOn, 0, "on 프레임에 검정 화소가 0 — 엣지 패스가 안 돈다(렌더러 기능 등록·머티리얼·깊이 요구 확인)");
                // 상자 둘레는 2 × (3유닛 상자의 화면 폭 + 높이) 남짓이고 선은 1~2px 이라, 화면의 한 줌이어야 한다(면이 통째로 칠해지면 임계가 틀린 것이다).
                Assert.Less(darkOn, Size * Size / 8, "검정이 화면의 1/8 을 넘는다 — 선이 아니라 면이 칠해졌다");
            }
            finally
            {
                SetActive(feature, false);
                EdgeOutlineHost.SetOn(true);
                RenderTexture.active = prevActive;
                if (on != null) Object.DestroyImmediate(on);
                if (off != null) Object.DestroyImmediate(off);
                Object.DestroyImmediate(rig);
                rt.Release(); Object.DestroyImmediate(rt);
            }
        }


        /// <summary>
        /// 렌더러 에셋에 실린 엣지 기능을 **이름으로** 찾는다(URP 타입을 컴파일에 끌어들이지 않으려고 리플렉션 — 하니스는 URP 를 스텁으로 문다).
        /// 못 찾으면 null: 파이프라인 꼴이 바뀐 것이니 이 자는 판정하지 않고 물러난다.
        /// </summary>
        static object Feature()
        {
            Object rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as Object
                        ?? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as Object;
            if (rp == null) return null;
            FieldInfo list = rp.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            var datas = list != null ? list.GetValue(rp) as System.Array : null;
            if (datas == null) return null;
            foreach (object data in datas)
            {
                if (data == null) continue;
                FieldInfo feats = data.GetType().GetField("m_RendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
                var items = feats != null ? feats.GetValue(data) as System.Collections.IEnumerable : null;
                if (items == null) continue;
                foreach (object f in items)
                {
                    var o = f as Object;
                    if (o != null && o.name == "EdgeOutline") return f;
                }
            }
            return null;
        }


        /// <summary>렌더러 에셋에 **실제로 실린** 값을 한 줄로 — 손으로 쓴 YAML 이 그대로 들어갔는지 보는 자리.</summary>
        static string Describe(object feature)
        {
            var sb = new System.Text.StringBuilder();
            string[] names = { "injectionPoint", "fetchColorBuffer", "requirements", "passIndex", "bindDepthStencilAttachment", "passMaterial" };
            for (int i = 0; i < names.Length; i++)
            {
                object v = Get(feature, names[i]);
                var o = v as Object;
                string text = v == null ? "(없음/null)" : (o != null ? o.name : v.ToString());
                sb.Append(names[i]).Append('=').Append(text);
                if (i < names.Length - 1) sb.Append(" · ");
            }
            return sb.ToString();
        }

        static object Get(object feature, string field)
        {
            FieldInfo f = feature != null ? feature.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public) : null;
            return f != null ? f.GetValue(feature) : null;
        }

        static void Set(object feature, string field, object value)
        {
            FieldInfo f = feature != null ? feature.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public) : null;
            if (f == null) return;
            object v = value;
            if (f.FieldType.IsEnum && !(value is System.Enum)) v = System.Enum.ToObject(f.FieldType, value);
            else if (f.FieldType != typeof(object) && value != null && f.FieldType != value.GetType()) v = System.Convert.ChangeType(value, f.FieldType);
            f.SetValue(feature, v);
        }

        static void SetActive(object feature, bool on)
        {
            if (feature == null) return;
            MethodInfo m = feature.GetType().GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public);
            if (m != null) m.Invoke(feature, new object[] { on });
        }

        /// <summary>그림이 통째로 한 색인가 — 안 물린 텍스처(회색 128)를 «장면» 으로 착각하지 않으려는 자.</summary>
        static bool Uniform(Texture2D tex)
        {
            Color32[] px = tex.GetPixels32();
            for (int i = 1; i < px.Length; i++)
                if (px[i].r != px[0].r || px[i].g != px[0].g || px[i].b != px[0].b) return false;
            return true;
        }

        static string Mid(Texture2D tex)
        {
            Color32 c = tex.GetPixels32()[tex.width * (tex.height / 2) + tex.width / 2];
            return "rgb " + c.r + "," + c.g + "," + c.b;
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
