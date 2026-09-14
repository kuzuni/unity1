using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;
using Forge.Game.Audio;

namespace Forge.Game.Ui
{
    /// <summary>정본 `grabEquipSwapFx(slot)` 이 돌려주는 것 — Core 셈 입력(<see cref="G"/>) + 붙잡은 칸(복제·빈 소켓용).</summary>
    public sealed class EquipSwapGrabbed
    {
        public string Slot;
        public RectTransform Cell;
        public EquipSwapGrab G;
        /// <summary>붙잡는 순간 떠 둔 옛 타일의 복제(정본 `inner: cell.innerHTML`) — 렌더가 칸을 갈아끼운 뒤에 날려도 **옛 장비** 모습이다. Play 가 가져간다.</summary>
        public RectTransform Ghost;
    }

    /// <summary>
    /// 장비 교체 «던져내기»(정본 `ui.js` `EQSW_*`·`grabEquipSwapFx`·`playEquipSwapFx` · css `#equip-swap-fx`·`.eqsw-*` · T118): 세 박자 —
    /// ⑴ 옛 장비 타일의 **복제**가 칸에서 튕겨 나와 바깥쪽으로 정수 바퀴 돌며 시트 바닥에 눕고 먼지를 낸다(`equipToss` → 착지 `equipDrop`) ⑵ 그동안 칸은 빈 소켓(내용물 감춤 · 프레임 어둡게)
    /// ⑶ 새 장비 복제가 위에서 떨어져 오버슈트로 딸깍 앉는다(`equipSnap`). 층은 정본 `#equip-swap-fx`(z 21 = 비교 팝업 위 · 세부정보 팝업 아래) → 앱 상자 안 `modals` 바로 다음 형제 · `overflow:hidden` = RectMask2D.
    /// 수치는 전부 `Resources/EquipSwapUi.json`(<see cref="EquipSwapStyle"/>) · 셈은 Core <see cref="EquipSwapRules"/> · 클럭은 정본(CSS 애니메이션)처럼 벽시계.
    /// 호출(정본 `ui.js` 3899·3904 — 장착 **전에** `Grab` · 렌더 **뒤에** `Play`)은 `ForgeHost.DoResolveCraft` 가 T87 lock 뒤에 잇는다(1회차는 연출·표·자만 · 결정 263).
    /// 정본의 `prefers-reduced-motion` 가드는 유니티에 그 OS 신호가 없어 두지 않는다(결정 263).
    /// </summary>
    public sealed class EquipSwapFx : MonoBehaviour
    {
        public static EquipSwapFx Instance { get; private set; }
        public RectTransform Layer { get { return (RectTransform)transform; } }
        /// <summary>지금 날고 있는 타일 · 깔린 먼지 · 앉는 중인 딸깍 수(테스트).</summary>
        public int Flying { get; private set; }
        public int Dusts { get; private set; }
        public int Snaps { get; private set; }
        public int PlayCount { get; private set; }
        public EquipSwapPlan LastPlan { get; private set; }
        public EquipSwapGrab LastGrab { get; private set; }
        /// <summary>마지막 연출이 울린 소리 이름(순서대로 · 정본 sfx.js 353: 던짐 0ms → 딸깍 130ms → 착지 522ms).</summary>
        public readonly List<string> LastSounds = new List<string>();

        static EquipSwapSpec spec;
        static System.Random rng = new System.Random();
        public static EquipSwapSpec Spec { get { if (spec == null) spec = EquipSwapSpec.From(EquipSwapStyle.Root); return spec; } }

        sealed class Hollow
        {
            public RectTransform Cell;
            public readonly List<Graphic> Hidden = new List<Graphic>();
            public Image Frame; public Color FrameColor;
        }

