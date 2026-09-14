using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Forge.Game.Ui
{
    /// <summary>
    /// T349 — 촬영 카메라 한 자리. 레포의 촬영·픽셀 자 열여섯이 새 카메라에 <c>Camera.CopyFrom(Camera.main)</c> 만 하는데,
    /// <c>CopyFrom</c> 은 URP 의 추가 데이터(<see cref="UniversalAdditionalCameraData"/> — renderPostProcessing · volumeLayerMask ·
    /// antialiasing · renderShadows …)를 **옮기지 않는다**. 그래서 모든 PNG 의 3D 띠가 톤맵(T1)·노출(T9)·색 보정(T341) 없이 찍혔다
    /// (런 503 실측 · T341 이 <c>WorldFrameShotTests</c> 한 곳만 손으로 켰다 · 결정 550).
    /// 여기서 <c>CopyFrom</c> 과 추가 데이터 복사를 한 번에 한다 — 촬영 자는 이것을 부르고, 그 뒤 제 framing(<c>Bootstrap.ApplyGameAreaProjection</c> ·
    /// cullingMask · clearFlags …)은 전처럼 손으로 잇는다. 게임 카메라가 무엇을 켜 두었든 **그대로** 따라간다(값을 여기서 정하지 않는다 · §1).
    /// </summary>
    public static class ShotCam
    {
        /// <summary>새 카메라 오브젝트를 만들고 <paramref name="main"/> 을 통째로 복사한다(CopyFrom + URP 추가 데이터). <paramref name="rt"/> 가 있으면 그리로 찍는다.</summary>
        public static Camera From(Camera main, string name, RenderTexture rt = null)
        {
            GameObject go = new GameObject(name);
            Camera cam = go.AddComponent<Camera>();
            if (main != null) cam.CopyFrom(main);
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            if (rt != null) cam.targetTexture = rt;
            CopyUrp(main, cam);
            return cam;
        }

        /// <summary>
        /// <paramref name="from"/> 의 URP 추가 데이터를 <paramref name="to"/> 에 복사한다(없으면 붙인다). <paramref name="from"/> 에 그 데이터가 없으면
        /// 아무것도 정하지 않는다 — 모르는 값을 지어내지 않는다. 돌려주는 것은 <paramref name="to"/> 쪽 데이터(호출한 쪽이 더 손볼 수 있게).
        /// </summary>
        public static UniversalAdditionalCameraData CopyUrp(Camera from, Camera to)
        {
            UniversalAdditionalCameraData dst = to.GetComponent<UniversalAdditionalCameraData>();
            if (dst == null) dst = to.gameObject.AddComponent<UniversalAdditionalCameraData>();
            UniversalAdditionalCameraData src = from != null ? from.GetComponent<UniversalAdditionalCameraData>() : null;
            if (src == null) return dst;
            dst.renderType = src.renderType;
            dst.renderPostProcessing = src.renderPostProcessing;
            dst.renderShadows = src.renderShadows;
            dst.requiresDepthOption = src.requiresDepthOption;
            dst.requiresColorOption = src.requiresColorOption;
            dst.antialiasing = src.antialiasing;
            dst.volumeLayerMask = src.volumeLayerMask;
            return dst;
        }

        /// <summary>두 카메라의 URP 추가 데이터가 «찍는 결과를 가르는 항» 에서 같은가 — 자가 쓴다.</summary>
        public static bool SameUrp(Camera a, Camera b)
        {
            UniversalAdditionalCameraData x = a != null ? a.GetComponent<UniversalAdditionalCameraData>() : null;
            UniversalAdditionalCameraData y = b != null ? b.GetComponent<UniversalAdditionalCameraData>() : null;
            if (x == null || y == null) return x == y;
            return x.renderType == y.renderType && x.renderPostProcessing == y.renderPostProcessing && x.renderShadows == y.renderShadows
                && x.requiresDepthOption == y.requiresDepthOption && x.requiresColorOption == y.requiresColorOption
                && x.antialiasing == y.antialiasing && x.volumeLayerMask == y.volumeLayerMask;
        }
    }
}
