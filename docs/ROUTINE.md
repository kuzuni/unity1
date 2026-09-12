# ROUTINE — 포지 클론 유니티 이식 (kuzuni/unity1) 작업 지시서

> **이 문서가 유일 지시서다.** 병렬 워커(루틴 세션)는 매 회차 이 문서 → `docs/PROGRESS.md` → `docs/claims/` 순서로 읽고 §0 절차대로 움직인다.
> 정본(스펙·수치·조형)은 **`kuzuni/wwwww` 의 `web/`**(HTML+Three.js 원작)이다. 이 레포는 그것을 **읽어** 유니티로 옮긴다 — wwwww 는 **읽기 전용**.
> 운영 틀은 `kuzuni/aaawunity`(꼬마기사 유니티 이식)의 `docs/ROUTINE.md` 규약을 그대로 옮긴 것이다(lock · SID · 90분 · T번호 · 결정 기록 · 게이트).

## ⚑ 신규 주인 지시 (위 항목이 최신 · 닫힌 것은 ✅ 를 단다)

- **(2026-09-12 · 주인 · 화면 셋)** «SafeArea 로 모바일 상단 카메라 안 가리게 · 60fps 로 돌아야 · **실제 게임 화면을 찍어서 봐라**» → §1 «실제 화면을 본다»·«SafeArea»·«60fps» 세 규칙 + T27(가장 먼저 · 노치 모의 촬영) · T44(60fps 게이트) · T45(SafeArea 노치 모의 검증). 테스트 초록만으로 ✅ 금지 — 워커가 PNG 를 열어 본 뒤 ✅.
- **(2026-09-12 · 주인 · 모델)** «루틴들 오퍼스로 해» → 워커·검수 루틴 모델 = **`claude-opus-5`**(§6 ②). 계정 2(E~H)는 20:14 UTC 에 · 계정 3(I~L·Q)은 20:40 UTC 에 바꿨다 · 계정 4 는 21:12 UTC 에 M 만 바꿨다(N·O·P 는 Opus 세션만 만들어 두고 루틴 묶기가 분류기에 막힘 · ③ 표 참조) · 계정 1·5 와 계정 4 의 N·O·P 는 그 계정의 다음 세션이 같은 방법으로 바꾼다(③ 표에 기록).
- **(2026-09-12 · 주인 · 완주)** «내가 더 말을 안 해도 루틴들이 원작을 유니티로 전체 빠짐없이 옮기게» → §7 «완결 정의 · 원작 ↔ 작업 대조표» 가 기준이다. 워커는 매 회차 §7 표에서 «없음» 인 줄을 보면 **작업으로 먼저 등재**하고, 검수 Q 는 ⑤ⓔ 로 원작 목록을 다시 훑는다. 마지막은 T33 완주 대조 — §7 전 줄 ✅ 가 «다 옮겨졌다» 의 뜻이다.
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
- **실제 화면을 본다(주인 지시 2026-09-12).** 화면·전투·연출·조형을 바꾼 작업은 판정에 **실제 게임 화면 PNG** 가 들어간다: PlayMode 촬영(`UiShots` · 540×1170 세로 · **노치 모의** = `safeArea` 위 120px·아래 60px 깎은 상태)이 CI `ui-screens/` → `screens` 브랜치에 올리고, 워커는 **그 PNG 를 `Read` 로 열어 눈으로 본 뒤**(글자 겹침 · 빈 칸 · 잘림 · 분홍 머티리얼 · 검은 화면 · 원작과 다른 배치) 완료 기록에 파일 이름과 «본 것» 한 줄을 적고 ✅ 한다. 테스트 초록만으로 ✅ 하면 검수 Q 가 되돌린다. `screens` 가 아직 안 올라오는 환경이면 `Assets/Tests/PlayMode` 촬영 테스트를 만들고 CI 런 뒤 다음 회차에 본다(lock 은 쥔 채).
- **SafeArea.** 모든 UI(HUD 상단바 · 탭바 · 팝업 닫기 ✕ · 채팅줄 · 토스트)는 `Screen.safeArea` **안**에 놓인다 — 폰 상단 카메라·노치·하단 홈바가 아무것도 가리지 않는다. 3D 세계는 화면 전체를 쓰되 «눌러야 하는 것» 은 safeArea 안. PlayMode 에 노치 모의 단언(T45)이 있고, 촬영도 노치 모의로 찍는다.
- **60fps.** `Application.targetFrameRate = 60` · `vSyncCount = 0`(WebGL 은 브라우저 rAF) · 전투 최대 부하(웨이브 4+보스 · 펫 3 · 스킬 오브젝트 · 히트 파티클 · 데미지 숫자)에서 **프레임당 GC 할당 0** · 복셀 메시는 몹당 하나로 병합(파츠마다 드로우콜 금지) · `Update` 에서 `Find`·`GetComponent`·문자열 연결 금지 · 프레임 예산 테스트(T44)가 CI 에서 메인스레드 시간을 잰다. 화면·전투 코드를 바꾼 작업은 완료 기록에 «부하 장면 평균 프레임 ms» 한 줄.
- **컴파일 파손을 남긴 채 다음 작업으로 넘어가지 않는다.** push 한 워커는 다음 회차에 «내 커밋을 담은 CI 유니티 잡이 컴파일을 지나 테스트를 실제로 돌렸는가» 를 먼저 본다 — «테스트 0개» 는 빨간 테스트보다 나쁘다. 컴파일이 깨져 있으면 그것이 그 회차의 첫 일이다(자기 lock 이든 남의 lock 이든).
- **유니티 «패키지» 타입을 새로 쓰면** 그 타입이 어느 어셈블리인지 확인해 `*.asmdef` 의 `references` 에 넣는다 — `tools/dotnet` 하니스는 URP·TMP 를 스텁으로 물어 **로컬에서 절대 안 걸린다**. 스텁(`tools/dotnet/Stubs`)에도 같은 서명을 더한다(추측 금지 — 실제 API 서명을 확인하고).
- `Assets/Scripts/Core` 에는 `UnityEngine` 을 참조하지 않는다(asmdef `noEngineReferences: true` · dotnet 이 강제). 엔진(전투 틱·대장간·펫·스킬 계산)은 전부 Core 다 — 유니티 없이 `dotnet test` 로 원작 JS 와 대조한다.
- **판단이 필요하면 기다리지 않고 스스로 정해 적용**하고 PROGRESS «워커 결정 기록» 에 «무엇을 · 왜 · 되돌리려면 어디» 한 줄을 남긴다. 번호는 커밋 «직전» 에 `python3 tools/check_decisions.py --next` 로 뽑는다. 겹치면 **늦게 push 한 쪽이 옮긴다**.
- **새 콘텐츠·새 시스템·밸런스 변경 금지.** 원작에 없는 것을 넣지 않는다. 원작 `web/TODO.md` 의 **미완(`[ ]`) 항목 7개도 «원작에 없는 것»** 이다 — 옮기지 않는다(그것은 wwwww 쪽 일). 옮기는 것은 원작이 **지금 하는 것** 전부다(§7 표).
- **에셋은 주인 에셋만**: UI `GUI PRO Kit - Casual Game` · 이펙트 `JMO Assets/Cartoon FX Remaster` · `DOTween` · `AllIn1SpriteShader` · 글꼴 `Assets/Fonts/NotoSans-Regular.ttf`. 3D 조형은 **코드 생성 복셀 메시**(T4)뿐이다 — 외부 모델·임시 그림 금지. 새 에셋을 쓰면 `docs/assets-map.md` 에 «용도 · 경로 · GUID» 한 줄.
- 승인 프롬프트가 뜨는 명령·대화형 편집기(`git rebase -i`) 금지. 캡처 PNG·대용량 바이너리 커밋 금지(예외: `screens` 브랜치는 CI 가 올린다 · **T31 의 아이콘 아틀라스**는 정본 `icongen.js` 에서 도구가 결정론으로 뽑은 것이라 `Assets/Forge/Icons/` 에 둔다 · 총 3MB 상한 · 손으로 안 고친다 · `tools/check_icons_sync.sh` 가 CI 에서 정본과 대조).
- **lock 은 «CI 가 그 커밋을 한 번은 돈 뒤» 반납한다.** 로컬 게이트는 PlayMode 를 못 돌리므로 «초록» 의 절반만 본 것이다.
  - **내 런이 `cancelled` 면**(실측 2026-09-12 18:49~19:05 런 9~15 전부 — ci.yml 의 `concurrency` 가 대기 중인 옛 런을 새 push 로 갈아치운다 · 빌드 잡 30분 때문) 그것은 실패가 아니다: main 은 선형이므로 **내 커밋 이후의 main 런이 초록이면 그것이 내 CI 확인**이다(그 런은 내 변경을 포함한다). 아무 런도 안 끝났으면 기다리지 말고 lock 을 쥔 채 종료하고 다음 회차에 본다. 그 뒤 런이 빨강이면 빨간 잡의 파일이 내 범위인지 본다 — 내 것이면 내 일, 아니면 그 임자 몫(«남의 lock 이 없는 빨강» 은 §0-6).
  - **유니티 잡이 라이선스로 빨강이면**(`no available seats` · `Unable to activate license` · 좌석 반납 실패 경고 뒤) 코드 탓이 아니다 — 재실행 1회(권한이 없으면 다음 push 를 기다린다) · 계속되면 «주인 콘솔 에러 보고함» 에 «유니티 라이선스 좌석» 한 줄 · lock 은 쥔 채 종료.
- 작업이 끝나면 lock 삭제 → PROGRESS 갱신 → 커밋 → push. **lock 만 잡는 커밋·문서만 바꾼 커밋은 제목 끝에 `[skip ci]`**. 커밋 메시지 **본문**에 그 표식을 인용하지 마라(GitHub 은 인용과 지시를 안 가린다).
- 브랜치는 `main` 하나다. 커밋 작성자는 `git -c user.name=kuzuni -c user.email=<그 계정의 이메일>`. 커밋 제목은 `T<번호> <무엇> (sess-… · 워커 X)` 꼴 — `check_claim_scope` 가 그 번호로 커밋을 센다.
- 문자열 `StartsWith`·`EndsWith`·`IndexOf(string)`·`Contains(string)`·`Compare` 에는 **`StringComparison.Ordinal`** 을 준다 — 문화권 비교는 유니티(Mono)와 dotnet(ICU)이 다르게 답한다(이모지 접두가 Mono 에선 항상 true · T36 실측). dotnet 초록이 유니티 초록을 보장하지 않는 자리다.
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

### T6 ✅ — 영웅: 박스 모델 캐릭터 + 무기 파지 + 대기/걷기/근접 스윙 애니 (Game · T4 뒤)
- 정본: `web/js/prochar.js`(머리·몸통·팔·다리 파츠 · 어깨 rx 한계 [−2.95, 2.35] · 근접 클립 = 예비동작/감기/정지/가속 타격/오버슈트/느린 회복 + 엉덩이·무릎) · `scene3d.js` 의 `applyWeaponGrip`(어깨 뼈 · anchorY = −(limbH − MC_GRIP_PULL) · `MC_CARRY_X` · 칼날 세우기 · `RANGED_SHAPES` 제외) · `WEAPON_GRIP` 표.
- 클립은 코드 애니(Animator 안 씀 · 원작이 프레임마다 각을 계산한다). 무기 8종 형상(`gamedata.js` 의 무기 모양 → 복셀 표)은 T15 가 잇는다 — 여기서는 «막대 한 자루» 로 파지각·타이밍만 맞춘다.
- PlayMode: 스윙 한 사이클의 어깨 각 곡선이 원작 키프레임(예비 → 타격 → 회복 시각 비)과 ±5% · 무기 로컬 회전 = (MC_CARRY_X, −π/2, 0).
- 범위: `Assets/Scripts/Game/Hero/` · `Assets/Tests/PlayMode/HeroTests.cs`.

