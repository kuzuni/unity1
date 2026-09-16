using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T345 18회차 — **메인 화면**의 둥근 모서리 두 자리가 표(`RadiusUi.json`)를 읽고 정본 값으로 선다:
    /// 상단바 재화 알약 `.currency-pills .pill`(정본 115 · 1rem) · 보스 웨이브 핍 `.pip.boss`(정본 195 · **절대 2px**).
    /// 보통 핍은 `.pip`(189 · `50%`) 그대로 원이다 — 보스만 45° 돌린 «모서리 조금 깎은 네모» 다.
    /// 2px 은 rem 이 아니라 절대 CSS px 이라 표 꼬리가 `_r_px` 고 환산은 <see cref="KeylineUi.CssPx"/> 한 군데다(T104 규약).
    /// </summary>
    public class HudRadiusTests
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
            while (!(MetaHost.Ready && Hud.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(Hud.Instance, "HUD 가 서지 않았다");
            yield return null;
        }

        static float Mult(string key) { return UiShapes.RoundedMultiplier(RadiusUi.Px(key)); }

        [Test]
        public void 표의_메인_화면_두_값은_정본_단위_그대로다()
        {
            Assert.AreEqual(1f * RadiusUi.PxPerRem, RadiusUi.Px("currency_pill_r_rem"), 1e-3f,
                "정본 115 `.currency-pills .pill { border-radius: 1rem }`");
            // 절대 px 자리 — rem 으로 읽히면 2rem(1rem px 의 두 배)이 되어 알약처럼 뭉개진다
            Assert.AreEqual(2f * KeylineUi.CssPx, RadiusUi.Px("pip_boss_r_px"), 1e-3f,
                "정본 195 `.pip.boss { border-radius: 2px }` — CSS px 환산은 KeylineUi.CssPx");
            Assert.Less(RadiusUi.Px("pip_boss_r_px"), RadiusUi.PxPerRem * 0.5f,
                "2 CSS px 은 반 rem 도 안 된다 — rem 으로 읽혔다면 이 자가 잡는다");
        }

        [UnityTest]
        public IEnumerator 재화_알약은_표의_1rem_으로_서고_보스_핍만_2px_마름모다()
        {
            yield return Boot();
            Canvas.ForceUpdateCanvases();

            foreach (string pill in new[] { "pill-coin", "pill-gem" })
            {
                GameObject go = GameObject.Find(pill);
                Assert.IsNotNull(go, pill + " 알약이 없다");
                Transform bg = go.transform.Find("bg");
                Assert.IsNotNull(bg, pill + " 의 판(bg)");
                Image img = bg.GetComponent<Image>();
                Assert.AreEqual(UiShapes.Rounded, img.sprite, pill + ": 둥근 9-슬라이스여야 한다");
                Assert.AreEqual(Mult("currency_pill_r_rem"), img.pixelsPerUnitMultiplier, 1e-3f,
                    pill + ": 반지름이 표 currency_pill_r_rem 과 다르다");
            }

            // 다섯 웨이브 · 지금은 둘째 · 셋째가 보스
            Hud.Instance.SetWaves(5, 2, 3);
            yield return null;
            Assert.AreEqual(5, Hud.Instance.WaveCount);

            GameObject row = GameObject.Find("wave-pips");
            Assert.IsNotNull(row, "웨이브 노드 줄이 없다");
            for (int i = 1; i <= 5; i++)
            {
                bool boss = i == 3;
                foreach (string part in new[] { "ring", "fill" })
                {
                    Transform t = row.transform.Find("pip-" + i + "/" + part);
                    Assert.IsNotNull(t, "pip-" + i + "/" + part);
                    Image img = t.GetComponent<Image>();
                    string what = "pip-" + i + "/" + part + (boss ? "(보스)" : "");
                    if (boss)
                    {
                        Assert.AreEqual(UiShapes.Rounded, img.sprite, what + ": 정본 195 는 2px 깎은 네모다(원이 아니다)");
                        Assert.AreEqual(Mult("pip_boss_r_px"), img.pixelsPerUnitMultiplier, 1e-3f,
                            what + ": 반지름이 표 pip_boss_r_px 와 다르다");
                        Assert.AreEqual(45f, t.localRotation.eulerAngles.z, 1e-2f, what + ": 정본 195 `rotate(45deg)`");
                    }
                    else
                    {
                        Assert.AreEqual(UiShapes.Circle, img.sprite, what + ": 정본 189 `.pip { border-radius: 50% }` — 원이다");
                        Assert.AreEqual(0f, t.localRotation.eulerAngles.z, 1e-2f, what + ": 보스가 아닌 핍은 안 돈다");
                    }
                }
            }
        }
    }
}
