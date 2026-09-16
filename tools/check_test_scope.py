#!/usr/bin/env python3
# -*- coding: utf-8 -*-
u"""PlayMode 자가 **앱 트리 전체에서 이름으로** 찾는 자리를 지킨다 (T414).

무엇이 문제였나 — 런 851 실측:
  `SkillGridTopSiteTests` 가 `FindActive(app, "sk-orb")` 로 앱 뿌리부터 찾는데
  `sk-orb` 라는 이름을 화면 **셋**(격자 · 스킬 상세 팝업 · 출전 줄)이 쓴다.
  같은 이름이 둘 이상이면 그 자는 **남이 열어 둔 화면의 상자**를 잴 수 있고,
  그렇게 나온 빨강은 «런 사이» 어느 커밋도 그 화면을 안 건드려서 **임자를 못 가린다**
  (워커 둘이 «임자 못 가림» 으로 적고 지나갔다).

이 자가 막는 것: **앱 뿌리부터 찾는 이름이 `Assets/Scripts/Game` 에서 두 곳 이상에 지어지는 것.**
  고치는 법은 둘 중 하나다 —
    ⓐ 찾는 뿌리를 그 화면으로 좁힌다(`PopupLayer.Instance.Find(<이름>).Root` · `UiRoot.Instance.Sheet` ·
       `SkillPetSheet.Instance.Skills.transform` …) — T414 1회차가 다섯 자리에 쓴 길
    ⓑ 그 이름을 그 화면에서만 쓰는 이름으로 바꾼다(게임 코드 · 남의 lock 일 수 있다)
  «한 화면만 쓰는» 이름은 안전하므로 통과시킨다 — 이 자는 **겹치는 것만** 운다.

쓰기:
  python3 tools/check_test_scope.py              # 본 검사
  python3 tools/check_test_scope.py --list       # 자리 전부와 «짓는 화면 수»
  python3 tools/check_test_scope.py --self-test  # 제 셈이 맞는지
"""
from __future__ import print_function
import os, re, sys, glob

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TESTS = os.path.join(REPO, "Assets", "Tests", "PlayMode")
GAME = os.path.join(REPO, "Assets", "Scripts", "Game")

# 자가 이름으로 찾는 꼴 — `Find*(<뿌리>, "이름")`. 뿌리는 뒤에서 «앱 뿌리인가» 로 가른다.
CALL = re.compile(r'Find\w*\(\s*(?:\(RectTransform\)\s*)?(?P<root>[A-Za-z_][\w.]*)\s*,\s*"(?P<name>[^"]+)"')

# 앱 **전체** 뿌리를 곧바로 적은 꼴.
APP_EXPR = "UiRoot.Instance.App"

# `Transform app = UiRoot.Instance.App;` 처럼 앱 뿌리를 담아 두는 이름 —
# **변수 이름이 아니라 «무엇이 담겼나» 로 가른다**(T414 1회차가 `app` 변수에 시트 뿌리를 담아 좁혔는데
#  이름만 보던 첫 판은 그것을 여전히 «앱 전체» 로 읽었다).
BIND = re.compile(r'\b(?:var|Transform|RectTransform)\s+(?P<var>\w+)\s*=\s*(?:\(RectTransform\)\s*)?'
                  r'(?P<rhs>UiRoot\.Instance\.App)\s*;')


def app_roots(text):
    u"""그 파일에서 «앱 전체 뿌리» 를 가리키는 이름들 — 직접 쓴 것 + 담아 둔 변수."""
    return set([APP_EXPR]) | set(m.group("var") for m in BIND.finditer(text))


# 이 자가 봐주는 것: 이름 뒤에 값이 붙는 꼴(`"cell-" + it.Slot`)도 접두만으로 센다.
def name_of(raw):
    return raw


def sites(tests_dir=TESTS):
    u"""[(자 파일, 줄, 이름)] — 앱 뿌리부터 이름으로 찾는 자리 전부."""
    out = []
    for path in sorted(glob.glob(os.path.join(tests_dir, "*.cs"))):
        try:
            text = open(path, encoding="utf-8").read()
        except TypeError:                                  # py2
            text = open(path).read().decode("utf-8")
        roots = app_roots(text)
        for i, line in enumerate(text.split("\n"), 1):
            for m in CALL.finditer(line):
                if m.group("root") in roots:
                    out.append((os.path.basename(path), i, name_of(m.group("name"))))
    return out


def builders(name, game_dir=GAME):
    u"""그 이름을 짓는 `Assets/Scripts/Game` 파일 — [(파일, 몇 번)]."""
    out = []
    for path in sorted(glob.glob(os.path.join(game_dir, "**", "*.cs"), recursive=True)):
        try:
            text = open(path, encoding="utf-8").read()
        except TypeError:
            text = open(path).read().decode("utf-8")
        n = text.count('"%s"' % name)
        if n:
            out.append((os.path.basename(path), n))
    return out


