using UnityEngine;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 코드 생성 UI 원시 도형(원작 IconGen 이 캔버스로 그리던 것과 같은 길 — 그림 파일을 새로 들이지 않는다).
    /// 알약·둥근 카드·웨이브 노드·탭 ✕ 원이 쓴다. 한 번 만들어 공유한다.
    /// </summary>
    public static class UiShapes
    {
        private const int Size = 64;
        private const int Radius = 24;
        private const float PixelsPerUnit = 100f;

        private static Sprite circle;
        private static Sprite rounded;

        /// <summary>안티에일리어싱 원판(흰색 · 색은 Image.color 로).</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    Texture2D tex = Make("ui-circle", (x, y) => Disc(x, y, Size * 0.5f - 0.5f, Size * 0.5f - 0.5f, Size * 0.5f - 1f));
                    circle = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                    circle.name = "ui-circle";
                }
                return circle;
            }
        }

        /// <summary>9-슬라이스 둥근 사각(모서리 반지름 24px · Image.Type.Sliced 로 쓰고 <see cref="RoundedMultiplier"/> 로 반지름을 맞춘다).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded == null)
                {
                    Texture2D tex = Make("ui-rounded", RoundedRect);
                    rounded = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect,
                        new Vector4(Radius, Radius, Radius, Radius));
                    rounded.name = "ui-rounded";
                }
                return rounded;
            }
        }

        /// <summary>Sliced 이미지가 화면에서 반지름 <paramref name="radiusPx"/>(캔버스 단위)로 보이게 하는 pixelsPerUnitMultiplier.</summary>
        public static float RoundedMultiplier(float radiusPx)
        {
            if (radiusPx <= 0.01f) return 1f;
            return Radius / radiusPx;
        }

        private static float Disc(int x, int y, float cx, float cy, float r)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }

        private static float RoundedRect(int x, int y)
        {
            float px = Mathf.Max(Radius - x, x - (Size - 1 - Radius), 0f);
            float py = Mathf.Max(Radius - y, y - (Size - 1 - Radius), 0f);
            float d = Mathf.Sqrt(px * px + py * py);
            return Mathf.Clamp01(Radius - d + 0.5f);
        }

        private static Texture2D Make(string name, System.Func<int, int, float> alpha)
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * alpha(x, y)));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
