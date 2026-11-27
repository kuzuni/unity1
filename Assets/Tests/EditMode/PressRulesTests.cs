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
        public void 표가_정본_열한_자리를_정본_값으로_쥔다()
        {
            PressTable t = T();
            // T515 1회차 — 일곱(T355) + **`modal_btn`**(정본 3563 `.modal-card .btn:active, .panel .btn:active, #equip-sheet .btn:active`).
            // T515 3회차 — + `anvil_btn`(987) · `tri_btn`(4591): `class` 에 `btn` 이 **없어** 3563 이 안 걸리는 두 자리다(결정 880).
            //   ⚑ 칸 수는 «정본 `:active` 선언 수» 가 아니다 — 열아홉 선언 중 여섯은 3563 의 **재선언**이라 제 칸이 없다(같은 수가 여러 칸에 복사되면 하나를 고칠 때 나머지가 조용히 어긋난다).
            // T515 4회차 — + `x_btn`(3750 · dy 가 **.12rem** 이라 3563 과 다른 칸이다).
            // T515 5회차 — + `af_dd_item`(4802 · 움직임이 **없고** 배경만 갈아입는 유일한 칸).
            Assert.AreEqual(12, t.Keys.Count, "정본 :active 자리 열둘(213·4304·7750·8087·4982·5002·5011 + 3563 + 987 + 4591 + 3750 + 4802)");
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
            // T515 4회차 — ✕ 는 **dy 가 .12rem** 이고 턱도 걷는다(정본 3750). 3563 의 .1rem 과 섞으면 2px 어긋난다.
            PressSpec xb = t.Get("x_btn");
            Assert.AreEqual(0.12, xb.DyRem, 1e-9, "3750 translateY(.12rem) — 3563 의 .1rem 과 다르다");
            Assert.AreEqual(0.0, xb.Ms, 1e-9, "3750 엔 transition 이 없다 — 즉시");
            Assert.IsTrue(xb.LipOff, "3750 box-shadow: none");
            Assert.AreEqual(1.0, xb.Scale, 1e-9); Assert.AreEqual(1.0, xb.Brightness, 1e-9);
            // T515 6회차 — 안쪽 그늘이 **생기는** 자리는 `af_spinner` 하나다(정본 5011 · 열두 자리 전수).
            PressSpec sp = t.Get("af_spinner");
            Assert.IsTrue(sp.HasInset, "5011 box-shadow: inset 0 .12rem .3rem rgba(0,0,0,.55)");
            Assert.AreEqual(0.12, sp.InsetDyRem, 1e-9, "inset 둘째 값 .12rem");
            Assert.AreEqual(0.30, sp.InsetBlurRem, 1e-9, "inset 셋째 값 .3rem");
            Assert.AreEqual(0.0, sp.InsetR, 1e-9); Assert.AreEqual(0.0, sp.InsetG, 1e-9); Assert.AreEqual(0.0, sp.InsetB, 1e-9);
            Assert.AreEqual(0.55, sp.InsetA, 1e-9, "rgba(0,0,0,.55)");
            Assert.AreEqual(70, sp.Ms, 1e-9, "5009 가 box-shadow .07s 를 쥔다 — 이 자리만 램프다");
            foreach (string k in t.Keys)
                if (k != "af_spinner") Assert.IsFalse(t.Get(k).HasInset, k + ": 눌림에 그늘이 생기는 자리는 `af_spinner` 하나다");
            foreach (string k in t.Keys)
                if (k != "modal_btn" && k != "x_btn")
                    Assert.IsFalse(t.Get(k).LipOff, k + ": 턱을 걷는 자리는 `modal_btn`·`x_btn` 둘뿐이다(나머지 선언엔 box-shadow 가 없다)");
            // T515 5회차 — 드롭다운 항목(정본 4802)은 **배경만** 바뀐다: 가라앉기도 배율도 밝기도 없고 교체 채널만 참이다.
            PressSpec dd = t.Get("af_dd_item");
            Assert.AreEqual(0.0, dd.DyRem, 1e-9, "4802 엔 transform 이 없다 — 가라앉지 않는다");
            Assert.AreEqual(1.0, dd.Scale, 1e-9); Assert.AreEqual(1.0, dd.Brightness, 1e-9, "4802 엔 filter 가 없다 — 밝기 곱하기가 아니다");
            Assert.AreEqual(0.0, dd.Ms, 1e-9, "4802 엔 transition 이 없다 — 즉시");
            Assert.IsFalse(dd.LipOff, "4802 엔 box-shadow 가 없다 — 걷을 턱이 없다");
            Assert.IsTrue(dd.FaceSwap, "4802 background: #2c313a — 면 색을 갈아입는다");
            foreach (string k in t.Keys)
                if (k != "af_dd_item")
                    Assert.IsFalse(t.Get(k).FaceSwap, k + ": 면 색을 갈아입는 자리는 `af_dd_item` 하나다(나머지 선언엔 background 가 없다)");
            // 이산이다 — 위상이 0 을 넘는 순간 갈아입고 0 이면 제 색이다(`ms: 0` 이라 그 넘김이 첫 프레임에 든다).
            Assert.IsFalse(PressRules.FaceSwapped(dd, 0.0), "놓인 상태는 제 색");
            Assert.IsTrue(PressRules.FaceSwapped(dd, 1e-9), "위상이 0 을 넘으면 갈아입은 색");
            Assert.IsTrue(PressRules.FaceSwapped(dd, 1.0));
            Assert.AreEqual(1.0, PressRules.Phase(dd, 0, 0, true), 1e-9, "ms 0 — 첫 프레임에 눌린다");
            Assert.IsFalse(PressRules.FaceSwapped(t.Get("modal_btn"), 1.0), "교체 채널이 거짓인 칸은 위상이 1이어도 안 갈아입는다");
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
            // T515 6회차 — 안쪽 그늘은 **반쯤 적으면 조용히 틀린다**(그늘이 없는 것과 구별이 안 된다) → 네 수를 다 요구한다.
            const string head = "{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":1,\"brightness\":1,\"ms\":70,\"ease\":\"ease-out\",\"inset\":{";
            Assert.Throws<Forge.Core.Data.JsonException>(() => PressTable.From(MiniJson.ParseObject(head + "\"dy_rem\":0.12,\"blur_rem\":0.3}}}}")), "inset.rgba 없음 — 빠진 키는 JsonException(J.Require) 이 막는다");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject(head + "\"dy_rem\":0.12,\"blur_rem\":0.3,\"rgba\":[0,0,0]}}}}")), "inset.rgba 가 넷이 아니다");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject(head + "\"dy_rem\":0.12,\"blur_rem\":-0.3,\"rgba\":[0,0,0,0.55]}}}}")), "inset.blur 음수");
            Assert.Throws<FormatException>(() => PressTable.From(MiniJson.ParseObject(head + "\"dy_rem\":0.12,\"blur_rem\":0.3,\"rgba\":[0,0,0,0]}}}}")), "inset 알파 0 — 그늘이 없는 것과 같다");
            PressTable ins = PressTable.From(MiniJson.ParseObject(head + "\"dy_rem\":0.12,\"blur_rem\":0.3,\"rgba\":[0,0,0,0.55]}}}}"));
            Assert.IsTrue(ins.Get("a").HasInset, "네 수가 다 있으면 받는다");
            Assert.AreEqual(0.55, ins.Get("a").InsetA, 1e-9);
            Assert.IsFalse(PressTable.From(MiniJson.ParseObject("{\"press\":{\"a\":{\"dy_rem\":0.1,\"scale\":1,\"brightness\":1,\"ms\":0}}}")).Get("a").HasInset,
                "`inset` 칸이 없으면 그 자리엔 안쪽 그늘이 없다");
        }
    }
}
