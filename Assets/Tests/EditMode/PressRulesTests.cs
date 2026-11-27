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
        public void 표가_정본_열_자리를_정본_값으로_쥔다()
        {
            PressTable t = T();
            // T515 1회차 — 일곱(T355) + **`modal_btn`**(정본 3563 `.modal-card .btn:active, .panel .btn:active, #equip-sheet .btn:active`).
            // T515 3회차 — + `anvil_btn`(987) · `tri_btn`(4591): `class` 에 `btn` 이 **없어** 3563 이 안 걸리는 두 자리다(결정 880).
            //   ⚑ 칸 수는 «정본 `:active` 선언 수» 가 아니다 — 열아홉 선언 중 여섯은 3563 의 **재선언**이라 제 칸이 없다(같은 수가 여러 칸에 복사되면 하나를 고칠 때 나머지가 조용히 어긋난다).
            Assert.AreEqual(10, t.Keys.Count, "정본 :active 자리 열(213·4304·7750·8087·4982·5002·5011 + 3563 + 987 + 4591)");
            PressSpec c = t.Get("offline_chest");
            Assert.AreEqual(0.08, c.DyRem, 1e-9); Assert.AreEqual(0.94, c.Scale, 1e-9); Assert.AreEqual(1.0, c.Brightness, 1e-9); Assert.AreEqual(100, c.Ms, 1e-9);
            PressSpec p = t.Get("pet_tile");
            Assert.AreEqual(0.08, p.DyRem, 1e-9); Assert.AreEqual(1.07, p.Brightness, 1e-9); Assert.AreEqual(80, p.Ms, 1e-9);
            Assert.AreEqual(0.06, t.Get("af_check").DyRem, 1e-9); Assert.AreEqual(1.12, t.Get("af_check").Brightness, 1e-9); Assert.AreEqual(70, t.Get("af_check").Ms, 1e-9);
            Assert.AreEqual(0.97, t.Get("af_sub_row").Brightness, 1e-9);
            Assert.AreEqual(0.10, t.Get("af_spinner").DyRem, 1e-9);
            // T33 83회차 — 장비 칸과 **알 칸은 전이가 갈린다**(결정 887): 정본 7743 이 `.equip-cell:not(.egg-cell)` 로
            //   알 칸을 **명시적으로 뺀다** → 장비 칸은 .08s ease-out 으로 가라앉고 알 칸(8087)은 전이가 없어 **즉시**다.
            //   `.equip-cell` 을 건드리는 `transition` 은 정본 전수로 7743 그 한 줄뿐이다.
            PressSpec ec = t.Get("equip_cell"), eg = t.Get("egg_cell");
            Assert.AreEqual(80, ec.Ms, 1e-9, "7743 transform .08s");
            Assert.AreEqual(0.0, eg.Ms, 1e-9, "8087 에는 transition 이 없다 — 즉시(7743 이 :not(.egg-cell) 로 뺀다)");
            Assert.AreEqual(eg.DyRem, ec.DyRem, 1e-9, "두 칸의 가라앉는 깊이는 같다(.08rem)");
            Assert.AreEqual(eg.Brightness, ec.Brightness, 1e-9, "밝기도 같다(1.07)");
            Assert.AreEqual(1.0, PressRules.Phase(eg, 0, 0, true), 1e-9, "알 칸은 첫 프레임에 다 내려간다");
            Assert.AreEqual(0.0, PressRules.Phase(eg, 1, 0, false), 1e-9, "떼는 것도 즉시다");
            double mid = PressRules.Phase(ec, 0, ec.Ms * 0.5, true);
            Assert.That(mid, Is.GreaterThan(0.0).And.LessThan(1.0), "장비 칸은 그 사이를 지난다 — 둘이 같은 수였으면 이 줄이 안 갈린다");
            // T515 — 턱 걷기 갈래: `.1rem` 가라앉고 턱이 걷히며 정본엔 `transition` 이 **없다**(ms 0 = 즉시).
            PressSpec mb = t.Get("modal_btn");
            Assert.AreEqual(0.10, mb.DyRem, 1e-9, "3563 translateY(.1rem)");
            Assert.AreEqual(0.0, mb.Ms, 1e-9, "정본에 transition 이 없다 — 즉시");
            Assert.IsTrue(mb.LipOff, "3563 box-shadow: none — 아래턱을 걷는다");
            Assert.AreEqual(1.0, mb.Scale, 1e-9); Assert.AreEqual(1.0, mb.Brightness, 1e-9);
            // T515 3회차 — 가라앉기**만** 하는 두 자리(정본 987·4591 은 `transform` 한 줄뿐이다 · 턱도 이징도 없다).
            foreach (string k in new[] { "anvil_btn", "tri_btn" })
            {
                PressSpec q = t.Get(k);
                Assert.AreEqual(0.10, q.DyRem, 1e-9, k + ": 정본 translateY(.1rem)");
                Assert.AreEqual(0.0, q.Ms, 1e-9, k + ": 그 선언에 transition 이 없다 — 즉시");
                Assert.AreEqual(1.0, q.Scale, 1e-9, k + ": scale 은 정본에 없다");
                Assert.AreEqual(1.0, q.Brightness, 1e-9, k + ": filter 는 정본에 없다");
                Assert.IsFalse(q.LipOff, k + ": 그 선언엔 box-shadow: none 이 없다 — 걷을 턱이 없다");
            }
            foreach (string k in t.Keys)
                if (k != "modal_btn") Assert.IsFalse(t.Get(k).LipOff, k + ": 턱을 걷는 자리는 `modal_btn` 하나다(나머지 선언엔 box-shadow 가 없다)");
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
            // T515 — `ms: 0` 은 이제 **옳은 값**이다(정본에 `transition` 이 없는 자리 = 즉시). 막는 것은 **음수**다.
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":1,\"brightness\":1,\"ms\":-1}}}")), "ms 음수");
            PressTable zero = PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":1,\"brightness\":1,\"ms\":0}}}"));
            Assert.AreEqual(0.0, zero.Get("a").Ms, 1e-9, "ms 0 은 받는다(즉시)");
            Assert.AreEqual(1.0, PressRules.Phase(zero.Get("a"), 0, 0, true), 1e-9, "ms 0 이면 **첫 프레임에** 목표다(한 프레임도 안 늦는다)");
            Assert.AreEqual(0.0, PressRules.Phase(zero.Get("a"), 1, 0, false), 1e-9, "떼면 그 자리에서 바로 0");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":0,\"brightness\":1,\"ms\":80}}}")), "scale 0");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":-0.1,\"scale\":1,\"brightness\":1,\"ms\":80}}}")), "dy 음수");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{}}")), "빈 표");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":1,\"brightness\":1,\"ms\":80,\"ease\":\"bouncy\"}}}")), "모르는 ease");
        }
    }
}
