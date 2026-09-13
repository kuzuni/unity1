#!/usr/bin/env python3
"""코드가 이름으로 찾는 것(`Shader.Find` · `Resources.Load`)이 **빌드에도 실리는가** (ROUTINE T126 막이).

왜 필요한가 — 에디터에서는 `Shader.Find` 가 프로젝트 안의 모든 셰이더를 본다. 그래서 PlayMode 테스트도
촬영 PNG 도 전부 초록인데, **빌드(WebGL·Android)에서는 아무 에셋도 참조하지 않는 셰이더가 통째로
스트립된다** — 그러면 `Shader.Find` 가 null 을 주고 코드가 폴백으로 넘어가, 정본에 있던 합성(가산·깊이 끄기)과
적 몸 연출(림·플래시·디졸브)이 **빌드에서만** 사라진다(2026-09-13 실측: `Forge/FxUnlit` · `Forge/EnemyBody`).

빌드에 실리는 길은 셋이다:
  ⓐ `Resources/` 폴더 안에 있다(셰이더 자체 또는 그것을 문 재질).
  ⓑ 씬·프리팹·재질·에셋이 **GUID 로** 참조한다(그 에셋이 빌드에 실릴 때 같이 딸려 간다).
  ⓒ `ProjectSettings/GraphicsSettings.asset` 의 `m_AlwaysIncludedShaders` 에 적혀 있다.

무엇을 보는가:
  1. `Assets/Scripts` 에서 `Shader.Find("<이름>")` 의 이름을 전부 걷는다(주석 줄은 뺀다).
  2. 유니티 내장·URP 이름(`Universal Render Pipeline/…` · `Unlit/…` · `Sprites/…` 등)은 건너뛴다 — 그것은 패키지가 싣는다.
  3. 남은 «이 레포가 만든» 이름(`Shader "<이름>"` 이 `Assets/**/*.shader` 에 있는 것)마다 위 셋 중 하나를 만족하는지 본다.
  4. 하나도 아니면 **rc 1**(빌드에서 잘린다) · 이름에 해당하는 `.shader` 파일이 아예 없어도 rc 1(오타·삭제).

의존성 0(순수 파이썬).

2회차에 한 갈래를 더 봤다 — **`Resources.Load<T>("경로")`**. 그 자리도 «없으면 조용히 null» 이라 같은 종류의 사고다
(에디터·빌드 둘 다에서 조용하다 · 게으르게 쓰는 자리가 많아 테스트가 안 밟는다). 리터럴·`const string` 상수·
«상수 + "/이름"» 꼴까지 풀어서 `**/Resources/<경로>.*` 가 실제로 있는지 본다(못 푸는 인자는 세지 않고 목록만 보여 준다).

사용:  python3 tools/check_shaders_included.py [--self-test]
"""
import os
import re
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
SCRIPTS = os.path.join(ROOT, 'Assets', 'Scripts')
ASSETS = os.path.join(ROOT, 'Assets')
GRAPHICS = os.path.join(ROOT, 'ProjectSettings', 'GraphicsSettings.asset')

# 패키지·엔진이 싣는 이름 — 이 자가 볼 몫이 아니다.
BUILTIN_PREFIX = (
    'Universal Render Pipeline/', 'Unlit/', 'Sprites/', 'UI/', 'Standard', 'Hidden/',
    'TextMeshPro/', 'Particles/', 'Skybox/', 'Legacy Shaders/',
)
REF_EXT = ('.mat', '.prefab', '.unity', '.asset', '.controller', '.playable', '.renderTexture')


def read(path):
    with open(path, encoding='utf-8', errors='replace') as fh:
        return fh.read()


def find_names(scripts_dir):
    """코드가 `Shader.Find("…")` 로 찾는 이름 → 그것을 쓴 파일들."""
    out = {}
    for base, _dirs, files in os.walk(scripts_dir):
        for name in files:
            if not name.endswith('.cs'):
                continue
            path = os.path.join(base, name)
            for line in read(path).split('\n'):
                if line.lstrip().startswith('//') or line.lstrip().startswith('///'):
                    continue
                for m in re.finditer(r'Shader\.Find\(\s*"([^"]+)"', line):
                    out.setdefault(m.group(1), set()).add(os.path.relpath(path, ROOT))
                # 이름을 상수로 두고 그것을 찾는 꼴: `public const string ShaderName = "Forge/X";`
                for m in re.finditer(r'ShaderName\s*=\s*"([^"]+)"', line):
                    out.setdefault(m.group(1), set()).add(os.path.relpath(path, ROOT))
    return out


