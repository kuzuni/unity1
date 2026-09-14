#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T182 — `Resources/` 아래 JSON 표가 **읽히는가**(게임이 읽기 전에 여기서 막는다).

왜 이 자가 있나(런 427 실측): `Assets/Forge/Resources/RibbonUi.json` 13행의 문자열이 안 닫혀 날 개행이
들어가자 `MiniJson` 이 «control character in string» 으로 던졌고, 그 표를 읽는 `ForgeUi.Ribbon` 을 지나는
**PlayMode 20개**가 같이 죽었다. 민 워커의 로컬 게이트(`dotnet build`·`dotnet test`·§3 자 열여섯)는 **전부 초록**이었다 —
표를 보는 자가 `data/*.json`(T2)과 `catalog.json`(T41)뿐이고, `Resources/` 의 **곁 표**들은 아무도 안 읽었기 때문이다.
그 곁 표는 «`catalog.json` 이 남의 lock 일 때가 잦아서» 늘어난 갈래라(T65 이래) 앞으로도 는다.

잣대는 게임이 쓰는 `MiniJson`(Assets/Scripts/Core/Data/MiniJson.cs)에 맞춘다 — 파이썬 `json` 보다 **더 좁게**:
  · 문자열 안 제어문자(0x20 미만) 금지 — 런 427 이 여기서 터졌다
  · 꼬리 쉼표 금지 · 주석 금지 · NaN/Infinity 금지 (파이썬 json 의 기본이 이미 그렇다)
못 읽으면 **파일·줄·칸·그 자리 앞뒤 글자**를 짚는다(로그만 보고도 고칠 수 있게).

`.meta` 짝도 같이 본다 — 새 표를 만들고 `gen_meta.py` 를 안 돌리면 유니티가 그 파일을 **안 싣는다**(Resources.Load 가 null).

