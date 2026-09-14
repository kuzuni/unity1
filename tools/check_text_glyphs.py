#!/usr/bin/env python3
"""화면에 나가는 글자가 **글꼴에 다 있는가** (ROUTINE T89 막이).

왜 필요한가 — 런 148 의 실제 화면에서 이모지 30종이 전부 두부(□)였는데 유니티 잡은 초록이었다.
`TextSizeGateTests` 는 한글 구간(U+AC00~D7A3)만 봐서 이모지·기호를 놓쳤고, 화면을 눈으로 열어 본 회차에만
드러났다. 이 자는 **코드에 박힌 화면 문구**를 훑어 «글꼴에 없는 글자» 를 찍는다.

무엇을 보는가:
  ⓐ `Assets/Scripts/Game` **과 `Assets/Scripts/Core`**(T137 · 이 레포는 문구를 Core 로 내린다 — 전투·던전 토스트가 거기서 태어난다)의
     **모든 문자열 리터럴**(주석 줄 제외) + `StreamingAssets/data/*.json`·`Assets/Forge/Resources/*.json`
     의 **문자열 값**을 본다. 부름 인자만 보던 T89 판은 구멍이 둘이었다(T100 · 검수 Q 실측): «변수에 담은 문구»
     (`string autoLabel = "자동 ↻…"`)와 «데이터에서 오는 글자»(정본 `chat.js` 의 `😭`·`ㅠ`)를 통째로 놓쳤다 —
     셋 다 **전부 초록인 런**의 PNG 에서 □ 로 보였다. 식별자·키는 ASCII 라 넓혀도 오탐이 안 는다
     (이 자는 «글꼴에 없는 비 ASCII» 만 센다).
  ⓑ 정본 `TOAST_ICON`(`Assets/StreamingAssets/data/ui-text.json`)에 있는 이모지는 **아이콘으로 치환**되므로 뺀다
     (T89 의 `IconText`/`UiKit.IconTextRow` 가 하는 일 · 이형 선택자 U+FE0F 포함). 다만 «표에 있다» 가 «그 자리가
     아이콘을 거친다» 는 뜻은 아니다 — 표의 이모지를 **그냥 라벨 글자로** 쓰면 그대로 □ 다(실측: `ForgeSheet`
     잠금 «🔒» · `ForgeCraftPopup` «판매\n🪙 +»). 그래서 4회차부터 그런 자리를 따로 세어(`label_risk`)
     `LABEL_KNOWN` 에 없는 **새 자리는 rc 1** 로 막는다.
     ⚠ 그 «둘레에 `Toast(` 가 보이면 아이콘 길» 이라는 치기는 **토스트 그릇이 정말 아이콘을 거칠 때만** 옳다.
     5회차(T107)에 재 보니 그릇 셋 중 둘이 안 거쳤다 — `DungeonToast.Show` 는 `Bold(...)` 로, `PetSkillModal.Toast`
     는 `PetSkillKit.Text(...)` 로 글자만 세웠다. 그래서 ⭐·🔒·🧪·💎(던전·기술·승천)과 데이터 문구의
     🥚·✨·🎉·🎫·🧩·⬆️·⚡·⚙️·📋(`PetSkillUi.json` /text/toast_* 20줄)이 **자는 rc 0 인데 화면은 □** 였다.
     이제 그 가정을 **검사**한다(`toast_sinks`): 문구를 그리는 토스트 그릇은 아이콘 길을 거치거나 다른 그릇으로
     넘겨야 하고, 아니면 rc 1 이다.
  ⓒ 남은 글자를 주인 글꼴(`Assets/Fonts/NotoSansKR-Forge.ttf`)의 cmap 과 맞춰 없는 것을 찍는다.

`KNOWN` 은 «지금 알고 있고 임자가 정해진» 자리다 — 그것만 통과시키고 **새로 생긴 두부는 rc 1** 로 막는다.
의존성 0(순수 파이썬 · fonttools 없이 cmap 4/12 형식을 직접 읽는다).

사용:  python3 tools/check_text_glyphs.py [--self-test]
"""
import json
import os
import re
import struct
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
FONT = os.path.join(ROOT, 'Assets', 'Fonts', 'NotoSansKR-Forge.ttf')
TABLE = os.path.join(ROOT, 'Assets', 'StreamingAssets', 'data', 'ui-text.json')
# T137 — 훑는 폴더는 **목록**이다. T89 는 «화면 문구는 Game/Ui 에 있다» 로 시작했지만 이 레포의 규약은 수치·문구를
# `Core`(UnityEngine 참조 0)로 내리는 것이라 전투·던전 토스트가 Core 에서 태어나 `Emit` 으로 흘러 나간다 —
# 그 2,388줄이 자의 눈 밖이어서 진짜 두부 둘(🚪·🔥)이 rc 0 으로 지나갔다(T33 6회차 실측 · 결정 294).
GAME_DIR = os.path.join(ROOT, 'Assets', 'Scripts', 'Game')
CORE_DIR = os.path.join(ROOT, 'Assets', 'Scripts', 'Core')
SCAN_DIRS = [GAME_DIR, CORE_DIR]
SCAN_DIR = GAME_DIR   # 옛 이름 — 남의 스크립트가 부를 수 있어 남긴다(뜻은 «Game 폴더» 하나)

