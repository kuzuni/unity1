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
- 진행(2026-09-12 · 워커 B): 스킬·펫 서브탭 · 소환 결과 연출 · 확률 팝업 · 펫 상세/알 상세/업그레이드 · 세이브 코덱 ✅ — `renderSkillBar`(전투 HUD 스킬 바 · 2회차) ✅ — 탈것 화면(`openMounts`·상세·업그레이드·확률 팝업 탈것 갈래 · 5회차 · T40 위 · 전체 모달 · 결정 137) ✅ — 남은 것: CI 유니티 잡 + 탈것 화면 PNG(T27 촬영 목록) 확인 뒤 ✅ · 내 모달/토스트를 T22 `PopupLayer` 로 합치기.
- 범위: `Assets/Scripts/Game/Ui/Pet*` · `Ui/Skill*` · `Ui/Mount*` · `Assets/Scripts/Core/PetSave/PetSkillSave.cs` · `Assets/Forge/Resources/PetSkillUi.json`(T20 색·배치·문구표 · 결정 70) · `Assets/Tests/EditMode/PetSkillSaveTests.cs` · `Assets/Tests/PlayMode/PetUiTests.cs`.
- T28 5회차 실측(2026-09-12 · 워커 N · 런 95 `screen_pets.png` ↔ `shot-042356`): 부화 칸의 **빛기둥(`.hatch-cone`)이 화면에 안 보인다**(램프 3개는 보인다). `PetPanel.cs` 369 가 `PetHatchCone` 을 만들긴 하니 색 키 `cone_top/bottom` 의 알파 · `cone_h` · 그리기 순서(램프·알 뒤에 깔렸는가)를 PNG 로 확인할 것 — 정본은 전구에서 시작해 셀 하단 77%H 까지 내려오는 초록 사다리꼴(`style.css` 4501~4515 · 빈 칸은 `dim`).

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

### T27 ✅ — PlayMode 스모크·플레이 봇: 모든 화면 열기 · 한 판 플레이(전투→제작→장착→펫→스킬→던전) · 콘솔 빨강 0 · 촬영(`ui-screens/*.png`) (검증 · T19~T22 뒤)
- ✅ 2026-09-12 워커 L (CI 런 83 유니티 잡 초록) — 정본은 `web/tools/shot-screens.js`(화면 31줄 = 원작 `ref/screens/shot-*.png` 30장 짝 + 짝 없는 탈것 · `SEED` 캡처 상태). `PlayLog`(빨강 수집기 · 화면 이름) · `UiShotsTests`(31 화면 열기 + `ui-screens/screen_<이름>.png` + 짝 표 `screens.json`) · `PlaythroughTests`(한 판 봇). T28 은 `screens.json` 에서 짝을 읽는다.
- 정본 각본: wwwww `web/ROUTINES-SETUP.md` §4-C(QA 시나리오). `PlayLog.AssertNoRed` 도우미 · `UiShotsTests`(540×1170 PNG 전 화면) → CI `screens` 브랜치.
- **(주인 지시 2026-09-12 · 가장 먼저)** 촬영은 **실제 게임 화면**이다 — 조형 시트가 아니라 부팅 직후 전투 화면(적·펫·영웅·HUD 가 한 프레임에), 웨이브 진행·보스 등장·스킬 발동 순간, 그리고 패널 전부(대장간·장비·펫·스킬·소환 결과·탈것·던전·기술트리·승천·상점·패스·퀘스트·리그·채팅·프로필·설정). 파일 이름은 원작 샷과 짝이 되게(`shot-042120` ↔ `ui-042120-battle.png` 꼴 · `docs/ref-layout.md` 가 짝 표를 쥔다). 해상도 셋: 540×1170(기본 · 노치 모의) · 360×800 · 430×932(원작 `ref/lvout-*` 과 같은 셋). 노치 모의는 `safeArea` 를 위 120px·아래 60px 깎아 넣고 그 경계선을 반투명 빨강으로 PNG 에 그려 «가림» 이 눈에 보이게. 각 PNG 는 만든 워커가 `Read` 로 열어 본다(§1).
- 판정에 «주인이 볼 것: `screens` 브랜치의 `ui-*-battle.png` 를 폰 화면과 나란히» 한 줄.
- 범위: `Assets/Tests/PlayMode/PlaythroughTests.cs` · `UiShotsTests.cs` · `PlayLog.cs`.

### T28 — 원작 대조 회차: `screens` PNG ↔ `web/ref/screens/shot-*.png` 비율 대조표(`docs/ref-layout.md`) + `tools/ui_score.py` (검증 · T27 뒤)
- aaawunity §5 방식: 화면마다 «요소 · x% · y% · w% · h%» 표를 원작 샷에서 5% 격자로 판독 → 우리 PNG 와 ±3%p 대조 → 점수. **8.0 미만이면 그 화면의 UI 작업을 «다음 고칠 것» 으로 재등재**.
- 범위: `docs/ref-layout.md` · `tools/ui_score.py`.
- ⚠ **원작 샷 30장은 배포 정본보다 낡았다**(2026-09-12 · 3회차 · 워커 K 실측 3건). 낮은 점수를 보면 **먼저 정본 코드를 읽어** «클론이 틀린 것인가 원작 샷이 옛것인가» 를 가른다 — 3회차에 «결함» 으로 보였다가 정본을 읽고 걷어낸 자리 셋:
  - `shot-042632`(상점) 특가 카드의 **젬 보상 pill 3번째 줄** — 정본 `shop.js` 4~8행이 «젬 보급 전면 제거» 로 그 필드를 **일부러 뺐다**. 클론의 2줄이 맞다.
  - `shot-042304`(던전 상세) 보상 «🔨302 · 🪙27.1k» — 정본 `dungeons.js` `rewards()` 는 `n × TechTree.thiefHammerMult/thiefCoinMult` 이고 두 배율의 기본값이 **1**(`techtree.js` 440~441)이다. 기술트리가 빈 클론이 «496 · 496» 으로 **같은 값**을 내는 것이 맞다 — 원작 샷은 `thiefCoin` 에 투자한 세이브의 캡처다.
  - 세계 밴드(나무·흙길·능선)는 결정 131 이 이미 같은 갈래로 적었다(`SIMPLE_BG`).
  - **장비 아이콘의 노란 별(★)**: 정본 `ui.js` 3105 는 `it.stars ?` — **승천 횟수가 있을 때만** 별을 얹는다. 승천이 0인 클론 세이브에 별이 없는 것은 맞다(4회차).
  - **`gear-detail` 의 빨간 ✕**(원작 샷에 없다): 정본이 2026-08-18 에 **더한** 것이고(`renderGearDetail` 주석 «이 팝업만 ✕ 가 없어서 닫는 길이 하나도 없었다»), `style.css .x-btn` 의 `margin-top:-1.7rem` + 주석 «플레이어 정보 ✕도 공용 규칙대로 **카드에 반걸침이 맞다**» 대로 카드에 반쯤 걸치는 것이 정본이다 — 클론이 맞다(4회차).
  - 반대로 **탭바 아이콘 아래 라벨**은 클론이 맞다 — 정본 `index.html` 157~164 가 `<span>PVP</span>` 를 두고 `style.css` 1707 이 `.62rem` 로 그린다(작고 어두워 원작 샷에서 안 보인다).
  - **`tech-branch` 맨 위 행이 노드 1개(모래시계 = `techTimer`)**: 정본 `techtree.js` `rows()` 주석 «트리의 맨 위 행과 맨 아래 행은 언제나 노드 1개»(사용자 지시 2026-08-17) — 원작 샷 042546 은 첫 행이 2노드인 그 이전 캡처다. 클론 `TechTree.Rows` 가 같은 규칙이니 클론이 맞다(5회차).
  - **`pets` 상단 «□ 8/250»**: 정본 `renderPets` 제목이 `펫 ${S.pets.length}/${INV_CAP}` — 한글 두부(T53)일 뿐 맞는 글자다. **부화 칸 1 vs 3** 은 `pets.js` `maxHatchSlots = min(CAP, BASE + S.hatchSlotBonus)` 라 세이브 차이. **`pass` 의 체크 표시** 도 수령 여부 세이브 차이(5회차).
  - **`profile` 오른쪽 위 «▼ 745»**: 정본 `style.css` `.dmg-hero::before { content:'▼' }` + `scene3d.js` 13358 의 **영웅 피해 숫자**다 — 촬영 순간 전투가 돌고 있어 찍혔다(원작 샷은 그 순간이 아니었을 뿐). 결함 아님 · 자리 문제는 T54 절 메모(5회차).

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

