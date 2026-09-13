using System;
using System.Collections.Generic;
using TMPro;
using Forge.Core.Data;
using Forge.Core.Ui;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>글자 종류 — 크기·하한은 카탈로그(<c>Assets/Forge/catalog.json</c> textKinds)가 쥔다. 하한: 본문 40 · 버튼 44 · 보조 36 · 제목 60(ROUTINE §1).</summary>
    public enum TextKind { Title, Button, Body, Sub }

    /// <summary>
    /// UI 조각 공장(ROUTINE T18). 글자는 반드시 여기서(<see cref="Text"/>) 만든다 — fontSize·색을 직접 주지 않는다.
    /// 좌표는 «기준 캔버스»(카탈로그 reference 1080×1920 · 원작 rem 스케일과 같은 뜻) 단위이고, 앱 상자(<see cref="UiRoot"/>)가 통째로 스케일된다.
    /// </summary>
    public static class UiKit
    {
        private static UiCatalog Cat { get { return UiCatalog.Instance; } }

        public static float RefW { get { return Cat.RefW; } }
        public static float RefH { get { return Cat.RefH; } }

        /// <summary>배치 값(분수). 폭 기준이면 <see cref="W"/>, 높이 기준이면 <see cref="H"/> 로 기준 px 를 얻는다.</summary>
        public static float L(string key) { return Cat.Layout(key); }
        public static float W(string key) { return Cat.Layout(key) * RefW; }
        public static float H(string key) { return Cat.Layout(key) * RefH; }
        public static Color C(string key) { return Cat.ColorOf(key); }

        /// <summary>
        /// 검정 덮개(딤)의 지각 α — 정본 css `rgba(0,0,0,.5)`(주인 지시 «투명도 50%») 는 브라우저가 sRGB 공간에서 섞어 흰 바탕이 127 이 되는데,
        /// 이 프로젝트는 선형 색 공간이라 캔버스가 선형에서 섞어 같은 α .5 로 흰 바탕이 188 이 된다(런 129 실측 187 · T79).
        /// «같은 sRGB 결과» 가 나오도록 α 를 환산한다: 남길 밝기 (1−α) 를 선형으로 옮긴 만큼만 남긴다. 감마 공간이면 그대로. 검정 덮개에만 쓴다.
        /// </summary>
        public static Color PerceivedDim(Color dim)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) return dim;
            float keep = Mathf.GammaToLinearSpace(1f - dim.a);
            return new Color(dim.r, dim.g, dim.b, 1f - keep);
        }

        // ---- 사각형 ----

        public static RectTransform Box(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            Fill(rt);
            return rt;
        }

        /// <summary>부모를 꽉 채운다.</summary>
        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>부모 높이의 <paramref name="topFrac"/>~<paramref name="bottomFrac"/>(위에서부터 분수) 가로 띠.</summary>
        public static void Band(RectTransform rt, float topFrac, float bottomFrac)
        {
            rt.anchorMin = new Vector2(0f, 1f - bottomFrac);
            rt.anchorMax = new Vector2(1f, 1f - topFrac);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>부모 왼쪽 위를 원점으로 (x, yTop) 에 w×h 상자(기준 px).</summary>
        public static void Place(RectTransform rt, float x, float yTop, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yTop);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>부모의 (ax, ay) 앵커에 피벗을 맞춰 w×h 상자(기준 px) — 가운데·오른쪽 정렬용.</summary>
        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 offset, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(w, h);
        }

        // ---- 면 ----

        /// <summary>단색 면(부모 꽉 채움 · 입력 안 받음).</summary>
        public static Image Panel(Transform parent, string name, string colorKey)
        {
            RectTransform rt = Box(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = C(colorKey);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>둥근 단색 면(알약·카드). 반지름은 기준 px.</summary>
        public static Image Rounded(Transform parent, string name, string colorKey, float radiusPx)
        {
            Image img = Panel(parent, name, colorKey);
            img.sprite = UiShapes.Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = UiShapes.RoundedMultiplier(radiusPx);
            return img;
        }

        /// <summary>원판.</summary>
        public static Image Circle(Transform parent, string name, string colorKey)
        {
            Image img = Panel(parent, name, colorKey);
            img.sprite = UiShapes.Circle;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            return img;
        }

        /// <summary>부모 위(또는 아래) 가장자리에 붙는 가로 키라인. 두께는 기준 px.</summary>
        public static Image Line(Transform parent, string name, string colorKey, float px, bool atTop)
        {
            Image img = Panel(parent, name, colorKey);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, atTop ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, atTop ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, atTop ? 1f : 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, px);
            return img;
        }

        /// <summary>아이콘. 원작 IconGen 키면 아틀라스(T31 <see cref="UiIcons"/> · tint 는 정본 <c>{ tint }</c> 옵션)에서, 아니면 카탈로그 스프라이트(GUI PRO Kit 조각)에서. 비율 유지.</summary>
        /// <summary>
        /// 이모지가 섞인 문구를 «아이콘 + 글자» 한 줄로 세운다(ROUTINE T89 · 정본 `UI.paintIconText`).
        /// 표(<see cref="UiText"/>)에 있는 이모지는 T31 아틀라스의 아이콘으로, 나머지는 글자 그대로 — 닉네임·장비명은 손대지 않는다.
        /// 아이콘 한 칸은 **글자 크기의 정사각**이라 줄 높이가 글자와 같다. 표에 걸리는 이모지가 하나도 없으면
        /// 조각 하나뿐이므로 <see cref="Text"/> 와 같은 모양(라벨 하나)이 나온다 — 호출부 계약이 안 바뀐다.
        /// </summary>
        /// <returns>줄 상자(가로 레이아웃). 글자 조각은 <c>"msg"</c>·<c>"msg-2"</c>… · 아이콘은 <c>"ico-N"</c>.</returns>
        public static RectTransform IconTextRow(Transform parent, string name, TextKind kind, string msg, string colorKey = null,
                                                TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            RectTransform row = Box(parent, name);
            var lay = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            lay.childAlignment = RowAnchor(align);
            lay.childControlWidth = true; lay.childControlHeight = true;
            lay.childForceExpandWidth = false; lay.childForceExpandHeight = false;
            lay.spacing = 0f;

            float size = Cat.Kind(kind).size;
            int texts = 0, icons = 0;
            foreach (Forge.Core.Ui.IconRun r in UiText.Split(msg))
            {
                if (r.IsIcon)
                {
                    Image ico = Icon(row, "ico-" + (++icons), r.Icon);
                    var le = ico.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = le.preferredHeight = size;
                    le.flexibleWidth = 0f;
                    continue;
                }
                // 글자 조각은 크기를 안 준다 — TextMeshProUGUI 가 ILayoutElement 라 가로 레이아웃이 제 폭을 물어 본다
                // (폭을 손으로 재려면 TMP_Text.GetPreferredValues 를 불러야 하는데, 하니스 스텁에 그 서명이 없어
                //  «스텁에 없는 서명을 추측해 넣지 않는다»(§1)는 규칙에 걸린다).
                Text(row, texts == 0 ? "msg" : "msg-" + (texts + 1), kind, r.Text, colorKey, align);
                texts++;
            }
            return row;
        }

        /// <summary>글자 정렬 → 줄 정렬(왼쪽·오른쪽 라벨이 아이콘 줄로 바뀌어도 자리가 안 움직이게 · T89).</summary>
        private static TextAnchor RowAnchor(TextAlignmentOptions a)
        {
            string k = a.ToString();
            if (k.IndexOf("Left", StringComparison.Ordinal) >= 0) return TextAnchor.MiddleLeft;
            if (k.IndexOf("Right", StringComparison.Ordinal) >= 0) return TextAnchor.MiddleRight;
            return TextAnchor.MiddleCenter;
        }

        /// <summary>줄 안의 글자 조각들(굵게·외곽선처럼 조각마다 걸어야 하는 것 · T89).</summary>
        public static TextMeshProUGUI[] RowTexts(RectTransform row) { return row.GetComponentsInChildren<TextMeshProUGUI>(true); }

        public static Image Icon(Transform parent, string name, string spriteKey, string tint = null)
        {
            RectTransform rt = Box(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            Sprite original = UiIcons.Get(spriteKey, tint);
            img.sprite = original != null ? original : Cat.SpriteOf(spriteKey);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        // ---- 글자 ----

        /// <summary>글자. 크기는 종류에서 · 색은 카탈로그 키에서(비우면 «ink»). 줄바꿈 없음 · 넘쳐도 자르지 않는다(원작 nowrap 알약과 같다).</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, TextKind kind, string text, string colorKey = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            RectTransform rt = Box(parent, name);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = UiFont.Primary;
            t.fontSize = Cat.Kind(kind).size;
            t.color = C(colorKey ?? "ink");
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = false;
            t.raycastTarget = false;
            t.text = text ?? string.Empty;
            rt.gameObject.AddComponent<UiTextKindTag>().Kind = kind;
            return t;
        }

        /// <summary>글자 외곽선(원작 stage-label 의 검정 text-shadow 테). 재질 인스턴스에 OUTLINE_ON 을 켠다.
        /// ⚠ <paramref name="width01"/> 은 TMP 의 SDF 여백 비율이라 글자가 작을수록 얇고, 띠의 절반이 채움을 먹는다(T104) —
        /// 정본 `-webkit-text-stroke` 를 옮기는 자리는 <see cref="OutlinePx"/> 로(카탈로그 px 키 · 2회차가 호출부를 옮긴다).</summary>
        public static void Outline(TextMeshProUGUI t, string colorKey, float width01)
        {
            Material m = t.fontMaterial;
            m.EnableKeyword("OUTLINE_ON");
            t.outlineColor = C(colorKey);
            t.outlineWidth = width01;
        }

        /// <summary>
        /// T104 — 정본 `-webkit-text-stroke: N` + `paint-order: stroke fill` 과 같은 그림: **바깥으로 N/2** 의 키라인 · 채움은 그대로.
        /// <paramref name="strokePx"/> 는 기준 캔버스 px(카탈로그 값 · CSS px 환산은 호출자). 식은 <see cref="OutlineSdf"/>(모바일 SDF 셰이더) —
        /// 재질의 `_GradientScale`·`_ScaleRatioA` 와 폰트 애셋의 샘플링 크기·<c>t.fontSize</c> 를 **그 순간** 읽으니 글자 크기를 정한 뒤에 부른다.
        /// 여백이 모자라면 1 로 잘리고 반환값의 <see cref="OutlineSdf.Clipped"/> 가 선다(경고 한 줄).
        /// </summary>
        public static OutlineSdf OutlinePx(TextMeshProUGUI t, string colorKey, float strokePx)
        {
            Material m = t.fontMaterial;
            float g = m.HasProperty("_GradientScale") ? m.GetFloat("_GradientScale") : 0f;
            float r = m.HasProperty("_ScaleRatioA") ? m.GetFloat("_ScaleRatioA") : 0f;
            float ps = t.font != null ? (float)t.font.faceInfo.pointSize : 0f;
            if (g <= 0f || r <= 0f || ps <= 0f)
            {
                // SDF 재질이 아니거나 애셋 정보가 비었다 — CreateFontAsset(Font) 기본(패딩 9 · 90pt → G 10 · R .9)으로 잇는다(엔진 기본값이지 게임 수치가 아니다)
                Debug.LogWarning("[UiKit.OutlinePx] " + t.name + ": 재질/폰트 값이 비어(G=" + g + " R=" + r + " pt=" + ps + ") TMP 기본값으로 환산한다");
                if (g <= 0f) g = 10f;
                if (r <= 0f) r = 0.9f;
                if (ps <= 0f) ps = 90f;
            }
            OutlineSdf o = OutlineSdf.FromStroke(strokePx, t.fontSize, g, r, ps);
            if (o.Clipped) Debug.LogWarning("[UiKit.OutlinePx] " + t.name + ": 획 " + strokePx + "px 는 이 글자(" + t.fontSize + "px)의 SDF 여백을 넘는다 — 보이는 띠 " + o.VisiblePx.ToString("0.00") + "px(원한 " + o.WantedPx.ToString("0.00") + ")");
            m.EnableKeyword("OUTLINE_ON");
            // 순서: _FaceDilate 를 먼저 — outlineWidth 세터가 메시 여백(m_padding)을 재계산하며 그때 재질의 dilate 를 읽는다
            m.SetFloat("_FaceDilate", (float)o.Dilate);
            t.outlineColor = C(colorKey);
            t.outlineWidth = (float)o.Width01;
            return o;
        }

        // ---- 버튼 ----

        /// <summary>투명 히트 영역 + Button. 겉모습(아이콘·글자)은 호출자가 자식으로 넣는다.</summary>
        public static Button Button(Transform parent, string name, UnityAction onClick)
        {
            RectTransform rt = Box(parent, name);
            Image hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            Button b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }
    }

    /// <summary>
    /// TMP 글꼴 — 카탈로그 글꼴(<c>Assets/Fonts/NotoSansKR-Forge.ttf</c> · 한글 1266 자 + 라틴·기호 서브셋 · 302KB)을
    /// 런타임 폰트 애셋으로 만든다(에디터 없이 굽는 길). 이 파일에는 한글 cmap 이 있으므로 리눅스 CI·WebGL 에서도 한글이 선다.
    /// OS 글꼴 폴백은 그대로 남겨 둔다 — 서브셋에 없는 글자(원작에 없던 한글·한자 등)를 폰이 가진 글꼴로 메운다.
    /// </summary>
    public static class UiFont
    {
        /// <summary>굽기 값 표(T121) — 샘플링 pt · SDF 패딩 px · 아틀라스 크기. 수치는 코드에 안 박는다(§1).</summary>
        public const string BakeResource = "UiFontBake";
        private static TMP_FontAsset primary;

        /// <summary>샘플링 포인트 크기(표 `sampling_pt`) — 1 텍셀 = 글자px / 이 값.</summary>
        public static int SamplingPt { get; private set; }
        /// <summary>SDF 패딩 px(표 `padding_px`) — 재질 `_GradientScale` = 이 값 + 1 · 낼 수 있는 최대 바깥 띠 = 패딩 × 글자px / 샘플링.</summary>
        public static int PaddingPx { get; private set; }
        /// <summary>아틀라스 한 장 크기(표 `atlas_w`·`atlas_h`) — 다중 아틀라스라 넘치면 장이 는다.</summary>
        public static int AtlasW { get; private set; }
        public static int AtlasH { get; private set; }

        /// <summary>표를 읽는다(한 번). 값이 비거나 0 이면 던진다 — TMP 기본으로 조용히 잇지 않는다(T121 의 병이 그 기본값이다).</summary>
        public static void LoadBake()
        {
            if (SamplingPt > 0) return;
            TextAsset ta = Resources.Load<TextAsset>(BakeResource);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + BakeResource + ".json 이 없다 (T121)");
            JsonObject o = MiniJson.ParseObject(ta.text);
            int sp = J.Int(o["sampling_pt"]), pad = J.Int(o["padding_px"]), w = J.Int(o["atlas_w"]), h = J.Int(o["atlas_h"]);
            if (sp <= 0 || pad <= 0 || w <= 0 || h <= 0)
                throw new System.InvalidOperationException(BakeResource + ".json 값이 비었다: sampling_pt=" + sp + " padding_px=" + pad + " atlas=" + w + "×" + h);
            SamplingPt = sp; PaddingPx = pad; AtlasW = w; AtlasH = h;
        }

        /// <summary>붙은 OS 폴백 글꼴 이름(없으면 null).</summary>
        public static string HangulFallback { get; private set; }

        public static TMP_FontAsset Primary
        {
            get
            {
                if (primary == null) primary = Build();
                return primary;
            }
        }

        private static TMP_FontAsset Build()
        {
            UiCatalog cat = UiCatalog.Instance;
            if (cat.font == null) throw new System.InvalidOperationException("UiCatalog.font 이 비었다 — Assets/Fonts/NotoSansKR-Forge.ttf 참조 (gen_ui_catalog.py)");
            LoadBake();
            // T121 — 기본 굽기(90pt · 패딩 9)는 정본 최대 키라인(.2em = 바깥 .1em)에서 W 가 정확히 1 이라 링이 «면» 이 됐다. 패딩을 표대로 넓혀 굽는다.
            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(cat.font, SamplingPt, PaddingPx, GlyphRenderMode.SDFAA, AtlasW, AtlasH, AtlasPopulationMode.Dynamic, true);
            if (fa == null) throw new System.InvalidOperationException("카탈로그 글꼴로 TMP 폰트 애셋을 못 만들었다");
            fa.name = cat.font.name + " (runtime)";
            Shader shader = ShipShader();
            if (shader != null && fa.material != null) fa.material.shader = shader;
            if (fa.fallbackFontAssetTable == null) fa.fallbackFontAssetTable = new List<TMP_FontAsset>();

            HashSet<string> installed = new HashSet<string>(Font.GetOSInstalledFontNames());
            foreach (string family in cat.FallbackOsFonts)
            {
                if (!installed.Contains(family)) continue;
                TMP_FontAsset fb = TMP_FontAsset.CreateFontAsset(family, "Regular");
                if (fb == null) continue;
                fb.name = family + " (OS fallback)";
                if (shader != null && fb.material != null) fb.material.shader = shader;
                fa.fallbackFontAssetTable.Add(fb);
                HangulFallback = family;
                break;
            }
            if (HangulFallback == null)
                Debug.Log("[UiFont] OS 한글 폴백 없음 — 카탈로그 글꼴이 한글을 직접 쥔다(NotoSansKR-Forge · T53)");
            return fa;
        }

        /// <summary>빌드에 실리는 것이 확실한 TMP 셰이더 — 기본 폰트 애셋(Resources 의 LiberationSans SDF)이 문 것. 런타임 애셋의 Shader.Find 결과는 빌드에 안 실릴 수 있다.</summary>
        private static Shader ShipShader()
        {
            TMP_FontAsset def = TMP_Settings.defaultFontAsset;
            if (def != null && def.material != null) return def.material.shader;
            return null;
        }
    }
}
