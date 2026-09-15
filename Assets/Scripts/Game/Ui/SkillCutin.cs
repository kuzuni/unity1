using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T384 — 스킬 컷인 콜아웃(정본 `#skill-cutin` · `ui.js skillCutin(def)`): 스킬이 터질 때 게임 영역 위에서 20% 자리에 «아이콘 + 스킬 이름» 을 그 스킬 색으로
    /// .8초 띄운다(0→15% 배율 .3→1.25 · 30% 1 · 75%~100% 사라지며 .6rem 위로). 전투 오버레이 띠(<see cref="BattleOverlay.Layer"/>) 안에 서고 정본 z 6 —
    /// 섬광(5)·비네트(12)와의 서열은 «섬광·비네트 바로 위» 로 둔다(그 둘은 스스로 맨 아래로 간다). 수치는 전부 <see cref="SkillCutinSpec"/>(표) · 시계는 <see cref="Tick"/>.
    /// `BattleOverlay.cs` 는 다른 작업의 lock 이라 새 파일로 열었다(결정 538 의 길).
    /// </summary>
    public sealed class SkillCutin : MonoBehaviour
    {
        public const string ResourcePath = "SkillCutinUi";
        public static SkillCutin Instance { get; private set; }
        static SkillCutinSpec spec;
        public static SkillCutinSpec Spec
        {
            get
            {
                if (spec != null) return spec;
                TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T384)");
                spec = SkillCutinSpec.From(MiniJson.ParseObject(ta.text));
                return spec;
            }
        }
        public static void ResetSpec() { spec = null; }

        RectTransform root; CanvasGroup group; Image icon, iconGlow; TextMeshProUGUI label;
        double t = -1; float baseY;
        /// <summary>참이면 <see cref="Update"/> 가 시계를 안 민다 — 테스트가 <see cref="Tick"/> 으로 민다.</summary>
        public bool ManualClock;

        public bool Active { get { return t >= 0; } }
        public double Ms { get { return t < 0 ? -1 : t * 1000; } }
        public RectTransform Root { get { return root; } }
        public TextMeshProUGUI Label { get { return label; } }
        public Image Icon { get { return icon; } }
        public Image IconGlow { get { return iconGlow; } }
        public float Alpha { get { return group != null ? group.alpha : 0f; } }
        public string SkillId { get; private set; }

        /// <summary>전투 오버레이 띠 안에 한 번 세운다(오버레이가 없는 씬이면 null).</summary>
        public static SkillCutin Ensure()
        {
            if (Instance != null) return Instance;
            BattleOverlay ov = BattleOverlay.Ensure();
            if (ov == null || ov.Layer == null) return null;
            RectTransform rt = UiKit.Box(ov.Layer, "skill-cutin");
            var c = rt.gameObject.AddComponent<SkillCutin>();
            c.root = rt;
            c.group = rt.gameObject.AddComponent<CanvasGroup>();
            c.group.alpha = 0f;
            c.group.blocksRaycasts = false;   // 정본 pointer-events: none
            c.group.interactable = false;
            rt.gameObject.SetActive(false);
            Instance = c;
            return c;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>`UI.skillCutin(def)` — 아이콘·이름·색을 새로 채우고 처음부터 돈다(정본은 리플로우 강제로 애니를 재시작한다).</summary>
        public void Show(SkillDef def)
        {
            if (def == null) return;
            SkillCutinSpec sp = Spec;
            float rem = BattleOverlay.Rem;
            float font = Mathf.Max((float)(sp.FontRem * rem), UiCatalog.Instance.Kind(TextKind.Sub).min);   // 정본 1.05rem — 하한(Sub 36) 바로 위
            Color col;
            if (!ColorUtility.TryParseHtmlString(def.Color ?? string.Empty, out col)) col = UiKit.C("ink");
            SkillId = def.Id;

            if (label == null)
            {
                label = UiKit.Text(root, "label", TextKind.Sub, string.Empty, null, TextAlignmentOptions.Left);
                label.fontStyle = FontStyles.Bold;   // font-weight 800
                WrapUi.Apply(label, "skill_cutin");   // T361 2회차 — 정본 white-space 표(WrapUi.json) 1899 `#skill-cutin { nowrap }`
                label.characterSpacing = (float)(sp.LsEm * 100.0);   // "skill_cutin_ls_em" · TMP 는 1/100 em
            }
            label.fontSize = font;
            label.text = def.Name ?? def.Id;
            label.color = col;
            ApplyTextGlow(label, sp.TextBlurPx, col);
            label.ForceMeshUpdate();
            float tw = label.preferredWidth, th = label.preferredHeight;

            // 아이콘 `.ico.cutin-ico` — 1.5em 정사각 · 오른쪽 .3em 띄움 · 줄 상자 세로 가운데(margin -.36em 위아래).
            Sprite sk = UiIcons.Skill(def.Id);
            float ico = sk != null ? (float)(sp.IconEm * font) : 0f, gap = sk != null ? (float)(sp.IconGapEm * font) : 0f;
            if (sk != null)
            {
                if (icon == null)
                {
                    iconGlow = UiKit.Box(root, "ico-glow").gameObject.AddComponent<Image>();
                    iconGlow.raycastTarget = false; iconGlow.preserveAspect = true; iconGlow.type = Image.Type.Simple;
                    icon = UiKit.Box(root, "cutin-ico").gameObject.AddComponent<Image>();
                    icon.raycastTarget = false; icon.preserveAspect = true; icon.type = Image.Type.Simple;
                }
                icon.sprite = sk;
                icon.color = Color.white;
                iconGlow.sprite = IconGlowSprite(sk, sp.IconBlurPx, ico);
                iconGlow.color = col;   // drop-shadow(0 0 6px currentColor)
                icon.gameObject.SetActive(true); iconGlow.gameObject.SetActive(true);
            }
            else if (icon != null) { icon.gameObject.SetActive(false); iconGlow.gameObject.SetActive(false); }

            float w = ico + gap + tw, h = Mathf.Max(th, ico);
            float layerH = ((RectTransform)root.parent).rect.height;
            // 정본 `top: 20%; left: 50%; translateX(-50%)` · 배율 축은 상자 가운데 → 피벗 가운데 · 위끝이 띠의 20%.
            baseY = -(float)(sp.TopF * layerH) - h * 0.5f;
            UiKit.Anchor(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, baseY), w, h);
            if (sk != null)
            {
                UiKit.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, ico, ico);
                UiKit.Anchor(iconGlow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, ico, ico);
            }
            UiKit.Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(ico + gap, 0f), tw, th);

            Reorder();
            t = 0;
            root.gameObject.SetActive(true);
            Apply();
        }

        /// <summary>정본 z 6 — 섬광(5)·비네트(12)는 스스로 맨 아래(0)로 가므로 그 둘 바로 위에 선다 · 보스 워닝·사망·씬컷(13~16)은 그 뒤에 서서 위가 된다.</summary>
        void Reorder()
        {
            Transform p = root.parent;
            int idx = 0;
            for (int i = 0; i < p.childCount; i++)
            {
                Transform c = p.GetChild(i);
                if (c == root) continue;
                if (c.name == "skill-flash" || c.name == "dmg-flash") idx = Mathf.Max(idx, c.GetSiblingIndex() + 1);
            }
            root.SetSiblingIndex(Mathf.Min(idx, p.childCount - 1));
        }

        /// <summary>정본 `text-shadow: 0 0 10px currentColor` — TMP 언더레이 한 겹(식은 T333 의 <see cref="UnderlaySdf"/> · 표는 이 작업 것).</summary>
        static void ApplyTextGlow(TextMeshProUGUI tm, double blurCssPx, Color col)
        {
            Material m = tm.fontMaterial;
            if (!m.HasProperty("_UnderlayColor")) return;
            float g = m.HasProperty("_GradientScale") ? m.GetFloat("_GradientScale") : 0f;
            float rc = m.HasProperty("_ScaleRatioC") ? m.GetFloat("_ScaleRatioC") : 0f;
            float ps = tm.font != null ? (float)tm.font.faceInfo.pointSize : 0f;
            if (g <= 0f) g = 10f;
            if (ps <= 0f) ps = 90f;
            if (rc <= 0f) rc = 1f;
            UnderlaySdf u = UnderlaySdf.FromPx(0, 0, blurCssPx * KeylineUi.CssPx, tm.fontSize, g, rc, ps);
            m.EnableKeyword("UNDERLAY_ON");
            m.SetColor("_UnderlayColor", col);
            m.SetFloat("_UnderlayOffsetX", (float)u.OffsetX01);
            m.SetFloat("_UnderlayOffsetY", (float)u.OffsetY01);
            m.SetFloat("_UnderlayDilate", 0f);
            m.SetFloat("_UnderlaySoftness", (float)u.Softness01);
        }

        /// <summary>정본 `filter: drop-shadow(0 0 6px currentColor)` — 아이콘을 번지게 구워 같은 자리 뒤에 스킬 색으로 깐다(T332 `DropShadow` 와 같은 길 · 표는 이 작업 것).</summary>
        static Sprite IconGlowSprite(Sprite sk, double blurCssPx, float displayPx)
        {
            double sigmaCanvas = blurCssPx * 0.5 * KeylineUi.CssPx;   // 반지름 → σ
            double sigmaBaked = FilterRules.BakeSigmaPx(sigmaCanvas, sk.textureRect.height, displayPx);
            return sigmaBaked > 0 ? UiFilter.Blur(sk, sigmaBaked, "cutin-glow-" + Mathf.RoundToInt((float)(sigmaBaked * 100)), displayPx) : sk;
        }

        void Update() { if (!ManualClock) Tick(Time.unscaledDeltaTime); }

        /// <summary>시계를 <paramref name="dt"/> 초 민다 — 끝나면 숨는다(`forwards` 의 마지막 키 = 투명).</summary>
        public void Tick(float dt)
        {
            if (t < 0) return;
            t += dt;
            if (Spec.Done(t * 1000))
            {
                t = -1;
                group.alpha = 0f;
                root.gameObject.SetActive(false);
                return;
            }
            Apply();
        }

        void Apply()
        {
            double a, s, rise;
            Spec.Sample(t * 1000, out a, out s, out rise);
            group.alpha = (float)a;
            root.localScale = new Vector3((float)s, (float)s, 1f);
            root.anchoredPosition = new Vector2(0f, baseY + (float)(rise * BattleOverlay.Rem));   // translateY(-.6rem) = 위로
        }
    }
}
