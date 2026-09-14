using System;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Tests.EditMode
{
    /// <summary>
    /// T133 — 메인 오프라인 보상 버튼의 셈이 정본 <c>style.css</c> 218~241 · <c>ui.js</c> 6085 와 같은가.
    /// 표(<c>OfflineButtonUi.json</c>)를 읽지 않고 값을 직접 세워 **식만** 잰다(표는 PlayMode 가 읽는다).
    /// </summary>
    public class OfflineButtonRulesTests
    {
        static OfflineButtonSpec S()
        {
            // 정본 값 그대로
            return OfflineButtonSpec.From(MiniJson.ParseObject(@"{""layout"":{
                ""btn_rem"":2.9,""left_rem"":0.5,""bottom_rem"":0.6,""chest_shadow_dy_rem"":0.1,
                ""press_dy_rem"":0.08,""press_scale"":0.94,""ready_sec"":60,
                ""bob_ms"":1600,""bob_dy_rem"":0.14,""bob_scale"":1.04,
                ""zzz_left_f"":0.58,""zzz_bottom_f"":0.62,""zzz_font_rem"":0.78,""zzz_line_px"":2.5,
                ""zzz_ms"":2700,""zzz_gap_ms"":900,""zzz_count"":3,
                ""zzz_end_dx_rem"":0.75,""zzz_end_dy_rem"":-1.7,""zzz_from_scale"":0.55,""zzz_to_scale"":1.25,
                ""zzz_from_rot"":-10,""zzz_to_rot"":12,""zzz_in_f"":0.16,""zzz_out_f"":0.7,""tick_ms"":1000}}"));
        }

        [Test]
        public void 예순초가_지나야_상자가_들썩인다()
        {
            OfflineButtonSpec s = S();
            Assert.IsFalse(OfflineButtonRules.Ready(s, 59_000, 0), "59초에는 아직 아니다 (정본 ui.js 6085 ≥ 60)");
            Assert.IsTrue(OfflineButtonRules.Ready(s, 60_000, 0), "60초 정각부터다 (경계 포함)");
            Assert.IsTrue(OfflineButtonRules.Ready(s, 600_000, 0));
            Assert.IsFalse(OfflineButtonRules.Ready(s, 100_000, 90_000), "방금 수령했으면 꺼진다 (정본 5944)");
        }

        [Test]
        public void 들썩임은_한가운데에서_가장_높고_양끝에서_제자리다()
        {
            OfflineButtonSpec s = S();
            double dy, sc;
            OfflineButtonRules.Bob(s, 0, out dy, out sc);
            Assert.AreEqual(0, dy, 1e-9, "0% 는 제자리 (정본 0%,100% translateY(0) scale(1))");
            Assert.AreEqual(1.0, sc, 1e-9);
            OfflineButtonRules.Bob(s, 800, out dy, out sc);          // 50%
            Assert.AreEqual(-0.14, dy, 1e-9, "50% 는 −.14rem (정본 keyframe)");
            Assert.AreEqual(1.04, sc, 1e-9, "50% 는 scale 1.04");
            OfflineButtonRules.Bob(s, 1600, out dy, out sc);         // 한 바퀴
            Assert.AreEqual(0, dy, 1e-9, "한 바퀴 뒤 제자리로 (infinite)");
            OfflineButtonRules.Bob(s, 1600 * 5 + 800, out dy, out sc);
            Assert.AreEqual(-0.14, dy, 1e-9, "여러 바퀴 뒤에도 같은 자리 (무한 반복)");
        }

        [Test]
        public void 졸음글자는_투명하게_시작해_머물다_사라진다()
        {
            OfflineButtonSpec s = S();
            Assert.AreEqual(0, OfflineButtonRules.Zzz(s, 0, 0).Alpha, 1e-9, "0% α0 (정본 keyframe)");
            Assert.AreEqual(1, OfflineButtonRules.Zzz(s, 2700 * 0.16, 0).Alpha, 1e-9, "16% α1");
            Assert.AreEqual(1, OfflineButtonRules.Zzz(s, 2700 * 0.5, 0).Alpha, 1e-9, "16~70% 는 머문다");
            Assert.AreEqual(1, OfflineButtonRules.Zzz(s, 2700 * 0.7, 0).Alpha, 1e-9, "70% 까지 α1");
            Assert.Less(OfflineButtonRules.Zzz(s, 2700 * 0.9, 0).Alpha, 1.0, "70% 뒤로는 옅어진다");
            Assert.AreEqual(0, OfflineButtonRules.Zzz(s, 2700 * 0.999999, 0).Alpha, 1e-4, "100% α0");
        }

        [Test]
        public void 졸음글자는_오른쪽_위로_커지며_돈다()
        {
            OfflineButtonSpec s = S();
            ZzzFrame a = OfflineButtonRules.Zzz(s, 0, 0);
            Assert.AreEqual(0, a.DxRem, 1e-9);
            Assert.AreEqual(0, a.DyRem, 1e-9);
            Assert.AreEqual(0.55, a.Scale, 1e-9, "0% scale .55");
            Assert.AreEqual(-10, a.RotDeg, 1e-9, "0% rotate −10deg");
            ZzzFrame z = OfflineButtonRules.Zzz(s, 2700 * 0.999999, 0);
            Assert.AreEqual(0.75, z.DxRem, 1e-3, "100% 오른쪽 .75rem");
            Assert.AreEqual(-1.7, z.DyRem, 1e-3, "100% 위로 1.7rem");
            Assert.AreEqual(1.25, z.Scale, 1e-3, "100% scale 1.25");
            Assert.AreEqual(12, z.RotDeg, 1e-2, "100% rotate 12deg");
            ZzzFrame mid = OfflineButtonRules.Zzz(s, 2700 * 0.5, 0);
            Assert.Greater(mid.DxRem, 0.75 * 0.5, "ease-out 이라 앞이 빠르다 — 절반 시각에 절반보다 멀리 가 있다");
        }

        [Test]
        public void 글자_셋은_구백밀리초씩_어긋난다()
        {
            OfflineButtonSpec s = S();
            Assert.AreEqual(0, OfflineButtonRules.DelayMs(s, 0), 1e-9);
            Assert.AreEqual(900, OfflineButtonRules.DelayMs(s, 1), 1e-9, "정본 nth-child(2) .9s");
            Assert.AreEqual(1800, OfflineButtonRules.DelayMs(s, 2), 1e-9, "정본 nth-child(3) 1.8s");
            // 지연 전에는 0% 자리에 머문다(JS animation-delay 와 같다)
            ZzzFrame late = OfflineButtonRules.Zzz(s, 100, 2);
            Assert.AreEqual(0, late.Alpha, 1e-9, "지연 중에는 안 보인다");
            Assert.AreEqual(0.55, late.Scale, 1e-9, "지연 중에는 0% 자리");
            // 같은 위상에서는 셋이 같은 그림이다(한 줄기 졸음)
            Assert.AreEqual(OfflineButtonRules.Zzz(s, 1000, 0).DxRem,
                            OfflineButtonRules.Zzz(s, 1900, 1).DxRem, 1e-9, "지연만 다르고 궤적은 같다");
        }

        [Test]
        public void 표가_어긋나면_읽다가_막는다()
        {
            Assert.Throws<FormatException>(() => OfflineButtonSpec.From(MiniJson.ParseObject(
                @"{""layout"":{""btn_rem"":0,""left_rem"":0.5,""bottom_rem"":0.6,""chest_shadow_dy_rem"":0.1,
                ""press_dy_rem"":0.08,""press_scale"":0.94,""ready_sec"":60,""bob_ms"":1600,""bob_dy_rem"":0.14,
                ""bob_scale"":1.04,""zzz_left_f"":0.58,""zzz_bottom_f"":0.62,""zzz_font_rem"":0.78,""zzz_line_px"":2.5,
                ""zzz_ms"":2700,""zzz_gap_ms"":900,""zzz_count"":3,""zzz_end_dx_rem"":0.75,""zzz_end_dy_rem"":-1.7,
                ""zzz_from_scale"":0.55,""zzz_to_scale"":1.25,""zzz_from_rot"":-10,""zzz_to_rot"":12,
                ""zzz_in_f"":0.16,""zzz_out_f"":0.7,""tick_ms"":1000}}")), "btn_rem 0 은 막는다");
            Assert.Throws<FormatException>(() => OfflineButtonSpec.From(MiniJson.ParseObject(
                @"{""layout"":{""btn_rem"":2.9,""left_rem"":0.5,""bottom_rem"":0.6,""chest_shadow_dy_rem"":0.1,
                ""press_dy_rem"":0.08,""press_scale"":0.94,""ready_sec"":60,""bob_ms"":1600,""bob_dy_rem"":0.14,
                ""bob_scale"":1.04,""zzz_left_f"":0.58,""zzz_bottom_f"":0.62,""zzz_font_rem"":0.78,""zzz_line_px"":2.5,
                ""zzz_ms"":2700,""zzz_gap_ms"":900,""zzz_count"":3,""zzz_end_dx_rem"":0.75,""zzz_end_dy_rem"":-1.7,
                ""zzz_from_scale"":0.55,""zzz_to_scale"":1.25,""zzz_from_rot"":-10,""zzz_to_rot"":12,
                ""zzz_in_f"":0.8,""zzz_out_f"":0.7,""tick_ms"":1000}}")), "in_f 가 out_f 보다 크면 막는다");
        }
    }
}
