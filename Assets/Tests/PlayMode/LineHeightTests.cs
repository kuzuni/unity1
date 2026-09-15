using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Core.Save;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T354 3회차 — 표의 줄높이가 **실제 줄 간격**으로 나오는가(단위 옮김의 실물 검산): TMP `lineSpacing` 은 em/100 이라
    /// `(배수 − 자산 줄높이 비율) × 100` 이어야 첫·둘째 줄 기준선 차 ÷ 글자 크기 = 정본 배수가 된다. 값은 표에서 읽는다.
    /// </summary>
    public class LineHeightTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (!DungeonUiHost.Ready || UiRoot.Instance == null)
            {
                Assert.Less(Time.realtimeSinceStartup - t, 20f, "DungeonUiHost/UiRoot 가 20초 안에 준비되지 않았다");
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
        public IEnumerator 표의_배수가_실제_줄_간격이_된다_합성_글자_셋()
        {
            yield return Boot();
            RectTransform box = UiKit.Box(UiRoot.Instance.App, "t354-box");
            UiKit.Place(box, 0f, 0f, UiKit.RefW * 0.6f, UiKit.RefH * 0.5f);
            string longText = "정본 줄높이 검산 — 이 글은 상자 폭을 넘어 두 줄 이상으로 꺾여야 한다. 줄 간격은 표가 정한다. 첫째 둘째 셋째 넷째.";
            string[] keys = { "asc_focus_eff_lh", "rates_tip_lh", "chat_preview_lines_lh" };   // 1.5 · 1.4 · 1.25 — 기본(1.448)보다 넓게·좁게·더 좁게
            foreach (string key in keys)
            {
                TextMeshProUGUI t = UiKit.Text(box, "t-" + key, TextKind.Sub, longText, "pp_ink", TextAlignmentOptions.TopLeft);
                t.textWrappingMode = TextWrappingModes.Normal;
                UiKit.Place(t.rectTransform, 0f, 0f, UiKit.RefW * 0.6f, UiKit.RefH * 0.5f);
                double r = LineHeight.Apply(t, key);
                Assert.AreEqual(LineHeight.Table.Get(key), r, 1e-9, key + ": 배수 키는 표값 그대로");
                FaceInfo f = t.font.faceInfo;
                Assert.AreEqual(LineHeightRules.TmpLineSpacing(r, f.lineHeight, f.pointSize), t.lineSpacing, 1e-4f, key + ": lineSpacing = (r − 자산 비율) × 100");
                yield return null;
                double measured = LineHeight.MeasuredRatio(t);
                Assert.Greater(t.textInfo.lineCount, 1, key + ": 두 줄 이상이어야 잰다");
                Assert.AreEqual(r, measured, 0.02, key + ": 첫·둘째 줄 기준선 차 ÷ 글자 크기 = 정본 배수 (자산 기본 1.448 이 아니라)");
                Object.Destroy(t.gameObject);
            }
            Object.Destroy(box.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 승천_초점_효과_글줄은_정본_1_5_배수로_선다()
        {
            yield return Boot();
            AscendPopup.Open("forge");
            yield return null;
            Transform eff = Find(AscendPopup.Root, "eff");
            Assert.IsNotNull(eff, "초점 카드의 효과 글줄");
            TextMeshProUGUI t = eff.GetComponent<TextMeshProUGUI>();
            double r = LineHeight.Table.Get("asc_focus_eff_lh");
            FaceInfo f = t.font.faceInfo;
            Assert.AreEqual(LineHeightRules.TmpLineSpacing(r, f.lineHeight, f.pointSize), t.lineSpacing, 1e-4f, "정본 5635 .asc-focus-eff { line-height: 1.5 }");
            double measured = LineHeight.MeasuredRatio(t);
            Assert.Greater(t.textInfo.lineCount, 1, "효과 글줄은 세 줄(· 셋)");
            Assert.AreEqual(r, measured, 0.02, "실제 줄 간격이 표대로");
            AscendPopup.Close();
            yield return null;
        }
    }
}
