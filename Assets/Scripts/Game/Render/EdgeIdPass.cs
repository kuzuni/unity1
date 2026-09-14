using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Forge.Core.Data;
using Forge.Core.Render;

namespace Forge.Game.Render
{
    /// <summary>
    /// T330 2회차 — 파츠 ID **보조 패스**(정본 `scene3d.js` 892~1000 `renderIdPass`). 액터 파츠만 `_rtId` 에 굽는다:
    /// rgb = 16bit 파츠 번호 · a = 선형깊이/idZFar. 컴포짓(`EdgeOutline.shader`)이 `_EdgeIdTex` 로 읽어 넷째 항을 긋는다.
    ///
    /// 길: 제 `ScriptableRendererFeature`(RenderGraph)가 아니라 **보조 카메라**다(ROUTINE T330 ⓒ 의 둘째 길). 하니스가 URP 를 스텁으로
    /// 물어 RenderGraph 서명이 하나만 어긋나도 CI 에서만 터지는데(결정 326), 카메라·렌더러·재질은 전부 UnityEngine 실물 참조라 그 위험이 없다.
    /// 정본의 «재질 바꿔 끼우기 + ID_LAYER» 는 유니티에선 **쌍둥이 렌더러**로 옮겼다: 파츠마다 같은 메시를 문 자식 렌더러 하나(ID 재질 인스턴스 ·
    /// `id_layer`)를 두고, 보조 카메라가 그리는 **그 순간에만** 켠다(`beginCameraRendering`/`endCameraRendering`) — 다른 어떤 카메라(본 카메라 ·
    /// 갤러리 · 썸네일 · 픽셀 자)도 쌍둥이를 볼 수 없어 마스크를 손댈 일이 없다. 정본이 «ID 패스에 넣는가»(불투명 · 투영 짧은 변 ≥ 6px)를
    /// 매 프레임 다시 묻듯 여기서도 `EdgePartId.UseId` 를 그때 묻는다.
    /// 보조 카메라는 본 카메라의 자식(같은 자리·같은 렌즈)이고 본 카메라보다 먼저 그린다(depth − 1). 판정 자는 <see cref="RenderNow"/> 로 손수 돌린다.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class EdgeIdPass : MonoBehaviour
    {
        public const string IdTexProp = "_EdgeIdTex";
        public const string IdOnProp = "_EdgeIdOn";
        public const string IdShaderName = "Forge/EdgePartId";
        public const string TwinName = "edge-id";

        /// <summary>이 패스가 섬기는 본 카메라 — 컴포짓이 ID 버퍼를 읽는 카메라.</summary>
        public Camera Host { get; private set; }
        /// <summary>ID 를 굽는 보조 카메라(본 카메라의 자식).</summary>
        public Camera Aux { get; private set; }
        public RenderTexture Target { get; private set; }
        /// <summary>마지막 보조 렌더에서 ID 패스에 넣은 파츠 수(진단·자).</summary>
        public int LastDrawn { get; private set; }
        /// <summary>true 면 보조 카메라가 스스로 안 돌고 <see cref="RenderNow"/> 만 그린다(판정 자).</summary>
        public bool Manual;

        static Shader idShader;
        static int idLayer = -1;

        /// <summary>표의 `layers.id_layer`(정본 ID_LAYER 7).</summary>
        public static int IdLayer
        {
            get
            {
                if (idLayer < 0)
                {
                    TextAsset ta = Resources.Load<TextAsset>(EdgeOutlineHost.ResourcePath);
                    if (ta == null) throw new System.InvalidOperationException("Resources/" + EdgeOutlineHost.ResourcePath + ".json 이 없다 (T330 파츠 ID)");
                    var root = MiniJson.ParseObject(ta.text);
                    idLayer = J.Int(J.Require(J.Obj(J.Require(root, "layers")), "id_layer"));
                    if (idLayer < 0 || idLayer > 31) throw new System.InvalidOperationException("id_layer 는 0~31 이어야 한다: " + idLayer);
                }
                return idLayer;
            }
        }

        public static Shader IdShader
        {
            get
            {
                if (idShader == null) idShader = Shader.Find(IdShaderName);
                return idShader;
            }
        }

        /// <summary>본 카메라에 패스를 단다(이미 있으면 그것).</summary>
        public static EdgeIdPass Attach(Camera host, bool manual = false)
        {
            if (host == null) return null;
            EdgeIdPass p = host.GetComponent<EdgeIdPass>();
            if (p == null) { p = host.gameObject.AddComponent<EdgeIdPass>(); }
            p.Manual = manual;
            p.Host = host;
            p.EnsureAux();
            return p;
        }

        void EnsureAux()
        {
            if (Aux != null) return;
            var go = new GameObject("EdgeIdAux");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            Aux = go.AddComponent<Camera>();
            var urp = go.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null) urp = go.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = false;
            urp.renderShadows = false;
            urp.requiresDepthOption = CameraOverrideOption.Off;
            urp.requiresColorOption = CameraOverrideOption.Off;
            urp.antialiasing = AntialiasingMode.None;
            SyncAux();
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBegin;
            RenderPipelineManager.endCameraRendering += OnEnd;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBegin;
            RenderPipelineManager.endCameraRendering -= OnEnd;
            Shader.SetGlobalFloat(IdOnProp, 0f);
            SetTwins(false);
        }

        void OnDestroy()
        {
            if (Target != null) { Target.Release(); Object.Destroy(Target); Target = null; }
            if (Aux != null) Object.Destroy(Aux.gameObject);
        }

