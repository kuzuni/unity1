using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Ui;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T124 — 시대 무늬 층. 1회차는 층을 직접 깐다(세 자리에 거는 줄은 T87 lock 뒤): 다섯 시대의 타일 픽셀이 서로 다르고 · 앞 다섯 시대는 층이 없고 ·
    /// 한 주기 뒤 uv 가 첫 프레임과 같으며(이음매 0) · 반짝임이 표 범위 안이고 · 양자 링이 위상을 따라 서고 · 장착 셀 흐림 .55 · 자동 제련 마스크가 정점 알파로 걸린다.
    /// </summary>
    public class AgePatternTests
    {
        static IEnumerator Boot()
        {
            try { if (System.IO.File.Exists(SaveIo.SavePath)) System.IO.File.Delete(SaveIo.SavePath); } catch (System.Exception) { }
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(ForgeHost.Ready && MetaHost.Ready && UiRoot.Instance != null) && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(ForgeHost.Ready, "ForgeHost 가 20초 안에 준비되지 않았다");
            yield return null;
        }

        /// <summary>앞 테스트가 던져 남긴 상자를 걷는다 — 남은 층이 캔버스 재구성에서 던지면 뒤 테스트가 전부 빨개진다(런 243·245).</summary>
        static void Sweep()
        {
            if (UiRoot.Instance == null) return;
            var gone = new List<GameObject>();
            foreach (Transform c in UiRoot.Instance.App) if (c.name.StartsWith("t124-")) gone.Add(c.gameObject);
            foreach (GameObject g in gone) Object.DestroyImmediate(g);
        }

        static RectTransform Host(string name, float w, float h)
        {
            RectTransform host = UiKit.Box(UiRoot.Instance.App, name);
            UiKit.Place(host, 40f, 300f, w, h);
            // 바탕 채움은 실제 막대·셀처럼 **자식** 하나(ForgeUi 의 fill 자리) — 층은 그 바로 위(형제 1)에 선다(런 246: 자기 자신에 Image 를 달았더니 층이 0 번이 됐다)
            Image bg = UiKit.Box(host, "bg").gameObject.AddComponent<Image>(); bg.color = Color.gray;
            return host;
        }

        static string Hash(Texture2D t)
        {
            Color32[] px = t.GetPixels32();
            unchecked { uint hsh = 2166136261; foreach (Color32 c in px) { hsh = (hsh ^ c.a) * 16777619; } return t.width + "x" + t.height + ":" + hsh; }
        }

        [UnityTest]
        public IEnumerator 다섯_시대의_무늬가_서로_다르고_앞_다섯_시대는_층이_없다()
        {
            yield return Boot();
            Sweep();
            string[] plain = { "primitive", "medieval", "earlyModern", "modern", "space" };
            string[] patterned = { "interstellar", "multiverse", "quantum", "underworld", "divine" };
            RectTransform host = Host("t124-host", 600f, 80f);
            int before = host.childCount;
            foreach (string a in plain) { Assert.IsNull(AgePattern.Attach(host, a), a + " 는 민무늬"); Assert.AreEqual(before, host.childCount, a + " 는 아무것도 안 깐다"); }
            var hashes = new HashSet<string>();
            var made = new List<AgePattern>();
            foreach (string a in patterned)
            {
                AgePattern p = AgePattern.Attach(host, a);
                Assert.IsNotNull(p, a);
                made.Add(p);
                Assert.AreEqual("age-pattern", p.Layer.name);
                Assert.IsNotNull(p.Layer.GetComponent<RectMask2D>(), "정본 overflow:hidden");
                foreach (var g in p.Layers) Assert.IsNotNull(g.GetComponent<CanvasRenderer>(), a + " 층에 CanvasRenderer(런 243 의 구멍)");
                if (p.Rings != null) Assert.IsNotNull(p.Rings.GetComponent<CanvasRenderer>(), "링 메시에 CanvasRenderer");
                if (a == "quantum") { Assert.IsNotNull(p.Rings, "양자는 링 메시"); Assert.AreEqual(0, p.Layers.Length); }
                else
                {
                    Assert.Greater(p.Layers.Length, 0, a + " 타일 층");
                    for (int i = 0; i < p.Layers.Length; i++)
                    {
                        Texture2D t = p.Layers[i].texture as Texture2D;
                        Assert.IsNotNull(t, a + " 층 " + i + " 타일");
                        Assert.AreEqual(TextureWrapMode.Repeat, t.wrapMode, "타일은 반복 래핑");
                        Assert.IsTrue(hashes.Add(Hash(t)), a + " 층 " + i + " 의 픽셀이 다른 무늬와 같다");
                        Assert.Greater(AgePatternRules.Coverage(ToCoverage(t)), 0.01, a + " 층 " + i + " 에 실제로 무늬가 깔린다");
                    }
                }
            }
            yield return null;
            foreach (AgePattern p in made) Assert.Greater(p.Group.alpha, 0f);
            Object.DestroyImmediate(host.gameObject);
            Sweep();
        }

        static float[] ToCoverage(Texture2D t) { Color32[] px = t.GetPixels32(); var f = new float[px.Length]; for (int i = 0; i < px.Length; i++) f[i] = px[i].a / 255f; return f; }

        [UnityTest]
        public IEnumerator 한_주기_뒤_첫_프레임과_같고_반짝임은_표_범위_안이며_양자_링은_위상을_따른다()
        {
            yield return Boot();
            Sweep();
            RectTransform host = Host("t124-cycle", 600f, 80f);
            AgePatternSpec s = AgePattern.Spec;
            foreach (string a in new[] { "interstellar", "multiverse", "underworld", "divine" })
            {
                AgePattern p = AgePattern.Attach(host, a);
                p.Manual = true;
                p.Tick(0);
                var uv0 = new List<Rect>(); foreach (var g in p.Layers) uv0.Add(g.uvRect);
                p.Tick(p.A.MoveS * 1000.0 * 0.37);
                for (int i = 0; i < p.Layers.Length; i++) Assert.IsTrue(uv0[i].position != p.Layers[i].uvRect.position || p.A.StepsN > 0, a + " 층 " + i + " 이 움직인다");
                p.Tick(p.A.MoveS * 1000.0 * 0.63);   // 합쳐 정확히 한 주기
                for (int i = 0; i < p.Layers.Length; i++)
                {
                    Rect u1 = p.Layers[i].uvRect;
                    float dx = Mathf.Repeat(u1.x - uv0[i].x, 1f), dy = Mathf.Repeat(u1.y - uv0[i].y, 1f);
                    Assert.IsTrue(dx < 1e-3f || dx > 1f - 1e-3f, a + " 층 " + i + " x: 한 주기 = 타일 정수 칸 (" + dx + ")");
                    Assert.IsTrue(dy < 1e-3f || dy > 1f - 1e-3f, a + " 층 " + i + " y: 한 주기 = 타일 정수 칸 (" + dy + ")");
                }
                if (p.A.Pulse != null)
                {
                    float lo = 1f, hi = 0f;
                    for (int k = 0; k < 40; k++) { p.Tick(p.A.Pulse.DurS * 2000.0 / 40); lo = Mathf.Min(lo, p.Group.alpha); hi = Mathf.Max(hi, p.Group.alpha); }
                    Assert.GreaterOrEqual(lo, (float)p.A.Pulse.From - 1e-3f, a + " 반짝임 아래 끝"); Assert.LessOrEqual(hi, (float)p.A.Pulse.To + 1e-3f, a + " 반짝임 위 끝");
                    Assert.Less(lo, hi, a + " 밝기가 실제로 오간다");
                }
                else Assert.AreEqual(1f, p.Group.alpha, 1e-6f, a + " 는 밝기 고정");
            }
            AgePattern q = AgePattern.Attach(host, "quantum");
            q.Manual = true; q.Tick(0);
            Assert.AreEqual(0f, q.Rings.PhaseRem, 1e-6f);
            yield return null;   // 메시가 한 번 선다
            int rings0 = q.Rings.RingCount;
            Assert.Greater(rings0, 3, "막대 안에 링이 여럿");
            q.Tick(1400); yield return null;
            Assert.AreEqual(0.42f, q.Rings.PhaseRem, 1e-4f, "1.4초 = 위상 반 주기");
            q.Tick(1400); yield return null;
            Assert.AreEqual(0f, q.Rings.PhaseRem, 1e-4f, "2.8초 = 위상 0 (같은 그림)");
            Assert.AreEqual(rings0, q.Rings.RingCount);
            Assert.AreEqual(1f, q.Group.alpha, 1e-6f);
            Object.DestroyImmediate(host.gameObject);
            Sweep();
        }

        [UnityTest]
        public IEnumerator 장착_셀은_흐림_55_이고_자동_제련_막대는_왼쪽_30에서_50_마스크가_정점_알파로_걸린다()
        {
            yield return Boot();
            Sweep();
            RectTransform host = Host("t124-cell", 120f, 120f);
            AgePatternSpec s = AgePattern.Spec;
            AgePattern cell = AgePattern.Attach(host, "divine", cell: true);
            cell.Manual = true; cell.Tick(0);
            Assert.AreEqual((float)s.CellOpacity * (float)AgePatternRules.Pulse(s, cell.A, 0), cell.Group.alpha, 1e-5f, "filter: opacity(.55) × 반짝임");
            Assert.AreEqual(1, cell.Layer.GetSiblingIndex(), "바탕 채움 바로 위(썸네일·글자 뒤)");
            RectTransform bar = Host("t124-bar", 600f, 60f);
            AgePattern m = AgePattern.Attach(bar, "underworld", cell: false, mask: true);
            m.Manual = true; m.Tick(0);
            Assert.IsTrue(m.Layers[0].Masked, "마스크 갈래");
            // 마스크는 정점 알파 0·0·1·1 (x 0 · 30% · 50% · 100%) — AgePatternGraphic.OnPopulateMesh 가 건다
            yield return null;
            Canvas.ForceUpdateCanvases();
            var cr = m.Layers[0].GetComponent<CanvasRenderer>();
            Assert.IsNotNull(cr);
            Assert.AreEqual(1f, m.Group.alpha / (float)AgePatternRules.Pulse(s, m.A, 0), 1e-4f, "막대는 흐림 1");
            Object.DestroyImmediate(host.gameObject); Object.DestroyImmediate(bar.gameObject);
            Sweep();
        }

        static readonly string[] PatternedAges = { "interstellar", "multiverse", "quantum", "underworld", "divine" };

        [UnityTest]
        public IEnumerator 확률_정보_막대와_자동_제련_막대와_장비_시트_칸에_무늬_층이_걸린다()
        {
            yield return Boot();
            Sweep();
            ForgeHost h = ForgeHost.Instance;
            Assert.IsNotNull(h);
            // ⓐ 확률 정보(.fi-age-bar) — 열 시대 막대 전부: 항성간 이상 다섯만 층 · 마스크 없음
            ForgeInfoPopup.Open(h);
            yield return null; yield return null;
            Popup info = h.Meta.Popups.Find(ForgeInfoPopup.Name);
            Assert.IsNotNull(info, "확률 정보 팝업");
            int seen = 0;
            foreach (string age in h.Defs.Ages)
            {
                RectTransform bar = FindDeep(info.Root, "age-" + age);
                if (bar == null) continue;
                seen++;
                Transform layer = bar.Find("age-pattern");
                if (System.Array.IndexOf(PatternedAges, age) >= 0)
                {
                    Assert.IsNotNull(layer, age + " 막대에 무늬 층");
                    Assert.AreEqual(1, layer.GetSiblingIndex(), age + " 층은 바탕 채움 바로 위(글자·체크 뒤)");
                    AgePattern p = layer.GetComponent<AgePattern>();
                    Assert.AreEqual(1f, p.BaseOpacity, 1e-6f, "막대는 흐림 1");
                    foreach (var g in p.Layers) Assert.IsFalse(g.Masked, age + " 확률 정보 막대에는 마스크가 없다");
                }
                else Assert.IsNull(layer, age + " 는 정본에 무늬가 없다(민무늬)");
            }
            Assert.GreaterOrEqual(seen, 10, "열 시대 막대");
            h.Meta.Popups.HideAll();
            yield return null;
            // ⓑ 자동 제련(.af-age-bar) — 왼쪽 30→50% 마스크
            h.Pull();
            h.Engine.AutoForgeConfig().FilterOn = false;
            h.Push();
            ForgeAutoPopup.Open(h);
            yield return null; yield return null;
            Popup auto = h.Meta.Popups.Find(ForgeAutoPopup.Name);
            Assert.IsNotNull(auto, "자동 제련 팝업");
            int masked = 0;
            foreach (string age in PatternedAges)
            {
                RectTransform bar = FindDeep(auto.Root, "af-age-" + age);
                if (bar == null) continue;   // 확률 0 인 시대는 행이 없다(정본과 같다)
                Transform layer = bar.Find("age-pattern");
                Assert.IsNotNull(layer, age + " 자동 제련 막대에 무늬 층");
                AgePattern p = layer.GetComponent<AgePattern>();
                if (p.Layers.Length > 0) { Assert.IsTrue(p.Layers[0].Masked, age + " 자동 제련 막대는 마스크"); masked++; }
                else Assert.IsNotNull(p.Rings, "양자 링");
            }
            foreach (string age in h.Defs.Ages)
            {
                if (System.Array.IndexOf(PatternedAges, age) >= 0) continue;
                RectTransform bar = FindDeep(auto.Root, "af-age-" + age);
                if (bar != null) Assert.IsNull(bar.Find("age-pattern"), age + " 는 민무늬");
            }
            h.Meta.Popups.HideAll();
            yield return null;
            // ⓒ 장비 시트 칸(.equip-cell) — 낀 장비의 시대가 다섯 안이면 흐림 .55 층 · 아니면 없음
            RectTransform sheet = UiRoot.Instance.Sheet;
            Assert.IsNotNull(sheet);
            int cells = 0;
            foreach (RectTransform c in sheet.GetComponentsInChildren<RectTransform>(true))
            {
                if (!c.name.StartsWith("cell-")) continue;
                cells++;
                var it = h.Gear.Get(c.name.Substring(5));
                Transform layer = c.Find("age-pattern");
                if (it != null && AgePattern.Has(it.Age))
                {
                    Assert.IsNotNull(layer, c.name + " 에 무늬 층(" + it.Age + ")");
                    Assert.AreEqual((float)AgePattern.Spec.CellOpacity, layer.GetComponent<AgePattern>().BaseOpacity, 1e-6f, "장착 셀은 filter: opacity(.55)");
                    Assert.AreEqual(1, layer.GetSiblingIndex(), "썸네일 뒤(형제 1)");
                }
                else Assert.IsNull(layer, c.name + " 은 무늬 없음");
            }
            Assert.Greater(cells, 0, "장비 시트 칸");
        }

        static RectTransform FindDeep(Transform root, string name)
        {
            foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true)) if (r.name == name) return r;
            return null;
        }
    }
}
