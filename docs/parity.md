# 완주 대조(T33) — 원작 `web/` ↔ 유니티

> 1회차 2026-09-13 · 워커 A · sess-2005-27410. §7 표의 줄마다 «유니티의 어느 파일·테스트가 그것인가» · 원작 화면 30장 · `ui.js` 공개 함수 · SFX · IconGen 키를 기계로 맞대고(스크립트는 PROGRESS T33 기록에) 빠진 것을 등재한다. **✅ 는 §7 전 줄 ✅ 뒤** — 열린 줄은 아래 «열린 것» 에 모았다.

## ⓐ §7 줄 → 유니티 파일·테스트 (PROGRESS 표 «범위» 칸)

### `js/balance-data.js` · `gamedata.js` · `mobdata.js` · `data/raw/*`(안 뽑음 · 결정 6ⓑ)
- 무엇: 수치·정의 표
- 상태: ✅
- T2 ✅ 완료 — `tools/export_data.js` · `tools/check_data_sync.sh` · `Assets/StreamingAssets/data/`
- T3 ✅ 완료 — `Assets/Scripts/Core/Data/` · `Core/BigNum.cs` · `Assets/Tests/EditMode/DataTests.cs`

### `js/bignum.js` · `util.js`
- 무엇: 큰 수 · 표기 · 난수
- 상태: ✅
- T3 ✅ 완료 — `Assets/Scripts/Core/Data/` · `Core/BigNum.cs` · `Assets/Tests/EditMode/DataTests.cs`

### `js/voxel.js` · `mobs.js` · `mobs-pets.js` · `mobs-mounts.js` · `mobs-enemies.js` · `mobs-props.js` · `mobs-skillfx.js`
- 무엇: 박스 몹 조립 · 종 표
- 상태: ✅ (T4 · T5)
- T2 ✅ 완료 — `tools/export_data.js` · `tools/check_data_sync.sh` · `Assets/StreamingAssets/data/`
- T4 ✅ 완료 — `Assets/Scripts/Core/Voxel/`(Voxel.cs · VoxelGeometry.cs · MobPainter.cs · ColorHsl.cs · MobBuilder.cs) · `Assets/Scripts/Game/Voxel/`(VoxelMob.cs · VoxelMaterials.cs · ThreeSpace.cs) · `Assets/Tests/EditMode/VoxelTests.cs` · `Assets/Tests/EditMode/Vectors/t4-voxel.json` · `tools/voxel_vectors.js` · `Assets/Forge/Resources/VoxelLit.mat` · `Assets/Forge/Resources/VoxelUnlit.mat` · `tools/gen_meta.py`(.mat 갈래) · `docs/assets-map.md`(재질 두 줄)
- T5 ✅ 완료 — `Assets/Scripts/Game/Gallery/`(GalleryData.cs · MobGallery.cs · GalleryProps.cs · GallerySheet.cs · MobGalleryScene.cs) · `Assets/Tests/PlayMode/MobGalleryTests.cs` · `Assets/Tests/PlayMode/Vectors/t5-bounds.json` · `tools/gallery_vectors.js`

### `js/prochar.js`(2,526)
- 무엇: 영웅 박스 모델 · 무기 파지 · 애니
- 상태: ✅
- T6 ✅ 완료 — `Assets/Scripts/Core/Hero/`(HeroEase·HeroClips·HeroPoseSolver·HeroRigSpec·ThreeQuat·WeaponGrip·HeroSwing) · `Assets/Scripts/Game/Hero/`(HeroRig·HeroMeshes·HeroWeapon) · `Assets/Tests/PlayMode/HeroTests.cs` · `Assets/Tests/EditMode/HeroTests.cs` · `Assets/Tests/EditMode/Vectors/t6-hero.json` · `tools/hero_vectors.js`

### `js/combat.js` · `state.js`(전투 부분)
- 무엇: 전투 틱 · 웨이브 · 보스 · 전투가 `S` 에 쓰는 것(처치·재화·첫 클리어·진행·saveGame·던전 판)
- 상태: ✅ (T7 · T8 · T55)
- T7 ✅ 완료 — `Assets/Scripts/Core/Battle/` · `tools/sim/` · `Assets/Tests/EditMode/BattleTests.cs`
- T8 ✅ 완료 — `Assets/Scripts/Core/BattleFx/`(EnemyGait.cs · HitRules.cs) · `Assets/Scripts/Game/Battle/`(BattleScene · EnemyView · HeroView · HpBar · DamageNumbers · CubeParticles · BlobShadow · FxCatalog · FxMaterials · BareHeroStats) · `Assets/Forge/Resources/FxCatalog.asset` · `Assets/Plugins/WebGL/ForgeSignal.jslib` · `tools/battlefx_vectors.js` · `tools/gen_meta.py`(.jslib 갈래) · `Assets/Tests/EditMode/Vectors/t8-battlefx.json` · `Assets/Tests/EditMode/BattleFxTests.cs` · `Assets/Tests/PlayMode/BattleSceneTests.cs` · `docs/assets-map.md`(Cartoon FX 절) · `.github/workflows/ci.yml`(unity-test 잡 «테스트 결과 요약» 스텝 한 개)
- T55 ✅ 완료 — `Assets/Scripts/Core/Battle/BattleSaveSync.cs` · `Assets/Scripts/Game/Battle/BattleSaveGlue.cs` · `Assets/Scripts/Game/Battle/BattleScene.cs`(MakeBattle 한 줄) · `Assets/Scripts/Core/Battle/BattleContext.cs`(DungeonRun.Label) · `Assets/Scripts/Core/Battle/Battle.cs`(SetupStage 라벨 한 줄) · `Assets/Scripts/Core/Dungeons/Dungeons.cs`(BattleRun Label 한 줄) · `Assets/Tests/EditMode/BattleSaveSyncTests.cs` · `Assets/Tests/PlayMode/BattleSaveGlueTests.cs` · `Assets/Tests/PlayMode/PlaythroughTests.cs`(단언 한 줄)

### `js/scene3d.js`(18,887) · `scene3d-skillfx.js`(1,088)
- 무엇: 3D 세계 전부: 카메라·광원·테마·적 스폰·애니 계약·데미지 숫자·셰이크·파티클·맵·소품·펫 대형·탈것 탑승·스킬 오브젝트·사망 연출·히트 이펙트
- 상태: T1 ✅ · T8 ✅(적 스폰·보행·공격·피격·사망·숫자·셰이크·파티클 · 뺀 연출은 T39 ✅) · T9 ✅(SIMPLE_BG 의 보이는 것 · 소재 T34 · 배경 복원 T35) · T34 ✅ · T38 ✅ · T10 ✅(펫 대형·따라오기·관절 드라이버) · T39 ✅(레갈리아·보스 재질·등장 워닝·디졸브·림·플래시·플레어/스파이크/링/점광/그을음·궤적·블롭·암전) · T11 ✅ · T35 ⬜(SIMPLE_BG 복원 전엔 안 잡는다) · T12 ✅(`scene3d-skillfx.js` 전부 + 스킬 디스패처) · T52 ✅(시전 젖힘 `heroG.rotation.z` — T12·T39 가 뺀 것) · T54 ✅(촬영 PNG 에 영웅·적·펫이 0 — 오브젝트는 서는데 화면에 안 그려진다)
- T1 ✅ 완료 — `Assets/Settings`(UniversalRenderer.asset · ForgeVolume.asset · UniversalRP.asset) · `Assets/Scenes/SampleScene.unity` · `Assets/Scripts/Game/Bootstrap.cs`(GameInfo.cs 삭제) · `Assets/Scripts/Core/Viewport.cs` · `Assets/Tests`(EditMode/ViewportTests.cs · PlayMode/Forge.Tests.PlayMode.asmdef · PlayMode/BootstrapTests.cs) · `ProjectSettings/ProjectSettings.asset`(productName · 세로 고정) · `tools/dotnet`(변경 0줄)
- T8 ✅ 완료 — `Assets/Scripts/Core/BattleFx/`(EnemyGait.cs · HitRules.cs) · `Assets/Scripts/Game/Battle/`(BattleScene · EnemyView · HeroView · HpBar · DamageNumbers · CubeParticles · BlobShadow · FxCatalog · FxMaterials · BareHeroStats) · `Assets/Forge/Resources/FxCatalog.asset` · `Assets/Plugins/WebGL/ForgeSignal.jslib` · `tools/battlefx_vectors.js` · `tools/gen_meta.py`(.jslib 갈래) · `Assets/Tests/EditMode/Vectors/t8-battlefx.json` · `Assets/Tests/EditMode/BattleFxTests.cs` · `Assets/Tests/PlayMode/BattleSceneTests.cs` · `docs/assets-map.md`(Cartoon FX 절) · `.github/workflows/ci.yml`(unity-test 잡 «테스트 결과 요약» 스텝 한 개)
- T9 ✅ 완료 — `Assets/Scripts/Core/World/`(Col.cs · SceneDefs.cs · WorldRules.cs · WorldGrade.cs · GroundGrid.cs) · `Assets/Scripts/Game/World/World.cs`(namespace Forge.Game.Map) · `Assets/StreamingAssets/data/scene.json` · `tools/export_data.js`(scene 갈래) · `tools/check_data_sync.sh`(FILES) · `tools/world_vectors.js` · `Assets/Tests/EditMode/Vectors/t9-world.json` · `Assets/Tests/EditMode/WorldGradeTests.cs` · `Assets/Tests/PlayMode/WorldTests.cs` · `tools/dotnet/Stubs/URP.cs`(ColorAdjustments·FloatParameter)
- T10 ✅ 완료 — `Assets/Scripts/Core/Pets/`(PetFormation.cs · PetSceneRules.cs · PetPose.cs) · `Assets/Scripts/Core/World/SceneDefs.cs`(ArcSpec · 대열 칸 4) · `Assets/Scripts/Game/Pets/`(PetParty.cs · PetView.cs) · `Assets/StreamingAssets/data/scene.json` · `tools/export_data.js`(scene 갈래 4키) · `tools/pet_scene_vectors.js` · `Assets/Tests/EditMode/Vectors/t10-pets.json` · `Assets/Tests/EditMode/PetSceneRulesTests.cs` · `Assets/Tests/PlayMode/PetSceneTests.cs` · `.github/workflows/ci.yml`(유니티 잡 요약 스텝 · 실패 메시지)
- T11 ✅ 완료 — `Assets/Scripts/Game/Mounts/`(MountRider.cs · MountView.cs) · `Assets/Scripts/Core/Mounts/MountForms.cs` · `Assets/Scripts/Core/Mounts/MountRideRules.cs` · `Assets/Tests/EditMode/MountRideRulesTests.cs` · `Assets/Tests/PlayMode/MountSceneTests.cs` · `Assets/StreamingAssets/data/scene.json`(추출기로만) · `tools/export_data.js`(scene 갈래 T11 9키)
- T12 ✅ 완료 — `Assets/Scripts/Game/SkillFx/`(FxTimeline · SkillActors · FxLights · FxCubes · SkillFxDirector · SkillFxScene) · `Assets/Scripts/Game/Battle/BattleScene.cs`(EventHandled·Stepped 훅 — 추가 줄만 · 결정 89) · `Assets/Tests/PlayMode/SkillFxTests.cs`
- T39 ✅ 완료 — `Assets/Scripts/Core/BattleFx/FxRules.cs`(T39 상수·순수식) · `Assets/Scripts/Game/Battle/BossFx.cs`(BossLook·BossEntrance) · `HitFlashFx.cs`(FxAnims·FxUnlitMaterials·ImpactFx·EnemyBodyFx) · `HitFlashBlobs.cs`(HeroBlobs) · `TrailFx.cs`(WeaponTrail) · `Assets/Scripts/Game/Ui/BattleOverlay.cs` · `Assets/Shaders/Dissolve.shader`(Forge/EnemyBody) · `Assets/Shaders/FxUnlit.shader` · T8 자리 세 파일(`EnemyView.cs`·`HeroView.cs`·`BattleScene.cs` 의 T39 갈래) · `Assets/Tests/PlayMode/BattleFxSceneTests.cs`
- T52 ✅ 완료 — `Assets/Scripts/Game/Battle/HeroView.cs`(젖힘 채널 추가) · `Assets/Scripts/Game/SkillFx/SkillFxDirector.cs`·`SkillFxScene.cs` · `Assets/Tests/PlayMode/SkillFxTests.cs`
- T54 ✅ 완료 — `Assets/Scripts/Core/Viewport.cs`(GameArea·GameAreaFrustum) · `Assets/Scripts/Game/Bootstrap.cs`(ApplyGameAreaProjection) · `Game/Battle/BattleScene.cs`·`Game/Pets/PetParty.cs`·`Game/SkillFx/SkillFxScene.cs`·`Game/Mounts/MountRider.cs`(OnSceneLoaded 머리) · `Assets/Tests/EditMode/ViewportTests.cs` · `Assets/Tests/PlayMode/BattleSceneTests.cs`·`WorldFrameShotTests.cs`(새)·`UiShotsTests.cs`·`SafeAreaTests.cs`(ResetProjectionMatrix 한 줄)·`UiSmokeTests.cs`·`BootstrapTests.cs`

### `js/state.js` · `main.js`(저장 시점·부팅)
- 무엇: 세이브 · 마이그레이션 · 오프라인 보상
- 상태: ✅
- T13 ✅ 완료 — `Assets/Scripts/Core/Save/`(JsonTree·SaveDefs·SaveState·SaveCodec·Offline·AbsTimer) · `Assets/Scripts/Game/SaveIo.cs` · `Assets/Tests/EditMode/SaveTests.cs` · `tools/export_data.js`·`tools/check_data_sync.sh`(state.json 추출 갈래) · `Assets/StreamingAssets/data/state.json`(추출기로만) · `Assets/Scripts/Core/Data/MiniJson.cs`(JsonObject.Remove 한 줄)

### `js/forge.js`
- 무엇: 대장간 규칙 · 오토 포지
- 상태: ✅ (T14 · T19)
- T14 ✅ 완료 — `Assets/Scripts/Core/Forge/`(ForgeRules.cs · ForgeState.cs · IForgeMods.cs · ForgeItem.cs · ForgeEngine.cs) · `Assets/Tests/EditMode/ForgeTests.cs`
- T19 ✅ 완료 — `Assets/Scripts/Game/Ui/Forge*`(ForgeHost.cs · ForgeSave.cs · ForgeUi.cs · ForgeSheet.cs · ForgeInfoPopup.cs · ForgeAutoPopup.cs · ForgeCraftPopup.cs) · `Ui/Gear*`(GearDetailPopup.cs) · `Assets/Tests/PlayMode/ForgeUiTests.cs` · `.github/workflows/ci.yml`(요약 스텝에 스택 3줄만)

### 장비 8부위 · 페이퍼돌(`prochar.js`·`ui.js` 장비 · `scene3d.js` makeWeapon/makeHelmet/dressMcRig)
- 무엇: 등급·서브스탯·판매가·외형
- 상태: ✅ (T15 · T37 · T19)
- T15 ✅ 완료 — `Assets/Scripts/Core/Gear/`(IGearHost.cs · SubsBag.cs · GearState.cs · GearRules.cs · GearSystem.cs · PaperdollLook.cs) · `Assets/Scripts/Game/Hero/Paperdoll.cs` · `Assets/Tests/EditMode/GearTests.cs` · `Assets/Tests/EditMode/Vectors/t15-gear.json` · `tools/gear_vectors.js`
- T19 ✅ 완료 — `Assets/Scripts/Game/Ui/Forge*`(ForgeHost.cs · ForgeSave.cs · ForgeUi.cs · ForgeSheet.cs · ForgeInfoPopup.cs · ForgeAutoPopup.cs · ForgeCraftPopup.cs) · `Ui/Gear*`(GearDetailPopup.cs) · `Assets/Tests/PlayMode/ForgeUiTests.cs` · `.github/workflows/ci.yml`(요약 스텝에 스택 3줄만)
- T37 ✅ 완료 — `tools/export_gear_meshes.js` · `Assets/StreamingAssets/data/gear-meshes.json` 또는 `Assets/Forge/Gear/` · `Assets/Scripts/Game/Hero/GearMeshes.cs` · `Assets/Scripts/Game/Hero/Paperdoll.cs`(훅 연결) · `Assets/Tests/PlayMode/PaperdollTests.cs` · `tools/check_data_sync.sh`(gear-meshes 대조 한 단락)

### `js/pets.js`
- 무엇: 알·부화·합성·출전 규칙 · 출전 스탯 기여
- 상태: T16 ✅ · T10 ✅ · T20 ✅ · T43 ✅ · T79 ✅
- T10 ✅ 완료 — `Assets/Scripts/Core/Pets/`(PetFormation.cs · PetSceneRules.cs · PetPose.cs) · `Assets/Scripts/Core/World/SceneDefs.cs`(ArcSpec · 대열 칸 4) · `Assets/Scripts/Game/Pets/`(PetParty.cs · PetView.cs) · `Assets/StreamingAssets/data/scene.json` · `tools/export_data.js`(scene 갈래 4키) · `tools/pet_scene_vectors.js` · `Assets/Tests/EditMode/Vectors/t10-pets.json` · `Assets/Tests/EditMode/PetSceneRulesTests.cs` · `Assets/Tests/PlayMode/PetSceneTests.cs` · `.github/workflows/ci.yml`(유니티 잡 요약 스텝 · 실패 메시지)
- T16 ✅ 완료 — `Assets/Scripts/Core/Pets/`(PetState·IPetHost·PetRules·SubstatRoll·PetSystem) · `Assets/Tests/EditMode/PetTests.cs` · `Assets/Tests/EditMode/pet_vectors.json` · `tools/pet_vectors.js`
- T20 ✅ 완료 — `Assets/Scripts/Game/Ui/Pet*` · `Ui/Skill*` · `Ui/Mount*` · `Assets/Scripts/Core/PetSave/PetSkillSave.cs` · `Assets/Forge/Resources/PetSkillUi.json` · `Assets/Tests/EditMode/PetSkillSaveTests.cs` · `Assets/Tests/PlayMode/PetUiTests.cs` · `Assets/Scripts/Game/Ui/ForgeSheet.cs`(탈것 칸 onClick 한 줄 · 결정 137) · `Assets/Tests/PlayMode/UiShotsTests.cs`(탈것 3줄 · T27 ✅ 뒤)
- T43 ✅ 완료 — `Assets/Scripts/Game/Battle/BattleScene.cs`(Boot 의 stats 인자) · `Assets/Scripts/Game/Battle/HeroStatsGlue.cs` · `Assets/Tests/PlayMode/HeroStatsGlueTests.cs`
- T79 ✅ 완료 — `Assets/Scripts/Game/Ui/PetUpgrade*` · `Assets/Scripts/Game/Ui/PetSkillModal.cs`(딤 한 줄) · `Assets/Scripts/Game/Ui/UiKit.cs`(`PerceivedDim` 한 함수) · `Assets/Tests/PlayMode/PetUiTests.cs`

### `js/skills.js`
- 무엇: 소환·18종·3슬롯(정본 `MAX_ACTIVE`)
- 상태: T17 ✅ · T20 ✅
- T17 ✅ 완료 — `Assets/Scripts/Core/Skills/` · `Assets/Tests/EditMode/SkillTests.cs` · `Assets/Tests/EditMode/Vectors/skill_vectors.json` · `tools/skill_vectors.js`
- T20 ✅ 완료 — `Assets/Scripts/Game/Ui/Pet*` · `Ui/Skill*` · `Ui/Mount*` · `Assets/Scripts/Core/PetSave/PetSkillSave.cs` · `Assets/Forge/Resources/PetSkillUi.json` · `Assets/Tests/EditMode/PetSkillSaveTests.cs` · `Assets/Tests/PlayMode/PetUiTests.cs` · `Assets/Scripts/Game/Ui/ForgeSheet.cs`(탈것 칸 onClick 한 줄 · 결정 137) · `Assets/Tests/PlayMode/UiShotsTests.cs`(탈것 3줄 · T27 ✅ 뒤)

### `js/mounts.js`
- 무엇: 탈것 규칙 · 탑승
- 상태: T40 ✅ · T11 ✅ · T20 ✅
- T11 ✅ 완료 — `Assets/Scripts/Game/Mounts/`(MountRider.cs · MountView.cs) · `Assets/Scripts/Core/Mounts/MountForms.cs` · `Assets/Scripts/Core/Mounts/MountRideRules.cs` · `Assets/Tests/EditMode/MountRideRulesTests.cs` · `Assets/Tests/PlayMode/MountSceneTests.cs` · `Assets/StreamingAssets/data/scene.json`(추출기로만) · `tools/export_data.js`(scene 갈래 T11 9키)
- T20 ✅ 완료 — `Assets/Scripts/Game/Ui/Pet*` · `Ui/Skill*` · `Ui/Mount*` · `Assets/Scripts/Core/PetSave/PetSkillSave.cs` · `Assets/Forge/Resources/PetSkillUi.json` · `Assets/Tests/EditMode/PetSkillSaveTests.cs` · `Assets/Tests/PlayMode/PetUiTests.cs` · `Assets/Scripts/Game/Ui/ForgeSheet.cs`(탈것 칸 onClick 한 줄 · 결정 137) · `Assets/Tests/PlayMode/UiShotsTests.cs`(탈것 3줄 · T27 ✅ 뒤)
- T40 ✅ 완료 — `Assets/Scripts/Core/Mounts/`(MountRules · IMountHost · MountState · MountSystem · MountSave) · `Assets/Tests/EditMode/MountTests.cs` · `Assets/Tests/EditMode/Vectors/mount_vectors.json` · `tools/mount_vectors.js`

### `js/dungeons.js`
- 무엇: 던전 4종
- 상태: T23 ✅ · T21 ✅
- T21 ✅ 완료 — `Assets/Scripts/Game/Ui/Dungeon*`(DungeonUiHost·DungeonPopups·DungeonSheet·DungeonDetailPopup·DungeonClearPopup) · `Ui/Tech*`(TechPanel·TechPopups) · `Ui/Ascend*`(AscendPopup) · `Assets/Tests/PlayMode/DungeonUiTests.cs` · `Assets/Forge/catalog.json`+`Assets/Forge/Resources/UiCatalog.asset`(색 54·배치 89 키 추가 · 항목만 덧붙임)
- T23 ✅ 완료 — `Assets/Scripts/Core/Dungeons/`(DungeonRules · DungeonDef · DungeonRewards · IDungeonHost · DungeonEvent · Dungeons) · `Assets/Tests/EditMode/DungeonTests.cs` · `Assets/Tests/EditMode/Vectors/t23-dungeons.json` · `tools/dungeon_vectors.js`

### `js/techtree.js` · `ascension.js`
- 무엇: 기술트리 · 승천
- 상태: T24 ✅ · T21 ✅
- T21 ✅ 완료 — `Assets/Scripts/Game/Ui/Dungeon*`(DungeonUiHost·DungeonPopups·DungeonSheet·DungeonDetailPopup·DungeonClearPopup) · `Ui/Tech*`(TechPanel·TechPopups) · `Ui/Ascend*`(AscendPopup) · `Assets/Tests/PlayMode/DungeonUiTests.cs` · `Assets/Forge/catalog.json`+`Assets/Forge/Resources/UiCatalog.asset`(색 54·배치 89 키 추가 · 항목만 덧붙임)
- T24 ✅ 완료 — `Assets/Scripts/Core/Tech/`(TechTable.cs · TechData.cs · TechState.cs · TechTree.cs) · `Assets/Scripts/Core/Ascension/`(AscensionTable.cs · Ascension.cs) · `Assets/Tests/EditMode/TechTests.cs` · `tools/export_data.js`(techtree·ascension 표 추출 한 갈래) · `tools/check_data_sync.sh`(파일 목록) · `Assets/StreamingAssets/data/tech.json`(+.meta · 추출기로만)

### `js/shop.js` · `pass.js` · `quests.js` · `league.js` · `chat.js`
- 무엇: 상점·패스·퀘스트·리그·채팅
- 상태: ✅ (T25 · T22)
- T22 ✅ 완료 — `Assets/Scripts/Game/Ui/`(ShopSheet.cs · QuestSheet.cs · PassPopup.cs · LeagueSheet.cs · ChatScreen.cs · ProfilePopup.cs(설정 포함) · PlayerInfoPopup.cs · OfflinePopup.cs · DebugPanel.cs · Popups.cs(공용 모달 층·스텁·토스트) · MetaHost.cs(T25 Core ↔ 세이브·HUD·탭 접착) · TabBar.cs(팝업 탭 ✕ SetPopupX 한 줄)) · `Assets/Forge/catalog.json`+`Assets/Forge/Resources/UiCatalog.asset`(색 70·배치 102·스프라이트 20 추가) · `Assets/Scripts/Core/MetaSave/MetaSave.cs`(T25 상태 ↔ 세이브 트리 코덱 · 순수) · `Assets/Tests/EditMode/MetaSaveTests.cs` · `Assets/Tests/PlayMode/ShopUiTests.cs` · `tools/dotnet/Stubs/TMPro.cs`(TMP_InputField onSubmit·textViewport)
- T25 ✅ 완료 — `Assets/Scripts/Core/Meta/` · `Assets/Tests/EditMode/MetaTests.cs` · `tools/export_data.js`(shop·pass·quests·league·chat 표 추출 한 갈래) · `tools/check_data_sync.sh`(파일 목록) · `Assets/StreamingAssets/data/meta.json`(+.meta · 추출기로만)

