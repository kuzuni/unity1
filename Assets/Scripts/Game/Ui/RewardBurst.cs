using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core;
using Forge.Core.Data;
using Forge.Core.Ui;
using Forge.Game.Audio;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 공통 수령 연출(정본 `ui.js rewardBurst(rewards, opts)` · T134): 보상을 받으면 누른 자리에서 재화 아이콘이 개수만큼 터져(2~7 · 로그 눈금)
    /// 상단바의 그 재화 pill 로 날아가 흡수된다. 같이 도는 것: 임팩트 글로우·링(`rw-glow`·`rw-ring`) · 누른 표면 박동(`rw-pulse`) ·
    /// 착지 잔불꽃·누적 카운터(`rw-pop-sm`·`rw-tick`) · 마지막 아이콘의 마침표 별 + pill 박동(`rw-pop`·`rw-pulse`/밴드는 `rw-pulse-band`) ·
    /// «+획득량» 라벨(`rw-amt`) · 도착 pill 이 시트·팝업에 가려져 있으면 덮은 카드 상단으로 승격 + 고정 앵커 배지(`rw-anchor`).
    /// 층은 정본 `#reward-burst`(z 70 = 팝업 **위** · 수령 확인 연출이라 의도된 역전). 수치는 전부 `Resources/RewardBurstUi.json` · 셈은 Core <see cref="RewardBurstRules"/>.
    /// 클럭은 정본(CSS/WAAPI)처럼 벽시계. 호출 여덟 자리(던전 소탕·퀘스트 개별/일괄·던전 클리어·상점 무료칸·패스·오프라인)는 각 파일 lock 뒤 2회차.
    /// </summary>
    public sealed class RewardBurst : MonoBehaviour
    {
        public static RewardBurst Instance { get; private set; }
        /// <summary>층(정본 `#reward-burst` · 앱 상자 전체 · 팝업 위).</summary>
        public RectTransform Layer { get { return (RectTransform)transform; } }
        /// <summary>지금 살아 있는 것들의 수(테스트) — 날고 있는 아이콘 · 앵커 배지 · 누적 카운터 · «+획득량» 라벨 · 임팩트(글로우+링).</summary>
        public int Flying { get; private set; }
        public int Anchors { get; private set; }
        public int Ticks { get; private set; }
        public int Amts { get; private set; }
        public int Impacts { get; private set; }
        /// <summary>마지막 호출의 결과 — 재화별 아이콘 수 · 도착점 · 총 수명(ms).</summary>
        public readonly List<RewardEntry> LastEntries = new List<RewardEntry>();
        public readonly List<int> LastCounts = new List<int>();
        public readonly List<RewardTarget> LastTargets = new List<RewardTarget>();
        public readonly List<string> LastTickLabels = new List<string>();
        public double LastTotalMs { get; private set; }
        public int PlayCount { get; private set; }

        static RewardBurstSpec spec;
        static System.Random rng = new System.Random();
        static Sprite ringSprite, starSprite;

        public static RewardBurstSpec Spec { get { if (spec == null) spec = RewardBurstSpec.From(RewardBurstStyle.Root); return spec; } }

        /// <summary>층을 세운다(없으면) — `UiRoot.App` 의 마지막 형제(모달·모달-over 위 · 정본 z 70).</summary>
        public static RewardBurst Ensure()
        {
            if (Instance != null) return Instance;
            UiRoot root = UiRoot.Instance;
            if (root == null || root.App == null) return null;
            RectTransform rt = UiKit.Box(root.App, RewardBurstStyle.T("layer"));
            UiKit.Fill(rt);
            rt.SetAsLastSibling();
            RewardBurst rb = rt.gameObject.AddComponent<RewardBurst>();
            Instance = rb;
            return rb;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>정본 `UI.rewardBurst(rewards, { from })`. 돌아오는 값 = 띄운 아이콘 총수(빈 목록이면 0).</summary>
        public static int Play(IEnumerable<KeyValuePair<string, double>> rewards, RectTransform from)
        {
            RewardBurst rb = Ensure();
            if (rb == null) return 0;
            return rb.Burst(rewards, from);
        }

        // ── T134 3회차 — 호출부 도우미: 정본 `rewardBurst({ [cur]: amt })` 의 «재화 → 양» 을 세 꼴에서 만든다(양 0 이하는 Burst 가 거른다).
        /// <summary>쌍 나열 — `Rewards("coins", 12, "hammers", 3)`.</summary>
        public static List<KeyValuePair<string, double>> Rewards(params object[] kv)
        {
            var list = new List<KeyValuePair<string, double>>();
            for (int i = 0; i + 1 < kv.Length; i += 2) list.Add(new KeyValuePair<string, double>((string)kv[i], Convert.ToDouble(kv[i + 1])));
            return list;
        }

        /// <summary>표(`OrderedMap` 등)에서 — <paramref name="except"/> 재화는 뺀다(정본 4941 상점 무료칸: `delete r.gems` — claimDeal 이 젬을 안 주니 «안 준 걸 준 것처럼» 안 보이게).</summary>
        public static List<KeyValuePair<string, double>> Rewards(IEnumerable<KeyValuePair<string, double>> m, string except)
        {
            var list = new List<KeyValuePair<string, double>>();
            if (m != null) foreach (KeyValuePair<string, double> e in m) if (except == null || e.Key != except) list.Add(e);
            return list;
        }

        /// <summary>던전 보상(정본 `grantRewards` 의 r · 지급 순서 해머 → 코인 → 티켓 → 알 → 물약).</summary>
        public static List<KeyValuePair<string, double>> Rewards(Forge.Core.Dungeon.DungeonRewards r)
        {
            if (r == null) return new List<KeyValuePair<string, double>>();
            return Rewards("hammers", r.Hammers, "coins", r.Coins, "tickets", r.Tickets, "eggCurrency", r.EggCurrency, "potions", r.Potions);
        }

        /// <summary>도착 pill(코인·젬은 `pill-coin`·`pill-gem`) — 없으면 상단바 — 그마저 없으면 null(정본 폴백 = 화면 위 가운데).</summary>
        public static RectTransform TargetOf(string currency, out bool isBand)
        {
            isBand = false;
            UiRoot root = UiRoot.Instance;
            if (root == null || root.HudLayer == null) return null;
            Transform bar = root.HudLayer.Find(RewardBurstStyle.T("topbar"));
            string pill;
            if (Spec.CurrencyPill.TryGet(currency, out pill) && bar != null)
            {
                Transform p = bar.Find(pill);
                if (p != null) return (RectTransform)p;
            }
            isBand = true;
            return bar as RectTransform;
        }

        /// <summary>
        /// 정본 `elementFromPoint` 판정 — 그 자리(층 좌표 · 왼쪽 위 원점)를 팝업 딤·카드 또는 열린 탭 패널이 덮고 있는가.
        /// 덮은 **카드**(팝업 Root 의 딤 아닌 자식 · 탭 패널)가 그 점을 품으면 card 로 준다(카드 상단으로 승격할 자리).
        /// </summary>
        public bool CoveredAt(double x, double y, out RectTransform card)
        {
            card = null;
            UiRoot root = UiRoot.Instance;
            if (root == null) return true;
            PopupLayer pl = PopupLayer.Instance;
            if (pl != null)
            {
                for (int i = pl.Open.Count - 1; i >= 0; i--)
                {
                    Popup p = pl.Open[i];
                    if (p == null || p.Root == null) continue;
                    for (int c = p.Root.childCount - 1; c >= 1; c--)
                    {
                        RectTransform ch = p.Root.GetChild(c) as RectTransform;
                        if (ch != null && ch.gameObject.activeInHierarchy && Contains(ch, x, y)) { card = ch; return true; }
                    }
                    if (Contains(p.Root, x, y)) return true;   // 딤이 잡혔다 — 승격 없이 가려짐
                }
            }
            if (root.TabBar != null && root.TabBar.ActiveTab != null)
            {
                RectTransform panel = root.TabBar.Panel(root.TabBar.ActiveTab);
                if (panel != null && Contains(panel, x, y)) { card = panel; return true; }
            }
            return false;
        }

        int Burst(IEnumerable<KeyValuePair<string, double>> rewards, RectTransform from)
        {
            RewardBurstSpec s = Spec;
            List<RewardEntry> entries = RewardBurstRules.Entries(rewards);
            if (entries.Count == 0) return 0;
            DimToasts(s, entries.Count);          // 정본 ui.js 3588~3593 — 떠 있던 토스트는 물러난다(T359 2회차)
            float hostW = Layer.rect.width, hostH = Layer.rect.height;
            double rem = PopupKit.Rem, cssPx = UiKit.L("anvil_fx_px");
            double sx, sy; double? srcTop;
            double fx = 0, fy = 0, fw = 0, fh = 0;
            bool hasFrom = from != null;
            if (hasFrom) RectOf(from, out fx, out fy, out fw, out fh);
            RewardBurstRules.Source(s, hostW, hostH, hasFrom, fx, fy, fw, fh, out sx, out sy, out srcTop);

            LastEntries.Clear(); LastCounts.Clear(); LastTargets.Clear(); LastTickLabels.Clear();
            PlayCount++;

            // 임팩트 프레임: 글로우 + 퍼지는 링 — 버튼 중앙보다 10px 위(흩어짐이 위쪽 편향이라 무게중심을 맞춘다)
            double iy = sy - s.ImpactUpPx * cssPx;
            StartCoroutine(GlowFx(s, sx, iy, rem));
            StartCoroutine(RingFx(s, sx, iy, rem));
            // 누른 자리 자체도 반응한다(rw-pulse 500ms)
            if (hasFrom) StartCoroutine(PulseFx(s, from, false, s.PulseMs));

            int total = 0, maxN = 0;
            for (int ci = 0; ci < entries.Count; ci++)
            {
                RewardEntry e = entries[ci];
                string icoKey = s.CurrencyIcon.Get(e.Currency, RewardBurstStyle.T("icon_fallback"));
                bool band;
                RectTransform pillRt = TargetOf(e.Currency, out band);
                RewardTarget t;
                if (pillRt == null) t = RewardBurstRules.FallbackTarget(s, hostW, cssPx);
                else
                {
                    double px, py, pw, ph;
                    RectOf(pillRt, out px, out py, out pw, out ph);
                    t = new RewardTarget { X = px + pw / 2, Y = py + ph / 2 };
                    RectTransform card;
                    bool covered = CoveredAt(t.X, t.Y, out card);
                    double cl = 0, ct = 0, cw = 0, chh = 0;
                    if (card != null) RectOf(card, out cl, out ct, out cw, out chh);
                    t = RewardBurstRules.Promote(s, t, covered, card != null, cl, ct, cl + cw, cssPx);
                }
                int n = RewardBurstRules.Count(s, e.Amount);
                double lastDelay = RewardBurstRules.LastDelayMs(s, ci, n);
                RewardIcon[] icons = RewardBurstRules.Icons(s, ci, n, cssPx, (a, b) => a + rng.NextDouble() * (b - a));
                LastEntries.Add(e); LastCounts.Add(n); LastTargets.Add(t);
                total += n; if (n > maxN) maxN = n;

                if (t.Covered) StartCoroutine(AnchorFx(s, t, icoKey, rem, ci * s.CurStepMs, RewardBurstRules.AnchorOutMs(s, lastDelay)));
                TickBox tick = new TickBox { Currency = e.Currency, IconKey = icoKey, X = t.X, Y = RewardBurstRules.TickY(s, t, cssPx) };
                for (int i = 0; i < n; i++)
                    StartCoroutine(FlyFx(s, icons[i], sx, sy, t, rem, icoKey, RewardBurstRules.Per(e.Amount, i, n), tick, cssPx));
                StartCoroutine(FinishFx(s, t, pillRt, band, tick, rem, RewardBurstRules.PopMs(s, lastDelay)));
                double lx, ly;
                RewardBurstRules.AmtPos(s, sx, sy, srcTop, ci, hostW, cssPx, out lx, out ly);
                StartCoroutine(AmtFx(s, lx, ly, rem, cssPx, e.Amount, icoKey, ci));
            }
            LastTotalMs = RewardBurstRules.TotalMs(s, entries.Count, maxN);
            Sfx.Gacha(RewardBurstStyle.T("sfx_gacha_rarity"));   // 수령 차임 — 판매 코인(common)보다 한 단 밝은 스윕
            return total;
        }

        /// <summary>같은 재화의 누적 카운터 상자(정본 `.rw-tick[data-cur]` — 첫 착지에 만들고 착지마다 갱신).</summary>
        sealed class TickBox
        {
            public string Currency, IconKey;
            public double X, Y;
            public RectTransform Row;
            public TextMeshProUGUI Text;
            public Image Icon;
            public double BumpStartMs = -1;
            public bool Out;
        }

        // ---- 좌표 (층 좌표 · 왼쪽 위 원점 · 기준 캔버스 px) ----

        // ── T359 2회차 — 수령 연출 동안 토스트가 물러난다 ────────────────────────────────────────────────
        // 정본: `ui.js` 3588~3593 이 `#toasts` 에 `rw-dim` 을 걸고 «연출이 끝나는 시각»(`_toastHoldUntil`)에 뗀다.
        // CSS 는 `#toasts { transition: opacity .25s ease-out }` · `#toasts.rw-dim { opacity: .1 }` — 두 값 다 표(OpacityUi)에 있다.
        // 왜 이 자리인가: «+획득량» 라벨(`rw-amt`)이 토스트 글자 위에 겹쳐 인쇄되던 충돌을 정본이 이렇게 지웠다(정본 주석).
        // 그릇(`#toasts` 레인)은 `Popups.cs` 가 세우는데 그 파일이 남의 산 lock 이라, **상자 이름을 표에 두고 여기서 찾는다**
        // — 이름이 바뀌면 PlayMode 자가 먼저 깨진다(조용히 안 사라진다).
        Coroutine toastDim;

        void DimToasts(RewardBurstSpec s, int entries)
        {
            if (UiRoot.Instance == null || UiRoot.Instance.App == null) return;
            Transform lane = UiRoot.Instance.App.Find(OpacityUi.Text("toasts_rw_dim", "box"));
            if (lane == null) return;
            CanvasGroup cg = lane.GetComponent<CanvasGroup>();
            if (cg == null) cg = lane.gameObject.AddComponent<CanvasGroup>();
            if (toastDim != null) StopCoroutine(toastDim);
            toastDim = StartCoroutine(ToastDim(cg, RewardBurstRules.HoldMs(s, entries)));
        }

        IEnumerator ToastDim(CanvasGroup cg, double holdMs)
        {
            float dim = OpacityUi.A("toasts_rw_dim");
            float fade = (float)OpacityUi.Num("toasts_rw_dim", "fade_ms") / 1000f;
            Forge.Core.CraftFx.CssEase ease = OpacityUi.Ease("toasts_rw_dim");
            yield return Fade(cg, 1f, dim, fade, ease);
            float rest = (float)(holdMs / 1000.0) - fade;           // 물러나 있는 시간(연출이 끝나는 시각까지)
            for (float t = 0f; t < rest; t += Time.unscaledDeltaTime) yield return null;
            yield return Fade(cg, dim, 1f, fade, ease);             // 정본은 class 를 떼면 같은 transition 으로 돌아온다
            cg.alpha = 1f;
            toastDim = null;
        }

        static IEnumerator Fade(CanvasGroup cg, float from, float to, float sec, Forge.Core.CraftFx.CssEase ease)
        {
            for (float t = 0f; t < sec; t += Time.unscaledDeltaTime)
            {
                if (cg == null) yield break;
                cg.alpha = Mathf.Lerp(from, to, (float)ease.Ease(t / sec));
                yield return null;
            }
            if (cg != null) cg.alpha = to;
        }

        void RectOf(RectTransform r, out double x, out double yTop, out double w, out double h)
        {
            Vector3[] c = new Vector3[4];
            r.GetWorldCorners(c);
            Vector3 lb = Layer.InverseTransformPoint(c[0]), rt = Layer.InverseTransformPoint(c[2]);
            float halfW = Layer.rect.width * 0.5f, halfH = Layer.rect.height * 0.5f;
            x = lb.x + halfW; w = rt.x - lb.x; h = rt.y - lb.y; yTop = halfH - rt.y;
        }

        bool Contains(RectTransform r, double x, double y)
        {
            double rx, ry, rw, rh;
            RectOf(r, out rx, out ry, out rw, out rh);
            return x >= rx && x <= rx + rw && y >= ry && y <= ry + rh;
        }

        /// <summary>가운데가 (cx, cy) 인 w×h 상자 — 피벗 가운데(CSS transform-origin 기본) 라 scale·rotate 가 중심을 돈다. `UiKit.Box` 의 늘림 앵커를 푼다(안 풀면 rect 폭 = 부모 폭 + w).</summary>
        static void Center(RectTransform rt, double cx, double cy, double w, double h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2((float)w, (float)h);
            rt.anchoredPosition = new Vector2((float)cx, -(float)cy);
        }

        /// <summary>정본 `translate(-50%, 0)` 자리 — 가로 가운데 cx · 위 yTop (피벗은 가운데).</summary>
        static void PlaceTop(RectTransform rt, double cx, double yTop, double w, double h)
        {
            Center(rt, cx, yTop + h * 0.5, w, h);
        }

        static void Alpha(Graphic g, double a) { Color c = g.color; c.a = (float)Math.Max(0, Math.Min(1, a)); g.color = c; }

        /// <summary>벽시계 진행 — delay 동안 감췄다가(WAAPI fill:both 는 첫 키 값이지만 정본 요소는 지연 중 보이지 않는 값이다) dur 동안 0~100% · 그 뒤 마지막 값으로 붙박이(forwards) · endMs 에 걷는다.</summary>
        IEnumerator Run(double delayMs, double durMs, double endMs, GameObject go, Action<double> step)
        {
            double ms = 0;
            if (delayMs > 0) go.SetActive(false);
            while (ms < endMs)
            {
                if (ms >= delayMs)
                {
                    if (!go.activeSelf) go.SetActive(true);
                    double pct = durMs <= 0 ? 100 : Math.Min(100.0, (ms - delayMs) / durMs * 100.0);
                    step(pct);
                }
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
        }

        // ---- 임팩트 ----

        IEnumerator GlowFx(RewardBurstSpec s, double cx, double cy, double rem)
        {
            Impacts++;
            // T178 14회차 — 정본 7460 `.rw-glow { background: radial-gradient(circle, #ffae14 0, rgba(255,150,10,.9) 30%, rgba(255,140,0,.4) 56%, transparent 72%) }`:
            // 단색 원 한 장이 아니라 **가운데가 진하고 72% 밖은 투명한 방사형 판**이다(SurfaceUi.json `rw_glow` · 정사각이라 비율 1). 박동(scale·알파)은 그대로 위에 탄다.
            RectTransform gb = UiKit.Box(Layer, "rw-glow");
            Image g = gb.gameObject.AddComponent<Image>();
            g.sprite = SurfaceArt.Bake("rw_glow", 1f);
            g.color = Color.white;
            g.raycastTarget = false;
            double size = s.GlowRem * rem;
            Center(gb, cx, cy, size, size);
            yield return Run(0, s.GlowAnimMs, s.ImpactMs, g.gameObject, pct =>
            {
                double sc, a; RewardBurstRules.Glow(s, pct, out sc, out a);
                g.rectTransform.localScale = new Vector3((float)sc, (float)sc, 1f);
                Alpha(g, a);
            });
            Destroy(g.gameObject);
            Impacts--;
        }

        IEnumerator RingFx(RewardBurstSpec s, double cx, double cy, double rem)
        {
            Impacts++;
            RectTransform box = UiKit.Box(Layer, "rw-ring");
            Image r = box.gameObject.AddComponent<Image>();
            r.sprite = RingSprite(s);
            r.color = RewardBurstStyle.C("ring");
            r.raycastTarget = false;
            double size = s.RingRem * rem;
            Center(box, cx, cy, size, size);
            yield return Run(0, s.RingAnimMs, s.ImpactMs, box.gameObject, pct =>
            {
                double sc, a, bw; RewardBurstRules.Ring(s, pct, out sc, out a, out bw);
                box.localScale = new Vector3((float)sc, (float)sc, 1f);
                Alpha(r, a);
            });
            Destroy(box.gameObject);
            Impacts--;
        }

        /// <summary>정본 `.rw-pulse`(scale 1→1.26 + 밝기 1.85) / `.rw-pulse-band`(밴드는 밝기만 — 앱 폭 전체라 커지면 잘린다). 460ms 뒤 원값.</summary>
        IEnumerator PulseFx(RewardBurstSpec s, RectTransform el, bool band, double removeMs)
        {
            if (el == null) yield break;
            Vector3 scale0 = el.localScale;
            Image face = el.GetComponent<Image>();
            if (face == null) { Transform f = el.Find("bg") ?? el.Find("face"); if (f != null) face = f.GetComponent<Image>(); }
            Color col0 = face != null ? face.color : Color.white;
            Color white = RewardBurstStyle.C("pulse_bright");
            double ms = 0;
            while (ms < removeMs && el != null)
            {
                double pct = Math.Min(100.0, ms / s.PulseAnimMs * 100.0);
                double sc, br; RewardBurstRules.Pulse(s, pct, band, out sc, out br);
                el.localScale = new Vector3(scale0.x * (float)sc, scale0.y * (float)sc, scale0.z);
                if (face != null) { Color c = Color.Lerp(col0, white, (float)(br * s.PulseBrightF)); c.a = col0.a; face.color = c; }
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
            if (el != null) el.localScale = scale0;
            if (face != null) face.color = col0;
        }

        // ---- 날아가는 아이콘 · 착지 ----

        IEnumerator FlyFx(RewardBurstSpec s, RewardIcon p, double sx, double sy, RewardTarget t, double rem, string icoKey, double per, TickBox tick, double cssPx)
        {
            Flying++;
            double size = s.FlyIcoRem * rem;
            RectTransform fly = UiKit.Box(Layer, "rw-fly");
            Image img = UiKit.Icon(fly, "ico", icoKey);
            img.raycastTarget = false;
            UiKit.Fill(img.rectTransform);
            Center(fly, sx, sy, size, size);
            double tx = t.X - sx, ty = t.Y - sy;
            bool landed = false;
            double landAt = RewardBurstRules.LandMs(s, p), end = RewardBurstRules.IconEndMs(s, p);
            double ms = 0;
            fly.gameObject.SetActive(false);
            while (ms < end)
            {
                if (ms >= p.DelayMs)
                {
                    if (!fly.gameObject.activeSelf) fly.gameObject.SetActive(true);
                    double pct = Math.Min(100.0, (ms - p.DelayMs) / s.FlyMs * 100.0);
                    double x = RewardBurstRules.FlyX(s, pct, p.Rx, tx);
                    double y, rot, sc, a;
                    RewardBurstRules.FlyY(s, pct, p.Ry, ty, p.RotDeg, out y, out rot, out sc, out a);
                    Center(fly, sx + x, sy + y, size, size);
                    fly.localRotation = Quaternion.Euler(0f, 0f, -(float)rot);   // CSS rotate(+) = 시계 방향
                    fly.localScale = new Vector3((float)sc, (float)sc, 1f);
                    Alpha(img, a);
                    if (!landed && ms >= landAt) { landed = true; Land(s, t, tick, rem, per, cssPx); }
                }
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
            if (!landed) Land(s, t, tick, rem, per, cssPx);
            Destroy(fly.gameObject);
            Flying--;
        }

        /// <summary>착지 박자 — 잔불꽃(작은 별) + 누적 카운터 «+per» 갱신·튐.</summary>
        void Land(RewardBurstSpec s, RewardTarget t, TickBox tick, double rem, double per, double cssPx)
        {
            double jx = (rng.NextDouble() * 2 - 1) * s.PopJitterXPx * cssPx, jy = (rng.NextDouble() * 2 - 1) * s.PopJitterYPx * cssPx;
            StartCoroutine(PopFx(s, t.X + jx, t.Y + jy, s.PopSmRem * rem, s.PopSmAnimMs, s.PopSmMs, "rw-pop-sm"));
            string label = RewardBurstStyle.T("plus", NumFmt.Fmt(per));
            if (tick.Row == null)
            {
                Ticks++;
                tick.Row = LabelRow(Layer, "rw-tick", label, tick.IconKey, s.TickFontRem * rem, s.TickIcoRem * rem, s.TickGapRem * rem, s.TickStrokePx, "tick", out tick.Text, out tick.Icon);
                StartCoroutine(TickFx(s, tick, rem));
            }
            else SetLabel(tick.Row, tick.Text, tick.Icon, label, s.TickGapRem * rem, s.TickIcoRem * rem);
            LastTickLabels.Add(label);
            tick.BumpStartMs = 0;
        }

        IEnumerator TickFx(RewardBurstSpec s, TickBox tick, double rem)
        {
            double outStart = -1, bump = -1;
            while (true)
            {
                if (tick.BumpStartMs == 0) { bump = 0; tick.BumpStartMs = 1; }
                double sc = 1, ty = 0;
                if (bump >= 0)
                {
                    double pct = Math.Min(100.0, bump / s.TickBumpMs * 100.0);
                    RewardBurstRules.TickBump(s, pct, out sc, out ty);
                    if (pct >= 100) bump = -1; else bump += Time.unscaledDeltaTime * 1000.0;
                }
                float w = tick.Row.sizeDelta.x, h = tick.Row.sizeDelta.y;
                PlaceTop(tick.Row, tick.X, tick.Y + ty * rem, w, h);
                tick.Row.localScale = new Vector3((float)sc, (float)sc, 1f);
                if (tick.Out)
                {
                    if (outStart < 0) outStart = 0; else outStart += Time.unscaledDeltaTime * 1000.0;
                    double a = 1 - Math.Min(1, outStart / s.TickOutMs);
                    Alpha(tick.Text, a); if (tick.Icon != null) Alpha(tick.Icon, a);
                    if (outStart >= s.TickOutMs) break;
                }
                yield return null;
            }
            Destroy(tick.Row.gameObject);
            Ticks--;
        }

        /// <summary>도착 마침표 — 그 재화의 **마지막** 아이콘이 들어오는 순간 큰 별 + 카운터 정리 + pill/밴드 박동.</summary>
        IEnumerator FinishFx(RewardBurstSpec s, RewardTarget t, RectTransform pill, bool band, TickBox tick, double rem, double atMs)
        {
            double ms = 0;
            while (ms < atMs) { yield return null; ms += Time.unscaledDeltaTime * 1000.0; }
            StartCoroutine(PopFx(s, t.X, t.Y, s.PopRem * rem, s.PopAnimMs, s.PopMs, "rw-pop"));
            tick.Out = true;
            if (pill != null) StartCoroutine(PulseFx(s, pill, band, s.PulseMs));
        }

        IEnumerator PopFx(RewardBurstSpec s, double cx, double cy, double size, double animMs, double removeMs, string name)
        {
            RectTransform box = UiKit.Box(Layer, name);
            Image st = box.gameObject.AddComponent<Image>();
            st.sprite = StarSprite();
            st.color = RewardBurstStyle.C("pop");
            st.raycastTarget = false;
            Center(box, cx, cy, size, size);
            yield return Run(0, animMs, removeMs, box.gameObject, pct =>
            {
                double sc, rot, a; RewardBurstRules.Pop(s, pct, out sc, out rot, out a);
                box.localScale = new Vector3((float)sc, (float)sc, 1f);
                box.localRotation = Quaternion.Euler(0f, 0f, -(float)rot);
                Alpha(st, a);
            });
            Destroy(box.gameObject);
        }

        // ---- 고정 앵커 배지 (가려진 도착점) ----

        IEnumerator AnchorFx(RewardBurstSpec s, RewardTarget t, string icoKey, double rem, double delayMs, double outAtMs)
        {
            Anchors++;
            double size = s.AnchorRem * rem, border = s.AnchorBorderRem * rem, ico = s.AnchorIcoRem * rem;
            RectTransform an = UiKit.Box(Layer, "rw-anchor");
            Center(an, t.X, t.Y, size, size);
            Image ring = UiKit.Circle(an, "border", "coin"); ring.color = RewardBurstStyle.C("anchor_border"); ring.raycastTarget = false; UiKit.Fill(ring.rectTransform);
            Image bg = UiKit.Circle(an, "bg", "coin"); bg.color = RewardBurstStyle.C("anchor_bg"); bg.raycastTarget = false;
            UiKit.Fill(bg.rectTransform); bg.rectTransform.offsetMin = new Vector2((float)border, (float)border); bg.rectTransform.offsetMax = new Vector2(-(float)border, -(float)border);
            Image img = UiKit.Icon(an, "ico", icoKey); img.raycastTarget = false;
            Center(img.rectTransform, size * 0.5, size * 0.5, ico, ico);
            CanvasGroup cg = an.gameObject.AddComponent<CanvasGroup>();
            double endMs = outAtMs + s.AnchorOutMs;
            yield return Run(delayMs, s.AnchorInMs, endMs, an.gameObject, pct =>
            {
                double sc, a; RewardBurstRules.AnchorIn(s, pct, out sc, out a);
                an.localScale = new Vector3((float)sc, (float)sc, 1f);
                cg.alpha = (float)a;
            });
            Destroy(an.gameObject);
            Anchors--;
        }

        // ---- «+획득량» 라벨 ----

        IEnumerator AmtFx(RewardBurstSpec s, double lx, double ly, double rem, double cssPx, double amount, string icoKey, int ci)
        {
            Amts++;
            TextMeshProUGUI txt; Image ico;
            string label = RewardBurstStyle.T("plus", NumFmt.Fmt(amount));
            RectTransform row = LabelRow(Layer, "rw-amt", label, icoKey, s.AmtFontRem * rem, s.AmtIcoRem * rem, s.AmtGapRem * rem, s.AmtStrokePx, "amt", out txt, out ico);
            float w = row.sizeDelta.x, h = row.sizeDelta.y;
            PlaceTop(row, lx, ly, w, h);
            double delay = RewardBurstRules.AmtDelayMs(s, ci), end = RewardBurstRules.AmtEndMs(s, ci);
            yield return Run(delay, s.AmtMs, end, row.gameObject, pct =>
            {
                double a, ty, sc; RewardBurstRules.Amt(s, pct, out a, out ty, out sc);
                PlaceTop(row, lx, ly + ty * rem, w, h);   // translate(-50%, ty)
                row.localScale = new Vector3((float)sc, (float)sc, 1f);
                Alpha(txt, a); if (ico != null) Alpha(ico, a);
            });
            Destroy(row.gameObject);
            Amts--;
        }

        /// <summary>«+N [아이콘]» 한 줄 — 금색 굵은 글자 + 두꺼운 어두운 키라인(정본 `-webkit-text-stroke` · CSS px → 캔버스 ×css_px) + 재화 아이콘.</summary>
        RectTransform LabelRow(Transform parent, string name, string label, string icoKey, double fontPx, double icoPx, double gapPx, double strokeCssPx, string colorKey, out TextMeshProUGUI txt, out Image ico)
        {
            RectTransform row = UiKit.Box(parent, name);
            Center(row, 0, 0, 1, 1);   // 늘림 앵커를 먼저 푼다 — SetLabel 의 sizeDelta 가 곧 rect 크기가 되게
            txt = UiKit.Text(row, "text", TextKind.Sub, label);
            txt.fontSize = (float)fontPx;
            txt.fontStyle = FontStyles.Bold;
            txt.color = RewardBurstStyle.C(colorKey);
            txt.raycastTarget = false;
            WrapUi.Apply(txt, name == "rw-tick" ? "rw_tick" : "rw_amt");   // T361 2회차 — 정본 white-space 표(WrapUi.json) 7495 `.rw-amt` · 7553 `.rw-tick` 둘 다 nowrap
            txt.alignment = TextAlignmentOptions.Left;
            UiKit.OutlinePx(txt, "pp_line", (float)(strokeCssPx * KeylineUi.CssPx));
            txt.outlineColor = RewardBurstStyle.C("stroke");
            ico = UiKit.Icon(row, "ico", icoKey);
            ico.raycastTarget = false;
            SetLabel(row, txt, ico, label, gapPx, icoPx);
            return row;
        }

        void SetLabel(RectTransform row, TextMeshProUGUI txt, Image ico, string label, double gapPx, double icoPx)
        {
            txt.text = label;
            Vector2 pref = txt.GetPreferredValues();   // 지금 문구의 선호 크기(인자 있는 오버로드는 스텁에 없다)
            float tw = pref.x, h = Mathf.Max(pref.y, (float)icoPx);
            float w = tw + (float)gapPx + (float)icoPx;
            row.sizeDelta = new Vector2(w, h);
            UiKit.Place(txt.rectTransform, 0f, (h - pref.y) * 0.5f, tw, pref.y);
            UiKit.Place(ico.rectTransform, tw + (float)gapPx, (h - (float)icoPx) * 0.5f, (float)icoPx, (float)icoPx);
        }

        // ---- 절차적 스프라이트 (정본 CSS 도형: border 링 · clip-path 8각 별) ----

        const int SpriteRes = 96;

        static Sprite RingSprite(RewardBurstSpec s)
        {
            if (ringSprite != null) return ringSprite;
            double inner = 1.0 - (s.RingBorderRem / (s.RingRem * 0.5));   // 반지름 대비 테 두께(.34rem / 1.5rem)
            Texture2D tex = new Texture2D(SpriteRes, SpriteRes, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[SpriteRes * SpriteRes];
            double c = (SpriteRes - 1) * 0.5, R = c;
            for (int y = 0; y < SpriteRes; y++)
                for (int x = 0; x < SpriteRes; x++)
                {
                    double d = Math.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / R;
                    double a = d <= 1 && d >= inner ? 1 : 0;
                    if (d > 1) a = Math.Max(0, 1 - (d - 1) * R);
                    else if (d < inner) a = Math.Max(0, 1 - (inner - d) * R);
                    px[y * SpriteRes + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply(false);
            ringSprite = Sprite.Create(tex, new Rect(0, 0, SpriteRes, SpriteRes), new Vector2(0.5f, 0.5f), 100f);
            return ringSprite;
        }

        /// <summary>정본 `.rw-pop` 의 `clip-path: polygon(...)` — 8각 별(10 꼭짓점 · % 좌표 · y 는 아래가 +).</summary>
        static readonly double[,] StarPoly = { { 50, 0 }, { 61, 35 }, { 98, 35 }, { 68, 57 }, { 79, 91 }, { 50, 70 }, { 21, 91 }, { 32, 57 }, { 2, 35 }, { 39, 35 } };

        static Sprite StarSprite()
        {
            if (starSprite != null) return starSprite;
            Texture2D tex = new Texture2D(SpriteRes, SpriteRes, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[SpriteRes * SpriteRes];
            int n = StarPoly.GetLength(0);
            for (int y = 0; y < SpriteRes; y++)
                for (int x = 0; x < SpriteRes; x++)
                {
                    double fx = (x + 0.5) / SpriteRes * 100.0, fy = 100.0 - (y + 0.5) / SpriteRes * 100.0;
                    bool inside = false;
                    for (int i = 0, j = n - 1; i < n; j = i++)
                    {
                        double xi = StarPoly[i, 0], yi = StarPoly[i, 1], xj = StarPoly[j, 0], yj = StarPoly[j, 1];
                        if ((yi > fy) != (yj > fy) && fx < (xj - xi) * (fy - yi) / (yj - yi) + xi) inside = !inside;
                    }
                    px[y * SpriteRes + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
                }
            tex.SetPixels32(px); tex.Apply(false);
            starSprite = Sprite.Create(tex, new Rect(0, 0, SpriteRes, SpriteRes), new Vector2(0.5f, 0.5f), 100f);
            return starSprite;
        }
    }

    /// <summary>`Resources/RewardBurstUi.json` — 색·문구(수치는 <see cref="RewardBurstSpec"/> 가 같은 표에서 읽는다).</summary>
    public static class RewardBurstStyle
    {
        public const string ResourcePath = "RewardBurstUi";
        static JsonObject root, colors, text;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        public static JsonObject Root { get { Load(); return root; } }

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T134)");
            root = MiniJson.ParseObject(ta.text);
            colors = J.Obj(root["colors"]);
            text = J.Obj(root["text"]);
        }

        public static void Reset() { root = null; colorCache.Clear(); }

        public static Color C(string key)
        {
            Load();
            Color c;
            if (colorCache.TryGetValue(key, out c)) return c;
            string hex = J.Str(colors[key]);
            if (hex == null) throw new KeyNotFoundException("RewardBurstUi.json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            colorCache[key] = c;
            return c;
        }

        public static string T(string key)
        {
            Load();
            string s = J.Str(text[key]);
            if (s == null) throw new KeyNotFoundException("RewardBurstUi.json 에 문구 «" + key + "» 이 없다");
            return s;
        }
        public static string T(string key, params object[] args) { return string.Format(T(key), args); }
    }
}
