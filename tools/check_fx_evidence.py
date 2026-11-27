#!/usr/bin/env python3
# -*- coding: utf-8 -*-
u"""T520 — 연출 표의 `ms`·`ease` 칸이 **기계가 읽는 근거**를 들고 있는가.

까닭(T33 84회차 · 결정 887 ⓑ): «클론 표 칸 → 정본 선언» 방향 전수를 회차마다 **손으로** 걸어야 했다.
84회차가 기계로 걸어 봤을 때 «안 맞는다» 12칸이 **전부 오탐**이었는데, 원인이 하나다 —
자가 **파일 머리 주석**이나 **이웃 칸의 주석**을 그 칸의 근거로 갖다 붙였다.

그래서 이 자는 «근거인가» 를 **그 주석이 그 칸의 수를 인용하는가**로 판정한다(귀속 검사):
`hero` 묶음의 `_` 가 «beam .52s» 만 적었으면 그 주석은 `beam_ms`(520)의 근거이고
`heroring_ms`(620)의 근거가 **아니다**. 머리 주석도 같은 잣대로 저절로 걸러진다 —
수를 안 적은 글은 어느 칸의 근거도 아니다. 손으로 고른 표가 필요 없다.

걸음 셋(칸마다):
  ⓐ **근거 후보** — 형제 `"_<키>"` · 그 객체의 `"_"` · `_src`(또는 `_` 지도)의 **합성 키**(`"pop_ms · dgcpop"` 꼴).
  ⓑ **귀속** — 후보가 그 칸의 값을 인용하는가(`380` → `.38s`/`0.38s`/`380ms`/`380` · `ease-out` → 그 낱말).
  ⓒ **정본 검산** — 그 주석이 댄 «`style.css` 줄»·«`ui.js` 줄» 자리에서 **그 값이 아직 그렇게 적혀 있는가**.
     줄만 밀린 것은 ⚠(알리기만) · 그 파일에 값이 아예 없으면 ✗.

잣대는 `tools/ui_score.py` 의 `verify_stale_notes`(T512 걸음 ⓐ)와 같다 — 그쪽은 손으로 적은
«경로·줄·바늘» 표를 검산하고, 이쪽은 **표 주석에서 그 셋을 읽어낸다**.

  python3 tools/check_fx_evidence.py             # 본 검사(report — 막지 않는다)
  python3 tools/check_fx_evidence.py --list      # 칸마다 한 줄
  python3 tools/check_fx_evidence.py --missing   # 근거 없는 칸만
  python3 tools/check_fx_evidence.py --self-test # 자기 검사
"""
import json
import os
import re
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(REPO, "Assets", "Forge", "Resources")
WEB = os.path.join(REPO, ".wwwww-src", "web")
# 주석이 댄 줄에서 이만큼 안이면 «그 자리» 로 본다(ui_score.NOTE_LINE_DRIFT 와 같은 뜻).
# 60 인 까닭: 주석은 **규칙 줄**을 대는데 값은 그 아래 `@keyframes` 에 적힌 자리가 흔하다(정본 6747 ↔ 6748 꼴).
LINE_DRIFT = 60

MS_KEY = re.compile(r"(^|_)ms$")
EASE_KEY = re.compile(r"(^|_)ease$")
# 주석이 대는 정본 자리 — «style.css 1936» · «css **5398**» · «ui.js 2315» · «js/ui.js 337~351»
# ⚑ «정본 6680~6688» 꼴도 자리 표기다 — 이 표들의 관례로 그것은 **style.css** 줄이다(연출 수치가 사는 파일).
#   그 꼴을 안 받으면 같은 주석 안의 «ui.js 431»(마크업 근거)만 집어 **없는 ✗** 를 만든다(실측: `ray/spin_ms`).
CITE = re.compile(r"(?:(?P<f>style\.css|css|ui\.js|js/ui\.js|gamedata\.js|combat\.js|main\.js|techtree\.js|index\.html))"
                  r"[^\d\n]{0,12}?\*{0,2}(?P<l>\d{2,5})\*{0,2}")