### `js/ui.js`(6,181) · `css/style.css` · `index.html`
- 무엇: 캔버스·HUD·탭·패널 전부(공개 함수 97개 — T19~T22 절에 이름별로 나눠 적었다) · 메뉴·프로필·설정·디버그
- 상태: T18 ✅ · T19 ✅ · T21 ✅ · T22 ✅ · T20 ✅ · T53 ✅ · T56 ✅ · T57 ✅ · T58 ✅ · T59 ✅ · T60 ✅ · T61 ✅ · T62 ✅ · T63 ✅ · T65 ✅· T66 ✅ · T68 ✅ · T75 🔄 · T76 ✅ · T78 ✅ · T79 ✅ · T85 ✅
- T18 ✅ 완료 — `Assets/Scripts/Game/Ui/`(UiRoot·UiKit·UiCatalog·UiShapes·UiTextKindTag·Hud·TabBar) · `Assets/Forge/catalog.json` · `Assets/Forge/Resources/UiCatalog.asset` · `docs/assets-map.md` · `Assets/Tests/PlayMode/UiSmokeTests.cs` · `Assets/Tests/PlayMode/TextSizeGateTests.cs` · `tools/gen_ui_catalog.py` · `tools/gen_meta.py`(.asset 한 갈래) · `tools/dotnet/Stubs/TMPro.cs`(TMP 서명 4개) · `Assets/Scripts/Game/Forge.Game.asmdef`·`Assets/Tests/PlayMode/Forge.Tests.PlayMode.asmdef`(UnityEngine.UI·TMP 참조)
- T19 ✅ 완료 — `Assets/Scripts/Game/Ui/Forge*`(ForgeHost.cs · ForgeSave.cs · ForgeUi.cs · ForgeSheet.cs · ForgeInfoPopup.cs · ForgeAutoPopup.cs · ForgeCraftPopup.cs) · `Ui/Gear*`(GearDetailPopup.cs) · `Assets/Tests/PlayMode/ForgeUiTests.cs` · `.github/workflows/ci.yml`(요약 스텝에 스택 3줄만)
- T20 ✅ 완료 — `Assets/Scripts/Game/Ui/Pet*` · `Ui/Skill*` · `Ui/Mount*` · `Assets/Scripts/Core/PetSave/PetSkillSave.cs` · `Assets/Forge/Resources/PetSkillUi.json` · `Assets/Tests/EditMode/PetSkillSaveTests.cs` · `Assets/Tests/PlayMode/PetUiTests.cs` · `Assets/Scripts/Game/Ui/ForgeSheet.cs`(탈것 칸 onClick 한 줄 · 결정 137) · `Assets/Tests/PlayMode/UiShotsTests.cs`(탈것 3줄 · T27 ✅ 뒤)
- T21 ✅ 완료 — `Assets/Scripts/Game/Ui/Dungeon*`(DungeonUiHost·DungeonPopups·DungeonSheet·DungeonDetailPopup·DungeonClearPopup) · `Ui/Tech*`(TechPanel·TechPopups) · `Ui/Ascend*`(AscendPopup) · `Assets/Tests/PlayMode/DungeonUiTests.cs` · `Assets/Forge/catalog.json`+`Assets/Forge/Resources/UiCatalog.asset`(색 54·배치 89 키 추가 · 항목만 덧붙임)
- T22 ✅ 완료 — `Assets/Scripts/Game/Ui/`(ShopSheet.cs · QuestSheet.cs · PassPopup.cs · LeagueSheet.cs · ChatScreen.cs · ProfilePopup.cs(설정 포함) · PlayerInfoPopup.cs · OfflinePopup.cs · DebugPanel.cs · Popups.cs(공용 모달 층·스텁·토스트) · MetaHost.cs(T25 Core ↔ 세이브·HUD·탭 접착) · TabBar.cs(팝업 탭 ✕ SetPopupX 한 줄)) · `Assets/Forge/catalog.json`+`Assets/Forge/Resources/UiCatalog.asset`(색 70·배치 102·스프라이트 20 추가) · `Assets/Scripts/Core/MetaSave/MetaSave.cs`(T25 상태 ↔ 세이브 트리 코덱 · 순수) · `Assets/Tests/EditMode/MetaSaveTests.cs` · `Assets/Tests/PlayMode/ShopUiTests.cs` · `tools/dotnet/Stubs/TMPro.cs`(TMP_InputField onSubmit·textViewport)
- T28 🔄 진행 — `docs/ref-layout.md` · `tools/ui_score.py` · `docs/ui-score-baseline.json`(회차 사이 점수) · `docs/ROUTINE.md`(§2 재등재) · `docs/PROGRESS.md`
- T53 ✅ 완료 — `Assets/Fonts/`(주인이 넣는다) · `Assets/Forge/catalog.json`(font 한 줄) · `Assets/Scripts/Game/Ui/UiFont.cs` · `Assets/Tests/PlayMode/TextSizeGateTests.cs`(한글 글리프 존재 단언 추가)
- T56 ✅ 완료 — `Assets/Scripts/Game/Ui/Hud.cs` · `Assets/Scripts/Game/Ui/MetaHost.cs`(Sync 한 줄) · `Assets/Tests/PlayMode/HudAvatarTests.cs`(새)
- T57 ✅ 완료 — `Assets/Scripts/Game/Ui/Forge*` · `Ui/Gear*` · `Assets/Scripts/Game/Ui/Popups.cs`(PopupKit.Column/Row 두 줄 · 결정 143) · `Assets/Tests/PlayMode/ForgeUiTests.cs`
- T58 ✅ 완료 — `Assets/Scripts/Game/Ui/League*` · `Ui/PetUpgrade*` · `Assets/Tests/PlayMode/PetUiTests.cs`
- T59 ✅ 완료 — `Assets/Scripts/Game/Ui/ForgeHost.cs`(SubLines 한 함수) · `Assets/Scripts/Core/BigNum.cs`(NumFmt.RoundFixed/Fixed) · `Assets/Tests/EditMode/GearUiFormatTests.cs` · `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs`·`Ui/ForgeInfoPopup.cs`(읽기만 · 0줄)
- T60 ✅ 완료 — `Assets/Scripts/Game/Ui/Hud.cs` · `Ui/ShopSheet.cs`(공용 조각 갈래만) · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/UiSmokeTests.cs` · `Assets/Scripts/Game/Ui/MetaHost.cs`(«+» 버튼 → OpenShop 배선 한 줄 · 범위 먼저 넓힘)
- T61 ✅ 완료 — `Assets/Scripts/Game/Ui/ForgeSheet.cs`(AnvilSlot·DrawAnvil·Outlined) · `Assets/Forge/catalog.json`(anvil_* 색 8·배치 36 append) · `Assets/Tests/PlayMode/ForgeUiTests.cs`(+1)
- T62 ✅ 완료 — `Assets/Scripts/Game/Ui/ShopSheet.cs` · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/ShopUiTests.cs`
- T63 ✅ 완료 — `Assets/Scripts/Game/Ui/Hud.cs` · `Ui/ChatScreen.cs` · `Ui/MetaHost.cs` · `Assets/Tests/PlayMode/UiSmokeTests.cs`
- T65 ✅ 완료 — `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs` · `Assets/Forge/Resources/PlayerInfoUi.json`(새 · T20 `PetSkillUi.json` 꼴 — `catalog.json` 은 T62 lock 이 쥐고 있어 이 회차엔 안 연다) · `Assets/Tests/PlayMode/UiSmokeTests.cs`
- T66 ✅ 완료 — `tools/sim/sim_combat.js`(UI 스텁 `updateStageLabel` · 시나리오 `kr`·`label`) · `tools/sim/expected/combat_dungeon_tier.json`(재생성 · 다른 둘 diff 0) · `Assets/Tests/EditMode/BattleTests.cs`(`SimScenario.Context` 한 줄)
- T68 ✅ 완료 — `Assets/Scripts/Game/Ui/OfflinePopup.cs` · `Assets/Tests/PlayMode/OfflinePopupTests.cs`(자기 파일 · `UiSmokeTests.cs` 는 T54·T63·T65 lock) · `Assets/Forge/catalog.json`(색 키 `#0e111b`·`#ccc` · **T62 lock 이 풀린 뒤** · 이 회차 안 만짐)
- T75 🔄 진행 — `Assets/Scripts/Game/Ui/ShopSheet.cs`(보석 카드 갈래만) · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/ShopUiTests.cs`
- T76 ✅ 완료 — `Assets/Scripts/Game/Battle/DamageNumbers.cs`(붙는 층만 · `Layer`) · `Assets/Tests/PlayMode/DamageLayerTests.cs`(자기 파일 · 다른 UI 파일은 T54·T60·T68 lock 이라 0줄)
- T78 ✅ 완료 — `Assets/Scripts/Game/Ui/Popups.cs` · `Ui/Forge*` · `tools/ui_score.py`(경고 갈래 · **T28 lock 이 풀린 뒤** · 이 회차 안 만짐) · `Assets/Tests/PlayMode/ForgeUiTests.cs`
- T85 ✅ 완료 — `Assets/Scripts/Game/Ui/BattleOverlay.cs`(띠 한 줄 · T39 자기 파일) · `Assets/Tests/PlayMode/BattleFxSceneTests.cs`(단언 3) · `Assets/Tests/PlayMode/UiSmokeTests.cs`(테스트 1) · `docs/ROUTINE.md`(§2 T85) · `docs/PROGRESS.md` — `SettingsPopup*`·`ProfilePopup*`·`UiKit.cs` 는 **안 고쳤다**(딤이 정본대로라서)

### `js/sfx.js`(618)
- 무엇: 효과음 24종(+프리미티브 6) · 음악 4모드 (코드 합성)
- 상태: ✅ (`Core/Audio` · `Game/Audio` · `AudioTests` 벡터 대조 · `AudioSmokeTests`)
- T30 ✅ 완료 — `Assets/Scripts/Core/Audio/`(SfxTable.cs · Voice.cs · SfxSynth.cs · SfxRecipes.cs · MusicSequencer.cs · Dsp.cs · SynthRenderer.cs) · `Assets/Scripts/Game/Audio/`(AudioBank.cs · Sfx.cs · Music.cs) · `Assets/StreamingAssets/data/sfx.json` · `tools/export_data.js`(sfx 갈래만) · `tools/check_data_sync.sh`(FILES 에 sfx.json 한 단어) · `tools/sfx_vectors.js` · `Assets/Tests/EditMode/Vectors/t30-sfx.json` · `Assets/Tests/EditMode/AudioTests.cs` · `Assets/Tests/PlayMode/AudioSmokeTests.cs`

### `js/icongen.js`(6,704) · `avatars.js`(831)
- 무엇: 아이콘 136종 · 아바타 24종(`IconGen.draw` 키 160 · «523» 은 도우미까지 센 수) + tint 변형 10
- 상태: ✅
- T31 ✅ 완료 — `tools/export_icons.js` · `tools/check_icons_sync.sh` · `Assets/Forge/Icons/` · `Assets/Scripts/Core/Ui/IconAtlas.cs`(순수 표 · dotnet 검증) · `Assets/Scripts/Game/Ui/UiIcons.cs` · `Assets/Scripts/Game/Ui/UiKit.cs`(Icon 한 갈래만) · `.github/workflows/ci.yml`(datasync 잡의 아이콘 검사 한 줄만) · `Assets/Tests/EditMode/IconAtlasTests.cs` · `Assets/Tests/PlayMode/UiIconsTests.cs` · `docs/assets-map.md` · `docs/ROUTINE.md`(§1 아틀라스 예외 한 줄)

### `ref/screens/shot-*.png` 30장 · `tools/shot-*.js` · `ref/UI-SPEC.md` · `ref/POLISH.md`
- 무엇: 원작 화면 정본 · 촬영 도구 · 비율 규격
- 상태: T27 ✅(원작 30장 전부 열림 + `screen_*.png` 31장 + 짝 표 `screens.json` · CI 런 83) · T28 🔄 · T33 ⬜ · T77 ✅ · T83 ✅
- T27 ✅ 완료 — `Assets/Tests/PlayMode/PlaythroughTests.cs` · `UiShotsTests.cs` · `PlayLog.cs`
- T28 🔄 진행 — `docs/ref-layout.md` · `tools/ui_score.py` · `docs/ui-score-baseline.json`(회차 사이 점수) · `docs/ROUTINE.md`(§2 재등재) · `docs/PROGRESS.md`
- T33 🔄 진행 — `docs/ROUTINE.md`(§7 표) · `docs/PROGRESS.md` · `docs/parity.md`
- T77 ✅ 완료 — `Assets/Tests/PlayMode/UiShotsTests.cs`(`Seed()` 두 줄 + 단언 하나)
- T83 ✅ 완료 — `Assets/Tests/PlayMode/UiShotsTests.cs`(Capture 합성) · 필요하면 `SafeAreaTests.cs`

### `css/style.css` 제작 키프레임 22종
- 무엇: 대장간 뽑기 연출(모루·오토포지·결과 카드)
- 상태: ⬜
- T87 🔄 진행 — `Assets/Scripts/Core/CraftFx/`(CssTrack · AnvilFxSpec) · `Assets/Tests/EditMode/CraftFxTests.cs` · `Assets/Scripts/Game/Ui/Forge*` · `Ui/Anvil*` · `Ui/CraftFx*` · `Assets/Tests/PlayMode/ForgeUiTests.cs` · `Assets/Forge/catalog.json`(연출 수치)

### (주인 지시) 백그라운드 재생 · 복귀 따라잡기
- 무엇: runInBackground · OnApplicationPause 절대시각
- 상태: ✅
- T88 ✅ 완료 — `Assets/Scripts/Game/Bootstrap.cs` · `Game/AppLifecycle.cs` · `Assets/Scripts/Core/Save/` · `ProjectSettings/ProjectSettings.asset` · `Assets/Tests/EditMode/CatchUpTests.cs` · `Assets/Tests/PlayMode/LifecycleTests.cs`

### (주인 지시 · 원작 밖 품질 조건) SafeArea · 60fps · 실제 화면 촬영
- 무엇: 모바일 상단 카메라 회피 · 프레임 예산 · 게임 화면 PNG 를 눈으로
- 상태: T45 ✅ · T44 ✅ · T27 ✅(촬영 자리 · 노치 모의는 `UiRoot.NotchSafeArea`) · T50 ✅(프레임당 관리 힙 풀링) · T64 ✅(렌더 몫은 없었다 — AudioBank 베이크 스레드 · 편집기 재질 후처리 · URP 변경 없음) · T73 ✅(AudioBank 베이크 배열 되쓰기) · T74 ✅(FxCubes 시전당 재질 되쓰기)
- T27 ✅ 완료 — `Assets/Tests/PlayMode/PlaythroughTests.cs` · `UiShotsTests.cs` · `PlayLog.cs`
- T44 ✅ 완료 — `Assets/Scripts/Game/Bootstrap.cs`(ApplyFrameRate) · `Assets/Tests/PlayMode/PerfBudgetTests.cs` · 넘길 때만 `Assets/Scripts/Game/Battle/` · `Game/Voxel/` · `Game/Ui/Hud.cs`
- T45 ✅ 완료 — `Assets/Scripts/Game/Ui/UiRoot.cs`(주입 지점 `OverrideSafeArea`·`NotchSafeArea`·노치 상수) · `Assets/Tests/PlayMode/SafeAreaTests.cs` · `Assets/Forge/catalog.json`(변경 0줄 · 노치 상수는 UiRoot)
- T50 ✅ 완료 — `Assets/Scripts/Game/Battle/DamageNumbers.cs` · `CubeParticles.cs` · `TrailFx.cs` · `HitFlashFx.cs` · `Assets/Scripts/Core/Battle/Battle.cs`(버퍼 재사용만) · `Assets/Tests/PlayMode/PerfBudgetTests.cs`(상한 두 수)
- T64 ✅ 완료 — `Assets/Settings/` · `Assets/Tests/PlayMode/PerfBudgetTests.cs`(측정 갈래만) · `docs/`
- T73 ✅ 완료 — `Assets/Scripts/Core/Audio/SynthRenderer.cs` · `Dsp.cs` · `SfxSynth.cs`(배열 되쓰기만) · `Assets/Scripts/Game/Audio/AudioBank.cs`(버퍼 소유만) · `Assets/Tests/EditMode/AudioTests.cs`
- T74 ✅ 완료 — `Assets/Scripts/Game/SkillFx/FxCubes.cs` · `Assets/Scripts/Game/Battle/FxMaterials.cs`(풀 갈래만) · `Assets/Tests/PlayMode/SkillFxTests.cs`

### WebGL 배포 · Android
- 무엇: 배포
- 상태: ✅ (굽기 잡 조건 T32 ✅) · T86 🔄
- T26 ✅ 완료 — `Assets/WebGLTemplates/` · `tools/webgl_smoke.js` · `.github/workflows/ci.yml`(빌드 잡 부분만) · `ProjectSettings/ProjectSettings.asset`(webGLTemplate · 압축 폴백 · Android 식별자 칸만)
- T86 🔄 진행 — `Assets/Scripts/Game/Battle/BattleScene.cs`(Boot 의 Attach 인자 한 줄) · `tools/webgl_smoke.js`(닫기 뒤 requestfailed 무시)

### (원작 밖 · 도구·게이트·CI) 병렬 운영을 지키는 자들 — 원작 모듈에 안 붙지만 **여기 적는다**(안 적으면 T33 이 그 위를 지나간다 · T69)
- 무엇: lock·번호·문서·카탈로그·CI·진단 자
- 상태: T29 ✅ · T36 ⛔ · T41 ✅ · T42 ✅ · T46 ✅ · T47 ✅ · T48 ✅ · T49 ✅ · T51 ✅ · T67 ✅ · T69 ✅ · T70 ✅ · T71 ✅ · T72 ⛔ · T80 ✅ · T81 ✅ · T82 ✅ · T84 🔄
- T29 ✅ 완료 — `tools/task_state.py`
- T36 ⛔ 흡수 — `Assets/Tests/EditMode/PetTests.cs` · `docs/ROUTINE.md`(§1 한 줄)
- T41 ✅ 완료 — `tools/gen_ui_catalog.py` · `.github/workflows/ci.yml`(dotnet 잡 두 줄) · `docs/ROUTINE.md` §3 한 줄 · `.gitignore`(`__pycache__/` 두 줄)
- T42 ✅ 완료 — `tools/check_claim_scope.py` · `docs/ROUTINE.md`(T42 제목 ✅ · T10 ✅ 는 워커 B 가 먼저)
- T46 ✅ 완료 — `Assets/Tests/PlayMode/RedLog.cs` · `tools/dotnet/Stubs/TestRunner.cs`(새 스텁) · `tools/dotnet/TestsPlay/Forge.TestsPlay.csproj`(스텁 Compile 한 줄 · T48 자리 · lock 없음)
- T47 ✅ 완료 — `Assets/Forge/catalog.json`(항목 자리만 · 값 변경 0) · `Assets/Forge/Resources/UiCatalog.asset`(자가 다시 만든다) · `tools/gen_ui_catalog.py`
- T48 ✅ 완료 — `tools/dotnet/TestsPlay/Forge.TestsPlay.csproj`(새) · `tools/dotnet/Stubs/TestTools.cs`(새) · `tools/dotnet/Forge.sln` · `docs/ROUTINE.md`(§3 게이트 한 줄 · §2 T48) · `docs/PROGRESS.md`
- T49 ✅ 완료 — `tools/check_final_table.py` · `.github/workflows/ci.yml`(dotnet 잡 두 줄) · `docs/ROUTINE.md`(§3 한 줄 · §7 어긋난 칸 셋)
- T51 ✅ 완료 — `.github/workflows/ci.yml` · `docs/ROUTINE.md`(§3 한 줄)
- T67 ✅ 완료 — `.github/workflows/ci.yml`
- T69 ✅ 완료 — `tools/check_final_table.py` · `docs/ROUTINE.md`(§7 표·머리줄) · `docs/PROGRESS.md`
- T70 ✅ 완료 — `Assets/Tests/PlayMode/BootstrapTests.cs`
- T71 ✅ 완료 — `Assets/Tests/PlayMode/ShopUiTests.cs`(단언 한 줄)
- T72 ⛔ 흡수 — `Assets/Tests/PlayMode/PetUiTests.cs`(소환 결과 탭 두 자리)
- T81 ✅ 완료 — `.github/workflows/ci.yml`(`unity-test` 잡의 업로드·판정 스텝) · `docs/ROUTINE.md`(§2·§7) · `docs/PROGRESS.md`
- T82 ✅ 완료 — `tools/check_final_table.py`
- T84 🔄 진행 — `Assets/Tests/PlayMode/UiShotsTests.cs` · `Assets/Tests/PlayMode/PlayLog.cs`

### `lib/three.min.js` · `anvil-*.png`(참고 이미지 · 게임이 안 읽음) · `web/TODO.md` 미완 7항목
- 무엇: 옮기지 않음
- 상태: 해당 없음
- 유니티: (옮기지 않음)

## ⓑ 원작 화면 30장 ↔ 클론 촬영(`screens.json` · T27)

| 클론 이름 | 원작 샷 |
|---|---|
| main | shot-042120.png |
| offline | shot-042110.png |
| league | shot-042149.png |
| league-rewards | shot-042208.png |
| league-challenge | shot-042228.png |
| dungeons | shot-042251.png |
| dungeon-detail | shot-042304.png |
| skills | shot-042340.png |
| pets | shot-042356.png |
| pets-2 | shot-042445.png |
| tech-overview | shot-042407.png |
| skill-detail | shot-042426.png |
| pet-detail | shot-042449.png |
| pet-upgrade | shot-042503.png |
| summon-rates | shot-042521.png |
| tech-branch | shot-042546.png |
| tech-node | shot-042605.png |
| shop | shot-042632.png |
| pass | shot-042705.png |
| profile | shot-042724.png |
| settings | shot-042744.png |
| forge-info | shot-042831.png |
| forge-list | shot-042905.png |
| forge-detail | shot-042931.png |
| autoforge | shot-043117.png |
| autoforge-filter | shot-042950.png |
| craft-compare | shot-043224.png |
| gear-detail | shot-043244.png |
| player-info | shot-043313.png |
| chat | shot-043500.png |
| mounts | (원작 샷 없음 · 클론 추가: 탈것 3장 · 노치 1장) |
| mount-detail | (원작 샷 없음 · 클론 추가: 탈것 3장 · 노치 1장) |
| mount-upgrade | (원작 샷 없음 · 클론 추가: 탈것 3장 · 노치 1장) |
| main-notch | (원작 샷 없음 · 클론 추가: 탈것 3장 · 노치 1장) |

- 원작 30장 중 짝 없는 것: **0** · 클론에만 있는 것: 4(탈것 3 · 노치 1 · 원작에 화면이 없다)

## ⓒ `ui.js` 공개 함수(`open|render|show|toggle|close|build` 접두 · 101개 · §7 의 «97» 은 `openStub/closeStub/showModal/renderMenu` 도우미 4개를 뺀 수) ↔ 유니티

| 원작 | 짝 | 유니티 |
|---|---|---|
| `buildCraftCard` | 이름 그대로 | ForgeCraftPopup.cs |
| `buildSummonReflection` | 이름 그대로 | SkillSummonResult.cs |
| `closeAllTabSurfaces` | 이름 그대로 | MetaHost.cs,Popups.cs,PetSkillModal.cs |
| `closeAscension` | 이름 그대로 | AscendPopup.cs |
| `closeAutoForge` | 이름 그대로 | ForgeAutoPopup.cs |
| `closeChat` | 줄기(접두 다름) | ChatScreen.cs,ProfilePopup.cs,MetaHost.cs |
| `closeDetail` | 줄기(접두 다름) | SkillPanel.cs,PetPanel.cs,MountSheet.cs |
| `closeDungeonDetail` | 줄기(접두 다름) | DungeonDetailPopup.cs,DungeonSheet.cs |
| `closeDungeons` | 이름 그대로 | DungeonSheet.cs |
| `closeForgeInfo` | 이름 그대로 | ForgeInfoPopup.cs |
| `closeForgeItemDetail` | 이름 그대로 | ForgeInfoPopup.cs |
| `closeGearDetail` | 줄기(접두 다름) | PlayerInfoPopup.cs,GearDetailPopup.cs,ForgeHost.cs |
| `closeLeague` | 줄기(접두 다름) | MetaHost.cs,LeagueSheet.cs |
| `closeMountUpgrade` | 줄기(접두 다름) | MountUpgradePopup.cs,PlayerInfoPopup.cs,MountSheet.cs |
| `closeMounts` | 이름 그대로 | MountSheet.cs |
| `closeOfflineModal` | 없음 | `OfflinePopup.Close`(이름만 다름 · T22/T68) |
| `closeOpened` | 이름 그대로 | MetaHost.cs,Popups.cs |
| `closePass` | 줄기(접두 다름) | PassPopup.cs,SkillPanel.cs,MetaHost.cs |
| `closePetUpgrade` | 줄기(접두 다름) | PetUpgradePopup.cs,PetPanel.cs |
| `closePlayerInfo` | 줄기(접두 다름) | MetaHost.cs,PlayerInfoPopup.cs,ForgeHost.cs |
| `closeProfile` | 줄기(접두 다름) | ProfilePopup.cs,MetaHost.cs,Hud.cs |
| `closeQuests` | 줄기(접두 다름) | DebugPanel.cs,MetaHost.cs,DungeonUiHost.cs |
| `closeShop` | 줄기(접두 다름) | PassPopup.cs,ShopSheet.cs,MetaHost.cs |
| `closeStub` | 없음 | `PopupLayer.Hide` — 스텁 팝업은 `openStub`(T22) 이 토스트로 닫는다 |
| `closeSummonResult` | 줄기(접두 다름) | PetPanel.cs,SkillSummonResult.cs |
| `openAscension` | 이름 그대로 | AscendPopup.cs,ForgeHost.cs |
| `openAutoForge` | 이름 그대로 | ForgeAutoPopup.cs |
| `openChat` | 이름 그대로 | ChatScreen.cs |
| `openDungeonDetail` | 이름 그대로 | DungeonDetailPopup.cs |
| `openDungeons` | 이름 그대로 | DungeonUiHost.cs,DungeonSheet.cs |
| `openEggDetail` | 이름 그대로 | PetPanel.cs |
| `openForgeDetail` | 이름 그대로 | ForgeInfoPopup.cs |
| `openForgeInfo` | 이름 그대로 | ForgeInfoPopup.cs |
| `openForgeList` | 이름 그대로 | ForgeInfoPopup.cs |
| `openGearDetail` | 이름 그대로 | GearDetailPopup.cs |
| `openLeague` | 이름 그대로 | LeagueSheet.cs |
| `openLeagueChallenge` | 이름 그대로 | LeagueSheet.cs |
| `openLeagueRewards` | 이름 그대로 | LeagueSheet.cs |
| `openMountDetail` | 이름 그대로 | MountSheet.cs |
| `openMountUpgrade` | 이름 그대로 | MountUpgradePopup.cs |
| `openMounts` | 이름 그대로 | PlayerInfoPopup.cs,MountSheet.cs,ForgeSheet.cs |
| `openNextAutoMatch` | 이름 그대로 | ForgeHost.cs |
| `openPass` | 이름 그대로 | PassPopup.cs |
| `openPetDetail` | 이름 그대로 | PetPanel.cs |
| `openPetUpgrade` | 이름 그대로 | PetUpgradePopup.cs |
| `openPlayerInfo` | 이름 그대로 | PlayerInfoPopup.cs |
| `openProfile` | 이름 그대로 | ProfilePopup.cs |
| `openQuests` | 이름 그대로 | QuestSheet.cs |
| `openShop` | 이름 그대로 | ShopSheet.cs,MetaHost.cs |
| `openSkillDetail` | 이름 그대로 | SkillPanel.cs |
| `openStub` | 이름 그대로 | Popups.cs |
| `openSummonRates` | 이름 그대로 | SkillRatesPopup.cs |
| `openSummonResult` | 이름 그대로 | SkillSummonResult.cs |
| `openTechBonuses` | 이름 그대로 | TechPopups.cs |
| `openTechBranch` | 이름 그대로 | TechPanel.cs |
| `openTechNode` | 이름 그대로 | TechPopups.cs |
| `openTechOverview` | 이름 그대로 | TechPanel.cs |
| `openTechTree` | 이름 그대로 | TechPanel.cs |
| `renderAutoForge` | 이름 그대로 | ForgeAutoPopup.cs |
| `renderChatFull` | 이름 그대로 | ChatScreen.cs |
| `renderChatList` | 이름 그대로 | ChatScreen.cs |
| `renderChatPreview` | 이름 그대로 | ChatScreen.cs,Hud.cs |
| `renderDebug` | 이름 그대로 | DebugPanel.cs |
| `renderDungeonDetail` | 이름 그대로 | DungeonDetailPopup.cs |
| `renderEquipSheet` | 이름 그대로 | ForgeSheet.cs |
| `renderForgeDetailView` | 이름 그대로 | ForgeInfoPopup.cs |
| `renderForgeInfo` | 줄기(접두 다름) | ForgeInfoPopup.cs,ForgeHost.cs,ForgeSheet.cs |
| `renderForgeLevelView` | 이름 그대로 | ForgeInfoPopup.cs |
| `renderForgeListView` | 이름 그대로 | ForgeInfoPopup.cs |
| `renderGearDetail` | 이름 그대로 | GearDetailPopup.cs |
| `renderLeagueBoard` | 이름 그대로 | LeagueSheet.cs |
| `renderLeagueChallenge` | 이름 그대로 | LeagueSheet.cs |
| `renderLeagueRewards` | 이름 그대로 | LeagueSheet.cs |
| `renderMenu` | 없음 | 원작 `renderMenu() {}` 빈 함수 — 옮길 것 없음 |
| `renderMountUpgrade` | 이름 그대로 | MountUpgradePopup.cs |
| `renderPass` | 이름 그대로 | PassPopup.cs |
| `renderPetUpgrade` | 이름 그대로 | PetUpgradePopup.cs |
| `renderPets` | 이름 그대로 | PetPanel.cs |
| `renderPlayerInfo` | 이름 그대로 | PlayerInfoPopup.cs,ForgeHost.cs |
| `renderProfile` | 이름 그대로 | ProfilePopup.cs |
| `renderProfileView` | 이름 그대로 | ProfilePopup.cs |
| `renderSettingsView` | 이름 그대로 | ProfilePopup.cs |
| `renderShop` | 이름 그대로 | ShopSheet.cs |
| `renderSkillBar` | 이름 그대로 | SkillBar.cs |
| `renderSkills` | 이름 그대로 | SkillPanel.cs |
| `renderSummonRates` | 이름 그대로 | SkillRatesPopup.cs |
| `renderTechBonuses` | 이름 그대로 | TechPopups.cs |
| `renderTechBranchView` | 이름 그대로 | TechPanel.cs |
| `renderTechNodeModal` | 이름 그대로 | TechPopups.cs |
| `renderTechOverview` | 이름 그대로 | TechPanel.cs |
| `renderTechTree` | 이름 그대로 | TechPanel.cs |
| `renderTopBar` | 이름 그대로 | MetaHost.cs,DungeonUiHost.cs,Hud.cs |
| `showAutoDropCard` | 이름 그대로 | ForgeCraftPopup.cs |
| `showCraftBatch` | 이름 그대로 | ForgeCraftPopup.cs |
| `showCraftModal` | 이름 그대로 | ForgeCraftPopup.cs |
| `showCraftReveal` | 이름 그대로 | ForgeCraftPopup.cs |
| `showDungeonClear` | 이름 그대로 | DungeonClearPopup.cs,DungeonUiHost.cs |
| `showModal` | 이름 그대로 | Popups.cs,PetSkillModal.cs |
| `showOffline` | 이름 그대로 | OfflinePopup.cs |
| `showSellConfirm` | 이름 그대로 | ForgeCraftPopup.cs |
| `toggleAuto` | 이름 그대로 | SkillBar.cs |

- 이름 그대로 84 · 줄기 14 · 없음 3(셋 다 위 표의 설명대로 **옮길 것이 없거나 이름만 다르다**) → 빠진 함수 **0**

## ⓓ SFX (`sfx.js` 24종 + 프리미티브 6 + 음악 4모드)

- 원작 코드가 `SFX.play(...)` 로 부르는 이름 0: 
- 유니티 `SfxRecipes.Names` 24: hit, bossSiren, anvilHit, stormRumble, stormCrackle, stormStrike, slashArc, arrowShot, mawRoar, mawBite, healDescend, auraRise, voidTear, voidPierce, voidSnap, equipToss, equipSnap, equipDrop, craft, craftReveal, levelUp, gacha, summonCharge, summonReveal
- 원작이 부르는데 유니티에 없는 것: **0** []
- 프리미티브 6(`tone·noiseBurst·thump·sparkle·ring·click`)은 `SfxSynth`(T30) · 음악 4모드(normal·boss·dungeon·shop)는 `SfxTable.MusicModes`·`Music.MusicMode`(T30) — `AudioTests` 가 벡터로 대조한다.

## ⓔ IconGen 키 (`icongen.js` `IconGen.draw` ↔ T31 아틀라스)

- 아틀라스 `Assets/Forge/Icons/Resources/Icons/atlas.json` 키 **170 = 아이콘 136 + 아바타 24 + tint 변형 10** — §7 T31 줄의 «아이콘 136종 · 아바타 24종 · tint 변형 10» 과 그대로 맞는다.
- 키 목록은 손으로 세지 않는다: `tools/export_icons.js` 135행이 정본을 headless Chromium 에서 실행해 `Object.keys(IconGen.draw)` 로 굽고, CI datasync 잡의 `check_icons_sync.sh`(T31)가 러너 chrome 으로 정본을 다시 그려 매 런 대조한다(런 148 초록) · EditMode `IconAtlasTests` 가 «키 ≥160 · 아바타 24 · 원작 키 25종+변형 10» 을 단언한다 → 빠진 키 **0**(빠지면 그 잡·테스트가 빨갛다).
- (1회차 초안이 정규식으로 `draw` 안 도우미 함수까지 세어 «98/54» 를 냈던 것은 틀린 셈이라 지웠다 — 정본 열거는 런타임 `Object.keys` 만이 맞다.)

## 열린 것(§7 에서 ✅ 아닌 칸 · 전부 등재돼 있고 임자 또는 조건이 있다)

- T28 🔄 진행 — `js/ui.js`(6,181) · `css/style.css` · `index.html`
- T75 🔄 진행 — `js/ui.js`(6,181) · `css/style.css` · `index.html`
- T33 🔄 진행 — `ref/screens/shot-*.png` 30장 · `tools/shot-*.js` ·
- T87 🔄 진행 — `css/style.css` 제작 키프레임 22종
- T86 🔄 진행 — WebGL 배포 · Android
- T84 🔄 진행 — (원작 밖 · 도구·게이트·CI) 병렬 운영을 지키는 자들 — 원작 모듈에 안 붙지만 **
- T35 ⬜ — `SIMPLE_BG=false` 배경 복원: **주인이 SIMPLE_BG 를 끌 때만**(§2 T35 · 정본이 지금 `SIMPLE_BG: true` 라 화면에 없다) — 원작 «있음/없음» 대조로는 «있음(끈 경로)» 이니 완주 전에 주인 결정이 필요하다.

## 이 회차의 판정
- 새로 빠진 것(등재 안 된 원작 모듈·화면·함수·효과음·아이콘): **0**. 열린 칸은 전부 §2 에 번호가 있다(T28 회차형 · T33 · T75 · T84 · T86 · T87 · T35 주인 결정).
- T33 ✅ 조건 = 위 열린 칸이 전부 ✅ + §7 머리에 «완주 YYYY-MM-DD · 커밋». 다음 회차는 이 파일의 «열린 것» 만 다시 센다.

---

# 2회차 — 2026-09-13 06:5x · 워커 H · sess-0657-6057

> 1회차가 남긴 «열린 것» 만 다시 셌다(1회차 지시 그대로). 기계 대조는 새로 돌렸다.

## 기계 대조 재확인(이 회차에 직접 돌린 것)
| 무엇 | 방법 | 결과 |
|---|---|---|
| 원작 화면 ↔ 클론 촬영 | `docs/ref-layout.md` 의 `## <이름> ↔ shot-*.png` 31줄 ↔ `screens` 브랜치 `screens.json` 34항목 | **원작에 있는데 클론에 없는 것 0** · 클론에만 있는 셋은 `mount-detail`·`mount-upgrade`(원작 샷이 없는 탈것 화면)·`main-notch`(노치 모의 촬영) |
| 효과음 | `Assets/Scripts/Core/Audio/SfxRecipes.cs` 의 `Names` | **24종** — 원작 `sfx.js` 24 와 같다 |
| 아이콘 키 | `tools/check_icons_sync.sh`(CI datasync 가 정본을 headless Chromium 으로 다시 그려 대조) | rc 0 |
| 수치 표 | `tools/check_data_sync.sh .wwwww-src` | rc 0(13파일) |
| 빌드·순수 테스트 | `dotnet build` · `dotnet test` | 0 오류 · **527/527** |

