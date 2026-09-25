#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T178 — 정본 «표면 겹»(`linear/radial/conic-gradient` 선언) ↔ 클론 «구운 겹» 대조 자.

정본 `web/css/style.css` 는 거의 모든 면에 같은 문법을 깐다 — 위쪽 1px 흰 줄(`linear-gradient(180deg, rgba(255,255,255,.6) 0 1px, …)`)
+ 45° 미세 빗금(`repeating-linear-gradient(45deg, … 0 2px, … 2px 9px)`) + 세로 명암. 선언 176(선택자 137 · `background` 83 ·
`background-image` 60 · `mask-image` 30 · `--af-pat` 3). 클론 공용 표면(`UiKit.Panel`·`Rounded`)은 색 한 칸짜리 `Image` 라
그 겹이 **조용히 사라진다** — 화면이 «플라스틱 단색» 으로 읽히는 까닭이다(T33 16회차 실측).

무엇을 «겹이 있다» 로 세나: 그 자리가 **굽는 길**(결정 223 — 이 레포에서 모양·명암은 전부 구운 스프라이트로 온다)을 지나가는가 —
`CraftFxPoly.Bake*`(정지점 그라디언트) · `AgePattern.Tile`(T124 시대 무늬) · `BattleOverlay.RadialSprite/VigSprite`(T173 방사형) ·
`BootLoading.GradSprite` · `CraftCardArt.Sheen` · 앞으로 설 공용 도우미 `UiKit.Surface*`/`SurfaceArt.*`(T178 ⓑ) · 제 손으로 굽는 `Sprite.Create`.

선택자 ↔ 클론 자리 짝은 TABLE 하나가 쥔다(T109 `check_keyline` · T159 `check_clip_paths` 의 꼴 그대로):
  "Ui/File.cs"           파일 안 어디든 굽는 호출이 하나 있으면 됨
  "Ui/File.cs#name"      그 이름으로 만든 오브젝트가 굽는 공장에서 나왔거나, 그 변수에 굽는 호출이 닿아야 함
  "Ui/File.cs@Method"    도우미 메서드 본문 안에 굽는 호출이 있어야 함
  "—<이유>"              클론에 그 자리가 없거나 정본이 그 겹을 끄는 규칙 — 대조하지 않는다

⚠ `check_clip_paths` 와 다른 한 가지 — **표에 없는 정본 선택자는 «미정» 으로 세고 막지 않는다**(rc 그대로). 선택자가 137 이라
한 회차에 다 짝지을 수 없고, 짝이 없는 자리는 «아직 안 옮긴 자리» 지 «표가 틀린 자리» 가 아니다. 회차마다 TABLE 이 자라고
«미정» 이 줄어드는 것이 이 자의 진행 수치다. 막는 것은 둘 — 표에 적힌 자리에 겹이 없는데 KNOWN 도 아닌 것 · 표에는 있는데
정본에서 사라진 선택자.

빈자리 중 «임자가 정해진 것» 은 KNOWN 에 두어 rc 0 으로 지나간다(T89·T109·T159 의 KNOWN 규약). 닫았는데 안 지우면 «이제 있다» 로 알린다.

