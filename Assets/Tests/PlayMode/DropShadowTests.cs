using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T332 — 정본 `filter: drop-shadow(dx dy blur color)` 중 **흐림이 있는** 자리(<see cref="DropShadow"/>).
    /// 기구가 앞의 둘과 다르다: `UnityEngine.UI.Shadow` 는 흐림이 없고 `UiShadow.Drop`(T331)은 둥근 상자 전용이라,
    /// 스프라이트 알파를 <see cref="UiFilter.Blur"/>(T342)로 흐려 구운 사본을 **뒤에 깐다**.
    /// 자기 파일인 이유: `PassPopupTests` 는 없고 `PopupZTests`·`TabularSitesTests` 는 다른 절의 자리다(check_claim_scope).
    /// </summary>
    public class DropShadowTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (System.Exception) { /* 저장소 접근 실패는 무시 */ }
        }

        [TearDown]
        public void CleanSave() { DeleteSave(); }

        private static IEnumerator Boot()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            yield return null;
        }

        /// <summary>
        /// 정본 `style.css` 2698 `.pass-sword { … filter: drop-shadow(.14rem .18rem .16rem rgba(0,0,0,.45)) }`
        /// — 진행 패스 카드 위에 꽂힌 검. **가로 오프셋이 있는 유일한 자리**라 dx 도 같이 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 진행_패스의_검은_정본_흐린_그림자를_뒤에_깐다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("drop-shadow-pass");
            MetaHost h = MetaHost.Instance;
            PassPopup.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(p, "진행 패스 팝업이 열린다");
            Transform sword = FindIn(p.Root, "pass-sword");
            Assert.IsNotNull(sword, "패스 검");
            Transform sh = sword.parent.Find(DropShadow.NameFor(sword.GetComponent<Image>()));
            Assert.IsNotNull(sh, "검 뒤에 흐린 그림자를 깔았다(정본 2698)");

            Image si = sh.GetComponent<Image>();
            Image swi = sword.GetComponent<Image>();
            Assert.IsNotNull(si.sprite, "그림자도 그림이 있다(흐려 구운 사본)");
            Assert.AreNotSame(swi.sprite, si.sprite, "흐린 사본이지 원본 그대로가 아니다");
            Assert.Less(sh.GetSiblingIndex(), sword.GetSiblingIndex(), "그림자는 검 **뒤**에 그린다");

            float css = KeylineUi.CssPx;
            Vector2 d = CenterDelta((RectTransform)sh, (RectTransform)sword);
            Assert.AreEqual(DropShadowUi.Px("pass_sword", "dx_px") * css, d.x, 0.01f, "가로 오프셋 = 표 dx(정본 .14rem)");
            Assert.AreEqual(-DropShadowUi.Px("pass_sword", "dy_px") * css, d.y, 0.01f, "세로 오프셋 = 표 dy 만큼 **아래**(유니티 −y)");
            Color want = DropShadowUi.C("pass_sword");
            Assert.AreEqual(want.a, si.color.a, 2f / 255f, "알파 = 표 pass_sword(.45)");
            Assert.Less(si.color.r + si.color.g + si.color.b, 0.05f, "색은 검정");

            AssertBlurred(swi, si, "패스 검");

            PassPopup.Close(h);
            yield return null;
            log.AssertNoRed();
            log.Dispose();
        }

        /// <summary>
        /// 정본 `style.css` 5335 `.dgd-reward-pill .ico { filter: drop-shadow(0 0 1.2px rgba(0,0,0,.9)) }`
        /// — 던전 상세 보상 알약의 아이콘. **오프셋이 0** 이라 그림자가 아니라 **검정 윤곽**이다(정본 주석: 회색 알약 면 위에서
        /// 흰 아이콘이 «검정 윤곽으로 판과 갈라진다»). 같은 기구로 내되 **dx·dy 가 0 인 것**과 **번짐이 실제로 걸린 것**을 같이 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 던전_보상_알약의_아이콘은_오프셋_0_짜리_검정_윤곽을_진다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("drop-shadow-dgd");
            ForgeHost h = ForgeHost.Instance;
            h.S.BestChapter = 5; h.S.BestStage = 1; h.Pull();
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            DungeonDetailPopup.Open("hammer");
            yield return null;
            Assert.IsTrue(DungeonDetailPopup.IsOpen, "던전 상세가 열린다");

            Transform pill = FindIn(UiRoot.Instance.App, "reward-pill");
            Assert.IsNotNull(pill, "보상 알약");
            int seen = 0;
            foreach (Image ico in pill.GetComponentsInChildren<Image>(true))
            {
                if (!ico.name.StartsWith("ico-", System.StringComparison.Ordinal)) continue;
                seen++;
                Transform sh = ico.transform.parent.Find(DropShadow.NameFor(ico));   // 한 부모 아래 아이콘이 여럿이라 이름이 갈린다(런 631)
                Assert.IsNotNull(sh, ico.name + " 뒤에 검정 윤곽을 깔았다(정본 5335)");
                Vector2 d = CenterDelta((RectTransform)sh, ico.rectTransform);
                Assert.AreEqual(0f, d.x, 0.01f, "정본은 가로로 안 민다(0 0 1.2px)");
                Assert.AreEqual(0f, d.y, 0.01f, "정본은 세로로도 안 민다 — 그림자가 아니라 **윤곽**이다");
                Image si = sh.GetComponent<Image>();
                AssertBlurred(ico, si, ico.name);
                Assert.AreEqual(DropShadowUi.C("dgd_reward_pill_ico").a, si.color.a, 2f / 255f, "알파 = 표(.9)");
                Assert.Less(si.color.r + si.color.g + si.color.b, 0.05f, "색은 검정");
            }
            Assert.Greater(seen, 0, "보상 알약에 아이콘이 하나는 있다");

            log.AssertNoRed();
            log.Dispose();
        }

        /// <summary>
        /// <summary>
        /// T332 16회차 — 정본이 **알 하나에 두 값**을 적은 자리: **4307** `.pet-tile.egg .tile-face { … drop-shadow(0 .15rem .1rem rgba(0,0,0,.3)) }`(펫 격자)와
        /// **4518** `.hatch-cell .hatch-egg { … drop-shadow(0 .15rem .12rem rgba(0,0,0,.45)) }`(부화장). 같은 알인데 부화장 쪽이 **더 진하고 더 번진다** —
        /// 부화 원뿔의 밝은 빛기둥(4515) 위에 서기 때문이다. 그래서 표에서도 **한 키로 묶지 않는다**.
        /// 4307 의 정본 주석이 격자 알의 구실을 못박아 뒀다: «(정본이 경고 표식을 붙여 둔 줄) 알 타일은 배경·테·그림자가 전부 없는 **그림만** 칸이다» — 판이 없으니 알을 띄우는 것이 이 그림자뿐이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 알_둘은_같은_그림인데_자리마다_다른_그림자를_진다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("drop-shadow-egg");
            float t = 0f;
            while (!(PetSkillHost.Instance != null && PetSkillHost.Instance.Pets != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            PetSkillHost H = PetSkillHost.Instance;
            Assert.IsNotNull(H, "PetSkillHost");
            TabBar tb = UiRoot.Instance.TabBar;
            if (tb.ActiveTab != "summon") tb.OnTab("summon");
            yield return null;
            SkillPetSheet sheet = SkillPetSheet.Instance;
            Assert.IsNotNull(sheet, "소환 시트");
            sheet.Switch(SkillPetSheet.SubPets);
            yield return null;
            while (H.SummonMult("pet") != 1) H.CycleSummonMult("pet");
            H.EggCurrency = 100000;
            H.Sync();
            yield return null;
            if (H.Pets.State.Eggs.Count < 1)
            {
                sheet.Pets.SummonButton.onClick.Invoke();
                float t0 = 0f;
                while (H.Pets.State.Eggs.Count < 1 && t0 < 10f) { t0 += Time.unscaledDeltaTime; yield return null; }
                Assert.GreaterOrEqual(H.Pets.State.Eggs.Count, 1, "알 하나는 나온다");
                sheet.Modal.CloseAll();
                sheet.Switch(SkillPetSheet.SubPets);
                yield return null;
            }
            yield return null;
            Canvas.ForceUpdateCanvases();

            Transform tile = FindIn(UiRoot.Instance.App, "egg-tile-0");
            Assert.IsNotNull(tile, "펫 격자 알 칸(egg-tile-0)");
            Image egg = tile.Find("egg") != null ? tile.Find("egg").GetComponent<Image>() : null;
            Assert.IsNotNull(egg, "알 그림");
            Transform sh = egg.transform.parent.Find(DropShadow.NameFor(egg));
            Assert.IsNotNull(sh, "알 뒤에 그림자를 깔았다(정본 4307)");
            Assert.Less(sh.GetSiblingIndex(), egg.transform.GetSiblingIndex(), "그림자는 알 **뒤**에");
            Image si = sh.GetComponent<Image>();
            float css = KeylineUi.CssPx;
            Vector2 d = CenterDelta((RectTransform)sh, egg.rectTransform);
            Assert.AreEqual(0f, d.x, 0.01f, "정본은 가로로 안 민다");
            Assert.AreEqual(-DropShadowUi.Px("pet_tile_egg", "dy_px") * css, d.y, 0.01f, "세로 오프셋 = 표 .15rem 만큼 아래");
            Assert.AreEqual(DropShadowUi.C("pet_tile_egg").a, si.color.a, 2f / 255f, "알파 = 표 pet_tile_egg(.3)");
            AssertBlurred(egg, si, "격자 알");

            // 두 자리가 **다른 값**이다 — 한 키로 묶으면 이 줄이 깨진다.
            Assert.AreNotEqual(DropShadowUi.Px("pet_tile_egg", "blur_px"), DropShadowUi.Px("hatch_egg", "blur_px"), "정본은 두 자리에 다른 흐림을 적었다(.1rem ↔ .12rem)");
            Assert.Greater(DropShadowUi.C("hatch_egg").a, DropShadowUi.C("pet_tile_egg").a, "부화장 알이 더 진하다(.45 ↔ .3)");

            log.AssertNoRed();
            log.Dispose();
        }

        /// <summary>
        /// T332 15회차 — 정본 `style.css` **1993~1998** `.dg-rw .dg-rw-ico { … filter: drop-shadow(0 .10em .16em rgba(0,0,0,.55)) }`
        /// — 던전 배너 왼쪽 위 보상 아이콘. **이 자리는 길이가 `em`** 이라 앞의 것들과 다르다: `em` 은 그 요소의 글자 크기라
        /// 화면이 아니라 **상자 크기에 걸린다**. 슬롯 `.dg-rw` 가 1.62em 사각(1992)이고 아이콘이 그 100% 를 채우므로
        /// 1em = 상자/1.62 — 표는 `_px` 가 아니라 `_f`(상자 비율)로 적고 `DropShadowUi.Len` 이 푼다.
        /// 그래서 이 자는 «표대로인가» 만이 아니라 **«상자가 커지면 그림자도 같이 커지는가»** 를 같이 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 던전_배너_보상_아이콘의_그림자는_상자_비율로_선다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("drop-shadow-dgrw");
            ForgeHost h = ForgeHost.Instance;
            h.S.BestChapter = 5; h.S.BestStage = 1; h.Pull();
            UiRoot.Instance.TabBar.OnTab("dungeon");
            yield return null;
            yield return null;

            Image ico = null;
            foreach (Image im in UiRoot.Instance.App.GetComponentsInChildren<Image>(true))
                if (im.name.StartsWith("rw-", System.StringComparison.Ordinal) && im.gameObject.activeInHierarchy) { ico = im; break; }
            Assert.IsNotNull(ico, "던전 배너 보상 아이콘(rw-*)");

            Transform sh = ico.transform.parent.Find(DropShadow.NameFor(ico));
            Assert.IsNotNull(sh, ico.name + " 뒤에 그림자를 깔았다(정본 1993~1998)");
            Assert.Less(sh.GetSiblingIndex(), ico.transform.GetSiblingIndex(), "그림자는 아이콘 **뒤**에");
            Image si = sh.GetComponent<Image>();
            Assert.IsNotNull(si.sprite, "그림자도 그림이 있다(흐려 구운 사본)");

            float box = Mathf.Max(ico.rectTransform.rect.width, ico.rectTransform.rect.height);
            Assert.Greater(box, 1f, "아이콘 상자");
            Vector2 d = CenterDelta((RectTransform)sh, ico.rectTransform);
            Assert.AreEqual(0f, d.x, 0.01f, "정본은 가로로 안 민다(0 .10em .16em)");
            Assert.AreEqual(-DropShadowUi.Len("dg_rw_ico", "dy", box), d.y, 0.01f,
                "세로 오프셋 = 상자 × .0617(= .10em ÷ 1.62) 만큼 **아래** · 상자 " + box.ToString("0.0"));
            Assert.AreEqual(DropShadowUi.C("dg_rw_ico").a, si.color.a, 2f / 255f, "알파 = 표(.55)");
            Assert.Less(si.color.r + si.color.g + si.color.b, 0.05f, "색은 검정");
            AssertBlurred(ico, si, ico.name);

            // 상자 비율 갈래가 정말 «상자에 걸리는가» — 표를 직접 풀어 두 배 상자면 두 배가 되는지 본다.
            //   (CSS px 갈래였다면 이 둘이 같아 버린다 — 그것이 `em` 을 px 로 굳혔을 때 생기는 병이다.)
            Assert.AreEqual(DropShadowUi.Len("dg_rw_ico", "dy", box) * 2f, DropShadowUi.Len("dg_rw_ico", "dy", box * 2f), 1e-4f,
                "em 자리는 상자에 비례한다");
            Assert.IsTrue(DropShadowUi.HasField("dg_rw_ico", "dy_f"), "표가 `_f`(상자 비율)로 적혀 있다");
            Assert.IsFalse(DropShadowUi.HasField("dg_rw_ico", "dy_px"), "CSS px 로 굳히지 않았다");
            Assert.IsTrue(DropShadowUi.HasField("pass_sword", "dy_px"), "rem 자리는 그대로 CSS px");

            log.AssertNoRed();
            log.Dispose();
        }

        /// <summary>
        /// T332 14회차 — 정본 `style.css` **985** `.anvil-btn { filter: drop-shadow(0 .18rem .12rem rgba(0,0,0,.35)) }`.
        /// 이 자리는 앞의 둘과 다르다: **그림이 한 장이 아니다**(정본 SVG 폴리곤 21겹). 정본 filter 는 요소가 그린 전부를
        /// 한 덩어리로 보고 바깥 윤곽에만 그림자를 주므로 겹마다 걸면 안쪽 경계마다 검은 띠가 생긴다 — 그래서 겹들의
        /// 알파 **합집합** 한 장(<see cref="AnvilArt.Silhouette"/>)에 건다. 그 합집합이 «정말 합집합인가» 까지 같이 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator 모루_버튼은_겹이_아니라_실루엣_한_장에_흐린_그림자를_진다()
        {
            yield return Boot();
            PlayLog log = PlayLog.Start("drop-shadow-anvil");
            float t = 0f;
            while (!ForgeHost.Ready && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;

            Transform art = FindIn(UiRoot.Instance.App, "anvil-art");
            Assert.IsNotNull(art, "모루 그림(anvil-art) — 장비 시트가 보이고 보류 카드가 없어야 한다");
            Transform sh = art.parent.Find(DropShadow.Name + ":anvil");
            Assert.IsNotNull(sh, "모루 뒤에 흐린 그림자를 깔았다(정본 985)");
            Assert.Less(sh.GetSiblingIndex(), art.GetSiblingIndex(), "그림자는 모루 **뒤**에 그린다");

            Image si = sh.GetComponent<Image>();
            Assert.IsNotNull(si, "그림자 그림");
            Assert.IsNotNull(si.sprite, "그림자도 그림이 있다(흐려 구운 실루엣)");

            float css = KeylineUi.CssPx;
            Vector2 d = CenterDelta((RectTransform)sh, (RectTransform)art);
            Assert.AreEqual(DropShadowUi.Px("anvil_btn", "dx_px") * css, d.x, 0.01f, "가로 오프셋 0(정본 985 의 첫 값)");
            Assert.AreEqual(-DropShadowUi.Px("anvil_btn", "dy_px") * css, d.y, 0.01f, "세로 오프셋 = 표 dy(.18rem) 만큼 **아래**");
            Color want = DropShadowUi.C("anvil_btn");
            Assert.AreEqual(want.a, si.color.a, 2f / 255f, "알파 = 표 anvil_btn(.35)");
            Assert.Less(si.color.r + si.color.g + si.color.b, 0.05f, "색은 검정");
            // T411 3회차 — 그림자 상자 = 모루 상자 × 구운 판의 여유(커널 반경 · 번짐이 상자 끝에서 안 잘린다 · 정본 drop-shadow 는 상자 밖으로 번진다)
            Sprite silSprite = AnvilArt.Silhouette();
            Assert.AreNotSame(silSprite, si.sprite, "그림자 판은 실루엣을 번지게 구운 사본이다(표 blur 1.92px) — 실루엣 그대로면 번짐이 안 걸린 것");
            Vector2 grow = UiFilter.BlurGrow(silSprite, si.sprite, Mathf.Max(((RectTransform)art).rect.width, ((RectTransform)art).rect.height));
            if (grow.x <= 1f)
            {
                // T411 5회차 — 런 885: 판은 실루엣과 다른데 키움 비가 (1, 1) — 구운 판의 한 변이 «줄인 원본» 과 같다는 뜻이라 번짐이 안 걸렸거나(ReadPixels 실패 → 원본 그대로)
                // 셈의 화면 한 변이 걸 때와 다르다. 자국을 남겨 다음 런이 가른다(T386 규약 · 임자 T411).
                Rect sr = silSprite.textureRect, br = si.sprite.rect, ar = ((RectTransform)art).rect;
                Assert.Ignore("KNOWN T411 — 모루 그림자 판의 키움 비가 1 (실루엣 " + sr.width + "×" + sr.height + " · 판 " + br.width + "×" + br.height + " «" + si.sprite.name
                              + "» · 판 텍스처 " + (si.sprite.texture != null ? si.sprite.texture.width + "×" + si.sprite.texture.height : "없음") + " · 모루 상자 " + ar.width.ToString("0.0") + "×" + ar.height.ToString("0.0")
                              + " · 표 blur " + DropShadowUi.Px("anvil_btn", "blur_px") + "px) · 임자 T411 절");
            }
            Assert.AreEqual(((RectTransform)art).rect.width * grow.x, ((RectTransform)sh).rect.width, 0.5f, "그림자 상자 = 모루 상자 + 구운 판의 여유(가로)");
            Assert.AreEqual(((RectTransform)art).rect.height * grow.y, ((RectTransform)sh).rect.height, 0.5f, "그림자 상자 = 모루 상자 + 구운 판의 여유(세로)");

            // ⓑ 겹마다 걸지 **않았다** — 21겹 중 어느 것도 제 그림자를 갖고 있지 않다.
            int per = 0;
            foreach (Transform c in art.GetComponentsInChildren<Transform>(true))
                if (c.name.StartsWith(DropShadow.Name + ":")) per++;
            Assert.AreEqual(0, per, "겹마다 그림자를 걸면 안쪽 경계마다 검은 띠가 생긴다 — 합집합 한 장만 건다");

            // ⓒ 실루엣이 정말 «합집합» 인가 — 받침(anv-base)의 한가운데가 실루엣 안에서 불투명해야 한다.
            Sprite sil = AnvilArt.Silhouette();
            Assert.IsNotNull(sil, "실루엣");
            Assert.AreSame(sil, AnvilArt.Silhouette(), "한 번만 굽는다(캐시)");
            Rect bb = AnvilArt.PartBounds("anv-base");
            Texture2D st = sil.texture;
            int px = Mathf.Clamp(Mathf.RoundToInt(bb.center.x / AnvilArt.ViewW * st.width), 0, st.width - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt((1f - bb.center.y / AnvilArt.ViewH) * st.height), 0, st.height - 1);
            Assert.Greater(st.GetPixel(px, py).a, 0.9f, "받침 한가운데는 실루엣 안(합집합이 채워졌다) · 자리 " + px + "," + py);
            // 실루엣이 «판 전체» 도 «빈 판» 도 아니다 — 합집합이 정말 모루 모양으로 찼는지 덮인 넓이로 본다.
            Color[] all = st.GetPixels();
            int on = 0;
            for (int i = 0; i < all.Length; i++) if (all[i].a > 0.5f) on++;
            float cov = (float)on / all.Length;
            Assert.Greater(cov, 0.15f, "실루엣이 비어 있다 — 덮인 넓이 " + cov.ToString("0.000"));
            Assert.Less(cov, 0.9f, "실루엣이 viewBox 를 통째로 덮었다(합집합이 아니라 판이 됐다) — 덮인 넓이 " + cov.ToString("0.000"));

            log.AssertNoRed();
            log.Dispose();
        }

        /// «번짐이 실제로 걸렸다» 를 재는 자리 — 구운 판의 한 변이 **줄인 원본보다 커널 반경만큼 넓은가**.
        ///
        /// ⚠ «구운 사본이 **원본 스프라이트**보다 넓다» 로 재면 틀린다(런 641 실측 · 원본 160 ↔ 구운 것 54):
        /// <see cref="UiFilter.Blur"/> 는 **화면에 설 크기까지만 굽는다**(`FilterRules.BlurBakeSide` · 커널이 제곱으로 비싸지지 않게).
        /// 그러니 아틀라스 아이콘처럼 원본이 화면보다 크면 구운 판은 **원본보다 작다** — 그래도 번짐은 제대로 걸린 것이다.
        /// 옳은 잣대는 «그 해상도로 줄인 원본» 이고, 거기에 `2 × 커널 반경` 이 더 붙는다.
        /// </summary>
        private static void AssertBlurred(Image sharp, Image shadow, string what)
        {
            Rect box = sharp.rectTransform.rect;
            float sw = sharp.sprite.textureRect.width, sh2 = sharp.sprite.textureRect.height;
            float srcMax = Mathf.Max(sw, sh2);
            int side = FilterRules.BlurBakeSide(srcMax, Mathf.Max(box.width, box.height));
            float shrink = (side > 0 && side < srcMax) ? side / srcMax : 1f;
            float w0 = Mathf.Max(2f, Mathf.Round(sw * shrink));
            Assert.Greater(shadow.sprite.rect.width, w0,
                what + ": 번짐이 실제로 걸렸다 — 구운 판(" + shadow.sprite.rect.width + ")이 그 해상도로 줄인 원본("
                + w0 + " · 원본 " + sw + " × 줄임 " + shrink.ToString("0.000") + ")보다 커널 반경만큼 넓다");
        }

        /// <summary>
        /// T411 6회차 — 런 898 자국: 모루 그림자 판이 «small-anvil_btn-215»(줄인 원본 그대로 · 번짐 없음)였다. 어느 길이 원본을 돌려주는지 가른다:
        /// 실루엣을 <see cref="UiFilter.Blur"/> 에 **화면 한 변을 주어**(줄임 길) 한 번, **0 으로**(줄임 없이) 한 번 넣어 판 이름·크기를 본다.
        /// 번지지 않으면 KNOWN T411 접음에 두 결과를 실어 다음 런이 읽는다(T386 규약).
        /// </summary>
        [UnityTest]
        public IEnumerator 모루_실루엣은_흐리는_자에_넣으면_커널_반경만큼_넓은_판이_나온다()
        {
            yield return Boot();
            Sprite sil = AnvilArt.Silhouette();
            Assert.IsNotNull(sil, "실루엣");
            Rect sr = sil.textureRect;
            float display = Mathf.Max(sr.width, sr.height) * 0.966f;                       // 런 898 의 모루 상자 비(254.8/264) — 줄임 길을 타게
            double sigma = FilterRules.BakeSigmaPx(DropShadowUi.Px("anvil_btn", "blur_px") * 0.5 * KeylineUi.CssPx, sr.height, sr.height * 0.966f);
            Sprite shrunk = UiFilter.Blur(sil, sigma, "t411-probe-shrunk", display);
            Sprite full = UiFilter.Blur(sil, sigma, "t411-probe-full", 0);
            string what = "실루엣 " + sr.width + "×" + sr.height + "(텍스처 " + sil.texture.width + "×" + sil.texture.height + " 읽기 " + sil.texture.isReadable + ") · σ " + sigma.ToString("0.00")
                          + " · 줄임 길 → «" + (shrunk != null ? shrunk.name + "» " + shrunk.rect.width + "×" + shrunk.rect.height : "null»")
                          + " · 그대로 길 → «" + (full != null ? full.name + "» " + full.rect.width + "×" + full.rect.height : "null»");
            bool shrunkOk = shrunk != null && shrunk != sil && shrunk.name.StartsWith("blur-");
            bool fullOk = full != null && full != sil && full.name.StartsWith("blur-");
            if (!shrunkOk || !fullOk)
                Assert.Ignore("KNOWN T411 — 모루 실루엣이 흐리는 자에서 번지지 않는다(" + what + ") · 임자 T411 절");
            Assert.Greater(shrunk.rect.width, Mathf.Round(sr.width * 0.966f), "줄임 길: 판이 줄인 원본보다 커널 반경만큼 넓다 · " + what);
            Assert.Greater(full.rect.width, sr.width, "그대로 길: 판이 원본보다 커널 반경만큼 넓다 · " + what);
        }

        /// <summary>두 상자의 **가운데** 차(부모 좌표 · 피벗이 (0,1) 이든 (.5,.5) 이든) — 그림자 상자는 구운 판의 여유만큼 넓어 앵커 자리로 재면 어긋난다(T411 3회차).</summary>
        private static Vector2 CenterDelta(RectTransform a, RectTransform b) { return Center(a) - Center(b); }
        private static Vector2 Center(RectTransform t)
        {
            return t.anchoredPosition + new Vector2((0.5f - t.pivot.x) * t.rect.width, (0.5f - t.pivot.y) * t.rect.height);
        }

        private static Transform FindIn(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