## §7 열린 칸 — 7개(1회차 때보다 줄었다)
1회차 뒤 **T53(한글 글꼴) · T75 · T77 · T83 · T84 가 닫혔다.** 지금 열린 칸은:

| 번호 | 무엇 | 완주 판정에 드는가 |
|---|---|---|
| T28 🔄 | 원작 대조 회차(회차형 · 12회차 진행 중) | 든다 — 마지막 회차가 ✅ 로 닫아야 한다 |
| T33 ⬜ | 이 작업 | 든다(자기 자신) |
| **T35 ⬜** | 배경 복원 `SIMPLE_BG=false` | **안 든다 — 결정 212**(정본이 지금 안 쓰는 경로 · §7 머리줄에 예외를 박았다) |
| T86 🔄 | WebGL 배포물이 부팅에서 죽는다 | 든다 |
| T89 ⬜ | 화면 이모지 30종 두부 | 든다 |
| T90 ⬜ | 한글 뒤 드러난 «글자가 칸 밖으로» | 든다 |
| T91 🔄 | 메인 채팅 미리보기 줄이 비었다 | 든다 |

**→ 완주까지 실제로 남은 것은 넷(T86 · T89 · T90 · T91) + 회차형 T28.** 넷 다 이 회차에 **임자 lock 이 살아 있다** — 이 작업은 그것들을 기다린다.

## 이 회차의 판정
- 새로 빠진 것(등재 안 된 원작 모듈·화면·함수·효과음·아이콘): **0**(1회차와 같다 · 위 표의 기계 대조로 다시 확인).
- T33 은 아직 ✅ 가 아니다 — 위 넷이 ✅ 가 되면 다음 회차가 §7 머리에 «완주 YYYY-MM-DD · 커밋» 을 적고 닫는다.
- **T35 를 완주 조건에서 뺀 것이 이 회차의 판단**(결정 212). 안 그러면 정본이 안 쓰는 경로 하나 때문에 «다 옮겨졌다» 가 영원히 성립하지 않는다.

---

# T33 완주 대조 — **3회차** (2026-09-13 17:5x · 워커 H · sess-1757-15757)

> 2회차가 «완주까지 남은 것은 넷(T86·T89·T90·T91)» 으로 멈춰 있었는데 그중 **셋(T89·T90·T91)이 ✅** 로 닫히고
> 그 사이 새 작업 여덟이 열렸다 — 목록이 낡아 다시 셌다. 기계 대조도 전부 새로 돌렸다.

## 기계 대조(이 회차에 직접 돌린 것)
| 무엇 | 방법 | 결과 |
|---|---|---|
| 원작 화면 ↔ 클론 촬영 | 정본 `web/ref/screens/*.png` 30장 ↔ `screens` 브랜치 `screens.json` 34항목의 `ref` | **원작에 있는데 짝이 없는 것 0** · 클론에만 있는 넷은 탈것 둘·노치 모의·짝 없는 촬영 |
| 효과음 이름 | `SfxRecipes.Names` ↔ 정본 `sfx.js` | 24종 그대로 |
| **효과음 «부르는 곳»** | `Names` 24종을 `Assets/Scripts` 전수와 대조(래퍼 `Core/Audio`·`Game/Audio` 정의는 호출이 아니다) | ✗ 이 회차의 손 대조로는 넷 — **뒤에 자(`check_sfx_calls.py` · T119 1회차)가 일곱으로 정정**(아래 ⓑ) |
| 아이콘 키 | `tools/check_icons_sync.sh`(정본을 headless Chromium 으로 다시 그려 대조) | rc 0 |
| 수치 표 | `tools/check_data_sync.sh .wwwww-src` | rc 0 |
| 키라인 | `tools/check_keyline.py` | rc 0 |
| 빌드·순수 테스트 | `dotnet build` · `dotnet test` | 0 오류 · **553/553** |
| §7 표 자체 | `check_final_table.py` + 이 회차가 더한 `unmarked()` | ✗ **T98 이 상태 칸에 없었다**(아래) |

## 이 회차가 새로 잡은 것 셋

### ⓐ §7 `ui.js` 줄의 상태 칸에 **T98 이 없었다** — 열린 작업이 완주 판정 위를 지나간다
작업 칸에는 «T98(모루 그림이 사각 근사)» 이 적혀 있는데 상태 칸(번호마다 제 표시를 다는 꼴)에만 빠져 있었다.
`check()` 는 «적혀 있는 표시» 만 보고 `unlisted()`(T69)는 «절 어딘가에 이름이 있는가» 만 보므로 **둘 다 못 본 구멍**이다.
→ 표시를 채워 넣고, 다시 안 나게 `tools/check_final_table.py` 에 `unmarked()`(작업 칸엔 있는데 상태 칸엔 표시가 없는 **열린** 작업)를 더했다(결정 254).

### ⓑ 정본 소리가 «구워는 놨는데 부르는 곳이 없다» — 손으로는 넷, 자로는 **일곱**
T30 이 24종을 전부 합성해 놓았지만 `Assets/Scripts` 어디에서도 안 우는 것이 있다.
**이 칸은 T119 1회차(워커 E · `tools/check_sfx_calls.py`)가 정정한 뒤의 값이다** — 내 손 대조는 `Sfx.Xxx` 참조만 봐서
ⓐ 메서드 묶음으로 넘긴 것(`SfxGacha = Sfx.Gacha`)을 «0» 으로 세고 ⓑ 훅 문자열(`PlaySfx("craft")`)을 «울린다» 로 셌다.
(`gacha`·`summonCharge`·`summonReveal` 은 그 메서드 묶음으로 꽂혀 있어 실제로 **울린다** — 그 셋은 처음부터 빠진 것이 아니었다.)

| 소리 | 정본 호출 자리 | 클론 | 임자 |
|---|---|---|---|
| `craftReveal` | `ui.js` 1938 `showCraftReveal` · 1976 `showCraftBatch` — 인자는 **나이 인덱스** | 호출 0 | T119 ⓐ(`ForgeCraftPopup` · T87 lock 뒤) |
| `equipToss` | `ui.js` 3428(`playEquipSwapFx` 안 · 던질 때) | 호출 0 | T118 |
| `equipDrop` | `ui.js` 3439(같은 함수 · 착지) | 호출 0 | T118 |
| `craft` · `anvilHit` · `equipSnap` | 대장간 전부 | `ForgeHost` 가 부르지만 **훅 `ForgeHost.Sfx` 를 아무도 안 꽂아** 통째로 무음 | **T120**(워커 E 등재) |
| `levelUp` | 대장간 레벨업 · 연구 완료 · 던전 클리어 | 호출·훅이 아예 없다 | T120 |
| `stormCrackle` | `scene3d.js` 14101 | **옮기지 않는다** | — |

> **`stormCrackle` 은 내 ⓑ 목록의 오판이었다(결정 258 · 워커 E)**: 정본에서 그 소리를 부르는 유일한 자리가
> `_legacyStormCloudStrike` 안이고 **그 함수를 부르는 데가 없다**(살아 있는 길은 `scene3d-skillfx.js` 663 `mcThunderStrike` ·
> 거기선 `stormStrike` 만 운다 — 3회차 뒤 직접 다시 확인했다: `_legacyStormCloudStrike` 는 정의 한 곳뿐이고 호출 0).
> «빠진 소리» 로 보고 넣었으면 그것이 §1 이 막는 «원작에 없는 것» 이 된다. 얻은 규칙: **«클론이 안 부른다» 다음에는
> 반드시 «정본은 그 길을 실제로 밟는가» 를 물어야 한다.**

### ⓒ 그 소리들의 뿌리 — **연출 둘이 통째로 없다**
| 정본 | 무엇 | 클론 |
|---|---|---|
| `ui.js` 3478~3560 `coinBurst(total)` | 장비를 팔면 모루 위에서 코인 3~10개가 격자 착지점으로 날아가 각자 «합÷개수» 를 단다 · 팝업·패널이 열려 있으면 **소리까지 통째로 생략** | 호출 0 — `ForgeHost.DoResolveCraft` 530 은 `GearSys.Sell(item)` 만 한다 |
| `ui.js` 3329 `grabEquipSwapFx` · 3362~3470 `playEquipSwapFx` | 장비를 바꾸면 옛 장비가 화면 **바깥쪽**으로 정수 바퀴(360°·720°) + 기울기 8~22° 로 돌며 바닥에 눕는다 · 팝업이 열려 있으면 착지점을 카드 옆 띠로 · `prefers-reduced-motion` 이면 생략 | 대응 0 |

→ **T117**(판매 코인 연출) · **T118**(교체 던져내기 연출) · **T119**(남은 소리 둘 + «호출 0» 자) 등재 — 처음 뽑은 T114~T116 은 같은 순간 검수 Q 가 T114 를 먼저 push 해 규약대로 내가 옮겼다(결정 255).

## §7 열린 칸 — 지금 열둘(2회차의 «넷» 은 낡았다)
| 번호 | 무엇 | 완주 판정에 드는가 |
|---|---|---|
| T28 🔄 | 원작 대조 회차(회차형 · 24회차) | 든다 |
| T33 ⬜ | 이 작업 | 든다(자기 자신) |
| **T35 ⬜** | 배경 복원 `SIMPLE_BG=false` | **안 든다 — 결정 212** |
| T86 🔄 | WebGL 배포물이 부팅에서 죽는다 | 든다 |
| T87 🔄 | 대장간·제작 연출 전수(24회차) | 든다 |
| T94·T98·T104·T106·T108·T109·T110·T113 ⬜ | 화면 결함 여덟 | 든다 |
| **T117·T118·T119 ⬜** | 이 회차가 등재한 셋 | 든다 |
| T114 ⬜ | 검수 Q 가 같은 시각에 등재(상세 카드 확률 줄) | 든다 |

## 이 회차의 판정
- 새로 빠진 것: **셋**(1·2회차의 «0» 은 «이름이 있는가» 만 봤기 때문이다 — 이번엔 «부르는 곳이 있는가» 로 물었더니 나왔다).
  얻은 규칙: **이름 대조는 «있다» 를 너무 쉽게 말한다 — 호출·연출은 «부르는 곳» 을 세야 한다.** 그 자가 T116 ⓐ 다.
- T33 은 아직 ✅ 가 아니다. 위 열둘 중 T35 를 뺀 전부가 ✅ 가 되면 다음 회차가 §7 머리에 «완주 YYYY-MM-DD · 커밋» 을 적고 닫는다.

## 3회차 덧붙임 — 판정 · lock 반납 (2026-09-13 18:5x · 워커 H · sess-1857-3434)
- **CI 런 226**(`12a4a35`) **초록** — 3회차 커밋이 한 바퀴 돌았다. 규약대로 `docs/claims/T33.lock` 을 반납한다(T33 은 ✅ 가 아니다 · 다음 회차가 다시 잡는다).
- 3회차가 등재한 셋은 **한 시간 만에 둘이 움직였다**: T117 1회차 ✅(워커 R · 런 229 초록 · `CoinBurstTests` 2 PASS · 호출 한 줄만 T87 lock 뒤) · T119 1회차(워커 E · 소리 자 + T120 «대장간 무음» 을 캤다) · T118 은 아직 임자 없음.
- 위 ⓑ 표를 그 정정대로 고쳐 적었다(넷 → 일곱 · `stormCrackle` 은 내 오판).

---

# T33 완주 대조 — **5회차** (2026-09-14 00:2x · 워커 J · sess-0017-5733)

4회차(워커 F)가 IconGen 키에 «부르는 곳이 있는가» 를 물었다. 아직 그 눈을 안 댄 축이 하나 남아 있었다 —
**정본 `css/style.css` 의 `@keyframes`**. T87 이 덮는 것은 «제작 키프레임 22종» 뿐인데 정본에는 **120종**(중복 포함 123)이 있다.

## ⓕ `@keyframes` 120종 ↔ 클론 (이 회차가 새로 연 축)

- 뽑는 법: `@keyframes <이름>` 전수 → 그중 정본 안에서 `animation:` 으로 **실제로 쓰이는 것만**(0개였다 — 정본에 죽은 키프레임은 없다)
  → 이름을 kebab·camel·Pascal·snake 로 펴서 `Assets/Scripts/**/*.cs` + `Assets/Forge/**/*.json` 전수 대조.
- 결과: **자취 있음 40 · 자취 0 이 80**. 80 을 그대로 «빠졌다» 로 적으면 3회차의 `stormCrackle` 오판을 되풀이한다 —
  네 갈래로 갈랐다.

| 갈래 | 몇 종 | 무엇 | 판정 |
|---|---|---|---|
| **결정으로 이미 닫힌 것** | 45 | `sr*` 전부 — `#summon-result-modal`(소환 결과) 의 방사선·빛줄기·소환진 눈금·먼지·반사 | **빠진 것이 아니다** — **결정 71**(T20 · 워커 B)이 «CSS 다층 그라데이션은 정점색 원판·광채 원·섬광·바닥 타원으로 줄였다 · 주인 눈 확인» 으로 기록했다. 연출 타임라인 자체는 `SkillSummonResult.cs`(654줄)에 그대로 있다. |
| **이름만 다르고 옮겨진 것** | 17 | `bw*` 7(보스 워닝 → `Battle/BossFx.cs`) · `dgc-pop`·`dgclear-sink`(→ `Ui/DungeonClearPopup.cs`) · `dmgrise`(→ `Battle/DamageNumbers.cs`) · `cutin`·`skflash`(→ `SkillFx/SkillFxDirector.cs`) · `tt-ready`(→ `Ui/TechPanel.cs`) 등 | 옮겨졌다. **이 축은 이름 대조로는 못 본다** — 클론은 CSS 클래스명을 안 쓰고 C# 이름을 쓴다. |
| **이미 등재된 것** | 8 | `gp-*` 6(시대 무늬 → **T124**) · `ob-bob`·`ob-zzz`(오프라인 버튼 → **T133**) | 임자 있음 — 이 축이 그 둘을 독립적으로 다시 짚었다(= 방법이 맞다는 교차 검증). |
| **새로 캔 것** | 10 | `rw*` 7 + `dmgvignette` · `cardpop` · `newpulse`·`shinesweep` · `lootpop` | **T134 · T135 등재** ↓ |

### 새로 캔 것 — T134 · T135
- **T134 · `rw*` 7종 = 공통 수령 연출 `rewardBurst`**: 정본 `ui.js` 3564 정의 + **호출 여덟 자리**(`dungeons.js` 180 · `ui.js` 4607·4623·4720·4941·5004·5941 — 던전 소탕·퀘스트 개별/일괄·던전 클리어·상점 무료칸·패스·오프라인). 보상을 받으면 아이콘이 누른 자리에서 터져 상단바 pill 로 날아가 흡수되고, 가려진 pill 은 카드 모서리로 승격해 앵커 배지를 세운다. 클론은 키프레임 7종 자취 0이고 **유일한 대응이 `DungeonSheet.cs:96` 의 주석 «원작 rewardBurst 대신(T22 공용 연출 전) — 한 줄»** 이다 — 비워 둔 것을 스스로 적어 놓았는데 아무도 채우지 않았다. T117(`coinBurst`)·T118(장비 교체)과 같은 갈래다.
- **T135 · 나머지 넷**: ⓐ 피격 붉은 비네트 `flashDamage`(`scene3d.js` 13351·13374 호출 · 자취 0) ⓑ 모달 열림 카드 팝 `cardpop`(자취 0) ⓒ 비교 카드 NEW 강조 `newpulse`+`shinesweep`(자취 0) ⓓ 전리품 피드 `floatLoot` — 정본은 화면 고정 스택 `#loot-feed`(최대 6줄·1.6초)인데 클론 `BattleScene.cs:363` 은 **영웅 위에 뜨는 숫자**다(옮겼으나 자리가 다르다).

## 이 회차가 얻은 규칙
> **«자취 0» 은 네 갈래다 — «빠졌다» 는 그중 하나뿐이다.**
> ⓐ 결정으로 이미 닫힌 축약 ⓑ 이름만 다르게 옮겨진 것 ⓒ 이미 등재된 것 ⓓ 진짜로 빠진 것.
> 이름 대조(3·4회차)는 ⓑ 를 «빠졌다» 로, 값 대조는 ⓐ 를 «빠졌다» 로 잘못 읽는다.
> **80 → 10** 으로 줄인 것이 이 회차 일의 대부분이었다. 다음 축을 여는 사람은 이 네 갈래 표를 먼저 만들라.

## 다음 회차가 열 축(아직 «부르는 곳» 을 안 물은 것)
- `index.html` 의 id·data-* 속성 ↔ 클론 이름표 · 정본 `state.js` 의 세이브 필드 ↔ 클론 저장 트리 · `css` 의 `@media`(반응형 분기) ↔ 클론 SafeArea·비율 갈래.

## 이 회차의 판정
- 새로 빠진 것: **열 종 · 작업 둘**(T134 · T135). T33 은 아직 ✅ 가 아니다.

# T33 완주 대조 — **6회차** (2026-09-14 01:2x · 워커 F · sess-0127-63118)

5회차(워커 J)가 «다음 회차가 열 축» 으로 셋을 적어 뒀다 — `index.html` id·data-* · `state.js` 세이브 필드 ·
`css @media`. 이 회차는 앞의 둘을 열었다.

## ⓖ `state.js` 세이브 필드 ↔ 클론 저장 트리 — **빠진 것 0 · 축을 닫는다**

- 뽑는 법: `defaultState()` 의 최상위 키 **55개** + 고정 형태 레코드 다섯(`settingsDummy`·`autoForge`·
  `lineAscend`·`summonMult`·`equipment`)의 안쪽 키 24개 → 클론 `Assets/Scripts/**/*.cs` 전수 + 데이터 JSON.
- 결과: **55/55 양방향 일치**. 중첩 24개도 전부 있다(`equipment` 여덟 부위는 코드가 아니라
  `gamedata.json` 의 `SLOTS` 로 온다 — §1 «수치는 코드에 박지 않는다» 대로다).
- 상수 선언만 있고 안 쓰는 필드도 **0개**였다(키 상수 이름으로 다시 세어 확인).
- **왜 이 축은 앞으로 다시 안 봐도 되는가**: 클론의 기본 세이브는 손으로 적은 표가 아니라
  `tools/export_data.js` 가 정본에서 뽑은 `StreamingAssets/data/state.json` 의 `DEFAULT_STATE` 다.
  그 파일은 매 커밋 `check_data_sync.sh` 가 정본과 바이트로 대조한다 — **구조적으로 어긋날 수 없다.**
  다음 회차는 이 축에 시간을 쓰지 말 것.

## ⓗ `index.html` id 54개 ↔ 클론 — **셋을 캤다**

- 뽑는 법: `id="…"` 전수 54 → kebab·camel·Pascal·snake 로 펴서 `Assets/**` 전수 대조 → 자취 0 이 23.
- ⚠ 23 을 그대로 «빠졌다» 로 적으면 3회차 `stormCrackle` 오판을 되풀이한다. DOM id 는 SFX·IconGen 키와
  달리 **이름이 계약이 아니다**(`chat-modal` = `ChatScreen`, `shop-modal` = `ShopSheet` …). 그래서
  이름이 아니라 **기능**으로 하나씩 물었다.
- 기능까지 없는 것 셋 → **T138**(`loot-feed` 전리품 레인 + `toasts-combat` 전투 토스트 레인) ·
  **T139**(`waypoint-mystery`·`waypoint-pass` 이정표 버튼 둘).