        /// <summary>층을 세운다(없으면) — 앱 상자 안 `modals` 바로 다음 형제(정본 z 21).</summary>
        public static EquipSwapFx Ensure()
        {
            if (Instance != null) return Instance;
            UiRoot root = UiRoot.Instance;
            if (root == null || root.App == null) return null;
            RectTransform rt = UiKit.Box(root.App, EquipSwapStyle.T("layer"));
            Transform modals = root.App.Find("modals");
            if (modals != null) rt.SetSiblingIndex(modals.GetSiblingIndex() + 1);
            rt.gameObject.AddComponent<RectMask2D>();      // 정본 overflow:hidden — 화면 밖으로 떨어지는 타일을 층이 잘라 준다
            EquipSwapFx fx = rt.gameObject.AddComponent<EquipSwapFx>();
            Instance = fx;
            return fx;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>장비 시트의 칸(`equip-grid/cell-<slot>`) — 없거나 접혀 있으면 null.</summary>
        public static RectTransform CellOf(string slot)
        {
            UiRoot root = UiRoot.Instance;
            if (root == null || root.Sheet == null) return null;
            Transform t = root.Sheet.Find(EquipSwapStyle.T("grid") + "/" + EquipSwapStyle.T("cell_prefix") + slot);
            return t != null ? t as RectTransform : null;
        }

        // 층 좌표(왼쪽 위 원점 · 기준 px)로: 월드 코너 → 층 로컬 → 반폭·반높이 더하기
        void Rect(RectTransform rt, out double l, out double t, out double r, out double b)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector3 lo = Layer.InverseTransformPoint(c[0]), hi = Layer.InverseTransformPoint(c[2]);
            float halfW = Layer.rect.width * 0.5f, halfH = Layer.rect.height * 0.5f;
            l = lo.x + halfW; r = hi.x + halfW; t = halfH - hi.y; b = halfH - lo.y;
        }

        /// <summary>정본 `grabEquipSwapFx(slot)` — 렌더가 칸을 갈아끼우기 **전에** 부른다. 칸이 없거나(다른 탭·시트 접힘) 크기가 0 이면 null(호출부는 조용히 생략).</summary>
        public static EquipSwapGrabbed Grab(string slot)
        {
            EquipSwapFx fx = Ensure();
            if (fx == null) return null;
            RectTransform cell = CellOf(slot);
            UiRoot root = UiRoot.Instance;
            if (cell == null || !cell.gameObject.activeInHierarchy || root.Sheet == null) return null;
            double l, t, r, b; fx.Rect(cell, out l, out t, out r, out b);
            if (r - l <= 0 || b - t <= 0) return null;
            double sl, st, sr, sb; fx.Rect(root.Sheet, out sl, out st, out sr, out sb);
            var g = new EquipSwapGrab
            {
                Cx = (l + r) / 2, Cy = (t + b) / 2, W = r - l, H = b - t,
                HostW = fx.Layer.rect.width, HostH = fx.Layer.rect.height,
                FloorY = sb,
            };
            // 열려 있는 팝업 카드(정본 `.modal:not(.hidden) .modal-card`)의 가로 범위 — 던져 낸 타일이 그 위에 누워 쉬지 않게 피할 자리
            PopupLayer pl = PopupLayer.Instance;
            if (pl != null)
                for (int i = pl.Open.Count - 1; i >= 0; i--)
                {
                    Popup p = pl.Open[i];
                    if (p == null || p.Root == null) continue;
                    Transform card = p.Root.Find(EquipSwapStyle.T("card"));
                    if (card == null || !card.gameObject.activeInHierarchy) continue;
                    double cl, ct, cr, cb; fx.Rect((RectTransform)card, out cl, out ct, out cr, out cb);
                    if (cr - cl > 0) { g.HasBlock = true; g.BlockL = cl; g.BlockR = cr; break; }
                }
            // T118 2회차 — 옛 타일의 «모습» 도 지금 붙잡아 둔다(정본은 innerHTML 을 담아 간다): 호출부가 그 뒤 칸을 갈아끼우면
            // Play 시점의 칸은 새 장비라, 그때 복제하면 «새 장비가 날아가는» 그림이 된다. 층에 꺼진 채 두었다가 Play 가 켠다.
            RectTransform ghost = fx.Clone(cell, g.W, g.H, EquipSwapStyle.T("fly"));
            if (ghost != null) ghost.gameObject.SetActive(false);
            return new EquipSwapGrabbed { Slot = slot, Cell = cell, G = g, Ghost = ghost };
        }

