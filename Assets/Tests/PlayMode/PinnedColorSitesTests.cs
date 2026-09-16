using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Forging;
using Forge.Core.Pets;
using Forge.Core.Save;
using CoreRng = Forge.Core.Data.Rng;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T377 3회차 — 정본 `style.css` 8686 `.btn.btn.danger.danger, .btn.btn.sell.sell { background-color: #ff1017; box-shadow: inset 0 -.22rem 0 #4e0507 }` 는
    /// 전역 토큰(`pp_red` #e8362f · `pp_red_dk`)이 아니라 버튼 규칙에만 준 리터럴이다(8692). 판매 버튼 두 자리(비교 팝업 · 판매 경고)의
    /// 면·턱 `Image` 가 표 `PinnedColorUi.json` 의 값으로 서고, 전역 토큰 값이 아닌지 잰다. 표값이 정본과 같은지는 `tools/check_pinned_colors.py` 가 본다.
    /// 눈 확인은 `screen_craft-compare.png` 판매 버튼 면 (255,16,23)±2 · 턱 (78,5,7)±4.
    /// </summary>
    public class PinnedColorSitesTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null; yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        static void AssertPinned(Transform sell, string what)
        {
            Assert.IsNotNull(sell, what + " — 판매 버튼(sell)");
            Image face = sell.Find("face").GetComponent<Image>();
            Image lip = sell.Find("lip").GetComponent<Image>();
            Color wantFace = PinnedColorUi.C("sell_btn_face"), wantLip = PinnedColorUi.C("sell_btn_lip");
            Assert.AreEqual(wantFace, face.color, what + " — 면 = 표 sell_btn_face(정본 8686 #ff1017)");
            Assert.AreEqual(wantLip, lip.color, what + " — 턱 = 표 sell_btn_lip(정본 8686 #4e0507)");
            Assert.AreNotEqual(UiKit.C("pp_red"), face.color, what + " — 면이 전역 토큰 pp_red 가 아니다(8692 «토큰을 옮기지 말 것»)");
            Assert.AreNotEqual(UiKit.C("pp_red_dk"), lip.color, what + " — 턱이 전역 토큰 pp_red_dk 가 아니다");
        }

        /// <summary>비교 팝업(정본 ui.js 3266 `.btn.sell`) — 촬영 자 `craft-compare` 와 같은 열기.</summary>
        [UnityTest]
        public IEnumerator 비교_팝업_판매_버튼의_면과_턱은_못박은_리터럴이다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            ForgeItem it = F.Engine.RollItem();
            it.Subs = SubstatRoll.Roll(F.Defs, CoreRng.Mulberry(43224), 2);
            ForgeCraftPopup.Show(F, it);
            yield return null; yield return null;
            Popup p = F.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업이 열렸다");
            AssertPinned(p.Root.Find("card/lower/row/sell"), "비교 팝업");
            ForgeCraftPopup.Hide(F);
            yield return null;
        }

        /// <summary>판매 경고(정본 ui.js 3865 `.btn.danger`).</summary>
        [UnityTest]
        public IEnumerator 판매_경고_판매_버튼의_면과_턱은_못박은_리터럴이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem a = h.Engine.RollItem(), b = h.Engine.RollItem();
            ForgeCraftPopup.ShowSellConfirm(h, a, b);
            yield return null;
            Popup p = PopupLayer.Instance.Find(ForgeCraftPopup.SellName);
            Assert.IsNotNull(p, "판매 경고 팝업");
            AssertPinned(p.Root.Find("card/row/sell"), "판매 경고");
            PopupLayer.Instance.Hide(ForgeCraftPopup.SellName);
            yield return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform t = FindDeep(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>T377 4회차 — 모루 «보류» 배지(정본 `style.css` 999 `.anvil-btn.held-slot .held-tag { background: #f0a020 }`).
        /// 전역 노랑 토큰 `coin`(#ffd54f)과 다른 앰버라 자리 전용 키가 쥔다.</summary>
        [UnityTest]
        public IEnumerator 모루_보류_배지의_면은_못박은_앰버지_전역_노랑_토큰이_아니다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeItem it = h.Engine.RollItem();
            h.SetPendingCraft(it);
            ForgeSheet.Render(h);
            yield return null;
            Transform tagBox = FindDeep(UiRoot.Instance.Sheet, "held-tag-bg");
            Assert.IsNotNull(tagBox, "보류 배지 상자(held-tag-bg) — 대기품을 세웠으니 모루 자리에 선다");
            Image bg = tagBox.Find("bg").GetComponent<Image>();
            Color want = PinnedColorUi.C("held_tag_face");
            Assert.AreEqual(want, bg.color, "배지 면 = 표 held_tag_face(정본 999 #f0a020)");
            Assert.AreNotEqual(UiKit.C("coin"), bg.color, "전역 노랑 토큰 coin(#ffd54f)이 아니다 — 정본이 선택자에만 못박은 값이다");
            h.ClearPendingCraft();
            ForgeSheet.Render(h);
            yield return null;
        }

        /// <summary>T396 5회차 — 종이 버튼 비활성 글자: 공용(정본 8725 #7b7b7b · `disabled_ink`)과 **은색**(5266 `.skd-btn.silver.disabled` · 5497 `.petup-selrow .btn.silver.disabled` · #6f6f6f · `disabled_ink2`)이 다르다.
        /// 전엔 종류와 무관하게 `disabled_ink` 였다. 표값이 정본과 같은지는 `check_pinned_colors` 가 본다 — 여기는 «자리가 그 키를 쓰는가».</summary>
        [UnityTest]
        public IEnumerator 은색_종이_버튼의_비활성_글자는_공용_비활성보다_한_톤_어두운_전용_잉크다()
        {
            yield return Boot();
            Transform app = UiRoot.Instance.App;
            RectTransform box = UiKit.Box(app, "t396-box");
            try
            {
                Button silver = PetSkillKit.PaperButton(box, "silver-off", PetSkillKit.BtnKind.Silver, "업그레이드", null, true, () => { });
                Button primary = PetSkillKit.PaperButton(box, "primary-off", PetSkillKit.BtnKind.Primary, "확인", null, true, () => { });
                Button silverOn = PetSkillKit.PaperButton(box, "silver-on", PetSkillKit.BtnKind.Silver, "업그레이드", null, false, () => { });
                yield return null;
                var sl = silver.transform.Find("label").GetComponent<TMPro.TextMeshProUGUI>();
                var pl = primary.transform.Find("label").GetComponent<TMPro.TextMeshProUGUI>();
                var ol = silverOn.transform.Find("label").GetComponent<TMPro.TextMeshProUGUI>();
                Assert.AreEqual(PetSkillStyle.C("disabled_ink2"), sl.color, "은색 비활성 = disabled_ink2(정본 5266·5497 #6f6f6f)");
                Assert.AreEqual(PetSkillStyle.C("disabled_ink"), pl.color, "공용 비활성 = disabled_ink(정본 8725 #7b7b7b)");
                Assert.AreNotEqual(sl.color, pl.color, "두 비활성 잉크는 다른 값이다(은색이 한 톤 어둡다)");
                Assert.Greater(pl.color.r, sl.color.r, "은색 쪽이 더 어둡다");
                Assert.AreEqual(PetSkillStyle.C("white"), ol.color, "활성 은색 버튼 글자는 흰색(정본 .petup-selrow .btn.silver)");
            }
            finally { Object.Destroy(box.gameObject); }
        }

        static Transform FindActive(Transform root, string name)
        {
            if (!root.gameObject.activeInHierarchy) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform r = FindActive(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        /// <summary>T396 6회차 — 정본 3452 `.chat-input-bar input::placeholder { color: #6b6b6b }`(«브라우저 기본이 흐려서» 일부러 진하게 못박은 리터럴).
        /// 클론은 전역 `pp_muted`(#8a8a8a)로 찍고 있었다. 채팅은 탭이 아니라 HUD 채팅 줄 → 전체화면 팝업(ChatGapTests 와 같은 길).</summary>
        [UnityTest]
        public IEnumerator 채팅_입력칸_안내글은_전역_muted_가_아니라_못박은_진한_회색이다()
        {
            yield return Boot();
            float t = 0f;
            while (!(UiRoot.Instance != null && Hud.Instance != null && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(Hud.Instance, "HUD");
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null; yield return null;
            Popup pop = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(pop, "채팅 팝업");
            Transform bar = FindActive(pop.Root, "input-bar");
            Assert.IsNotNull(bar, "입력 바");
            Transform ph = FindActive(bar, "placeholder");
            Assert.IsNotNull(ph, "안내글");
            var tm = ph.GetComponent<TMPro.TextMeshProUGUI>();
            Color want = PinnedColorUi.C("chat_placeholder_ink");
            Assert.AreEqual(want.r, tm.color.r, 0.02f, "안내글 R = 표 chat_placeholder_ink(정본 3452 #6b6b6b)");
            Assert.AreEqual(want.g, tm.color.g, 0.02f, "안내글 G");
            Assert.AreEqual(want.b, tm.color.b, 0.02f, "안내글 B");
            Assert.AreNotEqual(UiKit.C("pp_muted"), tm.color, "전역 pp_muted(#8a8a8a) 가 아니다 — 정본이 일부러 진하게 못박은 자리(8692 «토큰을 옮기지 말 것»)");
            // 공유 카드(정본 3409 `.chat-share-side small:last-child` · 3425 `.chat-share-label` — **양쪽** 전투력과 «승리» 가 같은 주황 #ff880f)
            Transform win = FindActive(pop.Root, "win"), lose = FindActive(pop.Root, "lose");
            Assert.IsNotNull(win, "공유 카드 이긴 쪽 — 세이브에 공유 카드 줄이 없으면 이 자를 못 잰다"); Assert.IsNotNull(lose, "공유 카드 진 쪽");
            Color orange = UiKit.C("chat_name");
            var winCp = FindActive(win, "cp").GetComponent<TMPro.TextMeshProUGUI>();
            var loseCp = FindActive(lose, "cp").GetComponent<TMPro.TextMeshProUGUI>();
            var winLb = FindActive(win, "label").GetComponent<TMPro.TextMeshProUGUI>();
            Assert.AreEqual(orange, winCp.color, "이긴 쪽 전투력 = #ff880f(전엔 chat_share_win 초록)");
            Assert.AreEqual(orange, loseCp.color, "진 쪽 전투력 = #ff880f(전엔 chat_share_lose 회색) — 정본 `.lose` 는 바탕만 다르다");
            Assert.AreEqual(orange, winLb.color, "«승리» 라벨 = #ff880f");
            Assert.AreNotEqual(UiKit.C("chat_share_win"), winCp.color, "초록 chat_share_win 이 아니다");
            PopupLayer.Instance.Hide(ChatScreen.Name);
        }

        /// <summary>T396 7회차 — 빈 장비 칸의 슬롯 이름. 정본 **866** `.equip-cell .slot-name { color: #d8caca }` 이고
        /// 865 주석이 «라벨도 **판독 확보 대상** — #b9a8a8→#d8caca 반 단계» 로 **일부러 올린 값**임을 적어 뒀다.
        /// 클론은 전역 `pp_muted`(#8a8a8a)라 어두운 마룬 칸 위에서 그 판독 확보가 통째로 빠져 있었다.
        /// 표값이 정본과 같은지는 `check_pinned_colors` 가 본다 — 여기는 «자리가 그 키를 쓰는가».</summary>
        [UnityTest]
        public IEnumerator 빈_장비_칸_슬롯_이름은_전역_muted_가_아니라_판독을_위해_올린_밝은_잉크다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            ForgeSheet.Render(h);
            yield return null;
            Transform nm = FindDeep(UiRoot.Instance.Sheet, "slot-name");
            Assert.IsNotNull(nm, "빈 장비 칸의 슬롯 이름(slot-name)");
            TMPro.TextMeshProUGUI t = nm.GetComponent<TMPro.TextMeshProUGUI>();
            Assert.IsNotNull(t, "슬롯 이름 글자");
            Color want = PinnedColorUi.C("equip_slot_name_ink");
            Assert.AreEqual(want, t.color, "슬롯 이름 = 표 equip_slot_name_ink(정본 866 #d8caca)");
            Assert.AreNotEqual(UiKit.C("pp_muted"), t.color, "전역 pp_muted(#8a8a8a)가 아니다 — 정본이 판독을 위해 반 단계 올린 값이다");
            Assert.Greater(want.r + want.g + want.b, UiKit.C("pp_muted").r + UiKit.C("pp_muted").g + UiKit.C("pp_muted").b,
                "«올렸다» 가 이 자리의 뜻이다 — 표값이 전역보다 어두워지면 이 줄이 먼저 깨진다");
        }

        /// <summary>T396 8회차 — 장비 **목록** 타일의 승천 별. 정본 **784** `.fl-face[data-asc]…::after { color: #ff8801 }` 은
        /// 격자 칸의 `.equip-cell .cell-star`(953 · #ffd54f)와 **다른 색**이다 — 정본은 같은 그림을 자리마다 다르게 둔다.
        /// 클론은 둘 다 전역 `coin` 으로 찍고 있었다. 표값이 정본과 같은지는 `check_pinned_colors` 가 본다 — 여기는 «자리가 그 키를 쓰는가».</summary>
        [UnityTest]
        public IEnumerator 목록_타일의_승천_별은_격자_칸_별과_다른_주황이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.Forge.AscendCount = 2;                       // 승천 0 이면 정본도 별을 안 만든다(`:not([data-asc=""])`)
            ForgeInfoPopup.OpenList(h);
            yield return null; yield return null;
            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "모든 장비의 목록 팝업");
            Transform st = FindDeep(p.Root, "asc");
            Assert.IsNotNull(st, "목록 타일의 승천 별(asc) — 승천 2회라 선다");
            TMPro.TextMeshProUGUI t = st.GetComponent<TMPro.TextMeshProUGUI>();
            Assert.IsNotNull(t, "승천 별 글자");
            Color want = PinnedColorUi.C("list_asc_star_ink");
            Assert.AreEqual(want, t.color, "목록 별 = 표 list_asc_star_ink(정본 784 #ff8801)");
            Assert.AreNotEqual(UiKit.C("coin"), t.color, "전역 coin(#ffd54f)이 아니다 — 그것은 격자 칸 별(953)의 값이다");
            Assert.Less(want.b, UiKit.C("coin").b, "«한 단계 주황» 이 이 자리의 뜻이다 — 파랑이 격자 별보다 낮다");
            h.Meta.Popups.HideAll();   // 이웃 자들과 같은 길(AgePatternTests 247·282·364)
        }
    }
}
