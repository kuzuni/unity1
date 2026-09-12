using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Skills;
using Forge.Game.Battle;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 전투 HUD 스킬 바(원작 ui.js renderSkillBar · updateSkillBar · toggleAuto · index.html `#skill-bar` · shot-042120 우중단): [자동] 알약 + 검정 원 3슬롯 고정 —
    /// 장착 슬롯은 등급색 오브 + 아이콘 + Lv · 빈 슬롯은 어두운 빈 원 · 쿨타임은 아래에서 차오르는 검은 막 · 준비되면 스킬색 광채. 탭 = `Combat.tryCast(id, true)`.
    /// T8 전투 씬이 `BattleContext` 에 스킬 훅을 안 꽂았으므로(T17 완료 기록 «다음 작업용») 여기서 <see cref="Wire"/> 가 `Skill = Skills.Spec` · `EquippedSkills` · `AutoCast` 를 꽂는다(결정 기록).
    /// </summary>
    public sealed class SkillBar : MonoBehaviour
    {
        public static SkillBar Instance { get; private set; }

        sealed class Slot
        {
            public string Id;
            public Button Button;
            public RectTransform Cd;
            public Image Glow;
            public double CdMax;
        }

        PetSkillHost host;
        RectTransform bar;
        readonly List<Slot> slots = new List<Slot>();
        Forge.Core.Battle.Battle wired;

        public Button AutoButton { get; private set; }
        public int SlotCount { get { return slots.Count; } }
        public string SlotId(int i) { return i >= 0 && i < slots.Count ? slots[i].Id : null; }
        public Button SlotButton(int i) { return i >= 0 && i < slots.Count ? slots[i].Button : null; }
        public bool AutoOn { get { return SaveIo.State != null && SaveIo.State.AutoCast; } }

        static Forge.Core.Battle.Battle BattleNow { get { return BattleScene.Instance != null ? BattleScene.Instance.Battle : null; } }

        public static SkillBar Attach(UiRoot root, PetSkillHost h)
        {
            if (Instance != null && Instance.gameObject.scene == root.gameObject.scene) return Instance;
            RectTransform rt = UiKit.Box(root.HudLayer, "skill-bar");
            SkillBar sb = rt.gameObject.AddComponent<SkillBar>();
            Instance = sb;   // HUD 층이 비활성일 때도 Awake 를 기다리지 않는다(SkillPetSheet 와 같은 이유)
            sb.host = h;
            sb.bar = rt;
            h.Changed += sb.Render;
            h.RecalcHero += sb.Render;
            sb.Render();
            return sb;
        }

        void Awake() { Instance = this; }
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (host != null) { host.Changed -= Render; host.RecalcHero -= Render; }
        }

        /// <summary>원작 `Combat` 이 `S.equippedSkills`·`S.autoCast`·`Skills.def/dmg` 를 직접 읽던 자리 — 전투 문맥에 꽂는다(전투가 새로 만들어질 때마다).</summary>
        public void Wire(Forge.Core.Battle.Battle b)
        {
            if (b == null || host == null || !PetSkillHost.Ready) return;
            BattleContext c = b.Context;
            c.Skill = host.Skills.Spec;
            c.EquippedSkills = host.Skills.State.Equipped;
            c.AutoCast = AutoOn;
            wired = b;
        }

        // ===== renderSkillBar =====
        public void Render()
        {
            if (host == null || !PetSkillHost.Ready) return;
            PetSkillKit.Clear(bar);
            slots.Clear();
            SkillSystem sk = host.Skills;
            GameDefs defs = host.Data.Defs;
            float btn = PetSkillStyle.Px("sb_btn_rem"), gap = PetSkillStyle.Px("sb_gap_rem");
            float autoH = PetSkillStyle.Px("sb_auto_h_rem"), autoPad = PetSkillStyle.Px("sb_auto_pad_rem");
            string autoText = PetSkillStyle.T("sb_auto");
            float autoW = PetSkillKit.TextWidth(TextKind.Sub, autoText) + autoPad * 2f;
            int n = sk.Rules.MaxActive;
            float total = autoW + gap + n * btn + (n - 1) * gap;
            bar.anchorMin = bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(1f, 0f);
            bar.anchoredPosition = new Vector2(-PetSkillStyle.Px("sb_right_rem"), PetSkillStyle.Px("sb_bottom_rem"));
            bar.sizeDelta = new Vector2(total, btn);
            // [자동]
            bool on = AutoOn;
            AutoButton = UiKit.Button(bar, "skill-btn-auto", OnToggleAuto);
            RectTransform ar = AutoButton.GetComponent<RectTransform>();
            UiKit.Place(ar, 0f, (btn - autoH) * 0.5f, autoW, autoH);
            RectTransform askin = PetSkillKit.Framed(ar, "skin", PetSkillStyle.C(on ? "sb_auto_on_bg" : "sb_auto_bg"), PetSkillStyle.Px("sb_auto_r_rem"), PetSkillKit.Line3);
            UiKit.Fill(askin);
            ((Image)askin.Find("line").GetComponent<Image>()).color = PetSkillStyle.C(on ? "sb_auto_on_line" : "sb_auto_line");
            TextMeshProUGUI at = on
                ? PetSkillKit.Stroked(ar, "t", TextKind.Sub, autoText, PetSkillStyle.C("white"), 0.25f)
                : PetSkillKit.Text(ar, "t", TextKind.Sub, autoText, PetSkillStyle.C("sb_auto_ink"));
            UiKit.Fill(at.rectTransform);
            // 슬롯 3
            for (int i = 0; i < n; i++)
            {
                string id = i < sk.State.Equipped.Count ? sk.State.Equipped[i] : null;
                SkillDef d = id != null ? sk.Def(id) : null;
                float x = autoW + gap + i * (btn + gap);
                var slot = new Slot { Id = d != null ? id : null };
                if (d == null)
                {
                    RectTransform e = UiKit.Box(bar, "skill-btn-empty-" + i);
                    UiKit.Place(e, x, 0f, btn, btn);
                    RectTransform orb = PetSkillKit.Orb(e, "orb", PetSkillStyle.C("sb_empty_bg"), PetSkillKit.Line2);
                    UiKit.Fill(orb);
                    ((Image)orb.Find("line").GetComponent<Image>()).color = PetSkillStyle.C("sb_empty_line");
                    slots.Add(slot);
                    continue;
                }
                string sid = id;
                slot.Button = UiKit.Button(bar, "skill-btn-" + id, () => OnCast(sid));
                RectTransform br = slot.Button.GetComponent<RectTransform>();
                UiKit.Place(br, x, 0f, btn, btn);
                Color sc = PetSkillStyle.C("sb_bg");
                ColorUtility.TryParseHtmlString(d.Color ?? string.Empty, out sc);
                slot.Glow = PetSkillKit.Disc(br, "glow", new Color(sc.r, sc.g, sc.b, 0.55f));
                float gs = btn * PetSkillStyle.L("sb_glow_f");
                UiKit.Anchor(slot.Glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, gs, gs);
                slot.Glow.enabled = false;
                RectTransform orbRt = PetSkillKit.Orb(br, "orb", PetSkillStyle.Rarity(defs, d.Rarity), PetSkillKit.Line3);
                UiKit.Fill(orbRt);
                Image ico = UiKit.Icon(orbRt, "sk-icon", "sk_" + id);
                float ip = btn * 0.06f;
                ico.rectTransform.offsetMin = new Vector2(ip, ip);
                ico.rectTransform.offsetMax = new Vector2(-ip, -ip);
                // 쿨타임 막(아래에서 차오른다)
                Image cd = PetSkillKit.Disc(orbRt, "sk-cd", PetSkillStyle.C("sb_cd"));
                cd.preserveAspect = false;
                RectTransform cr = cd.rectTransform;
                cr.anchorMin = new Vector2(0f, 0f);
                cr.anchorMax = new Vector2(1f, 0f);
                cr.pivot = new Vector2(0.5f, 0f);
                cr.anchoredPosition = Vector2.zero;
                cr.sizeDelta = Vector2.zero;
                slot.Cd = cr;
                SkillSpec spec = sk.Spec(id);
                slot.CdMax = spec != null ? spec.Cd : d.Cd;
                TextMeshProUGUI lv = PetSkillKit.Stroked(orbRt, "sk-lv", TextKind.Sub, PetSkillStyle.T("lv_short", sk.Level(id)), PetSkillStyle.C("white"), 0.3f);
                float lvH = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.1f;
                UiKit.Anchor(lv.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, PetSkillStyle.Px("sb_lv_bottom_rem")), btn * 1.4f, lvH);
                slots.Add(slot);
            }
            Forge.Core.Battle.Battle b = BattleNow;
            if (b != null) Wire(b);
        }

        // ===== updateSkillBar(매 프레임) =====
        void Update()
        {
            Forge.Core.Battle.Battle b = BattleNow;
            if (b == null) return;
            if (b != wired) Wire(b);
            foreach (Slot s in slots)
            {
                if (s.Id == null || s.Cd == null) continue;
                double cd;
                if (!b.Cooldowns.TryGetValue(s.Id, out cd)) cd = 0;
                float ratio = s.CdMax > 0 ? Mathf.Clamp01((float)(cd / s.CdMax)) : 0f;
                s.Cd.anchorMax = new Vector2(1f, ratio);
                s.Cd.sizeDelta = Vector2.zero;
                s.Glow.enabled = cd <= 0;
            }
        }

        /// <summary>원작 `Combat.tryCast(id, true)` — 전투가 없으면 아무 일도 없다.</summary>
        public bool OnCast(string id)
        {
            Forge.Core.Battle.Battle b = BattleNow;
            if (b == null || id == null) return false;
            if (b != wired) Wire(b);
            return b.TryCast(id, true);
        }

        /// <summary>원작 `toggleAuto` — `S.autoCast` 반전 · 저장 · 다시 그린다.</summary>
        public void OnToggleAuto()
        {
            if (SaveIo.State == null) return;
            SaveIo.State.AutoCast = !SaveIo.State.AutoCast;
            if (wired != null) wired.Context.AutoCast = SaveIo.State.AutoCast;
            host.Save();
            Render();
        }
    }
}
