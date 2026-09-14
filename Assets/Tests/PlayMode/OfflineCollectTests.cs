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
    /// T155 — 오프라인 [수집] 버튼의 치수와 흰 몸통 안 세로 배분(검수 Q · 런 364 `screen_offline.png` ↔ 원작 `shot-042110.png`).
    /// 정본 `style.css` 306~308: `.offline-collect-btn { width: 51.7%; height: 4.6rem }` + 주석 «원본 파란 면 실측 폭 29.80%W · 높이 7.49%H» ·
    /// 279~281: `.offline-bottom { padding: 1.1rem .9rem 1.3rem; justify-content: center; gap: 1.79rem }`.
    /// 자기 파일인 이유: `OfflinePopupTests.cs` 는 T144 의 파일이라 남의 단언을 안 흔들려고 뗐다(T140 SettingsRowTests 와 같은 꼴).
    /// </summary>
    public class OfflineCollectTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (System.Exception) { }
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
            while (!(MetaHost.Ready && PopupLayer.Instance != null && UiRoot.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            yield return null;
        }

        private static RectTransform FindUnder(RectTransform parent, string objName)
        {
            foreach (RectTransform rt in parent.GetComponentsInChildren<RectTransform>(true))
                if (rt != parent && rt.name == objName) return rt;
            Assert.Fail(parent.name + " 아래에 «" + objName + "» 이 없다");
            return null;
        }

        private static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        [UnityTest]
        public IEnumerator 수집_버튼_파란_면은_29_80W_7_49H_이고_합계줄과_버튼_덩어리는_흰_몸통_세로_가운데에_선다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            OfflinePopup.Show(h, new OfflineReward { Elapsed = 5000, Counted = 3600, Coins = 8870, Hammers = 149.05, CoinRate = 1.13, HammerRate = 1.14 });
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(OfflinePopup.Name);
            Assert.IsNotNull(p, "오프라인 팝업");
            Rect app = World(UiRoot.Instance.App);
            RectTransform bottom = FindUnder(p.Root, "bottom");
            RectTransform total = FindUnder(bottom, "total");
            RectTransform collect = FindUnder(bottom, "collect");
            RectTransform face = FindUnder(collect, "face");
            Rect rb = World(bottom), rt = World(total), rc = World(collect), rf = World(face);
            float unit = app.height / UiKit.RefH;
            float rem = PopupKit.Rem * unit;

            // ⓐ 파란 면 = 정본 주석의 원본 실측(± .3%p · §2 판정 폭)
            Assert.AreEqual(0.0749f, rf.height / app.height, 0.003f, "파란 면 높이 7.49%H(정본 4.6rem 상자 − 테·아래턱)");
            Assert.AreEqual(0.2980f, rf.width / app.width, 0.003f, "파란 면 폭 29.80%W");
            Assert.AreEqual(UiKit.H("offline_collect_h") * unit + PopupKit.Line3 * 2f * unit + UiKit.H("btn_lip") * unit, rc.height, 1.5f, "상자 = 면 + 테 둘 + 아래턱");
            Assert.AreEqual(rb.center.x, rc.center.x, 1.5f, "버튼은 가로 가운데(align-self: center)");

            // ⓑ 세로 배분 — 합계줄 아래 → 버튼 위 = gap 1.79rem · 덩어리는 패딩(1.1/1.3rem) 안에서 가운데 · 버튼 아래 빈칸이 위 빈칸과 대칭(패딩 차 .2rem 안)
            Assert.AreEqual(rem * OfflinePopup.BottomGapRem, rt.yMin - rc.yMax, 1.5f, "합계줄과 버튼 사이 = 정본 gap 1.79rem");
            float above = rb.yMax - rt.yMax, below = rc.yMin - rb.yMin;
            Assert.AreEqual(rem * (OfflinePopup.BottomPadBottomRem - OfflinePopup.BottomPadTopRem), below - above, 1.5f, "위아래 빈칸 차 = 패딩 차(1.3 − 1.1rem) — 덩어리가 세로 가운데다");
            Assert.Less(below / rb.height, 0.40f, "버튼 아래 빈칸이 몸통의 40% 를 넘지 않는다(종전 63% · 원작 25%)");
            Assert.Greater(below, 0f, "버튼이 몸통 밖으로 안 나간다");
            h.Popups.Hide(OfflinePopup.Name);
            yield return null;
        }
    }
}