- **판정 보류 하나**: `boot-loading`(모루+망치 부팅 오버레이 · 진행 막대 · «불 지피는 중…»).
  유니티 빌드는 제 로딩 화면이 따로 있어 «같은 자리» 가 아니다 — 옮길지 말지는 주인 조형 판단이라
  등재하지 않고 여기 적어 둔다. 나머지 19 는 이름만 다르고 기능이 있다(눈으로 하나씩 확인).

## ⓘ 덤으로 캔 것 — 자 자신의 구멍 (**T137**)

`index.html` 축을 훑다 `Core/Battle/Battle.cs` 의 전투 문구가 두부 막이에 한 번도 안 걸린 것을 봤다.
`check_text_glyphs.SCAN_DIR` 이 `Assets/Scripts/Game` 하나라 **`Assets/Scripts/Core` 2,388줄이 눈 밖**이고,
거기 **진짜 두부 둘**(`🚪` Dungeons.cs:237 · `🔥` Battle.cs:578 — 글꼴에도 아이콘 표에도 없다)이
초록으로 지나가고 있었다. 아이콘 표 글자도 Core 에 14종 22자리가 있어 T107 의 «아이콘 길» 판정이
그 자리에는 한 번도 안 걸렸다.

## 이 회차의 판정
- 새로 빠진 것: **작업 셋**(T137 · T138 · T139) · 축 하나를 **닫았다**(세이브 필드).
- T33 은 아직 ✅ 가 아니다. 7회차가 열 축: `css @media`(반응형 분기) ↔ 클론 SafeArea·비율 갈래 ·
  `ui.js` 공개 함수 97개 · 원작 화면 30장.

---

# T33 완주 대조 — **6회차** (2026-09-14 02:2x · 워커 J · sess-0217-24026)

5회차가 적어 둔 «다음 축 셋» 을 한 회차에 다 열었다. 결과는 **둘은 깨끗, 하나에서 둘을 캤다**.

## ⓖ 정본 `state.js` 세이브 필드 ↔ 클론 저장 트리 — **0건(깨끗)**

이 축은 «화면이 이상하다» 가 아니라 **«진행이 조용히 안 남는다»** 를 잡으러 열었다.

| 물음 | 결과 |
|---|---|
| 최상위 45칸이 클론에 있는가 | **45/45 있다** |
| 중첩 레코드(settingsDummy 4 · autoForge 5 · summonMult 3 · lineAscend 4 · equipment 8 · quests·pets·mounts·eggs·hatching·techResearch·dungeonRun 항목 꼴) | 전부 있다. `equipment` 의 gloves·necklace·shoes·belt 와 `skills` 의 powerStrike 가 «없다» 로 뜬 것은 **오탐** — 클론은 그것을 `gamedata.json` 의 `SLOTS`·스킬 표에서 읽는다(§1 «수치를 코드에 박지 않는다» 를 지킨 결과다) |
| **쓰기만 하고 읽는 데가 없는 칸은 없는가** | «한 파일에서만 보이는 칸» 11개를 전부 따라가 봤다 — 전부 타입 있는 속성(`Kills`·`AutoCast`·`AutoForgeOn`·`HatchSlotBonus`·`Cleared` …)으로 흘러 다른 파일에서 읽힌다. 예: `questsCleared` → `MetaSave` → `QuestState.Cleared` → `Quests.Tier()` → `NeedOf()` 로 정본 `quests.js` 90·154 와 같은 길 |

## ⓗ `@media` 분기 — **0건 · 5회차의 내 추측이 틀렸다**

5회차에 «`@media`(반응형 분기)» 라고 적었는데, 정본의 `@media` 는 **여섯 개뿐이고 전부 접근성**이다
(`prefers-color-scheme: dark` 1 · `forced-colors: active` 1 · **`prefers-reduced-motion: reduce` 4**).
**반응형 breakpoint 는 0개다** — 정본은 고정 9:16 앱 상자라 애초에 없다. 다음 사람이 이 축을 다시 열 필요 없다.
`prefers-reduced-motion` 넷도 이미 닫혀 있다 — **결정 263**(«유니티에 그 OS 신호가 없어 두지 않는다» · `EquipSwapFx`) 과
`AgePattern.Motion` 스위치가 그 자리다.

## ⓘ `index.html` 의 id 54개 ↔ 클론 — **2건 캤다**

- 날것으로 훑으면 «자취 0» 이 22개인데, 그중 14개는 **꼬리 이름 차이**다(정본 `#pet-upgrade-modal` ↔ 클론 `ModalName = "pet-upgrade"`). `-modal`·`-btn` 꼬리를 펴니 8개가 남았고, 그중 `game3d`(유니티 카메라) · `panel-pets`(→ `PetPanel`) 는 옮겨진 것이었다.
- **남은 여섯이 둘로 묶인다**:
  - **T141 · 맵 위 이정표 둘** — `#waypoint-mystery`(+ `#waypoint-mystery-time` 일일 초기화 카운트다운) · `#waypoint-pass`. 아이콘 `wp_mystery` 는 **T31 아틀라스에 이미 구워져 있는데** `ForgeUi.cs:81` 이 폴백 키로만 쓴다 — **T130·T133 과 똑같은 꼴**이다(그림은 있고 부르는 곳이 없다). 리그 이정표는 정본에서 주인 지시로 삭제됐으니 옮기지 않는다.
  - **T142 · 부팅 로딩 화면** — `#boot-loading`(모루+망치+불티 · 제목 · 진행바 `#bl-fill` · 단계 글자 `#bl-stage`). 부팅을 일곱 단계로 쪼개 퍼센트를 올린다(8 «세이브 불러오는 중…» → 96 «마무리 중…» → 100). 정본 주석이 왜 있는지까지 적어 뒀다: «첫 렌더의 셰이더 컴파일은 없앨 수 없으니 **로딩창 아래에서 소화한다**». **주인이 제일 먼저 보는 화면인데 클론엔 없다.**

## 이 회차가 얻은 규칙
> **«자취 0» 을 세기 전에 이름 꼬리를 먼저 편다.** id 축은 날것으로 22건이었고 꼬리(`-modal`·`-btn`)만 펴도 8건이 됐다 —
> 5회차의 네 갈래 표(결정 290)에 «ⓑ 이름만 다르게 옮겨진 것» 이 있는 이유가 이것이다. 꼬리를 안 펴면 회차가 남의 시간을 쓴다.

## 다음 회차가 열 축(아직 안 물은 것)
- 정본 `ui.js` 의 **토스트·문구 표**(사용자에게 보이는 문장 전수) ↔ 클론 문자열 · `index.html` 의 `data-*` 속성 · `css` 의 `::before/::after` 장식 층(키프레임 없이 그림만 있는 것).

## 이 회차의 판정
- 새로 빠진 것: **둘**(T141 · T142). 두 축(세이브 · `@media`)은 깨끗했고, 그중 하나는 **다시 열 필요 없음**으로 닫았다. T33 은 아직 ✅ 가 아니다.

---

# T33 완주 대조 — **7회차** (2026-09-14 03:3x · 워커 J · sess-0317-25749)

축: **정본 토스트·문구 표 전수** — «클론이 **같은 말을** 하는가».

## ⓙ 토스트 81 호출 · 문구 71개 ↔ 클론 — **1건(셋 묶음)**

- 뽑는 법: 정본 `js/*.js` 의 `UI.toast(` · `this.toast(` 호출을 전부 긁어(**81 호출** · `combat` 4 · `dungeons` 11 · `forge` 1 · `league` 1 · `pets` 4 · `techtree` 1 · `ui` 59) 리터럴 문구 **71개**를 뽑고, `${...}` 를 걷어낸 **핵심 한글 덩어리**로 `Assets/Scripts` + `Assets/Forge` 를 대조했다(문구가 토막나 있어도 걸리게).
- 결과: **71 중 68 이 그대로 있다.** 어긋난 셋을 **T143** 으로 묶었다.

| # | 정본 | 클론 | 갈래 |
|---|---|---|---|
| ⓐ | `ui.js` 4462 — 슬롯이 꽉 차면 «스킬은 최대 N개 장착 가능합니다» | `SkillPanel.cs:443` 이 `Sk.ToggleEquip(id);` 로 **bool 반환을 버린다** — 눌러도 아무 일이 없고 이유도 없다 | **빠졌다** |
| ⓑ | `techtree.js` 383 — 연구가 끝나면 `SFX.levelUp()` **과 함께** «🔬 <이름> <단계> Lv.N 연구 완료!» | `TechPopups.cs:69` 은 **소리만** 울린다(실패 갈래 둘은 이미 있다) | **빠졌다** |
| ⓒ | `league.js` — «🏆 리그 시즌 종료! 순위 보상을 획득했습니다» | `LeagueSheet.cs:33` «🏆 리그 시즌 종료! N위 보상 지급» | **원작에 없는 말을 더했다**(§1) |

- ⓐ 가 특히 값진 자리다: **같은 화면의 펫 쪽은 제대로 말한다**(`toast_pet_max`). 한 화면 안에서 갈렸으니 설계가 아니라 빠뜨림이다.

## 이 회차가 얻은 규칙
> **«문구 축» 은 «있는가» 가 아니라 «같은 말을 하는가» 로 묻는다.** 셋 중 하나(ⓒ)는 문구가 **있는데 달랐고**,
> 둘(ⓐ·ⓑ)은 **부르는 자리에서 반환값·성공 갈래를 버려서** 조용했다. 즉 이 축의 결함은 «문자열이 없다» 가 아니라
> **«말할 자리에서 안 말한다»** 로 나타난다 — 문자열 대조만으로는 ⓐ·ⓑ 를 못 잡고, 호출부까지 따라가야 한다.

## 다음 회차가 열 축(아직 안 물은 것)
- `index.html` 의 `data-*` 속성 · `css` 의 `::before/::after` 장식 층(키프레임 없이 그림만 있는 것) · 정본 `title=` 툴팁 문구.

## 이 회차의 판정
- 새로 빠진 것: **작업 하나**(T143 · 자리 셋). T33 은 아직 ✅ 가 아니다.

---

# T33 완주 대조 — **8회차** (2026-09-14 04:3x · 워커 J · sess-0417-4883)

7회차가 남긴 축 셋을 한 회차에 다 열었다. **셋 다 깨끗 — 새로 빠진 것 0.** 세 축 모두 «다시 열 필요 없음» 으로 닫는다.

## ⓚ `::before/::after` 장식 층 — **0건**

- 정본 `style.css` 의 `::before/::after` 규칙 **50개** 중 `animation` 이 없는 «그림만» 이 **36개**. 그 셀렉터의 클래스·id 이름을 `Assets/Scripts`+`Assets/Forge` 에 대조.
- **자취 0 이 5개**였고 전부 설명이 붙는다: `.sr-wrap::before/::after` · `.sr-canopy::before` · `.sr-stars i::before/::after` 넷은 **결정 71** 이 닫은 소환 결과 모달 축약(5회차 ⓐ갈래와 같은 덩어리) · `.tech-branch-icon::before`(아이콘 뒤 받침 원판)는 클론이 **`DungeonPopups.BorderedCircle`** 로 이미 그린다(`TechPanel.cs:190`) — 이름만 다르다.

## ⓛ `data-*` 속성 — **0건**

- 정본 `index.html`+`js` 의 `data-*` **21종** 중 자취 0 은 **셋**(`data-tid` · `data-ageidx` · `data-nameidx`).
- 셋 다 **기능이 아니라 DOM 기법**이다: `data-tid` 는 기술 노드 상자를 `querySelectorAll('.tech-tree-node[data-tid]')` 로 되찾아 연결선을 그리려는 조회 키(`ui.js` 5490)고, `data-ageidx`·`data-nameidx` 는 장비 칸 element 에 값을 얹어 뒀다가 `ui.js` 2191~2193 에서 **되읽어 아이템 객체를 복원**하는 자리다. 클론은 C# 객체를 그대로 쥐고 있어 얹었다 되읽을 일이 없다.

## ⓜ `title=` 툴팁 — **0건**

- 정본 `index.html` 의 `title=` 은 **3개**뿐이고 전부 클론에 있다.

## 이 회차가 얻은 규칙
> **«정본에만 있는 것» 중에는 «웹이라서 필요했던 것» 이 섞여 있다.** DOM 조회 키(`data-tid`)·DOM 값 보관(`data-ageidx`)처럼
> **브라우저에서 상태를 들고 다니려고 만든 장치**는 옮길 대상이 아니다 — 유니티는 객체를 직접 쥐기 때문이다.
> 5회차 네 갈래(결정 290)에 이 다섯째 갈래를 더한다: **ⓔ 원작의 «구현 수단» 이지 «기능» 이 아닌 것.**

## 다음 회차(9회차)가 열 축 — 아직 아무도 안 연 것
- **`ref/UI-SPEC.md` 조항 전수** ↔ 클론(T28 은 PNG 를 보지만 SPEC **조항**을 한 줄씩 대조한 회차는 없다) · **`ref/POLISH.md` 항목 전수** · 정본 `main.js` 의 **부팅 순서·자동저장 주기** ↔ 클론 `Bootstrap`·`Lifecycle`.

## 이 회차의 판정
- 새로 빠진 것: **0**. 축 셋을 닫았다. T33 은 아직 ✅ 가 아니다(§7 전 줄 ✅ 가 조건).

---

# T33 완주 대조 — **9회차** (2026-09-14 05:3x~06:0x · 워커 S · sess-0029-41207)

8회차가 남긴 축 셋 중 **둘**을 열었다(`ref/POLISH.md` 항목 전수는 안 열었다 — 10회차 몫). **둘 다 깨끗 — 새로 빠진 것 0.**

## ⓝ `ref/UI-SPEC.md` 조항 전수(66) ↔ 클론 — **0건**

- 방법: 조항마다 «클론의 어느 파일이 그것인가» 를 코드 grep 으로 잡고, 문구·수치가 있는 조항은 그 문구(«수집까지» · «열쇠는 던전을 완료할 때만» · «09:00» · «빠른 장착» · «모두 업그레이드» · «슬롯+1» · «건너뛰기» · «서버 시간» · «파워 랭킹/클랜 랭킹» · «메시지 보내기…» · «0.0000%»)로 다시 잡았다. T28 `ref-layout.md` 의 화면 31절이 «자리» 를 이미 재고 있으므로 이 축은 «있는가·같은 말을 하는가» 만 물었다.
- 공통 레이아웃 4 · 메인 8(이정표 = T139 ✅ · 알 칸 = `ForgeSheet` held-slot · 업그레이드 남은 시간 = `ForgeSheet`) · 오프라인 3(T144 ✅) · 리그 5(`LeagueSheet` — 보상 6종 `RewardCurs` · «수집까지» · 티켓 0/5 · 상대 ⭐+N 350행) · 던전 4(`DungeonSheet`·`DungeonDetailPopup` — 09:00 · 완료할 때만 · ◀▶ · 소탕/입장) · 스킬 9(`SkillPanel`·`SkillRatesPopup` — 15/18 · 패시브 배너 · 조각 게이지 · 장착됨 · 빠른 장착 · x5 · ℹ) · 펫 7(`PetPanel`·`PetUpgradePopup`·`PetHatchCone` — 부화장 3 · 슬롯+1 · 합칠 펫 = `AbsorbMaterials`) · 기술트리 5(`TechPanel`·`TechPopups` — 카드 4 · 노드 N/5 · 건너뛰기 ◆) · 상점 3 · 패스 2 · 프로필/설정 3(`ProfilePopup` — 연필 · 랭킹 둘 · 서버 시간 · 토글) · 대장간 팝업 5(`ForgeInfoPopup`·`ForgeAutoPopup` — 두 열 확률 · 목록 0.0000% · 서브스탯 13 · 유지/필터) · 비교 2(T113 ✅ · 위 장착됨/아래 새) · 세부 1 · 플레이어 정보 5(`BattlePreview` · 8칸 · 출전 줄 · 보유 옵션 목록 219행) · 채팅 4(`ChatScreen` — 공유 카드 · ◀ · 입력) · 승천 6(`AscendPopup` · 별 = 카드마다) · 재화 표 8 — **전부 자취 있음**.
- **SPEC 에만 있고 정본(web)에 없는 것**은 옮기지 않는다(§1 «정본이 지금 하는 것» · 결정 290 갈래): ⓐ 도전 티켓 «🎟 파랑» — 정본도 `IconGen.img('ticket')` 하나로 스킬 티켓과 같이 그린다(`ui.js` 4870) → 클론의 `ticket` 하나가 맞다 ⓑ 펫 업그레이드 «선택 슬롯 5칸» — 정본은 등급별 일괄 선택 칩 + 목록(`AbsorbMaterials`) → 클론이 정본과 같다 ⓒ 「❗현재 클론과 차이」 줄들(열쇠 소모 시점·리셋 09:00·난이도 선택·던전 탭)은 정본이 이미 고친 뒤라 클론도 같다.

## ⓞ `main.js` 부팅 순서 · 타이머 · 저장 시점 ↔ `Bootstrap`·`SaveIo`·`AppLifecycle`·호스트 — **0건**

| 정본 `main.js` | 클론 |
|---|---|
| `boot()`: loadGame → Dungeons/TechTree/Mounts/Ascension/Forge/Pass/Chat `ensure` → `pendingOffline` → UI.init → Scene3D.init → fitLayout → Combat.start → Dungeons.restoreRun → League.ensure → renderTopBar/updateStageLabel → warmup → 오프라인 팝업(≥60초) → restorePendingCraft → autoSeq | `SaveIo`(-900) 로드·보정 → `AppLifecycle`(-850) → `UiRoot`/`Bootstrap`(카메라·상자) → `MetaHost.OnReady`(League/Shop/Pass/Chat) → `BattleScene.Boot` + `BattleSaveGlue`(`Dungeons.RestoreRun` 73행) → `ForgeHost`(`RestorePendingCraft` 203행 · `StartAutoSeq` 204행) → 오프라인 팝업 `MetaHost` 152~153(`Elapsed >= 60`) · 부팅 화면 단계표는 **T142**(진행 중) |
| 논리 틱 `LOGIC_TICK_MS = 100` · hidden 이면 멈춤 · 복귀 시 `min(5000, …)` 따라잡기 | `BattleRules.Tick`(100ms 누적 · `BattleScene` 225행) · `AppLifecycle.MarkPaused/ResumeNow` · `Lifecycle.BackgroundGapMs = 5000` |
| 1초 틱: Forge.tickUpgrade · TechTree.tick · Pets.tick · Dungeons/League/Shop `ensure`(09:00) · Chat.tick · UI.tickSecond | `ForgeHost.Engine.TickUpgrade` 287행 · `TechPopups.Tick` · `PetSkillHost.TickSec = 1` · `DailyReset.ResetDateKey`(MetaHost 76) · `Chat.Tick` (MetaHost 267) |
| 3초 틱: 자동 제련 `autoSeqStep` · 열린 플레이어 정보/장비 세부 재렌더 | `ForgeHost` autoSeq(204·294·481) · 팝업 재렌더는 `Touch` 이벤트(밀기 대신 변화 시) |
| 저장: 30초 · `visibilitychange` hidden · `beforeunload` | `SaveIo.AutosaveIntervalSec = 30` · `OnApplicationPause/Focus` · `OnApplicationQuit` |
| 첫 `pointerdown` 에 `SFX.resume()`·`startMusic()` | `Sfx.Resume`(빈 몸 — 유니티는 사용자 제스처 잠금이 없다) · `Music` 부팅 시작 |

## 이 회차가 얻은 규칙
> **SPEC(원본 폰 게임 관찰 문서)과 정본(web)이 다를 때 기준은 정본이다.** SPEC 은 정본을 만들 때의 목표였고 정본은 그 뒤 주인 지시로 갈라진 자리(아이콘 통합·재료 선택 UI·던전 규칙)를 스스로 적어 두었다 — 클론은 §1 대로 «정본이 지금 하는 것» 을 따르고, SPEC 조항은 «정본에 그 기능이 있는가» 를 묻는 체크리스트로만 쓴다.

## 다음 회차(10회차)가 열 축 — 아직 아무도 안 연 것
- **`ref/POLISH.md` 항목 전수**(33항목 · 448줄) ↔ 클론 — 그래픽 목표 문서라 «정본(web)이 실제로 구현한 항목» 만 대상이다(정본에도 없는 목표 항목은 옮기지 않는다).

## 이 회차의 판정
- 새로 빠진 것: **0**. 축 둘을 닫았다. T33 은 아직 ✅ 가 아니다(§7 전 줄 ✅ 가 조건 · 열린 줄은 T28·T35·T110·T128·T136·T138·T142·T146 …).

---

# T33 완주 대조 — **10회차** (2026-09-14 05:4x~06:1x · 워커 S · sess-0029-41207)

9회차가 남긴 마지막 축 `ref/POLISH.md` 를 열었다. **새로 빠진 것 1(T147).**

## ⓟ `ref/POLISH.md` 항목 전수 ↔ 클론 — **1건**

- POLISH.md 는 «항목 표» 가 아니라 정본 그래픽 폴리싱의 **진행 로그**(2026-08-16~20 · 13 엔트리)이고 **전부 영역 ① 지형/배경**이다(②~⑥ 엔트리 0). 엔트리가 남긴 기능을 «정본이 지금 하는가 → 클론에 있는가» 로 갈랐다:
  - **SIMPLE_BG 로 꺼진 것(정본에 지금 없음 → T35 · 결정 212)**: 소품·중경 지형선·근경 앵커(`buildProps` 3146 return) · 능선 3겹 `RIDGE_MIX`(3011) · 하늘 돔·헤이즈·해달(1479 `visible = false`) · 구름(1730) · 공중 불티(1832) · 안개 블롭(3074) · 용암 발광 데칼 `lavaGlows`·`crackWorldSpots`·악센트 라이트 추종(3212~3244 · 18778 `crackLit` 조건) — 악센트 포인트라이트 3기는 씬에 상주하지만 intensity 0.
  - **SIMPLE_BG 와 무관하게 남은 것**: 지면 소재(사막 리플 배음·위상 잠금 해체·균열망·타일 이음매·포석 데칼) → **T34 ✅**(정본 함수 그대로 돌려 벡터 → 바이트 동일) · `terrainShade`/`applyShadeLift` → **T38 ✅** · `applyRimLight`/`setRimLook`(캐릭터 림) → **T39 ✅**(`FxRules.Rim*` · `Forge/EnemyBody`) · 월드 Rim 라이트 → T9 ✅(`WorldGrade.RimIntensity`).
  - **빠진 것 1**: 후처리 **깊이-엣지 아웃라인**(`initPost` 550 · `renderFrame` 995 컴포짓 · `postEdge = true` 모바일 포함 · `edgeK` .028 · `normalK` .9 · `creaseK` .010 · 파츠 ID `idOn` · `edgeMaxZ` 22 · `idZFar` 32). 클론은 URP 볼륨 `ColorAdjustments` 노출뿐 · Renderer Feature 0 · 엣지 셰이더 0. → **T147**(§7 `scene3d.js` 줄).
  - 옮기지 않는 것(이유 있음): 인버티드-헐 셸 `applyOutlineTree`(1192~1262 · 포스트 스택 없는 기기 전용 잔재 · `if (postOn || postEdge) return`) · 블룸+비네트(`postOn = !mobile` · 데스크톱 한정 → 모바일 클론 대상 밖 · T147 임자가 결정으로 남긴다) · `rimFlash` 림 셸(T39 결정 96).

## 이 회차가 얻은 규칙
> **«폴리싱 로그» 는 «지금 켜져 있는가» 를 코드 분기에서 다시 물어야 한다.** POLISH.md 의 13 엔트리 중 12 는 정본이 뒤에 `SIMPLE_BG` 로 꺼 버린 자리였고, 진짜 구멍은 로그에 한 줄도 없는 **주석 한 줄**(`scene3d.js` 1250 «후처리 깊이-엣지 아웃라인이 … 2026-08-25 부터 모바일에서도 켜지므로») 에 있었다. 앞선 회차(T39)가 «죽은 갈래를 뺀다» 고 적을 때 **그 옆의 산 갈래**를 같은 눈으로 봐야 한다.

## 다음 회차(11회차)가 열 축
- 8회차가 적은 셋(UI-SPEC · main.js · POLISH)이 다 닫혔다. **새 축은 없다** — 열린 §7 줄(T28·T35·T110·T128·T136·T138·T142·T146·T147 …)이 닫히면 §7 전 줄 ✅ 재검.

## 이 회차의 판정
- 새로 빠진 것: **작업 하나**(T147). T33 은 아직 ✅ 가 아니다.

---

# T33 완주 대조 — **9회차** (2026-09-14 07:3x · 워커 J · sess-0717-10287)

축: **`ref/UI-SPEC.md` 조항 전수**. T28 은 이 문서가 가리키는 **PNG** 를 보지만, 문서의 **조항을 한 줄씩** 클론과 대조한 회차는 없었다.

## ⓝ UI-SPEC 조항 84개 ↔ 클론 — **빠진 것 0 · 경고 1**

문서는 122줄 · 항목 84개(공통 레이아웃 + 화면 1~28 + 승천 + 재화 표)다. 눈에 띄게 «없을 법한» 조항과
문서가 **❗ 로 «클론과 다르다» 고 못 박은 조항**을 골라 코드로 확인했다 — **전부 있다**:

| 조항 | 클론 |
|---|---|
| §1 스킬 슬롯 «자동» 토글 + 원형 버튼 셋 | `SkillBar`(개수는 `Rules.MaxActive`) |
| §1·§27 메인 «!» 버튼 → 플레이어 정보 | `ForgeSheet.cs:110` `ForgeUi.InfoButton` |
| §6~7 ❗ 열쇠는 **입장이 아니라 완료 때** 소모 | `Dungeons.cs:201` 주석·코드 그대로 |
| §6~7 ❗ 리셋 **자정 → 09:00** | `Core/Meta/DailyReset.ResetHour = 9` |
| §18 진행 패스 무료/프리미엄 2열 | `PassPopup`(프리미엄은 데모 토스트) |
| §19 프로필 [파워 랭킹] [클랜 랭킹] | `ProfilePopup.cs:149·151` |
| §25 비교 팝업 **위 = 장착됨 · 아래 = 새 장비**(스펙이 «순서 교체» 라고 못 박은 자리) | `ForgeCraftPopup.cs:63~70` 위 `cur`+리본 · 아래 `new` |
| §28 채팅 하단 ◀ 뒤로 + «메시지 보내기...» 입력바 | `ChatScreen.cs:58·79` |
| 승천 = **라인별 개별** 프레스티지 | `Core/Ascension` `LineAscend` |
| 재화 8종(코인·해머·젬·티켓·알·물약·태엽·도전 티켓) | 전부 있다(도전 티켓은 `LeagueSheet`) |

## ⚠ 경고 하나 — **UI-SPEC 이 정본 코드보다 낡은 자리가 있다**

> §1 «오프라인 보상 버튼: 화면 **왼쪽 상단** 영역 (현재 클론 배치 유지 OK)»

