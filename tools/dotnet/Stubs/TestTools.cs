// T48 — UnityEngine.TestTools «컴파일용» 스텁 (tools/dotnet/TestsPlay 전용 · 유니티 프로젝트에는 들어가지 않는다).
// 실물(com.unity.test-framework 1.x)과 «서명» 만 같게 둔다 — 여기서 쓰는 것은 형식 검사뿐이고 실행은 CI 유니티 잡이 한다.
// ⚠ PlayMode 테스트가 새 TestTools API 를 쓰면 여기 같은 서명을 더한다(안 그러면 «로컬만 빨강»). 실물에 없는 멤버를 넣지 말 것(«로컬만 초록» 이 생긴다).
using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace UnityEngine.TestTools
{
    /// <summary>실물: CombiningStrategyAttribute + ISimpleTestBuilder + IImplyFixture · IEnumerator 를 돌려주는 코루틴 테스트. 컴파일 검사에는 «메서드 특성» 이면 된다.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UnityTestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UnitySetUpAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UnityTearDownAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public class UnityPlatformAttribute : Attribute
    {
        public RuntimePlatform[] include;
        public RuntimePlatform[] exclude;
        public UnityPlatformAttribute() { }
        public UnityPlatformAttribute(params RuntimePlatform[] include) { this.include = include; }
    }

    /// <summary>실물 LogAssert — 기대한 로그를 등록하고, 예고 없는 Error/Exception/Assert 가 테스트를 실패시키는 규칙을 끈다/켠다.</summary>
    public static class LogAssert
    {
        public static bool ignoreFailingMessages { get; set; }
        public static void Expect(LogType type, string message) { }
        public static void Expect(LogType type, Regex message) { }
        public static void NoUnexpectedReceived() { }
    }

    public interface IPrebuildSetup { void Setup(); }
    public interface IPostBuildCleanup { void Cleanup(); }
    public interface IMonoBehaviourTest { bool IsTestFinished { get; } }

    /// <summary>실물: MonoBehaviourTest&lt;T&gt; : CustomYieldInstruction where T : MonoBehaviour, IMonoBehaviourTest.</summary>
    public class MonoBehaviourTest<T> : CustomYieldInstruction where T : MonoBehaviour, IMonoBehaviourTest
    {
        public T component { get; private set; }
        public GameObject gameObject { get { return component != null ? component.gameObject : null; } }
        public MonoBehaviourTest(bool dontDestroyOnLoad = true) { component = new GameObject(typeof(T).Name).AddComponent<T>(); }
        public override bool keepWaiting { get { return component != null && !component.IsTestFinished; } }
    }
}
