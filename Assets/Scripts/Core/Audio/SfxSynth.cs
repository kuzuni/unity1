using System;
using Forge.Core.Data;

namespace Forge.Core.Audio
{
    /// <summary>
    /// 원작 `sfx.js` 의 버스·리미터·IR·음악 버스 상수 — 전부 함수(`ensure`·`startMusic`·`_musicKick`…) 안의 리터럴이라 표(`sfx.json`)로 못 뽑는다.
    /// ROUTINE T30 절의 지시대로 한 파일에 모았다(결정 기록 참조). 값은 정본 줄 그대로 · 바꾸지 않는다.
    /// </summary>
    public static class SfxConsts
    {
        /// <summary>`this.master.gain.value = 0.35` — 효과음 버스.</summary>
        public const double MasterGain = 0.35;
        /// <summary>`this.musicGain.gain.value = 0.16` — 음악 버스(스펙: 0.1~0.2).</summary>
        public const double MusicGain = 0.16;
        /// <summary>합성 IR 리버브 — `_makeIR(1.4, 2.6)` · `wet.gain.value = 0.18`.</summary>
        public const double ReverbSeconds = 1.4;
        public const double ReverbDecayPow = 2.6;
        public const double ReverbWet = 0.18;
        /// <summary>소프트 리미터(DynamicsCompressor) — threshold −12 · knee 18 · ratio 5 · attack 0.004 · release 0.22.</summary>
        public const double LimiterThresholdDb = -12;
        public const double LimiterKneeDb = 18;
        public const double LimiterRatio = 5;
        public const double LimiterAttack = 0.004;
        public const double LimiterRelease = 0.22;
        /// <summary>음악 템포 동기 피드백 딜레이 — delayTime = 스텝 × 6(점8분) · 피드백 0.3 · 피드백 저역통과 2400 · 웻 0.4.</summary>
        public const int DelayStepsDotted8th = 6;
        public const double DelayFeedback = 0.3;
        public const double DelayFeedbackLp = 2400;
        public const double DelayWet = 0.4;
        /// <summary>음악 시작 시 첫 스텝까지의 여유(`ctx.currentTime + 0.05`) · 스케줄 창 0.12 · 타이머 25ms — 실시간 스케줄러 상수(루프를 미리 굽는 유니티에서는 안 쓴다).</summary>
        public const double MusicLeadIn = 0.05;
    }

    /// <summary>
    /// 원작 `SFX` 의 합성 프리미티브(`tone`·`noiseBurst`·`thump`·`click`·`ring`·`sparkle`)와 음악 노트(`_musicOsc`·`_musicKick`·`_musicHat`)를
    /// **그래프 기록**으로 옮긴 것 — WebAudio 노드를 만드는 대신 <see cref="Voice"/> 를 <see cref="Score"/> 에 쌓는다. 인자 기본값·`||` 규약·
    /// 자동화 호출 순서·난수 소비 순서(지터 → 노이즈 버퍼)가 정본과 같아 `tools/sfx_vectors.js` 벡터와 보이스 단위로 맞는다.
    /// 소리 켜짐(`S.sfxOn`)은 여기서 보지 않는다 — 부르는 쪽(Game `Sfx`)이 가른다.
    /// </summary>
    public sealed class SfxSynth
    {
        public readonly Score Score;
        public readonly Rng Rng;
        public readonly int SampleRate;
        /// <summary>`ctx.currentTime` — 효과음은 0 에서 시작한다.</summary>
        public double Now;

        /// <summary>노이즈 버퍼 풀(T73 · null 이면 매번 새 배열) — 값·길이·난수 소비 순서는 풀이 있어도 같다.</summary>
        readonly NoisePool _noisePool;

        public SfxSynth(int sampleRate, Rng rng) : this(sampleRate, rng, null) { }

        public SfxSynth(int sampleRate, Rng rng, NoisePool noisePool)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException("sampleRate");
            if (rng == null) throw new ArgumentNullException("rng");
            SampleRate = sampleRate;
            Rng = rng;
            Score = new Score(sampleRate);
            _noisePool = noisePool;
        }

        public static Wave ParseWave(string s)
        {
            switch (s)
            {
                case null: case "": case "sine": return Wave.Sine;
                case "triangle": return Wave.Triangle;
                case "square": return Wave.Square;
                case "sawtooth": return Wave.Sawtooth;
            }
            throw new ArgumentException("모르는 파형: " + s);
        }

        public static FilterType ParseFilter(string s)
        {
            switch (s)
            {
                case null: case "": case "lowpass": return FilterType.Lowpass;
                case "highpass": return FilterType.Highpass;
                case "bandpass": return FilterType.Bandpass;
            }
            throw new ArgumentException("모르는 필터: " + s);
        }

