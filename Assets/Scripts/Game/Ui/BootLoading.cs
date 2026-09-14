using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;
using Forge.Game.Battle;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 부팅 로딩 오버레이(T142 2회차) — 정본 `web/index.html` 21~60 의 `#boot-loading` 을 세운다.
    /// 모루 + 흔들리는 망치 + 불티 셋 · 제목 «포지 클론» · 진행 막대 · 단계 글자.
    ///
    /// 수치·색·단계는 전부 <see cref="BootLoadingSpec"/>(표 `Resources/BootLoadingUi.json`)에서 온다(§1).
    /// 진행률을 미는 쪽(부팅 절차)은 3회차가 단다 — 여기서는 <see cref="Set"/>·<see cref="Done"/> 만 연다.
    ///
    /// ⚠ 정본이 이 오버레이만 외부 CSS 를 안 쓰는 이유(«무거운 스크립트 평가 전에 그려져야 한다»)는 유니티에서
    ///   «카탈로그가 아직 안 읽혔어도 선다» 로 옮긴다 — 그래서 색을 `UiCatalog` 가 아니라 제 표에서 읽는다.
    /// </summary>
    public sealed class BootLoading : MonoBehaviour
    {
        public static BootLoading Instance { get; private set; }

        /// <summary>지금 보이는 진행률(%) · 마지막으로 갈아 끼운 글자 · 사라지는 중인가(테스트가 본다).</summary>
        public double Pct { get; private set; }
        public string Stage { get; private set; }
        public bool Fading { get; private set; }

        private BootLoadingSpec spec;
        private RectTransform fill, hammer;
        private readonly List<RectTransform> sparks = new List<RectTransform>();
        private TextMeshProUGUI stageText;
        private CanvasGroup group;
        private float ms;
        private bool following;

        private static BootLoadingSpec cached;

        /// <summary>표를 읽는다(한 번만 · 테스트가 갈아 끼울 수 있게 공개).</summary>
        public static BootLoadingSpec Spec
        {
            get
            {
                if (cached == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>("BootLoadingUi");
                    if (ta == null) throw new InvalidOperationException("Resources/BootLoadingUi.json 을 못 읽었다(T142)");
                    cached = BootLoadingSpec.From(MiniJson.ParseObject(ta.text));
                }
                return cached;
            }
        }

        public static void ResetCache() { cached = null; }

        /// <summary>오버레이를 세운다(이미 있으면 그대로). 부모는 앱 상자 — 탭바·시트보다 위다.</summary>
        public static BootLoading Ensure(Transform parent) { return Ensure(parent, false); }

        /// <summary><paramref name="follow"/> 가 참이면 매 프레임 «무엇이 섰는가» 를 읽어 스스로 진행률을 민다.
        /// 진짜 부팅(드라이버)만 참으로 세운다 — 손으로 세우는 자리(테스트·미리보기)까지 따라가면 이미 다 선 씬에서
        /// 세우자마자 100%% 로 가 사라져 버린다.</summary>
        public static BootLoading Ensure(Transform parent, bool follow)
        {
            if (Instance != null) return Instance;
            if (parent == null) return null;
            RectTransform rt = UiKit.Box(parent, "boot-loading");
            BootLoading bl = rt.gameObject.AddComponent<BootLoading>();
            bl.following = follow;
            bl.Build();
            Instance = bl;
            return bl;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Build()
        {
            spec = Spec;
            RectTransform root = (RectTransform)transform;
            root.SetAsLastSibling();
            group = gameObject.AddComponent<CanvasGroup>();

            // 바탕: 정본 radial-gradient 세 칸 → 가운데 칸 색 한 장으로 덮고 바깥 칸을 그 위에 옅게 깐다.
            // (그라디언트 재질은 T30 계열이 들어올 때 · 지금은 «어둡게 덮는다» 가 이 화면의 일이다.)
            Image bg = Face(root, "bg", Hex("bg_mid"));
            UiKit.Fill(bg.rectTransform);

            RectTransform box = UiKit.Box(root, "bl-box");
            UiKit.Anchor(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                         (float)spec.TrackWPx, (float)(spec.ForgeHPx + spec.BoxGapPx * 3 + spec.TitlePx + spec.TrackHPx + spec.StagePx));

            float y = 0f;
            // ── 모루 + 망치 + 불티 ──────────────────────────────────────────────
            RectTransform forge = UiKit.Box(box, "bl-forge");
            UiKit.Anchor(forge, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -y),
                         (float)spec.ForgeWPx, (float)spec.ForgeHPx);
            y += (float)(spec.ForgeHPx + spec.BoxGapPx);

            Image anvil = Face(forge, "bl-anvil", Hex("anvil_top"));
            UiKit.Anchor(anvil.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                         new Vector2(0f, (float)spec.AnvilBottomPx), (float)spec.AnvilWPx, (float)spec.AnvilHPx);
            Image foot = Face(forge, "bl-anvil-foot", Hex("anvil_foot"));
            UiKit.Anchor(foot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                         new Vector2(0f, (float)(spec.AnvilBottomPx - spec.AnvilFootHPx)), (float)spec.AnvilFootWPx, (float)spec.AnvilFootHPx);

            hammer = UiKit.Box(forge, "bl-hammer");
            UiKit.Anchor(hammer, new Vector2((float)spec.HammerLeftF, 0f), new Vector2((float)spec.HammerLeftF, 0f),
                         new Vector2(0f, (float)spec.HammerBottomPx), (float)spec.HammerWPx, (float)spec.HammerHPx);
            hammer.pivot = new Vector2((float)spec.HammerPivotXF, (float)spec.HammerPivotYF);
            Image head = Face(hammer, "head", Hex("hammer_head_top"));
            UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -(float)spec.HammerHeadTopPx),
                         (float)spec.HammerHeadWPx, (float)spec.HammerHeadHPx);
            Image haft = Face(hammer, "haft", Hex("hammer_haft_top"));
            UiKit.Anchor(haft.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                         new Vector2((float)spec.HammerHaftLeftPx, -(float)spec.HammerHaftTopPx),
                         (float)spec.HammerHaftWPx, (float)spec.HammerHaftHPx);

            for (int i = 0; i < spec.SparkDxPx.Length; i++)
            {
                Image sp = DotFace(forge, "bl-spark-" + (i + 1), Hex("spark"));
                UiKit.Anchor(sp.rectTransform, new Vector2((float)spec.SparkLeftF, 0f), new Vector2((float)spec.SparkLeftF, 0f),
                             new Vector2(0f, (float)spec.SparkBottomPx), (float)spec.SparkDPx, (float)spec.SparkDPx);
                sparks.Add(sp.rectTransform);
            }

            // ── 제목 ────────────────────────────────────────────────────────────
            TextMeshProUGUI title = UiKit.Text(box, "bl-title", TextKind.Title, "포지 클론", "ink", TextAlignmentOptions.Center);
            title.color = Hex("title");
            title.fontSize = (float)spec.TitlePx;
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = (float)(spec.TitleTrackEm * 100.0);   // TMP 는 1/100 em
            UiKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -y),
                         (float)spec.TrackWPx, (float)spec.TitlePx * 1.4f);
            y += (float)(spec.TitlePx * 1.4f + spec.BoxGapPx);

            // ── 진행 막대 ───────────────────────────────────────────────────────
            Color tc = Hex("track"); tc.a = (float)spec.TrackAlpha;
            Image track = RoundFace(box, "bl-track", tc, (float)spec.TrackRPx);
            UiKit.Anchor(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -y),
                         (float)spec.TrackWPx, (float)spec.TrackHPx);
            Image f = RoundFace(track.rectTransform, "bl-fill", Hex("fill_from"), (float)spec.TrackRPx);
            fill = f.rectTransform;
            UiKit.Anchor(fill, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, 0f, (float)spec.TrackHPx);
            y += (float)(spec.TrackHPx + spec.BoxGapPx);

            // ── 단계 글자 ───────────────────────────────────────────────────────
            stageText = UiKit.Text(box, "bl-stage", TextKind.Sub, string.Empty, "ink", TextAlignmentOptions.Center);
            stageText.color = Hex("stage");
            stageText.fontSize = (float)spec.StagePx;
            UiKit.Anchor(stageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -y),
                         (float)spec.TrackWPx, (float)spec.StagePx * 1.4f);

            Stage = string.Empty;
            Apply(0);
        }

        /// <summary>단색 면 — `UiKit.Panel` 과 같지만 **색을 카탈로그 키가 아니라 값으로** 받는다.
        /// ⚠ `UiKit.Panel(parent, name, null)` 은 «카탈로그를 건너뛴다» 가 아니라 `ColorOf(null)` 로 들어가
        ///   `ArgumentNullException` 이다(런 313 실측 · 이 화면의 네 테스트가 전부 그 줄에서 터졌다).</summary>
        private static Image Face(Transform parent, string name, Color c)
        {
            RectTransform rt = UiKit.Box(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        private static Image RoundFace(Transform parent, string name, Color c, float radiusPx)
        {
            Image img = Face(parent, name, c);
            img.sprite = UiShapes.Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = UiShapes.RoundedMultiplier(radiusPx);
            return img;
        }

        private static Image DotFace(Transform parent, string name, Color c)
        {
            Image img = Face(parent, name, c);
            img.sprite = UiShapes.Circle;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            return img;
        }

        private Color Hex(string key)
        {
            string hex = spec.Colors.Get(key, null);
            if (hex == null) throw new KeyNotFoundException("BootLoadingUi.json 에 색 «" + key + "» 이 없다");
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) throw new FormatException("색 «" + key + "» 의 값 «" + hex + "» 을 못 읽는다");
            return c;
        }

        /// <summary>진행률을 민다 — 글자는 그 진행률이 닿은 단계의 것으로(정본 `blSet` 은 label 이 비면 글자를 안 건드린다).</summary>
        public void Set(double pct)
        {
            Pct = pct;
            int i = spec.StageAt(pct);
            if (i >= 0 && !string.IsNullOrEmpty(spec.Stages[i].Label)) Stage = spec.Stages[i].Label;
            if (stageText != null) stageText.text = Stage;
            if (fill != null) fill.sizeDelta = new Vector2((float)spec.FillWidthPx(pct), (float)spec.TrackHPx);
        }

        /// <summary>정본 `blDone` — .4s 페이드 뒤 450ms 에 치운다.</summary>
        public void Done()
        {
            if (Fading) return;
            Fading = true;
            StartCoroutine(FadeOut());
        }

        private System.Collections.IEnumerator FadeOut()
        {
            float t = 0f, fade = (float)(spec.FadeMs / 1000.0), remove = (float)(spec.RemoveMs / 1000.0);
            while (t < remove)
            {
                t += Time.unscaledDeltaTime;
                if (group != null) group.alpha = fade <= 0f ? 0f : Mathf.Clamp01(1f - t / fade);
                yield return null;
            }
            if (this != null) Destroy(gameObject);
        }

        /// <summary>부팅이 시작될 때 한 번 — UI 뿌리가 설 때까지 기다렸다가 오버레이를 세우고, 그 뒤로는
        /// **무엇이 섰는가** 를 보고 스스로 진행률을 민다(정본은 boot() 안에서 순서대로 blSet 을 부르지만
        /// 클론은 그 일들을 서로 다른 MonoBehaviour 가 제 차례에 한다 — 그래서 부름이 아니라 신호로 읽는다).
        /// 부르는 쪽은 <see cref="MetaHost"/> 한 줄뿐이다.</summary>
        public static void Begin()
        {
            if (Instance != null || driver != null) return;
            GameObject go = new GameObject("boot-loading-driver");
            driver = go.AddComponent<BootLoadingDriver>();
        }

        private static BootLoadingDriver driver;

        /// <summary>여섯 신호 → 진행률. 신호 하나하나는 그 일을 끝낸 쪽이 스스로 세우는 것이라 여기서는 읽기만 한다.</summary>
        public void Follow()
        {
            bool save = SaveIo.Instance != null && SaveIo.Ready && SaveIo.State != null;
            bool ui = UiRoot.Instance != null && UiRoot.Instance.TabBar != null;
            bool scene = BattleScene.Instance != null;
            bool battle = BattleScene.Instance != null && BattleScene.Instance.Ready;
            bool forge = ForgeHost.Ready;
            bool meta = MetaHost.Ready;
            Set(spec.PctFromReady(save, ui, scene, battle, forge, meta));
            if (save && ui && scene && battle && forge && meta && !Fading) Done();
        }

        private void Update()
        {
            Apply(ms += Time.unscaledDeltaTime * 1000f);
            if (following && !Fading) Follow();
        }

        /// <summary>망치 각도와 불티 셋을 그 시각의 값으로 — 표가 셈을 쥔다.</summary>
        private void Apply(float atMs)
        {
            if (spec == null) return;
            if (hammer != null) hammer.localRotation = Quaternion.Euler(0f, 0f, (float)spec.SwingDeg(atMs));
            for (int i = 0; i < sparks.Count; i++)
            {
                double op, dx, dy;
                spec.SparkAt(atMs, i, out op, out dx, out dy);
                RectTransform s = sparks[i];
                if (s == null) continue;
                s.anchoredPosition = new Vector2((float)dx, (float)(spec.SparkBottomPx + dy));
                Image img = s.GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color; c.a = (float)op; img.color = c;
                }
            }
        }
    }

    /// <summary>UI 뿌리가 설 때까지 기다렸다 오버레이를 세우는 얇은 층 — 세우고 나면 스스로 사라진다.</summary>
    internal sealed class BootLoadingDriver : MonoBehaviour
    {
        private System.Collections.IEnumerator Start()
        {
            float t = 0f;
            while ((UiRoot.Instance == null || UiRoot.Instance.App == null) && t < 10f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (UiRoot.Instance != null && UiRoot.Instance.App != null) BootLoading.Ensure(UiRoot.Instance.App, true);
            Destroy(gameObject);
        }
    }
}
