using System;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests
{
    /// <summary>T355 — 눌림 피드백 표(`PressFxUi.json`)와 셈(<see cref="PressRules"/>): 정본 `:active` 값 · transition 위상.</summary>
    public class PressRulesTests
    {
        static PressTable T()
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DataDir.Path)));
            return PressTable.From(MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "Assets", "Forge", "Resources", "PressFxUi.json"))));
        }

        [Test]
        public void 표가_정본_일곱_자리를_정본_값으로_쥔다()
        {
            PressTable t = T();
            Assert.AreEqual(7, t.Keys.Count, "정본 :active 자리 일곱(213·4304·7750·8087·4982·5002·5011)");
            PressSpec c = t.Get("offline_chest");
            Assert.AreEqual(0.08, c.DyRem, 1e-9); Assert.AreEqual(0.94, c.Scale, 1e-9); Assert.AreEqual(1.0, c.Brightness, 1e-9); Assert.AreEqual(100, c.Ms, 1e-9);
            PressSpec p = t.Get("pet_tile");
            Assert.AreEqual(0.08, p.DyRem, 1e-9); Assert.AreEqual(1.07, p.Brightness, 1e-9); Assert.AreEqual(80, p.Ms, 1e-9);
            Assert.AreEqual(0.06, t.Get("af_check").DyRem, 1e-9); Assert.AreEqual(1.12, t.Get("af_check").Brightness, 1e-9); Assert.AreEqual(70, t.Get("af_check").Ms, 1e-9);
            Assert.AreEqual(0.97, t.Get("af_sub_row").Brightness, 1e-9);
            Assert.AreEqual(0.10, t.Get("af_spinner").DyRem, 1e-9);
            Assert.IsTrue(t.Has("equip_cell") && t.Has("egg_cell"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => t.Get("없는_자리"));
        }

        [Test]
        public void 위상은_눌림에서_ms_안에_1로_가고_떼면_그_자리에서_0으로_돌아온다()
        {
            PressSpec s = T().Get("offline_chest");   // .1s ease-out
            Assert.AreEqual(0.0, PressRules.Phase(s, 0, 0, true), 1e-9);
            double half = PressRules.Phase(s, 0, s.Ms * 0.5, true);
            Assert.Greater(half, 0.5, "ease-out 은 앞이 빠르다 — 절반 시각에 절반을 넘는다");
            Assert.Less(half, 1.0);
            Assert.AreEqual(1.0, PressRules.Phase(s, 0, s.Ms, true), 1e-9);
            Assert.AreEqual(1.0, PressRules.Phase(s, 0, s.Ms * 3, true), 1e-9);
            // 단조 증가
            double prev = 0;
            for (int i = 1; i <= 20; i++) { double v = PressRules.Phase(s, 0, s.Ms * i / 20.0, true); Assert.GreaterOrEqual(v, prev - 1e-12); prev = v; }
            // 중간(위상 .6)에서 떼면 .6 → 0
            Assert.AreEqual(0.6, PressRules.Phase(s, 0.6, 0, false), 1e-9);
            double mid = PressRules.Phase(s, 0.6, s.Ms * 0.5, false);
            Assert.Less(mid, 0.6); Assert.Greater(mid, 0.0);
            Assert.AreEqual(0.0, PressRules.Phase(s, 0.6, s.Ms, false), 1e-9);
            Assert.IsTrue(PressRules.Settled(1.0, true)); Assert.IsFalse(PressRules.Settled(0.99, true));
            Assert.IsTrue(PressRules.Settled(0.0, false)); Assert.IsFalse(PressRules.Settled(0.01, false));
        }

        [Test]
        public void 위상별_이동_배율_밝기는_표값과_1_사이를_선형으로_오간다()
        {
            PressSpec c = T().Get("offline_chest");
            Assert.AreEqual(0.0, PressRules.DyRem(c, 0), 1e-9); Assert.AreEqual(0.08, PressRules.DyRem(c, 1), 1e-9); Assert.AreEqual(0.04, PressRules.DyRem(c, 0.5), 1e-9);
            Assert.AreEqual(1.0, PressRules.ScaleAt(c, 0), 1e-9); Assert.AreEqual(0.94, PressRules.ScaleAt(c, 1), 1e-9); Assert.AreEqual(0.97, PressRules.ScaleAt(c, 0.5), 1e-9);
            PressSpec p = T().Get("pet_tile");
            Assert.AreEqual(1.0, PressRules.BrightnessAt(p, 0), 1e-9); Assert.AreEqual(1.07, PressRules.BrightnessAt(p, 1), 1e-9);
            Assert.AreEqual(1.0, PressRules.BrightnessAt(c, 1), 1e-9, "상자는 밝기 없이 배율만");
            Assert.AreEqual(0.08, PressRules.DyRem(c, 1.7), 1e-9, "위상은 0~1 로 자른다");
        }

        [Test]
        public void 깨진_표는_거부한다()
        {
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":1,\"brightness\":1,\"ms\":0}}}")), "ms 0");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":0,\"brightness\":1,\"ms\":80}}}")), "scale 0");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":-0.1,\"scale\":1,\"brightness\":1,\"ms\":80}}}")), "dy 음수");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{}}")), "빈 표");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":1,\"brightness\":1,\"ms\":80,\"ease\":\"bouncy\"}}}")), "모르는 ease");
        }
    }
}
