using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;
using Forge.Core.Ui;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T331 2회차 — 정본 `box-shadow` 의 **바깥 그림자**를 화면에 거는 자리. 수치는 `Resources/ShadowUi.json`(표) ·
    /// 셈·부호는 <see cref="ShadowTable"/>(Core) 가 쥔다.
    ///
    /// 정본의 «딱딱한 턱»(`0 .25rem 0 rgba(0,0,0,.3)` 꼴 · 흐림 0 · 번짐 0)은 원작 UI 가 «종이 카드가 한 겹 떠 있다» 를
    /// 만드는 방식이다. 흐림이 없으니 **같은 모양을 한 겹 뒤에 깔고 내리기만** 하면 화면이 정본과 같아진다 — 구울 것이 없다.
    /// UGUI 에서 «뒤» 는 곧 **먼저 그려지는 것**이라, 그늘을 대상 상자의 **첫 자식**으로 꽂는다(부모가 먼저, 자식이 뒤에
    /// 그려지므로 형제 중 첫째가 가장 아래다). 레이아웃은 건드리지 않는다 — 그늘은 상자를 꽉 채우고 치우침만 받는다.
    ///
    /// 흐린 그림자(`blur > 0`)는 여기서 거절한다 — 굽는 길(스프라이트)이 먼저 있어야 하고 그것이 3회차다.
    /// 조용히 딱딱하게 그리면 «섰다» 고 세어져 자가 거짓으로 초록이 된다.
    ///
    /// 도우미를 `UiKit` 에 안 넣고 새 파일로 낸 까닭: `UiKit.cs` 는 남의 산 lock 이다(T106·T178·T335) — T342 가 낸 길과 같다.
    /// </summary>
    public static class UiShadow
    {
        public const string ResourcePath = "ShadowUi";
        /// <summary>그늘 겹 이름의 앞머리 — 실제 이름은 «앞머리-자리키» 다(<see cref="Layer"/>).</summary>
        public const string LayerName = "shadow";

        /// <summary>
        /// 그 자리의 그늘 겹 이름. **자리마다 다른 이름**을 쓰는 까닭: 정본은 한 상자에 그림자를 **여럿** 걸 수 있다
        /// (`.af-card` 가 «공용 아래턱 + 앰비언트» 둘이다 — style.css 5030 · 주석이 그 뜻을 적어 뒀다).
        /// 이름이 하나면 두 번째가 첫 번째를 덮어써 조용히 한 겹만 남는다.
        /// </summary>
        public static string Layer(string key) { return LayerName + "-" + key; }

        /// <summary>그 상자에 걸린 그 자리의 그늘(없으면 null) — 자가 이것으로 찾는다.</summary>
        public static Transform Find(RectTransform box, string key) { return box == null ? null : box.Find(Layer(key)); }

        /// <summary>
        /// 깔아 둔 겹 하나를 걷는다 — 정본이 그 자리에서 **다른 규칙으로 덮어쓴** 경우에 쓴다.
        ///
        /// CSS `box-shadow` 는 **겹치지 않는다**: 더 구체적인 규칙이 목록을 통째로 갈아 끼운다.
        /// 그래서 공용 카드가 깔아 둔 턱을 그 카드에서만 걷어야 정본과 같아진다(T331 25회차 · `.pass-card` 8602).
        /// `Destroy` 는 프레임 끝이라 그 사이 `Find` 가 아직 그것을 본다 — 트리에서 **먼저 뺀다**.
        /// </summary>
        public static void Remove(RectTransform box, string key)
        {
            if (box != null)
            {
                // 아직 안 구운 예약(34회차)도 같이 걷는다 — 안 그러면 걷은 뒤에 다시 깔린다.
                foreach (UiShadowLate late in box.GetComponents<UiShadowLate>())
                    if (late != null && late.Waiting(key)) UnityEngine.Object.Destroy(late);
            }
            Transform had = Find(box, key);
            if (had == null) return;
            had.SetParent(null, false);
            UnityEngine.Object.Destroy(had.gameObject);
        }

        static ShadowTable table;

        public static ShadowTable Table
        {
            get
            {
                if (table == null)
                {
                    TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
                    if (ta == null) throw new InvalidOperationException("Resources/" + ResourcePath + ".json 을 못 읽었다(.meta 가 없으면 유니티가 안 싣는다)");
                    table = ShadowTable.From(MiniJson.ParseObject(ta.text));
                }
                return table;
            }
        }

        /// <summary>테스트가 표를 다시 읽게 한다.</summary>
        public static void Reset() { table = null; baked.Clear(); }

        /// <summary>
        /// 상자 <paramref name="box"/> 뒤에 정본 자리 <paramref name="key"/> 의 턱을 깐다.
        /// </summary>
        /// <param name="radiusPx">상자의 둥근 모서리(기준 px) — 그늘도 같은 모양이라야 테두리에서 안 비어져 나온다.</param>
        /// <returns>깐 겹(같은 상자에 두 번 부르면 앞서 깐 것을 고쳐 준다).</returns>
        public static Image Drop(RectTransform box, string key, float radiusPx)
        {
            return Drop(box, key, radiusPx, -1f, -1f);
        }

        /// <summary>
        /// 크기를 **부르는 쪽이 주는** 꼴 — 레이아웃이 나중에 잡는 상자(`LayoutElement` 로 크기를 예약한
        /// 목록 행 따위)에서 쓴다. `box.rect` 는 그 프레임엔 아직 0 이라 굽는 길이 조용히 빈손으로 돌아온다
        /// (27회차 런 921 의 빨강 «하위 행의 그늘이 없다» 가 그것이다).
        /// </summary>
        /// <param name="w">상자의 폭(px · 음수면 `box.rect` 에서 읽는다).</param>
        /// <param name="h">상자의 높이(px · 음수면 `box.rect` 에서 읽는다).</param>
        /// <param name="grow">상자보다 **사방으로** 이만큼 더 큰 몸(border-box)에 그늘을 깐다 — T465(세로)·T473(가로): 팝업 카드는 rect 가 패딩 상자고 테가 사방 `Line3` 만큼 밖에 있다.</param>
        public static Image Drop(RectTransform box, string key, float radiusPx, float w, float h, float grow = 0f)
        {
            if (box == null) throw new ArgumentNullException("box");
            ShadowSpec s = Table.Get(key);
            if (!s.IsHard) return Blur(box, key, radiusPx, w, h, grow);

            Transform had = box.Find(Layer(key));
            Image img = had != null ? had.GetComponent<Image>() : UiKit.Rounded(box, Layer(key), "pp_line", radiusPx);
            img.rectTransform.SetAsFirstSibling();
            UiKit.Fill(img.rectTransform);
            img.raycastTarget = false;
            img.color = new Color((float)s.R, (float)s.G, (float)s.B, (float)s.A);

            double x, y;
            Table.OffsetPx(key, PetSkillStyle.RemPx, out x, out y);
            img.rectTransform.anchoredPosition = new Vector2((float)x, (float)y);
            img.rectTransform.sizeDelta = new Vector2(grow * 2f, grow * 2f);   // 피벗 가운데 · grow 0 이면 정확히 0(자 UiShadowTests «패널과 같은 크기»)
            return img;
        }

        /// <summary>
        /// 흐린 그림자(`blur > 0`) — 상자 크기에 맞춰 **한 장 구워** 뒤에 깐다(3회차).
        ///
        /// CSS 는 «그림자 모양을 가우시안으로 번지게 한 것» 이다. 곧은 모서리를 가우시안으로 번지게 하면 정확히
        /// **정규분포의 누적**이라, 화소마다 둥근 네모까지의 거리를 재서 <see cref="ShadowTable.EdgeCoverage"/> 를 부르면 된다 —
        /// 커널을 돌릴 것도, 판을 여러 번 그릴 것도 없다(σ = 흐림 반지름의 절반 · CSS 규격).
        ///
        /// 판은 흐림이 잘리지 않게 **테두리를 넓혀** 굽고, 그만큼 상자 밖으로 내민다. 자리와 크기를 한 자리에서
        /// 주려고 `offsetMin`·`offsetMax` 로 준다 — 늘어난 상자에서 그 둘이 **크기와 자리를 같이 쥔다**(런 528 의 교훈).
        /// </summary>
        static Image Blur(RectTransform box, string key, float radiusPx, float wantW, float wantH, float grow = 0f)
        {
            ShadowSpec s = Table.Get(key);
            float rem = PetSkillStyle.RemPx;
            float w = (wantW > 0f ? wantW : box.rect.width) + grow * 2f, h = (wantH > 0f ? wantH : box.rect.height) + grow * 2f;
            if (w <= 1f || h <= 1f)
            {
                // 여기서 조용히 돌아가면 **그늘이 없는 채로 화면이 선다** — 자는 «호출이 있다» 만 보므로 아무도 모른다.
                // 그래서 소리를 낸다(경고라 자를 안 넘어뜨린다 · 런 921 이 이 자리에서 빨갰다).
                Debug.LogWarning("UiShadow: " + key + " 를 못 구웠다 — 상자(" + box.name + ")의 크기가 아직 0이다."
                    + " 레이아웃이 나중에 잡는 자리면 `Drop(box, key, radius, w, h)` 로 크기를 줘라.");
                return null;
            }

            float blur = (float)s.BlurRem * rem, spread = (float)s.SpreadRem * rem;
            float pad = Mathf.Ceil(blur * 2f + Mathf.Max(0f, spread)) + 2f;   // 2σ 면 화소로 0 이다(누적 0.1% 아래)

            Transform had = box.Find(Layer(key));
            Image img = had != null ? had.GetComponent<Image>() : null;
            if (img == null)
            {
                RectTransform rt = UiKit.Box(box, Layer(key));
                img = rt.gameObject.AddComponent<Image>();
            }
            img.rectTransform.SetAsFirstSibling();
            img.raycastTarget = false;
            img.preserveAspect = false;
            img.color = Color.white;                         // 색은 구운 화소가 쥔다(틴트로 주면 알파가 두 번 곱해진다)
            img.sprite = Bake(key, s, w, h, radiusPx, pad);

            RectTransform r = img.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            double dx, dy;
            Table.OffsetPx(key, rem, out dx, out dy);
            r.offsetMin = new Vector2(-pad + (float)dx - grow, -pad + (float)dy - grow);
            r.offsetMax = new Vector2(pad + (float)dx + grow, pad + (float)dy + grow);
            return img;
        }

        static readonly Dictionary<string, Sprite> baked = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        /// <summary>구운 판 하나(같은 자리·같은 크기면 한 번만 굽는다).</summary>
        static Sprite Bake(string key, ShadowSpec s, float w, float h, float radiusPx, float pad)
        {
            int bw = Mathf.RoundToInt(w + pad * 2f), bh = Mathf.RoundToInt(h + pad * 2f);
            string id = key + "|" + bw + "x" + bh + "|" + Mathf.RoundToInt(radiusPx);
            Sprite hit;
            if (baked.TryGetValue(id, out hit) && hit != null) return hit;

            float rem = PetSkillStyle.RemPx;
            double sigma = ShadowTable.SigmaOf(s.BlurRem * rem);
            double halfW = w * 0.5 + s.SpreadRem * rem, halfH = h * 0.5 + s.SpreadRem * rem;
            double radius = radiusPx + s.SpreadRem * rem;
            var px = new Color32[bw * bh];
            for (int y = 0; y < bh; y++)
            {
                double py = y + 0.5 - bh * 0.5;
                for (int x = 0; x < bw; x++)
                {
                    double pxx = x + 0.5 - bw * 0.5;
                    double d = ShadowTable.RoundRectDistance(pxx, py, Math.Max(0.5, halfW), Math.Max(0.5, halfH), Math.Max(0, radius));
                    double a = ShadowTable.EdgeCoverage(d, sigma) * s.A;
                    px[y * bw + x] = new Color((float)s.R, (float)s.G, (float)s.B, (float)a);
                }
            }
            var tex = new Texture2D(bw, bh, TextureFormat.RGBA32, false);
            tex.name = "shadow-" + id;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(px);
            tex.Apply(false, true);                          // 화면에만 쓴다 — CPU 사본을 남기지 않는다
            Sprite sp = Sprite.Create(tex, new Rect(0f, 0f, bw, bh), new Vector2(0.5f, 0.5f), 100f);
            sp.name = "shadow-" + id;
            baked[id] = sp;
            return sp;
        }

        /// <summary>
        /// 상자가 실제로 쓰는 **둥근 모서리 반지름**을 그 그림에서 되읽는다 — 그늘도 같은 모양이라야 구석이 안 어긋난다.
        ///
        /// `UiKit.Rounded` 는 9-슬라이스 둥근 스프라이트에 `pixelsPerUnitMultiplier = 스프라이트 반지름 / 원하는 반지름` 을 준다.
        /// 그래서 되읽기는 그 나눗셈을 거꾸로 하면 된다 — 스프라이트 쪽 반지름은 상수가 아니라 **스프라이트의 `border`** 에서 읽는다
        /// (그 상수를 쥔 `UiShapes.cs` 는 남의 lock 이고, border 에서 읽으면 그 상수가 바뀌어도 따라간다).
        /// </summary>
        /// <returns>못 읽으면 0(각진 그늘) — 부르는 쪽이 아는 값이 있으면 그것을 직접 주는 편이 낫다.</returns>
        public static float RadiusOf(RectTransform box)
        {
            if (box == null) return 0f;
            foreach (Image img in box.GetComponentsInChildren<Image>(true))
            {
                if (img == null || img.sprite == null) continue;
                if (img.transform.name.StartsWith(LayerName, StringComparison.Ordinal)) continue;   // 내가 깐 그늘은 세지 않는다(두 번째 부름에서 자기를 읽는다)
                if (img.type != Image.Type.Sliced) continue;
                float spriteR = img.sprite.border.x;
                if (spriteR <= 0f) continue;
                float m = img.pixelsPerUnitMultiplier;
                return m <= 0.0001f ? 0f : spriteR / m;
            }
            return 0f;
        }

        /// <summary>반지름을 상자에서 되읽어 거는 꼴 — 복제된 상자(정본 `.eqsw-fly-box` 처럼)에 쓴다.</summary>
        public static Image Drop(RectTransform box, string key) { return Drop(box, key, RadiusOf(box)); }

        /// <summary>
        /// 크기가 **나중에** 잡히는 상자에 건다 — 첫 유효 크기에서 한 번 굽고 스스로 사라진다(34회차).
        ///
        /// 공용 카드 공장(`PopupKit.Card`)은 높이를 `-1` 로 받아 `ContentSizeFitter` 가 내용으로 정하게 두는
        /// 자리가 많다. 그런 상자는 세우는 그 프레임엔 `rect` 가 0 이라 굽는 길이 빈손으로 돌아간다(28회차).
        /// 딱딱한 턱은 크기를 안 쓰므로 그대로 걸고, 흐린 겹만 한 프레임 미룬다.
        /// </summary>
        public static void DropWhenSized(RectTransform box, string key, float radiusPx, float grow = 0f)
        {
            if (box == null) throw new ArgumentNullException("box");
            if (Table.Get(key).IsHard) { Drop(box, key, radiusPx, -1f, -1f, grow); return; }
            if (box.rect.width > 1f && box.rect.height > 1f) { Drop(box, key, radiusPx, -1f, -1f, grow); return; }
            UiShadowLate late = box.gameObject.AddComponent<UiShadowLate>();
            late.Arm(key, radiusPx, grow);
        }
    }

    /// <summary>
    /// <see cref="UiShadow.DropWhenSized"/> 가 다는 한 회용 부품 — 상자 크기가 잡히는 **첫 프레임**에 굽고 죽는다.
    /// 스스로를 지우므로 화면에 남지 않고, `UiShadow.Remove` 는 이것도 같이 걷는다(굽기 전에 취소해야 하는 자리가 있다).
    /// </summary>
    public sealed class UiShadowLate : MonoBehaviour
    {
        string key;
        float radiusPx, grow;
        int waited;

        internal void Arm(string k, float r, float g = 0f) { key = k; radiusPx = r; grow = g; }

        /// <summary>이 부품이 기다리는 키 — `Remove` 가 짝을 가른다.</summary>
        internal bool Waiting(string k) { return key == k; }

        void LateUpdate()
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null) { Destroy(this); return; }
            if (rt.rect.width <= 1f || rt.rect.height <= 1f)
            {
                // 60프레임(1초)을 기다려도 안 잡히면 그 상자는 «크기가 없는 상자» 다 — 조용히 죽지 말고 알린다.
                if (++waited > 60)
                {
                    Debug.LogWarning("UiShadow: " + key + " 를 못 구웠다 — 상자(" + name + ")의 크기가 1초가 지나도 0이다.");
                    Destroy(this);
                }
                return;
            }
            UiShadow.Drop(rt, key, radiusPx, -1f, -1f, grow);
            Destroy(this);
        }
    }
}
