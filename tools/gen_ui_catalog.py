#!/usr/bin/env python3
"""Assets/Forge/catalog.json → Assets/Forge/Resources/UiCatalog.asset (ROUTINE T18).

런타임은 Resources 밖의 GUI PRO Kit 스프라이트·주인 글꼴을 직접 못 읽는다. 그래서 카탈로그가 이름 댄 에셋들을
GUID 로 엮은 ScriptableObject(UiCatalog) 를 Resources 에 둔다 — 이 자가 catalog.json 과 각 에셋의 .meta 에서 그것을 만든다.
GUID 는 .meta 에서 읽는다(킷 조각은 주인 에디터가 준 진짜 GUID · 새 파일은 gen_meta.py 의 결정적 GUID). 먼저 gen_meta.py 를 돌린다.

T41 — **같은 키가 두 번이면 여기서 막는다.** CI 런 32 가 그 자리다: rebase 충돌을 «둘 다 살리기» 로 풀어
`"layout": [` 가 두 번이 됐는데 파이썬 `json.load` 는 **뒤** 블록을, 유니티 `JsonUtility` 는 **앞** 블록을 읽어
로컬 게이트도 dotnet 잡도 전부 초록인 채 PlayMode 만 `KeyNotFoundException` 으로 빨갰다. 두 가지를 본다:
  ⓐ 한 객체 안의 **JSON 키 중복**(최상위 `layout` 이든 `boot` 안이든 · `object_pairs_hook`) — 읽는 쪽마다 답이 다르다.
  ⓑ 한 배열 안의 **항목 키(`key`) 중복**(`colors`·`layout`·`sprites`…) — 유니티는 `map[e.key] = e.value` 로 **뒤** 것을,
     사람은 대개 **앞** 것을 읽어 조용히 엇갈린다.

T47 — **절의 «모양»** 도 본다(ⓒ). 런 46·54 가 그 자리다: T21 의 «복제 블록 병합» 이 색 항목 79개를 `colors` 가 아니라
`layout` 배열 안으로 넣어 44개 색(`white`·`muted2`·`pill_potion`…)이 사라졌는데, ⓐ·ⓑ 는 둘 다 안 걸렸다(키가 겹친 것이
아니라 **다른 절로 옮겨진** 것이다). 유니티는 그것을 `LayoutEntry`(값 0)로 읽고 `UiKit.C` 는 `KeyNotFoundException` —
로컬 게이트·dotnet 전부 초록인 채 던전·기술트리·승천 화면이 통째로 안 섰다.
  ⓒ 절마다 항목이 제 칸을 쥐는가: `colors` 는 `hex`(그리고 `value` 를 가지면 안 된다) · `layout` 은 `value` · `sprites` 는 `path`.

사용:  python3 tools/gen_ui_catalog.py [--check|--self-test]
       (--check: 지금 파일과 같은가 · 다르면 exit 1 · 키 중복·절 모양 어긋남도 exit 1)
"""
import json, os, sys, collections

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
CATALOG = 'Assets/Forge/catalog.json'
SCRIPT = 'Assets/Scripts/Game/Ui/UiCatalog.cs'
OUT = 'Assets/Forge/Resources/UiCatalog.asset'

FILEID_MONOSCRIPT = 11500000
FILEID_TEXTASSET = 4900000
FILEID_FONT = 12800000
FILEID_SPRITE = 21300000   # 단일 스프라이트(spriteMode 1) 텍스처의 Sprite 부 에셋


class DupKey(Exception):
    """같은 객체 안에 같은 키가 두 번(ⓐ) — 읽는 쪽마다 다른 값을 보게 된다."""

    def __init__(self, key, siblings):
        self.key = key
        self.siblings = siblings
        Exception.__init__(self, key)


def _no_dup_pairs(pairs):
    """`json.load(..., object_pairs_hook=…)` — 객체마다 키 중복을 그 자리에서 튕긴다(깊이 무관)."""
    seen = set()
    for k, _ in pairs:
        if k in seen:
            raise DupKey(k, [p[0] for p in pairs])
        seen.add(k)
    return dict(pairs)


