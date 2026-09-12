using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T20 모달 층 — 원작 `.modal`(딤 rgba(0,0,0,.5) · z 40 · 탭바까지 덮는다) + `.idet-wrap`(종이 카드 + 아래 겹치는 빨간 ✕) + `#toasts`.
    /// T22 의 공용 모달(`Popups.cs`)이 아직 없어 T20 몫만 여기 둔다(결정 기록 · T22 가 서면 갈아탈 수 있게 표면은 open/close/toast 셋뿐).
    /// 겹쳐 열면 위에 쌓인다(원작 showModal 의 z+1). 소환 시트가 닫히면 전부 닫는다(원작 closeAllTabSurfaces 의 detail-modal).
    /// </summary>
    public sealed class PetSkillModal : MonoBehaviour
    {
        public static PetSkillModal Instance { get; private set; }

        public sealed class Handle
        {
            public RectTransform Root;
            public RectTransform Card;
            public RectTransform Content;
            public Button XButton;
            public string Name;
            public bool Open { get { return Root != null; } }
        }

        readonly List<Handle> stack = new List<Handle>();
        RectTransform layer;
        RectTransform toasts;
        readonly List<RectTransform> toastList = new List<RectTransform>();

        public int OpenCount { get { return stack.Count; } }
        public Handle Top { get { return stack.Count > 0 ? stack[stack.Count - 1] : null; } }
        public bool IsOpen(string name)
        {
            foreach (Handle h in stack) if (h.Name == name) return true;
            return false;
        }
        public Handle Find(string name)
        {
            foreach (Handle h in stack) if (h.Name == name) return h;
            return null;
        }

        public static PetSkillModal Attach(UiRoot root)
        {
            if (Instance != null && Instance.gameObject.scene == root.gameObject.scene) return Instance;
            RectTransform layer = UiKit.Box(root.App, "modals");
            layer.SetAsLastSibling();
            PetSkillModal m = layer.gameObject.AddComponent<PetSkillModal>();
            Instance = m;   // 층이 비활성이어도 Awake 를 기다리지 않는다(SkillPetSheet 와 같은 이유)
            m.layer = layer;
            m.toasts = UiKit.Box(root.App, "toasts");
            m.toasts.SetAsLastSibling();
            PetSkillHost.Toast = m.Toast;
            return m;
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) { Instance = null; if (PetSkillHost.Toast == (Action<string>)Toast) PetSkillHost.Toast = null; } }

        /// <summary>
        /// 종이 카드 모달을 연다. <paramref name="widthFrac"/> = 앱 폭 분수(원작 .petd-card 75.7% 등) · <paramref name="heightPx"/> = 카드 높이(기준 px) ·
        /// <paramref name="topRem"/> = 원작 `.idet-wrap{top}` 오프셋. 반환 Content 는 카드 안쪽(패딩은 호출자가). 같은 이름이 열려 있으면 내용만 비우고 돌려준다(원작 재렌더 경로 · 애니메이션 없음).
        /// </summary>
        public Handle Open(string name, float widthFrac, float heightPx, float topRem, bool withX = true, Action onClose = null)
        {
            Handle h = Find(name);
            if (h != null)
            {
                PetSkillKit.Clear(h.Content);
                return h;
            }
            h = new Handle { Name = name };
            h.Root = UiKit.Box(layer, "modal-" + name);
            Image dim = UiKit.Panel(h.Root, "dim", "pp_line");
            dim.color = PetSkillStyle.C("modal_dim");
            dim.raycastTarget = true;
            float w = widthFrac * UiKit.RefW;
            float r = PetSkillStyle.Px("modal_card_r_rem");
            float shadow = PetSkillStyle.Px("card_shadow_rem");
            float x = PetSkillStyle.Px("x_btn_rem");
            float overlap = PetSkillStyle.Px("x_btn_overlap_rem");
            float top = PetSkillStyle.Rem(topRem);
            // idet-wrap: 카드 + (겹친) ✕ 를 세로 가운데에 (원작 flex column · align center)
            float wrapH = heightPx + (withX ? x - overlap : 0f);
            RectTransform wrap = UiKit.Box(h.Root, "idet-wrap");
            UiKit.Anchor(wrap, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -top), w, wrapH);
            Image sh = PetSkillKit.Fill(wrap, "shadow", PetSkillStyle.C("card_shadow"), r);
            UiKit.Place(sh.rectTransform, 0f, shadow, w, heightPx);
            h.Card = PetSkillKit.Framed(wrap, "card", UiKit.C("pp_paper"), r, PetSkillKit.Line3);
            UiKit.Place(h.Card, 0f, 0f, w, heightPx);
            Image block = h.Card.gameObject.AddComponent<Image>();
            block.color = new Color(0f, 0f, 0f, 0f);
            block.raycastTarget = true;
            h.Content = UiKit.Box(h.Card, "content");
            if (withX)
            {
                h.XButton = UiKit.Button(wrap, "x-btn", () => { Close(h); if (onClose != null) onClose(); });
                RectTransform xr = h.XButton.GetComponent<RectTransform>();
                UiKit.Anchor(xr, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(heightPx - overlap)), x, x);
                RectTransform orb = PetSkillKit.Orb(xr, "skin", PetSkillStyle.C("pp_red"), PetSkillKit.Line3);
                UiKit.Fill(orb);
                Image inset = PetSkillKit.Disc(orb, "inset", PetSkillStyle.C("pp_red_dk"));
                float ins = PetSkillStyle.Px("x_btn_inset_rem");
                inset.rectTransform.offsetMin = new Vector2(PetSkillKit.Line3, PetSkillKit.Line3);
                inset.rectTransform.offsetMax = new Vector2(-PetSkillKit.Line3, -PetSkillKit.Line3);
                Image face = PetSkillKit.Disc(orb, "face", PetSkillStyle.C("pp_red"));
                face.rectTransform.offsetMin = new Vector2(PetSkillKit.Line3, PetSkillKit.Line3 + ins);
                face.rectTransform.offsetMax = new Vector2(-PetSkillKit.Line3, -PetSkillKit.Line3);
                Image mark = UiKit.Icon(xr, "mark", "xmark");
                mark.color = PetSkillStyle.C("white");
                float mk = x * PetSkillStyle.L("x_btn_icon_f");
                UiKit.Anchor(mark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, mk, mk);
            }
            stack.Add(h);
            return h;
        }

        /// <summary>어두운 전체화면 층(소환 결과 · 원작 #summon-result-modal). 반환 Content = 층 전체.</summary>
        public Handle OpenFull(string name, Action onTap)
        {
            Handle h = Find(name);
            if (h != null) { PetSkillKit.Clear(h.Content); return h; }
            h = new Handle { Name = name };
            h.Root = UiKit.Box(layer, "modal-" + name);
            Button tap = UiKit.Button(h.Root, "tap", () => { if (onTap != null) onTap(); });
            h.Card = h.Root;
            h.Content = UiKit.Box(h.Root, "content");
            tap.transform.SetAsFirstSibling();
            stack.Add(h);
            return h;
        }

        public void Close(Handle h)
        {
            if (h == null || h.Root == null) return;
            stack.Remove(h);
            Destroy(h.Root.gameObject);
            h.Root = null;
        }

        public void Close(string name) { Close(Find(name)); }

        public void CloseAll()
        {
            for (int i = stack.Count - 1; i >= 0; i--) Close(stack[i]);
        }

        // ===== 토스트(원작 UI.toast · 2.6초 · 위 15% 가운데 · 최대 몇 개 쌓인다) =====

        public void Toast(string msg)
        {
            if (toasts == null) return;
            float w = UiKit.RefW * PetSkillStyle.L("toast_w_f");
            float padX = PetSkillStyle.Px("toast_pad_x_rem");
            float padY = PetSkillStyle.Px("toast_pad_y_rem");
            float h = UiCatalog.Instance.Kind(TextKind.Sub).size * 1.3f + padY * 2f;
            float tw = Mathf.Min(w, PetSkillKit.TextWidth(TextKind.Sub, msg) + padX * 2f);
            RectTransform t = PetSkillKit.Framed(toasts, "toast", PetSkillStyle.C("toast_bg"), PetSkillStyle.Px("toast_r_rem"), PetSkillStyle.L("line1_px"));
            ((Image)t.Find("line").GetComponent<Image>()).color = PetSkillStyle.C("toast_line");
            TextMeshProUGUI tx = PetSkillKit.Text(t, "t", TextKind.Sub, msg, PetSkillStyle.C("toast_ink"));
            UiKit.Fill(tx.rectTransform);
            t.sizeDelta = new Vector2(tw, h);
            toastList.Add(t);
            Relayout();
            StartCoroutine(Fade(t));
        }

        void Relayout()
        {
            float top = UiKit.RefH * PetSkillStyle.L("toast_top_f");
            float gap = PetSkillStyle.Px("toast_gap_rem");
            float y = top;
            foreach (RectTransform t in toastList)
            {
                if (t == null) continue;
                UiKit.Anchor(t, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -y), t.sizeDelta.x, t.sizeDelta.y);
                y += t.sizeDelta.y + gap;
            }
        }

        IEnumerator Fade(RectTransform t)
        {
            yield return new WaitForSecondsRealtime(PetSkillStyle.L("toast_sec"));
            toastList.Remove(t);
            if (t != null) Destroy(t.gameObject);
            Relayout();
        }

        public int ToastCount
        {
            get { int n = 0; foreach (RectTransform t in toastList) if (t != null) n++; return n; }
        }
        public string LastToast
        {
            get
            {
                for (int i = toastList.Count - 1; i >= 0; i--)
                    if (toastList[i] != null) return toastList[i].GetComponentInChildren<TextMeshProUGUI>().text;
                return null;
            }
        }
    }
}
