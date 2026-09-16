using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Forging;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T351 — 정본 `overflow` 축의 글자 자르기·말줄임: 표 <c>TextClampUi.json</c> 의 자리마다 «허용 줄 수» 가 서 있고,
    /// <see cref="TextClamp.Apply"/> 를 건 글자는 긴 이름을 넣어도 **줄 수 ≤ 표 · 폭 ≤ 상자 · 마지막 글자가 …** 이다.
    /// 자기 파일인 이유: 세 자리(프로필 칸 · 모루 이름 · 소환 이름)는 각 파일 lock 뒤라 자리 자는 그때 붙는다 — 이 자는 도우미와 표를 본다.
    /// </summary>
    public class TextClampTests
    {
        const string LongName = "[지하 세계] 망령의 활 — 아주 아주 길어서 한 칸에 절대로 안 들어가는 장비 이름 하나 둘 셋 넷 다섯";

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
            while (!(MetaHost.Ready && UiRoot.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다 (SaveIo → meta.json)");
            Assert.IsNotNull(UiRoot.Instance, "UiRoot 가 서지 않았다");
            yield return null;
        }

        static TextMeshProUGUI Make(RectTransform host, string site, string text, float w)
        {
            TextMeshProUGUI t = UiKit.Text(host, "t351-" + site, TextKind.Sub, text, "pp_ink", TextAlignmentOptions.TopLeft);
            TextClamp.Apply(t, site);
            UiKit.Place(t.rectTransform, 0f, 0f, w, TextClamp.BoxHeight(t, site));   // 높이가 곧 클램프 — 글꼴 지표로 잰다(런 528: 1.25 배는 줄을 통째로 버렸다)
            return t;
        }

        /// <summary>보이는 글자 중에 …(U+2026)이 있는가 — TMP 는 말줄임을 끼워 넣을 때 characterCount 가 그 글자를 가리키지 않는다
        /// (런 535 실측: characterInfo[characterCount-1].character 가 빈 글자). 그래서 글자 정보 배열을 끝까지 훑는다.</summary>
        static bool HasEllipsis(TextMeshProUGUI t)
        {
            TMP_CharacterInfo[] ci = t.textInfo.characterInfo;
            if (ci == null) return false;
            for (int i = 0; i < ci.Length; i++) if (ci[i].isVisible && ci[i].character == '\u2026') return true;
            return false;
        }

        [UnityTest]
        public IEnumerator 표의_세_자리가_서_있고_줄_수는_정본대로다()
        {
            yield return Boot();
            Assert.AreEqual(1, TextClamp.Lines("profile_field"), "3052 .profile-field 한 줄 말줄임");
            Assert.AreEqual(1, TextClamp.Lines("held_name"), "1005 .held-name 한 줄 말줄임");
            Assert.AreEqual(2, TextClamp.Lines("sr_name"), "7048 .sr-name > span -webkit-line-clamp: 2");
            Assert.Greater(TextClamp.LineHeightF(), 0f);
            Assert.GreaterOrEqual(TextClamp.SlackF(), 0f);
            foreach (string s in TextClamp.Sites()) Assert.GreaterOrEqual(TextClamp.Lines(s), 1, s);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => TextClamp.Lines("no-such-site"), "표에 없는 자리는 던진다 — 조용히 넘치지 않는다");
        }

        [UnityTest]
        public IEnumerator 한_줄_자리는_긴_이름을_한_줄로_자르고_끝에_말줄임표를_단다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t351-host-1");
            try
            {
                float w = PopupKit.Rem * 6f;
                UiKit.Place(host, 0f, 0f, w, PopupKit.Rem * 3f);
                TextMeshProUGUI t = Make(host, "profile_field", LongName, w);
                yield return null;
                t.ForceMeshUpdate();
                Assert.AreEqual(TextWrappingModes.NoWrap, t.textWrappingMode, "정본 white-space: nowrap");
                Assert.AreEqual(TextOverflowModes.Ellipsis, t.overflowMode, "정본 text-overflow: ellipsis");
                Assert.AreEqual(1, t.textInfo.lineCount, "한 줄");
                Assert.Less(t.textInfo.characterCount, LongName.Length, "잘렸다");
                Assert.IsTrue(HasEllipsis(t), "잘린 끝에 …(U+2026)이 보인다");
            }
            finally { Object.Destroy(host.gameObject); }
        }

        [UnityTest]
        public IEnumerator 두_줄_자리는_두_줄까지만_그리고_짧은_이름은_안_자른다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t351-host-2");
            try
            {
                float w = PopupKit.Rem * 6f;
                UiKit.Place(host, 0f, 0f, w, PopupKit.Rem * 4f);
                TextMeshProUGUI two = Make(host, "sr_name", LongName, w);
                yield return null;
                two.ForceMeshUpdate();
                Assert.AreEqual(TextWrappingModes.Normal, two.textWrappingMode, "두 줄 클램프는 줄바꿈을 허용한다");
                Assert.AreEqual(2, two.textInfo.lineCount, "-webkit-line-clamp: 2");
                Assert.Less(two.textInfo.characterCount, LongName.Length, "둘째 줄 뒤는 버린다");
                Assert.IsTrue(HasEllipsis(two), "둘째 줄 끝에 …(U+2026)이 보인다");

                const string Short = "망령의 활";
                TextMeshProUGUI one = Make(host, "profile_field", Short, w);
                yield return null;
                one.ForceMeshUpdate();
                Assert.AreEqual(Short.Length, one.textInfo.characterCount, "들어가는 이름은 한 자도 안 버린다");
                Assert.IsFalse(HasEllipsis(one), "안 잘린 이름엔 … 이 없다");
            }
            finally { Object.Destroy(host.gameObject); }
        }

        /// <summary>T423 1회차 — **줄높이를 표에서 받은**(T354) 두 줄 자리도 상자가 둘째 줄을 담는가.
        /// CSS 줄상자는 «`line-height` × 줄 수» 지만 **TMP 의 첫 줄은 pitch 가 아니라 face(NotoSansKR 1.448em)를 먹는다** —
        /// 정본 `.sr-name` 이 «두 줄이 딱 드는 높이» 로 적어 둔 2.36em(= 1.18 × 2)을 그대로 주면 TMP 는 둘째 줄을 통째로 버린다.
        /// 그래서 <see cref="TextClamp.BoxHeight"/> 는 `face + (줄 수 − 1) × pitch` 로 잰다.</summary>
        [UnityTest]
        public IEnumerator 줄높이를_표에서_받은_두_줄_자리도_상자가_둘째_줄을_담는다()
        {
            yield return Boot();
            RectTransform host = UiKit.Box(UiRoot.Instance.App, "t423-host");
            try
            {
                float w = PopupKit.Rem * 6f;
                UiKit.Place(host, 0f, 0f, w, PopupKit.Rem * 6f);
                TextMeshProUGUI t = UiKit.Text(host, "t423-sr-name", TextKind.Sub, LongName, "pp_ink", TextAlignmentOptions.TopLeft);
                TextClamp.Apply(t, "sr_name");
                double r = LineHeight.Apply(t, "sr_name_lh");           // 정본 7033 `.sr-name { line-height: 1.18 }`
                Assert.AreEqual(1.18, LineHeight.Table.Get("sr_name_lh"), 1e-9, "정본 7033 — 표가 그 값을 쥔다");

                float face = TextClamp.LineHeight(t), pitch = TextClamp.Pitch(t);
                Assert.AreEqual((float)(r * t.fontSize), pitch, 0.05f, "pitch = 표 배수 × 글자 크기(TMP lineSpacing 은 em/100)");
                Assert.Less(pitch, face, "정본 1.18 은 글꼴 face 1.448 보다 좁다 — 그래서 둘이 갈린다");

                float want = (face + pitch) * (1f + TextClamp.SlackF());
                Assert.AreEqual(want, TextClamp.BoxHeight(t, "sr_name"), 0.05f, "첫 줄은 face · 둘째 줄부터 pitch");
                float old = 2f * face * (1f + TextClamp.SlackF());
                Assert.AreNotEqual(old, TextClamp.BoxHeight(t, "sr_name"), "종전 셈(줄 수 × face)과는 다른 값이라야 이 회차가 뜻이 있다");

                UiKit.Place(t.rectTransform, 0f, 0f, w, TextClamp.BoxHeight(t, "sr_name"));
                yield return null;
                t.ForceMeshUpdate();
                Assert.AreEqual(2, t.textInfo.lineCount, "줄높이를 건 뒤에도 두 줄이 그려진다(-webkit-line-clamp: 2)");
                Assert.IsTrue(HasEllipsis(t), "둘째 줄 끝에 …(U+2026)");
                Debug.Log("[T423] face " + face.ToString("0.0") + "px · pitch " + pitch.ToString("0.0")
                    + "px · 상자 " + TextClamp.BoxHeight(t, "sr_name").ToString("0.0") + "px(종전 셈이면 " + old.ToString("0.0") + "px)");

                // 한 줄 자리는 값이 그대로다 — 이 회차가 바꾼 것은 «둘째 줄부터» 뿐이다.
                TextMeshProUGUI one = UiKit.Text(host, "t423-one", TextKind.Sub, "망령의 활", "pp_ink", TextAlignmentOptions.TopLeft);
                TextClamp.Apply(one, "profile_field");
                Assert.AreEqual(TextClamp.LineHeight(one) * (1f + TextClamp.SlackF()), TextClamp.BoxHeight(one, "profile_field"), 0.01f,
                    "한 줄 자리는 face × (1 + 여유) 그대로");
            }
            finally { Object.Destroy(host.gameObject); }
        }

        /// <summary>자리 배선 — 프로필 이름 칸(정본 3052 `.profile-field` 한 줄 말줄임): 닉네임은 12자 상한(ui.js 5066)이라 넓은 글자 12자로 채워 본다.</summary>
        [UnityTest]
        public IEnumerator 프로필_이름_칸은_한_줄_말줄임_규칙을_걸고_두_줄로_꺾이지_않는다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            const string Wide = "뷁뷁뷁뷁뷁뷁뷁뷁뷁뷁뷁뷁";   // 12자 · 가장 넓은 한글 꼴
            ProfilePopup.SetNickname(h, Wide);
            ProfilePopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업이 안 열렸다");
            Transform field = p.Root.Find("card/name-field");
            Assert.IsNotNull(field, "이름 칸(name-field)이 없다");
            TextMeshProUGUI t = field.Find("text").GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(t);
            t.ForceMeshUpdate();
            Assert.AreEqual(TextWrappingModes.NoWrap, t.textWrappingMode, "정본 white-space: nowrap");
            Assert.AreEqual(TextOverflowModes.Ellipsis, t.overflowMode, "정본 text-overflow: ellipsis");
            Assert.AreEqual(1, t.textInfo.lineCount, "한 줄 — 두 줄로 안 꺾인다");
            Assert.Greater(t.textInfo.characterCount, 0, "글자가 통째로 사라지면(런 528 꼴) 안 된다");
            if (t.textInfo.characterCount < Wide.Length) Assert.IsTrue(HasEllipsis(t), "잘렸으면 끝은 …");
            ProfilePopup.Close(h);
            yield return null;
        }

        /// <summary>자리 배선 — 모루 «들고 있는 장비» 이름(정본 1005 `.held-name` 한 줄 말줄임): 비교 팝업 딤을 눌러 보류 카드를 세우고 그 이름 글자를 본다.</summary>
        [UnityTest]
        public IEnumerator 모루_보류_카드의_이름은_한_줄_말줄임_규칙을_걸고_글자가_사라지지_않는다()
        {
            yield return Boot();
            float t0 = 0f;
            while (!ForgeHost.Ready && t0 < 20f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            ForgeHost h = ForgeHost.Instance;
            h.S.Hammers = 10; h.Pull();
            h.OnCraft();
            float t1 = 0f;
            while (!h.Meta.Popups.IsOpen(ForgeCraftPopup.Name) && t1 < 5f) { t1 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(h.Meta.Popups.IsOpen(ForgeCraftPopup.Name), "비교 팝업이 떠야 한다");
            yield return null;
            h.OnCraftDimClick();
            yield return null;
            Assert.IsNotNull(h.HeldItem, "딤을 누르면 보류품");
            Transform slot = null;
            foreach (Transform x in UiRoot.Instance.Sheet.GetComponentsInChildren<Transform>(true)) if (x.name == "held-slot") { slot = x; break; }
            Assert.IsNotNull(slot, "모루 자리에 보류 카드");
            Transform nameT = null;
            foreach (Transform x in slot.GetComponentsInChildren<Transform>(true)) if (x.name == "held-name") { nameT = x; break; }
            Assert.IsNotNull(nameT, "보류 카드의 이름 글자(held-name)");
            TextMeshProUGUI nm = nameT.GetComponent<TextMeshProUGUI>();
            nm.ForceMeshUpdate();
            Assert.AreEqual(TextWrappingModes.NoWrap, nm.textWrappingMode, "정본 white-space: nowrap");
            Assert.AreEqual(TextOverflowModes.Ellipsis, nm.overflowMode, "정본 text-overflow: ellipsis");
            Assert.GreaterOrEqual(nm.rectTransform.rect.height, TextClamp.LineHeight(nm) - 0.01f, "상자 높이 ≥ 실제 줄높이 — 낮으면 TMP 가 줄을 통째로 버린다(런 528)");
            Assert.AreEqual(1, nm.textInfo.lineCount, "한 줄");
            Assert.Greater(nm.textInfo.characterCount, 0, "이름이 통째로 사라지면 안 된다");
            if (nm.textInfo.characterCount < h.HeldItem.Name.Length) Assert.IsTrue(HasEllipsis(nm), "잘렸으면 끝은 …");
        }

        /// <summary>자리 배선(T351 5회차 · 마지막 자리) — 소환 결과 셀의 이름판(정본 7041 `.sr-name { overflow: hidden }` + 7048 `.sr-name > span { -webkit-line-clamp: 2 }`).
        /// 긴 이름을 한 칸에 넣고 ⓐ 줄 수 ≤ 표(2) · ⓑ 그린 글자가 **이름판 밖으로 안 흐른다** · ⓒ 잘렸으면 끝이 … · ⓓ 줄높이는 정본 1.18 을 본다.</summary>
        [UnityTest]
        public IEnumerator 소환_결과_이름판은_두_줄까지만_그리고_판_밖으로_안_흐른다()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260916;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;

            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = LongName },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "common", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "rare", Name = "다" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "rare", null);
            Assert.IsNotNull(v, "소환 결과 창이 안 열렸다");
            yield return null;
            Canvas.ForceUpdateCanvases();

            RectTransform plate = null;
            foreach (Transform x in v.GetComponentsInChildren<Transform>(true))
                if (x.name == "sr-name") { plate = (RectTransform)x; break; }
            Assert.IsNotNull(plate, "셀의 이름판(sr-name)이 없다");
            TextMeshProUGUI nt = plate.Find("t").GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(nt, "이름판 안의 글자(t)");
            nt.ForceMeshUpdate();

            Assert.AreEqual(TextOverflowModes.Ellipsis, nt.overflowMode, "정본 7048 -webkit-line-clamp = 상자 밖은 버리고 …");
            Assert.AreEqual(TextWrappingModes.Normal, nt.textWrappingMode, "정본 7038 white-space: normal — 두 줄까지 접는다");
            Assert.Greater(nt.textInfo.characterCount, 0, "이름이 통째로 사라지면 안 된다(런 528 꼴)");
            Assert.LessOrEqual(nt.textInfo.lineCount, TextClamp.Lines("sr_name"), "표 sr_name 의 줄 수를 넘지 않는다");
            Assert.Less(nt.textInfo.characterCount, LongName.Length, "긴 이름은 잘린다");
            Assert.IsTrue(HasEllipsis(nt), "잘린 끝에 …(U+2026)이 보인다");

            // 그린 글자가 이름판 밖으로 안 흐른다 — TMP 가 실제로 그린 덩어리의 높이를 판 높이와 견준다.
            float ink = nt.textInfo.lineInfo[0].ascender - nt.textInfo.lineInfo[nt.textInfo.lineCount - 1].descender;
            Assert.LessOrEqual(ink, plate.rect.height + 1f,
                "이름 잉크(" + ink.ToString("0.0") + ")가 이름판(" + plate.rect.height.ToString("0.0") + ") 밖으로 흘렀다");

            // 줄높이는 정본 1.18(T354 표 sr_name_lh) — 두 줄일 때만 잰다.
            if (nt.textInfo.lineCount >= 2)
            {
                double r = LineHeight.MeasuredRatio(nt);
                Assert.AreEqual(1.18, r, 0.02, "정본 7033 line-height: 1.18");
            }
        }
    }
}
