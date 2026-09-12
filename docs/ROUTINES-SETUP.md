# 계정별 루틴 세팅 런북 (kuzuni/unity1 · 복붙용)

> 목적: 여러 claude.ai 계정에서 **같은 저장소·같은 작업표**에 워커를 붙인다. 계정이 늘면 처리량이 늘고, 한 계정의 주간 한도가 소진돼도 나머지가 돈다.
> 루틴 = claude.ai Code 크론 세션(Routine). 워커는 `docs/ROUTINE.md` §0 절차로 lock 을 잡고 §2 작업을 하나 끝내고 push 한다. lock 이 저장소 커밋 기준이라 계정이 달라도 자동으로 직렬화된다.
> 관리 화면: https://claude.ai/code/routines · 저장소: https://github.com/kuzuni/unity1 · 지시서: `docs/ROUTINE.md`

## 0. 사전 준비 (계정마다 딱 3가지 · 주인)

1. **GitHub 저장소 접근** — 택1:
   - (간단) 같은 GitHub 계정(kuzuni)을 그 클로드 계정에도 연결. 그리고 **Claude GitHub App 의 저장소 접근에 `kuzuni/unity1` 을 켠다**(꺼져 있으면 세션이 «push access 없음» — 2026-09-12 실측).
   - (별도 GitHub 계정) `kuzuni/unity1` Settings → Collaborators → **Write** 로 초대·수락 → 그 계정으로 클로드 연결.
2. **환경(environment)** — 그 계정으로 claude.ai/code 에서 `github.com/kuzuni/unity1` 을 연결해 세션을 한 번 띄우면 계정 전용 `environment_id`(`env_…`)가 생긴다. **계정마다 다르다** — 아래 틀의 `environment_id` 를 그 값으로.
3. **모델 정책** — `claude-fable-5-1`, 한도 소진 시 `claude-opus-5`. **소넷 금지.**

## 1. 슬롯 표 (네 계정 · 16 워커 + 검수 Q)

> **어느 계정이 몇 번인지는 `docs/ROUTINE.md` §6 ⓪ 계정 식별표가 정본이다.** 계정 1 = `kimmoon2007@gmail.com`(A~D · 등록 완료 2026-09-12 17:44 UTC) · 계정 2 = `rudwpwjrwkdb1995@gmail.com`(E~H · 등록 완료 2026-09-12 17:53 UTC). 새 계정은 거기 빈 줄부터 채운다.

| 계정 | 워커 | cron (UTC) |
|---|---|---|
| 1 | A · B · C · D | `5 * * * *` · `20 * * * *` · `35 * * * *` · `50 * * * *` |
| 2 | E · F · G · H | `12 * * * *` · `27 * * * *` · `42 * * * *` · `57 * * * *` |
| 3 | I · J · K · L · **Q**(검수) | `2 * * * *` · `17 * * * *` · `32 * * * *` · `47 * * * *` · **`0 */2 * * *`** |
| 4 | M · N · O · P | `9 * * * *` · `24 * * * *` · `39 * * * *` · `54 * * * *` |

→ 매시 :02 :05 :09 :12 :17 :20 :24 :27 :32 :35 :39 :42 :47 :50 :54 :57 — 3~4분 간격. 세션이 10~40분 도니 실제로는 8~12개가 겹쳐 병렬로 돈다(lock 이 가른다).
계정이 넷보다 적으면 앞 계정부터 채운다(계정 하나면 A~D 만 · 둘이면 A~H). 검수 Q 는 계정이 몇이든 하나만.

## 2. 공통 job_config (create/update 바디 틀)

`NAME`·`CRON`·`PROMPT`·`environment_id` 만 바꾸고 나머지는 고정.

```json
{
  "name": "NAME",
  "cron_expression": "CRON",
  "enabled": true,
  "persist_session": true,
  "job_config": {
    "ccr": {
      "environment_id": "env_XXXXXXXX_그_계정_값으로_교체",
      "events": [
        { "data": { "message": {
          "role": "user",
          "content": "PROMPT (§4 블록 · X 만 바꾼다)",
          "type": "user",
          "uuid": "슬롯마다_고유_uuid"
        } } }
      ],
      "session_context": {
        "model": "claude-fable-5-1",
        "allowed_tools": ["Bash","Read","Write","Edit","Glob","Grep","WebFetch","Task"],
        "sources": [ { "git_repository": { "url": "https://github.com/kuzuni/unity1" } } ]
      }
    }
  }
}
```

- `persist_session: true` — 실행마다 새 대화창을 만들지 않는다(주인 지시 · aaawunity 2026-09-07).
- Q 루틴은 `allowed_tools` 에서 `Task` 를 빼도 된다(코드 수정 안 함).
- ⚠ **부분 업데이트(model 만) 금지** — `400 (environment_id 요구)`. 바꿀 땐 `job_config` **전체**(events 포함) 재전송.
- 생성 방법 택1: (a) 그 계정에서 세션을 띄워 `create_trigger`(claude-code-remote MCP · `create_new_session_on_fire` 대신 위 틀의 `persist_session`) / RemoteTrigger 로 create, (b) claude.ai/code/routines UI 에서 수동, (c) `/schedule` 스킬.

## 3. 이름
`unity1 포지 이식 워커 A (:05)` … `unity1 포지 이식 워커 P (:54)` · `unity1 포지 이식 검수 Q (짝수시 :00)`.

## 4. 프롬프트 (복붙 · 워커 A~P 공통 · `X` 만 바꾼다)