        // ---- 합성 프리미티브 (원작 sfx.js 130~220줄) ----

        /// <summary>`tone(freq, dur, opts)` — jitter: 호출마다 주파수를 ±비율로 흔든다 · rvb: 리버브 센드 비율.</summary>
        public void Tone(double freq, double dur, Wave type = Wave.Sine, double gain = 0.3, double delay = 0, double jitter = 0, double slideTo = 0, double attack = 0.012, double rvb = 0)
        {
            double j = jitter != 0 ? 1 + (Rng.Random() * 2 - 1) * jitter : 1;
            double t0 = Now + delay;
            var v = new Voice { Kind = VoiceKind.Osc, Bus = Bus.Sfx, Wave = type, Freq = new AudioParam(440) };
            v.Freq.SetValueAtTime(freq * j, t0);
            if (slideTo != 0) v.Freq.ExponentialRampToValueAtTime(slideTo * j, t0 + dur);
            v.Gain.SetValueAtTime(0.0001, t0);
            v.Gain.ExponentialRampToValueAtTime(gain, t0 + attack);
            v.Gain.ExponentialRampToValueAtTime(0.0001, t0 + dur);
            if (rvb != 0) v.Rvb = rvb;
            v.Start = t0;
            v.Stop = t0 + dur + 0.02;
            Score.Voices.Add(v);
        }

        /// <summary>`noiseBurst(dur, opts)` — 감쇠 노이즈 버스트 · type(lowpass/highpass/bandpass) · filterTo(스윕) · Q.</summary>
        public void NoiseBurst(double dur, FilterType type = FilterType.Lowpass, double filterFreq = 1200, double filterTo = 0, double q = 0, double gain = 0.3, double delay = 0, double rvb = 0)
        {
            double t0 = Now + delay;
            int n = Math.Max(1, (int)Math.Floor(SampleRate * dur));
            var v = new Voice { Kind = VoiceKind.Noise, Bus = Bus.Sfx, Noise = NoiseBuffer(n) };
            v.Filter = type;
            v.FilterFreq = new AudioParam(350);
            v.FilterFreq.SetValueAtTime(filterFreq, t0);
            if (filterTo != 0) v.FilterFreq.ExponentialRampToValueAtTime(filterTo, t0 + dur);
            if (q != 0) v.Q = q;
            v.Gain.SetValueAtTime(gain, t0);
            v.Gain.ExponentialRampToValueAtTime(0.001, t0 + dur);
            if (rvb != 0) v.Rvb = rvb;
            v.Start = t0;
            v.Stop = t0 + (double)n / SampleRate;
            Score.Voices.Add(v);
        }

        /// <summary>`thump(f0, f1, dur, opts)` — 피치가 뚝 떨어지는 사인 바디(타격의 '무게'). 지터 난수는 항상 하나 소비한다(원작 `opts.jitter || 0`).</summary>
        public void Thump(double f0, double f1, double dur, double gain = 0.35, double delay = 0, double jitter = 0)
        {
            double t0 = Now + delay;
            double j = 1 + (Rng.Random() * 2 - 1) * jitter;
            var v = new Voice { Kind = VoiceKind.Osc, Bus = Bus.Sfx, Wave = Wave.Sine, Freq = new AudioParam(440) };
            v.Freq.SetValueAtTime(f0 * j, t0);
            v.Freq.ExponentialRampToValueAtTime(Math.Max(20, f1 * j), t0 + dur);
            v.Gain.SetValueAtTime(0.001, t0);
            v.Gain.LinearRampToValueAtTime(gain, t0 + 0.006);
            v.Gain.ExponentialRampToValueAtTime(0.001, t0 + dur);
            v.Start = t0;
            v.Stop = t0 + dur + 0.02;
            Score.Voices.Add(v);
        }

        /// <summary>`click(opts)` — 어택 트랜지언트(아주 짧은 고역 클릭).</summary>
        public void Click(double freq = 3000, double gain = 0.25, double delay = 0)
        {
            NoiseBurst(0.008, FilterType.Highpass, freq, 0, 0, gain, delay);
        }

        /// <summary>`ring(freq, dur, opts)` — 비정수배 부분음 2개(디튠 삼각파)로 '쇠가 운다'.</summary>
        public void Ring(double freq, double dur, double gain = 0.12, double delay = 0, double rvb = 0.2)
        {
            Tone(freq, dur, Wave.Triangle, gain, delay, 0.02, 0, 0.012, rvb);
            Tone(freq * 1.503, dur * 0.8, Wave.Triangle, gain * 0.6, delay + 0.005, 0.02, 0, 0.012, rvb);
        }

