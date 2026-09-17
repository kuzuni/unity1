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
            /// <summary>T333 10회차 — 이 숫자의 글로우 종류(정본에 글로우가 없는 등급이면 null) · 지금 걸린 겹 키 · 재질을 다시 구울 때 쓰는 키라인 키 둘.</summary>
            public string GlowCls, GlowKey, OutlineKey, StrokeKey;
            /// <summary>T440 — «내게 들어온 피해» 의 ▼ 표식(정본 `.dmg-hero::before`) · 숫자와 따로 선 작은 글자 조각(없는 종류는 꺼 둔다).</summary>
            public TextMeshProUGUI Mark;
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
        /// <summary>T333 10회차 — 글로우(<see cref="DmgGlowUi"/>)까지 같은 공유 재질에 굽는다. 겹이 시간에 따라 바뀌는 등급(크리·처치)은 «겹마다 하나» 를 캐시해 두고 재질을 갈아 끼운다(인스턴스 금지 · T50).</summary>
        static Material OutlineMaterial(TextMeshProUGUI t, string colorKey, string strokeKey, string glowKey)
        {
            string key = colorKey + "|" + strokeKey + "|" + (glowKey ?? "-") + "|" + t.fontSize.ToString("0.##");
            Material m;
            if (outlineMats.TryGetValue(key, out m) && m != null) return m;
            m = new Material(t.fontSharedMaterial);
            m.name = "dmg outline " + colorKey + " " + strokeKey + (glowKey != null ? " glow " + glowKey : "");
            UiKit.OutlinePx(m, t.font, t.fontSize, colorKey, KeylineUi.Px(strokeKey));
            // ⚠ 복제 원본은 **이 글자의 지금 재질**이라, 풀에서 온 글자면 앞 숫자의 글로우가 그대로 묻어 온다(키라인은 아래에서 덮어써 안 보이던 함정).
            //   글로우가 있는 종류는 Apply 가 값을 전부 덮고, 없는 종류(일반타·영웅 피해 …)는 여기서 꺼야 정본처럼 «겹 없음» 이 된다.
            if (glowKey != null) DmgGlowUi.Apply(m, t.font, t.fontSize, glowKey);
            else DmgGlowUi.Off(m);
            outlineMats[key] = m;
            return m;
        }
        /// <summary>테스트용 — 공유 재질 캐시를 비운다(글꼴·표가 바뀐 뒤 다시 굽게).</summary>
        public static void ResetMaterials() { outlineMats.Clear(); DmgGlowUi.Reset(); }

        /// <summary>
        /// T396 2회차 — 정본이 **선택자에만 리터럴로 못박은 글자 색**. 정본 509 가 그 뜻을 적어 뒀다:
        /// «크리 위계는 '크기'가 아니라 **색·펀치**로 준다». 그래서 이 다섯은 전역 잉크 토큰으로 찍으면 안 된다 —
        /// 종전 클론은 크리를 `cp`(#ff8a65 ↔ 정본 #ff8a1e)로, 스킬·영웅·막음·처치를 `stage_ink`(흰색)로 찍었다.
        /// **스킬(#82b1ff)·막음(#90caf9)은 파랑·하늘색인데 흰색이었다** — 색이 곧 위계인 자리에서 위계가 통째로 없었다.
        /// `heal`(#69f0ae)만 카탈로그 `pip_done` 이 같은 값이라 그대로 둔다(토큰을 써도 되는 자리 · T377 결정 636 과 같은 셈).
        /// 값은 표(`Resources/PinnedColorUi.json`)가 쥐고 `tools/check_pinned_colors.py` 의 잉크 갈래가 정본과 같은지 지킨다.
        /// </summary>
        static void Style(string cls, out TextKind kind, out string colorKey, out string inkKey, out string outlineKey, out string strokeKey, out Frame[] anim, out string prefix)
        {
            kind = TextKind.Body; colorKey = "stage_ink"; inkKey = null; outlineKey = "stage_outline"; strokeKey = "float_dmg"; anim = Dmg; prefix = "";
            switch (cls)
            {
                case "dmg-crit": kind = TextKind.Button; inkKey = "dmg_crit_ink"; anim = Crit; break;
                case "dmg-kill": kind = TextKind.Button; inkKey = "dmg_kill_ink"; outlineKey = "cp"; strokeKey = "float_dmg_kill"; anim = Kill; break;
                case "dmg-skill": kind = TextKind.Button; inkKey = "dmg_skill_ink"; break;
                case "dmg-hero": prefix = "▼"; inkKey = "dmg_hero_ink"; strokeKey = "float_dmg_hero"; break;
                case "heal": kind = TextKind.Button; colorKey = "pip_done"; break;
                case "loot": kind = TextKind.Button; colorKey = "coin"; break;
                case "block": inkKey = "dmg_block_ink"; break;
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
            TextKind kind; string colorKey, inkKey, outlineKey, strokeKey, prefix; Frame[] anim;
            Style(cls, out kind, out colorKey, out inkKey, out outlineKey, out strokeKey, out anim, out prefix);
            // T440 — 접두 ▼ 는 숫자 문자열에 안 붙인다(같은 크기가 된다) · 아래 HeroMark 가 따로 조각으로 세운다.
            Num n = Take(layer, kind, colorKey, text ?? string.Empty);
            TextMeshProUGUI t = n.T;
            // T396 2회차 — 못박은 잉크는 표에서(§1 — 코드에 hex 를 안 박는다). `n.Color` 를 뜨기 **전**이어야 한다(아래 151행).
            if (inkKey != null) t.color = PinnedColorUi.C(inkKey);
            // T333 10회차 — 정본 `@keyframes dmgcrit/dmgkill` 은 **태어나는 프레임**의 겹이 다르다(561~563 «위계는 태어나는 프레임에 서 있어야 한다»).
            n.OutlineKey = outlineKey; n.StrokeKey = strokeKey;
            n.GlowCls = DmgGlowUi.Has(cls) ? cls : null;
            n.GlowKey = n.GlowCls != null ? DmgGlowUi.KeyAt(n.GlowCls, 0) : null;
            t.fontSharedMaterial = OutlineMaterial(t, outlineKey, strokeKey, n.GlowKey);
            HeroMark(n, prefix, text ?? string.Empty, colorKey, outlineKey, strokeKey);
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
        /// <summary>
        /// T440 — 정본 `style.css` 549 `.float-dmg.dmg-hero::before { content: '▼'; font-size: .72em; margin-right: .14em; vertical-align: .04em }`
        /// (546~548 주석 «색·위치만으로는 소유자가 안 읽혀서 기호로 못 박는다 · **숫자 크기는 안 건드리고 기호만 작게**»). 전엔 `prefix + text` 한 문자열이라
        /// ▼ 가 숫자와 같은 크기였고 틈도 없었다. richText 는 켜지 않는다(`tools/check_richtext.py` · T89) — 글자 조각 하나를 숫자 상자의 자식으로 세운다:
        /// 크기 = 숫자 × `hero_mark_em` · 틈 = 숫자 × `hero_mark_gap_em` · 올림 = 숫자 × `hero_mark_rise_em`(표 `Resources/DamageUi.json`).
        /// 정본은 «▼ + 틈 + 숫자» 한 덩어리가 원점에 가운데 서므로 숫자를 TMP `margin`(왼쪽 = 표식 폭 + 틈)으로 그 절반만큼 오른쪽에 물린다 — 자리·연출(Origin·Place)은 안 건드린다.
        /// 표식은 숫자 상자의 자식이라 배율·회전은 따라오고, 알파는 <see cref="Place"/> 가 같이 준다. 종류가 바뀌어 풀에서 다시 나온 숫자는 표식을 끄고 margin 을 0 으로 돌린다.
        /// </summary>
        static void HeroMark(Num n, string prefix, string text, string colorKey, string outlineKey, string strokeKey)
        {
            TextMeshProUGUI t = n.T;
            if (prefix.Length == 0)
            {
                if (n.Mark != null) n.Mark.gameObject.SetActive(false);
                t.margin = Vector4.zero;
                return;
            }
            float fs = t.fontSize;
            float markFs = fs * DamageStyle.L("hero_mark_em"), gap = fs * DamageStyle.L("hero_mark_gap_em"), rise = fs * DamageStyle.L("hero_mark_rise_em");
            float markW = ApproxWidth(markFs, prefix), numW = ApproxWidth(fs, text);
            if (n.Mark == null)
            {
                // 글자는 공장(UiKit.Text)에서만 만든다(check_richtext ⓑ). 종류는 Micro — 하한 18 의 예외 칸(결정 633)이라 숫자 × .72 가 하한 게이트에 안 걸린다.
                n.Mark = UiKit.Text(n.Rt, "mark", TextKind.Micro, prefix, colorKey);
                RectTransform mr = n.Mark.rectTransform;
                mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 0.5f);
                mr.pivot = new Vector2(0.5f, 0.5f);
            }
            TextMeshProUGUI m = n.Mark;
            m.gameObject.SetActive(true);
            m.text = prefix;
            m.fontSize = markFs;
            m.color = t.color;
            m.fontSharedMaterial = OutlineMaterial(m, outlineKey, strokeKey, null);   // 같은 잉크·키라인(정본 ::before 는 글자 색·stroke 를 물려받는다) · 크기가 달라 재질은 따로 굽는다
            m.rectTransform.sizeDelta = new Vector2(markW * 2f + gap, fs * 1.4f);
            t.margin = new Vector4(markW + gap, 0f, 0f, 0f);
            // 숫자 잉크 왼끝 = (표식 폭 + 틈)/2 − 숫자 폭/2 · 표식 가운데 = 그 왼끝 − 틈 − 표식 폭/2 = −숫자 폭/2 − 틈/2
            m.rectTransform.anchoredPosition = new Vector2(-numW * 0.5f - gap * 0.5f, rise);
        }

        /// <summary>글자 폭 어림 — `PetSkillKit.TextWidth` 와 같은 규칙(한중일 1.0 · 공백 .3 · 그 밖 .58)을 임의 크기로.</summary>
        public static float ApproxWidth(float size, string s)
        {
            float w = 0f;
            foreach (char ch in s) w += ch > 0x2E80 ? size * 1.0f : (ch == ' ' ? size * 0.3f : size * 0.58f);
            return w;
        }

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
            if (n.Mark != null) n.Mark.canvasRenderer.SetAlpha(1f);
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
            if (n.Mark != null && n.Mark.gameObject.activeSelf) n.Mark.canvasRenderer.SetAlpha((float)al);   // T440 — 표식도 같은 알파
            // T333 10회차 — 글로우 겹은 표의 단계(정본 키프레임 퍼센트)에서 갈린다. 겹마다 구운 공유 재질을 갈아 끼울 뿐이라 숫자마다 재질이 늘지 않는다.
            if (n.GlowCls != null)
            {
                string gk = DmgGlowUi.KeyAt(n.GlowCls, u);
                if (gk != n.GlowKey)
                {
                    n.GlowKey = gk;
                    n.T.fontSharedMaterial = OutlineMaterial(n.T, n.OutlineKey, n.StrokeKey, gk);
                }
            }
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

    /// <summary>
    /// T440 — 전투 숫자의 곁 표(<c>Assets/Forge/Resources/DamageUi.json</c> `layout`). 정본 549 `.dmg-hero::before` 의 em 값 셋 — 코드에 숫자를 박지 않는다(§1).
    /// <see cref="Forge.Game.Ui.CraftStyle"/> 와 같은 꼴.
    /// </summary>
    public static class DamageStyle
    {
        public const string ResourcePath = "DamageUi";
        static Forge.Core.Data.JsonObject root, layout;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T440)");
            root = Forge.Core.Data.MiniJson.ParseObject(ta.text);
            layout = Forge.Core.Data.J.Obj(root["layout"]);
        }

        public static void Reset() { root = null; layout = null; }

        /// <summary>배치 값 원문(em 배수).</summary>
        public static float L(string key)
        {
            Load();
            object v = layout[key];
            if (!Forge.Core.Data.J.IsNum(v)) throw new KeyNotFoundException("DamageUi.json 에 배치 값 «" + key + "» 이 없다");
            return (float)Forge.Core.Data.J.Num(v);
        }
    }
}
