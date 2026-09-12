using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Forging;

namespace Forge.Game.Ui
{
    /// <summary>장비 세부정보 팝업(ROUTINE T19 · 원작 ui.js `openGearDetail`·`renderGearDetail` · shot-043244): 장착 카드 한 장 + 카드 아래 빨간 ✕ · 바깥 탭하면 닫힘. 버튼 없음.</summary>
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
            float w = UiKit.L("modal_wide_w") * UiKit.RefW, rem = PopupKit.Rem;
            RectTransform card = PopupKit.Card(root, "card", w, -1f, "pp_paper", rem * 1.1f);
            PopupKit.Column(card, UiKit.H("card_pad") + rem * 0.6f, rem * 0.45f);
            RectTransform ic = ForgeUi.ItemCard(card, "cur", w - UiKit.H("card_pad") * 2f - rem * 1.2f, it, "장착됨", null, false, h.Defs, h.GearSys.ItemValue);
            ForgeUi.Ribbon(ic, "장착됨", false);
            PopupKit.XButton(card, () => Close(h));
        }
    }
}