사용:  python3 tools/check_surface_gradients.py [--css <style.css 경로>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 표의 자리 전부가 (겹이 있거나 KNOWN) 이고 표의 선택자가 전부 정본에 있다 · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')

# ── 정본 선택자 ↔ 클론 자리 ──────────────────────────────────────────────────────────────
# 선택자는 style.css 의 것을 공백 하나로 정규화한 그대로(한 규칙에 선택자가 여럿이면 쉼표 목록 통째로). 표에 없는 선택자 = 미정.
TABLE = {
    # T173 — 보스 경고 두 겹(방사형 딤 · 가산 플래시)은 BattleOverlay.Radial 공장이 RadialSprite 로 굽는다.
    '.bw-dim': ['Ui/BattleOverlay.cs#bw-dim'],
    '.bw-flash': ['Ui/BattleOverlay.cs#bw-flash'],
    # T135 — 피격 붉은 비네트(정본 #dmg-flash 의 radial + mask) → VigSprite.
    '#dmg-flash': ['Ui/BattleOverlay.cs@VigSprite'],
    '.rw-glow': ['Ui/RewardBurst.cs@GlowFx'],   # T178 14회차 — 수령 임팩트 글로우(방사형 · SurfaceUi.json rw_glow · 박동은 RewardBurstUi.json glow)
    # T178 36회차 — 시대 막대의 겹 셋: 면 위 톤 램프(8526 · 8250 은 덮인다 · 시대색 위에 FillMasked) · 광택 `::after` 둘(4845 자동 제련 · 5125 확률 정보 · 막대 맨 끝 형제) — `ForgeUi.AgeBar`.
    #   무늬 층의 마스크(4841 `.af-age-bar::before` 30→50% · 5116 `.fi-age-bar::before` 24→46% · T380 키 AfBar/FiBar)는 AgePattern 이 굽는다.
    '.fi-age-bar[data-age], .af-age-bar[data-age]': ['Ui/ForgeUi.cs@AgeBar'],
    '.af-age-bar::after': ['Ui/ForgeUi.cs@AgeBar'],
    '.fi-age-bar::after': ['Ui/ForgeUi.cs@AgeBar'],
    '.af-age-bar::before': ['Ui/AgePattern.cs'],
    '.fi-age-bar::before': ['Ui/AgePattern.cs'],
    # T124 — 시대 무늬 셋(--af-pat 사용자 속성)은 AgePattern.Tile 이 굽는다.
    '.af-age-bar[data-age="interstellar"], .fi-age-bar[data-age="interstellar"], .equip-cell[data-age="interstellar"]': ['Ui/AgePattern.cs@Tile'],
    '.af-age-bar[data-age="multiverse"], .fi-age-bar[data-age="multiverse"], .equip-cell[data-age="multiverse"]': ['Ui/AgePattern.cs@Tile'],
    '.af-age-bar[data-age="quantum"], .fi-age-bar[data-age="quantum"], .equip-cell[data-age="quantum"]': ['Ui/AgePattern.cs@Tile'],
    # T87 28회차 — 결과 카드 광택 띠(crsheen) 는 CraftCardArt.Sheen 이 굽는다.
    '.auto-drop-card.craft-reveal::after': ['Ui/CraftCardArt.cs@Sheen'],
    # T178 15회차 — 제작 카드 둘의 45°/−45° 교차 해칭(SurfaceUi.json stripes.cell_hatch · SurfaceArt.FillHatch · 바탕 color-mix 면 위 sRGB 합성) · `.equip-cell` 828 은 ForgeSheet lock 뒤 같은 키
    '.auto-drop-card': ['Ui/ForgeCraftPopup.cs@CraftCard'],
    # T178 24회차 — 장비 **상세** 팝업의 아이콘 상자도 같은 해칭 두 겹이다(정본 3657 주석 «장비 상세도 목록과 같은 언어 … 해칭 배경 + 시대색 58% 틴트 면 + 시대색 80% 테»).
    #   면 색(color-mix)은 T371 이 이미 덮어 뒀고 해칭만 없었다 — `ForgeCraftPopup.CraftCard` 와 같은 한 줄.
    '#forge-item-modal .idet-icon': ['Ui/ForgeInfoPopup.cs@RenderDetail'],
    '.equip-cell': ['Ui/ForgeSheet.cs@EquipCell'],   # T178 16회차 — 828 해칭 둘(7730 이 non-egg 셀 background-image 를 덮어써도 해칭은 맨 아래 두 겹으로 남는다) · 7730 의 나머지 셋은 미정

    '.craft-batch .cb-card': ['Ui/ForgeCraftPopup.cs@CraftCard'],
    # T178 18회차 — 7730 `.equip-cell:not(.egg-cell)` 의 위 세 겹(방사 둘 + 명암)과 8075/849 `.equip-cell.egg-cell` 세 겹은 SurfaceArt.FillFace 가 **셀 면 통째**(바탕 → 해칭 → 겹 · sRGB 차례 합성) 굽는다.
    '.equip-cell:not(.egg-cell)': ['Ui/ForgeSheet.cs@EquipCell'],
    '.equip-cell.egg-cell': ['Ui/ForgeSheet.cs@MountCell'],
    # ── T178 7회차 — «하드 스톱 띠»: 정본이 gradient 문법으로 적었지만 **정지점 사이에 섞임이 없는** 자리다.
    #    그림은 «가운데 11px 세로 줄» · «45° 줄무늬» 처럼 **색면 조각**이고, 클론이 조각(rect·dash)으로 그리면
    #    픽셀이 정본과 같다 — 여기에 그라디언트를 굽는 것은 같은 그림을 더 비싸게 그리는 것뿐이다.
    #    그래서 «굽기 없음» 이 정답인 자리로 적고, 어디가 그 조각인지 줄 번호로 남긴다(다음 사람이 다시 안 세게).
    '.pass-track': '—띠 가운데 11px 세로 레일(90deg 하드 스톱) · 클론은 `PassPopup.cs:111` 흰 트랙 + 구간마다 레일 조각으로 그린다',
    '.pass-seg': '—띠 미도달 구간 레일(#0f121c) · 클론 `PassPopup.cs:132` `UiKit.Panel(seg, "rail", "pass_rail_dead")` · 폭은 catalog `pass_rail_w`(11px/1080)',
    '.pass-seg.reached': '—띠 도달 구간 레일(#341cff) · 같은 줄의 `pass_rail`',
    '.bw-hazard': '—띠 45° 위험 줄무늬(repeating · 1.1rem 주기) · 클론 `BattleOverlay.cs:211~226` 이 조각 24개(`hazardDashes`)로 그리고 흐른다',
    # T178 2회차 — 던전 배너의 비스듬한 바탕과 «왼쪽 제목 자리 스크림» 은 공용 굽기 SurfaceArt 가 표(SurfaceUi.json)대로 굽는다.
    '.dg-banner': ['Ui/DungeonSheet.cs#bg-grad'],
    '.dg-banner::before': ['Ui/DungeonSheet.cs#scrim'],
    '.dg-detail-hero': ['Ui/DungeonDetailPopup.cs#bg-grad'],   # T178 19회차 — 2060 상세 hero 바탕 = 목록 배너와 같은 겹(dg_banner) · 일러스트 뒤
    '.idet-icon.tn-bronze': ['Ui/TechPopups.cs#bg-grad'],
    # T178 20회차 — 자동 제련 팝업의 «면 겹» 둘(스피너 검정 금속 톤 · 체크 상자). 둘 다 둥근 면 위라 SurfaceArt.FillMasked.
    '.af-spinner': ['Ui/ForgeAutoPopup.cs#bg-grad'],
    # T178 22회차 — 같은 팝업의 남은 면 겹 넷. 정본 주석 4984 가 앞 둘을 한 줄로 적어 뒀다 — «트랙은 **파인 홈**, 노브는 **광택 구슬**».
    #   ⚠ 토글 두 겹은 **자동 제련 토글에만** 준다(공용 `PopupKit.Toggle` 은 설정 토글도 쓰는데 정본 3107 엔 이런 줄이 없다) —
    #     그래서 도우미가 아니라 `ForgeAutoPopup` 이 그 두 칸에 얹는다.
    '.af-toggle': ['Ui/ForgeAutoPopup.cs#bg-grad'],          # 4986 파인 홈(위 어둡고 아래 밝다)
    '.af-toggle .knob': ['Ui/ForgeAutoPopup.cs#bg-grad'],    # 4990 광택 구슬(circle at 34% 26% · farthest-corner = .9916)
    '.af-sub-row': ['Ui/ForgeAutoPopup.cs#bg-grad'],         # 4998 «얇은 카드 두께»(T331 26회차가 세운 세 그림자의 짝)
    '.af-start': ['Ui/ForgeAutoPopup.cs#bg-grad'],           # 5016 솟은 면(트랙의 파인 홈과 반대 방향)
    '.af-check': ['Ui/ForgeAutoPopup.cs#bg-grad'],      # T178 19회차 — 3689 청동 원 면 위 마스크 겹(tn_bronze)
    # T178 3회차 — 둥근 면 위는 SurfaceArt.FillMasked(면에 Mask) 로 얹는다(모서리 밖으로 안 샌다).
    '.shop-banner': ['Ui/ShopSheet.cs#shop-banner-grad'],
    '.league-collect-pill': ['Ui/LeagueSheet.cs#collect-grad'],
    # T178 23회차 — 리그 시트 **발 밴드**의 면 겹(2361). 한 그라디언트가 px 와 % 를 섞는 첫 자리라 표에 `units` 를 새로 뒀다.
    '.league-foot': ['Ui/LeagueSheet.cs#bg-grad'],
    # T178 27회차 — 소환 결과 **제목 띠**(6207)와 그 위·아래 **금색 헤어라인**(6220). 둘 다 양끝 알파가 0 이라
    #   «투명을 품은 채» 얹히는 겹이고(바탕을 미리 안 섞는다), 띠는 새 조각을 만들지 않고 그 판의 **그림을 바꿔** 세운다.
    '.sr-title': ['Ui/SkillSummonResult.cs@Build'],
    '.sr-title::before, .sr-title::after': ['Ui/SkillSummonResult.cs@Build'],
    # T178 29회차 — x1 요약의 [다시 소환] 버튼(5791)의 세로 명암 겹. 알파 .9 는 바탕이 모달 방사형이라 표에 `over_color` 를 못 적고
    #   **부르는 쪽이 색을 준다**(`sr_bg_c` · af_check 갈래 · 결정 808) — SurfaceUi.json `sr_again` 의 `_2` 가 셈을 쥔다.
    '.sr-again': ['Ui/SkillSummonResult.cs@BuildFoot'],
    # T178 30회차 — 구슬 접지 그림자(6350 `::before`) · 검정 방사 타원(76% 밖 알파 0 · 투명을 품은 채) · 자리는 PetSkillUi.json sr_ground_*.
    '.sr-orbwrap::before': ['Ui/SkillSummonResult.cs@BuildCell'],
    # T178 31회차 — 중앙 광원(5955 · 0% 정지점이 런타임 등급색) · done 판(7166 · 정지점 둘 · 이산 전환) — SurfaceArt.Bake(key, aspect, stopIndex, color) 길.
    '.sr-halo': ['Ui/SkillSummonResult.cs@Build'],
    # T178 32회차 — 소환진 바닥 두 겹(5806~5813 · SurfaceUi.json sr_floor_fill·sr_floor_ring · 흰 알파 단면 + 틴트 · 결정 817) · 홀드백 착지 섬광(6156 · sr_flash · 26% 런타임 등급색 · farthest-corner 정사각 + 클립)
    '.sr-floor::before': ['Ui/SkillSummonResult.cs@BuildFloorPlates'],
    '.sr-flash': ['Ui/SkillSummonResult.cs@PlaceFlash'],
    '#summon-result-modal.done .sr-halo': ['Ui/SkillSummonResult.cs@SwapHaloDone'],
    # T178 33회차 — [확인] 금색 버튼 면(7139 = 8748 쌍둥이 · 불투명 셋) · NEW 배지 면(6980 · 불투명 둘) — 바탕 없이 FillMasked.
    '.sr-ok': ['Ui/SkillSummonResult.cs@BuildFoot'],
    '.btn.btn.sr-ok.sr-ok': ['Ui/SkillSummonResult.cs@BuildFoot'],
    '.sr-new': ['Ui/SkillSummonResult.cs@BuildCell'],
    # T178 34회차 — 소환 결과의 «투명 품는 방사» 둘: 5728 상시 비네트(`::after` · 충전 비네트 5744 `::before` 는 BakeVig 로 따로) · 5755 그리드 뒤 받침(124% × +2rem · 광선 다음 형제). 둘 다 SurfaceArt.Fill(표 sr_wrap_vig · sr_body_plate).
    '.sr-wrap::after': ['Ui/SkillSummonResult.cs#sr-vig-static'],
    '.sr-body::before': ['Ui/SkillSummonResult.cs#sr-body-plate'],
    # T178 23회차 — 단(tier) 행 위끝 대시 줄(2548 `repeating-linear-gradient`). **코드는 이미 서 있다** — T368 5회차가
    #   `LeagueSheet.TierDash`(`SurfaceArt.BakeStripe` · 표 `stripes.league_tier_dash`)로 세웠는데 이 자 표에만 안 올라
    #   «미정» 으로 세어지고 있었다(`#panel-skills .summon-bar::before` → `SkillPanel.cs@SummonDash` 와 같은 꼴).
    '.league-reward-tier': ['Ui/LeagueSheet.cs@TierDash'],
    '.pinfo-preview': ['Ui/PlayerInfoPopup.cs#preview-grad'],
    # T178 38회차 — 게이지: 트랙 홈 7992(셋 · 표 gauge_track · 바탕은 그 자리의 트랙 색) · 채움 광택 7953(넷 · 표 gauge_fill · units px+f · 바탕은 채움 색). 8812 `::after` 눈금(--seg 변수 · 되풀이 띠)은 다음.
    '.upg-progress, .summon-gauge, .qst-bar': '—정본 8798(aaa-skin ⓖ · 2026-08-19 «플랫/매트 + 분절») 이 `background-image: none` 으로 끈다 — 38회차가 세운 홈(gauge_track)을 39회차가 걷었다 · 트랙 = 색 한 칸(+ 하드 키라인 inset ol1 은 box-shadow 축)',
    '#upg-fill, #tech-node-fill, .tech-prog #tech-node-fill, .summon-gauge i': '—정본 8803(aaa-skin ⓖ) 이 `background-image: none` 으로 끈다 — 38회차의 광택(gauge_fill)을 39회차가 걷었다 · 채움 = 색 한 칸',
    # T178 37회차 — 은색 버튼 가족(5209 `.summon-btn` · 5260/5268 `.skd-btn.silver(.disabled)` · 5274 `.btn.silver` — #e3e3e3 → #c2c2c2 · 비활성 #d9d9d9 → #bdbdbd)과 승천 소환 버튼(5641 · #4caf50 → #2e7d32):
    #   `PetSkillKit.PaperButton`(Silver/Ascend · 둥근 면에 FillMasked · `.petup-selrow .btn.silver` 5487 단색은 plainFace) · 던전 상세 은색은 `DungeonPopups.Pill`. 리그 점수 알약(7881)은 `LeagueSheet` 의 `score/bg`.
    #   5626 `.btn.sm.ascend-ready`(대장간 만렙 버튼)는 클론 ForgeSheet 의 버튼이 파랑 한 벌이라 아직 미정(색 축 T377 과 함께 열 자리).
    '.summon-btn': ['Ui/PetSkillKit.cs@PaperButton'],
    '.skd-btn.silver': ['Ui/PetSkillKit.cs@PaperButton'],
    '.skd-btn.silver.disabled': ['Ui/PetSkillKit.cs@PaperButton'],
    '.btn.silver': ['Ui/PetSkillKit.cs@PaperButton', 'Ui/DungeonPopups.cs@Pill'],
    '.summon-bar .btn.big.ascend-ready': ['Ui/PetSkillKit.cs@PaperButton'],
    '.league-score': ['Ui/LeagueSheet.cs@Row'],
    # T178 35회차 — 상단바 밴드(7900 · 겹 둘 · 위 1px 림 · 탭바를 뒤집은 짝 · `Hud.Build` 의 `bar`) · 재화 알약 홈(7924 · 둥근 면이라 FillMasked · `Hud.Pill`).
    '#topbar': ['Ui/Hud.cs#topbar'],
    '.currency-pills .pill': ['Ui/Hud.cs@Pill'],
    # T178 4회차 — 하단 탭바 밴드(겹 둘 · 아래 1px 림은 표의 `unit: "px"`) · 스킬 확률 막대(에나멜 하이라이트 + 위 1px 림 · 둥근 면이라 FillMasked).
    # T178 6회차 — 퀘스트 진행 막대 채움의 두 겹(세로 띠 + 위 1px 광택). 상태는 띠 키로만 가른다(정본 주석).
    # T178 40회차 — 8812 분절 눈금 `::after` 여섯(aaa-skin ⓖ ㉳ · `repeating-linear-gradient(90deg, …)` · 표 stripes.gauge_seg · SurfaceArt.SegTicks 한 타일 + Tiled):
    #   summon-gauge·petup-xpbar·rates-prog 는 `PetSkillKit.Gauge` 한 공장(sk-shard 는 목록에 없다) · qst-bar 는 `QuestSheet.Render` · upg-progress 는 `ForgeInfoPopup.RenderLevelView` ·
    #   `.summon-prog` 는 정본 마크업(ui.js)에 없다 — 클론 자리 없음. 8798 키라인(inset box-shadow)은 겹이 아니라 자가 안 본다(SurfaceArt.Keyline · PlayMode 자가 잰다).
    '.upg-progress::after, .summon-gauge::after, .qst-bar::after, .summon-prog::after, .petup-xpbar::after, .rates-prog::after': ['Ui/PetSkillKit.cs@Gauge', 'Ui/QuestSheet.cs@Render', 'Ui/ForgeInfoPopup.cs@RenderLevelView'],
    '.af-age-bar': '—정본 4825 `.af-age-bar { background-image: none }`(시대별 무늬로 갈아탄 블록 · «공용 45° 줄무늬 해제») 가 4701 의 공용 줄무늬를 끈다 — 무늬는 `::before` 가 시대별로(AgePattern · 표 행 따로) · 40회차',
    '.qst-bar i': '—정본 8806 `.qst-bar i { background: #4fc3f7 }`(aaa-skin ⓖ) 이 2044 의 두 겹을 단색으로 끈다 — 6·10회차의 qst-fill-grad·rim 을 39회차가 걷었다 · 채움 = catalog quest_bar 한 칸',
    '.qst-row.done .qst-bar i': '—정본 8807 `… { background: #81e884 }`(aaa-skin ⓖ) 이 2047 의 두 겹을 단색으로 끈다 — 39회차가 걷었다 · 채움 = catalog quest_bar_done 한 칸',
    # T178 9회차 — 기술 트리 분기 원판(카테고리색 면) 위의 겹 둘: 왼쪽 위 방사형 광택 + 세로 명암.
    '.tech-branch-icon::before': ['Ui/TechPanel.cs#tb-icon-gloss', 'Ui/TechPanel.cs#tb-icon-shade'],
    '#panel-skills .summon-bar::before': ['Ui/SkillPanel.cs@SummonDash'],   # T368 3회차 — 되풀이 대시 한 타일(SurfaceArt.BakeStripe · 표 stripes.skills_summon_dash)
    '#tabbar': ['Ui/TabBar.cs#tabbar-grad', 'Ui/TabBar.cs#tabbar-rim'],
    # T178 5회차 — 켜진 칸(그리고 ✕ 칸)의 노란 방사형 둘. `SurfaceArt` 가 방사형을 굽고 `RefreshTabX` 가 켠다.
    '#tabbar button.active, #tabbar button.tab-x': ['Ui/TabBar.cs#tab-glow', 'Ui/TabBar.cs#tab-footglow'],
    # T178 25회차 — 같은 선택자 `#tabbar button.active` 가 7920·8010 에도 따로 서 있다(각각 언더글로우 하나 · 플레이트+언더글로우). 셋이 다 `background-image` 라
    #   같은 특이도의 **마지막(8326)** 이 이기고 클론은 그 8326 짝을 굽는다(5회차) — 같은 두 자리다.
    '#tabbar button.active': ['Ui/TabBar.cs#tab-glow', 'Ui/TabBar.cs#tab-footglow'],
    # T178 25회차 — 장비 교체 착지 먼지(정본 7338 · 방사형 타원 · 표 `eqsw_dust`). `Dust(` 코루틴이 알약 마스크 안에 `SurfaceArt.FillMasked` 로 굽는다.
    '.eqsw-dust': ['Ui/EquipSwapFx.cs@Dust'],
    # T178 25회차 — 소환 결과 연출(`SummonFx.cs`)의 **표에만 없던 자리들**(24회차 교훈 «미정 ≠ 클론에 없다»). 각 굽는 몸의 요약이 정본 줄을 인용하고
    #   같은 그라디언트 문법을 푼다(T179·T419·T448·T449 가 세웠다) — 여기서는 그 짝을 표에 올렸을 뿐이다. 주역·동급 링은 `BakeRing` 한 몸을, 재점화·챕터 심지·잔상은 `BakeRadial` 한 몸을 나눠 쓴다(감싸개는 한 줄이라 굽는 호출이 없다).
    '.sr-wrap::before': ['Ui/SummonFx.cs@BakeVig'],
    '.sr-streaks i': ['Ui/SummonFx.cs@BakeStreak'],
    '.sr-floor::after': ['Ui/SummonFx.cs@BakeFloorTicks'],
    '.sr-canopy::before': ['Ui/SummonFx.cs@BakeArch'],
    '.sr-canopy::after': ['Ui/SummonFx.cs@BakeArch'],
    '.sr-canopy i': ['Ui/SummonFx.cs@BakeRayBar'],
    '.sr-canopy b': ['Ui/SummonFx.cs@BakeSpill'],
    '.sr-canopy b::after': ['Ui/SummonFx.cs@BakeSweep'],
    '.sr-rays': ['Ui/SummonFx.cs@BakeRays'],
    '#summon-result-modal.done .sr-rays': ['Ui/SummonFx.cs@BakeRays'],
    '.sr-stars i': ['Ui/SummonFx.cs@BakeStar'],
    '.sr-stars i::before, .sr-stars i::after': ['Ui/SummonFx.cs@BakeStar'],
    '.sr-stars i::after': ['Ui/SummonFx.cs@BakeStar'],
    '.sr-motes i': ['Ui/SummonFx.cs@BakeParticle'],
    '.sr-near i': ['Ui/SummonFx.cs@BakeParticle'],
    '.sr-charge': ['Ui/SummonFx.cs@BakeChargeBurst'],
    '.sr-wipe': ['Ui/SummonFx.cs@BakeWipe'],
    '.sr-relight': ['Ui/SummonFx.cs@BakeRadial'],
    '.sr-tierpulse': ['Ui/SummonFx.cs@BakeTierPulse'],
    # T178 28회차 — T480 이 세운 소환 결과 겹 셋 + T459 ⓩ 의 스윕(넷 다 이미 굽는 길인데 표에만 없었다 · 27회차 뒤 미정 61 → 57).
    '.sr-orbwrap::after': ['Ui/SummonFx.cs@BakeLandRing'],   # 6358 착지 링 방사(closest-side 62/78/92%) — 등급색마다 한 장
    '.sr-ray': ['Ui/SummonFx.cs@BakeRaySpokes'],             # 6680 방사 마스크(farthest-corner 18/40/62%)
    '.sr-cell.hi .sr-ray': ['Ui/SummonFx.cs@BakeRaySpokes'],  # 6685 conic 살(26°마다 4°) — 마스크와 한 장에 굽는다
    '#summon-result-modal.done .sr-cell.on .sr-orb::after': ['Ui/SummonFx.cs@BakeOrbSweep'],   # 6914 done 뒤 구슬 스페큘러 띠(112deg · T459 ⓩ)
    '.sr-tierflash::after': ['Ui/SummonFx.cs@BakeRadial'],
    '.sr-ghost': ['Ui/SummonFx.cs@BakeRadial'],
    '.sr-cell.heroic .sr-beam': ['Ui/SummonFx.cs@BakeBeam'],
    '#summon-result-modal.hero .sr-cell.heroic::after': ['Ui/SummonFx.cs@BakeRing'],
    '.sr-cell.peer.on::after': ['Ui/SummonFx.cs@BakeRing'],
    '.sr-reflect': ['Ui/SummonFx.cs@BakeReflectSprite'],
    '.sr-ico::after': ['Ui/SummonFx.cs@BakeOrbIconSpec'],
    '.rate-bar': ['Ui/SkillRatesPopup.cs#rate-enamel', 'Ui/SkillRatesPopup.cs#rate-rim'],
    # T178 17회차 — 서브탭 켜진 칸의 겹 둘(위 1 CSS px 흰 광택 + 세로 명암). 정본 7958 머리말이 짝은 «① 파란 면 플랫» 자리다.
    #   면 색(pp_blue)이 곁 표에서 오므로 한 판에 사슬로 굽는다(subtab_active_rim ← subtab_active_shade ← 부르는 쪽의 색).
    '#summon-subtabs.subtab-strip button.active': ['Ui/SkillPetSheet.cs#active-grad'],
    # T178 11회차 — 보스 경고 배너 **면**(180° 3정지점 · 가운데 45% 가 가장 밝은 핏빛). 클론은 `pp_red_dk` 단색 한 장이었다.
    '.bw-banner': ['Ui/BattleOverlay.cs#bw-banner-grad'],
    # T178 12회차 — 정본 `#game-area::after` 의 **상시 비네트**(3D 위 · 모든 연출 아래). 클론엔 통째로 없었다
    #   (T135 의 피격 `#dmg-flash` 는 다른 자리다 — 그쪽은 맞으면 켜졌다 꺼진다).
    '#game-area::after': ['Ui/BattleOverlay.cs#vignette-grad'],
    # T178 13회차 — 특가 카드 보상 알약의 «오목한 홈»(위 그늘 → 아래 빛 네 정지점). 클론은 단색 면 한 장이었다.
    '.shop-reward-pill': ['Ui/ShopSheet.cs#pill-grad'],
    # T178 12회차 — 부화장 램프 빛기둥. 이미 맞게 서 있었는데 표에 없어 «미정» 으로 남아 있었다:
    #   `PetHatchCone` 이 제 메시에 위→아래 두 색을 물려 그리고(정본 180° 두 정지점 + clip-path polygon),
    #   색·꼭짓점 비율은 `PetSkillUi.json` 이 쥔다(`cone_top` #ffeb50e6 ↔ 정본 rgba(255,235,80,.9) · `cone_top_f` .38 ↔ 38%).
    '.hatch-cone': ['Ui/PetPanel.cs#hatch-cone'],
    '.hatch-cone.dim': ['Ui/PetPanel.cs#hatch-cone'],
}

# ── 임자가 정해진 빈자리(자리 → 이유) — 닫을 때마다 지운다 ────────────────────────────────
KNOWN = {
}

# 굽는 길(결정 223)
BAKE = (r'CraftFxPoly\.Bake\w*|AgePattern\.Tile|RadialSprite|VigSprite|GradSprite|CraftCardArt\.Sheen|Sheen'
        r'|SurfaceArt\.\w+|UiKit\.Surface\w*|Sprite\.Create|Texture2D|NewTex')   # `new Texture2D(` = 제 손으로 굽는 자리 · `NewTex(` = SummonFx 의 텍스처 공장(Bake* 스무 자리가 다 이 길 · T178 25회차)
BAKE_CALL = re.compile(r'\b(?:' + BAKE + r')\s*\(')
# 겹을 **제 손으로** 굽는 공장(이 호출 자체가 곧 겹이다) — `Radial(warnRoot, "bw-dim", …)` · `GradFace(root, "bg", …)`
FACTORY = r'Radial|GradFace|SurfaceArt\.\w+|UiKit\.Surface\w*|PetHatchCone\.Add'
#   `PetHatchCone.Add` — 제 메시에 **위→아래 두 색**을 직접 물려 그리는 공장이다(스프라이트를 안 굽는다).
#   정본 `.hatch-cone` 이 `linear-gradient(180deg, …)` + `clip-path` 로 적어 둔 그 그림이라 «굽는 길» 과 같은 자리다(T178 12회차).
SHAPE_FACTORY = re.compile(r'^(?:' + FACTORY + r')$')
CREATE_CALL = re.compile(r'([\w.]+)\s*\(\s*[^,()]+,\s*"([^"]+)"')
ASSIGN_TAIL = re.compile(r'([\w\[\]\.]+)\s*=\s*(?:[\w!.()\[\]]+\s*\?\s*)?[\w.]*$')
GRAD_PROP = re.compile(r'(?<![\w-])(background(?:-image)?|(?:-webkit-)?mask-image|--[\w-]+)\s*:\s*([^;]+)')
GRAD_KIND = re.compile(r'(repeating-linear-gradient|repeating-radial-gradient|linear-gradient|radial-gradient|conic-gradient)\(')
KIND_CODE = {'linear-gradient': 'L', 'radial-gradient': 'R', 'repeating-linear-gradient': 'rL',
             'repeating-radial-gradient': 'rR', 'conic-gradient': 'C'}


def bake_on(var):
    """그 변수에 굽는 호출이 닿는가 — 첫 인자로(`Bake(tl, …)`) 또는 그 변수의 그림 칸에 대입으로(`tl.sprite = Bake(…)` · `tl.texture = AgePattern.Tile(…)`)."""
    v = re.escape(var)
    return re.compile(r'\b(?:' + BAKE + r')\s*\(\s*' + v + r'\s*[,)]'
                      r'|' + v + r'\s*\.\s*(?:sprite|texture|overrideSprite|material)\s*=\s*(?:[\w.]+\s*\?\s*)?(?:' + BAKE + r')\s*\(')


# ── 정본 CSS ──────────────────────────────────────────────────────────────────────────────
def _blank_comments(css):
    def rep(m):
        return ''.join('\n' if ch == '\n' else ' ' for ch in m.group(0))
    return re.sub(r'/\*.*?\*/', rep, css, flags=re.S)


def parse_rules(css_text, offs=None):
    """[(줄, 선택자, 속성, 겹 코드)] — 값에 `…gradient(` 가 든 선언 전부(background · background-image · mask-image · 사용자 속성).
    겹 코드 = 겹마다 한 글자(L 선형 · R 방사 · rL/rR 반복 · C 원뿔) 를 이어 붙인 것 — «몇 겹인가» 가 한눈에 보인다.
    `offs`(dict) 를 주면 **같은 선택자에 뒤에 와서 그 겹을 끄는 선언**을 모아 준다 — `background-image: none` · `background: <단색>`(줄임은 image 를 되돌린다)
    · `mask-image: none` — {선택자: (끄는 줄, 속성, 값 머리)}. cascade 는 마지막 선언이 이긴다(T178 39회차 · 정본 8776~8820 «aaa-skin ⓖ» 가
    7992·7953·2044·2047 을 none/단색으로 끄는데 자가 못 봐 정본이 끈 겹을 세웠다)."""
    css = _blank_comments(css_text)
    out = []
    last_grad = {}        # (선택자, 속성군) → 마지막 겹 선언 줄
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        sel = ' '.join(m.group(1).split())
        lead = len(m.group(1)) - len(m.group(1).lstrip())
        line = css[:m.start(1) + lead].count('\n') + 1
        for d in GRAD_PROP.finditer(m.group(2)):
            prop, val = d.group(1), d.group(2)
            fam = 'mask' if 'mask' in prop else ('bg' if prop.startswith('background') else prop)
            if 'gradient(' in val:
                kinds = ''.join(KIND_CODE[k] for k in GRAD_KIND.findall(val))
                out.append((line, sel, prop, kinds))
                last_grad[(sel, fam)] = line
                if offs is not None:
                    offs.pop(sel, None)       # 뒤에 다시 겹을 켰다
                continue
            if offs is None or (sel, fam) not in last_grad or prop.startswith('--'):
                continue
            if line > last_grad[(sel, fam)]:
                offs[sel] = (line, prop, val.strip()[:24])
    return out


# ── 클론 ──────────────────────────────────────────────────────────────────────────────────
def _read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def _method_body(src, name):
    # T178 25회차 — `return BakeRing(…);` 같은 **호출문**도 «낱말 공백 이름(» 꼴이라 첫 후보로 잡혔고, 그 뒤의 `{` 는 **다음 메서드의 몸**이었다
    #   (`@BakeRing`·`@BakeRadial` 이 «겹 없음» 으로 울었다). 후보와 `{` 사이에 `;` 가 있으면 호출문이다 — 정의가 나올 때까지 넘긴다.
    for m in re.finditer(r'\b(?:static\s+)?[\w<>\[\],\s]+\s' + re.escape(name) + r'\s*\(', src):
        i = src.find('{', m.end())
        if i < 0:
            return None
        if ';' in src[m.end():i]:
            continue
        depth = 0
        for j in range(i, len(src)):
            if src[j] == '{':
                depth += 1
            elif src[j] == '}':
                depth -= 1
                if depth == 0:
                    return src[i:j + 1]
        return src[i:]
    return None


def check_target(game_dir, target):
    """(상태, 설명) — 'ok' | 'missing'(자리는 있는데 색 한 칸) | 'absent'(자리·메서드·파일 없음)."""
    file_part, sep, tail = re.match(r'([^#@]+)([#@]?)(.*)', target).groups()
    path = os.path.join(game_dir, file_part)
    if not os.path.isfile(path):
        return 'absent', '파일 없음 ' + file_part
    src = _read(path)
    if sep == '':
        return ('ok' if BAKE_CALL.search(src) else 'missing'), '파일 전체'
    if sep == '@':
        body = _method_body(src, tail)
        if body is None:
            return 'absent', '메서드 없음 ' + tail + '('
        return ('ok' if BAKE_CALL.search(body) else 'missing'), '메서드 ' + tail + '( 본문'
    found = False
    for m in CREATE_CALL.finditer(src):
        if m.group(2) != tail:
            continue
        found = True
        if SHAPE_FACTORY.match(m.group(1)):
            return 'ok', '"%s" 를 %s 로 굽는다' % (tail, m.group(1))
        head = src[max(0, m.start() - 160):m.start()].replace('\n', ' ')
        a = ASSIGN_TAIL.search(head)
        if a and bake_on(a.group(1)).search(src):
            return 'ok', '"%s" → %s 에 굽는 호출이 닿는다' % (tail, a.group(1))
    if not found:
        return 'absent', '"%s" 이름으로 만드는 자리가 없다' % tail
    return 'missing', '"%s" 는 굽는 호출이 안 닿는다 — 색 한 칸짜리 Image 다' % tail


# ── 대조 ──────────────────────────────────────────────────────────────────────────────────
def run(css_path, game_dir, table, known, out=print, list_pending=False):
    if not os.path.isfile(css_path):
        out('✗ 정본 CSS 를 못 읽었다: %s (git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)' % css_path)
        return 2
    offs = {}
    rules = parse_rules(_read(css_path), offs)
    problems = 0
    seen = set()
    n_off = n_ok = n_known = n_stripe = 0
    # ⓛ 정본이 **나중 선언으로 끈** 선택자(cascade)에 표가 굽는 자리를 적어 두면 «정본이 끈 겹을 세운 것» 이다 — 막는다(T178 39회차).
    for sel in sorted(offs):
        if sel in table and not isinstance(table[sel], str):
            problems += 1
            oline, oprop, oval = offs[sel]
            out('✗ 정본이 나중에 끈다  %s  ← style.css %d { %s: %s }  — 마지막 선언이 이긴다 · 클론 겹을 걷고 표는 «—» 로 적어라' % (sel, oline, oprop, oval))
    pending = []          # (줄, 선택자, 속성, 겹) — 표에 없는 정본 선언
    known_now_ok = []
    checked = set()       # (선택자, 자리) — 같은 선택자의 선언이 여럿이어도 자리는 한 번만 센다
    for line, sel, prop, kinds in rules:
        seen.add(sel)
        if sel not in table:
            pending.append((line, sel, prop, kinds))
            continue
        targets = table[sel]
        if isinstance(targets, str):
            if (sel, targets) not in checked:
                checked.add((sel, targets))
                # «—띠 …» 는 **하드 스톱**(정지점 사이 섞임 없음) 자리다 — 굽는 게 아니라 조각(rect·dash)으로 그리는 것이 정답이라
                # «끄는 규칙» 과 따로 센다(T178 7회차). 그냥 «—» 는 클론에 자리가 없거나 정본이 그 겹을 끄는 규칙이다.
                if targets.startswith('—띠'):
                    n_stripe += 1
                else:
                    n_off += 1
            continue
        for t in targets:
            if (sel, t) in checked:
                continue
            checked.add((sel, t))
            state, why = check_target(game_dir, t)
            if state == 'ok':
                n_ok += 1
                if t in known:
                    known_now_ok.append(t)
                continue
            tag = '겹 없음' if state == 'missing' else '자리 없음'
            if t in known:
                n_known += 1
                out('· KNOWN(%s)  %s  ← style.css %d %s { %s %s }  — %s' % (tag, t, line, sel, prop, kinds, known[t]))
            else:
                problems += 1
                out('✗ %s  %s  ← style.css %d  %s  { %s %s }  — %s' % (tag, t, line, sel, prop, kinds, why))
    for sel in table:
        if sel not in seen:
            problems += 1
            out('✗ 표에는 있는데 정본에 없는 선택자: %s  → 정본이 바뀌었다 · TABLE 에서 지우거나 고쳐라' % sel)
    for t in sorted(set(known_now_ok)):
        out('· KNOWN 인데 이제 겹이 있다: %s  → KNOWN 에서 지워라' % t)
    pend_sel = sorted(set(p[1] for p in pending))
    if list_pending:
        for line, sel, prop, kinds in pending:
            out('· 미정  style.css %5d  %-64s %-18s %s%s' % (line, sel[:64], prop, kinds, ('  ⟵ 정본이 %d 에서 끈다(%s: %s)' % offs[sel]) if sel in offs else ''))
    out('%s check_surface_gradients: 정본 겹 선언 %d(선택자 %d) · 정본이 나중에 끈 선택자 %d · 끄는 규칙 %d · 띠(조각으로 그린다) %d · 자리 초록 %d · KNOWN 빈자리 %d · 미정 선택자 %d(선언 %d · --list 로 본다) · 문제 %d'
        % ('✓' if problems == 0 else '✗', len(rules), len(set(r[1] for r in rules)), len(offs), n_off, n_stripe, n_ok, n_known, len(pend_sel), len(pending), problems))
    return 0 if problems == 0 else 1


# ── 자기 검사(고장 주입) ────────────────────────────────────────────────────────────────────
def self_test():
    fails = []
    tmp = tempfile.mkdtemp(prefix='sgrad_')
    css = os.path.join(tmp, 'style.css')
    game = os.path.join(tmp, 'Game')
    os.makedirs(os.path.join(game, 'Ui'))

    def w(path, text):
        with open(path, 'w', encoding='utf-8') as f:
            f.write(text)

    def cs(text):
        w(os.path.join(game, 'Ui', 'Face.cs'), text)

    def go(table, known, css_text=None, list_pending=False):
        if css_text is not None:
            w(css, css_text)
        lines = []
        rc = run(css, game, table, known, out=lines.append, list_pending=list_pending)
        return rc, '\n'.join(lines)

    def expect(name, cond, detail=''):
        if not cond:
            fails.append(name + (' — ' + detail if detail else ''))

    base_css = ('/* .fake-comment { background: linear-gradient(red, blue) } */\n'
                '.g-a { background: linear-gradient(180deg, rgba(255,255,255,.6) 0 1px, transparent 1px), '
                'repeating-linear-gradient(45deg, #000 0 2px, #fff 2px 9px); }\n'
                '.g-b { background-image: radial-gradient(circle, #fff, #000); }\n'
                '.g-c::before { -webkit-mask-image: linear-gradient(#000, transparent); mask-image: linear-gradient(#000, transparent); }\n'
                '.g-d { --af-pat: repeating-radial-gradient(circle, #000 0 1px, #fff 1px 3px); }\n'
                '.g-e { background: #fff; }\n'
                '.g-f, .g-g { background: conic-gradient(from 0deg, #000, #fff); }\n')
    # ⓐ 파싱 — 주석 속 선언은 빼고 · 속성 넷(background · background-image · mask-image 둘 · --af-pat) · 겹 코드
    rules = parse_rules(base_css)
    expect('ⓐ 선언 수', len(rules) == 6, str(len(rules)))
    expect('ⓐ 주석 제외', all(r[1] != '.fake-comment' for r in rules))
    expect('ⓐ 겹 코드', [r[3] for r in rules] == ['LrL', 'R', 'L', 'L', 'rR', 'C'], str([r[3] for r in rules]))
    expect('ⓐ 쉼표 선택자 통째', any(r[1] == '.g-f, .g-g' for r in rules))
    expect('ⓐ 줄 번호', rules[0][0] == 2, str(rules[0][0]))
    # ⓑ 자리에 겹이 있다 — #이름(공장) · #이름(변수에 닿음) · @메서드 · 파일 전체
    cs('class Face { static void B(Transform p){ Image a = Radial(p, "g-a", "k", null); '
       'Image b = UiKit.Panel(p, "g-b", "c"); b.sprite = CraftFxPoly.Bake("x", pts); '
       'RawImage c = UiKit.Raw(p, "g-c", "c"); c.texture = AgePattern.Tile(age, 0); }\n'
       ' static Sprite M(){ return Sprite.Create(t, r, v); } }')
    table = {'.g-a': ['Ui/Face.cs#g-a'], '.g-b': ['Ui/Face.cs#g-b'], '.g-c::before': ['Ui/Face.cs#g-c'],
             '.g-d': ['Ui/Face.cs@M'], '.g-f, .g-g': ['Ui/Face.cs']}
    rc, out = go(table, {}, base_css)
    expect('ⓑ 겹 있는 자리 rc 0', rc == 0, out)
    expect('ⓑ 자리 초록 5', '자리 초록 5' in out, out)
    expect('ⓑ 미정 0', '미정 선택자 0' in out, out)
    # ⓒ 표에 없는 정본 선택자 = 미정(막지 않는다 · --list 로 보인다)
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a']}, {}, list_pending=True)
    expect('ⓒ 미정은 rc 0', rc == 0, out)
    expect('ⓒ 미정 선택자 4', '미정 선택자 4(선언 5' in out, out)
    expect('ⓒ --list 줄', '· 미정  style.css' in out and '.g-d' in out, out)
    # ⓓ 표의 자리에 겹이 없다(색 한 칸) → rc 1 · KNOWN 이면 rc 0 · KNOWN 인데 닫혔으면 알림
    cs('class Face { static void B(Transform p){ Image a = UiKit.Panel(p, "g-a", "c"); } }')
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a']}, {})
    expect('ⓓ 겹 없음 rc 1', rc == 1 and '겹 없음' in out, out)
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a']}, {'Ui/Face.cs#g-a': 'T999 몫'})
    expect('ⓓ KNOWN rc 0', rc == 0 and 'KNOWN(겹 없음)' in out, out)
    cs('class Face { static void B(Transform p){ Image a = Radial(p, "g-a", "k", null); } }')
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a']}, {'Ui/Face.cs#g-a': 'T999 몫'})
    expect('ⓓ KNOWN 닫힘 알림', rc == 0 and 'KNOWN 인데 이제 겹이 있다' in out, out)
    # ⓔ 자리 없음(이름·메서드·파일) → rc 1
    rc, out = go({'.g-a': ['Ui/Face.cs#nope'], '.g-b': ['Ui/Face.cs@Nope'], '.g-d': ['Ui/Gone.cs']}, {})
    expect('ⓔ 자리 없음 rc 1', rc == 1 and out.count('자리 없음') == 3, out)
    # ⓕ 표에는 있는데 정본에 없는 선택자 → rc 1
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a'], '.g-zzz': ['Ui/Face.cs#g-a']}, {})
    expect('ⓕ 정본에 없는 선택자 rc 1', rc == 1 and '정본에 없는 선택자' in out, out)
    # ⓖ 끄는 규칙('—') 은 대조하지 않는다 · 같은 선택자의 선언이 여럿이어도 자리는 한 번만 센다
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a'], '.g-c::before': '—클론에 자리 없음'}, {})
    expect('ⓖ 끄는 규칙', rc == 0 and '끄는 규칙 1' in out and '자리 초록 1' in out, out)
    # ⓗ CSS 없음 → rc 2
    rc = run(os.path.join(tmp, 'none.css'), game, {}, {}, out=lambda s: None)
    expect('ⓗ CSS 없음 rc 2', rc == 2)
    # ⓘ 진짜 표(TABLE·KNOWN)의 자리 문법이 전부 유효한가
    for sel, ts in TABLE.items():
        if isinstance(ts, str):
            expect('ⓘ 끄는 규칙 문법 ' + sel, ts.startswith('—'))
            continue
        for t in ts:
            expect('ⓘ 자리 문법 ' + t, re.match(r'^[\w/]+\.cs([#@][\w-]+)?$', t) is not None)
    # ⓙ «—띠» 는 끄는 규칙과 따로 센다(T178 7회차 · 하드 스톱 자리는 굽지 않고 조각으로 그린다)
    cs('class Face { static void B(Transform p){ Image a = Radial(p, "g-a", "k", null); } }')
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a'], '.g-c::before': '—띠 조각으로 그린다'}, {}, base_css)
    expect('ⓙ 띠 따로 셈', rc == 0 and '끄는 규칙 0 · 띠(조각으로 그린다) 1' in out, out)
    expect('ⓙ 띠 자리 초록 1', '자리 초록 1' in out, out)

    # ⓛ «정본이 나중에 끈다»(cascade · T178 39회차) — 같은 선택자에 뒤에 온 `background-image: none` / `background: <단색>` / `mask-image: none` 이
    #   앞의 겹을 끈다. 표가 그 선택자에 굽는 자리를 적어 두면 «정본이 끈 겹을 세운 것» 이라 rc 1 · «—» 로 적으면 끄는 규칙으로 센다 ·
    #   뒤에 다시 겹을 켜면 끈 것이 아니다 · 미정 목록(--list)엔 «⟵ 정본이 N 에서 끈다» 가 붙는다.
    off_css = (base_css +
               '.g-a { background-image: none; }\n'
               '.g-b { background: #123; }\n'
               '.g-c::before { mask-image: none; }\n'
               '.g-f, .g-g { background: #fff; }\n'
               '.g-f, .g-g { background: linear-gradient(#000, #fff); }\n')
    cs('class Face { static void B(Transform p){ Image a = Radial(p, "g-a", "k", null); Image b = Radial(p, "g-b", "k", null); } }')
    rc, out = go({'.g-a': ['Ui/Face.cs#g-a'], '.g-b': ['Ui/Face.cs#g-b'], '.g-f, .g-g': ['Ui/Face.cs#g-a']}, {}, off_css)
    expect('ⓛ 끈 선택자 rc 1', rc == 1 and out.count('정본이 나중에 끈다') == 2, out)
    expect('ⓛ 끈 선택자 셈 3(a·b·c — f,g 는 다시 켰다)', '정본이 나중에 끈 선택자 3' in out, out)
    rc, out = go({'.g-a': '—정본이 끈다', '.g-b': '—정본이 끈다', '.g-f, .g-g': ['Ui/Face.cs#g-a']}, {}, off_css, list_pending=True)
    expect('ⓛ «—» 로 적으면 rc 0', rc == 0 and '끄는 규칙 2' in out and '자리 초록 1' in out, out)
    expect('ⓛ 미정 목록에 끈 자리 표식', '⟵ 정본이' in out and '.g-c::before' in out, out)
    cs('class Face { static void B(Transform p){ Image a = Radial(p, "g-a", "k", null); } }')
    w(css, base_css)

    # ⓚ «@메서드» — 같은 이름의 **호출문**(`return M(`)이 정의보다 먼저 나와도 정의의 몸을 읽는다(T178 25회차 · BakeRing/BakeRadial 갈래)
    cs('class Face { static Sprite W(){ return M(); }\n static Sprite M(){ return Sprite.Create(t, r, v); } }')
    rc, out = go({'.g-d': ['Ui/Face.cs@M']}, {}, base_css)
    expect('ⓚ 호출문 뒤의 정의', rc == 0 and '자리 초록 1' in out, out)
    cs('class Face { static Sprite W(){ return M(); }\n static Sprite M(){ return Plain(); } }')
    rc, out = go({'.g-d': ['Ui/Face.cs@M']}, {}, base_css)
    expect('ⓚ 정의에 굽는 호출 없음 rc 1', rc == 1 and '겹 없음' in out, out)

    if fails:
        print('✗ check_surface_gradients --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f[:400])
        return 1
    print('✓ check_surface_gradients --self-test 22칸 통과')
    return 0


def main(argv):
    css, game, list_pending = CSS_DEFAULT, GAME_DEFAULT, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_pending = True; i += 1; continue
        if a == '--css' and i + 1 < len(argv):
            css = argv[i + 1]; i += 2; continue
        if a == '--game' and i + 1 < len(argv):
            game = argv[i + 1]; i += 2; continue
        print('사용: check_surface_gradients.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]')
        return 2
    return run(css, game, TABLE, KNOWN, list_pending=list_pending)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