# 임자가 정해진 «아는 두부» — 정본도 이 글자를 문자열로 쓴다(ui.js 2272 ⏹ · 대장간 업그레이드 버튼 ⏱).
# 한글 글꼴에 이모지 글리프가 없어 □ 로 남는다: 주인 조치(이모지 폴백 글꼴)나 정본이 TOAST_ICON 에
# 그 두 글자를 넣어 주기 전에는 못 고친다. 여기 적은 것만 통과하고 **새 두부는 막는다**.
KNOWN = {
    '⏱': 'ForgeInfoPopup 업그레이드 버튼 — 정본도 **글자**로 쓴다(브라우저 이모지 글꼴이 그린다) · T106(이모지 폴백 글꼴)',
    '⏹': 'ForgeHost 자동 제련 종료 토스트(정본 ui.js 2272 그대로) — 정본도 **글자** · T106(이모지 폴백 글꼴)',
    # T100 — 넓힌 뒤 드러난 자리들. 임자가 정해져 있고 이 회차에 못 고친다(고칠 파일이 남의 lock · 이모지 폴백 글꼴은 주인 몫).
    # 2회차에 ㅋ·ㅠ 는 지웠다 — 글꼴 서브셋에 한글 자모 구획(U+3130~318F)을 더해 실제로 그려진다.
    # 3회차에 정본을 읽어 임자를 바로잡았다: 남은 일곱 중 `↻` 하나만 «정본이 아이콘으로 그리는 자리» 였고(T108 이 아틀라스 `autoloop` 아이콘으로 바꿔 지웠다 —
    # 그 자리가 다시 글자가 되면 «새 두부» 로 rc 1) 나머지 여섯은 정본도 **글자**라 고칠 길이 이모지 폴백 글꼴 하나뿐이다(T106 · 주인 에셋 승인).
    '😭': '채팅 문구(정본 chat.js 11행) — 정본도 **글자** · T106(이모지 폴백 글꼴)',
    '🐴': '탈것 토스트·얼굴 폴백(PetSkillUi.json /text/toast_mount_full · PetSkillKit 324행) — **정본도 글자다**: `ui.js` 2143 `creatureFace` 가 썸네일이 없는 동안 `<span>${emoji}</span>` 를 깐다(아이콘이 아니다 · 3회차에 정본을 읽고 바로잡음) · T106(이모지 폴백 글꼴)',
    '🐾': '같은 갈래(펫 보관함·출전 토스트 셋 + 얼굴 폴백) — **정본도 글자다**(`ui.js` 2162 `petFace` → `creatureFace`) · T106(이모지 폴백 글꼴)',
    # T137 — Core 를 훑자 드러난 둘. 정본도 **글자**다(아이콘 표에 없다) — 고칠 길은 이모지 폴백 글꼴(T106)뿐.
    '🚪': 'Core/Dungeons/Dungeons.cs:237 «🚪 진행 중이던 던전에서 나와…»(정본 dungeons.js 156 UI.toast 그대로 · DungeonPopups.Toast 로 나간다) — 정본도 **글자** · T106(이모지 폴백 글꼴)',
    '🔥': 'Core/Battle/Battle.cs:578 «🔥 난이도 상승!…»(정본 combat.js 499 UI.toast(…, combat) 그대로) — 정본도 **글자** · T106(이모지 폴백 글꼴) · 지금은 전투 토스트 레인이 없어 화면에 안 나간다(T138 이 세우면 그 길로)',
    '🛡': 'PlayerInfoUi.json /text/shield(미니 씬이 못 설 때의 폴백) — **정본도 글자다**: `ui.js` 5153 `<div class="pinfo-preview"><span>🛡️</span>…` · T106(이모지 폴백 글꼴)',
}

ROUTE = re.compile(r'IconTextRow|IconTextStack\.|IconText\.|UiText\.(?:Split|TextOnly)|Toast\(|TextOnly\(')   # IconTextStack(T110) = 줄마다 IconTextRow · 세로 갈래도 아이콘 길
ROUTE_CTX = 3   # 리터럴 둘레 몇 줄까지 «아이콘 길» 을 찾을까

# 아이콘 표 글자를 **아이콘을 안 거치고 글자로** 세우는 것이 이미 알려진 자리(임자 있음).
# 열쇠는 «파일 이름|리터럴» 이다 — 줄 번호로 잡으면 남이 위에 한 줄만 넣어도 어긋난다.
LABEL_KNOWN = {
    # T108 — `ForgeSheet.cs|🔒`(자동 제련 버튼 잠금) 은 아틀라스 `lock` 아이콘으로 바꿔 지웠다 · 다시 글자가 되면 «새 자리» 로 rc 1.
    # T110 2회차 — `ForgeCraftPopup.cs|판매…🪙` 둘 · `ForgeInfoPopup.cs|건너뛰기…💎` · `…업그레이드…🪙` 넷은 IconTextStack(이모지 표 길)로 바꿔 지웠다 · 다시 글자가 되면 «새 자리» 로 rc 1.
    # T137 — Core 자리. Core 는 그리지 않으므로 «둘레에 아이콘 길» 이 있을 수 없다 — 소비처를 손으로 따라가 적는다.
    'Dungeons.cs|🔨 ': 'Core Dungeons.RewardText — Game 에 **호출 0**(DungeonSheet.RewardLine·DungeonDetailPopup.BuildRewardRow 가 제 아이콘으로 그린다) · 부르는 날 IconTextRow 로',
    'Dungeons.cs|🪙 ': '같은 RewardText(호출 0)',
    'Dungeons.cs|🎫 ': '같은 RewardText(호출 0)',
    'Dungeons.cs|🥚 ': '같은 RewardText(호출 0)',
    'Dungeons.cs|🧪 ': '같은 RewardText(호출 0)',
    'Battle.cs|🪙 +': 'Battle Emit(Loot, tag) → BattleScene 371 DamageNumbers.Spawn(글자 그대로 · 아이콘 길 없음) — 정본은 #loot-feed 의 paintIconText · **T138**(전리품 레인)',
    'Battle.cs|🔨 +': '같은 Loot 갈래 · T138',
    'Battle.cs|🏆 ': 'Battle Emit(Toast, tag) — Game 에 받는 곳이 **없다**(BattleScene default) · 정본은 toast(…, combat) 레인 · **T138**(전투 토스트 레인)',
    'Battle.cs| 첫 클리어! 🪙+': '같은 줄의 둘째 리터럴 · T138',
}