# ⚑ «정본 6680~6688» 도 자리 표기다(이 표들의 관례로 **style.css** 줄이다) — 그런데 «정본 160» 은
#   **값**을 말하는 꼴이다(실측 `tierbreak._`: «`break_lead_ms`(정본 160)»). 둘을 가르는 표식은
#   ⓐ 구간(`~`)이거나 ⓑ 바로 뒤에 인용 조각(백틱)이 오는 것 — 그 둘만 줄로 받는다.
#   이 한 줄이 84회차 오탐의 **같은 함정**이다: 숫자가 보이면 줄로 읽는 버릇.
CITE_BARE = re.compile(r"(?:정본|원문)\s*\*{0,2}(?P<l>\d{2,5})\*{0,2}"
                       r"(?P<tail>\s*[~\-–]\s*\d{2,5}|\s*`)")
FILE_OF = {"style.css": "css/style.css", "css": "css/style.css", "ui.js": "js/ui.js",
           u"정본": "css/style.css", u"원문": "css/style.css",
           "js/ui.js": "js/ui.js", "gamedata.js": "js/gamedata.js", "combat.js": "js/combat.js",
           "main.js": "js/main.js", "techtree.js": "js/techtree.js", "index.html": "index.html"}
EASE_WORDS = ("ease-in-out", "ease-out", "ease-in", "linear", "steps", "ease")


def is_ms(key):
    return bool(MS_KEY.search(key))


def is_ease(key):
    return bool(EASE_KEY.search(key))


def ms_needles(v):
    u"""그 ms 값이 정본·주석에 적힐 수 있는 꼴 — `.38s` · `0.38s` · `380ms` · (js 는) `380`."""
    out = []
    s = ("%g" % (v / 1000.0))
    out.append(s + "s")
    if s.startswith("0."):
        out.append(s[1:] + "s")          # 0.38s → .38s (CSS 관례)
    out.append("%g" % v + "ms")
    return out, "%g" % v                 # (CSS 꼴들, js 가 쓰는 맨 수)


def num_forms(x):
    u"""한 수가 적힐 수 있는 꼴 — CSS 는 앞 0 을 뗀다(`0.1` ↔ `.1`)."""
    a = "%g" % x
    return (a, a[1:]) if a.startswith("0.") else (a,)


def ease_needles(v):
    if isinstance(v, (list, tuple)):     # cubic-bezier 네 수
        return ["cubic-bezier"] + [num_forms(x) for x in v], None
    return [str(v)], None


def quotes_value(note, kind, v):
    u"""ⓑ 귀속 — 그 주석이 **이 칸의 값**을 인용하는가. 이것이 84회차 오탐을 거르는 자리다."""
    if not isinstance(note, str):
        return False
    if kind == "ms":
        css, raw = ms_needles(v)
        if any(n in note for n in css):
            return True
        # js 타이밍은 맨 수로 적힌다 — 그 수가 «ui.js …» 를 댄 주석 안에 있을 때만 센다
        return bool(re.search(r"(ui\.js|js/ui\.js|combat\.js|main\.js)", note)) and \
            bool(re.search(r"(?<![\d.])" + re.escape(raw) + r"(?![\d.])", note))
    if isinstance(v, str):
        # `ease` 는 `ease-out` 의 일부라 낱말 경계로 본다(그래야 «기본 ease» 가 ease-out 을 안 먹는다)
        return bool(re.search(r"(?<![-\w])" + re.escape(v) + r"(?![-\w])", note))
    # cubic-bezier — 네 수가 **다** 있어야 그 칸의 근거다(한둘은 다른 칸의 수와 겹친다)
    return all(any(f in note for f in forms) for forms in ease_needles(v)[0][1:])


