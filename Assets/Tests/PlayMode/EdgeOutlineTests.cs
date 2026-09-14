using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Forge.Core.Render;
using Forge.Game.Gallery;
using Forge.Game.Render;
using Forge.Game.Voxel;
using Forge.Game.Hero;
using Forge.Core.Data;

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
            // 10회차부터 렌더러 에셋은 기능을 **켠 채** 싣는다 — 이 자는 원래 상태를 기억했다가 되돌린다.
            object savedActive = GetHidden(feature, "m_Active");
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

                // 🚨 런 364~395 의 자취: 이 패스는 한때 화면을 회색 128 로 씻었다 — 색 복사본(`fetchColorBuffer`)을
                //    받아야 했는데 손으로 쓴 에셋에서 그 칸만 `False` 로 실렸기 때문이다(`t147-feature.txt` 실측).
                //    8회차는 **색을 아예 안 읽는 쪽**으로 피했다(알파 혼합으로 선만 얹는다 · 정본 `mix(c, 검정, edge)` 와 같은 식).
                string before = Describe(feature);
                Debug.Log("[T147] 렌더러에 실제로 실린 값 — " + before);
                // 8회차부터 셰이더는 **색을 안 읽고 얹는다**(알파 혼합) — `fetchColorBuffer` 가 꺼져 있어도 그린다.
                //    (런 395 의 `t147-feature.txt`: 손으로 쓴 그 칸만 `False` 로 실렸다. 안 쓰는 쪽으로 피했다.)
                Note("t147-feature.txt", "T147 진단 — 렌더러에 실제로 실린 값\n" + before +
                     "\n셰이더는 색을 안 읽고 얹는다(Blend SrcAlpha OneMinusSrcAlpha) — fetchColorBuffer 는 안 쓴다.\n" +
                     "화면 " + Size + "px · 팽창 " + Shader.GetGlobalFloat(EdgeOutlineHost.DilateProp) + "\n");

                SetActive(feature, true);
                EdgeOutlineHost.SetOn(true);
                yield return null;
                on = Shoot(cam, rt);
                EdgeOutlineHost.SetOn(false);
                yield return null;
                off = Shoot(cam, rt);
                SetActive(feature, false);

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
                Assert.IsFalse(Uniform(off), "off 프레임이 통째로 단색이다(" + Mid(off) + ") — 패스는 도는데 입력(_BlitTexture)이 안 물렸다. 에셋에 실린 값 «" + before + "» · " + alt);
                Assert.IsFalse(Uniform(on), "on 프레임이 통째로 단색이다(" + Mid(on) + ") — 위와 같은 갈래(입력 없음) · 에셋 값 «" + before + "»");
                Assert.AreEqual(0, darkOff, "off 프레임에 검정 화소가 있다 — 네 항이 다 꺼지지 않았거나 장면이 원래 어둡다");
                Assert.Greater(darkOn, 0, "on 프레임에 검정 화소가 0 — 엣지 패스가 안 돈다(렌더러 기능 등록·머티리얼·깊이 요구 확인)");
                // 상자 둘레는 2 × (3유닛 상자의 화면 폭 + 높이) 남짓이고 선은 1~2px 이라, 화면의 한 줌이어야 한다(면이 통째로 칠해지면 임계가 틀린 것이다).
                Assert.Less(darkOn, Size * Size / 8, "검정이 화면의 1/8 을 넘는다 — 선이 아니라 면이 칠해졌다");

                // 🖊️ **두께 규약을 화면에서 잰다**(정본의 핵심: «선은 어느 기기에서나 1.00 CSS px»).
                //    같은 장면을 팽창만 켜고 한 장 더 찍어 검정 화소를 센다 — 반경 1 검출 + 한 칸 팽창이므로 **두 배**여야 한다.
                //    둘레로 눈금을 맞춘다: 앞 상자 색 화소 수 N 이 곧 (한 변)² 이라 둘레 ≈ 4√N — 그래서 화면 크기·카메라와 무관한 자가 된다.
                float savedDilate = Shader.GetGlobalFloat(EdgeOutlineHost.DilateProp);
                Shader.SetGlobalFloat(EdgeOutlineHost.DilateProp, 1f);
                SetActive(feature, true);
                EdgeOutlineHost.SetOn(true);
                yield return null;
                Texture2D wide = Shoot(cam, rt);
                SetActive(feature, false);
                Shader.SetGlobalFloat(EdgeOutlineHost.DilateProp, savedDilate);
                int darkWide = Dark(wide);
                int boxPx = Second(off);   // off 프레임에서 «바탕 다음으로 넓은 색» = 앞 상자(색을 코드에 안 박는다)
                try { GallerySheet.Save(wide, "screen_t147-edge-dilate"); } catch (System.Exception e) { Debug.LogWarning("[T147] 그림 저장 실패: " + e.Message); }
                Object.DestroyImmediate(wide);
                double perim = boxPx > 0 ? 4.0 * System.Math.Sqrt(boxPx) : 0.0;
                Note("t147-thickness.txt",
                     "T147 두께 실측 — 앞 상자 화소 " + boxPx + " → 둘레 ≈ " + perim.ToString("0") +
                     "\n팽창 off 검정 " + darkOn + " (둘레당 " + (perim > 0 ? (darkOn / perim).ToString("0.00") : "?") + ")" +
                     "\n팽창 on  검정 " + darkWide + " (둘레당 " + (perim > 0 ? (darkWide / perim).ToString("0.00") : "?") + ")\n");
                Assert.Greater(boxPx, 100, "앞 상자가 화면에 안 잡힌다 — 장면 세우기가 틀렸다");
                Assert.That(darkOn / perim, Is.EqualTo(1.0).Within(0.6),
                    "팽창 off 인데 선이 둘레당 한 줄이 아니다(" + (darkOn / perim).ToString("0.00") + ") — 정본은 이 대역에서 1 버퍼px 이다");
                Assert.That(darkWide / perim, Is.EqualTo(2.0).Within(0.8),
                    "팽창 on 인데 선이 둘레당 두 줄이 아니다(" + (darkWide / perim).ToString("0.00") + ") — 팽창은 반경 1 검출에 한 칸을 더해 2 버퍼px 을 만든다");
            }
            finally
            {
                SetActive(feature, savedActive is bool && (bool)savedActive);
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


        /// <summary>글자 한 장을 촬영 폴더에 남긴다 — CI 가 `screens` 로 올리므로 **초록인 런에서도** 다음 회차가 읽는다.</summary>
        static void Note(string file, string text)
        {
            try
            {
                string dir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), GallerySheet.OutDir);
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, file), text);
            }
            catch (System.Exception e) { Debug.LogWarning("[T147] 글자 남기기 실패(단언은 계속): " + e.Message); }
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


        /// <summary>숨은 필드 읽기 — 기반 클래스의 `m_Active`(SerializeField private)를 보려고.</summary>
        static object GetHidden(object feature, string field)
        {
            for (System.Type t = feature != null ? feature.GetType() : null; t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null) return f.GetValue(feature);
            }
            return null;
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


        /// <summary>
        /// «바탕 다음으로 넓은 색» 의 화소 수 = 앞 상자의 넓이. 색을 코드에 안 박는다(톤맵·색공간이 바뀌어도 산다).
        /// 이 넓이 N 이 (한 변)² 이므로 둘레 ≈ 4√N 이고, 그것이 선 두께를 재는 눈금이 된다.
        /// </summary>
        static int Second(Texture2D tex)
        {
            Color32[] px = tex.GetPixels32();
            var tally = new Dictionary<int, int>();
            for (int i = 0; i < px.Length; i++)
            {
                int key = (px[i].r >> 3 << 10) | (px[i].g >> 3 << 5) | (px[i].b >> 3);  // 5bit 씩 — 미세한 흔들림은 한 칸으로 본다
                int had; tally.TryGetValue(key, out had); tally[key] = had + 1;
            }
            int first = 0, second = 0;
            foreach (var kv in tally)
            {
                if (kv.Value > first) { second = first; first = kv.Value; }
                else if (kv.Value > second) second = kv.Value;
            }
            return second;
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
        // ───────── T330 1회차 — 파츠 ID 태그 배선 ─────────

        /// <summary>정본 `idMatFor`: 파츠마다 생성 시각에 번호 하나 · 0 없음 · 서로 다름 · 살아 있는 동안만 목록에 있다.</summary>
        [Test]
        public void 복셀_몹의_파츠마다_ID_번호가_생성_시각에_굳는다()
        {
            GameData data = GalleryData.Load();
            Assert.Greater(data.Pets.Count, 0, "펫 표가 비었다");
            MobModel model = data.Pets[data.Pets.Names[0]];
            var root = new GameObject("t330-rig");
            try
            {
                VoxelMobRig rig = VoxelMob.Build(model, 0, 0, root.transform, "t330 " + model.Name);
                Assert.Greater(rig.Renderers.Count, 1, "파츠가 하나뿐이면 이 항이 잴 경계가 없다");
                var seen = new HashSet<int>();
                foreach (MeshRenderer mr in rig.Renderers)
                {
                    EdgePartIdTag tag = mr.GetComponent<EdgePartIdTag>();
                    Assert.IsNotNull(tag, mr.name + " 에 파츠 ID 태그가 없다");
                    Assert.AreSame(mr, tag.Target);
                    Assert.That(tag.Id, Is.InRange(1, EdgePartIdRules.MaxId), mr.name + " 번호 범위");
                    Assert.IsTrue(seen.Add(tag.Id), mr.name + " 번호가 겹친다: " + tag.Id);
                    Assert.IsTrue(EdgePartId.Live.Contains(tag), "살아 있는 태그 목록에 있어야 ID 패스가 돈다");
                    Assert.AreSame(tag, EdgePartId.Tag(mr), "다시 붙여도 같은 번호(태그) — 번호는 안 바뀐다");
                    Vector4 u = EdgePartId.Uniform(tag.Id);
                    Assert.AreEqual(tag.Id, EdgePartIdRules.Decode(u.x, u.y), "전역 벡터 (r,g) 가 제 번호로 되읽힌다");
                }
                // «ID 패스에 넣는가» — 코앞 카메라는 전부 넣고, 아주 먼 카메라는 전부(짧은 변 < 6px) 뺀다
                var camGo = new GameObject("t330-cam");
                try
                {
                    Camera cam = camGo.AddComponent<Camera>();
                    cam.fieldOfView = 60f;
                    EdgeOutlineSpec s = EdgeOutlineHost.Spec;
                    cam.transform.position = new Vector3(0, 0.5f, -2f);
                    cam.transform.LookAt(root.transform.position + Vector3.up * 0.5f);
                    int use = 0;
                    foreach (MeshRenderer mr in rig.Renderers) if (EdgePartId.UseId(mr.GetComponent<EdgePartIdTag>(), cam, s, 960)) use++;
                    Assert.Greater(use, 0, "2m 앞에서는 큰 파츠가 남아야 한다");
                    cam.transform.position = new Vector3(0, 0.5f, -5000f);
                    foreach (MeshRenderer mr in rig.Renderers)
                        Assert.IsFalse(EdgePartId.UseId(mr.GetComponent<EdgePartIdTag>(), cam, s, 960), mr.name + " — 5km 밖에서는 화면 1px 도 안 된다");
                }
                finally { Object.DestroyImmediate(camGo); }
                Object.DestroyImmediate(root);
                root = null;
                foreach (int id in seen) Assert.IsFalse(EdgePartId.Live.Exists(t => t != null && t.Id == id), "걷힌 파츠는 목록에서 빠진다: " + id);
            }
            finally { if (root != null) Object.DestroyImmediate(root); }
        }
        // ───────── T330 4회차 — 영웅 렌더러 태그 ─────────

        /// <summary>정본 `renderIdPass` 의 roots 첫째는 `heroG` — 영웅의 상자·데칼·무기 전부가 파츠 번호를 받는다.</summary>
        [Test]
        public void 영웅_리그의_상자_데칼_무기_전부가_파츠_ID_를_받는다()
        {
            var root = new GameObject("t330-hero");
            try
            {
                HeroRig rig = HeroRig.Create(root.transform, "t330 hero");
                Assert.Greater(rig.Boxes.Count, 1, "영웅 상자가 하나뿐이면 잴 경계가 없다");
                var seen = new HashSet<int>();
                foreach (MeshRenderer mr in rig.Boxes)
                {
                    EdgePartIdTag tag = mr.GetComponent<EdgePartIdTag>();
                    Assert.IsNotNull(tag, "상자 " + mr.name + " 에 태그가 없다");
                    Assert.IsTrue(seen.Add(tag.Id), "번호가 겹친다: " + tag.Id);
                }
                foreach (MeshRenderer mr in rig.Decals) Assert.IsNotNull(mr.GetComponent<EdgePartIdTag>(), "데칼 " + mr.name + " 에 태그가 없다(작은 것은 ID 패스가 그때 뺀다)");
                // 런 483: Create 는 무기를 안 세운다 — 무기 렌더러는 Equip(정본 refreshHeroEquip)이 만든다(막대 한 자루 = 기본 club 파지).
                rig.Equip(null, null);
                Assert.Greater(rig.WeaponMount.childCount, 0, "무기 그룹이 비었다");
                var weapon = rig.WeaponMount.GetChild(0).GetComponent<MeshRenderer>();
                Assert.IsNotNull(weapon);
                Assert.IsNotNull(weapon.GetComponent<EdgePartIdTag>(), "무기에 태그가 없다");
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ───────── T330 2회차 — ID 보조 패스 + 컴포짓 넷째 항 ─────────

        static EdgePartIdTag TagCube(Transform parent, Vector3 pos, Vector3 scale, Color c)
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
            return EdgePartId.Tag(r);
        }

        static Texture2D ReadLinear(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true);
            tex.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            tex.Apply(false);
            RenderTexture.active = prev;
            return tex;
        }

        /// <summary>x 열 띠(반폭 hw) · y0..y1 행에서 어두운 화소 수와 «어두운 화소가 하나라도 있는 행» 수.</summary>
        static void ColumnDark(Texture2D tex, int cx, int hw, int y0, int y1, out int dark, out int rows)
        {
            Color32[] px = tex.GetPixels32();
            dark = 0; rows = 0;
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(tex.height - 1, y1); y++)
            {
                bool any = false;
                for (int x = Mathf.Max(0, cx - hw); x <= Mathf.Min(tex.width - 1, cx + hw); x++)
                {
                    Color32 c = px[y * tex.width + x];
                    float l = (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) / 255f;
                    if (l < DarkMax) { dark++; any = true; }
                }
                if (any) rows++;
            }
        }

        static int RectDark(Texture2D tex, int x0, int x1, int y0, int y1)
        {
            Color32[] px = tex.GetPixels32();
            int n = 0;
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(tex.height - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(tex.width - 1, x1); x++)
                {
                    Color32 c = px[y * tex.width + x];
                    float l = (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) / 255f;
                    if (l < DarkMax) n++;
                }
            return n;
        }

        /// <summary>
        /// 정본 `renderIdPass` 의 결과 버퍼: 액터 파츠 화소는 rgb 에 제 번호 · a 에 선형깊이/idZFar 를, 배경은 0 을 남긴다.
        /// 🚨 r·g 는 «바이트를 실은 숫자» 라 sRGB 곡선을 한 번만 타도 이웃 번호가 한 칸으로 뭉친다 — 그래서 되읽은 번호가 **정확히** 같아야 한다.
        /// </summary>
        [UnityTest]
        public IEnumerator ID_보조_패스는_파츠_화소에_제_번호와_깊이를_배경엔_0을_남긴다()
        {
            if (NoGraphics()) { Assert.Ignore("그래픽 장치가 없다 — 픽셀은 CI 의 유니티 잡이 본다"); yield break; }
            Shader sh = EdgeIdPass.IdShader;
            Assert.IsNotNull(sh, "ID 셰이더(" + EdgeIdPass.IdShaderName + ")가 없다");
            Assert.IsTrue(sh.isSupported, "ID 셰이더가 컴파일에 실패했다 — 에디터 로그의 «Shader error in Forge/EdgePartId» 줄을 보라");
            GameObject rig = new GameObject("t330-id-rig");
            RenderTexture rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            Texture2D id = null;
            try
            {
                var hostGo = new GameObject("t330-host");
                hostGo.transform.SetParent(rig.transform, false);
                hostGo.AddComponent<EdgeOutlineHost>().Apply();
                EdgeOutlineSpec s = EdgeOutlineHost.Spec;

                Camera cam = new GameObject("t330-cam").AddComponent<Camera>();
                cam.transform.SetParent(rig.transform, false);
                cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f; cam.fieldOfView = 45f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.6f, 0.6f, 0.65f);
                cam.targetTexture = rt;

                // 팔↔몸통 자리: 같은 앞면(z = 8.5)·같은 법선의 상자 둘이 x = 0 에서 맞닿는다. 세로로 치우쳐 두어 위아래가 뒤집힌 버퍼도 걸린다.
                EdgePartIdTag a = TagCube(rig.transform, new Vector3(-1.5f, 2f, 10f), new Vector3(3f, 3f, 3f), new Color(0.85f, 0.80f, 0.55f));
                EdgePartIdTag b = TagCube(rig.transform, new Vector3(1.5f, 2f, 10f), new Vector3(3f, 3f, 3f), new Color(0.85f, 0.80f, 0.55f));
                Assert.AreNotEqual(a.Id, b.Id);
                yield return null;

                EdgeIdPass pass = EdgeIdPass.Attach(cam, true);
                pass.RenderNow();
                // 런 469: 앞 자들이 남긴 액터(갤러리·펫 시험의 복셀 몹)도 살아 있는 태그라 20 이 나왔다 — «내 둘이 들어갔는가» 는 아래 화소가 가른다.
                Assert.GreaterOrEqual(pass.LastDrawn, 2, "상자 둘 다 화면에서 6 CSS px 보다 크니 ID 패스에 들어가야 한다");
                Assert.AreSame(EdgeIdPass.IdMaterial, a.Twin.sharedMaterial, "쌍둥이는 ID 재질 하나를 나눠 쓴다(T44 공유 재질 상한) — 번호는 MPB 에");
                var blk = new MaterialPropertyBlock();
                a.Twin.GetPropertyBlock(blk);
                Assert.AreEqual(a.Id, EdgePartIdRules.Decode(blk.GetVector(EdgePartId.IdProp).x, blk.GetVector(EdgePartId.IdProp).y), "쌍둥이 MPB 의 번호");
                Assert.IsNotNull(pass.Target);
                Assert.AreEqual(rt.width, pass.Target.width, "ID 버퍼는 본 카메라와 같은 크기");
                Assert.IsFalse(pass.Target.sRGB, "ID 버퍼는 선형이어야 한다(sRGB 면 번호가 뭉친다)");
                id = ReadLinear(pass.Target);
                try { GallerySheet.Save(id, "screen_t330-idbuf"); } catch (System.Exception e) { Debug.LogWarning("[T330] 그림 저장 실패: " + e.Message); }

                Vector3 pa = cam.WorldToScreenPoint(new Vector3(-1.5f, 2f, 8.5f));
                Vector3 pb = cam.WorldToScreenPoint(new Vector3(1.5f, 2f, 8.5f));
                Color32[] raw = id.GetPixels32();   // 바이트 그대로(GetPixel 의 float 왕복을 피한다)
                Color32 ca = raw[(int)pa.y * id.width + (int)pa.x];
                Color32 cb = raw[(int)pb.y * id.width + (int)pb.x];
                Color32 bg = raw[2 * id.width + 2];
                Debug.Log("[T330] ID 버퍼 — A " + ca + " · B " + cb + " · 배경 " + bg + " · 그린 파츠 " + pass.LastDrawn);
                Assert.AreEqual(a.Id, EdgePartIdRules.Decode(ca.r / 255.0, ca.g / 255.0), "상자 A 화소의 번호");
                Assert.AreEqual(b.Id, EdgePartIdRules.Decode(cb.r / 255.0, cb.g / 255.0), "상자 B 화소의 번호");
                Assert.That(ca.a / 255.0 * s.IdZFar, Is.EqualTo(8.5).Within(s.IdTolZ), "a = 선형깊이/idZFar — 앞면 8.5 유닛(허용오차 = 컴포짓의 id_tol_z)");
                Assert.That(cb.a / 255.0 * s.IdZFar, Is.EqualTo(8.5).Within(s.IdTolZ));
                Assert.AreEqual(0, bg.r + bg.g + bg.a, "배경은 키 0 · 깊이 0");
                Assert.IsFalse(a.Twin.enabled, "쌍둥이는 보조 카메라가 그린 뒤 꺼져 있어야 한다 — 다른 카메라가 보면 안 된다");
                Assert.AreEqual(EdgeIdPass.IdLayer, a.Twin.gameObject.layer);
            }
            finally
            {
                if (id != null) Object.DestroyImmediate(id);
                Object.DestroyImmediate(rig);
                rt.Release(); Object.DestroyImmediate(rt);
                Shader.SetGlobalFloat(EdgeIdPass.IdOnProp, 0f);
            }
        }

        /// <summary>
        /// 넷째 항의 판정(ROUTINE T330): 깊이·법선이 같은 파츠 경계(팔↔몸통)에 **on 프레임에만** 검정 줄이 서고 · ①②③ 만으로는 그 줄이 0 이며 ·
        /// 언덕 뒤 액터(가려진 파츠)는 가리개 위에 유령선을 안 그린다(`idkey` 의 가시성 step).
        /// </summary>
        [UnityTest]
        public IEnumerator 같은_평면_파츠_경계에_ID_항만이_줄을_긋고_가려진_파츠는_유령선이_없다()
        {
            if (NoGraphics()) { Assert.Ignore("그래픽 장치가 없다 — 픽셀은 CI 의 유니티 잡이 본다"); yield break; }
            object feature = Feature();
            if (feature == null) { Assert.Ignore("렌더러에서 EdgeOutline 기능을 못 찾았다(파이프라인 꼴이 바뀌었다)"); yield break; }
            Assert.IsTrue(Shader.Find(EdgeOutlineHost.ShaderName).isSupported, "엣지 셰이더가 컴파일에 실패했다");
            Assert.IsTrue(EdgeIdPass.IdShader != null && EdgeIdPass.IdShader.isSupported, "ID 셰이더가 없거나 컴파일에 실패했다");

            GameObject rig = new GameObject("t330-rig");
            RenderTexture rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevActive = RenderTexture.active;
            Texture2D on = null, noId = null, off = null;
            object savedActive = GetHidden(feature, "m_Active");
            try
            {
                var hostGo = new GameObject("t330-host");
                hostGo.transform.SetParent(rig.transform, false);
                hostGo.AddComponent<EdgeOutlineHost>().Apply();

                Camera cam = new GameObject("t330-cam").AddComponent<Camera>();
                cam.transform.SetParent(rig.transform, false);
                cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f; cam.fieldOfView = 45f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.6f, 0.6f, 0.65f);
                cam.targetTexture = rt;

                // 뒷벽(태그 없음 = 배경 키) · 위쪽: 맞닿은 상자 둘(같은 앞면 z 8.5) · 아래쪽: 가리개(태그 없음 · z 5.75 앞면) 뒤에 숨은 태그 상자
                Cube(rig.transform, new Vector3(0f, 0f, 20f), new Vector3(40f, 40f, 0.2f), new Color(0.72f, 0.74f, 0.78f));
                EdgePartIdTag a = TagCube(rig.transform, new Vector3(-1.5f, 2f, 10f), new Vector3(3f, 3f, 3f), new Color(0.85f, 0.80f, 0.55f));
                EdgePartIdTag b = TagCube(rig.transform, new Vector3(1.5f, 2f, 10f), new Vector3(3f, 3f, 3f), new Color(0.85f, 0.80f, 0.55f));
                Cube(rig.transform, new Vector3(0f, -1.6f, 6f), new Vector3(3f, 2.2f, 0.5f), new Color(0.80f, 0.86f, 0.80f));
                EdgePartIdTag hidden = TagCube(rig.transform, new Vector3(0f, -1.6f, 10f), new Vector3(2f, 1.2f, 2f), new Color(0.9f, 0.5f, 0.5f));
                Assert.IsNotNull(hidden);
                yield return null;

                EdgeIdPass pass = EdgeIdPass.Attach(cam, true);
                SetActive(feature, true);
                EdgeOutlineHost.SetOn(true);
                yield return null;
                pass.RenderNow();
                on = Shoot(cam, rt);
                // ①②③ 만: ID 패스를 끄고(전역 _EdgeIdOn 0) 같은 장면
                pass.enabled = false;
                noId = Shoot(cam, rt);
                // off 프레임(네 항 다 끔)
                EdgeOutlineHost.SetOn(false);
                yield return null;
                off = Shoot(cam, rt);
                SetActive(feature, false);
                try { GallerySheet.Save(on, "screen_t330-id-on"); GallerySheet.Save(noId, "screen_t330-id-noid"); GallerySheet.Save(off, "screen_t330-id-off"); }
                catch (System.Exception e) { Debug.LogWarning("[T330] 그림 저장 실패(단언은 계속): " + e.Message); }

                // 이음매 띠: x = 두 상자의 경계(0, ·, 8.5) 화면 열 ±2px · y 는 상자 위아래 실루엣을 피해 안쪽 0.4 유닛씩 뺀 구간
                Vector3 seamLo = cam.WorldToScreenPoint(new Vector3(0f, 0.9f, 8.5f));
                Vector3 seamHi = cam.WorldToScreenPoint(new Vector3(0f, 3.1f, 8.5f));
                int cx = Mathf.RoundToInt(seamLo.x), y0 = Mathf.RoundToInt(Mathf.Min(seamLo.y, seamHi.y)), y1 = Mathf.RoundToInt(Mathf.Max(seamLo.y, seamHi.y));
                int band = y1 - y0 + 1;
                int darkOn, rowsOn, darkNo, rowsNo, darkOff, rowsOff;
                ColumnDark(on, cx, 2, y0, y1, out darkOn, out rowsOn);
                ColumnDark(noId, cx, 2, y0, y1, out darkNo, out rowsNo);
                ColumnDark(off, cx, 2, y0, y1, out darkOff, out rowsOff);
                // 상자 A 안쪽(경계에서 떨어진 열) — 선이 없어야 한다
                Vector3 inA = cam.WorldToScreenPoint(new Vector3(-1.5f, 2f, 8.5f));
                int darkInA, rowsInA;
                ColumnDark(on, Mathf.RoundToInt(inA.x), 2, y0, y1, out darkInA, out rowsInA);
                // 가리개 안쪽(실루엣 안 25% 안쪽) — 숨은 파츠의 유령선 0
                Vector3 oc0 = cam.WorldToScreenPoint(new Vector3(-1.1f, -2.4f, 5.75f));
                Vector3 oc1 = cam.WorldToScreenPoint(new Vector3(1.1f, -0.8f, 5.75f));
                int ghost = RectDark(on, Mathf.RoundToInt(Mathf.Min(oc0.x, oc1.x)), Mathf.RoundToInt(Mathf.Max(oc0.x, oc1.x)),
                                         Mathf.RoundToInt(Mathf.Min(oc0.y, oc1.y)), Mathf.RoundToInt(Mathf.Max(oc0.y, oc1.y)));
                Note("t330-idline.txt",
                     "T330 넷째 항 실측 — 이음매 띠 " + band + "행 (x " + cx + " ±2)\n" +
                     "on: 검정 " + darkOn + " · 줄 있는 행 " + rowsOn + "\n①②③만: 검정 " + darkNo + " · 행 " + rowsNo + "\noff: 검정 " + darkOff + "\n" +
                     "상자 A 안쪽 열 검정 " + darkInA + " · 가리개 안쪽 유령 검정 " + ghost + " · ID 패스 파츠 " + pass.LastDrawn + "\n");
                Debug.Log("[T330] 이음매 on " + darkOn + "/" + band + "행 " + rowsOn + " · ①②③만 " + darkNo + " · off " + darkOff + " · A 안쪽 " + darkInA + " · 유령 " + ghost);

                Assert.Greater(band, 20, "이음매 띠가 너무 짧다 — 장면 세우기가 틀렸다");
                Assert.AreEqual(0, darkOff, "off 프레임에 검정이 남았다 — 네 항이 다 꺼지지 않았다");
                Assert.LessOrEqual(darkNo, band / 10, "①②③ 만으로 이음매에 줄이 섰다(" + darkNo + ") — 이 장면은 깊이·법선이 같은 경계라 ID 항만 잡아야 한다(장면이 틀렸거나 다른 항이 샌다)");
                Assert.GreaterOrEqual(rowsOn, band * 8 / 10, "on 프레임 이음매에 줄이 안 선다(줄 있는 행 " + rowsOn + "/" + band + ") — ID 버퍼가 컴포짓에 안 물렸거나 키가 같다(sRGB·uv 뒤집힘·_EdgeIdOn 확인 · t330-idline.txt)");
                Assert.LessOrEqual(darkOn, band * 3, "이음매가 3px 보다 두껍다(" + darkOn + "/" + band + "행) — 팽창 off 면 키 큰 쪽 1px 이어야 한다");
                Assert.LessOrEqual(darkInA, band / 10, "상자 안쪽 열에 줄이 있다(" + darkInA + ") — 같은 파츠 안에서 키가 흔들린다(버퍼 정밀도)");
                Assert.AreEqual(0, ghost, "가리개 위에 유령선 " + ghost + " — 숨은 파츠의 ID 가 깊이 검증(id_tol_z)을 통과했다");
            }
            finally
            {
                SetActive(feature, savedActive is bool && (bool)savedActive);
                EdgeOutlineHost.SetOn(true);
                RenderTexture.active = prevActive;
                if (on != null) Object.DestroyImmediate(on);
                if (noId != null) Object.DestroyImmediate(noId);
                if (off != null) Object.DestroyImmediate(off);
                Object.DestroyImmediate(rig);
                rt.Release(); Object.DestroyImmediate(rt);
                Shader.SetGlobalFloat(EdgeIdPass.IdOnProp, 0f);
            }
        }
        /// <summary>
        /// T350 — ID 보조 패스의 **프레임당 비용**은 «한 번만» 이어야 한다(§1 «`Update` 에서 `GetComponent` 금지 · 프레임당 GC 할당 0»).
        /// 화소가 아니라 **상태**를 본다: ⓐ 메시 필터는 처음 한 번만 찾고 «없더라» 도 기억한다(메시 필터 없는 파츠가 프레임마다 다시 파이던 자리) ·
        /// ⓑ 같은 메시로 다시 부르면 쌍둥이의 재질 배열을 **새로 만들지 않는다** ⓒ 메시가 바뀐 프레임에는 따라간다.
        /// </summary>
        [Test]
        public void ID_보조_패스는_파츠_참조를_한_번만_찾고_메시가_바뀔_때만_다시_세운다()
        {
            GameObject rig = new GameObject("t350-rig");
            try
            {
                // ⓐ 메시 필터가 **없는** 렌더러 — 1회차의 «TargetFilter == null 이면 찾는다» 는 이런 파츠를 프레임마다 다시 팠다.
                var bare = new GameObject("t350-no-filter");
                bare.transform.SetParent(rig.transform, false);
                var bareR = bare.AddComponent<MeshRenderer>();
                EdgePartIdTag bareTag = EdgePartId.Tag(bareR);
                Assert.IsFalse(bareTag.TargetFilterLooked, "아직 찾기 전이다");
                Assert.IsNull(EdgePartId.TargetMesh(bareTag, bareR), "메시 필터가 없으니 메시도 없다");
                Assert.IsTrue(bareTag.TargetFilterLooked, "«없더라» 를 기억해야 다음 프레임에 다시 안 판다");
                Assert.IsNull(bareTag.TargetFilter);

                // ⓑ 메시가 있는 파츠: 두 번째 호출은 쌍둥이를 다시 세우지 않는다.
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(rig.transform, false);
                var r = go.GetComponent<MeshRenderer>();
                EdgePartIdTag t = EdgePartId.Tag(r);
                EdgeIdPass.EnsureTwin(t);
                Assert.IsNotNull(t.Twin, "쌍둥이가 섰다");
                Assert.IsNotNull(t.TwinFilter, "쌍둥이의 메시 필터를 쥐고 있다");
                Mesh mesh0 = t.TwinMesh;
                Assert.AreSame(go.GetComponent<MeshFilter>().sharedMesh, mesh0, "쌍둥이는 대상의 메시를 쓴다");
                Material[] mats0 = t.Twin.sharedMaterials;
                MeshRenderer twin0 = t.Twin;
                EdgeIdPass.EnsureTwin(t);
                EdgeIdPass.EnsureTwin(t);
                Assert.AreSame(twin0, t.Twin, "같은 메시면 쌍둥이를 다시 만들지 않는다");
                Assert.AreSame(mesh0, t.TwinMesh, "기억한 메시가 그대로다");
                Assert.AreEqual(mats0.Length, t.Twin.sharedMaterials.Length);
                Assert.AreSame(EdgeIdPass.IdMaterial, t.Twin.sharedMaterial, "ID 재질 하나를 나눠 쓴다");

                // ⓒ 메시를 갈면 그 프레임엔 따라간다.
                GameObject donor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                donor.transform.SetParent(rig.transform, false);   // rig 와 함께 걷힌다
                Mesh other = donor.GetComponent<MeshFilter>().sharedMesh;
                go.GetComponent<MeshFilter>().sharedMesh = other;
                EdgeIdPass.EnsureTwin(t);
                Assert.AreSame(other, t.TwinMesh, "메시가 바뀐 프레임엔 쌍둥이도 바뀐다");
                Assert.AreSame(other, t.TwinFilter.sharedMesh);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }
    }
}
