using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T332 — 정본 `filter: drop-shadow(dx dy blur color)` 중 **흐림이 있는** 자리를 실루엣 그대로 건다.
    ///
    /// 왜 새 기구인가(9회차에 가린 것): 지금까지 쓴 둘이 다 안 맞는다 —
    /// `UnityEngine.UI.Shadow`(<see cref="ForgeUi.ImageShadow"/>)는 **흐림 손잡이가 없고**,
    /// `UiShadow.Drop`(T331)은 **둥근 상자** 전용이라 아이콘 실루엣을 못 따라간다.
    /// 그래서 그림 스프라이트의 알파를 <see cref="UiFilter.Blur"/>(T342 · 가우시안)로 흐려 구운 사본을
    /// **그 자리 형제로 뒤에 깔고** 표 색으로 칠한다 — 그것이 곧 정본의 drop-shadow 다.
    ///
    /// ⚠ CSS `drop-shadow` 의 셋째 값은 **흐림 반지름**이라 σ = blur / 2 다 — `filter: blur(σ)` 와 다르다(그쪽 인자가 곧 σ).
    ///    `PetHatchCone`(T342)이 `spec.BlurPx` 를 그대로 σ 로 넘기는 것은 그 자리가 `blur()` 라서 맞다.
    ///
    /// 도우미를 `ForgeUi`·`UiKit` 에 안 넣고 새 파일로 낸 까닭: 둘 다 남의 산 lock 을 자주 탄다(T342 의 `UiFilter` 와 같은 길).
    /// </summary>
    public static class DropShadow
    {
        /// <summary>깔아 둔 그림자 이름의 **머리** — 실제 이름은 <see cref="NameFor"/> 가 그림 이름을 붙여 만든다.</summary>
        public const string Name = "drop-shadow";

        /// <summary>
        /// 그 그림의 그림자 이름. **한 부모 아래 그림이 여럿일 수 있어** 이름에 그림 이름을 붙인다 —
        /// 런 631 이 그 실물이다: 던전 보상 알약은 아이콘 서넛이 **같은 알약을 부모로** 쓰는데 이름이 하나뿐이면
        /// 뒤 아이콘이 앞 아이콘의 그림자를 **제 자리로 옮겨 가** 첫 아이콘의 그림자가 127px 옆에 가 있었다.
        /// (`.pass-sword` 는 아이콘이 하나라 그 자리에서는 안 드러났다.)
        /// </summary>
        public static string NameFor(Image img) { return img == null ? Name : Name + ":" + img.name; }

        /// <summary>
        /// <paramref name="img"/> 뒤에 표 <paramref name="key"/> 의 그림자를 깐다(이미 있으면 갱신).
        /// 그림에 스프라이트가 없거나 아직 자리를 못 받았으면(레이아웃 전) 아무것도 안 한다 — 자리를 잡은 **뒤에** 부른다.
        /// </summary>
        public static Image Apply(Image img, string key)
        {
            if (img == null || img.sprite == null) return null;
            RectTransform ir = img.rectTransform;
            Rect r = ir.rect;
            if (r.width <= 1f || r.height <= 1f) return null;
            Transform parent = ir.parent;
            if (parent == null) return null;

            // 길이는 표가 푼다 — 정본이 `px`·`rem` 으로 적은 자리는 CSS px, **`em`** 으로 적은 자리는 상자 비율이다(T332 15회차).
            float boxPx = Mathf.Max(r.width, r.height);
            double sigmaCanvas = DropShadowUi.Len(key, "blur", boxPx) * 0.5f;               // 반지름 → σ
            double sigmaBaked = FilterRules.BakeSigmaPx(sigmaCanvas, img.sprite.textureRect.height, r.height);
            Sprite sp = sigmaBaked > 0
                ? UiFilter.Blur(img.sprite, sigmaBaked, key + "-" + Mathf.RoundToInt((float)(sigmaBaked * 100)), Mathf.Max(r.width, r.height))
                : img.sprite;

            string shName = NameFor(img);
            Transform old = parent.Find(shName);
            Image sh = old != null ? old.GetComponent<Image>() : null;
            if (sh == null)
            {
                RectTransform box = UiKit.Box(parent, shName);
                sh = box.gameObject.AddComponent<Image>();
                sh.raycastTarget = false;
            }
            sh.sprite = sp;
            sh.type = img.type;
            sh.preserveAspect = img.preserveAspect;
            sh.color = DropShadowUi.C(key);
            RectTransform sr = sh.rectTransform;
            sr.anchorMin = ir.anchorMin; sr.anchorMax = ir.anchorMax; sr.pivot = ir.pivot;
            FitPadded(sr, ir, sp, r, new Vector2(DropShadowUi.Len(key, "dx", boxPx), -DropShadowUi.Len(key, "dy", boxPx)));
            sr.SetSiblingIndex(ir.GetSiblingIndex());                                        // 그림 **뒤**에 그린다
            return sh;
        }

        /// <summary>
        /// T411 3회차 — 그림자 상자 = 원본 상자 **+ 구운 판의 여유**(<see cref="UiFilter.BlurGrow"/> · 커널 반경만큼) · 가운데는 원본 가운데 + 오프셋.
        /// 종전엔 원본과 같은 상자에 넣어 실루엣이 그 비만큼 줄고 번짐이 상자 끝에서 잘렸다 — 정본 `filter: drop-shadow` 는 요소 상자 **밖으로** 번진다
        /// (나는 코인의 금색 글로우가 코인 뒤에 통째로 숨어 T408 자에 한 화소도 안 잡히던 까닭 · 런 856·868).
        /// </summary>
        static void FitPadded(RectTransform sr, RectTransform like, Sprite bakedSprite, Rect r, Vector2 offset)
        {
            Vector2 grow = UiFilter.BlurGrow(bakedSprite);
            Vector2 extra = new Vector2(r.width * (grow.x - 1f), r.height * (grow.y - 1f));
            sr.sizeDelta = like.sizeDelta + extra;
            sr.anchoredPosition = like.anchoredPosition + offset
                                  + new Vector2((0.5f - like.pivot.x) * extra.x, (0.5f - like.pivot.y) * extra.y);
        }

        /// <summary>
        /// T332 14회차 — 그림이 **한 장이 아닌 자리**(여러 겹으로 그린 그림)에 같은 그림자를 건다.
        /// 정본 `filter: drop-shadow` 는 그 요소가 그린 **전부를 한 덩어리로** 보고 바깥 윤곽에만 그림자를 주므로,
        /// 부르는 쪽이 그 덩어리의 **실루엣 한 장**(<paramref name="silhouette"/> · 예: <see cref="AnvilArt.Silhouette"/>)을 주면
        /// 나머지는 <see cref="Apply"/> 와 똑같다. 겹마다 따로 걸면 안쪽 경계마다 검은 띠가 생겨 정본과 다른 그림이 된다.
        /// </summary>
        public static Image ApplyGroup(RectTransform box, Sprite silhouette, string key, string shName)
        {
            if (box == null || silhouette == null || string.IsNullOrEmpty(shName)) return null;
            Rect r = box.rect;
            if (r.width <= 1f || r.height <= 1f) return null;
            Transform parent = box.parent;
            if (parent == null) return null;

            float css = KeylineUi.CssPx;
            double sigmaCanvas = DropShadowUi.Px(key, "blur_px") * 0.5f * css;              // 반지름 → σ (CSS drop-shadow)
            double sigmaBaked = FilterRules.BakeSigmaPx(sigmaCanvas, silhouette.textureRect.height, r.height);
            Sprite sp = sigmaBaked > 0
                ? UiFilter.Blur(silhouette, sigmaBaked, key + "-" + Mathf.RoundToInt((float)(sigmaBaked * 100)), Mathf.Max(r.width, r.height))
                : silhouette;

            Transform old = parent.Find(shName);
            Image sh = old != null ? old.GetComponent<Image>() : null;
            if (sh == null)
            {
                RectTransform nb = UiKit.Box(parent, shName);
                sh = nb.gameObject.AddComponent<Image>();
                sh.raycastTarget = false;
            }
            sh.sprite = sp;
            sh.type = Image.Type.Simple;
            sh.color = DropShadowUi.C(key);
            RectTransform sr = sh.rectTransform;
            sr.anchorMin = box.anchorMin; sr.anchorMax = box.anchorMax; sr.pivot = box.pivot;
            FitPadded(sr, box, sp, r, new Vector2(DropShadowUi.Px(key, "dx_px") * css, -DropShadowUi.Px(key, "dy_px") * css));
            sr.SetSiblingIndex(box.GetSiblingIndex());                                       // 그림 **뒤**에
            return sh;
        }
    }

    /// <summary>T332 표(<c>Assets/Forge/Resources/DropShadowUi.json</c>) — 그림자 키마다 dx·dy·blur(정본 CSS px)·색(#RRGGBB + alpha). 수치는 코드에 안 박는다(§1).</summary>
    public static class DropShadowUi
    {
        public const string ResourcePath = "DropShadowUi";
        static JsonObject root, shadows;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T332)");
            root = MiniJson.ParseObject(ta.text);
            shadows = J.Obj(root["shadows"]);
        }

        public static void Reset() { root = null; shadows = null; colorCache.Clear(); }

        public static bool Has(string key) { Load(); return J.Obj(shadows[key]) != null; }

        static JsonObject Entry(string key)
        {
            Load();
            JsonObject o = J.Obj(shadows[key]);
            if (o == null) throw new KeyNotFoundException(ResourcePath + ".json 에 그림자 «" + key + "» 이 없다");
            return o;
        }

        /// <summary>표 값(정본 CSS px) — `dx_px` · `dy_px`(아래가 +) · `blur_px`(흐림 **반지름**).</summary>
        public static float Px(string key, string field)
        {
            object v = Entry(key)[field];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json «" + key + "» 에 «" + field + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>그 키가 그 칸을 갖고 있나 — «CSS px 갈래»(`*_px`)와 «상자 비율 갈래»(`*_f`)를 가르는 데 쓴다.</summary>
        public static bool HasField(string key, string field) { return J.IsNum(Entry(key)[field]); }

        /// <summary>
        /// 그 길이를 **캔버스 px** 로 푼다. 정본이 `px`·`rem` 으로 적은 자리는 표의 `<이름>_px`(정본 CSS px)에 `KeylineUi.CssPx` 를 곱하고,
        /// **`em` 으로 적은 자리**는 표의 `<이름>_f`(그림 상자에 대한 비율)에 <paramref name="boxPx"/> 를 곱한다.
        /// `em` 은 «그 요소의 글자 크기» 라 화면 크기가 아니라 **상자 크기에 걸리는 값**이다 — CSS px 로 적으면 그 자리가 커질 때 그림자만 안 커진다.
        /// </summary>
        public static float Len(string key, string name, float boxPx)
        {
            if (HasField(key, name + "_f")) return Px(key, name + "_f") * boxPx;
            return Px(key, name + "_px") * KeylineUi.CssPx;
        }

        /// <summary>색 = `color`(#RRGGBB) + `alpha`.</summary>
        public static Color C(string key)
        {
            Color c;
            if (colorCache.TryGetValue(key, out c)) return c;
            JsonObject o = Entry(key);
            string hex = J.Str(o["color"]);
            if (hex == null || !ColorUtility.TryParseHtmlString(hex, out c)) throw new System.FormatException(ResourcePath + ".json «" + key + "» 의 색 «" + hex + "» 을 못 읽는다");
            object a = o["alpha"];
            if (J.IsNum(a)) c.a = (float)J.Num(a);
            colorCache[key] = c;
            return c;
        }
    }
}