def load_no_dups(text):
    """JSON 문자열 → 값. 키가 겹치면 `DupKey`. (순수 함수 — 자기 검사가 이것을 본다.)"""
    return json.loads(text, object_pairs_hook=_no_dup_pairs)


def entry_dups(cat):
    """ⓑ 배열 항목의 `key` 중복 → [(절 이름, 키, 몇 번)] (순수 함수)."""
    out = []
    for sec, val in cat.items():
        if not isinstance(val, list):
            continue
        keys = [e['key'] for e in val if isinstance(e, dict) and 'key' in e]
        for key, n in collections.Counter(keys).items():
            if n > 1:
                out.append((sec, key, n))
    out.sort()
    return out


# ⓒ 절 이름 → (있어야 하는 칸, 있으면 안 되는 칸들). 여기 없는 절(textKinds·tabs…)은 안 본다.
SECTION_SHAPE = {
    'colors': ('hex', ('value', 'path')),
    'layout': ('value', ('hex', 'path')),
    'sprites': ('path', ('hex', 'value')),
}


def shape_errors(cat):
    """ⓒ 항목이 제 절의 칸을 쥐는가 → [(절, 키, 왜)] (순수 함수).

    «색 항목이 layout 절에 들어갔다» 같은 **절 뒤바뀜**을 잡는다 — 키 중복이 아니라서 ⓐ·ⓑ 는 못 본다(T47)."""
    out = []
    for sec, (need, forbid) in SECTION_SHAPE.items():
        val = cat.get(sec)
        if not isinstance(val, list):
            continue
        for e in val:
            if not isinstance(e, dict) or 'key' not in e:
                continue
            wrong = [f for f in forbid if f in e]
            if wrong:
                out.append((sec, e['key'], '«%s» 절 항목인데 «%s» 칸을 쥐었다 — 다른 절 것이 섞였다' % (sec, wrong[0])))
            elif need not in e:
                out.append((sec, e['key'], '«%s» 절 항목에 «%s» 칸이 없다' % (sec, need)))
    out.sort()
    return out


def read_catalog(path):
    """catalog.json 을 읽는다 — 키가 겹치면 ✗ 를 찍고 `SystemExit(1)`(rc 1)."""
    rel = os.path.relpath(path, ROOT)
    with open(path, encoding='utf-8') as f:
        text = f.read()
    try:
        cat = load_no_dups(text)
    except DupKey as e:
        sib = ' '.join(e.siblings[:12]) + (' …' if len(e.siblings) > 12 else '')
        print('✗ gen_ui_catalog: %s — 한 객체 안에 같은 키 «%s» 가 두 번 (그 객체의 키: %s)' % (rel, e.key, sib))
        print('  파이썬은 뒤 블록을, 유니티 JsonUtility 는 앞 블록을 읽는다 — 항목 단위로 **한 블록에 병합**해라(CI 런 32).')
        raise SystemExit(1)
    dups = entry_dups(cat)
    if dups:
        for sec, key, n in dups:
            print('✗ gen_ui_catalog: %s 의 «%s» 절 — 항목 키 «%s» 가 %d 번' % (rel, sec, key, n))
        print('  유니티는 map[e.key] = e.value 로 **뒤** 것을 쥔다 — 겹친 항목을 지워 한 줄로 둬라.')
        raise SystemExit(1)
    bad = shape_errors(cat)
    if bad:
        for sec, key, why in bad[:8]:
            print('✗ gen_ui_catalog: %s — 항목 «%s»: %s' % (rel, key, why))
        if len(bad) > 8:
            print('  … 그 밖 %d 개(같은 갈래)' % (len(bad) - 8))
        print('  유니티 JsonUtility 는 절마다 상이 정해져 있다(colors=hex · layout=value) — 엉뚱한 절에 든 항목은')
        print('  **조용히 값 0 짜리 다른 것**이 되고 원래 절에서는 사라진다(런 46·54 의 DungeonUiTests 4/4 · T47).')
        raise SystemExit(1)
    return cat


