using System;
using System.Collections.Generic;

namespace Forge.Game.SkillFx
{
    /// <summary>
    /// 원작 `Scene3D.addAnim(dur, fn, onDone)`(`anims` 큐 · 프레임마다 `t += dt; k = min(1, t/dur); fn(k); k ≥ 1 → onDone`) 과
    /// `setTimeout(fn, ms)` 를 **게임 시간(dt 누적)** 위에 올린 것(T12). 실시간이 아니라 스텝 시간이라 테스트가 결정적으로 되감는다.
    /// 규약(scene3d.js 18653~18658 그대로): 큐를 뒤에서 앞으로 돌고, 콜백 안에서 새로 더한 애니는 **다음 프레임**부터 돈다.
    /// </summary>
    public sealed class FxTimeline
    {
        sealed class Anim { public double T, Dur; public Action<double> Fn; public Action Done; }
        sealed class Timer { public double At; public long Seq; public Action Fn; }

        readonly List<Anim> anims = new List<Anim>();
        readonly List<Timer> timers = new List<Timer>();
        long seq;

        /// <summary>누적 게임 시간(초).</summary>
        public double Now { get; private set; }
        public int AnimCount { get { return anims.Count; } }
        public int TimerCount { get { return timers.Count; } }
        /// <summary>돌아가는 애니·예약이 하나도 없는가.</summary>
        public bool Idle { get { return anims.Count == 0 && timers.Count == 0; } }

        /// <summary>`addAnim(dur, fn, onDone)` — dur 초 동안 fn(k · 0→1) · 끝나면 onDone.</summary>
        public void Add(double dur, Action<double> fn, Action onDone = null)
        {
            anims.Add(new Anim { Dur = Math.Max(1e-6, dur), Fn = fn, Done = onDone });
        }

        /// <summary>`setTimeout(fn, sec·1000)` — sec 뒤 한 번(같은 시각이면 등록 순서).</summary>
        public void After(double sec, Action fn)
        {
            timers.Add(new Timer { At = Now + Math.Max(0, sec), Seq = ++seq, Fn = fn });
        }

        /// <summary>한 프레임(초). 예약(타이머) → 애니 큐 순.</summary>
        public void Step(double dt)
        {
            Now += dt;
            // 예약: 시각이 된 것을 등록 순서로 — 콜백이 다시 예약한 0ms 짜리는 이 프레임에 이어서 돈다(JS 매크로태스크와 같은 결).
            for (int guard = 0; guard < 64; guard++)
            {
                Timer due = null;
                for (int i = 0; i < timers.Count; i++)
                {
                    Timer t = timers[i];
                    if (t.At <= Now + 1e-9 && (due == null || t.At < due.At || (t.At == due.At && t.Seq < due.Seq))) due = t;
                }
                if (due == null) break;
                timers.Remove(due);
                due.Fn();
            }
            for (int i = anims.Count - 1; i >= 0; i--)
            {
                if (i >= anims.Count) continue;
                Anim a = anims[i];
                a.T += dt;
                double k = Math.Min(1, a.T / a.Dur);
                if (a.Fn != null) a.Fn(k);
                if (k >= 1)
                {
                    int idx = anims.IndexOf(a);
                    if (idx >= 0) anims.RemoveAt(idx);
                    if (a.Done != null) a.Done();
                }
            }
        }

        /// <summary>전부 버린다(onDone 을 부르지 않는다 — 씬 리셋용).</summary>
        public void Clear() { anims.Clear(); timers.Clear(); }
    }
}
