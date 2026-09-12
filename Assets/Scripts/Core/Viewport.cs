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

        /// <summary>
        /// 원작의 3D 캔버스 상자(`#game-area`)를 앱 상자 안에서 잘라 낸다 — 원작 `index.html` 은 `#app` 이 세로 flex 라
        /// 캔버스가 **상단바 밑 ~ 장비 시트 위** 띠만 차지한다(`#game-area { flex: 1 1 auto }`).
        /// 카메라 FOV 는 «뷰포트 높이» 에 걸리므로, 전체 화면에 걸면 같은 리그라도 화면 한가운데를 보게 되어
        /// 원작보다 한참 아래를 비춘다(T54 실측: 영웅이 장비 시트 뒤로 통째로 숨었다).
        /// </summary>
        /// <param name="app">레터박스로 구한 앱 상자</param>
        /// <param name="topFrac">앱 높이에서 캔버스가 시작하는 자리(위에서부터 · 원작 `topbar_h`)</param>
        /// <param name="bottomFrac">끝나는 자리(위에서부터 · 원작 `sheet_top`)</param>
        public static ViewportRect GameArea(ViewportRect app, float topFrac, float bottomFrac)
        {
            if (!(bottomFrac > topFrac) || topFrac < 0f || bottomFrac > 1f) return app;   // 표가 비었거나 뒤집혔으면 앱 상자 그대로
            // 뷰포트 y 는 아래에서 위로 — «위에서부터» 인 표값을 뒤집는다.
            float h = app.H * (bottomFrac - topFrac);
            float y = app.Y + app.H * (1f - bottomFrac);
            return new ViewportRect(app.X, y, app.W, h);
        }
    }
}