### T44 ✅ — 60fps 게이트: `targetFrameRate 60` · vSync 0 · 전투 최대 부하 프레임 예산 테스트 · 프레임당 GC 0 · 드로우콜 상한 (Game·검증 · T8·T10·T12 뒤 · **T27 다음으로 먼저**)
- 주인 지시 2026-09-12 «60fps 로 프레임 돌아야». 지금 `Application.targetFrameRate` 설정이 어디에도 없다(실측 grep 0건).
- 할 것: ⓐ `Bootstrap` 에서 `Application.targetFrameRate = 60` · `QualitySettings.vSyncCount = 0`(WebGL 은 rAF 를 따르므로 그대로) · 모바일 `Screen.sleepTimeout` 원작대로 ⓑ PlayMode `PerfBudgetTests`: 전투 최대 부하 장면(웨이브 보스 + 적 4 + 펫 3 + 스킬 오브젝트 2 + 히트 파티클·데미지 숫자 연속 3초)을 200프레임 돌려 **메인스레드 프레임 시간 평균·p95** 를 `Time.unscaledDeltaTime` 과 `UnityEngine.Profiling.Recorder`(`PlayerLoop`) 로 재고, CI 러너(GPU 없음 · 소프트웨어 렌더)에서는 «CPU 메인스레드 ≤ 8ms 평균 · p95 ≤ 12ms» 를 통과선으로(폰에선 그 2배 여유가 16.6ms 안) · `GC.GetTotalMemory` 차이로 **프레임당 관리 힙 증가 0** 단언 · `UnityStats.drawCalls` 상한(부하 장면 ≤ 150) ⓒ 넘으면 이 작업이 고친다: 복셀 몹 파츠 → 몹당 메시 하나 병합(`VoxelMob.ToMesh` 가 이미 하면 확인만) · 데미지 숫자·파티클 풀링 · `Update` 의 `Find`·`GetComponent`·`string+` 제거 · 머티리얼 공유(색은 정점색) ⓓ 완료 기록에 부하 장면 평균·p95·드로우콜·GC 수치.
- 판정: `PerfBudgetTests` 초록 + 수치 기록 + 「주인이 볼 것: 폰에서 전투 60fps(설정 탭에 FPS 표시 토글 — 원작 디버그 탭에 있으면 그것 · 없으면 넣지 않는다)」.
- 범위: `Assets/Scripts/Game/Bootstrap.cs`(targetFrameRate 두 줄) · `Assets/Tests/PlayMode/PerfBudgetTests.cs` · 넘길 때만 `Assets/Scripts/Game/Battle/`·`Game/Voxel/`·`Game/Ui/Hud.cs`(풀링·병합 갈래).
- ✅ 2026-09-12 워커 H — `Bootstrap.ApplyFrameRate`(vSync 0 + targetFrameRate 60 · WebGL 은 원작 rAF 그대로 · 결정 109) + `PerfBudgetTests` 3개. **CI 런 60 실측**: 메인스레드 게임 시간 평균 **4.123ms** · p95 **9.749ms** · 최대 41.892ms(예산 8/12 안 · 부팅 설정 테스트도 초록) · 렌더러 522 · 공유 재질 212(정본 `matKey` 가 emissive 색을 키에 넣는다 — 갈린 것이 아니다 · 결정 106) · 드로우콜 2754 · 벽시계 27.33ms(소프트웨어 렌더). **프레임당 관리 힙 997,376B 는 목표(≈0) 초과** — 데미지 숫자·파티클에 풀링이 없다(`DamageNumbers.Spawn` 이 피격마다 RectTransform+TMP 를 새로 만든다). 그 파일들(`Game/Battle/`)은 T8·T12·T39 의 **살아 있는 lock** 이라 열지 않고 **T50** 으로 등재했다(규약 «뒤 번호가 기다린다»). 게이트는 회귀 잡이(1.2MB)로 두고 T47 이 목표까지 내린다.

### T45 ✅ — SafeArea 노치 모의 검증: HUD·탭바·팝업 ✕·채팅줄·토스트가 `Screen.safeArea` 안에 있는가 (Game·검증 · T18 뒤 · **T27 다음으로 먼저**)
- 주인 지시 2026-09-12 «SafeArea 해서 모바일 상단 카메라 안 가리게». `UiRoot` 가 `Screen.safeArea` 를 읽어 앱 상자를 놓지만(T18) **노치가 있을 때 정말 안 가리는지 단언하는 테스트가 없다**(에디터·CI 는 safeArea = 전체 화면이라 조용히 초록).
- 할 것: ⓐ `UiRoot` 에 테스트용 safeArea 주입 지점(`UiRoot.OverrideSafeArea(Rect?)` · 게임 코드는 안 쓴다) ⓑ PlayMode `SafeAreaTests`: 위 120px·아래 60px·좌우 0 을 깎은 safeArea 를 주입하고 부팅 → HUD 상단바·스테이지 표시·탭바·시트 ✕·채팅줄·토스트의 **월드 코너 4점이 전부 safeArea 안** · 세 해상도(540×1170 · 360×800 · 430×932) · 가로로 뒤집어도(`Screen.orientation` 은 세로 고정이니 해상도만) ⓒ 3D 카메라는 전체 화면 그대로(레터박스 계산이 safeArea 를 이중으로 깎지 않는지 = T1 `Viewport.Letterbox` 와 T18 앱 상자의 관계를 한 줄로 결정 기록) ⓓ 촬영(T27)의 노치 모의 경계선과 같은 값을 쓴다(상수 하나 · `UiCatalog` 또는 테스트 공용 상수).
- 판정: `SafeAreaTests` 초록 + 노치 모의 PNG 를 열어 상단바가 빨간 선 아래에 있는 것을 본 기록 + 「주인이 볼 것: 노치 폰에서 상단바·✕ 가 카메라에 안 가림」.
- 범위: `Assets/Scripts/Game/Ui/UiRoot.cs`(주입 지점만) · `Assets/Tests/PlayMode/SafeAreaTests.cs` · `Assets/Forge/catalog.json`(노치 상수 한 줄 · 있으면).
### T46 ✅ — PlayMode 실패 진단 로그: 실패 테스트의 «왜» 를 `screens` 브랜치에서 읽는다 (검증 · 뒤 순서 없음)
- 왜: CI 유니티 잡 로그는 **꼬리 5000줄** 만 남아(2026-09-12 실측 · 러너가 결과 XML 을 그 앞에 다 찍는다) 실패 픽스처의 메시지·스택이 잘려 나간다. 아티팩트(`unity-test-results`)는 워커 컨테이너의 프록시가 막는다. 그래서 T8 의 «테스트 결과 요약» 스텝은 **이름만** 알려 준다 — 런 46 의 PlayMode 26 빨강에서 임자마다 이유를 추측으로 파야 했다.
- 무엇: PlayMode 어셈블리에 어셈블리 단위 `ITestAction`(NUnit) 하나를 두어 테스트마다 ⓐ 결과(Outcome·Message·StackTrace) ⓑ 그 테스트가 도는 동안 온 **콘솔 빨강**(`Application.logMessageReceived` 의 Error/Exception/Assert · condition + stackTrace)을 `ui-screens/playmode-red.txt` 에 **줄마다 바로 덧붙인다**(런이 중간에 죽어도 남는다). CI 가 `ui-screens/` 를 `screens` 브랜치로 올리므로(§5) 다음 워커는 `git show origin/screens:playmode-red.txt` 로 이유를 읽는다 — **ci.yml 은 건드리지 않는다**(그 «요약 스텝» 은 T8 범위다).
- 통과한 테스트는 한 줄(`PASS <이름>`)만 남긴다 — 파일이 붙는 순서가 곧 실행 순서라 «어느 테스트가 세이브를 오염시켰나» 를 뒤에서 읽을 수 있다.
- 판정: `dotnet build`·`dotnet test` 초록(스텁에 쓰는 서명이 있는가) + CI 유니티 잡이 돈 뒤 `screens` 브랜치에 `playmode-red.txt` 가 서고 그 안에 런 46 류의 실패 이유가 적혀 있다.
- 유니티 갈래는 **NUnit 어셈블리 특성이 아니라** `[assembly: TestRunCallback(typeof(RedLogCallbacks))]`(`UnityEngine.TestRunner.ITestRunCallback`)다 — UTF 의 `TestActionCommand` 는 `ITestAction` 을 **테스트 메서드에서만** 모아(정본 패키지 실측) 어셈블리 단위는 한 번도 안 불린다(런 64 실측 · 결정 124).
- 범위: `Assets/Tests/PlayMode/RedLog.cs` · `tools/dotnet/Stubs/TestRunner.cs` · `tools/dotnet/TestsPlay/Forge.TestsPlay.csproj`(스텁 Compile 한 줄).

### T48 ✅ — 게이트: PlayMode 테스트도 dotnet 하니스로 컴파일한다 (검증 · 뒤 순서 없음 · 워커 L 등재)
- 지금 `tools/dotnet` 는 `Assets/Tests/EditMode/**` 만 컴파일한다 — **PlayMode 테스트의 오타·잘못된 서명은 §3 게이트를 전부 초록으로 통과하고 유니티 CI 에서야 터진다**(§1 «컴파일 파손을 남기지 않는다» 가 가장 잘 뚫리는 자리 · 워커마다 PlayMode 파일을 쓴다).
- 할 일: `tools/dotnet/TestsPlay/Forge.TestsPlay.csproj`(`Assets/Tests/PlayMode/**/*.cs` + `Forge.Game` 프로젝트 참조 + NUnit 3.6.1 · **테스트를 돌리지 않는다 · 컴파일만**) · `tools/dotnet/Stubs/TestTools.cs`(`UnityTestAttribute` · `LogAssert.ignoreFailingMessages`·`Expect(LogType,string)`·`Expect(LogType,Regex)`·`NoUnexpectedReceived`) · `Forge.sln` 에 추가 · §3 게이트 목록에 한 줄.
- ⚠ 스텁이 실물 `UnityEngine.TestTools` 와 다르면 «로컬만 초록» 이 또 생긴다 — 새 API 를 쓰면 스텁에도 같은 서명을 더한다(§1 패키지 규칙과 같은 갈래).
- 실측(2026-09-12 워커 L · T27 회차): 스크래치에 이 꼴로 세웠더니 `Forge.Core.Rng`(진짜는 `Forge.Core.Data.Rng`) 오타가 그 자리에서 잡혔다 — 없었으면 유니티 CI 한 바퀴(30분)를 태웠다.
- 범위: `tools/dotnet/TestsPlay/` · `tools/dotnet/Stubs/TestTools.cs` · `tools/dotnet/Forge.sln` · `docs/ROUTINE.md`(§3 한 줄) · `docs/PROGRESS.md`.