LITERAL = re.compile(r'"((?:[^"\\]|\\.)*)"')
DATA_DIRS = [
    os.path.join(ROOT, 'Assets', 'StreamingAssets', 'data'),
    os.path.join(ROOT, 'Assets', 'Forge', 'Resources'),
]
# 데이터 안의 이모지가 전부 «화면 글자» 인 것은 아니다 — 정본은 이모지를 **아이콘 키**로도 쓴다
# (`PET_ICONS`·`MOUNT_ICONS`·`SKILL_ICONS`·`AGE_ICON`·`AVATAR_POOL`·기술트리 `icon`). 그 값은 `IconGen.img(…)`
# ·아바타 아틀라스(T31)로 그려지므로 글꼴과 무관하다. 키 경로로 가른다 — 값으로 가르면 같은 이모지가
# 글자로 쓰인 자리까지 놓친다.
ICON_KEY = re.compile(r'(?i)(icon|emoji|avatar)')
# T137 — C# 에도 같은 자리가 있다: `Icon = "🔨"`(DungeonDef) 처럼 **아이콘 키 이름에 대입되는** 리터럴은 화면 글자가
# 아니라 아틀라스 키다(데이터의 ICON_KEY 와 같은 규칙 · 리터럴 바로 앞의 «이름 =» 만 본다).
ICON_ASSIGN = re.compile(r'(?i)\b(?:icon|emoji|avatar)\w*\s*[=:]\s*$')


def literals(line):
    """한 줄의 문자열 리터럴 — 아이콘 키에 대입되는 것(`Icon = "…"`)은 뺀다(T137)."""
    return [m.group(1) for m in LITERAL.finditer(line) if not ICON_ASSIGN.search(line[:m.start()])]


def font_chars(path):
    """TTF cmap(형식 4·12) → 코드포인트 집합. (순수 함수 — 자기 검사가 이것을 본다.)"""
    d = open(path, 'rb').read()
    num = struct.unpack('>H', d[4:6])[0]
    tabs = {}
    for i in range(num):
        off = 12 + 16 * i
        tag = d[off:off + 4].decode('latin1')
        o, ln = struct.unpack('>II', d[off + 8:off + 16])
        tabs[tag] = (o, ln)
    if 'cmap' not in tabs:
        raise SystemExit('✗ check_text_glyphs: %s 에 cmap 표가 없다' % path)
    o = tabs['cmap'][0]
    n = struct.unpack('>H', d[o + 2:o + 4])[0]
    chosen = None
    for i in range(n):
        _pid, _eid, so = struct.unpack('>HHI', d[o + 4 + 8 * i:o + 12 + 8 * i])
        fmt = struct.unpack('>H', d[o + so:o + so + 2])[0]
        if fmt in (4, 12):
            chosen = (fmt, o + so)          # 형식 12 가 있으면 그것이 이긴다(BMP 밖까지 덮는다)
    if chosen is None:
        raise SystemExit('✗ check_text_glyphs: 읽을 수 있는 cmap 부표(형식 4·12)가 없다')
    fmt, base = chosen
    chars = set()
    if fmt == 4:
        segx2 = struct.unpack('>H', d[base + 6:base + 8])[0]
        seg = segx2 // 2
        ends = [struct.unpack('>H', d[base + 14 + 2 * i:base + 16 + 2 * i])[0] for i in range(seg)]
        sbase = base + 16 + segx2
        starts = [struct.unpack('>H', d[sbase + 2 * i:sbase + 2 * i + 2])[0] for i in range(seg)]
        for s, e in zip(starts, ends):
            if s == 0xFFFF:
                continue
            chars.update(range(s, min(e, 0xFFFE) + 1))
    else:
        groups = struct.unpack('>I', d[base + 12:base + 16])[0]
        for i in range(groups):
            s, e, _g = struct.unpack('>III', d[base + 16 + 12 * i:base + 28 + 12 * i])
            chars.update(range(s, min(e, s + 0xFFFF) + 1))
    return chars


def icon_chars(path):
    """정본 TOAST_ICON 의 키에 쓰인 글자 + 이형 선택자 — 아이콘으로 바뀌므로 글꼴에 없어도 된다."""
    table = json.load(open(path, encoding='utf-8'))['TOAST_ICON']
    out = {'️'}
    for key in table:
        out.update(key)
    return out


def screen_strings(root):
    """`root` 아래 C# 의 **모든 문자열 리터럴** → [(파일:줄, 문자열)] (주석 줄은 뺀다 · T100 으로 넓혔다)."""
    out = []
    for dirpath, _dirs, files in os.walk(root):
        for f in sorted(files):
            if not f.endswith('.cs'):
                continue
            p = os.path.join(dirpath, f)
            rel = os.path.relpath(p, ROOT)
            for i, line in enumerate(open(p, encoding='utf-8').read().split('\n'), 1):
                st = line.strip()
                if st.startswith('//') or st.startswith('*'):
                    continue
                for lit in literals(line):
                    out.append(('%s:%d' % (rel, i), lit))
    return out


