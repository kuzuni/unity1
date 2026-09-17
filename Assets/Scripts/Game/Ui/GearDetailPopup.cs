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
            UiKit.Anchor(card, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, GearDetailStyle.Px("bottom_h")), w, card.sizeDelta.y);
            VerticalLayoutGroup cardLg = PopupKit.Column(card, UiKit.H("card_pad") + rem * 0.6f, rem * 0.45f);
            // T434 — 정본은 이 카드를 **세 겹**으로 띄운다: 모달 패딩 1.1rem(`card_pad`) + `.cmp-wrap { margin-top: .5rem }`(1783) + `.cmp-card-wrap.cur .cmp-card { padding: .9rem … }`(1812) = **2.5rem**.
            //   종전 `card_pad + rem*0.6`(1.7rem)은 뒤 두 겹을 **한 어림수로 합친 것**이라 0.8rem(=14.6px) 짧았고, 카드는 맞는데 안쪽 글자 블록만 통째로 −10~13px 위였다(T28 96회차 실측).
            //   두 값은 `CraftUi.json` 이 쥔다 — 정본 `ui.js` 3296 이 장비 상세를, 제작 비교가 위 카드를 **같은 `.cmp-wrap` 조각**으로 세우므로 두 화면이 한 표를 나눠 쓴다.
            // ⚠ **위만** 바꾼다: `PopupKit.Column` 의 둘째 인자는 네 변에 똑같이 걸리는데, 좌우를 키우면 바로 아래 줄의 내용 폭(`− rem * 1.2f` = 좌우 0.6rem 두 몫)이 어긋나 카드 폭이 움직인다.
            cardLg.padding = new RectOffset(cardLg.padding.left, cardLg.padding.right,
                                            Mathf.RoundToInt(UiKit.H("card_pad") + CraftStyle.Px("cmp_wrap_mt_rem") + CraftStyle.Px("cmp_card_pt_rem")),
                                            cardLg.padding.bottom);
            RectTransform ic = ForgeUi.ItemCard(card, "cur", w - UiKit.H("card_pad") * 2f - rem * 1.2f, it, "장착됨", null, false, h.Defs, h.GearSys.ItemValue);
            ForgeUi.Ribbon(ic, "장착됨", false);
            PopupKit.XButton(card, () => Close(h));
        }
    }
}