def candidates(key, holder, maps, inherit=()):
    u"""ⓐ 근거 후보 — 형제 `_<키>` · 그 객체의 `_` · 지도들의 합성 키. 머리 주석은 **안 넣는다**."""
    out = list(inherit)
    sib = holder.get("_" + key) if isinstance(holder, dict) else None
    if isinstance(sib, str):
        out.append(("형제 _" + key, sib))
    own = holder.get("_") if isinstance(holder, dict) else None
    if isinstance(own, str):
        out.append(("그 객체의 _", own))
    out.sort(key=lambda t: 0 if t[0].startswith(u"형제") else (1 if t[0] == u"그 객체의 _" else 2))
    for where, m in maps:
        for k, v in m.items():
            if not isinstance(v, str):
                continue
            if key in [t.strip() for t in re.split(r"[·,]", k)]:
                out.append((where + " «" + k + "»", v))
    return out


def cells(doc):
    u"""표 한 장의 ms·ease 칸 전부 — (경로, 종류, 값, 그 칸을 담은 객체)."""
    maps = []
    for mk in ("_src", "_notes", "_"):
        m = doc.get(mk) if isinstance(doc, dict) else None
        if isinstance(m, dict):
            maps.append((mk, m))
    found = []

    def walk(o, pre, inherit):
        u"""`inherit` = 조상이 물려준 근거 후보들 — 배열 안 키프레임 칸(`lootpop[0]/ease`)은
        제 형제가 없고 **부모 배열의 `_<키>`** 가 곧 그 칸의 근거다(그 배열 하나가 한 자리다)."""
        if isinstance(o, dict):
            for k, v in o.items():
                if k.startswith("_"):
                    continue
                if isinstance(v, (int, float)) and is_ms(k):
                    found.append((pre + k, "ms", v, o, inherit))
                elif (isinstance(v, str) or isinstance(v, list)) and is_ease(k):
                    if isinstance(v, list) and not all(isinstance(x, (int, float)) for x in v):
                        continue
                    found.append((pre + k, "ease", v, o, inherit))
                elif isinstance(v, dict):
                    walk(v, pre + k + "/", inherit + mine(o, k))
                elif isinstance(v, list):
                    for i, x in enumerate(v):
                        walk(x, pre + k + "[%d]/" % i, inherit + mine(o, k))
        return found

    def mine(holder, key):
        n = holder.get("_" + key) if isinstance(holder, dict) else None
        return [(u"조상 _" + key, n)] if isinstance(n, str) else []
    walk(doc, "", [])
    return found, maps


# 주석이 «기본 ease» 를 말하는 꼴 — 그때 근거는 «낱말이 **없다**» 는 것이다(CSS 는 안 적으면 ease).
DEFAULT_EASE = re.compile(r"(기본\s*(값)?\s*`?ease`?|timing\s*(이|을)?\s*(없|안\s*적)|이징(이|을)?\s*(없|안\s*적))")
# 주석이 «css 5389~5392» 처럼 **구간**으로 대는 꼴 — 그 끝 줄도 그 자리다.
# ⚠ 주석 안의 «세 자리 이상 수 아무거나» 를 자리로 받으면 **틀린 통과**가 난다(실측: `_amt` 가
#   7502 를 댔는데 본문의 «660» 에 끌려 642 의 `ease-out` 으로 통과했다 — `ease` 낱말은 정본에 널렸다).
#   그래서 받는 것은 **댄 줄과 그 구간**뿐이고, 같은 주석이 다른 줄을 더 대려면 **파일을 다시 적는다**.
CITE_RANGE = re.compile(r"\*{0,2}(\d{2,5})\*{0,2}\s*[~\-–]\s*(\d{2,5})")


def cites_of(note):
    u"""주석이 대는 «파일 줄» 전부 — 파일을 적은 꼴과 «정본 NNNN~MMMM»·«정본 NNNN `…`» 꼴."""
    out = [(FILE_OF[m.group("f")], int(m.group("l"))) for m in CITE.finditer(note)]
    out += [("css/style.css", int(m.group("l"))) for m in CITE_BARE.finditer(note)]
    return out


