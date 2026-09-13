using System;
using Forge.Core.Data;

namespace Forge.Core.Audio
{
    /// <summary>순수 C# DSP 조각 — 오실레이터 · WebAudio 규약 Biquad · FFT 합성곱 · 합성 IR · 소프트 리미터. 엔진 참조 0.</summary>
    public static class Dsp
    {
        const double TwoPi = 2 * Math.PI;

        /// <summary>파형 한 샘플 — 위상 phase ∈ [0, 1). (WebAudio 는 대역 제한 파형이지만 짧은 효과음·저역 위주라 순수형으로 근사 — 결정 기록)</summary>
        public static double Osc(Wave w, double phase)
        {
            switch (w)
            {
                case Wave.Sine: return Math.Sin(TwoPi * phase);
                case Wave.Square: return phase < 0.5 ? 1 : -1;
                case Wave.Sawtooth: return 2 * phase - 1;
                default:
                    return phase < 0.25 ? 4 * phase : phase < 0.75 ? 2 - 4 * phase : 4 * phase - 4;
            }
        }

        /// <summary>WebAudio BiquadFilterNode 규약의 2차 IIR(DF2T). lowpass/highpass 의 Q 는 dB · bandpass 의 Q 는 선형(스펙 그대로).</summary>
        public struct Biquad
        {
            double b0, b1, b2, a1, a2;
            double z1, z2;

            public void Set(FilterType type, double freq, double q, int sampleRate)
            {
                double nyq = sampleRate * 0.5;
                if (type == FilterType.None) { b0 = 1; b1 = b2 = a1 = a2 = 0; return; }
                if (freq >= nyq)
                {
                    if (type == FilterType.Lowpass) { b0 = 1; b1 = b2 = a1 = a2 = 0; }
                    else { b0 = b1 = b2 = a1 = a2 = 0; }
                    return;
                }
                if (freq <= 0)
                {
                    if (type == FilterType.Highpass) { b0 = 1; b1 = b2 = a1 = a2 = 0; }
                    else { b0 = b1 = b2 = a1 = a2 = 0; }
                    return;
                }
                double w0 = TwoPi * freq / sampleRate;
                double cs = Math.Cos(w0), sn = Math.Sin(w0);
                double B0, B1, B2, A0, A1, A2;
                if (type == FilterType.Bandpass)
                {
                    double alpha = sn / (2 * Math.Max(1e-6, q));
                    B0 = alpha; B1 = 0; B2 = -alpha;
                    A0 = 1 + alpha; A1 = -2 * cs; A2 = 1 - alpha;
                }
                else
                {
                    double alpha = sn / (2 * Math.Pow(10, q / 20));
                    if (type == FilterType.Lowpass) { B0 = (1 - cs) / 2; B1 = 1 - cs; B2 = (1 - cs) / 2; }
                    else { B0 = (1 + cs) / 2; B1 = -(1 + cs); B2 = (1 + cs) / 2; }
                    A0 = 1 + alpha; A1 = -2 * cs; A2 = 1 - alpha;
                }
                b0 = B0 / A0; b1 = B1 / A0; b2 = B2 / A0; a1 = A1 / A0; a2 = A2 / A0;
            }

            public double Process(double x)
            {
                double y = b0 * x + z1;
                z1 = b1 * x - a1 * y + z2;
                z2 = b2 * x - a2 * y;
                return y;
            }
        }

        /// <summary>`_makeIR(seconds, decayPow)` — 지수 감쇠 노이즈 IR · 꼬리로 갈수록 1폴 저역만 남긴다.</summary>
        public static float[] MakeIR(int sampleRate, double seconds, double decayPow, Rng rng)
        {
            int n = (int)Math.Floor(sampleRate * seconds);
            var d = new float[n];
            double lp = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / n;
                double w = (rng.Random() * 2 - 1) * Math.Pow(1 - t, decayPow);
                lp += (w - lp) * (1 - t * 0.85);
                d[i] = (float)lp;
            }
            return d;
        }

        // ---- FFT 합성곱 (ConvolverNode 자리 · 균일 분할 overlap-add) ----

        /// <summary>제자리 복소 radix-2 FFT(n 은 2의 거듭제곱). inverse 면 1/n 스케일까지.</summary>
        public static void Fft(double[] re, double[] im, bool inverse) { Fft(re, im, inverse, re.Length); }

