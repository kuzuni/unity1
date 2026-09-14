#!/usr/bin/env python3
"""`using` 한 네임스페이스를 그 어셈블리가 실제로 참조하는가 (T343).

왜 있나 — 런 **490·492** 가 EditMode·PlayMode 를 **통째로 0개**로 잃었다. 한 줄이 원인이다:
`Assets/Tests/PlayMode/SceneGradeTests.cs:5` 의 `using UnityEngine.Rendering.Universal;` ↔
`Forge.Tests.PlayMode.asmdef` 의 `references` 에 URP 가 없음 → `CS0234` → «Scripts have compiler errors».
어셈블리 하나가 안 서면 **테스트가 한 개도 안 돈다**(ROUTINE §1 «테스트 0개는 빨간 테스트보다 나쁘다») —
그 동안 모든 워커의 판정이 멈춘다.

**`dotnet build`(T48 하니스)는 이 갈래를 구조적으로 못 잡는다** — 하니스는 `Assets/Scripts`·`Assets/Tests` 를
**한 덩어리**로 컴파일하므로 asmdef 경계가 없다. 그래서 asmdef 쪽은 이 자가 따로 본다.

무엇을 하나
  - 우리 asmdef(`Assets/Scripts`·`Assets/Tests` 아래) 마다 그 아래 `.cs` 의 `using X.Y.Z;` 를 걷는다.
    (중첩 asmdef 이 있으면 안쪽은 그쪽 몫이다 · 주석·문자열 안의 `using` 은 안 센다 · `using var`·`using (` 은 문장이라 뺀다)
  - 네임스페이스 → 어셈블리 이름은 아래 `TABLE` 이 쥔다(가장 긴 접두가 이긴다).
  - 그 어셈블리가 asmdef 의 `references` 에 없으면 **막는다**(rc 1).
  - 표에 없는 네임스페이스는 **막지 않고 알린다** — 거짓 빨강을 만들지 않는다(결정 493 꼴).
  - `UnityEngine*`·`UnityEditor*`·`System*` 중 표에 없는 것은 엔진·에디터 기본이라 건너뛴다.
  - `references` 가 `GUID:` 꼴이고 그 GUID 를 저장소 안에서 못 찾으면 그 asmdef 는 **판정을 건너뛴다**(알리기만).

쓰는 법
  python3 tools/check_asmdef_refs.py             # 게이트(막는 자)
  python3 tools/check_asmdef_refs.py --self-test # 자기 검사
"""
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCAN = ("Assets/Scripts", "Assets/Tests")

# 네임스페이스(접두) → 그 네임스페이스를 주는 **어셈블리 이름**. 가장 긴 접두가 이긴다.
# ⚠ `UnityEngine.Rendering` 은 엔진 기본 모듈이라 **일부러 없다** — 그 아래 `…Universal` 만 URP 다.
TABLE = {
    "UnityEngine.Rendering.Universal": "Unity.RenderPipelines.Universal.Runtime",
    "UnityEngine.Rendering.RenderGraphModule": "Unity.RenderPipelines.Core.Runtime",
    "UnityEngine.Experimental.Rendering.RenderGraphModule": "Unity.RenderPipelines.Core.Runtime",
    "UnityEditor.Rendering.Universal": "Unity.RenderPipelines.Universal.Editor",
    "UnityEngine.UI": "UnityEngine.UI",
    "UnityEngine.EventSystems": "UnityEngine.UI",
    "UnityEngine.InputSystem": "Unity.InputSystem",
    "UnityEngine.TestTools": "UnityEngine.TestRunner",
    "UnityEditor.TestTools": "UnityEditor.TestRunner",
    "Unity.PerformanceTesting": "Unity.PerformanceTesting",
    "TMPro": "Unity.TextMeshPro",
    "Forge.Core": "Forge.Core",
    "Forge.Game": "Forge.Game",
}
# 표에 없어도 «엔진·에디터·BCL 기본» 이라 참조가 필요 없는 뿌리.
NATIVE = ("UnityEngine", "UnityEditor", "System", "Unity.Collections", "Unity.Mathematics", "Unity.Jobs", "Unity.Burst",
          "Unity.Profiling")   # Unity.Profiling 은 UnityEngine.CoreModule 이라 참조가 따로 없다

# 네임스페이스(접두) → `precompiledReferences` 의 **dll 이름**. 테스트 어셈블리의 NUnit 이 이 길이다
# (`references` 가 아니라 `precompiledReferences` + `overrideReferences: true` 로 문다 — 둘을 안 가르면 테스트 전부가 거짓 빨강이 된다).
PRECOMP = {
    "NUnit.Framework": "nunit.framework.dll",
}

USING = re.compile(r"^\s*(?:global\s+)?using\s+(?:static\s+)?(?:[A-Za-z_][\w]*\s*=\s*)?([A-Za-z_][\w.]*)\s*;")