문서는 2026-08-16 **스크린샷 분석**이고, 그 뒤 정본 CSS 가 **왼쪽 아래**로 자리를 잡았다 —
`style.css` 205 `#offline-btn { position:absolute; bottom:.6rem; left:.5rem }`. 클론(T133)은 **정본 CSS 대로 왼쪽 아래**에 세웠고
`screen_main.png` 으로 눈 확인까지 했다. **문서를 근거로 위로 옮기면 정본과 어긋난다.**
일반 규칙으로 적어 둔다: **`ref/UI-SPEC.md` 는 설계 메모이고 정본은 `web/` 의 코드다** — 둘이 다르면 **코드가 이긴다**
(§1 «정본은 kuzuni/wwwww 의 `web/`» · 문서 자신도 머리에 «기존 구현과 다르면 이 문서에 맞춰 고친다» 라고 쓰지만,
그것은 *원작을 만들던 때* 의 지시였고 우리 일은 «지금 원작이 하는 것» 을 옮기는 것이다 · ⚑ 주인 지시 «원작이 지금 하는 것 전부»).

## 다음 회차(10회차)가 열 축
- `ref/POLISH.md` 항목 전수 · 정본 `main.js` 부팅 순서·자동저장 주기 ↔ `Bootstrap`·`Lifecycle`.

## 이 회차의 판정
- 새로 빠진 것: **0**. 축 하나를 닫았고 «문서 ↔ 코드» 우선순위를 못 박았다. T33 은 아직 ✅ 가 아니다.

---

# T33 완주 대조 — **10회차** (2026-09-14 08:4x · 워커 S · sess-0029-41207 · 정정만 · lock 반납)

축: **없다 — 9회차의 «다음 회차가 열 축» 을 정정한다.** 9회차(워커 J · 07:3x)가 남긴 둘(`ref/POLISH.md` 항목 전수 · 정본 `main.js` 부팅 순서·자동저장 주기)은 **이 문서의 ⓞ(main.js · 0건)·ⓟ(POLISH.md · 1건 = T147)** 로 07:0x 이전에 이미 닫혀 있었다(워커 S 9·10회차 · 커밋 53e5acd·ab48d68 — 9회차 절이 그 뒤에 덧붙어 번호가 겹쳤을 뿐이다). 그 둘을 다시 여는 회차는 헛일이다.

## 지금 남은 것 — 열린 §7 줄이 닫히는 것뿐
- 8회차가 적은 축 셋(UI-SPEC ⓝ · main.js ⓞ · POLISH ⓟ)은 전부 닫혔고 **새 축은 없다**. T33 이 ✅ 가 되려면 §7 전 줄 ✅ 여야 한다 — 지금 열린 행(⬜·🔄): T28 · T33 · T35 · T106 · T109 · T122 · T128 · T132 · T135 · T142 · T146 · T147 · T151 · T152 · T153.
- 11회차 = 그 행들이 닫힌 뒤 §7 전 줄 재검(«작업 칸엔 있고 상태 칸엔 없는 열린 작업» 자 `check_final_table` 포함) · 그 전엔 잡지 않는다.

## 이 회차의 판정
- 새로 빠진 것: **0**. 코드 0줄. T33 은 아직 ✅ 가 아니다(열린 §7 줄 15).

---

# T33 완주 대조 — **10회차** (2026-09-14 09:3x · 워커 J · sess-0917-17937)

축: **`ref/POLISH.md` 항목 전수**. 결론은 **«이 축은 완주 대조의 축이 아니다» — 닫는다.**

## ⓞ `ref/POLISH.md`(448줄) — **빠진 것 0 · 축을 닫는다**

두 겹으로 확인했다.

**ⓐ 문서의 성격**: 머리·«영역 6개»·«루프 프로토콜»·«기술 제약»·«통과 기록»·«진행 로그» —
이것은 **원작을 만들던 사람의 작업 프로토콜**이다(«영역당 개선 구현 → 비평가 10점 채점 → 통과하면 커밋 → 다음 영역»).
«원작이 **하는 것**» 의 목록이 아니라 «원작을 **어떻게 다듬을지**» 의 절차다. 9회차의 UI-SPEC 판단(**결정 329**)과 같은 갈래다.

**ⓑ 진행 로그가 기록한 실제 개선이 어디 사는가**: 로그 본문의 주인공은 전부 `scene3d.js` 의
`crackNetwork()`·`crackWorldSpots()`·`lavaGlows`·`accents`(포인트라이트 3기)·`buildProps` 다 — **용암 균열 데칼·발광 블리드·소품 스캐터·중경**.
그런데 **정본 `scene3d.js` 67행이 `SIMPLE_BG: true`** 이고 그 갈래가 `if (this.SIMPLE_BG) return;` 로
구름·소품·중경을 **통째로 건너뛴다**(1443·1479·1730 …). 즉 **원작 자신이 지금 그 길을 꺼 두고 있다.**

→ 우리가 옮길 것은 «원작이 **지금** 하는 것» 이다(⚑ 주인 지시). 꺼진 길은 **T35**(배경 복원 `SIMPLE_BG=false`)의 자리고,
그것은 **결정 212** 가 이미 완주 판정에서 뺐다. 이 회차가 그 결정을 **바깥에서 한 번 더 확인**한 셈이다 —
POLISH.md 를 읽고 «소품·용암 빛이 없다» 를 새 작업으로 등재하면 §1 이 막는 «원작에 없는 것» 을 짓게 된다.

## 이 회차가 얻은 규칙
> **`ref/` 아래 문서 셋의 무게가 다르다.** `screens/shot-*.png` = **정본이 지금 보여 주는 것**(옮긴다) ·
> `UI-SPEC.md` = 설계 메모(코드와 다르면 **코드가 이긴다** · 결정 329) · `POLISH.md` = **원작의 작업 절차**(옮길 대상이 아니다).
> 셋을 «정본» 한 낱말로 뭉치면 회차가 헛돈다.

## 다음 회차(11회차)가 열 축 — 이제 하나 남았다
- 정본 `main.js` **부팅 순서·자동저장 주기** ↔ 클론 `Bootstrap`·`Lifecycle`(T142 가 부팅 단계 표를 세웠으니 그 뒤가 좋다).
- 그 뒤로는 **열 축이 없다** — 11회차가 닫으면 T33 은 «남은 것 = §7 열린 칸이 ✅ 가 되는 것» 뿐이다.

## 이 회차의 판정
- 새로 빠진 것: **0**. 축 하나를 **영구히** 닫았다(결정 333). T33 은 아직 ✅ 가 아니다.

---

# T33 완주 대조 — **11회차** (2026-09-14 10:3x · 워커 J · sess-1017-32332) — **목록의 마지막 축**

축: 정본 `main.js`(272줄) **부팅 차례·격리·틱·자동 저장** ↔ 클론 `Bootstrap`·`SaveIo`·`Lifecycle`·`ForgeHost`.

## ⓟ `main.js` 계약 전수 — **1건**

| 정본 계약 | 클론 |
|---|---|
| 자동 저장 `setInterval(saveGame, 30000)` | `SaveIo.AutosaveIntervalSec = 30f`(주석에 정본 줄 인용) ✅ |
| `visibilitychange` 저장 · `beforeunload` 저장 | `OnApplicationPause`/`OnApplicationFocus`/`OnApplicationQuit` ✅ |
| 논리 틱 100ms · 밀린 틱 **최대 5초분** 따라잡기 · 숨은 동안 건너뜀 | `Lifecycle.BackgroundGapMs = 5000`(정본 191행 인용) · `BattleScene` 이 100ms 누적 ✅ |
| rAF 루프가 **먼저 예약**하고 `update` 를 try/catch(로그는 앞 5회만) | 유니티는 `Update()` 가 프레임을 잡아 구조가 다르다 — 옮길 대상 아님 |
| 부팅 차례: `loadGame` → ensure 7 → `pendingOffline` 먼저 잡기 → `UI.init` → `Scene3D.init` → `Combat.start` → `Dungeons.restoreRun` → **`League.ensure`(전투력 계산 뒤)** → `renderTopBar` → `warmup` → 오프라인 팝업(경과 ≥60초) → `restorePendingCraft` → 자동 제련 이어가기 | 순서 그대로 있다 ✅ |
| **격리**: `UI.init`·`Scene3D.init`·`Combat.start`·`Dungeons.restoreRun`·`warmup`·**`restorePendingCraft`·`startAutoSeq`** 를 각각 `try/catch` | **마지막 둘이 안 감싸였다 → T157** |

### 새로 캔 것 — T157
정본은 그 두 줄 위에 🚨 로 «이 저장소가 이미 **두 번 밟은** 함정» 이라고 적어 뒀다: 대기품 한 칸이 던지면
**논리 틱·rAF·1초 틱·오토포지 안전망·30초 자동 저장·딥링크가 통째로 등록되지 않는다**. 클론은 안쪽 가드
(`IsForgeShaped`)는 옮겼는데 **바깥 격리**를 안 옮겼다 — 정본이 «둘 중 하나만 하면 다음에 다른 필드가 같은 자리에서
터진다» 고 못 박은 바로 그 «둘 중 하나» 다. 파장은 정본보다 작다(코루틴이라 그 코루틴만 멈추고 자동 저장은 따로 선다) —
남는 해는 **자동 제련이 조용히 안 이어지는 것**과 **콘솔 빨강**이다.

## 이 회차가 얻은 규칙
> **주석의 🚨 는 옮길 목록이다.** 정본이 «왜 이렇게 썼는지» 를 적어 둔 자리는 그 저장소가 **실제로 밟은 함정**이고,
> 그 방어는 기능이 아니라서 화면·테스트 어디에도 안 드러난다 — 우리 자들(키프레임·아이콘·문구·세이브 축)이
> 전부 놓친 갈래가 이것이었다.

## 남은 축
- **없다.** 5~11회차가 연 축 아홉(`@keyframes`·세이브 필드·`@media`·`index.html` id·토스트 문구·`::before/::after`·`data-*`·`title=`·UI-SPEC·POLISH·`main.js`)이 전부 닫혔다.
  T33 에 남은 것은 **«§7 열린 칸이 전부 ✅ 가 되는 것»** 뿐이다 — 다음 회차는 새 축을 열 게 아니라 **§7 열린 칸을 세는 회차**다.

## 이 회차의 판정
- 새로 빠진 것: **하나**(T157). T33 은 아직 ✅ 가 아니다.

## ⓠ 12회차 — 축이 아니라 **§7 열린 칸 세기** (2026-09-14 · 워커 K · sess-1132-11195)

§7 표 26줄의 상태 표시를 세면 **✅ 141 · ⬜ 6 · 🔄 7 · ⛔ 3 · ✂ 1** — 열린 칸 **13**.
그 열셋을 «무슨 표시인가» 가 아니라 **«지금 누가 쥐고 있나»** 로 다시 가른다:

| 갈래 | 몇 | 누구 |
|---|---|---|
| 살아 있는 lock — 지금 돌고 있다 | 8 | T147 · T157 · T163 · T109 · T156 · T159 · T28 · T162 |
| **주인 몫** — 워커가 못 연다 | **2** | **T35**(정본 `SIMPLE_BG` off) · **T106**(단색 이모지 폴백 글꼴 · 에셋 승인) |
| 이 회차 | 1 | T33 |
| 차례 기다림 | 1 | T128(`ui_score` 한 덩어리가 T28 뒤) |
| **좌초** — 🔄 인데 lock 이 없다 | **1** | **T132**(11시간 30분 · 아래) |

### 좌초 하나 — T132
🔄 인데 lock 이 없다. 마지막 T132 커밋은 워커 D 의 1회차(2026-09-14 00:00)이고 그 세션(`sess-2352-23474`)은 그 뒤로 없다.
`task_state` 가 🔄 를 «잡지 마라» 로 거르므로 그동안 워커 여럿의 «선점할 것 없음» 목록에 **후보로도 안 올랐다**.
남은 몫은 `chatcam` 한 자리뿐이고 그것을 막던 T131 lock 도 풀렸다 — **행을 ⬜ 로 돌리고 남은 한 자리를 임자 칸에 적었다**(코드 0줄 · 결정 343).

## 이 회차가 얻은 규칙
> **열린 칸은 표시가 아니라 lock 으로 센다.** ⬜/🔄 만 보면 «세션이 죽어 멈춘 행» 이 «임자 있음» 에 숨는다 —
> 표시와 lock 을 맞대야 조용히 멈춘 행이 드러난다. 자가 그 갈래를 안 보는 것은 **T164** 로 등재했다.

## 이 회차의 판정
- 새로 캔 것: **좌초 하나**(T132 · 풀었다) · **자의 구멍 하나**(T164 · 등재).
- **T33 은 아직 ✅ 가 아니다** — 열린 칸 13 중 **둘이 주인 몫**(T35·T106)이라 그 둘이 풀리기 전에는 «§7 전 줄 ✅» 에 닿을 수 없다.

## ⓡ 13회차 — 정본 **모듈 API 전수**(ui.js 밖) · 최상위 상수 전수 (2026-09-14 · 워커 H · sess-1157-17535)

> 1회차가 센 것은 `ui.js` 의 «화면을 여는 함수» 101개뿐이다(ⓒ 절). 규칙을 쥔 모듈 열다섯(`forge`·`combat`·`pets`·`skills`·
> `mounts`·`dungeons`·`techtree`·`ascension`·`shop`·`pass`·`quests`·`league`·`chat`·`util`·`sfx`)의 메서드와, 표를 쥔 파일
> 여섯(`gamedata`·`state`·`balance-data`·`sfx`·`bignum`·`icongen`)의 최상위 상수는 **아무도 세지 않았다**. 이 회차가 셌다.

### ⓐ 규칙 모듈 메서드 **295** + `sfx` 45 = **340** — 짝 없는 것 **0**

- 잣대: 정본 파일에서 `^    이름(` (모듈 객체의 메서드) 를 뽑고, 클론 `Assets/Scripts/**.cs` 의 심볼(메서드·필드·상수)과 PascalCase 로 맞댄다.
- **Core 에 짝 289 · Game 에만 3 · 어디에도 없음 3.**
  - **Game 에만 셋** — `U.josa`(조사 은/는·이/가) · `U.subText` · `U.subRangeText`. 셋 다 **표기 도우미**라 UI 층이 맞다.
    `SubRangeText` 는 수를 `GameDefs.SubstatMin` 에서 읽는다(코드에 박힌 수 0 · §1 지킴).
  - **어디에도 셋 — 전부 해명된다**: `Mounts.migrateInventory` → `MountSave.MigrateInventoryHook`(이름만 다르다 · 세이브 코덱이 JSON 단계에서 한다) ·
    `U.sumSubs` → `Core/Gear/SubsBag`(클래스로 옮겼다) · `U.escapeHtml` → **아래 ⓒ**.
  - `sfx` 의 `_musicTick`·`_applyModeTiming`·`_musicScheduleStep` 중 앞 둘은 **엔진 구조 차이**다 — 정본은 WebAudio 에 120ms look-ahead 로 실시간 예약하고,
    클론은 `MusicSequencer` 가 스텝 하나를 같은 수치로 굽는다(`_musicScheduleStep` 은 그 클래스 머리 주석이 대응을 적어 두었다).

### ⓑ 최상위 상수 **68** — 짝 없는 것 **0**

- `gamedata` 36 · `state` 11 · `balance-data` 10 · `sfx` 5 · `bignum` 4 · `icongen` 2 를 추출 JSON(`StreamingAssets/data/*.json`) 키와 클론 심볼에 맞댔다.
- 기계로 못 찾은 넷은 `bignum` 의 `BIG_MAX_E`·`BIG_ADD_CUTOFF`·`BIG_UNITS`·`BIG_ALPHA` 인데 `Core/BigNum.cs` 에 `MaxE`(1e308)·`AddCutoff`(17)·`Units`·`Alpha` 로 **전부 있다**(접두 `BIG_` 를 뗀 이름).

### ⓒ 새로 캔 것 — 정본 `U.escapeHtml` 이 막던 구멍은 클론에서 **한 줄**이 막고 있다(자로 못 박았다)

- 정본은 플레이어가 고치는 글(`S.nickname` · 채팅 문구·태그 · 리그 이름)을 DOM 에 넣기 전에 `U.escapeHtml`(util.js 27)로 꺾쇠를 죽인다 — `ui.js` 1296·4740·4859·5038·5194·5259·5263 등 **11자리**.
- 클론에 그 함수가 없는 것은 **맞다**(HTML 이 아니다). 그런데 같은 구멍이 TMP 에 있다 — `richText` 가 켜져 있으면 닉네임 `<color=red>홍길동` 이 **글자가 아니라 태그로** 먹혀 이름이 사라지거나 색이 바뀐다.
- 실측: 이 저장소에서 TMP 글자 부품을 만드는 자리는 **딱 하나**(`Assets/Scripts/Game/Ui/UiKit.cs:215`)이고 거기서 `richText = false`(222)를 박는다. 입력칸 둘(`ChatScreen`·`ProfilePopup` 의 `TMP_InputField`)도 그 공장이 만든 글자를 쓴다. **그래서 지금은 구멍이 없다.**
- 다만 **아무도 그것을 지키지 않았다** — 누가 한 라벨에서 색을 갈아 끼우려고 `richText` 를 켜면 그날로 뚫린다. `tools/check_richtext.py`(이 회차 · rc 1 로 막는다)를 세웠다: ⓐ 공장이 끄고 있는가 ⓑ 공장 밖에서 TMP 를 만드는가 ⓒ 어디서든 `richText = true` 로 되켜는가. 셋 다 사본 실험으로 빨강을 확인했다(`--self-test` 7칸).

### 덧 — 정본이 하나로 쥔 표기를 클론은 둘로 갈라 쥔다(지금은 같다)

`U.subText` 하나가 장비·펫·탈것 서브스탯 줄을 다 그리는데(ui.js 3224·4010·5184·5689), 클론은 `ForgeUi.SubText`(장비)와 `PetSkillStyle.SubText`(펫·탈것 · 표 문구 `"{0}{1}% {2}"`)로 **둘**이다. 지금 두 출력은 글자까지 같다 — 그러나 한쪽만 고치면 장비와 펫의 같은 줄이 갈린다. 새 작업으로 등재하지 않는다(원작에 없는 것을 더하는 것이 아니고, 두 파일 다 남의 lock 자리다) — **그 둘 중 하나를 여는 회차가 합치면 된다**는 줄로 남긴다.

## 이 회차의 판정
- 새로 센 축 둘(모듈 메서드 340 · 최상위 상수 68) — **빠진 것 0**. ⓒ 절 하나를 새로 캐서 자로 막았다.
- **T33 은 여전히 ✅ 가 아니다** — 12회차가 적은 그대로, 열린 칸의 둘(T35·T106)이 주인 몫이다.

## ⓢ 14회차 — 정본 **클릭 진입점 전수**(onclick 116종) (2026-09-14 · 워커 H · sess-1257-2477)

> 13회차가 «모듈이 무엇을 할 줄 아는가»(메서드 340)를 셌다면, 이 회차는 «플레이어가 무엇을 누를 수 있는가» 를 센다. 둘은 다른 축이다 —
> 기능이 다 있어도 **들어가는 문이 없으면** 그 기능은 화면에 없는 것이다. 촬영 대조(ⓑ)는 픽셀을 보지 눌러지는지는 안 본다.

- 잣대: 정본 `ui.js` + `index.html` 의 `onclick="UI.xxx"` 를 걷으면 **116종**(호출 자리로는 173). 이름으로 클론과 맞대는 것은 **쓸모가 없다**(결정 494 — 클론은 `closeAscension` 같은 이름을 안 쓰고 `Popups.Hide`·람다로 닫는다). 그래서 «클론 어디에도(코드·주석·표) 그 이름이 한 번도 안 나오는 것» 26종을 뽑아 **하나씩 기능으로** 확인했다.
- **26종 중 25종은 있다**: `close*` 열셋은 공용 ✕(`PopupKit.XButton`→`Popups.Hide`) 하나로 모인 것 · `onSummonAgain`→«다시 소환»(`SkillSummonResult`) · `onToggleHammerDd`→`ForgeAutoPopup` af-spinner · `onBuyGems`→`ShopSheet:143`(데모 토스트) · `onPremiumPass`→`PassPopup:74`(데모 토스트) · `onToggleSkill`→`SkillPanel:445`(같은 토스트 문구) · `switchProfileView`→`ProfilePopup` 프로필/설정 · `onToggleAvatarPick`→`ProfilePopup` 아바타 고르기 · `onEditNickname`→`ProfilePopup.OpenNickname` · `onDebug*` 여섯의 **동작**은 `Ui/DebugPanel.cs` 에 다 있다.
- **하나가 없다 — 들어가는 문(T169 등재)**: 정본 하단 네비는 여섯 칸이고 여섯째가 🐞 디버그다(`index.html` 165). `main.js` 127~131 이 🚨 로 «기본 노출이다 · 되돌리지 말 것 — 다시 숨기려면 사용자 지시가 한 번 더 있어야 한다» 고 못 박았고 **숨기는 코드는 정본에 없다**. 클론은 `catalog.json` `tabs` 가 다섯 줄이라 그 칸이 없다 — 패널은 다 옮겨 놓고 문만 없앤 꼴이다(`MetaHost.OpenDebug()` 로만 열려 촬영 봇만 쓴다).
- 그 갈래는 **결정 52**(T22)가 «T18 카탈로그가 5개로 못 박았고 우리 자가 5를 단언하니 숨긴다» 로 고른 것이다. 근거가 **클론 쪽 제약**이라 §1 «옮기는 것은 원작이 지금 하는 것» 과 결정 336 «정본 주석의 🚨 는 옮길 목록이다» 를 못 이긴다 → **T169 로 등재하고 결정 52 를 되돌린다**(결정 497). 주인이 «디버그는 빼라» 하면 그 한 마디가 정본 주석이 요구하는 «사용자 지시» 이므로 그때 ✂.

## 이 회차의 판정
- 새 축(클릭 진입점 116종) — **빠진 것 하나**(T169 등재). 나머지 115종은 자리를 확인했다.
- T33 은 여전히 ✅ 가 아니다(T35·T106 주인 몫 + 이제 T169).

## ⓣ 15회차 — 원작 밖 축: **하니스가 못 잡는 갈래**(2026-09-14 · 워커 H · sess-1357-11882)

- parity 의 «(원작 밖 · 도구·게이트·CI) 병렬 운영을 지키는 자들» 줄에 하나를 보탠다: `tools/check_unity_messages.py` — MonoBehaviour 안에서 유니티 메시지 이름(`Start`·`Update`·`Awake`…)을 다른 뜻으로 쓴 메서드를 막는다(지금 81개 중 어긋남 0).
- 오늘 main 의 유니티 잡이 일곱 런 연속 «두 모드 0개» 로 죽은 뿌리 둘은 **둘 다 `dotnet build` 0 오류**였다: T171(`BattlePreview.Start(RectTransform)` → 콘솔 에러 폭주 → SIGSEGV) · T106 1회차(스텁에만 있던 TMP 서명 · CS1503). 앞의 것을 자로 옮겼고, **고치기 전 실물 파일로 사본 실험해 rc 1 을 확인**했다.
- 남은 절반(스텁 ↔ 실제 유니티/TMP API 서명)은 이 자가 못 본다 — 다음 회차의 자리로 적어 둔다(결정 502).

## ⓤ 16회차 — 정본 **그라디언트 176 선언** 전수 (2026-09-14 · 워커 H · sess-1457-15116)

- 잣대: `style.css` 에서 주석을 걷고 `linear/radial/conic-gradient` 선언을 센다 — **176 선언 · 선택자 137**(`background` 83 · `background-image` 60 · `mask-image` 30 · `--af-pat` 3). 같은 방법의 앞선 축 둘(T159 `clip-path` · T168 `letter-spacing`)이 실제 결함을 무더기로 캤고 T173(보스 경고 방사형)이 «이 축에 더 있다» 는 신호였다.
- **ⓐ 표면 겹이 통째로 안 옮겨졌다 → T178**: 정본은 거의 모든 면에 같은 문법을 쓴다 — 위쪽 **1px 흰 줄** + **45° 미세 빗금**(`repeating-linear-gradient(45deg, … 0 2px, … 2px 9px)`) + **세로 명암**. `.btn.btn`(4) · `#tabbar`(5) · `.modal-card`(7) · `.league-row`(5) · `.qst-row`(3) · `.pet-tile .tile-face`(2) · `.equip-cell`(2)… 클론은 `UiKit.Panel` = **색 한 칸짜리 `Image`**(UiKit.cs 104~111)이고 공용 표면 파일들에서 그라디언트 낱말이 **0** 이다. 굽는 도구는 이미 여섯 파일에 있다(`AgePattern`·`CraftFxPoly`·`UiShapes`·`BootLoading`·`RewardBurst`·`BattleOverlay`) — **없어서가 아니라 공용 면에 안 쓴 것**.
- **ⓑ 가장 큰 무리를 열었더니 다른 결함이 나왔다 → T179**: 선택자별 최대 무리는 `.sr-*`(소환 결과 · 40 선언 넘음)인데, 열어 보니 «겹이 단색» 이 아니라 **겹 자체가 없다** — `.sr-canopy`(아치 + 빛발 셋) · `.sr-rays` · `.sr-reflect`(그리드를 뒤집어 바닥에 깐다) · `.sr-stars` 가 클론에 하나도 없고 `sr-floor` 원판 한 장뿐이다.
- **죽은 CSS 인지 먼저 봤다**(T135 ⛔ 의 교훈): 넷 다 `ui.js` 에 실제 렌더 줄이 있다(351 · 491 · 773~781) — 옮길 것이 맞다.
- **이 회차가 안 한 것**: 137 선택자를 하나씩 클론 자리에 짝지어 주는 일(T178 첫 회차가 자를 세우며 한다). 여기까지는 «축이 있다 · 크기가 이만하다 · 공용 면은 0 이다».

## 이 회차의 판정
- 새 축(그라디언트 176) — **빠진 것 둘**(T178 · T179 등재 · 결정 507).
- T33 은 여전히 ✅ 가 아니다(T35·T106 주인 몫 + T169·T178·T179).

# T33 완주 대조 — **20회차** (2026-09-14 20:2x · 워커 B · sess-1920-15773)

19회차가 «다음 회차가 열 축» 으로 세어만 둔 둘을 열었다 — 파서는 19회차 것과 같은 결(주석 걷기 · `@keyframes`/`@media` 중첩 추적)이고 수도 같다(`border-radius` **223** · 정적 `transform` **87** · 규칙 7,165).

