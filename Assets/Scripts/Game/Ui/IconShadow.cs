using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T453 — 정본이 **글자**에 건 `text-shadow`(링·그림자)를, 클론이 그 자리를 **아이콘**으로 세운 곳에 옮긴다.
    /// <see cref="UiKit.TextShadow"/>(TMP 언더레이)와 <see cref="UiKit.OutlinePx(TMPro.TextMeshProUGUI, string, float)"/>(SDF 스트로크)는 글자에만 닿아
    /// 아틀라스 아이콘(채팅 ◀ `tri_left` · 켜진 ✓ `check`)엔 못 걸었다(T333 4·14회차 KNOWN).
    /// CSS `text-shadow` 가 실제로 하는 일 그대로 — **같은 그림을 오프셋만큼 옮겨 뒤에 깔고 표 색으로 칠한다**:
    /// 링은 오프셋 사본 4~8장(<see cref="Ring"/>), 흐린 그림자는 한 장 + 가우시안(<see cref="Drop"/> · <see cref="UiFilter.Blur"/> · T342).
    /// 사본은 `raycastTarget = false` 이고 원본 **뒤**(형제 순서)에 선다 — 링이 아이콘을 덮으면 그림이 뭉갠다.
    /// 값은 표 <see cref="IconShadowUi"/>(CSS px · rem)에만 있다(§1). 아틀라스에 링을 굽지 않고(정본 IconGen 은 링을 안 그린다) 아이콘을 글자로 되돌리지도 않는다(T58).
    /// </summary>
    public static class IconShadow
    {
        public const string RingName = "icon-ring";
        public const string DropName = "icon-drop";

        /// <summary>그 아이콘의 <paramref name="i"/> 번째 링 사본 이름 — 한 부모에 아이콘이 여럿일 수 있어 그림 이름을 붙인다(<see cref="DropShadow.NameFor"/> 와 같은 까닭).</summary>
        public static string RingNameFor(Image img, int i) { return RingName + ":" + (img != null ? img.name : "?") + ":" + i; }
        public static string DropNameFor(Image img) { return DropName + ":" + (img != null ? img.name : "?"); }

        /// <summary>
        /// <paramref name="img"/> 뒤에 표 <paramref name="key"/>(`rings`)의 오프셋만큼 옮긴 사본을 깐다(이미 있으면 갱신). 사본 수를 돌려준다.
        /// 오프셋은 정본 CSS px → <see cref="KeylineUi.CssPx"/> 로 캔버스 px(y 는 CSS 아래 + → 캔버스 위 + 로 부호를 뒤집는다).
        /// 원본이 자리를 못 받았으면(스프라이트 없음) 0 — 자리를 잡은 **뒤에** 부른다.
        /// </summary>
        public static int Ring(Image img, string key)
        {
            if (img == null || img.sprite == null) return 0;
            RectTransform ir = img.rectTransform;
            Transform parent = ir.parent;
            if (parent == null) return 0;
            List<Vector2> offs = IconShadowUi.RingOffsets(key);
            Color c = IconShadowUi.RingColor(key);
            float css = KeylineUi.CssPx;
            for (int i = 0; i < offs.Count; i++)
            {
                string name = RingNameFor(img, i);
                Transform old = parent.Find(name);
                Image cp = old != null ? old.GetComponent<Image>() : null;
                if (cp == null)
                {
                    RectTransform box = UiKit.Box(parent, name);
                    cp = box.gameObject.AddComponent<Image>();
                    cp.raycastTarget = false;
                }
                cp.sprite = img.sprite;
                cp.type = img.type;
                cp.preserveAspect = img.preserveAspect;
                cp.color = c;
                RectTransform cr = cp.rectTransform;
                cr.anchorMin = ir.anchorMin; cr.anchorMax = ir.anchorMax; cr.pivot = ir.pivot;
                cr.sizeDelta = ir.sizeDelta;
                cr.anchoredPosition = ir.anchoredPosition + new Vector2(offs[i].x * css, -offs[i].y * css);
                cr.SetSiblingIndex(ir.GetSiblingIndex());                                    // 원본 **뒤**(원본은 늘 사본들 다음)
            }
            return offs.Count;
        }

        /// <summary>
        /// <paramref name="img"/> 뒤에 표 <paramref name="key"/>(`shadows`)의 흐린 그림자 한 장을 깐다(이미 있으면 갱신) — <see cref="DropShadow.Apply"/> 와 같은 길이되
        /// 길이가 **rem**(정본이 이 자리를 rem 으로 적었다 · <see cref="PopupKit.Rem"/> = 캔버스 px/rem)이고 흐림은 반지름(σ = 반지름/2).
        /// 상자는 원본 상자 + 구운 판의 여유(<see cref="UiFilter.BlurGrow"/>)라 번짐이 상자 끝에서 안 잘린다(T411 3회차 규약).
        /// </summary>
        public static Image Drop(Image img, string key)
        {
            if (img == null || img.sprite == null) return null;
            RectTransform ir = img.rectTransform;
            Rect r = ir.rect;
            if (r.width <= 1f || r.height <= 1f) return null;
            Transform parent = ir.parent;
            if (parent == null) return null;

            float rem = PopupKit.Rem;
            float boxPx = Mathf.Max(r.width, r.height);
            double sigmaCanvas = IconShadowUi.ShadowRem(key, "blur_rem") * rem * 0.5f;      // 반지름 → σ
            double sigmaBaked = FilterRules.BakeSigmaPx(sigmaCanvas, img.sprite.textureRect.height, r.height);
            Sprite sp = sigmaBaked > 0
                ? UiFilter.Blur(img.sprite, sigmaBaked, key + "-" + Mathf.RoundToInt((float)(sigmaBaked * 100)), boxPx)
                : img.sprite;

            string name = DropNameFor(img);
            Transform old = parent.Find(name);
            Image sh = old != null ? old.GetComponent<Image>() : null;
            if (sh == null)
            {
                RectTransform box = UiKit.Box(parent, name);
                sh = box.gameObject.AddComponent<Image>();
                sh.raycastTarget = false;
            }
            sh.sprite = sp;
            sh.type = img.type;
            sh.preserveAspect = img.preserveAspect;
            sh.color = IconShadowUi.ShadowColor(key);
            RectTransform sr = sh.rectTransform;
            sr.anchorMin = ir.anchorMin; sr.anchorMax = ir.anchorMax; sr.pivot = ir.pivot;
            Vector2 grow = UiFilter.BlurGrow(img.sprite, sp, boxPx);
            Vector2 extra = new Vector2(r.width * (grow.x - 1f), r.height * (grow.y - 1f));
            Vector2 offset = new Vector2(IconShadowUi.ShadowRem(key, "dx_rem") * rem, -IconShadowUi.ShadowRem(key, "dy_rem") * rem);
            sr.sizeDelta = ir.sizeDelta + extra;
            sr.anchoredPosition = ir.anchoredPosition + offset + new Vector2((ir.pivot.x - 0.5f) * extra.x, (ir.pivot.y - 0.5f) * extra.y);
            sr.SetSiblingIndex(ir.GetSiblingIndex());                                        // 그림 **뒤**
            return sh;
        }
    }

    /// <summary>
    /// T453 표(<c>Assets/Forge/Resources/IconShadowUi.json</c>) — `rings`(오프셋 목록 CSS px + 색) · `shadows`(dx·dy·blur **rem** + 색).
    /// 꼴은 <see cref="TextShadowUi"/> 와 같다 — 그 표가 T333 산 lock 이라 제 표로 냈다(결정 774). 수치는 코드에 안 박는다(§1).
    /// </summary>
    public static class IconShadowUi
    {
        public const string ResourcePath = "IconShadowUi";
        static JsonObject root, rings, shadows;
        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new System.InvalidOperationException("Resources/" + ResourcePath + ".json 이 없다 (T453)");
            root = MiniJson.ParseObject(ta.text);
            rings = J.Obj(root["rings"]);
            shadows = J.Obj(root["shadows"]);
        }
        public static void Reset() { root = null; rings = null; shadows = null; colorCache.Clear(); }

        public static bool HasRing(string key) { Load(); return rings != null && J.Obj(rings[key]) != null; }
        public static bool HasShadow(string key) { Load(); return shadows != null && J.Obj(shadows[key]) != null; }

        static JsonObject RingEntry(string key)
        {
            Load();
            JsonObject o = rings != null ? J.Obj(rings[key]) : null;
            if (o == null) throw new KeyNotFoundException(ResourcePath + ".json 의 «rings» 에 «" + key + "» 이 없다");
            return o;
        }
        static JsonObject ShadowEntry(string key)
        {
            Load();
            JsonObject o = shadows != null ? J.Obj(shadows[key]) : null;
            if (o == null) throw new KeyNotFoundException(ResourcePath + ".json 의 «shadows» 에 «" + key + "» 이 없다");
            return o;
        }

        /// <summary>링 오프셋들(정본 CSS px · x 오른쪽 + · y 아래 +) — `offsets_px`.</summary>
        public static List<Vector2> RingOffsets(string key)
        {
            List<object> a = J.Arr(RingEntry(key)["offsets_px"]);
            if (a == null || a.Count == 0) throw new KeyNotFoundException(ResourcePath + ".json «" + key + "» 에 offsets_px 가 없다");
            List<Vector2> r = new List<Vector2>(a.Count);
            for (int i = 0; i < a.Count; i++)
            {
                List<object> p = J.Arr(a[i]);
                if (p == null || p.Count != 2 || !J.IsNum(p[0]) || !J.IsNum(p[1])) throw new System.FormatException(ResourcePath + ".json «" + key + "» offsets_px[" + i + "] 는 [x, y] 여야 한다");
                r.Add(new Vector2((float)J.Num(p[0]), (float)J.Num(p[1])));
            }
            return r;
        }

        /// <summary>링의 변당 두께(캔버스 px) = 오프셋 성분의 최대 절댓값 × <see cref="KeylineUi.CssPx"/> — 글자 자리(SDF 스트로크)가 쓴다.</summary>
        public static float RingPx(string key)
        {
            List<Vector2> offs = RingOffsets(key);
            float m = 0f;
            for (int i = 0; i < offs.Count; i++) m = Mathf.Max(m, Mathf.Max(Mathf.Abs(offs[i].x), Mathf.Abs(offs[i].y)));
            return m * KeylineUi.CssPx;
        }

        public static Color RingColor(string key) { return ColorOf("ring:" + key, RingEntry(key)); }
        public static Color ShadowColor(string key) { return ColorOf("shadow:" + key, ShadowEntry(key)); }

        /// <summary>그림자 길이(정본 rem) — `dx_rem` · `dy_rem`(아래가 +) · `blur_rem`(흐림 반지름).</summary>
        public static float ShadowRem(string key, string field)
        {
            object v = ShadowEntry(key)[field];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json «" + key + "» 에 «" + field + "» 이 없다");
            return (float)J.Num(v);
        }

        static Color ColorOf(string cacheKey, JsonObject o)
        {
            Color c;
            if (colorCache.TryGetValue(cacheKey, out c)) return c;
            string hex = J.Str(o["color"]);
            if (hex == null || !ColorUtility.TryParseHtmlString(hex, out c)) throw new System.FormatException(ResourcePath + ".json «" + cacheKey + "» 의 색 «" + hex + "» 을 못 읽는다");
            c.a = (float)J.Num(o["alpha"], 1);
            colorCache[cacheKey] = c;
            return c;
        }
    }
}