def guid_of(rel):
    meta = os.path.join(ROOT, rel + '.meta')
    if not os.path.exists(meta):
        raise SystemExit('✗ gen_ui_catalog: %s.meta 가 없다 — 경로가 틀렸거나 gen_meta.py 를 아직 안 돌렸다' % rel)
    with open(meta, encoding='utf-8') as f:
        for line in f:
            if line.startswith('guid:'):
                return line.split(':', 1)[1].strip()
    raise SystemExit('✗ gen_ui_catalog: %s.meta 에 guid 줄이 없다' % rel)


def sprite_mode_ok(rel):
    with open(os.path.join(ROOT, rel + '.meta'), encoding='utf-8') as f:
        t = f.read()
    return 'textureType: 8' in t and 'spriteMode: 1' in t


def render(cat):
    lines = [
        '%YAML 1.1',
        '%TAG !u! tag:unity3d.com,2011:',
        '--- !u!114 &11400000',
        'MonoBehaviour:',
        '  m_ObjectHideFlags: 0',
        '  m_CorrespondingSourceObject: {fileID: 0}',
        '  m_PrefabInstance: {fileID: 0}',
        '  m_PrefabAsset: {fileID: 0}',
        '  m_GameObject: {fileID: 0}',
        '  m_Enabled: 1',
        '  m_EditorHideFlags: 0',
        '  m_Script: {fileID: %d, guid: %s, type: 3}' % (FILEID_MONOSCRIPT, guid_of(SCRIPT)),
        '  m_Name: UiCatalog',
        '  m_EditorClassIdentifier: ',
        '  catalog: {fileID: %d, guid: %s, type: 3}' % (FILEID_TEXTASSET, guid_of(CATALOG)),
        '  font: {fileID: %d, guid: %s, type: 3}' % (FILEID_FONT, guid_of(cat['font'])),
    ]
    # T106 — 이모지 폴백 글꼴(단색 Noto Emoji 서브셋). 키가 없으면 빈 참조(옛 카탈로그도 그대로 선다).
    if cat.get('emojiFont'):
        lines.append('  emojiFont: {fileID: %d, guid: %s, type: 3}' % (FILEID_FONT, guid_of(cat['emojiFont'])))
    else:
        lines.append('  emojiFont: {fileID: 0}')
    lines.append('  sprites:')
    seen = set()
    for e in cat['sprites']:
        key, rel = e['key'], e['path']
        if key in seen:
            raise SystemExit('✗ gen_ui_catalog: 스프라이트 키 «%s» 가 두 번이다' % key)
        seen.add(key)
        if not sprite_mode_ok(rel):
            raise SystemExit('✗ gen_ui_catalog: %s 는 단일 스프라이트(textureType 8 · spriteMode 1)가 아니다 — fileID 21300000 이 안 맞는다' % rel)
        lines.append('  - key: %s' % key)
        lines.append('    sprite: {fileID: %d, guid: %s, type: 3}' % (FILEID_SPRITE, guid_of(rel)))
    return '\n'.join(lines) + '\n'


def main(argv):
    if '--self-test' in argv or '--selftest' in argv:
        return self_test()
    cat = read_catalog(os.path.join(ROOT, CATALOG))
    text = render(cat)
    out = os.path.join(ROOT, OUT)
    if '--check' in argv:
        cur = open(out, encoding='utf-8').read() if os.path.exists(out) else None
        if cur != text:
            print('✗ gen_ui_catalog --check: %s 가 catalog.json 과 다르다 — python3 tools/gen_ui_catalog.py 로 다시 만든다' % OUT)
            return 1
        print('✓ gen_ui_catalog: %s ↔ catalog.json 일치 (스프라이트 %d)' % (OUT, len(cat['sprites'])))
        return 0
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)
    print('✓ gen_ui_catalog: %s 를 썼다 (스프라이트 %d · 글꼴 %s)' % (OUT, len(cat['sprites']), cat['font']))
    return 0


