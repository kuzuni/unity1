using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Forge.Game.Ui;
using Forge.Game.Voxel;

namespace Forge.Game.Battle
{
    /// <summary>
    /// 플레이어 정보 팝업의 미니 씬(T97 · 원작 `Scene3D.previewStart/previewStop` 자리). 팝업의 프리뷰 상자에 **지금 전투 장면**을 작은 렌더 텍스처로
    /// 그려 넣는다 — 본 카메라(<c>Camera.main</c>)를 복사한 카메라가 상자 크기의 RT 에 그리고 <see cref="RawImage"/> 가 그것을 상자에 채운다.
    /// 팝업이 닫히면(<see cref="PlayerInfoPopup.PreviewStop"/> 또는 상자가 통째로 파괴되면) 카메라·RT 를 그 자리에서 반납한다(60fps 규칙).
    /// 원작은 별도 미니 디오라마(`previewBuild` · 굴러가는 지면·능선·소품 + 영웅 리그)인데 그 소품·능선은 T35(`SIMPLE_BG=false`) 갈래라
    /// 지금은 «같은 씬을 작은 칸에» 로 둔다(지시서 T97 · 결정 기록). 씬 파일·`BattleScene.cs` 는 안 고친다 — 훅에 대입만 한다.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class BattlePreview : MonoBehaviour
    {
        public static BattlePreview Instance { get; private set; }

        /// <summary>미니 씬이 서 있는가(팝업이 열려 RT 카메라가 도는 동안 true).</summary>
        public bool Active { get { return cam != null; } }
        public Camera PreviewCamera { get { return cam; } }
        public RenderTexture Texture { get { return rt; } }
        public RawImage Image { get { return img; } }
        /// <summary>선 횟수(테스트).</summary>
        public int StartCount { get; private set; }
        /// <summary>정본 미니 씬 카메라 리그(scene.json `PREVIEW_CAM`)에 섰는가 — false 면 본 카메라 복사(T97 방식 · 표가 없을 때만).</summary>
        public bool Rigged { get; private set; }

        private Camera cam;
        private RenderTexture rt;
        private RawImage img;
        private RectTransform host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != null) return;
            foreach (Bootstrap b in Resources.FindObjectsOfTypeAll<Bootstrap>())
            {
                if (!b.gameObject.scene.isLoaded) continue;
                Create(b.transform);
                return;
            }
        }

        public static BattlePreview Create(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("BattlePreview");
            go.transform.SetParent(parent, false);
            return go.AddComponent<BattlePreview>();
        }

        private void Awake()
        {
            Instance = this;
            // 원작 ui.js 5216: 팝업이 DOM 에 붙은 뒤 Scene3D.previewStart(host) · closePlayerInfo → previewStop
            PlayerInfoPopup.PreviewStart = Start;
            PlayerInfoPopup.PreviewStop = Stop;
        }

        private void OnDestroy()
        {
            Stop();
            if (Instance == this)
            {
                Instance = null;
                if (PlayerInfoPopup.PreviewStart == (System.Func<RectTransform, bool>)Start) PlayerInfoPopup.PreviewStart = null;
                if (PlayerInfoPopup.PreviewStop == (System.Action)Stop) PlayerInfoPopup.PreviewStop = null;
            }
        }

        /// <summary>원작 `previewStart(container)` — 못 서면 false(호출자가 정본 폴백을 그린다).</summary>
        public bool Start(RectTransform container)
        {
            Stop();
            if (container == null) return false;
            Camera main = Camera.main;
            if (main == null || BattleScene.Instance == null) return false;
            // 상자 크기(오버레이 캔버스 = 화면 픽셀 · UiRoot 주석) — 레이아웃 전이면 원작 previewResize 처럼 다음 프레임에 다시 잰다
            Vector2 size = container.rect.size;
            int w = Mathf.RoundToInt(size.x), h = Mathf.RoundToInt(size.y);
            if (w < 2 || h < 2) { w = 2; h = 2; }
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.name = "pinfo-scene";
            var go = new GameObject("pinfo-scene-cam");
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            cam.CopyFrom(main);                 // 클리어·배경·컬링·깊이 = 본 카메라(같은 씬)
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            Rigged = ApplyRig(cam, w, h);       // 정본 previewBuild 리그(T105) — 표가 없으면 본 리그 복사(T97)
            if (!Rigged) { cam.ResetProjectionMatrix(); cam.aspect = (float)w / h; }
            cam.targetTexture = rt;
            cam.depth = main.depth - 1;
            img = new GameObject("pinfo-scene-canvas", typeof(RectTransform)).AddComponent<RawImage>();
            img.gameObject.layer = container.gameObject.layer;
            img.rectTransform.SetParent(container, false);
            UiKit.Fill(img.rectTransform);
            img.texture = rt;
            img.raycastTarget = false;
            img.rectTransform.SetAsFirstSibling();
            host = container;
            StartCount++;
            return true;
        }

        /// <summary>
        /// 정본 `previewBuild()` 의 카메라 리그(scene.json `PREVIEW_CAM` · fov 42 · (0.1,1.95,4.3) → (0.05,0.92,0) · 영웅 리그 (0,0,0.4) · three 좌표)를
        /// **지금 전투의 영웅 리그 자리**에 건다 — 원작 미니 씬은 «주인공이 주제이므로 본편보다 바짝 붙는다»(멀면 초록 판에 점 하나).
        /// 본 리그를 그대로 복사하면 세로 화각 62° 가 납작한 칸에 그대로 걸려 띠 아래(시트 뒤 흙 절벽)까지 비친다(T105 · 런 195 실측). 표가 없으면 false.
        /// </summary>
        static bool ApplyRig(Camera cam, int w, int h)
        {
            var world = Forge.Game.Map.World.Instance;
            var d = world != null ? world.Defs : null;
            BattleScene bs = BattleScene.Instance;
            if (d == null || !d.HasPreviewCam || bs == null || bs.Hero == null || bs.Hero.Rig == null) return false;
            // 정본 미니 씬의 원점 = 영웅 리그가 (0,0,0.4) 에 서 있는 곳 → 지금 영웅 리그 자리에서 그만큼 되돌린 자리
            Vector3 origin = bs.Hero.Rig.transform.position - ThreeSpace.Pos(d.PvHero);
            cam.ResetProjectionMatrix();
            cam.fieldOfView = (float)d.PvFov;
            cam.nearClipPlane = (float)d.PvNear;
            cam.farClipPlane = (float)d.PvFar;
            cam.aspect = (float)w / h;
            cam.transform.position = origin + ThreeSpace.Pos(d.PvPos);
            cam.transform.LookAt(origin + ThreeSpace.Pos(d.PvLook), Vector3.up);
            return true;
        }

        /// <summary>원작 `previewStop()` — 카메라·RT 를 걷는다(팝업이 닫힐 때).</summary>
        public void Stop()
        {
            if (cam != null) { cam.targetTexture = null; Destroy(cam.gameObject); }
            cam = null;
            if (img != null) Destroy(img.gameObject);
            img = null;
            if (rt != null) { rt.Release(); Destroy(rt); }
            rt = null;
            host = null;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            // 상자가 통째로 파괴됐다(HideAll 등 PreviewStop 을 안 거친 길) → 그 자리에서 걷는다
            if (host == null || img == null) { Stop(); return; }
            Camera main = Camera.main;
            if (main == null) { Stop(); return; }
            // 본 카메라를 따라간다(셰이크·행군·FOV 펀치) · 상자 크기가 바뀌면 RT 를 다시 만든다(원작 previewResize)
            if (!Rigged || !ApplyRig(cam, rt.width, rt.height))
            {
                cam.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
                cam.fieldOfView = main.fieldOfView;
            }
            Vector2 size = host.rect.size;
            int w = Mathf.RoundToInt(size.x), h = Mathf.RoundToInt(size.y);
            if (w >= 2 && h >= 2 && (w != rt.width || h != rt.height))
            {
                cam.targetTexture = null;
                rt.Release(); Destroy(rt);
                rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                rt.name = "pinfo-scene";
                cam.targetTexture = rt;
                cam.aspect = (float)w / h;
                img.texture = rt;
            }
        }
    }
}