        /// <summary>정본 `playEquipSwapFx(fx)` — 렌더 **뒤에** 부른다. null 이면 조용히 생략(false).</summary>
        public static bool Play(EquipSwapGrabbed grabbed)
        {
            if (grabbed == null) return false;
            EquipSwapFx fx = Ensure();
            if (fx == null) return false;
            fx.Throw(grabbed);
            return true;
        }

        static double Rand(double a, double b) { return a + rng.NextDouble() * (b - a); }

        void Throw(EquipSwapGrabbed gr)
        {
            EquipSwapSpec s = Spec;
            double cssPx = UiKit.L("anvil_fx_px"), rem = PopupKit.Rem;
            EquipSwapPlan p = EquipSwapRules.Plan(s, gr.G, cssPx, Rand);
            LastPlan = p; LastGrab = gr.G; PlayCount++;
            LastSounds.Clear();
            RectTransform cell = gr.Cell;
            // ⑴ 옛 장비 — 붙잡을 때 떠 둔 복제(Ghost · 옛 모습)가 날아간다 · 없으면 지금 칸을 복제
            RectTransform clone = gr.Ghost != null ? gr.Ghost : Clone(cell, gr.G.W, gr.G.H, EquipSwapStyle.T("fly"));
            gr.Ghost = null;
            if (clone != null)
            {
                clone.gameObject.SetActive(true);
                clone.SetAsLastSibling();
                // 칸에서는 안 보이던 드롭섀도(공중에 뜬 물건이라는 단서 · 정본 box-shadow 0 .35rem .6rem rgba(0,0,0,.55) — 번짐은 못 내고 판만)
                Image sh = new GameObject(EquipSwapStyle.T("shadow"), typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                sh.transform.SetParent(clone, false);
                sh.transform.SetAsFirstSibling();
                sh.color = EquipSwapStyle.C("shadow");
                sh.raycastTarget = false;
                UiKit.Fill(sh.rectTransform);
                sh.rectTransform.anchoredPosition = new Vector2(0f, -(float)(s.ShadowDyRem * rem));
                StartCoroutine(Fly(s, gr.G, p, clone, (float)rem));
            }
            Sound("equipToss", Sfx.EquipToss);
            // ⑵ 그동안 칸은 빈 소켓 — 호출부(ForgeHost)가 같은 프레임에 시트를 다시 그렸으면 붙잡은 칸은 프레임 끝에 사라지므로
            //    다음 프레임에 살아 있는 칸을 다시 집어 빈 소켓을 옮긴다(Rehollow) · 딸깍(130ms)까지 새 장비가 맨살로 보이는 틈을 없앤다
            Hollow hollow = cell != null ? MakeHollow(cell, s) : null;
            StartCoroutine(Rehollow(gr, s, hollow));
            // ⑶ 새 장비 — 딸깍
            StartCoroutine(Snap(s, gr, hollow));
        }

        void Sound(string name, Action play) { LastSounds.Add(name); play(); }

        RectTransform Clone(RectTransform cell, double w, double h, string name)
        {
            if (cell == null) return null;
            GameObject go = Instantiate(cell.gameObject, Layer);
            go.name = name;
            Button b = go.GetComponent<Button>();
            if (b != null) Destroy(b);
            foreach (Graphic g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false;
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2((float)w, (float)h);
            rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
            return rt;
        }

        void Put(RectTransform rt, double cx, double cy) { rt.anchoredPosition = new Vector2((float)cx, -(float)cy); }

        IEnumerator Fly(EquipSwapSpec s, EquipSwapGrab g, EquipSwapPlan p, RectTransform clone, float rem)
        {
            Flying++;
            CanvasGroup cg = clone.GetComponent<CanvasGroup>();
            double ms = 0, end = EquipSwapRules.FlyEndMs(s), landAt = EquipSwapRules.LandMs(s);
            bool landed = false;
            while (ms < end)
            {
                if (clone == null) break;
                double pct = Math.Min(100.0, ms / s.FlyMs * 100.0);
                double y, a; EquipSwapRules.Y(s, p, rem, pct, out y, out a);
                double x = EquipSwapRules.X(s, p, pct);
                double r = EquipSwapRules.R(s, p, pct);
                double sx, sy; EquipSwapRules.Squash(s, pct, out sx, out sy);
                Put(clone, g.Cx + x, g.Cy + y);
                clone.localRotation = Quaternion.Euler(0f, 0f, -(float)r);       // CSS rotate(+) 는 시계 방향 · 유니티 z(+) 는 반시계
                clone.localScale = new Vector3((float)sx, (float)sy, 1f);
                cg.alpha = (float)a;
                if (!landed && ms >= landAt)
                {
                    landed = true;
                    if (p.Lands) { StartCoroutine(Dust(s, g, p)); Sound("equipDrop", Sfx.EquipDrop); }
                }
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
            if (!landed && p.Lands) { StartCoroutine(Dust(s, g, p)); Sound("equipDrop", Sfx.EquipDrop); }
            if (clone != null) Destroy(clone.gameObject);
            Flying--;
        }

        IEnumerator Dust(EquipSwapSpec s, EquipSwapGrab g, EquipSwapPlan p)
        {
            Dusts++;
            float w = (float)EquipSwapRules.DustW(s, g), h = (float)EquipSwapRules.DustH(s, g);
            double cx = EquipSwapRules.DustX(g, p), cy = EquipSwapRules.DustY(s, g, p);
            Image d = UiKit.Rounded(Layer, EquipSwapStyle.T("dust"), "pp_paper", h * 0.5f);   // 납작한 타원(색은 아래서 표값으로 덮는다)
            d.raycastTarget = false;
            Color col = EquipSwapStyle.C("dust");
            RectTransform rt = d.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(w, h);
            double ms = 0, end = s.DustRemoveMs;
            while (ms < end)
            {
                double pct = Math.Min(100.0, ms / s.DustMs * 100.0);
                double a, sx, sy, ty; EquipSwapRules.Dust(s, pct, out a, out sx, out sy, out ty);
                Put(rt, cx, cy + (ty + 50.0) / 100.0 * h);                     // translate(-50%, ty%) — 가운데 피벗이라 (ty + 50)% 만큼
                rt.localScale = new Vector3((float)sx, (float)sy, 1f);
                col.a = (float)(a * s.DustAlphaCoreF); d.color = col;             // 정본 radial-gradient 중심 .5 → 판 하나의 α 로
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
            Destroy(d.gameObject);
            Dusts--;
        }

        Hollow MakeHollow(RectTransform cell, EquipSwapSpec s)
        {
            var h = new Hollow { Cell = cell };
            string frameName = EquipSwapStyle.T("frame");
            foreach (Graphic g in cell.GetComponentsInChildren<Graphic>(true))
            {
                if (g.transform == cell) continue;
                if (g.transform.parent == cell && g.name == frameName && g is Image) { h.Frame = (Image)g; continue; }
                if (!g.enabled) continue;
                g.enabled = false; h.Hidden.Add(g);
            }
            if (h.Frame != null)
            {
                h.FrameColor = h.Frame.color;
                Color c = h.FrameColor; float k = (float)s.HollowBrightnessF;
                h.Frame.color = new Color(c.r * k, c.g * k, c.b * k, c.a);
            }
            return h;
        }

        /// <summary>다음 프레임: 렌더가 칸을 갈아끼웠으면(옛 칸은 `Destroy` 라 프레임 끝에 사라진다) 살아 있는 새 칸으로 빈 소켓을 옮긴다. 안 갈아끼웠으면 아무것도 안 한다.</summary>
        IEnumerator Rehollow(EquipSwapGrabbed gr, EquipSwapSpec s, Hollow h)
        {
            yield return null;
            if (h == null) yield break;
            RectTransform live = CellOf(gr.Slot);
            if (live == null || live == h.Cell || !live.gameObject.activeInHierarchy) yield break;
            if (h.Cell != null) Restore(h);   // 옛 칸이 아직 살아 있는 드문 경우만 되돌린다(파괴됐으면 == null)
            Hollow n = MakeHollow(live, s);
            h.Cell = n.Cell; h.Frame = n.Frame; h.FrameColor = n.FrameColor;
            h.Hidden.Clear(); h.Hidden.AddRange(n.Hidden);
        }

        static void Restore(Hollow h)
        {
            if (h == null) return;
            foreach (Graphic g in h.Hidden) if (g != null) g.enabled = true;
            if (h.Frame != null) h.Frame.color = h.FrameColor;
        }

        IEnumerator Snap(EquipSwapSpec s, EquipSwapGrabbed gr, Hollow hollow)
        {
            double ms = 0;
            while (ms < EquipSwapRules.SnapStartMs(s)) { yield return null; ms += Time.unscaledDeltaTime * 1000.0; }
            // 칸이 그새 다시 그려졌으면(오토포지 틱 등) 붙잡아 둔 것은 문서 밖 — 새로 집는다
            RectTransform live = CellOf(gr.Slot);
            if (live == null) live = gr.Cell;
            if (live == null || !live.gameObject.activeInHierarchy) { Restore(hollow); yield break; }
            double l, t, r, b; Rect(live, out l, out t, out r, out b);
            if (r - l <= 0) { Restore(hollow); yield break; }
            Hollow liveHollow = (hollow != null && live == hollow.Cell) ? null : MakeHollow(live, s);
            RectTransform clone = Clone(live, r - l, b - t, EquipSwapStyle.T("snap"));
            // 복제는 살아 있는 칸의 «보이는» 모습이어야 한다 — 빈 소켓으로 감춘 자식은 복제에서 되살린다
            foreach (Graphic g in clone.GetComponentsInChildren<Graphic>(true)) g.enabled = true;
            Transform fr = clone.Find(EquipSwapStyle.T("frame"));
            if (fr != null && hollow != null && hollow.Frame != null) fr.GetComponent<Image>().color = hollow.FrameColor;
            CanvasGroup cg = clone.GetComponent<CanvasGroup>();
            double cx = (l + r) / 2, cy = (t + b) / 2, h = b - t;
            Snaps++;
            Sound("equipSnap", Sfx.EquipSnap);
            ms = 0;
            while (ms < s.SnapMs)
            {
                if (clone == null) break;
                double pct = Math.Min(100.0, ms / s.SnapMs * 100.0);
                double ty, sc, a, bright; EquipSwapRules.Snap(s, pct, out ty, out sc, out a, out bright);
                Put(clone, cx, cy + ty / 100.0 * h);
                clone.localScale = new Vector3((float)sc, (float)sc, 1f);
                cg.alpha = (float)a;
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
            // 복제 타일이 제자리에 앉는 그 프레임에 원본 칸을 되살린다(둘이 겹치는 프레임이 없게)
            Restore(hollow); Restore(liveHollow);
            if (clone != null) Destroy(clone.gameObject);
            Snaps--;
        }
    }

    /// <summary>`Resources/EquipSwapUi.json` — 색·이름(수치는 <see cref="EquipSwapSpec"/> 가 같은 표에서 읽는다).</summary>
    public static class EquipSwapStyle
    {
        public const string ResourcePath = "EquipSwapUi";
        static JsonObject root, colors, text;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        public static JsonObject Root { get { Load(); return root; } }

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T118)");
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
            if (hex == null) throw new KeyNotFoundException("EquipSwapUi.json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            colorCache[key] = c;
            return c;
        }

        public static string T(string key)
        {
            Load();
            string s = J.Str(text[key]);
            if (s == null) throw new KeyNotFoundException("EquipSwapUi.json 에 이름 «" + key + "» 이 없다");
            return s;
        }
    }
}