def shader_files(assets_dir):
    """`Shader "<이름>"` → (경로, guid). 이 레포·패키지 안의 .shader 전부."""
    out = {}
    for base, _dirs, files in os.walk(assets_dir):
        for name in files:
            if not name.endswith('.shader'):
                continue
            path = os.path.join(base, name)
            m = re.search(r'^\s*Shader\s+"([^"]+)"', read(path), re.M)
            if not m:
                continue
            guid = ''
            meta = path + '.meta'
            if os.path.exists(meta):
                g = re.search(r'^guid:\s*([0-9a-f]+)', read(meta), re.M)
                if g:
                    guid = g.group(1)
            out[m.group(1)] = (os.path.relpath(path, ROOT), guid)
    return out


def always_included(graphics_path):
    """`m_AlwaysIncludedShaders` 에 적힌 guid 들."""
    if not os.path.exists(graphics_path):
        return set()
    txt = read(graphics_path)
    m = re.search(r'm_AlwaysIncludedShaders:\n((?:\s+- \{[^\n]*\}\n)+)', txt)
    if not m:
        return set()
    return set(re.findall(r'guid:\s*([0-9a-f]+)', m.group(1)))


def guid_referenced(assets_dir, guid):
    """씬·프리팹·재질 따위가 그 guid 를 물고 있는가(있으면 그 에셋과 함께 빌드에 실린다)."""
    if not guid:
        return None
    for base, _dirs, files in os.walk(assets_dir):
        for name in files:
            if not name.endswith(REF_EXT):
                continue
            path = os.path.join(base, name)
            if guid in read(path):
                return os.path.relpath(path, ROOT)
    return None


def in_resources(rel_path):
    parts = rel_path.replace('\\', '/').split('/')
    return 'Resources' in parts


def string_consts(scripts_dir):
    """`const string X = "Y";` → {"X": "Y", "Cls.X": "Y"} (같은 이름이 둘이면 뒤엣것도 담아 둔다)."""
    out, later = {}, []
    for base, _dirs, files in os.walk(scripts_dir):
        for name in files:
            if not name.endswith('.cs'):
                continue
            txt = read(os.path.join(base, name))
            cls = None
            for line in txt.split('\n'):
                m = re.search(r'\b(?:class|struct)\s+([A-Za-z_][A-Za-z0-9_]*)', line)
                if m:
                    cls = m.group(1)
                c = re.search(r'const\s+string\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*"([^"]*)"', line)
                if c:
                    out[c.group(1)] = c.group(2)
                    if cls:
                        out[cls + '.' + c.group(1)] = c.group(2)
            # 지역 변수가 상수를 받아 그 뒤에 경로를 이어 붙이는 꼴(`string dir = IconAtlas.ResourceDir;`)
            for v in re.finditer(r'\bstring\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*([A-Za-z_][A-Za-z0-9_.]*)\s*;', txt):
                later.append((v.group(1), v.group(2)))
    for name, src in later:
        hit = out.get(src) or out.get(src.split('.')[-1])
        if hit is not None:
            out.setdefault(name, hit)
    return out


def resource_loads(scripts_dir, consts):
    """`Resources.Load<T>(인자)` → [(경로|None, 원문 인자, 파일)]. 리터럴·상수·«상수 + "/이름"» 을 푼다."""
    out = []
    for base, _dirs, files in os.walk(scripts_dir):
        for name in files:
            if not name.endswith('.cs'):
                continue
            path = os.path.join(base, name)
            rel = os.path.relpath(path, ROOT)
            for line in read(path).split('\n'):
                if line.lstrip().startswith('//'):
                    continue
                for m in re.finditer(r'Resources\.Load(?:<([^>]+)>)?\(\s*([^()]*?)\s*\)', line):
                    arg = m.group(2).strip()
                    for got in resolve_arg(arg, consts) or [None]:
                        out.append((got, arg, rel, (m.group(1) or '').strip()))
    return out


