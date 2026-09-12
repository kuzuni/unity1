# 에셋 지도 — 용도 키 → 주인 에셋 (ROUTINE §1 «에셋은 주인 에셋만» · T18)

> 런타임은 `Assets/Forge/catalog.json`(정본) → `python3 tools/gen_ui_catalog.py` → `Assets/Forge/Resources/UiCatalog.asset` 로 읽는다. 새 에셋을 쓰면 **여기 한 줄 + catalog.json 한 줄** 을 같이 더하고 생성기를 다시 돌린다. GUID 는 그 파일의 `.meta`.

## 글꼴

| 용도 | 경로 | GUID | 비고 |
|---|---|---|---|
| UI 글자(모든 TextKind) | `Assets/Fonts/NotoSans-Regular.ttf` | `559ed32ebb203e74780aa2b6e9e283c0` | 런타임 `TMP_FontAsset.CreateFontAsset`. **한글 cmap 없음**(실측 2026-09-12) → OS 글꼴 폴백(원작 CSS font-family 순서: Malgun Gothic · Apple SD Gothic Neo · Noto Sans CJK KR · Noto Sans KR · NanumGothic). 한글 글꼴을 주인이 넣으면 catalog.json `font` 를 그것으로. |

## GUI PRO Kit - Casual Game (스프라이트)

| 용도 키 | 경로 (`Assets/GUI PRO Kit - Casual Game/ResourcesData/Sprite/` 아래) | GUID | 쓰는 곳 |
|---|---|---|---|
| `tab_pvp` | `Demo/Demo_Icon_ItemIcons(Original)/Icon_Battle.Png` | `e35fc472b6ada425894eef3b044e099f` | 탭바 PVP(원작 IconGen `tab_pvp` 해골+단검 자리) |
| `tab_dungeon` | `Demo/Demo_Icon_ItemIcons(Original)/Dungeon.Png` | `93e4230214e877e4089e18a8e09ae21f` | 탭바 던전(원작 문) |
| `tab_summon` | `Demo/Demo_Icon_ItemIcons(Original)/Icon_Potion02_Green.Png` | `0e2f4307e2b46440d8b1018c7ed04849` | 탭바 소환(원작 물약) |
| `tab_quest` | `Demo/Demo_Icon_ItemIcons(Original)/Icon_Quest.Png` | `1f75d46fd305144cdb45d27034ef7382` | 탭바 퀘스트(원작 두루마리 깃발) |
| `tab_shop` | `Demo/Demo_Icon_ItemIcons(Original)/Icon_Shop.Png` | `376de8be156ba41c5a1fa0f32c7c7670` | 탭바 상점 |
| `coin` | `Demo/Demo_Icon_ItemIcons(Original)/Icon_Gold.Png` | `3fb23bf6552b44aafb168ed0584e1641` | 상단바 코인 알약 |
| `gem` | `Demo/Demo_Icon_ItemIcons(Original)/Icon_Gems.Png` | `f434a60f1f85c47e085d09cab598d63d` | 상단바 젬 알약 |
| `power` | `Demo/Demo_Icon_ItemIcons(Original)/Icon_Sword.Png` | `169d45648e00348038299fc345aa6b4f` | 프로필 카드 전투력(원작 IconGen `power`) |
| `chat` | `Demo/Demo_Icon_ItemIcons(Original)/Icon_ImageIcon_Chat.Png` | `0fbc8cc851081b34d95122218868977b` | 채팅 미리보기 말풍선 |
| `xmark` | `Demo/Demo_Icon/Icon_PictoIcon_Close01.png` | `70f78e062098c4496b852abefff63acf` | 탭바 빨간 ✕ 안 표시(원작 IconGen `xmark`) |

## 코드 생성 도형 (그림 파일 아님 · `UiShapes`)

- `UiShapes.Circle` · `UiShapes.Rounded`(9-슬라이스 둥근 사각) — 알약 · 프로필 카드 · 아바타 타일 · 웨이브 노드 · 탭 ✕ 원. 원작 IconGen 이 캔버스로 그리던 원시 도형과 같은 길이라 새 그림을 들이지 않는다.

## 아직 안 쓴 주인 에셋 (뒤 작업이 쓸 때 여기 옮긴다)

- `JMO Assets/Cartoon FX Remaster` — 히트 파티클(T8) · `Plugins/Demigiant/DOTween` — UI 트윈 · `Plugins/AllIn1SpriteShader`.
- 킷 `Component/Popup/Popup_Frame01_White.png`·`Popup_Frame01_Navy.png`·`Button/Btn_MainButton_White.Png`·`Slider02_*` — 팝업·버튼·진행바(T19~T22). `.meta` 의 `spriteBorder` 가 0 이라 9-슬라이스로 쓰려면 주인 에디터에서 border 를 잡아야 한다(코드에서 텍스처를 손대지 않는다).
