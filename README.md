# 포지 클론 — 유니티 이식 (unity1)

[kuzuni/wwwww](https://github.com/kuzuni/wwwww) 의 `web/`(HTML + Three.js · 9:16 세로 방치 RPG «포지마스터 클론» · 마인크래프트 몹 문법 복셀 조형)를 **Unity 6000.3.8f1 (URP)** 로 옮기는 레포.
규칙·수치·조형의 **정본은 wwwww** 이고 이 레포는 그것을 읽어 실행한다. 웹판과 섞이지 않게 레포를 따로 팠다(주인 결정 2026-09-12).

| 항목 | 값 |
|---|---|
| 유니티 | **6000.3.8f1** (`ProjectSettings/ProjectVersion.txt` · 주인이 올린 «기본» 프로젝트: URP · TMP · Input System · GUI PRO Kit Casual · Cartoon FX Remaster · DOTween · AllIn1SpriteShader) |
| 수치·조형 | `Assets/StreamingAssets/data/*.json` = wwwww `web/js/{balance-data,gamedata,mobs-*}.js` 를 `tools/export_data.js`(T2)로 뽑은 것 — 손으로 고치지 않는다 |
| 코드 | `Assets/Scripts/Core` 순수 C# 엔진(UnityEngine 참조 0 · 전투·대장간·펫·스킬·복셀 기하) · `Assets/Scripts/Game` MonoBehaviour · `Assets/Tests` EditMode/PlayMode |
| 검사 | `tools/dotnet` — 유니티 없이 `dotnet build`/`dotnet test` |
| 운영 | `docs/ROUTINE.md`(**유일 지시서**) · `docs/PROGRESS.md`(진행표) · `docs/claims/`(lock) · `docs/ROUTINES-SETUP.md`(계정별 루틴 런북) |
| 알림 | `.github/workflows/ntfy-notify.yml` — main CI 완료 시 ntfy(Secret `NTFY_TOPIC`) · `CLAUDE.md` |

## 내가(주인) 할 일 — 한 번만

1. **Claude GitHub App 저장소 접근에 `kuzuni/unity1` 켜기** (claude.ai 설정 → GitHub) — 안 켜면 세션이 push 를 못 한다. 다른 계정을 더 붙일 때도 그 계정에서 같은 것(`docs/ROUTINE.md` §6 ①).
2. 레포 **Settings → Secrets and variables → Actions**:
   - `UNITY_EMAIL` · `UNITY_PASSWORD` — 유니티 계정(Personal 이면 된다). 없으면 CI 는 dotnet 검사만 돌고 초록.
   - `UNITY_LICENSE` — **굽기(WebGL)** 에만 필요(`unity-builder` 는 계정 방식을 안 받는다). 테스트 잡은 이 값을 **안 쓴다**(주면 «TimeStamp validation failed» 로 죽는 aaawunity 실측).
   - `NTFY_TOPIC` — ntfy 토픽(공개 저장소이므로 파일에 적지 않는다).
3. **Settings → Pages → Source: «Deploy from a branch» → `gh-pages` / `(root)`** (첫 WebGL 빌드가 끝나면 브랜치가 생긴다 · 주소 `https://kuzuni.github.io/unity1/`).
4. 계정마다 루틴 만들기: `docs/ROUTINES-SETUP.md`.

## 로컬에서 확인 (유니티 없이)

```bash
dotnet build tools/dotnet/Forge.sln -c Release
dotnet test  tools/dotnet/Tests/Forge.Tests.csproj
python3 tools/gen_meta.py [--check]      # 새 에셋의 .meta 생성 / 누락 검사
python3 tools/check_docs_intact.py       # 문서 손상
python3 tools/task_state.py --check      # 작업 표 대조
```

## 구조

```
Assets/
  Scenes/SampleScene.unity     Bootstrap 하나(T1) — UI·조형은 코드로 생성
  Scripts/Core/                순수 C#: Data(JSON) · BigNum · Voxel(기하) · Battle · Forge · Pets · Skills · Save · Dungeons · Tech · Meta
  Scripts/Game/                MonoBehaviour: Bootstrap · Voxel(VoxelMob) · Hero · Battle · World · Pets · Mounts · SkillFx · Ui · SaveIo
  StreamingAssets/data/*.json  정본에서 뽑은 표(T2)
  Tests/EditMode · PlayMode
tools/dotnet/                  dotnet 검사 프로젝트 (Core/Game/Tests + Stubs)
tools/*.py                     문서·lock 검사 자 (aaawunity 에서 옮김)
.github/workflows/ci.yml · ntfy-notify.yml
docs/ROUTINE.md · PROGRESS.md · ROUTINES-SETUP.md · claims/
```

## 규칙 (요약 — 상세 `docs/ROUTINE.md` §1)

- wwwww 는 **읽기 전용**. 수치를 바꾸거나 밸런스를 조정하지 않는다.
- 코드에 수치를 직접 박지 않는다 — `GameData` 에서 읽는다.
- 조형은 마인크래프트 몹 문법(축정렬 직육면체 + 칸 색) · 종 좌표는 정본 표가 쥔다.
- 새 시스템·새 기능은 주인 승인 없이 추가하지 않는다. 판단은 워커가 정하고 «워커 결정 기록» 에 남긴다.
- 커밋 전 `dotnet build` 초록 · 컴파일 파손 push 금지.
