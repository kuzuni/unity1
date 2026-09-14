#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T33 15회차 — 유니티 «메시지» 이름을 제 함수 이름으로 쓰지 않는다.

런 403 이 PlayMode 를 통째로 잃은 뿌리가 이것이었다(T171 · 워커 E): `BattlePreview` 는 `MonoBehaviour` 인데
그 안에 `public bool Start(RectTransform container)` 를 두었다. 유니티는 `Start` 를 **제 메시지**로 찾고,
인자가 있으면 못 부르며 인스턴스마다 «Script error (BattlePreview): Start() can not take parameters.» 를 찍는다 —
그 로그가 쌓이다 어셈블리 재적재 중 SIGSEGV 가 났고 **모든 워커의 판정이 한나절 멈췄다**.

`dotnet build`(하니스 · T48)는 이것을 못 잡는다 — C# 문법으로는 멀쩡한 메서드다. 잡을 수 있는 것은
«이름 + 그 클래스가 MonoBehaviour 인가» 뿐이라 이 자가 그것만 본다:

  MonoBehaviour 를 물려받은 클래스(직접이든, 이 저장소 안 다른 클래스를 거치든) 안에서
  유니티 메시지 이름을 쓴 메서드의 **인자 꼴이 그 메시지의 꼴과 다르면** rc 1.