### T47 ✅ — 카탈로그 색 44키가 `layout` 배열에 빠져 있다: `DungeonUiTests` 4/4 빨강(«색 «white»·«pill_potion»·«muted2» 이 없다») (검증·UI · 뒤 순서 없음 · 임자 없는 빨강)
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
### T50 ✅ — 프레임당 관리 힙 0 으로: 데미지 숫자·큐브 파티클·연출 풀링 (Game·성능 · T44 가 잰 것 · T8·T12·T39 lock 이 풀린 뒤)
- 왜: 주인 지시 «60fps» 의 남은 절반. T44 `PerfBudgetTests` 가 CI 런 60 에서 **프레임당 관리 힙 997,376B** 를 쟀다(전투 최대 부하 · 메인스레드 시간 자체는 평균 4.1ms 로 예산 안). 1MB/프레임이면 폰에서 몇 초마다 GC 멈춤이 오고 그때 프레임이 튄다(같은 측정의 «최대 41.9ms» 프레임이 그 꼴이다).
- 어디: ⓐ `DamageNumbers.Spawn` — 피격마다 `new GameObject` + RectTransform + TMP 를 만들고 끝나면 버린다(런 60 에서 측정 200프레임에 126개). 죽은 것을 되쓰는 풀로 바꾸고 문자열도 프레임마다 `string +` 를 하지 않는 갈래로(ROUTINE §1) ⓑ `CubeParticles`·`ImpactFx`·`FxAnims`·`TrailFx`(리본 정점 배열)가 프레임마다 배열을 새로 잡는지 본다 ⓒ `Battle.Tick` 이 매 틱 부르는 `AliveEnemies()` 같은 «호출마다 새 List» 를 재사용 버퍼로.
- 방법: 고치기 전에 무엇이 얼마나 무는지부터 잰다 — `PerfBudgetTests` 의 측정 루프를 갈래별로(숫자만 · 파티클만 · 스킬 연출만) 돌려 바이트를 가른 뒤 큰 것부터. 원작에 없는 시스템을 더하지 않는다(풀은 «같은 것을 되쓰기» 일 뿐 새 콘텐츠가 아니다).
- 판정: `PerfBudgetTests` 의 프레임당 관리 힙이 `GcTargetPerFrame`(16KB) 아래 + 그 상한(`GcPerFrameCap`)을 같이 내린다 + 평균·p95 가 지금보다 나빠지지 않는다 + 완료 기록에 갈래별 바이트.
- 범위: `Assets/Scripts/Game/Battle/DamageNumbers.cs`·`CubeParticles.cs`·`TrailFx.cs`·`HitFlashFx.cs` · `Assets/Scripts/Core/Battle/Battle.cs`(버퍼 재사용만) · `Assets/Tests/PlayMode/PerfBudgetTests.cs`(상한 두 수).


### T51 ✅ — CI 구멍 셋: 부르지 않는 자기 검사 둘(`ui_score --self-test` · `export_data --self-test`) · PNG 0장인 런이 진단 로그를 버린다 (게이트 · 뒤 순서 없음 · `ci.yml` 한 파일)
- 왜 ⓐ: `tools/ui_score.py`(T28 · 1,000줄 · 자기 검사 15칸)를 **CI 가 안 부른다**. 이 레포의 검사 자는 전부 `--self-test` 가 dotnet 잡에 걸려 있는데(`gen_ui_catalog`·`check_docs_intact`·`task_state`·`check_decisions`·`check_task_rows`·`check_final_table`·`check_claim_scope` 7개) `ui_score` 만 빠졌다 — T28 이 «`ci.yml` 은 내 범위 밖» 이라 남긴 한 줄이다(T28 진행 기록 끝).
- 왜 ⓑ: §3 게이트 목록에 있는 `node tools/export_data.js --self-test`(펫 25 · 탈것 29 · 적 7 이 나오는가)도 CI 에 없다. datasync 잡은 `check_data_sync.sh`(정본과 바이트 대조)만 부른다 — 그것은 «정본과 같은가» 이지 «추출기가 제 수를 내는가» 가 아니다.
- 왜 ⓒ: `screens` 배포 조건이 `steps.shots.outputs.count != '0'`(= **PNG 개수**)이라 촬영 픽스처까지 죽은 런에서는 T46 의 `playmode-red.txt` 가 **안 올라간다** — 진단 로그가 가장 필요한 런에서 정확히 사라진다(T46 완료 기록이 «다음 사람 몫» 으로 남긴 한 줄). 다만 배포는 `force_orphan: true` 라 «아무 파일이나 있으면 올린다» 로 넓히면 PNG 0장인 런이 **지난 PNG 를 통째로 지운다** — 그래서 PNG 가 0장이면 지난 `screens` 의 PNG 를 먼저 받아 담고(이어붙임) 올린다.
- 판정: `ci.yml` 만 바뀐다(코드·테스트 0줄) + 세 자기 검사가 로컬에서 rc 0 + 다음 main 런에서 dotnet·datasync 잡이 초록.
- 범위: `.github/workflows/ci.yml`.


### T52 ✅ — 시전 젖힘 채널: 원작 `skillCastBeat` 의 `heroG.rotation.z` 시전 포즈(−0.14 · 지원계 −0.07 · 릴리즈 +0.30 스냅) (Game · T12·T39 가 뺀 것 · T8 lock 반납 뒤)
- 왜: 정본 `scene3d.js` `skillCastBeat`(13,701~13,730행)는 시전 1박 동안 영웅 몸을 `heroG.rotation.z` 로 젖히고(평타 `_attacking` 중엔 양보 · 지원계는 절반) 2박 발화 시각에 +0.30 으로 «휙» 내지른 뒤 0.14초에 제자리로 — 몸과 이펙트가 같은 프레임에서 만난다(원작 주석 «3차 채점 A 치명 지적»). T12 는 T8 `HeroView` 가 리그 회전을 매 프레임 덮어써 T39 로 넘겼고(결정 90ⓒ) T39 는 그 채널 없이 ✅ 됐다 — §7 «원작에 있는데 없는 것» 이라 등재.
- 방법: `HeroView` 에 젖힘 채널 하나(`SetLean(z)` · 돌진·넉백이 z 를 쥐고 있으면 양보 · 원작과 같은 우선순위) · `ISkillFxStage.HeroAttacking`/`HeroLean(z)` · `SkillFxDirector.SkillCastBeat` 의 프레임 함수와 onDone 에 원작 수치 그대로. 새 콘텐츠·수치 0.
- 판정: PlayMode `SkillFxTests` 에 «시전 1박 끝 젖힘 −0.14(지원계 −0.07) → 릴리즈 → 0.14초 뒤 0» 단언 · 콘솔 빨강 0 · CI 유니티 잡.
- 범위: `Assets/Scripts/Game/Battle/HeroView.cs`(젖힘 채널 추가) · `Assets/Scripts/Game/SkillFx/SkillFxDirector.cs`·`SkillFxScene.cs` · `Assets/Tests/PlayMode/SkillFxTests.cs`.

### T53 — 한글 글꼴: 화면 글자가 전부 네모(□)다 (Game · T18 뒤 · **주인 에셋 대기**)
- 실측(2026-09-12 22:12 · `screens/ui_safearea_notch.png`): 스테이지·탭·버튼·토스트 등 **모든 한국어 라벨이 두부**. 원인은 결정 8 — 주인 글꼴 `NotoSans-Regular.ttf` 에 U+AC00 한글 구간 cmap 이 없고, 리눅스 CI·WebGL 에는 폴백할 OS 한글 글꼴도 없다.
- 워커가 할 수 없는 것: 글꼴 파일을 새로 들이는 것(§1 «에셋은 주인 에셋만»). **주인이 `Assets/Fonts/` 에 한글 TTF 를 넣어 주면** 이 작업은 `catalog.json` 의 `font` 한 줄 + `UiFont.Build` 폴백 정리 + PlayMode 단언(라벨 문자열의 글리프가 폰트에 **있는가** · 없으면 실패)으로 끝난다.
- 주인 조치 전에는 이 작업을 잡지 마라 — 잡으면 «주인 에셋 대기» 로 즉시 반납한다. 그동안 T27·T28 의 화면 대조는 **글자를 빼고 배치만** 본다.
- 실측 보탬(2026-09-12 · T28 2회차 · 워커 M): 촬영 30장 **전부** 같다(`screen_main.png` «□□ 1-1» ↔ 원작 «어려움 4-1» · 탭 5개 · `settings` 토글 8줄 · `shop` 상품 제목). 원인 자리는 `UiKit.Font.Build`(`Ui/UiKit.cs` 204~228)의 **OS 글꼴 폴백** — 리눅스 러너에 한글 글꼴이 없어 경고 한 줄로 지나가고, **WebGL 배포본에는 OS 글꼴 자체가 없어 주인 폰에서도 같은 그림**이다. 주인 글꼴이 들어오기 전에도 «폴백이 비면 경고가 아니라 빨강» 한 줄은 이 작업이 먼저 넣을 수 있다.
- 범위: `Assets/Fonts/`(주인) · `Assets/Forge/catalog.json`(font) · `Assets/Scripts/Game/Ui/UiFont.cs` · `Assets/Tests/PlayMode/TextSizeGateTests.cs`.

