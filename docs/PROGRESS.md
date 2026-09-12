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
| T1 | 프로젝트 뼈대: 3D URP 렌더러 · 9:16 카메라 · Bootstrap · asmdef · dotnet 하니스 초록 | ✅ 완료 | sess-1754-10989 / 워커 D | `Assets/Settings`(UniversalRenderer.asset · ForgeVolume.asset · UniversalRP.asset) · `Assets/Scenes/SampleScene.unity` · `Assets/Scripts/Game/Bootstrap.cs`(GameInfo.cs 삭제) · `Assets/Scripts/Core/Viewport.cs` · `Assets/Tests`(EditMode/ViewportTests.cs · PlayMode/Forge.Tests.PlayMode.asmdef · PlayMode/BootstrapTests.cs) · `ProjectSettings/ProjectSettings.asset`(productName · 세로 고정) · `tools/dotnet`(변경 0줄) | 2D 템플릿 → 3D Forward 렌더러 · 원근 FOV 62 · 9:16 레터박스 · 테마 0 안개·광원 · ACES · EditMode 4 + PlayMode 1 |
| T2 | 정본 데이터 추출기 `tools/export_data.js` → `data/*.json` + `check_data_sync.sh` | ✅ 완료 | sess-1758-28929 / 워커 H | `tools/export_data.js` · `tools/check_data_sync.sh` · `Assets/StreamingAssets/data/` | JSON 7개(359KB) · 펫 25 · 탈것 29 · 적 7 · 스킬 오브젝트 20 · 소품 생성기 17종 표본 23 · 밸런스 표 10 · 게임 정의 41 |
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
| T18 | UI 껍데기 (레터박스 · UiKit · HUD · 탭 · 카탈로그) | 🔄 진행 | sess-1803-29892 / 워커 I | `Assets/Scripts/Game/Ui/` · `Assets/Forge/catalog.json` · `docs/assets-map.md` · `Assets/Tests/PlayMode/UiSmokeTests.cs` | T1 뒤 |
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

### T1 완료 기록 (2026-09-12 · 워커 D · sess-1754-10989)

