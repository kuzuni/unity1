using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 소환 시트(원작 index.html `#panel-summon` + ui.js switchSummonSub · UI-SPEC 8~16): T18 탭바의 «summon» 흰 시트 안에
    /// 서브탭 3개(스킬 | 펫 | 기술 트리 · 실버 통짜 바 · 활성 = 파랑) 를 **아래**에 두고 그 위를 서브 패널이 채운다.
    /// 스킬 = <see cref="SkillPanel"/> · 펫 = <see cref="PetPanel"/> · 기술 트리 = 빈 자리(T21 이 채운다). 시트가 닫히면 모달을 전부 닫는다.
    /// </summary>
    public sealed class SkillPetSheet : MonoBehaviour
    {
        public static SkillPetSheet Instance { get; private set; }

        public const string SubSkills = "skills", SubPets = "pets", SubTech = "tech";
        static readonly string[] Subs = { SubSkills, SubPets, SubTech };
        static readonly string[] SubText = { "sub_skills", "sub_pets", "sub_tech" };

        public string ActiveSub { get; private set; }
        public SkillPanel Skills { get; private set; }
        public PetPanel Pets { get; private set; }
        public RectTransform TechArea { get; private set; }
        public PetSkillModal Modal { get; private set; }
        public PetSkillHost Host { get; private set; }

        RectTransform panel;
        RectTransform body;
        readonly RectTransform[] subRects = new RectTransform[3];
        readonly Button[] subButtons = new Button[3];
        readonly RectTransform[] subSkins = new RectTransform[3];
        readonly TextMeshProUGUI[] subLabels = new TextMeshProUGUI[3];
        RectTransform pill;

        public static SkillPetSheet Attach(UiRoot root, PetSkillHost host)
        {
            if (Instance != null) return Instance;
            RectTransform panel = root.TabBar.Panel("summon");
            if (panel == null) throw new InvalidOperationException("탭바에 summon 시트가 없다 (T18)");
            SkillPetSheet s = panel.gameObject.AddComponent<SkillPetSheet>();
            s.Host = host;
            s.Modal = PetSkillModal.Attach(root);
            s.Build(panel);
            root.TabBar.Switched += s.OnTabSwitched;
            host.Changed += s.OnChanged;
            return s;
        }

        void Awake() { Instance = this; }
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (UiRoot.Instance != null && UiRoot.Instance.TabBar != null) UiRoot.Instance.TabBar.Switched -= OnTabSwitched;
            if (Host != null) Host.Changed -= OnChanged;
        }

        void Build(RectTransform p)
        {
            panel = p;
            float stripH = PetSkillStyle.Px("subtab_h");
            float pad = PetSkillStyle.Px("pad_rem");
            float panelH = p.rect.height > 0f ? p.rect.height : UiKit.L("tabbar_top") * UiKit.RefH;

            // ---- 서브탭 통짜 바(아래) ----
            RectTransform strip = UiKit.Box(p, "summon-subtabs");
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.anchoredPosition = Vector2.zero;
            strip.sizeDelta = new Vector2(0f, stripH);
            Image bg = UiKit.Panel(strip, "bg", "pp_paper");
            bg.color = PetSkillStyle.C("subtab_bg");
            bg.raycastTarget = true;
            UiKit.Line(strip, "line", "pp_line", PetSkillKit.Line3, true);
            float pillW = UiKit.RefW * PetSkillStyle.L("subtab_pill_w_f");
            float pillH = PetSkillStyle.Px("subtab_pill_h");
            float activeH = PetSkillStyle.Px("subtab_active_h");
            pill = UiKit.Box(strip, "subtab-pill");
            UiKit.Anchor(pill, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, pillW, pillH);
            PetSkillKit.Fill(pill, "bg", PetSkillStyle.C("subtab_pill"), PetSkillStyle.Px("subtab_pill_r_rem"));
            float bw = pillW / 3f;
            for (int i = 0; i < 3; i++)
            {
                string sub = Subs[i];
                subButtons[i] = UiKit.Button(pill, "sub-" + sub, () => Switch(sub));
                RectTransform br = subButtons[i].GetComponent<RectTransform>();
                UiKit.Anchor(br, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * bw, 0f), bw, activeH);
                subSkins[i] = PetSkillKit.Framed(br, "active", PetSkillStyle.C("pp_blue"), PetSkillStyle.Px("subtab_btn_r_rem"), PetSkillKit.Line2);
                UiKit.Fill(subSkins[i]);
                subLabels[i] = PetSkillKit.Text(br, "label", TextKind.Sub, PetSkillStyle.T(SubText[i]), PetSkillStyle.C("subtab_ink"));
                UiKit.Fill(subLabels[i].rectTransform);
            }

            // ---- 서브 패널 영역(위 · 원작 .panel padding .8rem · 서브탭 위까지) ----
            body = UiKit.Box(p, "summon-body");
            body.anchorMin = new Vector2(0f, 0f);
            body.anchorMax = new Vector2(1f, 1f);
            body.offsetMin = new Vector2(pad, stripH);
            body.offsetMax = new Vector2(-pad, -pad);
            for (int i = 0; i < 3; i++)
            {
                subRects[i] = UiKit.Box(body, "panel-" + Subs[i]);
                subRects[i].gameObject.SetActive(false);
            }
            Skills = subRects[0].gameObject.AddComponent<SkillPanel>();
            Skills.Init(this, panelH - stripH - pad * 2f);
            Pets = subRects[1].gameObject.AddComponent<PetPanel>();
            Pets.Init(this, panelH - stripH - pad * 2f);
            TechArea = subRects[2];
            Switch(SubSkills, false);
        }

        /// <summary>원작 switchSummonSub — 시트가 닫혀 있으면 열고(탭바가 다시 부른다) · 서브 표시 · 그 패널 렌더.</summary>
        public void Switch(string sub) { Switch(sub, true); }

        void Switch(string sub, bool render)
        {
            ActiveSub = sub;
            if (render && UiRoot.Instance != null && UiRoot.Instance.TabBar.ActiveTab != "summon")
            {
                UiRoot.Instance.TabBar.Switch("summon");
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                bool on = Subs[i] == sub;
                subRects[i].gameObject.SetActive(on);
                subSkins[i].gameObject.SetActive(on);
                subLabels[i].color = PetSkillStyle.C(on ? "white" : "subtab_ink");
                if (on) UiKit.Outline(subLabels[i], "pp_line", 0.25f);
                else subLabels[i].outlineWidth = 0f;
            }
            if (!render) return;
            Render();
        }

        public void Render()
        {
            if (ActiveSub == SubSkills) Skills.Render();
            else if (ActiveSub == SubPets) Pets.Render();
        }

        public bool IsSheetOpen { get { return panel != null && panel.gameObject.activeInHierarchy; } }

        void OnTabSwitched(string tab)
        {
            if (tab == "summon") Render();
            else Modal.CloseAll();
        }

        void OnChanged()
        {
            if (IsSheetOpen) Render();
        }

        public Button SubButton(string sub) { return subButtons[Array.IndexOf(Subs, sub)]; }
        public bool SubVisible(string sub) { return subRects[Array.IndexOf(Subs, sub)].gameObject.activeInHierarchy; }
    }
}
