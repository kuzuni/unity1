using System.Collections;
using System.Collections.Generic;
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
    /// T352 ⓒ — 정본 `tabular-nums` 여섯 자리 중 클론에 배선된 자리의 숫자가 실제로 등폭으로 그려지는가.
    /// 판정은 «태그가 붙었다» 가 아니라 **글자 origin 의 간격** — 같은 구간의 이웃 숫자끼리 시작 x 차이가 같고, 그 값이 글꼴에서 읽은 칸(em × 글자 크기)이다.
    /// </summary>
    public class TabularSitesTests
    {
        static IEnumerator Boot()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(UiRoot.Instance != null && UiRoot.Instance.App != null && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f)
            { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "메타 호스트 부팅");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 글꼴에서_읽은_숫자_칸은_반_em_안팎이다()
        {
            yield return Boot();
            float em = TabularText.DigitEm(UiFont.Primary);
            Assert.Greater(em, 0.3f, "가장 넓은 숫자 advance / 샘플링 크기");
            Assert.Less(em, 1.0f, "숫자 하나가 1em 을 넘을 수 없다");
        }

        [UnityTest]
        public IEnumerator 패스_칸의_보상_수는_숫자_구간이_등폭이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            PassPopup.Open(h);
            yield return null;
            Popup p = PopupLayer.Instance.Find(PassPopup.Name);
            Assert.IsNotNull(p, "패스 팝업이 열린다");

            var amts = new List<TextMeshProUGUI>();
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == "amt") amts.Add(t);
            Assert.Greater(amts.Count, 0, "정본 `.pass-cell span:not(.pass-badge)` — 칸마다 보상 수 span");

            float em = TabularText.DigitEm(UiFont.Primary);
            int measured = 0;
            foreach (TextMeshProUGUI t in amts)
            {
                Assert.IsTrue(t.richText, t.name + " — 태그를 쓰려면 richText");
                Assert.IsTrue(TabularNums.IsWrapped(t.text), "숫자 구간이 <mspace> 로 감싸였다: " + t.text);
                t.ForceMeshUpdate(true, true);
                TMP_TextInfo info = t.textInfo;
                float cell = em * t.fontSize, tol = cell * 0.15f;
                for (int i = 0; i + 1 < info.characterCount; i++)
                {
                    TMP_CharacterInfo a = info.characterInfo[i], b = info.characterInfo[i + 1];
                    if (!char.IsDigit(a.character) || !char.IsDigit(b.character)) continue;
                    Assert.AreEqual(cell, b.origin - a.origin, tol, "이웃 숫자의 시작 x 간격 = 칸(" + t.text + ")");
                    measured++;
                }
            }
            Assert.Greater(measured, 0, "숫자가 둘 이상 이어진 보상 수가 하나는 있어야 간격을 잰다");

            PassPopup.Close(h);
            yield return null;
        }

        /// <summary>T352 2회차 — 정본 8635 `.rate-bar`·`.rates-prog span { font-variant-numeric: tabular-nums }`:
        /// 확률 팝업은 등급 여섯 줄의 확률이 **세로로 열을 이루고** 아래 게이지 글자도 숫자라, 등폭이 아니면 줄마다 소수점이 좌우로 흔들린다(정본 주석).</summary>
        [UnityTest]
        public IEnumerator 확률_팝업의_확률과_게이지_숫자는_등폭이다()
        {
            yield return Boot();
            float t0 = 0f;
            while (SkillPetSheet.Instance == null && t0 < 10f) { t0 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 섰다");
            SkillRatesPopup.Open(SkillPetSheet.Instance, "pet");
            yield return null;
            Assert.IsTrue(SkillPetSheet.Instance.Modal.IsOpen(SkillRatesPopup.ModalName), "확률 팝업이 열린다");

            float em = TabularText.DigitEm(UiFont.Primary);
            int pct = 0, gauge = 0, measured = 0;
            foreach (TextMeshProUGUI t in UiRoot.Instance.App.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                bool isPct = t.name == "rate-pct";
                bool isGauge = t.name == "t" && t.transform.parent != null && t.transform.parent.name == "rates-prog";
                if (!isPct && !isGauge) continue;
                if (!TabularNums.IsWrapped(t.text)) continue;         // 숫자가 없는 글자는 도우미가 안 건드린다
                if (isPct) pct++; else gauge++;
                Assert.IsTrue(t.richText, t.name + " — <mspace> 를 쓰려면 richText");
                t.ForceMeshUpdate(true, true);
                TMP_TextInfo info = t.textInfo;
                float cell = em * t.fontSize, tol = cell * 0.15f;
                for (int i = 0; i + 1 < info.characterCount; i++)
                {
                    TMP_CharacterInfo a = info.characterInfo[i], b = info.characterInfo[i + 1];
                    if (!char.IsDigit(a.character) || !char.IsDigit(b.character)) continue;
                    Assert.AreEqual(cell, b.origin - a.origin, tol, "이웃 숫자의 시작 x 간격 = 칸(" + t.text + ")");
                    measured++;
                }
            }
            Assert.Greater(pct, 0, "정본 `.rate-bar` — 등급 줄마다 확률 글자");
            Assert.Greater(gauge, 0, "정본 `.rates-prog span` — 게이지 글자(`PetSkillKit.Gauge` 의 «t»)");
            Assert.Greater(measured, 0, "숫자가 둘 이상 이어진 자리가 하나는 있어야 간격을 잰다");

            SkillPetSheet.Instance.Modal.CloseAll();
            yield return null;
        }

        /// <summary>T352 3회차 — 정본 8635 `.league-score { font-variant-numeric: tabular-nums }`:
        /// 랭킹 창 8행 + 발 밴드의 내 행 점수가 세로 열을 이루므로(정본 주석 «행마다 좌우로 흔들리던 자리») 숫자 구간이 등폭이어야 한다.
        /// 자리는 `LeagueSheet` 행의 상자 «score» 안 IconTextRow «text» 의 글자 조각 — 봇 점수 20~200 이라 이웃 숫자가 있다.</summary>
        [UnityTest]
        public IEnumerator 리그_점수는_숫자_구간이_등폭이다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            h.OpenLeague();
            yield return null;
            Popup p = PopupLayer.Instance.Find(LeagueSheet.Name);
            Assert.IsNotNull(p, "리그 시트가 열린다");

            float em = TabularText.DigitEm(UiFont.Primary);
            int scores = 0, measured = 0;
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Transform tp = t.transform.parent;
                if (tp == null || tp.name != "text" || tp.parent == null || tp.parent.name != "score") continue;   // 상자 «score» → IconTextRow «text» → 글자 조각
                if (!TabularNums.IsWrapped(t.text)) continue;         // 아이콘 조각 옆의 빈 글자 등은 도우미가 안 건드린다
                scores++;
                Assert.IsTrue(t.richText, "점수 «" + t.text + "» — <mspace> 를 쓰려면 richText");
                t.ForceMeshUpdate(true, true);
                TMP_TextInfo info = t.textInfo;
                float cell = em * t.fontSize, tol = cell * 0.15f;
                for (int i = 0; i + 1 < info.characterCount; i++)
                {
                    TMP_CharacterInfo a = info.characterInfo[i], b = info.characterInfo[i + 1];
                    if (!char.IsDigit(a.character) || !char.IsDigit(b.character)) continue;
                    Assert.AreEqual(cell, b.origin - a.origin, tol, "이웃 숫자의 시작 x 간격 = 칸(" + t.text + ")");
                    measured++;
                }
            }
            Assert.Greater(scores, 1, "정본 `.league-score` — 랭킹 행마다 점수(창 8행 + 발 밴드의 내 행)");
            Assert.Greater(measured, 0, "두 자리 이상 점수가 하나는 있어야 간격을 잰다(봇 점수 20~200)");

            LeagueSheet.Close(h);
            yield return null;
        }
    }
}
