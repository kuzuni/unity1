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
    /// 노이즈 버퍼 풀(T73) — 원작 `noiseBurst`·`_musicHat` 의 `Float32Array(n)` 자리. 길이가 같은 버퍼만 되쓴다(<see cref="Voice.Noise"/> 의 <c>Length</c> 가 곧 재생 길이라
    /// 더 긴 배열을 주면 소리가 달라진다). 값은 빌릴 때마다 전부 다시 채우므로 결정론(같은 시드 → 같은 샘플)은 그대로다.
    /// <see cref="ReleaseAll"/> 은 렌더가 끝난 뒤 <see cref="SynthRenderer"/> 가 부른다 — 그 전에는 빌린 버퍼가 점수(<see cref="Score"/>)에 물려 있다.
    /// </summary>
    public sealed class NoisePool
    {
        readonly Dictionary<int, Stack<float[]>> _free = new Dictionary<int, Stack<float[]>>();
        readonly List<float[]> _rented = new List<float[]>();
        /// <summary>실제로 새로 만든 버퍼 수(풀이 도는지 재는 자).</summary>
        public int Created { get; private set; }
        /// <summary>지금 빌려 나간 버퍼 수.</summary>
        public int Rented { get { return _rented.Count; } }

        public float[] Rent(int n)
        {
            Stack<float[]> st;
            float[] d;
            if (_free.TryGetValue(n, out st) && st.Count > 0) d = st.Pop();
            else { d = new float[n]; Created++; }
            _rented.Add(d);
            return d;
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _rented.Count; i++)
            {
                float[] d = _rented[i];
                Stack<float[]> st;
                if (!_free.TryGetValue(d.Length, out st)) { st = new Stack<float[]>(); _free[d.Length] = st; }
                st.Push(d);
            }
            _rented.Clear();
        }
    }

    /// <summary>
    /// 렌더 작업 공간(T73) — 한 렌더가 쓰는 버스 5(sfx·music·리버브 센드·딜레이 센드·믹스) · 딜레이 링 · FFT re/im · 합성곱 출력 · 노이즈 풀. 원작은 WebAudio 노드가 네이티브로 굽지만
    /// 유니티 Core 렌더러는 관리 배열이라, 잡마다 새로 잡으면 배경 스레드가 초당 수십 MB 의 쓰레기를 내고 그 GC 가 메인 스레드를 멈춘다(런 118 실측 · §1 60fps).
    /// 스레드 하나가 하나를 쥔다(공유 안전 아님) — <c>AudioBank</c> 워커가 하나, 테스트가 각자 하나.
    /// </summary>
    public sealed class RenderWorkspace
    {
        public double[] SfxBus, MusicBus, RvbSend, DlySend, Mix, Ring, FftRe, FftIm, Wet;
        public readonly NoisePool Noise = new NoisePool();
        /// <summary>배열을 새로 잡은 횟수(길이가 모자랄 때만 · 풀이 도는지 재는 자).</summary>
        public int Grown { get; private set; }

        /// <summary>길이 <paramref name="len"/> 이상의 0 으로 채운 배열 — 모자라면 새로 잡고, 아니면 앞 <paramref name="len"/> 칸만 지운다.</summary>
        public double[] Take(ref double[] field, int len)
        {
            if (field == null || field.Length < len) { field = new double[len]; Grown++; }
            else Array.Clear(field, 0, len);
            return field;
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

        /// <summary>효과음 한 건(비루프 · 작업 배열은 이 호출만 쓰고 버린다).</summary>
        public static RenderedClip Render(Score score) { return Render(score, 0, null); }

        /// <summary><paramref name="loopSeconds"/> &gt; 0 이면 그 길이로 접는 루프(음악). 0 이면 마지막 보이스 + 리버브 꼬리까지.</summary>
        public static RenderedClip Render(Score score, double loopSeconds) { return Render(score, loopSeconds, null); }

        /// <summary>
        /// <paramref name="ws"/> 를 주면 버스·믹스·딜레이 링·FFT·합성곱 출력 배열을 **그 작업 공간에서 되쓴다**(T73 — 잡마다 새로 잡으면 베이크 스레드가 447~638KB/프레임의
        /// 관리 쓰레기를 낸다 · 런 118). 새로 만드는 것은 결과 <c>float[]</c>(클립 데이터)뿐이다. 배열이 길이보다 길어도 모든 루프가 <c>len</c> 까지만 읽고 쓰므로 결과는 같다.
        /// null 이면 한 번 쓰고 버리는 작업 공간을 만든다(테스트·단발 호출).
        /// </summary>
        public static RenderedClip Render(Score score, double loopSeconds, RenderWorkspace ws)
        {
            if (ws == null) ws = new RenderWorkspace();
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

            double[] sfxBus = ws.Take(ref ws.SfxBus, len);
            double[] musicBus = ws.Take(ref ws.MusicBus, len);
            double[] rvbSend = hasRvb ? ws.Take(ref ws.RvbSend, len) : null;
            double[] dlySend = hasDly ? ws.Take(ref ws.DlySend, len) : null;

            for (int i = 0; i < score.Voices.Count; i++)
                RenderVoice(score.Voices[i], sr, len, sfxBus, musicBus, rvbSend, dlySend);

            double[] mix = ws.Take(ref ws.Mix, len);
            for (int n = 0; n < len; n++) mix[n] = SfxConsts.MasterGain * sfxBus[n] + SfxConsts.MusicGain * musicBus[n];

            if (hasDly)
            {
                int d = Math.Max(1, (int)Math.Round(score.DelaySeconds * sr));
                double[] ring = ws.Take(ref ws.Ring, d);
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
                Dsp.ConvolverKernel k = Kernel(sr, IrSeed);
                double[] wet = ws.Take(ref ws.Wet, len);
                Dsp.Convolve(rvbSend, len, k, len, wet, ws.Take(ref ws.FftRe, k.FftSize), ws.Take(ref ws.FftIm, k.FftSize));
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
            ws.Noise.ReleaseAll();   // 이 점수의 노이즈 버퍼는 다 읽었다 — 다음 점수가 같은 길이로 다시 쓴다
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

        /// <summary>효과음 한 건을 점수로(시드 = 지터·노이즈 난수 · ws 를 주면 노이즈 버퍼를 그 풀에서 빌린다 — 렌더가 끝나면 돌려준다).</summary>
        public static Score SfxScore(SfxCall call, IReadOnlyList<string> rarities, uint seed, int sampleRate = DefaultSampleRate, RenderWorkspace ws = null)
        {
            var s = new SfxSynth(sampleRate, Rng.Mulberry(seed), ws != null ? ws.Noise : null);
            SfxRecipes.Build(s, call, rarities);
            return s.Score;
        }

        /// <summary>효과음 한 건을 샘플로.</summary>
        public static RenderedClip RenderSfx(SfxCall call, IReadOnlyList<string> rarities, uint seed, int sampleRate = DefaultSampleRate, RenderWorkspace ws = null)
        {
            return SynthRenderer.Render(SfxScore(call, rarities, seed, sampleRate, ws), 0, ws);
        }

        /// <summary>음악 모드 하나의 8마디 루프를 점수로(steps 를 주면 앞부분만).</summary>
        public static Score MusicScore(SfxTable table, string mode, uint seed, out double seconds, int sampleRate = DefaultSampleRate, int steps = 0, RenderWorkspace ws = null)
        {
            var s = new SfxSynth(sampleRate, Rng.Mulberry(seed), ws != null ? ws.Noise : null);
            seconds = MusicSequencer.BuildLoop(s, table, mode, steps);
            return s.Score;
        }

        /// <summary>음악 모드 하나의 루프 클립(이음새 접기 포함). steps 를 주면 비루프로 앞부분만 굽는다(테스트).</summary>
        public static RenderedClip RenderMusic(SfxTable table, string mode, uint seed, int sampleRate = DefaultSampleRate, int steps = 0, RenderWorkspace ws = null)
        {
            double sec;
            Score score = MusicScore(table, mode, seed, out sec, sampleRate, steps, ws);
            return SynthRenderer.Render(score, steps > 0 ? 0 : sec, ws);
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
