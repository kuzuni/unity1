#!/usr/bin/env bash
# T184 — §3 게이트 한 자리.
#
# 왜 있나: §3 이 «손으로 복붙하는 스무 줄» 이던 동안 사람이 `| tail -3`·`&& echo ok` 를 덧붙이며
# rc 를 잃었고, 컴파일 안 되는 커밋이 main 에 실려 런 435·436 이 빨갰다(임자 T106 이 `45c03d5`
# 제목에 스스로 적었다: «내 빌드 확인 줄이 오류를 삼켰다»). §3 머리말은 그것을 **말로** 이미
# 경고하고 있었다 — 말로 있는 규칙은 샌다(T123·T125·T148·T153·T174 가 되풀이해 배운 것).
#
# 규약 셋:
#   · 목록은 **여기 한 곳**에 있다(아래 GATES). §3 은 «이것을 돌려라» 한 줄이다.
#   · `set -e` 를 쓰지 않는다 — 전부 돌린 뒤 모아서 보여 준다(회차 리듬에 맞는다).
#     대신 **막는 자가 하나라도 0이 아니면 이 스크립트가 0이 아닌 값으로 끝난다**.
#   · «보고 전용»(report)은 rc 를 안 센다 — ci.yml 이 `continue-on-error: true` 로 둔 것과
#     T127 이 rc 0 고정으로 정한 `check_lock_queue` 가 그것이다.
#
# 쓰기:
#   tools/gate.sh              전부 돌린다 (rc 0 = 초록)
#   tools/gate.sh --list       자 이름만 찍는다 (CI 가 이 목록을 읽는다)
#   tools/gate.sh --check-ci   이 목록 ↔ .github/workflows/ci.yml 이 부르는 이름 대조
#   tools/gate.sh --self-test  자기 검사 (고장 주입)
#
# 고치는 일이 아니다 — **부르는 방법만** 하나로 모은다. 자들 자체는 손대지 않았다.

set -uo pipefail
cd "$(dirname "$0")/.." || exit 2

