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
    /// </summary>
    public sealed class DamageNumbers
    {
        /// <summary>원작 `fitLayout`: 루트 폰트 = 앱높이/844×16 — CSS px 하나가 앱 상자에서 RefH/844 px.</summary>
        public const double CssRefH = 844;
        public const int MaxLive = 40;

        sealed class Frame { public double T, Dx, Rise, Scale, Alpha, Rot; public Frame(double t, double dx, double rise, double s, double a, double r) { T = t; Dx = dx; Rise = rise; Scale = s; Alpha = a; Rot = r; } }
        static readonly Frame[] Dmg = { new Frame(0, 0, 0, 0.9, 1, 0), new Frame(0.07, 0.16, 0.36, 1.24, 1, 0), new Frame(0.34, 0.7, 1, 1, 1, 0), new Frame(0.62, 0.9, 0.78, 1, 1, 0), new Frame(1, 1, 0.68, 0.9, 0, 0) };
        static readonly Frame[] Crit = { new Frame(0, 0, 0, 1.12, 1, -10), new Frame(0.09, 0.16, 0.34, 1.35, 1, 5), new Frame(0.26, 0.62, 1, 1.05, 1, -3), new Frame(0.60, 0.88, 0.76, 1, 1, 1), new Frame(1, 1, 0.66, 0.92, 0, 0) };
        static readonly Frame[] Kill = { new Frame(0, 0, 0, 1.28, 1, -6), new Frame(0.07, 0.12, 0.26, 1.62, 1, 3), new Frame(0.17, 0.3, 0.52, 1.34, 1, -2), new Frame(0.30, 0.58, 0.86, 1.44, 1, 1), new Frame(0.62, 0.88, 0.94, 1.2, 1, 0), new Frame(1, 1, 0.8, 1.05, 0, 0) };

        sealed class Num
        {
            public RectTransform Rt; public TextMeshProUGUI T; public Frame[] Anim; public Vector2 Origin; public double Dx, Rise, Pop, Age; public Color Color;
        }

        readonly List<Num> live = new List<Num>();
        public int Count { get { return live.Count; } }
        public int SpawnedTotal { get; private set; }
        public string LastText { get; private set; }
        public string LastClass { get; private set; }

        static double K { get { return UiKit.RefH / CssRefH; } }

        /// <summary>숫자마다 재질을 복제하지 않는다(초당 수십 개) — 아웃라인 색 키마다 하나를 만들어 나눠 쓴다(`UiKit.Outline` 은 fontMaterial 인스턴스를 만든다).</summary>
        static readonly Dictionary<string, Material> outlineMats = new Dictionary<string, Material>();
        static Material OutlineMaterial(TextMeshProUGUI t, string colorKey)
        {
            Material m;
            if (outlineMats.TryGetValue(colorKey, out m) && m != null) return m;
            m = new Material(t.fontSharedMaterial);
            m.name = "dmg outline " + colorKey;
            m.EnableKeyword("OUTLINE_ON");
            m.SetColor("_OutlineColor", UiKit.C(colorKey));
            m.SetFloat("_OutlineWidth", OutlineWidth);
            outlineMats[colorKey] = m;
            return m;
        }
        public const float OutlineWidth = 0.2f;

        static void Style(string cls, out TextKind kind, out string colorKey, out string outlineKey, out Frame[] anim, out string prefix)
        {
            kind = TextKind.Body; colorKey = "stage_ink"; outlineKey = "stage_outline"; anim = Dmg; prefix = "";
            switch (cls)
            {
                case "dmg-crit": kind = TextKind.Button; colorKey = "cp"; anim = Crit; break;
                case "dmg-kill": kind = TextKind.Button; colorKey = "stage_ink"; outlineKey = "cp"; anim = Kill; break;
                case "dmg-skill": kind = TextKind.Button; colorKey = "stage_ink"; break;
                case "dmg-hero": prefix = "▼"; break;
                case "heal": kind = TextKind.Button; colorKey = "pip_done"; break;
                case "loot": kind = TextKind.Button; colorKey = "coin"; break;
                case "block": colorKey = "stage_ink"; break;
            }
        }

        /// <summary>three 월드 좌표에 숫자를 띄운다. dx·rise = CSS px(원작 인자 그대로) · pop = 크기 배율. UI 가 없으면 세지 않고 건너뛴다.</summary>
        public void Spawn(Vector3 threeWorld, string text, string cls, double dx, double rise, double pop)
        {
            SpawnedTotal++; LastText = text; LastClass = cls;
            var root = UiRoot.Instance;
            var cam = Camera.main;
            if (root == null || root.App == null || cam == null) return;
            if (live.Count > MaxLive) return;
            Vector3 sp = cam.WorldToScreenPoint(ThreeSpace.Pos(threeWorld.x, threeWorld.y, threeWorld.z));
            Vector2 lp;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root.App, new Vector2(sp.x, sp.y), null, out lp)) return;
            double k = K;
            // 슬롯 회피(연타가 같은 픽셀에 겹치지 않게 · 4칸까지)
            double topFloor = root.App.rect.yMax - (HitRules.DmgTopMargin + Math.Abs(rise)) * k;
            for (int i = 0; i < 4; i++)
            {
                bool clash = false;
                foreach (var o in live) if (Math.Abs(o.Origin.x - lp.x) < HitRules.DmgSlotDx * k && Math.Abs(o.Origin.y - lp.y) < HitRules.DmgSlotDy * k) { clash = true; break; }
                if (!clash) break;
                if (lp.y + HitRules.DmgSlotStep * k > topFloor) break;
                lp.y += (float)(HitRules.DmgSlotStep * k);
            }
            lp.y = (float)Math.Min(lp.y, topFloor);
            TextKind kind; string colorKey, outlineKey, prefix; Frame[] anim;
            Style(cls, out kind, out colorKey, out outlineKey, out anim, out prefix);
            var t = UiKit.Text(root.App, "dmg " + cls, kind, prefix + text, colorKey);
            t.fontSharedMaterial = OutlineMaterial(t, outlineKey);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(UiKit.RefW * 0.5f, UiKit.RefH * 0.08f);
            // 가로 화면 클램프(아크가 다 흐른 뒤에도 앱 상자 안)
            float half = rt.sizeDelta.x * 0.5f * (float)pop * 0.5f;
            float pad = (float)(HitRules.DmgSidePad * k);
            float minX = root.App.rect.xMin + pad + half - (float)Math.Min(0, dx * k), maxX = root.App.rect.xMax - pad - half - (float)Math.Max(0, dx * k);
            if (minX <= maxX) lp.x = Mathf.Clamp(lp.x, minX, maxX);
            var n = new Num { Rt = rt, T = t, Anim = anim, Origin = lp, Dx = dx * k, Rise = rise * k, Pop = pop, Age = 0, Color = t.color };
            live.Add(n);
            Place(n, 0);
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
            var c = n.Color; c.a = (float)al; n.T.color = c;
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
                    if (n.Rt != null) UnityEngine.Object.Destroy(n.Rt.gameObject);
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
        }
    }
}
