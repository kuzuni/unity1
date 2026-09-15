using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T361 2회차 — 정본 `white-space` 표(`WrapUi.json`)를 자리 파일이 <c>WrapUi.Apply(t, key)</c> 로 건다.
    /// ⓐ(공장 기본을 «접는다» 로 뒤집기) 전에는 기본이 NoWrap 이라 nowrap 자리는 어차피 안 접히지만, 뒤집은 뒤에도 이 자가 그 자리를 지킨다.
    /// 정본: 2347 `.league-score` · 2556 `.league-tier-rank` · 2582 `.league-tier-grid span` · 3052 `.profile-field` · 7040 `.sr-name { normal }`.
    /// </summary>
    public class WrapSitesTests
    {
        private static void DeleteSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        private static IEnumerator Boot()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null && Hud.Instance != null) && t < 30f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 30초 안에 서지 않았다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 도우미는_표대로_건다_nowrap_은_NoWrap_normal_은_Normal()
        {
            yield return Boot();
            RectTransform box = UiKit.Box(PopupLayer.Instance.transform, "wrap-probe");
            TextMeshProUGUI t = UiKit.Text(box, "t", TextKind.Sub, "긴 문장 하나가 상자보다 길다 긴 문장 하나가 상자보다 길다", "pp_ink");
            Assert.IsFalse(WrapUi.Apply(t, "coin_amt"), "정본 7429 `.coin-amt { nowrap }` → 안 접는다");
            Assert.AreEqual(TextWrappingModes.NoWrap, t.textWrappingMode, "nowrap 자리는 NoWrap");
            Assert.IsTrue(WrapUi.Apply(t, "sr_name"), "정본 7040 `.sr-name { white-space: normal }` → 접는다");
            Assert.AreEqual(TextWrappingModes.Normal, t.textWrappingMode, "normal 자리는 Normal");
            Assert.IsTrue(WrapUi.Apply(t, "no_such_site_zzz"), "표에 없는 자리는 정본 기본 = 접는다");
            Object.Destroy(box.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 리그_점수_티어_순위_보상_수와_프로필_칸은_표대로_안_접힌다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            LeagueSheet.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(p, "리그 시트");
            int scores = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.transform.parent != null && t.transform.parent.parent != null && t.transform.parent.parent.name == "score") { scores++; Assert.AreEqual(TextWrappingModes.NoWrap, t.textWrappingMode, "리그 점수(.league-score) 는 nowrap"); }
            Assert.Greater(scores, 0, "리그 점수 글자를 못 찾았다");
            h.Popups.Hide(LeagueSheet.Name);
            yield return null;
            LeagueSheet.OpenRewards(h);
            yield return null;
            p = PopupLayer.Instance.Find(LeagueSheet.RewardsName);
            Assert.IsNotNull(p, "리그 보상");
            int amts = 0, labels = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name == "amt") { amts++; Assert.AreEqual(TextWrappingModes.NoWrap, t.textWrappingMode, "보상 격자 수(.league-reward-grid/.league-tier-grid span) 는 nowrap"); }
                else if (t.name == "label" && t.transform.parent != null && t.transform.parent.name == "rank") { labels++; Assert.AreEqual(TextWrappingModes.NoWrap, t.textWrappingMode, "티어 순위(.league-tier-rank) 는 nowrap"); }
            }
            Assert.Greater(amts, 0, "보상 격자 수를 못 찾았다");
            Assert.Greater(labels, 0, "티어 순위 라벨을 못 찾았다");
            h.Popups.Hide(LeagueSheet.RewardsName);
            yield return null;
            ProfilePopup.Open(h);
            yield return null;
            p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필");
            int fields = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "text" && t.transform.parent != null && t.transform.parent.Find("face") != null) { fields++; Assert.AreEqual(TextWrappingModes.NoWrap, t.textWrappingMode, "프로필 칸(.profile-field) 은 nowrap"); }
            Assert.Greater(fields, 0, "프로필 칸 글자를 못 찾았다");
            ProfilePopup.Close(h);
            yield return null;
        }
        /// <summary>T361 3회차 ⓐ — 공장(<c>UiKit.Text</c>)이 만든 글자는 정본대로 **기본이 접힌다**(전엔 NoWrap 이 박혀 있었다).</summary>
        [UnityTest]
        public IEnumerator 공장이_만든_글자는_기본이_접힌다()
        {
            yield return Boot();
            RectTransform box = UiKit.Box(PopupLayer.Instance.transform, "wrap-probe-2");
            UiKit.Place(box, 0f, 0f, 120f, 200f);
            TextMeshProUGUI t = UiKit.Text(box, "t", TextKind.Sub, "긴 문장 하나가 상자보다 길어서 두 줄 세 줄로 접혀야 한다 긴 문장 하나가 상자보다 길다", "pp_ink");
            Assert.AreEqual(TextWrappingModes.Normal, t.textWrappingMode, "공장 기본 = 접는다(정본 white-space 기본 normal)");
            t.ForceMeshUpdate();
            Assert.Greater(t.textInfo.lineCount, 1, "120px 상자의 긴 문장은 여러 줄로 선다");
            for (int i = 0; i < t.textInfo.lineCount; i++) Assert.LessOrEqual(t.textInfo.lineInfo[i].maxAdvance, 120f + 1f, "접히면 어느 줄도 상자 폭을 안 넘는다(줄 " + i + ")");
            Object.Destroy(box.gameObject);
            yield return null;
        }

        /// <summary>T361 ⓒ — 확률 팝업 안내문(정본 4600 `.rates-tip` · 표 밖 자리)은 접힘이 켜져 상자 폭 안에 선다.</summary>
        [UnityTest]
        public IEnumerator 확률_팝업_안내문은_접혀서_상자_폭_안에_선다()
        {
            yield return Boot();
            float t0 = 0f;
            while (SkillPetSheet.Instance == null && t0 < 10f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 섰다");
            SkillRatesPopup.Open(SkillPetSheet.Instance, "pet");
            yield return null;
            Canvas.ForceUpdateCanvases();
            TextMeshProUGUI tip = null;
            foreach (TextMeshProUGUI t in UiRoot.Instance.App.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "rates-tip") tip = t;
            Assert.IsNotNull(tip, "안내문(rates-tip)을 못 찾았다");
            Assert.AreEqual(TextWrappingModes.Normal, tip.textWrappingMode, "표 밖 자리는 정본 기본 = 접는다");
            tip.ForceMeshUpdate();
            float w = tip.rectTransform.rect.width;
            Assert.Greater(w, 0f, "안내문 상자에 폭이 있다");
            for (int i = 0; i < tip.textInfo.lineCount; i++) Assert.LessOrEqual(tip.textInfo.lineInfo[i].maxAdvance, w + 1f, "안내문이 상자 밖으로 가로로 안 삐져나온다(줄 " + (i + 1) + "/" + tip.textInfo.lineCount + ")");
            yield return null;
        }
    }
}
