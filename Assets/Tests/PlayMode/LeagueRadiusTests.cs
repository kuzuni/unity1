using NUnit.Framework;
using UnityEngine;
using Forge.Core.Ui;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T415 11회차 — 리그 화면의 모서리 다섯은 정본이 **선택자마다** 못 박은 값이다:
    /// `.league-avatar` **.4rem**(2331) · `.league-score` **.5rem**(2340) · `.league-ticket-pill` **.5rem**(2591) ·
    /// `.league-challenge-row` **.7rem**(2606) · `.league-challenge-avatar` **.4rem**(2627).
    /// 클론 `LeagueSheet.cs` 는 그 값을 `rem * 0.4f` 처럼 **코드에 박아** 뒀다(§1 «수치는 코드에 박지 않는다») — 표 `RadiusUi.json` 키로 옮겼다.
    /// 값은 그대로라 화면은 안 바뀐다. 같이 남는 셋(`.league-season-bar` · 보상/등급 격자 알약)은 **알약 동치**(결정 543)이므로
    /// 표가 아니라 여기서 그 셈을 못박는다 — 정본 1rem 이 그 상자의 반높이보다 커서 CSS 가 «높이의 반» 으로 줄인다.
    /// </summary>
    public class LeagueRadiusTests
    {
        static float Rem { get { return UiKit.L("rem_h") * UiKit.RefH; } }

        [Test]
        public void 표는_리그_모서리_다섯을_정본_값_그대로_쥔다()
        {
            Assert.AreEqual(0.4f * Rem, RadiusUi.Px("league_avatar_r_rem"), 1e-3f, "정본 2331 `.league-avatar { .4rem }`");
            Assert.AreEqual(0.5f * Rem, RadiusUi.Px("league_score_r_rem"), 1e-3f, "정본 2340 `.league-score { .5rem }`");
            Assert.AreEqual(0.5f * Rem, RadiusUi.Px("league_ticket_pill_r_rem"), 1e-3f, "정본 2591 `.league-ticket-pill { .5rem }`");
            Assert.AreEqual(0.7f * Rem, RadiusUi.Px("league_challenge_row_r_rem"), 1e-3f, "정본 2606 `.league-challenge-row { .7rem }`");
            Assert.AreEqual(0.4f * Rem, RadiusUi.Px("league_challenge_avatar_r_rem"), 1e-3f, "정본 2627 `.league-challenge-avatar { .4rem }`");
        }

        [Test]
        public void 시즌_바와_보상_격자_알약은_정본_1rem_이_알약으로_줄어든_자리다()
        {
            float canon = 1f * Rem;
            float barH = UiKit.H("league_bar_h");
            Assert.IsTrue(RadiusRules.IsPill(canon, barH),
                "정본 2300 `.league-season-bar { border-radius: 1rem }`(" + canon.ToString("0.0") + "px) ≥ 바 높이의 반(" + (barH * 0.5f).ToString("0.0") + "px) — CSS 가 알약으로 줄인다(결정 543)");
            float gridRowH = PopupKit.FontSize(TextKind.Sub) * 1.5f;
            Assert.IsTrue(RadiusRules.IsPill(canon, gridRowH),
                "정본 2516·2582 격자 알약 1rem ≥ 줄 높이의 반(" + (gridRowH * 0.5f).ToString("0.0") + "px) — 같은 알약 동치");
            Debug.Log("[T415] 시즌 바 " + barH.ToString("0.0") + "px · 격자 줄 " + gridRowH.ToString("0.0") + "px · 정본 1rem " + canon.ToString("0.0") + "px");
        }

        /// <summary>T415 12회차 — 리그 넷 더(정본 2326 `.league-row` .6 · 2483 `.league-reward-banner` .3 · 2522 `.league-collect-pill` .5 · 2534 `.league-reward-table` .7rem)가
        /// `rem × 상수` 에서 표로 옮겨졌다(값 그대로 · 화면 변화 0).</summary>
        [Test]
        public void 표는_리그_행_리본_수집_알약_보상_표_모서리_넷을_정본_값_그대로_쥔다()
        {
            Assert.AreEqual(0.6f * Rem, RadiusUi.Px("league_row_r_rem"), 1e-3f, "정본 2326 `.league-row { .6rem }`");
            Assert.AreEqual(0.3f * Rem, RadiusUi.Px("lgr_banner_r_rem"), 1e-3f, "정본 2483 `.league-reward-banner { .3rem }`");
            Assert.AreEqual(0.5f * Rem, RadiusUi.Px("league_collect_pill_r_rem"), 1e-3f, "정본 2522 `.league-collect-pill { .5rem }`");
            Assert.AreEqual(0.7f * Rem, RadiusUi.Px("league_reward_table_r_rem"), 1e-3f, "정본 2534 `.league-reward-table { .7rem }`");
        }

        /// <summary>T415 12회차 — 채팅 미리보기 뱃지(정본 3246 `.chat-preview-badge { border-radius: .6rem }`)는 높이 .72rem 의 반(.36rem)이 .6rem 보다
        /// 작아 정본도 알약으로 줄인다(결정 543) — 클론 `Hud.cs` 의 `badgeH * 0.5f` 가 그 동치다. 아바타(3232 .35rem)는 배경·테 없는 아이콘이라 자리가 없다.</summary>
        [Test]
        public void 채팅_미리보기_뱃지는_정본_06rem_이_알약으로_줄어든_자리다()
        {
            float canon = 0.6f * Rem, badgeH = 0.72f * Rem;
            Assert.IsTrue(RadiusRules.IsPill(canon, badgeH), "반높이 .36rem < .6rem — 알약 동치(결정 543)");
            Assert.Less(badgeH * 0.5f, canon, "클론이 쓰는 반높이가 정본 값보다 작다 — 그래서 반높이가 곧 정본이다");
        }
    }
}