## ⓡ `border-radius` 223 ↔ 클론 둥근 모서리 — **표값 자리 0건 · 리터럴 자리 10건 어긋남 · 판 없음 후보 3**
- 값 분포: `50%` 52(원) · rem 값 150(`.5rem` 29 · `1rem` 22 · `.6rem` 19 · `.7rem` 17 · `.4rem` 15 · `.55rem` 11 …) · `0`/`inherit` 12 · px 7(스크롤바 자국 5 · `.pip.boss` 2px) · `calc(var(--app-w) * .0094)` 2.
- 클론의 길: `UiKit.Rounded`(9-슬라이스 `UiShapes.Rounded` 반지름 24 · `pixelsPerUnitMultiplier = 24 / radiusPx` → 화면 반지름 = 넘긴 캔버스 px) **95곳** + `UiKit.Circle` **28곳**(정본 `50%` 자리). rem 환산은 `rem_h`(1rem = 16/844 H = 36.4 캔버스 px) 하나다 — 길 자체는 맞다.
- **표값 자리(0건)**: `PetSkillUi.json` `*_r_rem` 36키 · `catalog.json` `*_radius(_rem)`·`btn_r`·`card_r`·`pass_cell_r` 13키 · `LootFeedUi`·`WaypointsUi`·`PlayerInfoUi`·`AgePatternUi` — 키마다 정본 줄과 견줬다(subtab .42 · sk-shard .45 · ribbon .4 · tile .5 · equipped .6 · mini .4 · btn .7 · x5 1 · gauge .5 · modal 1.1 · petd .55/.4 · skd 1 · rate .5/.4 · petup .7/.6/.6/.55/.6 · toast 2 · sr-* 8 · sb 1 · dg-banner(sheet) .8 · dgd .6/.6 · dgc .7 · tt .35 · tb .8 · idet(tech) .8 · tech-prog .5 · cur-pill 1 · asc .5 · card 1 · btn .55 · pass-cell .6 · loot 1 · waypoint .4 · preview .5) — **하나도 안 어긋난다**.
- **알약 동치(결정 543)**: 정본이 `1rem`/`2rem`/`.48rem` 처럼 «높이의 반 이상» 을 준 자리(토스트 2rem/높이≈1.9rem · 퀘스트 바 .48/.95 · 토글 1/1.35 · 자동 제련 토글 1/1.27 · 하위 행 1 · 채팅 뱃지 .6/≈.65 · 상점 보상 알약 1/1.32 · 패스 라벨 1 · 통화 알약 1 · 확률 칩 1) 는 클론의 `h * 0.5`(알약) 와 **같은 그림**이다 — ✓ 로 센다(10곳).
- **리터럴 자리 — 어긋남 10**(정본 rem → 클론 · 파일:줄):

  | 정본 | 값 | 클론 | 값 |
  |---|---|---|---|
  | `.chat-bubble` 3371 | .42rem | `ChatScreen.cs:234` | `rem * 0.6` |
  | `.chat-share-card` 3389 | **0**(각진 카드 · 주석 «원본 카드는 모서리가 각져 있어») | `ChatScreen.cs:216` | `rem * 0.6` |
  | `.chat-input-bar input` 3449 | .3rem | `ChatScreen.cs:71` | `bh * 0.3` = .52rem |
  | `.chat-input-bar .btn.round` 3270 | .35rem | `ChatScreen.cs:61` | `bh * 0.3` = .52rem |
  | `.chat-share-side .icon-circle.sm` 3401 | .28rem | `ChatScreen.cs:251` | `av * 0.5` = 1.31rem(거의 원) |
  | `.idet-subs` 3703(`#forge-item-modal` 3722 는 반지름을 안 덮는다) | .8rem | `ForgeInfoPopup.cs:318` | `rem * 0.6` |
  | `.upg-progress` 694 | .55rem | `ForgeInfoPopup.cs:110·112` | `rem * 0.5` |
  | `.af-spinner` 4783 | .45rem | `ForgeAutoPopup.cs:101` | `rem * 0.3` |
  | `.af-dd-list` 4791 | .45rem | `ForgeAutoPopup.cs:129` | `rem * 0.3` |
  | `.btn.back-btn` 5189(`.back-btn` 5178 의 .6rem 을 덮는다) | calc(--app-w × .0094) = .28rem | `Popups.cs:471` | `Rem * 0.45`(= `.league-back-btn` 2383 값) |

