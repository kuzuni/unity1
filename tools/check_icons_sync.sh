#!/usr/bin/env bash
# check_icons_sync.sh — Assets/Forge/Icons/Resources/Icons/{atlas.json,atlas-N.png} 이 정본(kuzuni/wwwww web/js/icongen.js·avatars.js)에서
# 지금 headless Chromium 으로 뽑은 것과 같은가 (T31 · T2 의 check_data_sync.sh 와 같은 꼴).
#
#   tools/check_icons_sync.sh [<wwwww 체크아웃>] [--sync]
#     기본 체크아웃 = .wwwww-src · 다르면 rc 1 (어느 장이 어떻게 다른지 찍는다) · --sync 를 주면 새로 뽑아 덮고 .meta 를 만든다.
#   CI `datasync` 잡이 `tools/check_icons_sync.sh .wwwww-src` 로 부른다(러너의 google-chrome 을 쓴다 · Chromium 은 CHROMIUM 환경변수로도 준다).
#   PNG 는 픽셀 단위 대조(채널 차 8 · 0.2% 허용 — 브라우저 판 사이 AA 반올림) · atlas.json 은 글자 단위.
#   아틀라스는 손으로 고치지 않는다 — 이 스크립트(또는 node tools/export_icons.js)로만 갱신한다.
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
OUT="Assets/Forge/Icons/Resources/Icons"

if [ ! -f "$SRC/web/js/icongen.js" ]; then
  echo "✗ check_icons_sync: 정본이 없다: $SRC/web/js/icongen.js"
  echo "  고치는 법: git clone --depth 1 https://github.com/kuzuni/wwwww .wwwww-src"
  exit 1
fi
command -v node >/dev/null 2>&1 || { echo "✗ check_icons_sync: node 가 없다(node 22 필요)"; exit 1; }

if [ "$SYNC" = 1 ]; then
  node "$HERE/tools/export_icons.js" --src "$SRC" --out "$HERE/$OUT" || exit 1
  python3 "$HERE/tools/gen_meta.py" >/dev/null
  echo "✓ check_icons_sync --sync: $OUT 갱신함"
  exit 0
fi
node "$HERE/tools/export_icons.js" --src "$SRC" --compare "$HERE/$OUT"
rc=$?
if [ "$rc" != 0 ]; then
  echo "  고치는 법: tools/check_icons_sync.sh $SRC --sync 뒤 커밋 — 손으로 고치지 말 것"
fi
exit $rc
