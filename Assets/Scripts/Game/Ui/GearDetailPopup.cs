using System;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Forging;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T111 장비 상세 팝업의 배치표(<c>Assets/Forge/Resources/GearDetailUi.json</c> — 정본 `style.css` 1737~1738 `#gear-detail-modal`·`.gd-card`).
    /// T87 lock 이 <c>catalog.json</c> 을 쥐고 있어 T111 몫은 이 파일이 든다(T65 <see cref="PlayerInfoStyle"/> 과 같은 꼴 · 결정 249 · T33 이 합칠 수 있다). 코드에 숫자를 박지 않는다(§1).
    /// </summary>
    public static class GearDetailStyle
    {
        public const string ResourcePath = "GearDetailUi";
        static JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T111)");
            root = MiniJson.ParseObject(ta.text);
            layout = J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문(접미 _w 앱 폭 분수 · _h 앱 높이 분수).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!J.IsNum(v)) throw new System.Collections.Generic.KeyNotFoundException("GearDetailUi.json 에 배치 값 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>키 접미에 맞춰 기준 px 로(_w 앱 폭 · _h 앱 높이).</summary>
        public static float Px(string key)
        {
            float v = L(key);
            if (key.EndsWith("_w")) return v * UiKit.RefW;
            if (key.EndsWith("_h")) return v * UiKit.RefH;
            return v;
        }
    }

    /// <summary>
    /// 장비 세부정보 팝업(ROUTINE T19 · 원작 ui.js `openGearDetail`·`renderGearDetail` · shot-043244): 장착 카드 한 장 + 카드 아래 빨간 ✕ · 바깥 탭하면 닫힘. 버튼 없음.
    /// T111 — 카드는 가운데가 아니라 **하단 앵커**(정본 `#gear-detail-modal { align-items: flex-end; padding-bottom: .223·H − 1.7rem }` · 카드 바닥이 앱 바닥에서 .223·H 위 · ✕ 반쪽은 그 아래로)이고 폭은 `.gd-card` 70%(공용 74% 가 아니다). 두 값은 <see cref="GearDetailStyle"/>.
    /// </summary>
    public static class GearDetailPopup
    {
        public const string Name = "gear-detail";
        static string slot;

        public static void Open(ForgeHost h, string s)
        {
            if (h.Gear.Get(s) == null) return;
            slot = s;
            h.Meta.Popups.Show(Name);
            Render(h);
        }

        public static void Close(ForgeHost h) { h.Meta.Popups.Hide(Name); slot = null; }

        public static void Render(ForgeHost h)
        {
            Popup p = h.Meta.Popups.Find(Name);
            if (p == null || slot == null) return;
            ForgeItem it = h.Gear.Get(slot);
            if (it == null) { Close(h); return; }
            RectTransform root = PopupLayer.Clear(p);
            Button dim = root.GetChild(0).gameObject.GetComponent<Button>() ?? root.GetChild(0).gameObject.AddComponent<Button>();
            dim.onClick.RemoveAllListeners();
            dim.onClick.AddListener(() => Close(h));
            float w = GearDetailStyle.Px("card_w"), rem = PopupKit.Rem;
            RectTransform card = PopupKit.Card(root, "card", w, -1f, "pp_paper", rem * 1.1f);
            // 정본 1737: 카드 바닥을 앱 바닥에서 bottom_h(.223·H) 위에 앉힌다 — 높이는 내용을 따르고(ContentSizeFitter) 피벗이 바닥이라 위로 자란다.
            UiKit.Anchor(card, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, GearDetailStyle.Px("bottom_h") + PopupKit.Line3), w, card.sizeDelta.y);   // T465 — bottom_h 는 카드 몸의 바닥 · rect 는 패딩 상자
            // T471 — 정본 1738 `#gear-detail-modal .gd-card { width: 70% }` 는 **폭만** 정하고 패딩은 `.modal-card`(1752 `padding: 1.1rem`) 그대로다.
            //   종전 `card_pad + rem * 0.6f`(네 변 다 · 근거 없는 한 수)는 카드를 세로 +1.2rem 두껍게 했고, 하단 앵커라 그 몫이 전부 위끝으로 갔다(런 1166 실측 위끝 −1.51%p).
            //   안쪽에서 상쇄하던 −0.96%H 는 `ItemCard` 가 아니라 **정본 1783 `.cmp-wrap { margin-top: .5rem }`**(= 0.95%H)이 클론에 없던 것 — 층의 **위** 패딩에만 더한다(결정 기록).
            float pad = UiKit.H("card_pad");
            float wrapMt = CraftStyle.Px("cmp_wrap_mt_rem");
            VerticalLayoutGroup lg = PopupKit.Column(card, pad, rem * 0.45f);
            lg.padding = new RectOffset(lg.padding.left, lg.padding.right, Mathf.RoundToInt(pad + wrapMt), lg.padding.bottom);
            RectTransform ic = ForgeUi.ItemCard(card, "cur", w - pad * 2f, it, "장착됨", null, false, h.Defs, h.GearSys.ItemValue);
            ForgeUi.Ribbon(ic, "장착됨", false);
            PopupKit.XButton(card, () => Close(h));
        }
    }
}
