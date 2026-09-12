using UnityEngine;

namespace Forge.Game
{
    /// <summary>MonoBehaviour 계층의 자리표 — T1 이 Bootstrap 으로 바꾼다.</summary>
    public static class GameInfo
    {
        public static string Describe() { return Forge.Core.CoreInfo.Source + " → Unity " + Application.unityVersion; }
    }
}
