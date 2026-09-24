using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Forge.Game;
using Forge.Game.Ui;
using Forge.Core.Ui;

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
            // T334 9회차 — 주역 충격파 링(정본 `.sr-cell.heroic::after`)은 셀 **뒤**에 폭 100% 정사각으로 깔린다.
            Assert.IsNotNull(v.HeroRing, "주역 충격파 링이 없다");
            Assert.AreEqual(0, v.HeroRing.transform.GetSiblingIndex(), "링은 셀 뒤(z −1)에 그려진다");
            Assert.IsNotNull(v.HeroRing.sprite, "구운 고리 한 장");
            var rr = (RectTransform)v.HeroRing.transform;
            Assert.AreEqual(rr.rect.width, rr.rect.height, 0.01f, "정사각(aspect-ratio: 1)");
            Assert.Greater(v.RingMax, 1f, "최대 배율은 배치마다 다르되 1보다 크다(셀 경계를 넘어 퍼진다)");
            Assert.AreEqual(rc.r, v.FloorColor.r, 0.02f, "done 에서 소환진이 등급색으로 물든다");
            Assert.AreEqual(rc.b, v.FloorColor.b, 0.02f);
            if (onIdx >= 0) Assert.AreEqual(0f, v.CellPulledIn(onIdx), 1e-3f, "조연 셀도 제자리로");
        }

        /// <summary>
        /// T334 11회차 — 셀별 광원 재점화(정본 `.sr-relight` · style.css 6364~6389).
        ///
        /// 정본이 이 겹을 넣은 근거가 실측이다: 2~4번 셀이 사출되는 900ms 내내 광원 ±20px 평균 휘도가
        /// **시작 프레임 baseline 보다 낮아** 나머지 셀이 «꺼진 광원에서 튀어나오는 물체» 로 읽혔다.
        /// 그래서 자도 «판이 있다» 가 아니라 ⓐ 셀이 뜰 때 **켜졌다가** ⓑ 다시 **꺼지고**
        /// ⓒ 등급이 높을수록 **세고** ⓓ 자리가 **셀이 아니라 광원 한 점**임을 잰다(정본 주석이 못 박은 함정).
        /// </summary>
        [UnityTest]
        public IEnumerator 셀이_뜰_때마다_광원이_그_등급색으로_다시_켜진다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            Assert.AreEqual(4, v.RelightCount, "셀마다 하나씩 있어야 한다(정본 fillSummonRelights 는 _srDelays 를 그대로 훑는다)");

            Image r0 = v.RelightOf(0), r3 = v.RelightOf(3);
            Assert.IsNotNull(r0, "재점화 판이 없다");
            Assert.IsNotNull(r0.sprite, "구운 방사 판 한 장이라야 한다");
            RectTransform rt0 = r0.rectTransform;
            Assert.AreEqual(rt0.rect.width, rt0.rect.height, 0.01f, "정사각(정본 width == height · closest-side)");
            Assert.AreEqual("sr-relights", rt0.parent.name, "셀 안이 아니라 제 층에 산다(정본 ⚠ «셀 안에 넣으면 배율·비행·opacity 가 곱해져 광원에 서지 못한다»)");

            // 자리 — 넷이 **한 점**(광원)에 겹쳐 있다. 셀 자리면 넷이 흩어진다.
            for (int i = 1; i < v.RelightCount; i++)
                Assert.AreEqual(r0.transform.position, v.RelightOf(i).transform.position, "재점화는 셀 자리가 아니라 광원 한 점에 선다");

            // 사다리 — 격자 **바로 아래**(정본 z: 소환진 10 · 천개 20 · 재점화 26 · 격자 40).
            Transform host = rt0.parent, deck = host.parent;
            Transform gridT = deck.Find("sr-grid");
            if (gridT != null)
                Assert.AreEqual(gridT.GetSiblingIndex() - 1, host.GetSiblingIndex(), "재점화는 격자 바로 아래에 깔린다");

            var peak = new float[v.RelightCount];
            float t = 0f;
            while (!v.Done && t < 12f)
            {
                for (int i = 0; i < v.RelightCount; i++)
                {
                    Image ri = v.RelightOf(i);
                    if (ri != null && ri.color.a > peak[i]) peak[i] = ri.color.a;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(v.Done, "연출이 안 끝났다(경과 " + t.ToString("0.00") + "초)");

            Assert.Greater(peak[0], 0f, "첫 셀이 떴는데 광원이 안 켜졌다 — 이 겹이 고치려는 바로 그 증상이다");
            Assert.Greater(peak[3], peak[0], "등급이 오를수록 세게 켜진다(정본 --glow = 0.16 + tier * 0.13)");
            Assert.LessOrEqual(peak[3], 1f, "가산 판의 정점 알파가 1을 넘으면 광원이 하얗게 타 등급색 구분이 무너진다");

            // 꺼진다 — 정본 키프레임의 100% 는 알파 0 이다. 마지막 셀의 .34s 가 지나도록 더 돌린다.
            float t2 = 0f;
            while (t2 < 1.2f) { t2 += Time.unscaledDeltaTime; yield return null; }
            for (int i = 0; i < v.RelightCount; i++)
                Assert.AreEqual(0f, v.RelightOf(i).color.a, 1e-3f, "재점화가 안 꺼졌다 — 결과 화면 가운데에 등급색 얼룩이 남는다(" + i + "번)");
        }

        /// <summary>
        /// T334 12회차 — 착지 스파크(정본 `.sr-spark`). 정본 주석: «링 하나로는 «내려앉았다» 만 말하고
        /// «부딪혔다» 를 말하지 못한다». 자는 ⓐ 셀이 뜰 때 터지고 ⓑ 바깥으로 날아가고 ⓒ 꺼지는지 잰다.
        /// </summary>
        [UnityTest]
        public IEnumerator 셀이_내려앉으면_스파크가_터져_바깥으로_날아가_꺼진다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            Image s0 = v.SparkOf(0), s3 = v.SparkOf(3);
            Assert.IsNotNull(s0, "착지 스파크가 없다");
            Assert.IsNotNull(s0.sprite, "심 + 복제 여덟을 한 장에 구운 판이라야 한다");
            Assert.AreEqual("sr-orbwrap", s0.rectTransform.parent.name, "스파크는 구체 래퍼 안에 산다(정본 z 2)");
            RectTransform rt = s0.rectTransform;
            Assert.AreEqual(rt.rect.width, rt.rect.height, 0.01f, "정사각 판");

            var peakA = new float[4];
            var peakS = new float[4];
            float t = 0f;
            while (!v.Done && t < 12f)
            {
                for (int i = 0; i < 4; i++)
                {
                    Image si = v.SparkOf(i);
                    if (si == null) continue;
                    if (si.color.a > peakA[i]) peakA[i] = si.color.a;
                    float sc = si.rectTransform.localScale.x;
                    if (si.color.a > 0f && sc > peakS[i]) peakS[i] = sc;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(v.Done, "연출이 안 끝났다(경과 " + t.ToString("0.00") + "초)");
            Assert.Greater(peakA[0], 0f, "첫 셀이 내려앉았는데 스파크가 안 터졌다");
            Assert.Greater(peakA[3], peakA[0], "등급이 오를수록 세게 터진다(정본 calc(.45 + .55 * --glow))");
            Assert.Greater(peakS[3], peakS[0], "등급이 오를수록 멀리 날아간다(정본 calc(1.05 + .55 * --glow))");
            Assert.Greater(peakS[0], 0.16f, "제자리에서 안 움직였다 — 배율이 안 붙었다");

            float t2 = 0f;
            while (t2 < 1.2f) { t2 += Time.unscaledDeltaTime; yield return null; }
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(0f, v.SparkOf(i).color.a, 1e-3f, "스파크가 안 꺼졌다 — 구체 위에 흰 점 아홉이 남는다(" + i + "번)");
        }

        /// <summary>
        /// T334 13회차 · §1 «실제 화면을 본다» — x1 요약줄 «✨ 신규 스킬 획득!» 의 머리가 런 743 PNG 에서 **두부(□)** 였다.
        ///
        /// 정본 `TOAST_ICON` 이 «✨ → sparkle» 로 쥐고 있는데 클론이 글자 그대로 세우고 있었다(주인 글꼴에도
        /// 이모지 폴백에도 U+2728 이 없다). `check_text_glyphs` 는 «표에 있으면 아이콘으로 치환된다» 로 빼므로 rc 0 이다 —
        /// 그 자가 못 보는 자리라 이 자가 «아이콘 조각이 실제로 섰는가» 를 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator x1_요약줄의_이모지는_글자가_아니라_아이콘으로_선다()
        {
            yield return Boot();
            var one = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:a", IconKey = "sk_fireball", Rarity = "common", Name = "가", IsNew = true },
            };
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", one, "common", null);
            float t = 0f;
            while (!v.Done && t < 12f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "연출이 안 끝났다(경과 " + t.ToString("0.00") + "초)");
            yield return null;

            Transform solo = v.transform.Find("sr-foot/sr-solo") ?? FindDeep(v.transform, "sr-solo");
            Assert.IsNotNull(solo, "x1 요약 상자(sr-solo)가 없다");
            Transform lineRow = solo.Find("line");
            Assert.IsNotNull(lineRow, "요약 첫 줄이 없다");
            Assert.IsNotNull(lineRow.Find("ico-1"), "«✨» 가 아이콘 조각으로 안 섰다 — 글자 그대로면 화면에 두부(□)가 뜬다");
            var img = lineRow.Find("ico-1").GetComponent<Image>();
            Assert.IsNotNull(img, "아이콘 조각에 그림이 없다");
            Assert.IsNotNull(img.sprite, "T31 아틀라스의 sparkle 이 안 붙었다");
            // 글자 조각에는 이모지가 안 남는다 — 남아 있으면 그것이 그대로 □ 다.
            foreach (TMPro.TextMeshProUGUI tx in lineRow.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                Assert.IsFalse(tx.text.Contains("\u2728"), "글자 조각에 U+2728 이 남았다: «" + tx.text + "»");

            // ⚑ 14회차 — 같은 PNG 에서 나온 둘째 자리: «NEW» 배지가 «NE / W» 로 접혀 알약 밖으로 넘쳤다.
            //   정본 `.sr-new` 는 shrink-to-fit(absolute + padding)이라 폭이 잉크에 딱 맞아 접힐 수가 없다.
            //   자는 그 계약을 그대로 잰다 — **알약이 제 글자보다 넓다**.
            Transform nb = FindDeep(v.transform, "sr-new");
            Assert.IsNotNull(nb, "NEW 배지가 없다");
            TMPro.TextMeshProUGUI nt = nb.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            Assert.IsNotNull(nt, "NEW 배지에 글자가 없다");
            float ink = nt.GetPreferredValues().x;
            float pill = ((RectTransform)nb).rect.width;
            Assert.GreaterOrEqual(pill, ink,
                "NEW 알약(" + pill.ToString("0.0") + ")이 제 잉크(" + ink.ToString("0.0") + ")보다 좁다 — 접혀서 알약 밖으로 넘친다");
        }


        /// <summary>
        /// T334 15회차 — 사출 경로는 **전부 되짚지 않는다**(정본 `UI.SR_EJECT = 0.62` · ui.js 604).
        ///
        /// 정본 주석이 까닭을 이름으로 못 박았다: «벡터를 100% 되짚으면 전 셀이 한 점에서 겹쳐 나와
        /// 5개가 한 덩어리로 보인다 — 일부(EJECT)만 되짚어 «광원 쪽에서 밀려 나온» 인상만 남긴다».
        /// 3회차가 재는 길은 옮겼지만 그 한 줄을 빠뜨려 클론은 100% 를 되짚고 있었다.
        /// </summary>
        [UnityTest]
        public IEnumerator 사출_경로는_광원까지의_일부만_되짚는다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            // ⚑ T455 — 이 기다림이 오래 **제품 결함을 가리고 있었다**. 종전엔 «슬롯 → 광원» 벡터를 `AnimateCharge` 의
            //   **충전 창 안**에서만 쟀기 때문에, 프레임이 길어 그 창(수백 ms)에 한 프레임도 안 들어오면 벡터가 영영 0 이었다
            //   — 그러면 이 자는 6초를 기다리다 빨개지고(런 1102 실측 «경과 6.03초»), **화면에서는** 주역 등장·조연 흡기·잔상이
            //   전부 방향을 잃는다. 이제 그 셈은 **첫 틱에 한 번**(창이 아니라 상태 `ejectSet`) 돌므로 한두 프레임이면 찬다.
            //   기다림 자체는 줄이지 않는다 — 잣대를 조이면 «왜 오래 걸렸나» 를 다음 사람이 다시 못 본다.
            float t = 0f;
            while (v.EjectOf(0) == Vector2.zero && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            Vector2 ej = v.EjectOf(0);
            Assert.AreNotEqual(Vector2.zero, ej, "사출 벡터가 안 재졌다(경과 " + t.ToString("0.00") + "초) — T455 뒤로는 «충전 창» 과 무관하게 첫 틱에 차야 한다");

            Transform halo = FindDeep(v.transform, "halo");
            Transform cell = FindDeep(v.transform, "sr-cell-0");
            Assert.IsNotNull(halo, "광원(halo)이 없다");
            Assert.IsNotNull(cell, "0번 셀이 없다");
            // 런 1181(T458 보탬 3) — `full` 은 셀의 **지금** 자리가 아니라 **슬롯**(Home)에서 잰다. T458 2회차 뒤 셀은 팝 동안 광원 쪽
            //   (`Home + ToLight × fly_f`)에 가 있고, 보탬 2 가 벡터 재기를 첫 틱 앞으로 옮기자 «첫 틱 = 첫 공개» 인 러너에서 이 단언이
            //   그 순간의 셀 자리로 `full` 을 재 0.62/0.38 = 1.632 로 빨갰다(실측 그대로). 슬롯 기준이면 되짚는 비는 표대로 .62 다.
            //   anchoredPosition 과 localPosition 은 상수만큼 어긋나므로(앵커·피벗) 그 차이로 슬롯의 local 자리를 되돌린다.
            RectTransform crt = (RectTransform)cell;
            Vector2 homeLocal = (Vector2)crt.localPosition - (crt.anchoredPosition - v.HomeOf(0));
            Vector2 full = (Vector2)cell.parent.InverseTransformPoint(halo.position) - homeLocal;
            Assert.Greater(full.magnitude, 1f, "광원과 셀이 같은 자리다 — 잴 것이 없다");

            // ⓐ 이 단이 결함을 잡는 단이다 — 100% 되짚으면 여기서 빨개진다.
            Assert.Less(ej.magnitude, full.magnitude * 0.9f,
                "사출 벡터가 광원까지를 거의 다 되짚는다(" + (ej.magnitude / full.magnitude).ToString("0.000")
                + ") — 정본은 SR_EJECT 만큼만 되짚는다");
            // ⓑ 그 «일부» 는 표가 쥔다.
            float want = full.magnitude * SummonFxStyle.L("eject_f");
            Assert.AreEqual(want, ej.magnitude, full.magnitude * 0.02f, "되짚는 비율이 표(eject_f)와 다르다");
        }


        /// <summary>
        /// T334 15회차 ⓑ — 비행 잔상(정본 `.sr-ghost`)이 셀 **뒤**(z 0)에서 광원 쪽으로 뒤처졌다가 따라붙어 사라진다.
        ///
        /// 정본 주석: «잔상은 «아래에서 솟은 자국» 이 아니라 **비행 경로에 끌리는 꼬리** 다 — 셀이 광원 쪽에서
        /// 날아오므로 잔상은 그 뒤쪽(광원 쪽)에 남아 따라붙는다». 그래서 자는 «움직였다» 가 아니라
        /// **«어느 쪽으로»** 를 잰다 — 치우침이 그 셀의 사출 벡터와 **같은 방향**이어야 한다.
        /// </summary>
        [UnityTest]
        public IEnumerator 잔상은_셀_뒤에서_광원_쪽에_뒤처졌다가_따라붙어_사라진다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            Image g0 = v.GhostOf(0);
            Assert.IsNotNull(g0, "비행 잔상이 없다");
            Assert.IsNotNull(g0.sprite, "구운 방사 판 한 장이라야 한다");
            Assert.AreEqual(0, g0.transform.GetSiblingIndex(), "잔상은 구체 **뒤**에 깔린다(정본 z 0)");
            Assert.AreEqual("sr-orbwrap", g0.rectTransform.parent.name, "잔상은 셀 안에 산다(셀의 이동이 이미 곱해진 자리)");

            float peakA = 0f, bestDot = 0f, farthest = 0f;
            float t = 0f;
            while (!v.Done && t < 12f)
            {
                Image gi = v.GhostOf(0);
                if (gi != null && gi.color.a > 0f)
                {
                    if (gi.color.a > peakA) peakA = gi.color.a;
                    Vector2 off = gi.rectTransform.anchoredPosition;
                    Vector2 ej = v.EjectOf(0);
                    if (off.magnitude > farthest)
                    {
                        farthest = off.magnitude;
                        bestDot = ej.sqrMagnitude > 0f ? Vector2.Dot(off.normalized, ej.normalized) : 0f;
                    }
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(v.Done, "연출이 안 끝났다(경과 " + t.ToString("0.00") + "초)");
            Assert.Greater(peakA, 0f, "첫 셀이 날아왔는데 꼬리가 안 켜졌다");
            Assert.Greater(farthest, 0f, "꼬리가 제자리에만 있었다 — 궤적이 안 남는다");
            Assert.Greater(bestDot, 0.9f,
                "꼬리가 사출 벡터와 다른 쪽으로 밀렸다(코사인 " + bestDot.ToString("0.00") + ") — 정본은 «광원 쪽 뒤» 다");

            // ⚑ 18회차 — 끝난 뒤의 잔잔한 고리(정본 `.sr-idle`)는 **`done` 에서만** 돈다.
            Assert.AreEqual(2, v.IdleRingCount, "정본은 고리 둘이다");
            Image ir0 = v.IdleRingOf(0);
            Assert.IsNotNull(ir0, "잔잔한 고리가 없다");
            Assert.IsNotNull(ir0.sprite, "구운 고리 판이 없다");

            float t2 = 0f;
            float idlePeak = 0f, idleWide = 0f;
            while (t2 < 1.2f)
            {
                for (int i = 0; i < v.IdleRingCount; i++)
                {
                    Image ii = v.IdleRingOf(i);
                    if (ii == null) continue;
                    if (ii.color.a > idlePeak) idlePeak = ii.color.a;
                    float sc = ii.rectTransform.localScale.x;
                    if (ii.color.a > 0f && sc > idleWide) idleWide = sc;
                }
                t2 += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Greater(idlePeak, 0f, "끝난 뒤에 고리가 안 돈다 — 정본은 `.done` 에서만 켠다");
            Assert.Greater(idleWide, 0.42f, "고리가 안 퍼졌다");
            for (int i = 0; i < 4; i++)
            {
                Image gi = v.GhostOf(i);
                Assert.AreEqual(0f, gi.color.a, 1e-3f, "꼬리가 안 꺼졌다 — 결과 화면에 흐린 원이 남는다(" + i + "번)");
                Assert.AreEqual(Vector2.zero, gi.rectTransform.anchoredPosition, "꼬리가 본체 자리로 안 돌아왔다(" + i + "번)");
            }
        }


        /// <summary>
        /// T334 16회차 — 등급 챕터 펄스(정본 `.sr-tierpulse`)는 **대량 판(&gt;10셀)의 등급 경계**에만 선다.
        ///
        /// 정본 주석이 이 겹의 존재 이유를 실측으로 적었다: «링만으로는 화면 평균 휘도가 안 움직인다 —
        /// 챕터가 바뀌는 순간 화면 전체가 그 등급색으로 한 번 달아올랐다 식는다». 그래서 자도
        /// «판이 있다» 가 아니라 ⓐ 경계 수 ⓑ 예고가 경계 셀보다 **앞선다** ⓒ 등급이 오를수록 세다 ⓓ 식는다 를 잰다.
        /// </summary>
        [UnityTest]
        public IEnumerator 등급_경계마다_화면이_그_등급색으로_한_번_달아오른다()
        {
            yield return Boot();
            var many = new List<SkillSummonResultView.Entry>();
            string[] rar = { "common", "common", "common", "common", "common", "common",
                             "rare", "rare", "rare", "rare", "ultimate", "ultimate" };
            for (int i = 0; i < rar.Length; i++)
                many.Add(new SkillSummonResultView.Entry { Key = "sk:x" + i, IconKey = "sk_fireball", Rarity = rar[i], Name = "가" + i });
            SkillSummonResultView v = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", many, "ultimate", null);

            Assert.AreEqual(2, v.TierBreakCount, "등급이 두 번 바뀐다(일반 → 희귀 → 궁극)");
            Assert.Greater(v.TierBreakAt(0), 0f, "첫 경계 시각이 안 잡혔다");
            Assert.Greater(v.TierBreakAt(1), v.TierBreakAt(0), "경계는 차례대로 온다");
            Image p0 = v.TierPulseOf(0), p1 = v.TierPulseOf(1);
            Assert.IsNotNull(p0, "챕터 펄스 판이 없다");
            Assert.IsNotNull(p0.sprite, "구운 타원 판 한 장이라야 한다");

            // 화면 크기라야 뜻이 산다 — 격자가 아니라 판 전체를 덮는다(정본 inset -2%).
            Transform grid = FindDeep(v.transform, "sr-grid");
            Assert.IsNotNull(grid);
            Assert.Greater(p0.rectTransform.rect.width, ((RectTransform)grid).rect.width,
                "챕터 펄스가 격자보다 좁다 — «화면 전체가 달아오른다» 가 안 된다");

            // 링(정본 `.sr-tierflash`)과 그 심지 — 펄스와 같은 경계에 한 쌍으로 선다.
            Image r0 = v.TierRingOf(0), w0 = v.TierWickOf(0);
            Assert.IsNotNull(r0, "챕터 링이 없다");
            Assert.IsNotNull(r0.sprite, "구운 고리 판이 없다");
            Assert.IsNotNull(w0, "링 심지가 없다 — 정본 «링만 있으면 «테두리 원» 이다»");
            Assert.AreEqual(r0.rectTransform.rect.width, r0.rectTransform.rect.height, 0.01f, "정사각 판");

            var peak = new float[2];
            var seenWin = new int[2];      // 그 경계의 구간 안에 프레임이 몇 번 들어왔나
            float ringPeak = 0f, ringWide = 0f, wickPeak = 0f, worstGap = 0f;
            // T422 — 심지(`srtierwick`)는 `flash_ms` 의 **0~45%** 만 산다(정점 14%) — 같은 칸의 링(0~100%)보다 창이 절반 이하다.
            //   그 창에 프레임이 한 번도 안 들어오면 «안 켜졌다» 가 되는데 그것은 **게임이 아니라 런의 사정**이다(런 911 실측).
            //   그래서 창 안 프레임을 같이 세고, 0 이면 T386 이 세운 «환경» 갈래로 접는다(값은 그대로 둔다 · 결정 677).
            int seenWick = 0;
            float flashMs = (float)SummonFxStyle.TierBreak.FlashMs;
            float wickEndMs = 0f;
            for (int k = 1; k <= 100; k++)
            {
                float x = flashMs * k / 100f;
                if (SummonFxStyle.TierBreak.WickAt(x) > 0) wickEndMs = x;   // 표가 쥔 창 — 자에 수를 박지 않는다
            }
            var seen = new List<Sprite>();
            float t = 0f;
            float pulseMs = (float)SummonFxStyle.TierBreak.PulseMs;
            while (!v.Done && t < 20f)
            {
                if (Time.unscaledDeltaTime > worstGap) worstGap = Time.unscaledDeltaTime;
                for (int i = 0; i < 2; i++)
                {
                    Image pi = v.TierPulseOf(i);
                    if (pi == null) continue;
                    if (pi.color.a > peak[i]) peak[i] = pi.color.a;
                    // ⚑ 런 828 이 여기서 빨갰다 — 굽기가 Open 한 프레임에 몰려 **구간을 통째로 건너뛰었다**.
                    //   그래서 «프레임이 구간 안에 들어오기는 했는가» 를 같이 세어 실패 문구에 싣는다.
                    float el = t * 1000f - v.TierBreakAt(i);
                    if (el >= 0f && el <= pulseMs) seenWin[i]++;
                }
                Image ri = v.TierRingOf(0);
                if (ri != null && ri.color.a > 0f)
                {
                    if (ri.color.a > ringPeak) ringPeak = ri.color.a;
                    float sc = ri.rectTransform.localScale.x;
                    if (sc > ringWide) ringWide = sc;
                    if (ri.sprite != null && !seen.Contains(ri.sprite)) seen.Add(ri.sprite);
                }
                Image wi = v.TierWickOf(0);
                if (wi != null && wi.color.a > wickPeak) wickPeak = wi.color.a;
                float wel = t * 1000f - v.TierBreakAt(0);
                if (wel >= 0f && wel <= wickEndMs) seenWick++;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(v.Done, "연출이 안 끝났다(경과 " + t.ToString("0.00") + "초)");
            // T431 2회차 — 런 971 실측: 이 줄이 «첫 챕터의 540ms 구간에 프레임이 한 번도 안 들어왔다(가장 긴 프레임 2029ms)» 로 빨갰다.
            //   문구가 이미 «잴 기회가 없었다» 라 말하는데 단언은 빨강이었다 — 같은 칸의 심지(T422 ⓐ)와 같이 «환경» 으로 접는다(값은 그대로).
            //   둘째 경계의 구간도 같이 본다: 거기에 프레임이 0 이면 아래 «등급이 오를수록 세다» 가 0 을 놓고 비교한다.
            if (seenWin[0] == 0 || seenWin[1] == 0)
                Assert.Ignore("환경 — 챕터 펄스 구간(" + pulseMs.ToString("0") + "ms)에 프레임이 한 번도 안 들어온 경계가 있다(첫 " + seenWin[0]
                    + "번 · 둘째 " + seenWin[1] + "번 · 가장 긴 프레임 " + (worstGap * 1000f).ToString("0")
                    + "ms) — 달아올랐는지 잴 기회가 없었다(T431 · T422 의 심지와 같은 갈래)");
            Assert.Greater(peak[0], 0f, "첫 챕터가 안 달아올랐다(구간 안 프레임 " + seenWin[0] + "번)");
            Assert.Greater(peak[1], peak[0], "등급이 오를수록 세다(정본 --pk = .15 + tier * .04)");
            Assert.LessOrEqual(peak[1], 1f, "가산 판의 정점이 1을 넘으면 화면이 하얗게 탄다");

            Assert.Greater(ringPeak, 0f, "챕터 링이 안 켜졌다");
            Assert.Greater(ringWide, 1f, "링이 셀 경계를 넘어 안 퍼졌다");
            if (seenWick == 0)
                Assert.Ignore("환경 — 심지 창(" + wickEndMs.ToString("0") + "ms · flash " + flashMs.ToString("0")
                    + "ms 의 0~45%)에 프레임이 한 번도 안 들어왔다(가장 긴 프레임 " + (worstGap * 1000f).ToString("0")
                    + "ms) — 잴 기회가 없었다(T422 · 링은 창이 " + flashMs.ToString("0") + "ms 라 같은 런에서도 잡힌다)");
            Assert.Greater(wickPeak, 0f, "심지가 안 켜졌다 — 중심이 달아올라야 광원 문법에 앉는다(창 안 프레임 "
                + seenWick + "번 · 가장 긴 프레임 " + (worstGap * 1000f).ToString("0") + "ms)");
            // ⓐ 이 단이 «한 장을 배율로 날리기» 와 «단계마다 갈아 끼우기» 를 가른다.
            Assert.Greater(seen.Count, 1,
                "링이 처음부터 끝까지 **같은 판**이었다(" + seen.Count + "장) — 테 굵기가 안 변했다는 뜻이다(정본 «하드엣지 고정 굵기는 그래픽 스탬프다»)");

            float t2 = 0f;
            while (t2 < 1.2f) { t2 += Time.unscaledDeltaTime; yield return null; }
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(0f, v.TierPulseOf(i).color.a, 1e-3f, "챕터 펄스가 안 식었다 — 화면이 등급색으로 물든 채 굳는다(" + i + "번)");
                Assert.AreEqual(0f, v.TierRingOf(i).color.a, 1e-3f, "챕터 링이 안 사라졌다(" + i + "번)");
                Assert.AreEqual(0f, v.TierWickOf(i).color.a, 1e-3f, "심지가 안 꺼졌다(" + i + "번)");
            }
        }


        /// <summary>
        /// T334 19회차 — 예고 충격파 한 쌍(정본 `.sr-shock` · `.echo`).
        ///
        /// 정본 주석이 셋을 못 박았다: «빛이 터지는 정점에 나가야 한다 — 0ms 에 터지면 아무것도 없는 화면에서
        /// 링만 먼저 퍼진다» · «압력파가 하나면 «링 애니메이션», 둘이면 «터진 것» 으로 읽힌다 — **형제 요소**로 둔다» ·
        /// «최종 반경도 굴림 에너지에 물린다 — 75개를 뽑았는데 1개와 같은 크기로 터지면 안 된다».
        /// 마지막 것이 이 자의 핵심 단이다: **굴림 수가 다른 두 판의 에너지가 다르다**(같은 셀 수여도).
        /// </summary>
        [UnityTest]
        public IEnumerator 예고_충격파는_정점에_터져_잔파가_뒤따르고_굴림_수에_물린다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            Assert.IsNotNull(v.ShockMain, "예고 충격파 본파가 없다");
            Assert.IsNotNull(v.ShockEcho, "잔파가 없다 — 하나면 «링 애니메이션» 으로 읽힌다");
            Assert.AreNotSame(v.ShockMain.transform.parent, v.ShockMain.transform, "형제 계약");
            Assert.AreSame(v.ShockMain.transform.parent, v.ShockEcho.transform.parent, "둘은 **형제**다(정본 «::after 로 두면 배율이 중첩된다»)");
            Assert.IsNotNull(v.ShockMain.sprite);
            // 첫 프레임에는 아직 안 터졌다 — 지연이 있다.
            Assert.AreEqual(0f, v.ShockMain.color.a, 1e-3f, "0ms 에 터지면 아무것도 없는 화면에서 링만 먼저 퍼진다");

            float mainPeak = 0f, echoPeak = 0f, mainWide = 0f, mainPeakAt = -1f, echoPeakAt = -1f;
            float chargePeak = 0f, chargePeakAt = -1f, chargePeakScale = 0f;
            float streakPeak = 0f, streakFar = 0f, streakNear = float.MaxValue;
            // ⚑ 24회차 — 23회차가 런 872·875 에서 같은 자리로 빨갔다: 알파는 켜졌는데 «오른 높이» 가 **정확히 0** 이었다.
            //   둘이 같이 서려면 ⓐ 자리가 아예 안 써지거나 ⓑ 시각이 안 흐르거나 ⓒ 표본이 없어야 한다 —
            //   그러니 «못 올랐다» 만 외치지 말고 **숫자를 들고** 울자(집·처음·폭·알파·예상값·프레임·시각 창).
            var mq = new List<int>();
            for (int q = 0; q < v.MoteCount; q += 7) mq.Add(q);   // 80개를 프레임마다 다 훑지 않는다(자도 60fps 를 지킨다)
            int mn = mq.Count;
            float[] mFirst = new float[mn], mMin = new float[mn], mMax = new float[mn], mLastA = new float[mn];
            bool[] mSeen = new bool[mn];
            float motePeak = 0f, moteRose = 0f, mMsFirst = -1f, mMsLast = -1f;
            int mFrames = 0;
            System.Action Sample = () =>
            {
                mFrames++;
                mMsLast = v.ElapsedMs;
                if (mMsFirst < 0f) mMsFirst = mMsLast;
                for (int k = 0; k < mn; k++)
                {
                    Image mi2 = v.MoteOf(mq[k]);
                    if (mi2 == null) continue;
                    float yy = v.MoteAt(mq[k]).y;
                    mLastA[k] = mi2.color.a;
                    if (mi2.color.a > motePeak) motePeak = mi2.color.a;
                    if (!mSeen[k]) { mSeen[k] = true; mFirst[k] = yy; mMin[k] = yy; mMax[k] = yy; continue; }
                    if (yy < mMin[k]) mMin[k] = yy;
                    if (yy > mMax[k]) mMax[k] = yy;
                    float rose = yy - mFirst[k];
                    if (rose > moteRose) moteRose = rose;
                }
            };
            var seen = new List<Sprite>();
            // T431 — 빛줄기 창을 **표에서 자가 스스로 잰다**(자에 수를 안 박는다 · T422 ⓐ 와 같은 길): 스포크 지연은 0~DelayModMs−1,
            //   길이는 streak_ms — «지연 0 의 첫 켜짐 ~ 지연 최대의 마지막 켜짐» 을 1ms 눈금으로 훑는다. 런 966 은 프레임 하나가 402ms 라
            //   이 창(≈240ms)을 통째로 건너 «빛줄기가 안 켜졌다» 로 빨갰다 — 창 안 프레임이 0 이면 잰 것이 아니라 못 잰 것이다.
            var ssp = SummonFxStyle.Streaks;
            float streakWinStart = -1f, streakWinEnd = -1f;
            for (int ms = 0; ms < 10000; ms++)
            {
                double sa0, sd0, sy0, saN, sdN, syN;
                ssp.At(ms, 0, ssp.RBaseRem, out sa0, out sd0, out sy0);
                ssp.At(ms, ssp.DelayModMs - 1, ssp.RBaseRem, out saN, out sdN, out syN);
                if (streakWinStart < 0f && (sa0 > 0 || saN > 0)) streakWinStart = ms;
                if (sa0 > 0 || saN > 0) streakWinEnd = ms;
            }
            Assert.GreaterOrEqual(streakWinStart, 0f, "표의 빛줄기가 어느 순간에도 안 켜진다 — 표(streaks)가 이상하다");
            int seenStreakWin = 0;
            // T443 — **속창**(모든 스포크가 함께 켜져 있는 구간) = [창시작 + 지연최대, 창시작 + streak_ms].
            //   창(`streakWinStart`~`streakWinEnd`)은 «첫 스포크가 켜짐 ~ 마지막 스포크가 꺼짐» 이라 **가장자리엔 켜진 것이 거의 없다** —
            //   거기 한 표본이 앉으면 `streakPeak` 이 0 에 가깝게 나오고, 그것은 «연출이 없다» 가 아니라 «그 순간엔 없었다» 다.
            //   T431 의 `seenStreakWin > 0` 은 그 둘을 못 갈랐다(런 1041: 창 1~238ms 에 프레임 **1번** · 가장 긴 프레임 **237ms** — 창 하나에 표본 하나).
            float streakCoreStart = streakWinStart + (ssp.DelayModMs - 1), streakCoreEnd = streakWinStart + (float)ssp.Ms;
            int seenStreakCore = 0;
            float streakWorstGap = 0f;
            // §0-6 보탬(런 1173) — 빛 모임(`.sr-charge`)의 **봉우리 창**(표 알파 > .5 인 구간 · 열린 뒤 ms)도 같은 길로 표에서 잰다.
            //   런 1173: 1.01초 동안 프레임이 드물어 그 창에 표본이 0 → «빛 모임이 정점까지 안 갔다(0.0)» 로 빨갰다 — 못 잰 것이지 안 간 것이 아니다.
            var cbsp = SummonFxStyle.ChargeBurst;
            float burstCoreStart = -1f, burstCoreEnd = -1f;
            for (int ms = 0; ms < 10000; ms++)
            {
                double ba, bs;
                cbsp.At(ms, v.PreK, v.SrEnergy, out ba, out bs);
                if (ba > 0.5) { if (burstCoreStart < 0f) burstCoreStart = ms; burstCoreEnd = ms; }
            }
            Assert.GreaterOrEqual(burstCoreStart, 0f, "표의 빛 모임이 어느 순간에도 .5 를 안 넘는다 — 표(charge_burst)가 이상하다");
            int seenBurstCore = 0;
            // T365 27회차 §0-6 보탬(런 1258) — 본파의 **판 갈아 끼움**도 같은 길로 표에서 잰다: 본파는 `shock_delay_ms` 부터 `shock_ms` 동안
            //   `steps` 칸으로 나뉘어 **칸 경계에서만** 판을 갈아 끼운다(제품 `AnimateShock` → `MainStepOf`). 런 1258 은 가장 긴 프레임이 ≈900ms 라
            //   그 창(230~690ms)에 앉은 프레임이 전부 **한 칸**이었고 «본파가 처음부터 끝까지 같은 판이었다» 로 빨갰다 — 못 잰 것이지 안 줄어든 것이 아니다.
            //   프레임 시각이 **서로 다른 칸 둘 이상**에 앉았을 때만 «판이 바뀌었나» 를 묻고, 아니면 나머지를 다 본 뒤 맨 끝에서 «환경» 으로 접는다.
            var shsp = SummonFxStyle.Shock;
            var shockStepsHit = new HashSet<int>();
            int seenShockWin = 0;
            float t = 0f;
            while (!v.Done && t < 12f)
            {
                if (Time.unscaledDeltaTime > streakWorstGap) streakWorstGap = Time.unscaledDeltaTime;
                if (v.ElapsedMs >= streakWinStart && v.ElapsedMs <= streakWinEnd) seenStreakWin++;
                if (v.ElapsedMs >= streakCoreStart && v.ElapsedMs <= streakCoreEnd) seenStreakCore++;
                if (v.ElapsedMs >= burstCoreStart && v.ElapsedMs <= burstCoreEnd) seenBurstCore++;
                Image mi = v.ShockMain, ei = v.ShockEcho;
                if (v.ElapsedMs >= shsp.DelayMs && v.ElapsedMs < shsp.DelayMs + shsp.Ms)
                {
                    seenShockWin++;
                    shockStepsHit.Add(shsp.MainStepOf(v.ElapsedMs));
                }
                if (mi != null && mi.color.a > 0f)
                {
                    if (mi.color.a > mainPeak) { mainPeak = mi.color.a; mainPeakAt = t; }
                    float sc = mi.rectTransform.localScale.x;
                    if (sc > mainWide) mainWide = sc;
                    if (mi.sprite != null && !seen.Contains(mi.sprite)) seen.Add(mi.sprite);
                }
                if (ei != null && ei.color.a > echoPeak) { echoPeak = ei.color.a; echoPeakAt = t; }
                for (int q = 0; q < v.StreakCount; q++)
                {
                    Image si = v.StreakOf(q);
                    if (si == null || si.color.a <= 0f) continue;
                    if (si.color.a > streakPeak) streakPeak = si.color.a;
                    float d = v.StreakDist(q);
                    if (d > streakFar) streakFar = d;
                    if (d < streakNear) streakNear = d;
                }
                Sample();
                Image ci = v.ChargeBurst;
                if (ci != null && ci.color.a > chargePeak)
                {
                    chargePeak = ci.color.a; chargePeakAt = t;
                    chargePeakScale = ci.rectTransform.localScale.x;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            // ⚑ 24회차 — 정본은 깊이 평면을 **끝없이**(`infinite`) 돌린다 — `done` 이 됐다고 멈추지 않는다.
            //   창이 짧아 못 본 것이 아니게 끝난 뒤에도 더 본다(이 자체가 정본 계약이다).
            float tm = 0f;
            while (tm < 0.6f) { Sample(); tm += Time.unscaledDeltaTime; yield return null; }
            // ⚑ 21회차 — 빛 모임(정본 `.sr-charge`)은 충격파보다 **먼저** 정점을 찍는다(충격파가 그 정점에 나간다).
            Assert.IsNotNull(v.ChargeBurst, "빛 모임 판이 없다");
            Assert.IsNotNull(v.ChargeBurst.sprite, "구운 방사 판 한 장이라야 한다");
            Assert.AreEqual(v.ChargeBurst.rectTransform.rect.width, v.ChargeBurst.rectTransform.rect.height, 0.01f,
                "정사각이라야 한다(정본 `aspect-ratio: 1`)");

            Assert.Greater(mainPeak, 0f, "본파가 안 터졌다");
            Assert.Greater(echoPeak, 0f, "잔파가 안 터졌다");
            Assert.Greater(mainWide, 1f, "압력파가 안 퍼졌다");
            // T422 — 두 정점이 **같은 프레임**에서 잡혔으면 그 프레임이 둘 사이(정본 .23s ↔ .30s = 70ms)보다 길었다는 뜻이다:
            //   그때는 «차례» 를 잴 수 없으므로 프레임 시각 대신 **규칙**으로 묻는다 — Core `SummonShockSpec` 은 적재 때
            //   `EchoDelayMs > DelayMs` 를 강제한다(어기면 FormatException). 프레임이 갈렸을 때만 종전대로 순서를 본다.
            Assert.Greater(SummonFxStyle.Shock.EchoDelayMs, SummonFxStyle.Shock.DelayMs,
                "정본 6141·6162 — 잔파는 본파보다 늦게 나간다(.30s ↔ .23s)");
            if (mainPeakAt == echoPeakAt)
                Debug.Log("[T422] 본파·잔파 정점이 한 프레임(" + mainPeakAt.ToString("0.000")
                    + "초)에 같이 잡혔다 — 프레임이 둘 사이 " + (SummonFxStyle.Shock.EchoDelayMs - SummonFxStyle.Shock.DelayMs).ToString("0")
                    + "ms 보다 길다: 차례는 규칙(EchoDelayMs > DelayMs)으로 판정한다");
            else
                Assert.Less(mainPeakAt, echoPeakAt, "잔파가 본파보다 먼저 정점을 찍었다 — 차례가 뒤집혔다");
            if (shockStepsHit.Count > 1)   // T365 27회차 §0-6 보탬 — 프레임이 서로 다른 칸에 앉았을 때만 «판이 바뀌었나» 를 묻는다
                Assert.Greater(seen.Count, 1, "본파가 처음부터 끝까지 같은 판이었다 — 테 굵기가 안 줄었다는 뜻이다(프레임이 앉은 칸 "
                    + shockStepsHit.Count + "/" + shsp.Steps + " · 창 안 프레임 " + seenShockWin + "번)");
            // ⚑ 23회차 — 깊이 평면 셋(빛가루 18 · 먼지 48 · 보케 14 = 80)은 **끊임없이** 떠오른다.
            Assert.AreEqual(18 + 48 + 14, v.MoteCount, "정본 개수(18+48+14)와 다르다");
            // 울 때 내민 숫자 — 본 것과 **표가 이러야 한다는 값**을 나란히 둔다.
            //   알파가 예상과 같은데 폭이 0 이면 «자리만 안 써졌다» 고, 둘 다 어긋나면 «시각이 안 흐른다» 다.
            string mdbg = "표본 " + mn + "/" + v.MoteCount + " · 프레임 " + mFrames
                + " · " + mMsFirst.ToString("0") + "~" + mMsLast.ToString("0") + "ms";
            for (int k = 0; k < mn; k++)
            {
                int q = mq[k];
                var LL = q < 18 ? SummonFxStyle.Particles.Motes : (q < 66 ? SummonFxStyle.Particles.Dust : SummonFxStyle.Particles.Near);
                int li = q < 18 ? q : (q < 66 ? q - 18 : q - 66);
                double pa, ptx, pty, psc;
                LL.At(li, mMsLast, out pa, out ptx, out pty, out psc);
                mdbg += " | " + q + ": 집" + v.MoteHomeOf(q).y.ToString("0.0")
                    + " 처음" + mFirst[k].ToString("0.0") + " 폭" + (mMax[k] - mMin[k]).ToString("0.00")
                    + " a" + mLastA[k].ToString("0.000") + "(예" + pa.ToString("0.000") + ")"
                    + " ty예" + pty.ToString("0.00");
            }
            Assert.Greater(motePeak, 0f, "입자가 한 번도 안 켜졌다 — " + mdbg);
            Assert.Greater(moteRose, 0f, "입자가 안 떠올랐다(자리가 안 움직였다) — " + mdbg);

            // ⚑ 22회차 — 수렴 빛줄기는 바깥에서 광원으로 **모여든다**(퍼지면 반대 연출이 된다).
            Assert.GreaterOrEqual(v.StreakCount, 9, "빛줄기가 하한보다 적다");
            Assert.LessOrEqual(v.StreakCount, 24, "빛줄기가 상한을 넘었다");
            Assert.IsNotNull(v.StreakOf(0));
            Assert.IsNotNull(v.StreakOf(0).sprite, "막대 한 장을 나눠 써야 한다");
            Assert.AreSame(v.StreakOf(0).sprite, v.StreakOf(v.StreakCount - 1).sprite, "스포크마다 따로 구우면 안 된다(결정 691)");
            // T431 — 창 안에 프레임이 한 번이라도 들어왔을 때만 «켜졌나·모여들었나» 를 묻는다. 0 번이면 잴 기회가 없었던 것이라
            //   이 두 단언만 건너뛰고 나머지는 다 본 뒤 맨 끝에서 «환경» 으로 접는다(T386 갈래 · 사유에 창·프레임 수·가장 긴 프레임).
            if (seenStreakCore > 0)   // T443 — 창이 아니라 **속창**에 표본이 들었을 때만 «켜졌나·모여들었나» 를 묻는다
            {
                Assert.Greater(streakPeak, 0f, "빛줄기가 안 켜졌다(창 " + streakWinStart.ToString("0") + "~" + streakWinEnd.ToString("0")
                    + "ms 안 프레임 " + seenStreakWin + "번 · 가장 긴 프레임 " + (streakWorstGap * 1000f).ToString("0") + "ms)");
                Assert.Greater(streakFar, streakNear, "빛줄기가 광원으로 안 모여들었다(먼 " + streakFar.ToString("0")
                    + " → 가까운 " + streakNear.ToString("0") + ")");
            }
            for (int i = 0; i < v.StreakCount; i++)
                Assert.AreEqual(0f, v.StreakOf(i).color.a, 1e-3f, "빛줄기가 안 꺼졌다 — 아이콘 줄 위에 흰 막대가 남는다(" + i + "번)");

            if (seenBurstCore > 0)   // §0-6 보탬 — 봉우리 창에 표본이 들었을 때만 «정점까지 갔나» 를 묻는다(T443 의 속창과 같은 길)
            {
                Assert.Greater(chargePeak, 0.5f, "빛 모임이 정점까지 안 갔다(경과 " + t.ToString("0.00") + "초 · 봉우리 창 " + burstCoreStart.ToString("0") + "~" + burstCoreEnd.ToString("0") + "ms 안 프레임 " + seenBurstCore + "번)");
                Assert.Greater(chargePeakScale, 0f);
                Assert.Less(chargePeakAt, echoPeakAt, "빛 모임의 정점이 잔파보다 늦다 — 정본은 그 정점에 충격파가 나간다");
            }
            Assert.AreEqual(0f, v.ChargeBurst.color.a, 1e-3f, "빛 모임이 안 꺼졌다 — 화면에 흰 원이 남는다");
            Assert.AreEqual(0f, v.ShockMain.color.a, 1e-3f, "본파가 안 사라졌다");
            Assert.AreEqual(0f, v.ShockEcho.color.a, 1e-3f, "잔파가 안 사라졌다");

            // ⚑ 정본이 이름으로 경고한 자리 — 에너지는 **셀 수가 아니라 굴림 수**로 잰다.
            //   같은 «한 셀» 이라도 ×1 과 ×20 은 에너지가 달라야 한다.
            // ⚑ 런 828 에서 이 단이 빨갰다 — **자의 전제가 틀렸다**: `Open` 은 «묶기 전 굴림 목록» 을 받고
            //   `Group` 이 같은 Key 를 세어 `Qty` 를 **다시 매긴다**. 손으로 준 Qty 는 버려진다.
            //   그러니 ×20 을 흉내 내려면 같은 Key 를 **스무 줄** 넣어야 한다(제품 쪽 «Qty 의 합» 은 옳았다).
            var one = new List<SkillSummonResultView.Entry>
            {
                new SkillSummonResultView.Entry { Key = "sk:z", IconKey = "sk_fireball", Rarity = "common", Name = "하나" },
            };
            var many = new List<SkillSummonResultView.Entry>();
            for (int k = 0; k < 20; k++)
                many.Add(new SkillSummonResultView.Entry { Key = "sk:z", IconKey = "sk_fireball", Rarity = "common", Name = "스물" });
            v.OnTap(); v.OnTap();
            yield return null;
            SkillSummonResultView v1 = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", one, "common", null);
            float e1 = v1.SrEnergy;
            v1.OnTap(); v1.OnTap();
            yield return null;
            SkillSummonResultView v2 = SkillSummonResultView.Open(SkillPetSheet.Instance, "skill", many, "common", null);
            float e2 = v2.SrEnergy;
            Assert.AreEqual(0f, e1, 1e-4f, "×1 의 에너지는 0 이다");
            Assert.Greater(e2, e1, "같은 한 셀이어도 ×20 은 ×1 보다 세야 한다 — 셀 수로 재면 둘이 같아진다(정본이 이름으로 경고한 자리)");
            if (seenBurstCore == 0)
                Assert.Ignore("환경 — 빛 모임 **봉우리 창**(" + burstCoreStart.ToString("0") + "~" + burstCoreEnd.ToString("0")
                    + "ms · 표 알파 > .5 인 구간)에 프레임이 한 번도 안 들어왔다(가장 긴 프레임 " + (streakWorstGap * 1000f).ToString("0")
                    + "ms) — 잴 기회가 없었다(나머지 단언은 전부 지났다)");
            if (shockStepsHit.Count <= 1)
                Assert.Ignore("환경 — 본파 창(" + shsp.DelayMs.ToString("0") + "~" + (shsp.DelayMs + shsp.Ms).ToString("0") + "ms · " + shsp.Steps
                    + " 칸)에 프레임이 " + seenShockWin + "번 · 칸 " + shockStepsHit.Count + " 에만 앉았다(가장 긴 프레임 "
                    + (streakWorstGap * 1000f).ToString("0") + "ms) — 판 갈아 끼움을 잴 기회가 없었다(T365 27회차 · 나머지 단언은 전부 지났다)");
            if (seenStreakCore == 0)
                Assert.Ignore("환경 — 빛줄기 **속창**(" + streakCoreStart.ToString("0") + "~" + streakCoreEnd.ToString("0")
                    + "ms · 모든 스포크가 함께 켜진 구간 · 창 전체는 " + streakWinStart.ToString("0") + "~" + streakWinEnd.ToString("0")
                    + "ms · 지연 0~" + (ssp.DelayModMs - 1) + "ms + streak_ms " + ssp.Ms.ToString("0")
                    + ")에 프레임이 한 번도 안 들어왔다(창 전체엔 " + seenStreakWin + "번 · 가장 긴 프레임 "
                    + (streakWorstGap * 1000f).ToString("0") + "ms) — 잴 기회가 없었다(T443 · 나머지 단언은 전부 지났다)");
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
        /// <summary>T459 ⓧ — 정본 5657 `.sr-wrap { animation: srshake .25s … both }` + 6148 `animation-delay: .21s`: 소환이 열리면 판이 210~460ms 창에서
        /// 한 번 흔들리고 제자리로 돌아온다(그 뒤 주역 킥 `srshakehit` 은 별개 · 지연 0 이 맞다 — T33 38회차). 창에 프레임이 안 들어오면 «환경» 으로 접는다.</summary>
        [UnityTest]
        public IEnumerator 소환이_열리면_판이_210ms_뒤_한_번_흔들리고_제자리로_돌아온다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            RectTransform w = v.Wrap;   // 정본 `.sr-wrap` = 클론 `handle.Content`(이름이 다르다 — 런 1137 에서 이름으로 찾다 빨갰다)
            Assert.IsNotNull(w, "판(정본 .sr-wrap · 클론 handle.Content)");
            // §0-6 보탬(런 1149·1183) — 창은 **뷰의 시계**(`ElapsedMs` · 열린 순간 = `enterAt`)로 잰다. 종전엔 «열고 한 프레임 뒤» 의 t0 로 쟀는데
            //   느린 러너에선 그 한 프레임이 수백 ms 라 자의 창(215~455ms)이 실제 셰이크(열림 뒤 210~460ms)보다 늦게 앉아 «창 안 프레임 6 · 움직인 0» 로 빨갰다.
            Vector2 home = w.anchoredPosition;
            if (v.ElapsedMs >= SummonFxStyle.Enter.DelayMs)
                Assert.Ignore("환경 — 열고 첫 프레임까지 " + v.ElapsedMs.ToString("0") + "ms 라 셰이크 창(" + SummonFxStyle.Enter.DelayMs.ToString("0") + "ms 뒤)이 벌써 열렸다 — 제자리를 못 잡아 잴 기회가 없었다");
            float t0 = Time.unscaledTime;
            int inWin = 0, moved = 0;
            float worst = 0f, last = Time.unscaledTime;
            bool sawHome = false;
            while (v.ElapsedMs < 620f && Time.unscaledTime - t0 < 2f)
            {
                yield return null;
                float now = Time.unscaledTime;
                worst = Mathf.Max(worst, now - last); last = now;
                float ms = v.ElapsedMs;
                if (ms >= 215f && ms <= 455f)
                {
                    inWin++;
                    if ((w.anchoredPosition - home).sqrMagnitude > 0.01f || Mathf.Abs(w.localScale.x - 1f) > 1e-4f) moved++;
                }
                if (ms > 470f && !v.Kicking && (w.anchoredPosition - home).sqrMagnitude < 0.01f) sawHome = true;
            }
            if (inWin == 0)
                Assert.Ignore("환경 — 입장 셰이크 창(210~460ms)에 프레임이 한 번도 안 들어왔다(가장 긴 프레임 " + (worst * 1000f).ToString("0") + "ms)");
            Assert.Greater(moved, 0, "창 안 프레임 " + inWin + "번 중 판이 움직인 프레임이 0 — 입장 셰이크가 없다");
            Assert.IsTrue(sawHome || v.Kicking, "셰이크가 끝나면 판이 제자리(킥이 잡기 전)");
            Assert.IsTrue(SummonFxStyle.Enter.DelayMs > 0 && SummonFxStyle.Enter.ShakeMs > 0, "표가 쥔다");
        }

        /// <summary>T459 ⓨ — 정본 6674 `.sr-cell.hi.on .sr-orbwrap { animation: srpulse 1.6s ease-in-out infinite; animation-delay: calc(.45s + i×.17s) }`:
        /// 고등급 셀의 광채가 켜진 뒤 1.6s 주기로 커졌다 작아지고, 조연(common)에는 광채가 없다.</summary>
        [UnityTest]
        public IEnumerator 고등급_셀의_광채는_켜진_뒤_1_6초_주기로_뛴다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            float t = 0f;
            while (!v.Done && t < 12f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "소환이 12초 안에 끝난다");
            Assert.IsNull(v.GlowOf(0), "일반 셀에는 광채가 없다(정본 .hi 만)");
            Image g = v.GlowOf(3);
            Assert.IsNotNull(g, "최고 등급(ultimate) 셀의 광채");
            float lo = float.MaxValue, hi = 0f; int n = 0;
            float t0 = Time.unscaledTime, worst = 0f, last = t0;
            while (Time.unscaledTime - t0 < 1.75f)
            {
                yield return null;
                float now = Time.unscaledTime; worst = Mathf.Max(worst, now - last); last = now;
                float sc = g.rectTransform.localScale.x;
                lo = Mathf.Min(lo, sc); hi = Mathf.Max(hi, sc); n++;
            }
            if (n < 6)
                Assert.Ignore("환경 — 1.75초 동안 프레임이 " + n + "번뿐(가장 긴 프레임 " + (worst * 1000f).ToString("0") + "ms) — 맥동을 잴 기회가 없었다");
            float peak = (float)SummonFxStyle.HiPulse.PeakF;
            Assert.Greater(hi, 1f + (peak - 1f) * 0.5f, "한 주기 안에 정점의 절반은 넘는다(정점 " + peak.ToString("0.000") + " · 실측 최대 " + hi.ToString("0.000") + ")");
            Assert.Less(lo, 1f + (peak - 1f) * 0.5f, "골도 있다(실측 최소 " + lo.ToString("0.000") + ")");
            Assert.LessOrEqual(hi, peak + 1e-3f, "정점을 넘지 않는다");
        }

        /// <summary>T459 ⓩ — 정본 6919 `.done .sr-cell.on .sr-orb::after { animation: srsweep 3.6s ease-in-out infinite; animation-delay: calc(i × .29s) }`:
        /// done 뒤 구슬마다 스페큘러 띠가 서고(구슬 원판의 스텐실 마스크 안), 한 주기 안에 켜졌다(.62) 꺼지며 왼쪽으로 지나간다.</summary>
        [UnityTest]
        public IEnumerator done_뒤_구슬_띠가_마스크_안에서_한_주기_켜졌다_꺼지며_왼쪽으로_지나간다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            float t = 0f;
            while (!v.Done && t < 12f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "소환이 12초 안에 끝난다");
            yield return null; yield return null;
            Image sw = v.SweepOf(0);
            Assert.IsNotNull(sw, "0번 구슬의 띠");
            Assert.AreEqual("sr-orb", sw.transform.parent.name, "띠는 구슬 본체의 자식이다(마스크 안)");
            Assert.IsNotNull(sw.transform.parent.GetComponent<Mask>(), "구슬 본체에 스텐실 마스크 — 정본 border-radius 가 잘라 주던 것");
            Assert.IsNotNull(sw.sprite, "구운 띠 한 장");
            float aMax = 0f, aMin = 1f, xMax = float.MinValue, xMin = float.MaxValue; int n = 0;
            float t0 = Time.unscaledTime, worst = 0f, last = t0;
            while (Time.unscaledTime - t0 < 3.9f)
            {
                yield return null;
                float now = Time.unscaledTime; worst = Mathf.Max(worst, now - last); last = now;
                aMax = Mathf.Max(aMax, sw.color.a); aMin = Mathf.Min(aMin, sw.color.a);
                float x = sw.rectTransform.anchoredPosition.x; xMax = Mathf.Max(xMax, x); xMin = Mathf.Min(xMin, x); n++;
            }
            if (n < 8)
                Assert.Ignore("환경 — 3.9초 동안 프레임이 " + n + "번뿐(가장 긴 프레임 " + (worst * 1000f).ToString("0") + "ms) — 스윕을 잴 기회가 없었다");
            float a = (float)SummonFxStyle.OrbSweep.A;
            Assert.Greater(aMax, a * 0.5f, "한 주기 안에 띠가 켜진다(정본 .62 · 실측 최대 " + aMax.ToString("0.00") + ")");
            Assert.Less(aMin, a * 0.5f, "꺼진 때도 있다(실측 최소 " + aMin.ToString("0.00") + ")");
            Assert.Greater(xMax - xMin, sw.rectTransform.parent.GetComponent<RectTransform>().rect.width, "띠가 구슬 폭보다 멀리 움직인다(−80% → 180%)");
        }

        /// <summary>T458 2회차 — 셀은 켜진 프레임에 표의 0% 그대로(배율 .3 · 알파 .34 · 광원 자리)이고, 팝 길이 뒤 제자리·배율 1·알파 1 로 선다.</summary>
        [UnityTest]
        public IEnumerator 셀은_광원_자리에서_작게_켜져_팝_길이_뒤_슬롯에_배율_1_로_선다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            float t = 0f;
            while (!v.CellOn(0) && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.CellOn(0), "0번 셀이 6초 안에 켜진다");
            // 런 1164: 느린 러너가 공개와 충전을 **한 틱**에 몰면 켜진 프레임이 이미 충전 창 안이다 — 그때는 정본대로(6864 `.charging .sr-cell.on:not(.heroic)`
            //   이 `srinhale` 로 애니메이션을 통째로 갈아 끼운다) `AnimateCharge` 의 흡기가 팝의 자리·배율을 덮는다. 잴 기회가 없었던 것이지 팝이 틀린 게 아니다.
            if (v.Charging) Assert.Ignore("환경 — 0번 셀이 켜진 프레임이 이미 충전 창 안이다(공개와 충전이 한 틱에 몰렸다) — 흡기가 팝을 덮어 잴 기회가 없었다");
            // 켜진 그 Update 에서 AnimateCells 가 ms = 0 으로 돈다 — 프레임 길이와 무관하게 표의 0% 값 그대로다.
            RectTransform root = v.CellRootOf(0);
            SummonPopSpec pp = SummonFxStyle.Pop;
            double f0, sc0, a0; pp.At(0, 320, pp.Over(0), out f0, out sc0, out a0);
            Assert.AreEqual((float)sc0, root.localScale.x, 1e-4f, "켜진 프레임 배율 = 표 0% scale(.3 · 옛 .35 EaseOutBack 이 아니다)");
            Assert.AreEqual((float)a0, root.GetComponent<CanvasGroup>().alpha, 1e-4f, "켜진 프레임 알파 = 표 0% opacity(.34)");
            Vector2 home = v.HomeOf(0), eject = v.EjectOf(0);
            Vector2 want0 = eject == Vector2.zero ? home + new Vector2(0f, -(float)pp.Dy0Rem * PetSkillStyle.RemPx) : home + eject;
            Assert.AreEqual(want0.x, root.anchoredPosition.x, 0.5f, "켜진 프레임 자리 = 슬롯 + 사출 벡터(광원 자리)");
            Assert.AreEqual(want0.y, root.anchoredPosition.y, 0.5f, "켜진 프레임 자리 = 슬롯 + 사출 벡터(광원 자리)");
            Assert.Greater((want0 - home).magnitude, 1f, "광원 자리는 슬롯이 아니다");
            float t0 = Time.unscaledTime; bool settled = false;
            while (Time.unscaledTime - t0 < 2.5f && !v.Done)
            {
                yield return null;
                if (v.Charging && !settled) Assert.Ignore("환경 — 정착을 보기 전에 충전 창이 열렸다(흡기가 자리를 덮는다) — 잴 기회가 없었다");
                if (Mathf.Abs(root.localScale.x - 1f) < 1e-3f && (root.anchoredPosition - home).magnitude < 0.5f) { settled = true; break; }
            }
            Assert.IsTrue(settled, "팝 길이 뒤 슬롯 · 배율 1 · (실측 배율 " + root.localScale.x.ToString("0.000") + " · 거리 " + (root.anchoredPosition - home).magnitude.ToString("0.0") + ")");
            Assert.AreEqual(1f, root.GetComponent<CanvasGroup>().alpha, 1e-3f, "정착 알파 1");
        }

        /// <summary>T458 2회차 — [확인] 버튼은 done 프레임에 같은 srpop 0%(배율 .3 · 아래 .5rem) 로 켜져 .32s 뒤 제자리·배율 1 로 선다.</summary>
        [UnityTest]
        public IEnumerator 확인_버튼은_done_에_아래서_작게_켜져_팝_뒤_제자리에_선다()
        {
            yield return Boot();
            SkillSummonResultView v = OpenHoldback();
            float t = 0f;
            while (!v.Done && t < 12f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(v.Done, "소환이 12초 안에 끝난다");
            RectTransform okr = v.OkButton.GetComponent<RectTransform>();
            Assert.IsTrue(okr.gameObject.activeSelf, "done 에 [확인] 이 보인다");
            SummonPopSpec pp = SummonFxStyle.Pop;
            double f0, sc0, a0; pp.At(0, pp.OkMs, 1, out f0, out sc0, out a0);
            Assert.AreEqual((float)sc0, okr.localScale.x, 1e-4f, "done 프레임 배율 = 표 0% scale");
            Assert.AreEqual(-(float)pp.Dy0Rem * PetSkillStyle.RemPx, okr.anchoredPosition.y, 0.5f, "done 프레임 자리 = 아래 .5rem(정본 var(--dy, .5rem))");
            CanvasGroup g = okr.GetComponent<CanvasGroup>();
            Assert.IsNotNull(g, "알파는 CanvasGroup 으로 건다");
            Assert.AreEqual((float)a0, g.alpha, 1e-4f, "done 프레임 알파 .34");
            float t0 = Time.unscaledTime; bool settled = false;
            while (Time.unscaledTime - t0 < 2f)
            {
                yield return null;
                if (Mathf.Abs(okr.localScale.x - 1f) < 1e-3f && Mathf.Abs(okr.anchoredPosition.y) < 0.5f) { settled = true; break; }
            }
            Assert.IsTrue(settled, ".32s 뒤 제자리 · 배율 1(실측 배율 " + okr.localScale.x.ToString("0.000") + " · y " + okr.anchoredPosition.y.ToString("0.0") + ")");
            Assert.AreEqual(1f, g.alpha, 1e-3f, "정착 알파 1");
        }

    }
}