### T54 — 전투 화면에 영웅·적·펫이 하나도 안 선다 (Game·검증 · T8·T10 뒤 · **가장 먼저**)
- 실측(2026-09-12 22:12 · 같은 PNG): 3D 자리에 초록 HP 바 한 줄과 지면 그라디언트뿐 — **영웅도 적도 펫도 화면에 없다**. T8(전투 씬)·T10(펫 출전)은 ✅ 이고 `BattleSceneTests` 도 초록이다: 테스트가 «오브젝트가 만들어졌는가» 만 보고 **화면에 그려지는가** 를 안 봤다(§1 «실제 화면을 본다» 가 잡으라던 바로 그 종류).
- 규명 순서: ⓐ 촬영 시점에 전투가 시작됐는가(부팅 → 웨이브 스폰까지 몇 초 기다렸는가 · `UiShots` 가 너무 일찍 찍었을 수 있다) ⓑ 영웅·적 GameObject 가 씬에 있는데 안 보이는가(카메라 절두체 밖 · z 부호 · 스케일 0 · 머티리얼 null · 레이어 컬링 · UI 캔버스가 3D 를 덮는가 — 이 PNG 는 장비 시트가 열린 상태라 **시트가 덮었을 가능성이 가장 크다**) ⓒ 그렇다면 시트를 닫은 전투 단독 컷을 먼저 찍어 다시 본다(T27 과 짝).
- 고침 뒤 **촬영 단언**을 넣는다: 전투 컷의 화면 중앙 60% 영역 픽셀이 배경·지면 색만이 아니어야 한다(영웅 실루엣이 있어야 한다) · 적 스폰 뒤 컷은 적 색 픽셀이 N개 이상. 이 단언이 «비었는데 초록» 을 앞으로 막는다.
- 판정: 전투 단독 PNG 에 영웅·적·펫이 보이고(워커가 열어 본 기록) · 위 단언 초록 · CI 초록.
- 범위: `Assets/Scripts/Game/Battle/` · `Game/Hero/` · `Game/Pets/` · `Assets/Tests/PlayMode/BattleSceneTests.cs` · `UiShots*`.
- T28 5회차 메모(2026-09-12 · 워커 N · 런 95 `screen_profile.png`): 영웅 피해 숫자 «▼ 745» 가 **화면 오른쪽 위(x≈91%W · y≈19%H)** 에 찍혔다 — `HeroView.cs` 121 은 영웅 x + HP 바 위에 띄우므로 영웅이 그 자리에 있다는 뜻이다(메인 컷의 초록 HP 바는 왼쪽 아래 y≈50%H). 영웅 자리를 잡을 때 이 숫자 자리도 같이 본다.

### T55 ✅ — 전투 ↔ 세이브 접착의 나머지: 처치·재화·보스 클리어·진행·장착 스킬·자동시전·무기 종 (Game · T8·T13·T43 뒤 · 워커 L 등재)
- 실측(2026-09-12 · T27 플레이 봇 · CI 런 78): `BattleScene.MakeBattle` 은 `new BattleContext(defs, save) { HeroStats = stats, WeaponType = "sword" }` 로 **빈 문맥**을 세운다. 그래서 원작이 전투 중에 `S` 에 직접 쓰던 것이 유니티에서는 어디에도 안 남는다 — 한 판을 돌려 적을 잡아도 `SaveIo.State.Kills` 가 0 이다(정본 `combat.js:445` `S.kills++`).
- 안 이어진 칸: `Kills`·`Coins`·`Hammers`(전투 드랍) · `ClearedBosses`(첫 클리어 키) · `Progress`(챕터·스테이지·난이도·최고 기록) · `EquippedSkills`·`AutoCast`(세이브의 장착 스킬 3 · 자동시전 토글) · `WeaponType`(지금 `"sword"` 고정 — 장착 무기의 `wtype` 이어야 한다) · `Dungeon`(던전 판) · `Save`(원작 `saveGame()` 훅).
- 할 일: `BattleSaveGlue`(Game) 하나가 부팅 때 문맥의 그 칸들을 세이브에서 읽어 채우고, 전투가 올린 값을 세이브로 되돌린다(T43 `HeroStatsGlue` 와 같은 꼴 · 규칙은 Core 가 쥔다 · 씬은 접착만). 되돌리는 시점은 원작 `main.js` 의 저장 시점(T13)과 같게.
- 판정: PlayMode — 한 판 돌려 `S.kills`·`S.coins`·`S.hammers` 가 오르고, 보스 첫 클리어가 `S.clearedBosses` 에 남고, 장착 무기를 바꾸면 전투 문맥의 무기 종이 따라 바뀐다. T27 `PlaythroughTests` 의 «세이브 kills» 단언을 되살린다.
- 범위: `Assets/Scripts/Game/Battle/BattleSaveGlue.cs`(새) · `Assets/Scripts/Game/Battle/BattleScene.cs`(`MakeBattle` 문맥 채우기 갈래만) · `Assets/Tests/PlayMode/BattleSaveGlueTests.cs` · `Assets/Tests/PlayMode/PlaythroughTests.cs`(단언 한 줄 되살리기) · `Assets/Scripts/Core/Battle/BattleSaveSync.cs`(규칙 · Core) · `Assets/Scripts/Core/Battle/BattleContext.cs`·`Battle.cs`·`Assets/Scripts/Core/Dungeons/Dungeons.cs`(던전 판 라벨 `DungeonRun.Label` 한 줄씩) · `Assets/Tests/EditMode/BattleSaveSyncTests.cs`.
- ✅ 2026-09-12 워커 N — 규칙은 Core `BattleSaveSync`(거울+delta · 진행 우선순위 · 합집합 · 무기 종 · EditMode 6) · 씬은 `BattleSaveGlue`(MakeBattle Fill · Stepped 뒤 Sync · saveGame 훅 · T23 `Dungeons.Attach/RestoreRun` · 클리어 팝업 → `FinishDungeonClear` · PlayMode 1) · 장착 스킬·자동시전은 T20 그대로(결정 139) · 손의 무기 메시는 T37/T15(결정 141).

### T56 — HUD 상단바 프로필 카드에 아바타가 안 그려진다 (Game·UI · T18·T31 뒤 · 실제 화면 실측)
- 실측(2026-09-12 22:20 · 워커 J · `screens/screen_main.png`·`screen_profile.png` 를 열어 본 것): 상단바 왼쪽 프로필 카드의 아바타 자리가 **빈 흰 사각형**이다. 같은 PNG 의 프로필 팝업·리그 순위표·채팅에는 도트 초상이 제대로 그려진다 — T31 아틀라스는 멀쩡하고 **HUD 만 안 그린다**.
- 원인: `Hud.Build` 가 아바타 타일을 손으로 짠다(`UiKit.Rounded` 테 + `avatar_bg` 면 두 줄) — 남들이 쓰는 `PopupKit.Avatar(... , emoji, ...)` 의 초상 스프라이트 갈래가 없다. 정본 `ui.js:1290 renderTopBar` 는 `<span class="avatar">${IconGen.avatar(S.avatarEmoji)}</span>` 로 닉네임·전투력과 **같이** 그리고, `onPickAvatar`(`ui.js:5063`)가 아바타를 바꾸면 `renderTopBar()` 를 다시 불러 상단바도 따라 바뀐다.
- 할 일: ⓐ `Hud` 에 `SetAvatar(string emoji)` — 기존 아바타 타일 안에 `UiIcons.Avatar(emoji)` 초상을 넣고(없으면 지금처럼 빈 타일) 다시 부르면 갈아끼운다 ⓑ `MetaHost.Sync` 가 닉네임·전투력과 같은 자리에서 `AvatarEmoji` 도 밀어 준다(바뀔 때만 · 아바타 고르기 뒤 상단바가 따라 바뀌는 정본 행동) ⓒ PlayMode 단언: 부팅 뒤 HUD 아바타 타일에 초상 `Image` 가 서고 그 스프라이트가 `UiIcons.Avatar(기본 아바타)` 와 같다 · 아바타를 바꿔 `Sync` 하면 스프라이트가 따라 바뀐다.
- 판정: 위 단언 초록 + 다음 회차에 `screen_main.png` 를 열어 상단바에 초상이 보이는 것을 본 기록 + 콘솔 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/Hud.cs`(아바타 타일·`SetAvatar`) · `Assets/Scripts/Game/Ui/MetaHost.cs`(Sync 한 줄) · `Assets/Tests/PlayMode/HudAvatarTests.cs`(새).

### T57 — 대장간·장비 팝업이 제 판(카드) 없이 배경 위에 글자를 겹쳐 그린다 + 빈 검은·흰 막대 둘 + ✕ 가 둘 (Game·UI · T19 뒤 · T28 2회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 2회차 · 워커 M · 런 78 PNG): `screen_forge-detail.png` **1.8/10** — 원작(`shot-042931`)은 딤 위 **흰 카드 한 장**에 아이템 + 서브옵션 12줄인데, 클론은 목록 격자와 상세 글자가 **같은 자리에 겹쳐** 읽을 수 없고 ✕ 가 위아래로 둘이다. `screen_craft-compare.png` **3.1/10** — 비교 카드 둘 중 **두 번째 카드에 판이 없어** «35.6m …»·«+30% …» 가 3D 배경과 장비 격자 위에 떠 있다. `screen_gear-detail.png` **2.7/10** · `screen_autoforge-filter.png` **3.7/10** — 카드 위에 **속 빈 검은 막대 + 흰 막대**가 하나씩 떠 있다(원작에 없는 자리).
- 참고(오판 방지): 장비 상세가 **딤 없이** 격자 위에 뜨는 것 자체는 원작 그대로다(`shot-043244` 실측) — 문제는 «판이 없다 · 글자가 겹친다 · 빈 막대가 뜬다 · ✕ 가 둘» 이다.
- 무엇을 한다: 원작 `ui.js` 의 해당 팝업(`openGearDetail`·`showCraftCompare`·`openForgeDetail`·자동 제련 필터)이 **카드 한 장**을 먼저 세우고 그 안에 줄을 놓는 순서를 그대로 옮긴다. 빈 막대의 정체(폭 0 배치·라벨 없는 pill)를 찾아 없앤다. ✕ 는 화면당 하나.
- 판정(2026-09-12 워커 F 가 실측으로 고쳐 적었다 · 결정 159): 워커가 PNG 를 `Read` 로 열어 «판 있음 · 글자 겹침 0 · 빈 막대 0 · ✕ 하나» + `ForgeUiTests` PlayMode 빨강 0. **`ui_score` 8.0 은 이 작업의 판정이 아니다** — 채점 자는 화면 전체(3D 세계·글자 잉크)를 재고, 원작 샷은 `SIMPLE_BG` 이전 캡처라 나무·흙길이 가득한데 클론 배경은 T35(주인이 `SIMPLE_BG` 를 끌 때만)라 비어 있고 한글은 T53(주인 글꼴) 전까지 두부다. 그 둘이 팝업보다 점수를 크게 움직인다(런 95 실측: 팝업 결함 넷을 다 없앴는데 평균 2.88 → 2.96). 화면 점수는 T28 이 그 둘과 함께 본다.
- 범위: `Assets/Scripts/Game/Ui/Forge*`(ForgeInfoPopup · ForgeCraftPopup · ForgeAutoPopup · ForgeUi) · `Ui/Gear*` · `Assets/Tests/PlayMode/ForgeUiTests.cs`.

### T58 — 리그 도전·펫 업그레이드 팝업이 판 없이 부모 목록 위에 겹친다 (Game·UI · T20 lock 이 풀린 뒤 · T22 뒤 · T28 2회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 2회차 · 워커 M · 런 78 PNG): `screen_league-challenge.png` **1.9/10** — 원작(`shot-042228`)은 **흰 카드**에 «상대 선택» + 티켓 pill + 상대 5줄인데, 클론은 카드도 제목도 없이 상대 5줄만 리그 순위표 위에 얹혀 두 목록이 서로 겹친다. `screen_pet-upgrade.png` **1.7/10** — 카드는 있으나 ✕ 가 **위아래로 둘**(하나는 탭바 위)이고 원작(`shot-042503`)과 머리 구성이 다르다.
- 무엇을 한다: 원작 `ui.js` `openLeagueChallenge`·`openPetUpgrade`·`renderPetUpgrade` 의 카드·제목·티켓 줄을 그대로. ✕ 는 화면당 하나.
- 판정: 두 화면 `ui_score` **8.0 이상** + PNG 눈 확인 + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/League*` · `Ui/PetUpgrade*`(T20 이 쥔 `Ui/Pet*` 와 겹친다 — **T20 lock 이 풀린 뒤에 잡는다**) · `Assets/Tests/PlayMode/PetUiTests.cs`.

