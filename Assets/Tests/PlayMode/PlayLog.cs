using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T27 — «플레이 콘솔 에러 0»(ROUTINE §1) 을 재는 자. 스코프 동안 나온 빨간 줄(Error·Exception·Assert)을 모으고
    /// <see cref="Mark"/> 로 «지금 어느 화면인가» 를 찍어 두어, 터졌을 때 «어느 화면에서 몇 줄» 까지 말한다.
    /// 원작 `web/tools/shot-screens.js` 가 `page.on('pageerror')`·`console(error)` 를 화면 이름과 함께 모아
    /// 마지막에 `ERRORS(n)` 로 뱉던 것과 같은 셈.
    ///
    /// ⚠ `LogAssert.NoUnexpectedReceived()` 는 쓰지 않는다(§1). 대신 스코프 동안만
    /// <c>LogAssert.ignoreFailingMessages</c> 를 켜 러너가 첫 빨간 줄에서 테스트를 끊지 않게 하고,
    /// 끝에서 <see cref="AssertNoRed"/> 가 모은 목록을 한 번에 보여 주며 실패시킨다 — 화면 30장을 도는 테스트에서
    /// «첫 줄 하나» 만 보고 끝나면 나머지 29장이 성한지 알 수 없기 때문이다.
    /// 반드시 <c>using</c> 이나 <c>[TearDown]</c> 으로 <see cref="Dispose"/> 한다(안 하면 다음 테스트까지 빨강을 삼킨다).
    /// </summary>
    public sealed class PlayLog : IDisposable
    {
        /// <summary>모인 빨간 줄 하나.</summary>
        public struct Line
        {
            public string Where;
            public LogType Type;
            public string Message;
            public string Stack;
        }

        private readonly List<Line> red = new List<Line>();
        private readonly object gate = new object();
        private readonly string label;
        private readonly bool prevIgnore;
        private string where;
        private bool closed;

        private PlayLog(string label)
        {
            this.label = string.IsNullOrEmpty(label) ? "play" : label;
            this.where = this.label;
            prevIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceivedThreaded += OnLog;
        }

        /// <summary>수집을 시작한다. <paramref name="label"/> 은 <see cref="Mark"/> 전까지의 «어디» 이름.</summary>
        public static PlayLog Start(string label)
        {
            return new PlayLog(label);
        }

        /// <summary>이 다음에 나오는 빨간 줄은 이 화면 몫이다.</summary>
        public void Mark(string screen)
        {
            lock (gate) { where = string.IsNullOrEmpty(screen) ? label : screen; }
        }

        /// <summary>지금까지 모인 빨간 줄 수.</summary>
        public int RedCount
        {
            get { lock (gate) { return red.Count; } }
        }

        /// <summary>지금까지 모인 빨간 줄(복사본).</summary>
        public List<Line> Red
        {
            get { lock (gate) { return new List<Line>(red); } }
        }

        /// <summary>그 화면에서 나온 빨간 줄 수.</summary>
        public int RedOf(string screen)
        {
            int n = 0;
            lock (gate)
            {
                for (int i = 0; i < red.Count; i++) if (string.Equals(red[i].Where, screen, StringComparison.Ordinal)) n++;
            }
            return n;
        }

        /// <summary>모은 것을 사람이 읽는 꼴로(최대 25줄 — 원작 shot-screens.js 와 같은 상한).</summary>
        public string Report()
        {
            List<Line> lines = Red;
            var sb = new StringBuilder();
            sb.Append("[" + label + "] 콘솔 빨강 " + lines.Count + "줄");
            int max = lines.Count < 25 ? lines.Count : 25;
            for (int i = 0; i < max; i++)
            {
                sb.Append("\n  · (" + lines[i].Where + ") " + lines[i].Type + ": " + Head(lines[i].Message));
                string st = Head(lines[i].Stack);
                if (!string.IsNullOrEmpty(st)) sb.Append("\n      " + st);
            }
            if (lines.Count > max) sb.Append("\n  · … " + (lines.Count - max) + "줄 더");
            return sb.ToString();
        }

        /// <summary>메시지의 첫 두 줄만(스택 전체를 실패 메시지에 붙이면 러너 출력이 통째로 잠긴다).</summary>
        private static string Head(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string[] parts = s.Replace("\r", "").Split('\n');
            string a = parts[0];
            if (parts.Length > 1 && parts[1].Length > 0) a += " | " + parts[1];
            return a.Length > 300 ? a.Substring(0, 300) + "…" : a;
        }

        /// <summary>빨간 줄이 하나라도 있으면 «어느 화면에서 무엇» 까지 붙여 실패시킨다.</summary>
        public void AssertNoRed()
        {
            if (RedCount == 0) return;
            Assert.Fail(Report());
        }

        public void Dispose()
        {
            if (closed) return;
            closed = true;
            Application.logMessageReceivedThreaded -= OnLog;
            LogAssert.ignoreFailingMessages = prevIgnore;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            lock (gate)
            {
                red.Add(new Line { Where = where, Type = type, Message = message, Stack = stack });
            }
        }
    }
}