def usings(path):
    """그 파일이 쓴 네임스페이스(주석·문자열 밖의 `using X.Y;` 만)."""
    out = []
    block = False
    try:
        lines = open(path, encoding="utf-8").read().splitlines()
    except (OSError, UnicodeDecodeError):
        return out
    for ln in lines:
        s = ln
        if block:
            if "*/" in s:
                s = s.split("*/", 1)[1]
                block = False
            else:
                continue
        while "/*" in s:
            head, rest = s.split("/*", 1)
            if "*/" in rest:
                s = head + rest.split("*/", 1)[1]
            else:
                s = head
                block = True
                break
        s = s.split("//", 1)[0]
        m = USING.match(s)
        if m:
            out.append(m.group(1))
    return out


def owner_assembly(ns):
    """가장 긴 접두로 어셈블리를 고른다 — 없으면 None."""
    best = None
    for key, asm in TABLE.items():
        if ns == key or ns.startswith(key + "."):
            if best is None or len(key) > len(best[0]):
                best = (key, asm)
    return best[1] if best else None


def precomp_dll(ns):
    """`precompiledReferences` 로 무는 네임스페이스면 그 dll 이름 — 아니면 None."""
    for key, dll in PRECOMP.items():
        if ns == key or ns.startswith(key + "."):
            return dll
    return None


def native(ns):
    return any(ns == n or ns.startswith(n + ".") for n in NATIVE)


def asmdefs(root):
    """우리 asmdef 목록 — (경로, 이름, references)."""
    found = []
    for base in SCAN:
        for dirpath, _dirnames, filenames in os.walk(os.path.join(root, base)):
            for fn in filenames:
                if fn.endswith(".asmdef"):
                    p = os.path.join(dirpath, fn)
                    try:
                        d = json.load(open(p, encoding="utf-8"))
                    except (OSError, ValueError):
                        continue
                    found.append((p, d.get("name", fn[:-7]), list(d.get("references") or [])))
    return sorted(found)


def guid_names(root):
    """저장소 안 asmdef 의 GUID → 이름(`references` 가 `GUID:` 꼴일 때 푼다)."""
    out = {}
    for dirpath, _dn, filenames in os.walk(os.path.join(root, "Assets")):
        for fn in filenames:
            if not fn.endswith(".asmdef.meta"):
                continue
            meta = os.path.join(dirpath, fn)
            asm = meta[:-5]
            try:
                name = json.load(open(asm, encoding="utf-8")).get("name")
                for ln in open(meta, encoding="utf-8"):
                    if ln.startswith("guid:"):
                        out[ln.split(":", 1)[1].strip()] = name
                        break
            except (OSError, ValueError):
                continue
    return out


def files_of(asmdir, others):
    """그 asmdef 폴더 아래 `.cs` — **더 안쪽 asmdef** 폴더는 그쪽 몫이라 뺀다."""
    inner = [d for d in others if d != asmdir and d.startswith(asmdir + os.sep)]
    out = []
    for dirpath, _dn, filenames in os.walk(asmdir):
        if any(dirpath == i or dirpath.startswith(i + os.sep) for i in inner):
            continue
        for fn in filenames:
            if fn.endswith(".cs"):
                out.append(os.path.join(dirpath, fn))
    return sorted(out)


def scan(root):
    """(막는 것, 알리는 것, 센 수) — 막는 것은 «참조가 없다», 알리는 것은 «표에 없는 네임스페이스»."""
    defs = asmdefs(root)
    dirs = [os.path.dirname(p) for p, _n, _r in defs]
    guids = guid_names(root)
    bad, note = [], []
    counted = 0
    for path, name, refs in defs:
        asmdir = os.path.dirname(path)
        have, unresolved = {name}, False
        try:
            pre = set(json.load(open(path, encoding="utf-8")).get("precompiledReferences") or [])
        except (OSError, ValueError):
            pre = set()
        for r in refs:
            if r.startswith("GUID:"):
                g = guids.get(r[5:])
                if g is None:
                    unresolved = True
                else:
                    have.add(g)
            else:
                have.add(r)
        for f in files_of(asmdir, dirs):
            for ns in usings(f):
                counted += 1
                dll = precomp_dll(ns)
                if dll is not None:
                    if dll not in pre:
                        bad.append((os.path.relpath(f, root), ns, dll + " (precompiledReferences)", name, os.path.relpath(path, root)))
                    continue
                asm = owner_assembly(ns)
                if asm is None:
                    if not native(ns):
                        note.append((os.path.relpath(f, root), ns))
                    continue
                if asm in have:
                    continue
                if unresolved:
                    note.append((os.path.relpath(f, root), ns + " (이 asmdef 의 GUID 참조를 못 풀어 판정을 건너뛴다)"))
                    continue
                bad.append((os.path.relpath(f, root), ns, asm, name, os.path.relpath(path, root)))
    return bad, note, counted, len(defs)