def verify(note, kind, v, reader):
    u"""ⓒ 정본 검산 — (상태, 문구). 상태: 'ok' · 'drift'(⚠) · 'gone'(✗) · 'nocite'(근거에 자리 표기가 없다)."""
    cites = cites_of(note)
    if not cites:
        return "nocite", u"주석에 «파일 줄» 표기가 없다"
    # ⓒ 이 표들은 파일을 한 번 적고 **줄을 잇는다**(«5657 … + 6148 …») — 주석 안의 세 자리 이상 수를
    #   전부 «그 파일의 다른 줄» 로 본다. 귀속(ⓑ)이 이미 «이 주석이 이 값을 인용한다» 를 증명했으므로
    #   이 느슨함으로 틀린 통과가 생기지 않는다 — 좁게 보면 **없는 ⚠** 만 늘어난다(실측 `enter/shake_delay_ms`).
    hinted = set(l for _, l in cites)
    for m in CITE_RANGE.finditer(note):
        a, b = int(m.group(1)), int(m.group(2))
        if a in hinted and b > a:
            hinted.add(b)
    css, raw = ms_needles(v) if kind == "ms" else ease_needles(v)
    anywhere = []
    for path, line in cites:
        text = reader(path)
        if text is None:
            return "gone", u"%s 를 못 읽었다" % path
        lines = text.split("\n")
        isjs = path.endswith(".js")
        flat = []
        for c in css:
            flat.extend(c if isinstance(c, tuple) else [c])
        pats = flat + ([raw] if (isjs and raw) else [])
        hit = [i + 1 for i, l in enumerate(lines)
               if any((re.search(r"(?<![\d.])" + re.escape(p) + r"(?![\d.\w])", l) if p == raw
                       else p in l) for p in pats)]
        if not hit:
            continue
        anywhere.append((path, line, hit))
        near = [h for h in hit if any(abs(h - g) <= LINE_DRIFT for g in hinted)]
        if near:
            return "ok", u"%s %d" % (path, near[0])
    if anywhere:
        path, line, hit = anywhere[0]
        return "drift", u"%s 의 줄이 낡았다 — 글은 %d · 실제 %s(값 자체는 살아 있다)" % (
            path, line, hit[0] if len(hit) == 1 else hit[:4])
    return "gone", u"%s 의 그 자리에 값이 없다 — 댄 자리 %s" % (
        cites[0][0], ", ".join("%s %d" % c for c in cites))


def verify_default_ease(note, reader):
    u"""기본 `ease` 검산 — 댄 줄에 **timing 낱말이 없어야** 맞다(CSS 는 안 적으면 ease다)."""
    cites = cites_of(note)
    if not cites:
        return "nocite", u"주석에 «파일 줄» 표기가 없다"
    path, line = cites[0]
    text = reader(path)
    if text is None:
        return "gone", u"%s 를 못 읽었다" % path
    lines = text.split("\n")
    if not (1 <= line <= len(lines)):
        return "drift", u"%s 에 %d 줄이 없다(파일 %d줄)" % (path, line, len(lines))
    win = "\n".join(lines[max(0, line - 3):line + 2])
    if re.search(r"(transition|animation)[^;{}]*", win) is None:
        return "drift", u"%s %d 근처에 transition·animation 선언이 없다" % (path, line)
    decl = " ".join(re.findall(r"(?:transition|animation)[^;{}]*", win))
    has = [w for w in EASE_WORDS if re.search(r"(?<![-\w])" + w + r"(?![-\w])", decl)]
    if has:
        return "gone", u"%s %d 이 timing 을 **적었다**(%s) — 기본 ease 가 아니다" % (path, line, has[0])
    return "ok", u"%s %d — timing 을 안 적었다(= 기본 ease)" % (path, line)


def reader_of(web):
    def reader(path):
        try:
            return open(os.path.join(web, path), encoding="utf-8").read()
        except Exception:
            return None
    return reader


