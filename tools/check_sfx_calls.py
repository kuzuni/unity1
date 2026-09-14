#!/usr/bin/env python3
"""정본 소리 24종이 **게임 코드에서 실제로 울리는가** (ROUTINE T119 막이).

왜 필요한가 — T30 이 원작 `sfx.js` 24종을 전부 합성해 놓았는데, 그중 넷은 **부르는 곳이 없었다**
(`craftReveal`·`equipToss`·`equipDrop`·`stormCrackle` · T33 3회차 실측). 소리는 화면과 달리 PNG 에
안 찍히고 테스트도 «울렸는가» 를 안 물어서, 레시피만 있고 호출이 없는 것을 아무 자도 못 봤다.

무엇을 보는가:
  ⓐ `Assets/Scripts/Core/Audio/SfxRecipes.cs` 의 `Names` 표(= 원작 `SFX` 의 이름 24개)를 읽는다.
  ⓑ `Assets/Scripts/Game/Audio/Sfx.cs` 의 래퍼(`public static void Xxx(...) { Play(new SfxCall("이름"…`)로
     «이름 → C# 메서드» 를 맺는다.
  ⓒ `Assets/Scripts` 에서 **래퍼·레시피 폴더를 뺀** 코드가 `Sfx.Xxx` 를 쓰는지 센다(주석 줄은 뺀다).
     부름은 `Sfx.Xxx(` 만이 아니다 — 클론은 훅에 **메서드 묶음**(`SfxGacha = Sfx.Gacha`)으로도 건넨다.
     반대로 «이름 문자열을 훅에 넘기는» 길(`ForgeHost.PlaySfx("craft")`)은 **부름으로 안 센다** — 그 훅이
     실제로 꽂혔는지는 정적으로 못 보고, 실제로 T119 1회차에 `ForgeHost.Sfx` 가 아무 데서도 안 꽂혀
     대장간 소리 셋이 통째로 무음이었다(T120). 즉 이 자는 «울릴 길이 코드에 보이는가» 를 묻는다.

`KNOWN` 은 «지금 호출이 0인 것을 알고 있고 임자가 정해진» 이름이다 — 그것만 통과하고 **새로 호출이
0이 된 이름은 rc 1**(누가 마지막 호출을 지우면 그 자리에서 잡힌다).

의존성 0(순수 파이썬).

사용:  python3 tools/check_sfx_calls.py [--self-test]
"""
import os
import re
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
RECIPES = os.path.join(ROOT, 'Assets', 'Scripts', 'Core', 'Audio', 'SfxRecipes.cs')
WRAPPER = os.path.join(ROOT, 'Assets', 'Scripts', 'Game', 'Audio', 'Sfx.cs')
SCAN_DIR = os.path.join(ROOT, 'Assets', 'Scripts')
# 소리를 «정의» 하는 자리는 호출이 아니다 — 래퍼(Game/Audio)와 합성기(Core/Audio)를 뺀다.
SKIP_DIRS = [
    os.path.join(ROOT, 'Assets', 'Scripts', 'Core', 'Audio'),
    os.path.join(ROOT, 'Assets', 'Scripts', 'Game', 'Audio'),
]

# 임자가 정해진 «호출 0» — 왜 아직 0인지와 누가 갚는지를 같이 적는다.
KNOWN = {
    # ✅ craftReveal 은 T119 4회차(2026-09-14 · 워커 S)가 갚았다 — `ForgeCraftPopup.ShowReveal`·`ShowBatch` 가 정본 ui.js 1938·1976 대로 `Sfx.CraftReveal(AGES.indexOf(age))` 를 분다. KNOWN 에서 뺀다(남기면 호출이 지워져도 자가 조용히 넘어간다 · T120 2회차 규칙).
    # ✅ T119 1회차가 캤던 셋(craft·equipSnap·levelUp)은 **T120 1회차가 갚아 지웠다**(2026-09-13 · 런 238 PASS) —
    #    `Game/HostSfx.cs` 가 `ForgeHost.OnReady` 에서 훅을 꽂고(이름 → T30 래퍼), `levelUp` 은 세 자리에 걸렸다
    #    (`ForgeEngine.LevelReached` · `TechPopups.OnClaim` · `DungeonClearPopup.Show`). KNOWN 에 남겨 두면
    #    **누가 그 호출을 지워도 이 자가 조용히 넘어간다** — 갚은 이름은 반드시 여기서 뺀다(T120 2회차).
    # ⚠ 이 하나는 «아직 안 옮긴 것» 이 아니라 **안 옮기는 것**이다(결정 · T119 1회차).
    'stormCrackle': '**정본도 안 운다** — 유일한 호출이 `scene3d.js` 14101 `_legacyStormCloudStrike` 안인데 그 함수를 부르는 곳이 없다(살아 있는 길은 `scene3d-skillfx.js` 663 `mcThunderStrike` 이고 거기선 `stormStrike` 만 운다). 원작이 안 내는 소리를 클론이 내면 §1 «원작에 없는 것» 이다 — 레시피는 T30 이 이미 구워 뒀으니 그대로 두고 **호출은 넣지 않는다**.',
}

