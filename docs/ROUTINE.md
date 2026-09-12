# ROUTINE — 포지 클론 유니티 이식 (kuzuni/unity1) 작업 지시서

> **이 문서가 유일 지시서다.** 병렬 워커(루틴 세션)는 매 회차 이 문서 → `docs/PROGRESS.md` → `docs/claims/` 순서로 읽고 §0 절차대로 움직인다.
> 정본(스펙·수치·조형)은 **`kuzuni/wwwww` 의 `web/`**(HTML+Three.js 원작)이다. 이 레포는 그것을 **읽어** 유니티로 옮긴다 — wwwww 는 **읽기 전용**.
> 운영 틀은 `kuzuni/aaawunity`(꼬마기사 유니티 이식)의 `docs/ROUTINE.md` 규약을 그대로 옮긴 것이다(lock · SID · 90분 · T번호 · 결정 기록 · 게이트).

## ⚑ 신규 주인 지시 (위 항목이 최신 · 닫힌 것은 ✅ 를 단다)

- **(2026-09-12 · 주인 · 범위)** «다 되게 해야 하는데 왜 빼노» — 28개로 끝이 아니다. **원작 `web/` 전부**를 옮긴다: 소리(`sfx.js` · 지시서에 없었다)·아이콘·아바타(`icongen.js`·`avatars.js` · 없었다)까지 → T30·T31 등재(2026-09-12 계정 2 대화 세션). 앞으로도 원작 모듈 중 §2 어느 절에도 안 잡힌 것을 보면 **먼저 등재**한다(«주인 콘솔 에러 보고함» 이 아니라 §2 끝 + PROGRESS 표).
- **(2026-09-12 · 주인 · 착수)** «unity1 로 새로 팠다 이관해봐. aaawunity 라는 프로젝트 했듯이 루틴으로 나눠서 해줘야 함. 계정도 여러 개 쓸 수 있게 md 설정해 줘야 함.» → 이 문서 전체 + §6(계정별 루틴) + `docs/ROUTINES-SETUP.md`.
- **(상시 · wwwww 에서 이어받은 조형 지시)** 펫·탈것·적·소품·스킬 오브젝트는 전부 **마인크래프트 몹 문법**(축정렬 직육면체 + 칸 색 칠하기 · 곡면 근사 금지 · 종 자연색). 탑승 = **탈것 위에 서 있기**. 무기 = 마크 handheld 파지각(§1 «조형 계약»). wwwww `web/TODO.md` 상단 규약 블록이 원문이다.
- **(상시)** 작업 한 덩어리를 끝내고 push 한 직후 **ntfy 로 알린다**(`CLAUDE.md`). 앱 푸시·Routine 완료 알림은 도착하지 않는다 — 알림 경로는 ntfy 하나.

## 0. 세션 시작 절차 (모든 워커 공통 · 계정 1 = A~D · 계정 2 = E~H · 계정 3 = I~L·Q(검수) · 계정 4 = M~P · 계정 5 = R~U · §6)

1. `git fetch && git checkout -B main origin/main` (pull --rebase 금지 · 로컬 잔재 위에서 작업 금지). detached HEAD 환경이면 이어서 `git branch --set-upstream-to=origin/main main`.
2. SID 발급: `sess-HHMM-$RANDOM` (예: `sess-0512-23481`). 커밋 제목 끝에 `(sess-… · 워커 X)` 를 붙인다.
3. 읽는 순서: 이 문서(⚑ 머리 → §1 → §2) → `docs/PROGRESS.md`(표 + «주인 콘솔 에러 보고함» + «워커 결정 기록» 꼬리 20개) → `docs/claims/`. 정본은 `git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src` 로 옆에 둔다(**커밋 금지 폴더** · `.gitignore` 에 있다). 작업이 만지는 원작 파일(§2 각 절의 «정본» 줄)을 **먼저 Read 로 읽는다** — 이식은 «비슷하게 새로 만들기» 가 아니라 «그대로 옮기기» 다.
4. §2 에서 **선점 가능한 가장 앞 작업**을 lock 으로 선점한다(규약 `docs/claims/README.md`). 선점 «직전» 에 반드시:
   - `python3 tools/task_state.py <ID>` — 0 이면 잡아도 된다(살아 있는 남의 lock · 이미 ✅ · 그 번호를 가리키는 커밋이 있으면 1).
   - `python3 tools/check_task_rows.py` — 같은 작업이 두 줄로 갈라져 «⬜» 와 «✅» 가 어긋난 것을 잡는다.
   - `python3 tools/check_claim_scope.py` — 살아 있는 lock 이 «범위» 칸에 안 적은 파일을 쥐고 있는지 본다(내가 고를 파일이 실은 남의 자리일 수 있다).
   - «⬜ 대기» 를 믿기 전에 **그 자리 코드를 한 번 읽는다** — 이미 반영돼 있으면 표가 늦은 것이다.
5. 선점할 작업이 없으면(전부 lock 또는 완료): §3 게이트만 재실행해 검증하고 이상 없으면 **커밋 없이 조용히 종료**. 이상이 있으면 PROGRESS 에 «가장 큰 번호 +1» 로 등재(`python3 tools/task_state.py --new-id`)하고 종료.
6. 회차 첫 일은 언제나 **빨강**이다 — main 의 마지막 CI 런이 빨갛거나(컴파일 파손 · 테스트 0개) 남의 lock 이 없는 빨강이면 그것을 먼저 고친다(§1).

## 1. 절대 규칙

- **정본 수정 금지.** `kuzuni/wwwww` 는 읽기만 한다. 수치는 `Assets/StreamingAssets/data/*.json`(T2 가 정본에서 뽑는다)에서 읽고 **코드에 게임 수치를 직접 박지 않는다**. JSON 은 손으로 고치지 않는다 — `tools/export_data.js`(T2)로만 다시 뽑는다.
- **조형 계약(주인 상시 지시 · wwwww `web/TODO.md` 상단 블록 원문)**:
  - 펫 25 · 탈것 29 · 적 7 · 소품 · 스킬 오브젝트 = **축정렬 직육면체 몇 개 + 칸 색(paint)**. 구·원뿔·원기둥·비스듬한 관을 칸으로 근사하지 않는다(그것이 원작에서 폐기당한 사유다). 색은 종 자연색 — 등급색 전신 틴트 금지.
  - 조형 표는 원작 `web/js/mobs-pets.js`·`mobs-mounts.js`·`mobs-enemies.js`·`mobs-props.js`·`mobs-skillfx.js` 가 **단독으로** 쥔다. 유니티 쪽은 그것을 JSON 으로 받아(T2) `VoxelMob`(T4)이 그대로 세운다. **유니티에서 종 좌표를 손으로 고치지 않는다** — 고칠 것이 있으면 «주인 콘솔 에러 보고함» 에 «정본에서 고칠 것» 으로 적는다.
  - 관절 id 가 곧 애니 계약이다: `hipL/kneeL`(다리) · `shL/elbowL`(팔) · `glegL`(기둥 다리) · `legFL/kneeFL`(사족) · `tag:'wing'`+`s` · `tag:'tail'` · `cap/capDome/capTop` · `body`+`jelly`. 이름을 바꾸면 보행·공격·플린치가 **조용히** 죽는다.
  - 탑승 자세 = **탈것 위에 서기**(원작 `RIDE_STAND_POSE` · 발이 안장 칸에 · 등자·고삐 정렬 끔). 비행 탈것 hover = 지면에서 0.04(바닥 0.20~0.39 세계 단위) · 평지 0.10.
  - 무기 파지 = 마크 handheld: 어깨 뼈에 붙이고 `MC_CARRY_X = -1.05 + π` · `MC_GRIP_PULL 0.10`(손 쪽으로) · 칼날은 세워서(`rot[1] = -π/2`) — 원작 `scene3d.js` 의 `applyWeaponGrip` 이 정본.
