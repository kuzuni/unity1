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
    /// T372 — 「모든 장비의 목록」 칸 아래 «0.0000%» 라벨이 **칸 피치 안에** 서는가.
    /// 정본 `style.css` **790** `.forge-item-cell small { font-size: .56rem }`(= 기준 캔버스 20.4px)인데 클론은 §1 하한 `TextKind.Sub`(36)로 찍어
    /// **1.76배**가 됐고, 그 결과 라벨 잉크가 칸 피치를 넘어 스물다섯이 **한 줄로 붙었다**.
    /// 원작 `shot-042905` 실측: 라벨 덩어리 다섯 · 잉크 46px · 틈 15px · 피치 61px → **잉크/피치 0.754 · 틈/피치 0.246**.
    /// 이 비는 PNG 폭에 안 걸리는 값이라(둘 다 같은 자로 잰다) 화면 해상도와 무관하게 잴 수 있다.
    ///
    /// **다만 0.754 를 그대로 겨누지 않는다** — 클론 글꼴(NotoSansKR-Forge)의 자폭을 hmtx 에서 직접 재면 «0.0000%» 는 **3.974em**(0.555×5 · `.` 0.278 · `%` 0.921)이라
    /// 정본 크기 20.4px 를 그대로 줘도 칸 피치 135.63(기준 캔버스) 대비 **0.598** 밖에 안 된다. 0.754 는 정본 폴백 글꼴('Segoe UI'/'Malgun Gothic')의 자폭이지
    /// 이 자리의 병이 아니다 — 글꼴 폭 차는 T53·T121 의 축이다. 그래서 여기서 못박는 것은 **틈이 실제로 생겼는가**다.
    /// 예상: `Sub`(36) 1.055(= 붙는다) → `Micro`(18) **0.527**.
    /// 판정: ⓐ 잉크/피치 &lt; 0.80 — 이웃 사이에 원작(0.754)만큼은 틈이 있다 ⓑ &gt; 0.40 — 원작의 절반 밑으로 꺼지지 않는다
    /// ⓒ 라벨 종류가 `Micro` 고 굵기가 정본 렌더 결과(8633 `font-weight: 500` → 폴백 sans 는 보통 굵기)와 같다.
    /// </summary>
    public class ForgeListLabelTests
    {
        private static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && PopupLayer.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        [TearDown]
        public void CleanSave()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
        }

        private static Rect World(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        [UnityTest]
        public IEnumerator 목록_칸_퍼센트_라벨이_칸_피치_안에_선다()
        {
            yield return Boot();
            ForgeHost h = ForgeHost.Instance;

            // ⓒ-1 표 — 이 자리는 §1 하한의 예외 한 자리를 쓴다(결정 633). 값이 새 종류로 갈라지지 않게 여기서 못박는다.
            Assert.AreEqual(TextKind.Micro, ForgeInfoPopup.PctKind, "정본 .56rem 자리는 새 종류를 만들지 않고 Micro(18)를 쓴다(결정 633)");

            ForgeInfoPopup.OpenList(h);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            Popup p = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(p, "「모든 장비의 목록」 팝업이 열려 있다");

            // 한 줄(다섯 칸)을 이룬 이웃 라벨을 걷는다 — 격자는 5열이라 같은 y 에 선 것끼리가 한 줄이다.
            List<TextMeshProUGUI> row = null;
            float rowY = 0f;
            var byY = new Dictionary<int, List<TextMeshProUGUI>>();
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name != "pct" || !t.gameObject.activeInHierarchy) continue;
                int key = Mathf.RoundToInt(World(t.rectTransform).center.y);
                if (!byY.ContainsKey(key)) byY[key] = new List<TextMeshProUGUI>();
                byY[key].Add(t);
            }
            foreach (var kv in byY)
                if (kv.Value.Count >= 3 && (row == null || kv.Key > rowY)) { row = kv.Value; rowY = kv.Key; }
            Assert.IsNotNull(row, "% 라벨이 한 줄에 셋 이상 선 자리를 못 찾았다 — 목록이 안 그려졌다(걷은 라벨 " + byY.Count + "줄)");
            row.Sort((a, b) => World(a.rectTransform).center.x.CompareTo(World(b.rectTransform).center.x));

            // ⓒ-2 굵기 — 정본 8633 이 790 의 800 을 이겨 `font-weight: 500` 이고, 그 줄의 정본 주석대로 폴백 sans 에선 **보통**으로 내려간다.
            foreach (TextMeshProUGUI t in row)
            {
                Assert.AreEqual(UiCatalog.Instance.Kind(TextKind.Micro).size, t.fontSize, 0.5f, "라벨 글자 크기 = Micro(정본 .56rem 에 가장 가까운 예외 한 자리) · 실측 " + t.fontSize);
                Assert.IsFalse((t.fontStyle & FontStyles.Bold) != 0, "정본 8633 은 이 자리를 font-weight 500 으로 되돌린다 — 굵게 찍지 않는다");
            }

            // ⓐ·ⓑ 잉크 폭 ↔ 칸 피치. 피치는 이웃 라벨 상자의 가운데 사이(= 칸 피치 그대로), 잉크는 TMP 가 실제로 그린 글자 폭이다.
            float pitchSum = 0f;
            for (int i = 1; i < row.Count; i++) pitchSum += World(row[i].rectTransform).center.x - World(row[i - 1].rectTransform).center.x;
            float pitch = pitchSum / (row.Count - 1);
            Assert.Greater(pitch, 1f, "칸 피치를 못 쟀다");

            // `preferredWidth` 는 기준 캔버스 단위라 피치(월드 단위)와 같은 자로 재려면 스케일을 태운다.
            float scale = row[0].rectTransform.lossyScale.x;
            Assert.Greater(scale, 0f, "캔버스 스케일");
            float inkMax = 0f;
            foreach (TextMeshProUGUI t in row)
            {
                t.ForceMeshUpdate();
                float ink = t.preferredWidth * scale;   // 글자가 실제로 먹는 폭(TMP 가 그 글·그 크기로 잰 값) — 상자 폭이 아니다
                Assert.Greater(ink, 0f, "라벨 «" + t.text + "» 의 잉크 폭이 0 이다");
                if (ink > inkMax) inkMax = ink;
            }
            float ratio = inkMax / pitch;
            Assert.Less(ratio, 0.80f, "라벨 잉크가 칸 피치를 다 먹어 스물다섯이 한 줄로 붙는다 — 원작은 잉크/피치 0.754(잉크 46 · 틈 15 · 피치 61) · 실측 " + ratio.ToString("0.000") + "(잉크 " + inkMax.ToString("0.0") + " · 피치 " + pitch.ToString("0.0") + ")");
            Assert.Greater(ratio, 0.40f, "라벨이 원작(0.754)의 절반 밑으로 꺼졌다 — 예상은 Micro 18 로 0.527 · 실측 " + ratio.ToString("0.000"));
            Debug.Log("[T372] % 라벨 " + row.Count + "칸 · 잉크 " + inkMax.ToString("0.0") + " · 피치 " + pitch.ToString("0.0") + " · 잉크/피치 " + ratio.ToString("0.000") + "(원작 0.754 · 이 글꼴로는 0.527 예상) · 글자 " + row[0].fontSize);

            h.Meta.Popups.Hide(ForgeInfoPopup.Name);
            yield return null;
        }
    }
}