### T7 ✅ — Core 전투 엔진: 100ms 고정 틱 · 웨이브 4+보스 · 스탯·치명·넉백·처치·스테이지 진행 (Core · T3 뒤)
- 정본: `web/js/combat.js`(587줄 · 전부) · `state.js` 의 전투 관련 상태 · `balance-data.js` 의 적 스탯 곡선.
- `Battle`(순수 C#): 입력 = 영웅 스탯·장비·펫 3·스킬 4·스테이지 → 틱마다 이벤트(공격·피격·처치·웨이브·보스·드랍). **원작 JS 와 판 단위 일치**: `tools/sim/`(node) 로 원작 `combat.js` 를 같은 시드로 돌려 처치 시각·드랍 목록을 JSON 으로 뽑고 EditMode 가 그것과 대조한다(시드 3개 × 100판).
- 범위: `Assets/Scripts/Core/Battle/` · `tools/sim/` · `Assets/Tests/EditMode/BattleTests.cs`.

### T8 ✅ — 전투 씬: 적 스폰·보행·공격·플린치·사망 · 데미지 숫자 · 카메라 셰이크 · 히트 파티클 (Game · T6·T7 뒤)
- 정본: `scene3d.js` 의 `monsterMesh`(어댑터 · 관절 계약) · `killEnemy`(파편색 = `shardC` 부피 가중 평균) · `spawnEnemy` 자리·간격 · 데미지 숫자 스타일 · 셰이크 진폭/감쇠 · `heroAttack` 타이밍(T6 클립과 같은 시각에 판정).
- 파티클은 `Cartoon FX Remaster` 프리팹만(카탈로그 키로 · `docs/assets-map.md`). Core `Battle` 이벤트를 구독해 그린다 — 씬이 규칙을 계산하지 않는다.
- PlayMode: 30초 자동 전투 · 콘솔 빨강 0 · 적 7종 전부 한 번씩 스폰·사망.
- 범위: `Assets/Scripts/Core/BattleFx/`(EnemyGait.cs · HitRules.cs) · `Assets/Scripts/Game/Battle/`(BattleScene · EnemyView · HeroView · HpBar · DamageNumbers · CubeParticles · BlobShadow · FxCatalog · FxMaterials · BareHeroStats) · `Assets/Forge/Resources/FxCatalog.asset` · `Assets/Plugins/WebGL/ForgeSignal.jslib` · `tools/battlefx_vectors.js` · `tools/gen_meta.py`(.jslib 갈래) · `Assets/Tests/EditMode/Vectors/t8-battlefx.json` · `Assets/Tests/EditMode/BattleFxTests.cs` · `Assets/Tests/PlayMode/BattleSceneTests.cs` · `docs/assets-map.md`(Cartoon FX 절).
- ✅ 2026-09-12 워커 F — 규칙은 Core `BattleFx`(정본 실행 벡터 대조) · 씬은 `Battle` 이벤트 구독 · 뺀 연출은 T39(보스 레갈리아·워닝·디졸브·림·플래시·트레일·블롭·암전).

### T9 ✅ — 맵·바이옴: 챕터 테마 10종 · 소품 배치(근경/중경 점유) · 지면·안개·광원 (Game · T4 뒤)
- ⚠ **정본 실측(2026-09-12 워커 I)**: `scene3d.js` 67행 `SIMPLE_BG: true`(주인 지시 2026-08-21 «배경 아예 단순하면 어떤 느낌인지») 가 배포값이라 **소품·스캐터·능선 3겹·구름·안개 블롭·하늘 돔이 전부 꺼져 있고 `heightAt` 은 0**(`buildProps` 조기 return · `buildTerrain`/`buildSky`/`heightAt` 분기). 화면에 있는 것은 «복셀 지면 타일(정점색: 포석·연석·흙 결) + 안개·단색 배경 + 광원 3 + 테마 25 파생색» 뿐이다. 이 작업은 그 보이는 것을 옮겼고(T9 완료 기록), 지면 소재(캔버스 텍스처·노멀·균열 발광맵·포석 줄눈 데칼·지면 셰이더)는 **T34**, `SIMPLE_BG=false` 로 되살아나는 경로(소품 17종 생성기·배치·스캐터·능선·구름·하늘)는 **T35** 로 갈랐다 — 테마는 «10종» 이 아니라 `CHAPTER_THEMES` 25종(gamedata.json)이다.
- 정본: `scene3d.js` 의 `setTheme`(테마 10종 색표 · 잎 색은 테마가 쥔다) · 소품 어댑터(`makePine`·`makeRoundTree`·`makeBoulder`… → `Props` 표) · 배치 규칙(`probe-nearfield-mass`·`probe-midground-depth` 게이트 값) · «역할 하나 = 메시 하나 = 드로우콜 하나».
- 소품 `Props.fitU(칸, 목표높이, 목표폭)` 치수 역산을 그대로. 자작나무 흰 기둥만 재질을 따로.
- PlayMode: 테마 10종 순회 · 드로우콜(`UnityStats.drawCalls`) ≤ 원작 한도 · 콘솔 빨강 0.
- 범위: `Assets/Scripts/Game/World/` · `Assets/Tests/PlayMode/WorldTests.cs`.

### T10 ✅ — 펫 출전 3마리: 대형(`PET_ROW0`·`PET_ARC`) · 따라오기 · 공격 참여 · 제너릭 관절 드라이버 (Game · T8 뒤)
- 정본: `scene3d.js` 의 `makePetMesh` 어댑터 · 펫 대형 상수 · 펫 관절 드라이버(`joints` 서술 · axis/amp/f/ph/gain/abs/spin) · `pets.js` 의 출전·스탯 기여.
- 범위: `Assets/Scripts/Game/Pets/` · `Assets/Tests/PlayMode/PetSceneTests.cs`.
- ✅ 2026-09-12 워커 I — 대열·자세·관절은 Core(`PetFormation`·`PetSceneRules`·`PetPose` · 정본 `formationSpot` 실행 벡터 + 펫 블록 식 대조) · 씬은 `PetParty`/`PetView`(관절 = `VoxelJoint.Set`) · 스탯 접착은 T43 · 승천 데코는 T37.

### T11 ✅ — 탈것: 서서 타기 · 안장 칸 · 비행/평지 hover · 탈것 드라이버(다리·날개·바퀴·스피너) (Game · T6·T10 뒤)
- 정본: `scene3d.js` 의 `makeMountMesh` · `RIDE_STAND_POSE` · `RIDE_STAND_BULK 1.45` · `MOUNT_FORMS`(fly hover 0.04 · 평지 0.10) · `rideSkirt(false)` · `mounts.js`.
- 범위: `Assets/Scripts/Game/Mounts/` · `Assets/Scripts/Core/Mounts/MountForms.cs` · `Assets/Scripts/Core/Mounts/MountRideRules.cs` · `Assets/Tests/EditMode/MountRideRulesTests.cs` · `Assets/Tests/PlayMode/MountSceneTests.cs` · `tools/export_data.js`(scene 갈래 T11 9키) · `Assets/StreamingAssets/data/scene.json`(추출기로만).
- ✅ 2026-09-12 워커 N — 서서 타기·안장 칸·hover·접지 보정·파츠 드라이버·무리 호는 Core(`MountForms`·`MountRideRules`·`MountPartDriver` · 정본 식 대조 EditMode 7) · 씬은 `MountRider`/`MountView`(PlayMode 2) · 영웅 접착은 LateUpdate 덧씌우기(결정 97) · 마구 없음(결정 98) · 스탯 `MountBonus` 는 T20(결정 99).

### T12 ✅ — 스킬 오브젝트: 로봇·표창×10·드래곤 등 실체가 나와서 때린다 (Game · T8 뒤)
- 정본: `web/js/mobs-skillfx.js`(오브젝트 표) · `scene3d-skillfx.js`(연출 계약 — 등장·이동·타격 시각·퇴장) · `skills.js` 의 18종 효과 종류(광역/단일/회복/버프).
- 이펙트 «빛 덩어리» 로 되돌리지 않는다(주인 지시). Core 스킬 판정(T17)과 시각의 타격 시각을 맞춘다.
- 범위: `Assets/Scripts/Game/SkillFx/`(FxTimeline · SkillActors · FxLights · FxCubes · SkillFxDirector · SkillFxScene) · `Assets/Scripts/Game/Battle/BattleScene.cs`(`EventHandled`·`Stepped` 훅 — 추가 줄만) · `Assets/Tests/PlayMode/SkillFxTests.cs`.

### T13 ✅ — 세이브·오프라인: JSON 세이브(persistentDataPath) · 30초 자동 · 절대시각 타이머 · 오프라인 보상 (Core+Game · T3 뒤)
- 정본: `state.js`(스키마 · 마이그레이션 · 오프라인 수급률 코인 1/초 · 해머 1/분 · 캡 4시간) · `main.js` 의 저장 시점.
- Core `SaveData`+`Offline` 순수 계산 · Game `SaveIo`. EditMode: 오프라인 4시간 캡 · 타이머가 절대시각으로 이어지는가.
- 범위: `Assets/Scripts/Core/Save/` · `Assets/Scripts/Game/SaveIo.cs` · `Assets/Tests/EditMode/SaveTests.cs` · `tools/export_data.js`·`tools/check_data_sync.sh`(state.json 추출 갈래) · `Assets/StreamingAssets/data/state.json`(추출기로만 · 결정 22) · `Assets/Scripts/Core/Data/MiniJson.cs`(`JsonObject.Remove` 한 줄).

### T14 ✅ — Core 대장간: 업그레이드 비용·시간(Lv1~35) · 시대 티어 확률(10시대) · 젬 스킵 · 오토 포지 (Core · T3 뒤)
- 정본: `forge.js` · `balance-data.js`(`forgeProbabilities` 등). 확률표 합 = 100 단언 · 원작 함수와 같은 입력 → 같은 결과(시드 고정) 표 테스트.
- 범위: `Assets/Scripts/Core/Forge/` · `Assets/Tests/EditMode/ForgeTests.cs`.

### T15 ✅ — 장비 8부위 + 페이퍼돌: 무기/투구/갑옷 3D 외형 · 등급 6 · 서브스탯 · 판매가 `20×1.01^(Lv−1)` (Core+Game · T6·T14 뒤)
- ✅ 2026-09-12 워커 S: Core 규칙(itemValue·itemPower·isMatchingGear·sellPrice·equip·sell·autoResolve·allSubsBag·heroStats) + 페이퍼돌 표값(무기 종·투구 스타일/이름·갑옷 색·발광·문장) + `Paperdoll`(무기 → T6 `HeroRig.Equip` · 훅). **3D 외형은 T37**(캡처 이식 · 결정 54).
- 정본: `forge.js`(장착·판매·스탯) · `gamedata.js`(장비명·모양) · `scene3d.js` 의 무기 복셀 표(`WEAPON_GRIP`·형상) · 투구·갑옷 부착.
- 범위: `Assets/Scripts/Core/Gear/` · `Assets/Scripts/Game/Hero/Paperdoll.cs` · `Assets/Tests/EditMode/GearTests.cs`.

### T16 ✅ — Core 펫 시스템: 알 드랍(1-1~10-10) · 부화 시간(30분~32시간) · 펫 25 스탯 · 중복 레벨업 · 합성 (Core · T3 뒤)
- 정본: `pets.js` · `balance-data.js`. 표 테스트: 드랍표 행 100개 합 · 부화 시간 표 · 합성 규칙.
- 대조 벡터: `tools/pet_vectors.js` 가 정본 `pets.js` 를 vm 으로 **실제로 돌려** `Assets/Tests/EditMode/pet_vectors.json` 을 뽑는다(mulberry32 시드 · 정본이 바뀌면 다시 뽑는다 · `--check` 로 같은지 본다).
- 범위: `Assets/Scripts/Core/Pets/` · `Assets/Tests/EditMode/PetTests.cs` · `Assets/Tests/EditMode/pet_vectors.json` · `tools/pet_vectors.js`.

### T17 ✅ — Core 스킬 시스템: 소환 확률(소환 Lv1~100) · 18종 · 3슬롯(정본 `MAX_ACTIVE` · 지시서의 4 는 오기) · 자동/수동 · 중복 레벨업 (Core · T7 뒤)
- 정본: `skills.js` · `combat.js` 의 스킬 판정 · `balance-data.js`.
- 범위: `Assets/Scripts/Core/Skills/` · `Assets/Tests/EditMode/SkillTests.cs`.

### T18 ✅ — UI 껍데기: 9:16 레터박스 캔버스 · SafeArea · `UiKit`(GUI PRO Kit 조각 스폰 · TMP NotoSans · TextKind 하한) · HUD · 하단 탭 (Game · T1 뒤)
- 정본: `web/index.html` · `css/style.css`(rem 스케일 · 레터박스) · `ui.js` 의 HUD/탭 구조 · 원작 스크린샷 `web/ref/screens/shot-*.png`(**Read 로 직접 본다**).
- 글자는 `UiKit.Text/Label/Button` 으로만 · `fontSize` 직접 금지(§1). 카탈로그(`Assets/Forge/catalog.json` → 용도 키 → GUI PRO Kit 경로)를 세우고 `docs/assets-map.md` 를 만든다.
- PlayMode: 부팅 → HUD · 콘솔 빨강 0 · `TextSizeGateTests`(모든 활성 Text 가 하한 이상).
- 범위: `Assets/Scripts/Game/Ui/` · `Assets/Forge/catalog.json` · `docs/assets-map.md` · `Assets/Tests/PlayMode/UiSmokeTests.cs`.

### T19 ✅ — UI 패널: 대장간 · 장비(8부위·장착·판매·x1/x10 제작) (Game · T14·T15·T18 뒤)
- 정본: `ui.js` 의 해당 패널 + `web/ref/screens/` 해당 샷. 배치는 원작 비율(±2%p).
- 옮길 것(원작 `ui.js` 함수 이름 그대로 — 하나라도 빠지면 T33 이 잡는다): 대장간 `renderForgeListView`·`renderForgeLevelView`·`renderForgeDetailView`·`openForgeList/Detail/Info`·`renderForgeInfo`·`closeForgeItemDetail` · 오토 포지 `openAutoForge`·`renderAutoForge`·`toggleAuto`·`showAutoDropCard` · 제작 `showCraftModal`·`showCraftBatch`·`showCraftReveal`·`buildCraftCard` · 장비 `renderEquipSheet`·`openGearDetail`·`renderGearDetail`·`showSellConfirm`. 아이콘은 T31 `UiIcons.Get` · 소리는 T30 `Sfx.Play`(anvilHit·craft·craftReveal·equipSnap·equipToss·equipDrop).
- 범위: `Assets/Scripts/Game/Ui/Forge*` · `Ui/Gear*` · `Assets/Tests/PlayMode/ForgeUiTests.cs`.

### T20 — UI 패널: 펫(알·부화·합성·출전) · 스킬(소환·장착) · 탈것 (Game · T16·T17·T18 뒤)
- 정본: `ui.js` + `index.html` 의 `panel-pets`·`panel-skills`·`panel-summon` + 해당 샷. 옮길 것: 펫 `renderPets`·`openPetDetail`·`openEggDetail`·`openPetUpgrade`·`renderPetUpgrade` · 스킬 `renderSkills`·`renderSkillBar`·`openSkillDetail` · 소환 `openSummonRates`·`renderSummonRates`·`openSummonResult`·`buildSummonReflection`(원작 소환 연출 그대로 · `Sfx` gacha·summonCharge·summonReveal) · 탈것 `openMounts`·`openMountDetail`·`openMountUpgrade`·`renderMountUpgrade`.
- 진행(2026-09-12 · 워커 B): 스킬·펫 서브탭 · 소환 결과 연출 · 확률 팝업 · 펫 상세/알 상세/업그레이드 · 세이브 코덱 ✅ — `renderSkillBar`(전투 HUD 스킬 바 · 2회차) ✅ — 탈것 화면은 Core 규칙(**T40**)이 서면 같은 lock 으로 잇는다.
- 범위: `Assets/Scripts/Game/Ui/Pet*` · `Ui/Skill*` · `Ui/Mount*` · `Assets/Scripts/Core/PetSave/PetSkillSave.cs` · `Assets/Forge/Resources/PetSkillUi.json`(T20 색·배치·문구표 · 결정 70) · `Assets/Tests/EditMode/PetSkillSaveTests.cs` · `Assets/Tests/PlayMode/PetUiTests.cs`.

### T21 ✅ — UI 패널: 던전 4종 · 기술트리 · 승천 (Game · T23·T24·T18 뒤)
- 정본: `ui.js` + `index.html` 의 `panel-tech` + 해당 샷. 옮길 것: 던전 `openDungeons`·`openDungeonDetail`·`renderDungeonDetail`·`showDungeonClear`(실패 화면 포함 · `shot-dungeon-fail`) · 기술트리 `openTechTree`·`renderTechTree`·`openTechOverview`·`openTechBranch`·`renderTechBranchView`·`openTechNode`·`renderTechNodeModal`·`openTechBonuses`·`drawTechLinks`(가지 선 그리기) · 승천 `openAscension`·`closeAscension`.
- 범위: `Assets/Scripts/Game/Ui/Dungeon*` · `Ui/Tech*` · `Ui/Ascend*` · `Assets/Tests/PlayMode/DungeonUiTests.cs` · `Assets/Forge/catalog.json`+`Resources/UiCatalog.asset`(색·배치 키 추가).
- 실측 메모(워커 N): 실패 «화면» 은 원작에 없다 — onFail 토스트 + 전투 사망 암전(T8). 서브탭 줄(스킬·펫·기술 트리)은 T20 이 세우고 `TechPanel.Show/Hide` 를 부른다.

### T22 ✅ — UI 패널: 상점 · 패스 · 퀘스트 · 리그 · 채팅 · 메뉴·프로필·설정·디버그 (Game · T25·T18 뒤)
- 정본: `ui.js` + 해당 샷(`shot-shop`·`shot-pass`·`shot-league`·`shot-chatcam`·`shot-pinfo`·`shot-avatars`). 옮길 것: 상점 `openShop`·`renderShop` · 패스 `openPass`·`renderPass` · 퀘스트 `openQuests` · 리그 `openLeague`·`renderLeagueBoard`·`openLeagueChallenge`·`renderLeagueChallenge`·`openLeagueRewards`·`openNextAutoMatch` · 채팅 `openChat`·`renderChatList`·`renderChatFull`·`renderChatPreview`(아바타 = T31 `IconGen.avatar` 24종) · 메뉴 `renderMenu` · 프로필 `openProfile`·`renderProfile`·`renderProfileView`·`openPlayerInfo`·`renderPlayerInfo`(아바타 선택 · 성별 `S.gender` · 이름) · 설정 `renderSettingsView`(음악·효과음 토글 = T30 `toggleMusic`) · 디버그 `renderDebug`(panel-debug · 재화·스테이지 치트 그대로 · T27 봇이 쓴다) · 공용 `showModal`·`openStub`·`closeOpened`·`closeAllTabSurfaces`. 오프라인 모달 `showOffline` 은 T13 과 짝(여기서 화면만).
- 범위: `Assets/Scripts/Game/Ui/`(ShopSheet.cs · QuestSheet.cs · PassPopup.cs · LeagueSheet.cs · ChatScreen.cs · ProfilePopup.cs(설정 포함) · PlayerInfoPopup.cs · OfflinePopup.cs · DebugPanel.cs · Popups.cs(공용 모달 층·스텁·토스트) · MetaHost.cs(T25 Core ↔ 세이브·HUD·탭 접착) · TabBar.cs(팝업 탭 ✕ SetPopupX 한 줄)) · `Assets/Forge/catalog.json`+`Assets/Forge/Resources/UiCatalog.asset`(색 70·배치 102·스프라이트 20 추가) · `Assets/Scripts/Core/MetaSave/MetaSave.cs`(T25 상태 ↔ 세이브 트리 코덱 · 순수) · `Assets/Tests/EditMode/MetaSaveTests.cs` · `Assets/Tests/PlayMode/ShopUiTests.cs` · `tools/dotnet/Stubs/TMPro.cs`(TMP_InputField onSubmit·textViewport).

### T23 ✅ — Core 던전 4종: 입장·소탕·보상 (Core · T7 뒤)
- 정본: `dungeons.js`. 범위: `Assets/Scripts/Core/Dungeons/` · `Assets/Tests/EditMode/DungeonTests.cs` · `Assets/Tests/EditMode/Vectors/t23-dungeons.json`(정본 실행 벡터) · `tools/dungeon_vectors.js`(벡터 생성기 · 정본을 node vm 에 올린다).

### T24 ✅ — Core 기술트리 · 승천 (Core · T3 뒤)
- 정본: `techtree.js` · `ascension.js`. 범위: `Assets/Scripts/Core/Tech/` · `Core/Ascension/` · `Assets/Tests/EditMode/TechTests.cs` · `tools/export_data.js`(표 칸 추출 갈래 `TECH_FIELDS`) · `tools/check_data_sync.sh` · `Assets/StreamingAssets/data/tech.json`.
- 표(분기·노드·보너스·배수·승천 라인·별 배율)는 `tech.json`(추출기 파일 · `TechData.Load`)에서 온다 — `GameData` 의 7파일과 별도. 규칙은 `TechTree`·`Ascension`(Core) 이 원작 함수 이름 그대로 든다.

### T25 ✅ — Core 상점 · 패스 · 퀘스트 · 리그 (Core · T3 뒤)
- 정본: `shop.js` · `pass.js` · `quests.js` · `league.js` · `chat.js`(채팅은 표시용 문자열 표만). 범위: `Assets/Scripts/Core/Meta/` · `Assets/Tests/EditMode/MetaTests.cs`.

### T26 ✅ — WebGL 빌드 · gh-pages 배포 · 배포 스모크(headless 로 열어 콘솔 에러 0 · 전투 진입) · Android APK (배포 · T18 뒤)
- `ci.yml` 의 `build-webgl` 은 이미 있다(UNITY_LICENSE 가 있어야 굽는다 — README «주인이 할 일»). 여기서 하는 것: WebGL 템플릿(캔버스가 창을 채우는 세로 껍데기 · 로딩 완료 표식) · `tools/webgl_smoke.js`(Playwright · 컨테이너에서는 `kuzuni.github.io` 가 프록시에 막히므로 CI 러너에서 돈다) · Android 잡.
- 범위: `Assets/WebGLTemplates/` · `tools/webgl_smoke.js` · `.github/workflows/ci.yml`(빌드 잡 부분만) · `ProjectSettings/ProjectSettings.asset`(webGLTemplate · 압축 폴백 · Android 식별자 칸만 — 템플릿을 «쓰게» 하는 칸이 거기 있다).

### T27 — PlayMode 스모크·플레이 봇: 모든 화면 열기 · 한 판 플레이(전투→제작→장착→펫→스킬→던전) · 콘솔 빨강 0 · 촬영(`ui-screens/*.png`) (검증 · T19~T22 뒤)
- 🔄 2026-09-12 워커 L — 정본은 `web/tools/shot-screens.js`(화면 31줄 = 원작 `ref/screens/shot-*.png` 30장 짝 + 짝 없는 탈것 · `SEED` 캡처 상태). `PlayLog`(빨강 수집기 · 화면 이름) · `UiShotsTests`(31 화면 열기 + `ui-screens/screen_<이름>.png` + 짝 표 `screens.json`) · `PlaythroughTests`(한 판 봇). T28 은 `screens.json` 에서 짝을 읽는다.
- 정본 각본: wwwww `web/ROUTINES-SETUP.md` §4-C(QA 시나리오). `PlayLog.AssertNoRed` 도우미 · `UiShotsTests`(540×1170 PNG 전 화면) → CI `screens` 브랜치.
- **(주인 지시 2026-09-12 · 가장 먼저)** 촬영은 **실제 게임 화면**이다 — 조형 시트가 아니라 부팅 직후 전투 화면(적·펫·영웅·HUD 가 한 프레임에), 웨이브 진행·보스 등장·스킬 발동 순간, 그리고 패널 전부(대장간·장비·펫·스킬·소환 결과·탈것·던전·기술트리·승천·상점·패스·퀘스트·리그·채팅·프로필·설정). 파일 이름은 원작 샷과 짝이 되게(`shot-042120` ↔ `ui-042120-battle.png` 꼴 · `docs/ref-layout.md` 가 짝 표를 쥔다). 해상도 셋: 540×1170(기본 · 노치 모의) · 360×800 · 430×932(원작 `ref/lvout-*` 과 같은 셋). 노치 모의는 `safeArea` 를 위 120px·아래 60px 깎아 넣고 그 경계선을 반투명 빨강으로 PNG 에 그려 «가림» 이 눈에 보이게. 각 PNG 는 만든 워커가 `Read` 로 열어 본다(§1).
- 판정에 «주인이 볼 것: `screens` 브랜치의 `ui-*-battle.png` 를 폰 화면과 나란히» 한 줄.
- 범위: `Assets/Tests/PlayMode/PlaythroughTests.cs` · `UiShotsTests.cs` · `PlayLog.cs`.

### T28 — 원작 대조 회차: `screens` PNG ↔ `web/ref/screens/shot-*.png` 비율 대조표(`docs/ref-layout.md`) + `tools/ui_score.py` (검증 · T27 뒤)
- aaawunity §5 방식: 화면마다 «요소 · x% · y% · w% · h%» 표를 원작 샷에서 5% 격자로 판독 → 우리 PNG 와 ±3%p 대조 → 점수. **8.0 미만이면 그 화면의 UI 작업을 «다음 고칠 것» 으로 재등재**.
- 범위: `docs/ref-layout.md` · `tools/ui_score.py`.

### T29 ✅ — `task_state.py` «코드 자취» 오탐: 주석의 미래 참조(`T7 이 쓴다`)를 자취로 세어 T7·T13·T14·T25 선점을 막는다 (도구 · 뒤 순서 없음)
- 실측(2026-09-12 워커 K · 워커 M 결정 11ⓔ 도 같은 것): `Rng.cs`·`Hud.cs` 의 `///` 주석이 «T7 전투 · T13 세이브» 를 앞으로 가리키고, `check_claim_scope.py`·`task_state.py` 의 자기 검사 픽스처 문자열이 T14·T25 를 담아 `task_state.py T7` 등이 rc 1 을 낸다 → 규약대로면 아무도 못 잡는다.
- 고침: 자취 검사에서 **주석 줄(`//` · `///` · `#` · 문자열 리터럴)과 `tools/` 의 자기 검사 픽스처를 제외**하고 코드 식별자·파일명·폴더명만 센다. 자기 검사(`--self-test`)에 «주석만 가리키는 번호는 깨끗하다» 케이스를 더한다.
- 범위: `tools/task_state.py`.

### T30 ✅ — 사운드 이식: 효과음 24종(+합성 프리미티브 6 · 원문 «29종» 은 셈 차이 · 결정 57ⓖ) + 음악 4모드·레이어 6종 — 원작 `sfx.js` 의 **코드 합성**을 그대로 (Core+Game · T3 뒤)
- 정본: `web/js/sfx.js`(618줄 · 전부 · 외부 파일 0 — WebAudio 프로시저럴). 효과음: `anvilHit arrowShot auraRise bossSiren craft craftReveal equipDrop equipSnap equipToss gacha healDescend hit levelUp mawBite mawRoar slashArc stormCrackle stormRumble stormStrike summonCharge summonReveal voidPierce voidSnap voidTear`(호출마다 피치 랜덤화 · 레이어드 합성 = 어택 트랜지언트+바디+저역 텀프+테일). 음악: 모드 4종(`normal/boss/dungeon/shop`) · 레이어 6종(서브베이스·베이스·디튠 패드·아르페지오·멜로디·퍼커션) · 스윙·박 위계 다이내믹 · 템포 동기 딜레이/합성 리버브. 버스: master(SFX)·musicGain(BGM) → 소프트 리미터 → 출력 · 합성 IR 리버브 센드 공용. 공개 표면: `startMusic setMusicMode toggleMusic musicEnabled resume`.
- 원작이 «외부 파일 금지 · 전부 코드 합성» 이니 유니티도 **오디오 파일을 들이지 않는다**: `Assets/Scripts/Core/Audio/`(순수 C# DSP · 오실레이터·엔벨로프·노이즈·IR 합성·시퀀서 → `float[]` 샘플 · `dotnet test` 로 길이·피크·무음 아님·결정론(시드) 검증) + `Assets/Scripts/Game/Audio/`(`AudioClip.Create` 로 클립화 · 캐시 · `AudioSource` 버스 2개 + 리미터는 유니티 `AudioMixer` 없이 Core 리미터를 샘플에 미리 적용 · `Sfx.Play(name)`·`Music.SetMode(mode)` 표면 이름을 원작과 같게). 주파수·엔벨로프·BPM·음계·모드별 곡 데이터 같은 **수치는 코드에 박지 않고** `Assets/StreamingAssets/data/sfx.json` 으로(T2 추출기에 `sfx.js` 갈래를 더한다 · 표로 못 뽑는 함수 안 상수는 결정 기록에 사유를 적고 Core 상수 파일 하나에 모은다).
- 호출 지점(전투 타격·대장간 망치·소환·장비 장착·보스 사이렌·모드 전환)은 그 화면·전투 작업(T8·T19·T20·T21)이 붙인다 — 이 작업은 **모듈 + 전 효과음 재생 PlayMode 스모크** 까지. 이미 끝난 작업의 호출 지점은 이 작업이 붙인다.
- 판정: EditMode DSP 테스트(효과음 24종 · 모드 4종 각 8박 렌더 → 길이·피크 ≤ 1.0·RMS > 0·같은 시드면 같은 샘플 · 정본 합성 그래프 벡터 대조) + PlayMode(효과음 24종 전부 `Play` · 모드 4종 전환 · 콘솔 빨강 0) + CI 초록 + PROGRESS 행 + «주인이 확인할 것: 에디터 Play → 망치 소리·전투 타격음·상점 음악 전환이 원작(web 에서 재생)과 같은 인상인가».
- 범위: `Assets/Scripts/Core/Audio/` · `Assets/Scripts/Game/Audio/` · `Assets/StreamingAssets/data/sfx.json` · `tools/export_data.js`(sfx 갈래만) · `tools/check_data_sync.sh`(FILES 에 sfx.json 한 단어) · `tools/sfx_vectors.js`(정본 sfx.js 를 vm 에 올려 합성 호출 벡터를 찍는 도구) · `Assets/Tests/EditMode/Vectors/t30-sfx.json` · `Assets/Tests/EditMode/AudioTests.cs` · `Assets/Tests/PlayMode/AudioSmokeTests.cs`.

### T31 ✅ — 아이콘·아바타 이식: `icongen.js` 아이콘 136종 + `avatars.js` 아바타 24종(= `IconGen.draw` 키 160 · «523» 은 그리기 도우미까지 센 수 · 실측 2026-09-12) — 정본을 **래스터로 뽑아** 유니티가 받는다 (도구+Game · T2·T18 뒤 · T19~T22 는 이것을 쓴다)
- 정본: `web/js/icongen.js`(6,704줄 · 캔버스 2D 벡터 그리기 · 장비·펫·스킬·재화·탭·등급 왕관·성별 등 523 그리기 함수 · 표면 `IconGen.img(key)`·`url`·`skill`·`tab`·`cls`)· `web/js/avatars.js`(831줄 · 도트 초상 24종 · `IconGen.avatar`). `ui.js` 가 `IconGen.img` 를 146곳에서 부른다 — 원작 화면의 «그림» 은 전부 여기서 나온다.
- 7,500줄 캔버스 코드를 C# 으로 다시 그리지 않는다. **T2 와 같은 원칙**(정본이 단독으로 쥔다 · 유니티는 받아 세운다): `tools/export_icons.js` 가 Playwright Chromium(컨테이너·CI 에 있다 · T26 `webgl_smoke.js` 가 쓴다) 으로 `icongen.js`·`avatars.js` 를 진짜 캔버스에 올려 키 전부를 그리고 → `Assets/Forge/Icons/atlas-N.png`(2048² 이하 · 아틀라스 몇 장) + `atlas.json`(키 → 장·rect·원본 px) 로 낸다. 결정론(같은 정본 → 같은 바이트)이어야 `tools/check_icons_sync.sh` 가 T2 `check_data_sync.sh` 처럼 CI 에서 «정본과 같은가» 를 볼 수 있다(그 잡 한 줄 추가). PNG 총량 상한 3MB(넘으면 해상도 단을 낮춘다 · 캡처 PNG 금지 규칙의 예외로 «정본에서 기계로 뽑은 아틀라스» 를 §1 에 한 줄 적는다).
- Game: `UiIcons.Get(key)` → `Sprite`(아틀라스 슬라이스 · Resources 또는 카탈로그 GUID 참조 · T18 `UiCatalog` 와 같은 길) · `UiKit.Icon(key)` 이 이것을 쓰도록. 키 이름은 원작 문자열 그대로. 아바타 24종은 채팅·리그(T22)가 쓴다.
- 판정: `node tools/export_icons.js --self-test`(키 160(아이콘 136+아바타 24)+tint 변형 전부 그려짐 · 빈 캔버스 0) + 동기화 검사 초록 + PlayMode(키 전부 `Get` → null 0 · 콘솔 빨강 0) + CI 초록 + PROGRESS 행 + «주인이 확인할 것: 에디터에서 아틀라스를 열어 원작 아이콘 그대로인가».
- 범위: `tools/export_icons.js` · `tools/check_icons_sync.sh` · `Assets/Forge/Icons/` · `Assets/Scripts/Game/Ui/UiIcons.cs` · `Assets/Scripts/Game/Ui/UiKit.cs`(Icon 한 갈래만) · `.github/workflows/ci.yml`(datasync 잡의 아이콘 검사 한 줄만) · `Assets/Tests/PlayMode/UiIconsTests.cs`.

### T32 ✅ — CI concurrency: 긴 런이 도는 동안 뒤 push 의 CI 가 «대기 런 교체» 로 취소돼 워커 커밋이 검증 없이 지나간다 (배포·검증 · 뒤 순서 없음)
- 실측(2026-09-12 워커 H): 런 8(18:42 시작 · 긴 런)이 도는 동안 런 9(T4)·10(T16)·11(T14) 이 «cancelled». `concurrency.group: ci-${{ github.ref }}` 에 `cancel-in-progress: false` 여도 GitHub 은 같은 그룹의 **대기 중 런을 하나만** 두고 새 런이 오면 옛 대기 런을 취소한다. 그래서 «lock 은 CI 가 그 커밋을 한 번 돈 뒤 반납»(§1) 을 지킬 수 없고, `ntfy-notify.yml` 은 «CI 빨강(cancelled)» 을 쏜다.
- 고침: ⓐ 빠른 잡(dotnet · datasync · gate)은 push 마다 반드시 돌게 concurrency 를 잡별로 나눈다(예: 빠른 잡은 `ci-fast-${{ github.sha }}` · 긴 잡 unity-test·build-webgl·android 만 `ci-heavy-${{ github.ref }}` 직렬) ⓑ ntfy 자동 갈래는 `conclusion == cancelled` 를 쏘지 않는다.
- 판정: 연속 push 3개가 전부 dotnet 잡을 실제로 돌린 런 번호를 남긴다 · PROGRESS 행.
- 범위: `.github/workflows/ci.yml`(concurrency · 잡별 그룹) · `.github/workflows/ntfy-notify.yml`(cancelled 필터).
- (2026-09-12 계정 2 대화 세션 · 주인 «가장 먼저» 로 올림) 실측: 런 9~15 전부 cancelled · 원인은 굽기 잡(WebGL·Android 20~40분)이 push 마다 도는 것. 가장 싼 고침: `build-webgl`·`build-android` 의 `if` 를 `github.event_name == 'schedule' || inputs.build == true` 로 두고 `on.schedule: cron '0 */3 * * *'`(main 최신을 굽는다) · 굽기 잡은 별도 `concurrency` 그룹(`build-main`) · `unity-test` 는 push 마다 그대로(7분). 취소가 남는 동안의 lock 반납은 §1 «내 뒤 런이 초록이면 내 확인» 규칙으로.

### T33 — 완주 대조: §7 표의 모든 줄이 ✅ 이고 원작 화면 30장·`ui.js` 공개 함수 97개·`SFX` 24종(`SfxRecipes.Names`)·`IconGen` 키가 유니티에 다 있는가 (검증 · T27·T28·T30·T31 뒤 · **마지막**)
- 방법: ⓐ §7 표를 위에서 아래로 — 줄마다 «유니티의 어느 파일·테스트가 그것인가» 를 적는다(없으면 «가장 큰 번호 +1» 로 등재하고 그 줄을 그 번호로 바꾼다) ⓑ `web/ref/screens/shot-*.png` 30장 각각에 유니티 `ui-screens/*.png` 짝이 있는가(T28 대조표) ⓒ `grep -o "^\s*\(open\|render\|show\|toggle\|close\|build\)[A-Z][A-Za-z]*" .wwwww-src/web/js/ui.js` 의 함수 하나하나에 유니티 대응(같은 이름의 메서드·화면)이 있는가 ⓓ `SFX.*` 24종 · `IconGen.img/avatar/skill/tab` 키가 `Sfx.Play`·`UiIcons.Get` 로 다 불리는가 ⓔ 원작을 한 판(전투→제작→장착→펫→스킬→던전→상점→리그→채팅) 하고 유니티(T27 봇 + WebGL 배포본)로 같은 판을 해 **다른 곳을 전부 적는다**.
- 판정: 빠진 것 0 이 될 때까지 이 작업은 ✅ 가 아니다 — 빠진 것을 등재하고 «그 번호들 뒤» 로 자기 순서를 고쳐 lock 을 반납한다(다음 회차가 다시 잡는다). 전부 ✅ 면 §7 표 머리에 «완주 YYYY-MM-DD · 커밋» 을 적고 ✅.
- 범위: `docs/ROUTINE.md`(§7 표) · `docs/PROGRESS.md` · `docs/parity.md`(대조 결과).
### T34 ✅ — 지면 소재 굽기: `makeGroundTexture`/`makeGroundNormalMap`(kin 6 × 512 캔버스 레시피 + BIOMES tint) · 용암 균열 발광맵(`crackNetwork`·`makeCrackTexture`) · 포석 줄눈 데칼 (Game · T9 뒤) — 지면 셰이더(`terrainShade`·`applyShadeLift`)는 **T38** 로 갈랐다
- 정본: `scene3d.js` `makeGroundTexture`(2135~) · `makeGroundNormalMap`(2443~) · `crackNetwork`/`strokeCrackNet`/`makeCrackTexture`(2600~2731) · `buildTerrain` 의 포석 줄눈 캔버스 블록(2905~2985) · `terrainShade`(3650~3735) · `applyShadeLift`(1323~) · `setTheme` 의 `uRoad`/`uSnow`/`emissiveMap` 줄. T9 는 재질색·정점색(노면 회랑은 `uRoad` 배율을 정점색에 곱함)·발광 색표까지만 옮겼고 텍스처·셰이더는 여기다.
- 캔버스 그리기는 `Math.random` 을 쓰므로 T2 표본과 같은 xorshift32 시드로 node(캔버스 대신 순수 픽셀 버퍼)에서 뽑은 PNG 와 C# 베이크가 ±1/255 로 같아야 한다. 셰이더는 URP 커스텀(Particles/Lit 위에 macro·LOD·snow·road·shadeLift 4항).
- 판정: EditMode(텍스처 6×2 + 균열맵 픽셀 대조) + PlayMode(테마 25 순회 · 콘솔 빨강 0) + CI 초록 + PROGRESS 행 + «주인이 확인할 것: 에디터 Play → 초원 흙 결·사막 리플·용암 균열 발광이 원작(web)과 같은 인상인가».
- 대조(2026-09-12 워커 K): 브라우저 캔버스 대신 **픽셀 버퍼 캔버스 2D 시밍**(`tools/ground_vectors.js` 의 `Context2D` ↔ Core `GroundTexCanvas` · 같은 래스터 규칙)을 두고 정본 그리기 함수를 vm 에서 **그대로** 실행해 벡터를 뽑는다 → C# 굽기가 26장(알베도 12 · 노멀 12 · 발광 · 포석) 전부 **바이트 해시까지** 같다. 시드는 T2 표본과 같은 xorshift32 `LibSeed`.
- 범위: `Assets/Scripts/Core/World/GroundTexCanvas.cs` · `Assets/Scripts/Core/World/GroundTexBake.cs` · `Assets/Scripts/Game/World/GroundTextures.cs` · `Assets/Scripts/Game/World/World.cs`(훅 2줄) · `Assets/Tests/EditMode/GroundTexTests.cs` · `Assets/Tests/EditMode/Vectors/t34-ground.json` · `Assets/Tests/PlayMode/WorldTests.cs`(렌더러 2 · 텍스처 단언) · `tools/ground_vectors.js`.

### T35 — 배경 복원 경로(`SIMPLE_BG=false`): 소품 생성기 17종 C# 이식(`mobs-props.js` → T2 표본 23개와 같은 시드 대조) · `buildProps` 배치(큰 소품 16자리 + 랜드마크 + 중경 8 + 덤불 7·잔돌 7/11·꽃 2 + 근경 앵커 6 + 중경 앵커 7 + 원거리 11/14) · 스캐터 InstancedMesh 3층(`scatterSpots` 깊이 가중·군집) · 능선 3겹(`makeRidgeGeo`) · 구름·안개 블롭·하늘 돔·천체·오로라 · 잎/덤불/이끼/블롭/결정 파생색(`leafSet`·`stoneFrom` 나머지) · 지형 높이(`heightSmooth` 양자화 벽) (Game · T9·T4 뒤 · **주인이 SIMPLE_BG 를 끌 때만**)
- 정본은 지금 `SIMPLE_BG: true` 라 이 경로가 **화면에 없다**(T9 메모). 주인이 «배경 아예 단순» 실험(2026-08-21)을 되돌리면 그때 켠다 — 그 전에는 뒤 순서가 안 열린 것으로 본다(선점하지 않는다). `GroundGrid`(T9)는 `SimpleBg=false` 면 이미 높이·벽을 낸다.
- 범위: `Assets/Scripts/Core/World/Props*.cs`·`Scatter*.cs`·`Ridge*.cs` · `Assets/Scripts/Game/World/Props*.cs`·`Scatter*.cs`·`Ridges*.cs`·`Sky*.cs` · `Assets/Tests/EditMode/PropsTests.cs` · `Assets/Tests/PlayMode/PropsSceneTests.cs`.

### T36 ⛔ — Unity(Mono) 에서만 빨간 EditMode 테스트: 문화권 비교 `StartsWith("🥚")` (검증 · 뒤 순서 없음)
- 실측(2026-09-12 워커 M): CI 런 19(c0086ff) unity-test 가 EditMode 1개 실패 — `PetTests.tick_부화_완료_뒤에서부터_종_균등_옵션2_자동출전3_별`(PetTests.cs:360) «하나 더 #0 Expected True But was False». 같은 테스트가 dotnet 에서는 초록(런 19 dotnet 잡 303 통과).
- 원인: `string.StartsWith(string)` 은 **현재 문화권 비교**다. 유니티(Mono 관리 collation)는 비BMP 이모지(🥚 = 서로게이트 쌍)를 무게 0 으로 봐 어느 문자열에도 true 를 주고, dotnet(ICU)은 제대로 false 를 준다. 그래서 기대값이 뒤집혀 실제 값(false)과 어긋났다.
- 고침: PetTests 의 `StartsWith` 3곳(360 · 376 «새 펫: » · 411)에 `StringComparison.Ordinal`. §1 에 규칙 한 줄(문자열 접두·접미·포함 비교는 Ordinal).
- ⛔ 흡수: 워커 H 가 34ad307(T16 수리)로 먼저 고쳤다 — 이 절이 남긴 것은 §1 규칙 한 줄.
- 범위: `Assets/Tests/EditMode/PetTests.cs` · `docs/ROUTINE.md`(§1 한 줄).

### T37 ✅ — 장비 3D 외형 캡처 이식: `makeWeapon`(무기 52종 · 시대·등급 재질) · `makeHelmet`(스타일별) · `makeArmorExtras`+`dressMcRig`(옷 한 벌 · 관절 본 부착) · `gradeHeroGearValue` · `applyAscendDecor` — 정본을 **실물 three 로 돌려** 메시(정점·색·재질)를 JSON 으로 뽑고 `Paperdoll.WeaponMeshProvider`/`OnDressed` 훅에 꽂는다 (Game · T15·T6 뒤)
- 정본: `scene3d.js` 4768~5490(`makeWeapon` · `weaponMatKind` 재질 계열 · `WEAPON_HAFT_SHAPES`) · 6358~6600(`ageGearMats`/`ageGearMatsBase`) · 7236~7430(`makeHelmet`) · 7583~8136(`makeArmorExtras` · `MC_ERA_SHAPE` · `mcClothMat` · `dressMcRig`) · 521(`gradeHeroGearValue` · `HERO_VALUE_GRADE`) · `prochar.js` `leatherTex`(캔버스 텍스처 — 캡처 때 평균색으로 접거나 픽셀을 같이 뽑는다 · T31 이 캔버스를 Chromium 으로 뽑은 방식도 된다).
- 방법: T6 `hero_vectors.js`·T4 `voxel_vectors.js` 처럼 `three.min.js`(r128 실물) 위에서 정본 절만 잘라 실행 → 모델(무기 종 × 등장 시대 × 등급 · 투구 스타일 × 시대 · 갑옷 스타일 × 시대)마다 메시 목록 `{parts:[{pos,nor,col|color,mat:{color,emissive,emissiveIntensity,metalness,roughness,opacity,flatShading,map?},matrix}]}` 를 JSON 으로. 유니티 `GearMeshes` 가 그것을 Mesh/Material 로 세우고 `Paperdoll` 훅에 준다(무기 원점 = 파지점 · 자루 = 로컬 +y 규약 유지).
- 판정: 무기 52종 × 등장 시대 · 투구/갑옷 스타일 전부 예외 0 · PlayMode 로 영웅에 입혀 콘솔 빨강 0 · `screens` 시트로 원작 썸네일(`ui.js` 장비 카드)과 나란히.
- 범위: `tools/export_gear_meshes.js` · `Assets/StreamingAssets/data/gear-meshes.json`(추출기가 `check_data_sync` 에 들어간다) 또는 `Assets/Forge/Gear/` · `Assets/Scripts/Game/Hero/GearMeshes.cs` · `Assets/Scripts/Game/Hero/Paperdoll.cs`(훅 연결만) · `Assets/Tests/PlayMode/PaperdollTests.cs`.
- 실측(2026-09-12 워커 T): `makeArmorExtras` 는 정본이 `if (!this.heroRig)`(레거시 몸통) 갈래에서만 부른다 — 박스 리그가 켜진 지금 화면에는 없어 캡처하지 않았다(옷 = `dressMcRig`→`mcArmorParts`). 투구는 ITEM_NAMES 이름 수대로 53(modern 7 · interstellar 6). 캡처는 `gear-meshes.json`(3.1MB · 재질 235 · 칸 목록 103 전역 표 · 지오메트리 변형 421).
### T38 ✅ — 지면 셰이더: 정본 `terrainShade`(매크로 변조 uv 6 = 월드 30 · 거리 LOD 14~34 · 눈 수광면 탈색 uSnow · 노면 회랑 uRoad) + `applyShadeLift`(암부 리프트) 를 URP 커스텀 셰이더로 (Game · T34 뒤)
- 정본: `scene3d.js` `TERRAIN`(scene.json 에 있다 · macro 0.30 · lod 0.45 · lodNear 14 · lodFar 34 · snow 0) · `terrainShade`(3650~3735 · `onBeforeCompile` 의 map_fragment/envmap_fragment 치환) · `applyShadeLift`(1323~) · `setTheme` 의 `uRoad`/`uSnow` 줄. T34 가 알베도·노멀·발광맵을 `Particles/Lit` 에 붙였고 노면 회랑은 T9 가 정점색 배율로 이미 낸다 — 여기서는 매크로·LOD·눈 탈색·암부 리프트 4항을 `Assets/Shaders/Terrain.shader`(URP HLSL · 정점색 · 노멀맵 · 발광맵 · 안개) 에 넣고 `GroundTextures.Apply` 가 그 셰이더로 갈아끼운다.
- 판정: 유니티 잡에서 셰이더 컴파일 에러 0(콘솔 빨강 0 · `Shader.isSupported`) + PlayMode 테마 25 순회 + «주인이 확인할 것: 사막 리플이 근경~원경에서 같은 벽지로 안 보이고 · 설원 수광면이 백색으로 빠지는가».
- 범위: `Assets/Shaders/Terrain.shader`(+.meta) · `Assets/Scripts/Game/World/GroundTextures.cs`(셰이더 갈아끼우기) · `tools/dotnet/Stubs/URP.cs`(필요 시) · `Assets/Tests/PlayMode/WorldTests.cs`(셰이더 단언).

### T39 ✅ — 전투 씬 후속: T8 이 뺀 원작 연출 전부 (Game · T8 뒤)
- 정본: `scene3d.js` `bossRegalia`(관·가시 · 11744 근처 호출) · `bossMaterialTell`(11360~) · `bossEntrance`(13480~ · BOSS_WARN_DUR 2.0 · BOSS_BEAT 0.42 · 배너 `#boss-warning` · 링 · 돌리 인 `camPush`) · `installDissolve`/`setDissolve`(12470~ · 노이즈 알파 클립 + 잔불) · `applyRimLight`(1376 · ENEMY_RIM darkStrength 0.98/darkPower 0.85) · `flashMesh`/`flashTargets`/`rimFlash`(12760~) · `impactFlare`(12544)·`impactSpikes`·`impactRing`·`expandRing`(16892)·`flashLight`(16844)·`scorchDecal`(16873) · `swoosh`·`trailImpact`·`updateTrail`(무기 궤적) · `corpseBlob`(13400) · 영웅 블롭 · `deathFade`(17402)·`sceneCut`(17361).
- T8 의 자리: `EnemyView`(보스 분기 · `StepDying` 의 소멸 줄 · `Hit` 의 플레어 줄) · `HeroView` · `BattleScene.Handle` 의 bossEntrance/deathFade/sceneCut 갈래. 디졸브·림은 URP 커스텀 셰이더(`Assets/Shaders/`) — 파티클 셰이더 위에 `_Dissolve`·림 항.
- 판정: 보스 웨이브 캡처에서 관·가시·발광 · 워닝 배너 3박 · 시체가 가장자리부터 부스러짐 · 피격 프레임에 청백/주황 림 · PlayMode 콘솔 빨강 0.
- 범위: `Assets/Scripts/Core/BattleFx/FxRules.cs`(상수·순수식) · `Assets/Scripts/Game/Battle/Boss*.cs` · `Assets/Scripts/Game/Battle/HitFlash*.cs` · `Assets/Scripts/Game/Battle/Trail*.cs` · `Assets/Scripts/Game/Ui/Battle*.cs`(워닝 배너·암전 커버) · `Assets/Shaders/Dissolve*.shader`(적 몸 = 림·플래시·디졸브) · `Assets/Shaders/FxUnlit.shader`(임팩트 무조명) · T8 자리(`EnemyView.cs`·`HeroView.cs`·`BattleScene.cs` 의 T39 갈래) · `Assets/Tests/PlayMode/BattleFxSceneTests.cs`.

### T40 ✅ — Core 탈것 시스템: `mounts.js` 규칙 이식 — 소환 레벨(`mountSummonRates` needed 표 · MAX 50) · 태엽 비용(`WINDERS_PER_SUMMON` × 기술트리 `mountCostMult`) · 보관 250 · 장착 **1마리**(`equip`/`setRidden`/`isActive`/`riddenIdx`/`ridden`) · 경험치 흡수(`absorbMaterials` 내림차순 · 인덱스 보정) · 기여(`mountPower` = 같은 등급 장비 8부위 합 × 레벨 × 승천 × 기술트리 · `activeBonus`) · 구세이브 이관(`migrateInventory`·`migrateSpecies`) (Core · T3·T14·T24 뒤 · T20 탈것 화면·T11 탑승이 쓴다)
- 정본: `web/js/mounts.js`(284줄 전부) · `balance-data.js` 의 `mountSummonRates`·`mountNames`·`WINDERS_PER_SUMMON`(T2 `balance.json` → T3 `MountTable`). 원작이 밖에서 받는 것은 T16 `IPetHost` 꼴의 `IMountHost`(지갑 winders·젬 · `Forge.levelMult/gearSumAtkAt/gearSumHpAt` · `Ascension.starMult/count('mount')` · `TechTree.mountDmgMult/mountHpMult/mountCostMult/extraMountChance`). 코드 상수(`MAX_LEVEL 50`·`INDIV_MAX_LEVEL 100`·`INV_CAP 250`·`MAX_ACTIVE_MOUNTS 1`·xp 커브·`LEGACY_SPECIES`)는 T16 `PetRules` 처럼 `MountRules` 로.
- 대조: `tools/mount_vectors.js`(T16 `pet_vectors.js` 방식 — 정본을 vm 으로 실제 실행 · mulberry32) → `Assets/Tests/EditMode/Vectors/mount_vectors.json` · EditMode `MountTests`(레벨/needed/prevNeeded 51행 · 태엽 비용 · 소환 배치(보너스 확률·보관 상한·자동 장착은 빈 슬롯일 때만) · 장착 1마리 교체·해제 · 흡수 인덱스 보정 · 기여 Big · 이관 3꼴). T13 세이브 코덱(`mounts`·`activeMounts`·`mountOpens`·`winders`)은 T20 `PetSkillSave` 꼴로 여기서.
- 범위: `Assets/Scripts/Core/Mounts/` · `Assets/Tests/EditMode/MountTests.cs` · `Assets/Tests/EditMode/Vectors/mount_vectors.json` · `tools/mount_vectors.js`.

### T41 ✅ — 게이트: `catalog.json` 최상위 키 중복 검사 — `gen_ui_catalog --check` 가 같은 최상위 키(`layout`·`colors`…)가 두 번이면 rc 1 · CI dotnet 잡이 그 `--check` 를 부른다(지금은 안 부른다) (검증 · 뒤 순서 없음 · 검수 Q 등재)
- 왜: CI 런 32 — rebase 충돌 «둘 다 살리기» 로 `"layout": [` 가 둘이 됐는데 파이썬 `json.load` 는 **뒤** 블록을, 유니티 `JsonUtility` 는 **앞** 블록을 읽어 로컬 게이트·dotnet 잡 전부 초록인 채 PlayMode 만 `KeyNotFoundException`(결정 82 는 «grep 으로 본다» 는 손 규칙만 남겼다 — 자로 막는다).
- 방법: `json.load(..., object_pairs_hook=…)` 로 최상위(그리고 각 항목) 키 중복을 잡아 `✗` + rc 1 · `--self-test` 에 «중복 키 JSON 이면 rc 1» 한 칸 · `.github/workflows/ci.yml` dotnet 잡에 `python3 tools/gen_ui_catalog.py --check` 한 줄(§3 게이트 목록에도).
- 범위: `tools/gen_ui_catalog.py` · `.github/workflows/ci.yml`(dotnet 잡 한 줄) · `docs/ROUTINE.md` §3 한 줄.
- ✅ 2026-09-12 워커 F — `read_catalog` 가 두 가지를 막는다: ⓐ 한 객체 안의 JSON 키 중복(`object_pairs_hook` · 깊이 무관) · ⓑ 배열 항목 키(`key`) 중복(유니티는 `map[e.key] = e.value` 로 **뒤** 것을 쥔다 · 결정 89). `--self-test` 11칸 + CI dotnet 잡 두 줄(자기 검사 · `--check`) 은 **막는 갈래**(continue-on-error 아님).

### T42 ✅ — 자 수리: `check_claim_scope.undeclared()` 가 «폴더/» 로 적은 범위(`Assets/Scripts/Core/Battle/`)를 못 덮어 살아 있는 lock 마다 «범위에 안 적힌 채 쥔 파일» 오탐을 낸다(T7 폴더 7개 + T20 `Ui/Pet*` 글로브 11개 = 회차마다 18개 · 전부 오탐) — 폴더·글로브 토큰은 접두 매칭 (검증 · 뒤 순서 없음 · 검수 Q 등재)
- 왜: `undeclared()` 는 파일 줄기(`BattleContext`)가 범위 칸에 **글자로** 있는지만 본다. §2 «범위» 는 폴더로 적는 것이 규약이라 폴더 범위 lock 은 전부 오탐이고, 그 소음에 진짜 «밖 파일» 이 묻힌다(자 스스로 «그것을 매 회차 다시 한다» 고 적어 둔 자리).
- 방법: 범위 칸의 `…/` 로 끝나는 토큰과 `…*` 글로브 토큰(백틱 안)은 경로 접두로 보고(`Ui/Pet*` 는 `Assets/Scripts/Game/Ui/Pet` 접두) `p.startswith(prefix)` 면 덮은 것으로 · `--selftest` 에 «폴더·글로브 범위는 덮는다 · 밖 파일은 여전히 잡는다» 두 칸.
- 범위: `tools/check_claim_scope.py`.
### T43 ✅ — 전투 스탯 접착: 세이브 상태 → `GearSystem.HeroStats` → `BattleContext.HeroStats` (Game · T8·T13·T15·T16 뒤)
- 정본: `forge.js heroStats()`(329~369 · 장비 8부위 + `Pets.activeBonus()` + `Mounts.activeBonus()` + `Skills.ownedPassive()` + `TechTree.gearAtkMult/gearHpMult` …) · `combat.js` 가 공격·재계산 때 `Forge.heroStats()` 를 부르는 자리 · 출전/장착 변경 → 재계산.
- 유니티: `BattleScene.Boot` 의 `BareHeroStats.Make`(T8 결정 69ⓓ 자리표)를 `SaveIo.State` 위에 세운 `GearSystem`(T15 · `IGearHost` = 세이브 상태 + `PetSystem.ActiveBonus`(T16) + 탈것(T11) + 스킬 패시브(T12) + 기술트리(T24))의 `HeroStats()` 로 바꾼다. 세이브가 없으면 맨몸.
- 판정: EditMode 로 같은 세이브 → 정본 `heroStats` 실행 벡터와 atk/hp/치명/공속 일치 · PlayMode 로 펫 출전 뒤 `Battle.Hero.Atk` 이 오르는가 · 콘솔 빨강 0.
- 범위: `Assets/Scripts/Game/Battle/BattleScene.cs`(Boot 의 stats 인자) · `Assets/Scripts/Game/Battle/HeroStatsGlue.cs` · `Assets/Tests/PlayMode/HeroStatsGlueTests.cs`.

### T44 — 60fps 게이트: `targetFrameRate 60` · vSync 0 · 전투 최대 부하 프레임 예산 테스트 · 프레임당 GC 0 · 드로우콜 상한 (Game·검증 · T8·T10·T12 뒤 · **T27 다음으로 먼저**)
- 주인 지시 2026-09-12 «60fps 로 프레임 돌아야». 지금 `Application.targetFrameRate` 설정이 어디에도 없다(실측 grep 0건).
- 할 것: ⓐ `Bootstrap` 에서 `Application.targetFrameRate = 60` · `QualitySettings.vSyncCount = 0`(WebGL 은 rAF 를 따르므로 그대로) · 모바일 `Screen.sleepTimeout` 원작대로 ⓑ PlayMode `PerfBudgetTests`: 전투 최대 부하 장면(웨이브 보스 + 적 4 + 펫 3 + 스킬 오브젝트 2 + 히트 파티클·데미지 숫자 연속 3초)을 200프레임 돌려 **메인스레드 프레임 시간 평균·p95** 를 `Time.unscaledDeltaTime` 과 `UnityEngine.Profiling.Recorder`(`PlayerLoop`) 로 재고, CI 러너(GPU 없음 · 소프트웨어 렌더)에서는 «CPU 메인스레드 ≤ 8ms 평균 · p95 ≤ 12ms» 를 통과선으로(폰에선 그 2배 여유가 16.6ms 안) · `GC.GetTotalMemory` 차이로 **프레임당 관리 힙 증가 0** 단언 · `UnityStats.drawCalls` 상한(부하 장면 ≤ 150) ⓒ 넘으면 이 작업이 고친다: 복셀 몹 파츠 → 몹당 메시 하나 병합(`VoxelMob.ToMesh` 가 이미 하면 확인만) · 데미지 숫자·파티클 풀링 · `Update` 의 `Find`·`GetComponent`·`string+` 제거 · 머티리얼 공유(색은 정점색) ⓓ 완료 기록에 부하 장면 평균·p95·드로우콜·GC 수치.
- 판정: `PerfBudgetTests` 초록 + 수치 기록 + 「주인이 볼 것: 폰에서 전투 60fps(설정 탭에 FPS 표시 토글 — 원작 디버그 탭에 있으면 그것 · 없으면 넣지 않는다)」.
- 범위: `Assets/Scripts/Game/Bootstrap.cs`(targetFrameRate 두 줄) · `Assets/Tests/PlayMode/PerfBudgetTests.cs` · 넘길 때만 `Assets/Scripts/Game/Battle/`·`Game/Voxel/`·`Game/Ui/Hud.cs`(풀링·병합 갈래).

### T45 — SafeArea 노치 모의 검증: HUD·탭바·팝업 ✕·채팅줄·토스트가 `Screen.safeArea` 안에 있는가 (Game·검증 · T18 뒤 · **T27 다음으로 먼저**)
- 주인 지시 2026-09-12 «SafeArea 해서 모바일 상단 카메라 안 가리게». `UiRoot` 가 `Screen.safeArea` 를 읽어 앱 상자를 놓지만(T18) **노치가 있을 때 정말 안 가리는지 단언하는 테스트가 없다**(에디터·CI 는 safeArea = 전체 화면이라 조용히 초록).
- 할 것: ⓐ `UiRoot` 에 테스트용 safeArea 주입 지점(`UiRoot.OverrideSafeArea(Rect?)` · 게임 코드는 안 쓴다) ⓑ PlayMode `SafeAreaTests`: 위 120px·아래 60px·좌우 0 을 깎은 safeArea 를 주입하고 부팅 → HUD 상단바·스테이지 표시·탭바·시트 ✕·채팅줄·토스트의 **월드 코너 4점이 전부 safeArea 안** · 세 해상도(540×1170 · 360×800 · 430×932) · 가로로 뒤집어도(`Screen.orientation` 은 세로 고정이니 해상도만) ⓒ 3D 카메라는 전체 화면 그대로(레터박스 계산이 safeArea 를 이중으로 깎지 않는지 = T1 `Viewport.Letterbox` 와 T18 앱 상자의 관계를 한 줄로 결정 기록) ⓓ 촬영(T27)의 노치 모의 경계선과 같은 값을 쓴다(상수 하나 · `UiCatalog` 또는 테스트 공용 상수).
- 판정: `SafeAreaTests` 초록 + 노치 모의 PNG 를 열어 상단바가 빨간 선 아래에 있는 것을 본 기록 + 「주인이 볼 것: 노치 폰에서 상단바·✕ 가 카메라에 안 가림」.
- 범위: `Assets/Scripts/Game/Ui/UiRoot.cs`(주입 지점만) · `Assets/Tests/PlayMode/SafeAreaTests.cs` · `Assets/Forge/catalog.json`(노치 상수 한 줄 · 있으면).
### T46 ✅ — PlayMode 실패 진단 로그: 실패 테스트의 «왜» 를 `screens` 브랜치에서 읽는다 (검증 · 뒤 순서 없음)
- 왜: CI 유니티 잡 로그는 **꼬리 5000줄** 만 남아(2026-09-12 실측 · 러너가 결과 XML 을 그 앞에 다 찍는다) 실패 픽스처의 메시지·스택이 잘려 나간다. 아티팩트(`unity-test-results`)는 워커 컨테이너의 프록시가 막는다. 그래서 T8 의 «테스트 결과 요약» 스텝은 **이름만** 알려 준다 — 런 46 의 PlayMode 26 빨강에서 임자마다 이유를 추측으로 파야 했다.
- 무엇: PlayMode 어셈블리에 어셈블리 단위 `ITestAction`(NUnit) 하나를 두어 테스트마다 ⓐ 결과(Outcome·Message·StackTrace) ⓑ 그 테스트가 도는 동안 온 **콘솔 빨강**(`Application.logMessageReceived` 의 Error/Exception/Assert · condition + stackTrace)을 `ui-screens/playmode-red.txt` 에 **줄마다 바로 덧붙인다**(런이 중간에 죽어도 남는다). CI 가 `ui-screens/` 를 `screens` 브랜치로 올리므로(§5) 다음 워커는 `git show origin/screens:playmode-red.txt` 로 이유를 읽는다 — **ci.yml 은 건드리지 않는다**(그 «요약 스텝» 은 T8 범위다).
- 통과한 테스트는 한 줄(`PASS <이름>`)만 남긴다 — 파일이 붙는 순서가 곧 실행 순서라 «어느 테스트가 세이브를 오염시켰나» 를 뒤에서 읽을 수 있다.
- 판정: `dotnet build`·`dotnet test` 초록(스텁에 쓰는 서명이 있는가) + CI 유니티 잡이 돈 뒤 `screens` 브랜치에 `playmode-red.txt` 가 서고 그 안에 런 46 류의 실패 이유가 적혀 있다.
- 범위: `Assets/Tests/PlayMode/RedLog.cs` · `tools/dotnet/Stubs`(필요한 스텁 서명만).

### T48 ✅ — 게이트: PlayMode 테스트도 dotnet 하니스로 컴파일한다 (검증 · 뒤 순서 없음 · 워커 L 등재)
- 지금 `tools/dotnet` 는 `Assets/Tests/EditMode/**` 만 컴파일한다 — **PlayMode 테스트의 오타·잘못된 서명은 §3 게이트를 전부 초록으로 통과하고 유니티 CI 에서야 터진다**(§1 «컴파일 파손을 남기지 않는다» 가 가장 잘 뚫리는 자리 · 워커마다 PlayMode 파일을 쓴다).
- 할 일: `tools/dotnet/TestsPlay/Forge.TestsPlay.csproj`(`Assets/Tests/PlayMode/**/*.cs` + `Forge.Game` 프로젝트 참조 + NUnit 3.6.1 · **테스트를 돌리지 않는다 · 컴파일만**) · `tools/dotnet/Stubs/TestTools.cs`(`UnityTestAttribute` · `LogAssert.ignoreFailingMessages`·`Expect(LogType,string)`·`Expect(LogType,Regex)`·`NoUnexpectedReceived`) · `Forge.sln` 에 추가 · §3 게이트 목록에 한 줄.
- ⚠ 스텁이 실물 `UnityEngine.TestTools` 와 다르면 «로컬만 초록» 이 또 생긴다 — 새 API 를 쓰면 스텁에도 같은 서명을 더한다(§1 패키지 규칙과 같은 갈래).
- 실측(2026-09-12 워커 L · T27 회차): 스크래치에 이 꼴로 세웠더니 `Forge.Core.Rng`(진짜는 `Forge.Core.Data.Rng`) 오타가 그 자리에서 잡혔다 — 없었으면 유니티 CI 한 바퀴(30분)를 태웠다.
- 범위: `tools/dotnet/TestsPlay/` · `tools/dotnet/Stubs/TestTools.cs` · `tools/dotnet/Forge.sln` · `docs/ROUTINE.md`(§3 한 줄) · `docs/PROGRESS.md`.


### T47 — 카탈로그 색 44키가 `layout` 배열에 빠져 있다: `DungeonUiTests` 4/4 빨강(«색 «white»·«pill_potion»·«muted2» 이 없다») (검증·UI · 뒤 순서 없음 · 임자 없는 빨강)
- 무엇: `79dc03d`(T21) 가 «복제된 블록 병합» 으로 푼 자리에서 **색 항목 79개가 `colors` 가 아니라 `layout` 배열 안으로 들어갔다**. 그중 35개는 `colors` 에 이미 있는 것과 같은 값(무해)이고 **44개는 `colors` 어디에도 없다** — `white`·`muted2`·`silver`·`dgd_*`·`dg_*`·`x_btn*`·`info_btn`·`tech_*`·`tb_*`·`pill_*`·`dgclear_*`·`asc_*`·`tn_bronze*`·`idet_icon_*`·`pp_sheet`·`btn_disabled_ink`. 유니티는 그것을 `LayoutEntry`(값 0)로 읽고 `UiKit.C` 는 `KeyNotFoundException` 을 던져 던전·기술트리·승천 화면이 통째로 안 선다(런 46·54 `DungeonUiTests` 0/4 · T21 은 ✅ 이고 lock 이 없어 **임자 없는 빨강** · ROUTINE §0-6).
- 왜 자들이 못 잡았나: T41 이 막는 것은 ⓐ 한 객체 안의 JSON 키 중복 ⓑ 한 배열 안의 항목 키 중복 둘뿐이다. 여기는 병합이 **항목을 다른 절로 옮겨** 둘 다 아니다 — 절의 «모양»(색은 `hex` · 배치는 `value`)을 아무도 안 봤다.
- 무엇을 한다: ⑴ `layout` 의 색 항목 79개를 빼고 그중 `colors` 에 없는 44개를 `colors` 절에 잇는다(값은 그대로 · 새 색 0) ⑵ `gen_ui_catalog.py` 에 **절 모양 게이트** ⓒ 를 더한다 — `colors` 항목에 `value` 나 `hex` 없음, `layout` 항목에 `hex` 나 `value` 없음이면 rc 1(자기 검사 칸 포함) ⑶ `UiCatalog.asset` 재생성.
- 판정: `gen_ui_catalog --check`·`--self-test` rc 0 · 「색 키 ↔ 코드 참조」 훑기에 빠진 색 0 · CI 유니티 잡에서 `DungeonUiTests` 4/4 초록.
- 범위: `Assets/Forge/catalog.json`(항목 자리만 옮긴다 · 값 변경 0) · `Assets/Forge/Resources/UiCatalog.asset`(자가 다시 만든다) · `tools/gen_ui_catalog.py`.

### T49 ✅ — 자: §7 완결 대조표 ↔ PROGRESS 상태 (검증 · 뒤 순서 없음 · 워커 F 등재)
- 왜: §7 은 주인이 정한 «다 옮겨졌다» 의 기준이고 T33 이 마지막에 그것으로 판정하는데, 그 표를 보는 자가 없어 **끝난 작업이 🔄·⬜ 로 남는다**(2026-09-12 21:36 실측 4칸: scene3d 줄 «T10 🔄 · T12 🔄»(같은 칸 뒤에 «T12 ✅» 가 또 있다) · pets 줄 «T43 ⬜» · 품질 줄 «T45 ✅»(PROGRESS 는 🔄)). §2 제목 ↔ PROGRESS 는 `task_state --check` 가 보지만 §7 은 아무도 안 본다.
- 방법: `tools/check_final_table.py` — §7 표의 «상태» 칸에서 `T<번호> <표시>` 를 읽어(묶음꼴 `✅ (T4 · T5)` 와 표시 하나만 적힌 줄도 «작업» 칸의 번호 전부에 적용) PROGRESS 표의 상태와 대조 · 어긋나면 줄·번호·양쪽 표시를 찍고 rc 1 · 한 칸에 같은 번호가 두 표시로 있으면 그것도 잡는다 · `--self-test` · CI dotnet 잡에 **보고만**(continue-on-error · 배포가 죽는 갈래가 아니다) + 자기 검사는 막는다.
- 범위: `tools/check_final_table.py` · `.github/workflows/ci.yml`(dotnet 잡 두 줄) · `docs/ROUTINE.md`(§3 한 줄 · §7 어긋난 칸) · `docs/PROGRESS.md`(표 행·기록).
- ✅ 2026-09-12 워커 F — 실측 어긋남 5건 → 0(§7 세 칸 정정 · 결정 120) · 자기 검사 17칸 · CI dotnet 잡 두 스텝(자기 검사는 막고 대조는 보고만).

## 3. 게이트 (커밋 전 · 세션 종료 전)

> ⚑ **꼬리로 읽지 마라 — `rc` 를 보라.** 자들의 출력은 «고치는 법» 으로 끝나는 것이 많아 마지막 줄만 보면 빨강과 초록이 같아 보인다. `; echo rc=$?` 를 붙여 돌린다.

```bash
dotnet build tools/dotnet/Forge.sln -c Release --nologo                       # 컴파일 (Core · Game(스텁) · Tests · TestsPlay = PlayMode 테스트 «컴파일만» · T48)
dotnet test tools/dotnet/Tests/Forge.Tests.csproj -c Release --no-build       # 순수 C# 테스트 (NUnit 3.6.1 API 면만)
python3 tools/gen_meta.py --check                                             # .meta 누락/고아 (새 에셋을 만들면 --check 없이 돌려 생성)
python3 tools/gen_ui_catalog.py --check                                       # catalog.json ↔ UiCatalog.asset · 키 중복(T41 · CI dotnet 잡이 막는다)
python3 tools/check_docs_intact.py                                            # 문서가 통째로 깨졌는가 (충돌 표식 · 결정 기록 소실 · 표 0행) — CI 에서 막는다
python3 tools/check_decisions.py                                              # 결정 번호 겹침 · `--next` 로 다음 번호
python3 tools/check_task_rows.py                                              # PROGRESS 같은 작업 두 줄 어긋남
python3 tools/task_state.py --check                                           # ROUTINE §2 제목 ↔ PROGRESS 상태 · 번호 중복
python3 tools/check_claim_scope.py                                            # 살아 있는 lock 이 «범위» 밖 파일을 쥐고 있는가 (선점 전에도)
python3 tools/check_final_table.py                                            # §7 완결 대조표 ↔ PROGRESS 상태 (T49 · T33 이 이 표로 완주를 판정한다)
tools/check_data_sync.sh .wwwww-src                                           # (T2 뒤) data/*.json ↔ 정본
node tools/export_data.js --self-test                                         # (T2 뒤) 추출기 자기 검사
```

- 로컬에 `dotnet` 이 없는 컨테이너(클라우드 세션은 대개 없다)에서는 **먼저 `apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y dotnet-sdk-8.0` 을 시도한다**(약 2분 · 결정 10 · 계정 1·4 컨테이너에서 2026-09-12 실측 성공 — PPA 403 경고는 무시). 그래도 없으면 `dotnet` 두 줄을 건너뛰고 **그 사실을 완료 기록에 적는다** — 그때는 CI 의 `dotnet` 잡이 초록인 것을 확인하기 전에는 lock 을 반납하지 않는다.
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

- **이름**: `unity1 포지 이식 워커 X (:MM)` · **레포**: `https://github.com/kuzuni/unity1` · **모델**: **`claude-opus-5`**(주인 지시 2026-09-12 «루틴들 오퍼스로» · 전 계정 · persistent 세션에 묶인 루틴은 **세션의 모델**이 실행 모델이므로 바꾸려면 Opus 세션을 새로 만들어 루틴을 다시 묶는다 — 계정 2 가 20:14 UTC 에 그렇게 했다 · 계정 1·3·4·5 는 그 계정 세션이 같은 방법으로 바꾸고 ③ 표를 고친다 · **소넷 금지**) · **도구**: Bash, Read, Write, Edit, Glob, Grep, Task, WebFetch · **환경**: 그 계정의 Default · **`persist_session: true`**(실행마다 대화창을 새로 만들지 않는다 · 주인 지시).
- **프롬프트**: `docs/ROUTINES-SETUP.md` §4 의 블록을 그대로(`X` 만 바꾼다 · 네 계정의 프롬프트는 글자까지 같다).
- 만든 뒤 routine ID 와 첫 런 링크를 아래 ③ 표에 적어 커밋한다(`[skip ci]`).

### ③ 등록 기록 (각 계정 세션이 채운다)

| 워커 | 슬롯 | routine ID | 계정 | 첫 런 | 비고 |
|---|---|---|---|---|---|
| A | :05 | `trig_012jgYLoCrxjx93g81HEtfB6` | 계정 1 | 2026-09-12 18:05 UTC 예정 · 세션 https://claude.ai/code/session_015weaVBkodx5PzrfyHo1XEr · 루틴 https://claude.ai/code/routines/trig_012jgYLoCrxjx93g81HEtfB6 | 2026-09-12 17:44 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_014bNYWJnnxgzqfDN9JPBD6p` · enabled |
| B | :20 | `trig_01VgDxyoE699SFJH3dvNK8Mh` | 계정 1 | 2026-09-12 18:20 UTC 예정 · 세션 https://claude.ai/code/session_01Cw7XN9dSWNGb9USXPGiNN5 · 루틴 https://claude.ai/code/routines/trig_01VgDxyoE699SFJH3dvNK8Mh | 2026-09-12 17:44 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_014bNYWJnnxgzqfDN9JPBD6p` · enabled |
| C | :35 | `trig_01DRx8vYMXjWymP3ofAHDawL` | 계정 1 | 2026-09-12 18:35 UTC 예정 · 세션 https://claude.ai/code/session_01YaXyrwumgLb7Qh4dUDGojn · 루틴 https://claude.ai/code/routines/trig_01DRx8vYMXjWymP3ofAHDawL | 2026-09-12 17:44 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_014bNYWJnnxgzqfDN9JPBD6p` · enabled |
| D | :50 | `trig_014rJnPQ4DbKdorQWd2Xcn5o` | 계정 1 | 2026-09-12 17:50 UTC 예정 · 세션 https://claude.ai/code/session_01Pa7cTGCgeF1dfRiBWNi8Qt · 루틴 https://claude.ai/code/routines/trig_014rJnPQ4DbKdorQWd2Xcn5o | 2026-09-12 17:44 UTC 생성(착수 세션 · create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_014bNYWJnnxgzqfDN9JPBD6p` · enabled |
| E | :12 | `trig_011YnJXufgpZMMjQgEAFfaLJ` | 계정 2 | 2026-09-12 21:12 UTC 예정 · 세션 https://claude.ai/code/session_01AqEsQJjgYyE69969QWMDTS · 루틴 https://claude.ai/code/routines/trig_011YnJXufgpZMMjQgEAFfaLJ | 2026-09-12 20:14 UTC **`claude-opus-5` 로 재생성**(주인 지시 «루틴들 오퍼스로» · persistent 세션의 모델이 실행 모델이라 Opus 세션을 새로 만들어 다시 묶음 · 옛 루틴 `trig_01TpK2L8uMb8kYNKe34gyPWu` 삭제 · 옛 세션은 그대로 둠) · env `env_01JWPF8hM8XqtGYWuFnAsN93` · enabled |
| F | :27 | `trig_01MJqoCKD8Zo9RFkD6oH1aCt` | 계정 2 | 2026-09-12 20:27 UTC 예정 · 세션 https://claude.ai/code/session_01F2PY3r9jgD6qybb9cNfgPr · 루틴 https://claude.ai/code/routines/trig_01MJqoCKD8Zo9RFkD6oH1aCt | 2026-09-12 20:14 UTC **`claude-opus-5` 로 재생성**(주인 지시 «루틴들 오퍼스로» · persistent 세션의 모델이 실행 모델이라 Opus 세션을 새로 만들어 다시 묶음 · 옛 루틴 `trig_01CXHFxMGv75VubqVr32USRH` 삭제 · 옛 세션은 그대로 둠) · env `env_01JWPF8hM8XqtGYWuFnAsN93` · enabled |
| G | :42 | `trig_01QgJidRoEz6jucwxAApmTMA` | 계정 2 | 2026-09-12 20:42 UTC 예정 · 세션 https://claude.ai/code/session_01RsSRvWsTCyk66g3LmSgvP8 · 루틴 https://claude.ai/code/routines/trig_01QgJidRoEz6jucwxAApmTMA | 2026-09-12 20:14 UTC **`claude-opus-5` 로 재생성**(주인 지시 «루틴들 오퍼스로» · persistent 세션의 모델이 실행 모델이라 Opus 세션을 새로 만들어 다시 묶음 · 옛 루틴 `trig_01RRyATu6xhqWBcYV1hJ36LJ` 삭제 · 옛 세션은 그대로 둠) · env `env_01JWPF8hM8XqtGYWuFnAsN93` · enabled |
| H | :57 | `trig_01P7n9r9Hj8GUfQsk5QHDmZA` | 계정 2 | 2026-09-12 20:57 UTC 예정 · 세션 https://claude.ai/code/session_01UKqyvGvccdWrBPhn6rrCfA · 루틴 https://claude.ai/code/routines/trig_01P7n9r9Hj8GUfQsk5QHDmZA | 2026-09-12 20:14 UTC **`claude-opus-5` 로 재생성**(주인 지시 «루틴들 오퍼스로» · persistent 세션의 모델이 실행 모델이라 Opus 세션을 새로 만들어 다시 묶음 · 옛 루틴 `trig_018TPgh9cbG7GjRX7SWJUzBK` 삭제 · 옛 세션은 그대로 둠) · env `env_01JWPF8hM8XqtGYWuFnAsN93` · enabled |
| I | :02 | `trig_018Vjz8qXMGipp2hh8ohi43L` | 계정 3 | 2026-09-12 21:02 UTC 예정 · 세션 https://claude.ai/code/session_01HDCmUmu4EXfWD4gVdC4Svq · 루틴 https://claude.ai/code/routines/trig_018Vjz8qXMGipp2hh8ohi43L | 2026-09-12 20:40 UTC **`claude-opus-5` 로 재생성**(주인 지시 «루틴들 오퍼스로» · persistent 세션의 모델이 실행 모델이라 Opus 세션을 새로 만들어 다시 묶음 · 옛 루틴 `trig_018vJFGNoopJV5U8DcKLdRMd` 삭제 · 옛 세션은 그대로 둠 · 워커 K 회차가 처리) · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled |
| J | :17 | `trig_01DhLbaE3Zo3YHozpvfmuPWN` | 계정 3 | 2026-09-12 21:17 UTC 예정 · 세션 https://claude.ai/code/session_01Eo4cuMYRMym4R68TYHZVME · 루틴 https://claude.ai/code/routines/trig_01DhLbaE3Zo3YHozpvfmuPWN | 2026-09-12 20:40 UTC **`claude-opus-5` 로 재생성**(옛 루틴 `trig_01Wi4tU6pNfw88Yg7CjXpPCp` 삭제 · 옛 세션은 그대로 둠) · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled |
| K | :32 | `trig_01HqeNtnTXkQBrqn3fFb8Pt9` | 계정 3 | 2026-09-12 21:32 UTC 예정 · 세션 https://claude.ai/code/session_01Ubc53HMRbGd3zvRaL2dmmD · 루틴 https://claude.ai/code/routines/trig_01HqeNtnTXkQBrqn3fFb8Pt9 | 2026-09-12 20:40 UTC **`claude-opus-5` 로 재생성**(옛 루틴 `trig_0148B63LsU2pRov9y1yfivaj` 삭제 · 옛 세션 session_0132fa6K2bvevQ9Jr63rRW9h 은 이 회차를 끝내고 멈춘다) · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled |
| L | :47 | `trig_01BdzAk93WYGaSGLqowSAnr8` | 계정 3 | 2026-09-12 20:47 UTC 예정 · 세션 https://claude.ai/code/session_01B97s4g7pWoVba3fekgZVvY · 루틴 https://claude.ai/code/routines/trig_01BdzAk93WYGaSGLqowSAnr8 | 2026-09-12 20:40 UTC **`claude-opus-5` 로 재생성**(옛 루틴 `trig_012SuRjsZCV7QtkeSNF5L9Vk` 삭제 · 옛 세션은 그대로 둠) · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled |
| Q | 짝수시 :00 | `trig_01DMWuJm1srhVypGWosHcHT3` | 계정 3 | 2026-09-12 22:00 UTC 예정 · 세션 https://claude.ai/code/session_01JUiPmj1pfHDfY8TCeDkNtW · 루틴 https://claude.ai/code/routines/trig_01DMWuJm1srhVypGWosHcHT3 | 검수 · 2026-09-12 20:40 UTC **`claude-opus-5` 로 재생성**(옛 루틴 `trig_01AkHocLEtxx7oBfcSo9eXFS` 삭제 · 옛 세션은 그대로 둠) · env `env_01XKJDdWmKFxSg4FuR8yethb` · enabled · cron 은 시(hour)를 나열한 `0 0,2,…,22 * * *`(서버 앵커 회피 · 2026-09-12 실측) |
| M | :09 | `trig_013UTjzK35V8WZRx3uqf7Xeq` | 계정 4 | 2026-09-12 22:09 UTC 예정 · 세션 https://claude.ai/code/session_01JxNewmiUdzHFkyLZEPfYWS · 루틴 https://claude.ai/code/routines/trig_013UTjzK35V8WZRx3uqf7Xeq | 2026-09-12 21:12 UTC **`claude-opus-5` 로 재생성**(주인 지시 «루틴들 오퍼스로» · Opus 세션을 새로 만들어 다시 묶음 · 옛 루틴 `trig_01MdzGfq8mEdrutbMPDEygV9` 삭제 · 옛 세션 session_01Ntvg8qcH25hBHUm2QGumic 은 이 회차를 끝내고 멈춘다 · 워커 M 회차가 처리) · env `env_016Xis527zoBbZPqrtAZVQ6x` · enabled |
| N | :24 | `trig_01USZtkfikmcHyBWLPZ12FDZ` | 계정 4 | 2026-09-12 18:24 UTC 첫 런(이 줄을 적은 회차) · 세션 https://claude.ai/code/session_01BXsjJmqjNnZpFdUJQsyBtD · 루틴 https://claude.ai/code/routines/trig_01USZtkfikmcHyBWLPZ12FDZ | 2026-09-12 17:55 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_016Xis527zoBbZPqrtAZVQ6x` · enabled · ID 는 워커 N 첫 회차(sess-1824-31207)가 `list_triggers` 로 확인해 적음 · ⚠ **아직 `claude-fable-5-1`** — 워커 M 회차(21:12 UTC)가 Opus 세션 `session_01P9wBKmiqdEYD54rgiFBVzP` 을 만들어 두었으나 `create_trigger` 가 하니스 분류기(«Create Unsafe Agents»)에 막혀 루틴을 못 묶었다 → 계정 4 의 다음 세션이 그 세션에 `create_trigger(persistent_session_id=…)` 로 묶고 옛 루틴을 지운다(§4 프롬프트 · 이메일 kimmoon1995@gmail.com) |
| O | :39 | `trig_01UKii64CLP1z2ayvgQwTWtP` | 계정 4 | 2026-09-12 18:39 UTC 예정 · 세션 https://claude.ai/code/session_011szphzNiAkiJvfnHF6BaNM · 루틴 https://claude.ai/code/routines/trig_01UKii64CLP1z2ayvgQwTWtP | 2026-09-12 17:55 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_016Xis527zoBbZPqrtAZVQ6x` · enabled · ID 는 워커 N 첫 회차(sess-1824-31207)가 `list_triggers` 로 확인해 적음 · ⚠ **아직 `claude-fable-5-1`** — 워커 M 회차(21:12 UTC)가 Opus 세션 `session_019jsvF2cUnSQYM6G82Duy4c` 을 만들어 두었으나 `create_trigger` 가 하니스 분류기(«Create Unsafe Agents»)에 막혀 루틴을 못 묶었다 → 계정 4 의 다음 세션이 그 세션에 `create_trigger(persistent_session_id=…)` 로 묶고 옛 루틴을 지운다(§4 프롬프트 · 이메일 kimmoon1995@gmail.com) |
| P | :54 | `trig_01G6BEu4XYkm4bRHKNyD4vVR` | 계정 4 | 2026-09-12 18:54 UTC 예정 · 세션 https://claude.ai/code/session_01KrYt1SMJHj1swZGrVGfiZV · 루틴 https://claude.ai/code/routines/trig_01G6BEu4XYkm4bRHKNyD4vVR | 2026-09-12 17:55 UTC 생성(create_trigger · persistent_session 바인딩 = 대화창 하나) · `claude-fable-5-1` · env `env_016Xis527zoBbZPqrtAZVQ6x` · enabled · ID 는 워커 N 첫 회차(sess-1824-31207)가 `list_triggers` 로 확인해 적음 · ⚠ **아직 `claude-fable-5-1`** — 워커 M 회차(21:12 UTC)가 Opus 세션 `session_01SofXGvnwwAqJ1K87ksigjL` 을 만들어 두었으나 `create_trigger` 가 하니스 분류기(«Create Unsafe Agents»)에 막혀 루틴을 못 묶었다 → 계정 4 의 다음 세션이 그 세션에 `create_trigger(persistent_session_id=…)` 로 묶고 옛 루틴을 지운다(§4 프롬프트 · 이메일 kimmoon1995@gmail.com) |
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
- 회차마다: ⓐ main 의 최근 CI 런 3개(빨강이면 어느 커밋·누구·무엇 · `cancelled` 는 §1 규칙대로 뒤 런으로 판단) ⓑ 최근 ✅ 다섯 개가 «정말 도는가»(완료 기록의 확인 수단을 실제로 다시 돌린다) ⓒ 살아 있는 lock 의 나이 ⓓ `screens` 최신 PNG 를 원작 시트와 눈으로 대조 ⓔ **§7 대조표 점검** — `.wwwww-src/web/js/*.js`·`index.html`·`css`·`ref/screens` 를 다시 훑어 §7 에 없는 모듈·화면·함수가 있으면 «가장 큰 번호 +1» 로 등재하고 §7 에 줄을 더한다(«없음» 을 방치하는 것이 검수 Q 의 실패다). 발견은 PROGRESS «주인 콘솔 에러 보고함» 아래 «검수 Q 보고» 에 등재(«가장 큰 번호 +1» 로 작업화). 문서 커밋만(`[skip ci]`).

## 7. 완결 정의 · 원작 ↔ 작업 대조표 (주인 지시 2026-09-12 «전체 빠짐없이» · 워커·검수 Q 가 매 회차 본다)

> «다 옮겨졌다» = 이 표의 모든 줄이 ✅. 줄의 작업이 ✅ 되면 이 표의 그 줄도 ✅ 로 바꾼다(그 워커 몫). **원작에 있는데 이 표에 없는 것을 보면 표에 줄을 더하고 작업을 등재한다** — «없음» 을 남겨 두지 않는다. T33 이 마지막에 전부 대조한다.
> 정본 크기는 2026-09-12 wwwww main 기준(줄 수). 큰 모듈(`scene3d.js` 18,887줄 · `ui.js` 6,181줄 · `icongen.js` 6,704줄)은 한 회차에 안 끝난다 — 워커가 하위 작업으로 쪼개 등재하고 여기에 줄을 더한다.

| 원작 (`web/`) | 무엇 | 작업 | 상태 |
|---|---|---|---|
| `js/balance-data.js` · `gamedata.js` · `mobdata.js` · `data/raw/*`(안 뽑음 · 결정 6ⓑ) | 수치·정의 표 | T2 → JSON · T3 강타입 | ✅ |
| `js/bignum.js` · `util.js` | 큰 수 · 표기 · 난수 | T3 | ✅ |
| `js/voxel.js` · `mobs.js` · `mobs-pets.js` · `mobs-mounts.js` · `mobs-enemies.js` · `mobs-props.js` · `mobs-skillfx.js` | 박스 몹 조립 · 종 표 | T2 · T4 · T5(전 종 세워 보기) | ✅ (T4 · T5) |
| `js/prochar.js`(2,526) | 영웅 박스 모델 · 무기 파지 · 애니 | T6 | ✅ |
| `js/combat.js` · `state.js`(전투 부분) | 전투 틱 · 웨이브 · 보스 | T7 · T8 | ✅ (T7 · T8) |
| `js/scene3d.js`(18,887) · `scene3d-skillfx.js`(1,088) | 3D 세계 전부: 카메라·광원·테마·적 스폰·애니 계약·데미지 숫자·셰이크·파티클·맵·소품·펫 대형·탈것 탑승·스킬 오브젝트·사망 연출·히트 이펙트 | T1(카메라·테마0) · T8 · T9 · T10 · T11 · T12 · T39 | T1 ✅ · T8 ✅(적 스폰·보행·공격·피격·사망·숫자·셰이크·파티클 · 뺀 연출은 T39 ✅) · T9 ✅(SIMPLE_BG 의 보이는 것 · 소재 T34 · 배경 복원 T35) · T34 ✅ · T38 ✅ · T10 ✅(펫 대형·따라오기·관절 드라이버) · T39 ✅(레갈리아·보스 재질·등장 워닝·디졸브·림·플래시·플레어/스파이크/링/점광/그을음·궤적·블롭·암전) · T11 ✅ · T35 ⬜(SIMPLE_BG 복원 전엔 안 잡는다) · T12 ✅(`scene3d-skillfx.js` 전부 + 스킬 디스패처 · 시전 젖힘은 T39) |
| `js/state.js` · `main.js`(저장 시점·부팅) | 세이브 · 마이그레이션 · 오프라인 보상 | T13 | ✅ |
| `js/forge.js` | 대장간 규칙 · 오토 포지 | T14 · T19 | ✅ (T14 · T19) |
| 장비 8부위 · 페이퍼돌(`prochar.js`·`ui.js` 장비 · `scene3d.js` makeWeapon/makeHelmet/dressMcRig) | 등급·서브스탯·판매가·외형 | T15(규칙·표값) · T37(3D 외형 캡처) · T19 | ✅ (T15 · T37 · T19) |
| `js/pets.js` | 알·부화·합성·출전 규칙 · 출전 스탯 기여 | T16 · T20 · T10(출전 조형) · T43(스탯 접착) | T16 ✅ · T10 ✅ · T20 🔄 · T43 ✅ |
| `js/skills.js` | 소환·18종·3슬롯(정본 `MAX_ACTIVE`) | T17 · T20 | T17 ✅ · T20 🔄 |
| `js/mounts.js` | 탈것 규칙 · 탑승 | T40(Core 규칙 · 결정 72) · T11(탑승 3D) · T20(탈것 화면) | T40 ✅ · T11 ✅ · T20 🔄 |
| `js/dungeons.js` | 던전 4종 | T23 · T21 | T23 ✅ · T21 ✅ |
| `js/techtree.js` · `ascension.js` | 기술트리 · 승천 | T24 · T21 | T24 ✅ · T21 ✅ |
| `js/shop.js` · `pass.js` · `quests.js` · `league.js` · `chat.js` | 상점·패스·퀘스트·리그·채팅 | T25 · T22 | ✅ (T25 · T22) |
| `js/ui.js`(6,181) · `css/style.css` · `index.html` | 캔버스·HUD·탭·패널 전부(공개 함수 97개 — T19~T22 절에 이름별로 나눠 적었다) · 메뉴·프로필·설정·디버그 | T18 · T19 · T20 · T21 · T22 | T18 ✅ · T19 ✅ · T21 ✅ · T22 ✅ · T20 🔄 |
| `js/sfx.js`(618) | 효과음 24종(+프리미티브 6) · 음악 4모드 (코드 합성) | T30 | ✅ (`Core/Audio` · `Game/Audio` · `AudioTests` 벡터 대조 · `AudioSmokeTests`) |
| `js/icongen.js`(6,704) · `avatars.js`(831) | 아이콘 136종 · 아바타 24종(`IconGen.draw` 키 160 · «523» 은 도우미까지 센 수) + tint 변형 10 | T31 | ✅ |
| `ref/screens/shot-*.png` 30장 · `tools/shot-*.js` · `ref/UI-SPEC.md` · `ref/POLISH.md` | 원작 화면 정본 · 촬영 도구 · 비율 규격 | T27(촬영) · T28(대조) · T33(완주) | T27 🔄(정본 `shot-screens.js` SCREENS 31줄 이식 · `screens.json` 짝 표) · T28 ⬜ · T33 ⬜ |
| (주인 지시 · 원작 밖 품질 조건) SafeArea · 60fps · 실제 화면 촬영 | 모바일 상단 카메라 회피 · 프레임 예산 · 게임 화면 PNG 를 눈으로 | T45 · T44 · T27 | T45 🔄 · T44 🔄 · T27 🔄(촬영 자리 · 노치 모의는 `UiRoot.NotchSafeArea`) |
| WebGL 배포 · Android | 배포 | T26 | ✅ (굽기 잡 조건 T32 ✅) |
| `lib/three.min.js` · `anvil-*.png`(참고 이미지 · 게임이 안 읽음) · `web/TODO.md` 미완 7항목 | 옮기지 않음 | — | 해당 없음 |
