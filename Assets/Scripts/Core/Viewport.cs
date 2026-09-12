namespace Forge.Core
{
    /// <summary>정규화 뷰포트 사각형(0~1). UnityEngine.Rect 를 쓰지 않는 것은 Core 가 엔진 참조 0 이기 때문이다.</summary>
    public readonly struct ViewportRect
    {
        public readonly float X, Y, W, H;
        public ViewportRect(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
    }

    /// <summary>
    /// 9:16 세로 레터박스(ROUTINE T1·T18). 화면이 목표보다 넓으면 좌우를 비우고(필러박스), 좁으면 위아래를 비운다.
    /// 순수 계산이라 dotnet 하니스의 EditMode 테스트가 그대로 돈다.
    /// </summary>
    public static class Viewport
    {
        /// <param name="screenW">화면 픽셀 폭</param>
        /// <param name="screenH">화면 픽셀 높이</param>
        /// <param name="aspect">목표 가로/세로 비 (9:16 이면 0.5625)</param>
        public static ViewportRect Letterbox(int screenW, int screenH, float aspect)
        {
            if (screenW <= 0 || screenH <= 0 || aspect <= 0f) return new ViewportRect(0f, 0f, 1f, 1f);
            float screenAspect = screenW / (float)screenH;
            if (screenAspect > aspect)
            {
                float w = aspect / screenAspect;
                return new ViewportRect((1f - w) * 0.5f, 0f, w, 1f);
            }
            if (screenAspect < aspect)
            {
                float h = screenAspect / aspect;
                return new ViewportRect(0f, (1f - h) * 0.5f, 1f, h);
            }
            return new ViewportRect(0f, 0f, 1f, 1f);
        }
    }
}
