namespace Forge.Core
{
    /// <summary>순수 C# 엔진 어셈블리의 자리표. UnityEngine 을 참조하지 않는다(asmdef noEngineReferences · dotnet 이 강제).</summary>
    public static class CoreInfo
    {
        /// <summary>정본(kuzuni/wwwww web/) 을 읽어 옮기는 이식 — 수치는 코드에 박지 않고 StreamingAssets/data 의 JSON 에서 읽는다.</summary>
        public const string Source = "kuzuni/wwwww web/";
        public static int Add(int a, int b) { return a + b; }
    }
}