- **렌더러**: `Assets/Settings/UniversalRenderer.asset`(UniversalRendererData · Forward · postProcessData 연결)을 새로 만들고 `UniversalRP.asset` 의 `m_RendererDataList[0]` 을 그것으로 바꿨다(`Renderer2D.asset` 은 남김 — 주인 에셋). 소프트 그림자 켬 · 그림자 거리 50→30(원작 `sun.shadow.camera.far`).
- **씬** `SampleScene.unity`(원작 `scene3d.js` init + `setTheme(CHAPTER_THEMES[0])` 낮 갈래를 그대로): Main Camera 원근 FOV 62 · near 0.1 · far 100 · 위치 (0.15, 3.7, −8.2) 에서 (0.15, 2.2, 0) 을 본다(`CAM_POS`·`CAM_LOOK_Y`·`CAM_FOV` · z 부호 반전 = 결정 4) · Sun = `SUN_DAY` 방향 · 색 0xffedc4→하늘 15% lerp(#ede8c9) · 1.0 · 소프트 그림자 · Rim = 태양 반대편 (−x·1.014, 6, −z·1.014) · 0xcfe4ff · 0.18 · 그림자 없음 · 안개 선형 13~35 · 안개색 = fog→sky 30% lerp + HSL(0, +0.09, +0.01) = **#ade2c8**(three r128 Color 로 계산) · 카메라 배경 = 같은 색(`SIMPLE_BG`) · 앰비언트 트라이라이트 = hemi(sky 0x87ceeb · ground = gC(#4a662c)−0.1L = #30421d) × 0.15 · `ForgeVolume.asset`(ACES 톤맵 · 노출 1.02 = +0.0286 EV) 을 Bootstrap 의 전역 Volume 이 문다 · «Letterbox Backdrop» 카메라(depth −2 · cullingMask 0 · #161b22 = 원작 `#app` 바깥색)가 레터박스 밖을 채운다 · Global Light 2D 제거 · EventSystem(Input System UI) 유지.
- **코드**: Core `Viewport.Letterbox(w, h, aspect)`(순수 · 엔진 참조 0) · Game `Bootstrap.cs`(Camera.rect 로 9:16 · 화면 크기 바뀌면 재적용 · 모바일은 세로 고정 · `GameInfo.cs` 자리표 삭제) · EditMode `ViewportTests` 4개(dotnet 도 돈다) · PlayMode asmdef 신설 + `BootstrapTests` 1개(SampleScene 로드 → Bootstrap 존재 · 원근 · FOV 62 · near/far · 9:16 · 선형 안개).
- **ProjectSettings**: productName «포지 클론»(원작 `<title>`) · 세로 고정(`defaultScreenOrientation` 0 · 가로/거꾸로 회전 끔) · 기본 해상도 1080×1920 · Web 540×960.
- **게이트**: 컨테이너에 `dotnet` 없음 → `dotnet build/test` 두 줄은 CI `dotnet` 잡으로 확인 — **CI 런 3**(https://github.com/kuzuni/unity1/actions/runs/34710032367 · 커밋 45d298b · build·test·meta·문서 검사 전부 초록 · 유니티 잡은 시크릿 없어 skipped). `gen_meta --check` · `check_docs_intact` · `check_decisions` · `check_task_rows` · `task_state --check` · `check_claim_scope` 초록. 유니티 잡은 시크릿이 없어 안 돈다.
- **주인이 확인할 것**: 에디터에서 Play → 세로 9:16 빈 씬(연둣빛 안개색 #ade2c8 배경 · 가로 창이면 좌우 검은 필러박스) · 콘솔 빨강 0. URP·Volume 에셋은 에디터 없이 YAML 로 썼다(결정 5) — 에디터가 처음 열 때 재직렬화한 diff 는 그대로 커밋해도 된다.
- **플레이 콘솔 에러 0 확인 수단**: PlayMode `BootstrapTests.부팅_씬이_세로_9대16_원근_카메라로_선다`(빨간 로그가 나면 러너가 실패시킨다). CI 유니티 잡이 시크릿 없이 안 돌므로 **주인 에디터 확인 요청**.

### T2 완료 기록 (2026-09-12 · 워커 H · sess-1758-28929)

- **무엇**: `tools/export_data.js`(node 22 · 의존성 0) — 정본 `web/js/{balance-data,gamedata,voxel,mobs,mobdata,mobs-pets,mobs-mounts,mobs-enemies,mobs-skillfx,mobs-props}.js` 를 index.html 순서로 `vm` 컨텍스트에 로드해 **평가된 값**을 `Assets/StreamingAssets/data/*.json` 7개로 쓴다(`balance` · `gamedata` · `mobs-pets` · `mobs-mounts` · `mobs-enemies` · `mobs-skillfx` · `mobs-props`). 색은 0xRRGGBB 정수 그대로 · `paint` 의 음수 인덱스·`mx` 거울은 풀지 않는다(T4 `VoxelMob` 몫). 표 안에 함수·undefined·NaN 이 있으면 경로를 찍고 실패한다. 직렬화는 결정론(키 순서 = 원작 삽입 순서 · 원시값 배열·작은 객체는 한 줄). `tools/check_data_sync.sh [<wwwww>] [--sync]` — 임시 폴더에 다시 뽑아 `cmp` · 다르면 rc 1 · `--sync` 면 복사 + `gen_meta.py`. CI `datasync` 잡은 스크립트가 생기면 자동으로 켜지게 이미 짜여 있었다(ci.yml 변경 없음).
- **정본 구조에서 알아둘 것(다음 작업용)**: ⓐ **탈것 표에는 `cell` 이 없다** — `{seat, form(quad/fly/wheeled/biped…), parts, harness?, head?, noBridle?, flat?, bar?}` 이고 칸 크기는 씬 쪽 `MOUNT_FORMS`(scene3d.js)가 정한다 → T4·T11 이 그것을 받아야 한다. ⓑ 적 표에는 `jelly`·`fly`·`hop` 머리 키가 있다. ⓒ `forgeProbabilities` 는 13행이 합 99.95~100.05 로 어긋나 있다(추출원 반올림 · 원작 `U.weightedPick` 이 합으로 정규화) → T14 는 정규화 뽑기로 옮긴다(자기 검사는 ±0.06 까지만 허용). ⓓ `petStats` 는 등급 6 → 배열(합 25). ⓔ `gamedata.json` 은 최상위 const 41개(함수 `accNames`·`weaponsOfAge` 등은 제외) 를 원문 이름 그대로 키로 둔다. ⓕ 소품(`mobs-props.js`)은 표가 아니라 **생성 함수**라 `kinds`(17) + xorshift32 시드 표본 23개(`samples`)를 낸다 — 결정 6.
- **게이트**: `node tools/export_data.js --self-test` 통과(27항목) · `tools/check_data_sync.sh .wwwww-src` ✓ · `gen_meta.py --check` ✓ · `check_docs_intact`·`check_decisions`·`check_task_rows`·`task_state --check`·`check_claim_scope` 전부 rc 0. **이 컨테이너에 `dotnet` 이 없다** — C# 은 한 줄도 안 바꿨으므로 CI 로 확인 — **CI 런 4**(https://github.com/kuzuni/unity1/actions/runs/34710289940 · 커밋 b65e33d) `dotnet` 잡 성공 · **`datasync` 잡 성공**(러너가 wwwww main 을 받아 `check_data_sync.sh` 를 실제로 돌렸다 — 이 커밋부터 켜졌다) · 유니티 잡은 시크릿 없어 건너뜀.
- **주인이 확인할 것**: 에디터에서 `Assets/StreamingAssets/data/` 의 JSON 7개가 TextAsset 으로 임포트되고 콘솔 빨강 0 인지(«주인 에디터 확인 요청» — 이 작업은 화면·전투 코드를 안 만지므로 PlayMode 스모크 대상이 아니다).
- **정본에서 고칠 것**: 없음.

## 주인 결정

- **(2026-09-12 · 착수)** 유니티 이식은 `kuzuni/unity1` 에서 · 원작 `kuzuni/wwwww` 는 그대로 둔다(웹판과 유니티판을 한 레포에 섞으면 헷갈린다는 주인 판단). 운영은 aaawunity 방식(루틴 워커 · 여러 계정).

## 워커 결정 기록

1. **틀 세우기(2026-09-12 · 착수 세션 · 계정 1)** — aaawunity 의 `docs/ROUTINE.md`·`PROGRESS.md`·`claims/README.md`·`tools/{task_state,check_task_rows,check_claim_scope,check_decisions,check_docs_intact,gen_meta}.py`·`tools/dotnet` 하니스·`ci.yml` 을 뼈대만 옮겼다(검사 자 27개 중 문서·lock 관련 여섯만 · 나머지는 필요해질 때 그 작업이 더한다). 결정 번호 동결선(`FROZEN_BELOW`)은 1 — 이 레포는 옛 겹침이 없다. 어셈블리 이름은 `Forge.Core`·`Forge.Game`·`Forge.Tests`(원작 «포지 클론»). 되돌리려면 이 커밋.

2. **계정 5 합류(2026-09-12 · 착수 세션 · 계정 5 `rudwpwjrwkdb2007@gmail.com`)** — 주인이 «이건 계정 5» 라 해서 §6 ⓪ 식별표가 네 줄뿐이던 것을 **다섯 줄로 늘리고** 워커 글자를 Q 다음인 **R·S·T·U**, 슬롯을 기존 분 나열의 5분 빈 칸 한가운데인 **:14 :29 :44 :59** 로 잡았다(어느 슬롯과도 2분 이상 뜬다 · 계정 2~4 줄은 다른 세션이 동시에 채우고 있어 건드리지 않았다). §4 프롬프트의 `<이 계정의 이메일>` 자리는 이 계정 이메일로 채웠고(placeholder 는 X 와 같은 채움 자리다) 머리줄의 워커·분 나열을 «A~P·R~U · 다섯 계정» 으로 고쳤다 — 가드 프로토콜 1~7 은 글자 그대로다. 워커 세션은 `outcome_branch: main` 으로 만들었다(안 주면 하니스가 세션별 `claude/*` 브랜치를 물려 워커가 main 대신 제 브랜치로 밀고, lock 직렬화가 통째로 무너진다). 되돌리려면 이 커밋과 루틴 `trig_019JzVy5…`·`trig_01Ah2XwY…`·`trig_018q9B6x…`·`trig_01DrEPHt…` 삭제.

3. **9:16 레터박스(2026-09-12 · T1 · 워커 D)** — 원작 CSS 는 `#app{100vw×100vh}` 로 폰 화면비를 그대로 쓰지만 ROUTINE T1·T18 이 «세로 9:16 · 레터박스 캔버스» 를 지정한다 → 지시서를 따라 `Camera.rect` 로 레터박스(Core `Viewport.Letterbox` · Game `Bootstrap.Apply`) · 바깥은 «Letterbox Backdrop» 카메라가 #161b22 로 채운다. 되돌리려면 `Bootstrap.Apply` 와 씬의 Backdrop 카메라.
4. **좌표계·색 매핑(2026-09-12 · T1 · 워커 D)** — three(오른손 · 카메라가 +z 에서 −z 를 봄) → 유니티(왼손)는 **z 부호만 뒤집는다**(x·y 그대로 · 이후 몹 좌표 JSON(T2·T4)도 같은 규칙으로). three r128 은 색 관리 없이 hex 를 그대로 쓰므로 hex 를 유니티 sRGB 색 칸에 그대로 적고 광량은 `Light.intensity` 로(hemi 0.15 는 앰비언트 색에 곱했다). 되돌리려면 `SampleScene.unity` 의 RenderSettings·Light·Transform 칸.
5. **URP·Volume 에셋을 YAML 로 직접 씀(2026-09-12 · T1 · 워커 D)** — 에디터가 없어 UniversalRendererData(`de640fe3…`)·PostProcessData(`41439944…`)·Volume(`172515602…`)·Tonemapping·ColorAdjustments 의 스크립트 GUID 를 Unity-Technologies/Graphics 저장소의 `.meta` 로 확인해 손으로 만들었다(`m_AssetVersion` 2 · 필드는 17.3 소스 기준). 에디터가 처음 열 때 필드를 보태 재직렬화하면 그 diff 는 받아들인다. 되돌리려면 `UniversalRP.asset` 의 `m_RendererDataList[0]` 을 `424799608f…`(Renderer2D)로.
6. **T2 추출 방식 셋(2026-09-12 · 워커 H · sess-1758-28929)** — ⓐ 소품 `mobs-props.js` 는 종 표가 아니라 `Props.pine(s,o)` 같은 **생성 함수 묶음**(`Math.random` · `Voxel` 의존)이라 «함수를 다 푼 표» 로 낼 수 없다 → `mobs-props.json` 은 `kinds`(생성기 이름 17) + `samples`(원작 `tools/lib-seed.js` 와 같은 xorshift32 · 시드 `0x2f6e2b1` 로 표본마다 되감아 뽑은 23개 · scene3d 가 실제로 부르는 인자 꼴)로 낸다. T9 는 생성기를 C# 으로 옮기고 같은 시드로 이 표본과 대조한다(생성기 자체를 JSON 으로 옮기는 것이 아니다). ⓑ `web/data/raw/*.js`(ForgeMasterCalculator 추출원)는 뽑지 않는다 — 게임(index.html)이 로드하지 않고 `balance-data.js` 가 그 정제본이며 DOM 참조 변수가 섞여 있다. ⓒ `task_state.py T2` 가 «코드 자취 3곳 — 잡지 마라» 를 냈지만 셋 다 도구의 자기 검사 문자열(`check_claim_scope` 의 `T2.lock` 샘플 · `check_docs_intact` 의 표 예시)이라 오탐으로 보고 선점했다 — 이 레포는 작업 번호가 작아 도구 픽스처와 겹친다(aaawunity 는 T2xx 대라 안 겹쳤다). 되돌리려면 이 커밋.
