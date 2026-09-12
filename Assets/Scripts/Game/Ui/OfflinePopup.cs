using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Save;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 오프라인 보상 팝업(ROUTINE T22 · 원작 ui.js showOffline/updateOfflineValues/onCollectOffline · T13 <see cref="Offline"/> 와 짝 — 여기서는 화면만).
    /// 위 = 제목 · 수집 시간(최대) · 수급률 둘 / 아래 = 합계 · [수집]. 켜둔 채로 누적이 자라므로 1초마다 수치만 갱신한다. ✕ 는 단순 닫힘(보상은 남는다).
    /// </summary>
    public static class OfflinePopup
    {
        public const string Name = "offline";
        private static TextMeshProUGUI counted, max, coins, hammers;
        private static float acc;

        public static void Show(MetaHost h, OfflineReward o)
        {
            Popup p = h.Popups.Show(Name);
            if (p.Root.childCount > 1) { Update(o); return; }
            float rem = PopupKit.Rem, w = UiKit.RefW, H = UiKit.RefH;
            float cardW = UiKit.L("offline_w") * w, cardH = UiKit.L("offline_h") * H;
            RectTransform card = PopupKit.Card(p.Root, "card", cardW, cardH, "pp_paper", rem);
            float inner = cardW - PopupKit.Line3 * 2f;
            float topH = cardH * 0.58f;
            RectTransform top = UiKit.Box(card, "top");
            UiKit.Place(top, PopupKit.Line3, PopupKit.Line3, inner, topH);
            UiKit.Panel(top, "bg", "pp_panel");
            float y = rem * 0.8f;
            TextMeshProUGUI title = UiKit.Text(top, "title", TextKind.Title, "오프라인 보상", "stage_ink");
            title.fontStyle = FontStyles.Bold;
            float titleH = PopupKit.FontSize(TextKind.Title) * 1.25f;
            UiKit.Place(title.rectTransform, 0f, y, inner, titleH);
            PopupKit.Ring(title);
            y += titleH + rem * 0.3f;
            float lineH = PopupKit.FontSize(TextKind.Sub) * 1.3f;
            TextMeshProUGUI subL = UiKit.Text(top, "sub", TextKind.Sub, "수집 시간:", "pp_ink", TextAlignmentOptions.Right);
            UiKit.Place(subL.rectTransform, 0f, y, inner * 0.45f, lineH);
            counted = UiKit.Text(top, "counted", TextKind.Sub, string.Empty, "pp_green_dk", TextAlignmentOptions.Left);
            counted.fontStyle = FontStyles.Bold;
            UiKit.Place(counted.rectTransform, inner * 0.47f, y, inner * 0.3f, lineH);
            max = UiKit.Text(top, "max", TextKind.Sub, string.Empty, "pp_ink", TextAlignmentOptions.Left);
            UiKit.Place(max.rectTransform, inner * 0.77f, y, inner * 0.23f, lineH);
            y += lineH + rem * 0.6f;
            float rateH = rem * 2.2f;
            Rate(top, "coin", "coin", "coin", PopupKit.FmtDec(o.CoinRate) + "/초", inner * 0.05f, y, inner * 0.42f, rateH);
            Rate(top, "hammer", "hammer", "pp_green", PopupKit.FmtDec(o.HammerRate) + "/분", inner * 0.53f, y, inner * 0.42f, rateH);

            RectTransform bottom = UiKit.Box(card, "bottom");
            UiKit.Place(bottom, PopupKit.Line3, PopupKit.Line3 + topH, inner, cardH - topH - PopupKit.Line3 * 2f);
            float by = rem * 0.8f;
            float ico = lineH;
            RectTransform total = UiKit.Box(bottom, "total");
            UiKit.Place(total, 0f, by, inner, lineH);
            Image ci = PopupKit.IconOr(total, "coin-ico", "coin");
            UiKit.Place(ci.rectTransform, inner * 0.18f, 0f, ico, ico);
            coins = UiKit.Text(total, "coins", TextKind.Sub, string.Empty, "pp_ink", TextAlignmentOptions.Left);
            coins.fontStyle = FontStyles.Bold;
            UiKit.Place(coins.rectTransform, inner * 0.18f + ico + rem * 0.2f, 0f, inner * 0.25f, lineH);
            Image hi = PopupKit.IconOr(total, "hammer-ico", "hammer");
            UiKit.Place(hi.rectTransform, inner * 0.55f, 0f, ico, ico);
            hammers = UiKit.Text(total, "hammers", TextKind.Sub, string.Empty, "pp_ink", TextAlignmentOptions.Left);
            hammers.fontStyle = FontStyles.Bold;
            UiKit.Place(hammers.rectTransform, inner * 0.55f + ico + rem * 0.2f, 0f, inner * 0.25f, lineH);
            by += lineH + rem * 0.7f;
            float bw = inner * 0.5f, bh = UiKit.H("btn_h") * 1.2f;
            Button collect = PopupKit.Btn(bottom, "collect", "수집", "pp_green", "pp_green_dk", () => Collect(h), bw, bh);
            UiKit.Place(collect.GetComponent<RectTransform>(), (inner - bw) * 0.5f, by, bw, bh);

            PopupKit.XButton(card, () => Close(h));
            Update(o);
        }

        private static void Rate(Transform parent, string name, string icon, string circleKey, string text, float x, float y, float w, float h)
        {
            RectTransform box = UiKit.Box(parent, name);
            UiKit.Place(box, x, y, w, h);
            RectTransform circle = UiKit.Box(box, "circle");
            UiKit.Place(circle, 0f, 0f, h, h);
            UiKit.Circle(circle, "line", "pp_line");
            Image face = UiKit.Circle(circle, "face", circleKey);
            PopupKit.Inset(face.rectTransform, PopupKit.Line);
            Image ico = PopupKit.IconOr(circle, "ico", icon);
            PopupKit.Inset(ico.rectTransform, h * 0.2f);
            TextMeshProUGUI t = UiKit.Text(box, "text", TextKind.Sub, text, "pp_green_dk", TextAlignmentOptions.Left);
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.offsetMin = new Vector2(h + PopupKit.Rem * 0.3f, 0f);
        }

        /// <summary>원작 updateOfflineValues — 숫자만 갱신(버튼 노드를 매초 갈지 않는다).</summary>
        public static void Update(OfflineReward o)
        {
            if (counted == null || o == null) return;
            counted.text = PopupKit.FmtTime(o.Counted);
            max.text = o.Elapsed > o.Counted ? "(최대)" : string.Empty;
            coins.text = PopupKit.Fmt(o.Coins);
            hammers.text = PopupKit.Fmt(o.Hammers);
        }

        /// <summary>MetaHost 의 1초 틱이 부른다.</summary>
        public static void Tick(MetaHost h)
        {
            if (!h.Popups.IsOpen(Name) || SaveIo.Instance == null) return;
            Update(SaveIo.Instance.PendingOffline());
        }

        private static void Collect(MetaHost h)
        {
            OfflineReward r = SaveIo.Instance != null ? SaveIo.Instance.ClaimOffline() : null;
            if (r == null) { h.Toast("💤 아직 누적된 오프라인 보상이 없습니다"); Close(h); return; }
            Close(h);
            h.Touch();
        }

        public static void Close(MetaHost h)
        {
            h.Popups.Hide(Name);
            counted = max = coins = hammers = null;
        }
    }
}
