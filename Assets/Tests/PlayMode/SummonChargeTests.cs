using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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
            float t = 0f;
            while (v.EjectOf(0) == Vector2.zero && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            Vector2 ej = v.EjectOf(0);
            Assert.AreNotEqual(Vector2.zero, ej, "사출 벡터가 안 재졌다(경과 " + t.ToString("0.00") + "초)");

            Transform halo = FindDeep(v.transform, "halo");
            Transform cell = FindDeep(v.transform, "sr-cell-0");
            Assert.IsNotNull(halo, "광원(halo)이 없다");
            Assert.IsNotNull(cell, "0번 셀이 없다");
            Vector2 full = (Vector2)cell.parent.InverseTransformPoint(halo.position) - (Vector2)cell.localPosition;
            Assert.Greater(full.magnitude, 1f, "광원과 셀이 같은 자리다 — 잴 것이 없다");

            // ⓐ 이 단이 결함을 잡는 단이다 — 100% 되짚으면 여기서 빨개진다.
            Assert.Less(ej.magnitude, full.magnitude * 0.9f,
                "사출 벡터가 광원까지를 거의 다 되짚는다(" + (ej.magnitude / full.magnitude).ToString("0.000")
                + ") — 정본은 SR_EJECT 만큼만 되짚는다");
            // ⓑ 그 «일부» 는 표가 쥔다.
            float want = full.magnitude * SummonFxStyle.L("eject_f");
            Assert.AreEqual(want, ej.magnitude, full.magnitude * 0.02f, "되짚는 비율이 표(eject_f)와 다르다");
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