```
너는 «포지 클론 유니티 이식»(kuzuni/unity1) 병렬 워커 X 다. 여러 워커(A~P · 네 계정 · 매시 :02 :05 :09 :12 :17 :20 :24 :27 :32 :35 :39 :42 :47 :50 :54 :57 에 하나씩)가 동시에 돌아간다.
1. 먼저 `git fetch && git checkout -B main origin/main` 으로 최신 상태에서 시작한다 (pull --rebase 금지). detached HEAD 면 이어서 `git branch --set-upstream-to=origin/main main`.
2. **docs/ROUTINE.md 가 유일 지시서다.** 거기 적힌 세션 시작 절차(§0)·절대 규칙(§1)·작업 목록(§2)·게이트(§3)·기록 규약(§4)을 그대로 따른다. 정본 kuzuni/wwwww 는 ROUTINE.md 의 방법대로 옆에 clone 해서 읽기만 하고 절대 수정·푸시하지 않는다. `Assets/StreamingAssets/data/*.json` 은 손으로 고치지 않는다. 지시서가 사라졌거나 읽을 수 없으면 아무것도 수정하지 말고 «지시서 없음» 으로 보고하고 종료한다.
3. 작업 전 반드시 docs/claims/ 의 선점 lock 을 먼저 잡고(규약 docs/claims/README.md · 선점 직전 `python3 tools/task_state.py <ID>` 가 0 · lock 은 커밋·push 가 성공해야 유효), 남의 lock(다른 계정 포함)이 잡은 작업은 피한다. 선점할 작업이 없으면 게이트만 재실행하고 커밋 없이 조용히 종료한다.
4. 커밋 전 §3 게이트가 초록이어야 한다(`dotnet` 이 없는 환경이면 그 사실을 완료 기록에 적고 CI 초록을 확인한 뒤 lock 을 반납). 컴파일 안 되는 커밋 금지. 새 에셋을 만들면 `python3 tools/gen_meta.py` 로 .meta 를 같이 만든다.
5. 승인 프롬프트가 뜨는 명령·대화형 편집기 금지. 캡처 PNG·대용량 바이너리 커밋 금지. **주인의 승인·허락을 기다리지 않는다: 판단이 필요한 것은 네가 정해 바로 적용하고 PROGRESS «워커 결정 기록» 에 한 줄 남긴다.** 금지는 셋: wwwww 수정 · data/*.json 손대기 · 원작에 없는 콘텐츠·밸런스 추가.
6. 모든 보고·커밋 메시지·PROGRESS 기록은 한국어. 브랜치는 main 만. 커밋 작성자는 `git -c user.name=kuzuni -c user.email=<이 계정의 이메일>`, 커밋 제목은 `T<번호> <무엇> (sess-HHMM-NNNNN · 워커 X)`.
7. 작업 한 덩어리를 push 한 직후 CLAUDE.md 의 방법으로 ntfy 알림을 쏜다(문구 = 무엇을 끝냈는지 + 커밋 7자리). 컨테이너에서 ntfy.sh 로 직접 curl 하지 않는다.
```

### 4-Q. 검수 Q (코드 수정 안 함)

```
너는 «포지 클론 유니티 이식»(kuzuni/unity1) 의 검수 Q 다. 코드를 고치지 않는다(수정은 워커 A~P 몫). `git fetch && git checkout -B main origin/main` 뒤 docs/ROUTINE.md §6 ⑤ 절차대로: ⓐ main 최근 CI 런 3개(빨강이면 커밋·워커·원인) ⓑ 최근 ✅ 다섯 개의 «확인 수단» 을 실제로 다시 돌려 정말 도는지 ⓒ docs/claims/ 의 lock 나이(90분 넘은 것) ⓓ `screens` 브랜치 최신 PNG 를 정본 시트(.wwwww-src/web/ref/ · web/tools/shot-*.js)와 눈으로 대조. 발견은 docs/PROGRESS.md «검수 Q 보고» 에 적고 작업이 필요한 것은 `python3 tools/task_state.py --new-id` 번호로 ROUTINE §2 끝과 PROGRESS 표에 등재한다. 문서만 커밋(`[skip ci]` · 제목 끝 `(sess-… · 검수 Q)`)하고 push. 발견이 없으면 'QA_CLEAN' 출력 후 종료.
```

## 5. 빠른 체크리스트 (계정 하나 붙이기)

- [ ] 그 계정의 GitHub 연결 + Claude GitHub App 저장소 접근에 `kuzuni/unity1` (또는 Collaborator Write)
- [ ] 그 계정에서 세션 한 번 → `environment_id` 확보 → `docs/claims/README.md` 끝에 «계정 N 확인 YYYY-MM-DD» 한 줄 push 로 권한 확인
- [ ] §2 틀 + §4 프롬프트로 그 계정 슬롯 4개(계정 3 은 +Q) 생성 · `persist_session: true` · 모델 `claude-fable-5-1`
- [ ] 슬롯마다 `events[].data.message.uuid` 고유값
- [ ] `docs/ROUTINE.md` §6 ③ 표에 routine ID·첫 런 링크 기입 · 커밋 `[skip ci]`
- [ ] 첫 런 뒤 `docs/claims/` 에 lock 이 생기고 PROGRESS 행이 🔄 로 바뀌는지 확인

## 6. 운영 팁

- **한도 소진 징후**(런이 15초 만에 FAILED 연속) → 그 계정 루틴 전부 `claude-opus-5` 로(§2 전체 재전송). 풀리면 `claude-fable-5-1` 복원.
- 로컬(대화형) 세션은 직접 코딩하지 말고 등재·지시 정리만(주인 지시 · aaawunity 2026-09-05) — 급한 것은 «주인 콘솔 에러 보고함» 에 적으면 다음 워커가 가장 앞 작업으로 잡는다.
- 새 슬롯 추가 시 §4 프롬프트를 **그대로** 복붙(가드 프로토콜이 깨지면 직렬화가 무너진다).