        void LateUpdate() { Sync(); }

        /// <summary>보조 카메라·RT·쌍둥이를 이 프레임의 본 카메라에 맞춘다(자동이면 LateUpdate 가 매 프레임 부른다).</summary>
        public void Sync()
        {
            if (Host == null) Host = GetComponent<Camera>();
            if (Host == null) return;
            EnsureAux();
            SyncAux();
            var live = EdgePartId.Live;
            for (int i = 0; i < live.Count; i++) EnsureTwin(live[i]);
        }

        void SyncAux()
        {
            if (Host == null || Aux == null) return;
            int w = Mathf.Max(1, Host.pixelWidth), h = Mathf.Max(1, Host.pixelHeight);
            if (Target == null || Target.width != w || Target.height != h)
            {
                if (Target != null) { Target.Release(); Object.Destroy(Target); }
                // 🚨 선형(sRGB 아님) 이어야 한다 — r·g 는 바이트를 실은 숫자라 sRGB 곡선을 한 번만 타도 이웃 번호가 한 칸으로 뭉친다.
                Target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Target.name = "EdgeIdRT";
                Target.filterMode = FilterMode.Point;
                Target.wrapMode = TextureWrapMode.Clamp;
                Target.Create();
            }
            Aux.CopyFrom(Host);
            Aux.enabled = !Manual;
            Aux.cullingMask = 1 << IdLayer;
            Aux.clearFlags = CameraClearFlags.SolidColor;
            Aux.backgroundColor = new Color(0f, 0f, 0f, 0f);   // 키 0 = 배경(정본 setClearColor(0, 0.0))
            Aux.targetTexture = Target;
            Aux.depth = Host.depth - 1f;
            Aux.allowHDR = false;
            Aux.allowMSAA = false;
            Aux.allowDynamicResolution = false;
            Aux.useOcclusionCulling = false;
            Aux.rect = new Rect(0f, 0f, 1f, 1f);
        }

        /// <summary>파츠의 쌍둥이 렌더러(같은 메시 · ID 재질 인스턴스 · id_layer · 꺼진 채)를 세우거나 메시 교체를 따라간다.</summary>
        public static void EnsureTwin(EdgePartIdTag t)
        {
            if (t == null || t.Target == null) return;
            var mf = t.Target.GetComponent<MeshFilter>();
            Mesh mesh = mf != null ? mf.sharedMesh : null;
            if (t.Twin == null)
            {
                var go = new GameObject(TwinName);
                go.transform.SetParent(t.Target.transform, false);
                go.layer = IdLayer;
                go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                mr.enabled = false;
                t.TwinMaterial = new Material(IdShader);
                t.TwinMaterial.name = "EdgePartId " + t.Id;
                t.TwinMaterial.SetVector(EdgePartId.IdProp, EdgePartId.Uniform(t.Id));
                t.Twin = mr;
            }
            var tf = t.Twin.GetComponent<MeshFilter>();
            if (tf.sharedMesh != mesh) tf.sharedMesh = mesh;
            int subs = mesh != null ? mesh.subMeshCount : 1;
            Material[] mats = t.Twin.sharedMaterials;
            if (mats == null || mats.Length != subs || (subs > 0 && mats[0] != t.TwinMaterial))
            {
                mats = new Material[subs];
                for (int i = 0; i < subs; i++) mats[i] = t.TwinMaterial;
                t.Twin.sharedMaterials = mats;
            }
        }

        void OnBegin(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam == Aux)
            {
                Shader.SetGlobalFloat(EdgeOutlineHost.IdZFarProp, (float)EdgeOutlineHost.Spec.IdZFar);
                LastDrawn = Tag();
            }
            else if (cam == Host)
            {
                Shader.SetGlobalTexture(IdTexProp, Target);
                Shader.SetGlobalFloat(IdOnProp, Target != null && EdgeOutlineHost.On ? 1f : 0f);
            }
            else
            {
                // 다른 카메라(갤러리·썸네일·자)에는 이 카메라의 ID 버퍼가 맞지 않는다 — 항을 끈다(Core EdgeIdTaps.Has = false).
                Shader.SetGlobalFloat(IdOnProp, 0f);
            }
        }

        void OnEnd(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam == Aux) SetTwins(false);
        }

        /// <summary>정본 `tag` 클로저 — 이 프레임 ID 패스에 넣을 파츠의 쌍둥이만 켠다. 돌려주는 값 = 켠 수.</summary>
        int Tag()
        {
            EdgeOutlineSpec s = EdgeOutlineHost.Spec;
            double cssH = Aux.pixelHeight / EdgeOutlineRules.BufScale(s, Mathf.Max(1, Aux.pixelWidth));
            var live = EdgePartId.Live;
            int n = 0;
            for (int i = 0; i < live.Count; i++)
            {
                EdgePartIdTag t = live[i];
                if (t == null || t.Twin == null) continue;
                bool use = EdgePartId.UseId(t, Aux, s, cssH);
                t.Twin.enabled = use;
                if (use) n++;
            }
            return n;
        }

        static void SetTwins(bool on)
        {
            var live = EdgePartId.Live;
            for (int i = 0; i < live.Count; i++) { EdgePartIdTag t = live[i]; if (t != null && t.Twin != null) t.Twin.enabled = on; }
        }

        /// <summary>판정 자용 — 지금 상태로 보조 카메라를 한 번 그린다(쌍둥이 세우기 → ID 렌더 → 쌍둥이 끄기).</summary>
        public void RenderNow()
        {
            Sync();
            Aux.Render();
        }
    }
}
