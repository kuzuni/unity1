using System;

namespace Forge.Core.Audio
{
    /// <summary>
    /// 원작 `_musicScheduleStep(step, t0)` 의 이식 — 스텝 하나가 만드는 층 다섯(① 베이스 2겹 · 킥 ② 코드 패드 디튠 2겹 ③ 아르페지오 ④ 멜로디 A/B + 옥타브 광택 ⑤ 하이햇/셰이커)을
    /// 같은 순서·같은 수치로 <see cref="SfxSynth"/> 에 예약한다. 원작은 look-ahead 타이머로 실시간 예약하고 코드 경계에서 모드를 갈아탄다 —
    /// 유니티는 모드마다 8마디 루프(128스텝)를 미리 구워 <c>AudioClip</c> 으로 돌리고 갈아타기는 Game `Music` 이 코드 경계에서 클립을 바꾼다(결정 기록).
    /// </summary>
    public static class MusicSequencer
    {
        /// <summary>`_noteFreq(midi)` = 440 × 2^((midi − 69) / 12).</summary>
        public static double NoteFreq(int midi) { return 440 * Math.Pow(2, (midi - 69) / 12.0); }

        /// <summary>스텝 하나(원작 `_musicScheduleStep` 에서 모드 결정 부분을 뺀 것 — 모드는 호출자가 고정한다).</summary>
        public static void ScheduleStep(SfxSynth s, SfxTable table, MusicMode cfg, int step, double t0, double stepDur)
        {
            int chordSteps = table.ChordSteps;
            int barStep = step % table.StepsPerBar;
            MusicChord chord = cfg.Prog[(step / chordSteps) % cfg.Prog.Length];
            double t = t0 + (step % 2 == 1 ? stepDur * cfg.Swing : 0);
            double accent = barStep == 0 ? 1 : barStep % 8 == 0 ? 0.85 : 0.7;
            bool boss = cfg.Name == "boss";

            if (cfg.HasBassStep(barStep))
            {
                double f = NoteFreq(chord.Bass);
                s.MusicOsc(f, 0.42, t, Wave.Sine, 0.5 * accent, 0.008);
                s.MusicOsc(f * 2, 0.18, t, Wave.Triangle, 0.22 * accent, 0.006);
            }
            if (cfg.Kick != KickKind.None && barStep == 0) s.MusicKick(t, cfg.Kick == KickKind.Soft);

            if (step % chordSteps == 0)
            {
                double dur = chordSteps * stepDur * 1.03;
                Wave padType = SfxSynth.ParseWave(cfg.PadType);
                for (int i = 0; i < chord.Pad.Length; i++)
                {
                    double f = NoteFreq(chord.Pad[i]);
                    s.MusicOsc(f * 1.0035, dur, t0, padType, 0.16, dur * 0.3, cfg.PadLp, 0, 0.5);
                    s.MusicOsc(f * 0.9965, dur, t0, padType, 0.16, dur * 0.35, cfg.PadLp, 0, 0.5);
                }
            }

            if (cfg.ArpEvery != 0 && barStep % cfg.ArpEvery == cfg.ArpOff % cfg.ArpEvery)
            {
                int arpIdx = (step / cfg.ArpEvery) % chord.Arp.Length;
                s.MusicOsc(NoteFreq(chord.Arp[arpIdx]), 0.14, t, Wave.Triangle, 0.2 * accent, 0.004, 0, 0.5);
            }

            int[] half = step < table.LoopSteps / 2 ? cfg.Mel : cfg.MelB;
            int mel = half[step % half.Length];
            if (mel != 0)
            {
                double f = NoteFreq(mel);
                s.MusicOsc(f, boss ? 0.3 : 0.42, t, Wave.Triangle, 0.4, 0.01, 0, 0.25, 0.3);
                s.MusicOsc(f * 2, 0.16, t, Wave.Sine, 0.09, 0.01);
            }

            if (cfg.HatEvery != 0 && barStep % cfg.HatEvery == cfg.HatOff) s.MusicHat(t, (boss ? 0.1 : 0.12) * accent);
            if (cfg.Shaker && barStep % 2 == 0) s.MusicHat(t, 0.05 * accent, 3800);
        }

        /// <summary>
        /// 모드 하나의 루프(기본 128스텝 = 8마디)를 t0 = step × stepDur 로 예약한다. 반환 = 루프 길이(초).
        /// <paramref name="steps"/> 를 줄이면 앞부분만(테스트용 8박 = 32스텝).
        /// </summary>
        public static double BuildLoop(SfxSynth s, SfxTable table, string mode, int steps = 0)
        {
            MusicMode cfg = table.Mode(mode);
            if (cfg == null) throw new ArgumentException("모르는 음악 모드: " + mode);
            int n = steps > 0 ? steps : table.LoopSteps;
            double stepDur = cfg.StepDur;
            s.Score.DelaySeconds = stepDur * SfxConsts.DelayStepsDotted8th;
            for (int step = 0; step < n; step++) ScheduleStep(s, table, cfg, step, step * stepDur, stepDur);
            return n * stepDur;
        }
    }
}
