using System;
using System.Collections.Generic;
using UnityEngine;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T355 ⓖ — 탭 패널이 열릴 때 정본대로 아래에서 미끄러져 올라온다(`style.css` 642 `.panel { transform: translateY(105%); transition: transform .22s ease-out }` ·
    /// `.panel.open { transform: none }`). 패널 자체에 붙어 <c>OnEnable</c>(= 정본 `.open`) 에서 시작하므로 여닫는 쪽(<see cref="TabBar.Switch"/>)은 안 건드린다.
    /// 값은 표 <c>PressFxUi.json</c> 의 <c>panel_slide</c>(<see cref="PanelSlideSpec"/>) · 셈은 Core <see cref="PressRules.SlideOffset"/>.
    /// 닫힘은 즉시다(정본은 .22s 되내려가지만 클론의 자들이 «닫으면 바로 비활성» 을 단언한다 — 결정 기록 참고) · 정지 촬영은 <see cref="SettleAll"/> 로 끝난 모습을 찍는다(카드 팝의 `CardPop.SettleAll` 과 같은 길).
    /// </summary>
    public sealed class PanelSlide : MonoBehaviour
    {
        static PanelSlideSpec spec;
        static readonly List<PanelSlide> live = new List<PanelSlide>();

        /// <summary>표 `panel_slide`(한 번 읽는다).</summary>
        public static PanelSlideSpec Spec
        {
            get
            {
                if (spec == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(PressFx.ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + PressFx.ResourcePath + ".json 이 없다 (T355)");
                    spec = PanelSlideSpec.From(MiniJson.ParseObject(ta.text));
                }
                return spec;
            }
        }

        public static void Reset() { spec = null; }

        RectTransform rt;
        Vector2 basePos;
        double elapsedMs;
        bool sliding;

        /// <summary>지금 올라오는 중인가.</summary>
        public bool Sliding { get { return sliding; } }
        /// <summary>열린 뒤 지난 ms.</summary>
        public double ElapsedMs { get { return elapsedMs; } }
        /// <summary>놓였을 때의 자리(= 정본 `transform: none`).</summary>
        public Vector2 BasePos { get { return basePos; } }

        /// <summary>패널에 붙인다(비활성인 채 붙여도 된다 — 켜질 때마다 슬라이드).</summary>
        public static PanelSlide Attach(RectTransform panel)
        {
            if (panel == null) throw new ArgumentNullException("panel");
            PanelSlide p = panel.GetComponent<PanelSlide>();
            if (p == null) p = panel.gameObject.AddComponent<PanelSlide>();
            p.rt = panel;
            return p;
        }

        void Awake() { if (rt == null) rt = (RectTransform)transform; }

        void OnEnable()
        {
            if (rt == null) rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
            elapsedMs = 0;
            sliding = true;
            if (!live.Contains(this)) live.Add(this);
            Apply();
        }

        void OnDisable()
        {
            live.Remove(this);
            if (sliding) { sliding = false; rt.anchoredPosition = basePos; }
        }

        void Update()
        {
            if (!sliding) return;
            elapsedMs += Time.unscaledDeltaTime * 1000.0;
            Apply();
            if (PressRules.SlideDone(Spec, elapsedMs)) Settle();
        }

        void Apply()
        {
            if (!sliding) return;
            float h = rt.rect.height;
            rt.anchoredPosition = basePos + new Vector2(0f, -(float)PressRules.SlideOffset(Spec, elapsedMs, h));
        }

        /// <summary>지금 당장 제자리로(정지 촬영·자).</summary>
        public void Settle()
        {
            if (!sliding) return;
            sliding = false;
            rt.anchoredPosition = basePos;
        }

        /// <summary>켜져 있는 패널 전부를 제자리로 — 촬영 직전(`UiShotsTests`)에 부른다.</summary>
        public static void SettleAll()
        {
            for (int i = live.Count - 1; i >= 0; i--) if (live[i] != null) live[i].Settle();
        }
    }
}
