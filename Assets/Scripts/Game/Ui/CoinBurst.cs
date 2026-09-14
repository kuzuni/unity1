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
    /// 판매 코인 분출(정본 `ui.js coinBurst(total)` · T117): 모루 버튼 위에서 코인 3~10개가 줄×칸 격자 착지점으로 날아가고,
    /// 착지하는 순간 그 자리에서 «+합÷개수» 가 떠오른다(전부 같은 값 · `sell-coin-split-rising`). 층은 정본 `#coin-burst`(z 6 = 장비 시트 위 · 패널 아래).
    /// 정본 가드(`coin-burst-over-modal`): 열린 팝업·탭 패널이 있으면(모루가 가려졌다) **연출도 소리도 통째로 생략**한다.
    /// 수치는 전부 `Resources/CoinBurstUi.json`(<see cref="CoinBurstStyle"/>) · 셈은 Core <see cref="CoinBurstRules"/>. 클럭은 정본(CSS 애니메이션)처럼 벽시계.
    /// 호출(정본 네 자리 — 묶음 판매·오토포지·일괄·[판매])은 `ForgeHost` 가 T87 lock 뒤에 잇는다(1회차는 연출·표·자만).
    /// </summary>
    public sealed class CoinBurst : MonoBehaviour
    {
        public static CoinBurst Instance { get; private set; }
        /// <summary>층(정본 `#coin-burst` · 앱 상자 전체 · 장비 시트 바로 위).</summary>
        public RectTransform Layer { get { return (RectTransform)transform; } }
        /// <summary>지금 날고 있는 조각 수 · 떠 있는 금액 라벨 수(테스트).</summary>
        public int Pieces { get; private set; }
        public int Labels { get; private set; }
        /// <summary>마지막 분출의 라벨 문구(착지 순서) · 조각 수.</summary>
        public readonly List<string> LastLabels = new List<string>();
        public int LastCount { get; private set; }
        public int PlayCount { get; private set; }

        static CoinBurstSpec spec;
        static System.Random rng = new System.Random();

        public static CoinBurstSpec Spec { get { if (spec == null) spec = CoinBurstSpec.From(CoinBurstStyle.Root); return spec; } }

        /// <summary>층을 세운다(없으면) — `UiRoot.App` 안, 장비 시트 바로 위 형제.</summary>
        public static CoinBurst Ensure()
        {
            if (Instance != null) return Instance;
            UiRoot root = UiRoot.Instance;
            if (root == null || root.App == null) return null;
            RectTransform rt = UiKit.Box(root.App, "coin-burst");
            if (root.Sheet != null) rt.SetSiblingIndex(root.Sheet.GetSiblingIndex() + 1);
            CoinBurst cb = rt.gameObject.AddComponent<CoinBurst>();
            Instance = cb;
            return cb;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>정본 `.modal:not(.hidden)` / `.panel.open` — 모루가 가려져 있는가.</summary>
        public static bool Covered()
        {
            UiRoot root = UiRoot.Instance;
            if (root == null) return true;
            if (root.TabBar != null && root.TabBar.ActiveTab != null) return true;
            // 팝업(정본 `.modal:not(.hidden)`)은 팝업 층의 «열린 목록» 으로 본다 — `Hide` 가 판을 `Destroy` 로 걷어 그 오브젝트는 프레임 끝까지
            // `modals` 아래 남아 있으므로, 아이 수로 재면 [판매] 직후(비교 팝업을 방금 접은 그 프레임 = 정본 3901 의 주 경로)가 «가려짐» 으로
            // 잘못 읽혀 연출이 통째로 빠진다(T117 2회차 실측 · 결정 310). 층이 아직 없을 때만 아이 수로 대신 본다.
            PopupLayer pl = PopupLayer.Instance;
            if (pl != null) return pl.Open.Count > 0;
            Transform modals = root.App.Find("modals");
            return modals != null && modals.childCount > 0;
        }

        /// <summary>모루 버튼(정본 `.anvil-btn`) — 장비 시트 안에서 이름으로 찾는다(없으면 null · 시트가 든 것이 held-slot 이면 없다 = 정본 «조용히 생략»).</summary>
        public static RectTransform AnvilButton()
        {
            UiRoot root = UiRoot.Instance;
            if (root == null || root.Sheet == null) return null;
            string name = CoinBurstStyle.T("anvil_btn");
            foreach (RectTransform r in root.Sheet.GetComponentsInChildren<RectTransform>(false))
                if (r.name == name) return r;
            return null;
        }

        /// <summary>정본 `UI.coinBurst(total)`. 돌아오는 값 = 띄운 조각 수(가드에 걸리면 0).</summary>
        public static int Play(double total)
        {
            CoinBurst cb = Ensure();
            if (cb == null) return 0;
            return cb.Burst(total);
        }

        int Burst(double total)
        {
            CoinBurstSpec s = Spec;
            int n = CoinBurstRules.Count(s, total);
            if (n <= 0) return 0;
            RectTransform btn = AnvilButton();
            if (btn == null) return 0;
            if (Covered()) return 0;
            // 출발점: 모루 버튼 가로 가운데 · 위에서 0.4h (층 좌표 · 왼쪽 위 원점)
            Vector3[] c = new Vector3[4];
            btn.GetWorldCorners(c);
            Vector3 wl = Layer.InverseTransformPoint(c[0]), wr = Layer.InverseTransformPoint(c[2]);
            float bw = wr.x - wl.x, bh = wr.y - wl.y;
            float halfW = Layer.rect.width * 0.5f, halfH = Layer.rect.height * 0.5f;
            float ox = wl.x + halfW + bw * 0.5f;
            float oy = halfH - (wr.y - bh * (float)s.OriginYF);
            double rem = PopupKit.Rem, cssPx = UiKit.L("anvil_fx_px");
            CoinPiece[] pieces = CoinBurstRules.Layout(s, total, rem, cssPx, (a, b) => a + rng.NextDouble() * (b - a));
            double per = CoinBurstRules.Per(total, n);
            string label = CoinBurstStyle.T("plus", NumFmt.Fmt(per));
            LastLabels.Clear();
            LastCount = n;
            PlayCount++;
            for (int i = 0; i < pieces.Length; i++) StartCoroutine(Fly(s, pieces[i], ox, oy, (float)rem, label));
            Sfx.Gacha(CoinBurstStyle.T("sfx_gacha_rarity"));   // 정본: 동전 소리 대용 — 짧은 상승 스윕
            return n;
        }

        IEnumerator Fly(CoinBurstSpec s, CoinPiece p, float ox, float oy, float rem, string label)
        {
            Pieces++;
            float size = (float)s.CoinRem * rem;
            RectTransform fly = UiKit.Box(Layer, "coin-fly");
            UiKit.Place(fly, ox - size * 0.5f, oy - size * 0.5f, size, size);
            Image img = UiKit.Icon(fly, "coin-fly-img", CoinBurstStyle.T("icon"));
            img.raycastTarget = false;
            UiKit.Fill(img.rectTransform);
            double bounce = s.BounceRem * rem;
            double ms = -p.DelayMs;
            bool landed = false;
            double end = CoinBurstRules.PieceEndMs(s, p);
            double landAt = CoinBurstRules.LandMs(s, p);
            while (ms < end)
            {
                double t = Math.Max(0, ms);
                double pct = Math.Min(100.0, t / s.FlyMs * 100.0);
                double y, a;
                CoinBurstRules.FlyY(s, p, bounce, pct, out y, out a);
                double x = p.Dx * Math.Min(1.0, t / s.FlyMs);            // coinFlyX — 등속
                fly.anchoredPosition = new Vector2(ox - size * 0.5f + (float)x, -(oy - size * 0.5f + (float)y));
                fly.gameObject.SetActive(ms >= 0);
                Color col = img.color; col.a = (float)a; img.color = col;
                img.rectTransform.localRotation = Quaternion.Euler(0f, (float)(t / s.SpinMs * 360.0), 0f);   // coinSpin — rotateY
                if (!landed && ms + p.DelayMs >= landAt)
                {
                    landed = true;
                    StartCoroutine(Amount(s, ox + (float)p.Dx, oy + (float)p.Drop, rem, label));
                }
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
            if (!landed) StartCoroutine(Amount(s, ox + (float)p.Dx, oy + (float)p.Drop, rem, label));
            Destroy(fly.gameObject);
            Pieces--;
        }

        IEnumerator Amount(CoinBurstSpec s, float x, float y, float rem, string label)
        {
            Labels++;
            LastLabels.Add(label);
            TextMeshProUGUI t = UiKit.Text(Layer, "coin-amt", TextKind.Sub, label);
            t.fontSize = (float)s.AmtFontRem * rem;          // 정본 .82rem — 표값(TextKind 표에 없는 크기)
            t.color = CoinBurstStyle.C("amt");
            t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            UiKit.Outline(t, "pp_line", (float)s.AmtOutline);
            RectTransform rt = t.rectTransform;
            float w = rem * 6f, h = t.fontSize * 1.4f;
            UiKit.Place(rt, x - w * 0.5f, y - h * 0.5f, w, h);
            double ms = 0, end = CoinBurstRules.AmtEndMs(s);
            while (ms < end)
            {
                double pct = Math.Min(100.0, ms / s.AmtMs * 100.0);
                double a, ty, sc;
                CoinBurstRules.Amt(s, pct, out a, out ty, out sc);
                // translate(-50%, ty%) 는 자기 높이 기준 — 가운데(-50%) 에서 ty 만큼
                float dy = (float)((ty + 50.0) / 100.0) * h;
                rt.anchoredPosition = new Vector2(x - w * 0.5f, -(y - h * 0.5f + dy));
                rt.localScale = new Vector3((float)sc, (float)sc, 1f);
                Color col = t.color; col.a = (float)a; t.color = col;
                yield return null;
                ms += Time.unscaledDeltaTime * 1000.0;
            }
            Destroy(t.gameObject);
            Labels--;
        }
    }

    /// <summary>`Resources/CoinBurstUi.json` — 색·문구(수치는 <see cref="CoinBurstSpec"/> 가 같은 표에서 읽는다).</summary>
    public static class CoinBurstStyle
    {
        public const string ResourcePath = "CoinBurstUi";
        static JsonObject root, colors, text;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        public static JsonObject Root { get { Load(); return root; } }

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T117)");
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
            if (hex == null) throw new KeyNotFoundException("CoinBurstUi.json 에 색 «" + key + "» 이 없다");
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            colorCache[key] = c;
            return c;
        }

        public static string T(string key)
        {
            Load();
            string s = J.Str(text[key]);
            if (s == null) throw new KeyNotFoundException("CoinBurstUi.json 에 문구 «" + key + "» 이 없다");
            return s;
        }
        public static string T(string key, params object[] args) { return string.Format(T(key), args); }
    }
}
