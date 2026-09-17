#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T377 — 정본이 **선택자에만 리터럴 hex 로 못박은 면 색**(전역 토큰과 *다른* 값) ↔ 클론의 그 자리가 **전용 키**로 같은 값을 쓰는가.

왜 이 자가 있나(검수 Q · 런 641 실측): 정본 `style.css` 8692 는 «전역 `--pp-red`/`--pp-red-dk` 는 안 건드린다 — 화면마다 빨강이
다르니 토큰을 옮기지 말 것 … 그래서 **버튼 규칙에만 리터럴**로 준다» 고 적어 뒀는데, 클론은 판매 버튼·채팅 뒤로 버튼을 전역
토큰 `pp_red`(#e8362f)로 찍어 정본 `#ff1017` 과 어긋났다. 리터럴 자리는 «토큰으로 뭉개지 말라» 는 뜻이므로 자리마다
**전용 키**(`catalog.json` 또는 곁 표 `Resources/PinnedColorUi.json`)로 같은 값을 준다.

무엇을 세나: 주석을 걷은 `style.css` 에서 `background`/`background-color` 가 **리터럴 hex 로 시작하는** 선언(면 색) 전부 —
그중 `--토큰: #hex` 로 정의된 값과 **같은** 것은 뺀다(토큰을 쓰는 게 맞는 자리). 남는 것이 «못박은 면 색» 이다.

선택자 ↔ 클론 자리 짝은 TABLE 하나가 쥔다(T109 `check_keyline` · T365 `check_box_borders` 꼴):
  "Ui/File.cs#name|<source>:<key>"   그 파일에 «"name"» 이 있고 «"key"» 호출이 있으며, <source> 의 key 값이 정본 hex 와 같다
  "Ui/File.cs@Method|<source>:<key>" 도우미 메서드 본문에 «"key"» 호출이 있고 값이 같다
  "Ui/File.cs|<source>:<key>"        파일 어디든 «"key"» 호출이 있고 값이 같다
  "—<이유>"                           대조하지 않는다(정본이 그 자리를 안 그리는 등)
  <source> = catalog(`Assets/Forge/catalog.json` 의 colors) · res:<Name>(`Assets/Forge/Resources/<Name>.json` 안 어디든 "key": "#hex")
표에 없는 선택자 = **미정**(막지 않는다 · `--list` 로 본다 · 회차마다 표에 더한다). 빈자리 중 임자가 정해진 것은 KNOWN 에 둔다.

사용:  python3 tools/check_pinned_colors.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]
rc:    0 = 표의 선택자가 전부 정본에 있고 · 표의 자리 전부가 (호출 + 같은 값 이거나 KNOWN) · 1 = 아니다 · 2 = 정본 CSS 를 못 읽었다
"""
import json
import os
import re
import sys
import tempfile

CSS_DEFAULT = os.path.join('.wwwww-src', 'web', 'css', 'style.css')
GAME_DEFAULT = os.path.join('Assets', 'Scripts', 'Game')
CATALOG_DEFAULT = os.path.join('Assets', 'Forge', 'catalog.json')
RES_DEFAULT = os.path.join('Assets', 'Forge', 'Resources')

# ── 정본 선택자 ↔ 클론 자리 ──────────────────────────────────────────────────────────────────────
TABLE = {
    # T377 1회차 — 채팅 뒤로 버튼(정본 3283 `#ff1017` · 클론은 pp_red 였다)
    '.chat-input-bar .btn.danger.round': ['Ui/ChatScreen.cs#face|res:PinnedColorUi:chat_back_face'],
    # 이미 맞던 자리 둘(등재문이 «같이 깨뜨리지 마라» 고 적은 것) — 자가 그 값을 지킨다
    '.league-reward-banner': ['Ui/LeagueSheet.cs|catalog:lgr_ribbon'],
    '.petd-wrap .petd-btn.danger': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:pp_red'],   # 5435 · PetSkillUi pp_red = #f2191d(전역 pp_red 와 다른 값 — 이미 맞다)
    # T377 3회차 — 판매 버튼 둘(비교 팝업 · 판매 경고)의 면(8686 `#ff1017` · 턱 `#4e0507` 은 box-shadow 라 이 자의 «면» 목록엔 없다 — PlayMode 자가 잰다)
    '.btn.btn.danger.danger': ['Ui/ForgeCraftPopup.cs@PinSell|res:PinnedColorUi:sell_btn_face'],
    '.btn.btn.sell.sell': ['Ui/ForgeCraftPopup.cs@PinSell|res:PinnedColorUi:sell_btn_face'],
    # T377 4회차 — 산 lock 밖 자리 셋. 하나는 토큰으로 뭉개져 있었고(모루 «보류» 배지) 둘은 이미 제 값이라 자가 지키기만 한다.
    '.anvil-btn.held-slot .held-tag': ['Ui/ForgeSheet.cs|res:PinnedColorUi:held_tag_face'],   # 정본 999 #f0a020 ↔ 클론은 전역 coin(#ffd54f) 이었다
    '.skill-btn.auto': ['Ui/SkillBar.cs|res:PetSkillUi:sb_auto_bg'],                          # 정본 624 #2f3a33 ↔ PetSkillUi sb_auto_bg 같은 값(이미 맞다)
    '.skill-btn.auto.on': ['Ui/SkillBar.cs|res:PetSkillUi:sb_auto_on_bg'],                    # 정본 629 #2e7d32 ↔ sb_auto_on_bg 같은 값(이미 맞다)
    # T377 5회차 — 파일이 열린 자리 둘. 둘 다 **이미 제 값**이라 자가 지키기만 한다(값이 바뀌면 여기서 빨강이 난다).
    # T377 7회차 — 산 lock 밖 파일(Hud·TabBar·QuestSheet) 의 «못박은 면» 여섯. **여섯 다 이미 제 값**이라
    #   고칠 코드가 0 이고, 자가 그 값을 지키기만 한다(값이 바뀌면 여기서 빨강이 난다 · 6회차와 같은 꼴).
    '#topbar': ['Ui/Hud.cs@Build|catalog:topbar_bg'],                       # 정본 50 #1c2128(7900 의 같은 선택자는 `background-image` 만 얹는다 — 면 색은 여기가 정본이다)
    '.profile-card': ['Ui/Hud.cs@Build|catalog:card_bg'],                   # 정본 90 #22272e
    '.currency-pills .pill': ['Ui/Hud.cs@Pill|catalog:card_bg'],            # 정본 114 #22272e(같은 값이지만 정본이 따로 적은 자리다)
    '.pip.boss.now': ['Ui/Hud.cs@SetWaves|catalog:pip_boss'],               # 정본 196 #ff5252
    '#tabbar': ['Ui/TabBar.cs|catalog:tabbar_bg'],                          # 정본 1693 #0e111b(주석에 원본 실측 rgb(14,17,27) 이 적혀 있다)
    '.qst-row.done': ['Ui/QuestSheet.cs|catalog:quest_done_bg'],            # 정본 2030 #f2fff2
    # T377 8회차 — 기술 트리 노드 원반 **여섯 상태**(정본 2182·2188·2190·2197·2198·2201). 클론 `TechPanel.Node` 가
    #   상태마다 카탈로그 키를 고르고 값은 **여섯 다 이미 정본 그대로**다 — 자가 지키기만 한다.
    #   ⚠ 정본 주석이 두 자리에 «여기서 틀렸었다» 를 남겨 뒀다: 2195 «클론 #c9925a 는 주황기가 빠진 탠색이었다» ·
    #     2199 «연구 중 노드를 통째로 초록으로 칠하면 안 된다 — 초록은 아래 시간 배지 뿐이다». 값이 밀리면 여기서 빨강이 난다.
    '.tech-tree-node': ['Ui/TechPanel.cs@Node|catalog:tech_node'],                 # 2182 #c85c34
    '.tech-tree-node.active': ['Ui/TechPanel.cs@Node|catalog:tech_node'],          # 2197 #c85c34(같은 값이지만 정본이 따로 적은 자리)
    '.tech-tree-node.researching': ['Ui/TechPanel.cs@Node|catalog:tech_node'],     # 2201 #c85c34
    '.tech-tree-node.locked': ['Ui/TechPanel.cs@Node|catalog:tech_locked'],        # 2188 #f4f2ee
    '.tech-tree-node.tlocked': ['Ui/TechPanel.cs@Node|catalog:tech_tlocked'],      # 2190 #d9d6d0
    '.tech-tree-node.done': ['Ui/TechPanel.cs@Node|catalog:tech_done'],            # 2198 #a9793f
    # T377 8회차 — 산 lock 밖 파일 둘 더(둘 다 곁 표 `PetSkillUi.json` 이 정본 값을 그대로 쥐고 있다).
    '#summon-subtabs.subtab-strip': ['Ui/SkillPetSheet.cs|res:PetSkillUi:subtab_bg'],   # 3593 #afafaf(실버 패널 · 그 안의 검은 알약은 subtab_pill)
    '.mat-chip.on': ['Ui/MountUpgradePopup.cs|res:PetSkillUi:mtup_chip_on'],            # 810 #253326 · 바탕 `.mat-chip`(806)은 3527 이 흰 면으로 덮는 «덮개» 자리다
    # T377 9회차 — 산 lock 밖 파일 둘(프로필·공용 팝업)의 «못박은 면» 넷. 넷 다 **이미 제 값**이라 고칠 코드 0줄이다.
    '.avatar-pick-btn.on': ['Ui/ProfilePopup.cs|catalog:avatar_pick_on'],   # 3066 #dbe9ff
    '.settings-list': ['Ui/ProfilePopup.cs|catalog:settings_even'],         # 3094 #eee — 짝수 행이 목록 바탕색을 그대로 쓴다(정본도 그렇다)
    '.settings-row:nth-child(odd)': ['Ui/ProfilePopup.cs|catalog:settings_odd'],   # 3104 #ccc(정본 주석: 원본 shot-042744 는 rgb(204)/rgb(238) 교대)
    '.x-btn': ['Ui/Popups.cs@XButton|catalog:tabx_bg'],                     # 3739 #f2191d
    '.dgd-btn.silver': ['Ui/DungeonPopups.cs|catalog:dgd_btn'],         # 정본 5356 #a3a3a3(원본 픽셀 실측 주석) ↔ catalog dgd_btn
    '.rw-pop': ['Ui/RewardBurst.cs|res:RewardBurstUi:pop'],             # 정본 7511 #ffd54f(도착 마침표 별) ↔ RewardBurstUi colors.pop
    # T377 6회차 — 펫 업그레이드 팝업 다섯(파일이 열렸다). 다섯 다 곁 표 PetSkillUi 가 정본 리터럴을 그대로 쥐고 있어 «지키는 자» 만 붙인다.
    '.petup-panel': ['Ui/PetUpgradePopup.cs|res:PetSkillUi:petup_panel'],                      # 정본 4347 #b7b7b7(회색 판)
    '.petup-xpbar': ['Ui/PetUpgradePopup.cs|res:PetSkillUi:xpbar_bg'],                         # 정본 4350 #0d111b(검정 알약)
    '.petup-divider': ['Ui/PetUpgradePopup.cs|res:PetSkillUi:divider'],                        # 정본 4385 #dcdcdc(2px 가름줄)
    '.petup-selrow .btn.silver': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:silver'],      # 정본 5490 #a3a3a3 — 공용 종이 버튼의 Silver 갈래가 그 값을 쥔다
    '.petup-selrow .btn.silver.disabled': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:silver_disabled'],   # 정본 5495 #b4b4b4(비활성은 면을 따로 준다 · opacity 로 뭉개지 않는다)
    # 같은 회색 판 둘(정본이 두 자리에 같은 리터럴을 준다) — 클론도 한 키로 쓴다
    '.passive-banner': ['Ui/SkillPanel.cs|res:PetSkillUi:passive_bg'],   # 정본 3998 #c9c9c9
    '.skd-passive': ['Ui/SkillPanel.cs|res:PetSkillUi:passive_bg'],      # 정본 5252 #c9c9c9(스킬 상세의 같은 판)
    # T377 10회차 ⓐ — 채팅 입력 밴드. **토큰으로 뭉개진 자리**(고친 자리다 · 나머지는 지키는 자리다):
    #   정본 3438 `#0e111b` + 3439 주석 «카드가 흰색이 됐으므로 밴드는 **자기 배경 #0e111b 를 직접 갖는다**» ↔ 클론은 전역 `pp_paper`(#ffffff)였다.
    '.chat-input-bar': ['Ui/ChatScreen.cs#input-bar|res:PinnedColorUi:chat_bar_face'],
    '.cmp-lower': ['Ui/ForgeCraftPopup.cs#face|res:PinnedColorUi:cmp_lower_face'],
    # T377 11회차 — 8회차가 «채팅 4» 로 남긴 묶음. **셋 다 어긋나 있었고 원작 샷으로 화소까지 쟀다**(shot-043500 499×804):
    #   #cecece 100,343화소 · #39ab36 12,308화소 ↔ 클론 값 #f0f0f0 362 · #35c04f **0** · #8a8a8a 375 · #dbe9ff **0**.
    # T377 12회차 ⓐ — 자동 제련 **필터 토글**. 정본은 토글을 두 벌 쥔다(설정 3107 은 토큰 · 이 쪽 4759 는 **못박은 값**)인데
    #   클론은 공용 `PopupKit.Toggle` 한 벌로 그려 필터 토글이 설정 팔레트(`pp_gray`/`pp_blue`)로 찍히고 있었다 — 켜짐이 초록이 아니라 파랑이었다.
    # T377 13회차 — 던전 목록의 **잠긴** 배너 [열기] 칩. 정본 8149 주석이 까닭을 적어 뒀다 —
    #   «배너 일러스트 위에서 «유령»으로 읽히던 **회백**을 확실한 비활성 칩으로». 클론은 공용 `Skin.Gray`(pp_gray #c4c4c4)를
    #   그대로 써서 정본이 이미 고친 그 회백을 다시 밟고 있었다(턱 `pp_gray_dk` #9a9a9a 는 면보다 **밝아** 뒤집혀 있었다).
    #   ⚠ 같은 규칙의 글자 `#3d434a` 는 **T396(잉크 축) 몫**이라 그 표엔 안 올렸다 — 값·배선은 이 회차가 해 뒀다(반만 옮기면 #878e96 위 #7b7b7b 라 안 읽힌다).
    '.modal-card.sheet .dg-banner .btn.disabled': ['Ui/DungeonPopups.cs@Pill|catalog:dg_lock_open'],   # 8151 #878e96
    '.af-toggle': ['Ui/ForgeAutoPopup.cs#af-toggle|catalog:af_toggle'],          # 4761 #1e2a4a(꺼진 트랙)
    '.af-toggle.on': ['Ui/ForgeAutoPopup.cs#af-toggle|catalog:af_toggle_on'],    # 4768 #35d435(켜진 트랙 · 4994 글로우도 같은 rgb)
    # T377 12회차 ⓑ — 패스 묶음 열둘(8회차가 «패스 11» 로 적어 둔 것 · `PassPopup.cs` 가 열렸다). **열둘 다 이미 제 값**이라 자가 지키기만 한다.
    '.modal-card.pass-card': ['Ui/PassPopup.cs@Render|catalog:pass_bg'],                       # 2645 #0e111b
    '.pass-banner': ['Ui/PassPopup.cs@Render|catalog:pass_banner'],                            # 2700 #341cff
    '.pass-banner::before': ['Ui/PassPopup.cs@Render|catalog:pass_banner_dk'],                 # 2713 #2112a0(리본 꼬리 둘)
    '.pass-banner::after': ['Ui/PassPopup.cs@Render|catalog:pass_banner_dk'],                  # 2713 #2112a0
    '.pass-price::before': ['Ui/PassPopup.cs@Render|catalog:pass_price'],                      # 2745 #ff9a00(가격 페넌트)
    '.pass-header-row span:last-child': ['Ui/PassPopup.cs@Render|catalog:pass_tab_prem'],      # 2764 #f2a93c(프리미엄 탭)
    '.pass-seg.reached .pass-milestone-label': ['Ui/PassPopup.cs@Render|catalog:pass_label_lit'],   # 2810 #2c3684(닿은 마일스톤 필)
    '.pass-cell': ['Ui/PassPopup.cs@Render|catalog:pass_cell'],                                 # 2815 #0e111b
    '.pass-cell span:not(.pass-badge)': ['Ui/PassPopup.cs@Render|catalog:pass_pill'],           # 2826 #1b2032(칸 안 보상 알약)
    '.pass-cell.free.lit': ['Ui/PassPopup.cs@Render|catalog:pass_cell_lit'],                    # 2833 #afafaf
    '.pass-cell.free.lit span:not(.pass-badge)': ['Ui/PassPopup.cs@Render|catalog:pass_pill_lit'],  # 2838 #8c8c8c
    '.pass-cell.free.done': ['Ui/PassPopup.cs@Render|catalog:pass_cell_lit'],                   # 2840 #afafaf(같은 값이지만 정본이 따로 적은 자리)
    '.pass-cell.free.done span:not(.pass-badge)': ['Ui/PassPopup.cs@Render|catalog:pass_pill_lit'],  # 2841 #8c8c8c
    '.chat-bubble': ['Ui/ChatScreen.cs#bubble|catalog:chat_bubble'],               # 3372 #cecece — 정본은 말풍선 면을 이 한 줄로만 준다(«내 말풍선» 갈래가 없다)
    '.chat-share-side': ['Ui/ChatScreen.cs#win|catalog:chat_share_win'],             # 3397 #39ab36(이긴 쪽 초록 반쪽) — 클론은 쪽 면을 아예 안 칠했다
    '.chat-share-side.lose': ['Ui/ChatScreen.cs#lose|catalog:chat_share_lose'],      # 3412 #cecece — 정본 주석 «말풍선과 같은 회색이라 목록에 녹아든다»(= `chat_bubble` 과 같은 값)
    # T377 10회차 ⓑ — **리그 묶음 열둘**(8회차가 «큰 묶음 리그 12 는 그 lock 뒤» 로 남겨 둔 것). `LeagueSheet.cs` 가 열렸다.
    #   열둘 다 **이미 제 값**이라 고칠 코드가 0 이고 자가 그 값을 지키기만 한다(값이 밀리면 여기서 빨강이 난다 · 7~9회차와 같은 꼴).
    #   ⚠ 정본 주석이 두 자리에 «여기서 틀렸었다» 를 적어 뒀다: 2264 «시트 #0e111b 는 행(#05060a)보다 **밝다** — 이전 #05070f 는 명암이 뒤집혀 있었다» ·
    #     2579 «표 안 보상 pill 은 **#bdbdbd** — 클론 #d9d9d9 는 28단계 밝아 흰 바탕에 거의 묻혔다». 값이 밀리면 그 되돌림이 조용히 되살아난다.
    '.modal-card.sheet.league-sheet': ['Ui/LeagueSheet.cs@RenderBoard|catalog:league_bg'],        # 2266 #0e111b(랭킹 시트 · 다크 변형)
    '.league-season-bar': ['Ui/LeagueSheet.cs@RenderBoard|catalog:league_bar'],                   # 2299 #030405
    '.league-foot': ['Ui/LeagueSheet.cs@RenderBoard|catalog:league_foot'],                        # 2358 #1a1f2b(핀 행+도전 버튼을 받치는 밴드)
    '.league-row': ['Ui/LeagueSheet.cs@Row|catalog:league_row'],                                  # 2320 #05060a(내 행만 pp_blue — 정본도 그렇다)
    '.league-score': ['Ui/LeagueSheet.cs@Row|catalog:league_score'],                              # 2339 #020203(«남색 아님» 이라고 정본이 못박아 뒀다)
    '.modal-card.wide.lgr-card': ['Ui/LeagueSheet.cs@RenderRewards|catalog:league_bg'],           # 2456 #0e111b(정본 «명암 오독 6번째 — 그라데이션이 아니라 평면»)
    '.league-reward-banner::before': ['Ui/LeagueSheet.cs@RenderRewards|catalog:lgr_ribbon_dk'],   # 2496 #a00a0e(리본 꼬리 둘)
    '.league-reward-banner::after': ['Ui/LeagueSheet.cs@RenderRewards|catalog:lgr_ribbon_dk'],    # 2496 #a00a0e
    '.league-reward-grid span': ['Ui/LeagueSheet.cs@RenderRewards|catalog:lgr_pill'],             # 2515 #0a0d14(어두운 구역의 보상 pill — 판보다 살짝 어둡다)
    '.league-tier-grid span': ['Ui/LeagueSheet.cs@RenderRewards|catalog:lgr_table_pill'],         # 2580 #bdbdbd(흰 표 안의 보상 pill)
    '.league-challenge-row': ['Ui/LeagueSheet.cs@RenderChallenge|catalog:challenge_row'],         # 2605 #cacaca
    '.modal-card .league-challenge-side .btn': ['Ui/LeagueSheet.cs@RenderChallenge|catalog:challenge_btn'],   # 2637 #afafaf(«플랫 — 그라디언트 아님»)
    # T377 14회차가 «클론에 없는 자리» 로 판정하고 17회차가 **정본까지 확인**해 여기 싣는다(앞의 셋과 까닭이 다르다 —
    #   클래스를 안 붙이는 게 아니라 **더 구체적인 규칙이 유일한 자리에서 덮는다**):
    #   1812 `.modal-card .cmp-card-wrap.new .cmp-card { background: #ececec }` ↔ **1820** `.cmp-lower .cmp-card-wrap.new .cmp-card { background: transparent }`.
    #   `itemCardHTML` 의 call site 셋 중 `isNew: true` 는 **ui.js 3264 하나뿐이고 그것이 3263 `<div class="cmp-lower">` 안**이다
    #   (3260·3296 은 둘 다 `false` = `.cur`). 곧 정본에서도 이 #ececec 는 **한 번도 안 그려진다**. 클론도 같다(ForgeCraftPopup.cs:87 이 `lower` 안에서 세운다).
    '.modal-card .cmp-card-wrap.new .cmp-card': ['—죽음: 1820 `.cmp-lower .cmp-card-wrap.new .cmp-card{background:transparent}` 가 덮는다 — 정본의 유일한 `.new` 자리(ui.js 3264)가 그 `.cmp-lower`(3263) 안이라 #ececec 는 정본에서도 안 그려진다'],
}
# 임자가 정해진 빈자리(파일 lock 뒤) — 붙이면 여기서 지운다
KNOWN = {
}

# ── T396 — «못박은 **잉크** 색»(`color`)의 선택자 ↔ 클론 자리. 자리 꼴은 위 TABLE 과 **같다**.
#    1회차는 **자와 셈만** 세운다: 배선할 파일(`DamageNumbers.cs` · `ChatScreen.cs` · 탭·부화·소환 …)이
#    그때그때 남의 산 lock 이라, 자리를 하나씩 붙이는 것은 그 lock 이 풀리는 회차의 몫이다(결정 아래).
TABLE_INK = {
    # T396 6회차 — «덮개 있음» 중 **실물이 그 조상 안에만 서는** 자리 둘(그래서 리터럴이 한 번도 안 그려진다).
    #   `.mat-chip` 은 정본 `ui.js` 5767 이 탈것 업그레이드 팝업(5775 `<div class="modal-card wide">`) 안에서만 찍는다 —
    #   그래서 805 #eceff1 · 812 #90a4ae 는 3523 `.modal-card .mat-chip`(var(--pp-ink)) · 3520 `.modal-card … small`(var(--pp-muted))에 늘 진다.
    '.mat-chip': ['—덮인다: 실물은 `.modal-card wide`(ui.js 5767·5775) 안에만 선다 — 3523 이 토큰으로 덮는다(클론도 토큰 잉크가 맞다)'],
    '.mat-chip small': ['—덮인다: 같은 까닭 · 3520 `.modal-card .mat-chip small { color: var(--pp-muted) }`(5회차가 «자리 없음» 으로 남긴 그 자리)'],
    # T396 2회차 — 전투 숫자 다섯(정본 509 «크리 위계는 '크기'가 아니라 **색·펀치**로 준다»).
    #   클론은 크리를 `cp`(#ff8a65), 나머지를 `stage_ink`(흰색)로 찍고 있었다 — 스킬·막음은 파랑·하늘색인데 흰색이었다.
    '.float-dmg.dmg-crit': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_crit_ink'],
    '.float-dmg.dmg-kill': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_kill_ink'],
    '.float-dmg.dmg-skill': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_skill_ink'],
    '.float-dmg.dmg-hero': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_hero_ink'],
    '.float-dmg.block': ['Battle/DamageNumbers.cs@Style|res:PinnedColorUi:dmg_block_ink'],
    # 값이 이미 같아 토큰을 써도 되는 자리(자가 그 값을 지킨다 · T377 결정 636 과 같은 셈)
    '.float-dmg.heal': ['Battle/DamageNumbers.cs@Style|catalog:pip_done'],
    # T396 3회차 — 산 lock 이 없는 파일 셋. 둘은 이미 맞았고(자가 그 값을 지킨다) 하나는 근사였다.
    '.bw-sub': ['Ui/BattleOverlay.cs|catalog:coin'],                        # 정본 409 #ffd54f ↔ 카탈로그 coin 같은 값
    '.waypoint-time': ['Ui/Waypoints.cs|res:WaypointsUi:time_ink'],         # 정본 326 #ffd54f ↔ WaypointsUi.time_ink 같은 값
    '.offline-sub': ['Ui/OfflinePopup.cs|res:PinnedColorUi:offline_sub_ink'],   # 정본 267 #ccc ↔ 클론은 pp_gray(#c4c4c4) 였다
    # T396 5회차 — 산 lock 밖 파일의 자리 열둘. 열은 **이미 제 값**이라 자가 지키기만 하고(값이 바뀌면 여기서 빨강), 둘은 근사였다(은색 비활성).
    '#summon-subtabs.subtab-strip button': ['Ui/SkillPetSheet.cs|res:PetSkillUi:subtab_ink'],   # 정본 3604 #d9d9d9 ↔ PetSkillUi subtab_ink 같은 값
    '.rw-amt': ['Ui/RewardBurst.cs|res:RewardBurstUi:amt'],                                      # 정본 7491 #ffd54f ↔ RewardBurstUi colors.amt
    '.rw-tick': ['Ui/RewardBurst.cs|res:RewardBurstUi:tick'],                                    # 정본 7550 #ffd54f ↔ RewardBurstUi colors.tick
    '.ob-zzz i': ['Ui/OfflineButton.cs|res:OfflineButtonUi:zzz_ink'],                            # 정본 227 #eaf6ff ↔ OfflineButtonUi zzz_ink
    '.skill-btn.auto': ['Ui/SkillBar.cs|res:PetSkillUi:sb_auto_ink'],                            # 정본 624 #90a4ae ↔ PetSkillUi sb_auto_ink(켜짐은 white + 키라인 · 629)
    '.equip-cell .cell-star': ['Ui/ForgeUi.cs@StarBadge|catalog:coin'],                          # 정본 953 #ffd54f ↔ 카탈로그 coin 같은 값
    # T396 9회차 — 하나는 이미 제 값이고, 하나는 «공용 잉크를 밝은 판 위에» 쓰고 있었다.
    '#forge-item-modal .idet-subs .substat-row': ['Ui/ForgeInfoPopup.cs|catalog:idet_row_ink'],   # 정본 3731 #3a3a3a ↔ 카탈로그 idet_row_ink 같은 값(자리가 이미 그 키를 쓴다)
    # 대장간 시트 «남은 시간» — 정본 3635 가 `#equip-sheet` 로 좁혀 #6d6a63 으로 못박았다.
    # 클론은 전역 ink(#eceff1 · 어두운 판 위 값)로 찍어 밝은 종이 시트에서 글자가 바탕에 묻었다.
    '#equip-sheet .forge-time': ['Ui/ForgeSheet.cs|res:PinnedColorUi:forge_time_ink'],
    # T396 8회차 — 하나는 이미 제 값이고, 하나는 «같은 그림 다른 색» 이었다.
    '.settings-act.danger': ['Ui/ProfilePopup.cs|catalog:settings_act_danger'],   # 정본 3125 #d33 ↔ 카탈로그 settings_act_danger #dd3333 같은 값(자리가 이미 그 키를 쓴다)
    # 장비 **목록** 타일의 승천 별 — 정본 784 #ff8801(주황). 격자 칸 `.equip-cell .cell-star`(953)는 #ffd54f 라 전역 coin 이 맞다(위 5회차 줄).
    # 클론은 둘 다 coin 으로 찍고 있었다 — 같은 그림이라고 같은 키가 아니다.
    '.fl-face[data-asc]:not([data-asc=""])::after': ['Ui/ForgeInfoPopup.cs|res:PinnedColorUi:list_asc_star_ink'],
    # T396 7회차 — 산 lock 밖 파일 셋. 둘은 **이미 제 값**(자리만 적으면 닫힌다)이고 하나는 근사였다.
    '.tb-val': ['Ui/TechPopups.cs|catalog:tb_val'],                     # 정본 2252 #1fa64a ↔ 카탈로그 tb_val 같은 값(`_` 칸에 «.tb-val · .tn-gain» 이라 적혀 있다)
    '.shop-sheet .shop-title': ['Ui/ShopSheet.cs|catalog:shop_title'],  # 정본 2884 #ffb300 ↔ 카탈로그 shop_title 같은 값
    # 빈 장비 칸 슬롯 이름 — 정본 865 주석이 «판독 확보 대상 … #b9a8a8→#d8caca 반 단계» 로 **일부러 올린** 값임을 적어 뒀다.
    # 클론은 전역 pp_muted(#8a8a8a)라 어두운 마룬 칸 위에서 그 판독 확보가 통째로 빠져 있었다.
    '.equip-cell .slot-name': ['Ui/ForgeSheet.cs|res:PinnedColorUi:equip_slot_name_ink'],
    # 종이 버튼 비활성 글자 — 공용(8725 · #7b7b7b)은 PetSkillUi disabled_ink, **은색**(5266 `.skd-btn.silver.disabled` · 5497 `.petup-selrow .btn.silver.disabled` · #6f6f6f)은
    # 한 톤 어두운 disabled_ink2 다. 클론 PaperButton 은 종류와 무관하게 disabled_ink 로 찍고 있었다(표에 disabled_ink2 가 있었지만 쓰는 곳이 0).
    '.btn.btn.primary.primary.disabled': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:disabled_ink'],
    '.btn.btn.on.on.disabled': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:disabled_ink'],
    '.btn.btn.equip.equip.disabled': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:disabled_ink'],
    '.btn.btn.danger.danger.disabled': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:disabled_ink'],
    '.btn.btn.sell.sell.disabled': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:disabled_ink'],
    '.petup-selrow .btn.silver.disabled': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:disabled_ink2'],
    '.skd-btn.silver.disabled': ['Ui/PetSkillKit.cs@PaperButton|res:PetSkillUi:disabled_ink2'],   # SkillPanel 381 이 PaperButton(Silver, disabled) 로 세운다
    # T396 6회차 — T345 반납으로 열린 파일 셋(ChatScreen · DungeonClearPopup · MountSheet). 여섯은 이미 제 값, 셋은 근사였다(안내글 · 공유 카드 주황 둘).
    '.chat-row.mine .chat-name': ['Ui/ChatScreen.cs#name|catalog:chat_name'],                 # 정본 3323 #ff880f ↔ 카탈로그 chat_name(태그 [..] 도 같은 글자 안)
    '.chat-tag': ['Ui/ChatScreen.cs#name|catalog:chat_name'],                                 # 정본 3324 #ff880f — 클론은 «[태그] 이름» 한 글자라 같은 자리
    '.chat-time': ['Ui/ChatScreen.cs#time|catalog:chat_time'],                                # 정본 3359 #545454 ↔ 카탈로그 chat_time
    '.chat-input-bar input::placeholder': ['Ui/ChatScreen.cs#placeholder|res:PinnedColorUi:chat_placeholder_ink'],   # 정본 3452 #6b6b6b ↔ 클론은 pp_muted(#8a8a8a) 였다
    '.chat-share-side small:last-child': ['Ui/ChatScreen.cs#cp|catalog:chat_name'],            # 정본 3409 #ff880f(양쪽 전투력) ↔ 클론은 이긴 쪽 초록·진 쪽 회색이었다
    '.chat-share-label': ['Ui/ChatScreen.cs#label|catalog:chat_name'],                        # 정본 3425 #ff880f(«승리») ↔ 클론은 chat_share_win(#35c04f) 이었다
    # T396 10회차 — 산 lock 밖 파일의 잉크 자리 한 묶음(값·까닭은 PinnedColorUi.json «_T396_10회차»).
    '#chat-preview .chat-preview-name': ['Ui/Hud.cs#chat-preview-name|res:PinnedColorUi:chat_preview_name_ink'],   # 8069 #eef1f5 ↔ 전엔 chat_name #ff880f
    '#chat-preview': ['Ui/Hud.cs#chat-preview-name|res:PinnedColorUi:chat_preview_name_ink'],                        # 8056 블록 #eef1f5 — 띠 안 글자는 이름·메시지 둘뿐이라 이름 자리가 그 값
    '#chat-preview .chat-preview-msg': ['Ui/Hud.cs#chat-preview-msg|res:PinnedColorUi:chat_preview_msg_ink'],       # 8070 #aab3c0(3640 #2e2e2e 를 같은 선택자가 뒤에서 덮는다) ↔ 전엔 chat_ink #2e2e2e
    '.league-row.me .league-server': ['Ui/LeagueSheet.cs#server|res:PinnedColorUi:league_server_me_ink'],           # 2355 #dce6ff ↔ 전엔 stage_ink
    '.league-collect-pill b': ['Ui/LeagueSheet.cs#time|res:PinnedColorUi:league_collect_time_ink'],                # 2528 #1d8f3c ↔ 전엔 pp_green_dk #1f8c34
    '.league-challenge-name small': ['Ui/LeagueSheet.cs#cp|catalog:challenge_cp'],                                  # 2635 #ff880f — 카탈로그 challenge_cp 가 같은 값(짝만)
    '.league-name small': ['Ui/LeagueSheet.cs#cp|catalog:league_cp'],                                               # 2339 #f59e0b — 카탈로그 league_cp 가 같은 값(짝만 · ui.js 4740 이름 아래 <small> = 전투력)
    '#player-info-modal .pinfo-id-text .cp': ['Ui/PlayerInfoPopup.cs#cp|res:PinnedColorUi:pinfo_cp_ink'],         # 7693 #ff880f ↔ 전엔 pp_ink
    '#player-info-modal .pinfo-subs-list': ['Ui/PlayerInfoPopup.cs#sub|res:PinnedColorUi:pinfo_subs_ink'],         # 7698 #3a3a3a(5600 var(--pp-ink) 를 뒤에서 덮는다) ↔ 전엔 pp_ink
    '.af-check.on': ['Ui/ForgeAutoPopup.cs#mark|res:PinnedColorUi:af_check_on_ink',                                 # 4730 ✓ #23c552 — 상자는 #17181a 그대로(전엔 상자를 초록으로 칠했다)
                     'Ui/ForgeUi.cs@AgeBar|res:PinnedColorUi:af_check_on_ink'],                                    # 11회차 — 시대 막대의 체크(ui.js 2289 · ForgeUi.AgeBar)도 같은 자리
    '.dgclear-title': ['Ui/DungeonClearPopup.cs#title|catalog:dgclear_title'],                # 정본 5374 #ffd54f 같은 값
    '.dgclear-sub': ['Ui/DungeonClearPopup.cs#sub|catalog:dgclear_sub'],                      # 정본 5378 #b0bec5 같은 값
    '.dgc-amt': ['Ui/DungeonClearPopup.cs#amt|catalog:dgclear_amt'],                          # 정본 5391 #ffe082 같은 값
    '.petd-wrap .petd-subs': ['Ui/MountSheet.cs#petd-subs|res:PetSkillUi:subs_ink'],
    # ── T396 13회차 — 산 lock 밖 파일 셋(Hud · PetPanel · SkillPanel)의 잉크 자리 여섯 + 죽은 선언 셋.
    #   다섯은 **이미 제 값**이라 표에 올리기만 하면 닫힌다(자가 그 값을 지킨다 — 값이 바뀌면 여기서 빨강).
    #   하나(`.hatchery .slot-buy b`)는 «같은 그림 다른 색» 이었다: 정본은 부화장 값(#e8112d)과 소환 버튼 값(#f2191d)을
    #   **다른 리터럴**로 못박는데 클론은 둘을 `cost_red` 한 키로 찍고 있었다(8회차 `.fl-face[data-asc]` 와 같은 갈래).
    '.profile-info .cp': ['Ui/Hud.cs@Build|catalog:cp'],                         # 108 #ff8a65 ↔ 카탈로그 cp 같은 값(짝만)
    '.currency-pills .pill.coin': ['Ui/Hud.cs@Build|catalog:coin'],              # 118 #ffd54f ↔ 카탈로그 coin 같은 값(짝만)
    '.currency-pills .pill.gem': ['Ui/Hud.cs@Build|catalog:gem'],                # 119 #ff8a80 ↔ 카탈로그 gem 같은 값(짝만)
    '.hatch-cell.empty': ['Ui/PetPanel.cs#hatch-hint|res:PetSkillUi:hatch_hint'],   # 4515 #8b96b5 ↔ PetSkillUi hatch_hint 같은 값(짝만)
    '.hatchery .slot-buy b': ['Ui/PetPanel.cs#cost|res:PetSkillUi:slot_buy_cost_ink'],   # 4566 #e8112d ↔ 전엔 cost_red #f2191d(소환 버튼 값의 빨강)
    '.summon-btn .summon-cost b': ['Ui/SkillPanel.cs|res:PetSkillUi:cost_red'],  # 8668 #f2191d ↔ PetSkillUi cost_red 같은 값(짝만 · 위 자리와 갈리는 쪽)
    # 죽은 선언 셋 — 이 자의 «죽음» 갈래는 «같은 선택자가 뒤에 다시» 만 본다. 여기 셋은 그것이 아니라
    #   **정본 자신이 그 클래스를 한 번도 안 붙인다**(`grep -rn "hatch-slot\|egg-chip" js/ index.html` = 0).
    #   부화장이 `.hatch-slot`(1650~1655) → `.hatch-cell`(ui.js 3926) 로 갈아엎히며 남은 줄이고, `.egg-chip` 도 같다.
    #   클론에 자리가 없는 것이 **맞다** — 미정으로 두면 다음 사람이 또 «없는 자리» 를 찾는다(T371 11회차의 20분).
    '.hatch-slot.empty': ['—죽음: 정본이 `hatch-slot` 클래스를 한 번도 안 붙인다(ui.js 3926 이 `.hatch-cell` 로 갈아엎었다) — 그려지지 않는 리터럴'],
    '.hatch-slot.buy': ['—죽음: 같은 까닭(1655 · #cbb6f5) — `.slot-buy`(4566 · 살아 있다)와 **다른 자리**다'],
    '.egg-chip small': ['—죽음: 정본이 `egg-chip` 클래스를 한 번도 안 붙인다(js/ · index.html 통틀어 0) — 그려지지 않는 리터럴'],           # 정본 5454 #3a3a3a ↔ PetSkillUi subs_ink 같은 값(PetPanel 쪽 같은 키는 그 lock 뒤)
}
KNOWN_INK = {
    # T396 10회차 — 한 글자 안의 **부분 색**(<small>·<span> 조각): 클론은 그 글을 한 TMP 로 찍고 richText 를 안 켜므로(check_richtext)
    #   둘째 줄·조각을 따로 세워야 색이 갈린다(IconTextStack 둘째 줄 잉크 키 같은 것) — 조각 분리 몫 · 다음 회차.
    '.fi-skip-gem': '한 버튼 글 «건너뛰기\\n💎 N» 의 아랫줄(ForgeInfoPopup.cs 141 IconTextStack.ReplaceLabel 한 잉크) — 5160 #e11d48 · 조각 분리 뒤',
    '.tn-skip small': '한 알약 글 «건너뛰기\\n◆ N» 의 아랫줄(TechPopups.cs 270 DungeonPopups.Pill 한 잉크) — 4613 #c62828 · 조각 분리 뒤',
    '.tn-gain': '한 줄 «+N% (…)» 안의 <small>(TechPopups 주 수치 글 한 TMP) — 3693 #1fa64a · 조각 분리 뒤',
    '.asc-wipe-warn': '승천 안내 여러 줄 글 안의 한 줄 <span>(AscendPopup.cs 166 eff 한 TMP) — 5637 #ff6b5e · 줄 분리 뒤',
}

HEX = re.compile(r'#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})\b')
# `@keyframes` 단계 — `0%` · `12.5%` · `from` · `to` · 쉼표로 묶인 것(`0%, 40%`). 선택자가 아니다(T396 3회차).
STEP_SEL = re.compile(r'^(?:\d+(?:\.\d+)?%|from|to)(?:\s*,\s*(?:\d+(?:\.\d+)?%|from|to))*$')


def norm_hex(h):
    h = h.lower()
    if len(h) == 4:
        return '#' + ''.join(c * 2 for c in h[1:])
    return h[:7]


def strip_comments(css):
    return re.sub(r'/\*.*?\*/', lambda m: '\n' * m.group(0).count('\n'), css, flags=re.S)


# 걷는 속성 — 면(T377)과 **잉크**(T396). 잉크를 면과 **한 목록에 섞지 않는다**: 같은 선택자가 둘 다
# 못박을 수 있고(예: 알약이 바탕과 글자를 같이 준다) 그러면 뒤가 앞을 덮어 한 자리가 통째로 사라진다.
FACE_PROPS = r'(?:background|background-color)'
INK_PROPS = r'color'


def pinned_decls(css_text, props):
    """정본에서 «리터럴 hex · 토큰과 다른 값» 선언을 걷는다 → {selector: (line, hex)} (뒤 규칙이 앞을 덮는다).

    `props` 는 속성 이름 정규식이다 — 선언 **머리**에서만 맞춘다(`\s*<props>\s*:`). 그래서
    `background-color` 는 잉크(`color`)로 안 새고 `border-color`·`-webkit-text-stroke` 도 안 걸린다.
    """
    css = strip_comments(css_text)
    tokens = set()
    for m in re.finditer(r'--[a-z0-9-]+\s*:\s*(#[0-9a-fA-F]{3,6})\b', css):
        tokens.add(norm_hex(m.group(1)))
    rx = re.compile(r'\s*' + props + r'\s*:\s*(#[0-9a-fA-F]{3,6})\b')
    out = {}
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        sel_raw = ' '.join(m.group(1).split())
        if sel_raw.startswith('@') or 'keyframes' in sel_raw:
            continue
        # ⚠ `@keyframes` 의 **단계**(`0%` · `from` · `to`)는 선택자가 아니다(T396 3회차).
        #    바깥 블록 `@keyframes name { … }` 은 위에서 걸러지지만, 이 정규식은 **안쪽 블록**을 따로 물어
        #    `0% { color: #ff8a1e }` 를 «선택자 0%» 로 셌다 — 그것은 «못박은 색» 이 아니라 **연출 중간값**(T173·T334 축)이고,
        #    고칠 자리가 없어 «미정» 에 영원히 남는다(실측: 잉크 105 중 둘이 `@keyframes dmgcrit` 의 0%·9% 였다).
        if STEP_SEL.match(sel_raw):
            continue
        line = css[:m.start()].count('\n') + 1
        for d in m.group(2).split(';'):
            mm = rx.match(d)
            if not mm:
                continue
            h = norm_hex(mm.group(1))
            if h in tokens:
                continue
            for sel in [s.strip() for s in sel_raw.split(',') if s.strip()]:
                out[sel] = (line, h)
    return out


def all_decls(css_text, props):
    """그 속성을 **값과 상관없이** 세우는 규칙 전부 → [(선택자, 줄, 값)] (T396 6회차).

    «못박은 색» 목록(`pinned_decls`)은 리터럴만 걷는데, **덮는 쪽은 토큰이어도 덮는다**
    (실물: 813 `.mat-chip small { color: #b7b7b7 }` 를 3522 `.modal-card .mat-chip small { color: var(--pp-muted) }` 가 덮는다).
    그래서 덮개를 찾을 때는 이 목록을 쓴다.
    """
    css = strip_comments(css_text)
    rx = re.compile(r'\s*' + props + r'\s*:\s*([^;]+)')
    out = []
    for m in re.finditer(r'([^{}]+)\{([^{}]*)\}', css):
        sel_raw = ' '.join(m.group(1).split())
        if sel_raw.startswith('@') or 'keyframes' in sel_raw or STEP_SEL.match(sel_raw):
            continue
        line = css[:m.start()].count('\n') + 1
        for d in m.group(2).split(';'):
            mm = rx.match(d)
            if not mm:
                continue
            for sel in [x.strip() for x in sel_raw.split(',') if x.strip()]:
                out.append((sel, line, mm.group(1).strip()))
    return out


def covers_of(sel, decls_all):
    """그 선택자를 **조상 한정으로 덮는** 규칙들 → [(덮는 선택자, 줄, 값)] (T396 6회차).

    잣대는 하나 — «앞에 조상이 더 붙은 같은 꼬리»(`X Y` 가 `Y` 를 덮는다 · `.modal-card .mat-chip small` ⊃ `.mat-chip small`).
    그런 선택자는 구체성이 **엄격히 더 크므로 줄 차례와 상관없이** 그 조상 안에서는 늘 이긴다(T401 3회차·T365 12회차가 만난 그 계단).
    ⚠ «늘 덮인다» 는 뜻이 아니다 — 그 조상 **밖**에 같은 요소가 서면 원래 선언이 산다. 그래서 자는 이것을
    «미정» 에서 갈라 **«덮개 있음»** 으로만 적고, 실물에서 그 조상뿐인지는 그 자리를 여는 회차가 본다(결정 702 의 형제).
    """
    out = []
    for other, line, val in decls_all:
        if other != sel and other.endswith(' ' + sel):
            out.append((other, line, val))
    return out


def later_same(sel, line, decls_all):
    """**같은 선택자**가 더 뒤에서 그 속성을 다시 세우는가 → [(선택자, 줄, 값)] · 없으면 빈 목록 (T377 7회차).

    `covers_of` 는 «조상이 더 붙은 같은 꼬리»(구체성 계단)만 본다. 그런데 정본은 같은 선택자를
    **파일 뒤에서 한 번 더** 적어 갈아끼우는 자리가 많다(화풍 갈래를 파일 끝에 몰아 둔 8000번대 · 시트 스코프 3500번대).
    구체성이 같으면 **소스 차례로 뒤엣것이 이긴다** — 그러면 앞의 리터럴은 **한 번도 안 그려진다**.
    실측: `.subtab-strip button.active` 654 `#1f4a2c`(초록)는 3585·3612 의 `var(--pp-blue)` 가 덮어 **파랑으로 선다**
    (클론이 파란 것이 맞다 · T178 17회차가 그 자리 화소를 쟀다). `pinned_decls` 는 **리터럴만** 걷으므로
    뒤가 토큰이면 덮임을 못 본다 — 그래서 값과 상관없이 세는 `all_decls` 로 다시 본다.
    ⚠ «조상 덮개»(`covers_of`)와 달리 이쪽은 **조건 없이** 이긴다(같은 선택자라 서는 자리가 같다).
    """
    out = []
    for other, oline, val in decls_all or []:
        if other == sel and oline > line:
            out.append((other, oline, val))
    return out


def pinned_faces(css_text):
    """«못박은 면 색»(T377) — `background`/`background-color` 리터럴."""
    return pinned_decls(css_text, FACE_PROPS)


def pinned_inks(css_text):
    """«못박은 잉크 색»(T396) — `color` 리터럴. 전투 숫자 다섯·하위 탭 켜짐 글자·채팅 미리보기 … 가 여기 있다."""
    return pinned_decls(css_text, INK_PROPS)


def _find_hex(obj, key):
    """JSON 안 어디든 "key": "#hex" — 첫 것."""
    if isinstance(obj, dict):
        v = obj.get(key)
        if isinstance(v, str) and HEX.match(v):
            return norm_hex(v)
        for x in obj.values():
            r = _find_hex(x, key)
            if r:
                return r
    elif isinstance(obj, list):
        for x in obj:
            if isinstance(x, dict) and x.get('key') == key and isinstance(x.get('hex'), str):
                return norm_hex(x['hex'])
            r = _find_hex(x, key)
            if r:
                return r
    return None


def source_hex(source, key, catalog, resdir):
    if source == 'catalog':
        try:
            with open(catalog, encoding='utf-8') as f:
                return _find_hex(json.load(f), key)
        except (OSError, ValueError):
            return None
    if source.startswith('res:'):
        p = os.path.join(resdir, source[4:] + '.json')
        try:
            with open(p, encoding='utf-8') as f:
                return _find_hex(json.load(f), key)
        except (OSError, ValueError):
            return None
    return None


def method_body(text, name):
    m = re.search(r'\b' + re.escape(name) + r'\s*\([^)]*\)\s*(?:where[^{]*)?\{', text)
    if not m:
        return None
    i = m.end(); depth = 1
    while i < len(text) and depth:
        if text[i] == '{': depth += 1
        elif text[i] == '}': depth -= 1
        i += 1
    return text[m.end():i]


def check_site(site, game, catalog, resdir):
    """(ok, 문구)"""
    if site.startswith('—'):
        return True, site
    if '|' not in site:
        return False, '%s — 자리 표기에 «|source:key» 가 없다' % site
    place, src = site.split('|', 1)
    if ':' not in src:
        return False, '%s — «source:key» 꼴이 아니다' % site
    source, key = src.rsplit(':', 1)
    if place.count('#') == 1:
        path, name = place.split('#'); kind = '#'
    elif place.count('@') == 1:
        path, name = place.split('@'); kind = '@'
    else:
        path, name, kind = place, None, ''
    full = os.path.join(game, path)
    try:
        with open(full, encoding='utf-8') as f:
            text = f.read()
    except OSError:
        return False, '%s — 파일이 없다' % full
    scope = text
    if kind == '@':
        scope = method_body(text, name)
        if scope is None:
            return False, '%s — 메서드 %s 를 못 찾았다' % (path, name)
    elif kind == '#' and ('"%s"' % name) not in text:
        return False, '%s — 이름 «%s» 으로 만든 자리가 없다' % (path, name)
    if ('"%s"' % key) not in scope:
        return False, '%s — «"%s"» 호출이 없다(%s)' % (path, key, '메서드 ' + name if kind == '@' else '파일 어디든')
    return True, None


def _judge(kind, table, known, decls, game, catalog, resdir, bad, list_all, out, decls_all=None):
    """한 갈래(면·잉크)를 대조한다 — (자리 초록, KNOWN, 미정 수). `bad` 에 어긋난 것을 쌓는다(순수하지 않은 것은 출력뿐)."""
    ok_n = known_n = 0
    for sel, sites in table.items():
        if sel not in decls:
            bad.append('표의 선택자 «%s» 가 정본의 «못박은 %s» 목록에 없다(리터럴이 아니거나 토큰과 같은 값이 됐다) — 표를 고쳐라' % (sel, kind))
            continue
        line, want = decls[sel]
        for site in sites:
            ok, why = check_site(site, game, catalog, resdir)
            if not ok:
                bad.append('%s(%d) → %s' % (sel, line, why)); continue
            if site.startswith('—'):
                ok_n += 1; continue
            source, key = site.split('|', 1)[1].rsplit(':', 1)
            got = source_hex(source, key, catalog, resdir)
            if got is None:
                bad.append('%s(%d) → %s 에 «%s» 이 없다' % (sel, line, source, key))
            elif got != want:
                bad.append('%s(%d) → «%s» = %s 인데 정본은 %s — 전용 키의 값이 다르다' % (sel, line, key, got, want))
            else:
                ok_n += 1
    for sel in known:
        if sel in decls:
            known_n += 1
        else:
            bad.append('KNOWN 의 «%s» 가 정본 %s 목록에 없다 — 줄을 지워라' % (sel, kind))
    undecided = [s for s in decls if s not in table and s not in known]
    covered = dead = 0
    for s in undecided:
        # 둘을 가른다 — **같은 선택자가 뒤에 다시**(조건 없이 이긴다 · 그 리터럴은 한 번도 안 그려진다)가 먼저고,
        # 그 다음이 **조상이 더 붙은 같은 꼬리**(그 조상 안에서만 이긴다) — T377 7회차.
        lt = later_same(s, decls[s][0], decls_all or [])
        cv = [] if lt else covers_of(s, decls_all or [])
        if lt:
            dead += 1
        elif cv:
            covered += 1
        if list_all:
            if lt:
                out('  죽음 %s %5d %-52s %s  ← 같은 선택자가 %d 에 다시: %s' % (kind[:2], decls[s][0], s[:52], decls[s][1], lt[-1][1], lt[-1][2][:26]))
            elif cv:
                out('  덮개 %s %5d %-52s %s  ← %s(%d) %s' % (kind[:2], decls[s][0], s[:52], decls[s][1], cv[0][0][:44], cv[0][1], cv[0][2][:22]))
            else:
                out('  미정 %s %5d %-52s %s' % (kind[:2], decls[s][0], s[:52], decls[s][1]))
    return ok_n, known_n, len(undecided), covered, dead


def loader_hex(raw):
    """유니티 로더(`PinnedColorUi.C`)가 한 칸에서 hex 를 꺼내는 **그 규칙 그대로**.

    두 꼴을 받는다 — 홑값 `"키": "#hex"` 와 주석 달린 객체 `"키": {"hex": "#hex", "_": "..."}`
    (뒤엣것이 이웃 표들의 관례다: TextShadowUi.shadows 20 · OpacityUi.alpha 8 · TextSizeUi.size 3).
    로더가 못 읽는 꼴이면 None — 그 칸을 부르는 순간 KeyNotFoundException 이다.
    """
    if isinstance(raw, str):
        return raw if HEX.match(raw) else None
    if isinstance(raw, dict):
        h = raw.get('hex')
        return h if isinstance(h, str) and HEX.match(h) else None
    return None


def table_shape_problems(resdir):
    """표의 **모든** 칸이 로더가 읽을 수 있는 꼴인가 (T377 15회차).

    까닭 — 런 **#1064**: `fi_age_star_ink` 가 관례대로 객체로 적혔는데 로더만 홑값을 고집해
    `PinnedColorSitesTests` 가 KeyNotFoundException 으로 넘어졌다. 그때 이 자는 **초록**이었다 —
    그 선택자를 TABLE 에 안 갖고 있어 «미정» 으로 흘려보냈기 때문이다. 곧 **표에 오르지 않은 칸은
    아무도 안 보고 있었다.** 그래서 여기서는 TABLE 과 무관하게 «칸이 읽히는가» 만 전수로 본다 —
    값이 정본과 같은지는 위쪽 자리 검사 몫이다.
    """
    path = os.path.join(resdir, 'PinnedColorUi.json')
    try:
        with open(path, encoding='utf-8') as f:
            doc = json.load(f)
    except (OSError, ValueError) as e:
        return ['PinnedColorUi.json 을 못 읽었다 — %s' % e]
    colors = doc.get('colors')
    if not isinstance(colors, dict) or not colors:
        return ['PinnedColorUi.json 에 colors 가 없다(로더도 여기서 던진다)']
    out = []
    for key in sorted(colors):
        if key.startswith('_'):
            continue
        if loader_hex(colors[key]) is None:
            out.append('PinnedColorUi.json 의 «%s» 를 유니티 로더가 못 읽는다 — 홑값 "#hex" 도, '
                       '객체의 "hex" 칸도 아니다(부르는 순간 KeyNotFoundException · 런 #1064 가 그랬다)' % key)
    return out


def run(css_path, game, catalog, resdir, list_all=False, out=print):
    try:
        with open(css_path, encoding='utf-8') as f:
            css_text = f.read()
        faces = pinned_faces(css_text)
        inks = pinned_inks(css_text)
    except OSError:
        out('✗ check_pinned_colors: 정본 CSS 를 못 읽었다 — %s' % css_path)
        return 2
    bad = []
    bad.extend(table_shape_problems(resdir))   # T377 15회차 — 표에 안 오른 칸까지 전수로 «읽히는가» 를 본다
    ink_all = all_decls(css_text, INK_PROPS)
    face_all = all_decls(css_text, FACE_PROPS)
    ink_ok, ink_known, ink_undec, ink_cov, ink_dead = _judge('잉크 색', TABLE_INK, KNOWN_INK, inks, game, catalog, resdir, bad, list_all, out, ink_all)
    ok_n, known_n = 0, 0
    for sel, sites in TABLE.items():
        if sel not in faces:
            bad.append('표의 선택자 «%s» 가 정본의 «못박은 면 색» 목록에 없다(리터럴이 아니거나 토큰과 같은 값이 됐다) — 표를 고쳐라' % sel)
            continue
        line, want = faces[sel]
        for site in sites:
            ok, why = check_site(site, game, catalog, resdir)
            if not ok:
                bad.append('%s(%d) → %s' % (sel, line, why)); continue
            if site.startswith('—'):
                ok_n += 1; continue
            source, key = site.split('|', 1)[1].rsplit(':', 1)
            got = source_hex(source, key, catalog, resdir)
            if got is None:
                bad.append('%s(%d) → %s 에 «%s» 이 없다' % (sel, line, source, key))
            elif got != want:
                bad.append('%s(%d) → «%s» = %s 인데 정본은 %s — 전용 키의 값이 다르다' % (sel, line, key, got, want))
            else:
                ok_n += 1
    for sel in KNOWN:
        if sel in faces:
            known_n += 1
        else:
            bad.append('KNOWN 의 «%s» 가 정본 목록에 없다 — 줄을 지워라' % sel)
    undecided = [s for s in faces if s not in TABLE and s not in KNOWN]
    face_cov = face_dead = 0
    for s in undecided:
        lt = later_same(s, faces[s][0], face_all)          # T377 7회차 — 같은 선택자가 뒤에 다시 서면 앞 리터럴은 죽는다
        cv = [] if lt else covers_of(s, face_all)
        if lt:
            face_dead += 1
        elif cv:
            face_cov += 1
        if list_all:
            if lt:
                out('  죽음 면 %5d %-52s %s  ← 같은 선택자가 %d 에 다시: %s' % (faces[s][0], s[:52], faces[s][1], lt[-1][1], lt[-1][2][:26]))
            elif cv:
                out('  덮개 면 %5d %-52s %s  ← %s(%d) %s' % (faces[s][0], s[:52], faces[s][1], cv[0][0][:44], cv[0][1], cv[0][2][:22]))
            else:
                out('  미정 면 %5d %-52s %s' % (faces[s][0], s[:52], faces[s][1]))
    if bad:
        out('✗ check_pinned_colors: %d곳' % len(bad))
        for b in bad:
            out('  · ' + b)
        return 1
    out('✓ check_pinned_colors: 정본 «못박은 면 색» %d 선택자 · 자리 초록 %d · KNOWN %d · 미정 %d'
        '(그중 **죽음 %d** · 덮개 있음 %d)'
        ' ‖ «못박은 잉크 색» %d 선택자(색 %d) · 자리 초록 %d · KNOWN %d · 미정 %d(그중 **죽음 %d** · 덮개 있음 %d)  (--list)'
        % (len(faces), ok_n, known_n, len(undecided), face_dead, face_cov,
           len(inks), len(set(v[1] for v in inks.values())), ink_ok, ink_known, ink_undec, ink_dead, ink_cov))
    return 0


def self_test():
    fails = []
    def eq(name, got, want):
        if got != want:
            fails.append('%s: %r ≠ %r' % (name, got, want))
    css = ''':root { --pp-red: #e8362f; --x: #fff; }
/* 주석 안 .ghost { background: #123456 } */
.tok { background: var(--pp-red); }
.same { background-color: #E8362F; }
.short { background: #fff; }
.pin, .pin2 { background: #ff1017; box-shadow: none; }
.pin { background: #ff1017 url(x) }
.over { background: #111; }
.over { background: #222222; }
'''
    faces = pinned_faces(css)
    eq('ⓐ 토큰과 같은 값은 뺀다', '.same' in faces, False)
    eq('ⓑ 3자리 hex 도 토큰과 같으면 뺀다', '.short' in faces, False)
    eq('ⓒ 쉼표 목록은 하나씩', ('.pin' in faces, '.pin2' in faces), (True, True))
    eq('ⓓ 값 정규화', faces['.pin'][1], '#ff1017')
    eq('ⓔ 뒤 규칙이 앞을 덮는다', faces['.over'][1], '#222222')
    eq('ⓕ 주석 안은 안 센다', '.ghost' in faces, False)

    # ── T396 ⓖ~ⓙ — 잉크(`color`) 갈래. **면과 안 섞인다**: 같은 선택자가 둘 다 못박을 수 있어
    #    한 목록에 담으면 뒤가 앞을 덮어 한 자리가 통째로 사라진다(그래서 목록을 둘로 둔다).
    ink_css = ''':root { --ink: #eceff1; --pp-red: #e8362f; }
.both { background: #ff1017; color: #7ee2a8; }
.bgonly { background-color: #123456; }
.tokink { color: var(--ink); }
.sameink { color: #ECEFF1; }
.borderc { border-color: #abcdef; }
.stroke { -webkit-text-stroke: 2px #abcdef; }
.crit { color: #ff8a1e }
'''
    inks = pinned_inks(ink_css)
    ifaces = pinned_faces(ink_css)
    eq('ⓖ `background-color` 는 잉크로 안 샌다', '.bgonly' in inks, False)
    eq('ⓖ `border-color`·`-webkit-text-stroke` 도 안 샌다', ('.borderc' in inks, '.stroke' in inks), (False, False))
    eq('ⓗ 토큰과 같은 잉크는 뺀다', ('.tokink' in inks, '.sameink' in inks), (False, False))
    eq('ⓘ 한 선택자가 면·잉크를 둘 다 못박으면 **양쪽에** 선다',
       (inks.get('.both', (0, None))[1], ifaces.get('.both', (0, None))[1]), ('#7ee2a8', '#ff1017'))
    eq('ⓘ 잉크만 있는 자리는 면 목록에 없다', '.crit' in ifaces, False)
    eq('ⓙ 값 정규화·줄 번호', (inks['.crit'][1], inks['.crit'][0] > 0), ('#ff8a1e', True))

    # ⓚ T396 3회차 — `@keyframes` 의 **단계**는 선택자가 아니다. 바깥 블록은 이미 걸러지지만
    #    안쪽 블록(`0% { … }`)이 따로 물려 «선택자 0%» 로 세어지던 자리다(실측: `@keyframes dmgcrit` 의 0%·9%).
    kf_css = '''@keyframes dmgcrit {
  0%   { color: #ff8a1e; }
  12.5% { background: #123456; }
  from, to { color: #abcdef; }
}
.real { color: #ff8a1e; }
'''
    kf_ink = pinned_inks(kf_css); kf_face = pinned_faces(kf_css)
    eq('ⓚ 키프레임 단계는 잉크로 안 센다', sorted(kf_ink), ['.real'])
    eq('ⓚ 키프레임 단계는 면으로도 안 센다', sorted(kf_face), [])
    eq('ⓚ `from, to` 묶음도 안 센다', 'from, to' in kf_ink, False)
    eq('ⓚ 진짜 선택자는 그대로 선다', kf_ink['.real'][1], '#ff8a1e')
    with tempfile.TemporaryDirectory() as d:
        cssp = os.path.join(d, 'style.css'); open(cssp, 'w', encoding='utf-8').write(css)
        game = os.path.join(d, 'game'); os.makedirs(os.path.join(game, 'Ui'))
        res = os.path.join(d, 'res'); os.makedirs(res)
        cat = os.path.join(d, 'catalog.json')
        json.dump({'colors': [{'key': 'ok_key', 'hex': '#FF1017'}, {'key': 'bad_key', 'hex': '#e8362f'}]}, open(cat, 'w'))
        json.dump({'colors': {'pin2_face': '#ff1017'}}, open(os.path.join(res, 'PinnedColorUi.json'), 'w'))
        open(os.path.join(game, 'Ui', 'A.cs'), 'w', encoding='utf-8').write(
            'class A { void Build() { var f = Rounded(p, "face", "pp_red", 1); f.color = PinnedColorUi.C("pin2_face"); }\n'
            ' void Other() { X("ok_key"); Y("bad_key"); } }\n')
        global TABLE, KNOWN, TABLE_INK, KNOWN_INK
        # ⚠ **잉크 표도 같이 치운다**(T396 2회차): 안 치우면 임시 CSS 에 없는 실물 선택자(`.float-dmg…`)가
        #    «표의 선택자가 정본 목록에 없다» 로 빨개져 **자기 검사가 제 손으로 깨진다**(실제로 그랬다).
        saved = (TABLE, KNOWN, TABLE_INK, KNOWN_INK)
        TABLE_INK, KNOWN_INK = {}, {}
        try:
            lines = []
            TABLE = {'.pin': ['Ui/A.cs|catalog:ok_key'], '.pin2': ['Ui/A.cs#face|res:PinnedColorUi:pin2_face']}; KNOWN = {}
            eq('ⓖ 맞는 자리 둘 → rc 0', run(cssp, game, cat, res, out=lines.append), 0)
            TABLE = {'.pin': ['Ui/A.cs|catalog:bad_key']}
            eq('ⓗ 값이 다르면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            eq('ⓗ 문구', any('전용 키의 값이 다르다' in l for l in lines), True)
            TABLE = {'.pin': ['Ui/A.cs@Build|catalog:ok_key']}
            eq('ⓘ 메서드 안에 호출이 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {'.tok': ['Ui/A.cs|catalog:ok_key']}
            eq('ⓙ 표의 선택자가 목록에 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {}; KNOWN = {'.pin': '남의 lock'}
            eq('ⓚ KNOWN 은 지나간다', run(cssp, game, cat, res, out=lines.append), 0)
            KNOWN = {'.nope': '없는 선택자'}
            eq('ⓛ KNOWN 이 목록에 없으면 rc 1', run(cssp, game, cat, res, out=lines.append), 1)
            TABLE = {}; KNOWN = {}
            eq('ⓜ CSS 없음 → rc 2', run(os.path.join(d, 'no.css'), game, cat, res, out=lines.append), 2)
        finally:
            TABLE, KNOWN, TABLE_INK, KNOWN_INK = saved

    # ── T396 6회차 — «덮개 있음» 갈래(조상이 더 붙은 같은 꼬리는 구체성이 커서 그 조상 안에서 늘 이긴다)
    cov_css = ''':root { --pp-muted: #90a4ae; }
.chip small { color: #b7b7b7; }
.card .chip small { color: var(--pp-muted); }
.lonely { color: #c1c1c1; }
.lonely-ish { color: #d2d2d2; }
.deep .lonely-ish { background: #333333; }
'''
    all_ink = all_decls(cov_css, INK_PROPS)
    eq('ⓝ 덮개는 값이 토큰이어도 찾는다', [c[0] for c in covers_of('.chip small', all_ink)], ['.card .chip small'])
    eq('ⓞ 덮개가 없으면 빈 목록', covers_of('.lonely', all_ink), [])
    eq('ⓟ 다른 속성의 규칙은 덮개가 아니다', covers_of('.lonely-ish', all_ink), [])
    eq('ⓠ 제 자신은 덮개가 아니다', covers_of('.card .chip small', all_ink), [])
    eq('ⓡ 꼬리가 «겹치기만» 하는 것은 덮개가 아니다(.chip small ↔ .xchip small)',
       covers_of('.chip small', all_decls('.xchip small { color: #111111; }', INK_PROPS)), [])
    lines2 = []
    _judge('잉크 색', {}, {}, pinned_inks(cov_css), '', '', '', [], True, lines2.append, all_ink)
    eq('ⓢ --list 가 덮개를 따로 적는다', any(l.startswith('  덮개') and '.chip small' in l for l in lines2), True)
    eq('ⓣ 덮개 없는 자리는 그대로 «미정»', any(l.startswith('  미정') and '.lonely' in l for l in lines2), True)
    # ── T377 7회차 — «같은 선택자가 뒤에 다시»(소스 차례로 뒤가 이긴다 · 앞 리터럴은 한 번도 안 그려진다)
    dead_css = '''
.subtab-strip button.active { background: #1f4a2c; color: #7ee2a8; }
.subtab-strip button.active { background: var(--pp-blue); color: #fff; }
.only-once { background: #123456; }
.later-other { background: #654321; }
.deep .later-other { background: #abcdef; }
'''
    dead_all = all_decls(dead_css, FACE_PROPS)
    eq('ⓤ 같은 선택자가 뒤에 다시 서면 앞 리터럴은 죽는다',
       [(c[1], c[2]) for c in later_same('.subtab-strip button.active', 1, dead_all)], [(2, 'var(--pp-blue)')])
    eq('ⓥ 뒤가 토큰이어도 덮는다(리터럴만 걷는 pinned_decls 로는 못 본다)',
       '.subtab-strip button.active' in pinned_faces(dead_css), True)
    eq('ⓦ 한 번만 선 선택자는 안 죽는다', later_same('.only-once', 3, dead_all), [])
    eq('ⓧ **앞**에 있는 같은 선택자는 덮개가 아니다(줄 차례를 본다)',
       later_same('.subtab-strip button.active', 2, dead_all), [])
    eq('ⓨ 조상이 더 붙은 것은 «같은 선택자» 가 아니다(그쪽은 covers_of 몫)',
       later_same('.later-other', 4, dead_all), [])
    lines3 = []
    _judge('면 색', {}, {}, pinned_faces(dead_css), '', '', '', [], True, lines3.append, dead_all)
    eq('ⓩ --list 가 죽은 자리를 «죽음» 으로 따로 적는다',
       any(l.startswith('  죽음') and '.subtab-strip button.active' in l for l in lines3), True)
    eq('ⓐⓐ 죽은 자리는 «덮개» 로도 «미정» 으로도 안 적힌다',
       any(('.subtab-strip button.active' in l) and (l.startswith('  덮개') or l.startswith('  미정')) for l in lines3), False)

    n = 39   # T377 14 + T396 잉크 갈래 6 + 키프레임 단계 막이 4 + 6회차 덮개 갈래 8 + 7회차 «같은 선택자가 뒤에 다시» 7
    if fails:
        print('✗ check_pinned_colors --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  · ' + f)
        return 1
    print('✓ check_pinned_colors --self-test %d칸 통과' % n)
    return 0


def main(argv):
    css, game, catalog, res, list_all = CSS_DEFAULT, GAME_DEFAULT, CATALOG_DEFAULT, RES_DEFAULT, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test':
            return self_test()
        if a == '--list':
            list_all = True
        elif a == '--css' and i + 1 < len(argv):
            css = argv[i + 1]; i += 1
        elif a == '--game' and i + 1 < len(argv):
            game = argv[i + 1]; i += 1
        else:
            print('사용: check_pinned_colors.py [--css <style.css>] [--game <Assets/Scripts/Game>] [--list] [--self-test]')
            return 2
        i += 1
    return run(css, game, catalog, res, list_all)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