def judge(resdir, reader, want_list=False, only_missing=False, out=None):
    out = sys.stdout if out is None else out
    rows, bad = [], []
    for fn in sorted(os.listdir(resdir)):
        if not fn.endswith(".json"):
            continue
        try:
            doc = json.load(open(os.path.join(resdir, fn), encoding="utf-8"))
        except Exception as e:
            bad.append(u"%s — 읽기 실패: %s" % (fn, e))
            continue
        cs, maps = cells(doc)
        for path, kind, v, holder, inherit in cs:
            # ⓐ `ms: 0` 은 «정본에 transition·animation 이 **없다**» 는 뜻이라(결정 863) 검산할 값이 없다 —
            #   0 을 수로 찾아 나서면 정본 아무 줄의 `0` 이나 물어 **없는 ⚠** 를 만든다(실측 `af_dd_item`).
            if kind == "ms" and v == 0:
                rows.append((fn, path, kind, v, "zero", u"정본에 그 전이가 없다(0 = 즉시 · 검산할 값이 없다)", ""))
                continue
            got = None
            for where, note in candidates(path.rsplit("/", 1)[-1], holder, maps, inherit):
                if quotes_value(note, kind, v):
                    got = (where, note)
                    break
            if got is None:
                rows.append((fn, path, kind, v, "none", u"근거 없음", ""))
                continue
            # ⓑ 기본값 `ease` 는 **낱말이 없는 것**이 근거다 — 정본이 timing 을 안 적었다는 뜻이니
            #   그 낱말을 정본에서 찾으면 안 된다(실측 `TransitionUi` `toggle`: 3113 은 `transition: left .15s` 뿐이다).
            if kind == "ease" and v == "ease" and DEFAULT_EASE.search(got[1]):
                state, why = verify_default_ease(got[1], reader)
            else:
                state, why = verify(got[1], kind, v, reader)
            rows.append((fn, path, kind, v, state, why, got[0]))
            if state == "gone":
                bad.append(u"%s · %s = %s → %s" % (fn, path, v, why))
    n = len(rows)
    ok = [r for r in rows if r[4] == "ok"]
    drift = [r for r in rows if r[4] == "drift"]
    gone = [r for r in rows if r[4] == "gone"]
    nocite = [r for r in rows if r[4] == "nocite"]
    none = [r for r in rows if r[4] == "none"]
    zero = [r for r in rows if r[4] == "zero"]
    if want_list or only_missing:
        for r in rows:
            if only_missing and r[4] not in ("none", "nocite"):
                continue
            mark = {"ok": u"✓", "drift": u"⚠", "gone": u"✗",
                    "nocite": u"·", "none": u"·", "zero": u"—"}[r[4]]
            out.write(u"  %s %-24s %-34s %-9s %s%s\n" % (
                mark, r[0], r[1], str(r[3])[:9], r[5], (u" ‹" + r[6] + u"›") if r[6] else u""))
    out.write(u"%s check_fx_evidence: `ms`·`ease` 칸 %d(검산할 값이 있는 것 %d · `ms: 0`=정본에 전이 없음 %d)"
              u" · 기계가 읽는 근거 %d(정본 검산 통과 %d · 줄만 낡음 %d · **사라짐 %d** · 자리 표기 없음 %d) · 근거 없는 칸 %d\n"
              % (u"✗" if gone else u"✓", n, n - len(zero), len(zero),
                 n - len(none) - len(zero), len(ok), len(drift), len(gone), len(nocite), len(none)))
    if gone:
        for b in bad:
            out.write(u"  ✗ %s\n" % b)
        out.write(u"  ⇒ 그 칸의 주석이 댄 자리에 그 값이 없다 — 정본이 바뀌었거나 주석이 틀렸다(수를 고치기 전에 **정본을 먼저 읽어라**)\n")
    return 1 if gone else 0, rows