- **판(면) 자체가 없는 후보 3**(정본은 배경+반지름이 있는 판인데 클론에 그 이름의 둥근 면이 없다 · 셋 다 `ui.js` 에 렌더 줄이 있다 = 죽은 CSS 가 아니다): `.substat-row`(793 · #22272e · .4rem · ui.js 2232) · `.mat-chip`(806 · #22272e + 테 · .5rem · ui.js 5767) · `.tech-tree-node-time`(2209 · pp-ink 위 초록 글자 · .6rem · ui.js 5420·5422). 클론 `GearDetailPopup.cs` 에는 `Rounded/Panel` 호출이 0 이고 `TechPanel.cs` 에 그 라벨의 둥근 면이 없다 — **화면 PNG 로 가른 뒤** 수리(T345 ⓒ).
- **죽은 CSS(안 센다)**: `.stat-grid`·`.hatch-slot`·`.egg-chip` 은 `ui.js`·`index.html` 에 자취 0.
- ✓ 로 확인한 리터럴 자리(참고): 프로필 팝업 9(1/.5/.4/.4/.38/.3/.5) · 리그 8(.6/.5/.3/.5/.7/.7/.4 아바타 둘) · 상점 5(.9/.8/.3 + 알약 둘) · 패스 4 · 제작 비교 3(`.cmp-card` .8 · `.cmp-lower` .7 · `.swc-col` .55) · 대장간 시트 4(장비 칸 `size × .16` ≈ .7rem · 덱 .7 · 태그 알약) · 채팅 아바타 .4 · 토스트 알약 · 설정 토글·행동 .5 · 던전 상세 hero(`.dgd-card .dg-detail-hero` 5280 이 0 으로 덮는다 → 클론 `Box` 그대로 ✓).

## ⓢ 정적 `transform` 87 ↔ 클론 — **빠진 것 0 · 세 rotate 중 둘은 서 있고 하나는 T135 몫**
- 76 은 `translate*`(가운데 맞춤 `translate(-50%,-50%)`·`translateX(-50%)` · 절대 배치 오프셋 · `.tech-tree-col`·`.tech-tier-tag`·`.equipped-label` 의 calc 오프셋) — **모양이 아니라 자리**다. 클론은 앵커·피벗으로 같은 자리를 잡는다(§7 ui.js 줄 · T28 대조표가 이미 본다).
- 8 은 연출의 **시작 상태**(`.sr-wipe scale(.55)` · `.sr-cell scale(.35)` · `.sr-relight/.sr-tierflash/.sr-beam scale(.3/.24/.2)` · `.rw-glow/.rw-ring/.rw-pop scale(.2/.3/.25)` · `#skill-cutin scale(0)` · `.bw-banner scaleY(0)` · `#offline-btn:active scale(.94)` · `.ob-zzz` 셋) — 임자가 있다(T179·T334 소환 결과 · 리워드 버스트 · T335 ⓓ 눌림 · 오프라인 버튼 `OfflineButton.cs`). `.sr-reflect scaleY(-1.22)` 는 T179 3회차가 세웠다(`SummonFx`).
- **rotate 3**: `.pip.boss rotate(45deg)` 195 → `Hud.cs:311` ✓ · `.cmp-card.new::after rotate(15deg)`(1834 · `shinesweep` 띠) → 클론 자취 0 이지만 **T135 ⓒ 가 이미 쥔 연출**(5회차 등재 · `newpulse`·`shinesweep`) · `.rw-pop … rotate(0deg)` 7514 는 키프레임 시작값(`RewardBurst.cs:361·442` 가 각을 돈다) ✓. `skew` 0.
- 축을 **닫는다** — 새로 등재할 것 없음.

## 이 회차의 판정
- ⓡ → **T345 등재**(어긋난 리터럴 10 + 판 없음 후보 3 · 자 `tools/check_border_radius.py` 로 못 박는다 · T331 과 같은 갈래). ⓢ 닫힘.
- T33 은 여전히 ✅ 가 아니다(T35·T106 주인 몫 + 열린 §7 칸). 다음 회차가 열 축(세어만 뒀다): `opacity` 정적 선언 · `z-index` 층 순서(정본 z 5·6·25 …) · `overflow`(스크롤 영역) — 파서는 이 회차 것을 그대로 쓰면 된다.

## ⓥ 18회차 — 정본 **`z-index` 112 선언**(겹 순서) 전수 (2026-09-14 · 워커 H · sess-2057-19689)

- 남은 CSS 속성 축을 세어 고른 것이다 — `box-shadow`(T331) · `text-shadow`(T333) · `border-radius`(T345) · `filter`(T342) 는 이미 임자가 붙었고, 아직 아무도 안 센 것 중 가장 큰 것이 **`z-index` 112 선언 · 선택자 111** 이었다(그 다음은 `transform` 404 · `opacity` 370 인데 둘은 «자리마다 다른 값» 이라 축 하나로 못 묶는다).
- **정본 겹 순서(최종값 · 같은 특이도는 뒤 규칙이 이긴다)**: `#reward-burst` 70 · `#summon-result-modal` 60 · `#detail-modal`·`#forge-item-modal` 42 · `#offline`·`#chat`·`#profile`·`#player-info`·`#pass`·`#pet-upgrade`·`#dungeon-detail`·`#forge-info` **40** · `#autoforge` 31 · **`#tabbar` 30** · `#toasts` 30 · `#gear-detail`·`#mount-upgrade` 22 · `#equip-swap-fx` 21 · `.modal` 20.
- **클론은 이 갈래를 이미 알고 있었다**: `PopupLayer.Create` 가 `modals`(탭바 **아래** 형제)와 `modals-over`(위)를 세우고 `Show(name, tab, aboveTabBar, dim)` 이 고른다 · 토스트는 «정본 z 19 < .modal 20» 주석과 함께 모달 아래. `RewardBurst` 는 «앱 상자 마지막 형제 = 정본 z 70» 으로 이미 맞다.
- **빠진 것 다섯 → T346**: 정본 40 인데 기본값(`false`)으로 열려 탭바 **아래**로 가는 팝업 — offline · chat · profile · player-info · pass. 맞게 가 있는 셋은 `ForgeInfo`(40·42) · `ForgeAuto`(31) · `GearDetail`(22)이고, 탭이 여는 팝업(shop·quest·pvp)은 `.modal` 20 이라 아래가 **맞다**(그래야 그 탭이 빨간 ✕ 로 남는다).
- **이 회차가 못 본 것(정직하게)**: 제 층을 따로 세우는 셋 — 펫/탈것 상세·던전 팝업·소환 결과(정본 60)는 `PopupLayer` 를 안 거쳐 형제 자리를 따로 재야 한다. 그리고 **PNG 눈 확인을 못 했다** — 이 회차의 `screens` 머리가 런 494(두 모드 빨강)라 UI 촬영이 28개 파일뿐이었다. 그래서 T346 의 판정에 그 눈 확인을 넣어 두었다.

## 이 회차의 판정
- 새 축(`z-index` 112) — **빠진 것 다섯**(T346 등재 · 결정 546).
- T33 은 여전히 ✅ 가 아니다(T35 주인 몫 + 새로 연 T346).

## ⓦ 21회차 — 정본 **`font-weight` 233 선언 + `tabular-nums` 6 자리** 전수 (2026-09-14 · 워커 J · sess-2218-22711)

선언 분포(`css/style.css`): **900 ×123 · 800 ×63 · 700 ×41 · 600 ×1 · 500 ×1 · 400 ×4** = 233(+ `index.html`·`ui.js` 인라인 2 = 235).

### ⓐ 먼저 **가짜 경보를 껐다** — 900/800/700 은 화면에서 **한 단**이다
첫눈에는 «정본은 네 단인데 클론은 굵기 능력이 0» 으로 보였다. 정본 CSS 8624~8631 이 **스스로 반박한다**:

> 🚨 이 앱은 웹폰트 금지(CDN 제약)라 폰트가 **가변축이 없다** — 600·650 을 적어 봐야 **700 으로 반올림돼 아무것도 안 바뀐다**. 실제로 한 단 내려가려면 **500** 이어야 한다(눈이 아니라 폰트 스택의 성질이다: 'Segoe UI'/'Malgun Gothic' 부재 시 폴백 sans 는 **regular/bold 두 축뿐**).

즉 정본 화면에 실제로 서는 굵기는 **regular ↔ bold 둘**이고, 228 선언(700·800·900)은 전부 **같은 bold** 로 그려진다. 그러므로 **클론의 `FontStyles.Bold` on/off 두 단이 옳은 이식**이다 — 굵기 단을 더 만들거나 Black 자면을 새로 들이는 것은 **원작에 없는 것**(§1 금지)이고 에셋 금지에도 걸린다. **이 축에서 «없는 능력» 은 없다.**

### ⓑ 그러면 진짜 물음: **bold 를 정본과 같은 자리에 주는가** — 여기서 부호가 뒤집혀 있다
정본에서 **regular 로 남는 자리는 딱 여덟**이다: 400 넷(`.muted` 657 · `.btn small` 667 · `.forge-age-section .age-tag small` 724 · `.egg-chip small` 1661) + 500 넷(`.rates-tip`·`.pass-desc`·`.forge-item-cell small`·`.league-server` 8633). **나머지 225 선언은 전부 bold** 다.

클론은 반대로 서 있다:
- `UiKit.Text`(§1 이 «글자는 반드시 여기서 만든다» 로 못 박은 공장)는 font·size·color·align·wrap 을 주고 **굵기는 안 준다** → 기본이 **regular**.
- 굵기는 **27개 파일 120 자리**에서 `fontStyle = FontStyles.Bold` 를 **손으로 박아** 준다.
- 곧 정본 = «기본 bold + 예외 여덟» · 클론 = «기본 regular + 예외 120». **기본값이 서로 반대**다. 새 화면을 만드는 사람이 그 한 줄을 잊으면 **조용히 regular** 가 되고, 아무 자도 안 운다(글자 하한은 크기만 본다).

### ⓒ `font-variant-numeric: tabular-nums` 6 선택자 — 클론에 **0**
정본 8635~8638 이 `.league-score`·`.rate-bar`·`.rates-prog span`·`.qst-reward`·`.forge-item-cell small`·`.pass-cell span:not(.pass-badge)` 에 등폭 숫자를 주고, 까닭도 적어 뒀다(«세로로 열을 이루는 숫자만 등폭으로 — **행마다 좌우로 흔들리던 자리**»). 클론에는 `tabular`·`monoSpacing` 사용이 **0 건**이다. TMP 에 대응이 없는 것이 아니다 — `TMP_Text.monoSpacing` 이 그 노릇이다(⚠ `tools/dotnet/Stubs` 에 그 멤버가 없어 §1 «패키지 타입» 규칙대로 **스텁도 같이 보강**해야 한다 · T174).

### 이 회차의 판정
- **등재: T352**(ⓑ 기본값 뒤집기 + ⓒ 등폭 숫자). 이 축에서 나온 결함은 그 하나다.
- **등재 안 함**: 굵기 단 추가·Black 자면 도입 — 정본이 그리지 않는 것이다(위 ⓐ).
- 이 회차가 남긴 규칙: **«정본 CSS 의 선언 수» 를 세기 전에 그 선언이 실제로 그려지는지 정본 주석에서 확인한다** — 이 축은 235 중 228 이 «적혀 있지만 한 단» 이었다.

## ⓧ 22회차 — 정본 **`aspect-ratio` 21 + `object-fit` 9** 전수 (2026-09-15 · 워커 J · sess-0218-10672)

«그림·상자 모양» 축. 클론에 `AspectRatioFitter` 가 **0 건**이라 첫눈엔 «비율을 지키는 장치가 없다» 로 보였지만, 클론은 **상자 크기를 계산해서** 비율을 낸다 — 자리마다 실제로 확인했다.

### ⓐ `object-fit` 9 — **결함 0**(그중 하나는 죽은 CSS)
- `contain` **8** ↔ 클론 `UiKit.Icon` 의 기본이 `preserveAspect = true`(= contain). 자리: `.fl-face img`(756) · `.auto-drop-card .adc-img`(1081) · `.craft-batch .cb-card .adc-img`(1147) · `.cmp-img`(1856) · `.equip-cell .cell-img`(1875) · `.idet-icon img`(3696) · `.sr-ico > .mt-face > img`(6535) · `.mt-face.has-thumb > img`(7603).
- `cover` **1**(`.pinfo-preview.shot img` 5557)은 **죽은 CSS** 다 — `ui.js` 5147·5153 은 `.pinfo-preview scene`(살아 있는 캔버스) 아니면 민 `.pinfo-preview` 만 낸다. 정본 주석도 «정지 스냅샷 `<img>` 를 대체하는 살아 있는 캔버스» 라 적어 뒀다. UGUI 에 cover 대응이 없다는 이유로 등재할 뻔한 자리인데 **정본이 안 그린다**(T135 ⛔ 의 교훈).
- 클론에서 `preserveAspect = false`(= 늘림)인 **12 자리를 전부 열어 봤다**: 그림자 판(`UiShadow`) · 장착 판(`SkillPanel` `sk-eqplate`) · 글로/헤일로/비네트/소환진/그림자(`SkillSummonResult` 6) · 부화 원뿔(`PetHatchCone`) · 던전 배너 그림(`DungeonSheet`·`DungeonDetailPopup`). 앞 열은 **연출 겹**이고, 던전 배너는 정본에 `object-fit` 이 **없다** — 늘림이 틀렸다는 근거가 없다.

### ⓑ `aspect-ratio` 21 — **결함 0**
- **정사각(`1`) 13 자리**는 클론이 `UiKit.Place(rt, x, y, t, t)` 꼴로 **구성상 정사각**이다(실측: `ForgeUi` 248 · `ForgeInfoPopup` 310 · `MountSheet` 255 · `PetPanel` 528 …).
- `.modal-card.sheet .dg-banner` **3.45/1** ↔ 클론 `DungeonSheet` 163~164 `bh = bw / UiKit.L("dg_banner_aspect")` · 카탈로그 `dg_banner_aspect = **3.45**`.
- `.sr-canopy` 족 **2.5 · 3 · 3.9** ↔ `SummonFxUi.json` 의 `canopy_aspect 2.5` · `canopy_one_aspect 3.0` · `canopy_compact_aspect 3.9`.
- `.sr-floor` **2.6(one) · 2.5(그 밖)** ↔ `SkillSummonResult` 346 `fw / (one ? 2.6f : 2.5f)`.
- 기본 선언 `.sr-canopy 3.1`·`.sr-floor 3.1` 은 **정본에서도 안 쓰인다** — `.one` 과 `:not(.one)` 두 덮어쓰기가 늘 하나는 맞아 기본까지 안 내려간다.

### 남긴 것(등재 아님 · 임자 몫)
`.sr-floor` 의 두 비율 **2.6·2.5 가 표가 아니라 코드에 박혀 있다**(`SkillSummonResult` 346). 같은 파일의 `.sr-canopy` 는 표(`SummonFxUi.json`)로 갔으므로 **한 파일 안에서 갈래가 둘**이다. 그 파일은 지금 **T334 산 lock** 이라 안 건드렸다 — 임자가 마무리할 때 같이 표로 옮기면 된다.

### 이 회차의 판정
- **새 작업 0**. 이 축은 이미 옮겨져 있다.
- 21회차(`font-weight`)에 이어 **두 축 연속 «결함 0»** 이다. 남은 CSS 축 중 큰 것: `pointer-events` 74 · `cursor` 51 · `white-space` 41 · `background-position` 22 · `background-size` 15 — 다음 회차가 고른다.

# T33 완주 대조 — **24회차** (2026-09-15 02:3x · 워커 N · sess-0524-8791) — 축: `pointer-events` 68 선언

22회차가 «남은 큰 축» 으로 센 것 중 첫째. 정본 `style.css`(주석 걷고 `;` 로 쪼개 셈): **`pointer-events: none` 67 · `auto` 1**(`.craft-batch` 790 — none 층 안에서 다시 눌리게 되돌린 자리).

## 정본이 «눌리지 않게» 둔 것 — 갈래 넷
- ① **전면 연출 층 13**: `#game-area::after`(81) · `#fx-layer`(90) · `#boss-warning`(223) · `#dmg-flash`(295) · `#loot-feed`(318) · `.float-dmg`(328) · `#skill-cutin`(1333) · `#skill-flash`(1354) · `#toasts, #toasts-combat`(1357) · `.anvil-fx`(929) · `#equip-swap-fx`(5033) · `#coin-burst`(5110) · `#reward-burst`(5156).
- ② **소환 결과 연출 31**: `.sr-streaks`·`.sr-floor`·`.sr-canopy`·`.sr-rays`·`.sr-halo`·`.sr-stars`·`.sr-motes`·`.sr-dust`·`.sr-near`·`.sr-charge`·`.sr-shock`·`.sr-flash`·`.sr-wipe`·`.sr-relights`·`.sr-tierbreaks`·`.sr-ghost`·`.sr-spark`·`.sr-ray`·`.sr-beam`·`.sr-idle`·`.sr-reflect` + `::before/::after` 겹 10(3898~4816).
- ③ **장식 겹(가상 요소) 15**: `.fl-face[data-asc]::after`(539) · `.equip-cell[data-age]::before`(620) · `.auto-drop-card.craft-reveal::after`(771) · `.cmp-card.new::after`(1281) · `.dg-banner::before`(1391) · `.pass-cell::before/::after`(1962) · `.sk-orb.equipped::after`(2795) · `#panel-skills .summon-bar::before`(2867) · `.af-age-bar`·`.fi-age-bar` `::before/::after`(3300~3534) · 게이지 `::after` 넷(5980).
- ④ **버튼 안·위의 겹 8**: `.ob-zzz`(141) · `.skill-btn .sk-cd`(415 쿨타임 막) · `.auto-drop-card`(738) · `.equip-cell .cell-img`(1311) · `.tech-tree-links`(1522) · `.sk-eqplate`(2802) · `.hatch-cone`(3072) · **`.skill-btn.empty`(3836 — 빈 스킬 칸은 버튼 자체가 안 눌린다)**.

## 클론의 길 — 정책이 정본과 같다
- 장식 그림을 만드는 공장이 전부 **`raycastTarget = false`** 로 만든다: `UiKit.Panel`(107·109 — `Rounded`·`Circle`·`Line` 이 이것을 거친다) · `UiKit.Icon`(200·205) · `ClipShape`(129) · `SurfaceArt`(265) · `PetSkillKit`(350) · 글자 `UiKit.Text`(`raycastTarget = false`). 즉 **CSS 의 «기본 auto ↔ 정본이 none 으로 끈 67»** 이 클론에서는 **«기본 false ↔ 누르는 면만 true»** 로 뒤집혀 서 있다 — 방향이 반대라서 같은 결과다(정본이 none 을 적어야 했던 자리가 클론에서는 아무것도 안 적어도 된다).
- `true` 로 켜는 자리는 셋뿐이고 전부 «눌려야 하는 면» 이다: `UiKit.Button` 의 hit(346·348) · `Popups` 의 카드 hit(243·245)·모달 딤 mask(339·341 — 정본 `.modal` 딤도 눌러 닫는 면) · `PetSkillKit` 의 뷰 hit(315·317) · `TechPanel`·`TechPopups` 의 스크롤 뷰포트 hit(240·287). `AddComponent<Image>` 를 직접 부르면서 `raycastTarget` 을 안 적은 자리는 그 둘(뷰포트 hit)뿐이고 둘 다 켜야 하는 면이다.
- ① 전면 연출 층: `BattleOverlay`(끔 12 자리) · `LootFeed`(`CanvasGroup.blocksRaycasts = false`) · `CoinBurst`(2) · `RewardBurst`(9) · `EquipSwapFx`(4 + 그룹) · `SummonFx`(8 + 그룹 둘) · `DungeonClearFx`(그룹) · `ForgeSheet` 망치 그룹(`.anvil-fx`) — 전부 끈다. 토스트는 `UiKit.Rounded`+`IconTextRow` 라 공장에서 꺼진다.
- ④ `.skill-btn.empty`: `SkillBar` 117 이 빈 칸을 **`UiKit.Box`(버튼 없음)** 로 세운다 ✓. `.sk-cd`·`.sk-eqplate`·`.cell-img` 는 버튼 **안의** 자식이라 유니티에서는 켜져 있어도 클릭이 부모 `Button` 으로 올라간다(`ExecuteEvents` 버블) — 정본이 none 을 적어야 했던 까닭(DOM 은 자식이 이벤트를 먹는다)이 여기선 없다. 그래도 공장이 꺼 둔다.
- 결함 후보로 본 것 — **0**. ①②③ 의 짝을 파일마다 확인했고 켜진 면이 하나도 없다.

## 이 축을 못박는 자 — `PointerPassTests`(PlayMode · 3)
정책은 코드가 지키지만 «연출이 떠 있는 동안 아래 버튼이 눌리는가» 는 **레이캐스트로만** 답이 난다. 앱 캔버스의 `GraphicRaycaster` 로 탭바 첫 버튼 중심에 쏜 결과의 **맨 위가 그 버튼**인가를 평시 · 보스 경고 한가운데 프레임(`BossWarning(2.0)`+`Tick(0.5)` — BossWarnArtTests 와 같은 프레임)에서 재고, 경고 층(`BattleOverlay.Layer`)과 토스트에는 **한 건도 안 걸리는가** 를 잰다(화면 가운데 한 점 더). 판정은 다음 유니티 런 — 빨강이면 그 자리가 정본 `pointer-events: none` 을 어긴 것이고 임자는 그 파일의 lock 임자(없으면 §0-6 누구든). **판정(03:2x · 런 573 `d15658a` · 유니티 전체 초록): 3/3 PASS** — 정책이 화면에서도 선다. 이 축을 닫는다.

## 이 회차의 판정
- **새 작업 0**. 세 축 연속 «결함 0»(21·22·24). 다만 이 축은 21·22 와 달리 «정책이 코드에 서 있다» 를 자로 남겼다.
- 남은 CSS 축(22회차 목록에서 이것을 뺀 것): `cursor` 51(모바일 — 뜻 없음 · 닫아도 된다) · `white-space` 41(nowrap 40 · normal 1 — 클론 공장 기본이 `NoWrap` 이라 부호가 같다 · 접히는 자리 19곳은 T351 이 잰다) · `background-position` 22 · `background-size` 15 · `mix-blend-mode` 18(전부 `screen` — `CraftFxPoly.Screen` 재질 한 길 · 18 자리 대조는 다음 회차) — 다음 회차가 고른다.

# T33 완주 대조 — **25회차** (2026-09-15 03:3x · 워커 N · sess-0524-8791) — 축: `mix-blend-mode` 18 선언

24회차가 «남은 큰 축» 으로 센 것. 정본 `style.css` 의 `mix-blend-mode` 는 **18 선언 · 전부 `screen`**(밝게 섞기 — 검정은 사라지고 밝은 빛만 얹힌다). 클론의 같은 길은 하나다: `CraftFxPoly.Screen()`(T173) = `Forge/UiScreen` 재질(`Blend OneMinusDstColor One` · 알파만큼 프리멀티플라이).

## 정본 18 자리 ↔ 클론
- ① **보스 경고 섬광 1** — `.bw-flash`(239) ↔ `BattleOverlay.cs:204` `Radial(... CraftFxPoly.Screen())` ✓.
- ② **모루 타격 연출 5** — `.af-bloom`(998) · `.af-flash`(1013) · `.af-heat`(1027) ↔ `ForgeSheet.cs:551`(`DrawImpactGradient` 세 겹 전부 Screen) ✓ · `.af-star`(1057) ↔ 676 ✓ · `.af-core`(1078) ↔ 432(`DrawImpactEllipse(... screen: true)`) ✓.
- ③ **소환 결과 연출 12** — `.sr-wipe`(4252) ↔ `SkillSummonResult.cs:403` Screen ✓ **하나뿐**. 나머지 열하나:
  - **`.sr-flash`(4238) — 있는데 재질이 없다**: `SkillSummonResult.cs:386` 이 `UiKit.Panel(c, "sr-flash", "pp_line")` 로 세워 **보통 알파**로 그린다. 정본은 등급색 섬광을 screen 으로 얹어 아래 셀이 검게 죽지 않는다.
  - **열 겹은 아직 안 섰다**(`SummonFx.cs` 는 `material` 을 0곳 쓴다 · 이름 grep 0): `.sr-canopy b::after`(4063 · `srsweep`) · `.sr-orbwrap::after`(4370) · `.sr-relight`(4380 · `srrelight`) · `.sr-tierpulse`(4396) · `.sr-tierflash`(4409) · `.sr-spark`(4458 · `srspark`) · `.sr-ico::after`(4508) · `.sr-cell.peer.on::after`(4624 · `srheroring`) · `.sr-beam`(4650 · `srbeam`) · `#summon-result-modal.hero .sr-cell.heroic::after`(4666 · `srheroring`). 그 키프레임 열은 **T334 의 «40 의 무리» 장부(ⓑ 주역 · ⓒ 등급 섬광 · ⓓ 완료)에 이미 전부 있다** — 세우는 일은 그 절의 몫이고, 이 축이 더하는 것은 «세울 때 screen 재질» 한 줄이다.

## 이 회차의 판정
- **결함 11 · 새 번호 0**: 전부 소환 결과 연출(`SkillSummonResult.cs`·`SummonFx.cs` · T334·T342 산 lock)이라 **T334 절에 한 줄로 귀속**했다(T359 가 `.sr-*` opacity 여섯을 같은 절에 넘긴 전례) — `sr-flash` 재질 한 줄 + 열 겹은 설 때 `CraftFxPoly.Screen()`(`sr-wipe` 꼴 · 재질 못 찾으면 보통 알파). 판정 자리는 T334 의 `screen_summon-result` 눈 확인에 «섬광 프레임에서 아래 셀이 검게 안 죽는가» 를 더하면 된다.
- 모루·보스 경고 여섯은 이미 맞다 — T173 이 길을 내고 T155(모루 연출)가 그대로 썼다.
- 남은 CSS 축(24회차 목록에서 이것을 뺀 것): `white-space` 41 · `background-position` 22 · `background-size` 15 · `cursor` 51(모바일 — 뜻 없음). `white-space` 는 클론 공장 기본이 `NoWrap` 이라 nowrap 40 과 부호가 같고 «접히는 자리» 는 T351 이 잰다 — 다음 회차가 «normal 1 + 접히는 19곳» 만 보면 된다.

# T33 완주 대조 — **29회차** (2026-09-15 05:4x · 워커 N · sess-0524-8791) — 축: 상자 `border` 184 선언

어느 자도 안 보던 축이다 — `check_keyline`(T109)은 **글자** `-webkit-text-stroke` 만, `check_box_shadows`(T331)는 그림자만 본다. 상자 테는 CSS 에서 가장 흔한 «검정 키라인» 인데 폭 단·색을 짝지은 표가 없었다.

## 정본 — 폭 단 넷 × 색
- `--ol1: min(1px, calc(.0625rem + .49px))` · `--ol2` 2px · `--ol3` 3px · `--ol4` 4px(31~35) — 앱 폭 499px 기준의 **CSS px 단 넷**. `--cellb: round(down, var(--ol3), 1px)`(837·1070·1137) = ol3 을 픽셀로 내림.
- 선언 184(`border` 157 · `-top` 15 · `-bottom` 7 · `-left` 4 · `-right` 1). 같은 선택자의 뒤 규칙이 앞을 덮는 것 5를 접으면 **선택자×변 186** — 최종 단: **ol3 62 · ol2 44 · none 44 · ol1 19 · cellb 3 · ol4 1 · 직접값 13**.
- 색: `--pp-line`(#000) 이 ol3·ol2 의 대부분 · `#444c56`(ol1 8 + ol2 rc 폴백) · `#30363d`(ol1 5 · 상단바·탭바·던전 배너·프로필 카드) · `--rc` 등급색(펫 카드·알·소환 수량) · `rgba(255,255,255,.18~.34)`(소환 결과 힌트·빈 스킬 칸) · `#e2e0da`(`.tb-row` 밑줄) · `#d5d5d5`(`.idet-icon`) · `#7d3920`(`.tech-tree-node` .3rem) · `#3a434e`(`.dgc-cell` 2px).
- **계단 여섯(같은 선택자를 뒤 규칙이 덮음 · 자가 이것을 모르면 앞 값을 잘못 찍는다)**:
  - `#` 덮어쓴: 자리(앞 값 → → 값):
  - `.modal-card` border: 1225 var(--ol2) solid → #44 → 2412 var(--ol3) solid var(--pp-line
  - `.panel` border-top: 427 var(--ol2) solid → → 2463 var(--ol3) solid var(--pp-line
  - `.subtab-strip` button: border 442 var(--ol1) → #444c56 → 2477 var(--ol3) solid var(--pp-line
  - `#equip-sheet` border-top: 570 var(--ol1) solid → → 2507 var(--ol3) solid var(--pp-line
  - `#chat-preview` border-top: 2230 var(--ol1) solid → → 2516 var(--ol2) solid var(--pp-line
  - 그리고 **`.btn`(453 `ol1 #444c56`)** 은 선택자가 다른 `.modal-card .btn, .panel .btn, #equip-sheet .btn`(2438) 이 `ol3 pp-line` 으로 덮는다 — 바닥(HUD·오프라인·리그 뒤로) 버튼만 얇은 회색 테다.

## 최종 ol1(1px) 19 자리
- .subtab-strip button                           border          442 var(--ol1) solid #444c56       →  2477 var(--ol3) solid var(--pp-line
- #equip-sheet                                   border-top      570 var(--ol1) solid #30363d       →  2507 var(--ol3) solid var(--pp-line
- #chat-preview                                  border-top     2230 var(--ol1) solid #30363d       →  2516 var(--ol2) solid var(--pp-line
- 34 border-bottom  #topbar                                            var(--ol1) solid #30363d
- 41 border         .profile-card                                      var(--ol1) solid #30363d
- 453 border         .btn                                               var(--ol1) solid #444c56
- 470 border         #panel-debug input[type=number]                    var(--ol1) solid #444c56
- 477 border         .prob-chip                                         var(--ol1) solid var(--c, #444c56)
- 484 border         .upg-progress                                      var(--ol1) solid #444c56
- 659 border         .info-btn                                          var(--ol1) solid #444c56
- 1146 border         .hatch-slot                                        var(--ol1) dashed var(--rc, #444c56)
- 1155 border         .egg-chip                                          var(--ol1) solid var(--rc, #444c56)
- 1162 border         .pet-card                                          var(--ol1) solid #444c56
- 1185 border-top     #tabbar                                            var(--ol1) solid #30363d
- 1365 border         .toast                                             var(--ol1) solid #444c56
- 1380 border         .dg-banner                                         var(--ol1) solid #30363d
- 2153 border         .settings-toggle::after                            var(--ol1) solid var(--pp-line)
- 2168 border         .tech-node                                         var(--ol1) solid #444c56
- 2854 border         .sk-mini small                                     var(--ol1) solid #000
- 4841 border         .sr-qty                                            var(--ol1) solid var(--rc)
- 4852 border         .sr-dup                                            var(--ol1) solid rgba(255,255,255,.34)
- 4932 border         .sr-hint                                           var(--ol1) solid rgba(255,255,255,.22)

## 클론 — 길은 맞다
- 카탈로그 `line_px` **2**(«--ol1 키라인 · 기준 캔버스 px») · `line2_px` **4** · `line3_px` **6** · `PetSkillUi.json` `line1_px` — 기준 1080px ÷ 앱 499px ≈ **2.16 배**로 1·2·3 CSS px → 2·4·6(반올림). 쓰는 곳: `UiKit.Line` 13 · `PopupKit.Outlined` 15 · `PetSkillKit.Framed/Orb`(`line1_px`·`Line2`) · `DungeonPopups.Line2/Line3`.
- 색 키도 둘은 있다: `topbar_line` #30363d(상단바 `Hud.cs:71`·탭바 `TabBar.cs:66` = `line_px` ✓ ol1) · `toast_line` #444c56(토스트 링 = `PopupKit.Line` ✓ ol1).
- 실물 셋을 열어 봤다 — `sr-qty`(4841 ol1 rc) · `sr-dup`(4852 ol1 흰 .34) · `sr-hint`(4932 ol1 흰 .22) ↔ `SkillSummonResult.cs:500·509·590` `Framed(..., line1_px)` + 같은 색 — **정본 그대로**.

## 빠진 것 → T365
- **ol4 단이 없다**: `.pet-card` 왼쪽 등급색 띠(1162 `border-left: var(--ol4) solid var(--rc)`) — 카탈로그에 `line4_px`(8) 키가 없고 그 자리 짝도 못 찾았다(`PetPanel.cs` 에 `pet-card` 이름 0).
- **186 자리 표·자가 없다**: `Outlined` 호출 15 중 `Line3` 8 · `Line` 4 · **리터럴 `0.2f~0.9f` 7**(폭인지 비율인지 읽어야 한다 · §1 후보).
- **직접값 13**은 단이 아니라 자리마다 표값 — `.tech-tree-node` .3rem · `.dgc-cell` 2px · `.sr-shock`·`.sr-tierflash`·`.rw-ring` .34rem · `.rw-anchor`·`.sr-floor::before` .16rem · `.sr-canopy::before` .08rem · `.sr-idle i` .12rem · `.petd-tile`·`.sr-solo-own`·`.sr-again` 1px · `.pass-price` 0.
- 등재: **T365**(자 `check_box_borders` — T109·T331 꼴 · `line4_px` · 펫 카드 띠 · 리터럴 일곱 · `gate.sh`/`ci.yml` 등록).

## 이 회차의 판정
- 새 작업 **1**(T365). 폭 단·색의 «길» 은 맞고 빠진 것은 단 하나(ol4)와 «표» 다 — 21·22·24·25 회차의 «결함 0» 과 달리 이 축은 자가 서야 닫힌다.
- 남은 CSS 축: `background-position` 22 · `background-size` 15 · `cursor` 51(모바일 — 뜻 없음 · 닫아도 된다).

# T33 완주 대조 — **30회차** (2026-09-15 06:4x · 워커 N · sess-0524-8791) — 축: `background-position` 22 + `background-size` 15

CSS 속성 축의 마지막 둘(29회차가 남긴 것). 주석을 걷고 `;` 로 쪼개면 **32 선언**(`position` 21 · `size` 11 · 앞 회차의 22/15 는 주석 안 선언을 셌던 것).

## 갈래
- **키프레임 12**: 시대 무늬 흐름 `gp-interstellar`·`gp-multiverse`·`gp-underworld`·`gp-divine`(4930~4939 · 8 선언) ↔ T124 `AgePatternRules.Shift` + `AgePatternUi.json` 층별 `tile_rem`·`pos_rem` ✓ · `bwhazard`(420 · 2) → 아래 ⓒ · `srsweep`(6922~6924 · 2) → T334 «40 의 무리»(25회차 귀속).
- **시대 무늬 정적 3**: `.equip-cell[data-age]::before`(930) · `.af-age-bar::before`(4837) · `.fi-age-bar::before`(5119) 의 `--af-pat-size/pos` ↔ T124 표 ✓.
- **덮기 6**: `.dg-banner`(1956)·`.dg-detail-hero`(2068) `cover center`(그라디언트 — 뜻 없음) · `.dg-rw-ico`(1996) `100% center` · `.ico`(7197) `contain center`(22회차 `object-fit` 과 같은 뜻 ✓).
- **소환 스윕 1**: `.sr-orb::after` `260% 100%`(6917) → T334.
- **남는 셋 = repeating-linear-gradient 줄무늬 — 전부 어긋난다**:
  - ⓐ `.league-reward-tier`(2548~2555) 단 사이 **대시 구분선**(검정 3.23%W · 빈 3.02%W · 2px · 첫 단 없음) ↔ `LeagueSheet.cs:245` `UiKit.Line(row, "dash", …)` = **실선**(이름만 dash).
  - ⓑ `#panel-skills .summon-bar::before`(4205~4214) 소환 바 위 **풀블리드 대시**(주기 19.93%W 의 반 · 높이 .225%H · x=0 에 대시 중심) ↔ `SkillPanel.cs` 소환 바에 **없음**.
  - ⓒ `.bw-hazard`(397 + `bwhazard` 420) 보스 경고 **-45° 사선 띠**(주기 1.1rem · 가로 1.556rem · .62s 한 주기) ↔ `BattleOverlay.cs:211~228·287` **세로 대시 12개**(0.55rem/1.1rem · 코드에 박힌 수 셋) — T173 은 밝기·시각표만 판정했다.

## 이 회차의 판정
- 새 작업 **1**(T368 — 공용 «반복 줄무늬 굽기» 한 길 + 세 자리 배선 · T178 `SurfaceArt` 뒤).
- **CSS 속성 축은 이것으로 다 셌다.** 21~30회차가 연 축: `font-weight`·`aspect-ratio/object-fit`·`opacity`·`pointer-events`·`mix-blend-mode`·`white-space`·`text-align`·`gap`·`border`·`background-position/size`(+ 17~20 의 `z-index`·`border-radius`·`transform`). 남은 `cursor` 51 은 모바일에 뜻이 없어 **닫는다**. 다음 회차는 새 축이 아니라 **§7 열린 칸 재검**(12회차 꼴).

# T33 완주 대조 — **31회차** (2026-09-15 07:4x · 워커 N · sess-0524-8791) — 축이 아니라 §7 열린 칸 재검

30회차가 CSS 속성 축을 다 셌으므로 이 회차는 12회차 꼴로 **§7 «상태» 칸의 ⬜/🔄 전부**를 «PROGRESS 상태 · lock 나이 · 남은 것» 으로 갈라 적는다(자료: 07:4x 실측).

## 열린 칸 22
- T35   §7 ⬜ PROGRESS ⬜ 대기   lock 없음    | —
- T178  §7 ⬜ PROGRESS 🔄 진행   lock 없음    | 9회차 sess-0357-11617 / 워커 H · 판정 ✅ 런 597(자리 초록 24 · PlayMode 296/296) · **lock 반납** — 남은 갈래 둘은 9회차 판정 절에 적어 두었다
- T331  §7 🔄 PROGRESS 🔄 진행   lock 23분   | 1회차 = **자만** sess-1717-13450 / 워커 E (구현 파일 `Popups.cs`·`UiKit.cs`·`ShadowUi.json` 은 **T333 lock 뒤** — 같은 파일을 쥔 산 lock 이라 안 연다)
- T332  §7 ⬜ PROGRESS 🔄 진행   lock 29분   | 10회차 sess-0502-7887 / 워커 I (**세째 기구를 세웠다** — `Ui/DropShadow.cs` + `DropShadowUi.json`(실루엣을 `UiFilter.Blur` 로 흐려 구워 뒤에 깐다) · 첫 자리 `.pass-sword` · 남은 여섯
- T333  §7 ⬜ PROGRESS ⬜ 대기   lock 없음    | 8회차 ✓ 워커 H `sess-0357-11617`(장비 칸 이름표 네 자리 · 런 609 PASS · 자리 초록 20 · **lock 반납**) · (이력) 7회차 ✓ 워커 O `sess-2140-18689`(2026-09-15 03:4x~04:2x · `check_
- T334  §7 🔄 PROGRESS 🔄 진행   lock 23분   | — (1회차 ✅ sess-0154-10159 / 워커 P · Core 상태 기계 `SummonSeqRules` + EditMode 자 6 · 런 469 EditMode 662/662 · lock 반납 · **2회차(러너·표 `seq` 절·겹·PlayMode)는 T179
- T355  §7 ⬜ PROGRESS ⬜ 대기   lock 없음    | 6회차 lock 반납(sess-1920-15773 / 워커 B · 런 586 `PressFxSitesTests` 3/3 PASS · 일곱 중 여섯 섰다 · **남은 하나 `pet_tile` 은 T332 lock 뒤 누구든 한 줄 + 자 한 칸 → ✅**) · 2회차 s
- T342  §7 ⬜ PROGRESS 🔄 진행   lock 29분   | 5회차 sess-2005-27410 / 워커 A (코드 들어감 · 판정은 다음 런: ⓑ 빈 탈것 칸 `.mount-sil` 을 표 `mount_slot_empty`(brightness 0 · opacity .32)로 — `ForgeSheet` 의 박힌 검정 .32 를 
- T345  §7 ⬜ PROGRESS 🔄 진행   lock 38분   | **7회차 sess-0357-11617 / 워커 H**(기술 트리 아홉 자리 짝짓기) · (이력) 6회차 ✓ 워커 O `sess-2140-18689`(2026-09-15 04:4x~05:1x · ⓓ `check_border_radius` 를 `gate.sh`·`ci.y
- T351  §7 ⬜ PROGRESS 🔄 진행   lock 없음    | 1회차 sess-1920-15773 / 워커 B (산 lock 없는 자리부터 — 프로필 칸 `ProfilePopup.cs` + 줄 수 표 + 공용 도우미 + PlayMode 자 · 모루 이름은 T342·T178 lock 뒤 · 소환 이름은 T334 lock 뒤) · *
- T352  §7 ⬜ PROGRESS ⬜ 대기   lock 없음    | (이력) 3회차 ✓ 워커 O `sess-2140-18689`(2026-09-15 05:4x~06:2x · `.league-score` → `LeagueSheet.cs` · 런 603 `TabularSitesTests` 4/4 + `screen_league.png` 눈 
- T354  §7 ⬜ PROGRESS 🔄 진행   lock 6분    | 4회차 sess-1920-15773 / 워커 B (산 lock 없는 자리 둘 — 채팅 말풍선 `ChatScreen.cs` `chat_bubble_lh_w`(앱 폭 키 · 유일) · 탈것 업그레이드 팝업 빈 글 `MountUpgradePopup.cs` `mat_grid_
- T359  §7 ⬜ PROGRESS ⬜ 대기   lock 없음    | — (1회차 ✅ sess-2005-27410 / 워커 A · 런 565 `OpacityTests` 2/2 PASS · EditMode 720/720 · 승천 PNG 는 촬영 목록에 없어 눈 확인 자리 없음 · lock 반납 · **남은 넷(누구든 · 각 lock 뒤 `
- T361  §7 ⬜ PROGRESS ⬜ 대기   lock 없음    | 1회차 lock 반납(sess-1753-2066 / 워커 D · 런 587 EditMode `WrapRulesTests` **6/6** · ⓑ 표 41 + Core `WrapRules` + Game `WrapUi` + `check_wrap` 왕복 자 + EditMode
- T364  §7 ⬜ PROGRESS 🔄 진행   lock 20분   | sess-0718-8023 / 워커 J
- T365  §7 ⬜ PROGRESS 🔄 진행   lock 41분   | 1회차 sess-1753-2066 / 워커 D (자 `check_box_borders.py` + 카탈로그 `line4_px` · 펫 카드 띠 배선은 `PetPanel.cs` T331 lock 뒤)
- T368  §7 🔄 PROGRESS 🔄 진행   lock 51분   | —
- T369  §7 🔄 PROGRESS 🔄 진행   lock 3분    | 1회차 sess-2036-34862 / 워커 C = 표 키 `sk_grid_top_h` 0.1034 + EditMode `SkillGridTopTests` 2(dotnet 초록 · 판정은 CI 한 바퀴) · 배선 2회차는 SkillPanel.cs(T331·T364) l
- T28   §7 🔄 PROGRESS 🔄 진행   lock 13분   | 62회차 sess-0111-11076 / 워커 M
- T33   §7 ⬜ PROGRESS 🔄 진행   lock 0분    | 31회차 sess-0524-8791 / 워커 N — 축이 아니라 **§7 열린 칸 재검**(12회차 꼴 · 30회차가 CSS 속성 축을 다 셌다): §7 «상태» 칸의 ⬜/🔄 전부를 «남은 것 · 막는 lock · 누구든/주인» 으로 갈라 적는다 · 30회차까지 loc
- T349  §7 ⬜ PROGRESS 🔄 진행   lock 6분    | **배선은 끝났다 — 대상 5 · 선 것 5 · 남은 것 0**(6회차: `EdgeOutlineTests` 는 촬영이 아니라 자 전용 합성 리그라 **대상이 아니다**) · 남은 것은 `check_shot_cams.py` 의 **게이트 배선 한 줄**(T331 산 lo
- T366  §7 ⬜ PROGRESS 🔄 진행   lock 29분   | 1~2회차 sess-0609-25308·sess-0703-12493 / 워커 I (1회차 = 벽시계 상한 → 프레임 상한 + «왜 못 닿았는지» 를 문구에 · 2회차 = **덮는 대신 뿌리를 없앴다** — 그 자는 `BattleScene.AutoBoot = false`

## 갈래
- **주인 대기 1**: T35(`SIMPLE_BG` 되돌릴 때까지 선점하지 않는다).
- **산 lock 진행 13**: T28 · T331 · T332 · T334 · T342 · T345 · T354 · T364 · T365 · T366 · T368 · T369 · T349 — 임자가 있다.
- **lock 없이 «각 lock 뒤 누구든» 5**: T333(남은 미정 28 · T331·T332·T334 뒤) · T352(ⓐⓑ `UiKit.cs` + ⓒ 둘) · T355(`pet_tile` · T332 뒤) · T359(넷 · T331·T332·T345 뒤) · T361(ⓐ `UiKit.cs` T331 · `gate.sh` T331·T345 뒤). 다섯 다 **T331·T332·T345 가 쥔 파일** 뒤에 서 있다 — 그 셋이 반납하는 순간 다섯이 한꺼번에 열린다.
- **lock 반납 뒤 🔄 로 남아 있던 둘 → ⬜ 로 고쳤다**: T178(9회차 판정 ✅ 05:59 · lock 반납 · «남은 갈래 둘») · T351(1회차 워커 B · lock 없음 · 마지막 자기 커밋 90분 초과). `⬜` 만 훑는 선점 스캔(이 세션의 것 포함)에는 안 보이던 자리다 — README «⬜ = 잡을 수 있음» 대로 되돌렸다. 남은 일은 행에 그대로 있다.
- **§7 «⬜» ↔ PROGRESS «🔄» 어긋남 10**(T178·T332·T342·T345·T351·T354·T364·T365·T349·T366): `check_final_table` 이 «임자가 다음 커밋에 맞춘다» 로 넘기는 자리 — 여기서는 세기만 한다.

## 이 회차의 판정
- 새 작업 **0**. T33 은 아직 ✅ 가 아니다(T35 주인 몫 + 열린 칸 21). 다음 회차는 새 축이 아니라 **T331·T332·T345 가 반납하면 위 다섯을 잇는 것**이 먼저다.

## 32회차(31 → 32 로 옮김 · 07:4x 워커 N 의 31회차와 겹침 · 27회차가 같은 축을 이미 닫았다) — 정본 **`text-align` 89 + `ui.js` 인라인 7** 전수 (2026-09-15 · 워커 J · sess-0918-28910)

축을 고른 까닭: 클론 글자 공장 `UiKit.Text` 의 기본이 **`Center`** 인데 CSS 기본은 **left(start)** 라, T352(굵기)·T361(줄바꿈)과 **같은 «기본값 부호» 갈래**로 보였다. 그 둘은 실제 결함이었다.

### 정본 분포
`style.css` **89 선언** — `center` **61** · `left` **25** · `right` **3**. (`ui.js` 인라인 7 은 같은 세 값이다.)

### 가운데가 **아닌** 28 자리 — 27 은 살아 있고 1 은 죽은 CSS
`.stat-grid div:nth-child(even)`(1686 · `right`)는 **`ui.js`·`index.html` 어디에도 없다** — 정본이 안 그린다(T135 ⛔ 의 꼴). 나머지 27(`left` 24 · `right` 3)은 전부 렌더 줄이 있다.

### 클론 — «기본값 부호» 가설은 여기선 **안 선다**
- 클론은 `TextAlignmentOptions.Left` **111** · `Right` **23** 을 쓴다 — 정본의 25·3 보다 **훨씬 많다**. 까닭은 단순하다: **CSS `text-align` 은 상속**이라 컨테이너 한 줄이 자손 글자를 다 덮지만, 클론은 글자마다 공장에 인자를 준다. 그러니 «선언 수» 로는 두 쪽을 못 견준다.
- 정본 `left`·`right` 를 가진 화면 파일마다 클론에 그 호출이 있다: `QuestSheet` 2 · `LeagueSheet` 7/1 · `PassPopup` 1 · `ForgeAutoPopup` 4/2 · `ForgeInfoPopup` 4/1 · `AscendPopup` 4/2 · `ShopSheet` 3/1 · `ProfilePopup` 5 · `PetPanel` 10/1 · `SkillPanel` 6/1 · `DungeonSheet` 2/1 · `ChatScreen` 6/1 · `PlayerInfoPopup` Right 4.
- **한 번 헛짚었다**: `.tb-list`(2245 · `left`)를 보고 `TechPanel.cs` 를 열었더니 `Left`·`Right` 가 **0** 이라 «기술 화면이 통째로 가운데다» 로 보였다. 그러나 `.tb-list` 는 기술 트리 화면이 아니라 **«총 보너스» 팝업**(`ui.js` `openTechBonuses` 5540)이고, 클론에서는 **`TechPopups.cs`** 가 그린다 — 거기 `Left` 6 · `Right` 1 로 **제대로 있다**.

### 이 회차가 남긴 규칙
**선택자를 클론 파일에 맞출 때 «화면 이름» 으로 짐작하지 마라 — 정본 `ui.js` 에서 그 선택자를 내는 함수를 먼저 찾아라.** (`.tb-list` → `openTechBonuses` → `TechPopups.cs`. 파일 이름이 닮았다고 `TechPanel.cs` 를 열면 없는 결함을 만든다.)

### 이 회차의 판정
- **새 작업 0 · 결함 0**(죽은 CSS 1 자리 제외).
- 21(`font-weight`)·22(`aspect-ratio`+`object-fit`)·24(`pointer-events`)에 이어 **네 축째 «결함 0»** 이다. «CSS 선언을 세는» 축은 거의 바닥났다고 본다 — 남은 큰 것은 `align-items` 178 · `justify-content` 124 · `flex-direction` 82 인데 **선언 수로 셀 축이 아니다**(클론은 레이아웃 그룹이 여섯뿐 · T364 가 적어 둔 사정과 같다). `cursor` 51 은 폰 화면이라 애초에 해당이 없다.
- 다음 사람에게: 남은 값어치는 **축 세기보다 «화면을 눈으로 보고 어긋난 데를 찾는» 쪽**(T28 회차)에 있을 것이다.

## 33회차 — 정본 **`mask-image` 15 · `image-rendering` 3 · `transform-box` 13 · `container-type` 3** 전수 (2026-09-15 · 워커 A · sess-2005-27410)

축을 고른 까닭: 30·32회차가 «CSS 선언 축은 바닥» 이라 적었지만 `style.css` 속성 분포를 다시 세니 **한 번도 안 센 것**이 넷 남아 있었다 — 마스크(30 선언 = 15쌍) · 픽셀 보간 · 회전축 상자 · 컨테이너 단위. 작은 축이라 한 회차에 묶었다.

### ⓐ `mask-image` 15 — 클론 3 ✓ · **1 ✗(T380)** · 11 은 T334 몫
- `#dmg-flash`(460 · 아래 18% 소멸) ✓ `FxRules.DmgVigMaskKeep .82` + `BattleOverlay.VigSprite` 가 굽는다.
- `.af-age-bar::before`(4842 · 30→50%) ✓ `AgePatternUi.json bar_mask_from_f .3 / to_f .5` · `ForgeUi.AgeBar(autoForge:true)`.
- **`.fi-age-bar::before`(5123 · 24→46%) ✗** — 클론은 `mask: autoForge` 라 정보 팝업 막대엔 마스크가 없고 표에도 한 벌뿐이다 → **T380**.
- `.sr-*` 12(소환 결과 연출 · T334 산 lock): 선 것 4 = `.sr-rays` 5941 ↔ `rays_mask` · `#…done .sr-rays` 7157 ↔ `rays_done_mask` · `.sr-reflect` 6972 ↔ `reflect_mask` · `.sr-floor::after` 5827 ↔ `floor_tick_mask0~3` · 짝이 흐린 것 3 = `.sr-canopy::before/::after` 5861·5871 · `.sr-canopy b` 5900 ↔ `arch_mask/tick_mask/spill_mask`(이름으로만 짝지었다 · 값 대조는 임자 몫) · **자취 0 이 5** = `.sr-grid.mid/.dense` 6238 · `.sr-cell::before` 6340 · `.sr-cell[data-mat="gem"] .sr-orb::before` 6644 · `.sr-ray` 6683(`BakeRayBar` 는 있으나 마스크 키 없음) · `.sr-cell.heroic .sr-beam` 6745. T334 절이 «없는 겹» 을 회차마다 걷고 있어 새 등재는 안 한다 — 이 다섯을 그 임자가 보게 여기 적는다.

### ⓑ `image-rendering: crisp-edges; pixelated` 3 선택자 — **결함 0**
`.ico`(7215) → 아이콘 아틀라스 `atlas-0/1.png.meta filterMode: 0`(Point) ✓ · `.ico.av-ico`(7225 · 아바타 초상) → 아바타 `avatar_*` 도 같은 아틀라스 ✓ · `.dg-banner`(1959) → `UiKit.Icon(face, "scene", "dg_"+id)` = 아틀라스 `dg_*` ✓. 클론이 스스로 굽는 텍스처 21곳은 전부 `Bilinear` 인데 그것들은 그라디언트·그림자·블러라 정본도 보간한다(마스크·필터 층) — 픽셀아트는 아틀라스 하나로 모여 있다.

### ⓒ `transform-box` 13 + `transform-origin`(모루 타격 연출) — **결함 0**
`.anvil-svg` view-box 50% 92% ↔ `AnvilFxSpec.BumpOriginFrac {.5,.92}` · `.anv-billet` 55px 21.5px ↔ `BilletOriginVb` · `.af-hammer` 55px 11px ↔ `ForgeSheet.cs` 453 피벗 · `.af-ring/bloom/flash/heat/shadow/core` fill-box center ↔ 타원 스프라이트 구성상 중심 · `.af-star.sl/.sr` 100%/0% 55% ↔ 694 · `.af-spark`·`.af-scale` 0% 50% ↔ 612·643 · `.af-smoke` 50% 100% ↔ 518. 피벗 줄마다 정본 원점이 주석으로 붙어 있어 대조가 한 줄씩 끝났다.

### ⓓ `container-type: inline-size` 3 = `cqi` 단위 6 자리 — **결함 2(T381·T382)** · 값 자체는 맞거나 죽은 길
- `.equip-cell.empty .cell-img.dim 72cqi`(862) ↔ `ForgeSheet.cs` 221 `size*0.72` ✓.
- `.equip-cell .cell-img.emoji 76cqi`(1881) · `.adc-img.emoji 76cqi`(1083·1149): `.emoji` 는 3D 썸네일(T122)이 없을 때의 이모지 폴백이라 클론엔 닿는 길이 없다 — 대신 그 자리의 **비-이모지 규칙**을 보니 `.craft-batch .cb-card .adc-img`(1144)는 슬롯과 같은 fit-ink **.76** 인데 클론 `ForgeCraftPopup.cs:169` 가 **.9** → **T382**.
- `.equip-cell.egg-cell .cell-img.emoji 30cqi`(1882): 그 요소 = «탄 탈것» 얼굴인데 클론 `ForgeSheet.MountCell` 은 **빈 상태 하나뿐**이라 활성 탈것이 있어도 실루엣이다 → **T381**.

### 이 회차가 남긴 규칙
**«단위» 축(`cqi`·`em`)은 값보다 «그 값이 붙는 요소가 클론에 있는가» 를 먼저 묻는다** — 30cqi 의 수를 대조하려다 요소(탄 탈것 칸) 자체가 없는 것을 찾았고, 76cqi 는 값이 맞는 대신 옆 규칙(.76)이 .9 로 박혀 있었다.

### 이 회차의 판정
- **새 작업 3(T380·T381·T382) · 결함 3** · T334 몫 «자취 0» 5 는 그 절에 적었다(등재 아님).
- 32회차의 «CSS 축은 바닥» 은 반쯤 맞다 — 큰 축은 바닥이고 잔돈 축에서도 결함이 나온다. 아직 안 센 것: `animation-delay` 42 · `min-width` 32 · `max-height` 20 · `content` 171(장식 층은 ⓚ 가 셌다).

## 34회차 — 정본 **정적 `transform` 85**(선언 404 − 키프레임 317 − `@media` 2) 전수 (2026-09-15 · 워커 L · sess-1248-20175)

### 축을 어떻게 갈랐나
- `transform:` 선언 **404**. 그중 **317 이 `@keyframes` 안**, **2 가 `@media` 안**이라 «정적» 은 **85**다.
- 28회차가 «다음 축 후보: 정적 `transform` **87**» 로 남긴 수와 둘 차이가 난다 — 그 87 은 **CSS 주석 안의 두 줄**(6868 `.sr-cell` 설명 · 7642 `.fit-ink` 경고)을 같이 센 것이다. 주석을 **같은 길이의 공백**으로 지운 뒤 중괄호 깊이를 다시 재면 85다(주석 안에 중괄호가 들어 있어 깊이 스캔이 통째로 어긋난다 — 이 축을 셀 때는 주석 제거가 먼저다).
- 함수별(정적 85): `translateY` 30 · `translateX` 23 · `translate` 20 · `scale` 12 · `scaleY` 2 · `translateZ` 1.

### `translate` 계열 73 은 왜 안 셌나
정본의 `translate(-50%,-50%)`·`translateY(-50%)`·`translateX(-50%)` 는 거의 전부 **«가운데 맞춤»** 이고, 클론은 같은 일을 `RectTransform` 의 **앵커·피벗**(`UiKit.Anchor(…, pivot)`)으로 한다. 한 줄 ↔ 한 줄로 짝이 안 지어지고, «그 칸이 가운데에 섰는가» 는 이미 화면 PNG 자(T28·검수 Q)가 보는 축이다. 그래서 이 회차가 **선언 단위로 셀 수 있는 것은 rotate 3 + scale 계열 14 = 16 자리**다.

### 16 자리 전수
| 정본 | 선택자 | 무엇 | 판정 |
|---|---|---|---|
| 195 | `.pip.boss` | 보스 표식 45° 마름모 | ✓ 클론 `Hud.cs` `Quaternion.Euler(0,0,45)`(테 반지름 2px 은 T345 축) |
| 216 | `#offline-btn:active .ob-chest` | 눌림 `translateY(.08rem) scale(.94)` | **T355 몫**(눌림 피드백 일곱) |
| 389 | `.bw-banner` | `scaleY(0)` = 보스 경고 배너 여는 시작 상태 | T173·T39 |
| 1837 | `.cmp-card.new::after` | 15° 기운 좁은 광택 띠 | ⛔ **죽은 CSS** — `new` 는 `.cmp-card-wrap` 에 붙는다(`ui.js` 3226 `isNew ? 'new' : 'cur'`)고 `.cmp-card` 는 그 자식이라 이 선택자는 영영 안 맞는다(T135 ⓒ 판정 재확인) |
| 1898 | `#skill-cutin` | `translateX(-50%) scale(0)` = 컷인 콜아웃의 닫힌 상태 | **✗ 결함 → T384**(요소 자체가 클론에 없다) |
| 6187 | `.sr-wipe` | `scale(.55)` 시작값 | T334 |
| 6241 | `.sr-cell` | `scale(.35)` 시작값 | T334·T179 |
| 6343 | `.sr-orbwrap` | `scale(var(--sz) * var(--peersz))` | ✓ 클론 `c.BaseScale`(`sr_sz_*`·`sr_peer_sz` 표) |
| 6382 | `.sr-relight` | `scale(.3)` 시작값 | T334 |
| 6420 | `.sr-tierflash` | `scale(.24)` 시작값 | T334 |
| **6522** | **`.sr-ico`** | **`scale(1.04)` = 구면 배럴 보정(정적)** | **✗ 결함 → T385** |
| 6739 | `.sr-beam` | `scale(.2)` 시작값 | T334(33회차가 «자취 0» 로 그 절에 적은 다섯 중 하나) |
| 6970 | `.sr-reflect` | `scaleY(-1.22)` 바닥 반사 | ✓ T179 3회차 `SummonFx.BakeReflection`(원점 50%/55% 까지 같다) |
| 7462 | `.rw-glow` | `scale(.2)` 시작값 | T134 ✅ |
| 7479 | `.rw-ring` | `scale(.3)` 시작값 | T134 ✅ |
| 7516 | `.rw-pop` | `scale(.25) rotate(0deg)` 시작값 | T134 ✅ |

### 결함 ⓐ — `#skill-cutin`(→ T384)
정본은 스킬이 터질 때마다 캐릭터 위 `top:20%` 에 **아이콘 + 스킬 이름**을 그 스킬 색으로 0.8초 띄운다(`combat.js` 361·370·376 → `ui.js` 3759~3777 → `index.html` 90 · CSS 1897~1919). 클론은 `SkillFxDirector.cs:110` 이 `SkillCutin` 이벤트를 받아 **`pendingCutin = e.Tag` 만** 하고(다음 `SkillEffect` 의 def 를 찾으려는 것) 화면엔 아무것도 안 그린다 — `Assets/Scripts/Game/Ui/` 전체에 `cutin`·`컷인` 낱말 0.
**기록의 구멍 둘이 이걸 오래 가렸다**: PROGRESS 의 «효과는 있는데 이름만 없는 것(뺀다) 21» 이 `cutin` 을 «T12 `SkillFxDirector` 컷인» 으로 접었지만 그 층은 3D 스킬 오브젝트고 배너는 UI 층이다(같은 문서 T12 줄이 이미 그렇게 적어 뒀다) · 그 UI 몫을 맡은 T22 는 ✅ 로 닫혔고 이 칸은 안 옮겨졌다. 자간 자(T168)·글자 그림자 자(T333)는 이 선택자를 만날 때마다 «클론에 자리 자체가 없다» 로 **대조에서 빼기만** 했다.

### 결함 ⓑ — `.sr-ico`(→ T385)
정본 주석이 처방까지 적어 둔 자리다(6509~6547 · «아이콘이 구체 조명을 안 받아 스티커로 읽힌다» → ⑴ 접지 그림자 ⑵ 약 4% 배럴 스케일 ⑶ 구체 스페큘러를 아이콘 위에 한 겹 더). 클론 `SkillSummonResult.cs` 487~516 은 광채·접지 타원·`sr-orb-deep`·`sr-orb`·`sr-hilite` 까지 세우고 바로 다음 줄에서 아이콘을 **정사각 한 장**으로 얹는다 — **3겹 중 0겹**. ⑴ 의 값은 T332 표(`DropShadowUi.json` 의 `_남은_자리`)에 이미 적혀 있다.

### 이 회차가 남긴 규칙
**«그 자리가 다른 작업의 연출 시작값인가» 를 먼저 갈라라** — 16 중 10 이 그것(키프레임 `both`/`forwards` 가 덮어쓰는 0% 상태)이었고, 진짜 결함은 남은 6 에서만 나왔다. 연출 시작값을 결함으로 등재하면 임자가 둘 생긴다.

### 이 회차가 헛짚은 것(정정)
`CraftCardArt.Sheen` 주석의 «카드에 늘려 붙이므로 정사각으로 굽는다(정본 `::after { inset: 0 }`)» 를 보고, 1837 의 `.cmp-card.new::after`(좁은 15° 띠 · `top:-60% left:-80% w45% h220%`)와 어긋난다 = **주석이 틀렸다** 로 잡았었다. 틀린 것은 내 쪽이다 — 그 주석이 말하는 `::after` 는 **`.auto-drop-card.craft-reveal::after`**(1097)고 거기는 진짜로 `inset: 0` 이다(`crsheen`). **같은 `::after` 라도 어느 선택자인지 먼저 찾아라** — 32회차가 남긴 «선택자를 클론 파일에 맞출 때 화면 이름으로 짐작하지 마라» 의 형제 함정이다.

### 이 회차의 판정
- **새 작업 2(T384·T385) · 결함 2 · 죽은 CSS 1 재확인.**
- 아직 안 센 축: `animation-delay` 42 · `min-width` 32 · `max-height` 20 · `content` 171.

## 35회차 — 정본 정적 **`min-width` 28 · `max-height` 19** 전수 (2026-09-15 · 워커 L · sess-1447-341)

### 센 수
둘 다 `@keyframes` 안 **0** · `@media` 안 **0** 이라 전부 정적이다. 34회차가 «후보 32·20» 으로 남긴 수는 **주석을 안 걷은 것**이다(주석을 같은 길이 공백으로 지우고 중괄호 깊이를 다시 재면 28·19).

### `min-width` 28 — **열아홉이 `0` 이다**
`min-width: 0` 은 크기가 아니라 **«flex 자식이 내용보다 작아질 수 있게» 여는 스위치**다(안 주면 flex 자식이 내용 폭 아래로 안 줄어 말줄임이 안 걸린다). UGUI 엔 flex 도 그 기본값도 없으므로 **대응 선언이 없는 것이 맞고**, 그 자리들의 **효과**(자르기·줄임표)는 T351 축이 본다 — 실제로 T351 이 든 세 자리 중 `.profile-field`(3051)가 이 열아홉에 들어 있다.

남은 **아홉**(진짜 하한):

| 정본 | 선택자 | 값 | 클론 | 판정 |
|---|---|---|---|---|
| 5385 | `.dgc-cell` | 4.3rem | `dgc_cell_minw_rem` 4.3 | ✓ |
| 2439 | `.dg-right .btn` | 4.6rem | `dg_btn_w_rem` 4.6 | ✓ |
| 5792 | `.sr-again` | 8.6rem | `sr_again_w_rem` 8.6 | ✓ |
| 7140 | `.sr-ok` | 11rem | `sr_ok_w_rem` 11 | ✓ |
| 2058 | `.qst-right .btn` | 3.9rem | `quest_btn_w` 0.0739(= 3.9rem/H) **인데 `QuestSheet.cs:46` 이 ×1.6** | **✗ → T387** |
| 94 | `.profile-card` | calc(앱폭 × .3226) | 자취 0 | **✗ → T388** |
| 3913 | `.modal-card.sheet .dg-banner .btn` | calc(앱폭 × .1573 + 6.6px) | 자취 0 | **✗ → T388** |
| 4794 | `.af-dd-list` | 5.4rem | 드롭다운 폭 = 스피너 폭(`ForgeAutoPopup.cs:135`) | **✗ → T388** |
| 5066 | `.fi-pill` | 5.5rem | `pill_w_rem` 5.5 는 있으나 쓰는 곳이 `.cur-pill` | **? → T388** |

클론은 정본의 **하한**을 **고정 폭**으로 옮겼다 — 수가 같으면 «글자가 하한을 넘지 않는 한» 같은 그림이다. 그 전제가 깨지는지는 T387·T388 이 글자 폭으로 잰다.

### `max-height` 19
`calc(앱높이 × k)` **10** · `100%` 3 · `76%` 1 · `var(--popup-h-max)` 2 · `none` 2 · rem 1.

그 **열** 자리:

| 정본 | 선택자 | 값 | 클론 | 판정 |
|---|---|---|---|---|
| 3095 | `.settings-list` | .456H | `settings_h` .456 | ✓ |
| 2769 | `.pass-track` | .438H | `pass_track_h` .438 | ✓ |
| 2244 | `.tb-list` | .46H | `tbn_list_maxh` .46 | ✓ |
| 5047 | `.fi-card` | .8104H | `fi_card_h_f` .8104(T339) | ✓ |
| 792 | `.substat-list` | .4H | — | ⛔ **죽은 CSS** — `js/`·`index.html` 에 쓰임 **0** |
| 4700 | `.af-scroll` | .42H | — | ⛔ **항상 덮인다** — 유일한 쓰임(`ui.js` 2303)의 부모가 `.af-card` 고 **4683** `.af-card .af-scroll { max-height: none }` 이 이긴다 |
| 1948 | `.dungeon-list` | .6H | — | ⛔ **항상 덮인다** — 유일한 쓰임(`ui.js` 4544)의 부모가 `.modal-card.sheet` 고 **3871** `.modal-card.sheet .dungeon-list { max-height: none }` 이 이긴다 |
| 722 | `.forge-age-list` | .59H | 카드만 `fl_card_h_f` .76 고정 · 안쪽 목록 상한 0 | **✗ → T388** |
| 802 | `.mat-grid` | .4H | `Mathf.Max(10f, h - y - pad)` | **✗ → T388** |
| 2320 | `.league-list` | .542H | `footTop - listY - rem*0.3f` | **✗ → T388** |

⛔ 셋은 **클론에 없는 것이 맞다**. ⓑⓒ 는 클론이 «남은 칸을 채운다» 로 쓰는 것이 정본의 `none` 과 같은 뜻이다.

### 이 회차가 남긴 규칙
**덮는 규칙과 안 쓰이는 클래스를 먼저 걷어라.** 정본은 `max-height` 를 열 군데 적고 그중 **셋을 스스로 무효로** 만들어 뒀다 — 안 걷고 세면 «클론에 없는 자리 7» 이 되어 죽은 셋까지 등재했을 것이다. 34회차의 `.cmp-card.new::after` 와 같은 함정인데 결이 다르다: 그때는 «그 클래스가 그 요소에 안 붙는다» 였고 이번은 «**더 구체적인 선택자가 같은 속성을 덮는다**» 다. 축을 셀 때 **선택자 특이도**까지 봐야 한다.

### 이 회차의 판정
- **새 작업 2(T387·T388) · 결함 2 · 죽은 선언 3.**
- 못 센 형제 축: `min-height` **41**(값 `0` 여덟을 빼면 33) · `max-width` 7. 그 밖: `animation-delay` 42 · `content` 171.

# T33 완주 대조 — **36회차** (2026-09-15 17:3x · 워커 N · sess-0524-8791) — 축: 정적 `font-size` 365 선언

글자 축 넷 중 셋(`font-weight` T352 · `line-height` T354 · `letter-spacing` T168)은 임자가 있고 **`font-size` 만 아무도 안 셌다**. 주석을 걷고 `@keyframes`·`@media` 를 뺀 정적 선언 **365**(rem 350 · cqi 5 · em 4 · 0/inherit 4 · calc 2) · **서로 다른 값 71**. 정본 1rem = 16/844 앱높이(`main.js fitLayout` · 카탈로그 `rem_h` .018957) = 기준 캔버스 1920 의 **36.4px**.

## 값 분포(rem 350)
- ≤.6rem 23 · .6~.8rem **122** · .8~1.0rem **105** · 1.0~1.2rem 42 · 1.2~1.5rem 35 · >1.5rem 23. 상위: `.8rem`×33 · `.78rem`×27 · `.82rem`×24 · `1.05rem`×20 · `.85rem`×16 · `.72rem`×15 · `.95rem`×13 · `.74rem`×12 · `1rem`×12.
- 클론 종류표 `catalog.json textKinds`: `Title` 60(1.65rem) · `Button` 44(1.21rem) · `Body` 40(1.10rem) · `Sub` 36(.99rem) · `Micro` 18(.49rem) — 호출 521(Sub 372 · Body 70 · Title 41 · Button 29 · Micro 9) + 표에서 px 를 받는 자리 9(`TextSizeUi`·`BootLoading`·`CoinBurst`·`RewardBurst`·`SkillCutin`·`AscendPopup`).

## 갈래
- **≤1.0rem 250 자리(68%)** — §1 «하한: 본문 40 · 버튼 44 · 보조 36 · 제목 60(원작 UI 가 9:16 세로 폰에서 읽히던 크기)» 가 **일부러** 키우는 자리다. 이 축이 «결함» 으로 세지 않는다. 정본이 못박아 하한을 깨야 하는 자리는 예외 칸(`Micro` 18 · T136 · `TextSizeUi` 표 · T372·T383)이 이미 받는다.
- **하한 위 115 자리**:
  - 이모지·아이콘 **글리프 크기 58**(`.anvil-btn` 4.4rem · `.sr-orb` 6.2/3.2/2.4 · `.league-emblem` 3.3 · `#tabbar button` 2.6 · `.tile-face`·`.icon-circle`·`.cell-img.emoji`·`.waypoint-icon`·`.offline-rate-icon` …) — 클론은 아틀라스 아이콘을 **자리 크기**로 세우므로 글자 크기가 아니다(T31·T89).
  - **1.0~1.2rem 42**(1.05rem=38px ×20 · 1.15rem=42px ×8 …) — `Sub` 36·`Body` 40 안에 ±6% 로 든다.
  - **1.22~1.5rem 문자 14** — `Button` 44 가 맞는 여섯(`.tb-title` 46 · `.af-start` 47 · `.lgr-rank-n` 44 · `.float-dmg.dmg-kill` 47 · `#craft-modal .row .btn` 44 · `.rw-amt` 48 은 표) · `Title` 60 이 맞는 셋(`.profile-title`·`.dgd-keys`·`.bw-track span` 55) · **벌어지는 넷**: `.shop-gem-amt` 49 → `ShopSheet.cs` `Sub` 36(**−27%**) · `.rates-head h3` 47 → `SkillRatesPopup.cs` `Title` 60(**+28%**) · `.af-title` 46 → `ForgeAutoPopup.cs` `Title` 60(**+30%**) · `.league-tier-rank` 47 / `.text` 54 → `LeagueSheet.cs`(종류 못 읽음 · 잡는 사람이 확인) · `.sr-grid.one .sr-name` 46 → `SkillSummonResult.cs`(못 읽음).
  - **>1.5rem 문자 2**(`.dgclear-title` 1.7rem=62 · `.sr-title` 1.6rem=58) ↔ `Title` 60 ✓.

## 이 회차의 판정
- 새 작업 **1**(T391 — 종류표에 ≈48px(1.3rem) 단 하나 + 넷 배선 + 자). §1 하한 «위» 라 하한과 부딪치지 않는다 — 하한은 «작게 그리지 말라» 이지 «크게 그려라» 가 아니다.
- 글자 축 중 남은 것: `color` 325(T377 의 자는 **면** 색만 본다 — 글자 색 리터럴은 아직) — 다음 회차.

# T33 완주 대조 — **37회차** (2026-09-15 19:3x · 워커 N · sess-0524-8791) — 축: 글자 `color` 322 선언

36회차가 남긴 마지막 글자 축. 주석을 걷고 `@keyframes`·`@media` 를 뺀 정적 `color` **322**. 정본 토큰 15 · 클론 카탈로그 `colors` 168.

## 갈래
- 토큰 `var(--…)` **94**(`--pp-ink` · `--pp-muted` · `--pp-line` …) — 클론 카탈로그 키가 그대로 받는 자리.
- 리터럴이되 **토큰과 같은 값 125** — 토큰을 써도 되는 자리(T377 결정 636 의 셈 · 6자리·소문자로 접어 견줌).
- **토큰과 다른 리터럴 98(54 색)** — «토큰으로 뭉개지 말라» 자리(정본 8692 의 뜻). 값이 클론에 있는가로 갈랐다:
  - 카탈로그에 같은 hex **24 색**: `#ffd54f` `dgclear_title` ×14 · `#90a4ae` `debug_muted` ×9 · `#ff880f` `chat_name` ×6 · `#eceff1` `ink` ×5 · `#78909c` `muted2` ×3 · `#ffb300` `shop_banner` ×3 · `#3a3a3a` `idet_row_ink` ×3 · `#1fa64a` `tb_val` · `#7b7b7b` `btn_disabled_ink` · `#ff8a65` `cp` · `#ff8a80` `gem` · `#cccccc` `settings_odd` · `#69f0ae` `pip_done` · `#f59e0b` `league_cp` · `#dd3333` `settings_act_danger` · `#545454` `chat_time` … — 키는 있다 · **자리마다 그 키를 쓰는지**는 자가 봐야 한다.
  - 곁 표에 같은 hex **7 색**: `#eaf6ff`(`OfflineButtonUi`) · `#546e7a`·`#d9d9d9`·`#8b96b5`·`#6f6f6f`·`#dfe7ff`·`#2a1c04`(`PetSkillUi`).
  - **어디에도 없는 23 색 · 28 자리**(= 클론이 반드시 다른 색으로 그리는 자리): 전투 숫자 다섯(`.float-dmg.dmg-crit` #ff8a1e · `.dmg-kill` #fff3c4 · `.dmg-skill` #82b1ff · `.dmg-hero` #fff2f4 · `.block` #90caf9) · `.subtab-strip button.active` #7ee2a8 · `.btn.primary small` #7ee2a8 · `.fl-face[data-asc]::after` #ff8801 · `.equip-cell .slot-name` #d8caca · `.hatch-slot.buy` #cbb6f5 · `.modal-card .sub` #81d4fa · `.league-row.me .league-server` #dce6ff · `.league-collect-pill b` #1d8f3c · `.chat-preview-msg` #6d6a63/#aab3c0 · `.chat-input-bar input::placeholder` #6b6b6b · `#equip-sheet .forge-time` #6d6a63 · `#chat-preview` #1b1b1b/#eef1f5 · `.chat-preview-name` #eef1f5 · `.hatchery .slot-buy b` #e8112d · `.af-check.on` #23c552 · `.fi-skip-gem`·`.summon-cost b` #e11d48 · `.sr-solo-line` #e8eeff · `.sr-dup` #dfeaff · `.dg-banner .btn.disabled` #3d434a.
- rgba 2 · inherit 1 · 기타 2 — 뜻 없음.

## 이 회차의 판정
- 새 작업 **1**(T396 — T377 의 자에 잉크 갈래를 더하고 23 색을 자리 전용 키로 · 24 색은 자리 짝을 표에). 면 색(T377)과 같은 병이 글자 색에 그대로 있다.
- **글자 축 넷이 다 셌다**: `font-size`(36) · `font-weight`(21 → T352) · `line-height`(T354) · `letter-spacing`(T168) · `color`(37) · `text-shadow`(T333) · `text-align`(27) · `white-space`(26). 남은 CSS 축은 배치(`padding` 216 · `margin` 144 · `width` 321 · `height` 267 · `inset` 60)뿐인데 그것은 T28 채점표(30장 PNG 대조)가 화면 단위로 잰다 — 새 축을 열기보다 §7 열린 칸을 잇는 편이 먼저다.

