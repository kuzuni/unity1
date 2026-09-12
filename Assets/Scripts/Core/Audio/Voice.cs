using System;
using System.Collections.Generic;

namespace Forge.Core.Audio
{
    /// <summary>원작 오실레이터 파형 문자열(sine/triangle/square/sawtooth).</summary>
    public enum Wave { Sine, Triangle, Square, Sawtooth }

    /// <summary>BiquadFilter 종류 — 원작 noiseBurst 의 `type` · 음악 패드의 개별 lowpass.</summary>
    public enum FilterType { None, Lowpass, Highpass, Bandpass }

    /// <summary>버스 — master(SFX · 0.35) / musicGain(BGM · 0.16). 둘 다 소프트 리미터(comp)로 모인다.</summary>
    public enum Bus { Sfx, Music }

    public enum VoiceKind { Osc, Noise }

    /// <summary>WebAudio AudioParam 자동화 한 점 — `.value = v`(Value) · setValueAtTime(Set) · exponentialRampToValueAtTime(Exp) · linearRampToValueAtTime(Lin).</summary>
    public struct ParamPoint
    {
        public RampKind Kind;
        public double Value;
        public double Time;
        public ParamPoint(RampKind kind, double value, double time) { Kind = kind; Value = value; Time = time; }
    }

    public enum RampKind { Value, Set, Exp, Lin }

    /// <summary>WebAudio AudioParam 의 자동화 목록 + 그 시각의 값(<see cref="ValueAt"/>) — 렌더러가 샘플마다 읽는다.</summary>
    public sealed class AudioParam
    {
        public readonly List<ParamPoint> Points = new List<ParamPoint>(3);
        public readonly double Default;

        public AudioParam(double def) { Default = def; }

        /// <summary>`param.value = v`.</summary>
        public void Assign(double v) { Points.Add(new ParamPoint(RampKind.Value, v, double.NegativeInfinity)); }
        public void SetValueAtTime(double v, double t) { Points.Add(new ParamPoint(RampKind.Set, v, t)); }
        public void ExponentialRampToValueAtTime(double v, double t) { Points.Add(new ParamPoint(RampKind.Exp, v, t)); }
        public void LinearRampToValueAtTime(double v, double t) { Points.Add(new ParamPoint(RampKind.Lin, v, t)); }

        /// <summary>자동화가 없거나 상수 대입 하나뿐인가(필터 계수를 한 번만 계산해도 되는가).</summary>
        public bool IsConstant { get { return Points.Count == 0 || (Points.Count == 1 && Points[0].Kind != RampKind.Exp && Points[0].Kind != RampKind.Lin); } }

        /// <summary>WebAudio 규약: 램프는 «직전 이벤트의 값·시각» 에서 목표 시각까지 지수/선형으로 가고 그 뒤로는 목표값에 머문다.</summary>
        public double ValueAt(double t)
        {
            double v = Default, vt = double.NegativeInfinity;
            for (int i = 0; i < Points.Count; i++)
            {
                ParamPoint p = Points[i];
                switch (p.Kind)
                {
                    case RampKind.Value:
                        v = p.Value; vt = double.NegativeInfinity;
                        break;
                    case RampKind.Set:
                        if (t < p.Time) return v;
                        v = p.Value; vt = p.Time;
                        break;
                    default:
                        if (t < p.Time)
                        {
                            if (t <= vt || double.IsNegativeInfinity(vt)) return v;
                            double a = (t - vt) / (p.Time - vt);
                            if (p.Kind == RampKind.Exp) return v * Math.Pow(p.Value / v, a);
                            return v + (p.Value - v) * a;
                        }
                        v = p.Value; vt = p.Time;
                        break;
                }
            }
            return v;
        }
    }

    /// <summary>
    /// 합성 그래프의 «소리 한 줄기» — 원작이 `createOscillator`/`createBufferSource` 한 번에 만드는 것: 소스 → (필터) → 엔벨로프 게인 → 버스,
    /// 엔벨로프 뒤에서 리버브/딜레이 센드. <see cref="SfxSynth"/> 가 만들고 <see cref="SynthRenderer"/> 가 샘플로 굽는다.
    /// 정본 대조(`tools/sfx_vectors.js`)의 단위이기도 하다 — 칸 이름이 곧 원작 노드·자동화 호출이다.
    /// </summary>
    public sealed class Voice
    {
        public VoiceKind Kind;
        public Bus Bus;
        /// <summary>오실레이터 파형(Kind == Osc).</summary>
        public Wave Wave;
        /// <summary>osc.frequency 자동화(Kind == Osc).</summary>
        public AudioParam Freq;
        /// <summary>엔벨로프 게인(g.gain) 자동화.</summary>
        public readonly AudioParam Gain = new AudioParam(1);
        /// <summary>필터 — noise 는 항상 하나를 지난다 · osc 는 음악 패드의 개별 lowpass(`opts.lp`)뿐.</summary>
        public FilterType Filter = FilterType.None;
        public AudioParam FilterFreq;
        public double Q = 1;
        /// <summary>리버브 센드 게인(0 = 없음).</summary>
        public double Rvb;
        /// <summary>음악 템포 딜레이 센드 게인(0 = 없음).</summary>
        public double DelaySend;
        /// <summary>osc.start(t) / src.start(t).</summary>
        public double Start;
        /// <summary>osc.stop(t) · noise 는 start + n / sampleRate.</summary>
        public double Stop;
        /// <summary>노이즈 버퍼(Kind == Noise · 원작 `(Math.random()*2-1)*(1-i/n)` 을 float32 로).</summary>
        public float[] Noise;

        public double Duration { get { return Stop - Start; } }
    }

    /// <summary>한 효과음(또는 음악 루프)이 만드는 보이스 전부 + 렌더에 필요한 버스 상수.</summary>
    public sealed class Score
    {
        public readonly List<Voice> Voices = new List<Voice>();
        public readonly int SampleRate;
        /// <summary>음악 딜레이 시간(초 · 점8분 = 6 스텝) — 음악 점수에서만 0 이 아니다.</summary>
        public double DelaySeconds;

        public Score(int sampleRate) { SampleRate = sampleRate; }

        /// <summary>마지막 보이스가 끝나는 시각(초).</summary>
        public double End
        {
            get
            {
                double e = 0;
                for (int i = 0; i < Voices.Count; i++) if (Voices[i].Stop > e) e = Voices[i].Stop;
                return e;
            }
        }

        public bool HasReverb
        {
            get { for (int i = 0; i < Voices.Count; i++) if (Voices[i].Rvb > 0) return true; return false; }
        }

        public bool HasDelay
        {
            get { for (int i = 0; i < Voices.Count; i++) if (Voices[i].DelaySend > 0) return true; return false; }
        }
    }
}
