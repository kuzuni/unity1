using UnityEngine;
using Forge.Game;
using Forge.Game.Ui;
using Forge.Game.Gallery;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T490 — 판정 PNG 를 굽는 **공용 자리**. §1 «실제 화면을 본다» 는 조형·연출을 바꾼 자리마다 PNG 를 요구하는데,
    /// 그 50줄이 자 파일마다 복사돼 **열 벌**이 됐고 그 사이에 **조용히 벌어졌다**(T178 50회차 등재 · 아래 «벌어진 자리»).
    /// `PlayLog`·`RedLog` 와 같은 «자끼리 쓰는 도우미» 자리다.
    ///
    /// 두 꼴이 있고 **둘 다 뜻이 있다** — 하나로 합치지 마라:
    /// <list type="bullet">
    /// <item><see cref="Capture"/> — **UI 층만** 찍는다(`cullingMask` 를 UI 한 층으로 **덮어쓴다** · URP 추가 데이터를 안 옮긴다). 겹·색·자리를 재는 자리가 쓴다.</item>
    /// <item><see cref="CaptureWithWorld"/> — 3D 띠를 **같이** 찍는다(`cullingMask` 에 UI 층을 **더하고** <see cref="ShotCam.CopyUrp"/> 로 URP 추가 데이터를 옮긴다). 무대 위 연출(보스 경고 꼴)이 쓴다.</item>
    /// </list>
    ///
    /// ⚠ **함정 셋** — 복사본마다 손으로 고쳐 오던 것이라 여기 한 번만 적는다:
    /// <list type="number">
    /// <item><b>T135 ⓑ</b> — 판정 PNG 는 **카드 팝이 끝난 모습**이어야 한다(<see cref="CardPop.SettleAll"/> · T128 ⓒ 와 같은 갈래).</item>
    /// <item><b>T349 4회차</b> — UI 만 찍는 꼴에 **URP 포스트를 켜지 마라**: 정본은 `filter` 를 `#game3d` 에만 걸고 HUD·패널·팝업엔 안 건다(T357).
    ///       3회차에 켰다가 순백 코어가 rgb 215 로 내려가 자가 빨갰다. 반대로 3D 띠를 **같이** 찍는 꼴은 `CopyFrom` 이 URP 추가 데이터
    ///       (`renderPostProcessing`·`volumeLayerMask`·`antialiasing`·`renderShadows`)를 **안 옮기므로** `CopyUrp` 가 있어야 한다(런 503 실측).</item>
    /// <item><b>T124</b> — 저장이 실패해도 자를 넘어뜨리지 않는다(`try` 로 삼키고 콘솔에 한 줄). 촬영은 판정의 **곁**이라 저장 실패가 단언을 가리면 안 된다.</item>
    /// </list>
    ///
    /// **벌어진 자리(2026-11-26 실측 · 열 벌 전수)**: UI 만 찍는 아홉 중 <c>CardPop.SettleAll()</c> 을 가진 것이 **둘뿐**이었다
    /// (`SurfaceArtTests`·`AgePatternTests`) — 나머지 일곱은 T135 ⓑ 가 든 뒤에도 그대로였다. 바로 이 절이 막으려는 병이다.
    /// 옮기는 차례는 T490 절대로 **그 파일이 열리는 회차마다 한 줄씩**이다(열 파일이 저마다 다른 축의 임자 것이라 한꺼번에 하면 lock 과 부딪힌다).
    /// </summary>
    public static class PlayShot
    {
        /// <summary>UI 층만 찍어 <c>ui-screens/&lt;saveAs&gt;.png</c> 로 굽는다(위 함정 셋이 여기 들어 있다).</summary>
        public static void Capture(string saveAs) { Shoot(saveAs, false); }

        /// <summary>3D 띠까지 같이 찍는다 — 무대 위 연출을 재는 자리만 쓴다(보스 경고 꼴).</summary>
        public static void CaptureWithWorld(string saveAs) { Shoot(saveAs, true); }

        static void Shoot(string saveAs, bool withWorld)
        {
            CardPop.SettleAll();   // 함정 ⑴ T135 ⓑ
            UiRoot root = UiRoot.Instance;
            Canvas canvas = root.Canvas;
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevPlane = canvas.planeDistance;
            RenderTexture prevActive = RenderTexture.active;
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            GameObject camGo = new GameObject("playshot-cam");
            Camera cam = camGo.AddComponent<Camera>();
            try
            {
                if (Camera.main != null) cam.CopyFrom(Camera.main);
                // 함정 ⑵ T349 — 3D 를 같이 찍을 때만 URP 추가 데이터를 옮긴다.
                if (withWorld) ShotCam.CopyUrp(Camera.main, cam);
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                cam.ResetProjectionMatrix();
                int ui = 1 << canvas.gameObject.layer;
                cam.cullingMask = withWorld ? (cam.cullingMask | ui) : ui;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                root.Layout();
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
                tex.Apply(false);
                // 함정 ⑶ T124 — 저장 실패는 삼킨다(판정의 곁이다).
                try { GallerySheet.Save(tex, saveAs); } catch (System.Exception e) { Debug.Log("[T490] PNG 저장 생략: " + e.Message); }
                Object.Destroy(tex);
            }
            finally
            {
                RenderTexture.active = prevActive;
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
                canvas.planeDistance = prevPlane;
                root.Layout();
                cam.targetTexture = null;
                Object.Destroy(camGo);
                Object.Destroy(rt);
            }
        }
    }
}
