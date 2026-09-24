using NUnit.Framework;
using UnityEngine;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T415 13회차 — 자동 제련 팝업 넷(4724 `.af-check` .35rem · 4759 `.af-toggle` 1rem · 4763 `.af-toggle .knob` 50% · 4772 `.af-sub-row` 1rem)과
    /// 장비 정보 팝업 넷(5059 `.fi-info-btn` 50% · 5065 `.fi-pill` 1rem · 5070 `.fi-pill-ico` 50% · 5150 `.fi-card .fi-skip` .6rem).
    /// 리터럴이던 둘(체크 상자 `cb * 0.2f` · 건너뛰기 버튼 공용 `btn_r`)은 표 `RadiusUi.json` 키로 옮겼다(§1 «수치는 코드에 박지 않는다»).
    /// 1rem 셋은 상자 반높이가 1rem 보다 작아 정본도 알약으로 줄이는 자리(결정 543) — 표가 아니라 여기서 그 셈을 못박는다.
    /// </summary>
    public class AutoForgeRadiusTests
    {
        static float Rem { get { return UiKit.L("rem_h") * UiKit.RefH; } }

        [Test]
        public void 표는_체크_상자와_건너뛰기_버튼_모서리를_정본_값_그대로_쥔다()
        {
            Assert.AreEqual(0.35f * Rem, RadiusUi.Px("af_check_r_rem"), 1e-3f, "정본 4724 `.af-check { .35rem }` — 전엔 상자 한 변의 20%(≈.22rem)가 박혀 있었다");
            Assert.AreEqual(0.6f * Rem, RadiusUi.Px("fi_skip_r_rem"), 1e-3f, "정본 5150 `.fi-card .fi-skip { .6rem }` — 전엔 공용 btn_r(.7)");
            Assert.AreNotEqual(UiKit.H("btn_r"), RadiusUi.Px("fi_skip_r_rem"), "공용 버튼 반지름과 다른 값이라 인수를 연 것이다");
        }

        [Test]
        public void 토글_필터_행_재화_알약은_정본_1rem_이_알약으로_줄어든_자리다()
        {
            float canon = 1f * Rem;
            float tgH = ForgeAutoStyle.L("af_toggle_h_rem") * Rem;   // T365 28회차 — 트랙은 이제 제 표(1.269rem · 전엔 설정 토글 settings_toggle_h 를 빌렸다)
            Assert.AreEqual(1.269f * Rem, tgH, 1e-3f, "정본 4760 `.af-toggle { height: 1.269rem }` — 표 af_toggle_h_rem");
            Assert.IsTrue(RadiusRules.IsPill(canon, tgH), "정본 4759 `.af-toggle { height: 1.269rem; border-radius: 1rem }` ↔ 클론 트랙 " + tgH.ToString("0.0") + "px 의 반 < 1rem(" + canon.ToString("0.0") + "px) — 알약(결정 543)");
            Assert.IsTrue(RadiusRules.IsPill(canon, 1.269f * Rem), "정본 쪽 셈으로도 알약(반높이 .63rem < 1rem)");
            Assert.IsTrue(RadiusRules.IsPill(canon, 1.75f * Rem), "정본 4772 `.af-sub-row { 1rem }` ↔ 클론 행 1.75rem(반높이 .875rem) — 알약");
            Assert.IsTrue(RadiusRules.IsPill(canon, 1.7f * Rem), "정본 5065 `.fi-pill { 1rem }` ↔ 클론 알약 1.7rem(반높이 .85rem) — 알약");
            Debug.Log("[T415] 토글 " + tgH.ToString("0.0") + "px · 필터 행 " + (1.75f * Rem).ToString("0.0") + "px · 재화 알약 " + (1.7f * Rem).ToString("0.0") + "px · 정본 1rem " + canon.ToString("0.0") + "px");
        }
    }
}