def self_test():
    u"""자기 검사 — 고장을 **실제로 주입해** 자가 우는지 본다(gate.sh 관례)."""
    ok = [0]

    def chk(cond, label):
        ok[0] += 1
        if not cond:
            print(u"✗ 자기 검사 «%s» 실패" % label)
            sys.exit(1)
    # ⓑ 귀속 — 이웃 칸의 주석을 제 근거로 삼지 않는다(84회차 오탐의 원인)
    note = u"정본 6301 `.sr-beam { animation: srbeam .52s ease-out }` — 주역 광창"
    chk(quotes_value(note, "ms", 520), u"같은 수를 인용한 주석은 근거다(.52s ↔ 520)")
    chk(not quotes_value(note, "ms", 620), u"이웃 칸(620)은 그 주석을 못 쓴다")
    chk(not quotes_value(u"T492 — 소환 결과 연출 표", "ms", 520), u"수를 안 적은 머리 주석은 근거가 아니다")
    chk(quotes_value(u"css 4974 `transition: transform .07s ease-out`", "ease", "ease-out"), u"ease 낱말 인용")
    chk(not quotes_value(u"css 1936 `transition: all .25s` — 기본 ease", "ease", "ease-out"),
        u"«기본 ease» 가 ease-out 을 먹지 않는다(낱말 경계)")
    chk(quotes_value(u"css 1936 `transition: all .25s` — 기본 ease", "ease", "ease"), u"그 주석은 `ease` 의 근거다")
    chk(quotes_value(u"ui.js 3657 `setTimeout(… , 340)`", "ms", 340), u"js 는 맨 수로 적힌다")
    chk(not quotes_value(u"어디선가 340 이라고 적었다", "ms", 340), u"파일을 안 댄 맨 수는 근거가 아니다")
    # ⓐ 후보 — 머리 주석은 후보에 안 든다 · 합성 키는 든다
    holder = {"pop_ms": 380, "_": u"그 객체의 글"}
    cand = candidates("pop_ms", holder, [("_src", {u"pop_ms · dgcpop": u"css 5389 `.38s`"})])
    chk(any(w.startswith("_src") for w, _ in cand), u"합성 키가 후보에 든다")
    chk(any(w == u"그 객체의 _" for w, _ in cand), u"그 객체의 _ 도 후보다(귀속에서 걸러진다)")
    # ⓒ 검산 — 사본을 손으로 고쳐 ✗·⚠ 를 만든다
    fake = {"css/style.css": u"\n" * 100 + u".x { animation: pop .38s ease-out }\n"}
    rd = lambda p: fake.get(p)
    st, _ = verify(u"css 101 `.38s`", "ms", 380, rd)
    chk(st == "ok", u"댄 줄에 값이 있으면 통과")
    st, _ = verify(u"css 5389 `.38s`", "ms", 380, rd)
    chk(st == "drift", u"값은 있고 줄만 멀면 ⚠")
    st, _ = verify(u"css 101 `.99s`", "ms", 990, rd)
    chk(st == "gone", u"그 파일에 값이 없으면 ✗")
    st, _ = verify(u"근거 글만 있고 자리 표기가 없다", "ms", 380, rd)
    chk(st == "nocite", u"자리 표기가 없으면 nocite")
    # ms 꼴
    css, raw = ms_needles(1100)
    chk("1.1s" in css and raw == "1100", u"1100 → 1.1s")
    css, raw = ms_needles(90)
    chk(".09s" in css and "0.09s" in css, u"90 → .09s·0.09s")
    # ⓓ 찍기 — 다섯 상태가 **전부** 표시 지도에 있는가(`zero` 가 빠져 `--list` 가 터진 적이 있다)
    import io as _io
    fake_res = os.path.join(REPO, "Assets", "Forge", "Resources")
    buf = _io.StringIO()
    rc2, rows = judge(fake_res, lambda p: None, want_list=True, out=buf)
    chk(buf.getvalue().count("\n") > 10, u"--list 가 줄을 찍는다")
    chk(set(r[4] for r in rows) <= {"ok", "drift", "gone", "nocite", "none", "zero"}, u"상태는 다섯뿐")
    print(u"✓ check_fx_evidence --self-test: %d칸 통과" % ok[0])
    return 0


def main(argv):
    if "--self-test" in argv:
        return self_test()
    if not os.path.isdir(WEB):
        print(u"· check_fx_evidence: 정본 사본(.wwwww-src)이 없어 건너뛴다"
              u" — ROUTINE §0 의 `git clone --depth 1 … .wwwww-src` 뒤에 다시 돌려라")
        return 0
    rc, _ = judge(RES, reader_of(WEB), want_list="--list" in argv, only_missing="--missing" in argv)
    return rc


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
