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

## 원작 아이콘 아틀라스 (T31 · 정본에서 기계로 뽑은 그림 · 손으로 안 고친다)

| 용도 | 경로 | GUID(.meta) | 비고 |
|---|---|---|---|
| 원작 `IconGen` 아이콘 136 + 아바타 24 + tint 변형 10 = 키 170 | `Assets/Forge/Icons/Resources/Icons/atlas-0.png`(2048×1948) · `atlas-1.png`(2048×652) · `atlas.json` | `564cffe9da4542b55a4fe95928364c6c` · `a6853e53073f689676d96f91847713b6` · `568bf8d1ccc0cfb2586adf4fc6ecc12d` | `tools/export_icons.js` 가 정본 `web/js/icongen.js`+`avatars.js` 를 headless Chromium 으로 그려 낸다 · `tools/check_icons_sync.sh` 가 CI 에서 정본과 대조 · 런타임 `UiIcons.Get(키)` → `Sprite`(§1 캡처 PNG 금지의 예외) |

- `UiKit.Icon` 은 이 아틀라스를 먼저 보고 없는 키만 위 GUI PRO Kit 표로 폴백한다(결정 38) — 표의 `coin`·`gem`·`power`·`xmark`·`tab_*` 줄은 **원작 그림으로 대체됐다**(줄은 폴백용으로 남긴다).

## 코드 생성 도형 (그림 파일 아님 · `UiShapes`)

- `UiShapes.Circle` · `UiShapes.Rounded`(9-슬라이스 둥근 사각) — 알약 · 프로필 카드 · 아바타 타일 · 웨이브 노드 · 탭 ✕ 원. 원작 IconGen 이 캔버스로 그리던 원시 도형과 같은 길이라 새 그림을 들이지 않는다.

## 아직 안 쓴 주인 에셋 (뒤 작업이 쓸 때 여기 옮긴다)

- `Plugins/Demigiant/DOTween` — UI 트윈 · `Plugins/AllIn1SpriteShader`.
- 킷 `Component/Popup/Popup_Frame01_White.png`·`Popup_Frame01_Navy.png`·`Button/Btn_MainButton_White.Png`·`Slider02_*` — 팝업·버튼·진행바(T19~T22). `.meta` 의 `spriteBorder` 가 0 이라 9-슬라이스로 쓰려면 주인 에디터에서 border 를 잡아야 한다(코드에서 텍스처를 손대지 않는다).

## 히트 파티클 (T8 · Cartoon FX Remaster · `Assets/Forge/Resources/FxCatalog.asset` → `FxCatalog.Play(키)`)

> 원작이 캔버스 텍스처로 그리던 플레어·링·점광의 자리(결정 68). 파편·불티는 큐브 파티클 코드(`CubeParticles`)라 에셋이 아니다. 경로는 `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/` 아래. GUID 는 `.meta` · 카탈로그 YAML 은 프리팹 루트 GameObject fileID 를 가리킨다.

| 용도 키 | 프리팹 | GUID | 원작 자리 |
|---|---|---|---|
| `hit` | `Impacts/CFXR Hit D 3D (Yellow).prefab` | `265d3a5ba69981a4698ceabcedf368b0` | `hitEnemy` 일반 접촉 플레어(청백·노랑 불티) |
| `crit` | `Impacts/CFXR Hit A (Red).prefab` | `5c755f9bc5253ea418e919994537dcc7` | `hitEnemy` 크리 플레어(주황) |
| `kill` | `Eerie/CFXR2 WW Enemy Explosion.prefab` | `7ec363c8df426644ea85f8d6d570561c` | `killEnemy` 버스트(코어·중간·외곽 플레어 + 점광 + 링) |
| `bossKill` | `Explosions/CFXR Explosion 1.prefab` | `3ef3ae421f71c5c4e97fe12dc2fc6312` | `killEnemy` 보스 확대판 |
| `bossLand` | `Impacts/CFXR2 Ground Hit.prefab` | `87ee7299dd44ed747af24dbb91bcd33b` | `spawnEnemy` 보스 착지 먼지 파동(expandRing 0xbcaaa4 1.8) |
| `heroHit` | `Impacts/CFXR Hit A (Red).prefab` | `5c755f9bc5253ea418e919994537dcc7` | `heroHit` 붉은 틴트·림 자리 |
| `revive` | `Misc/CFXR Magic Poof.prefab` | `c4f829cb7dd4b864caf8832ec5b1ee55` | `heroRevive` 초록 링(0x9be7a0 1.3) |

## 복셀 재질 (T4 · 코드 생성 메시의 정점 색 셰이더)

| 용도 | 경로 | GUID(.meta) | 셰이더 | 비고 |
|---|---|---|---|---|
| 몹 파츠 재질 원형(조명) | `Assets/Forge/Resources/VoxelLit.mat` | `c2b614c31ac2a9874396c1e389a78bc1` | `Universal Render Pipeline/Particles/Lit`(GUID `b7839dad95683814aa64166edc107ae2`) | `VoxelMaterials` 가 matKey 마다 복제해 opacity·emissive·rough 를 준다 · 정점 색 × 흰색(결정 13) |
| 몹 파츠 재질 원형(무조명 `mat.basic`) | `Assets/Forge/Resources/VoxelUnlit.mat` | `bdfce4d75d7fdd9d1d89e9c14f356029` | `Universal Render Pipeline/Particles/Unlit`(GUID `0406db5a14f94604a8c57ccfbc9f3b46`) | 표에 basic 파츠는 아직 없다(정본 `matKey` 가 갈래를 갖고 있어 같이 둔다) |
