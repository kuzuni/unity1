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