def data_strings(dirs=None):
    """데이터가 쥔 화면 글자 → [(파일:키경로, 문자열)]. 채팅 문구처럼 **소스에 없는 글자**가 여기로 온다(T100 ⓑ)."""
    out = []
    for d in (dirs if dirs is not None else DATA_DIRS):
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith('.json'):
                continue
            p = os.path.join(d, f)
            rel = os.path.relpath(p, ROOT)
            try:
                doc = json.load(open(p, encoding='utf-8'))
            except ValueError:
                continue

            def walk(v, path):
                if isinstance(v, str):
                    if not ICON_KEY.search(path):          # 아이콘·아바타 «키» 는 글자가 아니다(아래 주석)
                        out.append(('%s:%s' % (rel, path or '/'), v))
                elif isinstance(v, dict):
                    for k, vv in v.items():
                        walk(vv, path + '/' + str(k))
                elif isinstance(v, list):
                    for i, vv in enumerate(v):
                        walk(vv, path + '/' + str(i))
            walk(doc, '')
    return out



def label_risk(root, font, table):
    """아이콘 표 글자를 쥔 코드 리터럴 중 **둘레에 아이콘 길이 안 보이는** 자리 → [(자리, 글자들, 리터럴)].

    표에 있는 이모지는 «아이콘으로 바뀐다» 는 전제로 건너뛰는데(ⓑ), 그 전제가 깨지는 자리가 실제로 있다.
    정적으로는 «이 리터럴 둘레에 `IconTextRow`·`Toast`·`IconText` 가 보이는가» 까지만 볼 수 있다 — 그래서
    이 함수는 **의심 자리**를 내놓고, 임자가 정해진 것은 `LABEL_KNOWN` 이 통과시킨다."""
    out = []
    for dirpath, _dirs, files in os.walk(root):
        for f in sorted(files):
            if not f.endswith('.cs'):
                continue
            path = os.path.join(dirpath, f)
            lines = open(path, encoding='utf-8').read().split('\n')
            rel = os.path.relpath(path, ROOT)
            for i, line in enumerate(lines, 1):
                st = line.strip()
                if st.startswith('//') or st.startswith('*'):
                    continue
                for lit in literals(line):
                    bad = sorted(set(c for c in lit if ord(c) >= 0x20 and ord(c) not in font and c in table))
                    if not bad:
                        continue
                    ctx = '\n'.join(lines[max(0, i - 1 - ROUTE_CTX):i + ROUTE_CTX])
                    if ROUTE.search(ctx):
                        continue
                    out.append(('%s:%d' % (rel, i), ''.join(bad), lit))
    return out


# ── 토스트 그릇 검사(T107) ────────────────────────────────────────────────────
# `ROUTE` 가 «둘레에 Toast( 가 보이면 아이콘 길» 로 치는 근거를 **코드로 확인**한다.
# 그릇 = 토스트 문구(`string`)를 받는 `Toast`/`Show` 메서드. 셋 중 하나면 통과다:
#   ⓐ 제 몸이 아이콘 길(`IconTextRow`·`UiText.Split`)을 부른다      — 실제로 그리는 그릇
#   ⓑ 같은 클래스의 메서드를 부르고 그 메서드가 ⓐ 다                 — 그리기를 한 겹 미룬 그릇(DungeonToast.Show → Paint)
#   ⓒ 다른 그릇(`…Toast(` · `…Show(`)으로 넘긴다                    — 전달자(MetaHost.Toast → PopupLayer.Toast)
#   ⓓ 이벤트로 넘긴다(`Emit(<Kind>.Toast, …)`) 그리고 **Game 이 그 이벤트를 그릇으로 받는다** — Core 의 그릇(T137 ·
#      Dungeons.Toast → Emit(DungeonEventKind.Toast) → DungeonSheet 86 `case DungeonEventKind.Toast: DungeonPopups.Toast(e.Text)`).
#      Core 는 그리지 못하니 ⓐ~ⓒ 가 없다 — 받는 줄이 Game 에 없으면 그 문구는 **어디에도 안 나간다**(그것도 rc 1 이다).
SINK_SIG = re.compile(r'\b(?:public|private|internal|protected|static|\s)*void\s+(Toast|Show)\s*\(\s*string\s')
ROUTER = re.compile(r'IconTextRow|IconTextStack\.|UiText\.Split')
FORWARD = re.compile(r'\b(?:Toast|Show)\s*\(')
CLASS_SIG = re.compile(r'\b(?:class|struct)\s+([A-Za-z_]\w*)')
EVENT_EMIT = re.compile(r'\bEmit\s*\(\s*([A-Za-z_]\w*)\.Toast\b')
EVENT_CTX = 2   # 받는 `case <Kind>.Toast` 줄부터 몇 줄 안에 그릇 호출이 있어야 하나

# 임자가 정해진 «안 거치는 그릇» — 지금은 비었다(T107 이 둘 다 이었다). 새로 생기면 여기 임자와 함께 적는다.
SINK_KNOWN = {}


