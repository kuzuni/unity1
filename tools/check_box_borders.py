#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T365 — 정본 상자 테(`border` · `border-top/-bottom/-left/-right`) ↔ 클론 테 호출 대조 자(T109 `check_keyline` · T331 `check_box_shadows` 꼴).

정본 `style.css` 의 테 선언을 전부 걷어 **선택자×변** 으로 접는다 — 쉼표 목록은 선택자마다 하나씩, 같은 (선택자, 변)의 뒤 규칙이
앞을 덮는다(계단 · T33 29회차가 다섯을 세었다). 값은 폭 단(`--ol1`~`--ol4` = 1·2·3·4 CSS px · `--cellb` = ol3 내림 · `none`/`0` ·
직접값 px/rem)과 색(`--pp-line` · `--rc` · hex · rgba)으로 가른다.

클론 자리 짝은 TABLE 하나가 쥔다(선택자 → 자리 목록):
  "Ui/File.cs#name"     그 이름으로 테를 세우는 호출이 있어야 함 — `PopupKit.Outlined(p, "name", …)` · `UiKit.Line(p, "name", …)` ·
                        `PetSkillKit.Framed(p, "name", …)` · `PetSkillKit.Orb(p, "name", …)` · `UiKit.Rounded(p, "name", …)`
  "Ui/File.cs@Method"   도우미 메서드 본문 안에 테 호출이 있어야 함(버튼·토스트처럼 한 곳이 여럿을 세우는 자리)
  "Ui/File.cs"          파일 안 어디든 테 호출 하나
  "—<이유>"             대조하지 않는다(정본이 «끄는» 규칙 none · 클론에 그 자리 자체가 없음)
표에 없는 선택자 = 미정(막지 않는다 · `--list` 로 본다 · 회차마다 표에 더한다). 빈자리 중 임자가 정해진 것은 KNOWN 에 두어 rc 0.
폭 단도 견준다(2회차): 호출의 폭 인자(`PopupKit.Line`/`Line3` · `PetSkillKit.Line2/Line3` · `line_px`·`line2_px`·`line3_px`·`line4_px`·`line1_px` · `line`/`line3` 변수)를
단으로 읽어 정본 단과 맞춘다(`cellb` = ol3 내림 → ol3 · 클론 line_px 2 = ol1 · line2_px 4 = ol2 · line3_px 6 = ol3 · T33 29회차). 도우미 매개변수(`line`·`linePx`·`borderPx`·`lineW`)로
넘겨받은 폭은 «못 읽음» 으로 두고 판정하지 않는다. `UiKit.Circle` 도 **짝**일 때만 테다(12회차) — 바깥 고리 `Circle(p, "line"|"ring", …)` 뒤에 안쪽 면 `Circle(p, "face", …)` 과
`PopupKit.Inset(face.rectTransform, <폭>)` 이 따라오는 꼴이 클론의 **둥근 테**다(오프라인 점·요율 원판·정보 버튼 버튼·알 칩 …). 그 `Inset` 의 폭이 곧 단이다. `UiKit.Rounded` 는 **짝**일 때만 테다 — 바깥 `Rounded(p, "line"|…)` 뒤에 `Rounded(…, r - <폭>)` 안쪽 면이 따라오면 그 «폭» 이 단이다.
`@Method` 자리는 본문 안의 단을 모두 모아 정본 단이 그중에 있으면 맞다(한 메서드가 여럿을 세운다).

사용:  python3 tools/check_box_borders.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 표의 선택자가 전부 정본에 있고 · 표의 자리 전부가 (테 호출이 있거나 KNOWN) · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다

테 **스타일**(T472): 정본 `border-style:` 과 단축 `border:` 안의 낱말(dashed/dotted/double/solid/none)을 선택자별로 읽어(뒤 규칙이 덮는다)
  DASHED 표의 선택자가 정본에서 정말 `dashed` 인지 · 그 자리에 점선 호출(`SurfaceArt.DashedFrame`)이 있는지 본다. 정본이 dashed 로 적었는데 표에 없는 선택자는 «점선 미정» 으로 세기만 한다.
