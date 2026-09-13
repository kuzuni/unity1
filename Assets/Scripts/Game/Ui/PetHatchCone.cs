using UnityEngine;
using UnityEngine.UI;

namespace Forge.Game.Ui
{
    /// <summary>
    /// 사다리꼴/삼각/돔 색면 — 원작 부화장의 램프 빛기둥(`.hatch-cone` · clip-path polygon(38% 0, 62% 0, 100% 100%, 0 100%) + 위→아래 알파 그라데이션) ·
    /// «장착됨» 라벨의 오른쪽 홈(clip-path 화살) · 램프 갓(`.hatch-lamp` 의 `border-radius: 50% 50% … / 100% 100% …` 반타원 돔).
    ///
    /// ⚠ T102 — 처음(T20)에는 <see cref="Graphic"/> 을 상속해 정점 메시로 그렸는데 이 레포에서는 **한 픽셀도 안 칠해졌다**(런 95·186 `screen_pets.png` 빛기둥 0 픽셀 ·
    ///   T87 6회차도 같은 길에서 같은 것을 실측했다 — 결정 223). 그래서 T87 이 세운 길(<see cref="CraftFxPoly.Bake"/> 로 한 번 구운 스프라이트 + <see cref="Image"/>)로 옮긴다.
    ///   도형은 정규 상자(<see cref="Box"/>)에 굽고 칸 크기로 늘려 쓴다 — clip-path 비율(38%·62%)은 늘려도 그대로다. 겉 API(<see cref="Add"/>·<see cref="Shape"/>·<see cref="Top"/>·<see cref="Bottom"/>·<see cref="TopFrac"/>)는 그대로라 부르는 쪽은 안 바뀐다.
    /// </summary>
    public sealed class PetHatchCone : Image
    {
        public enum Kind { Cone, NotchRight, Dome }

        /// <summary>굽는 정규 상자 한 변(viewBox 단위 · <see cref="CraftFxPoly.PixelsPerUnit"/> 배로 구워진다).</summary>
        private const float Box = 32f;
        /// <summary>돔 호를 잇는 점 수.</summary>
        private const int DomeSteps = 24;

        private Kind shape = Kind.Cone;
        /// <summary>도형 — 바꾸면 다시 굽는다(<c>Add</c> 뒤에 넣는 호출자가 있다).</summary>
        public Kind Shape { get { return shape; } set { if (shape == value) return; shape = value; Rebuild(); } }
        public Color Top = Color.white;
        public Color Bottom = Color.white;
        /// <summary>위 변의 폭 비율(원작 38%~62% → 0.24).</summary>
        public float TopFrac = 0.24f;

        public static PetHatchCone Add(Transform parent, string name, Color top, Color bottom, float topFrac, float dummy)
        {
            return Add(parent, name, top, bottom, topFrac, Kind.Cone);
        }

        /// <summary>도형을 처음부터 정해 굽는다(콘을 굽고 다시 굽는 낭비 없이).</summary>
        public static PetHatchCone Add(Transform parent, string name, Color top, Color bottom, float topFrac, Kind kind)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<PetHatchCone>();
            c.Top = top;
            c.Bottom = bottom;
            c.TopFrac = topFrac;
            c.shape = kind;
            c.raycastTarget = false;
            c.Rebuild();
            return c;
        }

        /// <summary>지금 값(<see cref="Shape"/>·<see cref="Top"/>·<see cref="Bottom"/>·<see cref="TopFrac"/>)으로 스프라이트를 굽고 건다.</summary>
        public void Rebuild()
        {
            type = Type.Simple;
            preserveAspect = false;
            if (shape == Kind.Cone)
            {
                // 색은 텍스처에 굽는다(위→아래 그라디언트) · Image.color 는 흰색
                float half = Box * TopFrac * 0.5f;
                Vector2[] pts = { new Vector2(Box * 0.5f - half, 0f), new Vector2(Box * 0.5f + half, 0f), new Vector2(Box, Box), new Vector2(0f, Box) };
                string key = "pet-cone-" + ColorUtility.ToHtmlStringRGBA(Top) + "-" + ColorUtility.ToHtmlStringRGBA(Bottom) + "-" + Mathf.RoundToInt(TopFrac * 1000f);
                sprite = CraftFxPoly.Bake(key, pts, new[] { Top, Bottom }, new[] { 0f, 1f }, Vector2.zero, new Vector2(0f, 1f));
                color = Color.white;
                return;
            }
            Vector2[] shapePts;
            string name;
            if (shape == Kind.NotchRight)
            {
                // 오른쪽 홈: 오른쪽 위·아래 모서리에서 가운데로 파고드는 삼각(바탕색으로 채워 라벨을 «화살» 꼴로)
                shapePts = new[] { new Vector2(Box, 0f), new Vector2(0f, Box * 0.5f), new Vector2(Box, Box) };
                name = "pet-notch";
            }
            else
            {
                // 반타원 돔(윗 호) + 평평한 바닥
                shapePts = new Vector2[DomeSteps + 1];
                for (int i = 0; i <= DomeSteps; i++)
                {
                    float t = Mathf.PI * (1f - (float)i / DomeSteps);      // π → 0 : 왼쪽 바닥 → 꼭대기 → 오른쪽 바닥
                    shapePts[i] = new Vector2(Box * 0.5f + Box * 0.5f * Mathf.Cos(t), Box * (1f - Mathf.Sin(t)));
                }
                name = "pet-dome";
            }
            sprite = CraftFxPoly.Bake(name, shapePts);
            color = Top;
        }
    }
}
