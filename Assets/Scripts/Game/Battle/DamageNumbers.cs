using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Forge.Core.BattleFx;
using Forge.Game.Ui;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 데미지 숫자(T8) — 원작 `damageNumber`(17267 · DOM 오버레이 `.float-dmg`) + `css/style.css` 의 아크 키프레임(`dmg`·`dmgcrit`·`dmgkill`)을 T18 앱 상자(<see cref="UiRoot.App"/>) 위의 TMP 글자로.
    /// 월드 → 화면 → 앱 상자 로컬 좌표. 글자는 <see cref="UiKit.Text"/> 로만(fontSize 직접 금지 · §1) · px 단위는 원작 rem 기준(앱 높이 844 → 1920)으로 환산.
    /// 색 키는 카탈로그의 가장 가까운 것(dmg 전용 키는 T22 가 catalog.json 에 더한다 · 결정 기록).
    /// 층(T76): 원작은 `.float-dmg` 가 `#game-area > #fx-layer`(`#app` 의 **첫** 자식 · `isolation: isolate`) 안에 있고 시트·`.modal`·탭바는 DOM 뒤에 와서 그 위를 덮는다 —
    /// 여기서도 앱 상자의 **맨 아래 형제** `fx-layer` 에만 붙인다(<see cref="Layer"/>). 앱 상자에 직접 붙이면 마지막 형제라 열린 시트 위에 겹친다(런 124 `screen_shop.png` 의 «▼745»).
    /// </summary>
    public sealed class DamageNumbers
    {
        /// <summary>원작 `#fx-layer` — 앱 상자 안 전투 글자 층의 이름.</summary>
        public const string LayerName = "fx-layer";

        /// <summary>앱 상자의 첫 자식 `fx-layer`(없으면 만든다 · 앱 상자를 꽉 채운다). HUD·시트·채팅줄·패널·탭바·팝업 층이 전부 그 뒤 형제라 위를 덮는다.</summary>
        public static RectTransform Layer(UiRoot root)
        {
            if (root == null || root.App == null) return null;
            RectTransform rt = root.App.Find(LayerName) as RectTransform;
            if (rt == null)
            {
                rt = UiKit.Box(root.App, LayerName);
                UiKit.Band(rt, 0f, 1f);
            }
            if (rt.GetSiblingIndex() != 0) rt.SetAsFirstSibling();
            return rt;
        }

        /// <summary>원작 `fitLayout`: 루트 폰트 = 앱높이/844×16 — CSS px 하나가 앱 상자에서 RefH/844 px.</summary>
        public const double CssRefH = 844;
        public const int MaxLive = 40;

        sealed class Frame { public double T, Dx, Rise, Scale, Alpha, Rot; public Frame(double t, double dx, double rise, double s, double a, double r) { T = t; Dx = dx; Rise = rise; Scale = s; Alpha = a; Rot = r; } }
        static readonly Frame[] Dmg = { new Frame(0, 0, 0, 0.9, 1, 0), new Frame(0.07, 0.16, 0.36, 1.24, 1, 0), new Frame(0.34, 0.7, 1, 1, 1, 0), new Frame(0.62, 0.9, 0.78, 1, 1, 0), new Frame(1, 1, 0.68, 0.9, 0, 0) };
        static readonly Frame[] Crit = { new Frame(0, 0, 0, 1.12, 1, -10), new Frame(0.09, 0.16, 0.34, 1.35, 1, 5), new Frame(0.26, 0.62, 1, 1.05, 1, -3), new Frame(0.60, 0.88, 0.76, 1, 1, 1), new Frame(1, 1, 0.66, 0.92, 0, 0) };
        static readonly Frame[] Kill = { new Frame(0, 0, 0, 1.28, 1, -6), new Frame(0.07, 0.12, 0.26, 1.62, 1, 3), new Frame(0.17, 0.3, 0.52, 1.34, 1, -2), new Frame(0.30, 0.58, 0.86, 1.44, 1, 1), new Frame(0.62, 0.88, 0.94, 1.2, 1, 0), new Frame(1, 1, 0.8, 1.05, 0, 0) };

        sealed class Num
        {
            public RectTransform Rt; public TextMeshProUGUI T; public UiTextKindTag Tag; public Frame[] Anim; public Vector2 Origin; public double Dx, Rise, Pop, Age; public Color Color;
        }

        readonly List<Num> live = new List<Num>();
        /// <summary>죽은 숫자의 글자 오브젝트(T50 풀) — 피격마다 GameObject+RectTransform+TMP 를 새로 만들지 않는다(원작 DOM 은 브라우저가 되쓴다 · 같은 «되쓰기»).</summary>
        readonly Stack<Num> pool = new Stack<Num>();
        public int Count { get { return live.Count; } }
        public int Pooled { get { return pool.Count; } }
        /// <summary>글자 오브젝트를 실제로 만든 수(풀이 도는지 재는 자 · T50).</summary>
        public int Created { get; private set; }
        /// <summary>false 면 세기만 하고 글자를 안 띄운다(T50 갈래별 측정용).</summary>
        public bool Enabled = true;
        public int SpawnedTotal { get; private set; }
        public string LastText { get; private set; }
        public string LastClass { get; private set; }
        /// <summary>피해 숫자(`dmg*` 등급)만 센 수 — loot/heal/block 은 뺀다(테스트용).</summary>
        public int DmgSpawned { get; private set; }

        static double K { get { return UiKit.RefH / CssRefH; } }

        /// <summary>
        /// 숫자마다 재질을 복제하지 않는다(초당 수십 개) — 아웃라인 색 키·키라인 키·글자 크기마다 하나를 만들어 나눠 쓴다(`UiKit.Outline` 은 fontMaterial 인스턴스를 만든다).
        /// T109 6회차: 두께는 코드 상수(.2 · 여백 비율)가 아니라 정본 `-webkit-text-stroke` 폭표(`KeylineUi.json` px · `.float-dmg .6px` · `.dmg-kill 1px` · `.dmg-hero .55px`)를
        /// <see cref="UiKit.OutlinePx(Material, TMP_FontAsset, float, string, float)"/>(D = W · T104 식)로 얹는다 — 글자 크기마다 W 가 다르니 키에 크기가 든다.
        /// </summary>
        static readonly Dictionary<string, Material> outlineMats = new Dictionary<string, Material>();
        static Material OutlineMaterial(TextMeshProUGUI t, string colorKey, string strokeKey)
        {
            string key = colorKey + "|" + strokeKey + "|" + t.fontSize.ToString("0.##");
            Material m;
            if (outlineMats.TryGetValue(key, out m) && m != null) return m;
            m = new Material(t.fontSharedMaterial);
            m.name = "dmg outline " + colorKey + " " + strokeKey;
            UiKit.OutlinePx(m, t.font, t.fontSize, colorKey, KeylineUi.Px(strokeKey));
            outlineMats[key] = m;
            return m;
        }
        /// <summary>테스트용 — 공유 재질 캐시를 비운다(글꼴·표가 바뀐 뒤 다시 굽게).</summary>
        public static void ResetMaterials() { outlineMats.Clear(); }

        static void Style(string cls, out TextKind kind, out string colorKey, out string outlineKey, out string strokeKey, out Frame[] anim, out string prefix)
        {
            kind = TextKind.Body; colorKey = "stage_ink"; outlineKey = "stage_outline"; strokeKey = "float_dmg"; anim = Dmg; prefix = "";
            switch (cls)
            {
                case "dmg-crit": kind = TextKind.Button; colorKey = "cp"; anim = Crit; break;
                case "dmg-kill": kind = TextKind.Button; colorKey = "stage_ink"; outlineKey = "cp"; strokeKey = "float_dmg_kill"; anim = Kill; break;
                case "dmg-skill": kind = TextKind.Button; colorKey = "stage_ink"; break;
                case "dmg-hero": prefix = "▼"; strokeKey = "float_dmg_hero"; break;
                case "heal": kind = TextKind.Button; colorKey = "pip_done"; break;
                case "loot": kind = TextKind.Button; colorKey = "coin"; break;
                case "block": colorKey = "stage_ink"; break;
            }
        }

        /// <summary>three 월드 좌표에 숫자를 띄운다. dx·rise = CSS px(원작 인자 그대로) · pop = 크기 배율. UI 가 없으면 세지 않고 건너뛴다.</summary>
        public void Spawn(Vector3 threeWorld, string text, string cls, double dx, double rise, double pop)
        {
            SpawnedTotal++; LastText = text; LastClass = cls;
            if (cls != null && cls.StartsWith("dmg", StringComparison.Ordinal)) DmgSpawned++;
            if (!Enabled) return;
            var root = UiRoot.Instance;
            var cam = Camera.main;
            if (root == null || root.App == null || cam == null) return;
            if (live.Count > MaxLive) return;
            RectTransform layer = Layer(root);
            Vector3 sp = cam.WorldToScreenPoint(ThreeSpace.Pos(threeWorld.x, threeWorld.y, threeWorld.z));
            Vector2 lp;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, new Vector2(sp.x, sp.y), null, out lp)) return;
            double k = K;
            // 슬롯 회피(연타가 같은 픽셀에 겹치지 않게 · 4칸까지)
            double topFloor = layer.rect.yMax - (HitRules.DmgTopMargin + Math.Abs(rise)) * k;
            for (int i = 0; i < 4; i++)
            {
                bool clash = false;
                foreach (var o in live) if (Math.Abs(o.Origin.x - lp.x) < HitRules.DmgSlotDx * k && Math.Abs(o.Origin.y - lp.y) < HitRules.DmgSlotDy * k) { clash = true; break; }
                if (!clash) break;
                if (lp.y + HitRules.DmgSlotStep * k > topFloor) break;
                lp.y += (float)(HitRules.DmgSlotStep * k);
            }
            lp.y = (float)Math.Min(lp.y, topFloor);
            TextKind kind; string colorKey, outlineKey, strokeKey, prefix; Frame[] anim;
            Style(cls, out kind, out colorKey, out outlineKey, out strokeKey, out anim, out prefix);
            Num n = Take(layer, kind, colorKey, prefix.Length == 0 ? (text ?? string.Empty) : prefix + text);
            TextMeshProUGUI t = n.T;
            t.fontSharedMaterial = OutlineMaterial(t, outlineKey, strokeKey);
            RectTransform rt = n.Rt;
            // 가로 화면 클램프(아크가 다 흐른 뒤에도 앱 상자 안)
            float half = rt.sizeDelta.x * 0.5f * (float)pop * 0.5f;
            float pad = (float)(HitRules.DmgSidePad * k);
            float minX = layer.rect.xMin + pad + half - (float)Math.Min(0, dx * k), maxX = layer.rect.xMax - pad - half - (float)Math.Max(0, dx * k);
            if (minX <= maxX) lp.x = Mathf.Clamp(lp.x, minX, maxX);
            n.Anim = anim; n.Origin = lp; n.Dx = dx * k; n.Rise = rise * k; n.Pop = pop; n.Age = 0; n.Color = t.color;
            live.Add(n);
            Place(n, 0);
        }

        /// <summary>풀에서 꺼내(없으면 <see cref="UiKit.Text"/> 로 한 번 만들고) 종류·색·글자를 다시 입힌다 — 종류 표식(<see cref="UiTextKindTag"/>)·글자 크기는 §1 하한 게이트가 보므로 같이 갱신한다.</summary>
        Num Take(RectTransform layer, TextKind kind, string colorKey, string text)
        {
            Num n = null;
            while (pool.Count > 0) { n = pool.Pop(); if (n.Rt != null) break; n = null; }
            if (n == null)
            {
                var t0 = UiKit.Text(layer, "dmg", kind, text, colorKey);
                var rt0 = t0.rectTransform;
                rt0.anchorMin = rt0.anchorMax = new Vector2(0.5f, 0.5f);
                rt0.pivot = new Vector2(0.5f, 0.5f);
                rt0.sizeDelta = new Vector2(UiKit.RefW * 0.5f, UiKit.RefH * 0.08f);
                n = new Num { Rt = rt0, T = t0, Tag = t0.GetComponent<UiTextKindTag>() };
                Created++;
                return n;
            }
            TextMeshProUGUI t = n.T;
            t.fontSize = UiCatalog.Instance.Kind(kind).size;
            t.color = UiKit.C(colorKey);
            t.text = text;
            if (n.Tag != null) n.Tag.Kind = kind;
            if (n.Rt.parent != layer) n.Rt.SetParent(layer, false);
            n.Rt.SetAsLastSibling();
            n.Rt.gameObject.SetActive(true);
            return n;
        }

        /// <summary>글자를 끄고 풀에 돌려놓는다(파괴하지 않는다).</summary>
        void Release(Num n)
        {
            if (n.Rt == null) return;
            n.T.canvasRenderer.SetAlpha(1f);
            n.Rt.gameObject.SetActive(false);
            pool.Push(n);
        }

        static void Place(Num n, double u)
        {
            Frame[] f = n.Anim;
            Frame a = f[0], b = f[f.Length - 1];
            for (int i = 0; i < f.Length - 1; i++) if (u >= f[i].T && u <= f[i + 1].T) { a = f[i]; b = f[i + 1]; break; }
            double w = b.T > a.T ? (u - a.T) / (b.T - a.T) : 0;
            double dx = a.Dx + (b.Dx - a.Dx) * w, rise = a.Rise + (b.Rise - a.Rise) * w, sc = a.Scale + (b.Scale - a.Scale) * w, al = a.Alpha + (b.Alpha - a.Alpha) * w, rot = a.Rot + (b.Rot - a.Rot) * w;
            // CSS y 는 아래가 + · rise 는 음수(위로) → 유니티 UI 는 위가 + 이므로 부호를 뒤집는다
            n.Rt.anchoredPosition = new Vector2((float)(n.Origin.x + n.Dx * dx), (float)(n.Origin.y - n.Rise * rise));
            n.Rt.localScale = Vector3.one * (float)(n.Pop * sc);
            n.Rt.localRotation = Quaternion.Euler(0, 0, (float)-rot);
            // 알파는 정점색(TMP 메시 재생성 · 프레임마다 관리 할당)이 아니라 CanvasRenderer 에 — 글자 메시는 띄울 때 한 번만 만든다(T50).
            n.T.canvasRenderer.SetAlpha((float)al);
        }

        public void Step(float dt)
        {
            double life = HitRules.DmgLifeMs / 1000;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var n = live[i];
                n.Age += dt;
                if (n.Age >= life || n.Rt == null)
                {
                    Release(n);
                    live.RemoveAt(i);
                    continue;
                }
                Place(n, n.Age / life);
            }
        }

        public void Clear()
        {
            foreach (var n in live) if (n.Rt != null) UnityEngine.Object.Destroy(n.Rt.gameObject);
            live.Clear();
            while (pool.Count > 0) { var n = pool.Pop(); if (n.Rt != null) UnityEngine.Object.Destroy(n.Rt.gameObject); }
        }
    }
}
