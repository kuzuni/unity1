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
- ⚠ Claude 앱 푸시(`PushNotification`)와 Routine 완료 알림은 **도착하지 않는다**(2026-08-21 실측). 알림 경로는 ntfy 하나.
- 문구는 한 줄로 **무엇을 끝냈는지**(«작업 완료» 는 정보가 0이다).

## 🎨 조형·게임 규약
- 펫·탈것·적·소품·스킬 오브젝트 = 마인크래프트 몹 문법. 종 표는 정본(wwwww `web/js/mobs*.js`)이 단독으로 쥐고 유니티는 JSON 으로 받아 세운다 — 유니티에서 종 좌표를 손으로 고치지 않는다.
- 탑승 = 탈것 위에 서기 · 무기 = 마크 handheld 파지각(`docs/ROUTINE.md` §1 «조형 계약»).
- `Assets/Scripts/Core` 는 UnityEngine 참조 0. 수치는 코드에 박지 않는다.