- **플레이 콘솔 에러 0.** 화면·전투·팝업 코드를 바꾼 커밋은 PlayMode 스모크(T27 `UiSmokeTests` · `PlayLog.AssertNoRed`)가 그 화면을 열어 빨간 줄 0 을 검증해야 한다. `LogAssert.NoUnexpectedReceived()` 는 쓰지 않는다.
- **컴파일 파손을 남긴 채 다음 작업으로 넘어가지 않는다.** push 한 워커는 다음 회차에 «내 커밋을 담은 CI 유니티 잡이 컴파일을 지나 테스트를 실제로 돌렸는가» 를 먼저 본다 — «테스트 0개» 는 빨간 테스트보다 나쁘다. 컴파일이 깨져 있으면 그것이 그 회차의 첫 일이다(자기 lock 이든 남의 lock 이든).
- **유니티 «패키지» 타입을 새로 쓰면** 그 타입이 어느 어셈블리인지 확인해 `*.asmdef` 의 `references` 에 넣는다 — `tools/dotnet` 하니스는 URP·TMP 를 스텁으로 물어 **로컬에서 절대 안 걸린다**. 스텁(`tools/dotnet/Stubs`)에도 같은 서명을 더한다(추측 금지 — 실제 API 서명을 확인하고).
- `Assets/Scripts/Core` 에는 `UnityEngine` 을 참조하지 않는다(asmdef `noEngineReferences: true` · dotnet 이 강제). 엔진(전투 틱·대장간·펫·스킬 계산)은 전부 Core 다 — 유니티 없이 `dotnet test` 로 원작 JS 와 대조한다.
- **판단이 필요하면 기다리지 않고 스스로 정해 적용**하고 PROGRESS «워커 결정 기록» 에 «무엇을 · 왜 · 되돌리려면 어디» 한 줄을 남긴다. 번호는 커밋 «직전» 에 `python3 tools/check_decisions.py --next` 로 뽑는다. 겹치면 **늦게 push 한 쪽이 옮긴다**.
- **새 콘텐츠·새 시스템·밸런스 변경 금지.** 원작에 없는 것을 넣지 않는다.
- **에셋은 주인 에셋만**: UI `GUI PRO Kit - Casual Game` · 이펙트 `JMO Assets/Cartoon FX Remaster` · `DOTween` · `AllIn1SpriteShader` · 글꼴 `Assets/Fonts/NotoSans-Regular.ttf`. 3D 조형은 **코드 생성 복셀 메시**(T4)뿐이다 — 외부 모델·임시 그림 금지. 새 에셋을 쓰면 `docs/assets-map.md` 에 «용도 · 경로 · GUID» 한 줄.
- 승인 프롬프트가 뜨는 명령·대화형 편집기(`git rebase -i`) 금지. 캡처 PNG·대용량 바이너리 커밋 금지(예외: `screens` 브랜치는 CI 가 올린다).
- **lock 은 «CI 가 그 커밋을 한 번은 돈 뒤» 반납한다.** 로컬 게이트는 PlayMode 를 못 돌리므로 «초록» 의 절반만 본 것이다.
- 작업이 끝나면 lock 삭제 → PROGRESS 갱신 → 커밋 → push. **lock 만 잡는 커밋·문서만 바꾼 커밋은 제목 끝에 `[skip ci]`**. 커밋 메시지 **본문**에 그 표식을 인용하지 마라(GitHub 은 인용과 지시를 안 가린다).
- 브랜치는 `main` 하나다. 커밋 작성자는 `git -c user.name=kuzuni -c user.email=<그 계정의 이메일>`. 커밋 제목은 `T<번호> <무엇> (sess-… · 워커 X)` 꼴 — `check_claim_scope` 가 그 번호로 커밋을 센다.
- 한 줄에 문장이 여럿인 코드 줄 끝에 `// 주석` 을 붙이지 않는다(뒤 문장이 주석이 된다 · dotnet 은 못 잡는다).
- 글자 크기·색을 코드에 숫자로 박지 않는다 — `UiKit`(T18)의 종류(`TextKind`)를 준다. 하한: 본문 40 · 버튼 44 · 보조 36 · 제목 60(원작 UI 가 9:16 세로 폰에서 읽히던 크기다).
- **작업 완료 알림**: 작업 한 덩어리를 push 한 직후 ntfy(`CLAUDE.md`). 문구는 «무엇을 끝냈는지» 한 줄 + 커밋 7자리.

## 2. 작업 목록 (순서 고정 — lock ID = 아래 번호 · 끝내면 제목에 ✅ · 접으면 ✂/⛔)

> 정본 경로는 전부 `.wwwww-src/web/` 기준. «범위» = 이 작업이 만지는 폴더/파일(PROGRESS 표의 «범위» 칸과 같아야 한다 · 여기 없는 파일을 열게 되면 표를 먼저 고친다).
> 이식 순서의 뼈대: **기반(T1~T5) → 전투 세계(T6~T12) → 시스템(T13~T17·T23~T25) → UI(T18~T22) → 배포·검증(T26~T28) → 보강(T30 소리 · T31 아이콘·아바타)**. 앞 번호가 열려 있어도 «뒤 순서» 가 만족되면 잡을 수 있다.

### T1 ✅ — 프로젝트 뼈대: 3D URP 렌더러 · 9:16 세로 카메라 · Bootstrap 씬 · asmdef 셋 · dotnet 하니스 초록 (기반 · 뒤 순서 없음)
- 지금 프로젝트는 2D 템플릿(`Assets/Settings/Renderer2D.asset` · `UniversalRP.asset` 이 2D 렌더러를 가리킨다)이다. 원작은 **3D**(Three.js · 반구광 + 방향광 · ACES 톤맵 · 플랫 셰이딩)라 **Universal Renderer(Forward) 에셋을 새로 만들어** `UniversalRP.asset` 의 `m_RendererDataList[0]` 을 그것으로 바꾼다(2D 렌더러 에셋은 지우지 않는다 — 주인 에셋).
- `Assets/Scenes/SampleScene.unity` 에 `Bootstrap` 하나(빈 GameObject + `Bootstrap.cs`) · 카메라 세로 9:16(원작 `scene3d.js` 의 카메라 FOV·거리·`camLock` 값을 그대로) · 배경색·안개·광원 = 원작 `setTheme` 의 0번 테마.
- `Assets/Scripts/Core`(`noEngineReferences`) · `Game` · `Tests/EditMode`·`PlayMode` asmdef — 이미 자리표가 있다(`Forge.Core`·`Forge.Game`·`Forge.Tests.EditMode`). PlayMode asmdef 를 더한다.
- 판정: CI `dotnet` 잡 초록 + (시크릿이 있으면) 유니티 잡이 테스트를 **실제로** 돌린 것(0개 아님) + PROGRESS 행 + «주인이 확인할 것: 에디터에서 Play → 빈 세로 씬 · 콘솔 빨강 0».
- 범위: `Assets/Settings` · `Assets/Scenes` · `Assets/Scripts/Game/Bootstrap.cs`(GameInfo.cs 자리표 삭제) · `Assets/Scripts/Core/Viewport.cs`(레터박스 순수 계산) · `Assets/Tests`(EditMode/ViewportTests.cs · PlayMode/Forge.Tests.PlayMode.asmdef · PlayMode/BootstrapTests.cs) · `ProjectSettings/ProjectSettings.asset`(productName · 세로 고정) · `tools/dotnet`.

### T2 ✅ — 정본 데이터 추출기: 원작 JS 표 → `Assets/StreamingAssets/data/*.json` + 동기화 검사 (기반 · 뒤 순서 없음)
- 정본: `web/js/balance-data.js`(대장간 확률·비용·시간 · 펫 알 드랍·부화 · 소환 확률 · 오프라인) · `gamedata.js`(시대·등급·장비명·스킬·펫 정의) · `mobdata.js`+`mobs-pets.js`·`mobs-mounts.js`·`mobs-enemies.js`·`mobs-props.js`·`mobs-skillfx.js`(조형 표) · `data/raw/*`.
- `tools/export_data.js`(node 22 · 의존성 0): 원작 파일을 `globalThis` 루트로 `vm` 에 로드해 **평가된 값**을 JSON 으로 쓴다. 조형 표는 종마다 `{cell, parts:[{id, box, at, c, paint, mat, parent, pivot, rot, joint, tag, s, gait, head}]}` 를 **함수를 다 푼 뒤** 내보낸다(`quad()`·`eyes()` 같은 도우미는 원작 안에서 이미 배열을 만든다 · `THREE` 는 표 정의 시점에 필요 없다 — 필요하면 그 종을 «정본에서 고칠 것» 으로 보고). 색은 `0xRRGGBB` 정수 그대로.
- 출력: `balance.json` · `gamedata.json` · `mobs-pets.json` · `mobs-mounts.json` · `mobs-enemies.json` · `mobs-props.json` · `mobs-skillfx.json` (+ `.meta` 는 `gen_meta.py`).
- `tools/check_data_sync.sh [<wwwww 체크아웃>]`: 정본으로 다시 뽑아 `cmp` — 다르면 rc 1 (`--sync` 로 복사). CI `datasync` 잡이 이것을 부른다(스크립트가 생기면 자동으로 켜진다).
- 판정: JSON 7개 + 자기 검사(`node tools/export_data.js --self-test` · 펫 25 · 탈것 29 · 적 7 이 나오는가) + CI 초록.
- 범위: `tools/export_data.js` · `tools/check_data_sync.sh` · `Assets/StreamingAssets/data/`.