사용:  python3 tools/check_resources_json.py [<뿌리>] [--self-test]
rc:    0 = 전부 읽힌다 · 1 = 못 읽는 표가 있다(또는 .meta 가 없다) · 2 = 쓰는 법이 틀렸다
"""
import json
import os
import sys
import tempfile

ROOT_DEFAULT = 'Assets'


def json_files(root):
    """`Resources/` 아래(어느 깊이든)의 .json 전부 — 경로 순."""
    out = []
    for dirpath, _dirs, files in os.walk(root):
        parts = dirpath.replace(os.sep, '/').split('/')
        if 'Resources' not in parts:
            continue
        for fn in files:
            if fn.endswith('.json'):
                out.append(os.path.join(dirpath, fn))
    return sorted(out)


def spot(text, line, col):
    """그 자리 앞뒤 글자 — 줄은 1부터, 칸도 1부터."""
    lines = text.split('\n')
    if line - 1 >= len(lines):
        return ''
    s = lines[line - 1]
    lo = max(0, col - 30)
    return ('…' if lo > 0 else '') + s[lo:col + 20].replace('\t', '\\t')


def check_one(path, out):
    """(ok, 왜) — 파싱과 .meta 짝."""
    bad = 0
    with open(path, encoding='utf-8') as f:
        text = f.read()
    try:
        json.loads(text)
    except ValueError as e:
        line = getattr(e, 'lineno', 0)
        col = getattr(e, 'colno', 0)
        out('✗ 못 읽는다  %s' % path)
        out('   · %s' % e)
        if line:
            out('   · %d행 %d칸 언저리: %s' % (line, col, spot(text, line, col)))
        out('   · 게임은 이 표를 `MiniJson` 으로 읽는다 — 여기서 못 읽으면 그 화면을 여는 **PlayMode 가 통째로** 죽는다(런 427).')
        bad += 1
    if not os.path.isfile(path + '.meta'):
        out('✗ .meta 가 없다  %s  → `python3 tools/gen_meta.py` (없으면 유니티가 안 싣고 Resources.Load 가 null 이다)' % path)
        bad += 1
    return bad


def run(root, out=print):
    if not os.path.isdir(root):
        out('✗ 그런 뿌리가 없다: %s' % root)
        return 2
    files = json_files(root)
    bad = 0
    for p in files:
        bad += check_one(p, out)
    out('%s check_resources_json: Resources 표 %d개 · 문제 %d'
        % ('✓' if bad == 0 else '✗', len(files), bad))
    return 0 if bad == 0 else 1


# ── 자기 검사(고장 주입) ────────────────────────────────────────────────────────────────────
def self_test():
    fails = []

    def build(d, name, body, meta=True):
        p = os.path.join(d, 'Assets', 'Forge', 'Resources')
        os.makedirs(p, exist_ok=True)
        f = os.path.join(p, name)
        with open(f, 'w', encoding='utf-8') as fh:
            fh.write(body)
        if meta:
            with open(f + '.meta', 'w', encoding='utf-8') as fh:
                fh.write('fileFormatVersion: 2\nguid: 0\n')
        return f

    def expect(name, bodies, want, meta=True):
        with tempfile.TemporaryDirectory() as d:
            for fn, body in bodies:
                build(d, fn, body, meta)
            lines = []
            rc = run(os.path.join(d, 'Assets'), out=lines.append)
            if rc != want:
                fails.append('%s: rc %d ≠ %d\n  ' % (name, rc, want) + '\n  '.join(lines))
            return lines

    good = '{\n  "a": 1,\n  "_": "설명 · 따옴표 안에 «»·… 다 된다",\n  "b": [1, 2, 3]\n}\n'
    expect('성한 표 → 0', [('Ok.json', good)], 0)

    # ⓐ 런 427 의 그 병 — 문자열이 안 닫혀 날 개행이 문자열 안으로
    broken = '{\n  "a": 1,\n  "_width": "여기서 안 닫는다\n  "b": 2\n}\n'
    lines = expect('안 닫힌 문자열 → 1', [('Bad.json', broken)], 1)
    if not any('행' in l and '칸 언저리' in l for l in lines):
        fails.append('줄·칸·앞뒤 글자를 안 짚는다')
    if not any('control character' in l.lower() or '제어' in l for l in lines):
        fails.append('제어문자 갈래가 안 보인다: %r' % lines)

    # ⓑ 꼬리 쉼표(MiniJson 도 파이썬도 거부한다)
    expect('꼬리 쉼표 → 1', [('Comma.json', '{\n  "a": 1,\n}\n')], 1)
    # ⓒ 주석(JSON 이 아니다 · 표에 // 를 쓰는 실수)
    expect('주석 → 1', [('Cmt.json', '{\n  // 설명\n  "a": 1\n}\n')], 1)
    # ⓓ .meta 가 없으면 1 (유니티가 안 싣는다)
    expect('.meta 없음 → 1', [('NoMeta.json', good)], 1, meta=False)
    # ⓔ 성한 표 여럿 + 깨진 하나 → 1 이고, 성한 것은 안 짚는다
    lines = expect('여럿 중 하나만 깨짐 → 1', [('Ok.json', good), ('Bad.json', broken)], 1)
    if any('Ok.json' in l and '✗' in l for l in lines):
        fails.append('성한 표를 짚는다')
    # ⓕ Resources 밖의 json 은 안 본다(그건 T2·T41 의 몫)
    with tempfile.TemporaryDirectory() as d:
        os.makedirs(os.path.join(d, 'Assets', 'StreamingAssets', 'data'))
        with open(os.path.join(d, 'Assets', 'StreamingAssets', 'data', 'x.json'), 'w', encoding='utf-8') as f:
            f.write('{ "a": 1, }')
        lines = []
        if run(os.path.join(d, 'Assets'), out=lines.append) != 0:
            fails.append('Resources 밖을 본다: %r' % lines)
    # ⓖ 깊은 Resources(`Assets/Forge/Icons/Resources/…`)도 본다
    with tempfile.TemporaryDirectory() as d:
        deep = os.path.join(d, 'Assets', 'Forge', 'Icons', 'Resources', 'Sub')
        os.makedirs(deep)
        with open(os.path.join(deep, 'y.json'), 'w', encoding='utf-8') as f:
            f.write('{ "a": }')
        lines = []
        if run(os.path.join(d, 'Assets'), out=lines.append) != 1:
            fails.append('깊은 Resources 를 안 본다: %r' % lines)
    # ⓗ 뿌리가 없으면 2
    if run('/nonexistent/Assets', out=lambda s: None) != 2:
        fails.append('없는 뿌리 rc 2')

    if fails:
        print('✗ check_resources_json --self-test 실패 %d' % len(fails))
        for f in fails:
            print('  - ' + f)
        return 1
    print('✓ check_resources_json --self-test 9칸 통과')
    return 0


def main(argv):
    root = ROOT_DEFAULT
    for a in argv:
        if a == '--self-test':
            return self_test()
        if a.startswith('-'):
            print('사용: check_resources_json.py [<뿌리>] [--self-test]')
            return 2
        root = a
    return run(root)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
