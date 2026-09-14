using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Forge.Core.Data;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T159 — 정본이 `clip-path: polygon(...)` 으로 **깎아 만든 면**을 그 모양 그대로 세운다.
    ///
    /// 왜 있나: 클론은 uGUI 라 모양이 저절로 안 온다 — `UiKit.Panel`·`UiKit.Rounded` 는 네모/둥근 네모 한 장이라
    /// 리본 꼬리의 V 홈·페넌트의 아래 꼭짓점 같은 것이 **조용히 사라진다**(T159 실측: 정본 도형 열다섯 중 다섯 자리가 민무늬).
    /// 맨 <see cref="Graphic"/> 은 이 레포에서 한 픽셀도 안 칠해지므로(결정 223) 폴리곤을 **구워** <see cref="Image"/> 에 얹는다 —
    /// T87 6회차가 세운 <see cref="CraftFxPoly"/> 길이고 T98 모루·T102 부화 빛기둥·T156 리본이 이미 같은 길을 썼다.
    ///
    /// 꼭짓점은 `Assets/Forge/Resources/ClipShapeUi.json` 이 쥔다(정규 좌표 · y 는 아래가 + — CSS 와 같은 방향).
    /// 이 파일은 수치를 하나도 안 쥔다(§1 «수치는 코드에 박지 않는다»).
    ///
    /// 굽는 비율: 꼬리·깃발은 옆으로 길거나 짧아 정규 정사각에 구워 늘리면 빗변 기울기가 틀어진다.
    /// 그래서 **그 자리의 실제 비율 그대로** 굽고(높이 <c>bake_h</c> 단위 고정) 스프라이트 이름에 비율을 적어 캐시한다
    /// — T156 <see cref="RibbonArt"/> 가 같은 이유로 같은 셈을 한다(그쪽은 제 리본 하나만 쥔 표다).
    /// </summary>
    public static class ClipShape
    {
        public const string ResourcePath = "ClipShapeUi";

        static JsonObject root;
        static JsonObject shapes;

        static void Load()
        {
            if (root != null) return;
            TextAsset ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new KeyNotFoundException("Resources/" + ResourcePath + ".json 이 없다");
            JsonObject r = J.Obj(MiniJson.Parse(ta.text));
            JsonObject s = J.Obj(r["shapes"]);
            if (s == null || s.Count == 0) throw new KeyNotFoundException(ResourcePath + ".json 에 «shapes» 가 없다");
            shapes = s;
            root = r;
        }

        /// <summary>굽는 높이(단위) — 실제 픽셀은 <see cref="CraftFxPoly.PixelsPerUnit"/> 배다.</summary>
        public static float BakeH
        {
            get
            {
                Load();
                object v = root["bake_h"];
                if (!J.IsNum(v)) throw new KeyNotFoundException(ResourcePath + ".json 에 «bake_h» 가 없다");
                return (float)J.Num(v);
            }
        }

        /// <summary>그 도형의 정본 꼭짓점(정규 좌표 · x 왼→오른쪽 · y 위→아래).</summary>
        public static Vector2[] Clip(string key)
        {
            Load();
            JsonObject one = J.Obj(shapes[key]);
            List<object> pts = one == null ? null : J.Arr(one["clip"]);
            if (pts == null || pts.Count < 3)
                throw new KeyNotFoundException(ResourcePath + ".json 의 shapes 에 «" + key + "»(꼭짓점 셋 이상)가 없다");
            Vector2[] p = new Vector2[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                List<object> xy = J.Arr(pts[i]);
                if (xy == null || xy.Count < 2)
                    throw new KeyNotFoundException(ResourcePath + ".json 의 «" + key + "» 꼭짓점 " + i + " 가 [x, y] 가 아니다");
                p[i] = new Vector2((float)J.Num(xy[0]), (float)J.Num(xy[1]));
            }
            return p;
        }

        /// <summary>
        /// 그 모양의 색면 한 장을 <paramref name="parent"/> 안에 세운다(자리·크기까지 잡는다).
        /// </summary>
        /// <param name="key">`ClipShapeUi.json` 의 도형 이름.</param>
        /// <param name="colorKey">카탈로그 색 키(§1 — 색을 코드에 박지 않는다).</param>
        public static RectTransform Face(Transform parent, string name, string key, float x, float y, float w, float h, string colorKey)
        {
            RectTransform box = UiKit.Box(parent, name);
            UiKit.Place(box, x, y, w, h);
            if (w <= 0f || h <= 0f) return box;

            float bakeH = BakeH;
            float aspect = w / h;
            float bw = bakeH * aspect;
            Vector2[] norm = Clip(key);
            Vector2[] face = new Vector2[norm.Length];
            for (int i = 0; i < norm.Length; i++) face[i] = new Vector2(norm[i].x * bw, norm[i].y * bakeH);

            // 굽은 그림은 **제 폴리곤의 상자**를 덮는다 — 구운 단위 → 캔버스 px 한 배율로 제 자리에 놓는다(T156 과 같은 셈).
            float k = w / bw;
            Rect b = CraftFxPoly.Bounds(face);
            RectTransform rt = UiKit.Box(box, "face");
            UiKit.Place(rt, b.xMin * k, b.yMin * k, b.width * k, b.height * k);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            img.sprite = CraftFxPoly.Bake(key + "-" + aspect.ToString("0.00"), face);
            img.color = UiKit.C(colorKey);
            return box;
        }
    }
}