def resolve_arg(arg, consts):
    """인자를 경로 후보 목록으로 — 못 풀면 None. 리터럴 · 상수 · «상수 + "/이름"» · 삼항(`c ? A : B`)을 푼다."""
    arg = arg.strip()
    tern = re.fullmatch(r'[^?]+\?\s*([^:]+?)\s*:\s*(.+)', arg)
    if tern:
        got = []
        for side in (tern.group(1), tern.group(2)):
            part = resolve_arg(side, consts)
            if part is None:
                return None
            got += part
        return got
    lit = re.fullmatch(r'"([^"]*)"', arg)
    if lit:
        return [lit.group(1)]
    if re.fullmatch(r'[A-Za-z_][A-Za-z0-9_.]*', arg):
        hit = consts.get(arg) or consts.get(arg.split('.')[-1])
        return [hit] if hit is not None else None
    m = re.fullmatch(r'([A-Za-z_][A-Za-z0-9_.]*)\s*\+\s*"([^"]*)"', arg)
    if m:
        head = consts.get(m.group(1)) or consts.get(m.group(1).split('.')[-1])
        if head is not None:
            return [head + m.group(2)]
    return None


def resources_files(assets_dir):
    """`Resources/` 아래의 «폴더 기준 경로(확장자 없음)» → 실제 파일들."""
    out = {}
    for base, _dirs, files in os.walk(assets_dir):
        parts = os.path.relpath(base, assets_dir).replace('\\', '/').split('/')
        if 'Resources' not in parts:
            continue
        under = '/'.join(parts[parts.index('Resources') + 1:])
        for name in files:
            if name.endswith('.meta'):
                continue
            stem = os.path.splitext(name)[0]
            key = (under + '/' + stem) if under and under != '.' else stem
            out.setdefault(key, []).append(os.path.relpath(os.path.join(base, name), ROOT))
    return out


def check_resources(assets_dir, scripts_dir, quiet):
    consts = string_consts(scripts_dir)
    have = resources_files(assets_dir)
    bad, unread = [], []
    ok = 0
    for path, arg, rel, kind in resource_loads(scripts_dir, consts):
        if path is None:
            unread.append((arg, rel))
            continue
        if path in have:
            ok += 1
            continue
        bad.append((path, arg, rel, kind))
    if not quiet:
        for arg, rel in sorted(set(unread)):
            print('  · (못 푼 인자 — 세지 않는다) %s  [%s]' % (arg, rel))
        if ok:
            print('  · Resources.Load 경로 %d자리 전부 실재한다' % ok)
    if bad and not quiet:
        print('Resources 에 없는 경로를 부른다 — 그 자리는 조용히 null 이 된다:')
        for path, arg, rel, kind in bad:
            print('  ✗ "%s" (인자 %s · %s%s)' % (path, arg, rel, (' · <' + kind + '>') if kind else ''))
        print('고치는 법: 그 파일을 `**/Resources/<경로>.<확장자>` 에 두거나(폴더 이름이 정확히 `Resources`), 부르는 쪽 경로를 고친다.')
    return 1 if bad else 0


def check(assets_dir=ASSETS, scripts_dir=SCRIPTS, graphics_path=GRAPHICS, quiet=False):
    names = find_names(scripts_dir)
    files = shader_files(assets_dir)
    always = always_included(graphics_path)
    bad, ok_lines = [], []
    for name in sorted(names):
        if name.startswith(BUILTIN_PREFIX):
            continue
        hit = files.get(name)
        if hit is None:
            bad.append((name, '그런 이름의 .shader 가 없다 — 오타이거나 지워졌다(폴백만 남는다)', sorted(names[name])))
            continue
        rel, guid = hit
        if in_resources(rel):
            ok_lines.append('%s ← %s (Resources 폴더)' % (name, rel))
            continue
        if guid and guid in always:
            ok_lines.append('%s ← %s (AlwaysIncludedShaders)' % (name, rel))
            continue
        ref = guid_referenced(assets_dir, guid)
        if ref:
            ok_lines.append('%s ← %s (%s 가 참조)' % (name, rel, ref))
            continue
        bad.append((name, '%s 가 Resources·참조·AlwaysIncludedShaders 어디에도 없다 — 빌드에서 스트립된다' % rel, sorted(names[name])))
    if not quiet:
        for line in ok_lines:
            print('  · ' + line)
    rc_res = check_resources(assets_dir, scripts_dir, quiet)
    if bad:
        if not quiet:
            print('빌드에 안 실리는 셰이더가 있다 — 에디터에서만 보이는 연출이 된다:')
            for name, why, users in bad:
                print('  ✗ %s — %s' % (name, why))
                print('     찾는 곳: %s' % ', '.join(users))
            print('고치는 법: ⓐ `ProjectSettings/GraphicsSettings.asset` 의 `m_AlwaysIncludedShaders` 에')
            print('           `- {fileID: 4800000, guid: <셰이더 guid>, type: 3}` 를 더하거나')
            print('           ⓑ 그 셰이더(또는 그것을 문 재질)를 `Resources/` 폴더에 둔다.')
        return 1
    if rc_res:
        return 1
    if not quiet:
        print('✓ check_shaders_included: 코드가 찾는 셰이더 %d개가 전부 빌드에 실리고 Resources 경로도 다 실재한다' % len(ok_lines))
    return 0


