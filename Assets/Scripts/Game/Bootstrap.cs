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
            ApplyRunInBackground();
            Apply();
            // T142 — 부팅 로딩 덮개. 정본은 이것을 `#app` **앞**에 두어 파서가 제일 먼저 만나게 한다
            // (`index.html` 21 «마크업도 #app 앞»). 클론에서 그 «제일 먼저» 에 해당하는 자리가 여기다 —
            // 부팅 뿌리는 `DefaultExecutionOrder(-1000)` 이라 씬의 어떤 호스트보다 먼저 돈다.
            // 덮개는 앱 캔버스가 아니라 제 오버레이 캔버스에 서고, 여섯 신호가 다 서면 스스로 사라진다.
            BootLoading.Begin();
        }

        /// <summary>
        /// 백그라운드에서도 돈다(T88 · 주인 지시 2026-09-13 «백그라운드에서도 플레이 되게»). 창이 초점을 잃어도(데스크톱 · WebGL 캔버스 blur)
        /// 루프가 멈추지 않는다 — `ProjectSettings.asset` 의 `runInBackground: 1` 과 같은 값이고, 여기서도 세워 빌드 설정이 되돌아가도 유지한다.
        /// 폰은 OS 가 앱을 재우므로 이것만으로는 안 된다 — 깨어날 때의 따라잡기는 <see cref="AppLifecycle"/>(원작 `visibilitychange` 자리)가 한다.
        /// </summary>
        public static void ApplyRunInBackground()
        {
            Application.runInBackground = true;
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
            ApplyGameAreaProjection(cam);
        }

        /// <summary>
        /// 원작 캔버스 상자(`#game-area` = 상단바 밑 ~ 장비 시트 위)에 카메라를 건 것과 **같은 그림**이 그 띠에 오도록
        /// 중심을 민 절두체를 건다(T54 · 셈은 <see cref="Viewport.GameAreaFrustum"/>). rect 는 앱 상자 그대로다 —
        /// rect 를 띠로 좁히는 길은 URP 가 세계를 안 그려서 되돌렸다(결정 176).
        /// 표가 비면 «앱 상자에 건 카메라» 로 물러나므로 옛 그림 그대로다.
        /// </summary>
        public static void ApplyGameAreaProjection(Camera cam)
        {
            if (cam == null || cam.orthographic) return;
            // 가로세로비는 **그 카메라가 실제로 그리는 상자**에서 잰다 — 촬영 RT(노치 컷은 540×1170)는 9:16 이 아니다.
            float aspect = cam.pixelHeight > 0 ? cam.pixelWidth / (float)cam.pixelHeight : PortraitAspect;
            Viewport.FrustumEdges f = Viewport.GameAreaFrustum(cam.nearClipPlane, cam.fieldOfView, aspect, GameAreaTop, GameAreaBottom);
            cam.projectionMatrix = Matrix4x4.Frustum(f.Left, f.Right, f.Bottom, f.Top, cam.nearClipPlane, cam.farClipPlane);
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
