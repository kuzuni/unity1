using System;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

// 유니티가 주는 정식 길 — 어셈블리에 한 번 붙이면 러너가 테스트마다 TestStarted/TestFinished 를 부른다.
// (NUnit 의 어셈블리 단위 `ITestAction` 은 유니티에서 **절대 안 불린다**: UTF 의 `TestActionCommand` 는
//  `BeforeAfterTestCommandBase.GetTestActions(methodInfo)` 로 **테스트 메서드에 붙은 것만** 모은다 — 정본 패키지 실측 1.4.6.)
[assembly: TestRunCallback(typeof(Forge.Tests.PlayMode.RedLogCallbacks))]

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T46 — PlayMode 런이 «왜 빨갰는가» 를 `ui-screens/playmode-red.txt` 에 남긴다(CI 가 그 폴더를 `screens` 브랜치로 올린다 · ROUTINE §5).
    /// CI 유니티 잡 로그는 꼬리 5000줄만 남아 실패 픽스처의 메시지·스택이 잘려 나가고(2026-09-12 런 46 실측 · 26 빨강인데 이름만 보였다)
    /// 아티팩트는 워커 컨테이너의 프록시가 막는다. 그래서 러너 바깥이 아니라 **테스트 안**에서 적는다.
    /// 다음 워커는 `git fetch origin screens && git show origin/screens:playmode-red.txt` 로 읽는다.
    /// </summary>
    public static class RedLogFile
    {
        /// <summary>결과 폴더 — 시트 PNG 와 같은 자리(`GallerySheet.OutDir` · CI 가 이 폴더를 올린다).</summary>
        public const string OutDir = "ui-screens";

        /// <summary>파일 이름. `screens` 브랜치에서 이 이름으로 읽는다.</summary>
        public const string FileName = "playmode-red.txt";

        private const int MessageCap = 2000;
        private const int StackLineCap = 24;
        private const int LineCap = 600;
        private const int RedPerTestCap = 40;

        private static bool installed;
        private static string currentTest = "(테스트 바깥)";
        private static int redInTest;
        private static readonly object Gate = new object();

        /// <summary>콘솔 빨강을 파일로 흘리기 시작한다(여러 번 불러도 한 번만 붙는다).</summary>
        public static void Install()
        {
            lock (Gate)
            {
                if (installed) return;
                installed = true;
            }
            Application.logMessageReceived += OnLog;
            Header();
        }

        /// <summary>플레이 모드에 들어가면 러너가 첫 테스트를 세우기 전에 붙는다 — 테스트 바깥에서 터진 빨강도 남는다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            Install();
        }

        /// <summary>테스트 하나가 시작될 때 이름을 바꾼다(뒤에 오는 빨강은 그 테스트 것이다).</summary>
        public static void BeginTest(string fullName)
        {
            lock (Gate)
            {
                currentTest = string.IsNullOrEmpty(fullName) ? "(이름 없음)" : fullName;
                redInTest = 0;
            }
            Append("── " + currentTest);
        }

        /// <summary>테스트 하나가 끝날 때 결과 한 줄(+ 실패면 메시지·스택)을 적는다.</summary>
        public static void EndTest(string fullName, string status, string message, string stack)
        {
            bool passed = string.Equals(status, "Passed", StringComparison.Ordinal);
            string head = (passed ? "PASS " : "FAIL ") + fullName + (passed ? "" : " · " + status);
            if (passed && string.IsNullOrEmpty(message))
            {
                Append(head);
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(head);
            if (!string.IsNullOrEmpty(message)) sb.Append('\n').Append(Indent("  msg  ", Clamp(message, MessageCap)));
            if (!passed && !string.IsNullOrEmpty(stack)) sb.Append('\n').Append(Indent("  at   ", Head(stack, StackLineCap)));
            Append(sb.ToString());
        }

        /// <summary>런 전체가 끝날 때 총계 한 줄.</summary>
        public static void EndRun(string status, int passCount, int failCount, int skipCount, double seconds)
        {
            Append("== 런 끝: " + status
                   + " · 초록 " + passCount.ToString(CultureInfo.InvariantCulture)
                   + " · 빨강 " + failCount.ToString(CultureInfo.InvariantCulture)
                   + " · 건너뜀 " + skipCount.ToString(CultureInfo.InvariantCulture)
                   + " · " + seconds.ToString("0.0", CultureInfo.InvariantCulture) + "초");
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string test;
            lock (Gate)
            {
                if (redInTest >= RedPerTestCap) return;
                redInTest++;
                test = currentTest;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("RED  ").Append(type).Append("  [").Append(test).Append(']');
            sb.Append('\n').Append(Indent("  |    ", Clamp(condition, MessageCap)));
            if (!string.IsNullOrEmpty(stackTrace)) sb.Append('\n').Append(Indent("  at   ", Head(stackTrace, StackLineCap)));
            Append(sb.ToString());
        }

        private static void Header()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("# PlayMode 진단 로그 (T46) — 실행 순서대로 붙는다. RED = 콘솔 빨강 · FAIL = 실패한 테스트");
            sb.Append('\n').Append("# ").Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            sb.Append(" · Unity ").Append(Application.unityVersion).Append(" · ").Append(Application.platform);
            sb.Append('\n').Append("# 세이브 자리: ").Append(Application.persistentDataPath);
            Append(sb.ToString());
        }

        private static void Append(string text)
        {
            try
            {
                Directory.CreateDirectory(OutDir);
                lock (Gate)
                {
                    File.AppendAllText(Path.Combine(OutDir, FileName), text + "\n", new UTF8Encoding(false));
                }
            }
            catch (Exception)
            {
                // 진단이 테스트를 죽이지 않는다 — 쓸 수 없는 판(권한·디스크)에서는 조용히 지나간다.
            }
        }

        private static string Clamp(string s, int cap)
        {
            if (s == null) return "";
            s = s.Replace("\r", "");
            return s.Length <= cap ? s : s.Substring(0, cap) + " …(자름)";
        }

        private static string Head(string s, int lines)
        {
            if (s == null) return "";
            string[] parts = s.Replace("\r", "").Split('\n');
            StringBuilder sb = new StringBuilder();
            int n = Math.Min(lines, parts.Length);
            for (int i = 0; i < n; i++)
            {
                if (parts[i].Length == 0) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(parts[i].Length <= LineCap ? parts[i] : parts[i].Substring(0, LineCap));
            }
            if (parts.Length > n) sb.Append("\n…(").Append(parts.Length - n).Append("줄 더)");
            return sb.ToString();
        }

        private static string Indent(string prefix, string body)
        {
            string[] parts = body.Split('\n');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(prefix).Append(parts[i]);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// 러너가 부르는 갈래 — 테스트마다 이름과 결과(상태·메시지·스택)를 <see cref="RedLogFile"/> 에 넘긴다.
    /// ⚠ 여기서 예외를 던지면 러너가 그것을 다시 던진다(`TestRunCallbackListener.InvokeAllCallbacks`) — 전부 `try/catch` 다.
    /// </summary>
    public sealed class RedLogCallbacks : ITestRunCallback
    {
        /// <summary>런 시작.</summary>
        public void RunStarted(ITest testsToRun)
        {
            try
            {
                RedLogFile.Install();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>런 끝 — 총계.</summary>
        public void RunFinished(ITestResult testResults)
        {
            try
            {
                if (testResults == null) return;
                RedLogFile.EndRun(testResults.ResultState.Status.ToString(), testResults.PassCount, testResults.FailCount, testResults.SkipCount, testResults.Duration);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>테스트 하나 시작(스위트 노드는 건너뛴다).</summary>
        public void TestStarted(ITest test)
        {
            try
            {
                if (test == null || test.IsSuite) return;
                RedLogFile.BeginTest(test.FullName);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>테스트 하나 끝(스위트 노드는 건너뛴다).</summary>
        public void TestFinished(ITestResult result)
        {
            try
            {
                if (result == null || result.Test == null || result.Test.IsSuite) return;
                RedLogFile.EndTest(result.Test.FullName, result.ResultState.Status.ToString(), result.Message, result.StackTrace);
            }
            catch (Exception)
            {
            }
        }
    }
}
