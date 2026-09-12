using UnityEngine;
using Forge.Core;

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
            Apply();
        }

        private void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH) Apply();
        }

        /// <summary>현재 화면 크기로 레터박스를 다시 계산해 카메라 뷰포트에 적용한다.</summary>
        public void Apply()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            Camera cam = TargetCamera;
            if (cam == null) return;
            ViewportRect r = Viewport.Letterbox(lastW, lastH, PortraitAspect);
            cam.rect = new Rect(r.X, r.Y, r.W, r.H);
        }
    }
}