def mask_cs(text):
    """주석·문자열을 같은 길이의 공백으로 지운 사본 — 중괄호 세기와 이름 찾기가 «문장 안의 { » 에 안 속는다.
    자리(인덱스)는 원본과 같다(순수 함수)."""
    out = list(text)
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == '/' and i + 1 < n and text[i + 1] == '/':
            while i < n and text[i] != '\n':
                out[i] = ' '; i += 1
        elif c == '/' and i + 1 < n and text[i + 1] == '*':
            while i < n and not (text[i] == '*' and i + 1 < n and text[i + 1] == '/'):
                if text[i] != '\n':
                    out[i] = ' '
                i += 1
            for _ in range(2):
                if i < n:
                    out[i] = ' '; i += 1
        elif c in '"\'':
            q = c
            verbatim = i > 0 and text[i - 1] == '@'
            out[i] = ' '; i += 1
            while i < n:
                if text[i] == '\\' and not verbatim:
                    out[i] = ' '
                    if i + 1 < n and text[i + 1] != '\n':
                        out[i + 1] = ' '
                    i += 2
                    continue
                if text[i] == q:
                    out[i] = ' '; i += 1
                    break
                if text[i] != '\n':
                    out[i] = ' '
                i += 1
        else:
            i += 1
    return ''.join(out)


def _close(masked, open_brace):
    """`open_brace` 의 `{` 와 짝이 맞는 `}` 자리."""
    depth = 0
    for i in range(open_brace, len(masked)):
        if masked[i] == '{':
            depth += 1
        elif masked[i] == '}':
            depth -= 1
            if depth == 0:
                return i
    return len(masked) - 1


def cs_methods(text):
    """C# 한 벌 → [(클래스, 메서드, 몸, 줄번호)] (주석·문자열을 지운 사본으로 자른다 · 순수 함수)."""
    masked = mask_cs(text)
    classes = []                                   # (이름, 여는 {, 닫는 })
    for m in re.finditer(r'\b(?:class|struct)\s+([A-Za-z_]\w*)', masked):
        ob = masked.find('{', m.end())
        if ob < 0:
            continue
        classes.append((m.group(1), ob, _close(masked, ob)))
    out = []
    for m in re.finditer(r'\b([A-Za-z_]\w*)\s*\([^;{}()]*\)\s*\{', masked):
        name = m.group(1)
        if name in ('if', 'for', 'foreach', 'while', 'switch', 'catch', 'lock', 'using', 'fixed', 'do'):
            continue
        ob = masked.index('{', m.end() - 1)
        ce = _close(masked, ob)
        cls = ''
        for cname, cob, cce in classes:
            if cob < m.start() < cce and (cls == '' or cob > best):
                cls, best = cname, cob
        out.append((cls, name, text[ob + 1:ce], text[:m.start()].count('\n') + 1))
    return out


def event_received(kind, game_root):
    """`<kind>.Toast` 이벤트를 `game_root` 의 어느 줄이 받아 그릇(`Toast(`·`Show(`)으로 넘기는가 → 그 자리 또는 None (T137 ⓓ)."""
    pat = re.compile(r'\b' + re.escape(kind) + r'\.Toast\b')
    for dirpath, _dirs, files in os.walk(game_root):
        for f in sorted(files):
            if not f.endswith('.cs'):
                continue
            path = os.path.join(dirpath, f)
            lines = mask_cs(open(path, encoding='utf-8').read()).split('\n')
            for i, line in enumerate(lines):
                if not pat.search(line) or 'Emit' in line:
                    continue                           # 보내는 쪽(Emit)은 받는 줄이 아니다
                if FORWARD.search('\n'.join(lines[i:i + 1 + EVENT_CTX])):
                    return '%s:%d' % (os.path.relpath(path, ROOT), i + 1)
    return None


def toast_sinks(root, game_root=None):
    """토스트 그릇 → [(자리, 클래스.메서드, 통과했는가, 사유)] (순수 함수 · `root` 아래 .cs 만 읽는다 ·
    `game_root` 를 주면 ⓓ «이벤트로 넘긴 것을 거기 어느 줄이 그릇으로 받는가» 까지 본다 · T137)."""
    out = []
    for dirpath, _dirs, files in os.walk(root):
        for f in sorted(files):
            if not f.endswith('.cs'):
                continue
            path = os.path.join(dirpath, f)
            rel = os.path.relpath(path, ROOT)
            text = open(path, encoding='utf-8').read()
            meths = cs_methods(text)
            for cls, name, body, line in meths:
                if name not in ('Toast', 'Show'):
                    continue
                if not SINK_SIG.search(_sig_line(mask_cs(text), line)):
                    continue                               # `Toast()` 처럼 문구를 안 받는 것은 그릇이 아니다
                if name == 'Show' and not cls.endswith('Toast'):
                    continue                               # `Show(string)` 은 흔한 이름이다 — 토스트 클래스의 것만 그릇으로 본다
                where = '%s:%d' % (rel, line)
                if ROUTER.search(body):
                    out.append((where, cls + '.' + name, True, 'ⓐ 제 몸이 아이콘 길을 부른다'))
                    continue
                helper = next((n for c, n, b, _l in meths
                               if c == cls and n != name and ROUTER.search(b)
                               and re.search(r'\b' + re.escape(n) + r'\s*\(', body)), None)
                if helper:
                    out.append((where, cls + '.' + name, True, 'ⓑ 같은 클래스의 «%s» 가 아이콘 길을 부른다' % helper))
                    continue
                if FORWARD.search(body):
                    out.append((where, cls + '.' + name, True, 'ⓒ 다른 그릇으로 넘긴다'))
                    continue
                ev = EVENT_EMIT.search(body)
                if ev:
                    got = event_received(ev.group(1), game_root) if game_root else None
                    if got:
                        out.append((where, cls + '.' + name, True, 'ⓓ 이벤트(%s.Toast)로 넘기고 %s 가 그릇으로 받는다' % (ev.group(1), got)))
                    else:
                        out.append((where, cls + '.' + name, False, '이벤트(%s.Toast)로 넘기는데 Game 에 그릇으로 받는 줄이 없다 — 문구가 어디에도 안 나간다' % ev.group(1)))
                    continue
                out.append((where, cls + '.' + name, False, '아이콘 길을 안 거치고 글자만 세운다'))
    return out


