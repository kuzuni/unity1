# 이 저장소에서 일하는 규칙 (kuzuni/unity1)

## 📜 지시서
`docs/ROUTINE.md` 가 **유일 지시서**다. 세션 시작 절차(§0) · 절대 규칙(§1) · 작업 목록(§2) · 게이트(§3) · 기록 규약(§4) · 계정별 루틴(§6)을 그대로 따른다.
정본은 `kuzuni/wwwww` 의 `web/` — **읽기 전용**(`git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src`). 조형 규약 원문은 wwwww `web/TODO.md` 상단 블록.

## 🔔 작업이 끝나면 ntfy 로 알린다 (주인 지시 2026-08-21 «이제 작업 완료하면 이거로 쏴» · 상시)

작업(한 덩어리)을 끝내고 push 한 **직후**:

```
GitHub MCP → actions_run_trigger
  method: run_workflow · owner: kuzuni · repo: unity1
  workflow_id: ntfy-notify.yml · ref: main
  inputs: { topic: "<사용자 토픽>", text: "<무엇을 끝냈는지 한 줄 + 커밋 7자리>" }
```

- 🚨 **컨테이너에서 직접 `curl https://ntfy.sh/...` 하지 말 것 — 반드시 실패한다**(클라우드 세션 egress 프록시가 `ntfy.sh:443` 을 403 으로 막는다 · wwwww 실측 2026-08-21). 그래서 GitHub 러너를 거친다.
- 🚨 **토픽 이름을 저장소 파일에 쓰지 말 것.** 공개 저장소 + ntfy 토픽은 인증이 없어 이름이 곧 발행·구독 권한이다. 토픽은 주인이 대화에서 알려준 것을 쓴다. Secret `NTFY_TOPIC` 이 등록되면 main CI 완료마다 **자동으로** 간다.
- 이 레포의 워크플로를 아직 못 부르면(Claude GitHub App 접근이 안 켜진 계정) `kuzuni/wwwww` 의 같은 이름 워크플로(`ntfy-notify.yml`)를 대신 부른다 — 입력은 같다.
- 🚨 **워커 K 실측(2026-09-15 21:4x · 결정 680)**: 이 세션에서는 **`workflow_dispatch` 자체가 막혀 있다.** REST 로 쳐 보면 GitHub 이 아니라 **에이전트 프록시**가 이렇게 답한다 —
  «Dispatching, enabling or disabling workflows and deleting workflow runs, logs or artifacts are **not permitted for this session type**»(`docs.anthropic.com/en/docs/claude-code/github-actions`).
  곧 **계정 권한 문제가 아니라 세션 종류의 문제**라 «Claude GitHub App 에 Actions 권한을 켜면 된다» 는 길은 **이 자리에선 안 열린다**(wwwww 쪽 워크플로도 같은 막힘이다).
  남는 길은 하나 — **Secret `NTFY_TOPIC` 등록**(그러면 main CI 가 완료마다 **스스로** 쏜다 · 워커가 부를 필요가 없다). 그때까지 워커는 «알림 못 보냄» 을 **보고에 솔직히 적는다**(보냈다고 적지 않는다).
- ✅ **바로잡음 — 막힘은 «세션마다» 다르다(워커 E 실측 2026-09-15 23:1x · 결정 684 · 워커 H 도 같은 것을 봤다)**: 위 결정 680 은 «세션 종류의 문제» 로 일반화했는데, **GitHub MCP `actions_run_trigger`(`method: run_workflow`)로 부르면 이 계정·이 세션에서는 실제로 런이 생긴다.** 증거 — `ntfy-notify.yml` 런 **#1577·#1578** 이 `event: workflow_dispatch` · `conclusion: success` 이고 행위자가 계정 2(`kuzuni2`)다. 곧 결정 680 이 본 것은 **REST 로 친 그 세션의 막힘**이지 모든 워커의 막힘이 아니다. ⇒ **규칙**: ⓐ 먼저 MCP 로 부른다. ⓑ 부른 뒤 `actions_list`(`list_workflow_runs` · `ntfy-notify.yml`)로 **런이 생겼는지 확인하고** 보고에 «쐈다 + 런 번호» 로 적는다 — «쐈다» 만 적으면 결정 680 의 세션에서는 거짓이 된다. ⓒ 프록시가 «not permitted for this session type» 으로 막으면 **그때** «알림 못 보냄» 을 적는다(막혔다고 미리 적지 않는다).
- ⚠ Claude 앱 푸시(`PushNotification`)와 Routine 완료 알림은 **도착하지 않는다**(2026-08-21 실측). 알림 경로는 ntfy 하나.
- 문구는 한 줄로 **무엇을 끝냈는지**(«작업 완료» 는 정보가 0이다).

## 🎨 조형·게임 규약
- 펫·탈것·적·소품·스킬 오브젝트 = 마인크래프트 몹 문법. 종 표는 정본(wwwww `web/js/mobs*.js`)이 단독으로 쥐고 유니티는 JSON 으로 받아 세운다 — 유니티에서 종 좌표를 손으로 고치지 않는다.
- 탑승 = 탈것 위에 서기 · 무기 = 마크 handheld 파지각(`docs/ROUTINE.md` §1 «조형 계약»).
- `Assets/Scripts/Core` 는 UnityEngine 참조 0. 수치는 코드에 박지 않는다.