        /// <summary>앞 <paramref name="n"/> 칸만 변환한다(배열이 더 길어도 된다 · T73 되쓰기).</summary>
        public static void Fft(double[] re, double[] im, bool inverse, int n)
        {
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    double tr = re[i]; re[i] = re[j]; re[j] = tr;
                    double ti = im[i]; im[i] = im[j]; im[j] = ti;
                }
            }
            for (int len = 2; len <= n; len <<= 1)
            {
                double ang = TwoPi / len * (inverse ? 1 : -1);
                double wr = Math.Cos(ang), wi = Math.Sin(ang);
                int half = len >> 1;
                for (int i = 0; i < n; i += len)
                {
                    double cr = 1, ci = 0;
                    for (int k = 0; k < half; k++)
                    {
                        int a = i + k, b = a + half;
                        double xr = re[b] * cr - im[b] * ci;
                        double xi = re[b] * ci + im[b] * cr;
                        re[b] = re[a] - xr; im[b] = im[a] - xi;
                        re[a] += xr; im[a] += xi;
                        double ncr = cr * wr - ci * wi;
                        ci = cr * wi + ci * wr;
                        cr = ncr;
                    }
                }
            }
            if (inverse)
            {
                double inv = 1.0 / n;
                for (int i = 0; i < n; i++) { re[i] *= inv; im[i] *= inv; }
            }
        }

        static int NextPow2(int n) { int p = 1; while (p < n) p <<= 1; return p; }

        /// <summary>미리 FFT 한 IR — 같은 표본율·시드의 클립들이 공유한다.</summary>
        public sealed class ConvolverKernel
        {
            public readonly int IrLength;
            public readonly int FftSize;
            public readonly int BlockSize;
            public readonly double[] Re, Im;

            public ConvolverKernel(float[] ir)
            {
                IrLength = ir.Length;
                FftSize = NextPow2(Math.Max(2, IrLength * 2));
                BlockSize = FftSize - IrLength + 1;
                Re = new double[FftSize]; Im = new double[FftSize];
                for (int i = 0; i < IrLength; i++) Re[i] = ir[i];
                Fft(Re, Im, false);
            }
        }

        /// <summary>x ⊛ ir 를 outLen 샘플까지(overlap-add · 블록마다 FFT 두 번). 배열을 새로 잡는 갈래 — 되쓰기는 아래 오버로드.</summary>
        public static double[] Convolve(double[] x, ConvolverKernel k, int outLen)
        {
            var y = new double[outLen];
            Convolve(x, x.Length, k, outLen, y, new double[k.FftSize], new double[k.FftSize]);
            return y;
        }

        /// <summary>
        /// 같은 합성곱을 호출자가 준 배열에(T73 되쓰기): <paramref name="x"/> 의 앞 <paramref name="xLen"/> 칸만 입력으로, 출력은 <paramref name="y"/> 의 앞 <paramref name="outLen"/> 칸(0 으로 지우고 더한다),
        /// <paramref name="re"/>·<paramref name="im"/> 은 길이 ≥ <c>k.FftSize</c> 의 작업 배열. 배열이 더 길어도 결과는 같다(모든 루프가 길이 인자까지만 본다).
        /// </summary>
        public static void Convolve(double[] x, int xLen, ConvolverKernel k, int outLen, double[] y, double[] re, double[] im)
        {
            int n = k.FftSize;
            if (re.Length < n || im.Length < n) throw new ArgumentException("FFT 작업 배열이 FftSize 보다 짧다");
            if (y.Length < outLen) throw new ArgumentException("출력 배열이 outLen 보다 짧다");
            Array.Clear(y, 0, outLen);
            for (int start = 0; start < xLen && start < outLen; start += k.BlockSize)
            {
                Array.Clear(re, 0, n); Array.Clear(im, 0, n);
                int cnt = Math.Min(k.BlockSize, xLen - start);
                bool any = false;
                for (int i = 0; i < cnt; i++) { re[i] = x[start + i]; if (re[i] != 0) any = true; }
                if (!any) continue;
                Fft(re, im, false, n);
                for (int i = 0; i < n; i++)
                {
                    double r = re[i] * k.Re[i] - im[i] * k.Im[i];
                    double m = re[i] * k.Im[i] + im[i] * k.Re[i];
                    re[i] = r; im[i] = m;
                }
                Fft(re, im, true, n);
                int lim = Math.Min(n, outLen - start);
                for (int i = 0; i < lim; i++) y[start + i] += re[i];
            }
        }

        // ---- 소프트 리미터 (DynamicsCompressorNode 자리) ----

        static double DbToLin(double db) { return Math.Pow(10, db / 20); }
        static double LinToDb(double x) { return 20 * Math.Log10(Math.Max(1e-9, x)); }

        /// <summary>정적 곡선 — 문턱 아래 0 dB · 문턱~문턱+knee 는 2차 소프트 니 · 그 위는 ratio.</summary>
        public static double CompressorGainDb(double inDb, double thresholdDb, double kneeDb, double ratio)
        {
            double slope = 1 - 1 / ratio;
            if (inDb <= thresholdDb) return 0;
            if (kneeDb > 0 && inDb < thresholdDb + kneeDb)
            {
                double over = inDb - thresholdDb;
                return -slope * over * over / (2 * kneeDb);
            }
            return -slope * (inDb - thresholdDb - kneeDb / 2);
        }

        /// <summary>
        /// 피드포워드 컴프레서 + 자동 메이크업(Chromium DynamicsCompressor 의 `fullRangeMakeupGain = (1 / gain(0 dBFS))^0.6` 규약).
        /// 원작은 버스 합에 한 번 거는데 유니티는 클립마다 미리 건다(ROUTINE T30 절) — 겹칠 때의 합은 유니티 출력에서 클램프된다.
        /// </summary>
        public static void Compress(float[] buf, int sampleRate, double thresholdDb, double kneeDb, double ratio, double attack, double release)
        {
            double aCoef = 1 - Math.Exp(-1.0 / (Math.Max(1e-4, attack) * sampleRate));
            double rCoef = 1 - Math.Exp(-1.0 / (Math.Max(1e-4, release) * sampleRate));
            double makeup = Math.Pow(1 / DbToLin(CompressorGainDb(0, thresholdDb, kneeDb, ratio)), 0.6);
            double env = 1;
            for (int i = 0; i < buf.Length; i++)
            {
                double x = buf[i];
                double target = DbToLin(CompressorGainDb(LinToDb(Math.Abs(x)), thresholdDb, kneeDb, ratio));
                env += (target - env) * (target < env ? aCoef : rCoef);
                double y = x * env * makeup;
                if (y > 1) y = 1; else if (y < -1) y = -1;
                buf[i] = (float)y;
            }
        }
    }
}
