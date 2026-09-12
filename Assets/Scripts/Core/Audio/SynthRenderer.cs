using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Audio
{
    /// <summary>렌더 결과 — 모노 샘플 + 표본율 + 통계(테스트·검수용).</summary>
    public sealed class RenderedClip
    {
        public float[] Samples;
        public int SampleRate;
        public double Seconds { get { return (double)Samples.Length / SampleRate; } }

        public float Peak
        {
            get { float p = 0; for (int i = 0; i < Samples.Length; i++) { float a = Math.Abs(Samples[i]); if (a > p) p = a; } return p; }
        }

        public double Rms
        {
            get { double s = 0; for (int i = 0; i < Samples.Length; i++) s += (double)Samples[i] * Samples[i]; return Samples.Length == 0 ? 0 : Math.Sqrt(s / Samples.Length); }
        }

        /// <summary>결정론 검사용 지문(FNV-1a · float 비트).</summary>
        public uint Fingerprint
        {
            get
            {
                uint h = 2166136261u;
                for (int i = 0; i < Samples.Length; i++)
                {
                    uint b = (uint)BitConverter.SingleToInt32Bits(Samples[i]);
                    h = (h ^ b) * 16777619u;
                }
                return h;
            }
        }
    }

    /// <summary>
    /// <see cref="Score"/> → 샘플. 원작 그래프 그대로: 보이스(소스 → 필터 → 엔벨로프) → 버스(master 0.35 / musicGain 0.16) · 엔벨로프 뒤 센드 →
    /// 합성 IR 리버브(wet 0.18) · 음악 템포 딜레이(피드백 0.3 · 저역 2400 · wet 0.4) → 소프트 리미터 → 출력. 루프 모드면 루프 길이 뒤의 꼬리를 앞으로 접어
    /// 이음새 없는 <c>AudioClip</c> 루프를 만든다.
    /// </summary>
    public static class SynthRenderer
    {
        /// <summary>IR 난수 시드 — 원작은 `Math.random` 이라 매 세션 다르다 · 유니티는 고정(결정론 · 테스트).</summary>
        public const uint IrSeed = 0x2f6e2b1;

        static readonly Dictionary<long, Dsp.ConvolverKernel> _kernels = new Dictionary<long, Dsp.ConvolverKernel>();

        /// <summary>표본율·시드별 IR 커널(공유 · 스레드 안전).</summary>
        public static Dsp.ConvolverKernel Kernel(int sampleRate, uint seed)
        {
            long key = ((long)sampleRate << 32) | seed;
            lock (_kernels)
            {
                Dsp.ConvolverKernel k;
                if (_kernels.TryGetValue(key, out k)) return k;
                float[] ir = Dsp.MakeIR(sampleRate, SfxConsts.ReverbSeconds, SfxConsts.ReverbDecayPow, Rng.Mulberry(seed));
                k = new Dsp.ConvolverKernel(ir);
                _kernels[key] = k;
                return k;
            }
        }

        /// <summary>효과음 한 건(비루프).</summary>
        public static RenderedClip Render(Score score) { return Render(score, 0); }

        /// <summary>
        /// <paramref name="loopSeconds"/> &gt; 0 이면 그 길이로 접는 루프(음악). 0 이면 마지막 보이스 + 리버브 꼬리까지.
        /// </summary>
        public static RenderedClip Render(Score score, double loopSeconds)
        {
            int sr = score.SampleRate;
            bool hasRvb = score.HasReverb;
            bool hasDly = score.HasDelay && score.DelaySeconds > 0;
            double tail = 0.05;
            if (hasRvb) tail = Math.Max(tail, SfxConsts.ReverbSeconds);
            if (hasDly) tail = Math.Max(tail, score.DelaySeconds * 6);
            double body = loopSeconds > 0 ? loopSeconds : score.End;
            int loopLen = loopSeconds > 0 ? (int)Math.Round(loopSeconds * sr) : 0;
            int len = (int)Math.Ceiling((body + tail) * sr);
            if (len <= 0) len = 1;

            var sfxBus = new double[len];
            var musicBus = new double[len];
            var rvbSend = hasRvb ? new double[len] : null;
            var dlySend = hasDly ? new double[len] : null;

            for (int i = 0; i < score.Voices.Count; i++)
                RenderVoice(score.Voices[i], sr, len, sfxBus, musicBus, rvbSend, dlySend);

            var mix = new double[len];
            for (int n = 0; n < len; n++) mix[n] = SfxConsts.MasterGain * sfxBus[n] + SfxConsts.MusicGain * musicBus[n];

            if (hasDly)
            {
                int d = Math.Max(1, (int)Math.Round(score.DelaySeconds * sr));
                var ring = new double[d];
                var lp = new Dsp.Biquad();
                lp.Set(FilterType.Lowpass, SfxConsts.DelayFeedbackLp, 1, sr);
                int w = 0;
                for (int n = 0; n < len; n++)
                {
                    double outp = ring[w];
                    ring[w] = dlySend[n] + lp.Process(outp * SfxConsts.DelayFeedback);
                    w++; if (w == d) w = 0;
                    mix[n] += SfxConsts.MusicGain * SfxConsts.DelayWet * outp;
                }
            }

            if (hasRvb)
            {
                double[] wet = Dsp.Convolve(rvbSend, Kernel(sr, IrSeed), len);
                for (int n = 0; n < len; n++) mix[n] += SfxConsts.ReverbWet * wet[n];
            }

            float[] outBuf;
            if (loopLen > 0)
            {
                outBuf = new float[loopLen];
                for (int n = 0; n < len; n++) outBuf[n % loopLen] += (float)mix[n];
            }
            else
            {
                outBuf = new float[len];
                for (int n = 0; n < len; n++) outBuf[n] = (float)mix[n];
            }

            Dsp.Compress(outBuf, sr, SfxConsts.LimiterThresholdDb, SfxConsts.LimiterKneeDb, SfxConsts.LimiterRatio, SfxConsts.LimiterAttack, SfxConsts.LimiterRelease);
            return new RenderedClip { Samples = outBuf, SampleRate = sr };
        }

        static void RenderVoice(Voice v, int sr, int len, double[] sfxBus, double[] musicBus, double[] rvbSend, double[] dlySend)
        {
            int n0 = Math.Max(0, (int)Math.Ceiling(v.Start * sr - 1e-9));
            int n1 = Math.Min(len, (int)Math.Ceiling(v.Stop * sr));
            if (n1 <= n0) return;
            double[] bus = v.Bus == Bus.Sfx ? sfxBus : musicBus;
            bool filt = v.Filter != FilterType.None;
            var bq = new Dsp.Biquad();
            bool filtAuto = filt && v.FilterFreq != null && !v.FilterFreq.IsConstant;
            if (filt) bq.Set(v.Filter, v.FilterFreq.ValueAt(v.Start), v.Q, sr);
            double phase = 0;
            double invSr = 1.0 / sr;
            for (int n = n0; n < n1; n++)
            {
                double t = n * invSr;
                double x;
                if (v.Kind == VoiceKind.Osc)
                {
                    x = Dsp.Osc(v.Wave, phase);
                    phase += v.Freq.ValueAt(t) * invSr;
                    if (phase >= 1) phase -= Math.Floor(phase);
                }
                else
                {
                    int k = n - n0;
                    x = k < v.Noise.Length ? v.Noise[k] : 0;
                }
                if (filt)
                {
                    if (filtAuto && ((n - n0) & 15) == 0) bq.Set(v.Filter, v.FilterFreq.ValueAt(t), v.Q, sr);
                    x = bq.Process(x);
                }
                double g = v.Gain.ValueAt(t);
                double y = x * g;
                bus[n] += y;
                if (v.Rvb > 0 && rvbSend != null) rvbSend[n] += y * v.Rvb;
                if (v.DelaySend > 0 && dlySend != null) dlySend[n] += y * v.DelaySend;
            }
        }
    }

    /// <summary>레시피·시퀀서·렌더러를 한데 묶은 진입점 — Game `Sfx`/`Music` 과 테스트가 이것만 부른다.</summary>
    public static class AudioFactory
    {
        public const int DefaultSampleRate = 44100;

        /// <summary>효과음 한 건을 점수로(시드 = 지터·노이즈 난수).</summary>
        public static Score SfxScore(SfxCall call, IReadOnlyList<string> rarities, uint seed, int sampleRate = DefaultSampleRate)
        {
            var s = new SfxSynth(sampleRate, Rng.Mulberry(seed));
            SfxRecipes.Build(s, call, rarities);
            return s.Score;
        }

        /// <summary>효과음 한 건을 샘플로.</summary>
        public static RenderedClip RenderSfx(SfxCall call, IReadOnlyList<string> rarities, uint seed, int sampleRate = DefaultSampleRate)
        {
            return SynthRenderer.Render(SfxScore(call, rarities, seed, sampleRate));
        }

        /// <summary>음악 모드 하나의 8마디 루프를 점수로(steps 를 주면 앞부분만).</summary>
        public static Score MusicScore(SfxTable table, string mode, uint seed, out double seconds, int sampleRate = DefaultSampleRate, int steps = 0)
        {
            var s = new SfxSynth(sampleRate, Rng.Mulberry(seed));
            seconds = MusicSequencer.BuildLoop(s, table, mode, steps);
            return s.Score;
        }

        /// <summary>음악 모드 하나의 루프 클립(이음새 접기 포함). steps 를 주면 비루프로 앞부분만 굽는다(테스트).</summary>
        public static RenderedClip RenderMusic(SfxTable table, string mode, uint seed, int sampleRate = DefaultSampleRate, int steps = 0)
        {
            double sec;
            Score score = MusicScore(table, mode, seed, out sec, sampleRate, steps);
            return SynthRenderer.Render(score, steps > 0 ? 0 : sec);
        }

        /// <summary>호출 키 + 테이크 번호 → 시드(같은 키·테이크는 언제나 같은 소리).</summary>
        public static uint Seed(string key, int take)
        {
            uint h = 2166136261u;
            for (int i = 0; i < key.Length; i++) h = (h ^ key[i]) * 16777619u;
            h = (h ^ (uint)take) * 16777619u;
            return h == 0 ? 1u : h;
        }
    }
}