# mode|need|label|command
#   mode : block  = rc 를 센다 (0이 아니면 전체가 빨강)
#          report = 알리기만 (ci.yml 의 continue-on-error 와 같은 자리)
#   need : -      = 언제나 돈다
#          dotnet / node / wwwww / gh = 그것이 없으면 SKIP (빨강 아님 · 꼬리에 적힌다)
read -r -d '' GATES <<'TABLE'
block|dotnet|dotnet build (컴파일 · Core·Game스텁·Tests·TestsPlay · T48)|dotnet build tools/dotnet/Forge.sln -c Release --nologo
block|dotnet|dotnet test (EditMode 순수 C#)|dotnet test tools/dotnet/Tests/Forge.Tests.csproj -c Release --no-build --nologo
block|-|.meta 누락/고아 (T46)|python3 tools/gen_meta.py --check
block|-|catalog.json ↔ UiCatalog.asset · 키 중복 (T41)|python3 tools/gen_ui_catalog.py --check
block|-|Resources 표가 전부 읽히는가 + .meta 짝 (T182)|python3 tools/check_resources_json.py
block|-|문서가 통째로 깨졌는가 (T47)|python3 tools/check_docs_intact.py
report|-|결정 번호 겹침 (T42)|python3 tools/check_decisions.py
report|-|PROGRESS 표 행 어긋남 (T70)|python3 tools/check_task_rows.py
block|-|ROUTINE §2 제목 ↔ PROGRESS 상태 · 번호 중복 (T29 · ⛔ 선점 덫은 rc 1 이라 막는다 · ⚠ 참고는 rc 0 · T403)|python3 tools/task_state.py --check
report|-|산 lock 이 «범위» 밖 파일을 쥐고 있는가 (T71)|python3 tools/check_claim_scope.py
report|-|§7 완결 대조표 ↔ PROGRESS 상태 (T49)|python3 tools/check_final_table.py
report|-|내 lock 뒤에 선 작업 (T127 · rc 늘 0)|python3 tools/check_lock_queue.py
block|-|원작 대조 자 자기 검사 (T28)|python3 tools/ui_score.py --self-test
block|-|화면 문구의 글자가 글꼴에 다 있는가 — 새 두부 □ (T89)|python3 tools/check_text_glyphs.py
block|-|MonoBehaviour 가 유니티 «메시지» 이름을 다른 뜻으로 쓰는가 (T171)|python3 tools/check_unity_messages.py
block|-|TMP richText 가 글자 공장 밖에서 켜지는가 (T175)|python3 tools/check_richtext.py
block|-|PlayMode 자가 앱 뿌리부터 «화면 둘이 쓰는 이름» 으로 찾는가 (T414 · 임자를 못 가리는 빨강이 나는 길)|python3 tools/check_test_scope.py
block|-|클릭 막이 자 자기 검사 (T33 24회차 `check_raycast.py --self-test` · 10칸)|python3 tools/check_raycast.py --self-test
block|-|클릭 막이 자 (T33 24회차 · 정본 pointer-events 68 ↔ 공장 밖 Image 의 raycastTarget)|python3 tools/check_raycast.py
block|-|이름으로 찾는 것이 빌드에도 실리는가 (T126)|python3 tools/check_shaders_included.py
block|-|원작 소리 24종이 실제로 울리는가 (T119)|python3 tools/check_sfx_calls.py
block|-|하니스 스텁이 실물에 없는 서명을 갖고 있나 (T174)|python3 tools/check_stub_sigs.py
block|-|screens 장부 합치기 자 자기 검사 (T336 · CI 배포 스텝도 밀기 전에 같은 자를 돌린다)|python3 tools/screens_ledger.py --self-test
block|-|screens 이어받기 자 자기 검사 (T347 · 부분 실패 런이 남의 그림을 지우던 자리 · CI 배포 스텝도 같은 자를 돌린다)|python3 tools/screens_carry.py --self-test
block|wwwww|정본 바깥 그림자 61자리가 클론에 서 있는가 (T331 · 안쪽 inset 은 안 본다)|python3 tools/check_box_shadows.py
block|-|`using` 한 네임스페이스를 그 asmdef 가 참조하는가 (T343 · 하니스가 구조적으로 못 잡는 갈래)|python3 tools/check_asmdef_refs.py
block|wwwww|data/*.json ↔ 정본 (T2)|tools/check_data_sync.sh .wwwww-src
block|wwwww|정본 -webkit-text-stroke ↔ 클론 키라인 (T109)|python3 tools/check_keyline.py --css .wwwww-src/web/css/style.css
block|wwwww|정본 clip-path 도형 ↔ 클론이 굽는가 (T159)|python3 tools/check_clip_paths.py --css .wwwww-src/web/css/style.css
block|wwwww|정본 gradient 겹 ↔ 클론이 굽는가 (T178)|python3 tools/check_surface_gradients.py --css .wwwww-src/web/css/style.css
block|wwwww|정본 letter-spacing ↔ 클론 자간 + 박힌 숫자 (T168)|python3 tools/check_letter_spacing.py --css .wwwww-src/web/css/style.css
block|wwwww|정본 text-shadow ↔ 클론 글자 그림자(TMP Underlay) (T333 · 키라인 표의 10 은 T109 몫 · 표에 없는 선택자는 «미정» 으로 세기만 한다)|python3 tools/check_text_shadows.py --css .wwwww-src/web/css/style.css
block|wwwww|정본 하한 위 글자(1.22~1.5rem) 35 선택자 ↔ 클론 글자 종류 px ±12% (T391 · 글리프 자리는 SKIP · 임자 있는 빈자리는 KNOWN)|python3 tools/check_text_kinds.py --css .wwwww-src/web/css/style.css
block|wwwww|정본 border-radius ↔ 클론 둥근 모서리 표값 (T345 · 50% 는 Circle · 알약 동치 결정 543 · 표에 없는 선택자는 «미정» 으로 세기만 한다)|python3 tools/check_border_radius.py --css .wwwww-src/web/css/style.css
block|wwwww|정본이 선택자에만 리터럴로 못박은 면·잉크 색 ↔ 클론이 그 값을 쓰는가 (T377 면 · T396 잉크 · 전역 토큰은 안 건드린다 · 표에 없는 선택자는 «미정» 으로 세기만 한다)|python3 tools/check_pinned_colors.py --css .wwwww-src/web/css/style.css
block|wwwww|정본 상자 테 border 186 자리 ↔ 클론 폭 단(ol1~ol4 = line_px·line2_px·line3_px·line4_px) (T365 · 임자 있는 빈자리는 KNOWN · 표에 없는 선택자는 «미정» 으로 세기만 한다)|python3 tools/check_box_borders.py --css .wwwww-src/web/css/style.css
block|-|촬영·픽셀 카메라가 UI 층만 그리는데 후처리를 켜 두었나 (T349 · 켜면 재는 값이 밀린다 · ShotCam.From 갈래까지 본다)|python3 tools/check_shot_cams.py
block|wwwww|정본 white-space 41 ↔ 표(T361 · 접는다가 기본 · nowrap 40 자리 · 공장 뒤집기 전에도 표가 정본과 안 어긋나게 지킨다)|python3 tools/check_wrap.py --css .wwwww-src/web/css/style.css
block|wwwww|정본 <br> 22 자리 ↔ 클론이 그 줄 수를 아는가 (T383 · «한 상자» 는 \n · «상자 여럿» 은 split · 화면 줄 수는 PlayMode BrLinesTests 몫)|python3 tools/check_br_lines.py --ui .wwwww-src/web/js/ui.js
block|wwwww|정본 color-mix 23 ↔ 표 ColorMixUi.json ↔ 그 자리가 표 키를 부르는가 (T371 · 임자 있는 빈자리는 KNOWN · 표에 없는 선언은 «미정» 으로 세기만 한다)|python3 tools/check_color_mix.py --css .wwwww-src/web/css/style.css
block|node|추출기 자기 검사 (T2)|node tools/export_data.js --self-test
report|gh|유니티 잡이 실제로 돈 마지막 main 런이 초록인가 (T123 · §0-6 의 눈)|python3 tools/check_unity_green.py --fetch
block|-|이 목록 ↔ ci.yml 이 부르는 이름 — 막는다 (T184 ⓑ · CI 도 같은 자를 돌린다)|tools/gate.sh --check-ci
TABLE

gate_table() {
  if [ -n "${GATE_TABLE_FILE:-}" ]; then cat "$GATE_TABLE_FILE"; else printf '%s\n' "$GATES"; fi
}

have() {
  case "$1" in
    -)      return 0 ;;
    dotnet) command -v dotnet >/dev/null 2>&1 ;;
    node)   command -v node   >/dev/null 2>&1 ;;
    wwwww)  [ -d .wwwww-src ] ;;
    gh)     [ -n "${GATE_FAKE_GH:-}" ] || command -v gh >/dev/null 2>&1 || [ -n "${GH_TOKEN:-}${GITHUB_TOKEN:-}" ] ;;
    *)      echo "  ⚠ 모르는 준비물 키 «$1» — 오타가 자를 조용히 끄지 않게 **그냥 돌린다**" >&2; return 0 ;;
  esac
}

miss_hint() {
  case "$1" in
    dotnet) echo "dotnet 없음 — §3 대로 «apt-get install -y dotnet-sdk-8.0» 을 먼저 시도하고, 그래도 없으면 **완료 기록에 적고** CI dotnet 잡 초록을 본 뒤 lock 을 반납한다" ;;
    node)   echo "node 없음" ;;
    wwwww)  echo ".wwwww-src 없음 — §0-3 의 «git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src»" ;;
    gh)     echo "gh·토큰 없음 — 이 자는 네트워크가 있어야 한다" ;;
    *)      echo "$1 없음" ;;
  esac
}

# --- 목록 ---------------------------------------------------------------
do_list() {
  gate_table | while IFS='|' read -r mode need label cmd; do
    [ -n "${mode:-}" ] || continue
    printf '%s\t%s\t%s\n' "$mode" "$need" "$cmd"
  done
}

# --- 목록 ↔ ci.yml 대조 (ⓒ) ---------------------------------------------
do_check_ci() {
  local ci="${1:-.github/workflows/ci.yml}"
  [ -f "$ci" ] || { echo "✗ gate --check-ci: $ci 가 없다"; return 1; }
  local missing=0 n=0
  while IFS='|' read -r mode need label cmd; do
    [ -n "${mode:-}" ] || continue
    [ "$mode" = report ] && continue          # 보고 전용은 CI 밖이어도 된다
    [ "$need" = gh ] && continue
    n=$((n+1))
    # 명령의 첫 «파일 경로» 를 이름으로 삼는다 (tools/x.py · dotnet build …)
    local key
    key=$(printf '%s\n' "$cmd" | grep -o 'tools/[A-Za-z0-9_./-]*' | head -1)
    [ -n "$key" ] || key=$(printf '%s\n' "$cmd" | awk '{print $1}')
    if grep -qF -- "$key" "$ci"; then
      printf '  ✓ %s\n' "$key"
    else
      printf '  ✗ %s — §3 은 돌리는데 ci.yml 이 안 부른다 (%s)\n' "$key" "$label"
      missing=$((missing+1))
    fi
  done < <(gate_table)
  if [ "$missing" -gt 0 ]; then
    echo "✗ gate --check-ci: 막는 자 $n 중 $missing 가 CI 밖이다 — 이 목록(--list)을 ci.yml 이 그대로 읽게 해야 ⓑ 가 닫힌다"
    return 1
  fi
  echo "✓ gate --check-ci: 막는 자 $n 전부 ci.yml 이 부른다"
  return 0
}

# --- 돌리기 -------------------------------------------------------------
do_run() {
  local total=0 bad=0 skipped=0 i=0
  local -a fail_names=() fail_out=() skip_notes=()
  local tmp; tmp=$(mktemp)
  gate_table > "$tmp"
  total=$(grep -cve '^[[:space:]]*$' "$tmp")
  echo "══ tools/gate.sh — 게이트 $total 자 (§3) ══"
  while IFS='|' read -r mode need label cmd; do
    [ -n "${mode:-}" ] || continue
    i=$((i+1))
    if ! have "$need"; then
      skipped=$((skipped+1))
      skip_notes+=("$label — $(miss_hint "$need")")
      printf '[%2d/%2d] SKIP   %-6s %s\n' "$i" "$total" "$mode" "$label"
      continue
    fi
    local out rc
    out=$(eval "$cmd" 2>&1); rc=$?
    printf '[%2d/%2d] rc=%-4d %-6s %s\n' "$i" "$total" "$rc" "$mode" "$label"
    if [ "$rc" -ne 0 ]; then
      if [ "$mode" = block ]; then
        bad=$((bad+1)); fail_names+=("$label"); fail_out+=("$cmd"$'\n'"$out")
      else
        fail_names+=("(보고) $label"); fail_out+=("$cmd"$'\n'"$out")
      fi
    fi
  done < "$tmp"
  rm -f "$tmp"

  if [ "${#fail_out[@]}" -gt 0 ]; then
    echo
    echo "── 0이 아니게 끝난 자의 출력 ──"
    local k=0
    while [ "$k" -lt "${#fail_out[@]}" ]; do
      echo "▼ ${fail_names[$k]}"
      # 막는 자는 통째로(고쳐야 하니까) · 보고 전용은 25줄까지(§0-6 의 눈이지 이 회차의 빨강이 아니다)
      case "${fail_names[$k]}" in
        "(보고) "*) printf '%s\n' "${fail_out[$k]}" | head -25 | sed 's/^/    /'
                   local nl; nl=$(printf '%s\n' "${fail_out[$k]}" | wc -l)
                   [ "$nl" -gt 25 ] && echo "    … $((nl-25))줄 더 — 그 자를 직접 돌려 봐라" ;;
        *)         printf '%s\n' "${fail_out[$k]}" | sed 's/^/    /' ;;
      esac
      echo
      k=$((k+1))
    done
  fi
  if [ "$skipped" -gt 0 ]; then
    echo "── 건너뛴 자 $skipped ──"
    local s; for s in "${skip_notes[@]}"; do echo "  · $s"; done
  fi
  echo
  if [ "$bad" -gt 0 ]; then
    echo "✗ gate.sh: 막는 자 $bad 가 0이 아니다 — 이대로 커밋하지 마라(§3)."
    return 1
  fi
  if [ "$skipped" -gt 0 ]; then
    echo "✓ gate.sh: 막는 자 전부 rc 0 · 건너뛴 자 $skipped"
    echo "  ⚑ 건너뛴 자가 있으면 그 사실을 완료 기록에 적는다(§3) — 그때는 CI 잡 초록을 본 뒤 lock 을 반납한다."
  else
    echo "✓ gate.sh: 막는 자 전부 rc 0 · 건너뛴 자 없다"
  fi
  return 0
}

# --- 자기 검사 ----------------------------------------------------------
st_pass=0; st_fail=0
ok()  { if [ "$1" = "$2" ]; then st_pass=$((st_pass+1)); else st_fail=$((st_fail+1)); echo "  ✗ $3 — 바란 값 «$1» 실제 «$2»"; fi; }
has() { case "$2" in *"$1"*) st_pass=$((st_pass+1));; *) st_fail=$((st_fail+1)); echo "  ✗ $3 — «$1» 이 출력에 없다";; esac; }
hasnt(){ case "$2" in *"$1"*) st_fail=$((st_fail+1)); echo "  ✗ $3 — «$1» 이 출력에 있으면 안 된다";; *) st_pass=$((st_pass+1));; esac; }

do_self_test() {
  local d; d=$(mktemp -d); local self="$PWD/tools/gate.sh"
  echo "══ gate.sh 자기 검사 ══"

  # ⓐ 전부 초록이면 rc 0
  printf 'block|-|늘 되는 자|true\nblock|-|또 되는 자|true\n' > "$d/t1"
  local out rc
  out=$(GATE_TABLE_FILE="$d/t1" "$self" 2>&1); rc=$?
  ok 0 "$rc" "ⓐ 전부 초록이면 rc 0"
  has "rc=0" "$out" "ⓐ 자마다 rc 를 찍는다"
  has "✓ gate.sh" "$out" "ⓐ 초록 꼬리"

  # ⓑ 막는 자 하나가 넘어지면 rc 1 · 그 줄이 rc=1 로 찍힌다  ← 런 435·436 이 놓친 자리
  printf 'block|-|되는 자|true\nblock|-|넘어지는 자|false\n' > "$d/t2"
  out=$(GATE_TABLE_FILE="$d/t2" "$self" 2>&1); rc=$?
  ok 1 "$rc" "ⓑ 막는 자가 넘어지면 rc 1"
  has "넘어지는 자" "$out" "ⓑ 넘어진 자 이름"
  has "rc=1" "$out" "ⓑ 그 줄이 rc=1"
  has "이대로 커밋하지 마라" "$out" "ⓑ 빨강 꼬리"

  # ⓒ «고치는 법» 으로 끝나는 출력에 속지 않는다 (§3 머리말이 말로만 경고하던 것)
  printf 'block|-|친절한 실패|sh -c %s\n' "'echo 고치는_법:_이렇게_해라; exit 3'" > "$d/t3"
  out=$(GATE_TABLE_FILE="$d/t3" "$self" 2>&1); rc=$?
  ok 1 "$rc" "ⓒ 꼬리가 «고치는 법» 이어도 rc 로 가른다"
  has "rc=3" "$out" "ⓒ 진짜 rc 를 찍는다"

  # ⓓ 보고 전용은 넘어져도 rc 0 (ci.yml continue-on-error · T127)
  printf 'report|-|보고 전용 자|false\n' > "$d/t4"
  out=$(GATE_TABLE_FILE="$d/t4" "$self" 2>&1); rc=$?
  ok 0 "$rc" "ⓓ 보고 전용은 rc 를 안 센다"
  has "(보고)" "$out" "ⓓ 보고 전용이라고 적는다"

  # ⓔ set -e 가 아니다 — 앞이 넘어져도 뒤가 돈다
  printf 'block|-|앞|false\nblock|-|뒤|false\nreport|-|끝|true\n' > "$d/t5"
  out=$(GATE_TABLE_FILE="$d/t5" "$self" 2>&1); rc=$?
  ok 1 "$rc" "ⓔ 둘 다 넘어지면 rc 1"
  has "막는 자 2 가 0이 아니다" "$out" "ⓔ 앞에서 안 멈추고 둘 다 센다"
  has "[ 3/ 3] rc=" "$out" "ⓔ 마지막 자까지 간다"

  # ⓕ 준비물: 모르는 키는 **조용히 안 꺼진다**(오타가 자를 끄면 그것이 T184 가 막는 사고다)
  printf 'block|없는것|오타 난 준비물|false\n' > "$d/t6"
  out=$(GATE_TABLE_FILE="$d/t6" "$self" 2>&1); rc=$?
  ok 1 "$rc" "ⓕ 모르는 준비물 키는 자를 끄지 않는다 — 그냥 돌린다"
  has "모르는 준비물 키" "$out" "ⓕ 오타를 크게 알린다"

  printf 'block|wwwww|정본이 필요한 자|false\n' > "$d/t7"
  ( cd "$d" && out=$(GATE_TABLE_FILE="$d/t7" "$self" 2>&1); echo "$?" > "$d/rc7"; printf '%s' "$out" > "$d/o7" )
  # (gate.sh 는 제 레포로 cd 하므로 .wwwww-src 는 레포 것이 보인다 — 여기서는 있는 쪽을 확인한다)
  if [ -d .wwwww-src ]; then
    ok 1 "$(cat "$d/rc7")" "ⓕ 정본이 있으면 그 자는 실제로 돈다(넘어지면 빨강)"
  else
    ok 0 "$(cat "$d/rc7")" "ⓕ 정본이 없으면 SKIP"
    has "SKIP" "$(cat "$d/o7")" "ⓕ SKIP 이라고 찍는다"
  fi

  # ⓖ --list
  out=$(GATE_TABLE_FILE="$d/t1" "$self" --list 2>&1); rc=$?
  ok 0 "$rc" "ⓖ --list rc 0"
  ok 2 "$(printf '%s\n' "$out" | grep -c .)" "ⓖ --list 가 자마다 한 줄"
  has "true" "$out" "ⓖ --list 가 명령을 찍는다"

  # ⓗ --check-ci 가 «CI 밖» 을 집어낸다
  printf 'block|-|안에 있는 자|python3 tools/aaa.py\nblock|-|밖에 있는 자|python3 tools/bbb.py\n' > "$d/t8"
  printf 'jobs:\n  x:\n    steps:\n      - run: python3 tools/aaa.py\n' > "$d/ci-half.yml"
  out=$(GATE_TABLE_FILE="$d/t8" "$self" --check-ci "$d/ci-half.yml" 2>&1); rc=$?
  ok 1 "$rc" "ⓗ CI 가 안 부르는 막는 자가 있으면 rc 1"
  has "tools/bbb.py" "$out" "ⓗ 빠진 이름을 댄다"
  hasnt "✗ tools/aaa.py" "$out" "ⓗ 있는 것은 안 댄다"

  printf 'jobs:\n  x:\n    steps:\n      - run: python3 tools/aaa.py\n      - run: python3 tools/bbb.py\n' > "$d/ci-full.yml"
  out=$(GATE_TABLE_FILE="$d/t8" "$self" --check-ci "$d/ci-full.yml" 2>&1); rc=$?
  ok 0 "$rc" "ⓗ 다 부르면 rc 0"

  # ⓘ 보고 전용은 CI 밖이어도 --check-ci 가 안 나무란다 (T127 의 check_lock_queue)
  printf 'report|-|보고 전용|python3 tools/zzz.py\n' > "$d/t9"
  out=$(GATE_TABLE_FILE="$d/t9" "$self" --check-ci "$d/ci-full.yml" 2>&1); rc=$?
  ok 0 "$rc" "ⓘ 보고 전용은 CI 밖이어도 된다"

  # ⓙ 실물 고장 주입 — Core 에 컴파일 오류 한 줄 (§2 의 판정 ⓐ)
  if command -v dotnet >/dev/null 2>&1; then
    local victim="Assets/Scripts/Core/__GateSelfTest.cs"
    printf 'namespace Forge.Core { public class __GateSelfTest { void X() { 이건 컴파일이 안 된다 } } }\n' > "$victim"
    printf 'block|dotnet|dotnet build|dotnet build tools/dotnet/Forge.sln -c Release --nologo\n' > "$d/t10"
    out=$(GATE_TABLE_FILE="$d/t10" "$self" 2>&1); rc=$?
    rm -f "$victim"
    ok 1 "$rc" "ⓙ Core 에 컴파일 오류 한 줄 → gate.sh 가 0이 아니다"
    hasnt "rc=0" "$out" "ⓙ 그 줄이 rc=0 으로 안 찍힌다"
    dotnet build tools/dotnet/Forge.sln -c Release --nologo >/dev/null 2>&1
  else
    echo "  · ⓙ 실물 고장 주입은 건너뛴다(dotnet 없음) — CI dotnet 잡이 같은 것을 본다"
  fi

  rm -rf "$d"
  echo "── 자기 검사 $((st_pass+st_fail))칸 · 초록 $st_pass · 빨강 $st_fail"
  [ "$st_fail" -eq 0 ] || return 1
  return 0
}

case "${1:-}" in
  --list)      do_list ;;
  --check-ci)  do_check_ci "${2:-}" ;;
  --self-test) do_self_test ;;
  -h|--help)   sed -n '1,30p' "$0" ;;
  "")          do_run ;;
  *)           echo "모르는 인자: $1 (--list · --check-ci · --self-test)"; exit 2 ;;
esac