"""
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# ── 정본 선택자 ↔ 클론 자리 ───────────────────────────────────────────────────
# 선택자는 style.css 의 것을 공백 하나로 정규화한 그대로(쉼표 목록은 갈라 하나씩). «변» 이 여럿이면 «선택자|변» 꼴(변 = top/bottom/left/right · 없으면 border 통째).
TABLE = {
    # T33 29회차가 실물로 연 셋(SkillSummonResult BuildCell/BuildFoot Framed line1_px + 같은 색)
    '.sr-qty': ['Ui/SkillSummonResult.cs#sr-qty'],
    '.sr-dup': ['Ui/SkillSummonResult.cs#sr-dup'],
    '.sr-hint': ['Ui/SkillSummonResult.cs#sr-hint'],
    '.sr-new': ['Ui/SkillSummonResult.cs#sr-new'],
    '.cmp-card': ['Ui/ForgeUi.cs@ItemCard'],   # T472 — 1826 ol3 --pp-line: 실물은 빈 슬롯 갈래의 점선 테(DashedFrame · Line3) — 장착·새 장비 갈래는 1812·1815 가 `border: none` 으로 덮는다(그 둘은 미정 그대로 · 같은 메서드라 «테 호출 0» 으로 못 가른다)
    # 한 줄 테 — UiKit.Line(p, "line", …)
    '#topbar|bottom': ['Ui/Hud.cs@Build'],
    '#tabbar|top': ['Ui/TabBar.cs@Build'],
    '#equip-sheet|top': ['Ui/UiRoot.cs@Build'],
    '#chat-preview|top': ['Ui/UiRoot.cs@Build'],
    '.panel|top': ['Ui/TabBar.cs@Build'],
    '.league-foot|top': ['Ui/LeagueSheet.cs@RenderBoard'],
    '.hatchery|top': ['Ui/PetPanel.cs@BuildHatchery'],
    '.tech-branch-head|bottom': ['Ui/TechPanel.cs@BranchCard'],
    '.tb-row|bottom': ['Ui/TechPopups.cs@OpenBonuses'],
    # 공용 도우미(Popups.cs) — 카드·버튼·토스트·토글·아바타
    '.modal-card': ['Ui/Popups.cs@Card'],
    '.modal-card .btn': ['Ui/Popups.cs@Btn'],
    '.panel .btn': ['Ui/Popups.cs@Btn'],
    '#equip-sheet .btn': ['Ui/Popups.cs@Btn'],
    '.btn': ['—덮인다: 정본 마크업의 `.btn` 은 전부 `.modal-card`·`.panel`·`#equip-sheet` 안(3543 ol3 pp-line 이 덮는다)이거나 제 규칙이 있는 것(60 `.summon-btn` · 501 `.sr-ok` · 674 `.sr-again` · 5902 offline-card 안) — 26회차에 ui.js 의 `class="btn` 78 곳·index.html 0 곳을 전수(가장 바깥이 되는 것은 HUD 에 없다). 664 의 ol1 #444c56 은 실물에 한 번도 안 선다 — 2회차 KNOWN «바닥 버튼(HUD·오프라인·리그 뒤로)» 은 오인(offline-btn 207 none · league-back-btn 은 제 행)'],
    '.btn.silver': ['Ui/Popups.cs@Btn'],
    '.league-back-btn': ['Ui/Popups.cs@BackButton'],
    '.toast': ['Ui/Popups.cs@Toast'],
    '.settings-toggle': ['Ui/Popups.cs@Toggle'],
    '.profile-avatar-big': ['—Popups.Avatar 의 폭은 호출부 인수(ProfilePopup 이 PopupKit.Line3 를 준다 · 10회차) — 자는 인수 이름만 봐서 못 읽고 PlayMode BoxBorderSitesTests 가 ol3 를 지킨다'],
    # 12회차 — 둥근 테 짝(Circle+Inset)을 읽게 된 뒤 열린 자리들 · 오프라인/정보 버튼/웨이브
    '.offline-collect-dot': ['Ui/OfflinePopup.cs@CollectDot'],
    # ── T365 21회차 — 정본 js·html 에 클래스가 0 인 **잔재 선언 여섯**(T377 23회차·T396 이 같은 잣대로 가른 자리 · 그려지지 않는 리터럴이라 클론에 짝이 없다).
    '.age-tag|left': ['—죽음: 정본 js·html 에 `age-tag` 0(708 · 왼쪽 ol3 #444c56) — 그려지지 않는 잔재'],
    '.hatch-slot': ['—죽음: 정본이 `hatch-slot` 을 한 번도 안 붙인다(ui.js 3926 `.hatch-cell` 로 갈아엎었다 · 1650 ol1 dashed --rc)'],
    '.egg-chip': ['—죽음: 정본 js·html 에 `egg-chip` 0(1658 ol1 --rc)'],
    '.pet-card': ['—죽음: 정본 js·html 에 `pet-card` 0(1665 ol1 #444c56 · 펫 목록은 `.pet-tile` 격자)'],
    '.pet-card .icon-circle': ['—죽음: 같은 까닭(1676 ol2 --rc) — `icon-circle` 은 다른 자리(채팅 공유 카드)에 살지만 `.pet-card` 조상이 없다'],
    '.summon-prog': ['—죽음: 정본 js·html 에 `summon-prog` 0(4233 ol3 --pp-line · 소환 바 진행은 `.summon-bar`·`.summon-gauge` 갈래)'],
    '.offline-rate-icon': ['—덮인다: 실물은 언제나 `.coin`/`.hammer`(ui.js 5896~5897)이고 7268 이 그 둘에 `border: none` 을 준다 — 274 의 ol2 #000 은 실물에 안 선다(7268 쪽 두 줄이 이 자리의 임자다)'],
    '.offline-rate-icon.coin': ['Ui/OfflinePopup.cs@Rate'],
    '.offline-rate-icon.hammer': ['Ui/OfflinePopup.cs@Rate'],
    '#offline-btn': ['—버튼 자체엔 테가 없다(정본 207 `border: none`) · 클론 `Ui/OfflineButton.cs` 도 테 호출 0 — 12회차가 전수 확인'],
    '.waypoint': ['—이정표 버튼엔 테가 없다(정본 316 `border: none`) · 클론 `Ui/Waypoints.cs` 는 시간 배지 `Rounded` 채움 하나뿐(고리 짝 아님)'],
    '.info-btn': ['—덮인다: 정본 마크업 세 곳이 전부 덮는 규칙 안에 있다 — `ui.js` 1544 는 `#equip-sheet` 안(3634 이 이긴다) · 2053·5459 는 `.fi-info-btn`(5059) 이다. 그래서 971 의 `--ol1` #444c56 고리는 **실물에 한 번도 안 선다** — 클론에도 그 갈래를 두지 않는다(T365 13회차 · `.offline-rate-icon` 과 같은 꺼)'],
    '#equip-sheet .info-btn': ['Ui/ForgeUi.cs@InfoButton'],
    '.fi-info-btn': ['Ui/ForgeUi.cs@InfoButton'],
    # 고리는 `RebuildPips` 가 만들고 **두께는 `SetWaves` 가 프레임마다 면 크기로** 낸다(15회차 ⑶ 꼴) — 단을 읽을 수 있는 쪽이 그 자리다.
    '.pip': ['Ui/Hud.cs@SetWaves'],
    '#wave-pips::before|top': ['Ui/Hud.cs@RebuildPips'],
    '#wave-pips::before|bottom': ['Ui/Hud.cs@RebuildPips'],
    # 상단바 카드·아바타·채팅 배지(Hud) · 채팅 화면(ChatScreen)
    '.profile-card': ['Ui/Hud.cs@Build'],
    '.profile-card .avatar': ['Ui/Hud.cs@Build'],
    '.chat-preview-badge': ['Ui/Hud.cs@BuildChat'],
    '.chat-input-bar|top': ['Ui/ChatScreen.cs@Open'],
    '.chat-input-bar input': ['Ui/ChatScreen.cs@Open'],
    '.chat-input-bar .btn.danger.round': ['Ui/ChatScreen.cs@Open'],   # T469 — 3284 `border: var(--ol2)` 를 3289 `border-width: var(--ol1)` 이 덮는다 → ol1(같은 메서드의 위 테·입력칸 ol2 와 섞여 «@» 판정은 단만 못 가른다 · PlayMode BoxBorderSitesTests 가 직접 잰다)
    # 카드 면 — PopupKit.Outlined(p, "face", …)
    '.league-row': ['Ui/LeagueSheet.cs@Row'],
    '.league-collect-pill': ['Ui/LeagueSheet.cs@RenderRewards'],
    '.league-reward-table': ['Ui/LeagueSheet.cs@RenderRewards'],
    '.pass-banner': ['Ui/PassPopup.cs@Render'],
    '.pass-cell': ['Ui/PassPopup.cs@Cell'],
    '.shop-banner': ['Ui/ShopSheet.cs@Banner'],
    '.shop-deal-card': ['Ui/ShopSheet.cs@Render'],
    '.shop-gem-card': ['Ui/ShopSheet.cs@Render'],
    '.qst-row': ['Ui/QuestSheet.cs@Render'],
    # 프로필 팝업
    '.profile-field': ['Ui/ProfilePopup.cs@Field'],
    '.profile-edit-btn': ['Ui/ProfilePopup.cs@EditButton'],
    '.profile-tabs': ['—탭 띠 고리(pp_line) 안에 탭 버튼이 Panel 을 Line3 만큼 안쪽에 두는 꼴(ol3 · 맞다) — 안쪽 면 Rounded 가 없어 짝으로 못 읽는다(T365 4회차 실물 확인)'],
    '.avatar-pick-btn': ['Ui/ProfilePopup.cs@RenderProfile'],
    '.settings-act': ['Ui/ProfilePopup.cs@ActRow'],
    # 펫·스킬 타일(PetSkillKit.Framed/Orb)
    '.pet-tile .tile-face': ['Ui/PetPanel.cs@TileFace'],
    '.sk-orb': ['Ui/SkillPanel.cs@BuildGrid'],
    '.equipped-row': ['Ui/SkillPanel.cs@BuildEquippedRow', 'Ui/PetPanel.cs@BuildEquippedRow'],
    '.equipped-label': ['Ui/SkillPanel.cs@EquippedLabel'],
    '.sk-mini small': ['Ui/SkillPanel.cs@MiniLv'],
    '.petup-panel': ['Ui/PetUpgradePopup.cs@Render'],
    '.rate-bar': ['Ui/SkillRatesPopup.cs@Render'],
    # 기술 트리(DungeonPopups.Bordered)
    '.tech-branch-card': ['Ui/TechPanel.cs@BranchCard'],
    '.tech-tier-tag': ['Ui/TechPanel.cs@RenderBranch'],
    '.tech-prog': ['Ui/TechPopups.cs@RenderAction'],
    '.modal-card .tech-prog': ['Ui/TechPopups.cs@RenderAction'],
    '.asc-focus': ['Ui/AscendPopup.cs@Open'],
    '.dgc-cell': ['Ui/DungeonClearPopup.cs@Show'],
    '.modal-card.sheet .dg-banner': ['Ui/DungeonSheet.cs@Banner'],
    # 3회차 — 미정에서 옮긴 자리(스킬 바·서브탭·기술 트리·업그레이드 막대·아바타·통화 알약·소환 결과 발)
    '.skill-btn': ['Ui/SkillBar.cs@Render'],
    '.skill-btn.empty': ['Ui/SkillBar.cs@Render'],
    '#summon-subtabs.subtab-strip|top': ['Ui/SkillPetSheet.cs@Build'],
    # T365 17회차 — 소환 서브탭 셋. 정본은 같은 띠 안에서 **끄고(3606) 켠다(3614)**: 비활성 칸엔 테가 없고 **활성 칸만 ol2** 다.
    #   클론도 그대로다 — 띠(`strip`)는 채움 + 위 키라인뿐이고, 칸은 `active` 스킨(`PetSkillKit.Framed(… Line2)`)만 테를 진다.
    '#summon-subtabs.subtab-strip button.active': ['Ui/SkillPetSheet.cs@Build'],
    '#summon-subtabs.subtab-strip': ['—띠 자체엔 테가 없다(정본 3594 `border: none`) · 클론 `SkillPetSheet.Build` 의 띠는 채움(`bg`)과 **위 키라인 한 줄**뿐이고 그 키라인은 3943 `#summon-subtabs|top`(ol3) 이 따로 쥔다 — 이 몸통의 테 호출은 활성 칸 몫이라 «없어야 한다» 를 이 자리로는 못 가른다(17회차 눈으로 확인)'],
    '#summon-subtabs.subtab-strip button': ['—비활성 칸엔 테가 없다(정본 3606 `border: none`) · 클론은 `subSkins[i]` 를 **활성일 때만** 켠다 — 같은 몸통에 활성 칸(ol2)이 같이 있어 «테 호출 0» 으로는 못 가른다(17회차 눈으로 확인)'],
    # T365 17회차 — 정본이 «끈다» 고만 적은 셋. 셋 다 클론이 이미 정본대로인데, 그 몸통에 **다른 자리의 테**가 같이 있어 자로는 못 가른다.
    '.pill-plus': ['Ui/Hud.cs@PlusBadge'],
    # T365 20회차 — 모루 자리 셋(`ForgeSheet.cs` 가 열렸다). `ForgeUi.Tile`(고리+면+Inset 을 한 번에 내는 이 레포 도우미)을
    #   이 회차에 도우미 목록에 넣어 «보류 카드» 의 ol3 을 자가 읽게 됐다.
    '.anvil-btn.held-slot': ['Ui/ForgeSheet.cs@AnvilSlot'],
    '.anvil-btn': ['—모루 버튼 자체엔 테가 없다(정본 983 `border: none`) · 클론 `ForgeSheet.AnvilSlot` 의 테 호출은 **보류 카드**(`held-slot`)와 그 배지 몫이라 «테 호출 0» 으로는 못 가른다 — 같은 메서드가 두 꼴(모루/보류)을 다 낸다(20회차 눈으로 확인)'],
    '.anvil-btn.held-slot .held-tag': ['—정본 1000 `border: var(--ol2) solid #000` · 클론은 `PopupKit.Line`(ol1)이라 한 단 얇았다 → 20회차에 `Line2` 로 고쳤다. 배지 고리는 «면 먼저, 고리 나중 + 따로 `Inset`» 꼴이라 짝 탐지가 안 잡는다 — PlayMode `BoxBorderSitesTests` 가 직접 잰다'],
    # T365 19회차 — 자동 제련 팝업의 검은 두 겹. 정본 4785·4793 은 **면 #17181a + ol3 --pp-line 테** 인데
    #   클론은 `pp_line`(#000000) 판 **한 장**이라 테가 아예 없었고 면까지 순검정이었다(`pp_ink` 가 곧 #17181a).
    #   ⚠ 자로는 **못 가른다** — 둘 다 `Render` 한 몸통 안이고 그 몸통엔 ol3 테가 여럿이라 «단 ol3» 판정이
    #      이 자리 덕인지 옆자리 덕인지 갈리지 않는다(고장 주입 셋이 전부 rc 0 이었다). 그래서 초록으로 세지 않고,
    #      대신 **PlayMode `BoxBorderSitesTests` 가 그 자리를 직접 잰다**(고리+면 짝 · 테 폭 ol3 · 면색 pp_ink).
    '.af-spinner': ['—정본 4785 `면 #17181a + ol3 --pp-line`. 19회차가 `RadiusUi.Outlined(… "pp_ink" … Line3)` 로 두 겹을 세웠다(전엔 `pp_line` 판 한 장이라 테가 없고 면까지 순검정이었다) — 자로는 못 가르니 PlayMode 자가 잰다'],
    '.af-dd-list': ['—정본 4793 도 같은 두 겹. 19회차가 같이 세웠다 — 자로는 못 가르니 PlayMode 자가 잰다'],
    # T365 18회차 — 자유 파일에 남아 있던 둘.
    '.tb-row:last-child|bottom': ['—마지막 줄만 밑줄이 없다(정본 2252 `border-bottom: none`) · 클론 `TechPopups` 336 이 `if (i < lines.Count - 1)` 로 **마지막만 건너뛴다**(그 위 2249 `.tb-row|bottom` ol2 는 이미 초록) — 같은 몸통이 다른 줄엔 선을 그으니 «테 호출 0» 으로는 못 가른다(18회차 눈으로 확인)'],
    '#panel-debug input[type=number]': ['—클론엔 그 자리가 없다: 디버그 패널은 **숫자 입력칸을 안 쓴다**(`DebugPanel.cs` 에 `InputField` 0 · 스테이지 이동·재화 더하기를 전부 버튼으로 낸다) — 정본 680 의 테는 클론에 짝이 없다(18회차 전수 확인)'],
    '#tabbar button': ['—버튼 자체엔 테가 없다(정본 1700 `border: none`) · 클론 `TabBar.Build` 의 테 호출 둘은 **바의 위 키라인**(`UiKit.Line(band, …)` = `#tabbar` 제 테)과 **✕ 표식**(`.tab-x-mark` 의 고리 원)이라 버튼 짝이 아니다(17회차 눈으로 확인)'],
    '.skill-btn .sk-lv': ['—Lv 배지엔 판도 테도 없다(정본 606 `background: none; border: none`) · 클론은 **T109 8회차가 알약을 걷고** `PetSkillKit.Stroked` 글자 한 장으로 돌렸다 — 같은 몸통에 오브 고리(ol3)가 있어 «테 호출 0» 으로는 못 가른다(17회차 눈으로 확인)'],
    '.upg-progress': ['Ui/ForgeInfoPopup.cs@RenderLevelView'],
    '.tech-node': ['Ui/TechPanel.cs@Node'],
    '.tech-tree-node': ['Ui/TechPanel.cs@Node'],
    '.tech-branch-icon::before': ['Ui/TechPanel.cs@BranchCard'],
    '.icon-circle.sm': ['Ui/TechPopups.cs@RenderNode'],
    '.settings-toggle::after': ['Ui/Popups.cs#knob'],   # 10회차 — 손잡이 고리(ol1) · 토글 몸통(ol2)과 따로 센다
    '.league-avatar': ['Ui/Popups.cs@Avatar'],
    '.league-challenge-avatar': ['Ui/Popups.cs@Avatar'],
    '.chat-avatar': ['Ui/Popups.cs@Avatar'],
    '.pinfo-id .avatar': ['Ui/Popups.cs@Avatar'],
    '.pinfo-preview': ['Ui/PlayerInfoPopup.cs@Fallback'],
    '.cur-pill': ['Ui/DungeonPopups.cs@CurPill'],   # 10회차 — 정본 .cur-pill 은 시트 머리(펫·리그·상점 ui.js 3968·4298·4988)이지 HUD 윗줄(.currency-pills .pill)이 아니다 · 클론 CurPill 은 Bordered Line3 = ol3
    '.qst-bar': ['Ui/QuestSheet.cs@Render'],
    '.pass-milestone-label': ['Ui/PassPopup.cs@Render'],
    '.petup-bulk': ['Ui/PetUpgradePopup.cs@Render'],
    '.rates-prog': ['Ui/SkillRatesPopup.cs@Render'],
    '.summon-bar .btn.x5-toggle': ['Ui/SkillPanel.cs@MultToggle', 'Ui/PetPanel.cs@MultToggle'],
    '.petd-share': ['Ui/PetPanel.cs@OpenPetDetail'],
    '.sr-ok': ['Ui/SkillSummonResult.cs@BuildFoot'],
    '.sr-solo-own': ['Ui/SkillSummonResult.cs@BuildFoot'],
    '.sr-again': ['Ui/SkillSummonResult.cs@BuildFoot'],
    '.skd-btn.silver': ['Ui/Popups.cs@Btn'],
    # T365 ⓐ — 펫 카드 왼쪽 등급색 띠(1664 border-left var(--ol4)) · 클론에 pet-card 이름 0 · PetPanel.cs 는 T331·T333 lock
    '.pet-card|left': ['—정본 ui.js·index.html 에 .pet-card 쓰임 없음(죽은 CSS · 10회차) · line4_px 키는 그대로 둔다'],
    # ── T365 22회차(2026-09-24 · 워커 T · sess-1447-10982) — 미정 71 을 셋으로 갈라 짝지었다: ⓐ 실물 자리(도우미 호출) ⓑ «끄는» 자리(none · 그 몸통을 읽고 까닭을 적음) ⓒ 구운 판·판 겹(자가 못 읽는 꼴).
    #   자에 둘을 더 가르쳤다 — `PetSkillKit.Gauge`(게이지 = Framed 한 겹 · 폭 인자 8)와 **맨 이름 `Tile(`**(ForgeUi 제 안의 호출 · ItemTile·AgeBar 가 그렇게 부른다 · DungeonPopups 의 맨 `Bordered` 와 같은 갈래).
    # ⓐ 실물 자리
    '.mat-chip': ['Ui/MountUpgradePopup.cs@Render'],   # 807 ol2 --rc — 재료 칩 `Framed(cr, "skin", … Line2)` · 고리 색은 등급색(선택/탑승 갈래는 표 키)
    '.equip-cell': ['Ui/ForgeUi.cs@ItemTile', 'Ui/ForgeSheet.cs@EquipCell', 'Ui/PlayerInfoPopup.cs@EquipCell'],   # 838 cellb(= ol3 내림) — 세 공장 다 `Tile(… "frame" … Line3)` · 색은 CellLine(섞은 시대색 · 면 축)
    '.auto-drop-card': ['Ui/ForgeUi.cs@ItemTile'],   # 1071 cellb — `ForgeCraftPopup.CraftCard` → ItemTile 틀(Line3) · `MixFrame` 이 색만 표 키로 덮는다(T371 5회차)
    '.craft-batch .cb-card': ['Ui/ForgeUi.cs@ItemTile'],   # 1138 cellb — 같은 CraftCard 길
    '.cmp-img': ['—정본 1856 ol2 --pp-line: 23회차가 `ItemTile` 에 `lineW` 인자를 열고 `ItemCard` 가 `PopupKit.Line2` 와 고리색 pp_line 을 준다 — 틀 폭이 인자라 자가 못 읽는다(`lineW > 0f ? lineW : Line3`) · PlayMode `BoxBorderSitesTests` 가 비교·상세 카드의 틀 폭·고리색을 잰다'],
    '.idet-icon': ['Ui/ForgeInfoPopup.cs@RenderDetail'],   # 3685 **ol2 #d5d5d5 · 면 #e9e9e9** ↔ 클론 상세 머리는 ItemTile(ol3 · 시대색) — KNOWN(ForgeInfoPopup T178·T415 lock) · ⚠ 5601 기술 노드 머리도 같은 클래스(`idet-icon tn-bronze`)라 그 자리(TechPopups)도 같은 테를 물려받는지 다음 사람이 본다
    '.chat-share-side .icon-circle.sm': ['Ui/Popups.cs@Avatar'],   # 3402 ol2 #000 — `ChatScreen.Side` 가 `PopupKit.Avatar` 기본 폭(Line2 · 10회차)으로 세운다
    '.x-btn': ['Ui/Popups.cs@XButton'],   # 3741 ol3 — 고리 Circle(ring) + 면 Circle(face) + 제 `Inset(face, Line3)`(BARE_INSET 갈래)
    '.modal-card .league-challenge-side .btn': ['Ui/Popups.cs@Btn'],   # 2644 ol3 — `RenderChallenge` 의 [도전] 은 공용 Btn(Line3)
    '.fi-card .fi-skip': ['Ui/Popups.cs@Btn'],   # 5153 ol3 — [건너뛰기] 도 공용 Btn(Line3 · 12번째 인자는 키라인 몫)
    '.summon-btn': ['Ui/PetSkillKit.cs@PaperButton'],   # 5211 ol3 — 종이 버튼 껍질 `Framed(rt, "skin", dk, r, Line3)`(소환·승천 두 갈래 다 이 공장)
    '.summon-gauge': ['Ui/SkillPanel.cs#summon-gauge'],   # 5225 ol2 — `PetSkillKit.Gauge(… Line2 …)`(펫·스킬·탈것 셋이 `SummonInfo` 한 공장)
    '.sk-shard': ['Ui/SkillPanel.cs#sk-shard'],   # 4105 ol2 — 격자 칸·상세 카드 둘 다 `Gauge(… Line2 …)`
    '.petup-xpbar': ['Ui/PetUpgradePopup.cs#petup-xpbar'],   # 4352 ol3 — `Gauge(… Line3 …)`
    '.sk-mini': ['Ui/SkillPanel.cs@BuildEquippedRow', 'Ui/PetPanel.cs@BuildEquippedRow'],   # 4156 ol2 — 스킬 줄은 `Orb(br, "orb", … Line2)` · 펫 줄(3957 `sk-mini square`)은 `Framed(br, "sq", … Line2)`
    '.petup-icon': ['Ui/PetPanel.cs@TileFace'],   # 4343 ol3 — 업그레이드 머리 얼굴 = TileFace(Line3 · 반지름만 제 키)
    '.pass-badge.check': ['Ui/PassPopup.cs@Render'],   # 2849 ol2 — 체크 배지 `Circle(line)+Circle(face)+PopupKit.Inset(cf, Line2)`(23회차 · 전엔 Line)
    '.af-check': ['Ui/ForgeAutoPopup.cs#box', 'Ui/ForgeUi.cs#box'],   # 4726 **ol3** — 시대 막대 체크(AgeBar)는 23회차가 Line3 로 · 자동 제련 팝업 제 체크 둘(계속하기 · 옵션 줄)은 아직 `PopupKit.Line` = ol1 — KNOWN(ForgeAutoPopup T178 lock)
    '.af-bottom|top': ['Ui/ForgeAutoPopup.cs#af-bottom-line'],   # 4777 **ol3 위 키라인** ↔ 클론 `Render` 의 `af-bottom` 상자엔 `UiKit.Line` 이 0 — 선 자체가 없다 · KNOWN(같은 lock) · 풀리면 `UiKit.Line(bottom, "af-bottom-line", "pp_line", Line3, true)` 한 줄(이 이름이라야 이 행이 초록이 된다)
    '.af-toggle': ['Ui/Popups.cs@Toggle'],   # 4761 **ol3** ↔ `Popups.Toggle` 은 설정 토글 값 한 벌(몸통 Line2) — KNOWN(아래)
    '.af-toggle .knob': ['Ui/Popups.cs@Toggle'],   # 4765 **ol3** ↔ 손잡이 Line(ol1) — KNOWN(아래)
    '#summon-subtabs|top': ['Ui/SkillPetSheet.cs@Build'],   # 3943 ol3 — `#summon-subtabs.subtab-strip|top` 과 같은 `UiKit.Line(strip, … Line3, true)`
    # ⓑ 정본이 «끄는» 자리 — 몸통에 다른 테가 없어 «호출 0» 으로 가를 수 있는 것
    '.shop-sheet .sheet-head .cur-pill': ['Ui/ShopSheet.cs@CurBar'],   # 3971 none — 코인·젬 바는 `Rounded(bar, "bg")` 채움 한 장(고리 짝 아님) · 3968 의 일반 `.cur-pill`(ol3 · DungeonPopups.CurPill)은 펫·리그 머리 몫
    '.pet-tile': ['Ui/PetPanel.cs@PetTileAt'],   # 4261 none — 칸 버튼 껍데기엔 테가 없다 · 테는 안의 얼굴(`.pet-tile .tile-face` 표 행 · TileFace 공장) 몫이라 이 메서드엔 테 호출이 없다
    '.pet-tile.egg .tile-face': ['Ui/PetPanel.cs@EggTileAt'],   # 4307 none — 알 타일은 «배경·테·그림자 없는 그림만 칸»(정본 주석) · 클론도 Icon + 표 그림자뿐
    '.hatch-cell': ['Ui/PetPanel.cs@BuildHatchCell'],   # 4476 none — 부화 칸엔 테가 없다 · 칸 안의 [건너뛰기] 종이 버튼은 제 선택자(`.hatch-skip`) 몫이라 여기 안 센다
    '.tri-btn': ['Ui/SkillRatesPopup.cs@TriButton', 'Ui/DungeonPopups.cs@TriButton'],   # 4584 none — ◀▶ 는 아이콘 한 장(정본 4380·4671 두 자리 = 소환 확률 팝업 · 던전 상세)
    '.fi-pill-ico.coin': ['Ui/ForgeUi.cs@Pill'],   # 7259 none — 알약 안 아이콘(IconOr)엔 테가 없다 · 알약 면 `Rounded(rt, "face")` 는 채움 한 장(고리 짝 아님)
    '.fi-pill-ico.gem': ['Ui/ForgeUi.cs@Pill'],
    # ⓑ′ 정본이 «끄는» 자리 — 클론이 정본대로인데 **같은 몸통에 다른 자리의 테**가 있어 자로는 못 가른다(17회차 잣대 · 22회차 눈으로 읽음)
    '.subtab-strip button': ['—덮인다: 정본 마크업의 유일한 띠는 `#summon-subtabs`(index.html 137)이고 그 안 버튼은 3606 `#summon-subtabs.subtab-strip button { border: none }`(id 특이도)이 덮는다 · 활성 칸만 3614 ol2 — 둘 다 17회차 표 행'],
    '.forge-item-cell': ['—버튼 껍데기엔 테가 없다(736 `border: none`) · 클론 `ForgeInfoPopup.Cell` 의 테는 안의 타일(`fl-face.equip-cell` = ItemTile 틀 ol3 · 위 `.equip-cell` 행) 몫이라 «호출 0» 으로 못 가른다'],
    '.equip-cell .cell-img': ['—그림엔 테가 없다(1875 `border: none` · 틀 밖으로 `--cellb` 만큼 부풀린 상자) · 클론 `ItemTile` 의 `img`(IconOr)는 홀로 선 Image · 틀(frame)과 같은 몸통이라 «호출 0» 으로 못 가른다'],
    '.cmp-card-wrap.cur .cmp-card': ['—장착·새 장비 카드엔 테가 없다(1812·1815 `border: none`) · 클론 `ForgeUi.ItemCard` 는 `item != null` 갈래에서 테를 안 세운다(주석 «카드 자체는 판이 없다» · T57) — 빈 슬롯 갈래의 점선(`.cmp-card.empty` · DashedFrame)이 같은 메서드라 «호출 0» 으로 못 가른다(T472 가 적어 둔 그대로)'],
    '.modal-card .cmp-card-wrap.cur .cmp-card': ['—같은 자리(1812 쉼표 목록의 둘째 선택자)'],
    '.cmp-card-wrap.new .cmp-card': ['—같은 까닭(1815) — 새 장비 카드도 판·테 없이 회색 하부 패널(`.cmp-lower`)이 판을 쥔다'],
    '.modal-card .cmp-card-wrap.new .cmp-card': ['—같은 자리(1815 쉼표 목록의 둘째 선택자)'],
    '#chat-modal .modal-card.chat-card': ['—채팅 전체화면엔 테가 없다(3266 `border: none`) · 클론 `ChatScreen.Open` 은 `PopupKit.Card` 를 안 부르고 흰 판을 깐다(22회차 `PopupKit.Card(` 호출 전수 열둘 중 채팅 0) — 같은 몸통의 입력줄 위 키라인·입력칸·둥근 버튼 테(표 행 셋)가 있어 «호출 0» 으로 못 가른다'],
    '#equip-sheet .anvil-btn': ['—`.anvil-btn`(983 · 20회차 행)과 같은 자리 — 정본 모루 버튼은 `#equip-sheet` 안에만 산다(ui.js 1544 갈래) · 그 행의 까닭 그대로(보류 카드·배지 테가 같은 `AnvilSlot` 몸통)'],
    '.idet-subs .substat-row': ['—옵션 줄엔 테가 없다(3709 `border: none`) · 클론 `ForgeInfoPopup.RenderDetail` 의 `subs` 는 `Rounded(subs, "bg")` 판 한 장 안의 글줄 — 같은 몸통에 머리 타일(ItemTile 틀)이 있어 «호출 0» 으로 못 가른다'],
    '.modal-card.sheet': ['—시트엔 테가 없다(3791 `border: none`) · 클론 시트는 `UiRoot.Sheet` 판(위 키라인 `#equip-sheet|top` 만 · 표 행)이고 `PopupKit.Card` 를 부르는 곳은 팝업 열둘뿐(22회차 grep 전수) — 시트 파일마다 제 안의 다른 테가 있어 «호출 0» 으로 못 가른다'],
    '.profile-tabs button': ['—버튼 자체엔 테가 없다(3077 `border: none`) · 클론 `ProfilePopup.Tab` 은 `UiKit.Panel` 면 한 장(`.on` 은 글자 링) — 왼쪽 ol3 는 아래 행'],
    '.profile-tabs button|left': ['—띠 고리(`.profile-tabs` Rounded line pp_line · 표 행)가 두 면 사이로 비치는 꼴이 곧 칸 사이 선이다(Tab: `x + Line3` · 폭 `w − 1.5·Line3`) — `UiKit.Panel` 이라 자가 못 읽는다. ⚠ 22회차 셈: 첫 칸 왼쪽 고리 = Line3 · 두 칸 사이 틈 = **1.5·Line3**(정본 ol3) · 오른쪽 끝 고리 = **.5·Line3** — 이 비대칭은 이 축의 어긋남 후보(ProfilePopup 산 lock 없음 · 다음 회차 · 둘째 칸을 `x + Line3·.5` 에 두면 셋 다 Line3)'],
    '.profile-tabs button:first-child|left': ['—첫 칸의 왼쪽은 띠 제 테(같은 검정 고리)라 «없음»(3080) 과 «있음» 이 화면에서 같다 — 위 행과 같은 몸통'],
    '.league-season-bar': ['—테 없음(2307 · 근흑 판 · 정본 주석 «테두리 없는 근흑 #030405 솔리드») · 클론 `RenderBoard` 의 `season-bar` 는 `Rounded(brt, "bg")` 채움 한 장(고리 짝 아님) — 같은 몸통의 `.league-foot|top` 키라인 때문에 «호출 0» 으로 못 가른다'],
    '.league-score': ['—테 없음(2346 · 정본 주석 «테두리 없음») · 클론 `Row` 의 `score` 는 `Rounded(score, "bg")` 채움 한 장 — 같은 몸통의 행 면(`.league-row` Outlined)이 있어 «호출 0» 으로 못 가른다'],
    '.league-challenge-row': ['—테 없음(2608 · #cacaca 판 · 정본 주석 «키라인 없이 흰 카드에서 바로 필로 전이») · 클론 `RenderChallenge` 의 `row` 는 `Rounded(row, "bg")` 채움 한 장 — 같은 몸통의 아바타 고리(Avatar ol2)·[도전](Btn ol3)이 있어 «호출 0» 으로 못 가른다'],
    '.passive-banner': ['—테 없음(4000) · 클론 `SkillPanel.Render` 의 배너는 `PetSkillKit.Fill` 채움 한 장 — 같은 몸통의 오브·게이지 테가 있어 «호출 0» 으로 못 가른다'],
    '.sk-cell': ['—칸 버튼 껍데기엔 테가 없다(4032 · 5169~5178 플레이어 정보의 같은 이름도) · 클론 격자 칸의 테는 안의 오브(`.sk-orb` 표 행) 몫 — 같은 몸통이라 «호출 0» 으로 못 가른다'],
    '.rates-i': ['—테 없음(4646 · 검정 원판) · 클론 `SkillRatesPopup.Render` 의 `rates-i` 는 `PetSkillKit.Disc` 한 장 — 같은 몸통의 등급 막대(Framed) 때문에 «호출 0» 으로 못 가른다'],
    '.info-dot': ['—테 없음(5220 · 검정 원판) · 클론 `SkillPanel.SummonInfo` 의 `info-dot` 은 `PetSkillKit.Disc` 한 장 — 같은 몸통의 `summon-gauge`(ol2 · 표 행)가 있어 «호출 0» 으로 못 가른다(펫·스킬·탈것 셋이 한 공장)'],
    '.sr-orb': ['—테 없음(6496) · 클론 `BuildCell` 의 `sr-orb` 는 `PetSkillKit.Disc` 한 장(T331 43회차: 아래 그늘은 구운 타원) — 같은 몸통의 `sr-qty`·`sr-dup`·`sr-new` 테가 있어 «호출 0» 으로 못 가른다'],
    '.sr-chip': ['—테 없음(7126 `border: none` · 첫 그림자 겹 `0 0 0 1px` 이 1px 고리 노릇 · T331 32회차) · 클론 `BuildFoot` 의 `sr-chip-*` 상자는 채움 + 표 그림자 — 같은 몸통의 `sr-hint`·`sr-ok` 테가 있어 «호출 0» 으로 못 가른다'],
    '.af-dd-list button': ['—테 없음(4798 `background: none; border: none` · 흰 굵은 글자 · `.on` 만 강조) ↔ **결함**: 클론 `Render` 의 드롭다운 항목은 `PopupKit.Btn`(파랑/회색 면 + ol3 테 + 아래턱)이라 정본에 없는 테와 면을 진다 — 공용 Btn 은 도우미 목록 밖이라 자가 못 가른다 · ForgeAutoPopup T178·T415 lock 뒤 «글자만(면·테 없음) + 선택 항목 강조» 로(면은 T377 축과 같이)'],
    '.dg-banner': ['—덮인다: 정본 마크업의 유일한 자리(ui.js 4528)는 던전 시트 안이라 2001 `.modal-card.sheet .dg-banner`(표 행)가 이긴다 — 1960 의 ol1 #30363d 는 실물에 안 선다'],
    '.petd-tile': ['—덮인다: 정본 마크업의 `.petd-tile` 둘(ui.js 4019 펫 · 4074 알)은 모두 `.petd-wrap`(4014) 안이라 5464 `.petd-wrap .petd-tile`(특이도 0,2,0 · 1px color-mix)이 5538 의 ol3 pp-line 을 이긴다 — 아래 행이 임자'],
    '.petd-wrap .petd-tile': ['—**1px**(직접값 · = 1 CSS px · ol1) color-mix: 23회차가 `PetPanel.TileFace` petd 갈래에 `line1_px` 를 줬다(격자 얼굴은 Line3 그대로 · 색은 `petd_line` 섞기) — 직접값이라 자의 단 대조 밖 · PlayMode `BoxBorderSitesTests` 가 상세 타일 폭을 잰다'],
    '.prob-chip': ['—클론에 그 자리가 없다: 정본은 두 자리(탈것 시트 확률 띠 ui.js 5637 · 디버그 판 열쇠 수 5976)에 쓴다 — 클론 탈것 시트는 확률을 `SkillRatesPopup` 색 막대로만 보이고(띠 0) 디버그 판은 T169 갈래 · T377 도 같은 두 자리를 «자리 없음 KNOWN» 으로 뒀다(면 축) · T415 19회차가 반지름 축에서 같은 자리를 잡고 있다(그 결론을 따른다)'],
    # ⓒ 구운 판·판 겹 — 테가 도우미 호출이 아닌 꼴로 서 있어 자가 못 읽는다(값은 표에 · 시간 축 값은 그 자가 잰다)
    '.cmp-ribbon': ['—구운 다각형: `RibbonArt.Build`(ForgeUi.Ribbon)가 면을 `--ol2` 의 절반만큼 부풀린 사본으로 테를 굽는다(RibbonArt 68·115 · 폭은 `KeylineUi.CssPx` 로 ol2 환산) — `RibbonArtTests` 가 잰다'],
    '.cmp-ribbon|bottom': ['—아랫변 없음(1804 `border-bottom: none`) · RibbonArt 115 «아랫변은 안 그린다»(y 를 BakeH 로 자른다) — 같은 구운 판'],
    '.pass-price': ['—테 속성은 0 이지만 검정 바깥 층(`background: --pp-line`) + `inset: 3px` 의 ::before 주황 면(2751~2755 · 정본 주석 «테두리 속성은 clip-path 에 잘려 쓸 수 없음»)이 곧 3px(≈ ol3) 테다 · 클론 `PassPopup.Render` 의 ClipShape 두 겹(`price-line` pp_line · `price-face` Line3 안쪽 · 면 `pass_price` #ff9a00)이 그대로 — 자가 못 읽는다'],
    '.pass-header-row|top': ['—판 겹으로 낸 테: `UiKit.Panel(header, "line", pp_line)` 위에 free/premium 판을 위·아래 Line3 안쪽에(PassPopup.Render 124~131) = 정본 2760 ol3 — `Panel` 이라 자가 못 읽는다(22회차 눈으로 읽음 · PassPopup T415 lock)'],
    '.pass-header-row|bottom': ['—같은 겹의 아래 Line3(2760)'],
    '.pass-header-row span:first-child|right': ['—같은 겹: free 판 오른쪽 `.5·Line3` + premium 판 왼쪽 `.5·Line3` = 가운데 세로선 Line3(2764 ol3)'],
    '.tab-x-mark': ['—고리 Circle(`ring` pp_line) + 면 Circle(`face`)을 `offsetMin/Max = line3`(`line3_px`) 로 깎는 꼴(TabBar.Build) = ol3 — 짝 읽기는 `PopupKit.Inset` 만 알아 자가 못 읽는다(22회차 눈으로 읽음)'],
    '.sr-floor::before': ['—광 고리(T334 갈래): 정본 5808 의 .16rem 테는 방사 그라디언트 두 겹과 한 몸인 소환진 가장자리 빛이다 · 클론 소환진은 `PetSkillKit.Disc`(floor_fill) + 구운 눈금 띠(`SummonFx.BakeFloorTicks`) — 정적 테 축이 아니라 이 자로는 안 잰다(`SummonFxTests`)'],
    '.sr-canopy::before': ['—같은 갈래: 5857 의 .08rem 테는 캐노피 아치의 빛 가장자리 · 클론 `SummonFx` 가 아치·빛발·스필을 굽는다(T179·T419)'],
    '.sr-shock': ['—구운 고리(T334 19회차): 정본 6121 의 .34rem 은 시작값이고 키프레임 `srshock` 이 굵기·번짐을 시간에 따라 바꾼다 · 클론 `ShockPlate` 가 단계마다 `SummonFx.BakeTierRing`(띠 폭 = 규칙 `sk.MainAt` 의 br)으로 굽는다 — 시간 축 값이라 정적 테 자로 안 잰다'],
    '.sr-tierflash': ['—같은 꼴(T334): 6417 .34rem + `srtierflash` 키프레임 · 클론 `BakeTierRing`(`tb.FlashAt` 의 kb)'],
    '.sr-idle i': ['—같은 꼴(T334 18회차): 6939 .12rem + `sridlering` · 클론 `BakeTierRing`(idle_ring)'],
    '.rw-ring': ['—구운 고리(`RewardBurst.RingSprite`): 테 두께 = 표 `RewardBurstUi.json` `ring_border_rem` .34 / `ring_rem` 3(정본 7478 .34rem / 3rem 그대로) · 키프레임 `rwRing` 이 배율·알파를 바꾼다 — 정적 테 자로 안 잰다(값은 표에 있다)'],
    '.rw-anchor': ['—`AnchorFx`: 고리 Circle(`border` · `anchor_border` 색 = 정본 #ffd54f) + 안쪽 Circle(`bg`)을 `offsetMin/Max = border`(표 `anchor_border_rem` .16 = 정본 7539 .16rem)로 깎는 꼴 — 이름이 line/face 꼴이 아니고 Inset 도 안 써 자가 못 읽는다(값은 표에 있다)'],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — 닫을 때마다 지운다 ───────────────────────────────
KNOWN = {
    # 열쇠는 «선택자[|변] → 자리» 쌍이다(2회차·3회차는 자리만 열쇠로 써서, 같은 도우미가 다른 선택자에서 맞으면 «이제 있다» 로 잘못 알렸다 · 4회차 수리). 자리만 적은 옛 열쇠도 읽는다(빈자리 판정에만 · «이제 있다» 알림은 쌍 열쇠만).
    # T365 22회차 — 미정 71 을 짝짓다 나온 결함 여섯(전부 남의 산 lock 안 · 자리·정본 줄·바꿀 키를 적어 둔다 · 풀리는 파일부터 누구든)
    '.idet-icon → Ui/ForgeInfoPopup.cs@RenderDetail': 'T365 22회차 — 정본 3685 `.idet-icon { border: var(--ol2) solid #d5d5d5; background: #e9e9e9 }` ↔ 클론 상세 머리 타일은 ItemTile(ol3 · 시대색 면·고리) — ForgeInfoPopup T178·T415 lock · 부르는 쪽에서 `frame/line`·`frame/face` 를 표 키(밝은 회색 둘)로 덮고 폭은 Line 으로',
    '.af-check → Ui/ForgeAutoPopup.cs#box': 'T365 22회차 — 정본 4726 `.af-check { border: var(--ol3) }` ↔ 계속하기 체크(Render 159)·옵션 줄 체크(SubRow 234) 둘 다 `ForgeUi.Tile(…, "box", …, PopupKit.Line)` = ol1 — ForgeAutoPopup T178 lock · 마지막 인자 `Line` → `Line3`(안의 ✓ 여백도 그만큼 · 시대 막대의 셋째 체크는 23회차가 ForgeUi.AgeBar 에서 닫았다)',
    '.af-bottom|top → Ui/ForgeAutoPopup.cs#af-bottom-line': 'T365 22회차 — 정본 4777 `.af-bottom { border-top: var(--ol3) solid var(--pp-line) }` ↔ 클론 `Render` 의 `af-bottom` 상자엔 `UiKit.Line` 호출이 0 — **선 자체가 없다** · ForgeAutoPopup T178·T415 lock · 풀리면 `UiKit.Line(bottom, "af-bottom-line", "pp_line", PopupKit.Line3, true)` 한 줄(이 이름이라야 이 행이 초록이 된다)',
    '.af-toggle → Ui/Popups.cs@Toggle': 'T365 22회차 · 26회차 보탬: 부르는 쪽 ForgeAutoPopup(T178 lock)이 `settings_toggle_h`·`tw − Line2·2`·`tgH − Line2·4` 로 그라디언트 판을 굽어(T178 22회차) 도우미만 정본 치수로 바꾸면 그 판이 어긋난다 — 두 파일을 한 회차에 · 정본 4761 `.af-toggle { border: var(--ol3) }` ↔ `Popups.Toggle` 은 설정 토글(3108 ol2) 값 한 벌 — Popups.cs 는 산 lock 밖이지만 폭만 고치면 반쪽이다: 정본 4765 손잡이(1.631rem)가 트랙(1.269rem)보다 **커서 밖으로 나오고** `left: calc(-.846rem - var(--ol3))` 로 걸린다 · 클론 손잡이는 트랙 안(`k = h − Line2·4`) — `ToggleSlide`·`PressFx`·자들이 `knob` 을 잡고 있어 제 회차로(af 갈래는 이름 `af-toggle` 로 가른다 · T331 43회차 그늘 갈래와 같은 길)',
    '.af-toggle .knob → Ui/Popups.cs@Toggle': 'T365 22회차 — 정본 4765 `.af-toggle .knob { border: var(--ol3) }` ↔ 손잡이 `Inset(knobFace, Line)` = ol1 — 위 행과 한 회차에',
    # T365 14회차 — 오프라인 요율 원판 둘은 T417 1회차(`42083bc`)가 정본 7268 대로 색 원·테를 걷어 이제 «정본대로 테 없음» ok 다 → KNOWN 에서 걷었다(경고 줄 2 → 0 · 표 자리 초록 84 그대로).
}

HELPERS = ('PopupKit.Outlined', 'RadiusUi.Outlined', 'ForgeUi.Tile', 'UiKit.Line', 'PetSkillKit.Framed', 'PetSkillKit.Orb', 'PetSkillKit.Gauge', 'DungeonPopups.Bordered', 'DungeonPopups.BorderedCircle', 'UiKit.Rounded', 'UiKit.Circle', 'Bordered', 'BorderedCircle', 'Tile', 'Framed', 'SurfaceArt.DashedFrame')   # 맨 이름 둘은 DungeonPopups 제 안의 호출(10회차 · CurPill) · 맨 `Tile` 은 ForgeUi 제 안의 호출(22회차 · ItemTile·AgeBar) · 맨 `Framed` 는 PetSkillKit 제 안의 호출(22회차 · PaperButton 껍질·Gauge) · `PetSkillKit.Gauge` 는 Framed 한 겹(22회차 · 폭 인자 8)
# 도우미별 폭 인자 자리(0부터 · 이름 인자는 1) — Rounded 는 짝(안쪽 면의 «r - 폭»)에서 읽는다
WIDTH_ARG = {'PopupKit.Outlined': 4, 'RadiusUi.Outlined': 4, 'ForgeUi.Tile': 5, 'Tile': 5, 'UiKit.Line': 3, 'PetSkillKit.Framed': 4, 'PetSkillKit.Orb': 3, 'PetSkillKit.Gauge': 8, 'DungeonPopups.Bordered': 4, 'DungeonPopups.BorderedCircle': 3, 'Bordered': 4, 'BorderedCircle': 3, 'Framed': 4, 'SurfaceArt.DashedFrame': 6}   # T472 — 점선 테(parent, name, colorKey, w, h, radius, **line**, dash, gap)
CALL_RE = re.compile(r'\b(' + '|'.join(re.escape(h) for h in HELPERS) + r')\s*\(')
TIER_PATTERNS = [
    ('ol15', re.compile(r'line15_px')),   # T365 26회차 — 정본 --ol15(1.5 CSS px) 한 자리(3249 채팅 배지) · `line1_px`·`\bline\b` 보다 먼저(둘 다 이 이름엔 안 맞지만 순서로 못 박는다)
    ('ol4', re.compile(r'line4_px')),
    ('ol3', re.compile(r'\bLine3\b|line3_px|\bline3\b')),
    ('ol2', re.compile(r'\bLine2\b|line2_px|\bline2\b')),
    ('ol1', re.compile(r'\bLine\b|line_px|line1_px|\bline\b')),
]
PARAM_RE = re.compile(r'^\s*(?:line|linePx|lineW|borderPx|width|w)\s*$')
DECL = re.compile(r'(?<![-\w])border(-top|-bottom|-left|-right)?\s*:\s*([^;}]+)')
# T469 — 같은 규칙 블록 안에서 단축 **뒤**에 오는 `border-width:`(변 있는 꼴 포함)는 단축의 굵기를 덮어쓴다(CSS 캐스케이드 · 정본 3284 ol2 → 3289 ol1).
#   단축보다 **앞**에 오면 단축이 다시 굵기를 정하므로 무시한다. 단축 없이 `border-width` 만 있는 블록(장식 삼각형 2908 · 키프레임 6131)은 테 선언이 아니라 안 센다.
WIDTH_DECL = re.compile(r'(?<![-\w])border(-top|-bottom|-left|-right)?-width\s*:\s*([^;}]+)')
SIDES = ('top', 'bottom', 'left', 'right')
# T472 — 테 스타일: `border-style:` 과 단축 안의 낱말. 변별 스타일(`border-top-style`)은 정본에 없어 안 읽는다.
STYLE_DECL = re.compile(r'(?<![-\w])border-style\s*:\s*([^;}]+)')
STYLE_WORDS = ('dashed', 'dotted', 'double', 'solid', 'none')
DASH_CALL = re.compile(r'SurfaceArt\.DashedFrame\s*\(')
# 정본이 점선으로 적은 선택자 → 점선 호출이 있어야 하는 클론 자리(꼴은 TABLE 과 같다 · «—» 는 대조 안 함)
DASHED = {
    '.cmp-card.empty': ['Ui/ForgeUi.cs@ItemCard'],   # 1830 — 제작 비교·장비 상세의 빈 슬롯 카드(면 없음 + 점선 ol3 · T472 1회차)
    '.hatch-slot': ['—죽음: 정본이 `hatch-slot` 을 한 번도 안 붙인다(ui.js 3926 `.hatch-cell`) — 1650 의 점선 ol1 --rc 도 그려지지 않는 잔재(T365 21회차)'],
}


def _override_width(val, width):
    """단축 값의 첫 토막(굵기)을 `border-width` 값으로 바꾼다 — 나머지(style · 색)는 그대로."""
    rest = val.strip().split(None, 1)
    return width.strip() + (' ' + rest[1] if len(rest) > 1 else '')


def _blank_comments(css):
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def width_tier(value):
    """값 → 폭 단: 'ol1'..'ol4' · 'ol15' · 'cellb' · 'none' · 'px:<수단위>'(직접값)."""
    v = value.strip()
    m = re.search(r'var\(--(ol\d+|cellb)\b', v)
    if m:
        return m.group(1)
    if re.match(r'^(none|0)(\s|$)', v) or v == '0':
        return 'none'
    m = re.match(r'^([0-9.]+(?:px|rem|em))\b', v)
    if m:
        return 'px:' + m.group(1)
    return 'px:' + v.split()[0]


def color_of(value):
    """값 → 색 표기(정본 낱말 그대로 · 없으면 '')."""
    v = value.strip()
    if re.search(r'color-mix\(', v):
        return 'color-mix'
    m = re.search(r'var\(--(rc|pp-line|c)\b[^)]*\)', v)
    if m:
        return '--' + m.group(1)
    m = re.search(r'(#[0-9a-fA-F]{3,8}|rgba?\([^)]*\))', v)
    return m.group(1) if m else ''


def _style_word(val):
    for tok in val.replace('!important', ' ').split():
        if tok in STYLE_WORDS:
            return tok
    return None


def parse_styles(css_text):
    """{선택자: (줄, 스타일)} — `border-style:` 과 변 없는 단축 `border:` 안의 스타일 낱말 · 정본 순서로 뒤 규칙이 앞을 덮는다(T472)."""
    css = _blank_comments(css_text)
    rows = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        body = m.group(2)
        sels = [' '.join(s.split()) for s in m.group(1).split(',')]
        for d in DECL.finditer(body):
            if d.group(1):
                continue
            w = _style_word(d.group(2))
            if w:
                rows.append((m.start(2) + d.start(), sels, w))
        for d in STYLE_DECL.finditer(body):
            w = _style_word(d.group(1))
            if w:
                rows.append((m.start(2) + d.start(), sels, w))
    out = {}
    for pos, sels, w in sorted(rows, key=lambda r: r[0]):
        line = css.count('\n', 0, pos) + 1
        for s in sels:
            if s:
                out[s] = (line, w)
    return out


def parse_rules(css_text):
    """[(줄, 선택자, 변, 원값)] — 정본 순서 · 쉼표 목록은 선택자마다."""
    css = _blank_comments(css_text)
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        body = m.group(2)
        decls = list(DECL.finditer(body))
        if not decls:
            continue
        sels = [' '.join(s.split()) for s in m.group(1).split(',')]
        sels = [s for s in sels if s]
        # T469 — 단축 뒤에 온 `border-width` 가 그 블록 안 단축의 굵기를 덮는다(변이 없는 width 는 모든 단축에 · 변이 있는 width 는 같은 변의 단축에만)
        widths = list(WIDTH_DECL.finditer(body))
        for d in decls:
            line = css.count('\n', 0, m.start(2) + d.start()) + 1
            side = (d.group(1) or '').lstrip('-')
            val = d.group(2).strip()
            for wd in widths:
                if wd.start() < d.start():
                    continue
                wside = (wd.group(1) or '').lstrip('-')
                if wside == '' or wside == side:
                    val = _override_width(val, wd.group(2))
            for s in sels:
                out.append((line, s, side, val))
    return out


def collapse(rows):
    """(선택자, 변) → 마지막 선언(뒤 규칙이 앞을 덮는다). 돌려주는 값: [(선택자, 변, 줄, 원값)] 정본 순 · 덮인 수."""
    final = {}
    order = []
    overridden = 0
    for line, sel, side, val in rows:
        k = (sel, side)
        if k in final:
            overridden += 1
        else:
            order.append(k)
        final[k] = (line, val)
    return [(sel, side, final[(sel, side)][0], final[(sel, side)][1]) for sel, side in order], overridden


def table_key(sel, side):
    return sel + ('|' + side if side else '')


# ── 클론 ─────────────────────────────────────────────────────────────
def _read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def _brace_body(src, start):
    """`{` 위치에서 짝 `}` 까지 — 문자열·문자·주석 속 중괄호는 세지 않는다(«{0}» 같은 포맷 문자열이 본문을 자르던 것 · T365 2회차)."""
    depth, i, n = 0, start, len(src)
    while i < n:
        c = src[i]
        if c == '"':
            verbatim = i > 0 and src[i - 1] == '@'
            i += 1
            while i < n:
                if src[i] == '\\' and not verbatim:
                    i += 2; continue
                if src[i] == '"':
                    if verbatim and i + 1 < n and src[i + 1] == '"':
                        i += 2; continue
                    break
                i += 1
        elif c == "'":
            i += 1
            while i < n and src[i] != "'":
                i += 2 if src[i] == '\\' else 1
        elif c == '/' and i + 1 < n and src[i + 1] == '/':
            while i < n and src[i] != '\n':
                i += 1
            continue
        elif c == '/' and i + 1 < n and src[i + 1] == '*':
            j = src.find('*/', i + 2)
            i = n if j < 0 else j + 2
            continue
        elif c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                return src[start:i + 1]
        i += 1
    return src[start:]


def _method_body(src, name):
    """메서드 본문(들) — 줄머리의 «(한정자) 반환형 이름(» 만 잡고(호출은 안 잡는다), 오버로드가 여럿이면 본문을 전부 잇는다. 없으면 None."""
    bodies = []
    for m in re.finditer(r'^[ \t]*(?:(?:public|private|internal|protected|static|override|readonly)\s+)*[\w<>\[\],.]+\s+' + re.escape(name) + r'\s*\(', src, flags=re.M):
        _, close = _args(src, m.end() - 1)
        head = src[close:close + 200]
        arrow = re.match(r'\s*=>', head)
        if arrow:
            end = src.find(';', close)
            bodies.append(src[close:end + 1 if end >= 0 else None])
            continue
        b = src.find('{', close)
        if b < 0:
            continue
        bodies.append(_brace_body(src, b))
    return '\n'.join(bodies) if bodies else None


def _args(src, open_paren):
    """여는 괄호 뒤의 인자 목록(괄호 깊이를 센다) — (인자들, 닫는 괄호 위치)."""
    depth, i, start, args = 0, open_paren, open_paren + 1, []
    while i < len(src):
        c = src[i]
        if c == '(':
            depth += 1
        elif c == ')':
            depth -= 1
            if depth == 0:
                args.append(src[start:i].strip()); return args, i
        elif c == ',' and depth == 1:
            args.append(src[start:i].strip()); start = i + 1
        i += 1
    return args, len(src)


LINE_VAR = re.compile(r'\bfloat\s+(\w+)\s*=\s*(?:UiKit|PopupKit|PetSkillStyle)\.L\s*\(\s*"(line\d?_px)"\s*\)')


def line_vars(body):
    """`float line = UiKit.L("line2_px");` 꼴을 걷는다 — {변수: 표키}.

    **왜 필요한가(15회차)**: 단 무늬의 마지막 줄이 `\bline\b` 라 **`line` 이라는 이름의 변수**면
    그 안에 무엇이 들었든 `ol1` 로 읽혔다. 정본 ol2 자리를 ol2 로 고쳐도 자는 여전히 «ol1» 이라 보고,
    반대로 ol1 자리를 ol2 로 잘못 바꿔도 안 운다 — 변수부터 풀어야 단을 제대로 읽는다."""
    return {m.group(1): m.group(2) for m in LINE_VAR.finditer(body or '')}


def tier_of_expr(expr, varmap=None):
    """폭 식 → 단('ol1'..'ol4') · 'param'(도우미 매개변수 — 판정 안 함) · None(모름)."""
    if expr is None:
        return None
    if varmap:
        # 변수를 먼저 그 표키로 바꾼다(길이 긴 이름부터 — `line` 이 `line2` 를 먹지 않게)
        for var in sorted(varmap, key=len, reverse=True):
            expr = re.sub(r'\b' + re.escape(var) + r'\b', varmap[var], expr)
    if PARAM_RE.match(expr):
        return 'param'
    for tier, rx in TIER_PATTERNS:
        if rx.search(expr):
            return tier
    return None


# 15회차 — UGUI 엔 border 가 없어 «고리» 는 셋 중 한 꼴로 그린다: ⑴ 도우미(Outlined·Framed…) ⑵ 고리+면+Inset ⑶ **면을 폭×2 만큼 깎기**.
#   ⑶ 은 크기를 프레임마다 다시 주는 자리(웨이브 핍)가 쓴다 — 고리 스프라이트는 한 번 만들고 두께는 면 크기로 낸다.
RING_SHRINK = re.compile(r'(\w+)\s*\.\s*rectTransform\s*\.\s*sizeDelta\s*=\s*new\s+Vector2\s*\(([^;]*?)\)\s*;')
#   ⚠ **면 꼴 이름에만** 건다 — `Inset(img.rectTransform, Line * 2f)`(초상 여백)처럼 테가 아닌 인셋까지 걷으면
#      멀쩡한 자리가 «단 어긋남» 으로 운다(15회차에 실제로 그렇게 여섯이 울었다). 고리+면 짝을 보는 다른 갈래와 같은 규율이다.
BARE_INSET = re.compile(r'(?<![.\w])Inset\s*\(\s*(face|bg|core|fill|\w*[Ff]ace|\w*[Ff]ill)\s*\.\s*rectTransform\s*,\s*([^;)]+)\)')


def border_calls(src):
    """[(이름, 단, 시작)] — 파일(또는 메서드 본문) 안의 테 호출 전부. Rounded 는 안쪽 면 «r - 폭» 짝이 따라올 때만."""
    out = []
    vars_ = line_vars(src)
    for m in CALL_RE.finditer(src):
        helper = m.group(1)
        args, close = _args(src, m.end() - 1)
        if len(args) < 2 or not args[1].startswith('"'):
            continue
        name = args[1].strip('"')
        if helper == 'UiKit.Circle':
            # 12회차 — 둥근 테는 «고리 Circle + 면 Circle + PopupKit.Inset(면, 폭)» 짝으로 그린다(UiKit.Rounded 짝과 같은 뜻).
            #   바깥 이름이 고리 꼴(line/ring/outline)이 아니거나 뒤에 면+Inset 이 안 따라오면 그냥 원판이다(테가 아니다).
            if name not in ('line', 'ring', 'outline'):
                continue
            look = src[close:close + 420]
            n = re.search(r'UiKit\.Circle\s*\(', look)
            if not n:
                continue
            inner, iclose = _args(src, close + n.end() - 1)
            if len(inner) < 2 or not inner[1].startswith('"'):
                continue
            iname = inner[1].strip('"')
            if not (iname in ('face', 'bg', 'fill') or iname.endswith('-face') or iname.endswith('fill')):
                continue
            # 폭은 그 면에 거는 Inset 에서 읽는다 — 원문에서(창 밖에서 끝나는 긴 식도 그대로 · 6회차 수리와 같은 까닭)
            ins = re.search(r'PopupKit\.Inset\s*\(', src[iclose:iclose + 420])
            if not ins:
                continue
            iargs, _ = _args(src, iclose + ins.end() - 1)
            out.append((name, tier_of_expr(iargs[1], vars_) if len(iargs) > 1 else None, m.start()))
            continue
        if helper == 'UiKit.Rounded':
            # 짝 판정: 바깥 고리(이름 line/outline/ring/avatar/…) 뒤 420자 안에 안쪽 면 Rounded 가 따라와야 테다.
            # 안쪽 반지름이 «r - 폭» 이면 그 폭이 단 · 반지름을 따로 준 «입술(lip)» 꼴이면 단은 못 읽는다(None).
            look = src[close:close + 420]
            n = re.search(r'UiKit\.Rounded\s*\(', look)
            if not n:
                continue
            # 인수는 창이 아니라 원문에서 읽는다 — 안쪽 호출이 창 안에서 시작해 창 밖에서 끝나면(긴 «r - 폭» 식) 창에서 읽은 인수가 잘린다(6회차 수리)
            inner, _ = _args(src, close + n.end() - 1)
            if len(inner) < 4 or not inner[1].startswith('"'):
                continue
            iname = inner[1].strip('"')
            inset = '-' in inner[3]
            # 안쪽 면: 이름이 면 꼴(face/bg/lip/ground/fill·…-fill)이거나, 반지름이 «r - 폭» 꼴이면 짝이다(upg-fill 처럼 이름이 다른 채움도 잡는다)
            if not (inset or iname in ('face', 'bg', 'lip', 'ground', 'fill') or iname.endswith('-fill') or iname.endswith('fill')):
                continue
            tail = inner[3].split('-', 1)[1] if inset else None
            out.append((name, tier_of_expr(tail, vars_), m.start()))
        else:
            idx = WIDTH_ARG[helper]
            out.append((name, tier_of_expr(args[idx], vars_) if len(args) > idx else None, m.start()))
    # ⑶ «면을 폭×2 만큼 깎아» 두께를 내는 고리 — `p.Fill.rectTransform.sizeDelta = new Vector2(size - line * 2f, …)`
    for m in RING_SHRINK.finditer(src):
        inner = m.group(2)
        if '-' not in inner:
            continue
        t = tier_of_expr(inner.split('-', 1)[1], vars_)
        if t:
            out.append((m.group(1), t, m.start()))
    # 같은 뜻의 «그 파일 제 Inset»(`PopupKit.` 이 안 붙은 갈래) — 트랙처럼 판 둘을 겹쳐 두께를 내는 자리
    for m in BARE_INSET.finditer(src):
        t = tier_of_expr(m.group(2), vars_)
        if t:
            out.append((m.group(1), t, m.start()))
    return out


def css_tier(value):
    t = width_tier(value)
    return 'ol3' if t == 'cellb' else t


def check_target(game_dir, target, want_tier=None):
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 테 호출 없음) | 'absent'(요소·메서드·파일 없음) | 'tier'(단이 정본과 다름) | 'extra'(정본은 끄는데 클론이 그린다 · 12회차) | 'skip'.

    ⚠ 정본 단이 `none` 인 자리는 **없어야 맞다** — 그래서 호출이 없으면 `ok`, 있으면 `extra` 다(11회차까지는 호출이 있으면
    그냥 `ok` 라 «군더더기 테» 를 한 건도 못 봤다 · 오히려 KNOWN 에 적어 두면 «이제 테가 있다 → 지워라» 로 거꾸로 울었다).
    `none` 자리는 이름(`#`)보다 **메서드(`@`)나 파일** 로 적는다 — 이름으로 적으면 «그 이름이 아예 없다» 와 «테가 없다» 를 못 가른다."""
    if target.startswith('—'):
        return 'skip', target[1:]
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        calls = border_calls(body)
        where = '메서드 ' + tail + '( 본문'
    elif sep == '#':
        calls = [c for c in border_calls(src) if c[0] == tail]
        where = '"%s" 자리' % tail
    else:
        calls = border_calls(src)
        where = '파일 전체'
    if not calls:
        if want_tier == 'none':
            return 'ok', where + ' · 정본대로 테 없음'
        return ('absent' if sep == '#' else 'missing'), where + '에 테 호출이 없다'
    tiers = {t for _, t, _ in calls if t}
    if want_tier == 'none':
        return 'extra', '%s: 정본은 테를 끄는데 클론이 그린다(단 %s)' % (where, '/'.join(sorted(tiers)) if tiers else '못 읽음')
    if want_tier and want_tier.startswith('ol') and tiers and 'param' not in tiers and want_tier not in tiers:
        return 'tier', '%s: 정본 %s ↔ 클론 %s' % (where, want_tier, '/'.join(sorted(tiers)))
    return 'ok', where + ' · 단 ' + ('/'.join(sorted(tiers)) if tiers else '못 읽음')


def check_dashed(game_dir, target):
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 점선 호출 없음) | 'absent' | 'skip'. 점선 호출 = `SurfaceArt.DashedFrame(` (T472)."""
    if target.startswith('—'):
        return 'skip', target[1:]
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        where = '메서드 ' + tail + '( 본문'
    elif sep == '#':
        body = ''
        for m in DASH_CALL.finditer(src):
            args, _ = _args(src, m.end() - 1)
            if len(args) > 1 and args[1].strip('"') == tail:
                body = m.group(0)
                break
        where = '"%s" 자리' % tail
    else:
        body, where = src, '파일 전체'
    if DASH_CALL.search(body):
        return 'ok', where + ' · 점선 호출 있음'
    return 'missing', where + '에 점선 호출(SurfaceArt.DashedFrame)이 없다'


# ── 대조 ─────────────────────────────────────────────────────────────
def run(css_text, game_dir, table, known, out=print, list_pending=False, dashed=None):
    rows = parse_rules(css_text)
    final, overridden = collapse(rows)
    tiers = {}
    for sel, side, line, val in final:
        t = width_tier(val)
        t = 'direct' if t.startswith('px:') else t
        tiers[t] = tiers.get(t, 0) + 1
    problems = 0
    n_ok = n_known = n_skip = 0
    known_now_ok = []
    css_by_key = {table_key(sel, side): val for sel, side, _, val in final}
    keys_in_css = set(css_by_key)
    pending = [(sel, side, line, val) for sel, side, line, val in final if table_key(sel, side) not in table]
    for key, targets in table.items():
        if key not in keys_in_css:
            out('  ✗ 표의 선택자가 정본에 없다: %s' % key)
            problems += 1
            continue
        for t in targets:
            state, why = check_target(game_dir, t, css_tier(css_by_key[key]))
            pair = key + ' → ' + t
            if state == 'ok':
                n_ok += 1
                if pair in known:
                    known_now_ok.append(pair)
            elif state == 'skip':
                n_skip += 1
            elif pair in known or t in known:
                n_known += 1
            else:
                label = {'missing': '테 없음', 'absent': '자리 없음', 'tier': '단 어긋남', 'extra': '군더더기 테'}[state]
                out('  ✗ %s → %s: %s(%s)' % (key, t, label, why))
                problems += 1
    for t in known_now_ok:
        out('  · KNOWN 인데 이제 테가 있다: %s → KNOWN 에서 지워라' % t)
    # T472 — 테 스타일: 점선 표의 선택자는 정본에서 dashed 여야 하고 그 자리엔 점선 호출이 있어야 한다
    styles = parse_styles(css_text)
    dashed = dashed or {}
    n_dash = 0
    for sel, targets in dashed.items():
        st = styles.get(sel)
        if not st or st[1] != 'dashed':
            out('  ✗ 점선 표의 선택자가 정본에서 dashed 가 아니다: %s(%s)' % (sel, st[1] if st else '선언 없음'))
            problems += 1
            continue
        for t in targets:
            state, why = check_dashed(game_dir, t)
            if state in ('ok', 'skip'):
                n_dash += 1
            elif (sel + ' → ' + t) in known or t in known:
                n_known += 1
            else:
                out('  ✗ %s → %s: 점선 아님(%s)' % (sel, t, why))
                problems += 1
    dash_pending = sorted((l, s) for s, (l, w) in styles.items() if w == 'dashed' and s not in dashed)
    if list_pending:
        for sel, side, line, val in pending:
            out('· 미정  style.css %5d  %-52s %-6s  %-8s %s' % (line, sel[:52], side or 'all', width_tier(val), color_of(val)))
        for line, sel in dash_pending:
            out('· 점선 미정  style.css %5d  %s' % (line, sel))
    tier_txt = ' · '.join('%s %d' % (k, tiers[k]) for k in ('ol3', 'ol2', 'none', 'ol1', 'cellb', 'ol4', 'ol15', 'direct') if k in tiers)
    out('%s check_box_borders: 정본 테 선언 %d → 선택자×변 %d(덮인 %d) · 단 %s · 표 자리 초록 %d · KNOWN %d · 건너뜀 %d · 미정 %d(--list) · 점선 %d(미정 %d) · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rows), len(final), overridden, tier_txt, n_ok, n_known, n_skip, len(pending), n_dash, len(dash_pending), problems))
    return 1 if problems else 0


def self_test():
    css = """
