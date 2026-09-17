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
        /// <summary>T377 15회차 — **표의 모든 칸이 실제 로더로 읽히는가.** 런 **#1064** 가 이 자리를 비싸게 가르쳤다:
        /// `fi_age_star_ink`(T333 18회차)가 이웃 표들의 관례대로 **주석 달린 객체**로 적혔는데 이 표의 로더만 홑값을 고집해
        /// 그 칸을 부르는 순간 `KeyNotFoundException` 이 났고, 시대 막대를 세우는 화면이 통째로 못 섰다.
        /// 그때 `check_pinned_colors` 는 **초록**이었다 — 그 선택자가 TABLE 에 없어 «미정» 으로 흘러갔기 때문이다.
        /// 곧 **표에 오르지 않은 칸은 아무도 안 보고 있었다.** 이 칸이 그 구멍을 막는다: 자리 배선과 무관하게 전수로 부른다.
        /// (파이썬 자도 같은 규칙을 갖지만 그것은 «모양» 만 본다 — 여기는 `ColorUtility` 까지 실제로 지난다.)</summary>
        [UnityTest]
        public IEnumerator 못박은_색표의_모든_칸이_로더로_읽힌다()
        {
            yield return Boot();
            TextAsset ta = Resources.Load<TextAsset>(PinnedColorUi.ResourcePath);
            Assert.IsNotNull(ta, "Resources/" + PinnedColorUi.ResourcePath + ".json");
            Forge.Core.Data.JsonObject root = Forge.Core.Data.MiniJson.ParseObject(ta.text);
            Forge.Core.Data.JsonObject colors = Forge.Core.Data.J.Obj(root["colors"]);
            Assert.IsNotNull(colors, "colors 칸");
            int n = 0;
            var bad = new System.Collections.Generic.List<string>();
            foreach (var kv in colors)
            {
                if (kv.Key.StartsWith("_")) continue;
                n++;
                try { PinnedColorUi.C(kv.Key); }
                catch (System.Exception e) { bad.Add(kv.Key + " → " + e.GetType().Name + ": " + e.Message); }
            }
            Assert.Greater(n, 50, "표가 통째로 비지 않았다(칸 수)");
            Assert.IsEmpty(bad, "로더가 못 읽는 칸이 있다(부르는 화면이 통째로 못 선다 · 런 #1064) — " + string.Join(" / ", bad.ToArray()));
        }

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

        /// <summary>T377 14회차 — 비교 팝업의 **회색 하부 패널**. 정본 **1819** `.cmp-lower { background: **#bebebe** }` 이고
        /// 바로 위 주석이 «원본 shot-043224 실측: 색 #bebebe(190,190,190)» 로 어디서 온 값인지 적어 뒀다.
        /// 클론은 값은 맞게 찍고 있었지만 `ForgeCraftPopup.cs` 에 `new Color(0xbe/255f, …)` 로 **코드에 박아** 뒀다(§1 «수치는 코드에 박지 않는다») —
        /// 자리 전용 키로 옮겼다. 표값이 정본과 같은지는 `check_pinned_colors` 가 본다 — 여기는 «자리가 그 키를 쓰는가».
        /// ⚠ 이웃 `.cmp-card`(1815 `#ececec`)는 **클론에 자리가 없다**: 정본 1820 `.cmp-lower .cmp-card-wrap.new .cmp-card { background: transparent }` 가 그것을 덮고,
        /// 클론의 유일한 `isNew: true` 카드가 바로 이 `lower` 안에서 서기 때문이다(`ForgeUi.cs` 281 주석이 같은 정본 줄을 이미 인용해 뒀다).
        /// 아래 마지막 줄이 그 «없음» 을 지킨다 — 누가 새 장비 카드에 #ececec 판을 깔면 깨진다.</summary>
        [UnityTest]
        public IEnumerator 비교_팝업_회색_하부_패널의_면은_못박은_리터럴이다()
        {
            yield return Boot();
            ForgeHost F = ForgeHost.Instance;
            ForgeItem it = F.Engine.RollItem();
            it.Subs = SubstatRoll.Roll(F.Defs, CoreRng.Mulberry(43224), 2);
            ForgeCraftPopup.Show(F, it);
            yield return null; yield return null;
            Popup p = F.Meta.Popups.Find(ForgeCraftPopup.Name);
            Assert.IsNotNull(p, "비교 팝업이 열렸다");
            Transform faceT = p.Root.Find("card/lower/face");
            Assert.IsNotNull(faceT, "회색 하부 패널의 면(card/lower/face)");
            Image face = faceT.GetComponent<Image>();
            Color want = PinnedColorUi.C("cmp_lower_face");
            Assert.AreEqual(want, face.color, "하부 패널 면 = 표 cmp_lower_face(정본 1819 #bebebe)");
            Assert.AreNotEqual(UiKit.C("pp_gray"), face.color, "전역 토큰 pp_gray 가 아니다 — 정본이 이 패널에만 따로 적은 회색이다");
            Assert.AreNotEqual(UiKit.C("pp_paper"), face.color, "흰 종이도 아니다 — 카드(흰 판) 위에 얹힌 회색 판이 이 자리의 뜻이다");
            // 정본 1820 이 덮어 둔 «없는 자리» — 새 장비 카드는 제 판을 깔지 않는다(#ececec 가 클론 어디에도 없어야 하는 까닭).
            Transform newCard = p.Root.Find("card/lower/new");
            Assert.IsNotNull(newCard, "새 장비 카드(card/lower/new)");
            Image nf = newCard.GetComponent<Image>();
            Assert.IsTrue(nf == null || nf.color.a < 0.01f, "새 장비 카드는 제 면이 없다(정본 1820 background: transparent) — 회색 패널이 그대로 비친다");
            // 판을 깔았다면 «빈 슬롯» 갈래(`ForgeUi.ItemCard`)처럼 `face` 칸으로 왔을 것이다 — 그 칸이 없다는 게 «자리가 없다» 의 실제 모습이다.
            Assert.IsNull(newCard.Find("face"), "새 장비 카드에 면 칸(face)이 아예 없다 — #ececec 를 받을 자리가 클론에 없다는 뜻");
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

        /// <summary>T396 9회차 — 대장간 시트 «남은 시간». 정본 **3635** `#equip-sheet .forge-time { color: #6d6a63 }` 은
        /// 공용 잉크가 아니라 **그 시트에서만** 쓰는 값이다 — 전역 `ink`(#eceff1)는 **어두운 판 위** 값이라
        /// 밝은 종이 시트 위에서는 글자가 바탕에 묻는다. 표값이 정본과 같은지는 `check_pinned_colors` 가 본다.</summary>
        [UnityTest]
        public IEnumerator 대장간_시트_남은_시간은_밝은_종이_위라_공용_흰_잉크가_아니다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            h.Forge.UpgradeEndsAt = SaveIo.NowMs() + 60 * 60e3;   // 진행 중(BrLinesTests 420 의 길)
            Assert.IsTrue(h.Upgrading, "업그레이드 진행 중 상태");
            ForgeSheet.Render(h);
            yield return null;
            Transform t = FindDeep(UiRoot.Instance.Sheet, "equip-upg-time");
            Assert.IsNotNull(t, "남은 시간 글자(equip-upg-time) — 업그레이드 중이라 선다");
            TMPro.TextMeshProUGUI tt = t.GetComponent<TMPro.TextMeshProUGUI>();
            Color want = PinnedColorUi.C("forge_time_ink");
            Assert.AreEqual(want, tt.color, "남은 시간 = 표 forge_time_ink(정본 3635 #6d6a63)");
            Assert.AreNotEqual(UiKit.C("ink"), tt.color, "전역 ink(#eceff1)가 아니다 — 그것은 어두운 판 위 값이다");
            Assert.Less(want.r + want.g + want.b, UiKit.C("ink").r + UiKit.C("ink").g + UiKit.C("ink").b,
                "«밝은 종이 위라 어두워야 한다» 가 이 자리의 뜻이다 — 표값이 공용 잉크만큼 밝아지면 이 줄이 먼저 깨진다");
            h.Forge.UpgradeEndsAt = null;
            ForgeSheet.Render(h);
            yield return null;
        }

        /// <summary>T377 10회차 — 채팅 **입력 밴드**. 정본 **3438** `.chat-input-bar { background: #0e111b }` 이고 바로 위 3439 주석이
        /// «카드가 흰색이 됐으므로 밴드는 **자기 배경 #0e111b 를 직접 갖는다**» 로 까닭을 적어 뒀다 — 시트가 흰 종이로 바뀐 뒤에도
        /// 이 밴드 하나만은 어둡게 남긴 자리다. 클론은 전역 `pp_paper`(#ffffff)로 찍어 밴드가 카드와 한 덩어리로 희었다(런 720 실측).
        /// 표값이 정본과 같은지는 `check_pinned_colors` 가 본다 — 여기는 «자리가 그 키를 쓰는가».</summary>
        [UnityTest]
        public IEnumerator 채팅_입력_밴드는_흰_종이가_아니라_자기_어두운_배경을_갖는다()
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
            Transform bgT = bar.Find("bg");
            Assert.IsNotNull(bgT, "입력 밴드의 면 칸(bg)");
            Image bg = bgT.GetComponent<Image>();
            Assert.IsNotNull(bg, "입력 밴드의 면(bg)");
            Color want = PinnedColorUi.C("chat_bar_face");
            Assert.AreEqual(want, bg.color, "입력 밴드 면 = 표 chat_bar_face(정본 3438 #0e111b)");
            Assert.AreNotEqual(UiKit.C("pp_paper"), bg.color, "전역 pp_paper(#ffffff)가 아니다 — 정본이 밴드에만 따로 준 어두운 면이다");
            Assert.Less(want.r + want.g + want.b, 1f, "«어둡다» 가 이 자리의 뜻이다 — 표값이 밝아지면 이 줄이 먼저 깨진다");
            // 같은 밴드의 이웃 자리(1회차) — 뒤로 버튼 면은 못박은 빨강 그대로다(이 고침이 그것까지 끌고 가지 않았다는 증거)
            Transform backT = bar.Find("close/face");
            Assert.IsNotNull(backT, "뒤로 버튼의 면(close/face)");
            Image back = backT.GetComponent<Image>();
            Assert.AreEqual(PinnedColorUi.C("chat_back_face"), back.color, "뒤로 버튼 면 = 표 chat_back_face(정본 3283 #ff1017) — 안 움직였다");
            PopupLayer.Instance.Hide(ChatScreen.Name);
        }

        /// <summary>T377 11회차 — 채팅 목록의 **못박은 면 셋**. 정본은 말풍선 면을 **한 줄**로만 주고(3372 `#cecece`)
        /// 공유 카드는 **쪽마다 제 면**을 준다(3397 이긴 쪽 `#39ab36` · 3412 진 쪽 `#cecece` + 주석 «말풍선과 같은 회색이라 목록에 녹아든다»).
        /// 클론은 말풍선을 #f0f0f0 으로, 내 말풍선을 정본에 없는 파랑으로 찍었고 쪽 면은 **아예 안 칠한 채** 카드 한 장을 `pp_panel` 로 덮고 있었다.
        /// 원작 `shot-043500` 화소: #cecece 100,343 · #39ab36 12,308 ↔ 클론 값 #f0f0f0 362 · #dbe9ff **0** · #35c04f **0** · #8a8a8a 375.
        /// 표값이 정본과 같은지는 `check_pinned_colors` 가 본다 — 여기는 «자리가 그 키를 쓰는가».</summary>
        [UnityTest]
        public IEnumerator 채팅_말풍선과_공유_카드_두_쪽은_저마다_못박은_면을_갖는다()
        {
            yield return Boot();
            float t = 0f;
            while (!(UiRoot.Instance != null && Hud.Instance != null && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(Hud.Instance, "HUD");
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null; yield return null;
            Popup pop = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(pop, "채팅 팝업");

            Color bubbleWant = UiKit.C("chat_bubble");
            Transform bubble = FindActive(pop.Root, "bubble");
            Assert.IsNotNull(bubble, "말풍선 — 세이브에 채팅 줄이 없으면 이 자를 못 잰다");
            Image bubbleBg = bubble.Find("bg").GetComponent<Image>();
            Assert.AreEqual(bubbleWant, bubbleBg.color, "말풍선 면 = catalog chat_bubble(정본 3372 #cecece)");
            // 정본엔 «내 말풍선» 갈래가 없다(`.mine` 규칙은 이름 색 하나뿐) — 어느 말풍선을 집어도 같은 면이어야 한다.
            foreach (Transform b in AllDeep(pop.Root, "bubble"))
            {
                Image bg = b.Find("bg").GetComponent<Image>();
                Assert.AreEqual(bubbleWant, bg.color, "말풍선은 전부 같은 면이다 — 정본에 «내 말풍선» 갈래가 없다(§1)");
            }

            Transform win = FindActive(pop.Root, "win"), lose = FindActive(pop.Root, "lose");
            Assert.IsNotNull(win, "공유 카드 이긴 쪽 — 세이브에 공유 카드 줄이 없으면 이 자를 못 잰다"); Assert.IsNotNull(lose, "공유 카드 진 쪽");
            Image winBg = win.Find("bg").GetComponent<Image>(), loseBg = lose.Find("bg").GetComponent<Image>();
            Assert.AreEqual(UiKit.C("chat_share_win"), winBg.color, "이긴 쪽 면 = catalog chat_share_win(정본 3397 #39ab36)");
            Assert.AreEqual(UiKit.C("chat_share_lose"), loseBg.color, "진 쪽 면 = catalog chat_share_lose(정본 3412 #cecece)");
            Assert.AreEqual(bubbleWant, loseBg.color, "진 쪽은 **말풍선과 같은 회색**이다 — 정본 3412 주석이 그렇게 못 박았다");
            Assert.AreNotEqual(UiKit.C("pp_panel"), winBg.color, "이긴 쪽이 카드 한 장의 회색(pp_panel)으로 덮이지 않는다 — 정본 카드는 transparent 다");
            Assert.Greater(winBg.color.g, winBg.color.r + 0.2f, "이긴 쪽은 초록이다(원작 반쪽 12,308화소)");
            PopupLayer.Instance.Hide(ChatScreen.Name);
        }

        /// <summary>T377 12회차 — 자동 제련 **필터 토글**. 정본은 토글을 **두 벌** 쥔다: 설정 토글(3107 · `var(--pp-gray)`/`var(--pp-blue)` 토큰 · 흰 손잡이)과
        /// 이 필터 토글(4759 · **못박은** `#1e2a4a` / 4768 `#35d435` · 손잡이 `var(--pp-blue)`). 클론은 공용 `PopupKit.Toggle` 한 벌로 그려
        /// 필터 토글이 **설정 팔레트**로 찍히고 있었다 — 켜짐이 초록이 아니라 **파랑**이라 두 토글이 구별되지 않았다.
        /// 표값이 정본과 같은지는 `check_pinned_colors` 가 본다 — 여기는 «자리가 그 키를 쓰는가» + «설정 토글은 안 끌려갔는가».</summary>
        [UnityTest]
        public IEnumerator 자동_제련_필터_토글은_설정_토글과_다른_제_색을_갖는다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            // 자동 제련은 2-10 뒤 해금이다(AgePatternTests 250 이 적어 둔 자리) — 안 열면 헛초록이 된다.
            h.S.BestChapter = 3; h.S.BestStage = 1; h.Pull();
            Assert.IsTrue(h.AutoForgeUnlocked, "2-10 뒤 해금");
            h.Engine.AutoForgeConfig().FilterOn = false;
            h.Push();
            ForgeAutoPopup.Open(h);
            yield return null; yield return null;
            Popup p = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(p, "자동 제련 팝업");
            Transform tg = FindActive(p.Root, "af-toggle");
            Assert.IsNotNull(tg, "필터 토글");
            Image off = tg.Find("face").GetComponent<Image>();
            Assert.AreEqual(UiKit.C("af_toggle"), off.color, "꺼진 트랙 = catalog af_toggle(정본 4761 #1e2a4a)");
            Assert.AreNotEqual(UiKit.C("pp_gray"), off.color, "설정 토글의 pp_gray 가 아니다 — 정본은 두 토글을 따로 못박아 뒀다");
            Image knob = tg.Find("knob/face").GetComponent<Image>();
            Assert.AreEqual(UiKit.C("af_toggle_knob"), knob.color, "손잡이 = catalog af_toggle_knob(정본 4766 var(--pp-blue))");
            Assert.AreNotEqual(UiKit.C("pp_paper"), knob.color, "설정 토글의 흰 손잡이(3113 #fff)가 아니다");

            // 켠 상태 — 정본 4768 은 **초록**이다(클론은 파랑이었다).
            h.Engine.AutoForgeConfig().FilterOn = true;
            h.Push();
            ForgeAutoPopup.Render(h);
            yield return null;
            p = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Transform tgOn = FindActive(p.Root, "af-toggle");
            Assert.IsNotNull(tgOn, "켜진 필터 토글");
            Image on = tgOn.Find("face").GetComponent<Image>();
            Assert.AreEqual(UiKit.C("af_toggle_on"), on.color, "켜진 트랙 = catalog af_toggle_on(정본 4768 #35d435)");
            Assert.AreNotEqual(UiKit.C("pp_blue"), on.color, "설정 토글의 pp_blue 가 아니다");
            Assert.Greater(on.color.g, on.color.b + 0.3f, "«초록» 이 이 자리의 뜻이다 — 파랑으로 되돌아가면 이 줄이 먼저 깨진다");

            // 설정 토글은 안 끌려갔다(공용 도우미의 기본값이 그대로인가)
            h.Meta.Popups.HideAll();
            yield return null;
            ProfilePopup.Open(h.Meta);
            yield return null;
            // 토글은 «설정» 갈래에만 선다(`ProfilePopup.Render` 45행). 탭 단추를 이름으로 찾지 않고 그 단추가 부르는 공개 문(`SwitchView`)을 그대로 쓴다 —
            //   런 1017 빨강: 단추 이름은 `settings` 가 아니라 **`tab-settings`**(`Tab()` 61행이 «tab-» 을 붙인다)라 이름으로 찾은 자가 null 을 물었다.
            //   이름에 기대지 않는 쪽이 이 자가 재려는 것(«공용 도우미 기본값이 그대로인가»)과도 맞다.
            ProfilePopup.SwitchView(h.Meta, "settings");
            yield return null; yield return null;
            Popup sp = h.Meta.Popups.Find(ProfilePopup.Name);
            Assert.IsNotNull(sp, "프로필 팝업");
            Assert.AreEqual("settings", ProfilePopup.View, "설정 갈래로 바뀌었다");
            Transform st = FindActive(sp.Root, "toggle");
            Assert.IsNotNull(st, "설정 토글");
            Image sf = st.Find("face").GetComponent<Image>();
            Assert.IsTrue(sf.color == UiKit.C("pp_blue") || sf.color == UiKit.C("pp_gray"),
                "설정 토글은 정본 3107 대로 토큰(pp_gray/pp_blue) 그대로다 — 필터 토글 고침이 이 자리를 끌고 가지 않았다");
            h.Meta.Popups.HideAll();
        }

        /// <summary>T377 13회차 — 던전 목록의 **잠긴** 배너 [열기] 칩. 정본 **8151** `.modal-card.sheet .dg-banner .btn.disabled`
        /// 는 공용 비활성과 **다른 값**을 못박는다(면 `#878e96` · 턱 `#666d75` · 글자 `#3d434a`)고, 바로 위 8149 주석이 까닭까지 적어 뒀다 —
        /// «배너 일러스트 위에서 «유령»으로 읽히던 **회백**을 확실한 비활성 칩으로». 클론은 공용 `Skin.Gray`(`pp_gray` #c4c4c4 / `pp_gray_dk` #9a9a9a)를
        /// 써서 정본이 이미 고친 그 회백을 다시 밟았고, **턱이 면보다 밝아 뒤집혀** 있었다. 표값은 `check_pinned_colors` 가 본다 — 여기는 «자리가 그 키를 쓰는가».</summary>
        [UnityTest]
        public IEnumerator 잠긴_던전_열기_칩은_공용_회색이_아니라_배너_위에서_읽히는_어두운_칩이다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;
            // 새 세이브는 던전이 전부 잠겨 있다 — 그대로 열면 잠긴 갈래를 잰다(해금하면 Blue 로 바뀌어 헛초록이 된다).
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null; yield return null;
            // 뿌리를 **이 화면**으로 좁힌다 — «open» 은 `OfflineButton` 도 쓰는 이름이라 앱 뿌리부터 찾으면 남의 화면 상자를 잴 수 있다(T414 · `check_test_scope` 가 막는다).
            //   ⚠ 런 1027 빨강: `UiRoot.Instance.Sheet` 는 이 시트가 아니다 — `DungeonSheet.Install` 51행이 **`root.PanelHost`** 밑에 «sheet-dungeon» 으로 붙인다.
            //   그 화면을 가리키는 것은 `DungeonSheet.Instance` 다(T414 가 적어 둔 «그 화면의 뿌리» 꼴 · `SkillPetSheet.Instance.…` 와 같다).
            Assert.IsNotNull(DungeonSheet.Instance, "던전 시트");
            Transform open = FindActive(DungeonSheet.Instance.transform, "open");
            Assert.IsNotNull(open, "잠긴 던전의 [열기] 칩");
            // `DungeonPopups.Bordered`(53행)는 **바깥 테 `bg` 안에 면 `face`** 를 넣고, `BottomShade` 가 그 면 안에 턱 `shade` 를 깐다.
            //   곧 이 칩의 «면» 은 `bg` 가 아니라 `bg/face` 다(`bg` 는 `pp_line` 테다).
            Transform faceT = open.Find("bg/face");
            Assert.IsNotNull(faceT, "칩의 면(bg/face) — 바깥 `bg` 는 테다");
            Image face = faceT.GetComponent<Image>();
            Color wantFace = UiKit.C("dg_lock_open"), wantDk = UiKit.C("dg_lock_open_dk");
            Assert.AreEqual(wantFace, face.color, "면 = catalog dg_lock_open(정본 8151 #878e96)");
            Assert.AreNotEqual(UiKit.C("pp_gray"), face.color, "공용 pp_gray(#c4c4c4)가 아니다 — 정본이 «유령 회백» 을 고쳐 둔 자리다");
            Assert.Less(wantFace.r + wantFace.g + wantFace.b, UiKit.C("pp_gray").r + UiKit.C("pp_gray").g + UiKit.C("pp_gray").b,
                "«확실한 비활성 칩» 이 이 자리의 뜻이다 — 표값이 공용 회색만큼 밝아지면 이 줄이 먼저 깨진다");
            // 턱은 면보다 **어두워야** 한다(클론의 pp_gray_dk #9a9a9a 는 면 #c4c4c4 보다 밝아 뒤집혀 있었다)
            Transform shadeT = faceT.Find("shade");
            Assert.IsNotNull(shadeT, "칩의 아래턱(bg/face/shade)");
            Image shade = shadeT.GetComponent<Image>();
            Assert.AreEqual(wantDk, shade.color, "턱 = catalog dg_lock_open_dk(정본 8152 #666d75)");
            Assert.Less(shade.color.r + shade.color.g + shade.color.b, face.color.r + face.color.g + face.color.b,
                "아래턱은 면보다 어둡다 — 클론의 pp_gray_dk 는 면보다 밝아 뒤집혀 있었다");
            Transform lab = open.Find("label");
            Assert.IsNotNull(lab, "칩 글자");
            TMPro.TextMeshProUGUI t = lab.GetComponent<TMPro.TextMeshProUGUI>();
            Assert.AreEqual(UiKit.C("dg_lock_open_ink"), t.color, "글자 = catalog dg_lock_open_ink(정본 8151 #3d434a) — 면만 어둡게 하고 글자를 두면 안 읽힌다");
        }

        /// <summary>이름이 같은 자리를 **전부** 모은다 — «하나만 맞다» 로 지나가지 않게.</summary>
        static System.Collections.Generic.List<Transform> AllDeep(Transform root, string name)
        {
            var found = new System.Collections.Generic.List<Transform>();
            Walk(root, name, found);
            return found;
        }

        static void Walk(Transform t, string name, System.Collections.Generic.List<Transform> found)
        {
            if (!t.gameObject.activeInHierarchy) return;
            if (t.name == name && t.Find("bg") != null) found.Add(t);
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), name, found);
        }

        /// <summary>
        /// T396 10회차 — 산 lock 밖 파일의 잉크 자리 한 묶음(값·정본 줄은 PinnedColorUi.json «_T396_10회차»): 채팅 미리보기 이름·메시지(8069·8070) ·
        /// 리그 내 행 «서버 N»(2355) · 수집 시간(2528) · 플레이어 정보 전투력(7693)·보유 옵션(7698) · 자동 제련 체크(4727·4730 — 상자는 검정 그대로, ✓ 만 초록).
        /// </summary>
        [UnityTest]
        public IEnumerator 채팅_미리보기_리그_플레이어_정보_자동_제련_체크의_잉크는_못박은_값이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ForgeHost fh = ForgeHost.Instance;
            Assert.IsNotNull(UiRoot.Instance, "UiRoot");
            // ① 채팅 미리보기 띠(HUD)
            TMPro.TextMeshProUGUI cn = null, cm = null;
            foreach (TMPro.TextMeshProUGUI t in UiRoot.Instance.App.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {
                if (t.name == "chat-preview-name") cn = t;
                else if (t.name == "chat-preview-msg") cm = t;
            }
            Assert.IsNotNull(cn, "채팅 미리보기 이름"); Assert.IsNotNull(cm, "채팅 미리보기 메시지");
            Assert.AreEqual(PinnedColorUi.C("chat_preview_name_ink"), cn.color, "이름 = 정본 8069 #eef1f5(전엔 chat_name #ff880f)");
            Assert.AreEqual(PinnedColorUi.C("chat_preview_msg_ink"), cm.color, "메시지 = 정본 8070 #aab3c0(전엔 chat_ink #2e2e2e — 어두운 띠 위에 어두운 글자)");
            Assert.AreNotEqual(UiKit.C("chat_name"), cn.color, "이름은 더 이상 채팅 화면의 주황이 아니다");
            Assert.Greater(cm.color.r + cm.color.g + cm.color.b, 1.5f, "메시지는 어두운 띠 위에서 읽히는 밝은 회색이다");

            // ② 리그 — 내 행의 «서버» · 보상 카드의 수집 시간
            LeagueSheet.Open(h);
            yield return null; Canvas.ForceUpdateCanvases();
            Popup lp = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(lp, "리그 시트");
            int meServers = 0, otherServers = 0;
            foreach (TMPro.TextMeshProUGUI t in lp.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {
                if (t.name != "server") continue;
                if (t.color == PinnedColorUi.C("league_server_me_ink")) meServers++;
                else if (t.color == UiKit.C("league_server")) otherServers++;
                else Assert.Fail("서버 글자의 잉크가 표값(me #dce6ff · 남 league_server) 어느 쪽도 아니다: " + t.color);
            }
            // 11회차 — 런 1040: 내 행은 목록 안과 발 띠(고정 내 행)에 **둘** 선다(정본도 `.league-row.me` 가 두 자리) → «하나만» 이 아니라 «하나 이상».
            Assert.GreaterOrEqual(meServers, 1, "내 행(목록 + 발 띠)의 서버 글자는 정본 2355 #dce6ff(전엔 stage_ink 흰색)");
            Assert.Greater(otherServers, 0, "남의 행은 회색 league_server 그대로");
            LeagueSheet.OpenRewards(h);
            yield return null; Canvas.ForceUpdateCanvases();
            Popup lr = PopupLayer.Instance.Find(LeagueSheet.RewardsName);   // 12회차 — 보상 카드는 시트 위 별도 팝업(«league-rewards») · 런 1047 은 시트에서 찾아 null 이었다
            Assert.IsNotNull(lr, "리그 보상 팝업");
            TMPro.TextMeshProUGUI ct = null;
            foreach (TMPro.TextMeshProUGUI t in lr.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                if (t.name == "time" && t.transform.parent != null && t.transform.parent.name == "collect") ct = t;
            Assert.IsNotNull(ct, "수집까지 시간 글자(collect/time)");
            Assert.AreEqual(PinnedColorUi.C("league_collect_time_ink"), ct.color, "수집 시간 = 정본 2528 #1d8f3c(전엔 토큰 pp_green_dk #1f8c34)");
            Assert.AreNotEqual(UiKit.C("pp_green_dk"), ct.color, "토큰 값과 다르다(정본이 리터럴로 못박은 자리)");
            h.Popups.Hide(LeagueSheet.RewardsName);
            h.Popups.Hide(LeagueSheet.Name);
            yield return null;

            // ③ 플레이어 정보 — 전투력 조각 · 보유 옵션 줄
            var saved = PlayerInfoPopup.PreviewStart;
            PlayerInfoPopup.PreviewStart = null;
            try
            {
                PlayerInfoPopup.Open(h);
                yield return null; Canvas.ForceUpdateCanvases();
                Popup pp = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
                Assert.IsNotNull(pp, "플레이어 정보");
                int cpPieces = 0, subLines = 0;
                foreach (TMPro.TextMeshProUGUI t in pp.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                {
                    Transform par = t.transform.parent;
                    if (par != null && par.name == "cp") { Assert.AreEqual(PinnedColorUi.C("pinfo_cp_ink"), t.color, "전투력 조각 = 정본 7693 #ff880f(전엔 pp_ink)"); cpPieces++; }
                    else if (t.name == "sub") { Assert.AreEqual(PinnedColorUi.C("pinfo_subs_ink"), t.color, "보유 옵션 줄 = 정본 7698 #3a3a3a(전엔 pp_ink #17181a)"); subLines++; }
                }
                Assert.Greater(cpPieces, 0, "전투력 글 조각이 있다");
                Assert.IsTrue(subLines > 0 || pp.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Length > 0, "보유 옵션 줄(없으면 «없음» 안내만)");
                h.Popups.Hide(PlayerInfoPopup.Name);
                yield return null;
            }
            finally { PlayerInfoPopup.PreviewStart = saved; }

            // ④ 자동 제련 체크 — 상자는 두 상태 다 #17181a · ✓ 만 #23c552(정본 4722 주석 «상자를 통째로 초록으로 채우던 종전 구현은 원본과 다른 물건»)
            fh.S.BestChapter = 3; fh.S.BestStage = 1; fh.Pull();
            ForgeAutoPopup.Open(fh);
            yield return null; Canvas.ForceUpdateCanvases();
            Popup ap = PopupLayer.Instance.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(ap, "자동 제련 팝업");
            int boxes = 0, marks = 0;
            Color face = PinnedColorUi.C("af_check_face"), green = new Color(0.14f, 0.77f, 0.32f, 1f);
            foreach (Transform box in AllNamed(ap.Root, "box"))
            {
                Transform parent = box.parent;
                if (parent == null || !(parent.name == "af-check-continue" || parent.name == "check")) continue;
                Image bi = box.GetComponent<Image>();
                if (bi == null) { Transform f = box.Find("face"); bi = f != null ? f.GetComponent<Image>() : null; }
                if (bi == null) continue;
                boxes++;
                Assert.AreNotEqual(green, bi.color, "체크 상자는 더 이상 초록 판이 아니다(" + parent.name + ")");
                Transform mk = parent.Find("mark");
                if (mk != null && mk.gameObject.activeSelf) { Assert.AreEqual(PinnedColorUi.C("af_check_on_ink"), mk.GetComponent<Image>().color, "✓ = 정본 4730 #23c552"); marks++; }
            }
            Assert.Greater(boxes, 0, "체크 상자가 있다(계속하기 + 필터 행)");
            Assert.Greater(marks, 0, "켜진 체크가 하나는 있다(시대 막대 기본 켜짐) — 그 체크가 정본 #23c552");
            Assert.AreEqual(face, PinnedColorUi.C("af_check_face"), "상자 표값 = #17181a");
            fh.Meta.Popups.Hide(ForgeAutoPopup.Name);
            yield return null;
        }

        static System.Collections.Generic.List<Transform> AllNamed(Transform root, string name)
        {
            var found = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) found.Add(t);
            return found;
        }
    }
}
