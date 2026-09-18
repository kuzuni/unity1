using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T466 — 정본 `word-break: keep-all` 세 자리(2236 `.swc-name` · 3856 `.sheet-sub` · 7040 `.sr-name`): 한글을 **어절(띄어쓰기)에서만** 꺾는다.
    /// 클론 TMP 는 음절마다 꺾는 것이 기본이라, 표 `WrapUi.json` `keep_all` 자리에서만 `KeepAll.Apply` 가 어절마다 `&lt;nobr&gt;` 를 씌운다.
    /// 자: ⓐ 순수 셈(어절 감싸기 · 태그 안 띄어쓰기 보존 · 멱등) ⓑ 좁은 폭 실물 — 안 씌우면 음절 한가운데서 꺾이고, 씌우면 꺾인 자리가 전부 띄어쓰기다(`textInfo.lineInfo`)
    /// ⓒ 자리 셋 — 퀘스트·상점 시트 안내(`sheet-sub`) · 소환 결과 이름판(`sr-name`)에 감싸기가 들어 있고 판매 경고 이름(`swc-name`)은 표가 keep-all 이다.
    /// </summary>
    public class KeepAllTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(PopupLayer.Instance != null && UiRoot.Instance != null && MetaHost.Ready && MetaHost.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 20초 안에 안 섰다");
            yield return null;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        /// <summary>꺾인 자리(각 줄의 끝 다음 글자)가 전부 띄어쓰기인가 — 아니면 그 줄 번호(0부터)를 돌려준다(-1 = 전부 띄어쓰기).</summary>
        static int FirstMidWordBreak(TMP_Text t)
        {
            var ti = t.textInfo;
            for (int i = 0; i < ti.lineCount - 1; i++)
            {
                int last = ti.lineInfo[i].lastVisibleCharacterIndex;
                int next = ti.lineInfo[i + 1].firstVisibleCharacterIndex;
                if (last < 0 || next < 0 || next >= ti.characterCount) continue;
                // 줄 끝 글자와 다음 줄 첫 글자 사이에 띄어쓰기(그려지지 않는 글자)가 있어야 어절 경계다
                bool space = false;
                for (int k = last + 1; k < next; k++) if (char.IsWhiteSpace(ti.characterInfo[k].character)) { space = true; break; }
                if (!space) return i;
            }
            return -1;
        }

        [Test]
        public void 어절마다_nobr_를_씌우고_태그_안_띄어쓰기는_안_가르고_두_번_씌워도_한_번이다()
        {
            Assert.AreEqual("<nobr>가나다</nobr> <nobr>라마바</nobr>", KeepAll.Wrap("가나다 라마바"));
            Assert.AreEqual("<nobr>가나다</nobr>  <nobr>라마바</nobr> ", KeepAll.Wrap("가나다  라마바 "), "띄어쓰기 수·끝 공백은 그대로");
            Assert.AreEqual("<nobr><sprite name=\"x\">가나</nobr> <nobr>다라</nobr>", KeepAll.Wrap("<sprite name=\"x\">가나 다라"), "태그 안의 띄어쓰기는 어절 경계가 아니다 · 태그는 이웃 글자와 한 덩어리");
            Assert.AreEqual("<nobr>[지하</nobr> <nobr>세계]</nobr> <nobr>망령의</nobr> <nobr>활</nobr>", KeepAll.Wrap("[지하 세계] 망령의 활"));
            string once = KeepAll.Wrap("가 나");
            Assert.AreEqual(once, KeepAll.Wrap(once), "멱등");
            Assert.IsTrue(KeepAll.Has(once)); Assert.IsFalse(KeepAll.Has("가 나"));
            Assert.AreEqual("가 나", KeepAll.Strip(once));
            Assert.AreEqual("", KeepAll.Wrap("")); Assert.IsNull(KeepAll.Wrap(null));
        }

        [UnityTest]
        public IEnumerator 좁은_폭에서_안_씌우면_음절_한가운데서_꺾이고_씌우면_띄어쓰기에서만_꺾인다()
        {
            yield return Boot();
            RectTransform box = UiKit.Box(UiRoot.Instance.App, "t466-box");
            TextMeshProUGUI t = UiKit.Text(box, "t", TextKind.Sub, "가나다라 마바사아 자차카타 파하거너", "pp_ink", TextAlignmentOptions.Left);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            // 폭 = 글자 다섯 반 — «가나다라» 는 들고 «가나다라 마바사아» 는 안 든다 · 음절마다 꺾으면 «마바» 가 앞 줄에 붙는다
            float w = t.fontSize * 5.5f;
            UiKit.Anchor(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, w, t.fontSize * 8f);
            UiKit.Fill(t.rectTransform);
            yield return null;
            t.ForceMeshUpdate();
            Assert.GreaterOrEqual(t.textInfo.lineCount, 2, "좁아서 꺾인다");
            int mid = FirstMidWordBreak(t);
            Assert.GreaterOrEqual(mid, 0, "TMP 기본(정본 normal)은 음절 한가운데서 꺾인다 — 그래야 keep-all 이 «다른 것» 이다");
            Assert.IsTrue(KeepAll.Apply(t, "sheet_sub"), "표의 keep-all 자리");
            Assert.IsTrue(KeepAll.Has(t));
            t.ForceMeshUpdate();
            Assert.GreaterOrEqual(t.textInfo.lineCount, 2, "여전히 꺾인다(어절이 폭보다 짧다)");
            Assert.AreEqual(-1, FirstMidWordBreak(t), "keep-all: 꺾인 자리가 전부 띄어쓰기다(줄 " + FirstMidWordBreak(t) + " 이 어절 한가운데)");
            Assert.AreEqual(16, t.textInfo.characterCount - 3, "태그는 글자로 안 센다(음절 16 + 띄어쓰기 3)");
            Assert.IsFalse(KeepAll.Apply(t, "waypoint_time"), "keep-all 이 아닌 자리는 안 건드린다");
            Object.Destroy(box.gameObject);
        }

        [UnityTest]
        public IEnumerator 퀘스트_상점_시트_안내와_소환_결과_이름판은_어절로_감싸져_있고_판매_경고_이름은_표가_keep_all_이다()
        {
            yield return Boot();
            Assert.IsTrue(WrapUi.Table.KeepsAll("swc_name"), "2236 .swc-name");
            Assert.IsTrue(WrapUi.Table.KeepsAll("sheet_sub"), "3856 .sheet-sub");
            Assert.IsTrue(WrapUi.Table.KeepsAll("sr_name"), "7040 .sr-name");
            MetaHost h = MetaHost.Instance;
            QuestSheet.Open(h);
            yield return null; yield return null;
            Popup p = h.Popups.Find(QuestSheet.Name);
            Assert.IsNotNull(p, "퀘스트 시트");
            TextMeshProUGUI qs = Find(p.Root, "sub").GetComponent<TextMeshProUGUI>();
            Assert.IsTrue(KeepAll.Has(qs), "퀘스트 안내(.sheet-sub)가 어절로 감싸져 있다: " + qs.text);
            Assert.AreEqual("모든 퀘스트는 수령해도 같은 내용으로 반복됩니다", KeepAll.Strip(qs.text), "글은 그대로");
            h.Popups.Hide(p);
            yield return null;
            ShopSheet.Open(h);
            yield return null; yield return null;
            Popup sp = h.Popups.Find(ShopSheet.Name);
            Assert.IsNotNull(sp, "상점 시트");
            TextMeshProUGUI ss = Find(sp.Root, "sub").GetComponent<TextMeshProUGUI>();
            Assert.IsTrue(KeepAll.Has(ss), "상점 안내(.sheet-sub)가 어절로 감싸져 있다");
            h.Popups.Hide(sp);
            yield return null;
            // 소환 결과 이름판
            PetSkillHost.SuppressSave = true;
            float tw = 0f;
            while (!(SkillPetSheet.Instance != null && PetSkillHost.Ready) && tw < 20f) { tw += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트");
            var list = new System.Collections.Generic.List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "불꽃 화살 비" },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "common", null);
            Assert.IsNotNull(v);
            yield return null;
            Transform nameBox = Find(v.transform, "sr-name");
            Assert.IsNotNull(nameBox, "이름판 sr-name");
            TextMeshProUGUI nt = nameBox.Find("t").GetComponent<TextMeshProUGUI>();
            Assert.IsTrue(KeepAll.Has(nt), "소환 결과 이름(.sr-name)이 어절로 감싸져 있다: " + nt.text);
            Assert.AreEqual("불꽃 화살 비", KeepAll.Strip(nt.text));
            v.Close();
        }
    }
}