### T59 ✅ — 수 표기 둘: 서브스탯이 `+7.699999999999999%` · 확률이 전부 `0.0000%` (Game·UI · T15·T19 뒤 · T28 2회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 2회차 · 워커 M · 런 78 PNG): `screen_player-info.png` 에 «+7.699999999999999% …» 줄이 그대로 찍힌다. 정본 `ui.js` 5182행은 `value: +stats.subs[key].toFixed(1)` 로 **소수 한 자리**를 만든 뒤 찍는다 — 클론은 double 을 그대로 이어 붙인다. 같은 화면의 `+15.4%`·`+46.8%` 는 우연히 짧게 떨어진 값이다.
- 둘째: `screen_forge-detail.png`·`screen_forge-info.png` 의 확률이 **전부 `0.0000%`** 다(`ForgeInfoPopup.cs` 215·276행의 `"0.0000"` 서식은 원작과 같은 자리수지만 값이 0 이다) — 표에서 확률을 못 읽어 오는 갈래인지 시대·등급 인자가 비어 있는지 본다.
- 무엇을 한다: 서브스탯 줄은 정본과 같은 반올림(`toFixed(1)` 상당 · `NumFmt` 갈래)을 쓰고, 확률 0 의 원인을 잡는다. 수치는 코드에 박지 않는다(§1).
- 판정: EditMode 표 테스트(정본 값 ↔ 표기 문자열) + PNG 눈 확인(«+7.7%» · 확률이 0 이 아님) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs` · `Assets/Scripts/Game/Ui/ForgeInfoPopup.cs` · `Assets/Scripts/Core/BigNum.cs`(필요하면 표기 함수만) · `Assets/Tests/EditMode/`.
- ✅ 2026-09-12 워커 S: ⓐ 합계 줄은 `ForgeHost.SubLines`(PlayerInfoPopup 은 줄을 받기만 한다)가 만든다 → 원작 순서·`toFixed(1)`·`> 0` 으로 고침(`NumFmt.RoundFixed/Fixed`). ⓑ 확률 «전부 0.0000%» 는 버그가 아니었다 — 런 78 PNG 의 목록은 **Lv29 의 원시 시대 한 절**(그 레벨 확률표에서 0%)이고 원작 `itemDropChance(...).toFixed(4)` 도 같은 값 · EditMode 가 Lv29 전 시대·부위를 식으로 잰다(결정 138).

### T60 — 재화 알약의 초록 «+»(상점 열기) 배지가 없다 (Game·UI · T18·T31 뒤 · 검수 Q 등재)
- 실측(2026-09-12 22:1x · 검수 Q · 런 79 `screen_main.png`·`ui_safearea_notch.png` ↔ 정본 `ref/screens/shot-042120.png`·`shot-042356.png`): 정본 상단바의 코인·젬 알약은 아이콘 오른쪽 아래에 **초록 원 «+» 배지**를 달고 있고 그것이 상점으로 가는 버튼이다. 클론 상단바에는 그 자리 자체가 없다 — 알약이 «아이콘 + 숫자» 뿐이다(숫자 색 코인 `#ffd54f`·젬 `#ff8a80` 은 정본 CSS 와 맞다 · 색은 문제 아님).
- 정본: `js/ui.js:1285` `curIcoPlus(kind)` = `<span class="pill-ico">{아이콘}<button class="pill-plus" onclick="UI.openShop()" aria-label="상점 열기">{IconGen.img('plus')}</button></span>` · `renderTopBar`(1300~1302행)가 코인·젬 둘 다에 쓴다. 치수는 `css/style.css:131` `.pill-plus`(`right:-.38rem` · `bottom:-.11rem` · `.74rem` 정사각)이고 그 CSS 주석이 **원본 실측 역산**을 적어 두었다 — 십자 폭 = 원판 지름의 **0.49배**, 중심은 원판 중심에서 `(+0.51, +0.33)×지름`.
- 할 일: `Hud` 의 코인·젬 알약 아이콘에 «+» 배지 버튼을 위 비율로 얹고 누르면 상점을 연다(`ShopSheet` 는 `ShopSheet.cs:140` 에 이미 같은 조각을 «원작 curIcoPlus» 로 갖고 있다 — 그 자리를 공용으로 빼 쓰면 두 번 안 짠다). 아이콘 키는 T31 아틀라스의 `plus`. **치수·색은 `catalog.json` 으로**(§1 — 코드에 숫자 금지 · `gen_ui_catalog` 로 갱신).
- 판정: PlayMode 단언(코인·젬 알약에 «+» 자식이 있고 누르면 상점 팝업이 열린다 · safeArea 안) + 다음 회차에 `screen_main.png` 를 열어 배지가 보이는 것을 본 기록 + 콘솔 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/Hud.cs` · `Assets/Scripts/Game/Ui/ShopSheet.cs`(공용 조각으로 빼는 갈래만) · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/UiSmokeTests.cs`.

### T61 — 모루의 망치 수가 안 읽힌다: 밝은 시트 위 흰 글자 · 모루에 받침이 없다 (Game·UI · T19·**T57 뒤** · 검수 Q 등재)
- 실측(2026-09-12 22:1x · 검수 Q · 런 79 `screen_main.png` 확대): 장비 시트 바닥(밝은 회색)에 «🔨 302k» 가 **거의 흰색**으로 찍혀 배경과 구별이 안 되고 모루 그림에 글자 왼쪽이 반쯤 물린다. 정본 `ref/screens/shot-042120.png` 은 같은 글자(«🔨 41307»)를 **모루의 어두운 몸통 위**에 얹어 흰 글자가 읽힌다 — 정본 모루는 붉은 상판 + 어두운 몸 + **회색 돌 받침** + 검은 외곽선이고, 클론 모루는 갈색 두 덩이뿐이라 글자가 시트 바닥으로 흘러내렸다.
- 할 일: 모루 조형(상판·몸·돌 받침·외곽선)과 망치 수 자리를 정본 실측(`ref/screens/shot-042120.png` · `web/tools/anvil-*.png` · `probe-anvil-ref.js`)대로 맞춘다. 색·치수는 `catalog.json`(§1).
- **T57 뒤인 이유**: T57 의 범위 `Assets/Scripts/Game/Ui/Forge*` 글로브가 `ForgeSheet.cs` 를 덮는다 — 규약 «두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다»(`docs/claims/README.md`).
- 판정: 촬영 PNG 를 열어 «🔨 <수>» 가 어두운 받침 위에서 읽힌다 + 대비 단언(글자 픽셀과 그 뒤 배경의 밝기 차 ≥ 문턱) 한 줄 + `ui_score.py --score` 의 `main` 점수가 안 내려간다.
- 범위: `Assets/Scripts/Game/Ui/ForgeSheet.cs`(모루 자리) · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/ForgeUiTests.cs`.

### T62 — 상점 시트: 특가 카드가 원작보다 높아 «보석» 절(젬 상품 3종)이 화면 밖으로 밀린다 (Game·UI · T22·T25 뒤 · T28 3회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 3회차 · 워커 K · 런 83 PNG): `screen_shop.png` **5.3/10**. 원작(`shot-042632`)의 특가 카드는 `min-height: app-h × .1528` · 카드 사이 `gap: app-h × .0091`(정본 `style.css` 2912~2921)인데 클론 카드는 그보다 한참 높고 간격도 넓어, 원작에서 66%H 자리에 있던 «보석» 배너와 젬 카드 3장(`shop-gems`)이 **화면 밖으로 밀려 아예 안 보인다**.
- 무엇을 한다: 특가 카드 높이·간격·안쪽 여백을 정본 `.shop-deal-card`/`.shop-deals` 비율 그대로.
- ⚠ **건드리지 않는 것 둘**: ⓐ 특가 카드의 «젬 보상 pill 3번째 줄» — 정본 `shop.js` 4~8행이 «젬 보급 전면 제거» 로 일부러 뺐다(원작 샷이 옛것 · T28 절 메모) ⓑ 재화 알약의 초록 «+» 배지 — **T60** 몫이다.
- 판정: `ui_score --score` 로 `shop` **8.0 이상** + PNG 눈 확인(«보석» 배너와 젬 카드가 화면 안에 있다) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/ShopSheet.cs` · `Assets/Forge/catalog.json`(배치 값) · `Assets/Tests/PlayMode/ShopUiTests.cs`.

