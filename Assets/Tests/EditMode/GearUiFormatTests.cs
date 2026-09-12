using System;
using NUnit.Framework;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Forging;

namespace Forge.Tests
{
    /// <summary>
    /// T59 — 수 표기 둘. ⓐ 플레이어 정보 «옵션 합계» 줄은 원작 `+stats.subs[key].toFixed(1)`(ui.js 5182)이라 <see cref="NumFmt.RoundFixed"/>/<see cref="NumFmt.Fixed"/> 가
    /// JS `toFixed(1)` 과 같은 수·문자열을 내야 한다(벡터는 node 22 로 뽑았다). ⓑ 대장간 «모든 장비 목록»·«장비 상세» 의 확률은 원작 `Forge.itemDropChance(age, slot).toFixed(4)` 이라
    /// 0% 시대(현재 레벨 확률표에서 0)는 원작도 `0.0000%` 다 — 런 78 PNG 의 «전부 0.0000%» 는 Lv29 의 원시 시대 한 절이었다(forge-info 의 앞 다섯 시대 0%).
    /// </summary>
    public class GearUiFormatTests
    {
        // node 22: vals.map(v => [v, +v.toFixed(1), String(+v.toFixed(1)), (+v.toFixed(1)) > 0])
        [TestCase(7.699999999999999, 7.7, "7.7", true)]
        [TestCase(15.4, 15.4, "15.4", true)]
        [TestCase(46.8, 46.8, "46.8", true)]
        [TestCase(4.1, 4.1, "4.1", true)]
        [TestCase(2.8, 2.8, "2.8", true)]
        [TestCase(6, 6, "6", true)]
        [TestCase(3.8, 3.8, "3.8", true)]
        [TestCase(18.2, 18.2, "18.2", true)]
        [TestCase(0.04, 0, "0", false)]
        [TestCase(0.05, 0.1, "0.1", true)]
        [TestCase(0.049999, 0, "0", false)]
        [TestCase(2.85, 2.9, "2.9", true)]
        [TestCase(2.25, 2.3, "2.3", true)]
        [TestCase(1.005, 1, "1", true)]
        [TestCase(0.95, 0.9, "0.9", true)]
        [TestCase(99.95, 100, "100", true)]
        [TestCase(12.34, 12.3, "12.3", true)]
        [TestCase(0.30000000000000004, 0.3, "0.3", true)]
        [TestCase(1e-7, 0, "0", false)]
        [TestCase(123456.789, 123456.8, "123456.8", true)]
        [TestCase(23, 23, "23", true)]
        [TestCase(7.7, 7.7, "7.7", true)]
        [TestCase(10.35, 10.3, "10.3", true)]
        [TestCase(10.25, 10.3, "10.3", true)]
        [TestCase(0, 0, "0", false)]
        [TestCase(-0.04, 0, "0", false)]
        public void JS_toFixed_1_과_같다(double v, double want, string wantStr, bool positive)
        {
            Assert.AreEqual(want, NumFmt.RoundFixed(v, 1), "RoundFixed(" + v.ToString("R") + ")");
            Assert.AreEqual(wantStr, NumFmt.Fixed(v, 1), "Fixed(" + v.ToString("R") + ")");
            Assert.AreEqual(positive, NumFmt.RoundFixed(v, 1) > 0, "value > 0 걸러내기");
        }

        [Test]
        public void 서브스탯_합계_줄은_소수_한_자리_뒤_0_초과만()
        {
            // 원작 subsHtml 규칙을 한 줄로: 부호 + String(+v.toFixed(1)) + "% " + label · 반올림 뒤 0 이면 안 찍는다
            Assert.AreEqual("+7.7", "+" + NumFmt.Fixed(3.3 + 4.1 + 0.3, 1));
            Assert.AreEqual("-2.9", "-" + NumFmt.Fixed(2.85, 1));
            Assert.IsFalse(NumFmt.RoundFixed(0.04, 1) > 0, "0.04 는 반올림하면 0 이라 줄이 안 나온다(원작 filter(value > 0))");
            Assert.IsTrue(NumFmt.RoundFixed(0.05, 1) > 0);
        }

        [Test]
        public void 대장간_목록_확률은_0_시대만_0이고_나머지는_원작_식대로()
        {
            GameData g = DataDir.Game;
            int level = 29;
            var st = new ForgeState { ForgeLevel = level };
            var eng = new ForgeEngine(g, st, new Wallet(), Rng.Mulberry(1), () => 0);
            OrderedMap<double> probs = eng.AgeProbsAt(level);
            int zero = 0, positive = 0;
            foreach (string age in g.Defs.Ages)
            {
                double ageP = probs.Get(age, 0);
                foreach (string slot in g.Defs.Slots)
                {
                    double got = eng.ItemDropChance(age, slot);
                    double want = ageP / 100 * (1.0 / g.Defs.Slots.Length) * (1.0 / eng.VariantCount(age, slot)) * 100;
                    Assert.AreEqual(want, got, 1e-12, age + "/" + slot);
                    string shown = got.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "%";
                    Assert.AreEqual(JsNum.ToFixed(got, 4) + "%", shown, age + "/" + slot + " 표기 = toFixed(4)");
                    if (ageP == 0) { Assert.AreEqual("0.0000%", shown, age + " 는 이 레벨에서 0% 시대 — 원작도 0.0000%"); zero++; }
                    else { Assert.Greater(got, 0, age + "/" + slot); positive++; }
                }
            }
            Assert.Greater(positive, 0, "Lv" + level + " 에 확률이 0 이 아닌 시대가 있다(forge-info 의 6.99% · 68% …)");
            Assert.Greater(zero, 0, "Lv" + level + " 에 0% 시대도 있다(원시~르네상스) — 목록 첫 절이 0.0000% 인 이유");
        }
    }
}