def _sig_line(text, line):
    """그 메서드의 서명 줄(«void Toast(string …» 인가를 본다)."""
    lines = text.split('\n')
    return lines[line - 1] if 0 < line <= len(lines) else ''


def label_key(where, lit):
    """`LABEL_KNOWN` 의 열쇠 — 파일 이름 + 리터럴(줄 번호는 안 쓴다)."""
    return '%s|%s' % (os.path.basename(where.rsplit(':', 1)[0]), lit)


def missing(strings, font, skip):
    """글꼴에도 없고 아이콘 표에도 없는 글자 → {글자: [자리…]} (순수 함수)."""
    out = {}
    for where, text in strings:
        for ch in text:
            if ord(ch) < 0x20 or ch in skip or ord(ch) in font:
                continue
            out.setdefault(ch, []).append(where)
    return out


def main(argv):
    if '--self-test' in argv:
        return self_test()
    font = font_chars(FONT)
    skip = icon_chars(TABLE)
    code = [s_ for d in SCAN_DIRS for s_ in screen_strings(d)]
    strings = code + data_strings()
    miss = missing(strings, font, skip)
    new = {ch: w for ch, w in miss.items() if ch not in KNOWN}
    risk = [r for d in SCAN_DIRS for r in label_risk(d, font, skip)]
    risk_new = [r for r in risk if label_key(r[0], r[2]) not in LABEL_KNOWN]
    sinks = [s_ for d in SCAN_DIRS for s_ in toast_sinks(d, GAME_DIR)]
    sink_bad = [s for s in sinks if not s[2] and s[1] not in SINK_KNOWN]
    for ch, where in sorted(miss.items()):
        tag = '(아는 것) ' + KNOWN[ch] if ch in KNOWN else '**새 두부**'
        print('  %s U+%05X «%s» %d곳 — %s' % ('·' if ch in KNOWN else '✗', ord(ch), ch, len(where), tag))
        if ch not in KNOWN:
            for w in sorted(set(where))[:6]:
                print('      %s' % w)
    for where, chars, lit in risk:
        tag = '(아는 것) ' + LABEL_KNOWN[label_key(where, lit)] if label_key(where, lit) in LABEL_KNOWN else '**새 자리**'
        print('  %s %s «%s» 라벨 «%s» — %s' % ('·' if tag.startswith('(') else '✗', where, chars, lit, tag))
    if new:
        print('✗ check_text_glyphs: 글꼴에 없는 글자가 화면 문구에 %d 종 새로 들어왔다 — 두부(□)로 찍힌다.' % len(new))
        print('  고침 둘: ⓐ 정본이 그 자리에 아이콘을 그리면 `UiIcons`/`UiKit.IconTextRow` 로(정본 줄을 먼저 읽는다)')
        print('          ⓑ 정본도 글자로 그리면 주인 글꼴에 그 구간이 있어야 한다 — 주인 조치이므로 KNOWN 에 임자와 함께 적는다.')
        return 1
    if risk_new:
        print('✗ check_text_glyphs: 아이콘 표의 이모지를 **아이콘을 안 거치고** 라벨 글자로 세운 자리가 %d 곳 새로 생겼다 — 그대로 □ 다.' % len(risk_new))
        print('  고침: 그 자리를 `UiKit.IconTextRow`(T89)로 세우거나, 정본이 정말 글자로 쓰면 `LABEL_KNOWN` 에 임자와 함께 적는다.')
        print('  («아이콘 표에 있으니 괜찮다» 는 전제가 깨지는 자리다 — T100 4회차가 낸 구멍.)')
        return 1
    if sink_bad:
        print('✗ check_text_glyphs: 토스트 그릇 %d개가 **아이콘 길을 안 거친다** — 그 그릇으로 가는 문구의 이모지는 전부 □ 다.' % len(sink_bad))
        for where, who, _ok, why in sink_bad:
            print('  ✗ %s  %s — %s' % (where, who, why))
        print('  왜 이것이 여기 있는가: 이 자는 «리터럴 둘레에 `Toast(` 가 보이면 아이콘 길» 로 쳐서 그 문구를 건너뛴다(ⓑ).')
        print('        그릇이 안 거치면 그 치기가 통째로 거짓이 되어 **초록인데 화면은 □** 다(T107 실측 · 그릇 둘 · 자리 45).')
        print('  고침: 그 그릇이 `UiKit.IconTextRow`(T89)로 문구를 세우게 한다 — 색이 제 표에서 오면 줄을 세운 뒤 조각마다 바른다.')
        return 1
    print('✓ check_text_glyphs: 문구 %d줄(코드 Game+Core %d + 데이터 %d) · 글꼴에 없는 글자 %d 종(전부 KNOWN · 임자 있음)'
          ' · 아이콘을 안 거친 라벨 %d곳(전부 LABEL_KNOWN) · 토스트 그릇 %d개 전부 아이콘 길'
          % (len(strings), len(code), len(data_strings()), len(miss), len(risk), len(sinks)))
    return 0