### T63 — 메인 HUD 둘: 전투력이 `⚔ 0` · 채팅 프리뷰가 한 줄이고 «99» 뱃지가 없다 (Game·UI · T18·T22 뒤 · **T56·T60 lock 이 풀린 뒤**)
- 실측(2026-09-12 · T28 3회차 · 워커 K · 런 83 `screen_main.png`):
  - ⓐ 프로필 카드의 전투력이 **`⚔ 0`** 이다 — 같은 화면의 장비 그리드에는 Lv.26~28 이 8부위 장착돼 있다. 정본 `renderTopBar`(`ui.js` 1290~1303)는 `Combat.combatPower()` 를 찍는다.
  - ⓑ 채팅줄이 «Zephyr: anyone want to trade tickets?» **한 줄**이고 말풍선에 뱃지가 없다. 정본 `renderChatPreview`(`ui.js` 5288~5299)는 말풍선 + **«99» 뱃지** + 이름 줄 / 메시지 줄 **두 줄**이다.
- 무엇을 한다: ⓐ 는 HUD 가 전투력을 T43·T55 가 세운 접착(`GearSystem.HeroStats` 갈래)에서 끌어오게 한다 — 수치를 코드에 박지 않는다(§1). ⓑ 는 정본 두 줄 + 뱃지.
- ⚠ `Ui/Hud.cs` 가 **T56**(아바타)·**T60**(재화 «+» 배지)와 같은 파일이다 — 규약 «두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다». 모루 망치 수가 안 읽히는 것은 **T61** 몫이다(중복 등재 아님).
- 판정: `ui_score --score` 로 `main` 점수가 오르고 + PNG 눈 확인(전투력이 0 이 아니다 · 채팅 두 줄) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/Hud.cs` · `Ui/ChatScreen.cs`(프리뷰 갈래) · `Ui/MetaHost.cs`(전투력 밀기) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.

### T64 — 렌더 쪽 프레임당 관리 할당 ≈880KB(전투 부하 장면의 81%): 플레이어 빌드에서 재고, 있으면 URP 설정으로 잡는다 (Game·성능 · T50 뒤 · T26 빌드 잡 뒤)
- 실측(2026-09-12 · T50 · CI 런 90 · 에디터 배치모드 소프트웨어 렌더): `PerfBudgetTests` 부하 장면 프레임당 관리 할당 1,078KB 중 **카메라·캔버스를 끄면 200KB** — 878KB 가 렌더(URP C# 렌더 루프 · 캔버스 리빌드 · 에디터) 몫이고, 임팩트/파편/스킬을 끄면 각각 300~700KB 가 «비가산» 으로 줄어든다 = 살아 있는 렌더러·재질·광원 수에 비례하는 공통 비용(우리 스텝 코드가 아니다 · `ui-screens/perf-t50.txt` 의 `[T50]` 줄).
- 무엇을 한다: ⓐ 그 할당이 **플레이어 빌드(IL2CPP·Mono)** 에도 있는지 잰다(에디터 전용 경로면 폰과 무관) — T26 의 Android/WebGL 잡 산출물에 개발 빌드 프로파일러(`ProfilerRecorder` 는 개발 빌드에서도 산다)로 같은 부하 장면을 1회 ⓑ 있으면 원인을 URP 쪽에서 가른다: Render Graph 디버그·SRP Batcher·추가 광원(`FxLights`·`FlashLight` 점광 4+4) 갈래·투명 정렬·캔버스 매 프레임 리빌드(TMP 글자 40개) 등 설정으로 잡는다(코드 콘텐츠를 바꾸지 않는다).
- 판정: 플레이어 빌드 측정 기록(있음/없음 · 바이트) + 있으면 설정 변경 뒤 PerfBudgetTests 의 «전부» 수가 «렌더 끔» 수에 가까워지는 것 + 콘솔 빨강 0.
- 범위: `Assets/Settings/`(URP 에셋 · 렌더러 데이터) · `Assets/Tests/PlayMode/PerfBudgetTests.cs`(측정 갈래만) · `docs/`.

### T65 — 플레이어 정보 팝업이 정본의 절반이다: 장비 칸이 빈 카드 · 탈것 와이드 칸 없음 · 스킬/펫/탈것 아이콘 줄 통째로 없음 · 미리보기 상자가 빈 상자 (Game·UI · T22·T15 뒤 · T28 4회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 4회차 · 워커 H · 런 90 `screen_player-info.png` **2.0/10 · 30화면 중 꼴찌** ↔ 정본 `ref/screens/shot-043313.png`):
  - 장비 8칸이 **빈 회색 카드 + «Lv.26» 글자**뿐이다 — 같은 런의 `screen_gear-detail.png` 장비 시트에는 아이콘·등급색이 제대로 나오므로 이 팝업만 다른 조각을 쓴다. 정본 `renderPlayerInfo`(`ui.js` 5157)는 `SLOTS.map(slot => this.equipCellHTML(slot))` 로 **장비 시트와 같은 조각**을 쓴다.
  - 장비 2행 오른쪽의 **와이드 파란 탈것 칸**(`pinfo-mount-wide` · 정본 5159~5170 «원본(043313): 장비 2행 우측 와이드 파란 탈것 카드»)이 없다.
  - **스킬 줄·펫 줄·탈것 아이콘 줄**(`sk-cell`+`sk-orb` 세 묶음 · 정본 5172~5186)이 통째로 없다 — 정본 샷의 동그란 아이콘 6개가 그것이다.
  - 미리보기 상자가 «□ □□□□ 4-1» 만 있는 빈 회색 상자다. 정본은 `Scene3D.previewStart` 미니 전투 씬(`.pinfo-preview.scene`)이 기본이고, WebGL 이 없을 때만 폴백(🛡️ + 스테이지 라벨 + **웨이브 핍**)이다 — 클론은 폴백조차 핍·🛡️ 가 빠졌다.
- 무엇을 한다: `PlayerInfoPopup` 이 장비 칸을 **장비 시트와 같은 조각**으로 그리게 하고(수치·색은 카탈로그/데이터에서 · §1) 탈것 와이드 칸 + 세 아이콘 줄을 정본 순서대로 더한다. 미니 씬은 T54(전투 화면에 아무도 안 선다)가 풀린 뒤에 붙이고, 그 전에는 정본 폴백(🛡️ + 라벨 + 핍)을 정확히 낸다.
- 판정: `ui_score --score` 의 `player-info` 점수가 오르고(2.0 → 8.0 목표) + PNG 눈 확인(장비 칸에 그림이 있다 · 아이콘 줄이 보인다) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs` · `Assets/Forge/Resources/PlayerInfoUi.json`(새 · T20 `PetSkillUi.json` 꼴 · `catalog.json` 은 T62 lock 이 쥐고 있어 이 회차엔 안 연다 — T33 이 합칠 수 있다) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.

