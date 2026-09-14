using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T156 1회차 — 제작 비교 «장착됨» 리본을 **정본 깃발 모양 그대로** 굽는다(정본 `web/css/style.css` 1801~1809 `.cmp-ribbon`).
    ///
    /// 클론은 지금 둥근 사각 한 장(`ForgeUi.Ribbon` 의 `PopupKit.Outlined`)이라 정본의 두 가지가 없다:
    ///  ⓐ 오른쪽 **«&lt;» 오목 노치**(`clip-path: polygon(0 0, 100% 0, 88% 50%, 100% 100%, 0 100%)`) — 정본 주석이
    ///     «이전 값(82% 100%)은 아래쪽만 비스듬히 깎아 원본의 오목한 노치가 아니라 그냥 사다리꼴이었다» 고 적어 둔 자리다.
    ///  ⓑ **아랫변 없는 테두리**(`border-bottom: none`) — 깃발이 카드 윗변에 이어 붙는다.
    ///
    /// 맨 `Graphic` 은 이 레포에서 안 칠해지므로(결정 223) 폴리곤을 **구워** `Image` 에 얹는다 — T87 6회차가 세운
    /// <see cref="CraftFxPoly"/> 길이고 T98 모루가 이미 같은 길을 썼다. 테두리는 «면을 부풀린 사본을 뒤에 깔기» 인데,
    /// 아랫변만은 부풀린 점을 **밑단으로 되눌러** 그리지 않는다 — 그것이 `border-bottom: none` 이다.
    ///
    /// 굽는 비율: 리본은 옆으로 길어(≈6:1) 정규 좌표로 구워 늘리면 테두리가 가로·세로로 다르게 늘어난다.
    /// 그래서 **실제 비율 그대로**(높이 <see cref="BakeH"/> 단위 고정 · 폭은 그 비율) 굽고 스프라이트 이름에 비율을 적어 캐시한다.
    /// </summary>
    public static class RibbonArt
    {
        public const string ResourcePath = "RibbonUi";

        /// <summary>굽는 높이(단위) — 실제 픽셀은 여기에 표의 `bake_px` 를 곱한 값이다.</summary>
        const float BakeH = 12f;

        static JsonObject root;
        static List<object> clip;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
            JsonObject r = J.Obj(MiniJson.Parse(ta.text));
            clip = J.Arr(r["clip"]);
            if (clip == null || clip.Count < 3) throw new KeyNotFoundException(ResourcePath + ".json 에 «clip»(정본 clip-path 꼭짓점)이 없다");
            root = r;
        }

        static float Num(string key)
        {
            Load();
            object v = root[key];
            if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 에 «" + key + "» 이 없다");
            return (float)J.Num(v);
        }

        /// <summary>정본 `clip-path` 꼭짓점(0~1 · x 는 왼→오른쪽 · y 는 위→아래).</summary>
        public static Vector2[] Clip()
        {
            Load();
            Vector2[] p = new Vector2[clip.Count];
            for (int i = 0; i < clip.Count; i++)
            {
                List<object> xy = J.Arr(clip[i]);
                p[i] = new Vector2((float)J.Num(xy[0]), (float)J.Num(xy[1]));
            }
            return p;
        }

        /// <summary>리본 폭 — 정본 `width: calc(var(--app-w) * .203)`. 래퍼가 아니라 **앱 폭** 기준이다(정본 주석).</summary>
        public static float Width(float appW) { return appW * Num("width_app_w"); }

        /// <summary>정본 `--ol2 = min(2px, calc(.125rem + .49px))` 를 기준 캔버스 px 로(절대 px 는 `KeylineUi.CssPx` 로 환산 · T104·T109 규약).</summary>
        public static float Border(float rem)
        {
            float css = KeylineUi.CssPx;
            return Mathf.Min(Num("border_px") * css, Num("border_rem") * rem + Num("border_add_px") * css);
        }

        /// <summary>정본 `padding: .15rem 1.5rem .15rem .5rem` — 오른쪽이 넓은 것은 «&lt;» 파임 몫이다.</summary>
        public static void Padding(float rem, out float left, out float right, out float top, out float bottom)
        {
            left = Num("pad_left_rem") * rem; right = Num("pad_right_rem") * rem;
            top = Num("pad_top_rem") * rem; bottom = Num("pad_bottom_rem") * rem;
        }

        /// <summary>
        /// 리본을 **안쪽 카드**에 붙일 때의 자리. 정본 `left: -1.52rem` 은 «카드 안쪽 상자» 기준인데 클론의 안쪽 여백이
        /// 정본과 꼭 같지 않아(런 418 실측 1.58rem ↔ 정본 1.32rem) 그 값이면 깃발이 못 내밀고 변에 붙는다.
        /// 그래서 정본이 주석에 스스로 적어 둔 합(«22.2 + 7(내밈) ≈ 2rem»)을 쓴다 — 지켜야 하는 것은 수가 아니라
        /// **«바깥 카드에서 7 CSS px 내민다»** 는 계약이고, 그것은 자가 잰다.
        /// </summary>
        public static Vector2 Offset(float rem) { return new Vector2(Num("left_from_card_rem") * rem, Num("top_rem") * rem); }

        /// <summary>정본이 적어 둔 **내밈** — 바깥 카드 왼쪽 변에서 이만큼(CSS px) 왼쪽으로 나와야 «깃발» 로 읽힌다.</summary>
        public static float ProtrudeCss() { return Num("protrude_css_px"); }

        /// <summary>정본 `font-size: .92rem`.</summary>
        public static float FontSize(float rem) { return Num("font_rem") * rem; }

        /// <summary>
        /// 깃발 한 장을 세운다 — 테두리 면(아랫변 없음)을 뒤에, 종이 면을 앞에. 글자는 부르는 쪽이 얹는다.
        /// </summary>
        /// <param name="w">리본 폭(캔버스 px · <see cref="Width"/>).</param>
        /// <param name="h">리본 높이(캔버스 px · 글자 + 위아래 패딩).</param>
        /// <param name="rem">기준 rem.</param>
        public static RectTransform Build(Transform parent, string name, float w, float h, float rem)
        {
            Load();
            RectTransform box = UiKit.Box(parent, name);
            UiKit.Place(box, 0f, 0f, w, h);
            if (w <= 0f || h <= 0f) return box;

            float aspect = w / h;
            float bw = BakeH * aspect;
            Vector2[] norm = Clip();
            Vector2[] face = new Vector2[norm.Length];
            for (int i = 0; i < norm.Length; i++) face[i] = new Vector2(norm[i].x * bw, norm[i].y * BakeH);

            // 테두리 = 면을 `--ol2` 의 절반만큼 바깥으로 부풀린 사본. 다만 **아랫변은 안 그린다**(정본 `border-bottom: none`) —
            // 부풀며 밑으로 나간 점들을 밑단(`BakeH`)으로 되눌러 그 변만 몸통과 겹치게 한다.
            float pad = Border(rem) / h * BakeH * 0.5f;
            Vector2[] line = CraftFxPoly.Inflate(face, pad);
            for (int i = 0; i < line.Length; i++) if (line[i].y > BakeH) line[i].y = BakeH;

            // 굽은 그림은 **제 폴리곤의 상자**를 덮는다 — 테두리 사본은 면보다 크므로 상자를 꽉 채우면(`Fill`) 눌려서 사라진다.
            // 그래서 둘 다 «구운 단위 → 캔버스 px» 한 배율(`w / bw`)로 제 상자 자리에 놓는다(테두리는 칸 밖으로 나간다 — 그것이 테두리다).
            float k = w / bw;
            string key = name + "-" + aspect.ToString("0.00");
            Poly(box, "line", CraftFxPoly.Bake(key + "-line", line), UiKit.C("pp_line"), line, k);
            Poly(box, "face", CraftFxPoly.Bake(key + "-face", face), UiKit.C("pp_paper"), face, k);
            return box;
        }

        static Image Poly(RectTransform box, string name, Sprite sp, Color tint, Vector2[] pts, float k)
        {
            Rect b = CraftFxPoly.Bounds(pts);
            RectTransform rt = UiKit.Box(box, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.color = tint;
            UiKit.Place(rt, b.xMin * k, b.yMin * k, b.width * k, b.height * k);
            return img;
        }
    }
}
