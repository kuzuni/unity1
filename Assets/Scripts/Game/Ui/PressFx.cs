using Forge.Core.Data;
using Forge.Core.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T355 — 눌림 피드백(정본 `:active` + `transition: transform … ease-out`). 누르는 상자(<c>source</c> · 정본의 `:active` 요소)에 붙어
    /// 포인터 다운/업/이탈을 받고, 움직이는 상자(<c>target</c> · 정본의 transform 요소 — 같을 수도 다를 수도 있다: `#offline-btn:active .ob-chest`)를
    /// 표 <c>Resources/PressFxUi.json</c> 값대로 아래로 밀고·줄이고·밝힌다. 셈은 <see cref="PressRules"/>(Core) · 값은 전부 표.
    /// <c>UiKit.cs</c> 가 T178·T331·T333 lock 이라 따로 둔 파일 — 공장에 넣는 것은 그 lock 뒤 한 줄이다.
    /// </summary>
    public sealed class PressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const string ResourcePath = "PressFxUi";
        static PressTable table;

        public static PressTable Table
        {
            get
            {
                if (table == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T355)");
                    table = PressTable.From(MiniJson.ParseObject(ta.text));
                }
                return table;
            }
        }
        public static void Reset() { table = null; }

        public PressSpec Spec { get; private set; }
        public RectTransform Target { get; private set; }
        Graphic tint;
        Vector2 basePos;
        Vector3 baseScale;
        Color baseColor;
        double phase, from, elapsedMs;
        bool pressed, dirty;

        /// <summary>지금 눌려 있는가.</summary>
        public bool Pressed { get { return pressed; } }
        /// <summary>위상 0(놓임)~1(눌림).</summary>
        public double Phase { get { return phase; } }
        /// <summary>눌려 있거나 되돌아가는 중 — 밖에서 같은 상자를 움직이는 쪽(들썩임 등)이 이때는 비켜 준다.</summary>
        public bool Active { get { return pressed || phase > 0.0; } }
        /// <summary>참이면 그리지 않는다 — 정본에서 `animation` 이 `transform` 을 쥔 동안 `:active` 의 transform 이 안 보이는 것과 같다(#offline-btn.ready 들썩임).</summary>
        public bool Suppressed { get; set; }

        /// <summary>붙인다. <paramref name="source"/> 는 누르는 상자(raycast 를 받는 Graphic 이 있어야 한다) · <paramref name="target"/> 은 움직일 상자 · <paramref name="tint"/> 는 밝기를 곱할 그림(없으면 밝기 생략).</summary>
        public static PressFx Attach(GameObject source, RectTransform target, string key, Graphic tint = null)
        {
            PressFx p = source.GetComponent<PressFx>();
            if (p == null) p = source.AddComponent<PressFx>();
            p.Spec = Table.Get(key);
            p.Target = target;
            p.tint = tint;
            p.basePos = target.anchoredPosition;
            p.baseScale = target.localScale;
            if (tint != null) p.baseColor = tint.color;
            return p;
        }

        /// <summary>기준(놓였을 때의 자리·배율)을 다시 잡는다 — 밖에서 target 의 자리를 옮긴 뒤 부른다.</summary>
        public void SetBase(Vector2 pos, Vector3 scale) { basePos = pos; baseScale = scale; }

        public void OnPointerDown(PointerEventData e) { Begin(true); }
        public void OnPointerUp(PointerEventData e) { Begin(false); }
        public void OnPointerExit(PointerEventData e) { if (pressed) Begin(false); }

        /// <summary>테스트·코드가 누름/뗌을 직접 건다(포인터 없이).</summary>
        public void Press(bool down) { Begin(down); }

        void Begin(bool down)
        {
            if (pressed == down) return;
            // T355 2회차 — 기준 자리는 «놓인 상태에서 누르는 순간» 다시 잡는다: 레이아웃 그룹 자식(자동 제련 하위 행)은 Attach 때 (0,0) 이었다가
            //   첫 프레임 뒤에야 제자리에 놓이고, 칸도 Place 뒤에 옮겨진다 — 그때의 값이 정본 `transform: none` 이다. 눌린·되돌아가는 중엔 안 잡는다.
            if (down && !Active && Target != null)
            {
                basePos = Target.anchoredPosition;
                baseScale = Target.localScale;
                if (tint != null) baseColor = tint.color;
            }
            pressed = down;
            from = phase;
            elapsedMs = 0;
            dirty = true;
        }

        void Update()
        {
            if (!dirty && PressRules.Settled(phase, pressed)) return;
            elapsedMs += Time.unscaledDeltaTime * 1000.0;
            phase = PressRules.Phase(Spec, from, elapsedMs, pressed);
            dirty = !PressRules.Settled(phase, pressed);
            Apply();
        }

        void Apply()
        {
            if (Suppressed || Target == null) return;
            float rem = PopupKit.Rem;
            Target.anchoredPosition = basePos + new Vector2(0f, -(float)PressRules.DyRem(Spec, phase) * rem);
            float s = (float)PressRules.ScaleAt(Spec, phase);
            Target.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
            if (tint != null)
            {
                float b = (float)PressRules.BrightnessAt(Spec, phase);
                tint.color = new Color(Mathf.Clamp01(baseColor.r * b), Mathf.Clamp01(baseColor.g * b), Mathf.Clamp01(baseColor.b * b), baseColor.a);
            }
        }
    }
}
