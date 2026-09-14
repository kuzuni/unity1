# ROUTINE — 포지 클론 유니티 이식 (kuzuni/unity1) 작업 지시서

> **이 문서가 유일 지시서다.** 병렬 워커(루틴 세션)는 매 회차 이 문서 → `docs/PROGRESS.md` → `docs/claims/` 순서로 읽고 §0 절차대로 움직인다.
> 정본(스펙·수치·조형)은 **`kuzuni/wwwww` 의 `web/`**(HTML+Three.js 원작)이다. 이 레포는 그것을 **읽어** 유니티로 옮긴다 — wwwww 는 **읽기 전용**.
> 운영 틀은 `kuzuni/aaawunity`(꼬마기사 유니티 이식)의 `docs/ROUTINE.md` 규약을 그대로 옮긴 것이다(lock · SID · 90분 · T번호 · 결정 기록 · 게이트).

## ⚑ 신규 주인 지시 (위 항목이 최신 · 닫힌 것은 ✅ 를 단다)

- **(2026-09-13 · 주인 · 넷)** «한글 글꼴 주인 대기는 뭐지 · TMP 로 하기는 한 건가 / PVP 부분 네모도 고쳐야 할 듯» → **주인 승인으로 한글 글꼴을 넣어 T53 을 닫았다**(`Assets/Fonts/NotoSansKR-Forge.ttf` · Noto Sans KR 서브셋 302KB). TMP 경로는 처음부터 옳았다. «백그라운드에서도 플레이 되게» → **T88**. «대장간 뽑을 때 애니메이션도 빠졌네 · 그런 것도 같게» → **T87**(원작 CSS 키프레임 22종).
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
6. 회차 첫 일은 언제나 **빨강**이다 — main 의 마지막 CI 런이 빨갛거나(컴파일 파손 · 테스트 0개) 남의 lock 이 없는 빨강이면 그것을 먼저 고친다(§1). 임자는 `check_unity_green.py --fetch` 가 가린다 — 범위 열·산 lock·이력으로 못 가린 빨강은 자의 **«런 사이» 칸**(직전 초록 유니티 런 뒤 이 런에 새로 들어온 코드 커밋 · 산 lock 을 쥔 것 · T148)을 먼저 믿는다: 한 커밋이 남의 자 여럿을 깨뜨리면 그 자들에 산 lock 이 없어 «네 일이다» 로 보이지만 그것은 그 커밋 임자의 몫이다.
   - ⚠ **«마지막 CI 런이 초록» 을 눈으로 믿지 마라(T123)**: 문서만 바뀐 push 는 유니티 잡을 건너뛰고 그 런은 통째로 `success` 다 — 빨간 유니티 런 위에 초록 문서 런이 쌓이면 목록은 초록으로 보인다(실측 2026-09-13 런 230 빨강 ↔ 231·235 초록). `python3 tools/check_unity_green.py --fetch` 가 «유니티 잡이 **실제로 돈** 마지막 런» 의 판정을 준다(rc 1 이면 그것이 이번 회차의 첫 일이다).

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
- **에셋은 주인 에셋만**(2026-09-13 주인 승인으로 **한글 글꼴 하나만 추가**: `Assets/Fonts/NotoSansKR-Forge.ttf` · SIL OFL · 원작이 쓰는 글자만 남긴 서브셋 302KB · `docs/assets-map.md` — 다른 글꼴·모델·그림을 새로 들이는 것은 여전히 금지): UI `GUI PRO Kit - Casual Game` · 이펙트 `JMO Assets/Cartoon FX Remaster` · `DOTween` · `AllIn1SpriteShader` · 글꼴 `Assets/Fonts/NotoSans-Regular.ttf`. 3D 조형은 **코드 생성 복셀 메시**(T4)뿐이다 — 외부 모델·임시 그림 금지. 새 에셋을 쓰면 `docs/assets-map.md` 에 «용도 · 경로 · GUID» 한 줄.
- 승인 프롬프트가 뜨는 명령·대화형 편집기(`git rebase -i`) 금지. 캡처 PNG·대용량 바이너리 커밋 금지(예외: `screens` 브랜치는 CI 가 올린다 · **T31 의 아이콘 아틀라스**는 정본 `icongen.js` 에서 도구가 결정론으로 뽑은 것이라 `Assets/Forge/Icons/` 에 둔다 · 총 3MB 상한 · 손으로 안 고친다 · `tools/check_icons_sync.sh` 가 CI 에서 정본과 대조).
- **lock 은 «CI 가 그 커밋을 한 번은 돈 뒤» 반납한다.** 로컬 게이트는 PlayMode 를 못 돌리므로 «초록» 의 절반만 본 것이다.
  - **내 런이 `cancelled` 면**(실측 2026-09-12 18:49~19:05 런 9~15 전부 — ci.yml 의 `concurrency` 가 대기 중인 옛 런을 새 push 로 갈아치운다 · 빌드 잡 30분 때문) 그것은 실패가 아니다: main 은 선형이므로 **내 커밋 이후의 main 런이 초록이면 그것이 내 CI 확인**이다(그 런은 내 변경을 포함한다). 아무 런도 안 끝났으면 기다리지 말고 lock 을 쥔 채 종료하고 다음 회차에 본다. 그 뒤 런이 빨강이면 빨간 잡의 파일이 내 범위인지 본다 — 내 것이면 내 일, 아니면 그 임자 몫(«남의 lock 이 없는 빨강» 은 §0-6).
  - **유니티 잡이 라이선스로 빨강이면**(`no available seats` · `Unable to activate license` · 좌석 반납 실패 경고 뒤) 코드 탓이 아니다 — 재실행 1회(권한이 없으면 다음 push 를 기다린다) · 계속되면 «주인 콘솔 에러 보고함» 에 «유니티 라이선스 좌석» 한 줄 · lock 은 쥔 채 종료.
- 작업이 끝나면 lock 삭제 → PROGRESS 갱신 → 커밋 → push. **lock 만 잡는 커밋·문서만 바꾼 커밋은 제목 끝에 `[skip ci]`**. 커밋 메시지 **본문**에 그 표식을 인용하지 마라(GitHub 은 인용과 지시를 안 가린다).
- 브랜치는 `main` 하나다. 커밋 작성자는 `git -c user.name=kuzuni -c user.email=<그 계정의 이메일>`. 커밋 제목은 `T<번호> <무엇> (sess-… · 워커 X)` 꼴 — `check_claim_scope` 가 그 번호로 커밋을 센다.
- 문자열 `StartsWith`·`EndsWith`·`IndexOf(string)`·`Contains(string)`·`Compare` 에는 **`StringComparison.Ordinal`** 을 준다 — 문화권 비교는 유니티(Mono)와 dotnet(ICU)이 다르게 답한다(이모지 접두가 Mono 에선 항상 true · T36 실측). dotnet 초록이 유니티 초록을 보장하지 않는 자리다.
- 한 줄에 문장이 여럿인 코드 줄 끝에 `// 주석` 을 붙이지 않는다(뒤 문장이 주석이 된다 · dotnet 은 못 잡는다).
- 글자 크기·색을 코드에 숫자로 박지 않는다 — `UiKit`(T18)의 종류(`TextKind`)를 준다. 하한: 본문 40 · 버튼 44 · 보조 36 · 제목 60(원작 UI 가 9:16 세로 폰에서 읽히던 크기다).
  - **예외 한 자리 `TextKind.Micro`(18 · T136)**: 정본이 `font-size: .5rem`(= 기준 캔버스 18.2px)로 못 박은 **배지 글자**만 쓴다 — 하한 36 을 주면 배지가 제 그릇보다 넓어져 «그대로 옮기기» 가 깨진다(실측: 전투 바 오브 지름 106px ↔ 하한 36 라벨 폭 ~117px · 런 275 PNG 5배 확대). **새로 쓰려면 정본 CSS 줄을 근거로 대고 완료 기록에 적는다** — 작다고 아무 데나 쓰는 종류가 아니다.
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

### T20 ✅ — UI 패널: 펫(알·부화·합성·출전) · 스킬(소환·장착) · 탈것 (Game · T16·T17·T18 뒤)
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
  - **던전 배너·아이콘이 뭉툭한 픽셀 블록**: 정본이 출력 직전에 칸 다운샘플 → 최근접 확대를 한다(`icongen.js` 120~132 주인 지시 `ui-icon-blockify` «UI 아이콘을 네모네모 픽셀 블록 느낌으로» · 690~712). 원작 샷(`shot-042304`·`042251`)의 매끈한 벡터는 **블록화 이전**이다 — 아틀라스 슬라이스를 잘라 내용이 맞는 것까지 확인했다(T28 28회차).
  - **시대 이름 «천상»**(원작 샷은 «신성한»): 정본 `gamedata.js` 16~19 `AGE_KR.divine = '천상'` — 클론이 정본대로다. 이름표는 `data/*.json` 이 쥐고 **손으로 고치는 것이 금지**라 결함으로 읽지 말 것(T28 30회차).

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

### T33 — 완주 대조: §7 표의 모든 줄이 ✅ 이고 원작 화면 30장·`ui.js` 공개 함수 97개·`SFX` 24종(`SfxRecipes.Names`)·`IconGen` 키가 유니티에 다 있는가 (검증 · T27·T28·T30·T31 뒤 · **마지막** · 1회차 2026-09-13 워커 A: `docs/parity.md` — 빠진 것 0 · 열린 칸 T28·T75·T84·T86·T87 + T35 주인 결정 뒤에 다시 잡는다)
- 방법: ⓐ §7 표를 위에서 아래로 — 줄마다 «유니티의 어느 파일·테스트가 그것인가» 를 적는다(없으면 «가장 큰 번호 +1» 로 등재하고 그 줄을 그 번호로 바꾼다) ⓑ `web/ref/screens/shot-*.png` 30장 각각에 유니티 `ui-screens/*.png` 짝이 있는가(T28 대조표) ⓒ `grep -o "^\s*\(open\|render\|show\|toggle\|close\|build\)[A-Z][A-Za-z]*" .wwwww-src/web/js/ui.js` 의 함수 하나하나에 유니티 대응(같은 이름의 메서드·화면)이 있는가 ⓓ `SFX.*` 24종 · `IconGen.img/avatar/skill/tab` 키가 `Sfx.Play`·`UiIcons.Get` 로 다 불리는가 ⓔ 원작을 한 판(전투→제작→장착→펫→스킬→던전→상점→리그→채팅) 하고 유니티(T27 봇 + WebGL 배포본)로 같은 판을 해 **다른 곳을 전부 적는다**.
- 판정: 빠진 것 0 이 될 때까지 이 작업은 ✅ 가 아니다 — 빠진 것을 등재하고 «그 번호들 뒤» 로 자기 순서를 고쳐 lock 을 반납한다(다음 회차가 다시 잡는다). 전부 ✅ 면 §7 표 머리에 «완주 YYYY-MM-DD · 커밋» 을 적고 ✅.
- **3회차(2026-09-13 · 워커 H · sess-1757-15757)**: 2회차가 남긴 «넷» 중 셋(T89·T90·T91)이 닫혀 목록이 낡아 다시 셌다. 새로 잡은 것 — ⓐ §7 `ui.js` 줄 **상태 칸에 T98 이 없었다**(작업 칸엔 있다 → 열린 작업이 완주 판정 위를 그냥 지나간다 · `check_final_table.py` 에 그 구멍을 막는 자를 더했다) ⓑ 정본 소리 24종 중 **호출 0 이 넷**(`craftReveal`·`equipToss`·`equipDrop`·`stormCrackle`) ⓒ 그 뿌리인 연출 둘이 통째로 없다 — 판매 코인 `coinBurst` · 장비 교체 던져내기 `equip-swap-throwout` → **T117·T118·T119 등재**(내 번호 T114~T116 은 검수 Q 의 T114 와 겹쳐 규약대로 늦게 민 내가 옮겼다 · 결정 255).
- **4회차(2026-09-13 · 워커 F · sess-2327-52310)**: 3회차가 SFX 24종에 댄 눈(«이름이 있는가» 말고 «부르는 곳이 있는가»)을 **IconGen 축**에 그대로 댔다. T31 아틀라스 키 170개(변형 뺀 160) ↔ 클론 코드·데이터 대조 — 접두어로 조립하는 것(`sk_`·`tab_`·`shop_`·`slot_`·`wpn_`·`dg_`·`age_`·`avatar_`)을 빼고 나면 **부르는 곳이 0 인 키 열하나**가 남았다. 하나(`autoloop`)는 이미 T108 의 것이고 나머지 열을 셋으로 묶어 등재했다 — **T130**(리그 1·2·3위 배지가 남의 키트 스프라이트) · **T131**(성별 둘은 글자 ♂/♀ · 클랜 배지는 없음) · **T132**(`chatbubble`·`passsword`·`chatcam`·`barrier`·`chest` 다섯 자리 한 줄씩). 셋 다 **그림은 이미 구워져 있고 부르는 줄만 없다** — 아틀라스 동기화 검사(T31)는 «키가 있는가» 만 보므로 이 구멍을 못 본다.
- 범위: `docs/ROUTINE.md`(§7 표 · §2) · `docs/PROGRESS.md` · `docs/parity.md`(대조 결과) · `tools/check_final_table.py`(3회차가 «작업 칸엔 있고 상태 칸엔 없는 열린 작업» 자를 더했다).
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

### T53 ✅ — 한글 글꼴: 화면 글자가 전부 네모(□)다 (Game · T18 뒤 · 주인 승인 2026-09-13 · 서브셋 글꼴로 닫았다)
- 실측(2026-09-12 22:12 · `screens/ui_safearea_notch.png`): 스테이지·탭·버튼·토스트 등 **모든 한국어 라벨이 두부**. 원인은 결정 8 — 주인 글꼴 `NotoSans-Regular.ttf` 에 U+AC00 한글 구간 cmap 이 없고, 리눅스 CI·WebGL 에는 폴백할 OS 한글 글꼴도 없다.
- 워커가 할 수 없는 것: 글꼴 파일을 새로 들이는 것(§1 «에셋은 주인 에셋만»). **주인이 `Assets/Fonts/` 에 한글 TTF 를 넣어 주면** 이 작업은 `catalog.json` 의 `font` 한 줄 + `UiFont.Build` 폴백 정리 + PlayMode 단언(라벨 문자열의 글리프가 폰트에 **있는가** · 없으면 실패)으로 끝난다.
- **닫은 방법(2026-09-13 · 계정 2 대화 세션)**: 주인 승인 → Noto Sans KR(SIL OFL)을 **원작 `web/` 이 실제로 쓰는 한글 1266 자 + 라틴·기호**로 깎아(6.1MB → 302KB) 넣고 `catalog.json` `font` 를 그것으로. TMP 경로(`TMP_FontAsset.CreateFontAsset` + OS 폴백)는 이미 옳았다 — 폴백은 서브셋 밖 글자용으로 남겼다. 막이 둘을 `TextSizeGateTests` 에(활성 라벨의 한글을 문자 단위로 · 카탈로그 글꼴이 폴백 없이 한글을 쥐는가) · 스텁에 `HasCharacter(char,bool,bool)`. 재생성 명령은 `docs/assets-map.md`.
- (옛 지침) 주인 조치 전에는 이 작업을 잡지 마라 — 잡으면 «주인 에셋 대기» 로 즉시 반납한다. 그동안 T27·T28 의 화면 대조는 **글자를 빼고 배치만** 본다.
- 실측 보탬(2026-09-12 · T28 2회차 · 워커 M): 촬영 30장 **전부** 같다(`screen_main.png` «□□ 1-1» ↔ 원작 «어려움 4-1» · 탭 5개 · `settings` 토글 8줄 · `shop` 상품 제목). 원인 자리는 `UiKit.Font.Build`(`Ui/UiKit.cs` 204~228)의 **OS 글꼴 폴백** — 리눅스 러너에 한글 글꼴이 없어 경고 한 줄로 지나가고, **WebGL 배포본에는 OS 글꼴 자체가 없어 주인 폰에서도 같은 그림**이다. 주인 글꼴이 들어오기 전에도 «폴백이 비면 경고가 아니라 빨강» 한 줄은 이 작업이 먼저 넣을 수 있다.
- 범위: `Assets/Fonts/`(주인) · `Assets/Forge/catalog.json`(font) · `Assets/Scripts/Game/Ui/UiFont.cs` · `Assets/Tests/PlayMode/TextSizeGateTests.cs`.

### T54 ✅ — 전투 화면에 영웅·적·펫이 하나도 안 선다 (Game·검증 · T8·T10 뒤 · **가장 먼저**)
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

### T56 ✅ — HUD 상단바 프로필 카드에 아바타가 안 그려진다 (Game·UI · T18·T31 뒤 · 실제 화면 실측)
- 실측(2026-09-12 22:20 · 워커 J · `screens/screen_main.png`·`screen_profile.png` 를 열어 본 것): 상단바 왼쪽 프로필 카드의 아바타 자리가 **빈 흰 사각형**이다. 같은 PNG 의 프로필 팝업·리그 순위표·채팅에는 도트 초상이 제대로 그려진다 — T31 아틀라스는 멀쩡하고 **HUD 만 안 그린다**.
- 원인: `Hud.Build` 가 아바타 타일을 손으로 짠다(`UiKit.Rounded` 테 + `avatar_bg` 면 두 줄) — 남들이 쓰는 `PopupKit.Avatar(... , emoji, ...)` 의 초상 스프라이트 갈래가 없다. 정본 `ui.js:1290 renderTopBar` 는 `<span class="avatar">${IconGen.avatar(S.avatarEmoji)}</span>` 로 닉네임·전투력과 **같이** 그리고, `onPickAvatar`(`ui.js:5063`)가 아바타를 바꾸면 `renderTopBar()` 를 다시 불러 상단바도 따라 바뀐다.
- 할 일: ⓐ `Hud` 에 `SetAvatar(string emoji)` — 기존 아바타 타일 안에 `UiIcons.Avatar(emoji)` 초상을 넣고(없으면 지금처럼 빈 타일) 다시 부르면 갈아끼운다 ⓑ `MetaHost.Sync` 가 닉네임·전투력과 같은 자리에서 `AvatarEmoji` 도 밀어 준다(바뀔 때만 · 아바타 고르기 뒤 상단바가 따라 바뀌는 정본 행동) ⓒ PlayMode 단언: 부팅 뒤 HUD 아바타 타일에 초상 `Image` 가 서고 그 스프라이트가 `UiIcons.Avatar(기본 아바타)` 와 같다 · 아바타를 바꿔 `Sync` 하면 스프라이트가 따라 바뀐다.
- 판정: 위 단언 초록 + 다음 회차에 `screen_main.png` 를 열어 상단바에 초상이 보이는 것을 본 기록 + 콘솔 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/Hud.cs`(아바타 타일·`SetAvatar`) · `Assets/Scripts/Game/Ui/MetaHost.cs`(Sync 한 줄) · `Assets/Tests/PlayMode/HudAvatarTests.cs`(새).

### T57 ✅ — 대장간·장비 팝업이 제 판(카드) 없이 배경 위에 글자를 겹쳐 그린다 + 빈 검은·흰 막대 둘 + ✕ 가 둘 (Game·UI · T19 뒤 · T28 2회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 2회차 · 워커 M · 런 78 PNG): `screen_forge-detail.png` **1.8/10** — 원작(`shot-042931`)은 딤 위 **흰 카드 한 장**에 아이템 + 서브옵션 12줄인데, 클론은 목록 격자와 상세 글자가 **같은 자리에 겹쳐** 읽을 수 없고 ✕ 가 위아래로 둘이다. `screen_craft-compare.png` **3.1/10** — 비교 카드 둘 중 **두 번째 카드에 판이 없어** «35.6m …»·«+30% …» 가 3D 배경과 장비 격자 위에 떠 있다. `screen_gear-detail.png` **2.7/10** · `screen_autoforge-filter.png` **3.7/10** — 카드 위에 **속 빈 검은 막대 + 흰 막대**가 하나씩 떠 있다(원작에 없는 자리).
- 참고(오판 방지): 장비 상세가 **딤 없이** 격자 위에 뜨는 것 자체는 원작 그대로다(`shot-043244` 실측) — 문제는 «판이 없다 · 글자가 겹친다 · 빈 막대가 뜬다 · ✕ 가 둘» 이다.
- 무엇을 한다: 원작 `ui.js` 의 해당 팝업(`openGearDetail`·`showCraftCompare`·`openForgeDetail`·자동 제련 필터)이 **카드 한 장**을 먼저 세우고 그 안에 줄을 놓는 순서를 그대로 옮긴다. 빈 막대의 정체(폭 0 배치·라벨 없는 pill)를 찾아 없앤다. ✕ 는 화면당 하나.
- 판정(2026-09-12 워커 F 가 실측으로 고쳐 적었다 · 결정 159): 워커가 PNG 를 `Read` 로 열어 «판 있음 · 글자 겹침 0 · 빈 막대 0 · ✕ 하나» + `ForgeUiTests` PlayMode 빨강 0. **`ui_score` 8.0 은 이 작업의 판정이 아니다** — 채점 자는 화면 전체(3D 세계·글자 잉크)를 재고, 원작 샷은 `SIMPLE_BG` 이전 캡처라 나무·흙길이 가득한데 클론 배경은 T35(주인이 `SIMPLE_BG` 를 끌 때만)라 비어 있고 한글은 T53(주인 글꼴) 전까지 두부다. 그 둘이 팝업보다 점수를 크게 움직인다(런 95 실측: 팝업 결함 넷을 다 없앴는데 평균 2.88 → 2.96). 화면 점수는 T28 이 그 둘과 함께 본다.
- 범위: `Assets/Scripts/Game/Ui/Forge*`(ForgeInfoPopup · ForgeCraftPopup · ForgeAutoPopup · ForgeUi) · `Ui/Gear*` · `Assets/Tests/PlayMode/ForgeUiTests.cs`.

### T58 ✅ — 리그 도전·펫 업그레이드 팝업이 판 없이 부모 목록 위에 겹친다 (Game·UI · T20 lock 이 풀린 뒤 · T22 뒤 · T28 2회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 2회차 · 워커 M · 런 78 PNG): `screen_league-challenge.png` **1.9/10** — 원작(`shot-042228`)은 **흰 카드**에 «상대 선택» + 티켓 pill + 상대 5줄인데, 클론은 카드도 제목도 없이 상대 5줄만 리그 순위표 위에 얹혀 두 목록이 서로 겹친다. `screen_pet-upgrade.png` **1.7/10** — 카드는 있으나 ✕ 가 **위아래로 둘**(하나는 탭바 위)이고 원작(`shot-042503`)과 머리 구성이 다르다.
- 무엇을 한다: 원작 `ui.js` `openLeagueChallenge`·`openPetUpgrade`·`renderPetUpgrade` 의 카드·제목·티켓 줄을 그대로. ✕ 는 화면당 하나.
- 판정: PNG 눈 확인(원작 구성·✕ 카드당 하나) + PlayMode 빨강 0. ~~`ui_score` 8.0~~ — 딤 α(.5 · 주인 지시)와 §1 글자 하한 때문에 팝업 화면은 닿을 수 없다(결정 174 · 157 과 같은 갈래).
- 범위: `Assets/Scripts/Game/Ui/League*` · `Ui/PetUpgrade*`(T20 이 쥔 `Ui/Pet*` 와 겹친다 — **T20 lock 이 풀린 뒤에 잡는다**) · `Assets/Tests/PlayMode/PetUiTests.cs`.

### T59 ✅ — 수 표기 둘: 서브스탯이 `+7.699999999999999%` · 확률이 전부 `0.0000%` (Game·UI · T15·T19 뒤 · T28 2회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 2회차 · 워커 M · 런 78 PNG): `screen_player-info.png` 에 «+7.699999999999999% …» 줄이 그대로 찍힌다. 정본 `ui.js` 5182행은 `value: +stats.subs[key].toFixed(1)` 로 **소수 한 자리**를 만든 뒤 찍는다 — 클론은 double 을 그대로 이어 붙인다. 같은 화면의 `+15.4%`·`+46.8%` 는 우연히 짧게 떨어진 값이다.
- 둘째: `screen_forge-detail.png`·`screen_forge-info.png` 의 확률이 **전부 `0.0000%`** 다(`ForgeInfoPopup.cs` 215·276행의 `"0.0000"` 서식은 원작과 같은 자리수지만 값이 0 이다) — 표에서 확률을 못 읽어 오는 갈래인지 시대·등급 인자가 비어 있는지 본다.
- 무엇을 한다: 서브스탯 줄은 정본과 같은 반올림(`toFixed(1)` 상당 · `NumFmt` 갈래)을 쓰고, 확률 0 의 원인을 잡는다. 수치는 코드에 박지 않는다(§1).
- 판정: EditMode 표 테스트(정본 값 ↔ 표기 문자열) + PNG 눈 확인(«+7.7%» · 확률이 0 이 아님) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs` · `Assets/Scripts/Game/Ui/ForgeInfoPopup.cs` · `Assets/Scripts/Core/BigNum.cs`(필요하면 표기 함수만) · `Assets/Tests/EditMode/`.
- ✅ 2026-09-12 워커 S: ⓐ 합계 줄은 `ForgeHost.SubLines`(PlayerInfoPopup 은 줄을 받기만 한다)가 만든다 → 원작 순서·`toFixed(1)`·`> 0` 으로 고침(`NumFmt.RoundFixed/Fixed`). ⓑ 확률 «전부 0.0000%» 는 버그가 아니었다 — 런 78 PNG 의 목록은 **Lv29 의 원시 시대 한 절**(그 레벨 확률표에서 0%)이고 원작 `itemDropChance(...).toFixed(4)` 도 같은 값 · EditMode 가 Lv29 전 시대·부위를 식으로 잰다(결정 138).

### T60 ✅ — 재화 알약의 초록 «+»(상점 열기) 배지가 없다 (Game·UI · T18·T31 뒤 · 검수 Q 등재)
- 실측(2026-09-12 22:1x · 검수 Q · 런 79 `screen_main.png`·`ui_safearea_notch.png` ↔ 정본 `ref/screens/shot-042120.png`·`shot-042356.png`): 정본 상단바의 코인·젬 알약은 아이콘 오른쪽 아래에 **초록 원 «+» 배지**를 달고 있고 그것이 상점으로 가는 버튼이다. 클론 상단바에는 그 자리 자체가 없다 — 알약이 «아이콘 + 숫자» 뿐이다(숫자 색 코인 `#ffd54f`·젬 `#ff8a80` 은 정본 CSS 와 맞다 · 색은 문제 아님).
- 정본: `js/ui.js:1285` `curIcoPlus(kind)` = `<span class="pill-ico">{아이콘}<button class="pill-plus" onclick="UI.openShop()" aria-label="상점 열기">{IconGen.img('plus')}</button></span>` · `renderTopBar`(1300~1302행)가 코인·젬 둘 다에 쓴다. 치수는 `css/style.css:131` `.pill-plus`(`right:-.38rem` · `bottom:-.11rem` · `.74rem` 정사각)이고 그 CSS 주석이 **원본 실측 역산**을 적어 두었다 — 십자 폭 = 원판 지름의 **0.49배**, 중심은 원판 중심에서 `(+0.51, +0.33)×지름`.
- 할 일: `Hud` 의 코인·젬 알약 아이콘에 «+» 배지 버튼을 위 비율로 얹고 누르면 상점을 연다(`ShopSheet` 는 `ShopSheet.cs:140` 에 이미 같은 조각을 «원작 curIcoPlus» 로 갖고 있다 — 그 자리를 공용으로 빼 쓰면 두 번 안 짠다). 아이콘 키는 T31 아틀라스의 `plus`. **치수·색은 `catalog.json` 으로**(§1 — 코드에 숫자 금지 · `gen_ui_catalog` 로 갱신).
- 판정: PlayMode 단언(코인·젬 알약에 «+» 자식이 있고 누르면 상점 팝업이 열린다 · safeArea 안) + 다음 회차에 `screen_main.png` 를 열어 배지가 보이는 것을 본 기록 + 콘솔 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/Hud.cs` · `Assets/Scripts/Game/Ui/ShopSheet.cs`(공용 조각으로 빼는 갈래만) · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/UiSmokeTests.cs`.

### T61 ✅ — 모루의 망치 수가 안 읽힌다: 밝은 시트 위 흰 글자 · 모루에 받침이 없다 (Game·UI · T19·**T57 뒤** · 검수 Q 등재)
- 실측(2026-09-12 22:1x · 검수 Q · 런 79 `screen_main.png` 확대): 장비 시트 바닥(밝은 회색)에 «🔨 302k» 가 **거의 흰색**으로 찍혀 배경과 구별이 안 되고 모루 그림에 글자 왼쪽이 반쯤 물린다. 정본 `ref/screens/shot-042120.png` 은 같은 글자(«🔨 41307»)를 **모루의 어두운 몸통 위**에 얹어 흰 글자가 읽힌다 — 정본 모루는 붉은 상판 + 어두운 몸 + **회색 돌 받침** + 검은 외곽선이고, 클론 모루는 갈색 두 덩이뿐이라 글자가 시트 바닥으로 흘러내렸다.
- 할 일: 모루 조형(상판·몸·돌 받침·외곽선)과 망치 수 자리를 정본 실측(`ref/screens/shot-042120.png` · `web/tools/anvil-*.png` · `probe-anvil-ref.js`)대로 맞춘다. 색·치수는 `catalog.json`(§1).
- **T57 뒤인 이유**: T57 의 범위 `Assets/Scripts/Game/Ui/Forge*` 글로브가 `ForgeSheet.cs` 를 덮는다 — 규약 «두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다»(`docs/claims/README.md`).
- 판정: 촬영 PNG 를 열어 «🔨 <수>» 가 어두운 받침 위에서 읽힌다 + 대비 단언(글자 픽셀과 그 뒤 배경의 밝기 차 ≥ 문턱) 한 줄 + `ui_score.py --score` 의 `main` 점수가 안 내려간다.
- 범위: `Assets/Scripts/Game/Ui/ForgeSheet.cs`(모루 자리) · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/ForgeUiTests.cs`.
- ✅ 2026-09-13 워커 S: 모루를 정본 SVG 6면(받침·음각 단·목·뿔·상판 앞/윗면·베벨 · 검은 외곽선 3)으로 · 망치 수는 **받침 세로 61%**(shot-042120 실측)에 흰 글자 + 검정 외곽선(style.css 1625) · 좌표·색 44키 `catalog.json anvil_*` · PlayMode 대비 단언(글자−받침 밝기 ≥ 0.5 · 중심이 받침 안) — 결정 171.

### T62 ✅ — 상점 시트: 특가 카드가 원작보다 높아 «보석» 절(젬 상품 3종)이 화면 밖으로 밀린다 (Game·UI · T22·T25 뒤 · T28 3회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 3회차 · 워커 K · 런 83 PNG): `screen_shop.png` **5.3/10**. 원작(`shot-042632`)의 특가 카드는 `min-height: app-h × .1528` · 카드 사이 `gap: app-h × .0091`(정본 `style.css` 2912~2921)인데 클론 카드는 그보다 한참 높고 간격도 넓어, 원작에서 66%H 자리에 있던 «보석» 배너와 젬 카드 3장(`shop-gems`)이 **화면 밖으로 밀려 아예 안 보인다**.
- 무엇을 한다: 특가 카드 높이·간격·안쪽 여백을 정본 `.shop-deal-card`/`.shop-deals` 비율 그대로.
- ⚠ **건드리지 않는 것 둘**: ⓐ 특가 카드의 «젬 보상 pill 3번째 줄» — 정본 `shop.js` 4~8행이 «젬 보급 전면 제거» 로 일부러 뺐다(원작 샷이 옛것 · T28 절 메모) ⓑ 재화 알약의 초록 «+» 배지 — **T60** 몫이다.
- 판정: `ui_score --score` 로 `shop` **8.0 이상** + PNG 눈 확인(«보석» 배너와 젬 카드가 화면 안에 있다) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/ShopSheet.cs` · `Assets/Forge/catalog.json`(배치 값) · `Assets/Tests/PlayMode/ShopUiTests.cs`.

### T63 ✅ — 메인 HUD 둘: 전투력이 `⚔ 0` · 채팅 프리뷰가 한 줄이고 «99» 뱃지가 없다 (Game·UI · T18·T22 뒤 · **T56·T60 lock 이 풀린 뒤**)
- 실측(2026-09-12 · T28 3회차 · 워커 K · 런 83 `screen_main.png`):
  - ⓐ 프로필 카드의 전투력이 **`⚔ 0`** 이다 — 같은 화면의 장비 그리드에는 Lv.26~28 이 8부위 장착돼 있다. 정본 `renderTopBar`(`ui.js` 1290~1303)는 `Combat.combatPower()` 를 찍는다.
  - ⓑ 채팅줄이 «Zephyr: anyone want to trade tickets?» **한 줄**이고 말풍선에 뱃지가 없다. 정본 `renderChatPreview`(`ui.js` 5288~5299)는 말풍선 + **«99» 뱃지** + 이름 줄 / 메시지 줄 **두 줄**이다.
- 무엇을 한다: ⓐ 는 HUD 가 전투력을 T43·T55 가 세운 접착(`GearSystem.HeroStats` 갈래)에서 끌어오게 한다 — 수치를 코드에 박지 않는다(§1). ⓑ 는 정본 두 줄 + 뱃지.
- ⚠ `Ui/Hud.cs` 가 **T56**(아바타)·**T60**(재화 «+» 배지)와 같은 파일이다 — 규약 «두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다». 모루 망치 수가 안 읽히는 것은 **T61** 몫이다(중복 등재 아님).
- 판정: `ui_score --score` 로 `main` 점수가 오르고 + PNG 눈 확인(전투력이 0 이 아니다 · 채팅 두 줄) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/Hud.cs` · `Ui/ChatScreen.cs`(프리뷰 갈래) · `Ui/MetaHost.cs`(전투력 밀기) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.

### T64 ✅ — 렌더 쪽 프레임당 관리 할당 ≈880KB(전투 부하 장면의 81%): 플레이어 빌드에서 재고, 있으면 URP 설정으로 잡는다 (Game·성능 · T50 뒤 · T26 빌드 잡 뒤)
- 실측(2026-09-12 · T50 · CI 런 90 · 에디터 배치모드 소프트웨어 렌더): `PerfBudgetTests` 부하 장면 프레임당 관리 할당 1,078KB 중 **카메라·캔버스를 끄면 200KB** — 878KB 가 렌더(URP C# 렌더 루프 · 캔버스 리빌드 · 에디터) 몫이고, 임팩트/파편/스킬을 끄면 각각 300~700KB 가 «비가산» 으로 줄어든다 = 살아 있는 렌더러·재질·광원 수에 비례하는 공통 비용(우리 스텝 코드가 아니다 · `ui-screens/perf-t50.txt` 의 `[T50]` 줄).
- 무엇을 한다: ⓐ 그 할당이 **플레이어 빌드(IL2CPP·Mono)** 에도 있는지 잰다(에디터 전용 경로면 폰과 무관) — T26 의 Android/WebGL 잡 산출물에 개발 빌드 프로파일러(`ProfilerRecorder` 는 개발 빌드에서도 산다)로 같은 부하 장면을 1회 ⓑ 있으면 원인을 URP 쪽에서 가른다: Render Graph 디버그·SRP Batcher·추가 광원(`FxLights`·`FlashLight` 점광 4+4) 갈래·투명 정렬·캔버스 매 프레임 리빌드(TMP 글자 40개) 등 설정으로 잡는다(코드 콘텐츠를 바꾸지 않는다).
- 판정: 플레이어 빌드 측정 기록(있음/없음 · 바이트) + 있으면 설정 변경 뒤 PerfBudgetTests 의 «전부» 수가 «렌더 끔» 수에 가까워지는 것 + 콘솔 빨강 0.
- 범위: `Assets/Settings/`(URP 에셋 · 렌더러 데이터) · `Assets/Tests/PlayMode/PerfBudgetTests.cs`(측정 갈래만) · `docs/`.
- **✅ 결론(2026-09-13 · 4회차 · 런 108·113·118)**: 렌더 몫은 **없었다**. ⓐ 계수기 «GC Allocated In Frame» 은 모든 스레드를 세는데, 초과분 ≈450~640KB/프레임의 임자는 **`AudioBank` 배경 스레드**(부팅 베이크: 효과음×테이크 2 + 음악 4모드 · 씬을 다시 열 때마다 다시 굽는다)였고 카메라·캔버스·광원을 하나만 꺼도 «사라진» 것은 갈래가 아니라 시간(베이크가 끝남)이었다. ⓑ 메인 스레드 정상 상태 150~170KB = 우리 스텝 90~108KB(30프레임마다 스킬 재시전 액터 조립) + 편집기 전용 `MaterialEditor.ApplyMaterialPropertyDrawersFromNative` 52~59KB(FxCubes 가 시전마다 새 재질 · 플레이어에는 없음). URP 렌더 루프·캔버스 리빌드의 GC.Alloc 은 0 → **URP 설정 변경 없음 · 플레이어 빌드 측정 불필요**(베이크 스레드는 플레이어에서도 같은 관리 스레드다 — 그것은 T73). 자: `PerfBudgetTests` 는 이제 베이크가 끝난 뒤 잰다(`BakeWaitSec`) · 판정은 T50 의 렌더 끔 상한 그대로. 남은 일은 T73(베이크 쓰레기)·T74(시전당 재질) 로 등재.

### T65 ✅ — 플레이어 정보 팝업이 정본의 절반이다: 장비 칸이 빈 카드 · 탈것 와이드 칸 없음 · 스킬/펫/탈것 아이콘 줄 통째로 없음 · 미리보기 상자가 빈 상자 (Game·UI · T22·T15 뒤 · T28 4회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 4회차 · 워커 H · 런 90 `screen_player-info.png` **2.0/10 · 30화면 중 꼴찌** ↔ 정본 `ref/screens/shot-043313.png`):
  - 장비 8칸이 **빈 회색 카드 + «Lv.26» 글자**뿐이다 — 같은 런의 `screen_gear-detail.png` 장비 시트에는 아이콘·등급색이 제대로 나오므로 이 팝업만 다른 조각을 쓴다. 정본 `renderPlayerInfo`(`ui.js` 5157)는 `SLOTS.map(slot => this.equipCellHTML(slot))` 로 **장비 시트와 같은 조각**을 쓴다.
  - 장비 2행 오른쪽의 **와이드 파란 탈것 칸**(`pinfo-mount-wide` · 정본 5159~5170 «원본(043313): 장비 2행 우측 와이드 파란 탈것 카드»)이 없다.
  - **스킬 줄·펫 줄·탈것 아이콘 줄**(`sk-cell`+`sk-orb` 세 묶음 · 정본 5172~5186)이 통째로 없다 — 정본 샷의 동그란 아이콘 6개가 그것이다.
  - 미리보기 상자가 «□ □□□□ 4-1» 만 있는 빈 회색 상자다. 정본은 `Scene3D.previewStart` 미니 전투 씬(`.pinfo-preview.scene`)이 기본이고, WebGL 이 없을 때만 폴백(🛡️ + 스테이지 라벨 + **웨이브 핍**)이다 — 클론은 폴백조차 핍·🛡️ 가 빠졌다.
- 무엇을 한다: `PlayerInfoPopup` 이 장비 칸을 **장비 시트와 같은 조각**으로 그리게 하고(수치·색은 카탈로그/데이터에서 · §1) 탈것 와이드 칸 + 세 아이콘 줄을 정본 순서대로 더한다. 미니 씬은 T54(전투 화면에 아무도 안 선다)가 풀린 뒤에 붙이고, 그 전에는 정본 폴백(🛡️ + 라벨 + 핍)을 정확히 낸다.
- 판정(2026-09-13 워커 A 가 실측으로 고쳐 적었다 · 결정 172): 워커가 PNG 를 `Read` 로 열어 «장비 8칸에 시대색 타일·아이콘·Lv · 파란 와이드 탈것 칸 · 오브 줄 · 🛡️+라벨+핍 폴백» + `UiSmokeTests` PlayMode 빨강 0. **`ui_score` 8.0 은 이 작업의 판정이 아니다**(T57 결정 159 와 같은 이유 — 채점은 화면 전체를 재고 이 화면은 미니 씬(T54 뒤)·한글 두부(T53)·3D 배경(T35)이 점수를 깎는다 · 런 90 2.0 → 런 113 2.9).
- ✅ 2026-09-13 워커 A — 정본 `renderPlayerInfo` 뼈대 그대로(장비 칸 = ForgeUi 조각 · 탈것 와이드 칸 · 스킬/펫/탈것 오브 · 폴백 핍) · 수치는 `PlayerInfoUi.json` · 런 113 PNG 눈 확인.
- 범위: `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs` · `Assets/Forge/Resources/PlayerInfoUi.json`(새 · T20 `PetSkillUi.json` 꼴 · `catalog.json` 은 T62 lock 이 쥐고 있어 이 회차엔 안 연다 — T33 이 합칠 수 있다) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.

- ✅ 2026-09-12 워커 O(sess-2140-18689): 풀 넷(숫자 TMP 되쓰기 + 알파는 CanvasRenderer · 임팩트 슬롯+재질 조합 풀+세대 토큰 · 파편 P/궤적 Pt/FxAnims 항목) · Core 틱 버퍼 · 자를 계수기 «GC Allocated In Frame» 으로. 실측(런 78~90): 풀은 돈다(숫자 126→글자 오브젝트 24 · 임팩트 슬롯 68) · 전부 ≈1MB 중 **렌더 몫 878KB(81%)** · 렌더 끔 200KB(러너 바닥 107~361KB 포함) → 판정은 `GcNoRenderCap`(640KB) · 렌더 몫은 **T64** 로.
### T66 ✅ — 던전 입장 뒤 스테이지 라벨이 본대 라벨(«쉬움 1-1»)로 되돌아간다: `DungeonUiTests` 1/4 빨강 → **T55 2회차가 먼저 고쳐** 남은 몫 = T7 sim 대조가 던전 라벨을 센다 (검증·Core 대조 · 뒤 순서 없음 · 임자 없는 빨강으로 등재)
- 무엇(처음): 런 90(8aade87) `DungeonUiTests.던전_상세는_난이도_보상_열쇠를_보이고_입장하면_판이_선다` — «Expected "망치 도둑" · But was "쉬움 1-1"». 정본 `combat.js:113` `setupStage()` 끝의 `UI.updateStageLabel()`(`ui.js:1313`)은 **`Dungeons.run` 이면 «{kr} {stage}단계»**, 아니면 본대 라벨인데 Core `Battle.SetupStage`(T7)는 던전 문맥에서도 본대 `StageName()` 을 냈고, T55(3a8110e) 의 `Dungeons.Attach` 뒤로 그 이벤트가 다음 프레임 HUD 의 던전 라벨을 덮었다.
- 겹침: **T55 2회차(9014a39 · 워커 N)** 가 같은 회차에 같은 뿌리를 `DungeonRun.Label`(+ `SetupStage` 한 줄 · `Dungeons.BattleRun` 한 줄 · EditMode 1)로 먼저 고쳤다 — Core 쪽은 T55 절·기록을 본다(결정 152).
- 남은 몫(T66): `tools/sim/sim_combat.js` 의 UI 스텁 `updateStageLabel` 이 던전에서도 `stageName()` 을 적어 `combat_dungeon_tier` 기대값이 «매우 어려움 24-8» 이었고, C# `SimScenario.Context` 는 `Label` 을 안 채워 폴백으로 우연히 같았다 → **던전 라벨이 빠져도 원작 대조가 초록**. 스텁을 `ui.js:1313` 대로(`Dungeons.run` 이면 `${kr} ${run.stage}단계` · 아이콘은 `<img>` 노드라 글자에 없다) 고치고 시나리오에 `kr`·해석값 `label` 을 넣어 `Context` 가 읽는다 · 기대 JSON 재생성(`dungeon_tier` 한 줄 · 다른 둘 diff 0).
- 판정: `dotnet test` 507/507 + 고장 주입(`Context` 의 `Label` 읽기를 빼면 `원작_대조_던전_티어상승` 빨강) + CI dotnet 잡 초록. `DungeonUiTests` 4/4 는 N 의 커밋이 든 유니티 잡에서.
- 범위: `tools/sim/sim_combat.js` · `tools/sim/expected/combat_dungeon_tier.json`(재생성) · `Assets/Tests/EditMode/BattleTests.cs`(`SimScenario.Context` 한 줄). Core 파일은 안 만진다(T55 lock 범위).
- ✅ 2026-09-12 워커 P(sess-2254-41204): e562f5e · dotnet 507/507 · 고장 주입으로 대조가 던전 라벨 누락을 잡는 것 확인 · CI 런 97 dotnet 잡 초록.

### T67 ✅ — 유니티 잡이 «모드 하나를 통째로 안 돌린 채» 빨강인 것을 아무 자도 말하지 않는다 (게이트 · 뒤 순서 없음 · `ci.yml` 한 파일 · 워커 M 등재)
- 실측(2026-09-12 · CI **런 94** · `acff94a`): `editmode-results.xml` 은 **505 전부 초록**인데 `playmode-results.xml` 은 **없다**(잡 로그 «cat: /github/workspace/unity-test-results/playmode-results.xml: No such file or directory» 두 줄 · 그 뒤 «Test run failed with exit code 1»). PNG 0장 · `screens` 는 T51 갈래로 지난 40장을 이어받아 올렸다.
- 왜 아무도 못 보나: 요약 스텝의 경고는 «XML 이 **하나도** 없을 때» 만 뜬다 — 한쪽 모드만 죽으면 EditMode 총계만 예쁘게 찍히고 끝난다. `screens` 의 `playmode-red.txt` 도 EditMode 만 담은 채 «== 런 끝: Passed · 초록 505 · 빨강 0» 으로 끝나, 다음 회차 워커가 그 꼬리를 보고 «내 커밋이 든 런은 돌았다» 로 읽는다. ROUTINE §1 «테스트 0개는 빨간 테스트보다 나쁘다» 가 막으려던 자리에 자가 없다.
- 런 94 의 실제 원인(코드 탓 아님 · §1 «라이선스 좌석» 갈래): EditMode 뒤 개인 라이선스 **좌석 반납이 4번 실패**했다 — «An error occured while trying to return the ULF license. Ulf license file not found (/root/.local/share/unity3d/Unity/Unity_lic.ulf) (1404)» → «Failed to return the Personal license seat after 4 attempts · That seat is likely still held … otherwise later runs on this account will fail with 'no available seats'» → «Failure» 로 끝나 PlayMode 단계가 결과 XML 을 못 냈다.
- 무엇을 한다(전부 `.github/workflows/ci.yml`): ⓐ 요약 스텝에 **모드별 존재 검사** — `editmode-results.xml`·`playmode-results.xml` 중 없는 것이 있으면 `::error::` 로 «그 모드가 한 개도 안 돌았다» 를 이름으로 찍는다(있는 쪽 총계는 그대로) ⓑ 잡 로그의 좌석 문구(`Failed to return the Personal license seat` · `no available seats` · `Unable to activate license`)를 러너 출력에서 잡아 «라이선스 좌석» 을 따로 한 줄 ⓒ `ui-screens/playmode-red.txt` **머리**에 «이 런에 PlayMode 결과 없음(모드 XML 부재)» 한 줄을 덧붙여 `screens` 로 읽는 워커가 꼬리만 보고 속지 않게 한다 ⓓ 좌석 실패가 다음 런에도 이어지면 §1 대로 «주인 콘솔 에러 보고함» 에 «유니티 라이선스 좌석» 한 줄.
- 판정: `ci.yml` 만 바뀐다(코드·테스트 0줄) · 다음 main 런에서 dotnet·datasync 잡 초록 · 모드 XML 이 둘 다 있는 런에서는 새 줄이 조용하고, 한쪽이 없는 런에서는 `::error::` 와 `playmode-red.txt` 머리줄이 보인다.
- 범위: `.github/workflows/ci.yml`.

### T68 ✅ — 오프라인 보상 팝업 머리가 정본의 어두운 판이 아니다: 밝은 회색 판 + 검정 글자 · 요율이 아이콘 옆 · 수집 버튼 빨간 점 없음 (Game·UI · T13·T22 뒤 · T28 5회차가 눈으로 잡음)
- 실측(2026-09-12 · T28 5회차 · 워커 N · 런 95 `screen_offline.png` **3.7/10** ↔ 정본 `ref/screens/shot-042110.png` + `style.css`·`ui.js`):
  - 정본 `.offline-top`(`style.css` 260)은 **평면 `#0e111b` 어두운 판 · 흰 글자 · 카드 높이의 42.8%** 이고 `«수집 시간:»` 은 `#ccc`, 경과 시간·요율은 `--pp-green`. 클론 `OfflinePopup.cs` 29 는 `UiKit.Panel(top, "bg", "pp_panel")`(#efefef) + `pp_ink` 검정 글자 + 제목만 `stage_ink` 흰색(밝은 판 위 흰 글자라 링에 기대 읽힌다).
  - 정본 `.offline-rate` 는 `flex-direction: column`(원형 아이콘 2.6rem **위** · `1.13/초` 글자 **아래** · 두 칸 사이 2.4rem). 클론 `Rate()` 는 원형 아이콘과 글자를 **옆으로** 붙였다.
  - 정본 수집 버튼 우상단에 `.offline-collect-dot`(`.7rem` 빨간 원 · 흰 테두리 · `style.css` 309)이 있다. 클론엔 없다.
  - ~~걷어낸 것: 원작 샷의 **파란** 수집 버튼은 옛것 — 정본 `.btn.primary` 가 초록(`#1f4a2c`/`#2ea043`)이라 클론의 초록이 맞다~~ **정정(T144 · 2026-09-14 · 워커 O)**: 668 의 초록은 기본 규칙(0-2-0)이고 팝업 안에서는 3548 `.modal-card .btn.primary { background: var(--pp-blue) }`(0-3-0)가 덮는다 — 오프라인 카드는 `modal-card offline-card`(ui.js 5891)라 **파랑(#005dff)** 이 정본이다(307 주석 «원본 파란 면 실측» 도 같은 말). 클론도 `pp_blue`/`pp_blue_dk` 로 고쳤다. 합계줄 `8.87k`·`149.05` 는 정본 주석대로 **흰 칠 + 검정 링**(클론은 검정 칠 · 링 규칙은 T25 갈래).
- 무엇을 한다: 머리 판을 어두운 색으로(카탈로그에 `#0e111b` 에 가까운 키가 없으면 키 하나 추가 — `catalog.json` 은 **T62 lock 이 풀린 뒤** · 그 전엔 `pp_ink`(#17181a)로 먼저) + 글자 색을 정본대로(흰 · #ccc · 초록) · 요율 칸을 세로 배치로 · 수집 버튼에 빨간 점. 수치는 `catalog.json`/`PopupKit` 에서(§1).
- 판정: PNG 눈 확인(위 절반이 어둡다 · 요율이 아이콘 아래 · 수집 버튼 빨간 점) + PlayMode `OfflinePopupTests` 초록 + 콘솔 빨강 0. `ui_score` 는 화면 전체(3D 배경·한글 두부)를 재서 팝업 범위로는 못 닿는다(T57 결정 157 과 같은 갈래 · 런 113 실측 1.4 «짝 없음» · 결정 173) — 화면 점수는 T28 이 본다.
- 범위: `Assets/Scripts/Game/Ui/OfflinePopup.cs` · `Assets/Forge/catalog.json`(색 키 하나 · T62 뒤) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.
### T69 ✅ — 자: §7 표에 **이름이 없는** 작업을 잡는다 — 지금 17개가 빠져 T33 의 완주 판정이 그 위를 지나간다 (검증 · 뒤 순서 없음 · T33 이 이 자를 쓴다)
- 범위: `Assets/Scripts/Game/Ui/OfflinePopup.cs` · `Assets/Tests/PlayMode/OfflinePopupTests.cs`(자기 파일 · `UiSmokeTests.cs` 는 T54·T63·T65 lock 이 쥔다) · `Assets/Forge/catalog.json`(색 키 `#0e111b`·`#ccc` · T62 뒤).
- 1회차(2026-09-12 · 워커 C · sess-2336-18715): 코드 + PlayMode 끝 · `pp_ink`·`pp_gray`·`offline_green` 으로 먼저 · ✅ 는 CI `screen_offline.png` 눈 확인 + `ui_score` 뒤(PROGRESS 완료 기록).
- 왜: `check_final_table.py`(T49)는 §7 «상태» 칸에 **적혀 있는** 번호의 표시만 PROGRESS 와 맞춰 본다 — «PROGRESS 에 있는 작업이 §7 어딘가에 적혀 있는가» 는 아무도 안 본다. 실측(2026-09-12 23:33 · 워커 K): PROGRESS 작업 67개 중 **17개가 §7 에 이름조차 없다** — 그중 `ui.js` 줄에 들어가야 할 **T62·T63·T65**, `scene3d.js` 줄의 **T54**, 품질 줄의 **T50·T64**, 그리고 `T66`(던전 라벨)이 게임 쪽이다. §7 은 주인이 정한 «다 옮겨졌다» 의 기준이고 T33 이 «§7 전 줄 ✅» 로 완주를 선언하므로, 빠진 작업은 **열린 채로 완주 선언을 통과한다**.
- 방법: ⓐ `check_final_table.py` 에 «미등재» 갈래 — PROGRESS 표의 번호 중 §7 본문 어디에도 안 나오는 것을 찍고 rc 1. 원작 모듈에 안 붙는 **도구·게이트·CI 작업**은 §7 에 그 줄을 하나 두어(«원작 밖 · 도구·게이트·CI») 거기 적는다 — 코드 안 예외 목록을 만들지 않는다(목록은 낡는다). ⓑ 지금 빠진 17개를 제 줄에 채운다. ⓒ `--self-test` 에 «§7 에 없는 번호가 있으면 rc 1» 칸.
- 판정: `check_final_table.py` rc 0(채운 뒤) · `--self-test` 초록 · 빠진 번호 0.
- 범위: `tools/check_final_table.py` · `docs/ROUTINE.md`(§7 표) · `docs/PROGRESS.md`.

### T70 ✅ — `BootstrapTests` 를 T54 의 새 카메라 계약에 맞춘다: «앱 상자 9:16» + «카메라 rect = `#game-area` 띠» (검증 · T54 가 바꾼 계약 · 뒤 순서 없음)
- 왜: T54 2회차(`b9c5fd9`)가 3D 카메라를 앱 상자 전체에서 원작 `#game-area` 띠(상단바 밑 ~ 장비 시트 위)로 좁혔다. 그 회차는 `SafeAreaTests`·`UiShotsTests`·`UiSmokeTests` 를 함께 고쳤지만 **T1 의 `BootstrapTests` 는 아직 «카메라 화면비 = 9:16» 을 단언**해 CI 런 103 에서 빨갛다(`Expected 0.5625 · But was 1.1538`). 그 파일은 T54 의 «범위» 칸에 없어 어느 살아 있는 lock 도 쥐고 있지 않다 — ROUTINE §0-6 «남의 lock 이 없는 빨강».
- 무엇을 한다: 단언을 **계약대로** 옮긴다 — ⓐ 앱 상자(`Viewport.Letterbox`)가 9:16 ⓑ 카메라 `rect` 가 `Viewport.GameArea(앱 상자, Bootstrap.GameAreaTop, GameAreaBottom)` 와 같다 ⓒ 카탈로그가 비면 띠가 앱 상자로 물러난다는 것까지(그 갈래에서만 카메라 화면비가 9:16). T54 의 판단을 되돌리지 않는다 — 테스트가 옛 계약을 쥐고 있던 것이다.
- 판정: `BootstrapTests` 초록(CI 유니티 잡) + 다른 테스트 영향 0.
- 범위: `Assets/Tests/PlayMode/BootstrapTests.cs`.

### T71 ✅ — 임자가 갈리는 빨강: `ShopUiTests` 의 채팅 미리보기 단언이 T63 의 «두 줄» 수정과 어긋난다 (검증·UI · **T62·T63 lock 이 풀린 뒤** · 워커 K 등재)
- 실측(2026-09-13 00:33 · 워커 K · CI 런 113 `playmode-red.txt` · PlayMode 84 중 빨강 2):
  `ShopUiTests.프로필_설정_채팅_패스_오프라인_디버그_스텁_토스트가_열리고_닫힌다`(`ShopUiTests.cs:210`)가 `chat-preview-msg` 에 «moonzzanf: 안녕» 이 들어 있기를 기대하는데 실제 값은 **«안녕»** 이다.
- 왜: T63(`130b8b1` · 워커 J)이 정본 `renderChatPreview`(`ui.js` 5288~5299) 대로 미리보기를 **이름 줄(`chat-preview-name`) / 메시지 줄(`chat-preview-msg`) 두 줄**로 갈랐다 — 그것이 정본이고 **수정이 옳다**. 옛 단언이 «닉네임: 메시지» 한 줄을 전제하고 있을 뿐이다. T63 은 제 범위의 `UiSmokeTests.cs` 는 같이 고쳤지만 `ShopUiTests.cs` 는 **T63 범위 밖**이다.
- ⚠ **이 절이 있는 이유는 임자가 갈리기 때문이다**: 빨강의 «원인» 은 T63(워커 J) 쪽이고 빨강의 «파일»(`ShopUiTests.cs`)은 **T62 범위**(워커 G)다. 규약대로면 T63 은 «내 범위 파일이 아니니 그 임자 몫» 이고 T62 는 «내가 낸 빨강이 아니다» 라 **둘 다 안 고치고 지나갈 수 있다**. 둘 중 누구든 제 회차에 고치면 이 번호는 `⛔ 흡수` 로 닫는다(T36 의 꼴).
- 무엇을 한다: `ShopUiTests.cs:210` 의 단언을 두 줄 구조에 맞춘다 — `chat-preview-name` 이 닉네임을, `chat-preview-msg` 가 «안녕» 을 쥐는지 **각각** 본다(합쳐 놓은 문자열을 다시 만들지 않는다 · 정본이 두 줄이다).
- 판정: CI 유니티 잡에서 `ShopUiTests` 초록 + PlayMode 빨강 0(그 테스트 갈래) + `dotnet build` 초록(T48 하니스가 PlayMode 도 컴파일한다).
- 범위: `Assets/Tests/PlayMode/ShopUiTests.cs`(그 한 단언).

### T72 ⛔ — 소환 결과 연출을 «두 번 탭» 으로 모는 PlayMode 단언이 CI 프레임 길이에 따라 터진다: `PetUiTests` 탈것 갈래 NRE (검증·UI · **T58 lock 이 풀린 뒤** · 워커 E 등재)
- 실측(2026-09-13 01:13 · 워커 E · CI 런 118 `playmode-red.txt` · PlayMode 85 중 빨강 3):
  `PetUiTests.탈것_시트_소환_상세_장착_타기_업그레이드_확률_팝업` 이 `PetUiTests.cs:330` 에서 `System.NullReferenceException`. 그 줄은 **첫 소환의 두 번째** `SkillSummonResultView.Current.OnTap()` 이다 — 즉 첫 탭에서 이미 `Current` 가 `null` 이 됐다. 런 113 에서는 초록이었다(그 런 빨강 둘은 T68·T71).
- 왜(정본 코드로 가른 것 · 클론 결함이 아니라 **단언의 가정**이 틀렸다): `SkillSummonResult.OnTap()` 은 «연출 중이면 스킵 · **끝났으면 닫기**»(원작 `onSummonResultTap` 과 같다 · 637행) 이고 `Close()` 가 `Current = null` 을 한다(648행). 그런데 연출 진행은 **벽시계**(`Time.unscaledTime` · 556·570행 `elapsed >= delays[마지막] + sr_tail_ms` → `Finish()` → `done = true`)다. 소환 1회짜리 표는 `sr_charge_ms + sr_tail_ms` 로 짧아서, CI 한 프레임이 그보다 길면 테스트의 `yield return null` **한 번 만에 연출이 저절로 끝난다** → 첫 탭이 «스킵» 이 아니라 «닫기» 가 되고 둘째 탭이 null 을 친다.
- 그래서 이것은 **프레임 길이에 달린 단언**이다(«한 번은 스킵 · 한 번은 닫기» 를 가정). 같은 꼴이 펫 갈래에도 있다(`PetUiTests.cs:201·203` · 런 118 에서는 우연히 초록) — 둘 다 고친다.
- 무엇을 한다: 탭을 **횟수로 세지 말고 상태로 몬다** — `while (SkillSummonResultView.Current != null && 프레임 한도) { Current.OnTap(); yield return null; }` 꼴로 «닫힐 때까지» 두드리고, 그 뒤 `Assert.IsFalse(Sheet.Modal.IsOpen(SkillSummonResultView.ModalName))` 로 판정한다. 연출 코드(`SkillSummonResult.cs`)는 정본 그대로 두라 — 고칠 것은 테스트의 가정뿐이다.
- ⚠ 임자: 빨강의 파일 `Assets/Tests/PlayMode/PetUiTests.cs` 는 **T58 범위**(워커 B · lock 살아 있음)이고 연출 파일은 T20(✅ · lock 없음)이다 — T71 과 같은 «임자가 갈리는» 자리라 이 절로 남긴다. T58 이 반납하면 누구든 잡는다 · T58 이 제 회차에 같이 고치면 ⛔ 로 흡수.
- 판정: CI 유니티 잡에서 `PetUiTests` 전 갈래 초록(런 118 은 1건 빨강) · `dotnet build` 초록(T48 하니스가 PlayMode 도 컴파일한다).
- 범위: `Assets/Tests/PlayMode/PetUiTests.cs`(소환 결과 탭 두 자리).
### T73 ✅ — AudioBank 베이크 스레드가 초당 수십 MB 의 관리 쓰레기를 만든다(런 118: 447~638KB/프레임 · 정본 sfx.js 는 브라우저 WebAudio 가 굽는다) (Game·성능 · T30 뒤 · T64 가 등재)
- 실측(2026-09-13 · T64 · CI 런 118 `perf-t64.txt` · 프로파일러 원시 프레임 전 스레드): 부하 장면 계수기 «프레임 전부» 681KB~1,164KB 중 메인 스레드 GC.Alloc 은 145~168KB 뿐이고 **`Scripting Threads/AudioBank` 스레드가 447KB(처음)~638KB(끝)/프레임** — `AudioBank.Work` → `AudioFactory.RenderSfx/RenderMusic` 이 잡마다 버스·FFT·링·출력 배열을 새로 만든다(`SynthRenderer.cs:86~127` `new double[len]` × 5 · `Dsp.cs:78·150~162` FFT 배열 · `SfxSynth.cs:212`). 씬을 다시 열 때마다(테스트 · 앱 재시작) 처음부터 다시 굽고, 정확한 변형이 없는 호출은 게임 중에도 백그라운드에 요청한다.
- 왜 문제인가: 관리 힙은 스레드 공용이라 배경 스레드의 쓰레기도 **메인 스레드를 멈추는 GC** 를 부른다(원작은 WebAudio 노드가 네이티브로 굽는다 — 관리 쓰레기 0). 60fps 지시(§1)의 «프레임당 GC 0» 은 스레드를 가리지 않는다.
- 할 일: ⓐ 렌더러의 작업 배열(버스 5 · FFT re/im · 링 · 출력)을 **잡 사이에 되쓴다**(길이 상한으로 한 번 잡고 `Array.Clear`) — 음색·표는 손대지 않는다(원작 합성 그래프 그대로 · `AudioTests` 결정론 단언이 지킨다) ⓑ 다 구운 결과 `float[]` 만 새로(클립 데이터) ⓒ 부팅 베이크 순서는 그대로(효과음 테이크 0 → 음악 normal → …). 수치는 코드에 박지 않는다(§1).
- 판정: `PerfBudgetTests` 의 «[T64] 프로파일러 GC.Alloc 버킷» 줄에서 `AudioBank` 스레드 합이 베이크 중에도 메인 스레드 합 아래 + `AudioTests`(dotnet 507+)·`AudioSmokeTests` 초록 + 콘솔 빨강 0.
- 범위: `Assets/Scripts/Core/Audio/SynthRenderer.cs` · `Dsp.cs` · `SfxSynth.cs`(배열 되쓰기만) · `Assets/Scripts/Game/Audio/AudioBank.cs`(되쓰기 버퍼 소유만) · `Assets/Tests/EditMode/AudioTests.cs`(되쓰기 뒤 결과가 같다는 단언 1).
- ✅ 2026-09-13 워커 N(sess-0125-19048) 1회차: `RenderWorkspace`(버스 5 · 링 · FFT · 합성곱 출력 · `NoisePool`) + `AudioBank` 가 하나 쥐고 되쓰기 · 되쓰기 갈래 지문 = 새 배열 갈래(EditMode 1) · dotnet 511/511 · CI 런 127 초록(EditMode 511 · PlayMode 87/87 · AudioBank 스레드가 [T64] 버킷 상위에서 사라짐) · 실측 효과음 24종 96.9MB → 6.3MB · 음악 루프 52~60MB → 3.4~3.6MB.

### T74 ✅ — 스킬 큐브 연출(`FxCubes`)이 시전마다 큐브 묶음마다 새 Material 을 만들고 버린다 — T50 의 «조합별 재질 되쓰기» 를 여기에도 (Game·성능 · T50·T52 뒤 · T64 가 등재)
- 실측(2026-09-13 · T64 · CI 런 113·118): 부하 장면 200프레임당 Material 오브젝트 수가 «스킬 재시전 끔» 에서 **−135**, 다시 시전하면 **+135** — 시전 때 만들고 액터가 끝나면 버린다. 편집기에선 그때마다 `MaterialEditor.ApplyMaterialPropertyDrawersFromNative` 가 52~59KB/프레임(메인 스레드 정상 상태 150~170KB 의 1/3)을 문다 · 플레이어에는 그 후처리는 없지만 네이티브 재질 생성·해제는 남는다. 자리: `Assets/Scripts/Game/SkillFx/FxCubes.cs:71` `FxMaterials.Instance(hex, opacity)`(호출자 소유 · 색을 매 프레임 바꾸는 재질).
- 할 일: `FxUnlitMaterials.Take/Release`(T50 · 가산·깊이·양면·텍스처 키 · 색·불투명도는 꺼낼 때 칠함) 꼴로 **FxCubes 의 재질을 풀에서 꺼내고 액터가 끝날 때 돌려준다** — 색을 매 프레임 바꾸는 재질은 «묶음마다 하나» 가 필요하므로 키 = (불투명 여부 · 가산) · 되돌릴 때 색을 리셋. 연출·수치는 그대로(정본 fx 그래프 · `SkillFxTests` 가 지킨다).
- 판정: `PerfBudgetTests` «시간 추이» 줄의 «스킬 재시전 끔/켬» 재질 증가가 **±10 이내** + `MaterialEditor…` 버킷이 10KB 아래 + `SkillFxTests` 초록 + 콘솔 빨강 0.
- 범위: `Assets/Scripts/Game/SkillFx/FxCubes.cs` · `Assets/Scripts/Game/Battle/FxMaterials.cs`(풀 갈래만) · `Assets/Tests/PlayMode/SkillFxTests.cs`(풀 회전 단언 1).
- ✅ 결론(2026-09-13 · 런 127 · 워커 B): 풀(`FxMaterials.Take/Release` · 키 = 투명 여부·가산) 뒤 200프레임당 재질 증가 «전부 +6 · 재시전 끔 +0»(종전 −135/+135) · 편집기 재질 후처리 5.9KB/프레임(종전 52~59KB) · 캡처 중 새 Material 61 → 0 · `SkillFxTests` 7/7 · 빨강 0. 기록은 PROGRESS «T74 완료 기록».

### T75 ✅ — 상점 보석 카드 안쪽: 자가 밴드8 에서 블록을 하나도 못 가른다(원작 7블록) (Game·UI · T62 뒤)
- 실측(2026-09-13 · T62 3회차 · 워커 G · 런 124): 카드 **자리**는 맞췄다(77.0%H ↔ 원작 76.3). 남은 것은 카드 **안쪽** — `ui_score` 가 원작 밴드8 의 블록 7개 중 둘을 짝 못 짓는다.
- 정본: `.shop-gem-card`(`style.css` 2966~2980) — 카드 안 세로 배분이 «수량 줄 +8~36px · 그림 +38~98px · 가격 버튼 +101~124px»(카드 상단 기준) · `.shop-gem-amt` 는 흰 글자 + 4px 검정 외곽선 · `.shop-gem-icon .ico` 는 6.84%H 정사각.
- 판정: `ui_score --score --only shop` 의 밴드8 미짝 0 + PNG 눈 확인 + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/ShopSheet.cs`(보석 카드 갈래만) · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/ShopUiTests.cs`.

### T76 ✅ — 전투 데미지 숫자가 열린 시트 위에 겹쳐 그려진다 (Game·UI · T8·T18 뒤)
- 실측(2026-09-13 · T62 3회차 · 워커 G · 런 124 `screen_shop.png`): 상점 시트가 열린 화면인데 첫 특가 카드 오른쪽 위에 회색 «▼745» 가 떠 있다 — `DamageNumbers` 의 `dmg-hero`(접두 «▼») 글자다.
- 원작: 데미지 숫자는 `#game-area` 안에 있고 시트·팝업은 그 위를 덮는 층이라 **시트가 열리면 안 보인다**. 클론은 전투 글자가 팝업 층 위로 온다.
- 할 것: 데미지 숫자(그리고 같은 층에 붙는 전투 글자)를 팝업 층 **아래** 층에 붙이거나, 시트가 열린 동안 그 층을 끈다(정본이 어느 쪽인지 `ui.js` 의 시트 열기 경로를 읽고 고른다).
- 판정: PlayMode 로 «시트 연 뒤 전투 글자가 팝업 위에 없다» 단언 + PNG 눈 확인 + 빨강 0.
- 범위: `Assets/Scripts/Game/Battle/DamageNumbers.cs`(붙는 층만 · `Layer`) · `Assets/Tests/PlayMode/DamageLayerTests.cs`(자기 파일). `UiRoot.cs`·`Popups.cs` 는 안 열었다(정본이 층 순서라 숫자 쪽만 옮기면 된다 · 1회차 sess-0150-8332).

### T77 ✅ — 촬영 시드(`UiShotsTests.Seed`)의 상단바 전투력이 `⚔ 45` 다: 장비 8부위가 전투 스탯에 안 탄다 — 정본 SEED 의 `Combat.recalcHero()` 자리가 클론 Seed 에 없다 (검증·UI · **T54 lock 이 풀린 뒤**(`UiShotsTests.cs` 같은 파일) · 보고함(워커 J) → 워커 T 등재)
- 실측(2026-09-13 01:2x · 워커 J · 런 118 `screen_main.png` · 보고함): 상단바 `⚔ 45`. 같은 화면의 장비 8부위는 Lv.26~27 = `20 + ageIdx` → 시대 6~7(multiverse·quantum · 정본 `forgeProbabilities[29]` 가 multiverse 68%·quantum 23%). 정본 식이면 그 장비 **하나**의 값이 `12×6^6 ≈ 56만`(공격)·`70×6^6 ≈ 326만`(체력)이라 전투력은 **수백만**이어야 한다 — 45 는 맨몸(≈36)에 부스러기가 붙은 수다.
- 원인(코드 읽기 · 워커 T): 정본 `web/tools/shot-screens.js` SEED(113~165행)는 장비·스킬을 세이브에 직접 넣은 뒤 **144행 `Combat.recalcHero()`** 를 부른다. 클론 `UiShotsTests.Seed()`(106~191행)는 `F.Gear.Set(...)`·`sk.Skills.Add(...)` 로 상태에 직접 넣고 `F.Push()`·`P.Sync()`·`M.Touch(false)` 만 부른다 — 셋 중 어느 것도 `Battle.RecalcHero` 를 부르지 않는다(`PetSkillHost.Sync` 는 `Skills.RecalcRequests` 가 바뀐 때만 `RequestRecalc` · 상태에 직접 `Add` 하면 그 수가 안 오른다 · `MetaHost.Touch` 는 HUD 만 다시 적는다). 그래서 HUD 는 부팅 때(장비 0) 계산해 둔 스탯을 읽는다. 접착 자체(`HeroStatsGlue.Make` → `GearSystem.HeroStats`)는 `HeroStatsGlueTests` 가 «전투 atk = GearSystem.HeroStats» 로 지키고 있어 의심 자리가 아니다. **45 − 36 ≈ 9 의 출처**는 안 밝혔다(부팅 뒤 어느 재계산이 무엇을 태웠는지) — 아래 단언이 그것도 같이 잡는다.
- 무엇을 한다: ⓐ `Seed()` 에서 정본 144행 자리(스킬을 세운 뒤 · 펫 전)와 끝(`P.Sync()` 뒤)에 정본 `Combat.recalcHero()` 상당 = `HeroStatsGlue.Recalc()`(또는 `P.RequestRecalc()`)를 부른다. ⓑ 캡처 전에 단언 하나: `M.MyCp` 가 `F.GearSys.HeroStats()` 로 `Battle.CombatPower` 식을 돌린 값과 같고(`Big` 지수·가수) 맨몸 전투력(`BareHeroStats` 상수로 같은 식)보다 크다 — 이 단언이 빨강이면 원인이 시드가 아니라 접착(T43·T55 갈래)이므로 그때 새 번호로 등재한다. ⓒ 수치는 안 박는다(§1 — 맨몸은 `BareHeroStats` · 식은 `Battle.CombatPower`).
- 판정: 다음 유니티 잡 `screen_main.png` 상단바가 `⚔ 45` 가 아니라 **수백만 단위**(`NumFmt` 표기 «N.Nm») + 위 단언 초록 + PlayMode 빨강 0 + T28 채점의 `main` 점수가 안 내려간다.
- **T54 뒤인 이유**: `UiShotsTests.cs` 가 T54(워커 I)의 살아 있는 lock 범위(«촬영 rect»)다 — 규약 «같은 파일이면 뒤 번호가 기다린다». 이 절이 손대는 자리는 `Seed()` 한 곳(촬영 rect 와 다른 함수)이라 T54 가 그 파일을 범위에서 빼거나 반납하면 바로 잡는다. T54 가 제 회차에 같이 고치면 `⛔ 흡수`(T71 꼴).
- 범위: `Assets/Tests/PlayMode/UiShotsTests.cs`(`Seed()` 두 줄 + 단언 하나). 게임 코드 0줄.

### T78 ✅ — 팝업 셋에 모달 딤이 없고 상단바 자리가 순수 검정이다: `forge-list` · `forge-detail` · `autoforge` (Game·UI · T19·T57 뒤 · 임자 없음 · 검수 Q 등재)
- 실측(2026-09-13 02:0x · 검수 Q · **런 127**(44b89f6 · 유니티 잡 전체 초록)의 PNG 를 Read 로 열고 픽셀로 잰 것 · 촬영 프레임 회귀는 이 런에서 이미 걷혔다):
  - ⓐ **상단바 자리가 순수 검정**이다 — `screen_forge-list`·`screen_forge-detail` 은 y=0~117, `screen_autoforge` 는 y=0~79 가 **RGB (0,0,0)**. 딤이 씌워진 어두움이 아니라(딤이면 상단바가 비쳐 (3,4,4) 꼴로 남는다) **아무것도 안 그려진 자리**다. 같은 층의 `screen_forge-info` 는 같은 자리가 (14,18,21) 로 멀쩡하다 — 팝업 셋만 그렇다.
  - ⓑ **모달 딤이 통째로 없다** — 카드 뒤 3D 세계(y≈900 에서 `(0,128,32)` 생 초록)·채팅줄·탭바(`(13,13,28)`)가 **하나도 안 어두워진다**. 정본은 팝업이 뜨면 화면 전체가 거의 검게 덮인다(`ref/screens/shot-042905`·`shot-042931`·`shot-043117` 은 위·아래 모두 `(0,0,0)`~`(2,2,2)` · 규약은 `css/style.css` 의 모달 딤 `rgba(0,0,0,.5)` — 356행 주석이 «모달 딤(rgba(0,0,0,.5))» 을 그렇게 부른다).
  - ⓒ `screen_autoforge` 는 카드 아래가 탭바에 물려 **닫기 ✕ 가 반쯤 가린다**.
- **T57(✅)이 고친 것과 다른 건**이다: T57 은 «판이 없다 · 글자가 겹친다 · 빈 막대 · ✕ 둘» 을 고쳤고 판은 실제로 섰다. 남은 것은 **딤 층과 상단바 자리**다. T57 lock 은 없으니 임자 없는 결함이다.
- **자가 이것을 매 회차 오진한다**: `tools/ui_score.py` 가 이 셋을 «앱 상자가 그림을 안 채웠다 → **촬영이 어긋난 것이라 화면마다 재등재하지 마라**» 로 찍는다. 촬영 프레임은 런 127 에서 멀쩡한데(다른 화면 27장은 판을 채운다) 그 문구 때문에 T28 회차마다 그냥 지나간다. 이 작업이 화면을 고친 뒤 **그 경고를 «앱 상자 전체가 검다» 갈래와 가르도록** 좁힌다.
- 할 일: 원작 `ui.js` 의 그 세 팝업이 여는 순서대로 — 모달 딤 층(앱 상자 전체 · `rgba(0,0,0,.5)`)을 먼저 깔고 그 위에 카드를 놓는다. 상단바 자리가 비는 원인(상단바를 끄면서 그 아래 앱 상자 바탕이 안 그려지는 자리)을 찾아 딤이 그 자리도 덮게 한다. `autoforge` 카드 높이는 탭바 위까지로 — ✕ 가 안 가리게.
- 판정: PlayMode 단언(팝업이 열리면 앱 상자 네 귀퉁이 픽셀이 전부 딤 색 · 상단바 자리가 순수 검정이 아니다) + **PNG 를 Read 로 열어 눈으로**(§1) + `ui_score --score` 에서 세 화면의 «앱 상자 안 채움» 경고가 사라진다.
- 범위: `Assets/Scripts/Game/Ui/Popups.cs`(딤 층) · `Ui/Forge*`(ForgeInfoPopup·ForgeAutoPopup·ForgeUi 의 목록·상세 갈래) · `tools/ui_score.py`(경고 갈래 좁히기) · `Assets/Tests/PlayMode/ForgeUiTests.cs`.
- ✅ 결론(2026-09-13 · 런 134·140 · 워커 E): **ⓒ** 닫기 ✕ 가 탭바에 가리던 자리 → `PopupKit.FitBetweenBars`(카드를 «상단바 아래 ~ 탭바 위 − ✕ 걸침» 띠에) · **ⓑ** 탭바가 안 어두워지던 자리 → 정본 `style.css` slug `modal-dim-tabbar` 가 id 로 가른 «ⓑ 진짜 팝업»(`#forge-info-modal`·`#forge-item-modal`·`#autoforge-modal`) 그대로 `aboveTabBar: true`. 런 140 픽셀 실측: 탭바 바탕이 `main (13,13,28)` → 팝업 넷에서 `(7,7,17)`(상세는 딤 두 겹 `(4,4,9)`) · ✕ 는 탭바 위. `ForgeUiTests` 8/8 초록(`AssertCovers` 넷: 딤 존재·색·앱 상자 네 변·✕ 자리·`AboveTabBar` 층). **ⓐ(카드 위 띠가 순수 검정)는 팝업 결함이 아니었다** — 딤(0.6배)이 아니라 세계·HUD 가 **아예 안 그려진** 것이라(같은 런 `forge-info` 는 세계가 보인다) 촬영·카메라 갈래다 → **T54** 가 쥔 자리로 넘긴다(보고함 2026-09-13 04:2x). `tools/ui_score.py` 경고 갈래 좁히기는 T28 lock 이 계속 살아 있어 **안 만졌다** — T28 회차가 제 자에서 한다.
### T79 ✅ — 펫 업그레이드 모달이 화면을 안 덮는다: 원작은 모달이 HUD·탭바를 가리고 ✕ 가 하나인데 클론은 시트 위에 떠 ✕ 가 둘 (Game·UI · T58 뒤 · T28 8회차가 눈으로 잡음)
- 실측(2026-09-13 · T28 8회차 · 워커 M · 런 128 `screen_pet-upgrade.png` **1.7/10** ↔ 원작 `shot-042503`): 원작은 흰 카드가 화면을 통째로 덮어 **상단바·탭바·부모 ✕ 가 하나도 안 보이고** ✕ 는 카드 아래 **하나**다. 클론은 카드가 펫 시트 위에 뜨고 **상단바(900·8/250·8.15k)·탭바·부모 ✕ 가 그대로 보이며 ✕ 가 위아래로 둘**이다(카드 아래 하나 + 탭바 위 하나).
- 원작 머리 구성도 다르다: 원작 카드 머리는 «장착됨/Lv.6 카드 + 24m 피해/558m 체력 + 경험치 막대(87988/796140 경험치) + «합칠 펫 선택» 줄 + 회색 «업그레이드» 버튼» 인데, 클론은 이름/수치 두 줄 + 막대 + 작은 버튼 하나로 줄었다.
- T58(리그 도전·펫 업그레이드 팝업 · ✅)이 리그 쪽은 실제로 고쳤다 — 같은 런의 `screen_league-challenge.png` 는 흰 카드·제목·티켓 pill·상대 5줄이 원작대로 선다(점수 2.9 는 한글 두부 탓 · T53). 남은 것은 펫 업그레이드 하나라 새 번호로 뗀다.
- 무엇을 한다: 정본 `ui.js` `openPetUpgrade`/`renderPetUpgrade` 와 `style.css` 의 그 모달 클래스를 읽어 ⓐ 모달이 앱 상자를 덮게(딤 + 부모 시트·탭바 가림) ⓑ ✕ 는 하나 ⓒ 머리 구성(등급색 이름 · 피해/체력 두 줄 · 경험치 막대 문구 · «합칠 펫 선택» · 업그레이드 버튼)을 원작 순서대로.
- 판정: ~~`ui_score --score --only pet-upgrade` 가 **8.0 이상**(지금 1.7) + «탭바 안 보임»~~ — 주인 지시 딤 α .5(css 1711~1721)와 양립하지 않는다(결정 191 · 174 와 같은 갈래). → 워커가 PNG 를 `Read` 로 열어 «딤이 시트·탭바를 덮고 시트 흰 자리가 브라우저 .5 와 같은 ≈127 · ✕ 는 카드 것 하나(탭바 ✕ 는 딤 아래 원작 그대로) · 머리 구성 원작 순서» 확인 + `PetUiTests` 층·딤·✕ 단언 초록 + PlayMode 빨강 0.
- 실측 정정(2026-09-13 · 런 129 · 워커 B): 딤은 이미 탭바 위 층이다(흰 255 → 187 · 탭바 45 → 31). 어긋난 것은 **밝기** — 선형 색 공간이라 α .5 가 브라우저(127)보다 밝게 남는다 → `UiKit.PerceivedDim` 으로 환산(표 값은 정본 그대로).
- 범위: `Assets/Scripts/Game/Ui/PetUpgrade*` · `Assets/Scripts/Game/Ui/PetSkillModal.cs`(딤 한 줄) · `Assets/Scripts/Game/Ui/UiKit.cs`(`PerceivedDim` 한 함수) · `Assets/Tests/PlayMode/PetUiTests.cs`.

### T80 ✅ — 유니티 잡이 테스트 0개인 채 1초 만에 죽는다: `unity-test-runner@v4` 의 «latest» 풀이가 GitHub API 무인증 한도에 걸린다(런 131 «GitHub API returned 403») (게이트 · 뒤 순서 없음 · `ci.yml` 한 파일 · 워커 N 등재)
- 실측(2026-09-13 · CI **런 131** · `d09896b`): `Run game-ci/unity-test-runner@v4` 가 1초 만에 `Failed to resolve the latest game-ci CLI release: GitHub API returned 403.` → 모드 XML 둘 다 없음(T67 자가 `::error::` 로 말했다) · dotnet·datasync 잡은 초록. 코드 탓이 아니다 — §1 «라이선스 좌석» 과 같은 갈래의 **러너 인프라 빨강**이고 §0-6 «임자 없는 빨강».
- 뿌리: 러너 소스 `dist/index.js` `resolveLatestTag` — `cliVersion: latest` 면 `api.github.com/repos/game-ci/cli/releases/latest` 를 부른다. 토큰이 없으면 IP 당 60/h 인데 호스티드 러너는 IP 를 남의 잡과 나눈다(러너 소스 주석이 같은 사고를 적어 두었다). 읽는 이름은 `GITHUB_TOKEN` 또는 `GH_TOKEN`.
- 무엇을 한다: unity-test 잡의 러너 스텝 `env` 에 `GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}` 한 줄(공개 릴리스 읽기 · 권한 추가 없음 · 5000/h). 입력 `githubToken: ''` 은 그대로(체크 런 안 만들기). 버전 고정은 안 한다(결정 190).
- 판정: `ci.yml` 만 바뀐다(코드·테스트 0줄) · 다음 유니티 잡에서 러너가 CLI 를 받고 EditMode·PlayMode XML 이 둘 다 있다 · 403 이 또 나면 `cliVersion` 고정으로(잡 로그 머리 «Downloading game-ci CLI vX» 의 태그).
- 범위: `.github/workflows/ci.yml`.
- ✅ 2026-09-13 워커 N(sess-0224-1833): `GH_TOKEN` 한 줄 + 주석(4976926) · 런 134(dda999b)에서 러너가 CLI 를 받아 EditMode 513 · PlayMode 89 를 돌렸다(`missing_modes` 빈 값). 403 이 또 나면 `cliVersion` 고정 갈래 · 같은 런의 덤 빨강은 T81.

### T81 ✅ — 러너가 죽은 런은 «아티팩트 업로드» 스텝까지 덤으로 빨갛다 + 테스트 0개를 잡 결과가 안 말한다 (배포·검증 · **T80 lock 이 풀린 뒤**(같은 `ci.yml`) · 워커 F 등재)
- 실측(2026-09-13 02:24 · CI 런 131 잡 로그 · T80 과 같은 런의 **다른 줄**): 러너가 CLI 403 으로 죽자 ⓐ `actions/upload-artifact` 가 `##[error]Input required and not supplied: path` 로 또 빨개졌다 — `path: ${{ steps.tests.outputs.artifactsPath }}` 가 빈 값이라서다(러너가 죽으면 출력이 없다). 우리가 `artifactsPath: unity-test-results` 를 이미 **주고** 있으므로 그 자리를 고정 경로로 적으면 되는 자리다. ⓑ 러너 스텝이 잡 결과를 통째로 쥐고 있어, 앞으로 «XML 은 생겼는데 테스트가 빨간» 런과 «러너가 죽어 0개인» 런을 잡 결과만 보고 못 가른다(T67 요약이 로그로만 말한다).
- 무엇을 한다: ⓐ 업로드 스텝 `path` 를 `unity-test-results` 로 · `if-no-files-found: ignore`(없으면 조용히). ⓑ T67 요약 스텝이 «실패 수(`failed`)» 를 출력으로 내고, 그 뒤 «결과 판정» 스텝 하나가 «모드 XML 이 빠졌거나 실패 테스트가 있으면 exit 1» 로 잡 결과를 **명시적으로** 낸다(러너 스텝은 그대로 두고 신호만 겹으로 — 초록 오판 0).
- ⚠ **T80 뒤인 이유**: 같은 파일 `.github/workflows/ci.yml` 의 같은 잡이다(규약 «두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다»). T80 임자가 제 회차에 같이 고치면 이 번호는 `⛔ 흡수`(T71 꼴).
- 판정: `ci.yml` 이 YAML 로 읽힌다(자) + 다음 런에서 업로드 스텝이 안 빨갛다 + **테스트가 빨간 런은 여전히 잡이 빨갛다**.
- 범위: `.github/workflows/ci.yml`(`unity-test` 잡의 업로드 스텝 + 판정 스텝) · `docs/ROUTINE.md`(§2 이 절 · §7 한 칸) · `docs/PROGRESS.md`.
- 🔄 2026-09-13 워커 P(sess-0254-40139): ⓐ·ⓑ 를 `ci.yml` 에 넣었다(업로드 `path: unity-test-results`+`if-no-files-found: ignore` · 요약 스텝 `failed`·`unknown` 출력 · «결과 판정» 스텝 `if: always()` · 로컬 고장 주입 5경우 확인 · 결정 192). ✅ 는 다음 코드 커밋의 유니티 런에서 판정 스텝을 본 뒤.
- ✅ 2026-09-13 워커 P: 런 138(빨강 런)에서 업로드 success · «결과 판정» failure · screens 배포 그대로, 런 139(초록 런)에서 둘 다 success — 잡 API 스텝 결론으로 실측. lock 반납.

### T82 ✅ — 자 수리: `check_final_table` 의 «열림 ↔ 열림»(⬜ ↔ 🔄)은 막지 않는다 (검증·자 · T49 뒤 · 뒤 순서 없음)
- 왜: 누가 작업을 **선점하는 순간** PROGRESS 는 🔄 가 되는데 §7 대조표 칸은 그 커밋에서 잘 안 바뀐다 — 그 몇 분 동안 `check_final_table` 이 **모든 워커에게** rc 1 을 준다. 남의 선점 때문에 내 게이트가 빨개지고, 워커마다 «남의 칸 한 글자» 를 고치는 커밋을 낸다(2026-09-12 T61 · 2026-09-13 T81 실측 · 워커 H 가 두 번 다 고쳤다).
- 무엇이 맞나: §7 머리줄 규약과 T33 의 판정은 **✅ 만** 본다(«이 표의 모든 줄이 ✅ = 다 옮겨졌다»). ⬜ 인지 🔄 인지는 그 판정을 한 글자도 바꾸지 않는다.
- 한 것: `is_soft(glyph, want)` — 양쪽 다 열림 표시(⬜·🔄)면 **알리기만**(⚠ + rc 0). 닫힌 표시(✅⛔✂)가 한쪽에라도 끼면 **그대로 rc 1**(끝난 것을 안 옮겼거나 안 끝난 것을 끝났다고 적은 갈래 · T49 가 잡으려던 바로 그것) · «PROGRESS 에 그 번호 행이 없다»·«한 칸 두 표시» 도 그대로 막는다. 자기 검사 6칸(선점 직후·반납 직후·끝났는데 진행·§7만 끝났다고·접힌 것을 대기로·사유 문자열).
- 판정: `--self-test` rc 0 + 지금 §7 의 T81(선점 직후)이 ⚠ 로 내려가고 자가 rc 0 + 닫힌 표시 갈래는 여전히 rc 1.
- 범위: `tools/check_final_table.py`.
- ✅ 결론(2026-09-13 · 런 134·137 · 워커 B): 층은 원래 탭바 위였고 어긋난 것은 밝기 — `UiKit.PerceivedDim` 뒤 시트 흰 자리 187 → **127.0**(브라우저 .5) · 탭바 45 → 19 · ✕ 카드 것 하나(탭바 ✕ 는 딤 아래 원작 그대로) · 머리 구성은 코드 불변(런 118 PNG) · `PetUiTests` 6/6 · 빨강 0. 기록은 PROGRESS «T79 완료 기록».

### T83 ✅ — 촬영 한 장에 «UI + 게임 framing» 을 같이 담는다 (검증 · T54 뒤 · T27 촬영 길)
- 왜: T54 뒤로 판정 그림이 둘로 갈렸다 — `screen_*.png` 는 UI 를 옳게 보여 주지만 3D 는 **앱 상자 framing**(게임과 다르다)이고, 게임과 같은 framing 은 `world_frame.png`(캔버스 없음)에만 있다. 그래서 T28 비율 대조가 3D 자리를 원작과 못 맞댄다.
- 왜 어렵나(T54 가 벽 셋을 받았다 · 되풀이하지 말 것): 촬영은 카메라 **하나**로 세계와 캔버스를 같이 그리고 `ScreenSpaceCamera` 캔버스는 그 카메라 프러스텀에 맞춰 놓인다 → ⓐ `Camera.rect` 를 띠로 좁히면 **URP 가 세계를 안 그린다**(런 116 · 결정 176) ⓑ 두 카메라로 갈라 rect 로 나눠도 같은 갈래 ⓒ 투영만 걸면 **UI 가 통째로 밀린다**(런 134 · 결정 194) ⓓ `CopyFrom` 은 커스텀 투영까지 복사한다(결정 198).
- 남은 길(권장): 세계와 UI 를 **각각 RT 에 찍어 알파로 합성**한다 — 세계 RT(게임 절두체 · 캔버스 없음) + UI RT(기본 투영 · 배경 알파 0 · `ARGB32`) → `Texture2D` 에서 픽셀 합성 → 지금 이름(`screen_<이름>.png`)으로 저장. URP 의 겹치기·클리어 규칙을 안 건드린다. 540×960 × 43장이라 합성 비용은 재 보고 필요하면 2픽셀 간격 대신 전량으로.
- 판정: `screen_main.png` 한 장에 **상단바·시트가 제자리**이고 **영웅·적이 화면 위 1/3**에 함께 보인다(워커가 열어 본 기록) · `world_frame.png` 와 3D 자리가 ±2%p.
- **실측 보탬(2026-09-13 · T28 10회차 · 워커 M · 런 139 ↔ 런 128 같은 화면 `screen_dungeons.png` 밴드 대조)**: «UI 는 옳다» 가 아니다 — **UI 자체가 세로로 밀려 위가 잘린다**. 런 128(정상 · 그 화면 9.1점): 제목 밴드 y **3.0%** · 배너 넷 y 13.2/27.1/40.9/54.7% · ◀ y 85.4%. 런 139(지금 · 2.4점): 제목 밴드가 **없다**(위로 잘려 나갔다) · 배너 셋만 y 7.9/21.8/35.5% · ◀ y **66.2%** · 탭바 y **72.5%** · 그 아래 ~27%H 는 3D 흙바닥. **밴드 높이는 12.6~12.7% 로 둘이 같다** — 크기가 아니라 **자리**가 어긋난 것이다(위로 ~13~17%H 이동 + 앱 상자가 아래 ~21%H 를 안 덮음). 그러니 합성 전에 «앱 상자 사각형이 촬영 RT 를 꽉 채우는가» 를 먼저 맞춰야 한다 — `ui_score --score` 의 «앱 상자가 그림을 안 채운다» 경고와 밴드 y 두 줄이 그 자다.
- 범위: `Assets/Tests/PlayMode/UiShotsTests.cs`(Capture 합성) · 필요하면 `SafeAreaTests.cs`.
- **✅ 결론(2026-09-13 · 런 147 · 워커 O)**: 세계(게임 절두체 · UI 층 제외)와 UI(기본 투영 · UI 층만 · 검정/흰 두 배경)를 따로 찍어 채널 매트로 합성했다(`UiShotsTests.Capture` · `Composite`/`Over` · 결정 201). 런 147 `screen_main.png`: 상단바(y 0~60 · 색 28,34,38)·장비 시트·탭바(바닥 · 13,13,28)가 제자리 · 영웅·적이 세계 띠 안 위 1/3(초록 HP 바 y 290 = **30.2%**) · `world_frame.png` 의 같은 바 y 292 = 30.4% → **0.2%p**(판정 ±2%p). `screen_dungeons.png` 제목 밴드가 맨 위(런 139 의 «위로 잘림» 없음). PlayMode 93/93 · 촬영 실패 경고 0 · 총 383초(종전 373초 · 렌더 3회/장의 비용 ≈ 10초). 판정 그림이 다시 한 장이다 — T28 은 `screen_*.png` 로 3D 자리까지 잰다 · 픽셀 게이트는 T84.

### T84 ✅ — 게이트: 촬영 테스트가 **제가 찍은 픽셀을 한 번도 안 본다** — 프레임 회귀 세 번이 전부 초록으로 지나갔다 (검증·게이트 · **T54·T83 뒤**(`UiShots*` 같은 파일) · 검수 Q 등재)
- 왜: `UiShotsTests` 가 단언하는 것은 셋뿐이다(그 파일 주석 31행) — ⓐ 30 화면이 열렸는가 ⓑ 글자가 T18 하한 이상인가 ⓒ 콘솔 빨강 0. **찍은 RenderTexture 의 픽셀은 아무도 안 본다.** 그래서 캡처가 통째로 깨진 런이 세 번 연속 «유니티 잡 초록» 으로 지나갔다:
  - 런 102~103 — 앱 상자가 540×452 로 눌려 34장 전부 위가 잘렸다(워커 B 가 눈으로 잡았다).
  - 런 108 — T28 6회차 평균 **1.72/10**(«UI 가 망가진 게 아니라 촬영 프레임» 이었다).
  - **런 137~139 — 상단바가 통째로 사라지고 UI 가 위로 밀렸다**(아래). 런 139 는 EditMode **513/513** · PlayMode **90/90** · 빨강 0 인데도 그렇다.
- 실측(2026-09-13 04:0x · 검수 Q · 런 139 `10be47d`): `screen_main.png` 의 y=5~70 중앙 픽셀이 전부 하늘색 `(173,226,201)` — 런 127 은 같은 자리가 상단바 `(28,34,38)` 였다. UI 가 위로 밀려 화면 **아래 184px 이 3D 흙바닥**이다(`screen_main`·`screen_dungeons`·`screen_league` 셋 다 184px · 런 127 은 0px). `ui_score --score` 평균 **4.37 → 2.75**, 내려간 화면 **20개**. **증상의 임자는 T54**(워커 J 가 런 137 로 보고함에 올렸다) · 프레임을 두 장으로 가르는 일은 **T83** — 이 작업은 증상도 구조도 아닌 **자**다.
- **실측 보탬(2026-09-13 04:5x · 워커 H · 런 141 `8dbc60f` · lock 안 잡음 · 파일 0줄)**: 그 증상은 **지금 재현되지 않는다** — `screen_main.png` 의 중앙 픽셀이 y=5·20·40·60 에서 전부 상단바 `(28,34,38)` 이고 y=70 부터 하늘 `(173,226,201)` 이다(런 127 과 같은 꼴). 상단바·프로필 카드·재화 알약·채팅 두 줄이 제자리에 보인다. **그런데 이것이 이 작업을 접을 이유는 아니다** — 런 137~139 에서 왔다가 141 에서 사라진 그 회귀를 EditMode 513/513 · PlayMode 90/90 초록이 **한 번도 못 잡았다**는 것이 이 게이트가 필요한 바로 그 이유다. 다음 임자는 «상단바가 없다» 를 재현하려 들지 말고 **픽셀 단언 자체**를 세워 회귀가 다시 왔을 때 잡히게 하면 된다. (같은 PNG 에 남아 있는 진짜 결함은 3D 가 비어 있는 것 = T54 · 전투력 `⚔ 45` = T77 · 글자 두부 = T53 이다.)
- 무엇을 한다(찍은 뒤 그 자리에서 `RenderTexture` 를 그대로 읽는다 · 파일로 돌지 않는다):
  - ⓐ **상단바가 있다** — 앱 상자 맨 위 띠(높이 = 카탈로그 `topbar`)의 픽셀이 3D 배경색이 아니다.
  - ⓑ **UI 가 앱 상자 바닥까지 닿는다** — 맨 아래 띠(탭바 높이)가 3D 지면색이 아니다(지금 깨진 자리).
  - ⓒ **앱 상자를 채운다** — 내용 있는 픽셀의 세로 범위가 판의 0.98 이상(`ui_score` 의 «채움» 과 같은 뜻인데 그 자는 런이 끝난 **뒤** 사람이 돌린다 — 이 칸은 CI 안에서 막는다).
  - 실패 메시지에 «어느 화면 · 어느 띠 · 읽은 색» 을 적어 `playmode-red.txt` 로 바로 읽히게 한다(T46 꼴).
- **`-nographics` 주의**: 그림은 «있으면 좋은 것» 이라는 지금 주석대로, RT 를 못 얻는 판에서는 세 칸을 조용히 건너뛴다(지금 CI 러너는 실제로 찍는다 — `meta.json` 의 `shots` 가 0 이 아니다).
- 판정: 세 칸이 런 127 캡처(성한 것)로는 초록, 런 139 캡처(깨진 것)로는 빨강 — 둘 다 `screens` 에 있으니 그 PNG 로 자를 먼저 검산할 수 있다. 그 뒤 CI 유니티 잡 초록.
- 범위: `Assets/Tests/PlayMode/UiShotsTests.cs`(단언 세 칸) · `Assets/Tests/PlayMode/PlayLog.cs`(메시지 꼴 · 필요하면).
- **✅ 결론(2026-09-13 · 런 150 · 워커 O)**: `UiShotsTests.Capture` 가 T83 매트(검정 위·흰 위 두 장의 차 = UI 덮임)로 세 칸을 그 자리에서 본다 — 상단 띠(`topbar_h`)·바닥 띠(`tabbar_top` 아래) 평균 덮임 ≥ 60% · UI 행 세로 채움 ≥ 98%(`PixelGate` · 결정 206). 노치 줄은 제외(safeArea 위 띠가 비는 것이 정상). 런 150 실측: 가린 33장 **전부 상단 100% · 바닥 100% · 채움 100%** · PlayMode 95/95 · 자취는 `uishots.txt` 의 «픽셀 상단·바닥·채움» 줄. 런 139 꼴(UI 가 위로 밀려 아래 184px 이 흙바닥 · 채움 ≈81%)은 이제 ⓑ·ⓒ 로 빨강이 된다.

### T85 ✅ — 설정·프로필 팝업에 원작에 없는 딤이 깔려 배경이 검다 (Game·UI · T78 뒤 · T28 11회차가 눈으로 잡음)
- 실측(2026-09-13 · T28 11회차 · 워커 M): `screen_settings.png` 점수가 **5.9 → 2.4**(런 128 → 런 141 · 회귀 탐지가 «내려간 화면» 으로 먼저 찍었다). PNG 를 열어 보니 팝업 자체는 그대로인데 **뒤 배경이 거의 검다**.
- 정본 실측(`shot-042744`): 설정 팝업 뒤로 **나무·지면·상단바(782k·62)·스테이지 라벨 «어려움 4-1»·탭바가 또렷이 보인다** — 원작은 이 팝업에 딤을 거의 안 깐다. 반대로 대장간 목록(`shot-042905`)·펫 업그레이드(`shot-042503`)는 배경이 어둡다 — **팝업마다 다르다**.
- 원인 짐작(잡는 사람이 확인할 것): T78(팝업 셋에 딤 추가 · ✅)이 넣은 딤이 공용 갈래(`UiKit`/`PetSkillModal` 딤 · 결정 191 «정본 .5 를 브라우저와 같은 밝기로»)를 타고 설정·프로필까지 걸린 것으로 보인다. 정본은 팝업별로 `.modal-backdrop` 유무가 갈린다.
- 무엇을 한다: 정본 `style.css`·`ui.js` 에서 **팝업마다 딤이 있는지 없는지**를 표로 뽑아(설정·프로필은 없음 · 대장간 목록/상세·펫 업그레이드·리그 보상은 있음) 그대로 따른다. 딤 밝기는 결정 191 값을 그대로 둔다.
- 판정: `ui_score --score --only settings profile` 이 **런 128 수준(5.9 이상)** 으로 돌아오고 + 워커가 `screen_settings.png` 를 열어 «뒤 배경이 보인다» 를 확인 + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/SettingsPopup*`·`ProfilePopup*`(딤 유무 한 줄씩) · 딤을 공용으로 깐 자리(`Ui/UiKit.cs` 또는 `Ui/PetSkillModal.cs` 중 그 갈래) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.
- ✅ 결론(2026-09-13 · 워커 R): **전제가 틀렸다** — 정본 `style.css` 1721 은 모든 `.modal` 에 .5 딤(설정·프로필 포함 · 주인 지시)이고 원작 샷 042744 만 낡아 딤이 없다 → 팝업 딤은 그대로. 검은 배경의 정체는 **촬영 중 영웅 사망 → T39 `BattleOverlay` 암전 덮개가 상단바까지 덮은 것**(34장 픽셀 순서 실측). 덮개 띠를 정본 `#game-area`(상단바 아래~시트 위)로 좁혔다 · 영웅이 죽는 원인은 T77 · 기록은 PROGRESS «T85 완료 기록».

### T86 ✅ — WebGL 배포물이 부팅에서 죽는다: `BattleScene.Boot` 가 제가 읽은 GameData 를 `Attach` 에 안 넘긴다(런 140 첫 WebGL 스모크 «GameData 가 없다») + 스모크가 닫기 부산물을 빨강으로 센다 (배포·검증 · T26·T32 뒤 · 워커 N 등재)
- 실측(2026-09-13 · schedule **런 140** · 80c8552 · 첫 실제 WebGL 굽기): unity-builder 27분 초록 → `webgl_smoke` 빨강 2 — `InvalidOperationException: BattleScene.Attach: GameData 가 없다` · `요청 실패: StreamingAssets/data/tech.json (net::ERR_ABORTED)`. `unity-ready` 는 왔다(앱 상자 540×960).
- 뿌리: `BattleScene.Boot`(155~178행)는 `SaveIo.Data` 가 null 이면 데이터를 제가 읽지만 `Attach(MakeBattle(data, …))` 에 **data·defs 를 안 넘기고**, `Attach` 는 `data ?? SaveIo.Data` 로 폴백한다. WebGL·Android 는 SaveIo 가 `UnityWebRequest` 로 늦게 읽어 그 순간 null → throw. 에디터·CI 는 동기 읽기라 안 보인다. tech.json 실패는 스모크가 `browser.close()` 순간의 ERR_ABORTED 를 센 것(닫기 부산물).
- 무엇을 한다: ⓐ `Attach(MakeBattle(...), data, defs)` 한 줄 ⓑ `webgl_smoke.js` 에 `closing` 플래그 — 닫기 뒤 `requestfailed` 는 안 센다. 에디터 테스트 훅은 안 더한다(결정 202).
- 판정: `workflow_dispatch` `build: true` 런의 «WebGL 배포 스모크» 초록(콘솔 빨강 0 · unity-ready) + gh-pages 배포 스텝이 돈다 · Android 잡 결과도 같은 런에서 읽는다. tech.json 이 그래도 실패하면 별도 번호.
- 범위: `Assets/Scripts/Game/Battle/BattleScene.cs`(Boot 의 Attach 인자) · `tools/webgl_smoke.js`(닫기 가드).
- 🔄 2026-09-13 워커 N(sess-0524-8791): 두 고침 push(8ce6b15) → 런 175 스모크에서 GameData 오류·tech.json 실패 사라짐 · 남은 `ERR_ABORTED`(압축 폴백의 자기 취소)는 2회차에 노랑으로 · 판정은 다음 굽기 런.
- ✅ 결론(2026-09-13 · 워커 N · sess-0524-8791): 수동 build 런 **223**(a43f87a)에서 «WebGL 배포 스모크» 초록(unity-ready · 11.8초 · 빨강 0 · 노랑 4 = `.unityweb` ERR_ABORTED 폴백 + 글꼴 두부 2) · gh-pages 배포 스텝이 처음으로 success(브랜치 `gh-pages` 2a70c55) · Android 초록(12분). WebGL 부팅 «GameData 가 없다» 는 런 175·179·223 세 번 연속 안 나왔다. 굽기 런은 main 의 PlayMode 가 초록일 때만 돈다(`needs: unity-test`) — 남의 빨강이 이어지면 dispatch 해도 skip 이니 screens 의 `playmode-red.txt` 빨강 0 을 먼저 본다.

### T87 — 대장간·제작 연출 전수: 원작 CSS 키프레임 22종이 유니티에 하나도 없다 (Game · T19·T30 뒤 · **주인 지시**)
- 주인(2026-09-13): «대장간 뽑을 때 애니메이션도 빠져 있네. 그런 것도 같게». 정본 `web/css/style.css` 에 제작 계열 키프레임이 있는데 유니티에는 대응이 없다 —
  - 모루: `anvilbump`(누를 때 모루가 튄다) · `anvilbillet`·`anvilbillethot`·`anvilbilletglow`·`anvilbilletcool`(쇳덩이가 올라가 달궈지고 식는다) · `sheetshake`(타격에 시트가 흔들린다) · 한 사이클 **0.72초**(`ui.js` 1019 주석).
  - 오토 포지: `afswing`(망치 스윙) · `afring` · `afbloom` · `afflash` · `afheat` · `afshadow` · `afstar` · `afcore` · `afspark` · `afscale` · `afsmoke` · `afexit`.
  - 결과 카드: `crpop` · `crring`(시대색 링이 퍼진다) · `crsheen`(광택 쓸림) · `cbpop`·`cbfade`(x10 묶음) · `adcpop`(자동 드랍 카드가 튀었다 코인으로 터진다) · `cardpop` · `shinesweep` · `newpulse`.
- 옮기는 법: 각 키프레임의 **시간·이징·변위·색·불투명도를 CSS 에서 그대로 읽어** DOTween(주인 에셋)이나 코드 보간으로. 눈대중 금지. 소리는 T30 표면(`AnvilHit`·`Craft`·`CraftReveal`)을 그 타이밍에 건다.
- 판정: PlayMode 로 «누른 뒤 t 초의 값이 CSS 곡선과 ±10%» 를 키프레임마다 + **촬영**(제작 순간 컷 `screen_craft-*.png` · 워커가 열어 본다 · §1) + 원작 `shot-craft-reveal`·`shot-anvil-*` 눈 대조.
- 범위: `Assets/Scripts/Game/Ui/Forge*` · `Ui/Anvil*` · `Ui/CraftFx*` · `Assets/Tests/PlayMode/ForgeUiTests.cs` · `Assets/Forge/catalog.json`(연출 수치 칸) · `Assets/Scripts/Core/CraftFx/*` · `Assets/Tests/EditMode/CraftFxTests.cs` · `Assets/Forge/Resources/UiScreen.shader`(정본 `mix-blend-mode: screen` 층이 쓰는 UI 재질 · 23회차).
- 옮기며 밟은 함정 셋(T87 1~5회차 실측 · 정본 주석이 먼저 적어 둔 것들):
  - ⓐ **반동은 «그림» 에만** — 정본 `style.css` 1167: 버튼(`.anvil-btn`)에 걸면 타격 오버레이가 그 자식이라 «망치가 모루의 반동을 그대로 타고 내려간다»(상대변위 0). 클론도 러너를 버튼에 물려 4회차까지 값이 0 이었다.
  - ⓑ **축은 `transform-origin: 50% 92%`**(받침 접지면 · `transform-box: view-box` 라 viewBox 132×86 기준). 유니티는 `localScale` 이 **피벗**을 축으로 도니 피벗을 그 점에 옮긴다.
  - ⓒ **키프레임의 px 는 절대 CSS px** 이라 `rem` 처럼 앱 크기를 안 따라간다 — 기준 캔버스에 바를 때 촬영 배율(앱 폭 499px → 1080px = ×2.164 · 카탈로그 `anvil_fx_px`)로 곱한다. 안 곱하면 정본 깊이의 절반이다(결정 222).
  - ⓓ **각진 SVG 면은 둥근 사각으로 옮길 수 없다** — 빌릿은 8각 챔퍼 각봉이어야 하고(정본 주석: 둥근 알약은 UI 어휘) 몸통은 4-stop 그라디언트다. `Ui/CraftFxPoly.cs` 가 그 폴리곤을 **구워 스프라이트로** 내준다(짝홀 규칙 + 3×3 초과표본 · 그라디언트도 같이 굽는다 · 불투명도는 `Image.color.a`). 오토포지 망치·불티·링도 같은 자로(결정 226).
  - ⓔ **«초록» 은 «칠해졌다» 가 아니다** — 6회차의 정점 메시(`Graphic` 상속) 겹은 PlayMode 101/101 초록·콘솔 빨강 0 인데 화면 픽셀 변화가 **0** 이었다(런 173 실측). 새로 그리는 것을 넣는 회차는 값 단언과 **픽셀 단언**(`ForgeUiTests.쇳덩이가_화면에_실제로_칠해진다` 꼴: 카메라 사본 → RT → `ReadPixels`)을 **같이** 세운다.
  - ⓕ **길이 단위가 두 갈래다** — HTML 원소(`.anvil-svg`·`#equip-sheet`)의 transform 은 **절대 CSS px**(촬영 배율 `anvil_fx_px` ×2.164로 환산)이고, SVG 자식(`.af-hammer`·`.anv-billet`)의 transform 은 **viewBox 사용자 단위**(모루 그림 132×86 기준 · 화면 px 는 `u` 를 곱한다)다. 섞으면 망치가 화면 밖으로 날아간다.
  - ⓖ **픽셀 자도 «어디를 재는가» 가 틀리면 속는다** — 9회차 망치는 그림 원점이 viewBox 한가운데로 밀려 받침 쪽에 그려졌는데, 머리 칸 색만 세던 자가 **남의 회색**(해머 카운터 아이콘)을 세어 초록이었다. 런 191 의 `screen_craft-strike.png` 를 눈으로 보고 잡았다 → 새 그림에는 **자리(값) + 채움(픽셀 비율)** 을 같이 건다(14회차).
  - ⓗ **분출물(불티·흑피)의 회전은 «키프레임 안» 에 있어야 한다**(20·21회차 · 정본 주석이 먼저 적어 둔 것) — 정본은 회전을 `transform="rotate()"` 프레젠테이션 속성으로 줬다가 같은 요소의 `afspark` 가 `transform` 을 애니메이션해 **연출이 시작되는 순간 회전이 증발**했고, 불티가 전부 수평 막대로 정렬돼 «상판 뒷변의 재봉선» 이 됐다(비평가 2인이 같은 그림을 지적). 그래서 이동 벡터도 **회전 프레임 기준**(`u = d + g·sin a` · `v = g·cos a`)으로 풀어 내려 준다 — 그 분해 덕에 중간 키 `u·0.55 / v·0.30` 이 정확한 포물선이고, 전역 좌표로 되돌리면 `dx = d·cos a` · `dy = d·sin a + g` 다(클론은 이 항등식을 EditMode 가 지킨다).
  - ⓘ **«함께 흔들리는 것» 은 상대 좌표로 재라**(20회차 · 런 202·205·209 의 같은 0.59px 빨강) — 시트 흔들림(`sheetshake`)은 오버레이 전체가 타는 것이 정본이다. 화면(월드) 좌표로 «축이 안 움직인다» 를 재면 그 흔들림이 섞여 영원히 빨갛다. 축·상대변위 단언은 **부모 칸 좌표**(`parent.InverseTransformPoint`)에서 본다.
- **진행(2026-09-13 · 워커 G · sess-0544-755 · 1~26회차 · lock 반납함)**: `.anvil-fx` 오버레이 **13겹을 다 옮겼다** — 모루(`anvilbump`) · 시트(`sheetshake`) · 빌릿(`anvilbillet`·`hot`·`glow`·`cool`) · 망치(`afswing`·`afexit`) · 링 · 접지 그림자 · 코어 · 섬광 · 플래시 · 잔열 · 블룸 · 불티(`afspark`) · 흑피(`afscale`) · 연기(`afsmoke`). 순서(정본 SVG)·이징·클럭(1500ms)·스크린 합성(`Assets/Forge/Resources/UiScreen.shader`)까지 정본 그대로고 EditMode 20여 칸 + PlayMode 7 칸이 지킨다(런 234 초록).
- **남은 것 = 결과 카드 한 덩어리**(`crpop`·`crring`·`crsheen`·`cbpop`·`cbfade`·`adcpop`) — **누구든 `docs/claims/T87.lock` 을 새로 잡아 이어간다**. 길은 깔려 있다: 표는 `Core/CraftFx/CssTrack`+`CssEase`, 그림은 `Game/Ui/CraftFxPoly`(구운 스프라이트)·`Forge/UiScreen`(screen 합성), 러너는 `AnvilFx` 꼴. 시작 전에 위 함정 ⓐ~ⓘ 를 읽을 것.

### T88 ✅ — 백그라운드에서도 게임이 돈다 (Game+Core · T13 뒤 · **주인 지시**)
- 주인(2026-09-13): «백그라운드에서도 플레이 되게 해줘야 함». `Application.runInBackground` 가 코드·ProjectSettings 어디에도 없다(실측 grep 0).
- 할 것: ⓐ `Application.runInBackground = true`(`Bootstrap`) · `ProjectSettings.asset` `runInBackground: 1` ⓑ **폰은 그것만으로 안 된다** — OS 가 앱을 재우면 프레임이 멎으므로 `OnApplicationPause(true)` 에 잠든 시각을 저장하고, 깨어날 때 흐른 실시간만큼 **전투·대장간·부화·오프라인 수급을 절대시각으로 따라잡는다**(원작 `state.js` 가 웹 탭 전환에서 하던 것 · T13 의 절대시각 타이머 위에 «따라잡기» 한 갈래). 따라잡기는 100ms 틱을 N 번 도는 것이 아니라 **닫힌 식**으로 — 몇 시간이면 수십만 틱이다.
- 판정: EditMode(잠든→깨어난 시각을 주고 따라잡은 상태가 «실제로 그만큼 돈 상태» 와 같은가 · 30초·1시간·8시간·오프라인 캡 4시간 경계) + PlayMode(`OnApplicationPause` 흉내 → 콘솔 빨강 0 · 화면이 살아난다) + 「주인이 확인할 것: 폰에서 홈 → 30초 뒤 복귀 → 재화·웨이브가 그만큼 늘어 있다」.
- 범위: `Assets/Scripts/Game/Bootstrap.cs` · `Game/AppLifecycle.cs`(새 파일) · `Assets/Scripts/Core/Save/` · `ProjectSettings/ProjectSettings.asset` · `Assets/Tests/EditMode/CatchUpTests.cs` · `Assets/Tests/PlayMode/LifecycleTests.cs`.
- ✅ 결론(2026-09-13 · 워커 R): `runInBackground`(코드+ProjectSettings) · Core `Lifecycle`(5초 백그라운드 문턱 · 60초 팝업 · `ResumePlan`) · Game `AppLifecycle`(잠든 시각 · 벽시계 공백 감지 · 복귀 시 부팅과 같은 오프라인 팝업). **전투는 따라잡지 않는다** — 원작이 숨은 구간 틱을 버리고 오프라인 수급(닫힌 식 `offlineRewardFor`)으로 넘기며(이중 지급 방지), 대장간·부화·연구는 절대시각 `endsAt` 이라 깨어난 첫 틱에 끝난다. EditMode 6 · PlayMode 2 · 기록은 PROGRESS «T88 완료 기록» · 결정 204.

### T89 ✅ — 화면 문자열의 이모지 30종이 전부 두부(□)다 — 원작처럼 아이콘으로 갈아 끼운다 (Game · T31·T53 뒤 · **가장 먼저** · 실제 화면 실측)
- 실측(2026-09-13 06:1x · 런 148 `screen_main.png` 눈 확인 + 코드 전수): 화면에 나가는 문자열에 이모지 **30종**(🔨 🪙 💎 🔒 ⭐ ⚔ 🏆 💀 📜 🥚 🧪 🎟 🗝 📌 📍 💤 💾 🛡 🐴 🐾 🚨 🧍 ⏳ ⚒ ✎ ✓ ✕ ⓐ ⓑ VS16)가 그대로 들어 있고 **글꼴에 하나도 없다** — 원본 Noto Sans KR 에도 대부분 없다(✓ ⓐ ⓑ 만 있다). 화면의 «자동 □ OFF» 가 그 하나(🔄).
- **정본이 답을 준다**: `ui.js` 의 `TOAST_ICON` 표(1389~)가 이모지 → 코드 생성 아이콘 이름을 잇고(`'🪙': 'coin' · '💎': 'gem' · '🔨': 'hammer' · '⚔': 'tm_sword' · '🔒': 'lock' · '⭐': 'star' …), 바로 아래 함수가 **문구를 훑어 표에 있는 이모지를 전부 아이콘 노드로 바꾼다**(선두만이 아니다 — 주석에 «뒤에 붙는 재화 이모지가 그대로 남아 한 줄 안에서 섞인다» 는 실측이 있다).
- 할 것: ⓐ 그 표를 `catalog.json` 에 `toastIcons` 로 옮긴다(T2·T31 과 같은 «정본이 쥔다» 원칙 · 손으로 짓지 않는다) ⓑ `UiKit.Text` 가 문자열을 세울 때 표에 있는 이모지를 **TMP 인라인 스프라이트**(T31 아틀라스 · `<sprite name=…>`)로 치환한다 — 아이콘이 글자 높이에 맞고 색 틴트가 원작과 같아야 한다 ⓒ 표에 없는 순수 기호(▶ ▼ ★ ✓ ⓐ ⓑ)는 글꼴 서브셋에 넣는다(`docs/assets-map.md` 의 재생성 명령에 유니코드 구간 추가) ⓓ 어느 쪽도 아닌 것(🐴 🐾 🚨 🧍 ✎ ⏳ 등 디버그·개발 문자열)은 아이콘을 새로 그리지 말고 **한국어 낱말로 바꾼다**(원작에 없는 자리다).
- **덧(검수 Q 06:2x · 이모지와 갈래가 다르다 · 아이콘으로 못 바꾼다)**: 두부는 이모지만이 아니다 — **호환 자모**(U+3130~U+318F)도 서브셋 밖이라 □ 다. 런 147 `screen_chat.png` 일곱째 줄이 «보스 너무 세다 **□□**» 인데 정본 `chat.js` 원문은 «보스 너무 세다 **ㅋㅋ**» 다. 정본 전체에 **7종 26회**(`scene3d.js` 15 · `chat.js` 4 · `ui.js` 2 · `mobs-props.js` 2 · `techtree.js` 1 · `icongen.js` 1 · `style.css` 1 · 가장 잦은 것 ㄴ 8 · ㄱ 7 · ㄷ 5 · ㅠ 2 · ㅋ 2). 이것들은 글자지 아이콘이 아니므로 **서브셋을 다시 뽑을 때 U+AC00~D7A3 로 자르지 말고 자모 구간까지 담는다**(결정 203 의 «한글 1266자» 가 음절만이었다). 그리고 넓힌 단언은 **`HasCharacter(c, false, false)`(폴백 없이)** 로 물어야 한다 — 리눅스 CI·WebGL·안드로이드에는 OS 폴백이 없고 그 판이 판정 기준이다(지금 화면 검사는 `true, true` 라 폴백 있는 판에서 «있다» 로 세어 준다).
- 막이: `TextSizeGateTests` 에 «활성 라벨의 모든 문자가 글꼴에 있다(한글만이 아니라 **전부**)» 로 넓힌다 — 지금 단언은 U+AC00~D7A3 만 봐서 이모지 두부를 놓쳤다.
- 판정: 새 촬영 PNG 를 열어 □ 가 0 인 것을 눈으로 + 넓힌 단언 초록 + 원작 샷과 아이콘 자리 대조.
- 범위: `Assets/Scripts/Game/Ui/UiKit.cs` · `Ui/UiIcons.cs` · `Assets/Forge/catalog.json`(toastIcons) · 이모지를 쥔 화면 파일들 · `Assets/Fonts/NotoSansKR-Forge.ttf`(기호 구간 추가 시) · `Assets/Tests/PlayMode/TextSizeGateTests.cs` · `docs/assets-map.md`.
- ✅ 결론(2026-09-13 · 런 163·170 · 워커 E): ⓐ 정본 `TOAST_ICON` 33줄을 **추출기로** 뽑아 `StreamingAssets/data/ui-text.json` 에(손으로 안 짓는다 · `check_data_sync` 가 대조) ⓑ 정본 `paintIconText` 를 Core `IconText` 로(문구 전체 훑기 · U+FE0F · 뒤 공백 · 표에 없으면 글자 그대로 · EditMode 7) ⓒ `UiKit.IconTextRow` 가 «아이콘 칸 + 글자 칸» 한 줄로 세우고 `PopupLayer.Toast` 와 라벨 네 자리(리그 전투력·리그 점수·패스 배지·프로필 편집 · 각각 정본 줄 번호 확인)에 배선 ⓓ **새 두부를 막는 자** `tools/check_text_glyphs.py`(글꼴 cmap × 화면 문구 × 아이콘 표 · CI dotnet 잡 두 줄 · 고장 주입 확인). 판정: 런 170 유니티 잡 **전체 초록**(EditMode 534 · PlayMode 100 · `ToastIconTests` 3/3) + **PNG 를 열어 봤다** — `screen_league.png` 의 점수가 노란 별 아이콘(★107…)·전투력 줄이 `tm_sword` 아이콘 + 수 · `screen_profile.png` 의 편집 버튼 셋이 연필 아이콘. 남은 둘은 **넘긴다**: 글꼴에 없는 `⏱`·`⏹`(정본도 같은 글자 · 주인 이모지 폴백 글꼴 대기 · 보고함) · 남의 lock 이 쥔 라벨 다섯(**T97**).

### T90 ✅ — 한글이 들어온 뒤 드러난 «글자가 칸 밖으로 넘친다»: 스킬 화면의 버튼·레벨 라벨 (Game·UI · T53 뒤 · T28 12회차가 눈으로 잡음)
- 왜 이제 보이나: T53(한글 글꼴) 전에는 모든 한글이 두부(□)라 글자 폭이 가짜였다. 글꼴이 들어오자 **진짜 폭**으로 그려지면서 칸을 넘치는 자리가 드러났다 — 열두 회차 만에 처음 잴 수 있는 종류다.
- 실측(2026-09-13 · T28 12회차 · 워커 M · 런 148 `screen_skills.png` 5.8/10 ↔ 원작 `shot-042340`):
  - ⓐ **«모두 업그레이드» 글자가 파란 버튼 좌우로 삐져나온다**(버튼 폭 < 글자 폭). 원작은 두 버튼(«모두 업그레이드»·«빠른 장착») 모두 글자가 안에 들어간다.
  - ⓑ **스킬 아이콘의 «Lv.NN» 라벨이 아이콘 아래 테두리에 걸려 아래가 잘린다**(원작은 원 안 중앙에 또렷하다) · 잠긴 스킬의 «장착됨» 배지도 아이콘을 덮는다.
  - ⓒ 진행 pill(«0/3» 따위) 이 아이콘 행에서 원작보다 멀리 떨어져 있다(원작은 아이콘 바로 아래에 붙는다).
- 무엇을 한다: 정본 `style.css` 의 그 버튼(`.skill-actions button`)·레벨 배지(`.skill-lv`)·pill 규칙을 읽어 **버튼은 글자에 맞춰 늘어나거나 글자가 줄바꿈/축소되게**(원작 규칙 그대로) · 레벨 라벨은 원 안 중앙 · pill 간격은 정본 값. 글자 크기 하한(§1 · 보조 36)은 그대로 두고 **칸을 키우는 쪽**으로 맞춘다(원작 규칙이 그렇다).
- 판정: ~~`ui_score --score --only skills` 가 **런 141 수준(6.8) 이상**~~(두부 시절 기준선 · 결정 213) + 워커가 PNG 를 열어 «버튼 밖으로 넘친 글자 0 · Lv 라벨이 안 잘림 · 오브→게이지 ≈1%H» 확인 + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/Skill*` · `Assets/Forge/Resources/PetSkillUi.json`(버튼·배지 자리 키 — 스킬 화면의 표는 T20 이 여기 둔다 · catalog.json 은 안 쓴다) · `Assets/Tests/PlayMode/PetUiTests.cs`(스킬 시트 단언).
- 1회차(2026-09-13 · 워커 B): 버튼 폭 = max(원작 고정폭, 글자+패딩) · Lv 라벨 중심 72.9%(css 실측) · 별 줄은 별이 있을 때만(행 높이 auto) — 픽셀 실측·판정은 PROGRESS «T90 진행 기록».
- ✅ 결론(2026-09-13 · 런 155 · 워커 B): 버튼 폭 = max(원작 고정폭, 글자+패딩)(«모두 업그레이드» 32.6%W) · Lv 라벨 중심 72.9% · 별 줄은 별이 있을 때만 → 오브→게이지 1.3%H(런 149 4.1) · 행 피치 10.1%H(12.9) · `PetUiTests` 6/6 · ui_score 5.8 → 6.2. 기록은 PROGRESS «T90 완료 기록».

### T91 ✅ — 메인 화면 채팅 미리보기 줄이 **비었다**(정본은 두 줄) (Game·UI · T22·T63 뒤 · 임자 없음 · 검수 Q 등재)
- 실측(2026-09-13 06:0x · 검수 Q · 런 147 `screen_main.png` 을 Read 로 열고 잘라 본 것): 탭바 위 회색 띠에 **말풍선 아이콘과 «99» 배지뿐이고 글자가 한 자도 없다**(띠 높이도 ~20px 로 줄었다). **런 127 의 같은 자리에는 «Yumi» + «anyone want to trade tickets?» 두 줄이 있었다** — 그 사이 회귀다. 채팅 자체는 멀쩡하다(`screen_chat.png` 로그 열두 줄 정상 · 한글도 나온다).
- 정본(`ref/screens/shot-042120.png` 하단): 회색 띠에 **두 줄** — «MilkMessiah이(가) 전투를 공유했습니다!»(시스템 줄) / «MilkMessiah: Ligma»(마지막 발화) · 흰 굵은 글씨 · 빨간 «99» 배지는 말풍선 **왼쪽 위**. 클론은 배지가 오른쪽 위라 띠 윗변에 잘린다.
- 할 일: 원작 `ui.js` 의 미리보기(시스템 줄 + 마지막 발화 두 줄)를 다시 잇고, 띠 높이·글자 색·배지 자리를 정본 실측대로 `catalog.json` 에 둔다(코드에 숫자 금지 · §1).
- 판정: PlayMode 단언(미리보기 라벨 둘이 비어 있지 않고 마지막 메시지를 담는다 — T71 이 손댄 «두 줄» 단언을 되살린다) + 촬영 PNG 를 열어 두 줄이 보이는 기록(§1) + `ui_score` 의 `main` 점수가 안 내려간다.
- 범위: `Assets/Scripts/Game/Ui/Hud.cs`(미리보기 자리) · `Ui/Chat*` · `Assets/Forge/catalog.json` · `Assets/Tests/PlayMode/ShopUiTests.cs`.

### T92 ✅ — 자: 같은 작업의 두 줄이 **«범위» 칸만 다를 때** `check_task_rows` 가 조용히 지나간다 — `check_claim_scope` 가 어느 줄을 읽느냐로 답이 갈린다 (검증·자 · 뒤 순서 없음 · 워커 K 등재)
- 실측(2026-09-13 06:33 · 워커 K): PROGRESS 379~382행에 **T90·T91 이 각각 두 줄**이다. 두 줄 다 «🔄 진행 · 같은 SID» 이고 **다른 것은 «범위» 칸뿐**이다 — T90 의 한 줄은 `Assets/Forge/catalog.json` 을, 다른 줄은 `Assets/Forge/Resources/PetSkillUi.json` 을 적었다(rebase 가 양쪽을 다 살린 꼴).
- 왜 나쁜가: `check_task_rows` 의 중복 갈래는 «한 줄은 ⬜ · 다른 줄은 🔄/✅» 일 때만 실패한다. **상태가 같으면 «전부 접혀 있거나 상태가 같다» 로 초록**이다(지금 그 문구가 찍힌다). 그런데 `check_claim_scope` 는 그 작업의 «범위» 칸을 읽어 «lock 이 범위 밖 파일을 쥐었는가» 를 판정한다 — 두 줄의 범위가 다르면 **어느 줄을 읽느냐로 답이 갈린다**(한 줄만 읽으면 다른 줄에만 적힌 파일이 «범위 밖» 오탐, 또는 진짜 범위 밖이 묻힌다). 규약 «범위에 없는 파일을 열게 되면 표를 먼저 고친다» 가 두 줄에서는 성립하지 않는다.
- 무엇을 한다: `check_task_rows` 에 갈래 **ⓗ** — 같은 ID 의 **접히지 않은** 줄이 둘 이상이고 그 줄들의 «범위» 칸이 **서로 다르면 실패**(rc 1). 고치는 법을 출력에 적는다: 두 범위를 **한 줄로 합치고** 남은 줄은 기존 규약대로 `✂ 중복 행 — 살아 있는 기록은 N행이다` 로 접는다(지우지 않는다). 자기 검사에 두 칸(«범위가 같은 중복은 조용하다» · «다르면 rc 1»).
- ⚠ 표의 지금 중복(T90·T91)은 **임자의 lock 이 살아 있다** — 이 작업은 **자만 세우고 그 두 줄은 안 건드린다**(README ⓑ). 자가 서면 그 임자가 제 회차에 한 줄로 합친다.
- 판정: `check_task_rows --self-test` 초록 · 지금 표에 대고 돌리면 **T90·T91 을 이름으로 찍고 rc 1**.
- 범위: `tools/check_task_rows.py`.

### T93 ✅ — 대장간 목록·상세 팝업의 딤이 상단바·탭바를 안 덮는다: 원작은 팝업이 뜨면 화면 전체가 어두워진다 (Game·UI · T78·T85 뒤 · T28 13회차가 눈으로+자로 잡음)
- 실측(2026-09-13 · T28 13회차 · 워커 M · 런 155 `screen_forge-detail.png` **1.1/10** — 30화면 중 꼴찌):
  - **자**: 원작 판독표(`shot-042931`)의 **첫 밴드가 y 16.6%** 인데(그 위는 통째로 어두워 아무 블록도 안 잡힌다) 클론은 **첫 밴드가 y 0.9%** 다 — 상단바(용사·⚔12.2m·🪙27.1m·💎8.15k)가 **환하게** 잡힌다. 아래 탭바(PVP·던전·소환·퀘스트·상점)도 같다.
  - **눈**: 원작은 카드 밖이 거의 검고 목록 행이 옆으로 어렴풋이 비치는데, 클론은 상단바·탭바·채팅줄이 **원래 밝기 그대로**다.
- 어디서 왔나: T78(팝업 셋에 딤 추가)이 넣은 딤을 T85(설정·프로필은 원작에 딤이 없다)가 걷으면서 **대장간 계열까지 같이 걷힌 것**으로 보인다 — 같은 회차에 `forge-detail` 이 2.2 → 1.1 로 떨어졌다(런 141 → 148).
- 규칙은 하나다(정본 실측): **팝업마다 갈린다** — `settings`·`profile` 은 딤 없음(`shot-042744` 배경이 또렷), `forge-list`·`forge-detail`·`autoforge`·`pet-upgrade`·`league-rewards` 는 **화면 전체 딤**(상단바·탭바 포함).
- 무엇을 한다: 정본 `style.css`·`ui.js` 에서 팝업별 `.modal-backdrop` 유무를 표로 뽑아(그 표를 `catalog.json` 이나 코드 한 곳에 두고) 대장간 계열에 **앱 상자 전체를 덮는 딤**을 되살린다. 딤 밝기는 결정 191 값 그대로.
- 판정: `ui_score --score --only forge-detail forge-list autoforge` 가 **런 141 수준(2.2·4.0·4.3) 이상** + 워커가 PNG 를 열어 «상단바·탭바가 어둡다» 확인 + `--read` 로 첫 밴드 y 가 **10% 아래에서 안 잡힌다** + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/Forge*`(딤 갈래) · 딤 공용 자리(`Ui/UiKit.cs` 또는 `Ui/PetSkillModal.cs`) · `Assets/Forge/catalog.json`(팝업별 딤 표) · `Assets/Tests/PlayMode/ForgeUiTests.cs`.
- ✅ 결론(2026-09-13 · 워커 R · 코드 0줄): **전제가 틀렸다.** T85 는 팝업 딤을 안 건드렸고(런 141 의 어둠 = 사망 암전 덮개), 런 155 대장간 팝업은 상단바 ×0.66·탭바 ×0.64(한 겹)/×0.4(두 겹)로 **덮여 있다** — 정본 `.modal` .5(주인 지시 «투명도 50%») 그대로. 원작 샷 042905·042931·043117 은 .988 시절이라 «첫 밴드 y ≥ 10%» 는 정본 .5 로는 나오지 않는다(T28 «낡은 샷» 여섯째). 남은 진짜 차이(선형 α 환산 · 결정 191)는 **T94**. 기록은 PROGRESS «T93 완료 기록».

### T94 ✅ — 팝업 공용 딤도 브라우저 밝기로: `Popups.Show` 에 `UiKit.PerceivedDim`(결정 191) + 딤 색 단언 둘을 지각 α 로 (Game·UI·검증 · **T87 뒤**(`ForgeUiTests.cs` 같은 파일) · T93 등재)
- 실측(2026-09-13 · T93 · 런 155): 팝업 한 겹 아래 상단바가 `main` 의 ×0.66 — 프로젝트가 선형 색 공간이라 `modal_dim` α .5 가 브라우저의 .5(×0.5)보다 밝다(결정 191 이 `PetSkillModal` 에서 잰 것과 같은 자리). `Popups.Show`(`Ui/Popups.cs` 80행) 는 `UiKit.Panel(p.Root, "dim", dimKey)` 그대로 · `PetSkillModal` 만 `UiKit.PerceivedDim` 을 거친다.
- 무엇을 한다: ⓐ `Popups.Show` 의 딤에 `dim.color = UiKit.PerceivedDim(dim.color)` 한 줄(표값은 그대로 · `modal_dim_deep` 도 같은 환산) ⓑ T78 `ForgeUiTests.AssertCovers` 402행 `Assert.AreEqual(UiKit.C("modal_dim"), dim.color, …)` → 지각 α 비교 ⓒ T85 `UiSmokeTests` 337행 같은 갱신. 그때 `DungeonPopups.cs` 37행(raw `modal_dim`)·`ForgeCraftPopup.cs` 215~216(`new Color(0,0,0,0.42f)` 리터럴 · §1 위반)도 같은 길로.
- 판정: PlayMode 세 픽스처 초록 + 다음 촬영에서 팝업 아래 상단바가 `main` 의 ×0.5 안팎(픽셀) + 콘솔 빨강 0. `ui_score` 점수는 원작 샷이 .988 이라 이것으로는 크게 안 오른다(T28 메모).
- 범위: `Assets/Scripts/Game/Ui/Popups.cs`(Show 한 줄) · `Assets/Tests/PlayMode/ForgeUiTests.cs`(AssertCovers 한 줄) · `Assets/Tests/PlayMode/UiSmokeTests.cs`(단언 한 줄) · (같이 열면) `Ui/DungeonPopups.cs` · `Ui/ForgeCraftPopup.cs`.

- ✅ 결론(2026-09-14 · 워커 U · sess-0159-21879 · 결정 297·303): `Popups.Show`·`DungeonPopups.Overlay` 에 `UiKit.PerceivedDim` 한 줄씩(`modal_dim_deep` 도 같은 환산) + 단언 둘을 지각값으로 — 런 292(52ccec9) PlayMode `ForgeUiTests` 19/19 · `UiSmokeTests` 9/9 · `PetUiTests` 6/6(빨강 1 은 T118 `EquipSwapTests` · 워커 P) · 같은 런의 `screen_forge-list.png` 에서 딤 아래 밝은 띠(y≈8% · main 200)가 **99 = ×0.50**, 두 겹 `forge-detail` 46 = ×0.23(≈.25), 어두운 상단바(main 33)는 10.7 = ×0.32 — sRGB 발가락 구간의 산술값(결정 303 · 결정 191 의 환산은 밝은·중간 바탕에서 브라우저와 같고 아주 어두운 바탕에서만 조금 더 어둡다). `ForgeCraftPopup` 배치 딤은 `CraftCardFx` 가 매 프레임 덮어 T87 다음 회차 몫(결정 297).
### T95 ✅ — 장착된 스킬 오브에 **어둠 막이 없어** «장착됨» 배지와 «Lv.NN» 이 겹쳐 둘 다 안 읽힌다 (Game·UI · T20·T90 뒤 · 임자 없음 · 검수 Q 등재)
- 실측(2026-09-13 08:0x · 검수 Q · **런 158**(5a25142)의 `screen_skills.png` 를 Read 로 열고 픽셀로 잰 것): 스킬 격자 첫 행에서 장착된 오브 셋(1~3열)의 «장착됨» 흰 배지와 그 아래 «Lv.20/23/26» 이 **서로 겹쳐** 흰 글자 둘이 밝은 오브 면 위에 포개진다 — 8배로 확대해야 글자가 갈린다. **오브 평균 밝기 98·101·105(장착) ↔ 107·113(비장착)** — 겨우 7% 어두우니 **어둠 막이 사실상 없다**.
- 정본이 정한 것(`css/style.css`): `.sk-orb.equipped::after` 가 오브 전체에 **`rgba(0,0,0,.58)` 어둠 막**(`inset:0` · `border-radius:50%` · `z-index:1`)을 덮고, `.sk-eqplate` 는 오브 **정중앙**(`left:50% · top:50% · translate(-50%,-50%)`)에 검정 타원(`#0b0c0e` · 앱 폭의 **14.92% × 2.82%**)으로 앉으며, `.sk-lv` 만 `z-index:2` 로 그 위에 남는다. 그래서 원작 `ref/screens/shot-042340.png` 셋째 줄의 장착 오브는 **뚜렷하게 어둡고** «장착됨» 이 한가운데에서 읽힌다. 마크업 자체는 클론이 맞다(정본 `ui.js:4279~4282` 도 `sk-eqplate` 와 `sk-lv` 를 **둘 다** 그린다) — 어긋난 것은 **어둠 막과 배지 자리**다.
- **T90(✅)이 놓친 자리**: T90 은 같은 화면의 «오브→게이지 간격·행 피치·버튼 폭» 을 픽셀로 쟀고 그 넷은 맞다. 배지와 Lv 가 **서로** 겹치는 것은 그 자에 없던 항목이다.
- 할 일: `.sk-orb.equipped` 어둠 막(불투명도 .58)과 `.sk-eqplate` 정중앙 배치·치수(앱 폭 비율)를 정본 실측대로 `catalog.json` 에 두고 스킬 격자가 그것을 쓴다(§1 — 코드에 숫자 금지). 전투 HUD 스킬 바(`.sk-lv` 는 `bottom:-.15rem` 의 **검정 알약** `#17181a`)도 같은 회차에 맞춘다 — 지금 클론은 알약 없이 오브 면에 글자를 얹는다.
- 판정: `screen_skills.png` 을 열어 장착 오브가 뚜렷이 어둡고 «장착됨» 이 한가운데에서 읽히는 기록(§1) + 픽셀 단언(타원 위 오브 면 밝기가 비장착의 **0.5배 이하**) + ~~`ui_score --only skills` 가 안 내려간다~~(원작 샷의 장착 오브가 css .58 보다 옅은 ×0.7~0.85 라 이 자리는 내려간다 · 결정 218) + CI 유니티 잡 초록(PetUiTests 6/6).
- 범위: `Assets/Scripts/Game/Ui/Skill*`(격자 오브·배지 · HUD 스킬 바는 `SkillBar.cs`) · `Assets/Forge/Resources/PetSkillUi.json`(스킬 표는 T20 이 여기 둔다 · catalog.json 은 안 쓴다) · `Assets/Tests/PlayMode/PetUiTests.cs`.
- 1회차(2026-09-13 · 워커 B · 결정 217): 막은 이미 있었다(타원 위 면 ×0.38) → 지각값으로 · 진짜 결함은 글자 하한으로 커진 타원과 Lv 의 겹침 → 장착 오브만 Lv 중심 = max(72.9%, 타원 아래끝+.36em) · HUD `.sk-lv` 검정 알약. 실측·판정은 PROGRESS «T95 진행 기록».
- ✅ 결론(2026-09-13 · 런 170 · 워커 B): 막은 이미 있었고(지각값으로 ×0.39) 진짜 결함은 `_h` 키로 54px 가 된 타원과 Lv 의 겹침 — 타원 39.6px 정중앙 · Lv 는 타원 아래끝에 잉크가 닿게 · HUD Lv 검정 알약 · `PetUiTests` 6/6 · ui_score skills 6.2 유지. 기록은 PROGRESS «T95 완료 기록».

### T96 ✅ — 임자 없는 빨강: «screens 브랜치 배포» 스텝이 런 164·168 연속 실패 — 눈 확인(§5)·PlayMode 진단(T46) 경로가 **런 163 에 멈춰 있다** (배포·검증 · 뒤 순서 없음 · 워커 K 등재)
- 실측(2026-09-13 09:3x · 워커 K · Actions API):
  - `screens` 브랜치 마지막 커밋 = `2e8676f 2026-09-13 08:42:26 deploy: c77191c…` = **런 163**. `meta.json` 도 `{"run":163}` 이다.
  - 런 **164**(`6c4f39c` · 08:48~09:00): «screens 브랜치용 meta.json» **success** → «screens 브랜치 배포» **failure**(08:59:56 · meta 뒤 22초).
  - 런 **168**(`daf48af` · 09:21~09:32): 같은 꼴 — meta **success**(09:31:24) → 배포 **failure**(09:31:46 · 22초).
  - 두 런은 22분 떨어져 있어 **동시 push 경쟁이 아니다**. 그 사이 `ci.yml` 변경은 `5c01c9b`(T89) 하나이고 그것은 **dotnet 잡에 스텝 둘을 더한 것**이라 이 스텝과 무관하다(런 168 이 정상 시작했으므로 YAML 도 성하다 — 로컬 `yaml.safe_load` 초록).
- 왜 급한가: §5 «눈 확인» 과 T46 «PlayMode 실패 진단 로그» 가 **둘 다 `screens` 를 통해서만** 워커에게 온다. 지금 워커는 런 163 의 빨강만 읽을 수 있고 그 뒤 런(164·168)이 무엇으로 빨간지 **볼 길이 없다** — «테스트 0개보다 나쁜» 자리다(§1).
- ⚠ **이 컨테이너에서는 원인을 못 읽는다**: 잡 로그 다운로드(`productionresultssa6.blob.core.windows.net`)와 `actions/permissions/workflow` API 가 **둘 다 프록시에 막힌다**(실측 · T46 이 적어 둔 «아티팩트는 프록시가 막는다» 와 같은 갈래). 그래서 등재만 하고 `ci.yml` 은 **추측으로 안 고쳤다**(잘못 고치면 모두의 CI 가 죽는다).
- 무엇을 한다: ⓐ 웹 UI 로 그 스텝 로그를 읽을 수 있는 사람(주인) 또는 로그가 뚫리는 세션이 **실패 문구를 먼저 읽는다** ⓑ 문구 없이 짚어 볼 자리 셋 — `peaceiris/actions-gh-pages@v4` 의 `github_token` 권한(잡에 `permissions:` 블록이 **없다** · 레포 기본값이 read 로 바뀌면 push 가 403), `force_orphan: true` + `screens` 보호 규칙, `publish_dir: ui-screens` 안의 새 파일 종류 ⓒ 고치는 김에 **이 스텝이 왜 죽었는지 스스로 말하게** 한다(T46 꼴) — 실패해도 `ui-screens/deploy-error.txt` 같은 것을 남기거나, 액션 대신 `git push` 한 줄로 바꿔 stderr 를 잡 요약에 찍는다.
- 판정: 다음 main 런에서 `screens` 의 `meta.json` 이 그 런 번호로 갱신되고 배포 스텝이 초록.
- 범위: `.github/workflows/ci.yml`(`unity-test` 잡의 screens 배포 스텝 · 필요하면 `permissions:` 한 블록).
- **✅ 결론(2026-09-13 · 런 170·171 · 워커 O)**: 원인은 GitHub 쪽 500(런 168 잡 로그: `git push --force screens` → `remote: Internal Server Error` · 커밋은 섰다). 배포 스텝을 같은 뜻의 셸(고아 루트 커밋 · force push · `.nojekyll` · `GITHUB_TOKEN`)로 바꾸고 15·30·45·60초 백오프 다섯 번 + 실패 문구 `::warning`/`::error` 주석(결정 221). 런 170(1d653bc): «screens 브랜치 배포» 초록 3초 · `::notice::screens 브랜치 배포 — 1번째 시도에 성공(런 170 · 44장)` · 런 171 도 배포 → `screens` `meta.json` 이 171 로 갱신(163 에서 멈춘 것이 풀렸다). 런 169(문서·ci.yml 만)는 유니티 잡이 건너뛰어 검증에 안 쓰였다.

### T97 ✅ — 플레이어 정보 팝업의 미니 씬이 안 꽂혀 «빈 갈색 상자» 다 (Game · T8·T65 뒤 · T28 16회차가 눈으로+코드로 잡음)
- 실측(2026-09-13 · T28 16회차 · 워커 M · 런 170 `screen_player-info.png` **2.9/10** ↔ 원작 `shot-043313`): 원작의 미리보기 칸에는 **작은 전투 장면**(풀·흙길·영웅·적·바위·나무)이 들어 있는데, 클론은 **갈색 그라디언트 상자**에 정본 폴백(«🛡️ + 어려움 4-1 + 웨이브 핍 4개»)만 있다.
- 코드로 확인(결정 219 순서대로): `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs` 93~95행이 «**T8 이 꽂는다** — 프리뷰 상자에 미니 씬을 세운다» 라며 `public static Func<RectTransform,bool> PreviewStart` / `Action PreviewStop` 훅을 두는데, **`Assets/Scripts/Game` 어디에도 그 훅에 대입하는 줄이 없다**(grep 0곳). 그래서 169행 `bool scene = PreviewStart != null && …` 이 항상 false 라 폴백만 그린다. T65(플레이어 정보 팝업 ✅)는 칸·줄을 채웠지 이 훅은 안 꽂았다.
- 무엇을 한다: `BattleScene`(또는 `MetaHost`)이 팝업이 열릴 때 **작은 렌더 텍스처 카메라**로 지금 전투 장면을 그려 `PreviewStart(rect)` 에 꽂고, 닫을 때 `PreviewStop()` 으로 끈다 — 원작 `ui.js` 의 `pinfo-scene`(같은 씬을 작은 칸에 다시 그린다)과 같은 뜻. 새 콘텐츠 0 · 60fps 규칙대로 팝업이 닫히면 카메라·RT 를 반납한다.
- 판정: `ui_score --score --only player-info` 가 **4.5 이상**(지금 2.9) + 워커가 PNG 를 열어 «미리보기 칸에 영웅·지면이 보인다» 확인 + `PerfBudgetTests` 상한 유지 + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Battle/BattleScene.cs`(훅 대입·RT 카메라 갈래) · `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs`(훅 호출부만) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.
- ✅ 결론(2026-09-13 · 워커 R): 새 파일 `Game/Battle/BattlePreview.cs` 가 훅에 꽂혀(`BattleScene.cs` 는 T86 lock 이라 안 열음) 본 카메라 복사 + 상자 픽셀 크기 RT + `RawImage(pinfo-scene-canvas)` 로 지금 전투 장면을 넣는다 · 닫으면 즉시 반납 · PlayMode 1. 원작은 별도 디오라마(`previewBuild` · 숲 지면·능선·소품 · T35 갈래)라 T35 뒤 그 자리에서 갈아 끼운다(결정 225). 점수·PNG 는 lock 반납 커밋에.

- ✅ 결론(2026-09-13 · 런 170 · 워커 B): 막은 이미 있었고(지각값으로 ×0.39) 진짜 결함은 `_h` 키로 54px 가 된 타원과 Lv 의 겹침 — 타원 39.6px 정중앙 · Lv 는 타원 아래끝에 잉크가 닿게 · HUD Lv 검정 알약 · `PetUiTests` 6/6 · ui_score skills 6.2 유지. 기록은 PROGRESS «T95 완료 기록».
### T98 ✅ — 모루 그림이 «사각 근사» 라 정본 SVG 와 다르다: 기운 상판 사다리꼴 · 둥근 총알 뿔 · 검은 키라인 3 (Game · T87 뒤 · 눈 확인 등재)
- 등재(2026-09-13 10:1x · 워커 G · T87 6회차 눈 확인): 런 170 의 `screen_gear-detail.png` 모루 자리를 원작 `shot-042120.png` 과 3배로 확대해 나란히 봤다.
  - 정본: 상판이 **기운 사다리꼴**(`M23 4 L90 3 L95 25 L12 26`)이고 뿔은 **둥근 총알**(`Q112 6 121 16 Q112 23 95 25`)이며 온 부분에 **검정 stroke 3**(`#170d0b`)이 둘러 있다.
  - 클론: 상판·앞면·받침이 전부 **둥근 사각**이고(`DrawAnvil` 주석이 «기운 사다리꼴을 사각으로» 라고 스스로 적어 뒀다) 뿔은 오른쪽으로 튀어나온 둥근 사각, 키라인은 눈에 안 띈다.
- 이제 길이 생겼다: T87 6회차가 넣은 `Ui/CraftFxPoly.cs`(정규 좌표 폴리곤 + 띠 그라디언트 + 무게중심 부풀림 stroke)로 **진짜 다각형**을 그릴 수 있다 — 모루 부분들을 그 자로 다시 그린다.
- 판정: 확대 눈 대조(원작 `shot-042120` ↔ `screen_gear-detail`) + PlayMode(상판 폴리곤 꼭짓점 4개가 카탈로그 값 · 키라인 면이 몸통보다 크다) + `ui_score` 대장간 칸.
- 범위: `Assets/Scripts/Game/Ui/ForgeSheet.cs`(DrawAnvil·Outlined) · `Assets/Forge/catalog.json`(anvil_* 좌표를 path 꼭짓점으로) · `Assets/Tests/PlayMode/ForgeUiTests.cs`. **T87 이 같은 파일을 쥐고 있으니 T87 이 끝난 뒤**.
### T99 ✅ — 남의 lock 이 쥐고 있던 이모지 라벨 다섯 자리(T89 가 못 간 곳) (Game·UI · T89 뒤 · **T87·T91 lock 이 풀린 뒤** · 워커 D · ChatScreen 자리 ✅ · Forge* 넷은 T110 으로 뗌)
- T89 가 세운 길(`UiKit.IconTextRow` + 정본 `TOAST_ICON` 표)을 **아직 못 간 라벨**에 잇는다. 토스트·라벨 전수 훑기(2026-09-13 10:1x · 워커 E)에서 남은 자리는 다섯뿐이다:
  - `ChatScreen.cs:206` «⚔ » 상대 전투력 — **T91**(워커 J) 범위.
  - `ForgeCraftPopup.cs:69·112` «판매 🪙 +N» · `ForgeInfoPopup.cs:113·120` «건너뛰기 💎 N»·«레벨 N 업그레이드 🪙 N» — **T87**(워커 G) 범위.
- ⚠ **두 줄 버튼 주의**: 그 버튼 라벨 셋은 `"판매\n🪙 +N"` 처럼 **줄바꿈이 있다**. `IconTextRow` 는 가로 한 줄이라 그대로 쓰면 두 줄이 한 줄로 눌린다 — 세로 칸(위: 글자 · 아래: 아이콘 줄)으로 감싸거나 `IconTextRow` 에 «줄바꿈이면 새 줄» 갈래를 더해야 한다. 정본은 `.btn` 안에서 `<br>` 로 나눈다(`ui.js` 의 해당 버튼).
- 판정: `tools/check_text_glyphs.py` 는 이 자리들을 «표가 덮는 글자» 로 세어 지금도 통과한다 — 판정은 **PNG 를 열어** 그 버튼·채팅줄에 아이콘이 섰는지 눈으로(§1) + 해당 화면 PlayMode 초록.
- 범위: `Assets/Scripts/Game/Ui/ChatScreen.cs` · `Ui/ForgeCraftPopup.cs` · `Ui/ForgeInfoPopup.cs` · (필요하면) `Ui/UiKit.cs`(두 줄 갈래).
- 진행(워커 D · 2026-09-13): 1회차 `ChatScreen` ⚔ → 정본 `power` 아이콘(런 179 PASS) · 2회차 채팅 목록이 바닥에 안 붙어 샷에 카드가 안 보이던 것을 정본 `pinChatBottom`·`_chatStick` 대로(결정 228) · `Forge*` 넷은 T87 lock 이 풀린 뒤.
- ✅ 결론(2026-09-13 · 워커 D · 결정 244): **ChatScreen 자리는 닫혔다** — 런 194 `ChatShareIconTests` 2/2 PASS + `screen_chat.png` 눈 확인(목록이 정본처럼 바닥 · 공유 카드 전투력 앞 검 아이콘 · ⚔ 두부 없음). 뿌리 둘을 같이 고쳤다: ⓐ ⚔ 글자 → 정본 `IconGen.img('power')` 아이콘 ⓑ `ChatScreen` 의 ScrollRect 참조가 T22 이래 null 이라 «바닥으로» 가 한 번도 안 돌던 것(정본 `pinChatBottom`·`_chatStick` 이식). **`ForgeCraftPopup`·`ForgeInfoPopup` 두 줄 버튼 넷은 T110 으로 뗐다**(T87 lock 20회차째 · 쥐고 있으면 T106·T109 가 «T99 lock 뒤» 로 못 집는다 · T100 의 선례).

### T100 ✅ — 두부가 아직 셋 남았다 · 자 둘이 그 셋을 **구조적으로** 못 본다 (Game·검증 · T53·T89 뒤 · 임자 없음 · 검수 Q 등재 · **T89 ✅ 되돌림 아님 — 남은 갈래**)
- 실측(2026-09-13 12:0x · 검수 Q · **런 179**(1c68d7e · EditMode 540/540 · PlayMode 104/104 · 빨강 0)의 PNG 를 Read 로 열어 본 것 — **전부 초록인 런에서 눈에 보인다**):
  - ⓐ `screen_main.png` 자동 제련 버튼이 «자동 **□** / OFF» 다. 글자는 **`↻`(U+21BB)** — `Assets/Scripts/Game/Ui/ForgeSheet.cs:94` 의 `string autoLabel = "자동 ↻\n" + (unlocked ? (h.AutoOn ? "ON" : "OFF") : "🔒");`. **정본은 글자가 아니라 아이콘이다**(`ui.js:1551` = `자동${IconGen.img('autoloop', 'auto-loop-ico')}` · 잠김은 `IconGen.img('lock')`) — T89 가 세운 «이모지 → T31 아이콘» 길에 이 둘(`autoloop`·`lock`)이 안 올라갔다.
  - ⓑ `screen_chat.png` 에 «need more hammers **□**» = **`😭`(U+1F62D)** · «오늘 던전 열쇠 다 씀 **□□**» = **`ㅠ`(U+3160) 둘**. 원문은 정본 `web/js/chat.js`(11·16행)이고 **채팅 문구는 C# 문자열이 아니라 `StreamingAssets/data/*.json`**(T2 가 정본에서 뽑은 것)으로 들어온다.
- **자 둘이 왜 못 잡나**(이 작업의 절반은 자다 · 셋 다 «초록인데 화면엔 □» 다):
  - `tools/check_text_glyphs.py`(T89) 는 지금 **rc 0**(«화면 문구 993줄 · 없는 글자 2종 · 전부 KNOWN»)이다. 그 자는 **부름 안에 직접 쓴 문자열 인자만** 본다(제 완료 기록: «주석·변수 대입은 안 센다») — ⓐ 는 **변수 대입**(`string autoLabel = …`)이라 안 보이고, ⓑ 는 **데이터에서 오는 글자**라 아예 소스에 없다.
  - `Assets/Tests/PlayMode/TextSizeGateTests.cs` 의 런타임 막이는 **T89 절이 «한글만이 아니라 전부 로 넓힌다» 고 적어 둔 대로 넓혀지지 않았다** — 65행이 여전히 `if (c < 0xAC00 || c > 0xD7A3) continue;  // 한글 음절만 본다` 이고 `HasCharacter(c, true, true)`(**폴백 포함**)로 묻는다. 그래서 화면에 실제로 선 `↻`·`😭`·`ㅠ` 를 하나도 안 센다.
- 할 일:
  - ⓐ `ForgeSheet` 의 자동 버튼을 정본대로 **아이콘 둘**(`autoloop`·`lock`)로 — T89 가 만든 인라인 스프라이트 길을 그대로 쓴다. 표는 `catalog.json`(§1 · 손으로 짓지 않는다).
  - ⓑ 채팅 문구의 이모지·자모: 서브셋을 다시 뽑을 때 **`data/*.json` 의 글자까지 훑어** 담는다(자모 U+3130~318F 포함 · 결정 203 의 «한글 1266자» 는 완성형만이었다). 이모지는 글꼴에 넣거나(용량) 정본처럼 아이콘으로 — **정본이 채팅에서는 이모지를 글자 그대로 쓴다**는 것이 판정 기준이다.
  - ⓒ **자 둘을 그 구멍만큼 넓힌다**: `check_text_glyphs.py` 에 «변수에 담긴 문자열» 과 **`Assets/StreamingAssets/data/*.json` 의 값**을 스캔 대상에 더하고, `TextSizeGateTests` 의 거르개를 «글꼴이 그려야 할 모든 문자» 로 바꾸고 **`HasCharacter(c, false, false)`**(폴백 없이 · 배포판이 판정 기준)로 묻는다.
- 판정: 고치기 **전에** 넓힌 자 둘이 **빨강**이 되는 것을 먼저 보이고(지금 rc 0 인 것이 문제다) · 고친 뒤 초록 · `screen_main.png`·`screen_chat.png` 을 열어 □ 가 0 인 기록(§1) · CI 유니티 잡 초록.
- 범위: `Assets/Scripts/Game/Ui/ForgeSheet.cs` · `Assets/Forge/catalog.json` · `Assets/Fonts/NotoSansKR-Forge.ttf`(다시 뽑으면) · `tools/check_text_glyphs.py` · `Assets/Tests/PlayMode/TextSizeGateTests.cs` · `docs/assets-map.md`.

### T101 ✅ — 리그 «상대 선택» 행의 별점이 도전 버튼 **왼쪽**에 있다: 정본은 버튼 **위**(세로 한 칸) (Game·UI · T22·T58 뒤 · 워커 J 등재)
- 실측(2026-09-13 12:2x · 워커 J · 런 183 `screen_league-challenge.png` ↔ 원작 `shot-042228.png`): 클론은 상대 행 오른쪽이 «⭐+5  [도전]» **가로 두 덩어리**인데, 원작은 «⭐+5» 가 **[도전] 버튼 바로 위**에 얹힌 **세로 한 칸**이다. `ui_score` 의 `league-challenge` 2.4/10 에서 못 짝지은 밴드2 블록 넷이 이 자리다.
- 정본: `ui.js` `renderLeagueChallenge` 의 `<span class="league-challenge-side">` 안에 `.star` 와 `button.btn.sm` 이 차례로 들어가고, CSS `.league-challenge-side { display:flex; flex-direction:column; align-items:center; gap:.3rem }`(style.css 2636) 가 그것을 세로로 쌓는다. 버튼 폭은 `.league-challenge-row .btn.sm { width: calc(var(--app-w) * .2173) }`(2626) — 클론 카탈로그 `lc_btn_w` 와 같은 값이라 폭은 이미 맞다.
- 할 일: 별 칸을 버튼 **위**로 옮기고(칸 폭 = 버튼 폭 · 가운데 정렬 · 간격 .3rem · 둘을 합친 높이를 행 가운데에), 이름 칸은 별 자리를 뺀 만큼 넓힌다.
- 판정: `ui_score --only league-challenge` 의 밴드2 미짝이 줄고 + PNG 눈 확인(별이 버튼 위) + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/LeagueSheet.cs`(도전 행 갈래) · `Assets/Tests/PlayMode/ShopUiTests.cs`(단언 한 줄).

### T102 ✅ — 펫 화면: 부화장 빛기둥이 안 그려진다 · 램프 키 접미(`_h`) · 첫 행 «장착됨» 리본이 마스크에 잘린다 (Game·UI · T20·T87 뒤 · 워커 B 등재)
- 실측(2026-09-13 13:0x · 워커 B · 런 186 `screen_pets.png` ↔ 원작 `shot-042356.png` · 부화장 크롭): 원작은 램프(돔 갓 + 노란 전구) 아래로 **노란 빛기둥**이 알까지 내려오는데 클론은 검은 세로 알약 + 노란 점뿐이고 빛기둥이 **0 픽셀**이다(T28 5회차가 런 95 에서 적어 둔 그 자리 · T20 절 메모). 첫 행 «장착됨» 리본은 검은 띠 반쪽만 보이고 글자가 잘린다.
- 뿌리: ⓐ `PetHatchCone : Graphic` 정점 메시 — T87 6회차가 같은 길로 «한 픽셀도 안 나왔다»(런 173)를 실측하고 `CraftFxPoly`(구운 스프라이트 + `Image`)로 돌아섰다(결정 223). 이 레포에서 맨 `Graphic` 은 안 칠해진다. ⓑ `lamp_h`·`lamp_bulb_h` 가 `_h` 접미라 `Px` 가 앱 **높이**를 곱한다 — 정본 `.hatch-lamp{height:calc(var(--app-w)*.0367)}` 는 앱 **폭** 기준(T95 `sk_eqplate_h` 와 같은 함정) · 갓은 `border-radius:50% 50% .1rem .1rem / 100% 100% …` 반타원 돔인데 클론은 반지름 h/2 알약. ⓒ `TileFace` 의 리본 앵커가 pivot(0.5,0.5)+y=+.2rem 이라 **가운데**가 칸 윗변 위 .2rem — 정본 `.sk-ribbon{top:-.2rem}` 은 **윗변**이 .2rem 위다(리본 높이의 반만큼 더 올라가 `grid-scroll` RectMask2D 가 자른다).
- 할 일: `PetHatchCone` 을 `Image` 상속으로 바꿔 `CraftFxPoly.Bake` 로 콘(38%~62% 사다리꼴 + 위→아래 그라디언트)·오른쪽 홈·돔 갓을 굽는다(API 유지 — `SkillPanel.EquippedLabel` 은 안 만진다) · 키 둘을 `_w` 접미로 · 리본 pivot(0.5,1). 픽셀 단언: 부화 중인 칸의 콘 상자 안에 따뜻한 노란 픽셀이 면적의 1/5 이상(T87 «쇳덩이가 화면에 칠해진다» 와 같은 길).
- 판정: PlayMode 빨강 0 + CI `screen_pets.png` 눈 확인(빛기둥이 전구에서 알까지 · 리본 글자가 온전) · `ui_score --only pets` 밴드5 미짝이 줄어든다.
- 범위: `Assets/Scripts/Game/Ui/PetPanel.cs` · `Ui/PetHatchCone.cs` · `Assets/Forge/Resources/PetSkillUi.json` · `Assets/Tests/PlayMode/PetUiTests.cs`.
- ✅ 결론(2026-09-13 13:4x · 워커 B · 런 192 d405b80 · 런 194 PNG): `PetUiTests` 6/6 PASS(빛기둥 픽셀 단언 포함) · `screen_pets.png` 눈 확인 — 빛기둥이 전구에서 알까지 노랗게 내려오고 빈 칸은 회청색 dim · 갓이 돔 · 첫 행 «장착됨» 글자가 온전(윗변 .2rem 만 정본처럼 잘림) · `ui_score pets` 6.7 → **7.2**. 런 192·194 의 빨강 1 은 T101 임자 테스트.

### T103 ✅ — 펫 업그레이드 모달이 탭바를 안 덮고 ✕ 가 둘 · 머리 판 글자가 배경과 대비가 없다 (Game·UI · T79 뒤 · T28 19회차가 정본 CSS 로 잡음)
- 실측(2026-09-13 · T28 19회차 · 워커 M · 런 188 `screen_pet-upgrade.png` **2.1/10** — 여섯 회차째 «다음 볼 화면» 둘째):
  - ⓐ **탭바가 환하게 보이고 그 위에 ✕ 가 하나 더** 있다(카드 아래 ✕ + 탭바 위 ✕ = 둘). 원작 `shot-042503` 은 카드가 화면을 덮어 상단바·탭바가 안 보이고 ✕ 가 **하나**다.
  - ⓑ 머리 판(`.petup-panel`)의 «[일반] 거북이 · 18 피해 · 105 체력» 이 **판 색과 대비가 거의 없다**(연한 회색 글자 ↔ 연한 회색 판). 원작은 같은 자리가 진한 글자다.
- 정본 근거(결정 219 순서대로 코드부터): `css/style.css` 3781~3783 이 **`#pet-upgrade-modal` 을 `.modal.dim-tabbar` 목록(z-index 40)** 에 넣는다 — 이 팝업은 **탭바까지 덮는** 층이다. 그리고 `.petup-panel` 은 `background: #b7b7b7` 판이라 글자는 기본 진한 잉크다(4347~4350행).
- T79(✅ · 워커 B)가 닫은 것은 **딤 밝기**(«흰 자리 127.0 = 브라우저 .5»)였고, 내가 8회차에 등재할 때 적은 «✕ 하나 · 화면을 덮는다 · 머리 구성» 은 그 회차 판정에서 빠졌다(그 완료 기록의 판정 넷). 그래서 남은 둘만 새 번호로 뗀다.
- 무엇을 한다: ⓐ 펫 업그레이드 팝업 층을 `dim-tabbar` 갈래로 올려 탭바를 덮고 부모 시트의 ✕ 를 가린다(화면당 ✕ 하나) ⓑ 머리 판 글자 색 키를 정본 잉크로 바꾼다(판 `#b7b7b7` 위 진한 글자).
- 판정: `ui_score --score --only pet-upgrade` 가 **4.0 이상**(지금 2.1 · 이 화면은 천장이 낮지 않다 · 결정 229) + 워커가 PNG 를 열어 «✕ 하나 · 탭바 안 보임 · 머리 글자가 읽힌다» 확인 + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/PetUpgrade*` · `Ui/PetSkillModal.cs`(층·딤 갈래) · `Assets/Forge/Resources/PetSkillUi.json`(잉크 키) · `Assets/Tests/PlayMode/PetUiTests.cs`.

- ✅ 결론(2026-09-13 · 워커 R · 코드 0줄 · 결정 233): **전제 둘 다 정본과 같다.** ⓐ 딤이 탭바를 덮는다(런 191 픽셀 탭바 32.8 → 13.4 · 탭바 ✕ 242 → 120 = ×0.5) — 아래 ✕ 는 딤 아래 탭바 것이고 정본도 z40 + `.modal` .5 라 같은 자리에 같은 ✕ 가 비친다(원작 샷은 .985 시절). ⓑ 정본 `style.css` 5525 가 이름을 희귀도 색 + 검정 2px 획, 피해/체력만 `#000` 으로 칠한다 — 클론과 같은 구성. 남은 진짜 차이 = **검정 획이 안 보인다**(이름 줄 어두운 픽셀 0) → 앱 전체 `UiKit.Outline` 의 일이라 **T104** 로 뗐다. `PetSkillUi.json`·`PetUiTests.cs`(T102 lock) 는 안 열었다.

### T104 ✅ — 글자 외곽선이 정본 2px 검정 키라인보다 옅다: `UiKit.Outline` 의 width01 이 SDF 여백 비율이라 작은 글자에서 1px 미만 (Game·UI · T53 뒤 · **T87·T102 lock 파일은 그 뒤** · T103 이 가름)
- 실측(2026-09-13 · T103 · 워커 R · 런 191): `screen_pet-upgrade.png` 이름 줄 «[일반] 거북이»(x160~330·y118~142) 어두운 픽셀(max<70) **0개** ↔ 같은 판 «18 피해» 줄 239개 · `screen_pet-detail.png` 흰 카드 위 같은 이름도 옅은 회색 테두리뿐. 정본은 `-webkit-text-stroke: max(1.2px,.113em) var(--pp-line)` + `paint-order: stroke fill` = 검정 **2px** 키라인(`style.css` 5451 `.petd-name` .125em · 5525 `.petup-panel .idet-name` .113em · 5518~5524 주석 «원본의 키라인은 양쪽 다 2px»).
- 원인(코드): `UiKit.Outline(t, key, width01)` 이 TMP `outlineWidth`(0~1 · SDF 여백 비율)에 호출자 상수를 그대로 넣는다. 런타임 폰트 애셋 `TMP_FontAsset.CreateFontAsset(cat.font)` 기본값 = 90pt 샘플 · 여백 9px → 획 두께 ≈ 여백 × (렌더 크기/90) × width01 — 17px 글자 · 0.25 면 1px 이 안 된다. 호출부 20곳(`PetSkillKit.Stroked` 0.25~0.35 · `Outline` 0.18~0.3 · 던전 `dg_name_outline` 등 카탈로그 키)이 전부 같은 병.
- 무엇을 한다: ⓐ `UiKit.Outline` 에 **px 갈래**(폰트 애셋 `atlasPadding`·`faceInfo.pointSize`·`t.fontSize` 로 width01 환산 · 1 클램프 · 환산식은 TMP SDF 셰이더 것이라 CI 픽셀로 잰다) ⓑ 카탈로그에 키라인 px 키 하나(정본 2px × 촬영 배율 2.164 · T87 결정 222 규약 · **T87 lock 뒤**) ⓒ 호출부를 그 키로(숫자 상수 제거 — §1 «수치는 코드에 박지 않는다») ⓓ `paint-order: stroke fill` 대응 — TMP 외곽선은 채움 안쪽으로 먹으니 `_FaceDilate` 로 채움 두께를 지키는지 같이 본다.
- 판정: PlayMode 픽셀 단언(새 파일 `OutlineTests.cs` · 이름 줄 어두운 픽셀 > 0 · 획 두께 ≈ 키 값) + 워커가 PNG 를 열어 이름 둘레 검정 선 확인(pet-upgrade · pet-detail · 스킬 시트 제목).
- 범위: `Assets/Scripts/Game/Ui/UiKit.cs` · `Assets/Forge/catalog.json`(T87 뒤) · 호출부 `Ui/PetSkillKit.cs`·`Ui/PetUpgradePopup.cs`·`Ui/Popups.cs`·`Ui/DungeonDetailPopup.cs`·`Ui/DungeonSheet.cs`·`Ui/Hud.cs`·`Ui/BattleOverlay.cs`·`Ui/SkillPanel.cs`·`Ui/SkillBar.cs`·`Ui/SkillPetSheet.cs`·`Ui/SkillRatesPopup.cs`·`Ui/MountSheet.cs` · `Ui/PetPanel.cs`(T102 뒤) · `Ui/ForgeSheet.cs`(T87 뒤) · `Assets/Tests/PlayMode/OutlineTests.cs`(새).
- 🔄 2026-09-13 워커 S(sess-1329-41207) 1회차: 뿌리 — 모바일 SDF 셰이더를 읽어 식을 세웠다(가장자리 이동 = `_FaceDilate`×R×½ · 띠 반폭 = `_OutlineWidth`×R×½ · 1 알파 = `_GradientScale` 텍셀). 정본 «바깥 N/2 · 채움 그대로» 는 **D = W** 로 난다 → `Core/Ui/OutlineSdf`(환산·잘림 표식) + `UiKit.OutlinePx`(재질 G·R·폰트 샘플링 크기를 그 순간 읽는다) + EditMode 5 + PlayMode `OutlineTests`(흰 판 위 «I» 셋을 픽셀로 · `screen_t104-outline.png`). 카탈로그 키·호출부·`ForgeSheet`·`PetPanel` 은 T87·T102 lock 뒤(2회차). 런 195: 식은 그대로(W=D=.833)인데 캡처 배율 0.262 에서 96px 글자가 줄기 1px 라 자가 못 쟀다 → 글자·획을 앱 폭 기준으로 키움(2차 판정 런 대기). 런 200: **식이 맞았다**(띠 5/6px ↔ 기대 5.08) · 코어 +2 는 문턱 비대칭이라 128 하나로 · 3차 판정 런 대기. **런 204: 1회차 ✅**(`OutlineTests` PASS · PNG 눈 확인: px 갈래 띠 6/6 · 코어 7 = 줄기 그대로) · lock 반납. **2회차(누구든 · T87 lock 뒤)**: 카탈로그 `text_keyline_px` + 호출부 18자리를 `OutlinePx` 로 + `ForgeSheet`·`PetPanel` + 옛 `Outline` 제거 · 3회차 PNG 눈 확인.
- 🔄 2026-09-13 워커 N(sess-0524-8791) 2회차: 호출부 30자리를 `KeylineUi.json` 폭표(px 17키 · em 5키 · 정본 줄 번호 출처)로 옮겼다 — `KeylineUi.Stroke(key, fontSize)` 가 px×`css_px`(2.164) · em×글자 · `max(Npx,.Mem)` 을 캔버스 px 로 내 `OutlinePx` 에 넘긴다 · `Px()` 의 ×2.164 누락(2ae9903)도 여기서 갚아 T109 자리도 같이 굵어진다 · 정본이 text-shadow 인 자리(HUD·보스 경고)·규칙 없는 링(퀘스트·플레이어 정보·리그 제목)은 안 옮김(결정 268) · 남의 lock 자리(`ForgeSheet`·`ForgeUi`·`ForgeInfoPopup` T87 · `DungeonSheet` T120)와 `catalog.json` 합치기는 3회차 · PlayMode 단언 +1 · 판정은 다음 런 PNG(pet-detail·skills·pet-upgrade).
- ✅ 결론(2026-09-13 · 워커 N · sess-0524-8791): 1회차(S)가 식·`OutlinePx`·픽셀 자를, 2회차(N)가 호출부 30자리와 `KeylineUi.json` 폭표(css_px 2.164)를 세웠다 — 런 238·240·241 에서 새 단언 PASS · `screen_pet-upgrade` 이름 줄 어두운 픽셀 0 → 554(T103 실측 자리). 1회차 픽셀 자의 빨강은 T121(글꼴 굽기 값)이 흔드는 중이라 그 절 몫. **남은 자리**(누구든 · T87 lock 뒤): `ForgeSheet.cs`(anvil_count_stroke → `KeylineUi` 2px · 1625행 · `Ring(nm)`) · `ForgeUi.cs` Ring 둘 · `ForgeInfoPopup.cs` Ring 하나 + `catalog.json` 으로 표 합치기. 정본이 text-shadow 인 자리(HUD 스테이지·보스 경고·던전 행)와 규칙 없는 링(퀘스트·플레이어 정보·리그 제목)은 안 옮긴다(결정 268).

### T105 ✅ — 플레이어 정보 미니 씬이 칸 아래 ~5%p 를 검게 남긴다 (Game · T97 뒤 · T28 17·20회차 실측)
- 실측(2026-09-13 · 워커 M · 런 195 `screen_player-info.png` **2.8/10**): 미리보기 칸은 화면의 y **24~38%**(≈14%p)인데 미니 씬은 y 24~33% 만 채우고 **아래 4~5%p 가 검다**. 행 평균 밝기로 잰 값: y31% **191** → y32% 112 → y33% **31** → y37~38% **25**(칸 바닥). 원작 `shot-043313` 은 그 자리까지 풀밭이 찬다.
- T97(✅ · 워커 R)이 미니 씬을 세운 것은 맞다(영웅·지면·하늘이 실제로 그려진다 — T28 17회차가 PNG 로 확인). 남은 것은 **칸을 꽉 안 채우는 것** 하나다. 17회차 기록에 ««다음 볼 화면» 다섯에 `player-info` 가 다시 오르면 그때 번호를 뗀다» 고 적었고, 20회차에 그 자리에 올라 뗀다.
- 어디를 볼까(잡는 사람이 확인할 것 · 둘 다 후보다): ⓐ `BattlePreview.Start` 가 카메라를 `CopyFrom(main)` 한 뒤 **`transform.SetParent(transform,false)`** 로 제 오브젝트 자리에 둔다 — 본 카메라의 **자리·각도는 CopyFrom 이 안 옮긴다**(설정만 복사). 본 카메라의 `position`·`rotation` 을 같이 옮겼는지 본다. ⓑ 본 카메라는 T54 의 **비대칭 절두체**(앱 상자 framing)인데 미니 카메라는 `ResetProjectionMatrix()` + `aspect = w/h` 로 다시 잡는다 — 세로 화각이 달라 지면이 칸 바닥에 못 닿을 수 있다.
- 판정: `ui_score --score --only player-info` 가 **3.5 이상**(지금 2.8) + 워커가 PNG 를 열어 «칸 바닥까지 지면» 확인 + 행 평균 밝기로 칸 아래 2%p 가 25 가 아님 + `PerfBudgetTests` 상한 유지 + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Battle/BattlePreview.cs` · `Assets/Tests/PlayMode/UiSmokeTests.cs`(칸 채움 단언 한 줄).
- ✅ 결론(2026-09-13 · 워커 R · 결정 236): 후보 ⓐⓑ 둘 다 아니었다 — 원인은 **본 리그 복사 그 자체**(fov 62 를 3:1 칸에 걸면 띠 아래 흙 절벽까지 들어온다 · 런 195 `screen_main` y430~530 도 같은 어둠). 정본 `previewBuild` 의 미니 카메라 리그(fov 42 · (0.1,1.95,4.3) → (0.05,0.92,0) · 영웅 (0,0,0.4))를 추출기 → `scene.json` `PREVIEW_CAM` → `SceneDefs` → `BattlePreview.ApplyRig` 로 끌어와 지금 영웅 리그 자리에 건다(코드에 수 0 · 표가 없으면 T97 방식). PlayMode 단언: 칸 아래 모서리 광선이 지면을 영웅 앞에서 만난다. 판정(런 번호 · PNG)은 완료 기록에.
### T106 — 이모지 여섯이 화면에서 □: 정본도 «글자» 라 이모지 폴백 글꼴 말고는 길이 없다 (Game·UI · T53·T100 뒤 · **주인 에셋 승인 사안** · T87·T99·T104 lock 파일은 그 뒤)
- 실측(2026-09-13 · T100 3회차 · 워커 E): `check_text_glyphs` 가 세는 «글꼴에 없는 글자» 일곱 중 여섯이 이모지다 — `⏱`(ForgeInfoPopup 업그레이드 버튼) · `⏹`(ForgeHost 자동 제련 종료 토스트 · 정본 `ui.js` 2272) · `🐴`·`🐾`(`PetSkillKit.PetFace` 의 얼굴 폴백 + 펫·탈것 토스트 문구) · `🛡`(`PlayerInfoUi.json /text/shield`) · `😭`(채팅 문구 · 정본 `chat.js` 11).
- 정본 확인: 전부 **글자**다 — `ui.js` 2143 `creatureFace` 가 3D 썸네일이 구워지기 전에는 `<span>${emoji}</span>` 를 깔고(2162 `petFace`), `ui.js` 5153 `.pinfo-preview` 는 `<span>🛡️</span>`, 토스트·채팅 문구는 문자열 그대로다. 브라우저가 OS 이모지 글꼴로 그려 주던 것이라 **리눅스 CI·WebGL 에는 그 글꼴이 없다**(한글이 그랬듯이 · T53).
- 아이콘으로 바꾸면 안 된다: T31 아틀라스에 비슷한 그림이 있어도 정본이 그 자리에서 글자를 쓰는 한 바꾸는 것은 «그대로 옮기기» 가 아니다(`↻` 는 반대로 정본이 아이콘이라 T100 이 고친다).
- 무엇을 한다(주인 승인 뒤): ⓐ 이모지 글꼴 하나를 `Assets/Fonts/` 에 · 정본에 실제로 나오는 이모지만 남긴 서브셋(T53·T100 2회차와 같은 `fontTools.subset` 길 · TMP 는 색 비트맵 글꼴(CBDT/COLR)을 못 그리므로 **단색** 글꼴이어야 한다 — 정본의 색 이모지와 다른 점을 결정 기록에 남긴다) ⓑ 카탈로그에 폴백 글꼴 키(`gen_ui_catalog.py` 같이 · **T87 lock 뒤**) ⓒ `UiFont.Build` 의 `fallbackFontAssetTable` 에 더한다(UiKit 309~319 에 OS 폴백을 붙이는 자리가 이미 있다 · **T99·T104 lock 뒤**) ⓓ `check_text_glyphs.KNOWN` 과 `TextSizeGateTests.KnownTofu` 에서 여섯을 빼고, PlayMode 물음을 그 여섯에 한해 «폴백 포함» 으로.
- 판정: `check_text_glyphs` 가 여섯을 `KNOWN` 없이 통과 + PlayMode 두부 막이 초록 + 워커가 `screen_pets.png`·`screen_player-info.png`·`screen_chat.png` 을 열어 □ 가 없는 것을 눈으로(§1).
- 범위: `Assets/Fonts/`(새 글꼴 · 주인 승인 뒤) · `Assets/Forge/catalog.json`(T87 뒤) · `tools/gen_ui_catalog.py` · `Assets/Scripts/Game/Ui/UiKit.cs`(T99·T104 뒤) · `tools/check_text_glyphs.py` · `Assets/Tests/PlayMode/TextSizeGateTests.cs` · `docs/assets-map.md`.

### T107 ✅ — 두부 막이(`check_text_glyphs`)가 통째로 건너뛴 자리: **토스트 그릇 둘이 아이콘 길을 안 거쳤다** (검증+Game·UI · T89 뒤 · T28 21회차가 PNG→코드로 잡음 · 1회차 워커 F)
- 실측(2026-09-13 · T28 21회차 · 워커 M · 런 205 `screen_craft-compare.png`): 판매 버튼이 «판매 / **□** +23» 이다 — 4배 확대해 보니 코인 아이콘이 아니라 **빈 네모(두부)** 다. 그런데 같은 커밋에서 `python3 tools/check_text_glyphs.py` 는 **rc 0**(«글꼴에 없는 글자 7종 · 전부 KNOWN»)이다.
- ⚠ **원인 정정(1회차 실측 · 워커 F)**: «이어 붙이면 안 본다» 는 틀렸다 — `screen_strings()` 는 `«판매\n🪙 +»` 를 **네 곳 다 본다**. 진짜 구멍은 자의 **치기**다: 리터럴 둘레 ±3줄에 `Toast(` 가 보이면 «아이콘 길» 로 쳐서 건너뛰는데(결정 237), 토스트 그릇 셋 중 **둘이 아이콘 길을 안 거쳤다** — `DungeonToast.Show` 는 `Bold(...)`, `PetSkillModal.Toast` 는 `PetSkillKit.Text(...)` 로 글자만 세웠다. 그래서 ⭐·🔒·🧪·💎(던전·기술·승천)과 `PetSkillUi.json` `/text/toast_*` **20줄**의 🥚·✨·🎉·🎫·🧩·⬆️·⚡·⚙️·📋 가 초록으로 지나갔다(그릇 둘 · 자리 45 실측).
- 얼마나: 화면에 글자를 세우는 부름(`Btn`·`Button`·`Text`·`Label`·`Bold`·`Toast`·`Pill`·`Stroked`)의 리터럴만 훑어도 **글꼴에 없는 이모지 20종 · 41자리**다 — `💎`6 · `⭐`5 · `🔒`4 · `🪙`4 · `⚒`2 · `📜`2 · `🏆`2 · `🔨`2 · `💤`·`📍`·`🥚`·`🗝`·`💾`·`🎟`·`💀`·`🧪`·`⏱`·`📌`·`⏹`·`U+FE0F`. (T106 이 쥔 여섯과 겹치지 않는 것들이다.)
- **1회차에 한 것**(워커 F · sess-1527-35260): ⓐ 토스트 그릇 둘을 아이콘 길로 이었다 — `DungeonToast.Paint`(부를 때마다 줄을 다시 세운다 · `Destroy` 는 프레임 끝이라 먼저 끈다) · `PetSkillModal.Toast`(색이 제 표에서 오므로 줄을 세운 뒤 조각마다 바른다 · `LastToast` 는 **원문**을 쥔다). ⓑ 자에 `toast_sinks` 를 더해 **그 치기의 근거를 검사**한다: 문구(`string`)를 받는 `Toast`/`Show` 는 ⓐ 제 몸이 `IconTextRow`·`UiText.Split` 을 부르거나 ⓑ 같은 클래스의 도우미가 부르거나 ⓒ 다른 그릇으로 넘겨야 하고, 아니면 **rc 1**. C# 자르기는 주석·문자열을 같은 길이 공백으로 지운 사본(`mask_cs`)에서 중괄호를 세므로 «문장 안의 `{`·`class`» 에 안 속는다.
- **판정(1회차 · 실측)**: 고장 주입 둘 다 rc 1 — 자기 검사 칸(`UiKit.Text` 만 부르는 그릇)과 **실제 파일**(`DungeonPopups.cs` 의 `IconTextRow` 를 `UiKit.Text` 로 되돌림) · 고친 뒤 rc 0(«토스트 그릇 5개 전부 아이콘 길») · PlayMode `ToastIconTests.던전_토스트도_이모지를_아이콘으로_세운다`(🔒 가 아이콘 칸으로 서고 글자 조각에 안 남고 `DungeonToast.Last` 는 원문).
- **T107 이 안 쥐는 것**: `LABEL_KNOWN` 에 남은 다섯은 **이미 임자가 있다** — `ForgeCraftPopup`·`ForgeInfoPopup` 의 **두 줄 라벨** 넷은 **T99**, `ForgeSheet` 잠금 🔒 는 **T108**(둘 다 T87 lock 뒤). 가로 `IconTextRow` 를 그대로 쓰면 줄이 무너지는 갈래라 세로 판이 먼저 필요하고, 그 판단은 그 두 작업의 몫이다. T107 은 «자가 못 보던 구멍» 과 «그릇 둘» 로 닫는다.
- 범위: `tools/check_text_glyphs.py` · `Assets/Scripts/Game/Ui/DungeonPopups.cs` · `Assets/Scripts/Game/Ui/PetSkillModal.cs` · `Assets/Tests/PlayMode/ToastIconTests.cs`.
### T108 ✅ — 대장간 자동 제련 버튼 라벨 둘이 글자다(«자동 ↻» · 잠금 «🔒») — 정본은 둘 다 아이콘 (Game·UI · T89·T100 뒤 · **T87 lock 뒤**)
- 실측(2026-09-13 · T100 4회차 · 워커 E · 런 191 `screen_settings.png` 오른쪽 끝에 «자동 **□** / OFF» 가 찍혔다): `ForgeSheet.cs` 108행이 `"자동 ↻\n" + (unlocked ? (AutoOn ? "ON" : "OFF") : "🔒")` 로 라벨을 만든다. `↻`(U+21BB)는 주인 글꼴에 없어 □ 이고, `🔒` 는 `TOAST_ICON` 에 있지만 **이 자리는 `IconTextRow` 를 안 거치는 그냥 라벨**이라 역시 □ 다(T100 4회차가 `label_risk` 로 잡은 다섯 중 하나).
- 정본: `ui.js` 1551 `자동${IconGen.img('autoloop', 'auto-loop-ico')}<br>${autoUnlocked ? (S.autoForgeOn ? 'ON' : 'OFF') : IconGen.img('lock')}` — 둘 다 `IconGen` 아이콘이고 T31 아틀라스에 `autoloop`·`lock` 키가 있다(`UiIcons.Get`).
- 무엇을 한다: ⓐ 라벨을 «자동 + `autoloop` 아이콘 / 아랫줄 ON·OFF 또는 `lock` 아이콘» 으로 — **세로로 쌓는 자리**라 가로 `UiKit.IconTextRow` 를 그대로 쓰면 줄이 무너진다(T99 의 `ForgeCraftPopup`·`ForgeInfoPopup` 두 줄 버튼과 같은 갈래 · 세로 갈래를 `UiKit` 에 하나 세우면 넷이 같이 풀린다) ⓑ `check_text_glyphs` 의 `KNOWN['↻']` 과 `LABEL_KNOWN['ForgeSheet.cs|🔒']` 을 지운다(그 자리가 다시 글자가 되면 자가 빨개진다) ⓒ `ForgeUiTests` 에 «자동 버튼 라벨에 글자 ↻·🔒 가 없고 아이콘 Image 가 둘» 단언.
- 판정: 자 rc 0(둘이 목록에서 사라진다) + PlayMode 단언 + 워커가 `screen_forge-list.png`·`screen_autoforge.png` 을 열어 버튼에 고리 아이콘이 선 것을 눈으로(§1).
- 범위: `Assets/Scripts/Game/Ui/ForgeSheet.cs`(T87 뒤) · `Assets/Scripts/Game/Ui/UiKit.cs`(세로 갈래를 세울 때 · T99·T104 뒤) · `tools/check_text_glyphs.py` · `Assets/Tests/PlayMode/ForgeUiTests.cs`(T87 뒤).
- 🔄 2026-09-14 02:4x 워커 N(sess-0524-8791) 1회차: T87 이 33회차로 반납해 `ForgeSheet.cs` 가 열렸다 — `ForgeSheet.AutoBtn`(`PopupKit.Btn` 의 빈 라벨을 끄고 `IconTextStack.Build` 두 줄 + 아이콘 칸) · 정본이 `IconGen.img('autoloop')`·`img('lock')` 를 **직접** 박는 자리라 `IconTextStack.AppendIcon`(아이콘 키로 칸 · 정본 `.ico` 음수 세로 마진 = 칸은 줄 높이·그림은 넘치게)과 줄 크기를 직접 재는 `Fit` 을 더했다(결정 308) · 치수는 카탈로그 layout 다섯(`ico_em`·`ico_my_em`·`ico_mr_em`·`auto_loop_ico_em`·`auto_loop_ico_ml_em`) · ⓑ `KNOWN['↻']`·`LABEL_KNOWN['ForgeSheet.cs|🔒']` 뺌(rc 0) · ⓒ 단언은 `ForgeUiTests.cs` 가 T94·T114 lock 이라 **새 파일** `ForgeAutoLabelTests`(잠금 → 해금 다시 그리기). → **런 301 PASS + `screen_settings`·`screen_main` 6배 눈 확인(«자동» 옆 고리 아이콘 · □ 없음) · ✅ · lock 반납**. 남은 것: 없음(T110 2회차 `ForgeCraftPopup`·`ForgeInfoPopup` 넷은 이모지 표 갈래라 `IconTextStack.Build` 만으로 · T94·T114 lock 뒤 누구든).
- ✅ 결론: 자동 제련 버튼은 정본대로 «자동 + autoloop 아이콘 / ON·OFF 또는 lock 아이콘» — 글자 ↻·🔒 는 코드에 0, 자가 그 자리를 «새 두부/새 자리» 로 막는다.

### T109 — 정본 키라인 52규칙 중 최소 20자리가 클론에 **호출 자체가 없다**: T104(폭 환산)가 닿지 않는 갈래 · 막는 자도 없다 (검증+Game·UI · **T104 뒤** · T28 22회차가 PNG→정본 CSS→코드로 잡음)
- 실측(2026-09-13 · T28 22회차 · 워커 M · 런 208 `screen_league-challenge.png` ↔ 원작 `shot-042228.png` · 둘 다 6배 확대): 원작의 전투력 «⚔32b» 는 **주황 획 둘레에 검정 키라인**이 또렷한데, 클론의 «75.6m» 은 **민주황**(둘레 0)이다.
- 정본: `css/style.css` 2635 `.league-challenge-name small { color:#ff880f; font-weight:900; -webkit-text-stroke: 2px var(--pp-line); paint-order: stroke fill; }` (`--pp-line: #000`).
- 클론: `Assets/Scripts/Game/Ui/LeagueSheet.cs` 334~336 은 `UiKit.Text(row, "cp", TextKind.Sub, PopupKit.Fmt(o.Bot.Cp), "challenge_cp", …)` 뿐 — `UiKit.Outline`·`OutlinePx`·`Stroked` 호출이 **이 파일 전체에 0개**다.
- 얼마나(실측): 정본 CSS 의 `-webkit-text-stroke` 규칙 **52개** ↔ 클론 키라인 호출 **32자리·14파일**. 정본이 키라인을 주는데 **호출이 0인 클론 파일 11개**: `LeagueSheet`(리그 행·서버·티어 순위·보상 리본·상대 전투력 등 7규칙) · `PlayerInfoPopup`(`#player-info-modal .pinfo-id-text .cp`) · `ShopSheet`(`.shop-deal-tag`·`.shop-gem-amt`) · `PassPopup`(`.pass-card`) · `OfflinePopup`(`.offline-card`·`.offline-total`·`.ob-zzz i`) · `ForgeAutoPopup`(`.af-start`·`.af-title`) · `ForgeInfoPopup`(`.fi-card .fi-skip`) · `ChatScreen`(`.chat-bubble, .chat-time`·`.chat-share-label`·`.chat-share-side small:last-child`) · `ProfilePopup`(`.profile-tabs button.on`) · `QuestSheet` · `ForgeCraftPopup`.
- **T104 와 다른 갈래다**: T104 는 «이미 부르는 자리의 폭 환산이 틀렸다»(호출부 18자리)를 고친다 — 여기 열한 파일은 그 18자리에 **하나도 없다**. 즉 T104 가 끝나도 이 자리들은 그대로 민글자다.
- 무엇을 한다: ⓐ 자 하나(`tools/check_keyline.py`)를 세운다 — 정본 CSS 에서 `-webkit-text-stroke` 규칙(선택자·폭)을 걷고, 클론 `Assets/Scripts/Game/Ui/*.cs` 에서 키라인 호출을 걷어 **«정본에 있는데 그 화면 파일에 호출 0»** 을 rc 1 로 센다(선택자 ↔ 클론 파일 짝은 도구 안 표 하나 · 새 화면이 늘면 그 표에 줄을 더한다). ⓑ 그 목록의 자리에 T104 1회차가 세운 `UiKit.OutlinePx` 를 붙인다(폭은 카탈로그 `text_keyline_px` — §1 «수치는 코드에 박지 않는다» · **T104 2회차 뒤**). ⓒ 살아 있는 lock 이 쥔 파일은 그 뒤로 미루고 절에 적는다.
- 판정: ⓐ 새 자가 고장 주입(호출 하나 지우기)에서 rc 1 · 붙인 뒤 rc 0 ⓑ 다음 런 `screen_league-challenge.png` 를 6배로 열어 전투력 숫자 둘레에 **검정 선** 확인(원작 `shot-042228` 과 나란히) ⓒ PlayMode 빨강 0.
- 범위: `tools/check_keyline.py`(새) · `Assets/Scripts/Game/Ui/LeagueSheet.cs`·`PlayerInfoPopup.cs`·`ShopSheet.cs`·`PassPopup.cs`·`OfflinePopup.cs`·`ForgeAutoPopup.cs`·`ForgeInfoPopup.cs`·`ChatScreen.cs`·`ProfilePopup.cs`·`QuestSheet.cs`·`ForgeCraftPopup.cs`(각 파일의 살아 있는 lock 뒤) · `docs/ROUTINE.md` §3 게이트 줄.
- 🔄 2026-09-13 워커 C(sess-1535-7768) 1회차 = ⓐ 자만: `tools/check_keyline.py` — 정본 `style.css` 의 `-webkit-text-stroke` 규칙 **51개**(끄는 규칙 2 포함)를 파서로 걷고 선택자↔클론 자리 표(파일 · `#요소 이름` · `@도우미 메서드`)로 대조한다. 실측: 자리 **31 초록 · 빈자리 33**(호출 없음 27 · 요소 자체 없음 6: 오프라인 zzz · 스킬바 슬롯 Lv · 펫 타일 Lv · 보상 날림 amt·tick) — 전부 `KNOWN`(임자 T109 ⓑ) 으로 두어 rc 0. 22회차의 «호출 0 파일 11개» 는 `PopupKit.Ring` 을 안 센 수다(리그 시트만 Ring 4) — 규칙 단위로 재면 위 수가 맞다(결정 242). 자기 검사 11칸(고장 주입: Ring 지우기 · 표 밖 규칙 · 정본에 없는 선택자 · 요소 없음 · 메서드 민글자). CI `datasync` 잡(정본 체크아웃이 있는 잡) 두 스텝 + §3 한 줄. ⓑ(호출 붙이기)는 T104 2회차 `text_keyline_px` 뒤 · `ChatScreen`·`ForgeInfoPopup`·`ForgeCraftPopup` 은 T99 lock 뒤. **lock 반납**(런 212 datasync 잡 두 스텝 초록 · 자리 자체는 ⬜ — ⓑ 는 T104 2회차 뒤 누구든 · KNOWN 33 이 «어디에 붙일지» 표다).

- 🔄 2026-09-13 20:5x 워커 P(sess-2054-26175) **5회차** = ⓑ 이어잡기(T104 ✅ 뒤 · 2~4회차는 워커 I 가 자유 파일 다섯 10자리): `LeagueSheet.cs` 네 자리 — 행 순위·이름 2px(8403) · 이름 아래 전투력 `max(1.4px,.1em)`(8408 · `small` = 전투력이지 서버가 아니다 · 자 표 정정 · 결정 274) · **me 행만** 서버 `max(1.2px,.1em)`(2355) · 도전 전투력 2px(2635). 폭은 `KeylineUi.json` · PlayMode `LeagueKeylineTests` 1 · 자리 초록 42 → 48 · 남은 자리는 PROGRESS 5회차 기록. **판정 ✅**(런 252 `LeagueKeylineTests` PASS · `screen_league`·`screen_league-challenge` 눈 확인: 순위·이름·전투력·내 행·도전 전투력 둘레에 검은 테 · 원작 042149·042228 과 같은 꼴) · **lock 반납 · 행 ⬜** — 남은 자리(Forge* 셋 = T87 뒤 · `Popups.cs@Btn` · `Battle/DamageNumbers.cs` · 자리 없는 다섯)는 PROGRESS 5회차 기록.

- 🔄 2026-09-13 22:1x 워커 A(sess-2005-27410) **6회차** = ⓑ 이어잡기: 4회차가 «제 회차를 따로» 로 남긴 `Battle/DamageNumbers.cs` — 정본 `.float-dmg .6px`(500) · `.dmg-kill 1px`(524) · `.dmg-hero .55px`(539)를 `KeylineUi.json` px 절로 두고, 숫자마다 재질을 안 복제하는 공유 재질 갈래(색 키마다 하나)에 px 갈래(D = W · T104 식)를 얹는다 — `UiKit.OutlinePx` 의 **재질용 오버로드**(글자 크기·글꼴을 인자로) 하나를 더한다(그 파일은 어느 lock 도 안 쥔다). 자: 새 `DamageKeylineTests`(스폰 뒤 재질 `_FaceDilate` > 0 · `_OutlineWidth` = 표 환산) + `check_keyline` KNOWN 에서 그 줄 삭제(자리 초록 48 → 51).
  → 런 257 `DamageKeylineTests` PASS(빨강 3 은 T87 불티·코어·링 자리) · `screen_main.png` 6배: «6.29m» 획마다 한 픽셀 어두운 테(.6px = 촬영 ½ 배율에서 .65px) · **lock 반납** — 남은 자리는 4회차 목록 그대로(T87 lock 뒤 셋 · `Popups.cs@Btn` · 자리 없는 다섯).
- 🔄 2026-09-13 22:4x 워커 O(sess-2140-18689) **7회차** = ⓑ 이어잡기: 공용 `Popups.cs@Btn` — 정본 `style.css` 8719 `.btn.btn.primary/on/equip/danger/sell { -webkit-text-stroke: var(--ol2) var(--pp-line) }`(모달 안 3548~3555: primary·on·equip = 파랑 · danger·sell = 빨강 · 클론 면 키 `pp_blue`·`pp_green`·`pp_red`) 를 `KeylineUi.json` 의 **면 키 → 폭표 키** 표(`btn_face`)로 두고 `PopupKit.Btn` 이 라벨을 세운 뒤 `Ring` 을 건다 · 회색(`pp_gray` 등 표에 없는 면)·비활성(정본 8725 `.disabled { -webkit-text-stroke: 0 }`)은 민글자 그대로 · 호출부에 `keylineKey` 를 열어 둔다(`""` = 끄기 · `.af-start`·`.fi-skip` 4px #000 은 `ForgeAutoPopup`·`ForgeInfoPopup` 이 T87 lock 이라 그 뒤에 호출부가 건다) · 자: 새 `PopupBtnKeylineTests`(면 셋 걸림 · 회색·비활성 0 · 리그 시트 발 «도전» 실물) · `check_keyline` 의 `Popups.cs@Btn` KNOWN 4줄은 `tools/check_keyline.py` 가 T129 lock 이라 **안 지운다**(자는 «KNOWN 인데 이제 있다» 를 알리되 rc 0 · 그 파일 반납 뒤 지운다 · `.dgd-btn.silver` 줄은 실물이 `DungeonPopups.Pill` 이라 표 자체가 틀렸다 — 같이 고칠 것).
  → 런 266 `PopupBtnKeylineTests` 2/2 PASS(런 262 의 내 빨강 둘은 자 쪽 — `outlineColor` 가 `Color32` · 행 «도전» 은 도전 팝업 안) · 남의 PlayMode 단언 0 흔들림(빨강 3 은 T87 자리) · `screen_league.png` ×3 발 «도전» · `screen_profile.png` ×2 «파워 랭킹»·«클랜 랭킹» 흰 글자 둘레 검은 테 눈 확인 · 자리 초록 51 → 55 · **lock 반납 · 행 ⬜** — 남은 자리: Forge* 셋 + `keylineKey` 호출부 셋(T87 lock 뒤) · KNOWN 4줄·dgd 자리 표 정정(T129 lock 뒤) · 자리 없는 다섯.
- 🔄 2026-09-14 02:4x 워커 O(sess-2140-18689) **9회차** = 자 정정(코드 한 줄도 그림은 안 바꾼다): 7회차가 공용 `Popups.cs@Btn` 에 키라인을 걸자 표에서 그 자리에 매인 **다른 규칙 셋**(`.af-start` 4px · `.fi-card .fi-skip` 4px · `.dgd-btn.silver` 2px)까지 «초록» 이 됐다 — 실물은 각각 `ForgeAutoPopup#af-start`(Btn 호출 · 표의 2px 만 걸림) · `ForgeInfoPopup#fi-skip`(회색 면이라 0) · `DungeonPopups@Pill`(Btn 이 아니라 0)이라 **거짓 초록**이다. 표를 실물 자리로 옮기고 `CREATE_CALL` 이 `Btn(` 생성도 알게 해 셋을 KNOWN(임자 · lock)으로 되돌린다(자리 초록 56 → 53 · KNOWN 8 → 11).
  → 런 300(77c366e) 전체 success · 자리 초록 53 · KNOWN 11 · 자기 검사 13칸 · **lock 반납 · 행 ⬜** — 남은 자리 KNOWN 11 은 각 lock(T114·T124·T94) 뒤 누구든(Btn 호출부는 12번째 인자 `keylineKey` 한 줄 · Pill 은 `Ring` 한 줄).


### T110 ✅ — 대장간 팝업 두 줄 버튼 라벨 넷이 이모지 글자다(«판매 / 🪙 +N» 둘 · «건너뛰기 / 💎 N» · «레벨 N 업그레이드 / 🪙 N») — 정본은 `<br>` 아래 줄에 아이콘 (Game·UI · T89·T99 뒤 · **T87 lock 뒤** · T99 에서 뗌)
- 자리(T89 4회차 전수 훑기 · T100 4회차 `label_risk` · `LABEL_KNOWN` 넷): `ForgeCraftPopup.cs:69·112` «판매\n🪙 +N» · `ForgeInfoPopup.cs:113·120` «건너뛰기\n💎 N»·«레벨 N 업그레이드\n🪙 N». 표(`TOAST_ICON`)에 있는 이모지지만 그냥 라벨 글자라 □ 다. 정본은 `.btn` 안에서 `<br>` 로 나누고 아래 줄에 `IconGen` 아이콘 + 수를 그린다(`ui.js` 의 해당 버튼).
- ⚠ **두 줄 버튼**: 가로 `UiKit.IconTextRow` 를 그대로 쓰면 두 줄이 한 줄로 눌린다 — 세로 칸(위: 글자 · 아래: 아이콘 줄)으로 감싸거나 `IconTextRow` 에 «줄바꿈이면 새 줄» 갈래를 더한다. T108(`ForgeSheet` «자동 ↻ / 🔒»)과 **같은 갈래**라 `UiKit` 에 세로 갈래를 하나 세우면 다섯이 같이 풀린다.
- 판정: `check_text_glyphs` 의 `LABEL_KNOWN` 에서 넷을 빼도 rc 0 + 해당 화면 PlayMode 초록 + `screen_craft-compare.png`·`screen_forge-info.png` 를 열어 버튼 아랫줄에 아이콘이 선 것을 눈으로(§1).
- 범위: `Assets/Scripts/Game/Ui/ForgeCraftPopup.cs`·`Ui/ForgeInfoPopup.cs`(T87 반납 뒤 열림) · `Ui/IconTextStack.cs`(1회차 제 파일 · 버튼 라벨 갈아 끼우는 갈래) · `tools/check_text_glyphs.py`(`LABEL_KNOWN` 넷 빼기) · `Assets/Tests/PlayMode/IconTextStackTests.cs`(2회차 단언 · `ForgeUiTests.cs` 는 T98 lock).
- 🔄 2026-09-13 워커 N(sess-0524-8791) 1회차: 세로 갈래를 **새 파일** `Ui/IconTextStack.cs`(`Build` = 줄바꿈마다 `UiKit.IconTextRow` 를 세로로 · `UiKit.cs` 는 T121 lock · 결정 275) + PlayMode `IconTextStackTests` 1 로 세웠다. 호출부 넷(+T108 의 `ForgeSheet` 자동 버튼)과 `LABEL_KNOWN` 빼기는 T87 lock 뒤 2회차(누구든). → 런 252 새 단언 초록 
- 🔄 2026-09-14 04:5x 워커 T(sess-0444-16036) 2회차: T87 33회차 반납으로 `ForgeCraftPopup`·`ForgeInfoPopup` 이 열렸다 — 호출부 넷(판매 둘 · 건너뛰기 · 업그레이드)을 `IconTextStack` 으로 갈아 끼우고(아이콘은 이모지 표 길 · 결정 313) `LABEL_KNOWN` 넷을 뺀다 · 단언은 `IconTextStackTests`(`ForgeUiTests.cs` 는 T98 lock). 2회차 코드 들어감: `IconTextStack.ReplaceLabel`(Btn 라벨 끄고 세로 갈래 · 굵게·키라인·Fit) · 호출부 넷(판매 확인 버튼도 정본 3865 대로 두 줄) · `check_text_glyphs` ROUTE/ROUTER 에 `IconTextStack.` 을 아이콘 길로 더하고 자기 검사 한 칸 · `IconTextStackTests` +1(세 팝업 버튼 넷 = 아랫줄 아이콘 하나 + 이모지 0) · dotnet 627/627. ✅ 는 CI 초록 + `screen_craft-compare.png`·`screen_forge-info.png` 눈 확인 뒤.
- ✅ 결론(2026-09-14 05:4x · 워커 T · sess-0444-16036): 런 328(f4af690 · 내 9eb7874 포함) EditMode 627/627 · PlayMode 169/169 · 빨강 0 · `IconTextStackTests` 2/2 PASS. 런 328 `screen_craft-compare.png` — 빨강 «판매» 버튼 아랫줄이 **코인 아이콘 + «+29»** · `screen_forge-info.png` — 회색 «건너뛰기» 아랫줄이 **보석 아이콘 + «10»** · □ 없음(업그레이드 버튼은 캡처가 «진행 중» 상태라 화면엔 안 나오고 단언으로만 잰다). `LABEL_KNOWN` 넷을 뺀 채 `check_text_glyphs` rc 0. lock 반납. 후속(누구든): 정본 `.btn small .ico` 1.55em 과 `.fi-skip-gem` 빨강은 `catalog.json` 키라 T33 이 합칠 때(결정 313).

### T111 ✅ — 장비 상세 카드가 **가운데**에 뜬다: 정본은 하단 앵커(카드 바닥 77.15%H)이고 폭도 공용 74% 가 아니라 `.gd-card` 70% 다 (Game·UI · T19 뒤 · **T87 lock(catalog.json) 뒤** · T28 23회차 실측)
- 실측(2026-09-13 · T28 23회차 · 워커 M · 런 214 `screen_gear-detail.png` ↔ 원작 `shot-043244.png` · 같은 픽셀 코드로 흰 카드를 쟀다):
  - 세로 — 클론 카드 바닥 **59.2%H** ↔ 원작 **77.0%H** = **−17.8%p**(카드가 장비 시트 위가 아니라 세계 한가운데 떠 있다).
  - 가로 — 클론 **72.6%W** ↔ 원작 **68.2%W** = **+4.4%p**.
- 정본: `css/style.css` 1737~1738 `#gear-detail-modal { align-items: flex-end; padding-bottom: calc(var(--app-h) * .223 - 1.7rem); }` · `#gear-detail-modal .gd-card { width: 70%; }` — 그 위 주석이 원작 실측을 «흰 카드 바운딩 박스 y 59.64%H → 하단 77.15%H · 폭 ≈70%» 로 못 박아 두었고, «✕ 가 카드 아래로 반쯤 튀어나오므로 하단 앵커에서 그 몫을 빼 둔다» 까지 적혀 있다.
- 클론(원인): `Assets/Scripts/Game/Ui/GearDetailPopup.cs` 33~35 는 폭을 **공용 `modal_wide_w`(0.74)** 로 잡고 `PopupKit.Card` 기본 자리(가운데)에 세운다 — 하단 앵커도, `.gd-card` 전용 폭도 없다.
- 무엇을 한다: ⓐ 카탈로그에 `gd_card_w`(0.70)와 하단 띄움 키(`gd_bottom` = `.223·H − 1.7rem` 의 유니티 몫 · **T87 lock 뒤**) ⓑ `GearDetailPopup.Render` 가 카드를 **아래 앵커**로 세우고 그 키를 쓴다(수치는 코드에 박지 않는다 · §1) ⓒ ✕ 가 카드 아래로 반쯤 걸치는 지금 모습은 정본대로이므로 건드리지 않는다.
- 판정: ⓐ PlayMode 픽셀/레이아웃 단언 — 카드 바닥이 앱 높이의 77±1.5%, 폭이 70±1%W ⓑ 다음 런 `screen_gear-detail.png` 를 열어 카드가 **장비 시트 바로 위**에 앉았는지 눈 확인(원작 `shot-043244` 와 나란히).
- 범위: `Assets/Scripts/Game/Ui/GearDetailPopup.cs` · `Assets/Forge/Resources/GearDetailUi.json`(새 · `catalog.json` 은 T87 lock → T65 꼴 · 결정 249) · `Assets/Tests/PlayMode/GearDetailTests.cs`(새 · `ForgeUiTests.cs` 는 T87 lock → 자기 파일 · T68 꼴). 원래 적었던 `catalog.json` 키 둘·`gen_ui_catalog.py`·`ForgeUiTests.cs` 한 칸은 T33 이 카탈로그로 합칠 때 옮긴다.
- 🔄 2026-09-13 워커 T(sess-1644-31925): T87 lock 은 `catalog.json`·`ForgeUiTests.cs` 만 막고 `GearDetailPopup.cs` 는 자유라, 보고함(워커 O)의 정체를 풀 길로 «자기 파일» 꼴을 택했다(결정 249). 키 둘 = 정본 1737~1738: `card_w` .70 · `bottom_h` .223(카드 바닥이 앱 바닥에서 뜨는 몫 — 정본 `padding-bottom` 의 `−1.7rem` 은 `.idet-wrap` 이 ✕ 반쪽을 끌어안는 몫이라 카드 자체는 .223·H 에 앉는다 · 그 CSS 주석 «카드 하단 77.70%H 유지»). **1회차**: `GearDetailStyle`(표 로더) + `Render` 가 카드를 앱 바닥에서 `bottom_h` 위에 바닥 피벗으로 앉힌다(높이는 ContentSizeFitter 가 위로 키운다) + `GearDetailTests` 1 — 세계 좌표로 앱 상자 대비 폭·바닥 비율을 잰다. ✅ 는 CI `GearDetailTests` 초록 + 다음 런 `screen_gear-detail.png` 를 열어 카드가 장비 시트 바로 위에 앉은 것을 본 뒤.
- ✅ 2026-09-13 17:4x 워커 T(sess-1744-24466): CI 런 223(a43f87a · 내 437569a 포함) PlayMode 114/114 · `GearDetailTests` PASS. 런 223 `screen_gear-detail.png` 을 열어 봤다 — 흰 카드가 장비 시트 위(카드 위 58.8%H · 바닥 ≈77.6%H · 폭 ≈70%W)에 앉고 x-btn 이 아래턱에 반쯤 걸친다 · 원작 `shot-043244` 와 같은 띠. 카드 위 %H 는 `ui_score --read` 밴드6 시작 58.8 ↔ 정본 CSS 주석의 실측 59.64. lock 반납.

### T112 ✅ — 두부 막이가 «글꼴 파일에 있는가» 가 아니라 «이미 구워졌는가» 를 묻고 있었다: `HasCharacter(c, false, false)` 의 셋째 인자 (Tests · T100 뒤 · 런 216 실측)
- 증상(런 216 · cc88bd2): PlayMode 빨강 `TextSizeGateTests.카탈로그_글꼴이_한글을_직접_쥔다` — «카탈로그 글꼴에 **'ㅋ'** 가 없다». 그런데 같은 커밋에서 파이썬 자 `tools/check_text_glyphs.py`(글꼴 cmap 을 직접 읽는다)는 **rc 0** 이다. **두 자가 어긋난 것 자체가 증거다.**
- 사실 확인: `Assets/Fonts/NotoSansKR-Forge.ttf` 의 cmap 을 직접 읽으면 `ㅋ`(U+314B)·`ㅠ`(U+3160)·`가`·`대` 가 **전부 있다**(코드포인트 2,061 · T100 2회차 `d3b5254` 가 U+3130~318F 를 넣은 그대로). 글꼴은 멀쩡하다.
- 진짜 원인: `UiFont.Primary` 는 `TMP_FontAsset.CreateFontAsset(cat.font)` — **Dynamic** 애셋이라 글자표(`characterLookupTable`)가 처음엔 비어 있고 **그려진 글자만** 채워진다. TMP 의 `HasCharacter(char c, bool searchFallbacks, bool tryAddCharacter)` 는 두 인자가 **둘 다 false** 면 그 표만 본다 — 즉 «글꼴에 있는가» 가 아니라 «**이 순간까지 아틀라스에 구워졌는가**» 를 묻는다. 그래서 이 단언은 **앞 테스트가 무엇을 그렸느냐**에 따라 초록·빨강이 갈린다(런 195·209 초록 → 런 216 빨강 · 그 사이 글꼴도 카탈로그도 안 바뀌었다).
- T100 2회차가 `(c, true, true)` → `(c, false, false)` 로 바꾼 것은 **폴백을 빼려던 것**이 맞다(배포판에 OS 폴백이 없으니 옳다). 다만 같이 꺼 버린 셋째 인자가 «원본 글꼴을 찾아본다» 는 뜻이라 질문 자체가 바뀌었다.
- 무엇을 한다: 두 단언을 **`HasCharacter(c, /*searchFallbacks*/ false, /*tryAddCharacter*/ true)`** 로 — 폴백은 계속 빼고(판정 기준 유지), 원본 글꼴은 보게 한다. 셋째 인자를 켜면 **글꼴에 있으면 true · 없으면 false** 라 «없는 글자는 빨강» 이 그대로 살아 있다(더 느슨해지는 것은 «있는데 아직 안 구워진» 경우뿐이고 그것은 두부가 아니다).
- 두 인자의 뜻을 주석으로 코드에 박아 같은 실수가 다시 안 나게 한다.
- 🔶 **선점이 겹쳤다(2026-09-13 16:4x · 결정 248)**: 런 216 빨강 자리(`카탈로그_글꼴이…`)는 워커 C 가 T100 갈래로 **먼저 push** 했다(`13a64a5` · 결정 247). 규약대로 그 줄은 C 것을 살렸고, 이 번호는 **C 가 안 덮은 반쪽 = 같은 파일 73행 `화면_한글이…`** 로 좁혔다(번호를 ✂ 로 태우면 그 자리는 아무도 안 본다).
- 판정: 다음 유니티 런에서 `TextSizeGateTests` 넷 전부 초록 · 파이썬 자와 답이 같아진다. → **런 222(`a43f87a`) EditMode 553/553 · PlayMode 114/114 · 빨강 0 · 넷 전부 PASS 로 닫혔다**(2026-09-13 · 워커 K).
- 범위: `Assets/Tests/PlayMode/TextSizeGateTests.cs`(73행 한 줄 + 주석) · `docs/ROUTINE.md`(§2 등재) · `docs/PROGRESS.md`.

### T113 ✅ — 제작 비교 팝업도 **가운데**에 뜬다: 정본은 하단 앵커(카드 바닥 86.9%H)이고 폭도 공용 74% 가 아니라 68.8% 다 — T111 과 같은 병, 다른 팝업 (Game·UI · **T111 뒤**(같은 길을 쓴다) · T28 24회차 실측)
- 실측(2026-09-13 · T28 24회차 · 워커 M · 런 219 `screen_craft-compare.png` ↔ 원작 `shot-043224.png`): 흰 카드가 클론 y **26.3%~72.9%H** ↔ 원작 **44.8%~86.8%H** — **바닥이 −14%p**(카드가 장비 시트 위가 아니라 세계 한가운데 떠 있다) · 폭 클론 **72.6%W** ↔ 원작 **67.5%W**.
- 정본: `css/style.css` 1742 `#craft-modal { align-items: flex-end; padding-bottom: calc(var(--tabbar-h) + 1.65rem); }` · 1748 `#craft-modal .modal-card.wide { width: 68.8%; }` — 그 위 주석이 «원본(043224) 카드 하단은 **86.90%H**» 와 «흰 본체 x 16.53~83.86%W = **67.33%W**(DOM 68.8%)» 를 실측으로 적어 두었다.
- 클론(원인): `Assets/Scripts/Game/Ui/ForgeCraftPopup.cs` 54·56 이 폭을 **공용 `modal_wide_w`(0.74)** 로 잡고 `PopupKit.Card` 기본 자리(가운데)에 세운다 — T111 이 장비 상세에서 고친 것과 **같은 자리, 같은 병**이다(그 팝업은 `GearDetailUi.json` + 하단 앵커로 닫혔다).
- 무엇을 한다: T111 이 낸 길을 그대로 따른다 — ⓐ 이 팝업의 배치표(정본 두 값: 카드 폭 0.688 · 하단 띄움 = 탭바 높이 + 1.65rem)를 **자기 파일**(`Assets/Forge/Resources/CraftUi.json` · `catalog.json` 은 T87 lock 이라 비켜 간다) ⓑ `ForgeCraftPopup` 이 카드를 **아래 앵커**로 세우고 그 표를 읽는다(수치는 코드에 박지 않는다 · §1) ⓒ 카드 안의 두 줄 버튼(판매액·기존 교체)은 **정본대로**이므로 손대지 않는다(T28 15회차 · 자 주석).
- 판정: ⓐ PlayMode 단언 — 카드 바닥이 앱 높이의 86.9±1.5%, 폭 68.8±1%W ⓑ 다음 런 `screen_craft-compare.png` 를 원작 `shot-043224` 와 나란히 놓고 눈 확인.
- 범위: `Assets/Scripts/Game/Ui/ForgeCraftPopup.cs` · `Assets/Forge/Resources/CraftUi.json`(새 · `gen_meta.py`) · `Assets/Tests/PlayMode/`(새 파일 하나 · `ForgeUiTests.cs` 는 T87 lock 이라 비켜 간다).
- ⚠ 순서(워커 D · 2026-09-13 17:5x · lock 안 잡음): `ForgeCraftPopup.cs` 는 T87 표 «범위» 의 `Ui/Forge*` 글로브에 든다 — `check_claim_scope` 가 같은 파일 쌍으로 알리고 규약(«같은 파일이면 뒤 번호가 기다린다»)대로 **T87 lock 뒤**(T110 과 같은 처지). `CraftUi.json`·새 테스트 파일은 겹치지 않으니 표·단언은 먼저 세워도 되지만 팝업 코드는 T87 이 반납한 뒤.

### T114 ✅ — 장비 상세 카드 머리에 **원작에 없는 «확률 0.0000%» 줄**이 하나 더 있다 (Game·UI · T19·T57·T93 뒤 · 임자 없음 · 검수 Q 등재)
- 실측(2026-09-13 18:0x · 검수 Q · **런 223**(a43f87a · EditMode 553/553 · PlayMode 114/114 · 빨강 0)의 `screen_forge-detail.png` 를 Read 로 열어 본 것 — **전부 초록인 런에서 눈에 보인다**): 상세 카드 머리가 **석 줄**이다 — «[원시적] 몽둥이» · «12 피해» · **«확률 0.0000%»**.
- **정본은 두 줄뿐이다**: `ui.js:2242~2248` 의 `idet-head` = `idet-icon` + `idet-title`(`idet-name` «[원시적] 가죽» + `idet-main` «2k 체력») — **확률 줄이 없다**. 원작 샷 `ref/screens/shot-042931.png` 도 카드 머리가 이름·스탯 두 줄이고 그 아래 바로 «장비은(는) 아래 목록에서…» 리드가 온다.
- **클론이 더 넣은 자리**: `Assets/Scripts/Game/Ui/ForgeInfoPopup.cs:280` — `UiKit.Text(head, "idet-pct", TextKind.Sub, "확률 " + pct.ToString("0.0000", …) + "%", …)`. §1 «원작에 없는 것을 넣지 않는다» 다.
- **값이 왜 큰가**: `screen_forge-detail` 은 원작 대조 채점에서 **30장 중 꼴찌 1.2/10** 이다(런 223 · 2등 `pet-upgrade` 2.2). 머리에 줄이 하나 더 들어가면 그 아래 밴드가 전부 아래로 밀려 비율이 통째로 어긋난다 — 한 줄을 빼는 것으로 그 화면의 밴드 짝이 여럿 살아날 자리다.
- 할 일: `idet-pct` 줄을 뺀다(정본이 그 수를 상세 카드에 안 보인다 — «모든 장비의 목록» 격자 셀의 % 는 그대로 둔다 · 그 자리는 정본에도 있다). 뺀 뒤 `ui_score --score --only forge-detail` 로 점수가 오르는 것을 본다.
- 판정: `ui_score --only forge-detail` 이 **런 223 의 1.2 보다 오른다** + 워커가 PNG 를 Read 로 열어 카드 머리가 «이름 / 스탯» 두 줄인 것을 본 기록(§1) + PlayMode 빨강 0 · CI 유니티 잡 초록.
- 범위: `Assets/Scripts/Game/Ui/ForgeInfoPopup.cs`(그 한 줄) · `Assets/Tests/PlayMode/ForgeUiTests.cs`(머리 줄 수 단언 한 칸).
- **왜 여섯 시간째 임자가 없나(2026-09-14 · T28 32회차 · 워커 M)**: 게을러서가 아니라 **막혀 있다** — 고칠 자리 `Ui/ForgeInfoPopup.cs` 가 **T87 의 범위(`Assets/Scripts/Game/Ui/Forge*`)** 안이고 그 lock 이 살아 있다(32회차 시점까지 연속 갱신 중). 규약대로 T87 lock 이 풀린 뒤에 잡으면 되고, 그 전에 할 수 있는 것은 **PlayMode 단언을 자기 파일에 미리 세워 두는 것**뿐이다(T113·T122 가 쓴 길: `ForgeUiTests.cs` 도 T87 것이라 새 파일로).
- ⚠ 순서(워커 D · 2026-09-13 18:5x · lock 안 잡음): 두 파일 다 T87 표 «범위»(`Ui/Forge*` · `ForgeUiTests.cs`)에 든다 — 규약(«같은 파일이면 뒤 번호가 기다린다»)대로 **T87 lock 뒤**(T110·T113 과 같은 처지). 한 줄 빼기라 T87 이 반납하면 바로 닫힌다.

### T115 ✅ — 버튼 글자 넘침 막이가 **두 자리만** 본다: 펫 업그레이드의 «업그레이드» 가 버튼 밖으로 삐져나온 채 초록으로 지나갔다 (검증+Game·UI · T90 뒤 · **T102 lock 파일은 그 뒤** · T28 25회차가 PNG 로 잡음)
- 실측(2026-09-13 · T28 25회차 · 워커 M · 런 224 `screen_pet-upgrade.png` 4배 확대): «합칠 펫 선택» 줄의 은색 버튼에서 **«업» 이 버튼 왼쪽 밖, «드» 가 오른쪽 밖**에 찍힌다 — 둥근 사각 테두리가 글자 한가운데를 가로지른다. 원작 `shot-042503` 은 같은 폭 버튼 안에 «업그레이드» 다섯 자가 다 들어간다.
- 정본이 어떻게 넣는가: `css/style.css` 4363 `.petup-selrow .btn.silver { flex: none; width: calc(var(--app-w) * .1525); font-size: .78rem; letter-spacing: -.02em; white-space: nowrap; padding: .35rem 0; }` — **폭을 못 박는 대신 글자를 줄이고 자간을 좁힌다**(그 위 주석: 원본 77px = 15.25%W).
- 클론(원인): `Assets/Scripts/Game/Ui/PetUpgradePopup.cs` 155~158 이 폭만 `petup_sel_btn_w`(0.1525)로 가져오고, 라벨은 `PetSkillKit.PaperButton` 이 **공용 `TextKind.Button` 크기 그대로** 세운다 — `PetSkillUi.json` 에 이 버튼의 **글자 크기·자간 키가 없다**(`petup_sel_btn_w`·`_r_rem` 둘뿐).
- **막이가 왜 못 봤나**: T90 이 세운 넘침 단언(`Assets/Tests/PlayMode/PetUiTests.cs` 221~226)은 **스킬 서브시트의 버튼 둘**(«모두 업그레이드»·«빠른 장착»)만 잰다. 같은 파일의 펫 업그레이드 절은 그 물음을 한 번도 안 던진다 — 그래서 «글자가 칸 밖으로» 가 초록으로 지나간다(T107 과 같은 꼴의 **자 구멍**).
- 무엇을 한다: ⓐ 넘침 단언을 **그 화면의 활성 버튼 전부**로 넓힌다(라벨 preferred 폭 + 패딩 ≤ 버튼 폭 · 일부러 줄이는 자리는 표에 예외로 적고 이유를 단다) ⓑ 이 버튼에 정본 값을 준다 — `PetSkillUi.json` 에 글자 크기·자간 키를 더하고 `PaperButton` 이 그것을 받도록(수치는 코드에 박지 않는다 · §1) ⓒ 넓힌 자가 **고치기 전 rc 1 · 고친 뒤 rc 0** 임을 같은 런에서 보인다.
- 판정: PlayMode 넘침 단언이 pet-upgrade 를 포함해 초록 + 다음 런 `screen_pet-upgrade.png` 4배 확대에서 «업그레이드» 다섯 자가 테두리 **안**에 있다(원작 `shot-042503` 과 나란히).
- 범위: `Assets/Tests/PlayMode/PetUiTests.cs`(**T102 lock 뒤**) · `Assets/Scripts/Game/Ui/PetSkillKit.cs` · `Assets/Scripts/Game/Ui/PetUpgradePopup.cs` · `Assets/Forge/Resources/PetSkillUi.json`.
### T117 ✅ — 장비를 팔면 코인이 튀는 연출(`coinBurst`)이 통째로 없다 — 정본은 모루 위에서 3~10개가 격자 착지점으로 날아가 각자 «합÷개수» 를 단다 (Game·UI · **T87 lock 뒤**(`Ui/Forge*`) · T33 3회차 실측)
- 정본: `js/ui.js` 3478~3560 `coinBurst(total)`. 규칙 그대로 — 개수 `clamp(3 + round(log10(max(1,총액))·1.7), 3, 10)` · **라벨은 전부 `max(1, round(총액/개수))` 같은 값**(사용자 지시 `sell-coin-split-rising`) · 착지점은 줄×칸 격자(줄 1~3 · 칸 간격 4.6rem · 줄 간격 2.4rem · 첫 줄 1.2rem · i 를 줄에 라운드로빈) · 떠오름 −58~−104px · 지연 `i·26 + rand(0,24)`ms · 출발점은 모루 버튼 중심 가로 · 위에서 0.4h.
- 정본 가드(`coin-burst-over-modal` QA 실측): **열린 `.modal` 이나 `.panel.open` 이 있으면 연출도 소리도 통째로 생략**한다 — 모루가 가려진 상태라 «소리만 나는 반쪽» 이 남지 않게.
- 호출 네 자리: `ui.js` 1711(묶음 판매) · 1756(오토포지 배치) · 1841·1890(일괄) · 3901(`showCraftModal` 의 [판매]).
- 클론: **호출 0**. `Assets/Scripts/Game/Ui/ForgeHost.cs` 530 `DoResolveCraft` 는 `GearSys.Sell(item)` 만 하고 연출이 없다(코인 숫자는 상단바가 조용히 바뀔 뿐이다).
- 판정: ⓐ PlayMode — 판매 뒤 코인 조각 개수가 총액 눈금대로 3~10 · 라벨이 전부 같은 값 · 팝업이 열려 있으면 조각 0 ⓑ 다음 런 `screen_*.png` 눈 확인.
- 범위: `Assets/Scripts/Game/Ui/CoinBurst.cs`(새) · `Assets/Forge/Resources/CoinBurstUi.json`(새 · 수치표 · `catalog.json` 은 T87 lock) · `Assets/Scripts/Game/Ui/ForgeHost.cs`(호출 · **T87 lock 뒤**) · `Assets/Tests/PlayMode/`(새 파일 · `ForgeUiTests.cs` 는 T87 lock).
- ✅ 1회차(2026-09-13 · 워커 R · 결정 260 · 런 229 초록 · lock 반납): 연출·표·자를 세웠다 — Core `CoinBurstRules`(셈 · EditMode 6) · `Resources/CoinBurstUi.json`(수치·키프레임·색·문구) · `Ui/CoinBurst.cs`(층 = 장비 시트 위 형제 · 정본 가드 · 벽시계) · PlayMode `CoinBurstTests` 2(직접 호출). **남은 것 = 호출 한 줄**(`ForgeHost.DoResolveCraft` 의 Sell 뒤 `CoinBurst.Play(가격)` + 오토포지 갈래 · T87 lock 뒤 · 누구든) + 실판매 단언.

- 🔄 2026-09-14 04:0x 워커 P(sess-0154-10159) **2회차**: T87 lock 반납 뒤 — 정본 호출 다섯 자리(3901 수동 [판매] · 1756 autoSeqStep · 1711 purge · 1841 craftUntilMatches · 1890 drainAutoBatch · 뒤 셋은 배치당 한 번)를 `ForgeHost` 에 그대로 · `resolvePendingCraft` 자리는 정본도 안 부른다 · `Covered()` 가 `modals` 아이 수로 보던 것을 «열린 목록» 으로(Hide 의 Destroy 가 프레임 끝까지 남아 [판매] 프레임이 가려짐으로 읽혔다 · 결정 310) · PlayMode 실판매 1. 3회차: 런 318 의 내 단언(라벨을 착지 전에 셈)을 고치고 보류 카드도 모루 자리로(정본 `anvil-btn held-slot` · 결정 314). **✅ 런 329 전체 초록(`CoinBurstTests` 3/3) + `screen_main` 눈 확인(시트 위 코인 잔재 0) · lock 반납**.

### T118 ✅ — 장비 교체 «던져내기» 연출(`equip-swap-throwout`)이 없다 — 옛 장비가 회전하며 바닥에 눕는 그 연출 · 소리 둘(`equipToss`·`equipDrop`)은 구워만 놓고 **호출이 0** (Game·UI · **T87 lock 뒤**(`Ui/Forge*`) · T33 3회차 실측)
- 정본: `js/ui.js` 3329 `grabEquipSwapFx(slot)`(렌더가 칸을 갈아끼우기 **전에** 옛 타일 자리를 붙잡는다) · 3362~3470 `playEquipSwapFx(fx)`. 옛 장비는 **화면 바깥쪽**으로 날아가고 회전은 정수 바퀴(360°·720°) + 기울기 8~22° 로 끝난다(아무 각도로 멈추면 AABB 가 √2배로 부풀어 자리 계산이 어긋난다) · 착지 반경은 `(W·sk·1.12·cosθ + H·sk·1.05·sinθ)/2 + 2` · 팝업 카드가 열려 있으면 착지점을 카드 **옆 빈 띠**로 옮긴다 · `prefers-reduced-motion` 이면 통째로 생략.
- 소리: `SFX.equipToss()`(3428 · 던질 때) · `SFX.equipDrop()`(3439 · 착지) · `SFX.equipSnap()`(3462 · 새 장비가 칸에 붙을 때) 셋이 **이 함수 안에** 있다.
- 클론: `playEquipSwapFx` 대응 0 · `Sfx.EquipToss()`·`Sfx.EquipDrop()` 은 T30 이 구워 뒀지만 **부르는 곳이 없다**(`Assets/Scripts` 전수 · 래퍼 정의 제외) · `ForgeHost.DoResolveCraft` 529 는 `equipSnap` 만 운다.
- 판정: ⓐ PlayMode — 빈 부위에 처음 끼우면 연출 0(«교체» 가 아니다) · 교체면 옛 타일이 바깥쪽으로 나가 바닥에 눕고 소리 셋이 순서대로 · 팝업이 열려 있으면 착지점이 카드 밖 ⓑ 다음 런 PNG 눈 확인.
- 범위: `Assets/Scripts/Core/Ui/EquipSwapRules.cs`(새 · 셈·키프레임) · `Assets/Tests/EditMode/EquipSwapRulesTests.cs`(새) · `Assets/Scripts/Game/Ui/EquipSwapFx.cs`(새) · `Assets/Forge/Resources/EquipSwapUi.json`(새 · 수치표) · `Assets/Tests/PlayMode/EquipSwapTests.cs`(새) · `Assets/Scripts/Game/Ui/ForgeHost.cs`(호출 두 줄 · **T87 lock 뒤 · 2회차**).
- 1회차 끝 2026-09-13 워커 P(sess-1854-15611 · 결정 263): T117 꼴로 셈(Core `EquipSwapRules`)·연출(`EquipSwapFx` · 층 `modals` 다음 형제)·표(`EquipSwapUi.json`)·테스트(EditMode 6 · PlayMode 2)를 세웠다 — 런 234 `EquipSwapTests` 2/2 PASS. **2회차(누구든 · T87 lock 뒤)**: `ForgeHost.DoResolveCraft` 에 정본 3899·3904 자리 두 줄(`var fx = (mode == "equip" && prev != null) ? EquipSwapFx.Grab(item.Slot) : null;` 장착 전 · `EquipSwapFx.Play(fx);` 렌더 뒤) + 실장착 PlayMode 단언 + `tools/check_sfx_calls.py` KNOWN 의 `equipToss`·`equipDrop` 두 줄 빼기 + PNG 눈 확인.

- 🔄 2026-09-14 02:0x 워커 P(sess-0154-10159) **2회차**: T87 lock 반납(8d6e5fe) 직후 — `ForgeHost.DoResolveCraft` 에 Grab(장착 전)/Play(렌더 뒤) 두 줄 · 바로 울리던 `equipSnap` 을 뺐다(정본 3462 한 곳 · 연출 안 130ms · 결정 296) · `EquipSwapFx` 는 Grab 때 옛 타일을 복제해 두고(Ghost) 다음 프레임에 새 칸으로 빈 소켓을 옮긴다(Rehollow) · `check_sfx_calls` KNOWN 둘 제거 · PlayMode 실장착 1. 3회차: 런 292 의 내 단언 하나가 프레임 길이에 매여 빨갛던 것을 순서 단언으로(코드 0줄). **✅ 런 308 `EquipSwapTests` 3/3 + `screen_main`·`screen_craft-compare` 눈 확인(칸이 새 장비로 서고 층 잔재 0) · lock 반납**.

### T119 ✅ — 정본 소리 24종 중 **호출이 0** 인 둘: 제작 공개 `craftReveal` · 폭풍 충전 `stormCrackle` — 그리고 그것을 다시 놓치지 않을 자 (Game·소리 · T30 뒤 · T33 3회차 실측)
- 실측(2026-09-13 · T33 3회차): `SfxRecipes.Names` 24종을 `Assets/Scripts` 전수(래퍼 `Core/Audio`·`Game/Audio` 제외)와 대조하니 호출 0 이 넷 — `craftReveal` · `equipToss` · `equipDrop` · `stormCrackle`. 가운데 둘은 T118 가 갚는다(같은 함수 안에 있다).
- `craftReveal`: 정본 `ui.js` 1938 `showCraftReveal` · 1976 `showCraftBatch` 에서 `SFX.craftReveal(AGES.indexOf(item.age))` — **나이 인덱스가 인자**다(등급이 높을수록 다른 소리). 클론 `ForgeCraftPopup`/`ForgeHost` 는 `craft`·`anvilHit`·`equipSnap` 만 운다.
- `stormCrackle`: 정본 `scene3d.js` 14101 — 폭풍 스킬 **첫 발 직전에 한 번**(«구름이 차올랐다» 를 소리로). 클론 `SkillFxDirector` 는 `StormRumble`·`StormStrike` 만 부른다.
- 자: `tools/check_sfx_calls.py`(새) — `SfxRecipes.Names` 중 게임 코드 호출이 0인 이름을 rc 1 로 알린다(래퍼 정의·주석은 호출이 아니다). CI `datasync` 잡에 한 스텝. 지금 돌리면 넷이 잡히므로 **T117·T118 이 둘을 갚은 뒤** 자를 빨강으로 켠다(그 전엔 `--report` 로 알리기만).
- 범위: `Assets/Scripts/Game/Ui/ForgeCraftPopup.cs`(**T87 lock 뒤**) · `Assets/Scripts/Game/SkillFx/SkillFxDirector.cs` · `tools/check_sfx_calls.py`(새) · `.github/workflows/ci.yml`(datasync 한 스텝) · `Assets/Tests/EditMode/`(자 자기 검사).

### T120 ✅ — 대장간 소리 셋이 통째로 무음(훅 `ForgeHost.Sfx` 를 아무도 안 꽂는다) · `levelUp` 은 호출 자체가 없다 (Game·소리 · T30·T119 뒤 · **T87 lock 뒤**)
- 실측(2026-09-13 · T119 1회차 · 워커 E): `ForgeHost` 는 `PlaySfx("craft")`(175) · `PlaySfx("equipSnap")`(529) · `PlaySfx("anvilHit")`(762) 를 부르는데 그 훅 `public Action<string> Sfx`(81)에 대입하는 코드가 `Assets/Scripts`·`Assets/Tests` 어디에도 **없다** → 세 소리 모두 무음(`anvilHit` 은 스킬 연출 쪽 `Sfx.AnvilHit` 으로만 운다). 소리는 PNG 에도 테스트에도 안 드러나 아무도 못 봤다.
- `levelUp`: 클론에 호출도 훅도 0. 정본은 셋 — `forge.js` 303(대장간 레벨업) · `techtree.js` 382(연구 완료) · `ui.js` 4710(던전 클리어 «클리어 팬페어» 주석).
- 무엇을 한다: ⓐ `ForgeHost` 가 서는 자리(`Boot`/`Create`)에서 훅을 꽂는다 — 이름 문자열 → `Sfx` 래퍼(정본 `SFX[name]()` 과 같은 짝) · 훅을 남겨 둔 뜻(테스트가 갈아끼운다)을 지키려면 **기본값만 채운다**(이미 꽂혀 있으면 안 덮는다) ⓑ `levelUp` 세 자리를 정본대로 잇는다(대장간 레벨업 · 연구 완료 · 던전 클리어) ⓒ PlayMode 단언: 대장간 훅이 null 이 아니고, 제작을 돌리면 그 이름이 실제로 울린다(T30 `AudioSmokeTests` 의 길) ⓓ `check_sfx_calls.KNOWN` 에서 `craft`·`equipSnap`·`levelUp` 을 뺀다.
- 판정: `python3 tools/check_sfx_calls.py` 가 셋을 `KNOWN` 없이 통과 + PlayMode 초록 + (소리는 PNG 가 없으니) 완료 기록에 «어느 자리에서 어느 이름이 우는가» 표 한 줄.
- 범위: `Assets/Scripts/Game/Ui/ForgeHost.cs`(**T87 뒤**) · `Assets/Scripts/Game/Ui/TechPopups.cs` · `Ui/DungeonSheet.cs` · `Assets/Tests/PlayMode/AudioSmokeTests.cs` · `tools/check_sfx_calls.py`.
- 🔄 2026-09-14 05:4x 워커 N(sess-0524-8791) 3회차 = ⓐ: `ForgeHost` 타격 루프가 셋째에 `anvilHitStrong` 을 부르고 `HostSfx.Table` 이 `Sfx.AnvilHit(true)` 로 잇는다(훅 서명은 그대로 · 결정 315) — `ForgeHost.cs` 는 T117 lock 이 90분을 넘겨 열었다(내 자리는 한 줄) · `AudioSmokeTests` 이름 다섯 + 새 순서 시험(약·약·강). → **런 334 셋 PASS · ✅ · lock 반납**(빨강 17 은 T142 자리).
- ✅ 결론: 대장간 소리는 `HostSfx` 가 밖에서 꽂아 `craft`·`equipSnap`·`anvilHit`(약)·`anvilHitStrong`(셋째 타격)·`levelUp`(대장간 레벨업·연구 완료·던전 클리어) 전부 실제로 운다 — `check_sfx_calls` 가 KNOWN 없이 지킨다.

### T121 ✅ — 글꼴 애셋의 SDF 패딩이 정본 최대 키라인(`.2em`)을 못 담는다: 링이 글자 여백을 다 먹어 **회색 사각 띠**가 된다 (Game·UI · T53 글꼴 굽기 · T109 2·3회차 실측 등재)
- 실측(2026-09-13 · T109 3회차 · 워커 I · 런 229 `screen_offline.png` 5배): 합계 줄이 정본대로 **흰 칠 + 어두운 링**으로 바뀌었는데(✔) 글자 뒤에 **연회색 사각 띠**가 남는다.
- 셈으로 확인: T104 `OutlineSdf` 의 «이 애셋이 낼 수 있는 최대 바깥 띠» `UnitPx = R × G × fontSize / samplingPointSize` 는 기본 굽기값(G 10 · R .9 · 90pt)에서 **글자 36px → 3.60px** 다. 정본 `.offline-total { -webkit-text-stroke-width: .2em }` 은 36px 글자에서 획 **7.2px** = 바깥 띠 **3.6px** 요청이라 `W = 7.2 / (2×3.60) = 1.00` — **정확히 천장**이다. W=1·Dilate=1 이면 외곽선이 SDF 여백 전체를 덮어 링이 아니라 «면» 이 된다.
- 정본 값이 틀린 게 아니다: `style.css` 292 주석의 원본 실측이 «검정획 3.59px» 이고 우리가 요청하는 바깥 띠 3.6px 과 같다 — 모자란 것은 **글꼴 애셋의 패딩**이다.
- 무엇을 한다: T53 이 구운 글꼴 애셋의 SDF 패딩(또는 샘플링 포인트 크기)을 키워 `UnitPx` 가 정본 최대 획의 절반보다 넉넉하게 만든다(적어도 36px 글자에서 5px 이상). 굽는 자리가 코드면 그 값을, 에디터 애셋이면 재굽기 절차를 `docs/assets-map.md` 에 적는다. 다른 자리는 이미 여유가 있다(제목 `.11em` W .55 · `2px` W .28 · `.5px` W .07).
- 판정: `screen_offline.png` 5배에서 **회색 사각 띠가 사라지고** 원작 `shot-042110` 처럼 링만 남는다(워커가 눈으로 · §1) · `OutlinePx` 의 «Clipped» 경고 0 · PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/UiKit.cs`(`UiFont.Build` 굽기 값 · T53 자리) · `Assets/Forge/Resources/UiFontBake.json`(새 · 샘플링·패딩 표) · `Assets/Tests/PlayMode/FontBakeTests.cs`(새) · `tools/dotnet/Stubs/TMPro.cs`(굽기 오버로드 서명) · `docs/assets-map.md` · `Assets/Scripts/Core/Ui/OutlineSdf.cs`·`Assets/Tests/EditMode/OutlineSdfTests.cs`(주석).
- 🔄 1회차 2026-09-13 워커 A(sess-2005-27410): 굽는 자리는 코드(`UiFont.Build` 의 `TMP_FontAsset.CreateFontAsset(cat.font)` 기본 90pt·패딩 9)다 — 값을 `UiFontBake.json` 으로 빼고 패딩을 키운다(§1 «수치는 코드에 박지 않는다» · `catalog.json` 은 T87 lock 이라 비켜 간다).
- 🔄 2회차 2026-09-13 워커 A: 런 234 에서 패딩 15 가 `OutlineTests` 를 빨갛게(띠가 식의 1.77배) — 패딩 9 를 두고 샘플링 54 로(결정 269) + 아틀라스 실제 램프 진단 `ui-screens/t121-ramp.txt`.
- 🔄 3회차 2026-09-13 워커 A: 런 239 진단이 램프 = 2×(패딩+1) 텍셀(TMP 가 박는 G 의 두 배)을 읽었다 — 표 `alpha_texels` 20 으로 재질 `_GradientScale` 을 세운다(결정 271) · `FontBakeTests` 가 램프를 단언.
- 🔄 4회차 2026-09-13 워커 A: 런 241 — 링은 식대로(px 4 ↔ 5.08) 내려왔고 남은 빨강은 자 버그 + 옛 갈래(여백 비율)가 3.4배 굵어진 것. 램프를 부팅 때 아틀라스에서 직접 재 G 로 세우고 옛 `Outline(width01)` 은 TMP 기본 두께로 환산(결정 273).
- ✅ 결론(2026-09-13 · 워커 A · 결정 264·269·271·273): **런 250 초록**(`FontBakeTests` · `OutlineTests` 2/2) + `screen_offline.png` 5배 눈 확인 — 흰 숫자에 검은 링만, 회색 사각 띠 없음. 굽기 54pt · 패딩 9 · 재질 G = 부팅 때 아틀라스에서 잰 램프(19.6) · 옛 갈래는 TMP 기본 두께 환산. 옛 호출부 10곳의 표 이전은 T104 3회차 몫.


### T122 — 장비 그림이 전부 **슬롯 실루엣**이다: 정본은 3D 장비 썸네일(`Scene3D.itemThumb`)로 갈아 끼우는데 클론엔 그 경로가 없다 (Game·UI · **T37 뒤**(메시는 이미 섰다) · T28 26회차가 PNG→정본 코드로 잡음)
- 실측(2026-09-13 · T28 26회차 · 워커 M · 런 230 `screen_forge-list.png` ↔ 원작 `shot-042905.png`): 클론의 «모든 장비의 목록» 은 **한 줄 다섯 칸이 완전히 같은 그림**이다(몽둥이 5 · 투구 5 · 갑옷 5 …) — 25칸이 슬롯 실루엣 5종의 반복. 원작은 같은 자리에 **칸마다 다른 장비 그림**이 들어 있다.
- 정본: `js/ui.js` 2104 가 셀을 `IconGen.img('slot_' + slot)` **플레이스홀더**로 깔고(같은 부위면 같은 그림 — 클론이 멈춘 지점이 정확히 여기다), 2130 `this.hydrateForgeThumbs(...)` 가 `Scene3D.itemThumb({slot, age, ageIdx, rarity, wtype, nameIdx})` 로 **칸마다 3D 스냅샷**을 구워 `<img>` 로 갈아 끼운다(2183~2210 · 한 프레임 6장씩 · 목록을 다시 그리면 이전 작업은 스스로 멈춘다).
- 클론: `Assets/Scripts/Game/Ui/ForgeUi.cs` 120~130 `ItemTile` 이 `PopupKit.IconOr(rt, "img", iconKey)` 한 장으로 끝난다 — 썸네일로 갈아 끼우는 자리가 **없다**. 같은 그림이 장비 슬롯·장비 상세·제작 비교에도 그대로 간다.
- **길은 이미 둘 다 나 있다**: ⓐ 펫은 `Assets/Scripts/Game/Ui/PetFaces.cs` 가 정본 `Scene3D.petThumb` 을 그대로 옮겨 뒀다(T4 `VoxelMob` 으로 종을 세우고 T5 도감 리그로 한 번 찍어 스프라이트로 캐시) ⓑ 장비 메시는 **T37 ✅**(무기 52종·투구·갑옷 · `Hero/GearMeshes.cs`)가 이미 세웠다. 즉 **새 조형이 아니라 그 둘을 잇는 일**이다.
- 무엇을 한다: ⓐ `ItemFaces`(새 · `PetFaces` 와 같은 꼴: 캐시 키 = slot·age·ageIdx·wtype·nameIdx·rarity · 도감 리그로 한 번 찍어 `Sprite` 로 캐시 · 한 프레임에 몇 장씩) ⓑ `ForgeUi.ItemTile` 이 썸네일이 있으면 그것을, 없으면 지금 실루엣을 쓴다(정본과 같은 폴백 순서) ⓒ 목록을 닫거나 다시 그리면 남은 굽기는 멈춘다(정본 `_thumbJob`).
- 판정: ⓐ PlayMode — 한 시대 한 부위의 다섯 칸 스프라이트가 **서로 다르다**(픽셀 해시 5개 서로 다름) · 굽는 중에도 프레임이 안 멈춘다(한 프레임 상한) ⓑ 다음 런 `screen_forge-list.png` 를 원작 `shot-042905` 와 나란히 눈 확인.
- 범위: `Assets/Scripts/Game/Ui/ItemFaces.cs`(새) · `Assets/Scripts/Game/Ui/ForgeUi.cs` · `Assets/Tests/PlayMode/`(새 파일 하나) · 필요하면 `Assets/Forge/Resources/`(굽기 수치표 · 새 파일).
- 진행(2026-09-13 19:4x · 워커 B): 1회차 = `ItemFaces.cs`(굽기·캐시·펌프) + `ItemFacesUi.json` + `ItemFacesTests.cs` **새 파일만** — `ForgeUi.cs` 는 T87 범위 `Ui/Forge*` 라 그 lock 뒤 2회차(누구든). 장신구는 캡처가 없어 실루엣(결정 267).
- 🔄 2026-09-14 05:5x 워커 T(sess-0544-27311) 2회차: B 의 lock 이 23:21 부터 6.5시간 갱신 0 이라 README 대로 인계(결정 318). T87 은 풀렸지만 호출 자리 넷 중 셋(`ForgeInfoPopup` 목록·상세 · `ForgeCraftPopup` · `ForgeSheet` 장착 셀)이 지금 T109·T98 lock 이라 **이 회차는 `ForgeUi.cs` 하나** — `ItemTile` 에 `ForgeItem` 오버로드(`ItemFaces.Get` 이 주면 썸네일 · 아니면 실루엣) + `ItemCard` 가 그것을 부른다 → `screen_craft-compare`·`screen_gear-detail` 의 카드 그림이 3D 썸네일. 단언은 `ItemFacesTests`(+1 · 비교 팝업 두 카드의 `img` 가 아틀라스가 아닌 구운 스프라이트 · 장신구는 실루엣). 목록(`screen_forge-list` · 판정 화면)·장착 셀은 **3회차**(T109·T98 뒤 · 누구든). **2회차 코드 들어감**: `ForgeUi.ItemTile(…, ForgeItem)` 오버로드 + `ApplyThumb`(타일 `img` 를 구운 스프라이트로 · 크기 = 타일 × `img_frac` 1.0 = 정본 `.fl-face img` 100%) · `ItemCard` 가 그것을 부른다 · `ItemFacesTests` +1 · dotnet 635/635. ✅ 는 런 판정(`ItemFacesTests` 3/3) + `screen_craft-compare.png`·`screen_gear-detail.png` 눈 확인 뒤(목록 화면은 3회차).

### T123 ✅ — «main 의 마지막 CI 런» 이 초록인데 **PlayMode 는 빨갛다**: 문서 런이 유니티 잡을 건너뛰고도 통째로 success 로 끝난다 (검증·게이트 · T67·T81 뒤 · 뒤 순서 없음 · 워커 J 등재)

- 실측(2026-09-13 19:17 회차 · 워커 J): §0-6(«회차 첫 일은 언제나 빨강») 을 하려고 CI 를 봤더니 **런 235 success · 231 success** 였다. 그런데 그 둘의 잡 목록은 `Unity EditMode·PlayMode 테스트  skipped` 다 — 문서만 바뀐 push 라 `ci.yml` 의 `gate.outputs.has_code != 'false'` 가 유니티 잡을 건너뛴 것이고(그 자체는 옳다 · 30분을 아낀다), **잡이 건너뛰어지면 런은 통째로 초록**이다. 유니티 잡이 실제로 돈 마지막 런은 **230(`34fbfbb`) failure**(PlayMode 117 중 빨강 1 · `ForgeUiTests.불티가_타격점에서_위쪽_반구로_튄다`)였고, 그 위에 초록 문서 런들이 얹혀 빨강을 덮고 있었다.
- 왜 자가 필요한가: §0-6 은 워커에게 «main 의 마지막 CI 런이 빨간가» 를 눈으로 묻는다. 지금 그 물음의 답은 **«초록»**(오답)이다 — 워커가 빨강을 못 보고 제 작업으로 넘어간다. T67 은 «런 **안**에서 모드 XML 이 빠졌는가» 를, T81 은 «잡 결과가 스텝에 갇히는가» 를 보지만 둘 다 **유니티 잡이 아예 안 돈 런**에는 켜지지 않는다(잡이 없으니 스텝도 없다). 구멍은 «런 사이» 에 있다.
- 무엇을 한다: `tools/check_unity_green.py` — 네트워크 없이 `screens` 브랜치의 `meta.json` 을 읽는다. 그 파일은 `ci.yml` 이 «유니티 잡이 실제로 돈 런» 마다(빨간 런이어도) 쓰는 `{"sha","run","tests",…}` 이므로 그것이 곧 «유니티가 마지막으로 본 커밋과 그 판정» 이다. 자는 ⓐ `tests != success` 면 rc 1 로 «지금 main 의 PlayMode 는 빨갛다 — 이것이 이번 회차의 첫 일이다» 를 찍고(같은 브랜치의 `playmode-red.txt` 에서 FAIL 줄을 뽑아 이름까지 보여 준다) ⓑ 그 sha 뒤에 main 커밋이 몇 개 쌓였는지(= 판정이 얼마나 낡았는지) ⓒ 그 sha 가 main 조상이 아니면(force push·다른 갈래) 그 사실을 말한다. `--self-test` 로 자기 검사한다.
- 판정: **지금 상태**(런 230 빨강 위에 초록 문서 런)에서 자가 rc 1 을 내고 빨간 런 번호·sha·실패 테스트 이름을 찍는다. 유니티 잡이 초록인 런이 올라오면 rc 0.
- 범위: `tools/check_unity_green.py`(새) · `docs/ROUTINE.md`(§0-6 한 줄 · §3 한 줄) · `docs/PROGRESS.md`. **`.github/workflows/ci.yml` 은 안 만진다** — 지금 T119 lock 이다(자는 워커가 §0 에서 부르는 자라 CI 스텝이 필요 없다).


### T124 ✅ — 시대 무늬(항성간 이상 다섯)가 **통째로 없다**: 정본은 무늬 층 + 모양별 움직임을 세 자리에 건다 — **주인 지시**(`grade-pattern-animation`) (Game·UI · T19·T31 뒤 · T28 27회차가 PNG→정본 CSS 로 잡음)
- 실측(2026-09-13 · T28 27회차 · 워커 M · 런 239 `screen_autoforge.png` ↔ 원작 `shot-043117.png`): 원작의 시대 막대 다섯은 색 위에 **무늬**가 깔려 있다(항성간 별밭 점 · 다중 우주 픽셀 밴드 · 양자 동심원 · 지하 세계 마름모 비늘 · 천상 별). 클론의 같은 막대는 **민색**이다. 클론 저장소 전체에 «무늬/패턴» 자취가 **0곳**이고 `docs/ROUTINE.md` 에도 이 일이 **한 번도 등재된 적 없다**(grep 실측).
- 정본(주인 원문 그대로 CSS 주석에 박혀 있다 · `css/style.css` 4907~4910): «항성간부터 그 위로는 등급에 패턴 무늬가 있는데, 그거 각각 패턴 모양에 맞게 패턴들이 움직이는 애니메이션 효과 있어야 함. 확률정보 팝업에서도 그렇고, **실제 그 등급들 장착해도 그 패턴 무늬가 장착 슬롯에서** 애니메이션 효과 있어야 함.»
- 정본이 어디에 거는가(한 정의를 셋이 나눠 쓴다): `.af-age-bar`(자동 제련 행) · `.fi-age-bar`(확률 정보·모든 장비의 목록 머리줄) · `.equip-cell`(장착 슬롯·목록 타일) — 무늬 자체는 `--af-pat` 다섯(4873·4880·4889·4896·4903 · 앞 두 개는 그라디언트, 뒤 둘은 인라인 SVG 타일), 층은 `[data-age]::before`(918~935 · `filter: opacity(.55)`), 움직임은 4917~4975(`gp-interstellar` 12s 두 층 반대 방향 · `gp-multiverse` 3.5s `steps(7)` 계단 · `gp-quantum` 2.8s 파문(**막대 본체**에 건다 — 커스텀 속성 치환 자리 때문) · `gp-underworld` 7s 흘러내림+`gp-breathe` · `gp-divine` 16s 표류+`gp-twinkle`). 이동량은 전부 **타일 한 주기**라 이음매가 없다. `prefers-reduced-motion` 이면 무늬는 남고 움직임만 멈춘다.
- 무엇을 한다: ⓐ 무늬 다섯을 유니티 재질/스프라이트로 세운다(그라디언트 둘 + SVG 타일 둘 + 동심원 하나 · **수치는 정본 값 그대로 표에**) ⓑ 그 층을 세 자리(자동 제련 행·머리줄·장착 셀)에 **한 정의로** 건다(정본이 셀렉터 하나에 셋을 묶은 것과 같은 뜻) ⓒ 움직임을 시대마다 정본 주기·방향 그대로, 되돌아갈 때 튀지 않게(한 주기 = 타일 한 칸) ⓓ OS «움직임 줄이기» 를 존중한다.
- 판정: ⓐ PlayMode — 다섯 시대 막대의 픽셀이 **서로 다르고**(무늬가 실제로 깔린다) 한 주기 뒤 첫 프레임과 같다(이음매 0) ⓑ 다음 런 `screen_autoforge.png`·`screen_forge-list.png` 를 원작과 나란히 눈 확인 ⓒ 앞 다섯 시대(무늬 없는 시대)는 민색 그대로다.
- 범위: `Assets/Scripts/Game/Ui/`(새 파일 하나 — 무늬 층·움직임) · `Assets/Forge/Resources/`(무늬 수치표 · 새 파일) · `Assets/Scripts/Game/Ui/ForgeAutoPopup.cs`·`ForgeInfoPopup.cs`·`ForgeUi.cs`(층을 거는 자리 · 각 파일의 살아 있는 lock 뒤) · `Assets/Tests/PlayMode/`(새 파일 하나).
- ✅ 1회차(2026-09-13 · 워커 R · 결정 272 · 런 251 전체 초록 · lock 반납): 무늬 다섯·움직임·자를 세웠다 — Core `AgePatternRules`(진행·자리·반짝임·양자 위상·정본 정의 래스터 · EditMode 5) · `Resources/AgePatternUi.json`(정본 수치 전부) · `Ui/AgePattern.cs`(`Attach(host, age, cell, mask)` · 타일 굽기+uv 스크롤 · 마스크 정점 알파 · 양자 링 메시 · `Motion` 스위치) · PlayMode `AgePatternTests` 3(직접 깔기). **남은 것 = 세 자리에 거는 줄**(`ForgeUi.AgeBar`·장착 셀·`ForgeAutoPopup`·`ForgeInfoPopup` · T87 lock 뒤 · 누구든) + 촬영 눈 확인.
- ✅ 2회차(2026-09-14 · 워커 R · 결정 299 · 런 306 `AgePatternTests` 4/4 · lock 반납): T87 반납 뒤 세 자리에 걸었다 — `ForgeUi.AgeBar`(확률 정보·머리줄·자동 제련 전부 · 자동 제련만 마스크 인자) · `ForgeSheet.EquipCell`(시트 칸 .55) · `ItemTile` 스위치(목록 타일용). **남은 것**: `ForgeInfoPopup.cs:209` 의 `agePattern:true` 한 인자(T114 lock 뒤) · `PlayerInfoPopup.EquipCell`(T131 lock 뒤) · 촬영 눈 확인. PlayMode 배선 테스트 1. **런 299 판정**: `screen_autoforge`·`screen_forge-info` 뒤 다섯 막대에 무늬 ✅(앞 다섯 민색 ✅) · 배선 테스트 1 빨강은 테스트의 해금 누락(코드 결함 아님) → 수리 · 런 306(0dacd87 포함) 4/4 PASS → 행 ⬜ 대기. **3회차(누구든)**: `ForgeInfoPopup.cs:209` 의 `ItemTile(...)` 에 `agePattern: true` 한 인자(T114 lock 뒤) · `PlayerInfoPopup.EquipCell` 에 `AgePattern.Attach(rt, it.Age, cell: true, mask: false, siblingIndex: 1)` 한 줄(T131 lock 뒤) · 판정은 `screen_forge-list`·플레이어 정보 PNG 눈 확인.
- ✅ 3회차 ⓐ(2026-09-14 04:1x · 워커 R · sess-2015-28206): T114 가 반납해 `ForgeInfoPopup.cs` 가 열렸다 — «모든 장비의 목록» 격자 타일(`Cell` → `ItemTile("fl-face", …)`)에 `agePattern: true` 한 인자(정본 `ui.js` 2090 `fl-face equip-cell[data-age]`) + PlayMode 배선 단언(뒤 다섯 시대 섹션 타일에 층 · 앞 다섯 없음). **판정 ✅ 런 318**(`AgePatternTests` 5/5).
- ✅ 3회차 ⓑ(2026-09-14 05:0x · 워커 R · sess-2015-28206): T131 반납 뒤 `PlayerInfoPopup.EquipCell` 에 층 한 줄(정본 `equipCellHTML` ui.js 3102) + PlayMode 자 1 — 뒤 다섯 시대 장비를 다섯 칸에 끼워 층을 확인하고 판정 PNG `screen_t124-cell.png` 를 굽는다(촬영 목록엔 무늬 장비를 낀 화면이 없다). **판정 ✅ 런 328**(PlayMode 169/169 · `AgePatternTests` 6/6 · `screen_t124-cell.png` 다섯 칸 무늬 눈 확인) → **T124 전체 ✅ · lock 반납**.

### T125 ✅ — `check_unity_green` 이 빨강의 임자를 **커밋 제목**으로 가린다: main 은 여럿이 미는 가지라 그건 «마지막에 민 사람» 이다 (검증 · T123 뒤)
- 실측(2026-09-13 21:2x · 워커 F · `--fetch`): 런 250 의 빨강은 `AgePatternTests`(**T124** 의 새 테스트)인데 자는 «임자: **T109**» 로 찍었다. 런 머리 커밋 `20db536` 의 제목이 `T109 …` 였기 때문이다.
- 왜 나쁜가: 이 자의 쓸모가 «§0-6 의 첫 일 — 이 빨강이 **네 일인가**» 를 가려 주는 것이다. 임자를 틀리면 ⓐ 엉뚱한 워커의 lock 을 믿고 **진짜 임자 없는 빨강을 지나치거나** ⓑ 그 lock 이 죽어 있으면 «이것이 네 일이다» 로 **남이 지금 고치고 있는 자리**로 워커를 보낸다. 둘 다 병렬 운영이 기대는 판정이다.
- 무엇을 한다: 임자를 **빠진 테스트**에서 가린다 — 실패한 픽스처 이름(`Forge.Tests.PlayMode.AgePatternTests`)으로 그 테스트 파일을 찾고, `docs/PROGRESS.md` 의 «범위» 열이 그 파일을 적은 작업을 임자로 삼는다. 여러 작업이 같은 파일을 적었으면 **살아 있는 lock 쪽**을 고르고, 그래도 갈리면 둘 다 적는다. 아무 작업도 그 파일을 안 적었을 때만 옛 «커밋 제목» 갈래로 물러나고 **그렇다고 말한다**.
- 판정: 자기 검사에 이번 실측을 칸으로 박는다(픽스처 `AgePatternTests` + 머리 커밋 제목 `T109 …` → 임자 **T124**) · 고장 주입(범위에 없는 픽스처 → «못 가렸다» 갈래) · 실제 `--fetch` 가 지금 main 에서 T124 를 찍는다.
- 범위: `tools/check_unity_green.py`.

### T126 ✅ — 빌드에는 **없는** 셰이더 둘: `Forge/FxUnlit`·`Forge/EnemyBody` 가 어떤 에셋에도 안 걸려 플레이어에서 잘린다 (배포·Game · T39 뒤 · 워커 G 등재)
- 실측(2026-09-13 21:4x · 워커 G · 코드 읽기 + GUID 역추적): 런타임이 `Shader.Find` 로 찾는 이름은 넷(`Forge/Terrain` · `Forge/FxUnlit` · `Forge/EnemyBody` · `Forge/UiScreen`)인데, 빌드에 실리는 길은 셋뿐이다 — ⓐ Resources 폴더 ⓑ 씬·프리팹·재질이 GUID 로 참조 ⓒ `ProjectSettings/GraphicsSettings.asset` 의 `m_AlwaysIncludedShaders`.
  - `Forge/Terrain` ← `Assets/Forge/Resources/Terrain.mat` 이 GUID 로 물고 있다(ⓐ+ⓑ) — 안전.
  - `Forge/UiScreen` ← `Assets/Forge/Resources/UiScreen.shader`(ⓐ) — 안전(T87 23회차).
  - **`Forge/FxUnlit`(`Assets/Shaders/FxUnlit.shader`) 과 `Forge/EnemyBody`(`Assets/Shaders/Dissolve.shader`) 는 셋 다 아니다** — `m_AlwaysIncludedShaders` 여덟 줄은 전부 유니티 내장 GUID 이고, 두 셰이더의 GUID 를 `--include=*.mat --include=*.prefab --include=*.unity --include=*.asset` 로 훑어도 참조가 **0** 이다.
- 무엇이 깨지나: 에디터(PlayMode·촬영)에서는 `Shader.Find` 가 프로젝트의 모든 셰이더를 보므로 **전부 초록**이다. 그러나 WebGL·Android 빌드에서는 둘이 스트립돼 `Shader.Find` 가 null 을 주고 코드가 폴백으로 넘어간다(`FxUnlitMaterials.Find` → URP Unlit · `EnemyBodyFx.Find` → URP Lit). 그러면 ⓐ 임팩트 연출의 **가산 합성·깊이 끄기·양면**(`_SrcBlend`·`_DstBlend`·`_ZTest`·`_Cull`)이 통째로 사라지고(폴백에 그 프로퍼티가 없어 `HasProperty` 가드가 전부 거짓) ⓑ 적 몸의 **림 라이트·피격 플래시·디졸브**가 없어진다. 즉 «에디터에서만 보이는 연출» 이다.
- 무엇을 한다: ⓐ 두 셰이더를 `m_AlwaysIncludedShaders` 에 넣는다(`{fileID: 4800000, guid: <셰이더 guid>, type: 3}`) — 재질 에셋을 새로 만들지 않는 가장 작은 고침이다. ⓑ 자(`tools/check_shaders_included.py`)를 새로 두어 **코드가 `Shader.Find` 로 찾는 모든 `Forge/*` 이름**이 위 셋 중 하나로 빌드에 실리는지 검사한다(아니면 rc 1 · 고장 주입 자기 검사 포함) · §3 과 CI dotnet 잡에 한 줄. ⓒ 다음 빌드 런에서 `unity-build` 가 초록인지 + 배포 스모크 콘솔 빨강 0 을 확인한다.
- 판정: 자가 rc 0 이고 고장 주입(둘 중 하나를 목록에서 빼면) rc 1 · `dotnet build`·기존 테스트 초록 · 다음 WebGL 빌드 런에서 스모크 초록.
- 범위: `ProjectSettings/GraphicsSettings.asset` · `tools/check_shaders_included.py`(새 파일) · `.github/workflows/ci.yml`(스텝 두 줄) · `docs/ROUTINE.md` §3.

- ✅ 결론(2026-09-14 02:4x · 워커 G · sess-2144-31207): 두 셰이더를 `m_AlwaysIncludedShaders` 에 넣어 **빌드에 실리게** 했고, 같은 사고를 앞으로 막는 자(`tools/check_shaders_included.py` · 자기 검사 13칸 · CI 스텝 둘)를 세웠다. 자는 셰이더 이름(`Shader.Find`)과 `Resources.Load<T>("경로")` 두 갈래를 본다 — 지금 셰이더 4 · Resources 23자리가 전부 실재한다. **판정 근거**: ⓐ 진짜 파일 고장 주입(고침 전 상태 rc 1 · `KeylineUi.ResourcePath` 오타 rc 1 · 되돌리면 rc 0) ⓑ CI dotnet 잡에서 두 스텝이 초록(런 269·286 …) ⓒ 런 271 이 유니티 잡까지 전 초록이라 이 변경이 에디터 경로를 안 깨뜨린다. **남긴 것 한 줄**: WebGL 굽기 런의 로그에서 «Compiling shader Forge/FxUnlit·Forge/EnemyBody» 를 눈으로 보는 확인은 **다음 굽기 런에서** 하면 된다 — 수동 굽기 런(286)은 push 가 잦아 `unity-test` 가 concurrency 로 취소돼(T32 설계) 굽기가 skip 됐고, 그것을 기다리며 lock 을 쥐고 있을 이유가 없다(자와 고장 주입이 이미 같은 것을 지킨다).
### T127 ✅ — 자: **lock 하나가 몇 작업을 세우고 있는가** — 그리고 그중 몇이 «임자가 오래 안 건드린 파일» 때문인가 (도구·게이트 · 뒤 순서 없음 · T87 실측)
- 왜: 규약(`docs/claims/README.md`)의 «두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다» 는 옳지만, **한 작업이 공용 파일을 오래 쥐면 그 뒤로 줄이 길어진다** — 2026-09-13 실측: T87(대장간 연출)이 05:44~21:5x 동안 `Ui/Forge*`·`Assets/Forge/catalog.json`·`Assets/Tests/PlayMode/ForgeUiTests.cs` 를 쥐어 **열린 작업 여럿**(T94·T98·T108·T113·T114·T117 2회차·T120 ⓐ·T124 2회차 …)이 회차마다 게이트만 돌리고 물러났다(워커 O 16:4x 보고 · 워커 H 다섯 회차 연속).
- 규약에 이미 둘째 길이 있다: **«범위에 없는 파일을 열게 되면 표의 «범위» 칸을 먼저 고쳐 push»** — 뒤집으면 «더는 안 여는 파일은 범위에서 빼도 된다». 그런데 그 판단에 필요한 사실(«그 파일을 내가 마지막으로 만진 게 언제고, 그 때문에 누가 기다리는가»)을 아무도 안 보여 준다. 이 자가 그것을 **lock 임자 자신의 게이트 출력에** 띄운다.
- 무엇을 한다: `tools/check_lock_queue.py`(새) — ⓐ `docs/claims/*.lock` 의 산 lock 마다 PROGRESS «범위» 칸을 읽어 파일·글로브를 모은다 ⓑ 열린 작업(⬜)의 범위와 겹치는 것을 찾아 «이 lock 뒤에 선 작업» 을 적는다 ⓒ 겹친 파일마다 **그 lock 의 작업이 마지막으로 그 파일을 만진 커밋 시각**을 git 이력에서 재어 «N시간째 안 건드림» 을 붙인다 ⓓ 90분이 지난(죽은) lock 도 같이 알린다.
- **판정을 안 바꾼다(rc 0 고정 · 보고 전용)**: 남의 선점 사정으로 모든 워커의 게이트를 빨갛게 만들면 T82 가 산 교훈을 되풀이한다. 이 자는 «사실» 만 띄우고 범위를 줄일지는 **그 lock 임자가 정한다**.
- 판정: 자기 검사(순수 함수 · 픽스처로 글로브·겹침·시간 계산) + 지금 레포에서 T87 줄이 실제로 찍히는가 + §3 게이트 목록에 한 줄.
- 범위: `tools/check_lock_queue.py`(새) · `docs/ROUTINE.md`(§3 한 줄 · §2 이 절) · `docs/PROGRESS.md`.

### T128 — **촬영 상태가 런마다 다르다**: 같은 `Seed()` 인데 장비 레벨·등급이 흔들려 T28 채점의 «내려간 화면» 을 못 믿는다 (검증·게이트 · T27·T28·T77 뒤 · 임자 없음 · 검수 Q 등재)
- 실측(2026-09-13 22:0x · 검수 Q · `screens` 의 **런 238 ↔ 런 254** `screen_player-info.png` 를 픽셀로 견줬다 · 둘 다 유니티 잡 초록인 런이다):
  - 장비 셀 다섯이 **런 238 «Lv.25(보라) · Lv.27 · Lv.27 · Lv.26 · Lv.27»** ↔ **런 254 «Lv.26(청록) · Lv.27 · Lv.27 · Lv.27 · Lv.26»** — **레벨과 등급색이 둘 다 바뀐다**.
  - 그 차이가 이 회차 자가 찍은 «**내려간 화면 1개 · `player-info` 4.3 → 3.0(-1.3) · 팝업 안 3.3**» 의 정체다: 두 PNG 의 픽셀 차가 **장비 셀 두 줄**(y 385~445 · 500~515)에 몰려 있고, 팝업 전체 평균 차 **7.1 · 크게 다른 픽셀 5.5%** 다(같은 두 런의 `settings` 는 1.9 · 1.0% · `pet-detail` 은 2.1 · 1.1%).
  - **재화는 고정이다**(코인 27.1m · 젬 8.15k · 망치 302k 가 회차마다 같다) — 곧 `Seed()` 가 **직접 세운 값은 안 흔들리고 장비만 흔들린다**.
- **왜 문제인가**: T28 의 원작 대조 채점은 «지난 회차 대비» 로 회귀를 가린다(«내려간 화면 N개 — 깬 사람을 찾는다»). 상태가 런마다 다르면 그 경고가 **진짜 회귀인지 상태 차이인지 못 가린다** — 검수 Q 가 20:01·22:01 두 회차 연속 그 경고를 좇았고 둘 다 «깬 사람» 이 아니었다(20:01 은 T104·T121 의 글자 획 · 22:01 은 이 장비 흔들림). 자를 믿게 만드는 것이 이 작업이다.
- 찾을 곳(둘 중 하나로 본다 · 검수 Q 는 코드를 안 고쳐 여기까지만 짚는다):
  - ⓐ **앞 테스트가 남긴 세이브**를 물려받는다 — 같은 PlayMode 세션에서 `PlaythroughTests` 가 제작·장착을 돌려 세이브를 바꾸고, 뒤에 도는 `UiShotsTests` 가 그 위에서 찍는다(테스트가 늘수록 진행도가 올라간다 — 실측: 상단바 전투력이 회차마다 22.4m → 33m → 35.6m 로 **올라만 간다**).
  - ⓑ 장비 굴리기가 `CoreRng.Mulberry(20260912)` 로 씨는 고정인데 **굴린 횟수**가 앞 상태에 따라 달라진다(이미 찬 칸에 장착하면 한 번 더 굴리는 꼴).
- 할 일: 촬영 직전에 세이브를 **깨끗한 판**으로 되돌리고(`Seed()` 가 세운 것만 남게) 장비까지 고정 굴림으로 세운다. `UiShotsTests` 가 제 판을 스스로 세우면 앞 테스트 순서에 안 물린다.
- 판정: **같은 커밋으로 두 번 찍은 PNG 가 서로 같아야 한다** — `screen_player-info.png`·`screen_main.png` 의 «크게 다른 픽셀» 이 **1% 아래**(지금 5.5%). CI 두 런(같은 sha 로 `workflow_dispatch` 두 번)으로 재는 것이 가장 곧다. 그 뒤 `ui_score --score` 의 «내려간 화면» 이 실제 회귀만 가리키는지 한 회차 지켜본다.
- 실측 덧붙임(2026-09-13 · T28 29회차 · 워커 M · 지문 자로 여러 런을 견줬다 — 임자가 판정에 바로 쓸 수 있는 수치)
  - **촬영 자체는 거의 결정적이다**: 런 208 ↔ 214 를 화면마다 픽셀로 견주니 **30장 중 20장이 차 0.0**. 흔들린 것은 넷뿐이고 전부 «팝업 안»(게임 상태)이다 — `player-info` 안 5.0 · `league` 16.8 · `chat` 10.5 · `league-challenge` 2.8. 즉 고칠 것은 촬영 기계가 아니라 **판(세이브) 하나**다.
  - **장비만 흔들리는 게 아니다**: 런 224 ↔ 230 의 `screen_player-info.png` 는 팝업 **뒤에 보스 워닝 배너**(빨간 줄·«WARNING»)가 깔려 −1.9 가 났다(지문 «안 14.1 · 뒤 17.0»). 판정에 장비 셀만 넣으면 이 갈래가 새므로 **전투 진행 상태**(보스 워닝 · 채팅 줄)도 같이 고정하거나 판정에서 빼야 한다.
  - **판정을 손으로 안 세도 된다**: `python3 tools/ui_score.py --score --shots <같은 커밋 두 번째 촬영>` 이 화면마다 «그림 차: 팝업 안 N · 뒤 M» 을 찍는다(기준선 지문). **둘 다 1.0 아래면 사실상 같은 그림**이고, 그 아래 «밴드 N → M» 줄이 뜨면 자가 화면을 다르게 쪼갠 것이라 점수 비교가 무의미하다는 뜻이다(T28 23·26·27회차).
- 범위: `Assets/Tests/PlayMode/UiShotsTests.cs`(`Seed()`·판 되돌리기) · `Assets/Tests/PlayMode/PlaythroughTests.cs`(세이브를 남기지 않게 · 필요하면) · `tools/ui_score.py`(«상태 차이» 와 «회귀» 를 가르는 문구 · 있으면 좋다).
### T129 ✅ — 플레이어 정보 «출전 줄» 오브의 Lv 라벨이 **잘려 찍힌다**(«Lv.20» → «v.2») — 폭 상한 · 어림 폭 · 알약 셋이 겹친 자리 (Game·UI · T65·T109 뒤 · 워커 I 등재·닫음 · 런 284 초록 + PNG 8배 눈 확인)
- 실측(2026-09-13 22:0x · 워커 I · 런 254 `screen_player-info.png` 을 8배로 확대 ↔ 원작 `ref/screens/shot-043313.png` 같은 배율): 출전 줄 오브 여섯의 레벨 라벨이 **글자가 좌우로 잘린 채** 찍힌다 — 첫 칸이 «Lv. 20» 인데 «v. 2» 만 보인다. 여섯 칸 전부 같다.
- 원인(코드): `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs` 의 `OrbCell` 이 라벨 상자 폭을 `Mathf.Min(orb * 1.1f, 글자폭 + 안여백×2)` 로 잡는다. **`orb * 1.1f` 상한은 정본에 없는 클론의 발명**이다 — 정본 `.sk-lv`(`style.css` 4045)는 `white-space: nowrap` + `padding: 0 .25rem` 라 **글자 폭대로** 늘어나고 오브보다 넓어져도 그만이다. 지금 오브 지름은 정본대로 앱 폭 6.24%(`PlayerInfoUi.json orb_w`)라 상한이 «Lv. NN» 을 못 담는다.
- 같이 고치는 것(같은 줄의 내 실수 · T109 2회차 `a43f87a`): 423행 `UiKit.OutlinePx(t, "white", KeylineUi.Px("equip_cell_lv"))` 는 **다른 요소의 규칙을 얹은 것**이다 — 정본에서 `-webkit-text-stroke: .3px #fff` 를 받는 것은 `.equip-cell .cell-lv`(`style.css` 936)이고 이 자리는 `.sk-lv`(테 규칙 없음 · `#panel-skills .sk-grid` 안에서만 2px 검정 테)다. 클론의 `.equip-cell .cell-lv` 실물은 `Ui/ForgeUi.cs` 135행(`"lv"`)이라 **T87 lock 안**이다 — `tools/check_keyline.py` 의 `MAP['.equip-cell .cell-lv']` 을 그 자리로 옮기고 `KNOWN` 에 «T87 뒤» 로 적는다.
- ⚠ **별(★)은 쫓지 마라**: 원작 샷의 오브 바닥에 붙은 주황 별은 지금 정본 `renderPlayerInfo`(`ui.js` 5169~5180)의 출전 줄 마크업에 **없다**(`.sk-star` 는 스킬·펫·탈것 패널에만 있다) — 클론에 별이 없는 것은 정본대로다.
- 판정: PlayMode — 출전 줄 라벨 상자 폭 ≥ 그 글자의 `preferredWidth`(잘림 0) · 라벨 글자에 테 0 + 다음 런 `screen_player-info.png` 을 8배로 열어 «Lv. NN» 이 온전히 보이는가(§1) + `check_keyline` rc 0.
- 범위: `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs`(OrbCell) · `Assets/Forge/Resources/PlayerInfoUi.json`(`sk_lv_center_f`) · `tools/check_keyline.py`(MAP·KNOWN 한 칸) · `Assets/Tests/PlayMode/UiSmokeTests.cs`(단언) · `docs/ROUTINE.md`(§2 이 절) · `docs/PROGRESS.md`.
- **2회차 실측(2026-09-13 23:0x · 런 259 PNG 8배)**: 1회차 뒤에도 라벨의 **마지막 자리가 안 보인다** — 다만 원인이 바뀌었다. 이제는 제 상자에 잘리는 것이 아니라 **이웃 칸의 알약이 덮는 것**이다(알약 여섯이 이어져 검은 띠 하나로 보인다). 폭 어림(`PetSkillKit.TextWidth` · 라틴 한 자 = 0.58em)이 «Lv.20» 을 1.18배 부풀린 탓이라 **TMP 가 실제로 잰 `preferredWidth`** 로 바꿨다(어림 104.4px → 실측 ≈88px).
- **3회차(결정 289) — 그 충돌을 «정본이 같은 병을 고친 길» 로 풀었다**: 알약을 걷고 **오브 면 위 흰 글자 + 검정 링**(폭은 T109 폭표 `KeylineUi.json px.sk_lv` = 정본 2px · `sk_lv_rise_w` .0042 = 정본 `bottom: calc(app-w*.0042)`). 불투명한 판이 사라져 이웃 칸을 안 가린다. 근거 셋이 같은 곳을 가리킨다 — 정본 자신의 처방(`sk-orb-lv-pill` · `style.css` 4066) · **원작 샷 `shot-043313` 의 출전 줄이 실제로 그 꼴** · 클론의 글자 크기 하한을 깨지 않는다. 정본이 그 규칙을 `#panel-skills` 로 좁혀 둔 것은 «그쪽은 아직 요소 실측 전»(같은 주석)이라서다.
- **4회차(런 275 눈 확인)**: 검은 띠가 사라지고 «Lv.20»·«Lv.11» 이 **끝 자리까지 읽힌다**. 남은 흠은 라벨이 오브 한가운데를 가리던 것 — 정본 실측 «잉크 세로중심이 오브 상단에서 72.9%»(`style.css` 4050 머리말 픽셀 census · 원작 샷도 같은 자리)대로 표 `sk_lv_center_f` .729 로 내렸다. 잉크가 오브 폭의 131%(정본 제 글자 크기로는 116%)인 것은 글자 크기 하한 탓이라 그대로 둔다.
- **2회차 셈(그대로 둔다 · 결정 283)** — 셈: 칸 간격 = 오브 67.4px(앱폭 6.24%) + 사이 16.2px(1.5%) = **83.6px**. 정본 `.sk-lv` 은 `.6rem`(21.8 캔버스px)이라 알약이 53.4 + 안여백 18.2 = **71.6px** 로 간격 안에 든다. 그런데 클론은 `TextKind.Sub`(**36px** · `TextSizeGateTests` 가 지키는 하한)라 같은 글자가 88 + 18.2 = **106px** — 간격보다 22px 넓다. 즉 **정본대로 그리면 하한을 깨고, 하한을 지키면 알약이 겹친다.** 정본 자신이 스킬 화면에서 같은 병을 «알약을 걷고 오브 면에 흰 글자 + 검정 링»(`sk-orb-lv-pill` · `style.css` 4066)으로 고쳤고 **원작 샷 `shot-043313` 의 출전 줄도 그 꼴**이다 — 다만 정본은 그 규칙을 `#panel-skills` 로 좁혀 뒀다. 어느 쪽으로 갈지는 «글자 크기 하한» 을 쥔 자리(T53·`TextSizeGateTests`)와 같이 볼 일이라 **별 회차·별 판단**으로 남긴다.

### T130 ✅ — 리그 보상 1·2·3위 배지가 **정본 아이콘이 아니라 남의 키트 스프라이트**다 (Game·UI · T22·T58 뒤 · T33 4회차 등재)
- 정본: `ui.js` 4790~4792 — `const badge = t.rank <= 3 ? 'rank' + t.rank : ''` → `IconGen.img(badge, 'lgr-rank-ico')` + 그 위에 흰 숫자. 정본 주석이 못 박아 뒀다: «1·2위가 **왕관 배지 위에 흰 숫자**, 3위가 **벽돌색 마름모 배지** — 🥈🥉 이모지가 아니다».
- 클론: `LeagueSheet.cs:250` 이 `PopupKit.IconOr(rk, "badge", t.Rank <= 2 ? "crown" : "badge")` — `crown`·`badge` 는 **GUI PRO Kit 데모 스프라이트**(`catalog.json`)다. 정본이 제 손으로 그린 `rank1`~`rank3` 는 **T31 아틀라스에 이미 구워져 있고 부르는 곳만 없다**(실측: 아틀라스 키 170개 중 하나).
- 무엇을 한다: 그 두 자리를 `UiIcons.Get("rank" + t.Rank)` 로 바꾼다. 카탈로그는 안 건드린다(아틀라스 키라 `catalog.json` = T87 lock 을 안 연다).
- 판정: PNG 눈 확인(원작 `shot-042208` ↔ `screen_league-rewards`) — 1·2위 왕관 · 3위 마름모 · 그 위 흰 숫자 + PlayMode 단언.
- 범위: `Assets/Scripts/Game/Ui/LeagueSheet.cs` · `Assets/Tests/PlayMode/ShopUiTests.cs`.
- 🔄 2026-09-13 23:4x 워커 O(sess-2140-18689) 1회차: `LeagueSheet.RenderRewards` 의 `IconOr(rk, "badge", crown/badge)` 를 `"rank" + t.Rank` 로(아틀라스 `rank1`~`rank3` · `Assets/Forge/Icons/Resources/Icons/atlas.json`) · 크기 2.15rem·흰 숫자+검정 링은 그대로(정본 2572 `.lgr-rank-ico 2.15rem` · `.lgr-rank-n`) · 자: `ShopUiTests` 에 «1·2·3위 `badge` 스프라이트가 `ico:rank1~3` 이고 4위 이하는 배지 없음» 단언 · 판정은 다음 런 `screen_league-rewards.png` ↔ 원작 `shot-042208` 눈 대조.
- ✅ 결론(2026-09-14 · 런 271 · 워커 O): 한 줄(`IconOr(rk, "badge", "rank" + t.Rank)`) + `ShopUiTests` 단언 1 · PlayMode 137/137 · PNG ×3 눈 확인 — 1위 금 왕관 · 2위 올리브회색 왕관 · **3위 동관(銅冠)** 위 흰 숫자. ⚠ «3위 벽돌색 마름모» 는 옛 원작 샷(042208) 기준의 문구 — 정본 `icongen.js` 2024 `rank3` 는 스스로 동관으로 바꿨다(주석: 계열 통일 · 격자에서 젬으로 오독) · §1 «지금 하는 것» 대로 동관이 맞다. 기록은 PROGRESS «T130 1회차 기록».

### T131 ✅ — 사람 표시 아이콘 셋이 통째로 안 불린다: 성별 둘은 **글자로**, 클랜 배지는 **아예 없이** (Game·UI · T63·T89 뒤 · T33 4회차 등재)
- 정본: 성별을 **아이콘으로** 그린다 — `ui.js` 5043(프로필 칸) · 5195(`.clan` 줄) · 5239(채팅 이름 줄)가 전부 `IconGen.img(S.gender === '♀' ? 'gender_f' : 'gender_m')`. 클랜 배지는 `ui.js` 5244 `if (h % 3 !== 0) out += IconGen.img('clanbadge', 'chat-clan')`.
- 클론: `ProfilePopup.cs:112` · `PlayerInfoPopup.cs:146` · `ChatScreen.cs:188` 이 **글자 `♂`/`♀`** 를 찍고, 클랜 배지는 자취 0. 셋 다 아틀라스에 구워져 있다.
- ⚠ **이 갈래는 «두부» 가 아니다**: `♂`·`♀` 는 주인 글꼴에 있어 화면에 **보인다** — 그래서 `check_text_glyphs`(T89)도 `TextSizeGateTests` 도 안 짖는다. 틀린 것은 «안 보인다» 가 아니라 «정본은 아이콘인데 우리는 글자» 다. T100 4회차가 «아이콘 표에 있으니 괜찮다» 를 의심했듯, 여기서는 «글꼴에 있으니 괜찮다» 가 깨진다.
- 판정: PNG 눈 확인(프로필·플레이어 정보·채팅 세 화면) + PlayMode 단언(그 자리에 `Image` 가 서고 글자 조각에 `♂`·`♀` 가 없다).
- 범위: `Assets/Scripts/Game/Ui/ProfilePopup.cs` · `Ui/ChatScreen.cs` · `Ui/PlayerInfoPopup.cs`(**T129 lock 뒤** · 2회차) · `Assets/Scripts/Core/Meta/Chat.cs`(정본 `chatNameIcons` 해시·성별 아이콘 키 순수식) · `Assets/Scripts/Game/Ui/PersonIcons.cs`(새 · 표 로더) · `Assets/Forge/Resources/PersonIconsUi.json`(새 · 정본 CSS 치수 · `catalog.json` 은 T87 lock) · `Assets/Tests/PlayMode/ChatShareIconTests.cs`.
- 🔄 2026-09-13 워커 T(sess-2344-20050) 1회차: 프로필 성별 칸 → `gender_m/f` 아이콘(정본 5043 · `.profile-field .ico` 1.15em) · 채팅 이름줄 → 성별 아이콘 + 클랜 배지(정본 `chatNameIcons` · 배지는 이름 해시 `h*33+c`, `h%3≠0` 인 이름만 · 치수 `.chat-gender` .0370W · `.chat-clan` .0540W · 여백 −.008W) · 규칙은 `Core/Meta/Chat.cs`(`GenderIcon`·`ClanBadge`) · 치수는 `PersonIconsUi.json`(결정 285) · PlayMode 단언은 `ChatShareIconTests` 에. `PlayerInfoPopup.cs` 의 `clan` 줄은 T129 lock 반납 뒤 2회차. 1회차 코드 들어감(dotnet build 0 오류 · test 582/582 · 자 전부 rc 0) — ✅ 는 CI `ChatShareIconTests` 4/4 초록 + `screen_chat.png`·`screen_profile.png` 눈 확인 뒤. **1회차 판정(00:4x · sess-0044-9878)**: CI 런 275(c82166a · 6577c9d 포함) `ChatShareIconTests` **4/4 PASS**(PlayMode 137/138 · 빨강 1 은 T132 임자 테스트) · 런 275 `screen_chat.png` 눈 확인 — 이름 뒤에 성별 아이콘(분홍 ♀형·파랑 ♂형)이 서고 보라 클랜 배지가 Nova·Orin·Bearopotamus·MilkMessiah·Robson 에만 붙고 moonzzanf·Zephyr·Jennzee 엔 없다(해시 벡터와 일치) · `screen_profile.png` 성별 칸에 파란 아이콘 · 글자 ♂/♀ 0. 남은 것은 `PlayerInfoPopup.cs` `clan` 줄 하나 — T129 lock(워커 I · 3회차) 반납 뒤 2회차. **2회차(03:4x · sess-0344-18954)**: T129 ✅ · T109 9회차 반납으로 파일이 열려 `clan` 줄을 정본 5195 대로 `gender_*` 아이콘(1.05em · 오른쪽 .05em · `PersonIconsUi.json` 키 둘) + « · 서버 1» 로 · `ChatShareIconTests` +1(아이콘 Image·정사각·글자 왼쪽 · ♂/♀ 0) · dotnet 621/621. ✅ 는 CI 5/5 초록 + `screen_player-info.png` 눈 확인 뒤.
- ✅ 2026-09-14 04:4x 워커 T(sess-0444-16036): CI 런 316(5c591ff · edebf02 포함) `ChatShareIconTests` **5/5 PASS**(PlayMode 빨강 6 은 T142·T134 임자 테스트) · 런 316 `screen_player-info.png` 눈 확인 — «성별 · 서버 1» 줄에 파란 성별 아이콘 + « · 서버 1» 글자, 글자 ♂/♀ 0. 세 자리(프로필 · 채팅 이름줄 · 플레이어 정보) 전부 정본대로 아이콘. lock 반납.

### T132 — 정본이 아이콘으로 그리는 다섯 자리가 클론에 **부르는 곳이 0** (Game·UI · T22·T63·T89 뒤 · T33 4회차 등재)
- 다섯과 정본 줄: 채팅 미리보기 아바타 `chatbubble`(`ui.js` 5294) · 패스 칼 `passsword`(4924) · 채팅 공유 카메라 `chatcam`(5271) · 준비 중 팝업 `barrier`(1257) · 오프라인 버튼 상자 `chest`(6146 · 정본도 «정적 마크업이라 부팅 때 갈아 끼운다» 고 적어 뒀다).
- 다섯 다 **T31 아틀라스에 구워져 있다** — 빠진 것은 그림이 아니라 부르는 줄 하나씩이다.
- 판정: PNG 눈 확인(§1) — 메인 채팅 미리보기 · 패스 · 채팅 공유 카드 · 준비 중 팝업 · 오프라인 버튼.
- 범위: `Assets/Scripts/Game/Ui/Hud.cs` · `Ui/PassPopup.cs` · `Ui/ChatScreen.cs` · `Ui/OfflinePopup.cs` · `Ui/Popups.cs`(**T109 lock 뒤**) · `Assets/Tests/PlayMode/UiIconsTests.cs`.
- 진행(워커 D · 2026-09-13 23:5x · sess-2352-23474): 1회차 = `chatbubble`(Hud · «chat» 키는 아틀라스에 없어 카탈로그 스프라이트로 물러나던 자리) · `barrier`(준비 중 팝업 · 가로 1.88em × 1.35em 줄) · `passsword`(패스 카드 위 4.81rem · 3.72×6.72rem · 리본이 덮는다) — 치수는 `StaticIconsUi.json`(catalog 은 T87 lock) · PlayMode `UiIconsTests` +1. **`chatcam` 은 T131 lock(`ChatScreen.cs`) 뒤 2회차** · **`chest` 는 «한 줄» 이 아니라 버튼(`#offline-btn`) 자체가 클론에 없어 T133 으로 뗐다**(결정 287).

### T133 — 메인 화면 오프라인 보상 버튼(`#offline-btn` · 상자 `chest` + zzz + 보상이 쌓이면 들썩임 · 탭하면 보상 수령)이 통째로 없다 (Game·UI · T14·T63 뒤 · T132 1회차가 캠 · T132 에서 뗌)
- 실측(2026-09-13 · T132 1회차 · 워커 D): 정본 `index.html` 75~78 `<button id="offline-btn">`(`.ob-zzz` z 세 글자 + `.ob-chest`) · `style.css` 204~218: 왼쪽 아래(bottom .6rem · left .5rem · z 4) **2.9rem 정사각** · `.ob-chest` 100% + drop-shadow · `#offline-btn.ready .ob-chest { animation: ob-bob 1.6s ease-in-out infinite }`(보상이 쌓였을 때만 들썩) · `ui.js` 841 클릭 → `onClaimOffline()` · 6085 `ready` = `(now − S.lastOfflineClaim)/1000 ≥ 60` · 6145 부팅 때 `.ob-chest` 에 `IconGen.img('chest')`.
- 클론: `Assets/Scripts` 전수에 `offline-btn`·`chest`·`ob-` 자취 **0** — 오프라인 팝업(`OfflinePopup`)은 부팅 때 `pending.Elapsed ≥ 60` 이면 자동으로 뜰 뿐(`MetaHost.cs` 152) 화면에 버튼이 없다. T132 가 «chest 한 줄» 로 적었지만 부를 버튼 자체가 없어 여기로 뗐다.
- 무엇을 한다: HUD 왼쪽 아래에 버튼(chest 아이콘 100% + zzz 글자 + `ready` 들썩임 = 정본 `ob-bob` 키프레임 그대로) · 탭 → 정본 `onClaimOffline` 과 같은 길(`OfflinePopup`) · `ready` 판정은 정본 식(마지막 수령 뒤 60초) · 치수는 자기 표(`StaticIconsUi.json` 에 더한다 · `catalog.json` 은 T87 lock).
- 판정: PlayMode(버튼이 있고 chest 스프라이트 · 수령 직후엔 `ready` 아님 · 탭이 팝업을 연다) + `screen_main.png` 왼쪽 아래 상자 눈 확인(§1).
- 범위: `Assets/Scripts/Game/Ui/OfflineButton.cs`(새 · `MetaHost.OnReady` 훅으로 밖에서 꽂는다 · T120 `HostSfx` 꼴) · `Assets/Forge/Resources/OfflineButtonUi.json`(새 · 치수·키프레임 표) · `Assets/Tests/PlayMode/OfflineButtonTests.cs`(새). `Hud.cs`·`OfflinePopup.cs`·`StaticIconsUi.json` 은 T132 lock 이라 이 회차는 안 연다(수령 길은 공개 `OfflinePopup.Show` 를 부른다).
- 🔄 1회차 2026-09-14 00:1x 워커 A(sess-2005-27410): 범위를 «자기 파일» 로 먼저 고쳤다(규약) — 버튼·zzz·ob-bob·ready 판정·탭 → `OfflinePopup.Show` 를 새 파일 하나로.

### T134 ✅ — 공통 수령 연출(`rewardBurst`)이 통째로 없다: 보상을 받아도 아무것도 안 터진다 (Game·UI · T22 뒤 · T117·T118 과 같은 갈래 · T33 5회차가 키프레임 훑기로 잡음)

- 정본(`ui.js` 3564 `rewardBurst(rewards, opts)`): 누른 자리에서 재화 아이콘이 개수만큼 터져(개수 = `clamp(2 + round(log10(max(1,양))×1.3), 2, 7)` — `coinBurst` 와 같은 로그 눈금) 상단바의 그 재화 pill 로 날아가 흡수된다. 같이 도는 것 다섯: 임팩트 글로우·링(`rw-glow`·`rw-ring` · 버튼 중앙보다 10px 위 · 640ms) · 누른 표면 반응(`rw-pulse` 500ms) · 상단바 전체가 받을 때는 띠 반응(`rw-pulse-band`) · pill 숫자 튐(`rw-tick-bump`) · **도착 pill 이 시트·팝업에 가려져 있으면**(`elementFromPoint` 로 확인) 도착점을 덮은 카드의 상단 모서리 안쪽으로 올리고 고정 앵커 배지(`rw-anchor`)를 세운다.
- 호출 여덟 자리: `dungeons.js` 180(소탕 수령) · `ui.js` 4607(퀘스트 개별) · 4623(퀘스트 일괄 — 주석이 «일괄수령만 토스트 한 줄» 이던 것을 고친 자리다) · 4720(던전 클리어 [보상 수령]) · 4941(상점 무료칸) · 5004(패스 보상) · 5941(오프라인 수령).
- 클론: 키프레임 7종(`rwAnchorIn`·`rwGlow`·`rwPop`·`rwPulse`·`rwPulseBand`·`rwRing`·`rwTickBump`) **자취 0**. 유일한 대응이 `DungeonSheet.cs:96` 의 주석 «원작 rewardBurst 대신(T22 공용 연출 전) — «+해머 302 · +코인 27.1k» 한 줄» 이다 — **자리를 비워 둔 것을 스스로 적어 놓았는데 아무도 채우지 않았다**. 나머지 일곱 자리는 연출이 아예 없다.
- 무엇을 한다: T117(`coinBurst`)·T118(장비 교체 던져내기)과 같은 꼴 — ⓐ Core `RewardBurstRules`(개수·지연·비행·앵커 승격 판정 · UnityEngine 0) ⓑ `RewardBurstUi.json`(수치·키프레임 구간) ⓒ Game `RewardBurst.Play(재화별 양, 누른 RectTransform)` ⓓ 호출 여덟 자리(각 파일 lock 뒤 · 1회차는 셈·연출·테스트만).
- 판정: EditMode 벡터(개수 눈금·지연·앵커 승격) + PlayMode(터진 아이콘 수·수명 뒤 0·가려진 pill 갈래) + PNG 눈 확인(§1).
- 범위: 위 표의 «범위» 칸 그대로.
- 🔄 2026-09-14 03:4x 워커 O(sess-2140-18689) **3회차** = ⓓ 호출 여덟 자리(여섯 파일 · 지금 산 lock 어디에도 없다): `QuestSheet` 개별(`ui.js` 4607 `{[got.cur]: got.amt}` · from 그 [수령] 버튼)·일괄(4623 `gains` · **토스트보다 먼저**) · `DungeonClearPopup.Confirm`(4720 · from [보상 수령] · Close 전에) · `ShopSheet.OnClaimDeal`(4941 `d.reward` 에서 **gems 를 뺀다** — claimDeal 이 젬을 안 주므로 «안 준 걸 준 것처럼» 안 보이게 · from 가격 버튼) · `PassPopup.OnClaim`(5004 `m.free` · from 그 칸) · `OfflinePopup.Collect`(5941 `{coins, hammers}` · from [수집] · **팝업 닫기 전**) · `DungeonSheet` 소탕(`dungeons.js` 180 `rewardBurst(r)` · from 없음 · 종전 T22 대체 토스트 «+해머 302 · +코인 27.1k» 는 걷는다 — 정본 ⚡ 소탕 토스트는 Core 266 이 따로 낸다). `RewardBurst.Rewards(…)` 도우미 셋(쌍 · `OrderedMap` 제외 키 · `DungeonRewards`) · 자: 새 `RewardBurstWiringTests`(상점 무료칸 수령 → PlayCount 1 · gems 없음 · from = 가격 버튼).
- ✅ 결론(2026-09-14 · 런 318 · 워커 O): 1·2회차(워커 S)의 셈·표·연출·자 위에 3회차가 정본 호출 여덟 자리를 그대로 이었다(퀘스트 개별·일괄 · 던전 클리어 · 상점 무료칸(gems 뺌) · 패스 · 오프라인 · 던전 소탕) · `RewardBurst.Rewards` 도우미 셋 · `RewardBurstWiringTests` PASS · 남의 단언 0 흔들림 · 연출 컷 눈 확인. 기록은 PROGRESS «T134 3회차 기록».

### T135 — 전투·팝업 화면 연출 넷이 자취 0 또는 «자리가 다르다» (Game·UI·전투 · T33 5회차가 같은 훑기로 잡음)

- ⓐ **피격 붉은 비네트가 없다**: 정본 `ui.js` 1360 `flashDamage(sev)` 는 `#dmg-flash` 에 `--vig = min(.64, .32 + sev×1.05)` 를 주고 `dmgvignette` 를 돌려 **화면 가장자리를 붉게** 물들인다(지속·감쇠는 CSS 키프레임이 쥔다 — 주석: JS 타이머+트랜지션 조합은 연타 때 서로 잘라먹는다). 호출은 `scene3d.js` 13351(피격 · 세기 비례)·13374(치명 피격 · 세기 1). 클론 자취 **0**.
- ⓑ **모달이 열릴 때 카드가 안 튄다**: 정본 `.modal.opening .modal-card { animation: cardpop }`. 클론 팝업은 그냥 나타난다.
- ⓒ **비교 카드 «NEW» 강조가 없다**: 정본 `.cmp-card.new` 는 `newpulse`(테두리 맥박) + `::after` `shinesweep`(빛쓸기) 둘을 돌린다. 클론 자취 0.
- ⓓ **전리품 피드가 다른 자리에 있다**: 정본 `floatLoot` 는 화면 **고정 스택** `#loot-feed`(최대 6줄 · 1.6초 · `lootpop`)에 쌓는데, 클론 `BattleScene.cs:363` 은 같은 글자를 **영웅 위에 뜨는 숫자**(`Numbers.Spawn`)로 그린다 — 옮기긴 했으나 자리가 다르다.
- **ⓐ 부터 한다**: ⓐ 의 자리(`Game/Battle/BattleOverlay.cs`)가 지금 살아 있는 lock 밖이라 바로 된다. ⓑ 는 `Ui/Popups.cs`(T109) · ⓒ 는 `Ui/ForgeCraftPopup.cs`(T87) lock 뒤다.
- 판정: PlayMode 단언(비네트 α 가 세기대로 · 카드 팝 스케일 곡선 · NEW 두 겹) + PNG 눈 확인(§1).
- 범위: 위 표의 «범위» 칸 그대로.
- **1회차 ⓐ 완료**(2026-09-14 · 워커 K · sess-0032-25925): 셈은 Core `FxRules`(세기 `DmgVigPeak` · 시계 `DmgVigAlpha` · 그림 `DmgVigSample`/`DmgVigT`/`DmgVigMask`)가 쥐고 화면은 `Game/Ui/BattleOverlay.cs` 가 그라디언트+마스크를 한 장에 구워 칠한다. 호출은 정본과 같은 자리 둘(`BattleScene` 의 `HeroHit` → `FlashDamage(sev)` · `HeroDown` → `FlashDamage(1)`). 층은 형제 맨 아래 = 정본 z 12(사망 암전 15·씬컷 16 아래). 단언 EditMode 8 + PlayMode 4.
  - ⚠ **등재 절의 `Game/Battle/BattleOverlay.cs` 는 없는 경로였다** — 실제 자리는 `Game/Ui/BattleOverlay.cs` 다(표의 «범위» 칸을 고쳐 두었다).
  - 남은 ⓑ·ⓒ·ⓓ 는 그대로 2회차 몫(ⓑ `Popups.cs` = T109·T132 lock · ⓒ `ForgeCraftPopup.cs`·`catalog.json` = T87 lock · ⓓ `LootFeed.cs` 새 파일).


### T136 — 전투 바 Lv 라벨이 **오브보다 넓다**: 글자 종류 하한(Sub 36px)이 정본 `.5rem`(18.2px)의 **두 배**라 판을 걷자 폭이 드러났다 (Game·UI · **T87 lock 뒤**(`catalog.json`) · T109 8회차 PNG 눈 확인)
- 실측(2026-09-14 · 런 275 `screen_main.png` 5배 확대 · T109 8회차): 오브 지름 **106px**(2.9rem) · 라벨 «Lv.20» 이 오브 좌우로 흘러나오고 아래로도 걸친다. 셈: 1rem = 36.4px(앱 상자 1080×1920 · `rem_h` 0.018957) → **정본 글자 .5rem = 18.2px** ↔ 클론 최소 종류 `Sub` = **36px**(카탈로그 `textKinds`).
- 정본 비율: `.skill-btn .sk-lv` 는 .5rem/2.9rem ≈ **폭 45%** · 스킬 격자(`#panel-skills .sk-grid .sk-lv`)는 지름의 **90%**(style.css 4045 주석의 원본 실측). 둘 다 «오브 안» 이다.
- 왜 T109 8회차가 안 고쳤나: 글자 하한(36~60)은 **지시서 §1 규칙**(T18)이라 한 화면 사정으로 깨지 않는다 — 판(알약)을 걷는 것과 크기를 내리는 것은 다른 판단이다.
- 길 둘: ⓐ **정본 자리에 맞는 작은 종류를 카탈로그에 더한다**(예 `Micro` 18~20px) + §1 의 하한 줄에 «그 종류는 예외» 를 적는다 — 정본과 같아지는 길 ⓑ 하한을 지키고 라벨을 오브 밖에 둔다 — 정본과 달라진다. **ⓐ 가 정본이다**(다만 하한은 주인 규칙이라 기록을 남긴다).
- 판정: `screen_main.png` 5배 확대에서 라벨이 오브 안에 들어오고(폭 ≤ 지름) · `TextSizeGateTests` 가 새 종류를 알고 초록 · `ui_score` 의 `main` 점수가 안 내린다.
- 범위: `Assets/Forge/catalog.json`(`textKinds` · **T87 lock 뒤**) · `Assets/Scripts/Game/Ui/UiKit.cs`·`PetSkillKit.cs`(그 종류를 쓰는 자리) · `Assets/Scripts/Game/Ui/PlayerInfoPopup.cs`(**둘째 자리 · T131 lock 뒤**) · `Assets/Tests/PlayMode/TextSizeGateTests.cs` · `docs/ROUTINE.md`(§1 하한 줄).
- **둘째 자리 — 플레이어 정보 «출전 줄» 이 더 나쁘다(검수 Q 2026-09-14 04:0x · 런 310 `screen_player-info.png` 실측 · 새 번호 안 뽑았다 — 병도 처방(`Micro` 종류)도 이 작업 것이고 부르는 자리만 하나 는다):**
  - 잰 값(540×960): 오브 여섯의 링 `x92~121·133~163·175~204·217~246·259~288·301~330` → **지름 30px · 피치 41.7px**(피치는 정본 `.pinfo-loadout-row` 6.24%W+1.5%W = 41.8px 와 **맞다**). 라벨 잉크는 `x86~125`·`128~167`… → **폭 40px · 이웃과 틈 2~3px**. 2px 검정 링이 그 틈을 먹어 여섯이 **한 덩어리로 뭉갠다**(3배 확대: «Lv.2(Lv.2(Lv.2(Lv.1(Lv.1(Lv.16»).
  - 원작 `shot-043313`(496×880) 같은 줄: 라벨 잉크 **29px**(`x122~150`·`161~189`) · 이웃과 틈 **10~11px** · 피치 39px. → **라벨/오브 = 원작 85% ↔ 클론 133%.** 클론 라벨이 **1.27배** 넓다.
  - 꼴(알약이 아니라 흰 글자+검정 링)은 **클론이 맞다** — `shot-043313` 을 8배로 확대하면 알약 없이 오브 면 위 흰 «Lv.9» 다(`PlayerInfoPopup.cs` 3회차 결정 289 의 판단이 옳았다). **틀린 것은 폭 하나**이고 그 폭은 `Sub` 36px 하한이 만든 것이라 처방이 이 작업의 ⓐ(`Micro`)와 같다.
  - 판정에 한 줄 더: `screen_player-info.png` 3배 확대에서 라벨 여섯이 **서로 안 닿는다**(이웃 틈 ≥ 정본 2.0%W).
- **세 번째 같은 벽(2026-09-14 06:1x · 워커 I)**: T129(플레이어 정보 출전 줄 · 잉크가 오브 폭의 131%) · 이 절(전투 바 Lv 라벨) 에 이어 **T142 부팅 제목**(`bl-title` 22 < `Title` 하한 60)이 같은 이유로 빨갛다 — 런 331 의 빨강 **여덟이 전부** 그 한 글자 때문이다(`AssertTextGate` 가 화면의 모든 활성 글자를 훑는다). 그래서 예외 종류를 세울 때 **«정본이 제 크기를 px 로 직접 박아 둔 자리»**(부팅 덮개는 rem 이 아니라 `font-size: 22px`)도 같이 예외로 적는다.
- **막혀 있다(2026-09-14 03:0x · 워커 I · 선점했다가 규약대로 물러났다)**: T87 은 풀렸지만 그 사이 `catalog.json` 을 **T98·T108·T131·T135** 가, `UiKit.cs`·`SkillBar.cs` 를 **T109** 가 제 범위로 적었다(전부 살아 있는 lock · 전부 앞 번호) — 규약 «두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다» 그대로 손을 뗐다. **다음 사람은 그 다섯이 반납됐는지부터 보라.**
- **그대로 실행할 수 있게 적어 둔 설계(워커 I · 이 회차에 한 번 만들어 게이트 초록까지 본 뒤 되돌린 것이다 — 617/617 통과)**:
  - ⓐ `catalog.json` `textKinds` 에 `{ "kind": "Micro", "size": 18.2, "min": 18 }` — 18.2 = 정본 `.5rem`(= `rem_h` .018957 × 1920 = 36.4px 의 절반). 넣은 뒤 **`python3 tools/gen_ui_catalog.py`** 로 `UiCatalog.asset` 을 다시 쓴다(안 쓰면 `--check` 가 빨갛다).
  - ⓑ `UiKit.cs` 14행 `public enum TextKind { Title, Button, Body, Sub, Micro }`.
  - ⓒ `SkillBar.cs` 160~162행의 `TextKind.Sub` **셋**(`lvH` 의 `Kind(...).size` · `lvW` 의 `TextWidth` · `Stroked`)을 `TextKind.Micro` 로. 링 굵기는 `Stroked` 가 **그 순간의 fontSize** 로 환산하니 따라온다(`KeylineUi.Stroke`).
  - ⓓ `TextSizeGateTests.카탈로그의_종류_하한은_ROUTINE_규칙_그대로다` 에 «`Micro`.min ≥ 18» 과 «**`Micro`.min < `Sub`.min**» 두 줄 — 예외가 본문·버튼으로 새지 않게 막는 자리다.
  - ⓔ §1 하한 줄에 예외 한 문장: «**`Micro` 18** — 정본이 판(알약) 없이 그림 위에 얹는 작은 배지(`.skill-btn .sk-lv` .5rem · `.sk-lv` 4045)에만. 본문·버튼·제목·보조 라벨에는 쓰지 않는다.» (주인 규칙을 건드리는 것이라 PROGRESS «워커 결정 기록» 에도 한 줄.)
  - ⓕ 같은 병인 `PlayerInfoPopup` 출전 줄(T129 가 «잉크가 오브 폭의 131%» 로 남겨 둔 자리)은 정본이 `.6rem` 이라 종류를 `Micro` 로 바꾸고 그 자리 표에서 `.6rem` 을 주면 된다 — 그 파일은 **T131 lock**.

### T137 ✅ — 두부 막이가 `Assets/Scripts/Core` 를 **통째로 안 본다**: 거기 진짜 두부 둘이 초록으로 지나간다 (검증 · T89·T100·T107 뒤 · T33 6회차가 캤다)
- 실측(2026-09-14 · 워커 F): `check_text_glyphs.SCAN_DIR` 은 `Assets/Scripts/Game` 하나다. `Assets/Scripts/Core` 를 같은 자로 훑으면 리터럴 **2,388줄**이 나오고, 그 안에 **글꼴에도 아이콘 표에도 없는 글자 둘**이 있다 — `Core/Dungeons/Dungeons.cs:237` 의 «🚪 진행 중이던 던전에서 나와 본대로 복귀했습니다» 와 `Core/Battle/Battle.cs:578` 의 «🔥 난이도 상승! …». 둘 다 **토스트로 화면에 나가는 문구**인데 자는 지금 rc 0 이다.
- 같은 구멍의 둘째 겹: 아이콘 표 글자도 Core 에 **14종 22자리**가 있다(`Dungeons.cs`·`DungeonDef.cs` 의 🔨·🪙·🥚·⚔·🗝·👻·🧟…). T107 이 세운 «그 자리가 아이콘 길을 거치는가»(`label_risk`) 판정이 그 22자리에는 **한 번도 안 걸렸다**.
- 왜 이 경계가 그어졌나: T89 가 «화면 문구는 `Game/Ui` 에 있다» 는 전제로 시작했다. 그런데 이 레포의 규약은 **수치와 문구를 Core 로 내리는 것**이라(`Core` 는 UnityEngine 참조 0), 전투·던전의 토스트 문구가 Core 에서 만들어져 `Emit`·`Toast` 로 흘러 나간다. 전제가 규약과 어긋난 자리다.
- 무엇을 한다: `SCAN_DIR` 을 **목록**으로 바꿔 `Assets/Scripts` 전부를 훑고(`label_risk` 도 같이), 지금 드러나는 둘은 임자와 함께 `KNOWN` 에 적는다(정본도 글자라 고칠 길이 이모지 폴백 글꼴뿐 — **T106** 과 같은 갈래다).
- 판정: **고장 주입**(Core 에 글꼴 없는 글리프 한 자 → rc 1) · 넓힌 뒤 rc 0(둘은 KNOWN) · 자기 검사에 «Core 도 훑는다» 칸.
- 범위: `tools/check_text_glyphs.py` · `Assets/Tests/PlayMode/TextSizeGateTests.cs`(같은 구멍인지 확인).
- 🔄 2026-09-14 01:4x 워커 O(sess-2140-18689) 1회차: 마른 실행 실측 — Core 리터럴 2,388줄 · 두부 둘(🚪 `Dungeons.cs:237` ↔ 정본 `dungeons.js` 156 도 글자 · 🔥 `Battle.cs:578` ↔ 정본 `combat.js` 499 도 글자 → KNOWN · T106) · `label_risk` 13자리(`DungeonDef.Icon` 넷은 **아이콘 키**(데이터의 `ICON_KEY` 규칙과 같은 자리) · `RewardText` 다섯 · 전투 `Emit(Loot/Toast, tag:)` 넷은 소비처가 `DamageNumbers`(글자) 또는 **없음** = T138 몫) · 그릇 하나(`Dungeons.Toast` → `Emit(DungeonEventKind.Toast)` → `DungeonSheet.cs:86` `DungeonPopups.Toast` — 이벤트로 한 겹 미룬 전달자). 자에 ⓐ `SCAN_DIRS` 목록(Game·Core) ⓑ C# 아이콘 키 대입(`Icon = "…"`) 은 데이터 규칙대로 뺌 ⓒ 그릇 갈래 ⓓ «이벤트로 넘기고 Game 이 그릇으로 받는다» 를 코드로 확인 ⓔ 자기 검사(고장 주입 포함) · `TextSizeGateTests.KnownTofu` 도 같은 둘.
- ✅ 결론(2026-09-14 · 런 287 · 워커 O): 자가 `SCAN_DIRS = [Game, Core]` 를 돈다(코드 7,822줄) · C# 아이콘 키 대입은 데이터 규칙대로 제외 · Core 그릇은 «이벤트로 넘기고 Game 이 그릇으로 받는가»(ⓓ · `event_received`)까지 확인 · 두부 둘(🚪·🔥)은 정본도 글자라 KNOWN(T106) · Core 라벨 9자리는 소비처를 따라가 LABEL_KNOWN(RewardText 호출 0 · 전투 넷 T138) · 자기 검사 +3(고장 주입 셋) · `TextSizeGateTests.KnownTofu` 같은 둘 · 런 287 전체 초록. 기록은 PROGRESS «T137 1회차 기록».

### T138 ✅ — 전투 화면 **전리품 줄(`#loot-feed`)과 전투 토스트 레인(`#toasts-combat`)이 자취 0** (Game·UI · T27·T22 뒤 · T33 6회차 등재)
- 정본 ⓐ: `ui.js` 1371 `floatLoot(text)` — 화면 한쪽 레인에 줄을 쌓고 **여섯 줄이 넘으면 맨 위를 버리며**, 각 줄은 `paintIconText` 로 아이콘+글자, 1.6초 뒤 사라진다. 부르는 곳은 `combat.js` 450(코인)·454(보스 해머).
- 클론 ⓐ: 그 자리가 **영웅 머리 위 뜨는 숫자**다(`BattleScene.cs:371` → `DamageNumbers` kind `loot`). 자리도 꼴도 다르고 «여섯 줄 상한» 같은 규칙이 없다.
- 정본 ⓑ: `toast(msg, lane)` — 레인이 둘이고 전투 문구 세 자리가 `'combat'` 레인을 쓴다. `index.html` 203 주석: «플레이어가 팝업을 읽는 중에 끼어들면 안 된다 — 레인을 하나 더 두는 것이 유일한 방법».
- 클론 ⓑ: `PopupLayer.Toast(string msg)` 하나뿐 — 레인 인자가 없다.
- 판정: PNG 눈 확인(§1) + PlayMode(여섯 줄 상한 · 1.6초 · 전투 레인이 팝업을 안 가린다).
- 범위: `Assets/Scripts/Game/Ui/BattleOverlay.cs` · `Ui/Popups.cs`(레인 인자) · `Assets/Scripts/Game/Battle/BattleScene.cs` · `Assets/Tests/PlayMode/`(새 파일).
- 🔄 2026-09-14 05:4x 워커 O(sess-2140-18689) **3회차** = 전투 호출부(T135 가 05:36 범위를 좁혀 반납해 Battle 파일이 열렸다): `BattleScene.Handle` 의 Loot 갈래를 `DamageNumbers`(영웅 머리 위 숫자)에서 `LootFeed.Push(e.Tag)` 로(정본 `combat.js` 450 코인 · 454 보스 해머 = `UI.floatLoot`) · `Toast` 갈래를 세워 `PopupLayer.Toast(e.Tag, e.Lane)` — 레인은 정본 `toast(msg, lane)` 둘째 인자를 Core 가 이벤트에 싣는다: `BattleEvent.Lane`(새 필드 · **해시 밖** — EditMode `BattleTests` 는 정본 sim 벡터와 판 단위로 대조하므로 Kind·Id·Num·Flag 를 건드리면 깨진다) · `Battle.Emit(… lane:)` 으로 첫 클리어(484)·난이도 상승(499)만 `"combat"` · «사거리 안에 적이 없습니다»(373)는 정본도 기본 레인 · 쓰러짐(566)은 정본이 `deathFade` 를 못 띄울 때만 토스트라 클론(암전 있음)엔 자리가 없다. `DamageNumbers` 의 `loot` 종류는 그 파일이 T109 lock 이라 다음(죽은 갈래). 자: PlayMode `LootFeedWiringTests`(주입 Loot → 줄 1 · 주입 Toast lane → `LastToastLane`) · EditMode `BattleEventLaneTests`(Lane 이 해시 줄에 안 들어간다).
- ✅ 결론(2026-09-14 · 런 334 · 워커 O): 1·2회차(워커 S · 레인·토스트 레인·표·자) 위에 3회차가 전투 호출부를 이었다 — Loot → `LootFeed.Push`(`combat.js` 450·454) · Toast → `PopupLayer.Toast(tag, lane)` · 레인은 `BattleEvent.Lane`(해시 밖 · 결정 317)으로 첫 클리어·난이도 상승만 combat · `LootFeedWiringTests`·`BattleEventLaneTests` PASS · `screen_main` 눈 확인(머리 위 «🪙» 없음 · 오른쪽 아래 레인 알약). 남긴 것: `DamageNumbers` 의 죽은 `loot` 갈래(T109 lock 뒤 한 줄) · `check_text_glyphs` LABEL_KNOWN 문구(T110 lock 뒤). 기록은 PROGRESS «T138 3회차 기록».

### T139 ✅ — 메인 화면 **이정표 버튼 둘이 자취 0**(미스터리 상자 · 진행 패스) (Game·UI · T22·T63 뒤 · T33 6회차 등재)
- 정본 `index.html` 83·86: `#waypoint-mystery`(❓ · 남은 시간 `#waypoint-mystery-time`) · `#waypoint-pass`(⚔️). 아이콘은 `ui.js` 6135 `WP_ICON = { 'waypoint-mystery': 'wp_mystery', 'waypoint-pass': 'power' }`, 누르면 `onWaypointMystery()`(준비 중 팝업 · 6177) · `openPass()`.
- ⚠ 같은 자리의 **리그 보상 이정표는 정본이 지웠다**(`index.html` 81 주석 · 주인 지시 2026-08-19 «메인에 왼쪽에 랭킹 버튼 없애기») — 남은 둘만 옮긴다. 지운 것을 되살리면 §1 «원작에 없는 것» 이다.
- 판정: PNG 눈 확인(§1 · 메인 화면 왼쪽) + PlayMode(둘이 서고 누르면 각각 준비 중 팝업·패스가 열린다).
- 범위: `Assets/Scripts/Game/Ui/Hud.cs` · `Assets/Forge/Resources/`(치수표 새 파일) · `Assets/Tests/PlayMode/UiSmokeTests.cs`.
- (2026-09-14 · 워커 O · 결정 302) T141 이 같은 것을 다시 등재해 ⛔ 흡수했다 — 그 절의 설계를 여기 잇는다: 새 파일 `Ui/Waypoints.cs`(3D 위·HUD 아래 층 · `Hud.cs` 는 T133 lock 이라 안 건드린다) · 자리표 `Assets/Forge/Resources/WaypointsUi.json`(`catalog.json` 은 산 lock 여럿 · 결정 249 꼴) · 아이콘 `WP_ICON`(`wp_mystery` · `power` · 둘 다 T31 아틀라스에 있다 · `ForgeUi.cs:81` 은 `wp_mystery` 를 폴백 키로만 쓴다) · 카운트다운 정본 `ui.js` 6126 `U.fmtTime(msUntilDailyReset()/1000)` 초마다 · 미스터리 탭 = 준비 중 팝업(6177) · 패스 탭 = `openPass()` · 리그 이정표는 정본이 지웠으니 옮기지 않는다 · 자는 새 `WaypointsTests.cs`(`UiSmokeTests.cs` 는 T94 lock).

- ✅ 결론(2026-09-14 · 워커 U · sess-0259-12861 · 결정 306): 표 `WaypointsUi.json` + Core `WaypointsRules` + `Ui/Waypoints.cs` + `UiRoot.Build` 한 줄 — 런 308(ea89a5b · c0a4043 포함) EditMode 621/621 · PlayMode `WaypointsTests` 2/2 · `WaypointsRulesTests` 4/4(빨강 1 은 T98 `AnvilArtTests` · 워커 L 이 19a39a1 로 수리) · 같은 런 `screen_main.png`: 무대 띠 오른쪽 21% 에 미스터리 생물(초록 덩어리+`?`) · 그 아래 노란 «5시 15분» 알약 · 36% 에 교차 검 — 원작 shot-042120 과 같은 «상자 없이 그림만». 런 306(내 커밋 자체)은 PlayMode 라이선스 자리 부재로 0개 돈 런이라 판정에 안 썼다. 리그 이정표는 옮기지 않았다(정본이 지움).
### T140 ✅ — **설정 목록의 행 높이가 정본의 1.25배**라 목록이 열 줄이 아니라 여덟 줄만 보인다(차단 목록·개인정보 보호가 첫 화면에서 사라졌다) (Game·UI · 검수 Q 등재 · 런 284 PNG 실측)
- 정본: `css/style.css` 3098~3104 `.settings-row` — 주석이 높이를 **못 박아 두었다**: «세로 .5rem은 행 높이를 39.5px로 만들어 원본 42px(4.75%H)보다 0.32%p 낮았고, 10행이 쌓이며 목록 하단에서 3%p 넘게 벌어졌다 — 행 하나로는 통과처럼 보이지만 누적되면 불통과다». 목록 상자는 `max-height: calc(var(--app-h) * .456)`.
- 클론: `Assets/Scripts/Game/Ui/ProfilePopup.cs:292` `float rowH = UiKit.H("settings_row_h") * 1.25f;` — `catalog.json` 386~387 은 이미 `settings_h .456` · `settings_row_h .0475` 로 **정본 값 그대로**인데 호출부가 거기에 `1.25` 를 한 번 더 곱한다. `Assets/Scripts/Game/Ui` 를 통틀어 `H("…") * 1.25` 는 **이 한 줄뿐**이다(다른 `*1.25` 는 전부 `PopupKit.FontSize(...)` 의 줄높이라 갈래가 다르다).
- 실측(런 284 `screen_settings.png` 540×960 ↔ 원작 `shot-042744.png` 489×884 · 행 경계를 세로로 훑어 잰 값):
  - 원작 행 간격 **42.1px**(219→260→302→345→387→429→471→513→556→598 · 9칸 평균) = 4.76%H · 목록 상자 219~620 = **401px** = 45.4%H.
  - 클론 행 간격 **57.0px**(245→302→359→416→473→530→587→644) = 5.94%H = 정본의 **1.25배** · 목록 상자 245~683 = **438px** = 45.6%H(상자는 맞다).
  - 그래서 같은 상자에 원작은 **열 줄**(열째 «개인정보 보호» 가 반쯤 잘려 보인다 — 정본 주석의 «잘림 단서»), 클론은 **여덟 줄**(여덟째 «계정» 이 잘린다)만 든다. `차단 목록`·`개인정보 보호` 는 스크롤하기 전엔 **자취가 없다**.
- 코드는 열두 행을 다 만든다(`ProfilePopup.cs:282~287`) — 만드는 수가 아니라 **행 높이 하나**가 원인이다.
- 판정: `1.25` 를 뺀 뒤 ⓐ PNG 눈 확인(§1 · 첫 화면에 «차단 목록» 이 서고 «개인정보 보호» 가 반쯤 잘린다) ⓑ PlayMode 단언 — 행 높이 == `UiKit.H("settings_row_h")`(지금은 자가 **아무도 없다**: `Assets/Tests` 에 `settings_row_h` 를 재는 줄이 0이다).
- 범위: `Assets/Scripts/Game/Ui/ProfilePopup.cs` · `Assets/Tests/PlayMode/`(설정 행 높이 단언 · 파일은 임자가 고른다).
- ⚠ `계정` 행의 ✓ 가 원작 샷에선 **초록**, 클론에선 **흰 획+검정 테** 인 것은 **클론이 맞다** — 정본 `ui.js:5080` 은 틴트 없이 `IconGen.img('check')` 를 부르고 `icongen.js:3985 stroked` 의 fill 기본값이 `#fff` 다(초록은 `ui.js` 2289·2295·2318 의 자동제작 체크만 `tint:'#23c552'`). 원작 샷이 글자 «✓» 시절이라 초록이다 — **이 자리를 초록으로 되돌리지 마라.**

### T141 ⛔ — 맵 위 이정표 둘이 통째로 없다: 미스터리 상자(카운트다운)·진행 패스 (Game·UI · T18·T22·T31 뒤 · T33 6회차가 `index.html` id 훑기로 잡음)

- 정본: `index.html` 83·86 에 3D 위로 떠 있는 버튼 둘 — `#waypoint-mystery`(«미스터리 상자» · `onclick=UI.onWaypointMystery()`) 와 `#waypoint-pass`(«진행 패스» · `UI.openPass()`). 미스터리 쪽에는 `#waypoint-mystery-time` 이 붙어 **일일 초기화까지 남은 시간**을 초마다 새로 쓴다(`ui.js` 6126 `U.fmtTime(this.msUntilDailyReset()/1000)`).
- 아이콘은 정적 마크업이라 `IconGen` 을 못 불러 **부팅 때 한 번 갈아 끼운다**(`ui.js` 6135 `WP_ICON = { 'waypoint-mystery': 'wp_mystery', 'waypoint-pass': 'power' }` · `paintWaypointIcons()`). 원본 `shot-042120` 8배 주석: 미스터리 = 갈색 통나무 위 초록 덩어리 생물 + 머리 위 흰 `?` · 진행 패스 = 교차 검. (리그 이정표는 주인 지시 2026-08-19 로 정본에서 **삭제**됐다 — 옮기지 않는다.)
- 클론: 버튼·카운트다운 **자취 0**. `wp_mystery` 는 T31 아틀라스에 **이미 구워져 있는데** `ForgeUi.cs:81` 이 «없는 키면 `wp_mystery`» 라는 **폴백 아이콘 키로만** 쓴다 — T130(리그 1·2·3위 배지)·T133(오프라인 버튼)과 **같은 꼴**이다(그림은 있고 부르는 곳이 없다).
- 무엇을 한다: `Ui/Waypoints.cs` 새 파일 — 3D 위·HUD 아래 층에 두 버튼을 세우고(§1 SafeArea 안) 미스터리에 카운트다운 글자를 붙인다. 자리·크기는 `catalog.json`. 누르면 정본대로 미스터리 상자 갈래·패스 시트.
- 판정: PlayMode(버튼 둘이 서고 아이콘 스프라이트가 걸리고 카운트다운이 초마다 준다) + PNG 눈 확인(§1 · 원작 `shot-042120`).
- ⛔ 흡수(2026-09-14 · 워커 O · 결정 302): **T139 와 같은 등재**(같은 T33 6회차 · 같은 버튼 둘 · 같은 정본 줄) — 이 절의 설계(새 파일 `Ui/Waypoints.cs` · 3D 위·HUD 아래 층 · 카운트다운 · `wp_mystery` 가 폴백 키로만 쓰이던 실측)는 T139 가 물려받는다. 이 번호로는 잡지 않는다.

### T142 — 부팅 로딩 화면이 통째로 없다: 주인이 제일 먼저 보는 화면이다 (Game·UI · T1·T13 뒤 · T33 6회차가 같은 훑기로 잡음)

- 정본: `index.html` 21~ 의 `#boot-loading` 전체 덮개(`position:fixed; inset:0; z-index:200`) — 모루+망치+불티 애니(`.bl-forge`·`.bl-anvil`·`.bl-hammer`·`.bl-spark` 셋) · 제목 «포지 클론» · 진행바(`.bl-track` > `#bl-fill`) · 단계 글자(`#bl-stage`, 처음 «불 지피는 중…»).
- 왜 있는가(정본 주석 `main.js` 28~): «시작 렉의 지배 비용은 첫 렌더의 셰이더 컴파일(수 초, 없앨 수 없음)이라 **로딩창 아래에서 소화한다**». 그래서 부팅을 단계로 쪼개 단계마다 `rAF`+`setTimeout` 으로 이벤트 루프에 양보한다 — 통짜 동기 블록이면 로딩창을 넣어도 첫 페인트가 끝까지 밀린다.
- 단계 일곱(`blSet(퍼센트, 문구)`): 8 «세이브 불러오는 중…» · 24 «인터페이스 조립 중…» · 42 «전장 짓는 중…» · 58 «전투 준비 중…» · 74 «용광로 데우는 중…» · 96 «마무리 중…» · 100. 끝나면 `.bl-done`(opacity 0 · `.4s`) 뒤 450ms 에 제거.
- 클론: **자취 0**. 유니티도 첫 씬에서 셰이더 워밍·세이브 읽기·UI 조립이 같은 순서로 일어나므로 자리는 그대로 있다.
- 무엇을 한다: `Ui/BootLoading.cs`(전체 덮개 · 진행바 · 단계 글자 · 정본 애니) + `BootLoadingUi.json`(단계·퍼센트·문구표 — 문구를 코드에 박지 않는다) + `Bootstrap`·`MetaHost` 가 단계마다 알린다(각 파일 lock 뒤).
- 판정: PlayMode(부팅 중 덮개가 서고 퍼센트가 일곱 단계로 오르고 끝나면 사라진다 · 콘솔 빨강 0) + PNG 눈 확인(§1).


- **1회차(2026-09-14 · 워커 F · sess-0227-77341)**: 셈·표·시험을 세웠다 — `Core/Ui/BootLoadingRules.cs`(`BootLoadingSpec` · UnityEngine 0) · `Resources/BootLoadingUi.json`(단계 일곱 · 인라인 CSS 의 px·색·시각 · `bl-swing`·`bl-spark` 키프레임) · EditMode `BootLoadingRulesTests` **8**. 표가 쥔 것: 단계 퍼센트·글자 · 진행률 → 채움 폭 · 망치 각도(주기로 접어 무한 반복) · 불티 셋의 지연과 방향 · 페이드(.4s)와 제거(450ms)의 순서. **2회차(같은 워커·같은 lock)**: `Ui/BootLoading.cs` 로 화면을 세웠다 — 모루·망치(피벗 88%/88%)·불티 셋·제목·진행 막대·단계 글자 · `Set(pct)`·`Done()` · PlayMode `BootLoadingTests` **4**. **색은 `UiCatalog` 가 아니라 제 표에서 읽는다**(조각마다 값으로 넘긴다 · 글자 둘만 `UiKit.Text` 를 거쳐 카탈로그 크기·글꼴을 쓰고 색은 덮어쓴다 — 3회차가 이것도 뗄지 본다). ⚠ 2회차는 이것을 «`colorKey` 에 null 을 넘기면 된다» 로 했다가 **런 313 에서 네 테스트가 전부 터졌다** — `UiKit.Panel(…, null)` 은 카탈로그를 건너뛰는 게 아니라 `ColorOf(null)` 로 들어가 `ArgumentNullException` 이다. 3회차가 색을 값으로 받는 조각 공장 셋(`Face`·`RoundFace`·`DotFace`)으로 고쳤다(결정 기록). **3회차**: 런 313 빨강 넷 수리(위 ⚠). **4회차**: 배선 — `MetaHost.Awake` **한 줄**(`BootLoading.Begin()`)로 띄우고, 진행률은 그 화면이 **«무엇이 섰는가» 를 스스로 읽어**(`Follow` → `PctFromReady`) 민다. 정본은 `boot()` 한 함수 안에서 순서대로 `blSet` 을 부르지만 클론은 그 여섯 가지 일을 **서로 다른 MonoBehaviour 가 제 차례에** 하기 때문이다(`SaveIo`·`UiRoot`·`BattleScene` 둘·`ForgeHost`·`MetaHost`) — 단계마다 부르게 하면 그 부팅이 남의 준비를 기다리게 되어 순서가 바뀐다. 따라가기는 **진짜 부팅(드라이버)만** 켠다(손으로 세우는 자리까지 따라가면 이미 다 선 씬에서 세우자마자 사라진다).
### T143 ✅ — 정본이 말하는 자리 둘에서 클론이 조용하다: 스킬 슬롯 가득 · 기술 연구 완료 (Game·UI · T17·T20·T24·T25 뒤 · T33 7회차가 토스트 문구 전수 대조로 잡음)

- ⓐ **스킬 슬롯이 꽉 찼는데 아무 말도 없다**: 정본 `ui.js` 4462 `if (!Skills.toggleEquip(id)) this.toast(\`스킬은 최대 ${Skills.MAX_ACTIVE}개 장착 가능합니다\`)`. 클론 `SkillPanel.cs:443` 은 `Sk.ToggleEquip(id);` 로 **bool 반환을 버린다** — 슬롯이 차 있으면 눌러도 아무 일이 안 일어나고 이유도 안 알려 준다(«고장난 버튼» 으로 읽힌다). 같은 화면의 **펫 쪽은 제대로 말한다**(`PetPanel.cs:457` → `toast_pet_max` «🐾 펫은 {0}마리까지 출전할 수 있습니다 — 한 마리를 먼저 제거하세요») — 한 화면 안에서 갈린 자리다.
- ⓑ **기술 연구가 끝나도 토스트가 없다**: 정본 `techtree.js` 383 은 레벨 올리고 `SFX.levelUp()` **과 함께** «🔬 <이름> <단계> Lv.N 연구 완료!» 를 띄운다. 클론 `TechPopups.cs:69` 은 `if (Tree.Claim(Host.Now())) { Sfx.LevelUp(); AfterChange(); }` — **소리만 울리고 말이 없다**. (실패 갈래 둘 «🧪 물약이 부족하거나…»·«💎 젬이 부족합니다» 는 이미 있다 — 성공 갈래만 빠졌다.)
- ⓒ **리그 시즌 종료 문구가 정본과 다르다**: 정본 «🏆 리그 시즌 종료! 순위 보상을 획득했습니다» ↔ 클론 `LeagueSheet.cs:33` «🏆 리그 시즌 종료! N위 보상 지급» — 원작에 없는 순위 숫자를 더했다(§1 «원작에 없는 것을 넣지 않는다»). 정본 문구로 되돌린다.
- 무엇을 한다: ⓐ `if (!Sk.ToggleEquip(id)) { 토스트; return; }` + 문구는 `PetSkillUi.json` 에 키로(코드에 문장을 박지 않는다 · 개수는 `Sk.Rules.MaxActive`) ⓑ `Claim` 이 준 `claimedId` 로 정본과 같은 문구를 띄운다(`Claim(nowMs, out string claimedId)` 오버로드가 이미 있다) ⓒ 문구 한 줄.
- 판정: PlayMode — 슬롯을 다 채운 상태에서 다른 스킬을 토글하면 토스트가 뜨고 장착 수는 안 변한다 · 연구를 완료 시각으로 당겨 수령하면 토스트가 뜬다 · 콘솔 빨강 0.
- 범위: 위 표의 «범위» 칸 그대로.
- 🔄 2026-09-14 03:4x 워커 N(sess-0524-8791) 1회차: 셋 다 한 줄 — ⓐ `SkillPanel.OnToggle` 이 bool 을 받아 `toast_skill_max`(`PetSkillUi.json` · 개수 `Sk.Rules.MaxActive`) ⓑ `TechPopups.OnClaim` 이 `Claim(now, out id)` 로 «🔬 이름 로마단계 Lv.N 연구 완료!»(소리 뒤) ⓒ `LeagueSheet` 문구를 `league.js` 64 그대로 · PlayMode `MissingToastTests` 3(결정 309). → **런 313 셋 PASS · ✅ · lock 반납**(빨강 다섯은 T142·T134 자리).
- ✅ 결론: 스킬 슬롯 가득·연구 완료는 정본 문구로 말하고, 리그 시즌 종료는 정본 문구 그대로(순위 숫자 없음) — 셋 다 PlayMode 가 지킨다.


### T144 ✅ — 오프라인 [수집] 버튼이 **초록**이다: 정본은 `.modal-card .btn.primary` 로 **파랑**(#005dff) — T68 이 기본 규칙만 보고 «초록이 정본» 이라 적은 것을 정정한다 (Game·UI · T68 뒤 · **`OfflinePopup.cs` 의 살아 있는 lock 뒤** · T28 35회차 실측)
- 실측(2026-09-14 · T28 35회차 · 워커 M · 런 310 `screen_offline.png` ↔ 원작 `shot-042110.png`): 클론의 [수집] 버튼은 **초록 면**, 원작은 **파란 면**이다(같은 자리 · 같은 크기).
- **T68 의 판단이 어디서 어긋났나**: 그 완료 기록은 «원작 샷의 파란 수집 버튼은 옛것 — 정본 `.btn.primary` 가 초록(`#1f4a2c`/`#2ea043`)» 이라고 적었다. 그런데 그 규칙(`style.css` 668 · 특이도 0-2-0)은 **팝업 안에서 덮인다**: 3548~3551 `.modal-card .btn.primary, .panel .btn.primary, #equip-sheet .btn.primary { background: var(--pp-blue); … }`(0-3-0)이 이긴다. 오프라인 카드는 `<div class="modal-card offline-card">`(`ui.js` 5891)라 **`.modal-card` 문맥 안**이고, `--pp-blue` 는 3507 에서 **#005dff** 다.
- 정본이 파랑이라는 둘째 근거: `style.css` 307 주석이 이 버튼을 **«원본 **파란 면** 실측 x 34.80%W · 폭 29.80%W · 높이 7.49%H»** 로 적어 두었다 — 폭·자리를 그 파란 면에서 쟀다는 뜻이다.
- 클론: `Assets/Scripts/Game/Ui/OfflinePopup.cs` 83 `PopupKit.Btn(bottom, "collect", "수집", "pp_green", "pp_green_dk", …)`.
- 무엇을 한다: 색 키를 `pp_blue`/`pp_blue_dk` 로 바꾸고(수치는 카탈로그 키 그대로 · 새 색을 만들지 않는다), T68 의 «걷어낸 것» 줄을 이 정정으로 고쳐 적는다. 그 팝업의 다른 것(머리 판 어두운 색 · 빨간 점 · 요율 세로 배치)은 T68 이 이미 맞춰 뒀으니 **손대지 않는다**.
- 판정: PlayMode 색 단언(버튼 면 = 카탈로그 `pp_blue`) + 다음 런 `screen_offline.png` 를 원작 `shot-042110` 과 나란히 눈 확인.
- 범위: `Assets/Scripts/Game/Ui/OfflinePopup.cs`(**T132·T133·T134 등 이 파일을 쥔 lock 이 풀린 뒤**) · `Assets/Tests/PlayMode/OfflinePopupTests.cs`(단언 한 줄) · `docs/ROUTINE.md` T68 절의 그 줄.
- 🔄 2026-09-14 04:5x 워커 O(sess-2140-18689) 1회차: T134 반납 직후(`OfflinePopup.cs` 를 쥔 산 lock 은 그것뿐이었다 · T133 은 270분 지나 죽었다) — `PopupKit.Btn(bottom, "collect", …)` 의 면·아래턱 키를 `pp_blue`/`pp_blue_dk`(카탈로그 #005dff/#001c4e · 정본 3507 `--pp-blue`) 로 · `OfflinePopupTests` 에 «[수집] 면 = pp_blue» 한 줄 · T68 절의 «클론의 초록이 맞다» 줄을 이 정정으로 고쳐 적음(정본 3548 `.modal-card .btn.primary` 0-3-0 이 668 의 0-2-0 을 덮는다).
- ✅ 결론(2026-09-14 · 런 328 · 워커 O): 키 둘(`pp_blue`/`pp_blue_dk`) + `OfflinePopupTests` 색 단언 1 + T68 절 «초록이 맞다» 줄 정정(취소선 · 근거 3548/5891/307) · PlayMode 169/169 · `screen_offline.png` ↔ `shot-042110` 눈 확인(파란 면 · 흰 글자 · 빨간 점). 기록은 PROGRESS «T144 1회차 기록».

### T145 ✅ — `check_unity_green` 이 «범위 열에 없는 테스트 파일» 을 **주인 없는 자리**로 뒤집어 남의 진행 중인 작업으로 보낸다 (도구·게이트 · T125 뒤 · 워커 G 등재·실행)
- 실측(2026-09-14 04:4x · 워커 G · 런 313): 빨강 셋 중 `CoinBurstTests` 에 대해 자가 «**못 가렸다** — 그 파일을 «범위» 열에 적은 작업이 없다 … §0-6 대로 **네가 고친다**» 를 찍었다. 그런데 그 자리는 **T117 이 lock 을 쥔 채 그 회차에 쓰던 단언**이다(2회차 제목: «ForgeHost 호출 + 오토포지 갈래 + **실판매 단언**» · lock 48분). 임자가 «범위» 열에 테스트 파일을 안 적은 실수를 자가 «아무도 없다» 로 뒤집으면 **남의 살아 있는 작업을 건드리게 된다** — T125 가 막으려던 바로 그 사고의 반대 방향이다.
- 무엇을 했나: ⓐ `history_owners(fixture)` — 그 **파일**을 고쳐 온 커밋 제목의 `T<번호>`(최근 순 · `git log -12 -- '*/<이름>.cs'`)를 보조 증거로 읽는다(«누가 마지막으로 밀었나»(`pusher`)와 다르다 — 그 파일을 고친 커밋만 본다). ⓑ 갈래 ⓓ(아무도 범위에 안 적음)에서 그 후보 중 **산 lock 이 있으면 그의 몫**으로 찍고, 임자에게 «범위 열에 그 파일을 적어라» 를 한 줄로 이른다. ⓒ 죽었거나 이력이 없으면 종전 문구(«못 가렸다 → 네가 고친다») 그대로 두되 이력 후보를 참고로 덧붙인다.
- 판정: `--self-test` **35 → 42칸**(이력 파싱 둘 · 산 lock 이면 그의 몫 · 범위에 적으라 이름 · 죽은 lock 이면 종전대로 · 이력 참고 표시 · 이력 없으면 문구 불변) + **실제 main 에서** `CoinBurstTests` 줄이 «못 가렸다» → «**T117** — 범위 열엔 없지만 그 파일을 고쳐 온 커밋이 그 작업이고 lock 48분 전» 으로 바뀌는 것을 확인.
- 범위: `tools/check_unity_green.py`.

### T146 — 장비 시대 상세(`#forge-item-modal`)의 회색 판이 **25 밝다**: 정본이 이 모달만 `#d6d6d6` 로 덮어썼는데 클론은 공용 `pp_panel`(#efefef) 그대로 (Game·UI · **T109·T110·T124 lock 뒤**(`ForgeInfoPopup.cs`) · 워커 I 등재 · 30화면 중 꼴찌 1.4/10)
- 실측(2026-09-14 05:0x · 워커 I · 런 321 `screen_forge-detail.png` ↔ 원작 `ref/screens/shot-042931.png` · 픽셀 직독): 하위 스탯 판 바탕이 **클론 (239,239,239) ↔ 원작 (214,214,214)**. 카드 안 같은 높이 다섯 자리(y 34·40·50·70·74%)에서 전부 239 다.
- 정본이 스스로 적어 뒀다(`style.css` 3722~3726 · `#forge-item-modal .idet-subs`): `background: #d6d6d6;` 옆 주석이 «원본 실측 rgb(214,214,214) — **`--pp-panel`(#efefef)은 25 밝았다**». 즉 정본은 공용 판 색을 이 모달에서만 일부러 덮었고, 클론은 그 덮어쓰기를 안 옮겨 **정확히 그 25 만큼** 밝다.
- 같은 자리에서 같이 어긋난 둘(정본 3726·3729 · 한 커밋으로):
  - `idet-lead` 색 — 정본 `#forge-item-modal .idet-lead { color: #000 }`(순검정) ↔ 클론 `"pp_ink"`(#17181a). 굵기도 정본 `font-weight: 800`(3707) 인데 클론은 굵기를 안 준다.
  - 하위 스탯 행 색·굵기 — 정본 `.substat-row { color: #3a3a3a; font-weight: 700; letter-spacing: -.01em }`(3708·3729) ↔ 클론 `"pp_ink"` 민글자.
- **색은 카탈로그로**(§1): `pp_panel` 은 공용이라 그것을 고치면 다른 화면이 따라 어두워진다 — 이 모달 몫으로 `idet_panel`(#d6d6d6) · `idet_lead_ink`(#000) · `idet_row_ink`(#3a3a3a) 세 키를 `catalog.json` 에 더한다(**그 파일도 lock 이 자주 붙으니 `check_lock_queue` 로 먼저 보라**).
- 판정: `screen_forge-detail.png` 판 바탕 픽셀이 **214±2** + 행 글자가 굵다(눈 확인) + `ui_score --score --only forge-detail` 이 안 내린다 + PlayMode 빨강 0.
- 범위: `Assets/Scripts/Game/Ui/ForgeInfoPopup.cs`(판·lead·행 셋 · **T109·T110·T124 lock 뒤**) · `Assets/Forge/catalog.json`(색 키 셋) · `Assets/Tests/PlayMode/ForgeUiTests.cs`(판 색 단언).

### T147 — 캐릭터 **윤곽선(후처리 깊이-엣지 아웃라인)** 이 클론에 없다: 정본 `initPost`/`renderFrame` 컴포짓이 영웅·펫·탈것·적에 1px 검정 윤곽을 그리고 **모바일에서도 켠다**(`postEdge = true`) (Game·전투 3D · T8·T39 뒤 · T33 10회차 등재 · 워커 S)
- 정본: `scene3d.js` 550 `initPost()` — `postOn = !mobile`(블룸+비네트 · **데스크톱 한정**) · `postEdge = true`(**모든 기기**). `renderFrame`(995): 씬을 RT(`_rtScene` + 깊이 텍스처)와 파츠 ID 버퍼(`_rtId` · rgb 16bit 파츠 ID · a 선형깊이/`idZFar` 32)에 그린 뒤 풀스크린 컴포짓 `_compMat` 이 네 항으로 윤곽을 판정해 검정선을 얹는다: 깊이 상대 임계 `edgeK` .028(이웃이 나보다 edgeK×깊이×탭거리 이상 멀면 나를 칠함 · `edgeMaxZ` 22 안에서만 · 하늘 화소 제외) · 법선 `normalK` .9 · 크리즈 `creaseK` .010 · 파츠 ID 경계 `idOn`. 텍셀 1/512. 1192~1262 의 인버티드-헐 셸(`applyOutlineTree` · `OUTLINE_E` .02)은 **포스트 스택이 없을 때만** 쓰는 잔재다(`if (this.postOn || this.postEdge) return;`) — 옮기지 않는다.
- 클론: URP 볼륨(`Assets/Settings/ForgeVolume.asset`)은 `ColorAdjustments` 노출(정본 `toneMappingExposure`)만 쓰고 **Renderer Feature 0 · 엣지 셰이더 0**(`Assets/Scripts`·`Assets/Shaders` grep). T39 는 «`rimFlash` 림 셸·`flashTargets.out` 은 아웃라인 복원 뒤 죽은 갈래» 로 뺐지만 **살아 있는 쪽(포스트 엣지)** 은 아무도 안 잡았다 — 촬영 PNG 의 영웅·적에 검정 윤곽이 없는 것이 그 자취다.
- 무엇을 한다: ⓐ Core `EdgeOutlineRules`(계수 넷·`edgeMaxZ`·`idZFar`·텍셀 · 판정식 · UnityEngine 0 · 표 `EdgeOutlineUi.json` 또는 catalog) ⓑ URP `ScriptableRendererFeature` + 풀스크린 셰이더(깊이·법선은 URP `_CameraDepthTexture`/`_CameraNormalsTexture` · 파츠 ID 는 액터 렌더러에 ID 색을 쓰는 보조 패스) — **UI 카메라/캔버스에는 안 건다**(정본도 DOM 위가 아니라 3D 캔버스 안) ⓒ 블룸+비네트는 `postOn = !mobile` 이라 모바일 클론 대상에서는 «정본이 지금 하는 것» 이 아니다 — 안 옮기고 그 판단을 결정으로 남긴다(WebGL 데스크톱 빌드까지 맞추려면 별도 등재).
- 판정: PlayMode — 엣지 패스 on/off 두 프레임을 같은 장면에서 찍어 영웅 실루엣 둘레의 **어두운 화소 띠(≈1px)** 가 on 에만 있다(정본 판정기의 «off 프레임은 네 항을 다 끈다» 규칙 그대로) · 콘솔 빨강 0 · 프레임 예산(T44) 안 · `screen_main.png` 눈 확인(영웅·적 윤곽).
- 범위: `Assets/Scripts/Core/Render/EdgeOutlineRules.cs`(새) · `Assets/Scripts/Game/Render/EdgeOutlineFeature.cs`(새 · Renderer Feature + Pass) · `Assets/Shaders/EdgeOutline.shader`(새) · `Assets/Forge/Resources/EdgeOutlineUi.json`(새 · `catalog.json` 이 남의 lock 이면 T65 꼴) · URP 렌더러 데이터 에셋(Feature 등록 · `Assets/Settings`) · `Assets/Tests/EditMode/EdgeOutlineRulesTests.cs`(새) · `Assets/Tests/PlayMode/EdgeOutlineTests.cs`(새).
- 🔄 2026-09-14 워커 G(sess-0542-31207) 1회차 = **ⓐ 만**(셈·표·자): Core `EdgeOutlineRules`(네 항 + 팽창 + `cN` 가드 + `amax` 억제 + 비접촉 게이트 + 이력 + ID 가시성 검증) · 표 `Assets/Forge/Resources/EdgeOutlineUi.json`(정본 유니폼 + 줄 번호 출처) · EditMode 7칸(dotnet 641/641). **2회차는 ⓑ**(URP Feature + `EdgeOutline.shader` + ID 보조 패스 + 렌더러 데이터 등록 + PlayMode on/off 픽셀 + PNG) — 셰이더는 이 규칙을 줄 단위로 옮기고 새 셰이더 등록은 `check_shaders_included`(T126)가 본다. 함정 둘은 이미 단언에 박혔다: `edge_max_z` 는 **두께 판정에 안 끼운다** · off 프레임은 `EdgeOutlineTerms.Off` 로 **네 항을 한꺼번에** 끈다(결정 320). 블룸+비네트는 안 옮긴다(결정 320). lock 유지.

### T148 ✅ — `check_unity_green` 이 «한 커밋이 **남의** 자 여럿을 깨뜨린 빨강» 을 임자 없음으로 찍는다 — 지난 초록 유니티 런과 이 빨강 런 **사이의 코드 커밋**을 안 본다 (도구 · T123·T125·T145 가 세운 자의 넷째 구멍 · 검수 Q 등재 · 런 331 실측)
- 실측(2026-09-14 06:0x · main `7dd7f89`): 런 **331**(`42a3f8e`)이 PlayMode **174 중 빨강 16**. 자를 그대로 돌리면 `BootLoadingTests` 둘만 «임자 **T142** · lock 살아 있다(29분)» 로 맞히고, 나머지 **열넷**(`ForgeUiTests` 5 · `PetUiTests` 5 · `ShopUiTests` 3 · `TextSizeGateTests` 1)은 **«산 lock 이 하나도 없다 → §0-6 대로 네 일이다»** 로 찍는다.
- 그런데 그 열넷도 **T142 의 것**이다. 갈래를 이렇게 잡았다:
  - 런 **329**(`da87587` · 05:27)가 **전체 초록**(PlayMode 172/172 · 워커 P·H·L 이 그 런으로 판정했다)이고, 그 뒤 **코드** 커밋은 둘뿐이다 — `15d2922`(T120 3회차 ⓐ · `HostSfx.cs`·`ForgeHost.cs`·`AudioSmokeTests.cs`)와 `42a3f8e`(T142 4회차 · `BootLoadingRules.cs`·`BootLoading.cs`·**`MetaHost.cs` +4줄**). 나머지는 전부 `[skip ci]` lock 커밋이다.
  - **`AudioSmokeTests` 는 빨갛지 않다** → T120 갈래가 아니다.
  - `42a3f8e` 의 `MetaHost.Awake` 는 `BootLoading.Begin()` 을 **조건 없이** 부른다. MetaHost 를 세우는 PlayMode 테스트는 전부 전체 화면 오버레이를 얻고, `TextSizeGateTests` 는 그 오버레이의 글자까지 «활성 글자» 로 센다 — 빨강 열넷의 자리가 정확히 그것이다. T142 **제 커밋 메시지도 그 함정을 안다**고 적었다(«Follow 를 늘 돌리면 … 앞선 테스트 넷이 깨진다»).
- 왜 자가 못 가리나: 임자 사다리(ⓐ 범위 열 → ⓑ 산 lock → ⓒ 여럿 → ⓓ 그 파일을 고쳐 온 커밋 이력 · T145)가 **전부 «그 테스트 파일» 단위**다. 남의 파일을 깨뜨린 커밋은 그 파일을 안 만졌으니 어느 칸에도 안 걸린다 — 실행 확인: `history_owners('ForgeUiTests.cs')` · `('ShopUiTests.cs')` 둘 다 **빈 목록**이다.
- 자 머리말(10행)이 스스로 «구멍은 «런 안» 이 아니라 **«런 사이»** 에 있다» 라고 적어 두었는데, 그 «사이» 를 **런의 초록/빨강**에만 쓰고 **임자 가리기**에는 안 쓴다. 같은 자리에 한 칸 더 두는 일이다.
- 왜 급한가: 이 오판은 T123·T125 가 막으려던 바로 그 사고다 — 다음 워커가 «네 일이다» 를 믿고 **살아 있는 남의 4회차**(T142)를 건드린다. 지금 main 이 그 상태다.
- 처방(ⓔ 한 칸): 빨강을 만나면 ⓐ~ⓓ 로 못 가린 자리에 대해 **직전 «유니티가 실제로 돈 초록 런» 의 sha ↔ 이번 sha 사이**를 `git log --format=%H%x09%s` 로 훑어 **코드 커밋만**(`[skip ci]`·문서 전용 제외) 뽑고, 그 제목의 `T<번호>` 중 **산 lock 이 있는 것**을 «이 런에 새로 들어온 후보» 로 먼저 말한다. 사이 코드 커밋이 하나면 단정해도 좋고, 여럿이면 나란히 준다(«AudioSmokeTests 처럼 그 커밋의 제 자가 초록이면 그 갈래는 아니다» 도 같이 적을 수 있다). 직전 초록 sha 는 `screens` 의 `meta.json` 이력(`git log origin/screens -- meta.json`)에서 읽으면 API 없이도 된다.
- 판정: ⓐ **고장 주입** — 런 331 상태를 넣으면 열넷이 «네 일이다» 가 아니라 «런 329 뒤 새 코드 커밋은 T120·T142 · 그중 산 lock 은 **T142**» 로 나온다 ⓑ 사이에 코드 커밋이 없을 때·여럿일 때·초록 런을 못 찾을 때 세 갈래가 각각 제 문구를 낸다 ⓒ `--self-test` 에 그 칸들(순수 함수로 · 지금 자의 꼴 그대로).
- 범위: `tools/check_unity_green.py` · `docs/ROUTINE.md`(§0-6 한 줄이 필요하면).

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
python3 tools/check_lock_queue.py                                             # (T127) 내 lock 뒤에 몇 작업이 서 있는가 · 그중 «내가 오래 안 건드린 파일» 때문인 것 (보고만 · rc 늘 0)
python3 tools/check_text_glyphs.py                                            # (T89) 화면 문구의 글자가 주인 글꼴에 다 있는가 — 새 두부(□)를 막는다 + 토스트 그릇이 아이콘 길을 거치는가(T107)
python3 tools/check_shaders_included.py                                       # (T126) 이름으로 찾는 것(`Shader.Find`·`Resources.Load`)이 **빌드에도** 실리는가 — «조용히 null» 을 막는다
python3 tools/check_sfx_calls.py                                              # (T119) 원작 소리 24종이 게임 코드에서 실제로 울리는가 — 레시피만 있고 호출이 없는 이름을 막는다
tools/check_data_sync.sh .wwwww-src                                           # (T2 뒤) data/*.json ↔ 정본
python3 tools/check_keyline.py                                               # (T109) 정본 -webkit-text-stroke 규칙 ↔ 클론 키라인 호출(.wwwww-src 필요) — 정본이 주는데 클론이 안 부르는 자리를 막는다(CI datasync 잡)
python3 tools/check_unity_green.py --fetch                                     # (T123) 유니티 잡이 **실제로 돈** 마지막 main 런이 초록인가 — 문서 런(유니티 잡 skipped)이 빨강을 덮는 것을 막는다(§0-6 의 눈을 대신한다)
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
- **실측 보탬(2026-09-13 09:4x · 워커 O · GitHub MCP `get_job_logs` 는 이 세션 프록시를 지났다)**: 런 168 유니티 잡 꼬리 — `[command]/usr/bin/git push origin --force screens` → `remote: Internal Server Error` · `Request ID 5410:270069:F8EBD2:14B623A:6AA66D6D` · `Time 2026-09-13T09:31:46Z` · `! [remote rejected] screens -> screens (Internal Server Error)` → `Action failed with "The process '/usr/bin/git' failed with exit code 1"`. 커밋 자체(52 files · root-commit)는 만들어졌다 — 권한(403)·보호 규칙(pre-receive hook declined)·YAML 이 아니라 **GitHub 쪽 500** 이다(같은 시각 런 167 이 `startup_failure` 였던 것과 같은 갈래). 1회차: 액션을 같은 뜻의 셸(고아 루트 커밋 · force push)로 바꾸고 15·30·45·60초 백오프로 다섯 번 밀며 실패 문구를 `::warning`/`::error` 주석으로 찍는다(결정 221).

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
> **완주 판정에서 빼는 한 줄(T33 2회차 · 워커 H · 결정 212)**: `T35`(배경 복원 `SIMPLE_BG=false`)는 **정본이 지금 `SIMPLE_BG: true` 라 화면에 없는 경로**다 — §1 «옮기는 것은 원작이 **지금 하는 것**» 과 §2 T35 «선점하지 않는다» 를 그대로 따르면 이 줄은 «다 옮겨졌다» 의 조건이 아니다(안 그러면 T33 이 영원히 ✅ 가 못 된다). 주인이 `SIMPLE_BG` 를 끄면 그때 T35 를 열고 이 예외를 지운다. **다른 줄은 예외 없다.**
> **반대 방향도 자가 본다(T69)**: PROGRESS 에 있는 작업이 이 절 어디에도 이름이 없으면 `tools/check_final_table.py` 가 rc 1 을 낸다 — 새 작업을 등재한 워커는 **여기 제 원작 줄에도 적는다**. 원작 모듈에 안 붙는 도구·게이트·CI 작업은 아래 «(원작 밖 · 도구·게이트·CI)» 줄에 적는다.
> 정본 크기는 2026-09-12 wwwww main 기준(줄 수). 큰 모듈(`scene3d.js` 18,887줄 · `ui.js` 6,181줄 · `icongen.js` 6,704줄)은 한 회차에 안 끝난다 — 워커가 하위 작업으로 쪼개 등재하고 여기에 줄을 더한다.

| 원작 (`web/`) | 무엇 | 작업 | 상태 |
|---|---|---|---|
| `js/balance-data.js` · `gamedata.js` · `mobdata.js` · `data/raw/*`(안 뽑음 · 결정 6ⓑ) | 수치·정의 표 | T2 → JSON · T3 강타입 | ✅ |
| `js/bignum.js` · `util.js` | 큰 수 · 표기 · 난수 | T3 | ✅ |
| `js/voxel.js` · `mobs.js` · `mobs-pets.js` · `mobs-mounts.js` · `mobs-enemies.js` · `mobs-props.js` · `mobs-skillfx.js` | 박스 몹 조립 · 종 표 | T2 · T4 · T5(전 종 세워 보기) | ✅ (T4 · T5) |
| `js/prochar.js`(2,526) | 영웅 박스 모델 · 무기 파지 · 애니 | T6 | ✅ |
| `js/combat.js` · `state.js`(전투 부분) | 전투 틱 · 웨이브 · 보스 · 전투가 `S` 에 쓰는 것(처치·재화·첫 클리어·진행·saveGame·던전 판) | T7 · T8 · T55 | ✅ (T7 · T8 · T55) |
| `js/scene3d.js`(18,887) · `scene3d-skillfx.js`(1,088) | 3D 세계 전부: 카메라·광원·테마·적 스폰·애니 계약·데미지 숫자·셰이크·파티클·맵·소품·펫 대형·탈것 탑승·스킬 오브젝트·사망 연출·히트 이펙트 | T1(카메라·테마0) · T8 · T9 · T10 · T11 · T12 · T39 · T52 · T54(화면에 아무도 안 선다) · T147(후처리 깊이-엣지 아웃라인) | T1 ✅ · T8 ✅(적 스폰·보행·공격·피격·사망·숫자·셰이크·파티클 · 뺀 연출은 T39 ✅) · T9 ✅(SIMPLE_BG 의 보이는 것 · 소재 T34 · 배경 복원 T35) · T34 ✅ · T38 ✅ · T10 ✅(펫 대형·따라오기·관절 드라이버) · T39 ✅(레갈리아·보스 재질·등장 워닝·디졸브·림·플래시·플레어/스파이크/링/점광/그을음·궤적·블롭·암전) · T11 ✅ · T35 ⬜(SIMPLE_BG 복원 전엔 안 잡는다) · T12 ✅(`scene3d-skillfx.js` 전부 + 스킬 디스패처) · T52 ✅(시전 젖힘 `heroG.rotation.z` — T12·T39 가 뺀 것) · T54 ✅(촬영 PNG 에 영웅·적·펫이 0 — 오브젝트는 서는데 화면에 안 그려진다) · T147 🔄(캐릭터 윤곽선 — 정본 `postEdge` 는 모바일도 켠다 · T33 10회차) |
| `js/state.js` · `main.js`(저장 시점·부팅) | 세이브 · 마이그레이션 · 오프라인 보상 | T13 | ✅ |
| `js/forge.js` | 대장간 규칙 · 오토 포지 | T14 · T19 | ✅ (T14 · T19) |
| 장비 8부위 · 페이퍼돌(`prochar.js`·`ui.js` 장비 · `scene3d.js` makeWeapon/makeHelmet/dressMcRig) | 등급·서브스탯·판매가·외형 | T15(규칙·표값) · T37(3D 외형 캡처) · T19 | ✅ (T15 · T37 · T19) |
| `js/pets.js` | 알·부화·합성·출전 규칙 · 출전 스탯 기여 | T16 · T20 · T10(출전 조형) · T43(스탯 접착) · T79(업그레이드 모달) · T103(업그레이드 층·대비 — 정본대로 있음) | T16 ✅ · T10 ✅ · T20 ✅ · T43 ✅ · T79 ✅ · T103 ✅ |
| `js/skills.js` | 소환·18종·3슬롯(정본 `MAX_ACTIVE`) | T17 · T20 | T17 ✅ · T20 ✅ |
| `js/mounts.js` | 탈것 규칙 · 탑승 | T40(Core 규칙 · 결정 72) · T11(탑승 3D) · T20(탈것 화면) | T40 ✅ · T11 ✅ · T20 ✅ |
| `js/dungeons.js` | 던전 4종 | T23 · T21 | T23 ✅ · T21 ✅ |
| `js/techtree.js` · `ascension.js` | 기술트리 · 승천 | T24 · T21 | T24 ✅ · T21 ✅ |
| `js/shop.js` · `pass.js` · `quests.js` · `league.js` · `chat.js` | 상점·패스·퀘스트·리그·채팅 | T25 · T22 | ✅ (T25 · T22) |
| `js/ui.js`(6,181) · `css/style.css` · `index.html` | 캔버스·HUD·탭·패널 전부(공개 함수 97개 — T19~T22 절에 이름별로 나눠 적었다) · 메뉴·프로필·설정·디버그 | T18 · T19 · T20 · T21 · T22 · T53(한글 글꼴) · T56 · T57 · T58 · T59(T28 2회차가 PNG 로 잡은 결함) · T60 · T61(검수 Q 가 PNG 로 잡은 결함) · T62 · T63 · T65 · T68(T28 3~5회차가 PNG 로 잡은 결함) · T66(던전 라벨) · T75(보석 카드 안쪽) · T76(전투 글자가 시트 위로) · T78(검수 Q 가 런 127 PNG 로 잡은 딤·상단바 자리) · T85(설정 딤) · T90(글자 넘침) · T89(이모지) · T91(채팅 미리보기 빔) · T93(대장간 딤 — 정본대로 있음) · T94(팝업 딤 지각 α) · T95(장착 오브 어둠 막·배지 자리) · T97(미니 씬) · T98(모루 그림이 사각 근사 — 정본 SVG 는 사다리꼴·총알 뿔·검정 stroke) · T100(남은 두부 셋 · 자 둘의 구멍) · T102(부화장 빛기둥·램프 키·리본) · T104(글자 외곽선 두께) · T109(키라인 호출이 아예 없는 열한 파일 — T104 와 다른 갈래) · T111(장비 상세 카드 자리·폭) · T113(제작 비교 카드 자리·폭 — 같은 병) · T115(버튼 글자 넘침 막이가 두 자리만 본다) · T122(장비 그림이 슬롯 실루엣뿐 — 3D 썸네일 경로 없음) · T144(오프라인 수집 버튼 색 — T68 정정) · T129(출전 줄 Lv 라벨 잘림 — 클론이 만든 폭 상한) · T124(시대 무늬·움직임이 통째로 없다 — 주인 지시) · T108(자동 버튼 라벨 둘) · T106(이모지 폴백 글꼴 — 주인 승인) · T105(미니 씬 칸 채움) · T114(상세 카드 확률 줄) · T117(판매 코인 연출 `coinBurst` — 호출 0) · T118(장비 교체 던져내기 `equip-swap-throwout` — 대응 0) · T121(글꼴 SDF 패딩이 정본 최대 키라인을 못 담는다) · T129(출전 줄 Lv 라벨 잘림) · T134(공통 수령 연출) · T135(비네트·카드 팝·NEW·전리품 피드) · T136(전투 바 Lv 라벨 폭 — 글자 하한 ↔ 정본 .5rem) · T138 · T139 · T140(설정 행 높이가 정본의 1.25배 — 목록이 열 줄 대신 여덟 줄) · T141(맵 위 이정표 둘) · T142(부팅 로딩 화면) · T143(정본이 말하는 자리 둘에서 조용하다) · T146(장비 시대 상세 회색 판 25 밝다) | T18 ✅ · T19 ✅ · T21 ✅ · T22 ✅ · T20 ✅ · T53 ✅ · T56 ✅ · T57 ✅ · T58 ✅ · T59 ✅ · T60 ✅ · T61 ✅ · T62 ✅ · T63 ✅ · T65 ✅· T66 ✅ · T68 ✅ · T75 ✅ · T76 ✅ · T78 ✅ · T79 ✅ · T85 ✅ · T90 ✅ · T89 ✅ · T91 ✅ · T93 ✅ · T94 ✅ · T95 ✅ · T97 ✅ · T99 ✅ · T110 ✅ · T101 ✅ · T100 ✅ · T102 ✅ · T104 ✅ · T109 ⬜ · T111 ✅ · T113 ✅ · T115 ✅ · T122 🔄 · T144 ✅ · T129 ✅ · T124 ✅ · T106 ⬜ · T108 ✅ · T105 ✅ · T114 ✅ · **T98 ✅**(3회차가 이 칸에서 빠진 것을 찾았다) · T117 ✅ · T118 ✅ · T121 ✅ · T129 ✅ · T134 ✅ · T135 ⬜ · T136 🔄 · T138 ✅ · T139 ✅ · T140 ✅ · T141 ⛔ · T142 ⬜ · T143 ✅ · T146 ⬜ |
| `js/sfx.js`(618) | 효과음 24종(+프리미티브 6) · 음악 4모드 (코드 합성) | T30 · T119(구워는 놨는데 **부르는 곳이 없는** 소리 + 호출 0 자) · T120(대장간 소리 훅 미연결 · levelUp 호출 0) | T30 ✅ (`Core/Audio` · `Game/Audio` · `AudioTests` 벡터 대조 · `AudioSmokeTests`) · T119 ✅ · T120 ✅ |
| `js/icongen.js`(6,704) · `avatars.js`(831) | 아이콘 136종 · 아바타 24종(`IconGen.draw` 키 160 · «523» 은 도우미까지 센 수) + tint 변형 10 | T31 · T130 · T131 · T132 · T133 | T31 ✅ · T130 ✅ · T131 ✅ · T132 🔄 · T133 ⬜ |
| `ref/screens/shot-*.png` 30장 · `tools/shot-*.js` · `ref/UI-SPEC.md` · `ref/POLISH.md` | 원작 화면 정본 · 촬영 도구 · 비율 규격 | T27(촬영) · T28(대조) · T33(완주) · T77(촬영 시드 전투력) · T83(촬영 두 장 가르기) · T128(촬영 상태 결정론) | T27 ✅(원작 30장 전부 열림 + `screen_*.png` 31장 + 짝 표 `screens.json` · CI 런 83) · T28 🔄 · T33 ⬜ · T77 ✅ · T83 ✅ · T128 ⬜|
| `css/style.css` 제작 키프레임 22종 | 대장간 뽑기 연출(모루·오토포지·결과 카드) | T87 | 🔄 |
| (주인 지시) 백그라운드 재생 · 복귀 따라잡기 | runInBackground · OnApplicationPause 절대시각 | T88 | ✅ |
| (주인 지시 · 원작 밖 품질 조건) SafeArea · 60fps · 실제 화면 촬영 | 모바일 상단 카메라 회피 · 프레임 예산 · 게임 화면 PNG 를 눈으로 | T45 · T44 · T27 · T50 · T64 · T73 · T74 | T45 ✅ · T44 ✅ · T27 ✅(촬영 자리 · 노치 모의는 `UiRoot.NotchSafeArea`) · T50 ✅(프레임당 관리 힙 풀링) · T64 ✅(렌더 몫은 없었다 — AudioBank 베이크 스레드 · 편집기 재질 후처리 · URP 변경 없음) · T73 ✅(AudioBank 베이크 배열 되쓰기) · T74 ✅(FxCubes 시전당 재질 되쓰기) |
| WebGL 배포 · Android | 배포 | T26 · T86(부팅 GameData 인자) | ✅ (굽기 잡 조건 T32 ✅) · T86 ✅(런 223 스모크 초록 · gh-pages 배포) |
| (원작 밖 · 도구·게이트·CI) 병렬 운영을 지키는 자들 — 원작 모듈에 안 붙지만 **여기 적는다**(안 적으면 T33 이 그 위를 지나간다 · T69) | lock·번호·문서·카탈로그·CI·진단 자 | T29 · T36 · T41 · T42 · T46 · T47 · T48 · T49 · T51 · T67 · T69 · T70 · T71 · T72 · T81 · T82 · T84 · T92 · T96 · T107 · T112 · T123(유니티 잡을 건너뛴 문서 런이 빨강을 덮는다) · T125(그 자의 임자 판별) · T126(빌드에 안 실리는 셰이더) · T127 · T137(Core 를 안 보던 두부 막이) · T145(빨강 임자를 이력으로 되짚기) · T148(빨강 임자를 «런 사이 코드 커밋» 으로도 가린다) | T29 ✅ · T36 ⛔ · T41 ✅ · T42 ✅ · T46 ✅ · T47 ✅ · T48 ✅ · T49 ✅ · T51 ✅ · T67 ✅ · T69 ✅ · T70 ✅ · T71 ✅ · T72 ⛔ · T80 ✅ · T81 ✅ · T82 ✅ · T84 ✅ · T92 ✅ · T96 ✅ · T107 ✅ · T112 ✅ · T123 ✅ · T125 ✅ · T126 ✅ · T127 ✅ · T137 ✅ · T145 ✅ · T148 ✅ |
| `lib/three.min.js` · `anvil-*.png`(참고 이미지 · 게임이 안 읽음) · `web/TODO.md` 미완 7항목 | 옮기지 않음 | — | 해당 없음 |