NAMES_BLOCK = re.compile(r'Names\s*=\s*\{(.*?)\};', re.S)
QUOTED = re.compile(r'"([^"]+)"')
# public static void AnvilHit(bool strong) { Play(new SfxCall("anvilHit", …
WRAP = re.compile(r'public\s+static\s+void\s+([A-Za-z0-9_]+)\s*\([^)]*\)\s*\{[^}]*?new\s+SfxCall\(\s*"([^"]+)"')


def names(path=RECIPES):
    """`SfxRecipes.Names` 의 이름들 (원작 `SFX` 이름 그대로)."""
    m = NAMES_BLOCK.search(open(path, encoding='utf-8').read())
    if not m:
        return []
    return QUOTED.findall(m.group(1))


def wrappers(path=WRAPPER):
    """{소리 이름: C# 메서드 이름} — 래퍼가 맺어 주는 짝."""
    out = {}
    for meth, name in WRAP.findall(open(path, encoding='utf-8').read()):
        out.setdefault(name, meth)
    return out


def call_counts(root, methods, skip_dirs=()):
    """{메서드: 부른 자리 수} — `Sfx.메서드(` 를 센다(주석 줄 제외 · skip_dirs 아래는 안 본다)."""
    # 부름은 «Sfx.Xxx(» 만이 아니다 — 클론은 훅에 **메서드 묶음**(`SfxGacha = Sfx.Gacha`)으로도 건넨다.
    # 그래서 이름 뒤가 «식별자 글자가 아니면» 센다(그러면 `Sfx.Craft` 가 `Sfx.CraftReveal` 을 안 삼킨다).
    pat = re.compile(r'\bSfx\.(' + '|'.join(re.escape(m) for m in methods) + r')(?![A-Za-z0-9_])') if methods else None
    out = dict((m, 0) for m in methods)
    if pat is None:
        return out
    skip = [os.path.normpath(d) for d in skip_dirs]
    for dirpath, _dirs, files in os.walk(root):
        if any(os.path.normpath(dirpath).startswith(d) for d in skip):
            continue
        for f in sorted(files):
            if not f.endswith('.cs'):
                continue
            for line in open(os.path.join(dirpath, f), encoding='utf-8').read().split('\n'):
                st = line.strip()
                if st.startswith('//') or st.startswith('*'):
                    continue
                for m in pat.findall(line):
                    out[m] += 1
    return out


def silent(root=SCAN_DIR, recipes=RECIPES, wrapper=WRAPPER, skip_dirs=None):
    """호출이 0인 소리 이름 → 이유 꼬리표 ('' = 이유 모름 · 래퍼가 없으면 그것도 적는다)."""
    if skip_dirs is None:
        skip_dirs = SKIP_DIRS
    table = names(recipes)
    wrap = wrappers(wrapper)
    counts = call_counts(root, sorted(set(wrap.values())), skip_dirs)
    out = {}
    for n in table:
        meth = wrap.get(n)
        if meth is None:
            out[n] = '래퍼 없음(Sfx.cs 에 이 이름을 내는 메서드가 없다)'
        elif counts.get(meth, 0) == 0:
            out[n] = ''
    return out


