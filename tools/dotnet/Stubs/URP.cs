// dotnet 검사 빌드 전용 스텁 — Unity 의 Unity.RenderPipelines.Universal.Runtime(URP 17) 중 이 레포가 쓰는 표면만 서명을 맞춘다.
// Assets 에는 들어가지 않는다(유니티에서는 진짜 URP 가 잡힌다). 새 API 를 쓰면 여기에도 같은 서명을 더한다.
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public enum CameraRenderType { Base = 0, Overlay = 1 }
    public enum CameraOverrideOption { Off = 0, On = 1, UsePipelineSettings = 2 }
    public enum AntialiasingMode { None = 0, FastApproximateAntialiasing = 1, SubpixelMorphologicalAntiAliasing = 2, TemporalAntiAliasing = 3 }

    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public CameraRenderType renderType { get; set; }
        public bool renderPostProcessing { get; set; }
        public bool renderShadows { get; set; }
        public CameraOverrideOption requiresDepthOption { get; set; }
        public CameraOverrideOption requiresColorOption { get; set; }
        public AntialiasingMode antialiasing { get; set; }
        public bool allowXRRendering { get; set; }
        public LayerMask volumeLayerMask { get; set; }
        public void SetRenderer(int index) { }
    }
}

namespace UnityEngine.Rendering
{
    // Volume 프레임워크 — T181 이 쓰는 표면만(진짜 URP 에서는 UnityEngine.Rendering 에 있다).
    public class VolumeParameter { public bool overrideState { get; set; } }
    public class VolumeParameter<T> : VolumeParameter { public T value { get; set; } }
    public class MinFloatParameter : VolumeParameter<float> { }
    public class ClampedFloatParameter : VolumeParameter<float> { }
    public class BoolParameter : VolumeParameter<bool> { }

    public class VolumeComponent : ScriptableObject { public bool active { get; set; } }

    public class VolumeProfile : ScriptableObject
    {
        public T Add<T>(bool overrides = false) where T : VolumeComponent => ScriptableObject.CreateInstance<T>();
        public bool TryGet<T>(out T component) where T : VolumeComponent { component = null; return false; }
    }

    public class Volume : MonoBehaviour
    {
        public bool isGlobal { get; set; }
        public float priority { get; set; }
        public float weight { get; set; }
        public VolumeProfile profile { get; set; }
        public VolumeProfile sharedProfile { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold = new MinFloatParameter();
        public MinFloatParameter intensity = new MinFloatParameter();
        public ClampedFloatParameter scatter = new ClampedFloatParameter();
        public BoolParameter highQualityFiltering = new BoolParameter();
    }
}