### T3 ✅ — Core: `MiniJson` · `GameData`(JSON 로더 · 표 접근자) · `Rng`(원작과 같은 mulberry32/시드) (Core · T2 뒤)
- 정본: `web/js/util.js`·`bignum.js`(큰 수 표기 — K/M/B/T… 원작 표기 그대로) · `state.js` 의 난수 사용처.
- `GameData.Load(json 문자열들)` → 강타입 표(`ForgeTable`·`PetTable`·`SkillTable`·`MobModel`…). 어느 수치도 코드 상수로 두지 않는다. `BigNum` 표기 함수는 원작 `bignum.js` 와 **같은 입력 → 같은 문자열**(EditMode 표 테스트 30개 이상).
- 범위: `Assets/Scripts/Core/Data/` · `Assets/Scripts/Core/BigNum.cs` · `Assets/Tests/EditMode/DataTests.cs`.

### T4 ✅ — `VoxelMob`: 마인크래프트 박스 모델 조립기 (Game · T2·T3 뒤)
- 정본: `web/js/voxel.js`(`Voxel.box`·`Voxel.build` — 칸 → 면 병합 메시 · 정점 색 · AO 0.85 · jitter 0.022) · `web/js/mobs.js`(`Mobs.build` — paint/expand(mx 거울) · matKey 재질 공유 · pivot Group · parent 연결 · tag/joint 수집).
- C# 로 **같은 규약**: `VoxelMob.Build(MobModel, cell, vivid)` → `GameObject` 트리. 파츠 = 정점 색 `Mesh` 하나 · 성질(basic/opacity/emissive/rough)이 같은 파츠끼리 `Material` 하나(URP Lit · 정점 색 · 플랫 셰이딩은 노멀을 면마다 따로 둔다). `pivot` 이 있으면 빈 `Transform` 아래에 · `parent` 는 그 pivot 아래에. 반환: `parts[id]` · `legs`(gait) · `wings`(s) · `head` · `tail` · `wheels` · `spinners` · `glow` · `claws` · `joints`(axis/amp/f/ph/gain/abs/spin).
- EditMode(순수 계산은 Core 로 뺀다: `VoxelGeometry` — 칸 목록 → 정점/색/인덱스 배열): 6면 박스 1칸 = 24정점 · paint 의 음수 인덱스·mx 거울 · 재질 키 병합.
- 범위: `Assets/Scripts/Core/Voxel/` · `Assets/Scripts/Game/Voxel/` · `Assets/Tests/EditMode/VoxelTests.cs`.

### T5 ✅ — 몹 도감 씬 + 시트 촬영: 펫 25 · 탈것 29 · 적 7 · 소품 · 스킬 오브젝트 전부 세워 본다 (Game·검증 · T4 뒤)
- `MobGallery` 씬(또는 PlayMode 테스트가 세운다): JSON 의 모든 종을 격자로 세우고 **예외 0 · 파츠 수 = 표 파츠 수 · 바운딩 높이 = cell × 표 높이(±1칸)** 를 단언. 카메라 각은 원작 `web/tools/shot-mobs.js` 와 같게(정면 3/4 · 위에서 20°).
- PlayMode 테스트가 `ui-screens/mobs_pets.png`·`mobs_mounts.png`·`mobs_enemies.png`·`mobs_props.png`·`mobs_skillfx.png` 를 남긴다 → CI 가 `screens` 브랜치로 올린다(§5). 주인이 원작 시트와 나란히 본다.
- 범위: `Assets/Scripts/Game/Gallery/`(GalleryData.cs · MobGallery.cs · GalleryProps.cs · GallerySheet.cs · MobGalleryScene.cs) · `Assets/Tests/PlayMode/MobGalleryTests.cs` · `Assets/Tests/PlayMode/Vectors/t5-bounds.json` · `tools/gallery_vectors.js`(정본 경계 상자 벡터).

### T6 — 영웅: 박스 모델 캐릭터 + 무기 파지 + 대기/걷기/근접 스윙 애니 (Game · T4 뒤)
- 정본: `web/js/prochar.js`(머리·몸통·팔·다리 파츠 · 어깨 rx 한계 [−2.95, 2.35] · 근접 클립 = 예비동작/감기/정지/가속 타격/오버슈트/느린 회복 + 엉덩이·무릎) · `scene3d.js` 의 `applyWeaponGrip`(어깨 뼈 · anchorY = −(limbH − MC_GRIP_PULL) · `MC_CARRY_X` · 칼날 세우기 · `RANGED_SHAPES` 제외) · `WEAPON_GRIP` 표.
- 클립은 코드 애니(Animator 안 씀 · 원작이 프레임마다 각을 계산한다). 무기 8종 형상(`gamedata.js` 의 무기 모양 → 복셀 표)은 T15 가 잇는다 — 여기서는 «막대 한 자루» 로 파지각·타이밍만 맞춘다.
- PlayMode: 스윙 한 사이클의 어깨 각 곡선이 원작 키프레임(예비 → 타격 → 회복 시각 비)과 ±5% · 무기 로컬 회전 = (MC_CARRY_X, −π/2, 0).
- 범위: `Assets/Scripts/Game/Hero/` · `Assets/Tests/PlayMode/HeroTests.cs`.

