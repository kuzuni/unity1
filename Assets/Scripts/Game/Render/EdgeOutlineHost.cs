using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Forge.Core.Data;
using Forge.Core.Render;

namespace Forge.Game.Render
{
    /// <summary>
    /// T147 2회차 — 캐릭터 윤곽선(후처리 깊이-엣지)의 **배선**. 그리는 것은 URP 렌더러에 등록한
    /// `FullScreenPassRendererFeature`(`Assets/Settings/UniversalRenderer.asset` · 머티리얼 `Forge/EdgeOutline`)가 하고,
    /// 여기서는 표(`Resources/EdgeOutlineUi.json`)의 수치를 **전역 셰이더 유니폼**으로 실어 준다.
    ///
    /// 머티리얼 에셋에 수치를 굽지 않는 이유: 그러면 표와 에셋 둘이 같은 수를 쥐어 한쪽만 고쳐지는 날이 온다(§1 «수치는 코드·에셋에 박지 않는다»).
    /// 전역이면 표 하나가 단독으로 쥔다. 두께는 화면 폭에 딸린 값이라(정본 devicePixelRatio 자리) 해상도가 바뀌면 다시 싣는다.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class EdgeOutlineHost : MonoBehaviour
    {
        public const string ResourcePath = "EdgeOutlineUi";
        /// <summary>셰이더 이름(`Assets/Shaders/EdgeOutline.shader`) — 머티리얼 에셋이 GUID 로 물고 있어 빌드에도 실린다.</summary>
        public const string ShaderName = "Forge/EdgeOutline";

        // 전역 유니폼 이름 — 셰이더의 선언과 같아야 한다(한 자리에 모아 둔다).
        public const string OnProp = "_EdgeOn";
        public const string EdgeKProp = "_EdgeK";
        public const string NormalKProp = "_EdgeNormalK";
        public const string CreaseKProp = "_EdgeCreaseK";
        public const string CreaseHystProp = "_EdgeCreaseHyst";
        public const string MaxZProp = "_EdgeMaxZ";
        public const string DilateProp = "_EdgeDilate";
        public const string DiagFProp = "_EdgeDiagF";
        public const string R2FProp = "_EdgeR2F";
        public const string ColorProp = "_EdgeLineColor";

        static EdgeOutlineSpec spec;
        public static EdgeOutlineSpec Spec
        {
            get
            {
                if (spec == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T147 캐릭터 윤곽선)");
                    spec = EdgeOutlineSpec.From(MiniJson.ParseObject(ta.text));
                }
                return spec;
            }
        }

        public static EdgeOutlineHost Instance { get; private set; }

        int lastW = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != null) return;
            foreach (Bootstrap b in Resources.FindObjectsOfTypeAll<Bootstrap>())
            {
                if (!b.gameObject.scene.isLoaded) continue;
                Create(b.transform);
                return;
            }
        }

        public static EdgeOutlineHost Create(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("EdgeOutline");
            go.transform.SetParent(parent, false);
            return go.AddComponent<EdgeOutlineHost>();
        }

        private void Awake() { Instance = this; Apply(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Update() { if (Screen.width != lastW) Apply(); }

        /// <summary>표 → 전역 유니폼. 두께 스위치는 그때의 화면 폭으로 다시 잰다.</summary>
        public void Apply()
        {
            lastW = Screen.width;
            EdgeOutlineSpec s = Spec;
            Shader.SetGlobalFloat(EdgeKProp, (float)s.EdgeK);
            Shader.SetGlobalFloat(NormalKProp, (float)s.NormalK);
            Shader.SetGlobalFloat(CreaseKProp, (float)s.CreaseK);
            Shader.SetGlobalFloat(CreaseHystProp, (float)s.CreaseHystF);
            Shader.SetGlobalFloat(MaxZProp, (float)s.EdgeMaxZ);
            Shader.SetGlobalFloat(DiagFProp, (float)s.DiagF);
            Shader.SetGlobalFloat(R2FProp, (float)s.R2F);
            Shader.SetGlobalColor(ColorProp, LineColor(s));
            Shader.SetGlobalFloat(DilateProp, EdgeOutlineRules.DilateOn(s, EdgeOutlineRules.BufScale(s, lastW)) ? 1f : 0f);
            Shader.SetGlobalFloat(OnProp, On ? 1f : 0f);
        }

        /// <summary>표의 선 색(정본 검정).</summary>
        public static Color LineColor(EdgeOutlineSpec s)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(s.LineHex, out c)) throw new FormatException("EdgeOutlineUi 선 색이 #RRGGBB 가 아니다: " + s.LineHex);
            return c;
        }

        /// <summary>
        /// 판정기의 on/off 프레임. 🚨 **네 항을 한꺼번에** 끈다(정본 규칙 · Core `EdgeOutlineTerms.Off` 와 같은 자리) —
        /// 항을 하나만 끄는 길은 열지 않는다. 하나라도 남으면 그 선이 on/off 양쪽에 똑같이 찍혀 차분 마스크에서 통째로 지워진다.
        /// </summary>
        public static bool On { get; private set; } = true;

        public static void SetOn(bool on)
        {
            On = on;
            Shader.SetGlobalFloat(OnProp, on ? 1f : 0f);
        }
    }
}