def main(argv):
    if '--self-test' in argv:
        return self_test()
    table = names()
    quiet = silent()
    new = {n: r for n, r in quiet.items() if n not in KNOWN}
    for n in table:
        if n not in quiet:
            continue
        tag = '(아는 것) ' + KNOWN[n] if n in KNOWN else '**새로 조용해졌다**'
        print('  %s %-14s — %s%s' % ('·' if n in KNOWN else '✗', n, tag, (' · ' + quiet[n]) if quiet[n] else ''))
    if new:
        print('✗ check_sfx_calls: 원작 소리 %d개가 게임 코드에서 한 번도 안 울린다 — 레시피만 있고 호출이 없다.' % len(new))
        print('  고침 둘: ⓐ 정본이 그 자리에서 실제로 우는 소리면 그 자리를 옮긴다(정본 줄을 먼저 읽는다 — 죽은 레거시 경로일 수 있다)')
        print('          ⓑ 정본도 안 우는 소리면 **넣지 말고** KNOWN 에 «정본도 안 운다» 를 근거(파일·줄)와 함께 적는다.')
        return 1
    print('✓ check_sfx_calls: 소리 %d종 · 호출 0 은 %d종(전부 KNOWN · 임자 있음)' % (len(table), len(quiet)))
    return 0


def self_test():
    ok = True
    import tempfile
    if len(names()) != 24:
        print('✗ 이름 표: 24종이 아니라 %d종' % len(names())); ok = False
    w = wrappers()
    for n, meth in [('anvilHit', 'AnvilHit'), ('stormCrackle', 'StormCrackle'), ('craftReveal', 'CraftReveal')]:
        if w.get(n) != meth:
            print('✗ 래퍼 짝: %s → %s (받은 %s)' % (n, meth, w.get(n))); ok = False
    # 호출 세기: 주석은 호출이 아니다 · 래퍼 폴더는 안 본다
    with tempfile.TemporaryDirectory() as d:
        os.makedirs(os.path.join(d, 'Ui'))
        open(os.path.join(d, 'Ui', 'A.cs'), 'w', encoding='utf-8').write(
            'void F() { Sfx.AnvilHit(true); }\n// Sfx.CraftReveal(0);\nAction g = Sfx.Gacha;   // 메서드 묶음도 부름이다\nvar x = Sfx.CraftReveal;\n')
        os.makedirs(os.path.join(d, 'Audio'))
        open(os.path.join(d, 'Audio', 'Sfx.cs'), 'w', encoding='utf-8').write('Sfx.CraftReveal(0);\n')
        got = call_counts(d, ['AnvilHit', 'CraftReveal', 'Gacha', 'Craft'], [os.path.join(d, 'Audio')])
        for meth, want in [('AnvilHit', 1), ('CraftReveal', 1), ('Gacha', 1), ('Craft', 0)]:
            if got[meth] != want:
                print('✗ 호출 세기 %s: 기대 %d · 받은 %d' % (meth, want, got[meth])); ok = False
    # 고장 주입: 래퍼가 안 맺어진 이름은 «래퍼 없음» 으로 잡힌다
    with tempfile.TemporaryDirectory() as d:
        rec = os.path.join(d, 'R.cs'); wrp = os.path.join(d, 'W.cs')
        open(rec, 'w', encoding='utf-8').write('Names =\n{\n    "hit", "ghostSound",\n};\n')
        open(wrp, 'w', encoding='utf-8').write('public static void Hit(bool c) { Play(new SfxCall("hit", 0)); }\n')
        src = os.path.join(d, 'src'); os.makedirs(src)
        open(os.path.join(src, 'A.cs'), 'w', encoding='utf-8').write('Sfx.Hit(true);\n')
        got = silent(src, rec, wrp, [])
        if list(got) != ['ghostSound']:
            print('✗ 고장 주입: 기대 [ghostSound] · 받은 %s' % list(got)); ok = False
    if main([]) != 0:
        print('✗ 지금 Assets/Scripts 가 이 검사에 걸린다 — 위 ✗ 를 먼저 고쳐라'); ok = False
    print('✓ check_sfx_calls 자기 검사 통과' if ok else '✗ check_sfx_calls 자기 검사 실패')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
