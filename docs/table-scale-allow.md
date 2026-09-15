# «표값 × 박힌 상수» 허용 목록 (T378 · `tools/check_table_scale.py` 가 읽는다)

자는 `Assets/Scripts/Game` 에서 `UiKit.L/H("키")` 뒤에 붙은 상수 곱·합을 전수하고 이 표와 맞춘다.
열쇠는 **파일 · 키 · 연산 상수** 다(줄 번호는 남이 파일을 고칠 때마다 밀리므로 열쇠가 아니다 · 참고로만 적는다).

- **기하**(영구): 뜻이 있는 기하 — `×2` 는 양쪽 패딩·테 두 겹, `×0.5` 는 반. 표로 옮길 필요가 없다.
- **임시**(임자 있음 · 알리되 막지 않는다): 자리 파일이 남의 lock 이라 아직 못 고친 «치수를 곱으로 부풀리는» 자리.
  그 lock 이 풀리면 정본 CSS 의 값을 표 키(`catalog.json`)로 옮기고 곱을 없앤다 — **고친 자리는 이 표에서 지운다**(남아 있으면 자가 «목록에만 있다» 로 운다).
- 새 곱이 생기면 자가 빨갛다: 표 키로 옮기든지, 정말 기하면 여기 «기하» 로 까닭과 함께 적는다.

| 파일 | 키 | 곱 | 갈래 | 왜 / 임자 |
|---|---|---|---|---|
| `Ui/LeagueSheet.cs` | `card_pad` | ×2 | 기하 | 양쪽 패딩(334) |
| `Ui/GearDetailPopup.cs` | `card_pad` | ×2 | 기하 | 양쪽 패딩(82) |
| `Ui/TechPopups.cs` | `idet_subs_pad` | ×2 | 기하 | 양쪽 패딩(125) |
| `Ui/Hud.cs` | `line_px` | ×2 | 기하 | 테 두 겹(261) |
| `Ui/Popups.cs` | `xbtn` | ×0.5 | 기하 | 반겹 — ✕ 버튼이 카드 모서리에 반쯤 걸친다(214) |
| `Ui/ForgeSheet.cs` | `billet_stroke` | ×0.5 | 기하 | 반폭(406) |
| `Ui/ChatScreen.cs` | `topbar_h` | ×0.3 | 임시 | T378(누구든) · 치우침 0.3 — 정본 값을 찾아 표 키로(41) |
| `Ui/LeagueSheet.cs` | `league_row_h` | ×1.1 | 임시 | T331 lock 뒤 · 정본 2320 `.league-list gap .6rem` · 2326 `.league-row padding .18/.5/.42rem` — 화소까지 닫힌 첫 자리(94) |
| `Ui/LeagueSheet.cs` | `league_bar_w` | ×1.25 | 임시 | T331 lock 뒤(72 · `* w * 1.25f`) |
| `Ui/LeagueSheet.cs` | `league_bar_h` | ×1.6 | 임시 | T331 lock 뒤(72) |
| `Ui/LeagueSheet.cs` | `league_score_h` | ×1.6 | 임시 | T331 lock 뒤(137) |
| `Ui/LeagueSheet.cs` | `lgr_ribbon_h` | ×1.2 | 임시 | T331 lock 뒤(188) |
| `Ui/LeagueSheet.cs` | `lgr_tier_h` | ×1.15 | 임시 | T331 lock 뒤(241) |
| `Ui/LeagueSheet.cs` | `lgr_rank_w` | ×1.5 | 임시 | T331 lock 뒤(242) |
| `Ui/LeagueSheet.cs` | `lc_pill_h` | ×1.4 | 임시 | T331 lock 뒤(311 · `* w * 1.4f`) |
| `Ui/LeagueSheet.cs` | `lc_pill_w` | ×1.3 | 임시 | T331 lock 뒤(321 · `* w * 1.3f`) |
| `Ui/ForgeCraftPopup.cs` | `btn_h` | ×1.7 | 임시 | T331 lock 뒤 · 큰 버튼 둘(71·72) — 정본이 그 버튼에 준 높이와 대조 |
| `Ui/ForgeCraftPopup.cs` | `btn_h` | ×1.5 | 임시 | T331 lock 뒤 · 버튼 둘(119·120) |
| `Ui/QuestSheet.cs` | `quest_btn_w` | ×1.6 | 임시 | T331 lock 뒤(46) |
| `Ui/QuestSheet.cs` | `quest_btn_h` | ×1.3 | 임시 | T331 lock 뒤(46) |
| `Ui/QuestSheet.cs` | `quest_bar_h` | ×1.6 | 임시 | T331 lock 뒤(56) |
| `Ui/ForgeAutoPopup.cs` | `btn_h` | ×1.9 | 임시 | T331 lock 뒤 · 시작 버튼 둘(61·125) |
| `Ui/ProfilePopup.cs` | `settings_toggle_h` | ×1.1 | 임시 | T378 3회차가 잰 값: 정본 3121 `.settings-act` 는 높이를 안 주고 `line-height 1.15rem + padding .1rem×2 + ol2(.125rem)×2 = 1.6rem`(= 0.0303H) — 지금은 토글 높이 1.35rem×1.1 = 1.485rem 이라 8% 낮다 · **새 키 `settings_act_h` 0.0303** 이 필요한데 `catalog.json` 이 T365·T375 lock(336) |
| `Ui/ShopSheet.cs` | `shop_cur_h` | ×1.6 | 임시 | T378 3회차: 정본 `style.css` 에 `.shop-cur*` 선택자가 없다(`shop-gems`·`shop-gem-card` 뿐) — 상점 머리 통화 막대(`coin-bar`·`gem-bar`)가 정본의 어느 규칙인지(`ui.js` 상점 머리) 먼저 찾아야 표값 2.39%H 와 ×1.6 중 무엇이 정본인지 가릴 수 있다(47) |
| `Ui/ForgeInfoPopup.cs` | `btn_h` | ×1.7 | 임시 | T332 lock 뒤(98) |
