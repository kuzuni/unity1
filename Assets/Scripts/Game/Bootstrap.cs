using UnityEngine;
using Forge.Core;
using Forge.Game.Ui;

namespace Forge.Game
{
    /// <summary>
    /// 부팅 뿌리(ROUTINE T1). 씬의 Main Camera 를 9:16 세로 레터박스로 고정한다 — 카메라 리그 값(위치·FOV·near/far)은
    /// 씬(SampleScene)이 원작 scene3d.js 의 CAM_POS·CAM_LOOK_Y·CAM_FOV 그대로 쥐고, 여기서는 화면 비만 맞춘다.
    /// 뒤 작업(T18 UI · T8 전투 씬)이 이 오브젝트 아래에서 자기 것을 세운다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrap : MonoBehaviour
    {
        /// <summary>세로 폰 화면비(원작 UI 가 읽히던 9:16 · ROUTINE T18 «9:16 레터박스 캔버스»).</summary>
        public const float PortraitAspect = 9f / 16f;

        /// <summary>목표 프레임(주인 지시 2026-09-12 «60fps 로 프레임 돌아야» · ROUTINE §1 «60fps» · T44).</summary>
        public const int TargetFps = 60;

        [Tooltip("비우면 Camera.main")]
        [SerializeField] private Camera targetCamera;

        private int lastW = -1, lastH = -1;

        public Camera TargetCamera
        {
            get
            {
                if (targetCamera == null) targetCamera = Camera.main;
                return targetCamera;
            }
        }

        private void Awake()
        {
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;
            ApplyFrameRate();
            Apply();
        }

        /// <summary>
        /// 60fps 고정(T44). WebGL 은 브라우저 rAF 가 프레임을 주므로 원작(<c>requestAnimationFrame</c>)대로 두고 건드리지 않는다 —
        /// 거기서 <c>targetFrameRate</c> 를 박으면 120Hz 기기가 60 으로 깎인다. 화면 잠금은 원작에 wakeLock 이 없으므로 시스템 설정 그대로.
        /// </summary>
        public static void ApplyFrameRate()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // 원작은 rAF 에 실려 돈다 — 아무것도 세우지 않는다.
#else
            QualitySettings.vSyncCount = 0;          // vSync 가 켜져 있으면 targetFrameRate 를 무시한다
            Application.targetFrameRate = TargetFps;
#endif
            if (Application.isMobilePlatform) Screen.sleepTimeout = SleepTimeout.SystemSetting;   // 원작(웹)에 wakeLock 이 없다
        }

        private void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH) Apply();
        }

        /// <summary>
        /// 현재 화면 크기로 레터박스를 다시 계산해 카메라 뷰포트에 적용한다.
        /// 카메라가 쓰는 것은 앱 상자 전체가 아니라 원작의 3D 캔버스 상자(`#game-area` = 상단바 밑 ~ 장비 시트 위)다 — T54.
        /// </summary>
        public void Apply()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            Camera cam = TargetCamera;
            if (cam == null) return;
            // T54 3회차 되돌림: 카메라 rect 를 원작 `#game-area` 띠로 좁혔더니 **URP 가 세계를 아예 안 그렸다**
            // (런 116 실측 · 촬영 두 길 모두 안개색 단색 · 지면·HP 바까지 사라졌다). rect 는 앱 상자로 두고,
            // 원작 캔버스 상자 framing 은 다음 회차에 **투영 행렬**(off-center frustum)로 준다 — 그 길은 rect·클리어를 안 건드린다.
            ViewportRect r = Viewport.Letterbox(lastW, lastH, PortraitAspect);
            cam.rect = new Rect(r.X, r.Y, r.W, r.H);
        }

        /// <summary>원작 `#game-area` 의 위·아래 자리(앱 높이 비). 수치는 UI 카탈로그(정본 판독표)가 쥔다 — 코드에 박지 않는다(§1).</summary>
        public static float GameAreaTop { get { return LayoutOr("topbar_h", 0f); } }
        public static float GameAreaBottom { get { return LayoutOr("sheet_top", 1f); } }

        /// <summary>카탈로그가 아직 없거나 키가 비면 «앱 상자 전체» 로 물러난다 — 부팅이 이것 때문에 죽지 않는다.</summary>
        static float LayoutOr(string key, float fallback)
        {
            try
            {
                UiCatalog c = UiCatalog.Instance;
                return c == null ? fallback : c.Layout(key);
            }
            catch (System.Exception)
            {
                return fallback;
            }
        }
    }
}