def self_test():
    """자기 검사 — 자가 조용히 고장 나면 «중복 0» 만 찍는다(T41)."""
    ok = True

    # ⓐ JSON 키 중복: 겹치면 DupKey · 안 겹치면 그대로 읽힌다.
    for text, want_dup, note in [
        ('{"a": 1, "b": 2}', None, '깨끗한 객체'),
        ('{"layout": [1], "colors": [], "layout": [2]}', 'layout', '최상위 중복 = CI 런 32 꼴'),
        ('{"boot": {"stage": "1-1", "stage": "1-2"}}', 'stage', '중첩 객체 안의 중복'),
        ('{"a": [{"k": 1, "k": 2}]}', 'k', '배열 안 객체의 중복'),
    ]:
        try:
            load_no_dups(text)
            got = None
        except DupKey as e:
            got = e.key
        if got != want_dup:
            print('✗ ⓐ %s: %s — 기대 %r · 받은 %r' % (note, text, want_dup, got)); ok = False

    # 겹치지 않은 JSON 은 json.load 와 같은 값이어야 한다(훅이 값을 바꾸지 않는다).
    sample = '{"a": {"b": [1, 2, {"c": 3}]}, "d": "e"}'
    if load_no_dups(sample) != json.loads(sample):
        print('✗ ⓐ 훅이 값을 바꿨다'); ok = False

    # ⓑ 항목 키 중복.
    for cat, want, note in [
        ({'colors': [{'key': 'a', 'value': 1}, {'key': 'b', 'value': 2}]}, [], '깨끗한 절'),
        ({'colors': [{'key': 'a'}, {'key': 'a'}]}, [('colors', 'a', 2)], '같은 절 두 번'),
        ({'layout': [{'key': 'a'}, {'key': 'a'}, {'key': 'a'}]}, [('layout', 'a', 3)], '세 번'),
        ({'colors': [{'key': 'a'}], 'layout': [{'key': 'a'}]}, [], '절이 다르면 겹친 것이 아니다'),
        ({'font': 'x', 'reference': {'w': 1}}, [], '배열이 아닌 칸은 안 본다'),
    ]:
        got = entry_dups(cat)
        if got != want:
            print('✗ ⓑ %s: 기대 %r · 받은 %r' % (note, want, got)); ok = False

    # ⓒ 절 모양 — «색 항목이 layout 에 들어갔다»(T47 · 런 46·54).
    for cat, want, note in [
        ({'colors': [{'key': 'a', 'hex': '#fff'}], 'layout': [{'key': 'b', 'value': 1}]}, [], '제자리'),
        ({'layout': [{'key': 'white', 'hex': '#ffffff'}]},
         [('layout', 'white', '«layout» 절 항목인데 «hex» 칸을 쥐었다 — 다른 절 것이 섞였다')], '색이 layout 에'),
        ({'colors': [{'key': 'rem_h', 'value': 0.018}]},
         [('colors', 'rem_h', '«colors» 절 항목인데 «value» 칸을 쥐었다 — 다른 절 것이 섞였다')], '배치가 colors 에'),
        ({'colors': [{'key': 'a', '_': '주석만'}]},
         [('colors', 'a', '«colors» 절 항목에 «hex» 칸이 없다')], '칸이 아예 없다'),
        ({'textKinds': [{'kind': 'Body', 'size': 40}], 'boot': {'stage': '1-1'}}, [], '안 보는 절'),
    ]:
        got = shape_errors(cat)
        if got != want:
            print('✗ ⓒ %s: 기대 %r · 받은 %r' % (note, want, got)); ok = False

    # 진짜 catalog.json 이 이 자를 지나는가(자기 검사가 늘 초록이면 아무것도 지키지 않는다).
    try:
        cat = read_catalog(os.path.join(ROOT, CATALOG))
        print('✓ gen_ui_catalog --self-test: 정본 %s 도 깨끗하다 (절 %d)' % (CATALOG, len(cat)))
    except SystemExit:
        print('✗ 지금 %s 가 이 검사에 걸린다 — 위 ✗ 를 먼저 고쳐라' % CATALOG); ok = False

    print('✓ gen_ui_catalog 자기 검사 통과' if ok else '✗ gen_ui_catalog 자기 검사 실패')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
