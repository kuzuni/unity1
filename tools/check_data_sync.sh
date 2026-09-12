#!/usr/bin/env bash
# check_data_sync.sh — Assets/StreamingAssets/data/*.json 이 정본(kuzuni/wwwww web/)에서 지금 뽑은 것과 같은가 (T2).
#
#   tools/check_data_sync.sh [<wwwww 체크아웃>] [--sync]
#     기본 체크아웃 = .wwwww-src (ROUTINE §0 · git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src)
#     다르면 rc 1 (어느 파일이 다른지 찍는다) · --sync 를 주면 새로 뽑은 것을 복사하고 .meta 를 만든다.
#   CI `datasync` 잡이 `tools/check_data_sync.sh .wwwww-src` 로 부른다 — 정본 main 이 움직이면 여기서 빨개진다.
#   data/*.json 은 손으로 고치지 않는다 — 이 스크립트(또는 node tools/export_data.js)로만 갱신한다(ROUTINE §1).
set -u
HERE="$(cd "$(dirname "$0")/.." && pwd)"
SRC=".wwwww-src"; SYNC=0
for a in "$@"; do
  case "$a" in
    --sync) SYNC=1 ;;
    -h|--help) sed -n 2,9p "$0"; exit 0 ;;
    *) SRC="$a" ;;
  esac
done
case "$SRC" in /*) ;; *) SRC="$HERE/$SRC" ;; esac
DATA="$HERE/Assets/StreamingAssets/data"
FILES="balance.json gamedata.json mobs-pets.json mobs-mounts.json mobs-enemies.json mobs-props.json mobs-skillfx.json state.json tech.json meta.json scene.json sfx.json"

if [ ! -d "$SRC/web/js" ]; then
  echo "✗ check_data_sync: 정본이 없다: $SRC/web/js"
  echo "  고치는 법: git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src"
  exit 1
fi
command -v node >/dev/null 2>&1 || { echo "✗ check_data_sync: node 가 없다(node 22 필요)"; exit 1; }

TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT
if ! node "$HERE/tools/export_data.js" --src "$SRC" --out "$TMP" >/dev/null; then
  echo "✗ check_data_sync: 추출기가 실패했다 — node tools/export_data.js --src $SRC 를 직접 돌려 보라"
  exit 1
fi

rc=0; diffs=""
for f in $FILES; do
  if [ ! -f "$DATA/$f" ]; then diffs="$diffs $f(없음)"; rc=1
  elif ! cmp -s "$TMP/$f" "$DATA/$f"; then diffs="$diffs $f"; rc=1; fi
done

if [ "$rc" = 0 ]; then
  echo "✓ check_data_sync: data/*.json $(echo $FILES | wc -w)개가 정본($SRC)과 같다"
  exit 0
fi
if [ "$SYNC" = 1 ]; then
  mkdir -p "$DATA"
  for f in $FILES; do cp "$TMP/$f" "$DATA/$f"; done
  python3 "$HERE/tools/gen_meta.py" >/dev/null
  echo "✓ check_data_sync --sync: 갱신함:$diffs"
  exit 0
fi
echo "✗ check_data_sync: 정본과 다른 파일:$diffs"
echo "  고치는 법: tools/check_data_sync.sh $SRC --sync  (또는 node tools/export_data.js) 뒤 커밋 — 손으로 고치지 말 것"
exit 1