/* 주석 { border: var(--ol4) solid #000; } */
.a, .b { border: var(--ol1) solid #444c56; }
.a { border: var(--ol3) solid var(--pp-line); }
.c { border-top: var(--ol2) solid #30363d; border-left: 2px solid #000; }
.d { border: none; }
.e { border: var(--cellb) solid color-mix(in srgb, var(--rc, #6b3538) 80%, #000); }
.f { border: .34rem solid rgba(255,255,255,.34); }
.g { border: var(--ol2) solid #000; color: #fff; border-width: var(--ol1); box-shadow: inset 0 -5px 0 #4e0507; }
.h { border-width: var(--ol1); border: var(--ol3) solid #000; }
.i { border-width: .95rem .85rem .95rem 0; border-color: transparent #a86a00 transparent transparent; }
.j { border-style: dashed; opacity: .7; }
"""
    cs = """
namespace X {
    public static class Popups {
        public static void Toast(Transform p) { PopupKit.Outlined(p, "face", "pp_paper", 4f, PopupKit.Line); }
        public static void Bare(Transform p) { }
    }
    class Sheet {
        void Build(Transform p) {
            UiKit.Line(p, "line", "pp_line", PopupKit.Line3, true);
            PetSkillKit.Framed(p, "tile-face", Color.white, 3f, 2f);
        }
    }
}
"""
    rows = parse_rules(css)
    final, overridden = collapse(rows)
    checks = []
    checks.append(('선언 10(쉼표 목록은 선택자마다 · .c 는 변 둘 · 단축 없는 border-width 뿐인 .i 는 안 센다) · 주석 속 선언은 안 센다', len(rows) == 10))
    checks.append(('같은 (선택자, 변)의 뒤 규칙이 앞을 덮는다 → 9 · 덮인 1', len(final) == 9 and overridden == 1))
    tiers = {(s, d): width_tier(v) for s, d, _, v in final}
    checks.append(('폭 단: 덮인 .a 는 ol3 · .b 는 ol1', tiers[('.a', '')] == 'ol3' and tiers[('.b', '')] == 'ol1'))
    checks.append(('변: .c 는 top(ol2) · left(직접값 2px)', tiers[('.c', 'top')] == 'ol2' and tiers[('.c', 'left')] == 'px:2px'))
    checks.append(('none · cellb · 직접값 rem', tiers[('.d', '')] == 'none' and tiers[('.e', '')] == 'cellb' and tiers[('.f', '')] == 'px:.34rem'))
    vals = {(s, d): v for s, d, _, v in final}
    checks.append(('색: --pp-line · #30363d · color-mix · rgba', color_of(vals[('.a', '')]) == '--pp-line' and color_of(vals[('.c', 'top')]) == '#30363d'
                   and color_of(vals[('.e', '')]) == 'color-mix' and color_of(vals[('.f', '')]).startswith('rgba(')))
    # T469 — 같은 블록 안 단축 뒤의 `border-width` 는 굵기를 덮고(색은 남는다) · 단축 앞의 것은 무시 · 단축 없는 것은 선언이 아니다
    checks.append(('단축 뒤 border-width 가 굵기를 덮는다(.g ol2 → ol1 · 색 #000 그대로)', tiers[('.g', '')] == 'ol1' and color_of(vals[('.g', '')]) == '#000'))
    checks.append(('단축 앞의 border-width 는 단축이 되덮는다(.h ol3)', tiers[('.h', '')] == 'ol3'))
    checks.append(('단축 없는 border-width 뿐인 블록은 테 선언이 아니다(.i 없음)', ('.i', '') not in tiers))
    tmp = tempfile.mkdtemp()
    ui = os.path.join(tmp, 'Ui'); os.makedirs(ui)
    with open(os.path.join(ui, 'Face.cs'), 'w', encoding='utf-8') as f:
        f.write(cs)
    logs = []
    good = {'.a': ['Ui/Face.cs#line'], '.b': ['Ui/Face.cs@Toast'], '.c|top': ['Ui/Face.cs#tile-face'], '.c|left': ['—직접값 자리 · 클론에 없음'], '.d': ['—정본이 끄는 규칙(none)'], '.f': ['Ui/Face.cs']}
    checks.append(('맞는 표는 rc 0', run(css, tmp, good, {}, logs.append) == 0))
    checks.append(('미정은 막지 않는다(.e·.g·.h 가 표에 없다)', any('미정 3' in l for l in logs)))
    bad = dict(good); bad['.e'] = ['Ui/Face.cs#nothing']
    checks.append(('자리 없음은 rc 1', run(css, tmp, bad, {}, logs.append) == 1))
    checks.append(('KNOWN 이면 rc 0', run(css, tmp, bad, {'Ui/Face.cs#nothing': '임자 있음'}, logs.append) == 0))
    bad2 = dict(good); bad2['.b'] = ['Ui/Face.cs@Bare']
    checks.append(('메서드 본문에 테 호출이 없으면 rc 1', run(css, tmp, bad2, {}, logs.append) == 1))
    bad3 = dict(good); bad3['.zzz'] = ['Ui/Face.cs']
    checks.append(('표의 선택자가 정본에 없으면 rc 1', run(css, tmp, bad3, {}, logs.append) == 1))
    # 2회차 — 폭 단: .a 는 정본 ol3 인데 Toast 는 PopupKit.Line(ol1) 이라 어긋남 · Build 의 UiKit.Line 은 Line3 이라 맞다
    bad4 = dict(good); bad4['.a'] = ['Ui/Face.cs@Toast']
    checks.append(('폭 단이 정본과 다르면 rc 1(단 어긋남)', run(css, tmp, bad4, {}, logs.append) == 1))
    checks.append(('폭 단이 맞으면 ok(.a ol3 ↔ Build 의 Line3)', check_target(tmp, 'Ui/Face.cs@Build', 'ol3')[0] == 'ok'))
    with open(os.path.join(ui, 'Ring.cs'), 'w', encoding='utf-8') as f:
        f.write('class R {\n'
                '    void Card(Transform p, float r) { UiKit.Rounded(p, "line", "pp_line", r); Image face = UiKit.Rounded(p, "face", "pp_paper", r - PopupKit.Line3); }\n'
                '    void Fill(Transform p, float r) { UiKit.Rounded(p, "bg", "pp_paper", r); }\n'
                '    void Lip(Transform p, float r) { UiKit.Rounded(p, "line", "pp_line", r); Image lip = UiKit.Rounded(p, "lip", "pp_blue", r * 0.8f); }\n'
                '    void Far(Transform p, float r) { UiKit.Rounded(p, "line", "pp_line", r); ' + ('/* ' + '긴 주석 ' * 60 + '*/ ') + 'Image ground = UiKit.Rounded(p, "ground", "pp_paper", Mathf.Max(1f, r - UiKit.L("line2_px"))); }\n'
                '}\n')
    checks.append(('Rounded 짝(line + face r - Line3)은 테 ol3', check_target(tmp, 'Ui/Ring.cs@Card', 'ol3')[0] == 'ok'))
    checks.append(('Rounded 홀로(bg 채움)는 테가 아니다', check_target(tmp, 'Ui/Ring.cs@Fill', 'ol1')[0] == 'missing'))
    checks.append(('입술 꼴(lip · 반지름 따로)은 테지만 단은 못 읽는다 → 판정 안 함', check_target(tmp, 'Ui/Ring.cs@Lip', 'ol2')[0] == 'ok'))
    checks.append(('안쪽 면이 창 안에서 시작해 밖에서 끝나도 짝이고 단 ol2 를 읽는다(6회차)', check_target(tmp, 'Ui/Ring.cs@Far', 'ol2')[0] == 'ok' and check_target(tmp, 'Ui/Ring.cs@Far', 'ol1')[0] == 'tier'))
    # 12회차 — 둥근 테 짝(Circle + 면 + PopupKit.Inset) · 정본이 «끄는» 자리의 군더더기 테
    with open(os.path.join(ui, 'Dot.cs'), 'w', encoding='utf-8') as f:
        f.write('class D {\n'
                '    void Ring(Transform p) { UiKit.Circle(p, "line", "pp_line"); Image face = UiKit.Circle(p, "face", "pp_paper"); PopupKit.Inset(face.rectTransform, UiKit.L("line2_px")); }\n'
                '    void Disc(Transform p) { UiKit.Circle(p, "face", "pp_paper"); }\n'
                '    void Bare(Transform p) { UiKit.Box(p, "x"); }\n'
                '}\n')
    checks.append(('Circle 짝(고리 + 면 + Inset)은 테이고 단은 Inset 에서 읽는다', check_target(tmp, 'Ui/Dot.cs@Ring', 'ol2')[0] == 'ok'))
    checks.append(('Circle 짝의 단이 정본과 다르면 «단 어긋남»', check_target(tmp, 'Ui/Dot.cs@Ring', 'ol3')[0] == 'tier'))
    checks.append(('Circle 홀로(원판)는 테가 아니다', check_target(tmp, 'Ui/Dot.cs@Disc', 'ol1')[0] == 'missing'))
    checks.append(('정본이 끄는 자리(none)는 테 호출이 없어야 ok', check_target(tmp, 'Ui/Dot.cs@Bare', 'none')[0] == 'ok'))
    checks.append(('정본이 끄는 자리에 테가 있으면 «군더더기 테»', check_target(tmp, 'Ui/Dot.cs@Ring', 'none')[0] == 'extra'))
    # 15회차 — ⑴ 지역 변수 해석 ⑵ «면을 폭×2 만큼 깎는» 고리 ⑶ 그 파일 제 `Inset`
    with open(os.path.join(ui, 'Pip.cs'), 'w', encoding='utf-8') as f:
        f.write('class P {\n'
                '    void Shrink(Transform p) { float line = UiKit.L("line2_px"); ring.Ring.rectTransform.sizeDelta = new Vector2(size, size); ring.Fill.rectTransform.sizeDelta = new Vector2(size - line * 2f, size - line * 2f); }\n'
                '    void Track(Transform p) { float line = UiKit.L("line2_px"); UiKit.Panel(t, "edge", "pp_line"); Image core = UiKit.Panel(t, "core", "pip"); Inset(core.rectTransform, line); }\n'
                '    void Portrait(Transform p) { float line = UiKit.L("line_px"); Inset(img.rectTransform, line * 2f); }\n'
                '}\n')
    checks.append(('깎기 꼴 고리는 테이고 단은 변수를 풀어 읽는다(line = line2_px → ol2)',
                   check_target(tmp, 'Ui/Pip.cs@Shrink', 'ol2')[0] == 'ok'))
    checks.append(('그 변수가 ol1 이었으면 «단 어긋남» 이다 — 이름만 보고 ol1 로 읽지 않는다',
                   check_target(tmp, 'Ui/Pip.cs@Shrink', 'ol3')[0] == 'tier'))
    checks.append(('그 파일 제 Inset(면 꼴 이름)도 테다', check_target(tmp, 'Ui/Pip.cs@Track', 'ol2')[0] == 'ok'))
    checks.append(('테가 아닌 인셋(초상 여백)은 안 센다', check_target(tmp, 'Ui/Pip.cs@Portrait', 'ol1')[0] == 'missing'))
    checks.append(('변수 해석 — `line` 이라는 이름에 line2_px 가 들면 ol2 다',
                   tier_of_expr('line * 2f', {'line': 'line2_px'}) == 'ol2'))
    checks.append(('변수를 안 풀면 이름 탓에 ol1 로 읽힌다(이 회차가 고친 함정)',
                   tier_of_expr('line * 2f') == 'ol1'))
    logs4 = []
    checks.append(('군더더기 테는 rc 1', run(css, tmp, {'.d': ['Ui/Dot.cs@Ring']}, {}, logs4.append) == 1 and any('군더더기 테' in l for l in logs4)))
    checks.append(('군더더기 테도 KNOWN 이면 rc 0', run(css, tmp, {'.d': ['Ui/Dot.cs@Ring']}, {'.d → Ui/Dot.cs@Ring': '임자 있음'}, [].append) == 0))
    logs5 = []
    run(css, tmp, {'.d': ['Ui/Dot.cs@Ring']}, {'.d → Ui/Dot.cs@Ring': '임자 있음'}, logs5.append)
    checks.append(('군더더기 테를 KNOWN 에 둬도 «이제 테가 있다» 로 거꾸로 울지 않는다', not any('이제 테가 있다' in l for l in logs5)))
    logs2 = []
    run(css, tmp, {'.a': ['Ui/Face.cs#line']}, {'.a → Ui/Face.cs#line': 'x'}, logs2.append)
    checks.append(('KNOWN(쌍 열쇠)인데 이제 있다 → 알린다', any('이제 테가 있다' in l for l in logs2)))
    logs3 = []
    # 같은 도우미(@Toast)를 두 선택자가 쓰는데 하나만 KNOWN — 맞는 쪽(.b ol1) 때문에 «이제 있다» 가 울면 안 된다(4회차 수리)
    run(css, tmp, {'.a': ['Ui/Face.cs@Toast'], '.b': ['Ui/Face.cs@Toast']}, {'.a → Ui/Face.cs@Toast': '단 어긋남 임자 있음'}, logs3.append)
    checks.append(('쌍 열쇠는 다른 선택자의 초록에 «이제 있다» 로 안 운다', not any('이제 테가 있다' in l for l in logs3) and any('문제 0' in l for l in logs3)))
    # T472 — 테 스타일(border-style · 단축 안 낱말) · 점선 표 · 점선 호출
    st = parse_styles(css)
    checks.append(('border-style: dashed 를 읽는다(.j) · border-style 뿐인 블록은 테 선언으로 안 센다', st.get('.j', (0, ''))[1] == 'dashed' and ('.j', '') not in tiers))
    st2 = parse_styles('.k { border: var(--ol1) dashed #000; } .k { border-style: solid; } .l, .m { border: 2px solid #000; } .n { border-top: 1px dashed #000; }')
    checks.append(('단축 안 스타일 낱말도 읽고 뒤 border-style 이 덮는다(.k solid) · 쉼표 목록은 선택자마다(.l·.m) · 변 있는 단축은 안 읽는다(.n)',
                   st2['.k'][1] == 'solid' and st2['.l'][1] == 'solid' and st2['.m'][1] == 'solid' and '.n' not in st2))
    with open(os.path.join(ui, 'Dash.cs'), 'w', encoding='utf-8') as f:
        f.write('class Q {\n'
                '    void Empty(Transform p, float w, float h) { SurfaceArt.DashedFrame(p, "line", "pp_line", w, h, 3f, PopupKit.Line3, PopupKit.Line3 * 3f, PopupKit.Line3 * 3f); }\n'
                '    void Solid(Transform p) { PopupKit.Outlined(p, "face", "pp_paper", 4f, PopupKit.Line3); }\n'
                '}\n')
    checks.append(('점선 자리에 DashedFrame 이 있으면 ok · 실선 도우미뿐이면 «점선 아님»', check_dashed(tmp, 'Ui/Dash.cs@Empty')[0] == 'ok' and check_dashed(tmp, 'Ui/Dash.cs@Solid')[0] == 'missing'))
    checks.append(('이름(#line)으로도 점선 호출을 찾는다', check_dashed(tmp, 'Ui/Dash.cs#line')[0] == 'ok' and check_dashed(tmp, 'Ui/Dash.cs#face')[0] == 'missing'))
    checks.append(('DashedFrame 은 테 호출이고 단은 일곱째 인자에서 읽는다(ol3)', check_target(tmp, 'Ui/Dash.cs@Empty', 'ol3')[0] == 'ok' and check_target(tmp, 'Ui/Dash.cs@Empty', 'ol1')[0] == 'tier'))
    logs6 = []
    checks.append(('점선 표가 맞으면 rc 0 · 요약에 «점선 1»', run(css, tmp, good, {}, logs6.append, dashed={'.j': ['Ui/Dash.cs@Empty']}) == 0 and any('점선 1(' in l for l in logs6)))
    checks.append(('점선 자리가 실선이면 rc 1', run(css, tmp, good, {}, [].append, dashed={'.j': ['Ui/Dash.cs@Solid']}) == 1))
    checks.append(('정본이 dashed 가 아닌 선택자를 점선 표에 적으면 rc 1', run(css, tmp, good, {}, [].append, dashed={'.a': ['Ui/Dash.cs@Empty']}) == 1))
    logs7 = []
    run(css, tmp, good, {}, logs7.append, list_pending=True)
    checks.append(('정본이 dashed 로 적었는데 점선 표에 없으면 «점선 미정» 으로 세기만 한다(막지 않는다)', any('점선 0(미정 1)' in l for l in logs7) and any('점선 미정' in l and '.j' in l for l in logs7)))
    # T365 22회차 — 제 안의 맨 이름 호출(`Tile(`·`Framed(`)과 `PetSkillKit.Gauge`(Framed 한 겹 · 폭 인자 8)를 읽는다
    with open(os.path.join(ui, 'Kit.cs'), 'w', encoding='utf-8') as f:
        f.write('class PetSkillKit {\n'
                '    public static RectTransform Framed(Transform parent, string name, Color fill, float radiusPx, float linePx) { UiKit.Rounded(parent, "line", "pp_line", radiusPx); return null; }\n'
                '    public static Button PaperButton(Transform parent, string name) { RectTransform box = Framed(parent, "skin", dk, r, Line3); return null; }\n'
                '    public static RectTransform Gauge(Transform parent, string name, float w, float h, float ratio, string label, Color bg, float radiusPx, float linePx, TextKind kind) { return Framed(parent, name, bg, radiusPx, linePx); }\n'
                '}\n'
                'class ForgeUi {\n'
                '    static void AgeBar(Transform bar) { Tile(bar, "box", face, Color.black, r, PopupKit.Line); }\n'
                '}\n'
                'class SkillPanel {\n'
                '    static void Build(Transform p) { RectTransform g = PetSkillKit.Gauge(p, "summon-gauge", 10f, 2f, .5f, "x", c, r, PetSkillKit.Line2, TextKind.Sub); }\n'
                '}\n')
    checks.append(('제 안의 맨 `Framed(` 도 테 호출이다(PaperButton 껍질 ol3) · 이름이 변수인 호출(Gauge 본문의 `Framed(parent, name, …)`)은 안 센다 — 그래서 게이지는 부르는 쪽 이름(#)으로 짝짓는다',
                   check_target(tmp, 'Ui/Kit.cs@PaperButton', 'ol3')[0] == 'ok' and check_target(tmp, 'Ui/Kit.cs@PaperButton', 'ol1')[0] == 'tier' and check_target(tmp, 'Ui/Kit.cs@Gauge', 'ol3')[0] == 'missing'))
    checks.append(('제 안의 맨 `Tile(` 은 여섯째 인자가 단이다(#box ol1 → ol3 자리면 «단 어긋남»)',
                   check_target(tmp, 'Ui/Kit.cs#box', 'ol1')[0] == 'ok' and check_target(tmp, 'Ui/Kit.cs#box', 'ol3')[0] == 'tier'))
    checks.append(('`PetSkillKit.Gauge` 는 아홉째 인자가 단이다(#summon-gauge ol2)',
                   check_target(tmp, 'Ui/Kit.cs#summon-gauge', 'ol2')[0] == 'ok' and check_target(tmp, 'Ui/Kit.cs#summon-gauge', 'ol3')[0] == 'tier'))
    failed = [n for n, ok in checks if not ok]
    for n, ok in checks:
        print(('  ✓ ' if ok else '  ✗ ') + n)
    print(('✓' if not failed else '✗') + ' check_box_borders --self-test %d칸 %s' % (len(checks), '통과' if not failed else '실패 %d' % len(failed)))
    return 0 if not failed else 1


def main(argv):
    css_path, game_dir, list_pending = CSS_DEFAULT, GAME_DEFAULT, False
    i = 1
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--css': css_path = argv[i + 1]; i += 2; continue
        if a == '--game': game_dir = argv[i + 1]; i += 2; continue
        if a == '--list': list_pending = True; i += 1; continue
        print('모르는 인자: ' + a); return 2
    if not os.path.isfile(css_path):
        print('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    return run(_read(css_path), game_dir, TABLE, KNOWN, list_pending=list_pending, dashed=DASHED)


if __name__ == '__main__':
    sys.exit(main(sys.argv))