### T7 — Core 전투 엔진: 100ms 고정 틱 · 웨이브 4+보스 · 스탯·치명·넉백·처치·스테이지 진행 (Core · T3 뒤)
- 정본: `web/js/combat.js`(587줄 · 전부) · `state.js` 의 전투 관련 상태 · `balance-data.js` 의 적 스탯 곡선.
- `Battle`(순수 C#): 입력 = 영웅 스탯·장비·펫 3·스킬 4·스테이지 → 틱마다 이벤트(공격·피격·처치·웨이브·보스·드랍). **원작 JS 와 판 단위 일치**: `tools/sim/`(node) 로 원작 `combat.js` 를 같은 시드로 돌려 처치 시각·드랍 목록을 JSON 으로 뽑고 EditMode 가 그것과 대조한다(시드 3개 × 100판).
- 범위: `Assets/Scripts/Core/Battle/` · `tools/sim/` · `Assets/Tests/EditMode/BattleTests.cs`.

### T8 — 전투 씬: 적 스폰·보행·공격·플린치·사망 · 데미지 숫자 · 카메라 셰이크 · 히트 파티클 (Game · T6·T7 뒤)
- 정본: `scene3d.js` 의 `monsterMesh`(어댑터 · 관절 계약) · `killEnemy`(파편색 = `shardC` 부피 가중 평균) · `spawnEnemy` 자리·간격 · 데미지 숫자 스타일 · 셰이크 진폭/감쇠 · `heroAttack` 타이밍(T6 클립과 같은 시각에 판정).
- 파티클은 `Cartoon FX Remaster` 프리팹만(카탈로그 키로 · `docs/assets-map.md`). Core `Battle` 이벤트를 구독해 그린다 — 씬이 규칙을 계산하지 않는다.
- PlayMode: 30초 자동 전투 · 콘솔 빨강 0 · 적 7종 전부 한 번씩 스폰·사망.
- 범위: `Assets/Scripts/Game/Battle/` · `Assets/Tests/PlayMode/BattleSceneTests.cs`.

### T9 — 맵·바이옴: 챕터 테마 10종 · 소품 배치(근경/중경 점유) · 지면·안개·광원 (Game · T4 뒤)
- 정본: `scene3d.js` 의 `setTheme`(테마 10종 색표 · 잎 색은 테마가 쥔다) · 소품 어댑터(`makePine`·`makeRoundTree`·`makeBoulder`… → `Props` 표) · 배치 규칙(`probe-nearfield-mass`·`probe-midground-depth` 게이트 값) · «역할 하나 = 메시 하나 = 드로우콜 하나».
- 소품 `Props.fitU(칸, 목표높이, 목표폭)` 치수 역산을 그대로. 자작나무 흰 기둥만 재질을 따로.
- PlayMode: 테마 10종 순회 · 드로우콜(`UnityStats.drawCalls`) ≤ 원작 한도 · 콘솔 빨강 0.
- 범위: `Assets/Scripts/Game/World/` · `Assets/Tests/PlayMode/WorldTests.cs`.

### T10 — 펫 출전 3마리: 대형(`PET_ROW0`·`PET_ARC`) · 따라오기 · 공격 참여 · 제너릭 관절 드라이버 (Game · T8 뒤)
- 정본: `scene3d.js` 의 `makePetMesh` 어댑터 · 펫 대형 상수 · 펫 관절 드라이버(`joints` 서술 · axis/amp/f/ph/gain/abs/spin) · `pets.js` 의 출전·스탯 기여.
- 범위: `Assets/Scripts/Game/Pets/` · `Assets/Tests/PlayMode/PetSceneTests.cs`.

### T11 — 탈것: 서서 타기 · 안장 칸 · 비행/평지 hover · 탈것 드라이버(다리·날개·바퀴·스피너) (Game · T6·T10 뒤)
- 정본: `scene3d.js` 의 `makeMountMesh` · `RIDE_STAND_POSE` · `RIDE_STAND_BULK 1.45` · `MOUNT_FORMS`(fly hover 0.04 · 평지 0.10) · `rideSkirt(false)` · `mounts.js`.
- 범위: `Assets/Scripts/Game/Mounts/` · `Assets/Tests/PlayMode/MountSceneTests.cs`.

### T12 — 스킬 오브젝트: 로봇·표창×10·드래곤 등 실체가 나와서 때린다 (Game · T8 뒤)
- 정본: `web/js/mobs-skillfx.js`(오브젝트 표) · `scene3d-skillfx.js`(연출 계약 — 등장·이동·타격 시각·퇴장) · `skills.js` 의 18종 효과 종류(광역/단일/회복/버프).
- 이펙트 «빛 덩어리» 로 되돌리지 않는다(주인 지시). Core 스킬 판정(T17)과 시각의 타격 시각을 맞춘다.
- 범위: `Assets/Scripts/Game/SkillFx/` · `Assets/Tests/PlayMode/SkillFxTests.cs`.

### T13 ✅ — 세이브·오프라인: JSON 세이브(persistentDataPath) · 30초 자동 · 절대시각 타이머 · 오프라인 보상 (Core+Game · T3 뒤)
- 정본: `state.js`(스키마 · 마이그레이션 · 오프라인 수급률 코인 1/초 · 해머 1/분 · 캡 4시간) · `main.js` 의 저장 시점.
- Core `SaveData`+`Offline` 순수 계산 · Game `SaveIo`. EditMode: 오프라인 4시간 캡 · 타이머가 절대시각으로 이어지는가.
- 범위: `Assets/Scripts/Core/Save/` · `Assets/Scripts/Game/SaveIo.cs` · `Assets/Tests/EditMode/SaveTests.cs` · `tools/export_data.js`·`tools/check_data_sync.sh`(state.json 추출 갈래) · `Assets/StreamingAssets/data/state.json`(추출기로만 · 결정 22) · `Assets/Scripts/Core/Data/MiniJson.cs`(`JsonObject.Remove` 한 줄).

### T14 ✅ — Core 대장간: 업그레이드 비용·시간(Lv1~35) · 시대 티어 확률(10시대) · 젬 스킵 · 오토 포지 (Core · T3 뒤)
- 정본: `forge.js` · `balance-data.js`(`forgeProbabilities` 등). 확률표 합 = 100 단언 · 원작 함수와 같은 입력 → 같은 결과(시드 고정) 표 테스트.
- 범위: `Assets/Scripts/Core/Forge/` · `Assets/Tests/EditMode/ForgeTests.cs`.

### T15 — 장비 8부위 + 페이퍼돌: 무기/투구/갑옷 3D 외형 · 등급 6 · 서브스탯 · 판매가 `20×1.01^(Lv−1)` (Core+Game · T6·T14 뒤)
- 정본: `forge.js`(장착·판매·스탯) · `gamedata.js`(장비명·모양) · `scene3d.js` 의 무기 복셀 표(`WEAPON_GRIP`·형상) · 투구·갑옷 부착.
- 범위: `Assets/Scripts/Core/Gear/` · `Assets/Scripts/Game/Hero/Paperdoll.cs` · `Assets/Tests/EditMode/GearTests.cs`.

### T16 ✅ — Core 펫 시스템: 알 드랍(1-1~10-10) · 부화 시간(30분~32시간) · 펫 25 스탯 · 중복 레벨업 · 합성 (Core · T3 뒤)
- 정본: `pets.js` · `balance-data.js`. 표 테스트: 드랍표 행 100개 합 · 부화 시간 표 · 합성 규칙.
- 대조 벡터: `tools/pet_vectors.js` 가 정본 `pets.js` 를 vm 으로 **실제로 돌려** `Assets/Tests/EditMode/pet_vectors.json` 을 뽑는다(mulberry32 시드 · 정본이 바뀌면 다시 뽑는다 · `--check` 로 같은지 본다).
- 범위: `Assets/Scripts/Core/Pets/` · `Assets/Tests/EditMode/PetTests.cs` · `Assets/Tests/EditMode/pet_vectors.json` · `tools/pet_vectors.js`.

### T17 — Core 스킬 시스템: 소환 확률(소환 Lv1~100) · 18종 · 4슬롯 · 자동/수동 · 중복 레벨업 (Core · T7 뒤)
- 정본: `skills.js` · `combat.js` 의 스킬 판정 · `balance-data.js`.
- 범위: `Assets/Scripts/Core/Skills/` · `Assets/Tests/EditMode/SkillTests.cs`.

### T18 ✅ — UI 껍데기: 9:16 레터박스 캔버스 · SafeArea · `UiKit`(GUI PRO Kit 조각 스폰 · TMP NotoSans · TextKind 하한) · HUD · 하단 탭 (Game · T1 뒤)
- 정본: `web/index.html` · `css/style.css`(rem 스케일 · 레터박스) · `ui.js` 의 HUD/탭 구조 · 원작 스크린샷 `web/ref/screens/shot-*.png`(**Read 로 직접 본다**).
- 글자는 `UiKit.Text/Label/Button` 으로만 · `fontSize` 직접 금지(§1). 카탈로그(`Assets/Forge/catalog.json` → 용도 키 → GUI PRO Kit 경로)를 세우고 `docs/assets-map.md` 를 만든다.
- PlayMode: 부팅 → HUD · 콘솔 빨강 0 · `TextSizeGateTests`(모든 활성 Text 가 하한 이상).
- 범위: `Assets/Scripts/Game/Ui/` · `Assets/Forge/catalog.json` · `docs/assets-map.md` · `Assets/Tests/PlayMode/UiSmokeTests.cs`.

### T19 — UI 패널: 대장간 · 장비(8부위·장착·판매·x1/x10 제작) (Game · T14·T15·T18 뒤)
- 정본: `ui.js` 의 해당 패널 + `web/ref/screens/` 해당 샷. 배치는 원작 비율(±2%p).
- 범위: `Assets/Scripts/Game/Ui/Forge*` · `Ui/Gear*` · `Assets/Tests/PlayMode/ForgeUiTests.cs`.

### T20 — UI 패널: 펫(알·부화·합성·출전) · 스킬(소환·장착) · 탈것 (Game · T16·T17·T18 뒤)
- 범위: `Assets/Scripts/Game/Ui/Pet*` · `Ui/Skill*` · `Ui/Mount*` · `Assets/Tests/PlayMode/PetUiTests.cs`.

### T21 — UI 패널: 던전 4종 · 기술트리 · 승천 (Game · T23·T24·T18 뒤)
- 범위: `Assets/Scripts/Game/Ui/Dungeon*` · `Ui/Tech*` · `Ui/Ascend*` · `Assets/Tests/PlayMode/DungeonUiTests.cs`.

### T22 — UI 패널: 상점 · 패스 · 퀘스트 · 리그 · 채팅 · 설정·디버그 탭 (Game · T25·T18 뒤)
- 범위: `Assets/Scripts/Game/Ui/Shop*` · `Ui/Pass*` · `Ui/Quest*` · `Ui/League*` · `Ui/Chat*` · `Ui/Settings*` · `Assets/Tests/PlayMode/ShopUiTests.cs`.

### T23 — Core 던전 4종: 입장·소탕·보상 (Core · T7 뒤)
- 정본: `dungeons.js`. 범위: `Assets/Scripts/Core/Dungeons/` · `Assets/Tests/EditMode/DungeonTests.cs`.

### T24 ✅ — Core 기술트리 · 승천 (Core · T3 뒤)
- 정본: `techtree.js` · `ascension.js`. 범위: `Assets/Scripts/Core/Tech/` · `Core/Ascension/` · `Assets/Tests/EditMode/TechTests.cs` · `tools/export_data.js`(표 칸 추출 갈래 `TECH_FIELDS`) · `tools/check_data_sync.sh` · `Assets/StreamingAssets/data/tech.json`.
- 표(분기·노드·보너스·배수·승천 라인·별 배율)는 `tech.json`(추출기 파일 · `TechData.Load`)에서 온다 — `GameData` 의 7파일과 별도. 규칙은 `TechTree`·`Ascension`(Core) 이 원작 함수 이름 그대로 든다.

### T25 — Core 상점 · 패스 · 퀘스트 · 리그 (Core · T3 뒤)
- 정본: `shop.js` · `pass.js` · `quests.js` · `league.js` · `chat.js`(채팅은 표시용 문자열 표만). 범위: `Assets/Scripts/Core/Meta/` · `Assets/Tests/EditMode/MetaTests.cs`.

### T26 ✅ — WebGL 빌드 · gh-pages 배포 · 배포 스모크(headless 로 열어 콘솔 에러 0 · 전투 진입) · Android APK (배포 · T18 뒤)
- `ci.yml` 의 `build-webgl` 은 이미 있다(UNITY_LICENSE 가 있어야 굽는다 — README «주인이 할 일»). 여기서 하는 것: WebGL 템플릿(캔버스가 창을 채우는 세로 껍데기 · 로딩 완료 표식) · `tools/webgl_smoke.js`(Playwright · 컨테이너에서는 `kuzuni.github.io` 가 프록시에 막히므로 CI 러너에서 돈다) · Android 잡.
- 범위: `Assets/WebGLTemplates/` · `tools/webgl_smoke.js` · `.github/workflows/ci.yml`(빌드 잡 부분만) · `ProjectSettings/ProjectSettings.asset`(webGLTemplate · 압축 폴백 · Android 식별자 칸만 — 템플릿을 «쓰게» 하는 칸이 거기 있다).

### T27 — PlayMode 스모크·플레이 봇: 모든 화면 열기 · 한 판 플레이(전투→제작→장착→펫→스킬→던전) · 콘솔 빨강 0 · 촬영(`ui-screens/*.png`) (검증 · T19~T22 뒤)
- 정본 각본: wwwww `web/ROUTINES-SETUP.md` §4-C(QA 시나리오). `PlayLog.AssertNoRed` 도우미 · `UiShotsTests`(540×1170 PNG 전 화면) → CI `screens` 브랜치.
- 범위: `Assets/Tests/PlayMode/PlaythroughTests.cs` · `UiShotsTests.cs` · `PlayLog.cs`.

### T28 — 원작 대조 회차: `screens` PNG ↔ `web/ref/screens/shot-*.png` 비율 대조표(`docs/ref-layout.md`) + `tools/ui_score.py` (검증 · T27 뒤)
- aaawunity §5 방식: 화면마다 «요소 · x% · y% · w% · h%» 표를 원작 샷에서 5% 격자로 판독 → 우리 PNG 와 ±3%p 대조 → 점수. **8.0 미만이면 그 화면의 UI 작업을 «다음 고칠 것» 으로 재등재**.
- 범위: `docs/ref-layout.md` · `tools/ui_score.py`.

### T29 — `task_state.py` «코드 자취» 오탐: 주석의 미래 참조(`T7 이 쓴다`)를 자취로 세어 T7·T13·T14·T25 선점을 막는다 (도구 · 뒤 순서 없음)
- 실측(2026-09-12 워커 K · 워커 M 결정 11ⓔ 도 같은 것): `Rng.cs`·`Hud.cs` 의 `///` 주석이 «T7 전투 · T13 세이브» 를 앞으로 가리키고, `check_claim_scope.py`·`task_state.py` 의 자기 검사 픽스처 문자열이 T14·T25 를 담아 `task_state.py T7` 등이 rc 1 을 낸다 → 규약대로면 아무도 못 잡는다.
- 고침: 자취 검사에서 **주석 줄(`//` · `///` · `#` · 문자열 리터럴)과 `tools/` 의 자기 검사 픽스처를 제외**하고 코드 식별자·파일명·폴더명만 센다. 자기 검사(`--self-test`)에 «주석만 가리키는 번호는 깨끗하다» 케이스를 더한다.
- 범위: `tools/task_state.py`.

### T30 — 사운드 이식: 효과음 29종 + 음악 4모드·레이어 6종 — 원작 `sfx.js` 의 **코드 합성**을 그대로 (Core+Game · T3 뒤)
- 정본: `web/js/sfx.js`(618줄 · 전부 · 외부 파일 0 — WebAudio 프로시저럴). 효과음: `anvilHit arrowShot auraRise bossSiren craft craftReveal equipDrop equipSnap equipToss gacha healDescend hit levelUp mawBite mawRoar slashArc stormCrackle stormRumble stormStrike summonCharge summonReveal voidPierce voidSnap voidTear`(호출마다 피치 랜덤화 · 레이어드 합성 = 어택 트랜지언트+바디+저역 텀프+테일). 음악: 모드 4종(`normal/boss/dungeon/shop`) · 레이어 6종(서브베이스·베이스·디튠 패드·아르페지오·멜로디·퍼커션) · 스윙·박 위계 다이내믹 · 템포 동기 딜레이/합성 리버브. 버스: master(SFX)·musicGain(BGM) → 소프트 리미터 → 출력 · 합성 IR 리버브 센드 공용. 공개 표면: `startMusic setMusicMode toggleMusic musicEnabled resume`.
- 원작이 «외부 파일 금지 · 전부 코드 합성» 이니 유니티도 **오디오 파일을 들이지 않는다**: `Assets/Scripts/Core/Audio/`(순수 C# DSP · 오실레이터·엔벨로프·노이즈·IR 합성·시퀀서 → `float[]` 샘플 · `dotnet test` 로 길이·피크·무음 아님·결정론(시드) 검증) + `Assets/Scripts/Game/Audio/`(`AudioClip.Create` 로 클립화 · 캐시 · `AudioSource` 버스 2개 + 리미터는 유니티 `AudioMixer` 없이 Core 리미터를 샘플에 미리 적용 · `Sfx.Play(name)`·`Music.SetMode(mode)` 표면 이름을 원작과 같게). 주파수·엔벨로프·BPM·음계·모드별 곡 데이터 같은 **수치는 코드에 박지 않고** `Assets/StreamingAssets/data/sfx.json` 으로(T2 추출기에 `sfx.js` 갈래를 더한다 · 표로 못 뽑는 함수 안 상수는 결정 기록에 사유를 적고 Core 상수 파일 하나에 모은다).
- 호출 지점(전투 타격·대장간 망치·소환·장비 장착·보스 사이렌·모드 전환)은 그 화면·전투 작업(T8·T19·T20·T21)이 붙인다 — 이 작업은 **모듈 + 전 효과음 재생 PlayMode 스모크** 까지. 이미 끝난 작업의 호출 지점은 이 작업이 붙인다.
- 판정: EditMode DSP 테스트(효과음 29종 · 모드 4종 각 8박 렌더 → 길이·피크 ≤ 1.0·RMS > 0·같은 시드면 같은 샘플) + PlayMode(효과음 29종 전부 `Play` · 모드 4종 전환 · 콘솔 빨강 0) + CI 초록 + PROGRESS 행 + «주인이 확인할 것: 에디터 Play → 망치 소리·전투 타격음·상점 음악 전환이 원작(web 에서 재생)과 같은 인상인가».
- 범위: `Assets/Scripts/Core/Audio/` · `Assets/Scripts/Game/Audio/` · `Assets/StreamingAssets/data/sfx.json` · `tools/export_data.js`(sfx 갈래만) · `Assets/Tests/EditMode/AudioTests.cs` · `Assets/Tests/PlayMode/AudioSmokeTests.cs`.

### T31 — 아이콘·아바타 이식: `icongen.js` 523종 + `avatars.js` 24종 — 정본을 **래스터로 뽑아** 유니티가 받는다 (도구+Game · T2·T18 뒤 · T19~T22 는 이것을 쓴다)
- 정본: `web/js/icongen.js`(6,704줄 · 캔버스 2D 벡터 그리기 · 장비·펫·스킬·재화·탭·등급 왕관·성별 등 523 그리기 함수 · 표면 `IconGen.img(key)`·`url`·`skill`·`tab`·`cls`)· `web/js/avatars.js`(831줄 · 도트 초상 24종 · `IconGen.avatar`). `ui.js` 가 `IconGen.img` 를 146곳에서 부른다 — 원작 화면의 «그림» 은 전부 여기서 나온다.
- 7,500줄 캔버스 코드를 C# 으로 다시 그리지 않는다. **T2 와 같은 원칙**(정본이 단독으로 쥔다 · 유니티는 받아 세운다): `tools/export_icons.js` 가 Playwright Chromium(컨테이너·CI 에 있다 · T26 `webgl_smoke.js` 가 쓴다) 으로 `icongen.js`·`avatars.js` 를 진짜 캔버스에 올려 키 전부를 그리고 → `Assets/Forge/Icons/atlas-N.png`(2048² 이하 · 아틀라스 몇 장) + `atlas.json`(키 → 장·rect·원본 px) 로 낸다. 결정론(같은 정본 → 같은 바이트)이어야 `tools/check_icons_sync.sh` 가 T2 `check_data_sync.sh` 처럼 CI 에서 «정본과 같은가» 를 볼 수 있다(그 잡 한 줄 추가). PNG 총량 상한 3MB(넘으면 해상도 단을 낮춘다 · 캡처 PNG 금지 규칙의 예외로 «정본에서 기계로 뽑은 아틀라스» 를 §1 에 한 줄 적는다).
- Game: `UiIcons.Get(key)` → `Sprite`(아틀라스 슬라이스 · Resources 또는 카탈로그 GUID 참조 · T18 `UiCatalog` 와 같은 길) · `UiKit.Icon(key)` 이 이것을 쓰도록. 키 이름은 원작 문자열 그대로. 아바타 24종은 채팅·리그(T22)가 쓴다.
- 판정: `node tools/export_icons.js --self-test`(키 523+24 전부 그려짐 · 빈 캔버스 0) + 동기화 검사 초록 + PlayMode(키 전부 `Get` → null 0 · 콘솔 빨강 0) + CI 초록 + PROGRESS 행 + «주인이 확인할 것: 에디터에서 아틀라스를 열어 원작 아이콘 그대로인가».
- 범위: `tools/export_icons.js` · `tools/check_icons_sync.sh` · `Assets/Forge/Icons/` · `Assets/Scripts/Game/Ui/UiIcons.cs` · `Assets/Scripts/Game/Ui/UiKit.cs`(Icon 한 갈래만) · `.github/workflows/ci.yml`(datasync 잡의 아이콘 검사 한 줄만) · `Assets/Tests/PlayMode/UiIconsTests.cs`.

## 3. 게이트 (커밋 전 · 세션 종료 전)

> ⚑ **꼬리로 읽지 마라 — `rc` 를 보라.** 자들의 출력은 «고치는 법» 으로 끝나는 것이 많아 마지막 줄만 보면 빨강과 초록이 같아 보인다. `; echo rc=$?` 를 붙여 돌린다.

```bash
dotnet build tools/dotnet/Forge.sln -c Release --nologo                       # 컴파일 (Core · Game(스텁) · Tests)
dotnet test tools/dotnet/Tests/Forge.Tests.csproj -c Release --no-build       # 순수 C# 테스트 (NUnit 3.6.1 API 면만)
python3 tools/gen_meta.py --check                                             # .meta 누락/고아 (새 에셋을 만들면 --check 없이 돌려 생성)
python3 tools/check_docs_intact.py                                            # 문서가 통째로 깨졌는가 (충돌 표식 · 결정 기록 소실 · 표 0행) — CI 에서 막는다
python3 tools/check_decisions.py                                              # 결정 번호 겹침 · `--next` 로 다음 번호
python3 tools/check_task_rows.py                                              # PROGRESS 같은 작업 두 줄 어긋남
python3 tools/task_state.py --check                                           # ROUTINE §2 제목 ↔ PROGRESS 상태 · 번호 중복
python3 tools/check_claim_scope.py                                            # 살아 있는 lock 이 «범위» 밖 파일을 쥐고 있는가 (선점 전에도)
tools/check_data_sync.sh .wwwww-src                                           # (T2 뒤) data/*.json ↔ 정본
node tools/export_data.js --self-test                                         # (T2 뒤) 추출기 자기 검사
```

- 로컬에 `dotnet` 이 없는 컨테이너(클라우드 세션은 대개 없다)에서는 `dotnet` 두 줄을 건너뛰고 **그 사실을 완료 기록에 적는다** — 그때는 CI 의 `dotnet` 잡이 초록인 것을 확인하기 전에는 lock 을 반납하지 않는다.
- PlayMode 는 워커 환경에서 못 돌린다 — CI 유니티 잡 런 번호로 확인한다(시크릿이 없어 유니티 잡이 안 돌면 «주인 에디터 확인 요청» 으로 적는다).

## 4. PROGRESS.md 기록 규약

- 표의 자기 작업 행을 갱신: 상태(⬜ 대기 / 🔄 진행 / ✅ 완료 / ⛔ 폐기·흡수 / ✂ 번호 태움) · SID · 워커 · 핵심 수치.
- 완료 시 반드시: 게이트 결과(테스트 수 · 빌드 초록 · CI 런 번호) + 커밋 해시 + **«주인이 확인할 것» 한 줄** + «플레이 콘솔 에러 0 을 무엇으로 확인했는가»(PlayMode 테스트 이름·CI 런 / 또는 «주인 에디터 확인 요청»).
- 완료 기록은 `### T<n> 완료 기록 (날짜 · 워커)` 절로 표 아래에 붙인다 — 다음 사람이 «무엇을 · 왜 · 어디서 확인» 을 거기서 읽는다.
- 판단이 필요한 것은 기다리지 않고 정해 적용하고 «워커 결정 기록» 에 번호를 이어 한 줄(«무엇을 · 왜 · 되돌리려면 어디»). «주인 승인 대기» 절은 만들지 않는다.
- «주인 콘솔 에러 보고함» 에 주인이 적은 것은 «가장 큰 번호 +1» 로 등재하고 UI 작업보다 앞에 둔다.

## 5. 눈 확인 회차 (`screens` 브랜치)

- CI 유니티 잡은 PlayMode 촬영 테스트가 남긴 `ui-screens/*.png` 를 main push 마다 `screens` 브랜치로 올린다(빨간 런이어도 · `meta.json` 의 `tests` 칸으로 빨간 런의 그림인지 안다 · PNG 0장이면 안 올린다).
- 워커는 `git fetch origin screens && git show origin/screens:<파일>.png > /tmp/x.png` 로 받아 **Read 로 직접 본다**. 원작 시트는 `.wwwww-src/web/ref/` 와 `web/tools/shot-*.js` 가 만드는 것(원작을 node+Playwright 로 직접 찍어도 된다 — 컨테이너에 Chromium 이 있다: `/opt/pw-browsers/chromium`).
- ✅ 조건(UI 작업): T28 의 점수 8.0 이상. 조형 작업: 시트에서 원작과 실루엣·색이 같다(주인 눈).

## 6. 다른 계정의 워커 합류 (계정 1 = A~D · 계정 2 = E~H · 계정 3 = I~L·Q(검수) · 계정 4 = M~P · 계정 5 = R~U)

> 주인 지시: «계정도 여러 개 쓸 수 있게». 계정마다 **그 계정의 Claude Code 세션**이 이 절만 읽고 루틴 4개(계정 3 은 +Q)를 만든다. 자세한 복붙용 런북은 **`docs/ROUTINES-SETUP.md`**.

### ⓪ 계정 식별표 — «내가 몇 번째 계정인가» 는 여기서 본다 (세션 시작 시 `get_session` 의 이메일/env 로 대조)

| 계정 | 로그인 이메일 | account uuid | environment_id | 워커 | 슬롯(UTC) |
|---|---|---|---|---|---|
| **계정 1** | `kimmoon2007@gmail.com` (표시명 «김문») | `b7a233c8-…` | `env_014bNYWJnnxgzqfDN9JPBD6p` | A · B · C · D | :05 :20 :35 :50 |
| **계정 2** | `rudwpwjrwkdb1995@gmail.com` | (미확인 — 워커 H 세션의 `get_session` 에 uuid 가 안 실린다) | `env_01JWPF8hM8XqtGYWuFnAsN93` | E · F · G · H | :12 :27 :42 :57 |
| **계정 3** | `rudwpwjrwkdb95@gmail.com` (표시명 «김문») | `7029fe80-2b87-422e-98e6-9d6aafaf5c7f` | `env_01XKJDdWmKFxSg4FuR8yethb` | I · J · K · L · Q | :02 :17 :32 :47 · Q 짝수시 :00 |
| **계정 4** | `kimmoon1995@gmail.com` | (미확인 — 워커 N 세션의 `get_session` 에 uuid 가 안 실린다) | `env_016Xis527zoBbZPqrtAZVQ6x` | M · N · O · P | :09 :24 :39 :54 |
| **계정 5** | `rudwpwjrwkdb2007@gmail.com` (표시명 «김문») | `2968d241-39ae-47a7-aa6a-cb96fa1ea1b4` | `env_01Pzd5v3t1DrfBQqJkzqcXqC` | R · S · T · U | :14 :29 :44 :59 |

- 새 계정을 붙이는 세션은 **이 표의 빈 줄을 먼저 채우고**(이메일·uuid·env) 그 줄의 워커 글자·슬롯을 쓴다. 같은 이메일이 이미 있으면 그 계정이다 — 새 줄을 만들지 않는다.
- 이메일이 표에 없고 빈 줄도 없으면 **주인이 계정 번호를 준 경우에만** 표 끝에 새 줄을 만든다(워커 글자는 Q 다음인 R 부터 · 슬롯은 §6 ② 분 나열에서 2분 이상 떨어진 빈 칸). 번호를 못 받았으면 루틴을 만들지 않고 «계정 표가 찼다» 로 보고한다.
- ⚠ 표시명이 같아도(«김문» 이 계정 1·5 둘 다) **이메일·`account uuid`·`environment_id` 가 다르면 다른 계정이다** — 그 셋으로 대조한다.

### ① 주인이 먼저 할 것 (계정마다 · 한 번만)
1. 그 계정의 claude.ai → **GitHub 연결**에 `kuzuni/unity1` 이 보이고 **push 가 되어야** 한다. 같은 GitHub 사용자(kuzuni)를 연결하면 끝. 다른 GitHub 사용자면 레포 Settings → Collaborators 에 **Write** 로 추가. **Claude GitHub App 의 저장소 접근에 `kuzuni/unity1` 을 켠다**(안 켜면 그 계정의 세션이 `add_repo` 에서 «push access 없음» 으로 막힌다 — 2026-09-12 계정 1 실측).
2. 그 계정에 **환경(Environment)** 이 하나 있어야 한다(기본 «Default» 면 된다). `environment_id` 는 **계정마다 다르다** — 루틴을 만들 때 그 계정 값을 쓴다.
3. 확인법: 그 계정에서 클라우드 세션을 열어 `docs/claims/README.md` 끝에 «계정 N 확인 YYYY-MM-DD» 한 줄을 붙여 `git push origin main` 이 되는지 본다.

### ② 그 계정의 Claude Code 세션이 할 것 — 루틴 만들기
- **슬롯(cron UTC · 분)** — 네 계정 16개 + Q 가 서로 안 겹치게 3~4분 간격으로 엇갈린다:

| 계정 | 워커 | cron |
|---|---|---|
| 1 | A `5 * * * *` · B `20 * * * *` · C `35 * * * *` · D `50 * * * *` | :05 :20 :35 :50 |
| 2 | E `12 * * * *` · F `27 * * * *` · G `42 * * * *` · H `57 * * * *` | :12 :27 :42 :57 |
| 3 | I `2 * * * *` · J `17 * * * *` · K `32 * * * *` · L `47 * * * *` · **Q `0 */2 * * *`**(검수 · 코드 수정 안 함) | :02 :17 :32 :47 · Q 짝수시 :00 |
| 4 | M `9 * * * *` · N `24 * * * *` · O `39 * * * *` · P `54 * * * *` | :09 :24 :39 :54 |
| 5 | R `14 * * * *` · S `29 * * * *` · T `44 * * * *` · U `59 * * * *` | :14 :29 :44 :59 |

- **이름**: `unity1 포지 이식 워커 X (:MM)` · **레포**: `https://github.com/kuzuni/unity1` · **모델**: `claude-fable-5-1`(한도 소진 시 `claude-opus-5` · **소넷 금지**) · **도구**: Bash, Read, Write, Edit, Glob, Grep, Task, WebFetch · **환경**: 그 계정의 Default · **`persist_session: true`**(실행마다 대화창을 새로 만들지 않는다 · 주인 지시).
- **프롬프트**: `docs/ROUTINES-SETUP.md` §4 의 블록을 그대로(`X` 만 바꾼다 · 네 계정의 프롬프트는 글자까지 같다).
- 만든 뒤 routine ID 와 첫 런 링크를 아래 ③ 표에 적어 커밋한다(`[skip ci]`).

### ③ 등록 기록 (각 계정 세션이 채운다)

| 워커 | 슬롯 | routine ID | 계정 | 첫 런 | 비고 |
|---|---|---|---|---|---|
| A | :05 | `trig_012jgYLoCrxjx93g81HEtfB6` | 계정 1 | 2026-09-12 18:05 UTC 예정 · 세션 https://claude.ai/code/session_015weaVBkodx5PzrfyHo1XEr · 루틴 https://claude.ai/code/routines/trig_012jgYLoCrxjx93g81HEtfB6 | 2026-09-12 17:44 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_014bNYWJnnxgzqfDN9JPBD6p` · enabled |
| B | :20 | `trig_01VgDxyoE699SFJH3dvNK8Mh` | 계정 1 | 2026-09-12 18:20 UTC 예정 · 세션 https://claude.ai/code/session_01Cw7XN9dSWNGb9USXPGiNN5 · 루틴 https://claude.ai/code/routines/trig_01VgDxyoE699SFJH3dvNK8Mh | 2026-09-12 17:44 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_014bNYWJnnxgzqfDN9JPBD6p` · enabled |
| C | :35 | `trig_01DRx8vYMXjWymP3ofAHDawL` | 계정 1 | 2026-09-12 18:35 UTC 예정 · 세션 https://claude.ai/code/session_01YaXyrwumgLb7Qh4dUDGojn · 루틴 https://claude.ai/code/routines/trig_01DRx8vYMXjWymP3ofAHDawL | 2026-09-12 17:44 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_014bNYWJnnxgzqfDN9JPBD6p` · enabled |
| D | :50 | `trig_014rJnPQ4DbKdorQWd2Xcn5o` | 계정 1 | 2026-09-12 17:50 UTC 예정 · 세션 https://claude.ai/code/session_01Pa7cTGCgeF1dfRiBWNi8Qt · 루틴 https://claude.ai/code/routines/trig_014rJnPQ4DbKdorQWd2Xcn5o | 2026-09-12 17:44 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_014bNYWJnnxgzqfDN9JPBD6p` · enabled |
| E | :12 | `trig_01TpK2L8uMb8kYNKe34gyPWu` | 계정 2 | 2026-09-12 18:12 UTC 첫 런(이 줄을 적은 회차) · 세션 https://claude.ai/code/session_01PgrXU84C5zXp1g6bCR7utx · 루틴 https://claude.ai/code/routines/trig_01TpK2L8uMb8kYNKe34gyPWu | 2026-09-12 17:53 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01JWPF8hM8XqtGYWuFnAsN93` · enabled · ID 는 워커 E 첫 회차(sess-1812-24752)가 `list_triggers` 로 확인해 적음 |
| F | :27 | `trig_01CXHFxMGv75VubqVr32USRH` | 계정 2 | 2026-09-12 18:27 UTC 예정 · 세션 https://claude.ai/code/session_01TKwVJkVng2hyTUwYcpCKob · 루틴 https://claude.ai/code/routines/trig_01CXHFxMGv75VubqVr32USRH | 2026-09-12 17:53 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01JWPF8hM8XqtGYWuFnAsN93` · enabled · ID 는 워커 E 첫 회차(sess-1812-24752)가 `list_triggers` 로 확인해 적음 |
| G | :42 | `trig_01RRyATu6xhqWBcYV1hJ36LJ` | 계정 2 | 2026-09-12 18:42 UTC 예정 · 세션 https://claude.ai/code/session_017szbKpgWAcM1YCrn27KyaH · 루틴 https://claude.ai/code/routines/trig_01RRyATu6xhqWBcYV1hJ36LJ | 2026-09-12 17:53 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01JWPF8hM8XqtGYWuFnAsN93` · enabled · ID 는 워커 E 첫 회차(sess-1812-24752)가 `list_triggers` 로 확인해 적음 |
| H | :57 | `trig_018TPgh9cbG7GjRX7SWJUzBK` | 계정 2 | 2026-09-12 17:52 UTC 첫 런(T2) · 세션 https://claude.ai/code/session_015KtwTwgC1Tt7CWrca3633j · 루틴 https://claude.ai/code/routines/trig_018TPgh9cbG7GjRX7SWJUzBK | 2026-09-12 17:53 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01JWPF8hM8XqtGYWuFnAsN93` · enabled · ID 는 워커 E 첫 회차(sess-1812-24752)가 `list_triggers` 로 확인해 적음 |
| I | :02 | `trig_018vJFGNoopJV5U8DcKLdRMd` | 계정 3 | 2026-09-12 18:02 UTC 예정 · 세션 https://claude.ai/code/session_01VuP4po4zaU2S1g3qXxvHcL · 루틴 https://claude.ai/code/routines/trig_018vJFGNoopJV5U8DcKLdRMd | 2026-09-12 17:55 UTC 생성(계정 3 착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled |
| J | :17 | `trig_01Wi4tU6pNfw88Yg7CjXpPCp` | 계정 3 | 2026-09-12 18:17 UTC 예정 · 세션 https://claude.ai/code/session_017YU8zAmyrBGAgAXAsUf5yB · 루틴 https://claude.ai/code/routines/trig_01Wi4tU6pNfw88Yg7CjXpPCp | 2026-09-12 17:55 UTC 생성(계정 3 착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled |
| K | :32 | `trig_0148B63LsU2pRov9y1yfivaj` | 계정 3 | 2026-09-12 18:32 UTC 예정 · 세션 https://claude.ai/code/session_0132fa6K2bvevQ9Jr63rRW9h · 루틴 https://claude.ai/code/routines/trig_0148B63LsU2pRov9y1yfivaj | 2026-09-12 17:55 UTC 생성(계정 3 착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled |
| L | :47 | `trig_012SuRjsZCV7QtkeSNF5L9Vk` | 계정 3 | 2026-09-12 18:47 UTC 예정 · 세션 https://claude.ai/code/session_017oQ6VCPffLFvz9NThbxyGh · 루틴 https://claude.ai/code/routines/trig_012SuRjsZCV7QtkeSNF5L9Vk | 2026-09-12 17:55 UTC 생성(계정 3 착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled |
| Q | 짝수시 :00 | `trig_01AkHocLEtxx7oBfcSo9eXFS` | 계정 3 | 2026-09-12 18:06 UTC 예정(서버가 최초로 잡은 시각) · 세션 https://claude.ai/code/session_011dnU6KpzeQdFqwGfdBSVAS · 루틴 https://claude.ai/code/routines/trig_01AkHocLEtxx7oBfcSo9eXFS | 검수 · 2026-09-12 17:55 UTC 생성(계정 3 착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled · cron 은 `0 0,2,4,6,8,10,12,14,16,18,20,22 * * *`(⚠ `0 */2 * * *` 로 만들면 서버가 «생성 분» 으로 앵커해 `55 */2 * * *` 가 된다 — 짝수시 :00 을 그대로 쓰려면 시(hour)를 나열한다 · 2026-09-12 실측) |
| M | :09 | `trig_01MdzGfq8mEdrutbMPDEygV9` | 계정 4 | 2026-09-12 18:09 UTC 첫 런(T3 선점 · sess-1810-3554) · 세션 https://claude.ai/code/session_01Ntvg8qcH25hBHUm2QGumic · 루틴 https://claude.ai/code/routines/trig_01MdzGfq8mEdrutbMPDEygV9 | 2026-09-12 17:55 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_016Xis527zoBbZPqrtAZVQ6x` · enabled · ID 는 워커 N 첫 회차(sess-1824-31207)가 `list_triggers` 로 확인해 적음 |
| N | :24 | `trig_01USZtkfikmcHyBWLPZ12FDZ` | 계정 4 | 2026-09-12 18:24 UTC 첫 런(이 줄을 적은 회차) · 세션 https://claude.ai/code/session_01BXsjJmqjNnZpFdUJQsyBtD · 루틴 https://claude.ai/code/routines/trig_01USZtkfikmcHyBWLPZ12FDZ | 2026-09-12 17:55 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_016Xis527zoBbZPqrtAZVQ6x` · enabled · ID 는 워커 N 첫 회차(sess-1824-31207)가 `list_triggers` 로 확인해 적음 |
| O | :39 | `trig_01UKii64CLP1z2ayvgQwTWtP` | 계정 4 | 2026-09-12 18:39 UTC 예정 · 세션 https://claude.ai/code/session_011szphzNiAkiJvfnHF6BaNM · 루틴 https://claude.ai/code/routines/trig_01UKii64CLP1z2ayvgQwTWtP | 2026-09-12 17:55 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_016Xis527zoBbZPqrtAZVQ6x` · enabled · ID 는 워커 N 첫 회차(sess-1824-31207)가 `list_triggers` 로 확인해 적음 |
| P | :54 | `trig_01G6BEu4XYkm4bRHKNyD4vVR` | 계정 4 | 2026-09-12 18:54 UTC 예정 · 세션 https://claude.ai/code/session_01KrYt1SMJHj1swZGrVGfiZV · 루틴 https://claude.ai/code/routines/trig_01G6BEu4XYkm4bRHKNyD4vVR | 2026-09-12 17:55 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_016Xis527zoBbZPqrtAZVQ6x` · enabled · ID 는 워커 N 첫 회차(sess-1824-31207)가 `list_triggers` 로 확인해 적음 |
| R | :14 | `trig_019JzVy5HrTCsMchZCqjPa6w` | 계정 5 | 2026-09-12 18:14 UTC 예정 · 세션 https://claude.ai/code/session_01PSGgjquRnHUgzjoqrfkQp7 · 루틴 https://claude.ai/code/routines/trig_019JzVy5HrTCsMchZCqjPa6w | 2026-09-12 17:58 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01Pzd5v3t1DrfBQqJkzqcXqC` · outcome 브랜치 main · enabled |
| S | :29 | `trig_01Ah2XwYKo3rf9ipLBzjS4vB` | 계정 5 | 2026-09-12 18:29 UTC 예정 · 세션 https://claude.ai/code/session_018Z11XwYJkkw28yddDbJGfR · 루틴 https://claude.ai/code/routines/trig_01Ah2XwYKo3rf9ipLBzjS4vB | 2026-09-12 17:58 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01Pzd5v3t1DrfBQqJkzqcXqC` · outcome 브랜치 main · enabled |
| T | :44 | `trig_018q9B6xpEpjnNRVNPQkCEQd` | 계정 5 | 2026-09-12 18:44 UTC 예정 · 세션 https://claude.ai/code/session_01RyT7Zs5oTFQuLtfQAZydQF · 루틴 https://claude.ai/code/routines/trig_018q9B6xpEpjnNRVNPQkCEQd | 2026-09-12 17:58 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01Pzd5v3t1DrfBQqJkzqcXqC` · outcome 브랜치 main · enabled |
| U | :59 | `trig_01DrEPHtF99HsLKH1C9wsKpJ` | 계정 5 | 2026-09-12 17:59 UTC 예정 · 세션 https://claude.ai/code/session_01DULNSnnTyRZfXtKSozLXv6 · 루틴 https://claude.ai/code/routines/trig_01DrEPHtF99HsLKH1C9wsKpJ | 2026-09-12 17:58 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_01Pzd5v3t1DrfBQqJkzqcXqC` · outcome 브랜치 main · enabled |

> ⚠ 이 루틴들은 MCP 커넥터 없이 뜬다(create_trigger 가 커넥터를 못 싣는다). 그래서 워커 세션에는 **GitHub MCP 가 없다** — `actions_run_trigger` 로 ntfy 를 못 쏜다. 알림은 Secret `NTFY_TOPIC` 을 넣어 **CI 완료 자동 알림**(`ntfy-notify.yml` workflow_run)으로 받는다. git push 는 프록시가 unity1 을 소스로 쥐고 있어 된다.

### ④ 규약은 계정과 무관하게 같다
- lock·SID·90분 규약은 그대로 — **다른 계정의 lock 도 남의 lock** 이다. SID 는 `sess-HHMM-$RANDOM` 이라 계정이 달라도 안 겹친다.
- push 충돌이 잦다: **push 실패 → `git fetch && git rebase origin/main` → `python3 tools/check_docs_intact.py` → 재push** 를 습관처럼. 리베이스 뒤 자기 lock 이 살아 있는지 본다.
- 어느 계정의 워커가 잡으면 안 되는 작업은 없다 — §2 «순서» 만 지킨다.
- 그 계정의 환경에서 `dotnet`·GitHub MCP 가 안 되면 «워커 결정 기록» 에 «계정 N 환경 차이: …» 한 줄을 남기고 돌릴 수 있는 게이트만 돌린 뒤, CI 가 초록을 확인해 줄 때까지 lock 을 쥔다.
- 한도 소진 징후(런이 15초 만에 FAILED 연속) → 그 계정 루틴 전부 `claude-opus-5` 로(job_config 를 통째로 다시 보낸다 — model 만 보내면 400). 풀리면 `claude-fable-5-1` 복원.

### ⑤ 검수 Q (계정 3 · 코드 수정 안 함)
- 회차마다: ⓐ main 의 최근 CI 런 3개(빨강이면 어느 커밋·누구·무엇) ⓑ 최근 ✅ 다섯 개가 «정말 도는가»(완료 기록의 확인 수단을 실제로 다시 돌린다) ⓒ 살아 있는 lock 의 나이 ⓓ `screens` 최신 PNG 를 원작 시트와 눈으로 대조. 발견은 PROGRESS «주인 콘솔 에러 보고함» 아래 «검수 Q 보고» 에 등재(«가장 큰 번호 +1» 로 작업화). 문서 커밋만(`[skip ci]`).