SELF = [
    # (이름, 코드 줄들, asmdef references, 막혀야 하나)
    ("런 490 그대로 — URP 를 참조 안 하는 PlayMode 자", ["using UnityEngine.Rendering.Universal;"], ["Forge.Game"], True),
    ("고친 뒤 — 참조가 있으면 통과", ["using UnityEngine.Rendering.Universal;"], ["Unity.RenderPipelines.Universal.Runtime"], False),
    ("엔진 기본 `UnityEngine.Rendering` 은 URP 가 아니다", ["using UnityEngine.Rendering;"], [], False),
    ("`UnityEngine` 자체는 건너뛴다", ["using UnityEngine;"], [], False),
    ("`System.*` 은 건너뛴다", ["using System.Collections.Generic;"], [], False),
    ("표에 없는 네임스페이스는 막지 않는다", ["using DG.Tweening;"], [], False),
    ("`UnityEngine.UI` 는 제 어셈블리가 있다 — 참조 없으면 막는다", ["using UnityEngine.UI;"], [], True),
    ("`UnityEngine.EventSystems` 도 UnityEngine.UI 어셈블리다", ["using UnityEngine.EventSystems;"], ["UnityEngine.UI"], False),
    ("`TMPro` → Unity.TextMeshPro", ["using TMPro;"], [], True),
    ("`NUnit.Framework` 는 precompiledReferences 로 문다 — references 에 있어도 소용없다", ["using NUnit.Framework;"], ["UnityEngine.TestRunner"], True),
    ("한 줄 주석 안의 using 은 안 센다", ["// using UnityEngine.UI;"], [], False),
    ("블록 주석 안의 using 은 안 센다", ["/*", "using UnityEngine.UI;", "*/"], [], False),
    ("여는 블록 주석이 같은 줄에서 닫히면 그 뒤는 센다", ["/* 메모 */ using UnityEngine.UI;"], [], True),
    ("`using var` 는 문장이라 안 센다", ["        using var x = F();"], [], False),
    ("별칭 using 도 센다", ["using Ui = UnityEngine.UI;"], [], True),
    ("`using static` 도 센다", ["using static UnityEngine.UI.Image;"], [], True),
    ("제 어셈블리 이름은 늘 있다", ["using Forge.Core.Data;"], [], False),   # 아래에서 asmdef 이름을 Forge.Core 로 준다
    ("Game 이 Core 를 안 적으면 막는다", ["using Forge.Core.Data;"], [], True),
    ("NUnit 은 precompiledReferences 가 있으면 통과", ["using NUnit.Framework;"], [], False),
    ("`Unity.Profiling` 은 엔진 코어라 건너뛴다", ["using Unity.Profiling;"], [], False),
]


def self_test():
    import shutil
    import tempfile
    ok = fail = 0
    for i, (title, lines, refs, want_bad) in enumerate(SELF):
        tmp = tempfile.mkdtemp(prefix="asmdefself")
        try:
            name = "Forge.Core" if title.startswith("제 어셈블리") else "Forge.Tests.PlayMode"
            d = os.path.join(tmp, "Assets", "Tests", "PlayMode")
            os.makedirs(d)
            pre = ["nunit.framework.dll"] if "precompiledReferences 가 있으면" in title else []
            json.dump({"name": name, "references": refs, "precompiledReferences": pre},
                      open(os.path.join(d, name + ".asmdef"), "w"))
            open(os.path.join(d, "T.cs"), "w", encoding="utf-8").write("\n".join(lines) + "\n")
            bad, _note, _c, _n = scan(tmp)
            got = len(bad) > 0
            if got == want_bad:
                ok += 1
            else:
                fail += 1
                print("  ✗ 칸 %d «%s» — 막혔나 %s, 바란 것 %s" % (i + 1, title, got, want_bad))
        finally:
            shutil.rmtree(tmp, ignore_errors=True)
    print("%s check_asmdef_refs --self-test: %d칸 · 어긋남 %d" % ("✓" if fail == 0 else "✗", ok + fail, fail))
    return 0 if fail == 0 else 1


def main():
    if "--self-test" in sys.argv:
        return self_test()
    bad, note, counted, ndefs = scan(ROOT)
    for f, ns, asm, aname, apath in bad:
        print("  ✗ %s — `using %s;` 인데 `%s` 의 references 에 **%s** 가 없다" % (f, ns, aname, asm))
        print("      고침: %s 의 \"references\" 에 \"%s\" 한 줄. 유니티는 CS0234 로 그 어셈블리를 통째로 못 세우고," % (apath, asm))
        print("            그러면 **그 모드의 테스트가 0개**가 된다(§1 · 런 490·492 실물).")
    for f, ns in note[:12]:
        print("  · (알림 · 막지 않는다) %s — `using %s;` 은 표에 없다(어느 어셈블리인지 이 자가 모른다)" % (f, ns))
    if len(note) > 12:
        print("  · … 그 밖 알림 %d줄" % (len(note) - 12))
    if bad:
        print("✗ check_asmdef_refs: asmdef %d개 · `using` %d줄 · **참조가 빠진 곳 %d** — 이대로면 유니티가 그 어셈블리를 못 세운다." % (ndefs, counted, len(bad)))
        return 1
    print("✓ check_asmdef_refs: asmdef %d개 · `using` %d줄 · 참조 빠짐 0 · 표에 없어 알리기만 한 것 %d" % (ndefs, counted, len(note)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
