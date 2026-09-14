using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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
            UiKit.Place(t.rectTransform, 0f, 0f, w, TextClamp.BoxHeight(TextKind.Sub, site));
            TextClamp.Apply(t, site);
            return t;
        }

        static char LastVisible(TextMeshProUGUI t)
        {
            int n = t.textInfo.characterCount;
            Assert.Greater(n, 0, "글자가 한 자도 안 그려졌다");
            return t.textInfo.characterInfo[n - 1].character;
        }

        [UnityTest]
        public IEnumerator 표의_세_자리가_서_있고_줄_수는_정본대로다()
        {
            yield return Boot();
            Assert.AreEqual(1, TextClamp.Lines("profile_field"), "3052 .profile-field 한 줄 말줄임");
            Assert.AreEqual(1, TextClamp.Lines("held_name"), "1005 .held-name 한 줄 말줄임");
            Assert.AreEqual(2, TextClamp.Lines("sr_name"), "7048 .sr-name > span -webkit-line-clamp: 2");
            Assert.Greater(TextClamp.LineHeightF(), 0f);
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
                Assert.AreEqual('…', LastVisible(t), "마지막 글자는 …");
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
                Assert.AreEqual('…', LastVisible(two), "둘째 줄 끝이 …");

                const string Short = "망령의 활";
                TextMeshProUGUI one = Make(host, "profile_field", Short, w);
                yield return null;
                one.ForceMeshUpdate();
                Assert.AreEqual(Short.Length, one.textInfo.characterCount, "들어가는 이름은 한 자도 안 버린다");
                Assert.AreNotEqual('…', LastVisible(one));
            }
            finally { Object.Destroy(host.gameObject); }
        }
    }
}
