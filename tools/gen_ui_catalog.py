#!/usr/bin/env python3
"""Assets/Forge/catalog.json → Assets/Forge/Resources/UiCatalog.asset (ROUTINE T18).

런타임은 Resources 밖의 GUI PRO Kit 스프라이트·주인 글꼴을 직접 못 읽는다. 그래서 카탈로그가 이름 댄 에셋들을
GUID 로 엮은 ScriptableObject(UiCatalog) 를 Resources 에 둔다 — 이 자가 catalog.json 과 각 에셋의 .meta 에서 그것을 만든다.
GUID 는 .meta 에서 읽는다(킷 조각은 주인 에디터가 준 진짜 GUID · 새 파일은 gen_meta.py 의 결정적 GUID). 먼저 gen_meta.py 를 돌린다.

사용:  python3 tools/gen_ui_catalog.py [--check]     (--check: 지금 파일과 같은가 · 다르면 exit 1)
"""
import json, os, sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
CATALOG = 'Assets/Forge/catalog.json'
SCRIPT = 'Assets/Scripts/Game/Ui/UiCatalog.cs'
OUT = 'Assets/Forge/Resources/UiCatalog.asset'

FILEID_MONOSCRIPT = 11500000
FILEID_TEXTASSET = 4900000
FILEID_FONT = 12800000
FILEID_SPRITE = 21300000   # 단일 스프라이트(spriteMode 1) 텍스처의 Sprite 부 에셋


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
        '  sprites:',
    ]
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
    with open(os.path.join(ROOT, CATALOG), encoding='utf-8') as f:
        cat = json.load(f)
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


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