        /// <summary>`sparkle(base, opts)` — 고역 사인 짧은 계단(보상·리빌의 '가루').</summary>
        public void Sparkle(double baseFreq, int n = 3, double gain = 0.07, double delay = 0)
        {
            for (int i = 0; i < n; i++)
                Tone(baseFreq * Math.Pow(1.335, i), 0.09, Wave.Sine, gain, delay + i * 0.045, 0.04, 0, 0.012, 0.35);
        }

        // ---- 음악 노트 (원작 sfx.js 562~617줄) ----

        /// <summary>`_musicOsc(freq, dur, t0, opts)` — 절대 시각 t0 에 시작하는 음악 노트 · lp(개별 저역통과) · delaySend · rvb 센드.</summary>
        public void MusicOsc(double freq, double dur, double t0, Wave type, double gain, double attack = 0.01, double lp = 0, double delaySend = 0, double rvb = 0)
        {
            var v = new Voice { Kind = VoiceKind.Osc, Bus = Bus.Music, Wave = type, Freq = new AudioParam(440) };
            v.Freq.SetValueAtTime(freq, t0);
            double a = Math.Max(0.005, attack != 0 ? attack : 0.01);
            v.Gain.SetValueAtTime(0.0001, t0);
            v.Gain.LinearRampToValueAtTime(gain, t0 + a);
            v.Gain.ExponentialRampToValueAtTime(0.0001, t0 + dur);
            if (lp != 0)
            {
                v.Filter = FilterType.Lowpass;
                v.FilterFreq = new AudioParam(350);
                v.FilterFreq.Assign(lp);
            }
            if (delaySend != 0) v.DelaySend = delaySend;
            if (rvb != 0) v.Rvb = rvb;
            v.Start = t0;
            v.Stop = t0 + dur + 0.05;
            Score.Voices.Add(v);
        }

        /// <summary>`_musicKick(t0, soft)` — 피치가 아래로 꺾이는 사인파. soft(던전)는 낮고 부드럽게, 보스는 단단하게.</summary>
        public void MusicKick(double t0, bool soft)
        {
            var v = new Voice { Kind = VoiceKind.Osc, Bus = Bus.Music, Wave = Wave.Sine, Freq = new AudioParam(440) };
            v.Freq.SetValueAtTime(soft ? 110 : 150, t0);
            v.Freq.ExponentialRampToValueAtTime(soft ? 36 : 45, t0 + (soft ? 0.22 : 0.15));
            v.Gain.SetValueAtTime(0.001, t0);
            v.Gain.LinearRampToValueAtTime(soft ? 0.4 : 0.55, t0 + (soft ? 0.015 : 0.008));
            v.Gain.ExponentialRampToValueAtTime(0.001, t0 + (soft ? 0.3 : 0.22));
            v.Start = t0;
            v.Stop = t0 + 0.32;
            Score.Voices.Add(v);
        }

        /// <summary>`_musicHat(t0, gain, freq)` — 여린 하이햇/셰이커(고역통과 노이즈 · freq 로 밝기).</summary>
        public void MusicHat(double t0, double gain, double freq = 0)
        {
            const double dur = 0.045;
            int n = Math.Max(1, (int)Math.Floor(SampleRate * dur));
            var v = new Voice { Kind = VoiceKind.Noise, Bus = Bus.Music, Noise = NoiseBuffer(n) };
            v.Filter = FilterType.Highpass;
            v.FilterFreq = new AudioParam(350);
            v.FilterFreq.Assign(freq != 0 ? freq : 5500);
            v.Gain.SetValueAtTime(gain, t0);
            v.Gain.ExponentialRampToValueAtTime(0.001, t0 + dur);
            v.Start = t0;
            v.Stop = t0 + (double)n / SampleRate;
            Score.Voices.Add(v);
        }

        /// <summary>`d[i] = (Math.random() * 2 - 1) * (1 - i / n)` — Float32Array 에 담기던 대로 float 로 반올림.</summary>
        float[] NoiseBuffer(int n)
        {
            float[] d = _noisePool != null ? _noisePool.Rent(n) : new float[n];
            for (int i = 0; i < n; i++) d[i] = (float)((Rng.Random() * 2 - 1) * (1 - (double)i / n));
            return d;
        }

        /// <summary>노이즈 버퍼 합(벡터 대조용 · 정본과 같은 난수 순서면 같다).</summary>
        public static double NoiseSum(float[] d)
        {
            double s = 0;
            for (int i = 0; i < d.Length; i++) s += d[i];
            return s;
        }
    }
}
