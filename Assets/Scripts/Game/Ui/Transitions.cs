using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>T454 — 표 `Resources/TransitionUi.json`(정본 `transition` 자리) 로더. 값은 전부 표 · 셈은 Core <see cref="TransitionRules"/>.</summary>
    public static class TransitionUi
    {
        public const string ResourcePath = "TransitionUi";
        static TransitionTable table;

        public static TransitionTable Table
        {
            get
            {
                if (table == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T454)");
                    table = TransitionTable.From(MiniJson.ParseObject(ta.text));
                }
                return table;
            }
        }
        public static void Reset() { table = null; }
    }

    /// <summary>
    /// T454 ⓐ — 토스트 등장(정본 `style.css` 1936 `.toast { opacity: 0; transform: translateY(-.5rem); transition: all .25s }` · 1938 `.toast.show { opacity: 1; transform: none }`).
    /// 상자에 붙어 켜지는 순간 위 .5rem 에서 내려오며 불투명해진다 — 정본 `opacity` 는 상자 한 겹 전체라 CanvasGroup 한 장이 같은 뜻이다(`PopupLayer` 의 딤과 같은 길).
    /// 정지 촬영·자는 <see cref="SettleAll"/> 로 끝난 모습을 본다(<see cref="PanelSlide.SettleAll"/> 과 같은 길).
    /// </summary>
    public sealed class ToastEnter : MonoBehaviour
    {
        static readonly List<ToastEnter> live = new List<ToastEnter>();

        RectTransform rt;
        CanvasGroup cg;
        TransitionSpec spec;
        Vector2 basePos;
        double elapsedMs;
        bool entering;

        /// <summary>아직 들어오는 중인가.</summary>
        public bool Entering { get { return entering; } }
        public double ElapsedMs { get { return elapsedMs; } }
        /// <summary>끝났을 때의 자리(= 정본 `transform: none`).</summary>
        public Vector2 BasePos { get { return basePos; } }
        /// <summary>지금 불투명도(CanvasGroup).</summary>
        public float Alpha { get { return cg != null ? cg.alpha : 1f; } }
        public TransitionSpec Spec { get { return spec; } }

        /// <summary>상자에 붙여 지금 자리를 «끝 자리» 로 삼고 첫 프레임을 시작 상태(α 0 · 위 .5rem)로 놓는다. 자리를 잡은 뒤에 부른다.</summary>
        public static ToastEnter Attach(RectTransform box, string key)
        {
            if (box == null) throw new ArgumentNullException("box");
            ToastEnter t = box.GetComponent<ToastEnter>();
            if (t == null) t = box.gameObject.AddComponent<ToastEnter>();
            t.rt = box;
            t.cg = box.GetComponent<CanvasGroup>();
            if (t.cg == null) t.cg = box.gameObject.AddComponent<CanvasGroup>();
            t.spec = TransitionUi.Table.Get(key);
            t.basePos = box.anchoredPosition;
            t.elapsedMs = 0;
            t.entering = true;
            if (!live.Contains(t)) live.Add(t);
            t.Apply();
            return t;
        }

        void Update()
        {
            if (!entering) return;
            elapsedMs += Time.unscaledDeltaTime * 1000.0;
            Apply();
            if (TransitionRules.Done(spec, elapsedMs)) Settle();
        }

        void Apply()
        {
            if (!entering || rt == null) return;
            double p = TransitionRules.Progress(spec, elapsedMs);
            cg.alpha = (float)TransitionRules.Lerp(0, 1, p);
            // CSS translateY 는 아래가 + · 캔버스 y 는 위가 + → 부호를 뒤집는다(시작 −.5rem = 위 .5rem).
            rt.anchoredPosition = basePos + new Vector2(0f, -(float)TransitionRules.DyRem(spec, p) * PopupKit.Rem);
        }

        /// <summary>지금 당장 끝 상태로(정지 촬영·자).</summary>
        public void Settle()
        {
            if (!entering) return;
            entering = false;
            live.Remove(this);
            if (cg != null) cg.alpha = 1f;
            if (rt != null) rt.anchoredPosition = basePos;
        }

        void OnDestroy() { live.Remove(this); }

        /// <summary>들어오는 중인 토스트 전부를 끝 상태로 — 촬영 직전(`UiShotsTests`)에 부른다.</summary>
        public static void SettleAll()
        {
            for (int i = live.Count - 1; i >= 0; i--) if (live[i] != null) live[i].Settle();
        }
    }

    /// <summary>
    /// T454 ⓑ — 토글 손잡이 미끄러짐(정본 3113 `.settings-toggle::after { transition: left .15s }` · 4992 `.af-toggle .knob { transition: left .15s ease-out }`).
    /// 클론의 토글은 누르면 화면을 **다시 세우는** 자리라 전이가 설 자리가 없었다 — 그래서 «누른 순간의 상태» 를 열쇠(부모 이름/토글 이름)로 적어 두고(<see cref="Expect"/>),
    /// 다시 세워진 토글이 그 열쇠를 찾으면(<see cref="Take"/>) 손잡이를 앞 닻에서 새 닻으로 표의 ms·ease 로 옮긴다(«앞 상태 → 새 상태» 를 한 프레임 안에 잇는다).
    /// 눌리지 않고 세워진 토글(첫 열기 · 자)은 적어 둔 것이 없어 한 화소도 안 움직인다.
    /// </summary>
    public sealed class ToggleSlide : MonoBehaviour
    {
        static readonly Dictionary<string, bool> pressed = new Dictionary<string, bool>();
        static readonly List<ToggleSlide> live = new List<ToggleSlide>();

        RectTransform rt;
        TransitionSpec spec;
        float fromAnchorX, toAnchorX, fromOffX, toOffX;
        double elapsedMs;
        bool sliding;

        public bool Sliding { get { return sliding; } }
        public double ElapsedMs { get { return elapsedMs; } }
        public float FromAnchorX { get { return fromAnchorX; } }
        public float ToAnchorX { get { return toAnchorX; } }
        public TransitionSpec Spec { get { return spec; } }

        /// <summary>토글이 눌렸다 — 그 순간의 상태를 열쇠로 적어 둔다(다시 세워질 때 <see cref="Take"/> 가 가져간다).</summary>
        public static void Expect(string key, bool wasOn) { pressed[key] = wasOn; }

        /// <summary>적어 둔 «누르기 전 상태» 를 가져가며 지운다 — 없으면 null(전이 없음).</summary>
        public static bool? Take(string key)
        {
            bool v;
            if (!pressed.TryGetValue(key, out v)) return null;
            pressed.Remove(key);
            return v;
        }

        /// <summary>적어 둔 것을 전부 잊는다(자).</summary>
        public static void Forget() { pressed.Clear(); }

        /// <summary>손잡이에 붙인다 — 손잡이는 이미 **새** 닻·오프셋에 놓여 있고, 첫 프레임을 **앞** 닻·오프셋으로 되돌려 거기서 출발한다.</summary>
        public static ToggleSlide Begin(RectTransform knob, string key, float fromAnchorX, float fromOffX)
        {
            if (knob == null) throw new ArgumentNullException("knob");
            ToggleSlide s = knob.GetComponent<ToggleSlide>();
            if (s == null) s = knob.gameObject.AddComponent<ToggleSlide>();
            s.rt = knob;
            s.spec = TransitionUi.Table.Get(key);
            s.toAnchorX = knob.anchorMin.x;
            s.toOffX = knob.anchoredPosition.x;
            s.fromAnchorX = fromAnchorX;
            s.fromOffX = fromOffX;
            s.elapsedMs = 0;
            s.sliding = true;
            if (!live.Contains(s)) live.Add(s);
            s.Apply();
            return s;
        }

        void Update()
        {
            if (!sliding) return;
            elapsedMs += Time.unscaledDeltaTime * 1000.0;
            Apply();
            if (TransitionRules.Done(spec, elapsedMs)) Settle();
        }

        void Apply()
        {
            if (!sliding || rt == null) return;
            double p = TransitionRules.Progress(spec, elapsedMs);
            float ax = (float)TransitionRules.Lerp(fromAnchorX, toAnchorX, p);
            rt.anchorMin = new Vector2(ax, rt.anchorMin.y);
            rt.anchorMax = new Vector2(ax, rt.anchorMax.y);
            rt.anchoredPosition = new Vector2((float)TransitionRules.Lerp(fromOffX, toOffX, p), rt.anchoredPosition.y);
        }

        /// <summary>지금 당장 새 닻으로(정지 촬영·자).</summary>
        public void Settle()
        {
            if (!sliding) return;
            sliding = false;
            live.Remove(this);
            if (rt == null) return;
            rt.anchorMin = new Vector2(toAnchorX, rt.anchorMin.y);
            rt.anchorMax = new Vector2(toAnchorX, rt.anchorMax.y);
            rt.anchoredPosition = new Vector2(toOffX, rt.anchoredPosition.y);
        }

        void OnDestroy() { live.Remove(this); }

        public static void SettleAll()
        {
            for (int i = live.Count - 1; i >= 0; i--) if (live[i] != null) live[i].Settle();
        }
    }
}