def self_test():
    """고장 주입 — 임시 프로젝트를 만들어 세 갈래(Resources · 참조 · 항상 포함)와 «어디에도 없음» 을 본다."""
    import shutil
    import tempfile
    root = tempfile.mkdtemp(prefix='shadertest-')
    try:
        def put(rel, text):
            path = os.path.join(root, rel)
            os.makedirs(os.path.dirname(path), exist_ok=True)
            with open(path, 'w', encoding='utf-8') as fh:
                fh.write(text)
            return path

        put('Assets/Scripts/A.cs', 'class A { void M() { Shader.Find("T/Res"); Shader.Find("T/Ref"); Shader.Find("T/Always"); Shader.Find("Unlit/Color"); } }')
        put('Assets/Shaders/Res/Resources/Res.shader', 'Shader "T/Res" { }')
        put('Assets/Shaders/Res/Resources/Res.shader.meta', 'guid: 1111\n')
        put('Assets/Shaders/Ref.shader', 'Shader "T/Ref" { }')
        put('Assets/Shaders/Ref.shader.meta', 'guid: 2222\n')
        put('Assets/Mats/M.mat', 'm_Shader: {fileID: 4800000, guid: 2222, type: 3}\n')
        put('Assets/Shaders/Always.shader', 'Shader "T/Always" { }')
        put('Assets/Shaders/Always.shader.meta', 'guid: 3333\n')
        gr = put('ProjectSettings/GraphicsSettings.asset',
                 'm_AlwaysIncludedShaders:\n  - {fileID: 4800000, guid: 3333, type: 3}\nm_PreloadedShaders: []\n')
        assets, scripts = os.path.join(root, 'Assets'), os.path.join(root, 'Assets', 'Scripts')

        cases = []
        cases.append(('셋 다 실리면 rc 0', check(assets, scripts, gr, quiet=True) == 0))
        # ⓐ 항상 포함 목록에서 빼면 잡힌다
        with open(gr, 'w', encoding='utf-8') as fh:
            fh.write('m_AlwaysIncludedShaders:\n  - {fileID: 7, guid: 0000, type: 0}\nm_PreloadedShaders: []\n')
        cases.append(('AlwaysIncluded 에서 빼면 rc 1', check(assets, scripts, gr, quiet=True) == 1))
        with open(gr, 'w', encoding='utf-8') as fh:
            fh.write('m_AlwaysIncludedShaders:\n  - {fileID: 4800000, guid: 3333, type: 3}\nm_PreloadedShaders: []\n')
        # ⓑ 참조하던 재질을 지우면 잡힌다
        os.remove(os.path.join(root, 'Assets/Mats/M.mat'))
        cases.append(('참조 재질을 지우면 rc 1', check(assets, scripts, gr, quiet=True) == 1))
        put('Assets/Mats/M.mat', 'm_Shader: {fileID: 4800000, guid: 2222, type: 3}\n')
        # ⓒ Resources 밖으로 옮기면 잡힌다
        shutil.move(os.path.join(root, 'Assets/Shaders/Res/Resources/Res.shader'), os.path.join(root, 'Assets/Shaders/Res/Res.shader'))
        shutil.move(os.path.join(root, 'Assets/Shaders/Res/Resources/Res.shader.meta'), os.path.join(root, 'Assets/Shaders/Res/Res.shader.meta'))
        cases.append(('Resources 밖으로 옮기면 rc 1', check(assets, scripts, gr, quiet=True) == 1))
        shutil.move(os.path.join(root, 'Assets/Shaders/Res/Res.shader'), os.path.join(root, 'Assets/Shaders/Res/Resources/Res.shader'))
        shutil.move(os.path.join(root, 'Assets/Shaders/Res/Res.shader.meta'), os.path.join(root, 'Assets/Shaders/Res/Resources/Res.shader.meta'))
        # ⓓ 이름을 오타내면(그 .shader 가 없다) 잡힌다
        put('Assets/Scripts/B.cs', 'class B { void M() { Shader.Find("T/Nope"); } }')
        cases.append(('없는 이름을 찾으면 rc 1', check(assets, scripts, gr, quiet=True) == 1))
        os.remove(os.path.join(root, 'Assets/Scripts/B.cs'))
        # ⓔ 내장·URP 이름은 안 센다
        put('Assets/Scripts/C.cs', 'class C { void M() { Shader.Find("Universal Render Pipeline/Lit"); } }')
        cases.append(('내장 이름은 건너뛴다', check(assets, scripts, gr, quiet=True) == 0))
        # ⓕ 주석 줄의 이름은 안 센다
        put('Assets/Scripts/D.cs', '// Shader.Find("T/Comment") 는 주석이다\nclass D { }')
        cases.append(('주석 속 이름은 안 센다', check(assets, scripts, gr, quiet=True) == 0))

        # ── Resources.Load 갈래(2회차) ──────────────────────────────────────────
        put('Assets/Res/Resources/Deep/thing.json', '{}')
        put('Assets/Scripts/R1.cs', 'class R1 { const string P = "Deep/thing"; void M() { Resources.Load<TextAsset>(P); Resources.Load<TextAsset>("Deep/thing"); } }')
        cases.append(('Resources 리터럴·상수가 실재하면 rc 0', check(assets, scripts, gr, quiet=True) == 0))
        put('Assets/Scripts/R2.cs', 'class R2 { void M() { Resources.Load<TextAsset>("Deep/nope"); } }')
        cases.append(('Resources 에 없는 경로면 rc 1', check(assets, scripts, gr, quiet=True) == 1))
        os.remove(os.path.join(root, 'Assets/Scripts/R2.cs'))
        # 삼항·지역 변수도 푼다
        put('Assets/Res/Resources/Deep/other.json', '{}')
        put('Assets/Scripts/R3.cs', 'class R3 { const string A = "Deep/thing"; const string B = "Deep/other"; void M(bool q) { Resources.Load<TextAsset>(q ? A : B); } }')
        cases.append(('삼항 양쪽을 다 푼다', check(assets, scripts, gr, quiet=True) == 0))
        put('Assets/Scripts/R4.cs', 'class R4 { const string D = "Deep"; void M() { string dir = D; Resources.Load<TextAsset>(dir + "/thing"); } }')
        cases.append(('지역 변수 + 이어 붙인 경로도 푼다', check(assets, scripts, gr, quiet=True) == 0))
        put('Assets/Scripts/R5.cs', 'class R5 { const string D2 = "Deep"; void M() { string d2 = D2; Resources.Load<TextAsset>(d2 + "/nope"); } }')
        cases.append(('이어 붙인 경로가 없으면 rc 1', check(assets, scripts, gr, quiet=True) == 1))
        os.remove(os.path.join(root, 'Assets/Scripts/R5.cs'))
        put('Assets/Scripts/R6.cs', 'class R6 { void M(string x) { Resources.Load<TextAsset>(x); } }')
        cases.append(('못 푸는 인자는 세지 않는다', check(assets, scripts, gr, quiet=True) == 0))

        bad = [name for name, ok in cases if not ok]
        for name, ok in cases:
            print('  %s %s' % ('✓' if ok else '✗', name))
        if bad:
            print('✗ check_shaders_included 자기 검사 실패: %s' % ', '.join(bad))
            return 1
        print('✓ check_shaders_included 자기 검사 %d칸 통과' % len(cases))
        return 0
    finally:
        shutil.rmtree(root, ignore_errors=True)


if __name__ == '__main__':
    if '--self-test' in sys.argv:
        sys.exit(self_test())
    sys.exit(check())