def judge(found, build_of):
    u"""(rc, 줄들) — `build_of(name)` 가 [(파일, 수)] 를 준다."""
    lines, bad = [], []
    for fn, ln, name in found:
        hits = build_of(name)
        tot = sum(n for _, n in hits)
        if tot > 1:
            bad.append((fn, ln, name, hits, tot))
    if not bad:
        lines.append(u"✓ check_test_scope: 앱 뿌리부터 이름으로 찾는 자리 %d개 · 이름이 겹치는 것 0"
                     % len(found))
        return 0, lines
    lines.append(u"✗ check_test_scope: **이름이 화면 둘 이상에 있는데 앱 뿌리부터 찾는** 자리 %d개 —"
                 u" 그 자는 남이 열어 둔 화면의 상자를 잴 수 있고, 그렇게 난 빨강은 임자를 못 가린다(T414)"
                 % len(bad))
    for fn, ln, name, hits, tot in bad:
        lines.append(u"  · %s:%d  «%s» — 짓는 곳 %d (%s)"
                     % (fn, ln, name, tot, u" · ".join(u"%s×%d" % h for h in hits[:4])))
    lines.append(u"고치는 법 ⓐ 찾는 뿌리를 그 화면으로 좁힌다 —"
                 u" `PopupLayer.Instance.Find(<Name>).Root` · `UiRoot.Instance.Sheet` ·"
                 u" `SkillPetSheet.Instance.Skills.transform`(T414 1회차가 다섯 자리에 쓴 길) ·")
    lines.append(u"          ⓑ 또는 그 이름을 그 화면에서만 쓰는 이름으로 바꾼다(게임 코드 · 남의 lock 일 수 있다).")
    return 1, lines


def cmd_list():
    found = sites()
    print(u"앱 뿌리부터 이름으로 찾는 자리 %d개\n" % len(found))
    for fn, ln, name in found:
        hits = builders(name)
        tot = sum(n for _, n in hits)
        print(u"%-28s %4d  %-22s 짓는 곳 %d  %s" % (fn, ln, name, tot, u"⚠ 겹침" if tot > 1 else u"✓ 하나"))
    return 0


def self_test():
    ok = [0, 0]
    def chk(cond, what):
        ok[1] += 1
        if cond: ok[0] += 1; print(u"  ✓ " + what)
        else:    print(u"  ✗ " + what)

    # ① 찾는 꼴을 알아본다 — «변수 이름» 이 아니라 «무엇이 담겼나» 로 가른다
    def hit(src):
        roots = app_roots(src)
        return [m.group("name") for m in CALL.finditer(src) if m.group("root") in roots]
    chk(hit('Find(UiRoot.Instance.App, "desc")') == ["desc"],
        u"`Find(UiRoot.Instance.App, \"…\")` 를 잡는다")
    chk(hit('FindActive((RectTransform)UiRoot.Instance.App, "slot-buy")') == ["slot-buy"],
        u"캐스트가 끼어도 잡는다")
    chk(hit('Transform app = UiRoot.Instance.App;\nTransform t = FindDeep(app, "cell-");') == ["cell-"],
        u"앱 뿌리를 담아 둔 변수도 잡는다")
    # 좁힌 자리는 **안 잡는다** — 그것이 이 자가 바라는 꼴이다
    chk(hit('Find(pass.Root, "desc")') == [], u"팝업 뿌리로 좁힌 자리는 안 잡는다")
    chk(hit('FindActive(panel, "sk-orb")') == [], u"패널 뿌리로 좁힌 자리는 안 잡는다")
    chk(hit('Transform app = UiRoot.Instance.Sheet;\nTransform t = FindDeep(app, "cell-");') == [],
        u"이름이 `app` 이어도 담긴 것이 시트 뿌리면 안 잡는다(T414 1회차의 그 자리다)")

    # ② 판정 — 한 화면만 쓰는 이름은 통과, 둘 이상이면 rc 1
    one = lambda n: [("A.cs", 1)]
    two = lambda n: [("A.cs", 1), ("B.cs", 1)]
    rc, out = judge([("T.cs", 3, "x")], one)
    chk(rc == 0 and u"겹치는 것 0" in out[0], u"한 화면만 쓰는 이름은 초록")
    rc, out = judge([("T.cs", 3, "x")], two)
    chk(rc == 1 and "T.cs:3" in out[1], u"두 화면이 쓰는 이름은 rc 1 이고 자리를 찍는다")
    chk(any(u"좁힌다" in l for l in out), u"고치는 법 두 갈래를 적는다")
    # 한 파일 안에서 두 번 지어도 겹침이다(그 화면이 두 상자에 같은 이름을 준 것)
    rc, _ = judge([("T.cs", 3, "x")], lambda n: [("A.cs", 2)])
    chk(rc == 1, u"한 파일이 같은 이름을 두 번 지어도 겹침으로 본다")
    rc, _ = judge([], one)
    chk(rc == 0, u"자리가 없으면 초록")

    # ③ 실물 — 이 레포에서 돌려도 셈이 선다
    found = sites()
    chk(len(found) >= 10, u"실물 자리를 센다 (%d개)" % len(found))

    print(u"\n%s check_test_scope self-test %s (%d칸)"
          % (u"✓" if ok[0] == ok[1] else u"✗",
             u"통과" if ok[0] == ok[1] else u"%d/%d 칸 실패" % (ok[1] - ok[0], ok[1]), ok[1]))
    return 0 if ok[0] == ok[1] else 1


def main():
    a = sys.argv[1:]
    if "--self-test" in a: return self_test()
    if "--list" in a: return cmd_list()
    rc, lines = judge(sites(), builders)
    for l in lines: print(l)
    return rc


if __name__ == "__main__":
    sys.exit(main())
