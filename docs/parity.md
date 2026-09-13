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

## ⓔ IconGen 키 (`icongen.js` `draw` 표 ↔ T31 아틀라스)

- `IconGen.draw` 키 98 · 유니티 아틀라스 키 170 · 아틀라스에 없는 draw 키 **54** ['arrow', 'arrows', 'axe', 'banner', 'bat', 'burst', 'bush', 'cir', 'cleaver', 'cloud', 'crate', 'cross', 'dbush', 'dragon', 'ell', 'fill', 'flame', 'flame3', 'flask', 'fn', 'for', 'ground', 'halo', 'horn', 'hourglass', 'if', 'ink', 'mark', 'maw', 'meteor']
- 아틀라스는 `tools/export_icons.js` 가 정본 `icongen.js` 를 **브라우저에서 그대로 실행**해 굽고 CI `check_icons_sync.sh`(datasync 잡 · 러너 chrome 으로 정본을 다시 그려 대조)가 매 런 지킨다 — 키가 빠지면 그 잡이 빨갛다(런 148 초록).

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
