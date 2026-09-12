using System;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;

// 어셈블리 단위로 붙인다 — PlayMode 의 모든 테스트가 이 행동(ITestAction)을 지난다.
[assembly: Forge.Tests.PlayMode.RedLog]

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
        private static bool headerWritten;
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

        /// <summary>플레이 모드에 들어가면 러너가 첫 테스트를 세우기 전에 붙는다 — ITestAction 이 안 먹는 판에도 콘솔 빨강은 남는다.</summary>
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
            if (passed)
            {
                Append(head);
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(head);
            if (!string.IsNullOrEmpty(message)) sb.Append('\n').Append(Indent("  msg  ", Clamp(message, MessageCap)));
            if (!string.IsNullOrEmpty(stack)) sb.Append('\n').Append(Indent("  at   ", Head(stack, StackLineCap)));
            Append(sb.ToString());
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
            if (headerWritten) return;
            headerWritten = true;
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
    /// 모든 PlayMode 테스트에 붙는 NUnit 행동 — 테스트 이름과 결과(메시지·스택)를 <see cref="RedLogFile"/> 에 넘긴다.
    /// 어셈블리 단위 붙임이 안 먹는 판에서도 `RedLogFile` 의 콘솔 갈래는 살아 있다(둘이 겹쳐도 줄만 는다).
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RedLogAttribute : Attribute, ITestAction
    {
        /// <summary>테스트 하나하나에 붙는다(스위트에는 안 붙인다).</summary>
        public ActionTargets Targets
        {
            get { return ActionTargets.Test; }
        }

        /// <summary>테스트 시작.</summary>
        public void BeforeTest(ITest test)
        {
            try
            {
                RedLogFile.Install();
                RedLogFile.BeginTest(test == null ? null : test.FullName);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>테스트 끝 — 결과를 적는다.</summary>
        public void AfterTest(ITest test)
        {
            try
            {
                TestContext.ResultAdapter r = TestContext.CurrentContext.Result;
                string name = test == null ? TestContext.CurrentContext.Test.FullName : test.FullName;
                RedLogFile.EndTest(name, r.Outcome.Status.ToString(), r.Message, r.StackTrace);
            }
            catch (Exception)
            {
            }
        }
    }
}
