// T46 — UnityEngine.TestRunner «컴파일용» 스텁 (tools/dotnet/TestsPlay 전용 · 유니티 프로젝트에는 들어가지 않는다).
// 실물(com.unity.test-framework · UnityEngine.TestRunner/Utils/ITestRunCallback.cs · TestRunCallbackAttribute.cs)에서
// 서명을 그대로 옮겼다 — 1.4.6 과 2.0.1-pre.18 이 글자까지 같다(2026-09-12 정본 대조 · 프로젝트는 1.6.0).
// ⚠ PlayMode 테스트가 새 TestRunner API 를 쓰면 여기 같은 서명을 더한다. 실물에 없는 멤버를 넣지 말 것(«로컬만 초록» 이 생긴다).
using System;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestRunner
{
    /// <summary>실물: 러너가 테스트 진행을 알려 주는 갈래(에디터·플레이어 양쪽). `TestRunCallbackAttribute` 로 등록한다.</summary>
    public interface ITestRunCallback
    {
        void RunStarted(ITest testsToRun);
        void RunFinished(ITestResult testResults);
        void TestStarted(ITest test);
        void TestFinished(ITestResult result);
    }

    /// <summary>실물: 어셈블리 특성 — `ITestRunCallback` 을 구현한 형식을 러너에 붙인다(실물은 구현 안 하면 ArgumentException).</summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class TestRunCallbackAttribute : Attribute
    {
        public TestRunCallbackAttribute(Type type) { }
    }
}