고치는 법은 하나 — **이름을 바꾼다**(`Start`→`Begin` 꼴). 메시지를 정말 쓰는 것이면 인자를 꼴에 맞춘다.
"""
import os, re, sys

ROOTS = [os.path.join('Assets', 'Scripts'), os.path.join('Assets', 'Tests')]

# 이름 → 허용되는 인자 «타입 이름» 목록(빈 리스트 = 인자 없음). 여러 꼴이면 여러 줄.
NOARG = """Awake Start Update LateUpdate FixedUpdate OnEnable OnDisable OnDestroy OnGUI OnValidate Reset
OnApplicationQuit OnBecameVisible OnBecameInvisible OnPreCull OnPreRender OnPostRender OnRenderObject
OnWillRenderObject OnDrawGizmos OnDrawGizmosSelected OnMouseDown OnMouseUp OnMouseUpAsButton OnMouseEnter
OnMouseExit OnMouseOver OnMouseDrag OnTransformChildrenChanged OnTransformParentChanged OnAnimatorMove
OnBeforeTransformParentChanged OnCanvasGroupChanged OnRectTransformDimensionsChange OnDidApplyAnimationProperties
OnConnectedToServer OnDisconnectedFromServer OnServerInitialized""".split()
MESSAGES = {n: [[]] for n in NOARG}
MESSAGES.update({
    'OnApplicationPause': [['bool']],
    'OnApplicationFocus': [['bool']],
    'OnCollisionEnter': [['Collision']], 'OnCollisionStay': [['Collision']], 'OnCollisionExit': [['Collision']],
    'OnCollisionEnter2D': [['Collision2D']], 'OnCollisionStay2D': [['Collision2D']], 'OnCollisionExit2D': [['Collision2D']],
    'OnTriggerEnter': [['Collider']], 'OnTriggerStay': [['Collider']], 'OnTriggerExit': [['Collider']],
    'OnTriggerEnter2D': [['Collider2D']], 'OnTriggerStay2D': [['Collider2D']], 'OnTriggerExit2D': [['Collider2D']],
    'OnAnimatorIK': [['int']],
    'OnAudioFilterRead': [['float[]', 'int']],
    'OnRenderImage': [['RenderTexture', 'RenderTexture']],
    'OnParticleCollision': [['GameObject']],
    'OnJointBreak': [['float']], 'OnJointBreak2D': [['Joint2D']],
    'OnControllerColliderHit': [['ControllerColliderHit']],
    'OnLevelWasLoaded': [['int']],
})

RE_CLASS = re.compile(r'^\s*(?:public|internal|private|protected|sealed|abstract|static|partial|\s)*class\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?::\s*([^\{]+))?')
RE_METHOD = re.compile(r'^\s*(?:\[[^\]]*\]\s*)*(?:public|private|protected|internal|static|virtual|override|sealed|async|unsafe|extern|new|\s)*'
                       r'[A-Za-z_][A-Za-z0-9_<>,\.\[\]\s]*?\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)\s*(?:\{|$)')


def param_types(sig):
    """«RectTransform container, int n» → ['RectTransform', 'int'] (기본값·ref/out·이름은 뗀다)."""
    sig = sig.strip()
    if not sig:
        return []
    out = []
    depth = 0
    cur = ''
    for ch in sig:                      # 제네릭 안의 쉼표는 안 가른다
        if ch in '<([':
            depth += 1
        elif ch in '>)]':
            depth -= 1
        if ch == ',' and depth == 0:
            out.append(cur); cur = ''
        else:
            cur += ch
    out.append(cur)
    types = []
    for p in out:
        p = p.split('=')[0].strip()
        for kw in ('ref ', 'out ', 'in ', 'params ', 'this '):
            if p.startswith(kw):
                p = p[len(kw):].strip()
        parts = p.split()
        types.append(parts[0] if len(parts) < 2 else ' '.join(parts[:-1]))
    return [t.replace(' ', '') for t in types]


def scan_file(text):
    """[(줄번호, 클래스, 베이스목록, 메서드, 인자타입)] — 메서드는 가장 가까운 앞 class 에 붙인다."""
    out = []
    cls, bases = None, []
    for i, line in enumerate(text.splitlines(), 1):
        m = RE_CLASS.match(line)
        if m:
            cls = m.group(1)
            bases = [b.strip().split('<')[0] for b in (m.group(2) or '').split(',') if b.strip()]
            continue
        m = RE_METHOD.match(line)
        if m and cls is not None:
            name, sig = m.group(1), m.group(2)
            if name in MESSAGES:
                out.append((i, cls, bases, name, param_types(sig)))
    return out


def class_bases(text):
    out = {}
    for line in text.splitlines():
        m = RE_CLASS.match(line)
        if m:
            out[m.group(1)] = [b.strip().split('<')[0] for b in (m.group(2) or '').split(',') if b.strip()]
    return out


def self_test():
    ok = True
    cases = [
        ("    public bool Start(RectTransform container)\n    {", [(1, 'Start', ['RectTransform'])]),
        ("    private void Update()\n    {", [(1, 'Update', [])]),
        ("    void OnApplicationPause(bool paused) {", [(1, 'OnApplicationPause', ['bool'])]),
        ("    public static void Begin(RectTransform c) {", []),
        ("    IEnumerator Start() {", [(1, 'Start', [])]),
        ("    void OnAudioFilterRead(float[] data, int channels) {", [(1, 'OnAudioFilterRead', ['float[]', 'int'])]),
    ]
    for src, want in cases:
        got = [(i, n, p) for i, c, b, n, p in scan_file("class Z : MonoBehaviour\n{\n" + src)]
        got = [(i - 2, n, p) for i, n, p in got]
        if got != want:
            print('✗ 자기 검사: %r → %r (기대 %r)' % (src, got, want)); ok = False
    if param_types('ref int a, System.Collections.Generic.List<int, string> b') != ['int', 'System.Collections.Generic.List<int,string>']:
        print('✗ 자기 검사: 인자 가르기'); ok = False
    print('%s check_unity_messages --self-test: %d칸' % ('✓' if ok else '✗', len(cases) + 1))
    return 0 if ok else 1


def main():
    if '--self-test' in sys.argv:
        return self_test()
    files = []
    for root in ROOTS:
        for r, d, fs in os.walk(root):
            for fn in sorted(fs):
                if fn.endswith('.cs'):
                    files.append(os.path.join(r, fn))
    if not files:
        print('✗ check_unity_messages: %s 아래에 .cs 가 없다 — 저장소 뿌리에서 돌려라' % ' · '.join(ROOTS))
        return 1
    texts = {p: open(p, encoding='utf-8', errors='ignore').read() for p in files}
    bases = {}
    for t in texts.values():
        bases.update(class_bases(t))

    def is_mono(cls, seen=None):
        seen = seen or set()
        if cls in seen:
            return False
        seen.add(cls)
        for b in bases.get(cls, []):
            if b == 'MonoBehaviour' or is_mono(b, seen):
                return True
        return False

    bad, checked = [], 0
    for p, t in texts.items():
        for lineno, cls, cbases, name, ptypes in scan_file(t):
            if not ('MonoBehaviour' in cbases or is_mono(cls)):
                continue
            checked += 1
            if ptypes in MESSAGES[name]:
                continue
            want = ' 또는 '.join('(%s)' % ', '.join(s) if s else '()' for s in MESSAGES[name])
            bad.append('%s:%d — `%s.%s(%s)` : 이 클래스는 MonoBehaviour 라 유니티가 «%s» 를 제 메시지로 찾는다. '
                       '꼴은 %s 뿐이라 이대로면 인스턴스마다 «%s() can not take parameters.» 를 찍는다(T171 · 런 403 은 그 로그가 쌓이다 SIGSEGV). '
                       '**이름을 바꿔라**(`Begin`·`Open`…).' % (p, lineno, cls, name, ', '.join(ptypes), name, want, name))
    if bad:
        print('✗ check_unity_messages: %d곳' % len(bad))
        for b in bad:
            print('  · ' + b)
        return 1
    print('✓ check_unity_messages: MonoBehaviour 안의 메시지 이름 메서드 %d개 — 인자 꼴이 어긋난 것 0' % checked)
    return 0


if __name__ == '__main__':
    sys.exit(main())