- ✅ 2026-09-12 워커 O(sess-2140-18689): 풀 넷(숫자 TMP 되쓰기 + 알파는 CanvasRenderer · 임팩트 슬롯+재질 조합 풀+세대 토큰 · 파편 P/궤적 Pt/FxAnims 항목) · Core 틱 버퍼 · 자를 계수기 «GC Allocated In Frame» 으로. 실측(런 78~90): 풀은 돈다(숫자 126→글자 오브젝트 24 · 임팩트 슬롯 68) · 전부 ≈1MB 중 **렌더 몫 878KB(81%)** · 렌더 끔 200KB(러너 바닥 107~361KB 포함) → 판정은 `GcNoRenderCap`(640KB) · 렌더 몫은 **T64** 로.
### T66 ✅ — 던전 입장 뒤 스테이지 라벨이 본대 라벨(«쉬움 1-1»)로 되돌아간다: `DungeonUiTests` 1/4 빨강 → **T55 2회차가 먼저 고쳐** 남은 몫 = T7 sim 대조가 던전 라벨을 센다 (검증·Core 대조 · 뒤 순서 없음 · 임자 없는 빨강으로 등재)
- 무엇(처음): 런 90(8aade87) `DungeonUiTests.던전_상세는_난이도_보상_열쇠를_보이고_입장하면_판이_선다` — «Expected "망치 도둑" · But was "쉬움 1-1"». 정본 `combat.js:113` `setupStage()` 끝의 `UI.updateStageLabel()`(`ui.js:1313`)은 **`Dungeons.run` 이면 «{kr} {stage}단계»**, 아니면 본대 라벨인데 Core `Battle.SetupStage`(T7)는 던전 문맥에서도 본대 `StageName()` 을 냈고, T55(3a8110e) 의 `Dungeons.Attach` 뒤로 그 이벤트가 다음 프레임 HUD 의 던전 라벨을 덮었다.
- 겹침: **T55 2회차(9014a39 · 워커 N)** 가 같은 회차에 같은 뿌리를 `DungeonRun.Label`(+ `SetupStage` 한 줄 · `Dungeons.BattleRun` 한 줄 · EditMode 1)로 먼저 고쳤다 — Core 쪽은 T55 절·기록을 본다(결정 152).
- 남은 몫(T66): `tools/sim/sim_combat.js` 의 UI 스텁 `updateStageLabel` 이 던전에서도 `stageName()` 을 적어 `combat_dungeon_tier` 기대값이 «매우 어려움 24-8» 이었고, C# `SimScenario.Context` 는 `Label` 을 안 채워 폴백으로 우연히 같았다 → **던전 라벨이 빠져도 원작 대조가 초록**. 스텁을 `ui.js:1313` 대로(`Dungeons.run` 이면 `${kr} ${run.stage}단계` · 아이콘은 `<img>` 노드라 글자에 없다) 고치고 시나리오에 `kr`·해석값 `label` 을 넣어 `Context` 가 읽는다 · 기대 JSON 재생성(`dungeon_tier` 한 줄 · 다른 둘 diff 0).
- 판정: `dotnet test` 507/507 + 고장 주입(`Context` 의 `Label` 읽기를 빼면 `원작_대조_던전_티어상승` 빨강) + CI dotnet 잡 초록. `DungeonUiTests` 4/4 는 N 의 커밋이 든 유니티 잡에서.
- 범위: `tools/sim/sim_combat.js` · `tools/sim/expected/combat_dungeon_tier.json`(재생성) · `Assets/Tests/EditMode/BattleTests.cs`(`SimScenario.Context` 한 줄). Core 파일은 안 만진다(T55 lock 범위).
- ✅ 2026-09-12 워커 P(sess-2254-41204): e562f5e · dotnet 507/507 · 고장 주입으로 대조가 던전 라벨 누락을 잡는 것 확인 · CI 런 97 dotnet 잡 초록.

### T67 — 유니티 잡이 «모드 하나를 통째로 안 돌린 채» 빨강인 것을 아무 자도 말하지 않는다 (게이트 · 뒤 순서 없음 · `ci.yml` 한 파일 · 워커 M 등재)
- 실측(2026-09-12 · CI **런 94** · `acff94a`): `editmode-results.xml` 은 **505 전부 초록**인데 `playmode-results.xml` 은 **없다**(잡 로그 «cat: /github/workspace/unity-test-results/playmode-results.xml: No such file or directory» 두 줄 · 그 뒤 «Test run failed with exit code 1»). PNG 0장 · `screens` 는 T51 갈래로 지난 40장을 이어받아 올렸다.
- 왜 아무도 못 보나: 요약 스텝의 경고는 «XML 이 **하나도** 없을 때» 만 뜬다 — 한쪽 모드만 죽으면 EditMode 총계만 예쁘게 찍히고 끝난다. `screens` 의 `playmode-red.txt` 도 EditMode 만 담은 채 «== 런 끝: Passed · 초록 505 · 빨강 0» 으로 끝나, 다음 회차 워커가 그 꼬리를 보고 «내 커밋이 든 런은 돌았다» 로 읽는다. ROUTINE §1 «테스트 0개는 빨간 테스트보다 나쁘다» 가 막으려던 자리에 자가 없다.
- 런 94 의 실제 원인(코드 탓 아님 · §1 «라이선스 좌석» 갈래): EditMode 뒤 개인 라이선스 **좌석 반납이 4번 실패**했다 — «An error occured while trying to return the ULF license. Ulf license file not found (/root/.local/share/unity3d/Unity/Unity_lic.ulf) (1404)» → «Failed to return the Personal license seat after 4 attempts · That seat is likely still held … otherwise later runs on this account will fail with 'no available seats'» → «Failure» 로 끝나 PlayMode 단계가 결과 XML 을 못 냈다.
- 무엇을 한다(전부 `.github/workflows/ci.yml`): ⓐ 요약 스텝에 **모드별 존재 검사** — `editmode-results.xml`·`playmode-results.xml` 중 없는 것이 있으면 `::error::` 로 «그 모드가 한 개도 안 돌았다» 를 이름으로 찍는다(있는 쪽 총계는 그대로) ⓑ 잡 로그의 좌석 문구(`Failed to return the Personal license seat` · `no available seats` · `Unable to activate license`)를 러너 출력에서 잡아 «라이선스 좌석» 을 따로 한 줄 ⓒ `ui-screens/playmode-red.txt` **머리**에 «이 런에 PlayMode 결과 없음(모드 XML 부재)» 한 줄을 덧붙여 `screens` 로 읽는 워커가 꼬리만 보고 속지 않게 한다 ⓓ 좌석 실패가 다음 런에도 이어지면 §1 대로 «주인 콘솔 에러 보고함» 에 «유니티 라이선스 좌석» 한 줄.
- 판정: `ci.yml` 만 바뀐다(코드·테스트 0줄) · 다음 main 런에서 dotnet·datasync 잡 초록 · 모드 XML 이 둘 다 있는 런에서는 새 줄이 조용하고, 한쪽이 없는 런에서는 `::error::` 와 `playmode-red.txt` 머리줄이 보인다.
- 범위: `.github/workflows/ci.yml`.

