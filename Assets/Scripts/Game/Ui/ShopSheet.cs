using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Meta;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 상점 시트(ROUTINE T22 · 원작 ui.js openShop/renderShop · shot-042632): 다크 마룬 풀스크린 + 재화 바 둘 + 금색 «상점» + 리본 배너 «오늘의 특가» + 특가 카드 3(빨간 깃발 태그 · 회색 보상 pill · 상품 그림 · 파란 가격 버튼) + «보석» + 보석 카드.
    /// 데모판은 결제 대신 하루 1회 무료 수령(원작 Shop.claimDeal) · 보석 패키지는 토스트.
    /// </summary>
    public static class ShopSheet
    {
        public const string Name = "shop";
        private static readonly string[] GemIcons = { "shop_gems1", "shop_gems2", "shop_gems3", "shop_gems4" };

        public static void Open(MetaHost h)
        {
            if (h.Shop.Ensure(h.ShopState, h.TodayKey)) h.WriteStates();
            h.Popups.Show(Name, "shop");
            Render(h);
        }

        public static void Close(MetaHost h) { h.Popups.Hide(Name); }

        public static void Render(MetaHost h)
        {
            Popup p = h.Popups.Find(Name);
            if (p == null) return;
            RectTransform root = PopupLayer.Clear(p);
            RectTransform sheet = PopupKit.Sheet(root, "sheet", "shop_bg");
            float rem = PopupKit.Rem;
            float w = UiKit.RefW;

            RectTransform scrollBox = UiKit.Box(sheet, "scroll");
            UiKit.Band(scrollBox, 0f, UiKit.L("tabbar_top"));
            RectTransform content = PopupKit.ScrollList(scrollBox, "list", rem * 0.5f, 0f, UiKit.H("sheet_pad_top"));

            // ---- 머리: 코인 바 · 상점 · 젬 바 ----
            RectTransform head = PopupKit.Item(content, "head", -1f, PopupKit.FontSize(TextKind.Head) * 1.3f);
            TextMeshProUGUI title = UiKit.Text(head, "title", TextKind.Head, "상점", "shop_title");   // T391 ⓑ — 정본 3805 `.sheet-title { 1.35rem }` = 49.1px → Head 48(전엔 Title 60)
            title.fontStyle = FontStyles.Bold;
            PopupKit.Ring(title, "sheet_title", "pp_line");   // 정본 h2.sheet-title .11em
            UiKit.TextShadow(title, "paper_emboss");   // T333 2회차 — 정본 8381 한 벌 «밝은 종이 위 글자는 흰 엠보스»(0 1px 0 rgba(255,255,255,.92))
            // T378 6회차 — 정본 3968~3975 `.shop-sheet .sheet-head .cur-pill { width: .155W; height: .0239H }` = 표 `shop_cur_w`·`shop_cur_h` 그대로(전엔 높이에 ×1.6 이 얹혀 3.82%H 였다)
            float curW = UiKit.L("shop_cur_w") * w, curH = UiKit.H("shop_cur_h");
            CurBar(head, "coin-bar", "coin", PopupKit.Fmt(h.S.Coins), UiKit.L("shop_banner_x") * w, curW, curH, h);
            CurBar(head, "gem-bar", "gem", PopupKit.Fmt(h.S.Gems), w * 0.866f - curW, curW, curH, h);

            Banner(content, "오늘의 특가");
            TextMeshProUGUI sub = PopupKit.Label(content, "sub", TextKind.Sub, "일일 특가 3개 모두 구매하면 새로운 3개가 나와요!", "stage_ink");
            sub.fontStyle = FontStyles.Bold;
            // 정본 `.shop-sub` 는 아래 여백이 1.42rem 이다 — 목록이 넣는 칸 간격(0.5rem)만으로는 카드가 2.7%H 위에서 시작한다.
            PopupKit.Spacer(content, UiKit.H("shop_sub_gap") - rem * 0.5f);

            // ---- 특가 카드 ----
            float cardX = UiKit.L("shop_banner_x") * w, cardW = UiKit.L("shop_banner_w") * w;
            float cardH = UiKit.H("shop_deal_h");
            List<ShopDeal> deals = h.Meta.Shop.Deals;
            for (int i = 0; i < deals.Count; i++)
            {
                ShopDeal d = deals[i];
                bool claimed = h.Shop.Claimed(h.ShopState, d.Key);
                // 카드 사이 0.91%H 는 **목록 칸 간격이 이미 넣는다**(rem*0.5 = 0.95%H) — 칸 높이에 또 더하면 간격을 두 번 세어 카드 줄이 원작보다 벌어진다.
                RectTransform rowBox = PopupKit.Item(content, "deal-" + d.Key, -1f, cardH);
                RectTransform card = UiKit.Box(rowBox, "card");
                UiKit.Place(card, cardX, 0f, cardW, cardH);
                PopupKit.Outlined(card, "face", "pp_paper", RadiusUi.Px("shop_deal_card_r_rem"), PopupKit.Line3);   // T345 13회차 — 정본 2916 `.shop-deal-card` .9rem

                // 빨간 깃발 태그(카드 바깥선보다 왼쪽에서 시작 · 폭 고정)
                float tagW = UiKit.L("shop_tag_w") * w, tagH = UiKit.H("shop_tag_h");
                // T159 3회차 — 정본 2923 은 오른쪽에 **제비꼬리 홈**(`calc(100% - .8rem) 50%`)이 파인 깃발이다. 여태 민무늬 직사각형이었다.
                // 깊이가 %가 아니라 절대 .8rem 이라 `ClipShape` 에 rem 을 넘겨 꼭짓점을 그 자리에서 잡는다.
                RectTransform tag = ClipShape.Face(card, "tag", "shop_deal_tag",
                    -UiKit.L("shop_tag_left") * w, UiKit.H("shop_tag_top"), tagW, tagH, "pp_red", PopupKit.Rem);
                TextMeshProUGUI tagT = UiKit.Text(tag, "name", TextKind.Sub, d.Name, "stage_ink", TextAlignmentOptions.Left);
                tagT.fontStyle = FontStyles.Bold;
                tagT.rectTransform.offsetMin = new Vector2(w * 0.0503f, 0f);
                PopupKit.Ring(tagT, "shop_deal_tag", "pp_line");   // 정본 .shop-deal-tag 2px

                // 보상 pill 세로 나열
                float pillW = UiKit.L("shop_pill_w") * w, pillH = UiKit.H("shop_pill_h");
                float py = UiKit.H("shop_deal_pad_top");
                for (int r = 0; r < d.Reward.Count; r++)
                {
                    string cur = d.Reward.KeyAt(r);
                    RectTransform pill = UiKit.Box(card, "pill-" + cur);
                    UiKit.Place(pill, UiKit.L("shop_deal_pad_x") * w, py + r * (pillH + UiKit.H("shop_pill_gap")), pillW, pillH);
                    Image pillFace = UiKit.Rounded(pill, "bg", "shop_pill", pillH * 0.5f);
                    // T178 13회차 — 정본 8235 `.shop-reward-pill { background-image: linear-gradient(180deg, rgba(0,0,0,.16) 0, rgba(0,0,0,.03) 40%, rgba(255,255,255,.10) 82%, rgba(255,255,255,.34) 100%) }`.
                    // 회색 알약 위의 «오목한 홈»(위는 그늘 · 아래는 빛) — 클론은 단색 면 한 장이었다. 둥근 면이라 마스크를 걸어 얹는다(T178 3회차).
                    SurfaceArt.FillMasked(pillFace, "pill-grad", "shop_reward_pill", pillW, pillH);
                    Image ico = PopupKit.IconOr(pill, "ico", CurIcon(cur));
                    UiKit.Place(ico.rectTransform, rem * 0.3f, (pillH - pillH * 0.8f) * 0.5f, pillH * 0.8f, pillH * 0.8f);
                    TextMeshProUGUI amt = UiKit.Text(pill, "amt", TextKind.Sub, PopupKit.Fmt(d.Reward.ValueAt(r)), "pp_ink", TextAlignmentOptions.Left);
                    amt.fontStyle = FontStyles.Bold;
                    amt.rectTransform.offsetMin = new Vector2(rem * 0.3f + pillH * 0.9f, 0f);
                }

                // 상품 그림(우상) · 가격 버튼(우하 · 그림 위로 겹친다)
                float artW = UiKit.L("shop_art_w") * w, artH = UiKit.H("shop_art_h");
                RectTransform art = UiKit.Box(card, "art");
                UiKit.Place(art, cardW - UiKit.L("shop_art_right") * w - artW, UiKit.H("shop_art_top"), artW, artH);
                PopupKit.IconOr(art, "img", "shop_" + d.Key);
                float priceW = UiKit.L("shop_price_w") * w, priceH = UiKit.H("shop_price_h");
                string key = d.Key;
                Button price = null;
                price = PopupKit.Btn(card, "price", claimed ? "수령 완료" : d.PriceKr, "pp_blue", "pp_blue_dk", () => OnClaimDeal(h, key, price ? price.GetComponent<RectTransform>() : null), priceW, priceH, "stage_ink", TextKind.Sub, claimed);
                UiKit.Place(price.GetComponent<RectTransform>(), cardW - UiKit.L("shop_price_right") * w - priceW, cardH - UiKit.H("shop_price_bottom") - priceH, priceW, priceH);
                // T354 9회차 — 정본 2959 `.shop-price-btn { line-height: 1.15 }`. 라벨이 두 줄로 꺾이는 값(«수령 완료» 같은 긴 문구)에서 줄 간격이 정본과 같아진다.
                TextMeshProUGUI priceLabel = price.GetComponentInChildren<TextMeshProUGUI>();
                if (priceLabel != null) LineHeight.Apply(priceLabel, "shop_price_btn_lh");
            }

            // 정본 `.shop-deals + .shop-banner` = 카드 바닥 ↔ 배너 3.08%H. 목록이 이미 넣는 것(카드 칸 안쪽 간격 + 위아래 목록 간격 둘)을 뺀다.
            PopupKit.Spacer(content, UiKit.H("shop_deals_banner_gap") - rem);
            Banner(content, "보석");

            // ---- 보석 카드(3열 격자) ----
            float gemW = UiKit.L("shop_gem_w") * w, gemH = UiKit.H("shop_gem_h"), gemGap = UiKit.L("shop_gem_gap") * w;
            List<GemPack> packs = h.Meta.Shop.GemPacks;
            int cols = 3;
            int rows = (packs.Count + cols - 1) / cols;
            // 정본 `.shop-gems { margin-top: calc(var(--app-h) * .026 - .5rem) }` — 목록이 넣는 칸 간격만큼 빼는 것까지 정본 그대로.
            float gemsTop = UiKit.H("shop_gems_top") - rem * 0.5f;
            RectTransform grid = PopupKit.Item(content, "gems", -1f, rows * gemH + (rows - 1) * gemGap + gemsTop);
            for (int i = 0; i < packs.Count; i++)
            {
                GemPack gp = packs[i];
                RectTransform card = UiKit.Box(grid, "gem-" + i);
                UiKit.Place(card, UiKit.L("shop_gems_x") * w + (i % cols) * (gemW + gemGap), gemsTop + (i / cols) * (gemH + gemGap), gemW, gemH);
                PopupKit.Outlined(card, "face", "pp_paper", RadiusUi.Px("shop_gem_card_r_rem"), PopupKit.Line3);   // T345 13회차 — 정본 2971 `.shop-gem-card` .8rem
                // 카드 «안쪽» 세로 배분은 정본 `.shop-gem-card` 주석의 실측 그대로다(카드 상단 기준 · 894px 캡처 기준을 %H 로):
                // 수량 줄 +8~36px · 그림 +38~98px · 가격 버튼 +101~124px. rem 눈대중으로 두면 자가 밴드8 의 블록을 못 가른다(T75).
                float amtTop = UiKit.H("shop_gem_amt_top"), amtH = UiKit.H("shop_gem_amt_h");
                RectTransform amtRow = UiKit.Box(card, "amt-row");
                UiKit.Place(amtRow, 0f, amtTop, gemW, amtH);
                Image dia = PopupKit.IconOr(amtRow, "dia", "gem");
                UiKit.Place(dia.rectTransform, gemW * 0.18f, 0f, amtH, amtH);
                TextMeshProUGUI amt = UiKit.Text(amtRow, "amt", TextKind.Head, PopupKit.Fmt(gp.Gems), "stage_ink", TextAlignmentOptions.Left);   // T391 ⓑ — 정본 2983 `.shop-gem-amt { 1.35rem }` = 49.1px → Head 48(전엔 Body 40)
                amt.fontStyle = FontStyles.Bold;
                amt.rectTransform.offsetMin = new Vector2(gemW * 0.18f + amtH * 1.08f, 0f);
                PopupKit.Ring(amt, "shop_gem_amt", "pp_line");   // 정본 .shop-gem-amt 4px
                float icon = UiKit.H("shop_gem_icon");
                RectTransform iconBox = UiKit.Box(card, "icon");
                UiKit.Place(iconBox, (gemW - icon) * 0.5f, UiKit.H("shop_gem_icon_top"), icon, icon);
                PopupKit.IconOr(iconBox, "img", GemIcons[i < GemIcons.Length ? i : GemIcons.Length - 1]);
                float bw = w * 0.1811f, bh = UiKit.H("shop_gem_btn_h");
                Button buy = PopupKit.Btn(card, "buy", gp.PriceKr, "pp_blue", "pp_blue_dk", () => h.Toast("💎 데모 버전에서는 결제를 지원하지 않습니다"), bw, bh, "stage_ink", TextKind.Sub);
                UiKit.Place(buy.GetComponent<RectTransform>(), (gemW - bw) * 0.5f, UiKit.H("shop_gem_btn_top"), bw, bh);
            }
            PopupKit.Spacer(content, UiKit.RefH - PopupKit.TabTop + rem * 0.9f);

            PopupKit.SheetBack(sheet, () => Close(h));
        }

        /// <summary>원작 curIcoPlus + 근흑 재화 바(042632 실측) — 누르면 상점(이미 상점).</summary>
        private static void CurBar(Transform parent, string name, string icon, string text, float x, float w, float h, MetaHost host)
        {
            RectTransform bar = UiKit.Box(parent, name);
            UiKit.Anchor(bar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), w, h);
            UiKit.Rounded(bar, "bg", "shop_cur_bar", h * 0.5f);
            TextMeshProUGUI t = UiKit.Text(bar, "value", TextKind.Sub, text, "stage_ink", TextAlignmentOptions.Right);
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.offsetMax = new Vector2(-UiKit.RefW * 0.016f, 0f);
            Image ico = PopupKit.IconOr(bar, "ico", icon);
            float s = h * 1.1f;
            UiKit.Anchor(ico.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, s, s);
        }

        /// <summary>금색 리본 배너(원작 .shop-banner · 좌우 어두운 꼬리).</summary>
        private static void Banner(Transform content, string text)
        {
            float w = UiKit.RefW;
            float bh = UiKit.H("shop_banner_h");
            RectTransform row = PopupKit.Item(content, "banner-" + text, -1f, bh);
            float bx = UiKit.L("shop_banner_x") * w, bw = UiKit.L("shop_banner_w") * w;
            RectTransform tailL = UiKit.Box(row, "tail-l");
            UiKit.Place(tailL, bx - PopupKit.Rem * 0.85f, PopupKit.Rem * 0.45f, PopupKit.Rem * 0.85f, bh - PopupKit.Rem * 0.5f);
            UiKit.Panel(tailL, "bg", "shop_banner_dk");
            RectTransform tailR = UiKit.Box(row, "tail-r");
            UiKit.Place(tailR, bx + bw, PopupKit.Rem * 0.45f, PopupKit.Rem * 0.85f, bh - PopupKit.Rem * 0.5f);
            UiKit.Panel(tailR, "bg", "shop_banner_dk");
            RectTransform band = UiKit.Box(row, "band");
            UiKit.Place(band, bx, 0f, bw, bh);
            Image face = PopupKit.Outlined(band, "face", "shop_banner", RadiusUi.Px("shop_banner_r_rem"), PopupKit.Line3);   // T345 13회차 — 정본 2899 `.shop-banner` .3rem
            SurfaceArt.FillMasked(face, "shop-banner-grad", "shop_banner", bw, bh);   // 정본 .shop-banner linear-gradient(180deg, #ffb300, #e89400) · T178 3회차
            TextMeshProUGUI t = UiKit.Text(band, "label", TextKind.Body, text, "pp_ink");
            t.fontStyle = FontStyles.Bold;
        }

        private static void OnClaimDeal(MetaHost h, string key, RectTransform from)
        {
            if (!h.Shop.ClaimDeal(h.ShopState, h.Wallet, key)) { h.Toast("오늘은 이미 수령했습니다"); return; }
            ShopDeal d = null;
            foreach (ShopDeal x in h.Meta.Shop.Deals) if (x.Key == key) { d = x; break; }
            // 정본 ui.js 4941 — 젬은 claimDeal 이 지급을 걸러내므로 연출에서도 뺀다 · 토스트 없음(수령 연출이 이미 말한다) · T134 3회차
            if (d != null) RewardBurst.Play(RewardBurst.Rewards(d.Reward, "gems"), from);
            h.Touch();
        }

        /// <summary>재화 키 → 아이콘 키(원작 CURRENCY_ICON · QUEST_CUR_ICON).</summary>
        public static string CurIcon(string cur)
        {
            switch (cur)
            {
                case "coins": return "coin";
                case "hammers": return "hammer";
                case "gems": return "gem";
                case "tickets": return "ticket";
                case "winders": return "winder";
                case "eggCurrency": return "egg";
                case "potions": return "potion";
                default: return cur;
            }
        }
    }
}
