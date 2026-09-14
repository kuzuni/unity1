#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T174 — 하니스 스텁(`tools/dotnet/Stubs/*.cs`)이 **실물에 없는 멤버**를 가지는 것을 막는다.

무엇이 아팠나
-------------
`tools/dotnet` 하니스는 URP·TMP 를 스텁으로 물고 컴파일만 확인한다(T48 설계). 그래서 스텁에
«실물엔 없는» 서명을 적어 넣으면 **로컬은 초록인데 유니티가 죽는다** — 그것도 빨간 테스트가
아니라 «Scripts have compiler errors → 결과 XML 0개» 로(§1 «빨간 테스트보다 나쁘다»).
런 409·410·412 가 그렇게 셋 다 날아갔다(`HasCharacter(uint, bool, bool)` · 실제 TMP 에 없다).

⚠ «출처 줄을 달게 한다» 로는 못 막는다 — **그 가짜 오버로드에는 이미 출처 줄이 붙어 있었다**
   («/// T106 — TMP 3.2 실서명: HasCharacter(uint unicode, …)»). 사람이 잘못 읽은 것을
   사람이 적은 주석으로 검사할 수는 없다.

그래서 이 자가 쓰는 진실의 출처는 **CI 의 진짜 유니티**다
------------------------------------------------------
1. 이 자가 스텁을 읽어 «검증해 달라» 는 멤버 목록을 `tools/stub-sigs.want.txt` 로 쓴다(`--emit`).
2. PlayMode `StubSigsTests` 가 **진짜 TMP·URP 어셈블리에 리플렉션**을 걸어 그 목록을 한 줄씩
   `OK`/`MISSING` 으로 판정하고 `ui-screens/stub-sigs.txt` 로 남긴다(CI 가 `screens` 로 올린다).
3. 이 자가 그 판정을 읽어 `MISSING` 이 하나라도 있으면 rc 1 — **밀기 전에** 로컬에서 빨개진다.

판정 파일이 아직 없으면(첫 회차·네트워크 없음) **알리기만 하고 rc 0** 이다.
"""
import os, re, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STUB_DIR = os.path.join(ROOT, 'tools', 'dotnet', 'Stubs')
WANT = os.path.join(ROOT, 'tools', 'stub-sigs.want.txt')
VERDICT_REF = 'origin/screens:stub-sigs.txt'

# 스텁 파일 중 «진짜 패키지를 흉내 내는» 것만 본다. TestRunner·TestTools 는 테스트 러너 쪽이라
# 이 레포가 유니티에서 실제로 쓰는 표면이 좁고, 리플렉션 대상 어셈블리가 에디터 전용이라 뺀다.
REAL = {'TMPro.cs': 'TMPro', 'URP.cs': 'URP'}

# 검사에서 빼는 자리 — «이 레포가 만든 것» 이라 실물에 없는 게 맞는 것들.
KNOWN = {
    # 예) 'TMPro.TMP_Text.forgeOnly': '이 레포가 더한 것이 아니라 …'
}

MEMBER = re.compile(
    r'^\s*public\s+(?:static\s+|virtual\s+|override\s+|sealed\s+|readonly\s+|const\s+)*'
    r'(?P<type>[A-Za-z_][\w<>\[\],\.\? ]*?)\s+'
    r'(?P<name>[A-Za-z_]\w*)\s*'
    r'(?P<tail>\(|\{|=|;)'
    r'(?P<params>[^)]*)')

def short(t):
    """«UnityEngine.TextCore.LowLevel.GlyphRenderMode» → «glyphrendermode» — 양쪽을 같은 자로 줄인다.
    네임스페이스·공백·ref/out/params·기본값을 떼고 소문자로. 제네릭은 «list<tmp_fontasset>» 꼴로 남긴다."""
    t = t.strip()
    t = re.sub(r'\b(?:ref|out|params|in)\s+', '', t)
    t = t.split('=')[0].strip()
    t = re.sub(r'\s+', '', t)
    # 네임스페이스 떼기(제네릭 인자 안쪽까지)
    t = re.sub(r'[A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)+', lambda m: m.group(0).rsplit('.', 1)[-1], t)
    return t.lower()


def params_of(decl):
    """«(Font font, int samplingPointSize, …)» → «font,int32,…» 로 줄인 목록. 메서드가 아니면 None."""
    i = decl.find('(')
    if i < 0: return None
    depth = 0
    for j in range(i, len(decl)):
        if decl[j] == '(': depth += 1
        elif decl[j] == ')':
            depth -= 1
            if depth == 0: break
    else:
        return None
    inner = decl[i + 1:j].strip()
    if not inner: return []
    out, cur, d = [], '', 0
    for ch in inner:
        if ch in '<([': d += 1
        elif ch in '>)]': d -= 1
        if ch == ',' and d == 0:
            out.append(cur); cur = ''
        else:
            cur += ch
    out.append(cur)
    res = []
    for one in out:
        one = one.split('=')[0].strip()
        toks = one.split()
        res.append(short(toks[0] if len(toks) < 2 else ' '.join(toks[:-1])))
    return res
TYPE_DECL = re.compile(r'^\s*(?:\[[^\]]*\]\s*)*public\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*'
                       r'(?:class|struct|interface|enum)\s+(?P<name>[A-Za-z_]\w*)')
NS_DECL = re.compile(r'^\s*namespace\s+(?P<name>[\w\.]+)')


def parse(path):
    """스텁 한 파일에서 «타입.멤버» 와 그 꼴(메서드/속성)을 뽑는다.

    ⚠ 여는 중괄호가 **선언 다음 줄**에 오는 꼴(이 레포 스타일)이 많아, 줄 단위로 «선언을 봤으니
      지금 타입 안» 이라고 세면 바로 닫혀 버린다(첫 판이 그랬다 · 읽은 멤버 0개). 중괄호를 실제로
      세어 «열린 타입» 을 스택으로 쥔다."""
    out = []
    ns = ''
    stack = []      # [(타입 이름, 그 타입이 열린 depth)]
    pending = None  # 선언은 봤고 여는 중괄호는 아직
    depth = 0
    for lineno, line in enumerate(open(path, encoding='utf-8'), 1):
        m = NS_DECL.match(line)
        if m: ns = m.group('name')
        t = TYPE_DECL.match(line)
        if t:
            pending = t.group('name')
        elif stack:
            mm = MEMBER.match(line)
            if mm:
                ps = params_of(line) if mm.group('tail') == '(' else None
                kind = 'method' if ps is not None else 'member'
                out.append((ns, stack[-1][0], mm.group('name'), kind, lineno, line.strip(), ps))
        for _ in range(line.count('{')):
            depth += 1
            if pending is not None:
                stack.append((pending, depth))
                pending = None
        for _ in range(line.count('}')):
            if stack and stack[-1][1] == depth: stack.pop()
            depth -= 1
    return out


def collect():
    rows = []
    for fn in sorted(os.listdir(STUB_DIR)):
        if fn not in REAL: continue
        for ns, ty, name, kind, lineno, src, ps in parse(os.path.join(STUB_DIR, fn)):
            rows.append({'file': fn, 'ns': ns, 'type': ty, 'name': name, 'kind': kind,
                         'line': lineno, 'src': src, 'params': ps})
    return rows


def key(r):
    return ('%s.%s.%s' % (r['ns'], r['type'], r['name'])) if r['ns'] else ('%s.%s' % (r['type'], r['name']))


def full_of(r):
    return ('%s.%s' % (r['ns'], r['type'])) if r['ns'] else r['type']


def sig_of(r):
    """스텁 한 줄의 서명 — 메서드는 «이름(줄인 매개변수)», 속성·필드는 «이름»."""
    if r['kind'] == 'method':
        return '%s(%s)' % (r['name'], ','.join(r['params'] or []))
    return r['name']


def emit(rows):
    """리플렉션 쪽에 «이 타입들의 공개 표면을 찍어 달라» 고 적는다 — **타입만** 준다.
    서명 견주기는 이쪽에서 한다(그래야 오버로드가 이름으로 뭉개지지 않는다 · 런 412 가 그 자리다)."""
    types = []
    for r in rows:
        hint = 'TMPro' if r['file'] == 'TMPro.cs' else 'URP'
        one = (hint, full_of(r))
        if one not in types: types.append(one)
    lines = ['# T174 — 진짜 유니티에게 «이 타입들의 공개 표면을 찍어 달라» 는 목록.',
             '#   `python3 tools/check_stub_sigs.py --emit` 이 스텁을 읽어 다시 쓴다(손으로 고치지 않는다).',
             '#   PlayMode `StubSigsTests` 가 이것을 읽어 ui-screens/stub-sigs.txt 로 답한다.',
             '# 한 줄 = <어셈블리 힌트>\t<타입 전체이름>']
    for hint, full in types:
        lines.append('%s\t%s' % (hint, full))
    text = '\n'.join(lines) + '\n'
    old = open(WANT, encoding='utf-8').read() if os.path.exists(WANT) else None
    if old != text:
        open(WANT, 'w', encoding='utf-8').write(text)
        return True, len(types)
    return False, len(types)


def verdict():
    """`screens` 가지의 판정 파일을 읽는다. 없으면 None."""
    try:
        subprocess.run(['git', 'fetch', '--depth', '1', '-q', 'origin', 'screens'],
                       cwd=ROOT, check=False, timeout=60,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        out = subprocess.run(['git', 'show', VERDICT_REF], cwd=ROOT, check=False,
                             timeout=60, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
        if out.returncode != 0 or not out.stdout: return None
        return out.stdout.decode('utf-8', 'replace')
    except Exception:
        return None


def read_dump(text):
    """진짜 유니티가 찍은 것을 «타입 → {이름: [매개변수 목록…]}» 으로 읽는다. 못 찾은 타입은 값이 None.

    fmt 2 부터 **선택 매개변수 뒤에 «=»** 가 붙는다(`char,bool=,bool=`) — 그게 있어야
    «스텁이 짧게 적은 것» 과 «실물에 없는 것» 을 가를 수 있다."""
    out, cur = {}, None
    fmt = 1
    for line in text.split('\n'):
        if not line: continue
        if line.startswith('#'):
            if line.strip() == '# fmt 2': fmt = 2
            continue
        p = line.split('\t')
        if p[0] == 'T':
            cur = p[1]
            out[cur] = None if (len(p) > 2 and p[2] == 'notfound') else {}
        elif p[0] == 'M' and cur is not None and out.get(cur) is not None:
            ps = [x for x in (p[2].split(',') if len(p) > 2 and p[2] else []) if x != '']
            out[cur].setdefault(p[1], []).append(ps)
        elif p[0] == 'P' and cur is not None and out.get(cur) is not None:
            out[cur].setdefault(p[1], []).append(None)      # 속성·필드 — 매개변수 없음
    return out, fmt


def fits(want, real_list):
    """스텁이 적은 매개변수 `want` 가 실물 오버로드 하나와 **부를 수 있게** 맞는가.

    ⓐ 그대로 같으면 맞다.
    ⓑ 실물이 더 길어도, 넘치는 자리가 **전부 선택 매개변수**면 그 호출은 컴파일된다
       (실물 `HasCharacter(char, bool = false, bool = false)` ↔ 스텁 `HasCharacter(char)`).
    돌려주는 것은 (맞는가, 앞자리가 겹쳤는가). 뒤엣것이 참이면 «스텁이 짧게 적은 자리» 라
    옛 판 판정 파일(선택 표시가 없다)에서는 빨강 대신 «모르는 것» 으로 내린다."""
    saw_prefix = False
    for real in real_list:
        if real is None: continue
        bare = [x[:-1] if x.endswith('=') else x for x in real]
        if bare == want: return True, False
        if len(real) > len(want) and bare[:len(want)] == want:
            rest = real[len(want):]
            if all(x.endswith('=') for x in rest): return True, False
            saw_prefix = True
    return False, saw_prefix


def judge(rows, dump, fmt=2):
    """스텁 멤버마다 «진짜에 있는가». (없는 것, 본 타입 수, 못 찾은 타입, 모르는 것)."""
    missing, unknown_types, unsure = [], [], []
    seen_types = set()
    for r in rows:
        full = full_of(r)
        if full not in dump: continue          # 아직 안 찍힌 타입 — 다음 런이 답한다
        real = dump[full]
        if real is None:
            if full not in unknown_types: unknown_types.append(full)
            continue
        seen_types.add(full)
        sig = sig_of(r)
        if ('%s.%s' % (full, sig)) in KNOWN or ('%s.%s' % (full, r['name'])) in KNOWN: continue
        cand = real.get(r['name'])
        if cand is None:
            missing.append((full, sig, r['file'], r['line'], r['src'][:110], '이름부터 없다'))
            continue
        if r['kind'] != 'method':
            if any(c is None for c in cand): continue
            missing.append((full, sig, r['file'], r['line'], r['src'][:110], '같은 이름이 메서드로만 있다'))
            continue
        ok, prefix = fits(r['params'] or [], cand)
        if ok: continue
        shown = ' · 실물: ' + ' / '.join('%s(%s)' % (r['name'], ','.join(c)) for c in cand if c is not None)
        # 앞자리가 겹쳤는데 **옛 판 파일**이면 «나머지가 선택인가» 를 알 길이 없다 — 빨강 대신 알림.
        if prefix and fmt < 2:
            unsure.append((full, sig, shown))
            continue
        missing.append((full, sig, r['file'], r['line'], r['src'][:110], '매개변수가 안 맞는다' + shown))
    return missing, len(seen_types), unknown_types, unsure


def run():
    rows = collect()
    if not rows:
        print('✗ check_stub_sigs: 스텁에서 공개 멤버를 하나도 못 읽었다 — 자가 고장 났다')
        return 1
    changed, ntypes = emit(rows)
    v = verdict()
    if v is None:
        print('⚠ check_stub_sigs: 진짜 유니티의 판정(%s)이 아직 없다 — **알리기만 한다**(rc 0).' % VERDICT_REF)
        print('  · 스텁 공개 멤버 %d개 · 물어볼 타입 %d개를 `tools/stub-sigs.want.txt` 에 적었다%s.'
              % (len(rows), ntypes, '(바뀌어 다시 썼다)' if changed else ''))
        print('  · 다음 유니티 런의 `StubSigsTests` 가 그 타입들의 진짜 표면을 찍어 screens 로 올린다.')
        return 0

    dump, fmt = read_dump(v)
    missing, nseen, unknown, unsure = judge(rows, dump, fmt)
    for t in unknown:
        print('⚠ check_stub_sigs: 진짜 유니티가 타입 «%s» 를 못 찾았다 — 이름이나 어셈블리가 바뀐 자리일 수 있다(알림).' % t)
    for full, sig, shown in unsure:
        print('⚠ check_stub_sigs: %s.%s — 스텁이 **짧게** 적었고 실물의 나머지 자리가 선택인지 이 판정 파일로는 모른다%s'
              % (full, sig, '(옛 판 파일 · fmt 2 를 찍는 런 뒤에 다시 본다)' if fmt < 2 else ''))
    if missing:
        print('✗ check_stub_sigs: 스텁이 **실물에 없는 서명** %d개를 갖고 있다 — 이대로 밀면 유니티가'
              ' «Scripts have compiler errors» 로 죽고 결과 XML 이 0개가 된다(§1 · 런 409·410·412).' % len(missing))
        for full, sig, fn, ln, src, why in missing:
            print('  · %s.%s — %s' % (full, sig, why))
            print('      %s:%d  %s' % (fn, ln, src))
        print('  고치는 법: 그 서명을 실물에서 다시 확인해 스텁을 고치거나, 그 멤버를 쓰는 코드를 실물에'
              ' 있는 길로 바꾼다(런 409 는 뒤쪽이었다). 이 레포가 일부러 더한 것이면 자의 KNOWN 에 까닭과 함께 적는다.')
        return 1

    print('✓ check_stub_sigs: 스텁 공개 멤버 %d개 · 진짜 유니티가 찍은 타입 %d개와 견줘 없는 서명 0%s'
          % (len(rows), nseen, (' · 모르는 것 %d(위 알림)' % len(unsure)) if unsure else ''))
    return 0


def self_test():
    """이 자가 «무엇을 잡는가» 를 스스로 증명한다 — 특히 **런 412 의 그 꼴**(가짜 오버로드)."""
    ok = 0
    fails = []

    def chk(name, cond):
        nonlocal ok
        if cond: ok += 1
        else: fails.append(name)

    rows = collect()
    keys = set('%s.%s' % (r['type'], r['name']) for r in rows)
    chk('TMP_FontAsset.CreateFontAsset 를 읽는다', 'TMP_FontAsset.CreateFontAsset' in keys)
    chk('TMP_Text 를 읽는다', any(r['type'] == 'TMP_Text' for r in rows))
    chk('공개 멤버가 스무 개는 넘는다', len(rows) > 20)
    chk('네임스페이스를 붙인다', any(r['ns'] == 'TMPro' for r in rows))
    chk('메서드와 속성을 가른다', {'method', 'member'} <= set(r['kind'] for r in rows))

    # 줄 꼴 — 여는 중괄호가 다음 줄인 스타일에서도 타입 안을 안 놓친다(첫 판이 여기서 0개였다)
    import tempfile
    src = ('namespace TMPro\n{\n'
           '  public class TMP_FontAsset\n  {\n'
           '    /// T106 — TMP 3.2 실서명: HasCharacter(uint unicode, bool, bool)\n'
           '    public bool HasCharacter(uint unicode, bool a, bool b) { return false; }\n'
           '    public bool HasCharacter(char c) { return false; }\n'
           '    public int atlasWidth { get; set; }\n'
           '    private bool Hidden(int x) { return false; }\n'
           '  }\n}\n')
    with tempfile.NamedTemporaryFile('w', suffix='.cs', delete=False, encoding='utf-8') as fh:
        fh.write(src); tmp = fh.name
    got = parse(tmp)
    os.unlink(tmp)
    names = [g[2] for g in got]
    chk('여는 중괄호가 다음 줄이어도 타입 안을 읽는다', len(got) == 3)
    chk('오버로드 둘을 따로 잡는다', names.count('HasCharacter') == 2)
    chk('속성도 잡는다', 'atlasWidth' in names)
    chk('private 는 안 잡는다', 'Hidden' not in names)

    fake = [{'file': 'TMPro.cs', 'ns': 'TMPro', 'type': 'TMP_FontAsset', 'name': g[2],
             'kind': g[3], 'line': g[4], 'src': g[5], 'params': g[6]} for g in got]
    chk('서명이 매개변수까지 담긴다', 'HasCharacter(uint,bool,bool)' in set(sig_of(r) for r in fake))
    chk('속성 서명은 이름뿐이다', 'atlasWidth' in set(sig_of(r) for r in fake))

    # ── 핵심 ①: 진짜가 `HasCharacter(char, bool=, bool=)` 뿐일 때 — 가짜 uint 는 잡고, 짧게 적은 char 는 안 잡는다.
    #    (선택 매개변수 표시 «=» 가 그 둘을 가른다 · 첫 판은 이걸 몰라 진짜를 셋이나 잘못 잡았다)
    real = ('# 머리\n# fmt 2\n'
            'T\tTMPro.TMP_FontAsset\n'
            'M\tHasCharacter\tchar,bool=,bool=\n'
            'P\tatlasWidth\n')
    dump, fmt = read_dump(real)
    chk('fmt 2 를 읽는다', fmt == 2)
    miss, nseen, unk, unsure = judge(fake, dump, fmt)
    chk('가짜 오버로드(uint)를 잡는다', any(m[1] == 'HasCharacter(uint,bool,bool)' for m in miss))
    chk('짧게 적은 진짜(char)는 안 잡는다 — 나머지가 선택이라 호출이 선다', not any(m[1] == 'HasCharacter(char)' for m in miss))
    chk('짧게 적은 진짜는 «모르는 것» 으로도 안 샌다', not any(u[1] == 'HasCharacter(char)' for u in unsure))
    chk('진짜 속성은 안 잡는다', not any(m[1] == 'atlasWidth' for m in miss))
    chk('본 타입 수를 센다', nseen == 1)
    chk('못 찾은 타입은 없다', unk == [])

    # ── 핵심 ②: 나머지가 **선택이 아니면** 짧게 적은 것은 진짜로 못 부른다 → 빨강
    d_req, f_req = read_dump('# fmt 2\nT\tTMPro.TMP_FontAsset\nM\tHasCharacter\tchar,bool,bool\nP\tatlasWidth\n')
    miss_req, _, _, _ = judge(fake, d_req, f_req)
    chk('나머지가 필수면 짧게 적은 것을 잡는다', any(m[1] == 'HasCharacter(char)' for m in miss_req))

    # ── 옛 판 판정 파일(fmt 1)은 선택 여부를 모른다 → 빨강이 아니라 «모르는 것»
    d_old, f_old = read_dump('T\tTMPro.TMP_FontAsset\nM\tHasCharacter\tchar,bool,bool\nP\tatlasWidth\n')
    miss_old, _, _, unsure_old = judge(fake, d_old, f_old)
    chk('옛 판 파일은 fmt 1 로 읽는다', f_old == 1)
    chk('옛 판에서 짧게 적은 것은 «모르는 것»', any(u[1] == 'HasCharacter(char)' for u in unsure_old))
    chk('옛 판에서도 가짜 오버로드는 잡는다', any(m[1] == 'HasCharacter(uint,bool,bool)' for m in miss_old))

    # 이름만 같고 매개변수가 다르면 **다른 것**이다(이름 대조였다면 여기서 뚫린다)
    d2, f2 = read_dump('# fmt 2\nT\tTMPro.TMP_FontAsset\nM\tHasCharacter\tuint,bool,bool\nM\tHasCharacter\tchar\nP\tatlasWidth\n')
    miss2, _, _, _ = judge(fake, d2, f2)
    chk('진짜에 있으면 초록', miss2 == [])

    # 타입을 못 찾았을 때는 «빨강» 이 아니라 «알림»
    d3, f3 = read_dump('# fmt 2\nT\tTMPro.TMP_FontAsset\tnotfound\n')
    miss3, _, unk3, _ = judge(fake, d3, f3)
    chk('타입을 못 찾으면 알림만', miss3 == [] and unk3 == ['TMPro.TMP_FontAsset'])

    # 아직 안 찍힌 타입은 건너뛴다(다음 런이 답한다)
    d4, f4 = read_dump('# fmt 2\nT\tTMPro.OtherType\n')
    miss4, _, _, _ = judge(fake, d4, f4)
    chk('안 찍힌 타입은 건너뛴다', miss4 == [])

    # 이름부터 없으면 잡는다
    d5, f5 = read_dump('# fmt 2\nT\tTMPro.TMP_FontAsset\nP\tatlasWidth\n')
    miss5, _, _, _ = judge(fake, d5, f5)
    chk('이름부터 없으면 잡는다', any(m[5] == '이름부터 없다' for m in miss5))

    # KNOWN — 이 레포가 일부러 더한 자리는 넘어간다
    KNOWN['TMPro.TMP_FontAsset.HasCharacter(uint,bool,bool)'] = '자기 검사'
    miss6, _, _, _ = judge(fake, dump, fmt)
    KNOWN.pop('TMPro.TMP_FontAsset.HasCharacter(uint,bool,bool)')
    chk('KNOWN 에 적힌 서명은 넘어간다', miss6 == [])

    # 줄인 이름 — 양쪽이 같은 자로 줄어야 견줄 수 있다
    chk('네임스페이스를 뗀다', short('UnityEngine.TextCore.LowLevel.GlyphRenderMode') == 'glyphrendermode')
    chk('기본값을 뗀다', short('AtlasPopulationMode = AtlasPopulationMode.Dynamic') == 'atlaspopulationmode')
    chk('제네릭은 남긴다', short('List<TMP_FontAsset>') == 'list<tmp_fontasset>')
    chk('매개변수 없는 메서드는 빈 목록', params_of('public void Stop() { }') == [])
    chk('속성은 매개변수가 없다(None)', params_of('public int atlasWidth { get; set; }') is None)

    print('%s check_stub_sigs --self-test: %d칸%s'
          % ('✓' if not fails else '✗', ok, '' if not fails else ' · 어긋남: ' + ', '.join(fails)))
    return 0 if not fails else 1


if __name__ == '__main__':
    if '--self-test' in sys.argv: sys.exit(self_test())
    if '--emit' in sys.argv:
        rows = collect(); _, n = emit(rows)
        print('tools/stub-sigs.want.txt — 스텁 공개 멤버 %d개 · 물어볼 타입 %d개' % (len(rows), n)); sys.exit(0)
    sys.exit(run())
