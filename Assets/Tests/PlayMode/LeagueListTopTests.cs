using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T421 — 리그 시트 랭킹 목록의 위끝은 원작 실측(shot-042149 첫 행 카드 21.21%H · 표 `league_list_top`)이다.
    /// 종전 «시즌 바 아래 + .4rem» 흐름 셈은 T378 이 시즌 바를 정본 높이로 되돌린 뒤 3.09%H 높게 섰다(런 901: 18.12%H).
    /// </summary>
    public class LeagueListTopTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null))
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "MetaHost/PopupLayer 가 20초 안에 준비되지 않았다");
                yield return null;
            }
            yield return null;
        }
        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { Transform r = Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        [UnityTest]
        public IEnumerator 리그_랭킹_목록_위끝은_원작_실측_21_21퍼센트H_다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.Open(h);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(p, "리그 시트");
            Transform sheet = Find(p.Root, "sheet");
            Assert.IsNotNull(sheet, "시트 판(sheet)");
            RectTransform list = Find(sheet, "list") as RectTransform;
            Assert.IsNotNull(list, "랭킹 목록(list)");
            Assert.AreEqual(0.2121f, UiKit.L("league_list_top"), 1e-6f, "표 = 원작 shot-042149 첫 행 카드 위끝 21.21%H(style.css 2317 주석과 같다)");
            float top = -list.anchoredPosition.y;   // UiKit.Place 좌상단 앵커 · anchoredPosition.y = −위끝
            Assert.AreEqual(UiKit.H("league_list_top"), top, 0.5f, "목록 위끝 = 표(전엔 시즌 바 아래 +.4rem 흐름 셈 · 18.12%H)");
            // 시즌 바보다 아래에 있고(겹치지 않는다) · 발 밴드 위끝(74.44%H)보다 위다
            RectTransform bar = Find(sheet, "season-bar") as RectTransform;
            Assert.IsNotNull(bar, "시즌 바");
            float barBottom = -bar.anchoredPosition.y + bar.rect.height;
            Assert.Greater(top, barBottom, "목록이 시즌 바 아래에서 시작한다(정본: 바 아래끝 → 첫 행 ≈3.4%H)");
            Assert.Less(top, UiKit.L("league_foot_top") * UiKit.RefH, "발 밴드 위");
            // 첫 행 카드가 목록 위끝에 붙어 있다(ScrollList 의 content 는 0 에서 시작)
            Transform firstRow = null;
            foreach (Transform t in list.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("row-")) { firstRow = t; break; }
            Assert.IsNotNull(firstRow, "첫 랭킹 행(row-N)");
            Vector3[] lc = new Vector3[4], rc = new Vector3[4];
            list.GetWorldCorners(lc); ((RectTransform)firstRow).GetWorldCorners(rc);
            Assert.AreEqual(lc[1].y, rc[1].y, 1.0f, "첫 행 카드 위끝 = 목록 위끝(월드)");
            Debug.Log("[T421] 리그 목록 위끝 " + (top / UiKit.RefH * 100f).ToString("0.00") + "%H · 시즌 바 아래끝 " + (barBottom / UiKit.RefH * 100f).ToString("0.00") + "%H");
            LeagueSheet.Close(h);
            yield return null;
        }
    }
}