### T68 — 오프라인 보상 팝업 머리가 정본의 어두운 판이 아니다: 밝은 회색 판 + 검정 글자 · 요율이 아이콘 옆 · 수집 버튼 빨간 점 없음 (Game·UI · T13·T22 뒤 · T28 5회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 5회차 · 워커 N · 런 95 `screen_offline.png` **3.7/10** ↔ 정본 `ref/screens/shot-042110.png` + `style.css`·`ui.js`):
  - 정본 `.offline-top`(`style.css` 260)은 **평면 `#0e111b` 어두운 판 · 흰 글자 · 카드 높이의 42.8%** 이고 `«수집 시간:»` 은 `#ccc`, 경과 시간·요율은 `--pp-green`. 클론 `OfflinePopup.cs` 29 는 `UiKit.Panel(top, "bg", "pp_panel")`(#efefef) + `pp_ink` 검정 글자 + 제목만 `stage_ink` 흰색(밝은 판 위 흰 글자라 링에 기대 읽힌다).
  - 정본 `.offline-rate` 는 `flex-direction: column`(원형 아이콘 2.6rem **위** · `1.13/초` 글자 **아래** · 두 칸 사이 2.4rem). 클론 `Rate()` 는 원형 아이콘과 글자를 **옆으로** 붙였다.
  - 정본 수집 버튼 우상단에 `.offline-collect-dot`(`.7rem` 빨간 원 · 흰 테두리 · `style.css` 309)이 있다. 클론엔 없다.
  - 걷어낸 것: 원작 샷의 **파란** 수집 버튼은 옛것 — 정본 `.btn.primary` 가 초록(`#1f4a2c`/`#2ea043`)이라 클론의 초록이 맞다. 합계줄 `8.87k`·`149.05` 는 정본 주석대로 **흰 칠 + 검정 링**(클론은 검정 칠 · 링 규칙은 T25 갈래).
- 무엇을 한다: 머리 판을 어두운 색으로(카탈로그에 `#0e111b` 에 가까운 키가 없으면 키 하나 추가 — `catalog.json` 은 **T62 lock 이 풀린 뒤** · 그 전엔 `pp_ink`(#17181a)로 먼저) + 글자 색을 정본대로(흰 · #ccc · 초록) · 요율 칸을 세로 배치로 · 수집 버튼에 빨간 점. 수치는 `catalog.json`/`PopupKit` 에서(§1).
- 판정: `ui_score --score` 의 `offline` 점수가 오르고(3.7 → 8.0 목표) + PNG 눈 확인(위 절반이 어둡다 · 요율이 아이콘 아래) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/OfflinePopup.cs` · `Assets/Forge/catalog.json`(색 키 하나 · T62 뒤) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.
### T69 ✅ — 자: §7 표에 **이름이 없는** 작업을 잡는다 — 지금 17개가 빠져 T33 의 완주 판정이 그 위를 지나간다 (검증 · 뒤 순서 없음 · T33 이 이 자를 쓴다)
- 왜: `check_final_table.py`(T49)는 §7 «상태» 칸에 **적혀 있는** 번호의 표시만 PROGRESS 와 맞춰 본다 — «PROGRESS 에 있는 작업이 §7 어딘가에 적혀 있는가» 는 아무도 안 본다. 실측(2026-09-12 23:33 · 워커 K): PROGRESS 작업 67개 중 **17개가 §7 에 이름조차 없다** — 그중 `ui.js` 줄에 들어가야 할 **T62·T63·T65**, `scene3d.js` 줄의 **T54**, 품질 줄의 **T50·T64**, 그리고 `T66`(던전 라벨)이 게임 쪽이다. §7 은 주인이 정한 «다 옮겨졌다» 의 기준이고 T33 이 «§7 전 줄 ✅» 로 완주를 선언하므로, 빠진 작업은 **열린 채로 완주 선언을 통과한다**.
- 방법: ⓐ `check_final_table.py` 에 «미등재» 갈래 — PROGRESS 표의 번호 중 §7 본문 어디에도 안 나오는 것을 찍고 rc 1. 원작 모듈에 안 붙는 **도구·게이트·CI 작업**은 §7 에 그 줄을 하나 두어(«원작 밖 · 도구·게이트·CI») 거기 적는다 — 코드 안 예외 목록을 만들지 않는다(목록은 낡는다). ⓑ 지금 빠진 17개를 제 줄에 채운다. ⓒ `--self-test` 에 «§7 에 없는 번호가 있으면 rc 1» 칸.
- 판정: `check_final_table.py` rc 0(채운 뒤) · `--self-test` 초록 · 빠진 번호 0.
- 범위: `tools/check_final_table.py` · `docs/ROUTINE.md`(§7 표) · `docs/PROGRESS.md`.

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
python3 tools/ui_score.py --self-test                                         # (T28 뒤) 원작 대조 자 자기 검사 15칸 (CI dotnet 잡도 부른다 · T51)
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
> **반대 방향도 자가 본다(T69)**: PROGRESS 에 있는 작업이 이 절 어디에도 이름이 없으면 `tools/check_final_table.py` 가 rc 1 을 낸다 — 새 작업을 등재한 워커는 **여기 제 원작 줄에도 적는다**. 원작 모듈에 안 붙는 도구·게이트·CI 작업은 아래 «(원작 밖 · 도구·게이트·CI)» 줄에 적는다.
> 정본 크기는 2026-09-12 wwwww main 기준(줄 수). 큰 모듈(`scene3d.js` 18,887줄 · `ui.js` 6,181줄 · `icongen.js` 6,704줄)은 한 회차에 안 끝난다 — 워커가 하위 작업으로 쪼개 등재하고 여기에 줄을 더한다.

| 원작 (`web/`) | 무엇 | 작업 | 상태 |
|---|---|---|---|
| `js/balance-data.js` · `gamedata.js` · `mobdata.js` · `data/raw/*`(안 뽑음 · 결정 6ⓑ) | 수치·정의 표 | T2 → JSON · T3 강타입 | ✅ |
| `js/bignum.js` · `util.js` | 큰 수 · 표기 · 난수 | T3 | ✅ |
| `js/voxel.js` · `mobs.js` · `mobs-pets.js` · `mobs-mounts.js` · `mobs-enemies.js` · `mobs-props.js` · `mobs-skillfx.js` | 박스 몹 조립 · 종 표 | T2 · T4 · T5(전 종 세워 보기) | ✅ (T4 · T5) |
| `js/prochar.js`(2,526) | 영웅 박스 모델 · 무기 파지 · 애니 | T6 | ✅ |
| `js/combat.js` · `state.js`(전투 부분) | 전투 틱 · 웨이브 · 보스 · 전투가 `S` 에 쓰는 것(처치·재화·첫 클리어·진행·saveGame·던전 판) | T7 · T8 · T55 | ✅ (T7 · T8 · T55) |
| `js/scene3d.js`(18,887) · `scene3d-skillfx.js`(1,088) | 3D 세계 전부: 카메라·광원·테마·적 스폰·애니 계약·데미지 숫자·셰이크·파티클·맵·소품·펫 대형·탈것 탑승·스킬 오브젝트·사망 연출·히트 이펙트 | T1(카메라·테마0) · T8 · T9 · T10 · T11 · T12 · T39 · T52 · T54(화면에 아무도 안 선다) | T1 ✅ · T8 ✅(적 스폰·보행·공격·피격·사망·숫자·셰이크·파티클 · 뺀 연출은 T39 ✅) · T9 ✅(SIMPLE_BG 의 보이는 것 · 소재 T34 · 배경 복원 T35) · T34 ✅ · T38 ✅ · T10 ✅(펫 대형·따라오기·관절 드라이버) · T39 ✅(레갈리아·보스 재질·등장 워닝·디졸브·림·플래시·플레어/스파이크/링/점광/그을음·궤적·블롭·암전) · T11 ✅ · T35 ⬜(SIMPLE_BG 복원 전엔 안 잡는다) · T12 ✅(`scene3d-skillfx.js` 전부 + 스킬 디스패처) · T52 ✅(시전 젖힘 `heroG.rotation.z` — T12·T39 가 뺀 것) · T54 🔄(촬영 PNG 에 영웅·적·펫이 0 — 오브젝트는 서는데 화면에 안 그려진다) |
| `js/state.js` · `main.js`(저장 시점·부팅) | 세이브 · 마이그레이션 · 오프라인 보상 | T13 | ✅ |
| `js/forge.js` | 대장간 규칙 · 오토 포지 | T14 · T19 | ✅ (T14 · T19) |
| 장비 8부위 · 페이퍼돌(`prochar.js`·`ui.js` 장비 · `scene3d.js` makeWeapon/makeHelmet/dressMcRig) | 등급·서브스탯·판매가·외형 | T15(규칙·표값) · T37(3D 외형 캡처) · T19 | ✅ (T15 · T37 · T19) |
| `js/pets.js` | 알·부화·합성·출전 규칙 · 출전 스탯 기여 | T16 · T20 · T10(출전 조형) · T43(스탯 접착) | T16 ✅ · T10 ✅ · T20 🔄 · T43 ✅ |
| `js/skills.js` | 소환·18종·3슬롯(정본 `MAX_ACTIVE`) | T17 · T20 | T17 ✅ · T20 🔄 |
| `js/mounts.js` | 탈것 규칙 · 탑승 | T40(Core 규칙 · 결정 72) · T11(탑승 3D) · T20(탈것 화면) | T40 ✅ · T11 ✅ · T20 🔄 |
| `js/dungeons.js` | 던전 4종 | T23 · T21 | T23 ✅ · T21 ✅ |
| `js/techtree.js` · `ascension.js` | 기술트리 · 승천 | T24 · T21 | T24 ✅ · T21 ✅ |
| `js/shop.js` · `pass.js` · `quests.js` · `league.js` · `chat.js` | 상점·패스·퀘스트·리그·채팅 | T25 · T22 | ✅ (T25 · T22) |
| `js/ui.js`(6,181) · `css/style.css` · `index.html` | 캔버스·HUD·탭·패널 전부(공개 함수 97개 — T19~T22 절에 이름별로 나눠 적었다) · 메뉴·프로필·설정·디버그 | T18 · T19 · T20 · T21 · T22 · T53(한글 글꼴) · T56 · T57 · T58 · T59(T28 2회차가 PNG 로 잡은 결함) · T60 · T61(검수 Q 가 PNG 로 잡은 결함) · T62 · T63 · T65 · T68(T28 3~5회차가 PNG 로 잡은 결함) · T66(던전 라벨) | T18 ✅ · T19 ✅ · T21 ✅ · T22 ✅ · T20 🔄 · T53 ⬜ · T56 🔄 · T57 🔄 · T58 ⬜ · T59 ✅ · T60 ⬜ · T61 ⬜ · T62 🔄 · T63 🔄 · T65 🔄 · T66 ✅ · T68 🔄 |
| `js/sfx.js`(618) | 효과음 24종(+프리미티브 6) · 음악 4모드 (코드 합성) | T30 | ✅ (`Core/Audio` · `Game/Audio` · `AudioTests` 벡터 대조 · `AudioSmokeTests`) |
| `js/icongen.js`(6,704) · `avatars.js`(831) | 아이콘 136종 · 아바타 24종(`IconGen.draw` 키 160 · «523» 은 도우미까지 센 수) + tint 변형 10 | T31 | ✅ |
| `ref/screens/shot-*.png` 30장 · `tools/shot-*.js` · `ref/UI-SPEC.md` · `ref/POLISH.md` | 원작 화면 정본 · 촬영 도구 · 비율 규격 | T27(촬영) · T28(대조) · T33(완주) | T27 ✅(원작 30장 전부 열림 + `screen_*.png` 31장 + 짝 표 `screens.json` · CI 런 83) · T28 ⬜ · T33 ⬜ |
| (주인 지시 · 원작 밖 품질 조건) SafeArea · 60fps · 실제 화면 촬영 | 모바일 상단 카메라 회피 · 프레임 예산 · 게임 화면 PNG 를 눈으로 | T45 · T44 · T27 · T50 · T64 | T45 ✅ · T44 ✅ · T27 ✅(촬영 자리 · 노치 모의는 `UiRoot.NotchSafeArea`) · T50 ✅(프레임당 관리 힙 풀링) · T64 ⬜(남은 렌더 쪽 ≈880KB 를 플레이어 빌드에서 잰다) |
| WebGL 배포 · Android | 배포 | T26 | ✅ (굽기 잡 조건 T32 ✅) |
| (원작 밖 · 도구·게이트·CI) 병렬 운영을 지키는 자들 — 원작 모듈에 안 붙지만 **여기 적는다**(안 적으면 T33 이 그 위를 지나간다 · T69) | lock·번호·문서·카탈로그·CI·진단 자 | T29 · T36 · T41 · T42 · T46 · T47 · T48 · T49 · T51 · T67 · T69 | T29 ✅ · T36 ⛔ · T41 ✅ · T42 ✅ · T46 ✅ · T47 ✅ · T48 ✅ · T49 ✅ · T51 ✅ · T67 🔄 · T69 ✅ |
| `lib/three.min.js` · `anvil-*.png`(참고 이미지 · 게임이 안 읽음) · `web/TODO.md` 미완 7항목 | 옮기지 않음 | — | 해당 없음 |
