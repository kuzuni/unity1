# PROGRESS — 포지 클론 유니티 이식 (kuzuni/unity1)

> 갱신 규약은 `docs/ROUTINE.md` §4. 정본은 `kuzuni/wwwww` `web/`(읽기 전용 · 변경 금지). 수치는 `Assets/StreamingAssets/data/*.json`(T2 가 정본에서 뽑는다).

## 주인 콘솔 에러 보고함

> 주인이 에디터·폰에서 본 빨간 줄·이상을 여기 적는다. 워커는 매 회차 이것을 읽고 «가장 큰 번호 +1» 로 등재한다(UI 작업보다 우선).

- (비어 있음)

### 검수 Q 보고
- (비어 있음)

## 작업 상태

| ID | 작업 | 상태 | SID / 워커 | 범위 | 핵심 |
|---|---|---|---|---|---|
| T1 | 프로젝트 뼈대: 3D URP 렌더러 · 9:16 카메라 · Bootstrap · asmdef · dotnet 하니스 초록 | ⬜ 대기 | — | `Assets/Settings` · `Assets/Scenes` · `Assets/Scripts/Game/Bootstrap.cs` · `Assets/Tests` · `ProjectSettings/ProjectSettings.asset` · `tools/dotnet` | 2D 템플릿 → 3D Forward 렌더러 |
| T2 | 정본 데이터 추출기 `tools/export_data.js` → `data/*.json` + `check_data_sync.sh` | ⬜ 대기 | — | `tools/export_data.js` · `tools/check_data_sync.sh` · `Assets/StreamingAssets/data/` | 펫 25 · 탈것 29 · 적 7 · 밸런스 표 |
| T3 | Core `MiniJson` · `GameData` · `Rng` · `BigNum` | ⬜ 대기 | — | `Assets/Scripts/Core/Data/` · `Core/BigNum.cs` · `Assets/Tests/EditMode/DataTests.cs` | T2 뒤 |
| T4 | `VoxelMob` 박스 모델 조립기 (Voxel.build + Mobs.build 규약) | ⬜ 대기 | — | `Assets/Scripts/Core/Voxel/` · `Assets/Scripts/Game/Voxel/` · `Assets/Tests/EditMode/VoxelTests.cs` | T2·T3 뒤 |
| T5 | 몹 도감 씬 + 시트 촬영 (전 종 예외 0 · 치수 단언) | ⬜ 대기 | — | `Assets/Scripts/Game/Gallery/` · `Assets/Tests/PlayMode/MobGalleryTests.cs` | T4 뒤 |
| T6 | 영웅 박스 모델 + 무기 파지 + 대기/걷기/스윙 클립 | ⬜ 대기 | — | `Assets/Scripts/Game/Hero/` · `Assets/Tests/PlayMode/HeroTests.cs` | T4 뒤 |
| T7 | Core 전투 엔진 (100ms 틱 · 웨이브 4+보스 · sim 대조) | ⬜ 대기 | — | `Assets/Scripts/Core/Battle/` · `tools/sim/` · `Assets/Tests/EditMode/BattleTests.cs` | T3 뒤 |
| T8 | 전투 씬 (적 스폰·애니 계약·데미지 숫자·셰이크·파티클) | ⬜ 대기 | — | `Assets/Scripts/Game/Battle/` · `Assets/Tests/PlayMode/BattleSceneTests.cs` | T6·T7 뒤 |
| T9 | 맵·바이옴 (테마 10 · 소품 배치 · 지면·안개·광원) | ⬜ 대기 | — | `Assets/Scripts/Game/World/` · `Assets/Tests/PlayMode/WorldTests.cs` | T4 뒤 |
| T10 | 펫 출전 3마리 (대형 · 따라오기 · 관절 드라이버) | ⬜ 대기 | — | `Assets/Scripts/Game/Pets/` · `Assets/Tests/PlayMode/PetSceneTests.cs` | T8 뒤 |
| T11 | 탈것 (서서 타기 · hover · 드라이버) | ⬜ 대기 | — | `Assets/Scripts/Game/Mounts/` · `Assets/Tests/PlayMode/MountSceneTests.cs` | T6·T10 뒤 |
| T12 | 스킬 오브젝트 (로봇·표창×10·드래곤) | ⬜ 대기 | — | `Assets/Scripts/Game/SkillFx/` · `Assets/Tests/PlayMode/SkillFxTests.cs` | T8 뒤 |
| T13 | 세이브·오프라인 보상 | ⬜ 대기 | — | `Assets/Scripts/Core/Save/` · `Assets/Scripts/Game/SaveIo.cs` · `Assets/Tests/EditMode/SaveTests.cs` | T3 뒤 |
| T14 | Core 대장간 (비용·시간·시대 확률·오토) | ⬜ 대기 | — | `Assets/Scripts/Core/Forge/` · `Assets/Tests/EditMode/ForgeTests.cs` | T3 뒤 |
| T15 | 장비 8부위 + 페이퍼돌 | ⬜ 대기 | — | `Assets/Scripts/Core/Gear/` · `Assets/Scripts/Game/Hero/Paperdoll.cs` · `Assets/Tests/EditMode/GearTests.cs` | T6·T14 뒤 |
| T16 | Core 펫 시스템 (알·부화·스탯·합성) | ⬜ 대기 | — | `Assets/Scripts/Core/Pets/` · `Assets/Tests/EditMode/PetTests.cs` | T3 뒤 |
| T17 | Core 스킬 시스템 (소환·18종·4슬롯) | ⬜ 대기 | — | `Assets/Scripts/Core/Skills/` · `Assets/Tests/EditMode/SkillTests.cs` | T7 뒤 |
| T18 | UI 껍데기 (레터박스 · UiKit · HUD · 탭 · 카탈로그) | ⬜ 대기 | — | `Assets/Scripts/Game/Ui/` · `Assets/Forge/catalog.json` · `docs/assets-map.md` · `Assets/Tests/PlayMode/UiSmokeTests.cs` | T1 뒤 |
| T19 | UI 패널: 대장간 · 장비 | ⬜ 대기 | — | `Assets/Scripts/Game/Ui/Forge*` · `Ui/Gear*` · `Assets/Tests/PlayMode/ForgeUiTests.cs` | T14·T15·T18 뒤 |
| T20 | UI 패널: 펫 · 스킬 · 탈것 | ⬜ 대기 | — | `Assets/Scripts/Game/Ui/Pet*` · `Ui/Skill*` · `Ui/Mount*` · `Assets/Tests/PlayMode/PetUiTests.cs` | T16·T17·T18 뒤 |
| T21 | UI 패널: 던전 · 기술트리 · 승천 | ⬜ 대기 | — | `Assets/Scripts/Game/Ui/Dungeon*` · `Ui/Tech*` · `Ui/Ascend*` · `Assets/Tests/PlayMode/DungeonUiTests.cs` | T23·T24·T18 뒤 |
| T22 | UI 패널: 상점 · 패스 · 퀘스트 · 리그 · 채팅 · 설정 | ⬜ 대기 | — | `Assets/Scripts/Game/Ui/Shop*` · `Ui/Pass*` · `Ui/Quest*` · `Ui/League*` · `Ui/Chat*` · `Ui/Settings*` · `Assets/Tests/PlayMode/ShopUiTests.cs` | T25·T18 뒤 |
| T23 | Core 던전 4종 | ⬜ 대기 | — | `Assets/Scripts/Core/Dungeons/` · `Assets/Tests/EditMode/DungeonTests.cs` | T7 뒤 |
| T24 | Core 기술트리 · 승천 | ⬜ 대기 | — | `Assets/Scripts/Core/Tech/` · `Core/Ascension/` · `Assets/Tests/EditMode/TechTests.cs` | T3 뒤 |
| T25 | Core 상점 · 패스 · 퀘스트 · 리그 | ⬜ 대기 | — | `Assets/Scripts/Core/Meta/` · `Assets/Tests/EditMode/MetaTests.cs` | T3 뒤 |
| T26 | WebGL 템플릿 · 배포 스모크 · Android 잡 | ⬜ 대기 | — | `Assets/WebGLTemplates/` · `tools/webgl_smoke.js` · `.github/workflows/ci.yml` | T18 뒤 |
| T27 | PlayMode 스모크·플레이 봇·촬영 | ⬜ 대기 | — | `Assets/Tests/PlayMode/PlaythroughTests.cs` · `UiShotsTests.cs` · `PlayLog.cs` | T19~T22 뒤 |
| T28 | 원작 대조 회차 (`docs/ref-layout.md` · `tools/ui_score.py`) | ⬜ 대기 | — | `docs/ref-layout.md` · `tools/ui_score.py` | T27 뒤 |

## 주인 결정

- **(2026-09-12 · 착수)** 유니티 이식은 `kuzuni/unity1` 에서 · 원작 `kuzuni/wwwww` 는 그대로 둔다(웹판과 유니티판을 한 레포에 섞으면 헷갈린다는 주인 판단). 운영은 aaawunity 방식(루틴 워커 · 여러 계정).

## 워커 결정 기록

1. **틀 세우기(2026-09-12 · 착수 세션 · 계정 1)** — aaawunity 의 `docs/ROUTINE.md`·`PROGRESS.md`·`claims/README.md`·`tools/{task_state,check_task_rows,check_claim_scope,check_decisions,check_docs_intact,gen_meta}.py`·`tools/dotnet` 하니스·`ci.yml` 을 뼈대만 옮겼다(검사 자 27개 중 문서·lock 관련 여섯만 · 나머지는 필요해질 때 그 작업이 더한다). 결정 번호 동결선(`FROZEN_BELOW`)은 1 — 이 레포는 옛 겹침이 없다. 어셈블리 이름은 `Forge.Core`·`Forge.Game`·`Forge.Tests`(원작 «포지 클론»). 되돌리려면 이 커밋.
