using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T334 3회차 ⓑ — 홀드백 대기 구간(정본 `#summon-result-modal.charging` · style.css 6815~6879)이 **정지 프레임이 아니다**.
    ///
    /// 정본이 이 구간을 세 번 고쳐 쓴 까닭이 «480ms 가 통째로 멈춰 보였다» 라, 자도 «섰다» 가 아니라 **«움직였다»** 를 잰다:
    /// 소환진이 부풀며 밝아지고 · 중앙 광원이 뛰고 · 비네트가 조여들고 · 정착한 조연 셀이 광원 쪽으로 빨려든다.
    /// 그 뒤 구간이 끝나면(주역 착지) 정본이 클래스를 떼는 것과 같게 **제자리로 돌아온다**(그 순간은 섬광/와이프가 덮는다).
    /// </summary>
    public class SummonChargeTests
    {
        static IEnumerator Boot()
        {
            PetSkillHost.SuppressSave = true;
            PetSkillHost.Seed = 20260914;
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < 600 && !(SkillPetSheet.Instance != null && SkillPetSheet.Instance.gameObject.scene == active && PetSkillHost.Ready); i++) yield return null;
            Assert.IsNotNull(SkillPetSheet.Instance, "소환 시트가 서지 않았다");
            Assert.IsTrue(PetSkillHost.Ready);
            yield return null;
        }

        /// <summary>홀드백 한 판 — 조연 셋 + 최고 등급 **하나**(정본 `_srHoldback` 조건: 마지막 항목 qty 1).</summary>
        static SkillSummonResultView OpenHoldback()
        {
            var list = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가" },
                new SkillSummonResultView.Entry { Key = "sk:b", IconKey = "sk_fireball", Rarity = "common", Name = "나" },
                new SkillSummonResultView.Entry { Key = "sk:c", IconKey = "sk_fireball", Rarity = "rare", Name = "다" },
                new SkillSummonResultView.Entry { Key = "sk:d", IconKey = "sk_fireball", Rarity = "ultimate", Name = "라" },
            };
            return SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", list, "ultimate", null);
        }

        [UnityTest]
        public IEnumerator 홀드백_대기창은_멈추지_않고_끝나면_제자리로_돌아온다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            Assert.IsNotNull(v, "결과 연출 팝업이 서지 않았다");
            Assert.Greater(v.VigAlpha, -0.5f, "비네트 판(`.sr-wrap::before`)이 서야 한다");
            Assert.AreEqual(0f, v.VigAlpha, 1e-4f, "충전 전에는 비네트가 안 보인다");

            // ⓐ 대기창에 들어갈 때까지 — 마지막 한 칸을 남긴 순간부터가 charging 이다.
            float t = 0f;
            while (!v.Charging && !v.Hero && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Charging, "홀드백이면 마지막 한 칸을 남긴 순간 charging 이어야 한다(정본 720)");

            // 정본은 소환진을 **`.done` 에서만** 등급색으로 물들인다(style.css 7182~7185 · ui.js 551) —
            // 그전에 원색으로 칠해 두면 «원색이 배경에 묻혀 소환진이 사라진다»(ui.js 548) 고 정본이 못 박았고,
            // 밝기 램프도 화면에서 안 읽힌다(궁극의 #ff1c1c 는 빨강이 이미 1.0 이라 CSS 도 거기서 자른다 · 런 512).
            Color rc = PetSkillStyle.Rarity(PetSkillHost.Instance.Data.Defs, "ultimate");
            Assert.Greater(Mathf.Abs(v.FloorColor.b - rc.b), 0.2f, "충전 중 소환진은 아직 등급색이 아니다(정본 기본 푸른 바탕)");

            // ⓑ 구간 안에서 값이 **자란다** — 한 프레임도 같은 그림이 아니다.
            var vig = new List<float>();
            var tickSeen = new List<float>();
            var bright = new List<float>();
            float floorMax = 0f, pulled = 0f, haloMin = 2f, haloMax = -1f, brightMax = -1f;
            int onIdx = -1;
            for (int i = 0; i < v.CellCount; i++) if (v.CellPulledIn(i) >= 0f) { onIdx = i; break; }
            while (v.Charging && t < 8f)
            {
                vig.Add(v.VigAlpha);
                bright.Add(v.FloorBright);
                brightMax = Mathf.Max(brightMax, v.FloorBright);
                floorMax = Mathf.Max(floorMax, v.FloorScale);
                haloMin = Mathf.Min(haloMin, v.HaloAlpha);
                haloMax = Mathf.Max(haloMax, v.HaloAlpha);
                if (onIdx >= 0) pulled = Mathf.Max(pulled, v.CellPulledIn(onIdx));
                if (tickSeen.Count == 0 || Mathf.Abs(tickSeen[tickSeen.Count - 1] - v.TickAlpha) > 1e-4f) tickSeen.Add(v.TickAlpha);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.GreaterOrEqual(vig.Count, 3, "대기창이 몇 프레임은 돌아야 잰 것이 뜻이 있다");
            Assert.Greater(vig[vig.Count - 1], vig[0] + 0.05f, "비네트가 조여들지 않았다 — 이 구간이 다시 정지 프레임이다");
            // 마지막 프레임이 아니라 **구간 안 최댓값**과 견준다: 정본 곡선은 88% 에 밝기 정점을 찍고 100% 로 가며
            // 아주 조금 내려온다(채도가 같이 오르는 탓) — 마지막 한 점만 보면 그 차이에 걸릴 수 있다.
            Assert.Greater(brightMax, bright[0], "소환진 밝기가 안 올랐다(세 성분의 합 — 등급색은 한 성분이 이미 잘려 있다)");
            Assert.Greater(floorMax, 1.03f, "소환진이 부풀지 않았다");
            Assert.Greater(haloMax - haloMin, 0.1f, "중앙 광원이 뛰지 않았다 — «축적» 이 안 읽힌다");
            Assert.Greater(pulled, 0.5f, "정착한 조연 셀이 광원 쪽으로 안 빨려들었다(정본 srinhale)");
            // 룬 눈금(정본 `.sr-floor::after`)은 충전 구간에서 `steps(9)` 로 점등한다 — 계단이라 «자랐다» 가 아니라 «칸이 여럿» 을 본다.
            Assert.IsTrue(v.TickBaked, "룬 눈금은 구운 판이라야 한다(원판을 늘려 쓰면 굵기가 각도마다 달라진다)");
            Assert.GreaterOrEqual(tickSeen.Count, 2, "충전 구간에서 눈금 불투명도가 한 칸도 안 올랐다");

            // ⓒ 구간이 끝나면(주역 착지) 겹은 제자리 — 정본이 `.charging` 클래스를 떼는 것과 같다.
            while (!v.Hero && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return null;
            Assert.IsTrue(v.Hero, "주역이 착지해야 한다");
            Assert.AreEqual(0f, v.VigAlpha, 1e-4f, "구간이 끝나면 비네트는 걷힌다(그 순간은 섬광이 덮는다)");
            Assert.AreEqual(1f, v.FloorScale, 1e-3f, "소환진도 제자리로");

            // 끝까지 가면(`done`) 그때 등급색으로 물든다 — 정본이 승격하는 유일한 순간이다.
            float t2 = 0f;
            while (!v.Done && t2 < 6f) { t2 += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done);
            Assert.AreEqual(rc.r, v.FloorColor.r, 0.02f, "done 에서 소환진이 등급색으로 물든다");
            Assert.AreEqual(rc.b, v.FloorColor.b, 0.02f);
            if (onIdx >= 0) Assert.AreEqual(0f, v.CellPulledIn(onIdx), 1e-3f, "조연 셀도 제자리로");
        }
    }
}