def self_test():
    ok = True
    font = font_chars(FONT)
    for ch, want in [('가', True), ('A', True), ('★', True), ('→', True), ('⏹', False), ('\U0001F528', False)]:
        got = ord(ch) in font
        if got != want:
            print('✗ cmap: «%s» U+%04X 기대 %s · 받은 %s' % (ch, ord(ch), want, got)); ok = False
    skip = icon_chars(TABLE)
    for ch, want in [('\U0001FA99', True), ('⚔', True), ('️', True), ('가', False)]:
        if (ch in skip) != want:
            print('✗ 아이콘 표: «%s» 기대 %s' % (ch, want)); ok = False
    # 스캔: 부름 안의 문자열만 · 주석은 뺀다
    cases = [
        ('UiKit.Text(p, "n", TextKind.Sub, "⏹ 끝", "ink");', 1),
        ('// UiKit.Text(p, "n", TextKind.Sub, "⏹ 끝");', 0),
        ('string s = "⏹";', 1),          # T100 — 변수 대입도 센다(ForgeSheet 의 «자동 ↻» 를 놓친 구멍)
    ]
    # 데이터: 글자 값은 세고 아이콘 «키» 는 안 센다(T100 · 정본이 이모지를 아이콘 이름으로도 쓴다)
    import json as _json
    import tempfile
    import tempfile as _tmp
    for doc, want, note in [
        ({'text': {'a': '⏹ 끝'}}, 1, '글자 값'),
        ({'PET_ICONS': {'Cat': '🐱'}}, 0, '아이콘 키'),
        ({'avatars': {'AVATAR_POOL': ['🐱']}}, 0, '아바타 키'),
        ({'chat': {'LINES': ['⏹']}}, 1, '채팅 문구'),
    ]:
        with _tmp.TemporaryDirectory() as d:
            _json.dump(doc, open(os.path.join(d, 'x.json'), 'w', encoding='utf-8'))
            got = len(missing(data_strings([d]), font, skip))
            if got != want:
                print('✗ 데이터 스캔 «%s»: 기대 %d · 받은 %d' % (note, want, got)); ok = False
    for code, want in cases:
        with tempfile.TemporaryDirectory() as d:
            open(os.path.join(d, 'X.cs'), 'w', encoding='utf-8').write(code)
            got = len(missing(screen_strings(d), font, skip))
            if got != want:
                print('✗ 스캔 «%s»: 기대 %d · 받은 %d' % (code[:40], want, got)); ok = False
    # 라벨 갈래(T100 4회차): 아이콘 표 글자를 아이콘 없이 세운 자리를 잡는가
    for code, fname, want, note in [
        ('UiKit.IconTextRow(p, "row", TextKind.Body, "🪙 +3", "ink", 0);', 'X.cs', 0, '아이콘 길이 보이면 안 센다'),
        ('IconTextStack.ReplaceLabel(b, TextKind.Sub, "판매\\n🪙 +3", "ink", "pp_red");', 'X.cs', 0, 'T110 세로 갈래(IconTextStack)도 아이콘 길이라 안 센다'),
        ('string s = "🪙 +3";', 'X.cs', 1, '그냥 라벨이면 센다'),
        ('string s = "🔒";', 'ForgeSheet.cs', 1, '아는 자리도 목록에는 오른다(rc 는 LABEL_KNOWN 이 가른다)'),
    ]:
        with tempfile.TemporaryDirectory() as d:
            open(os.path.join(d, fname), 'w', encoding='utf-8').write(code)
            got = len(label_risk(d, font, skip))
            if got != want:
                print('✗ 라벨 갈래 «%s»: 기대 %d · 받은 %d' % (note, want, got)); ok = False
    if label_key('Assets/Scripts/Core/Battle/Battle.cs:1', '🪙 +') not in LABEL_KNOWN:   # T110 2회차 뒤 남은 열쇠(T138 자리) — T138 이 빼면 다른 열쇠로
        print('✗ 라벨 열쇠: 파일 이름 + 리터럴로 LABEL_KNOWN 을 못 찾는다'); ok = False

    # T137 — Core 도 훑는다: 목록에 Core 가 있고, 둘째 폴더의 두부도 센다(고장 주입 · «Game 하나» 로 돌아가면 여기서 잡힌다)
    if not any(os.path.basename(d) == 'Core' for d in SCAN_DIRS) or not any(os.path.basename(d) == 'Game' for d in SCAN_DIRS):
        print('✗ SCAN_DIRS 에 Game·Core 둘 다 있어야 한다: %r' % SCAN_DIRS); ok = False
    with tempfile.TemporaryDirectory() as d:
        os.makedirs(os.path.join(d, 'Game')); os.makedirs(os.path.join(d, 'Core'))
        open(os.path.join(d, 'Game', 'A.cs'), 'w', encoding='utf-8').write('string a = "hello";')   # 첫 폴더는 깨끗한 줄(한글도 안 쓴다 — 서브셋 글꼴)
        open(os.path.join(d, 'Core', 'B.cs'), 'w', encoding='utf-8').write('Toast("🧿");')   # 글꼴에도 표에도 없는 글자(한글은 안 섞는다 — 서브셋 글꼴이라 없는 음절이 또 셀 수 있다)
        got = len(missing([x for sub in ('Game', 'Core') for x in screen_strings(os.path.join(d, sub))], font, skip))
        if got != 1:
            print('✗ Core 갈래(고장 주입): 둘째 폴더의 두부를 %d개로 셌다(기대 1)' % got); ok = False
    # T137 — C# 아이콘 키 대입은 화면 글자가 아니다(데이터 ICON_KEY 와 같은 규칙) · 라벨 대입은 여전히 센다
    for code, want, note in [
        ('new Def { Id = "x", Icon = "⏹", Kr = "이름" },', 0, '아이콘 키 대입'),
        ('Label = "⏹";', 1, '라벨 대입'),
        ('Toast("⏹ 끝");', 1, '부름 인자'),
    ]:
        with tempfile.TemporaryDirectory() as d:
            open(os.path.join(d, 'X.cs'), 'w', encoding='utf-8').write(code)
            got = len(missing(screen_strings(d), font, skip))
            if got != want:
                print('✗ 아이콘 키 대입 «%s»: 기대 %d · 받은 %d' % (note, want, got)); ok = False
    # T137 ⓓ — Core 그릇이 이벤트로 넘기면 Game 이 받는 줄까지 본다(받는 줄이 없으면 고장)
    core_sink = 'class Dungeons { void Toast(string text) { Emit(DungeonEventKind.Toast, text); } void Emit(DungeonEventKind k, string t = null) { } }'
    for game_code, want, note in [
        ('class S { void On(DungeonEvent e) { switch (e.Kind) { case DungeonEventKind.Toast: DungeonPopups.Toast(e.Text); break; } } }', True, 'ⓓ Game 이 그릇으로 받는다'),
        ('class S { void On(DungeonEvent e) { if (e.Kind == DungeonEventKind.Toast) { log(e.Text); } } }', False, '**고장 주입** — 받지만 그릇으로 안 넘긴다'),
        ('class S { void On(DungeonEvent e) { } }', False, '**고장 주입** — 받는 줄이 없다'),
    ]:
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'Core')); os.makedirs(os.path.join(d, 'Game'))
            open(os.path.join(d, 'Core', 'D.cs'), 'w', encoding='utf-8').write(core_sink)
            open(os.path.join(d, 'Game', 'S.cs'), 'w', encoding='utf-8').write(game_code)
            got = toast_sinks(os.path.join(d, 'Core'), os.path.join(d, 'Game'))
            if len(got) != 1 or got[0][2] != want:
                print('✗ 그릇 ⓓ «%s»: 기대 %s · 받은 %s' % (note, want, got)); ok = False

    # 주석·문자열 지우기(T107) — 중괄호·`class` 가 글 속에 있어도 안 속아야 한다
    for src, gone, note in [
        ('int a = 1; // class Fake {', 'class Fake', '줄 주석'),
        ('string s = "class Fake {";', 'class Fake', '문자열'),
        ('/* class Fake { */ int a;', 'class Fake', '덩이 주석'),
    ]:
        if gone in mask_cs(src):
            print('✗ 주석·문자열 지우기 «%s»: 아직 «%s» 가 보인다' % (note, gone)); ok = False
    if 'int a' not in mask_cs('int a = 1; // class Fake {'):
        print('✗ 주석·문자열 지우기: 코드까지 지웠다'); ok = False

    # 토스트 그릇 갈래(T107) — «둘레에 Toast( 가 보이면 아이콘 길» 이라는 치기의 근거를 코드로 확인한다
    SINK_CASES = [
        ('class T { public void Toast(string m) { UiKit.IconTextRow(box, "t", TextKind.Sub, m); } }',
         True, 'ⓐ 제 몸이 아이콘 길'),
        ('class DungeonToast { public static void Show(string m) { instance.Paint(m); }'
         ' void Paint(string m) { UiKit.IconTextRow(box, "t", TextKind.Sub, m); } }',
         True, 'ⓑ 같은 클래스의 도우미'),
        ('class T { public void Toast(string m) { PopupLayer.Instance.Toast(m); } }',
         True, 'ⓒ 전달자'),
        ('class T { public void Toast(string m) { UiKit.Text(box, "t", TextKind.Sub, m, "ink"); } }',
         False, '**고장 주입** — 글자만 세우는 그릇'),
        ('class T { public void Toast() { Clear(); } }',
         None, '문구를 안 받으면 그릇이 아니다'),
        ('class Panel { public void Show(string id) { Open(id); } }',
         None, '토스트 클래스가 아닌 Show(string) 은 그릇이 아니다'),
    ]
    for code, want, note in SINK_CASES:
        with tempfile.TemporaryDirectory() as d:
            open(os.path.join(d, 'X.cs'), 'w', encoding='utf-8').write(code)
            got = toast_sinks(d)
            if want is None:
                if got:
                    print('✗ 토스트 그릇 «%s»: 그릇이 아닌데 %d개를 잡았다' % (note, len(got))); ok = False
                continue
            if len(got) != 1 or got[0][2] != want:
                print('✗ 토스트 그릇 «%s»: 기대 %s · 받은 %s' % (note, want, got)); ok = False

    # 진짜 코드가 이 자를 지나는가
    rc = main([])
    if rc != 0:
        print('✗ 지금 Assets/Scripts(Game·Core)가 이 검사에 걸린다 — 위 ✗ 를 먼저 고쳐라'); ok = False
    print('✓ check_text_glyphs 자기 검사 통과' if ok else '✗ check_text_glyphs 자기 검사 실패')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
