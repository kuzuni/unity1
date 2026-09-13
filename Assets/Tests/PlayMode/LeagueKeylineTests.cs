using System.Collections;
using System.Collections.Generic;
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
    /// T109 5회차 — 리그 시트의 정본 키라인 네 자리가 실제로 걸리는가(정본 `style.css` 8403 `.league-row .league-name, .league-row .league-rank { 2px }` ·
    /// 8408 `.league-row .league-name small { max(1.4px, .1em) }` · 2355 `.league-row.me .league-server { max(1.2px, .1em) }`(me 행만) ·
    /// 2635 `.league-challenge-name small { 2px }`). 폭은 `KeylineUi.json` 에서 오고 `UiKit.OutlinePx` 가 SDF 로 환산한다(T104) —
    /// 여기서는 «걸렸는가 · 안 걸릴 행은 안 걸렸는가 · em 바닥이 정본 max() 대로인가» 를 본다. 픽셀 띠 자체는 T104 `OutlineTests` 가 지킨다.
    /// </summary>
    public class LeagueKeylineTests
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
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        /// <summary>팝업 안에서 «부모 이름이 prefix 로 시작하는 칸» 의 이름 name 글자 전부.</summary>
        private static List<TextMeshProUGUI> Texts(string popupName, string parentPrefix, string name)
        {
            Popup p = PopupLayer.Instance.Find(popupName);
            Assert.IsNotNull(p, popupName + " 이 열려 있지 않다");
            var list = new List<TextMeshProUGUI>();
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == name && t.transform.parent != null && t.transform.parent.name.StartsWith(parentPrefix)) list.Add(t);
            return list;
        }

        [UnityTest]
        public IEnumerator 리그_행의_순위_이름_전투력에_키라인이_걸리고_서버는_me_행만_걸린다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            h.OpenLeague();
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(LeagueSheet.Name), "리그 시트");

            List<TextMeshProUGUI> ranks = Texts(LeagueSheet.Name, "row-", "rank");
            List<TextMeshProUGUI> names = Texts(LeagueSheet.Name, "row-", "name");
            Assert.Greater(ranks.Count, 1, "랭킹 행이 둘 이상(창 8행 + 발 밴드의 내 행)이어야 한다");
            Assert.AreEqual(ranks.Count, names.Count, "행마다 순위·이름 하나씩");
            foreach (TextMeshProUGUI t in ranks) Assert.Greater(t.outlineWidth, 0f, "순위 «" + t.text + "» 에 2px 키라인(정본 8403)");
            foreach (TextMeshProUGUI t in names) Assert.Greater(t.outlineWidth, 0f, "이름 «" + t.text + "» 에 2px 키라인(정본 8403)");

            // 이름 아래 전투력 = IconTextRow «cp» 의 글자 조각들 — 정본 8408 small 규칙(글자 크기 비례 · 바닥 1.4 CSS px)
            int cpPieces = 0;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.Name);
            foreach (RectTransform cp in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (cp.name != "cp" || cp.parent == null || !cp.parent.name.StartsWith("row-")) continue;
                foreach (TextMeshProUGUI piece in UiKit.RowTexts(cp))
                {
                    cpPieces++;
                    Assert.Greater(piece.outlineWidth, 0f, "전투력 조각 «" + piece.text + "» 에 키라인(정본 8408)");
                    float want = KeylineUi.Em("league_name_small", piece.fontSize);
                    Assert.GreaterOrEqual(want, 1.4f * KeylineUi.CssPx - 1e-3f, "정본 max(1.4px, .1em) 의 바닥");
                    Assert.GreaterOrEqual(want, 0.1f * piece.fontSize - 1e-3f, "정본 max(1.4px, .1em) 의 .1em");
                }
            }
            Assert.Greater(cpPieces, 0, "전투력 글자 조각이 있어야 한다");

            // 서버 글자: 파란 me 행(발 밴드 pinned 아래)만 키라인 · 어두운 행은 정본도 민무늬(2350 주석)
            int meServers = 0, otherServers = 0;
            foreach (TextMeshProUGUI sv in Texts(LeagueSheet.Name, "row-", "server"))
            {
                bool pinned = sv.transform.parent.parent != null && sv.transform.parent.parent.name == "pinned";
                if (pinned) { meServers++; Assert.Greater(sv.outlineWidth, 0f, "me 행 서버 «" + sv.text + "» 에 키라인(정본 2355)"); }
                else if (sv.outlineWidth <= 0f) otherServers++;
            }
            Assert.AreEqual(1, meServers, "발 밴드에 고정된 내 행이 하나");
            Assert.Greater(otherServers, 0, "어두운 행의 서버 글자는 민무늬여야 한다(정본 2350 주석)");
            float wantMe = KeylineUi.Em("league_server_me", PopupKit.FontSize(TextKind.Sub));
            Assert.GreaterOrEqual(wantMe, 1.2f * KeylineUi.CssPx - 1e-3f, "정본 max(1.2px, .1em) 의 바닥");

            // 도전 상대 선택 팝업 — 전투력 «cp» 2px(정본 2635 · T28 22회차가 잡은 민주황 자리)
            LeagueSheet.OpenChallenge(h);
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(LeagueSheet.ChallengeName), "도전 팝업");
            List<TextMeshProUGUI> cps = Texts(LeagueSheet.ChallengeName, "row", "cp");
            Assert.Greater(cps.Count, 0, "도전 상대 행의 전투력 글자");
            foreach (TextMeshProUGUI t in cps) Assert.Greater(t.outlineWidth, 0f, "도전 전투력 «" + t.text + "» 에 2px 키라인(정본 2635)");
            Assert.AreEqual(2f * KeylineUi.CssPx, KeylineUi.Px("league_challenge_cp"), 1e-4f, "표: 2 CSS px × css_px");
            Assert.AreEqual(2f * KeylineUi.CssPx, KeylineUi.Px("league_row_text"), 1e-4f, "표: 2 CSS px × css_px");
        }
    }
}
