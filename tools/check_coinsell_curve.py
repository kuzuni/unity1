#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T408 — 판매 코인 연출의 **시간축**: 정본 `web/ref/shots/coinsell-<ms>ms.png` 25 프레임(480×854)의 «금빛 화소» 를
**자리(띠)로 좁혀** 센 곡선을 표(`Assets/Forge/Resources/CoinSellCurveUi.json`)에 쥐고, 표가 정본 프레임에서 다시 센 값과 같은지 지킨다.

셈(T393 2회차의 잣대를 자리로 좁힌 것 · 등재 ⓐ):
  · 금빛 = r>200 · 150<g<235 · b<130 · r−b>90 (표 `gold`).
  · 정적 바닥 = **마지막 프레임(3200ms)의 금빛 화소 집합** — 프레임마다 그 집합에 없는 화소만 «동적» 으로 센다(수를 빼는 것이 아니라 자리를 뺀다 ·
    HUD 알약·아이콘처럼 늘 있는 금빛이 빠진다).
  · 띠 셋(앱 높이 비율 · 표 `bands`): top 0~10%(HUD 코인 알약 — 닿는 곳) · mid 10~60%(나는 곳) · bot 60~100%(모루 띠 — 태어나는 곳 · 금액 라벨).
  정본 실측(이 자가 표로 쓴다): mid 는 **두 물결**(200~560 · 860~940) · 봉우리 **900ms**(5944) · **1000ms 에 끝**(89) · bot 은 1000~2200ms 에
  ≈1250 으로 평평하다(= 금액 라벨 `.coin-amt` 2000ms · 클론 `amt_ms` 2000 과 같은 뜻) · top 은 900·1100·1400ms 에 416(알약 박동).

PlayMode `CoinSellCurveTests` 가 클론 프레임을 같은 잣대(같은 시각 · 같은 띠 · 같은 금빛)로 재서 `ui-screens/t408-coinsell.txt` 에 남기고
봉우리·끝 시각을 표와 견준다 — 원작 PNG 는 이 레포에 복사하지 않는다(§1).

사용:  python3 tools/check_coinsell_curve.py [--shots <dir>] [--table <json>] [--write] [--list] [--self-test]
rc:    0 = 표가 정본에서 다시 센 값과 같다 · 1 = 다르다(--write 로 갱신) · 2 = 정본 프레임이나 표를 못 읽었다
"""
import glob
import json
import os
import re
import sys
import tempfile

SHOTS_DEFAULT = os.path.join('.wwwww-src', 'web', 'ref', 'shots')
TABLE_DEFAULT = os.path.join('Assets', 'Forge', 'Resources', 'CoinSellCurveUi.json')
GOLD = {'r_min': 200, 'g_min': 150, 'g_max': 235, 'b_max': 130, 'rb_min': 90}
BANDS = {'top': [0.0, 0.10], 'mid': [0.10, 0.60], 'bot': [0.60, 1.0]}
FRAME_RE = re.compile(r'coinsell-(\d+)ms\.png$')


def is_gold(r, g, b, gold=GOLD):
    return r > gold['r_min'] and gold['g_min'] < g < gold['g_max'] and b < gold['b_max'] and r - b > gold['rb_min']


def gold_set(path, gold=GOLD):
    """(폭, 높이, {(x, y)}) — PIL 로 읽는다."""
    from PIL import Image
    im = Image.open(path).convert('RGB'); w, h = im.size; px = im.load()
    out = set()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y][:3]
            if is_gold(r, g, b, gold): out.add((x, y))
    return w, h, out


def frames(shots_dir):
    """[(ms, path)] — 시각 차례."""
    fs = []
    for p in glob.glob(os.path.join(shots_dir, 'coinsell-*ms.png')):
        m = FRAME_RE.search(p)
        if m: fs.append((int(m.group(1)), p))
    return sorted(fs)


def measure(shots_dir, gold=GOLD, bands=BANDS):
    """정본 프레임 → {'w','h','frames':[{'ms','total','top','mid','bot'}]} · 프레임이 없으면 None."""
    fs = frames(shots_dir)
    if len(fs) < 2: return None
    w, h, floor = gold_set(fs[-1][1], gold)
    rows = []
    for ms, p in fs:
        _, _, pts = gold_set(p, gold)
        dyn = pts - floor
        row = {'ms': ms, 'total': len(dyn)}
        for name, (a, b) in bands.items():
            row[name] = sum(1 for x, y in dyn if a <= y / h < b)
        rows.append(row)
    return {'w': w, 'h': h, 'frames': rows}


def derive(rows, band='mid', end_f=0.05):
    """(봉우리 ms, 봉우리 값, 끝 ms) — 끝 = 봉우리 뒤 그 띠가 봉우리의 end_f 아래로 처음 내려가는 시각."""
    peak = max(rows, key=lambda r: r[band])
    end = None
    for r in rows:
        if r['ms'] > peak['ms'] and r[band] <= peak[band] * end_f:
            end = r['ms']; break
    return peak['ms'], peak[band], end


def build_table(m):
    peak_ms, peak_v, end_ms = derive(m['frames'])
    return {
        '_': 'ROUTINE T408 — 정본 web/ref/shots/coinsell-<ms>ms.png 25 프레임의 «금빛 화소» 를 띠(top HUD 0~10%H · mid 나는 곳 10~60%H · bot 모루 띠 60~100%H)로 좁혀 센 시간축. '
             '정적 바닥 = 마지막 프레임의 금빛 자리 집합(수가 아니라 자리를 뺀다). tools/check_coinsell_curve.py 가 정본에서 다시 세어 이 표를 지키고(--write 로 갱신), '
             'PlayMode CoinSellCurveTests 가 클론을 같은 잣대로 잰다. 원작 PNG 는 이 레포에 없다(§1 · .wwwww-src 읽기 전용).',
        'src_w': m['w'], 'src_h': m['h'],
        'gold': dict(GOLD),
        'bands': {k: list(v) for k, v in BANDS.items()},
        'flight_band': 'mid',
        'end_f': 0.05,
        'peak_ms': peak_ms, 'peak_mid': peak_v, 'end_ms': end_ms,
        'frames': m['frames'],
    }


def load_table(path):
    with open(path, encoding='utf-8') as f: return json.load(f)


def run(shots_dir, table_path, write=False, list_all=False, out=print):
    m = measure(shots_dir)
    if m is None:
        out('✗ check_coinsell_curve: 정본 프레임을 못 읽었다 — %s (coinsell-*ms.png 2장 이상)' % shots_dir)
        return 2
    fresh = build_table(m)
    if write:
        with open(table_path, 'w', encoding='utf-8') as f:
            json.dump(fresh, f, ensure_ascii=False, indent=2); f.write('\n')
        out('✓ check_coinsell_curve: 표를 썼다 — %s (프레임 %d · 봉우리 %dms %d · 끝 %sms)' % (table_path, len(fresh['frames']), fresh['peak_ms'], fresh['peak_mid'], fresh['end_ms']))
        return 0
    try:
        t = load_table(table_path)
    except (OSError, ValueError) as e:
        out('✗ check_coinsell_curve: 표를 못 읽었다 — %s (%s)' % (table_path, e))
        return 2
    bad = []
    for k in ('src_w', 'src_h', 'peak_ms', 'peak_mid', 'end_ms'):
        if t.get(k) != fresh[k]: bad.append('%s: 표 %r ↔ 정본 %r' % (k, t.get(k), fresh[k]))
    tf = {r['ms']: r for r in t.get('frames', [])}
    for r in fresh['frames']:
        o = tf.get(r['ms'])
        if o is None: bad.append('%dms: 표에 없다' % r['ms']); continue
        for k in ('total', 'top', 'mid', 'bot'):
            if o.get(k) != r[k]: bad.append('%dms %s: 표 %r ↔ 정본 %r' % (r['ms'], k, o.get(k), r[k]))
    extra = sorted(set(tf) - set(r['ms'] for r in fresh['frames']))
    if extra: bad.append('표에만 있는 시각: %s' % extra)
    if list_all:
        out('    ms  total   top   mid   bot')
        for r in fresh['frames']: out('  %5d %6d %5d %5d %5d' % (r['ms'], r['total'], r['top'], r['mid'], r['bot']))
    if bad:
        out('✗ check_coinsell_curve: 표 ↔ 정본 %d곳 (--write 로 갱신)' % len(bad))
        for b in bad[:20]: out('  · ' + b)
        return 1
    out('✓ check_coinsell_curve: 정본 프레임 %d · 표와 같다 · 봉우리(mid) %dms %d · 끝 %sms · 띠 %s' % (len(fresh['frames']), fresh['peak_ms'], fresh['peak_mid'], fresh['end_ms'], '/'.join(BANDS)))
    return 0


def self_test():
    fails = []
    def eq(name, got, want):
        if got != want: fails.append('%s: %r ≠ %r' % (name, got, want))
    from PIL import Image
    eq('ⓐ 금빛 판별', (is_gold(255, 200, 50), is_gold(255, 240, 50), is_gold(255, 200, 200), is_gold(100, 200, 50)), (True, False, False, False))
    with tempfile.TemporaryDirectory() as d:
        w, h = 40, 100
        def frame(ms, gold_pts):
            im = Image.new('RGB', (w, h), (30, 30, 30)); px = im.load()
            for x, y in gold_pts: px[x, y] = (255, 200, 40)
            im.save(os.path.join(d, 'coinsell-%04dms.png' % ms))
        static = {(1, 2), (2, 2)}                              # HUD 알약처럼 늘 있는 금빛(top)
        frame(40, static | {(5, 70), (6, 70)})                   # bot 2
        frame(200, static | {(5, 30), (6, 30), (7, 30), (5, 70)})   # mid 3 · bot 1
        frame(900, static | {(x, 30) for x in range(10)} | {(0, 5)})   # mid 10 · top 1 (봉우리)
        frame(1000, static | {(5, 70)})                           # mid 0 → 끝
        frame(3200, static)                                       # 마지막 = 바닥
        eq('ⓑ 프레임 차례', [ms for ms, _ in frames(d)], [40, 200, 900, 1000, 3200])
        m = measure(d)
        rows = {r['ms']: r for r in m['frames']}
        eq('ⓒ 정적 바닥은 자리로 뺀다(마지막 프레임 동적 0)', rows[3200]['total'], 0)
        eq('ⓓ 띠 셈', (rows[200]['mid'], rows[200]['bot'], rows[900]['top'], rows[900]['mid'], rows[40]['bot']), (3, 1, 1, 10, 2))
        eq('ⓔ 봉우리·끝', derive(m['frames']), (900, 10, 1000))
        tp = os.path.join(d, 'tbl.json')
        lines = []
        eq('ⓕ --write → rc 0', run(d, tp, write=True, out=lines.append), 0)
        eq('ⓖ 쓴 표는 그대로 초록', run(d, tp, out=lines.append), 0)
        t = load_table(tp); t['frames'][2]['mid'] = 9
        json.dump(t, open(tp, 'w'))
        eq('ⓗ 표값이 다르면 rc 1', run(d, tp, out=lines.append), 1)
        eq('ⓗ 문구', any('900ms mid' in l for l in lines), True)
        t['frames'][2]['mid'] = 10; t['peak_ms'] = 200; json.dump(t, open(tp, 'w'))
        eq('ⓘ 봉우리 시각이 다르면 rc 1', run(d, tp, out=lines.append), 1)
        eq('ⓙ 표 없음 → rc 2', run(d, os.path.join(d, 'no.json'), out=lines.append), 2)
        eq('ⓚ 프레임 없음 → rc 2', run(os.path.join(d, 'nope'), tp, out=lines.append), 2)
        lines = []
        json.dump(build_table(m), open(tp, 'w'))
        run(d, tp, list_all=True, out=lines.append)
        eq('ⓛ --list 는 시각 차례로 띠 수를 준다', [l.split()[0] for l in lines if l.startswith('  ') and l.split()[0].isdigit()][:3], ['40', '200', '900'])
    n = 14
    if fails:
        print('✗ check_coinsell_curve --self-test 실패 %d' % len(fails))
        for f in fails: print('  · ' + f)
        return 1
    print('✓ check_coinsell_curve --self-test %d칸 통과' % n)
    return 0


def main(argv):
    shots, table, write, list_all = SHOTS_DEFAULT, TABLE_DEFAULT, False, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--self-test': return self_test()
        if a == '--write': write = True
        elif a == '--list': list_all = True
        elif a == '--shots' and i + 1 < len(argv): shots = argv[i + 1]; i += 1
        elif a == '--table' and i + 1 < len(argv): table = argv[i + 1]; i += 1
        else:
            print('사용: check_coinsell_curve.py [--shots <dir>] [--table <json>] [--write] [--list] [--self-test]'); return 2
        i += 1
    return run(shots, table, write, list_all)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
