using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Audio;
using Forge.Core.Data;

namespace Forge.Tests
{
    /// <summary>
    /// T30 — 사운드 이식. 정본 `web/js/sfx.js` 를 node vm 에 **그대로** 올리고 기록하는 가짜 AudioContext 를 물려 찍은 합성 그래프 벡터
    /// (`Assets/Tests/EditMode/Vectors/t30-sfx.json` · `tools/sfx_vectors.js`)와 C# <see cref="SfxRecipes"/>·<see cref="MusicSequencer"/> 가 같은 시드로 만드는
    /// <see cref="Voice"/> 를 보이스 단위로 대조한다 — 노드 종류 · 버스 · 파형/필터 · 자동화 호출(종류·값·시각) · 센드 · 시작/정지 · 노이즈 샘플 수·합.
    /// 렌더러는 길이·피크 ≤ 1 · RMS &gt; 0 · 결정론(같은 시드 → 같은 지문)으로 검증한다(ROUTINE T30 판정).
    /// </summary>
    public class AudioTests
    {
        static SfxTable _table;
        static SfxTable Table { get { return _table ?? (_table = SfxTable.Parse(File.ReadAllText(Path.Combine(DataDir.Path, SfxTable.File)))); } }
        static IReadOnlyList<string> Rarities { get { return DataDir.Game.Defs.Rarities; } }

        static JsonObject _vec;
        static JsonObject Vec
        {
            get
            {
                if (_vec != null) return _vec;
                string file = Path.GetFullPath(Path.Combine(DataDir.Path, "..", "..", "Tests", "EditMode", "Vectors", "t30-sfx.json"));
                if (!File.Exists(file)) throw new FileNotFoundException("T30 벡터가 없다 — node tools/sfx_vectors.js 로 뽑는다", file);
                return _vec = MiniJson.ParseObject(File.ReadAllText(file));
            }
        }

        const int TestRate = 22050;

        static void Near(double expected, double actual, string what)
        {
            double tol = 1e-6 * Math.Max(1, Math.Abs(expected));
            Assert.That(actual, Is.EqualTo(expected).Within(tol), what);
        }

        static string RampChar(RampKind k)
        {
            switch (k) { case RampKind.Value: return "v"; case RampKind.Set: return "s"; case RampKind.Exp: return "e"; default: return "l"; }
        }

        static string WaveName(Wave w)
        {
            switch (w) { case Wave.Sine: return "sine"; case Wave.Triangle: return "triangle"; case Wave.Square: return "square"; default: return "sawtooth"; }
        }

        static string FilterName(FilterType f)
        {
            switch (f) { case FilterType.Lowpass: return "lowpass"; case FilterType.Highpass: return "highpass"; case FilterType.Bandpass: return "bandpass"; default: return "none"; }
        }

        static void CompareParam(List<object> expected, AudioParam actual, string what)
        {
            Assert.AreEqual(expected.Count, actual.Points.Count, what + " 자동화 호출 수");
            for (int i = 0; i < expected.Count; i++)
            {
                List<object> e = J.Arr(expected[i]);
                ParamPoint p = actual.Points[i];
                Assert.AreEqual(J.Str(e[0]), RampChar(p.Kind), what + "[" + i + "] 종류");
                Near(J.Num(e[1]), p.Value, what + "[" + i + "] 값");
                if (e.Count > 2) Near(J.Num(e[2]), p.Time, what + "[" + i + "] 시각");
                else Assert.IsTrue(double.IsNegativeInfinity(p.Time), what + "[" + i + "] `.value =` 대입은 시각이 없다");
            }
        }

        static void CompareVoices(List<object> expected, List<Voice> actual, string label)
        {
            Assert.AreEqual(expected.Count, actual.Count, label + ": 보이스 수");
            for (int i = 0; i < expected.Count; i++)
            {
                JsonObject e = J.Obj(expected[i]);
                Voice v = actual[i];
                string w = label + " #" + i + "(" + J.Str(e["k"]) + ")";
                Assert.AreEqual(J.Str(e["k"]), v.Kind == VoiceKind.Osc ? "osc" : "noise", w + " 종류");
                Assert.AreEqual(J.Str(e["bus"]), v.Bus == Bus.Sfx ? "sfx" : "music", w + " 버스");
                Near(J.Num(e["t0"]), v.Start, w + " start");
                Near(J.Num(e["t1"]), v.Stop, w + " stop");
                if (v.Kind == VoiceKind.Osc)
                {
                    Assert.AreEqual(J.Str(e["type"]), WaveName(v.Wave), w + " 파형");
                    CompareParam(J.Arr(e["f"]), v.Freq, w + " frequency");
                    if (e.Has("lp"))
                    {
                        Assert.AreEqual(FilterType.Lowpass, v.Filter, w + " 개별 lowpass");
                        Near(J.Num(e["lp"]), v.FilterFreq.ValueAt(v.Start), w + " lp");
                    }
                    else Assert.AreEqual(FilterType.None, v.Filter, w + " 필터 없음");
                }
                else
                {
                    Assert.AreEqual(J.Int(e["n"]), v.Noise.Length, w + " 노이즈 샘플 수");
                    Assert.That(SfxSynth.NoiseSum(v.Noise), Is.EqualTo(J.Num(e["sum"])).Within(2e-4), w + " 노이즈 합(같은 난수 순서)");
                    JsonObject f = J.Obj(e["filt"]);
                    Assert.AreEqual(J.Str(f["type"]), FilterName(v.Filter), w + " 필터 종류");
                    CompareParam(J.Arr(f["f"]), v.FilterFreq, w + " filter.frequency");
                    Near(J.Num(f["Q"]), v.Q, w + " Q");
                }
                CompareParam(J.Arr(e["g"]), v.Gain, w + " gain");
                Near(e.Has("rvb") ? J.Num(e["rvb"]) : 0, v.Rvb, w + " 리버브 센드");
                Near(e.Has("dly") ? J.Num(e["dly"]) : 0, v.DelaySend, w + " 딜레이 센드");
            }
        }

        static SfxCall CallOf(string name, List<object> args)
        {
            var c = new SfxCall(name);
            int num = 0;
            for (int i = 0; i < args.Count; i++)
            {
                object a = args[i];
                double val;
                if (a is bool) val = (bool)a ? 1 : 0;
                else if (a is double) val = (double)a;
                else { c.Rarity = J.Str(a); continue; }
                if (num == 0) c.A = val; else c.B = val;
                num++;
            }
            return c;
        }

        [Test]
        public void 표를_읽는다()
        {
            SfxTable t = Table;
            CollectionAssert.AreEqual(new[] { "normal", "boss", "dungeon", "shop" }, t.Modes.Keys, "MUSIC_MODES 순서");
            Assert.AreEqual(4 * t.BarsPerChord * t.StepsPerBar, t.LoopSteps, "MUSIC_LOOP_STEPS");
            Assert.AreEqual(t.BarsPerChord * t.StepsPerBar, t.ChordSteps);
            foreach (var kv in t.Modes)
            {
                MusicMode m = kv.Value;
                Assert.AreEqual(kv.Key, m.Name);
                Assert.Greater(m.Bpm, 0, kv.Key + " bpm");
                Assert.AreEqual(4, m.Prog.Length, kv.Key + " 코드 4개");
                Assert.AreEqual(t.StepsPerBar * 2, m.Mel.Length, kv.Key + " 모티프 32스텝");
                Assert.AreEqual(t.StepsPerBar * 2, m.MelB.Length);
                Assert.IsTrue(m.PadType == "triangle" || m.PadType == "sawtooth", kv.Key + " padType");
                Assert.AreEqual(60.0 / m.Bpm / 4, m.StepDur, 1e-12, kv.Key + " 16분음표");
                int notes = 0;
                for (int i = 0; i < m.Mel.Length; i++) { if (m.Mel[i] != 0) notes++; if (m.MelB[i] != 0) notes++; }
                Assert.Greater(notes, 0, kv.Key + " 멜로디 음");
            }
            Assert.AreEqual(KickKind.None, t.Mode("normal").Kick);
            Assert.AreEqual(KickKind.Hard, t.Mode("boss").Kick);
            Assert.AreEqual(KickKind.Soft, t.Mode("dungeon").Kick);
            Assert.IsTrue(t.Mode("shop").Shaker && !t.Mode("normal").Shaker);
            Assert.IsNull(t.Mode("nope"));
            Assert.IsFalse(t.HasMode(null));
        }

        [Test]
        public void 효과음_24종_이름과_기본_변형()
        {
            CollectionAssert.AreEqual(J.StrArr(Vec["effects"]), SfxRecipes.Names, "정본 게임 효과음 이름·순서");
            Assert.AreEqual(24, SfxRecipes.Names.Length);
            var covered = new HashSet<string>();
            var keys = new HashSet<string>();
            foreach (SfxCall c in SfxRecipes.DefaultCalls(Rarities))
            {
                covered.Add(c.Name);
                Assert.IsTrue(keys.Add(c.Key), "기본 변형 키 중복 " + c.Key);
                Score sc = AudioFactory.SfxScore(c, Rarities, 7, TestRate);
                Assert.Greater(sc.Voices.Count, 0, c.Key);
            }
            CollectionAssert.AreEquivalent(SfxRecipes.Names, covered, "기본 변형이 24종을 다 덮는다");
            Assert.Throws<ArgumentException>(() => SfxRecipes.Build(new SfxSynth(TestRate, Rng.Mulberry(1)), new SfxCall("nope"), Rarities));
            Assert.AreEqual("slashArc:2:3:", new SfxCall("slashArc", 2, 3).Key);
            Assert.AreEqual("gacha:0:0:mythic", new SfxCall("gacha", 0, 0, "mythic").Key);
        }

        [Test]
        public void 효과음_합성_그래프가_정본과_같다()
        {
            List<object> cases = J.Arr(Vec["sfx"]);
            int sr = J.Int(Vec["sampleRate"]);
            Assert.Greater(cases.Count, 40, "사례 수");
            foreach (object o in cases)
            {
                JsonObject c = J.Obj(o);
                SfxCall call = CallOf(J.Str(c["name"]), J.Arr(c["args"]));
                Score sc = AudioFactory.SfxScore(call, Rarities, J.UInt(c["seed"]), sr);
                CompareVoices(J.Arr(c["voices"]), sc.Voices, call.Key);
            }
        }

        [Test]
        public void 음악_4모드_128스텝_그래프가_정본과_같다()
        {
            List<object> modes = J.Arr(Vec["music"]);
            int sr = J.Int(Vec["sampleRate"]);
            Assert.AreEqual(4, modes.Count);
            foreach (object o in modes)
            {
                JsonObject m = J.Obj(o);
                string mode = J.Str(m["mode"]);
                double sec;
                Score sc = AudioFactory.MusicScore(Table, mode, J.UInt(m["seed"]), out sec, sr);
                Near(J.Num(m["stepDur"]), Table.Mode(mode).StepDur, mode + " 16분음표");
                Assert.AreEqual(J.Int(m["steps"]), Table.LoopSteps, mode + " 루프 스텝");
                Near(J.Int(m["steps"]) * J.Num(m["stepDur"]), sec, mode + " 루프 길이");
                Near(Table.Mode(mode).StepDur * SfxConsts.DelayStepsDotted8th, sc.DelaySeconds, mode + " 점8분 딜레이");
                CompareVoices(J.Arr(m["voices"]), sc.Voices, mode);
            }
        }

        [Test]
        public void 효과음_렌더_길이_피크_RMS_결정론()
        {
            int checkedJitter = 0;
            foreach (string name in SfxRecipes.Names)
            {
                var call = new SfxCall(name, 0, 0, "common");
                RenderedClip a = AudioFactory.RenderSfx(call, Rarities, 11, TestRate);
                RenderedClip b = AudioFactory.RenderSfx(call, Rarities, 11, TestRate);
                Score sc = AudioFactory.SfxScore(call, Rarities, 11, TestRate);
                double expect = sc.End + (sc.HasReverb ? SfxConsts.ReverbSeconds : 0.05);
                Assert.AreEqual(expect, a.Seconds, 1.0 / TestRate + 1e-9, name + " 길이 = 마지막 보이스 + 꼬리");
                Assert.LessOrEqual(a.Peak, 1f, name + " 피크 ≤ 1");
                Assert.Greater(a.Peak, 0.01f, name + " 무음 아님");
                Assert.Greater(a.Rms, 1e-4, name + " RMS > 0");
                Assert.AreEqual(a.Fingerprint, b.Fingerprint, name + " 같은 시드 → 같은 샘플");
                bool jitter = false;
                foreach (Voice v in sc.Voices) if (v.Kind == VoiceKind.Noise) { jitter = true; break; }
                if (jitter)
                {
                    RenderedClip c = AudioFactory.RenderSfx(call, Rarities, 12, TestRate);
                    Assert.AreNotEqual(a.Fingerprint, c.Fingerprint, name + " 다른 시드 → 다른 소리(호출마다 피치·노이즈 랜덤화)");
                    checkedJitter++;
                }
            }
            Assert.Greater(checkedJitter, 20);
        }

        [Test]
        public void 음악_4모드_8박_렌더()
        {
            foreach (var kv in Table.Modes)
            {
                string mode = kv.Key;
                int steps = Table.StepsPerBar * 2;
                RenderedClip a = AudioFactory.RenderMusic(Table, mode, 21, TestRate, steps);
                RenderedClip b = AudioFactory.RenderMusic(Table, mode, 21, TestRate, steps);
                double sec;
                Score sc = AudioFactory.MusicScore(Table, mode, 21, out sec, TestRate, steps);
                Assert.Greater(a.Seconds, steps * kv.Value.StepDur, mode + " 8박보다 길다(패드·딜레이·리버브 꼬리)");
                Assert.LessOrEqual(a.Peak, 1f, mode + " 피크 ≤ 1");
                Assert.Greater(a.Rms, 1e-3, mode + " RMS > 0");
                Assert.AreEqual(a.Fingerprint, b.Fingerprint, mode + " 결정론");
                Assert.IsTrue(sc.HasReverb && sc.HasDelay, mode + " 패드 리버브 · 아르페지오/멜로디 딜레이 센드");
            }
        }

        [Test]
        public void 음악_루프는_길이가_딱_맞고_이음새_꼬리를_앞으로_접는다()
        {
            const int sr = 8000;
            MusicMode m = Table.Mode("normal");
            RenderedClip loop = AudioFactory.RenderMusic(Table, "normal", 3, sr);
            Assert.AreEqual((int)Math.Round(Table.LoopSteps * m.StepDur * sr), loop.Samples.Length, "루프 길이 = 128스텝");
            Assert.LessOrEqual(loop.Peak, 1f);
            Assert.Greater(loop.Rms, 1e-3);
            double head = 0;
            int n = sr / 10;
            for (int i = 0; i < n; i++) head += Math.Abs(loop.Samples[i]);
            Assert.Greater(head / n, 1e-4, "루프 첫 0.1초에 앞으로 접힌 꼬리 + 첫 박이 있다");
        }

        [Test]
        public void 되쓰기_작업_공간으로_구워도_결과가_같고_배열을_다시_쓴다()
        {
            // T73 — AudioBank 워커가 잡 사이에 되쓰는 RenderWorkspace: 긴 음악을 먼저 구워 배열을 더럽힌 뒤 효과음·음악을 다시 구워도
            // 새 배열로 구운 것과 지문(float 비트)까지 같아야 한다(«길이보다 긴 배열» · «지운 뒤 더하기» · 노이즈 풀 되쓰기가 소리를 못 바꾼다).
            var ws = new RenderWorkspace();
            int steps = Table.StepsPerBar * 2;
            AudioFactory.RenderMusic(Table, "normal", 21, TestRate, steps, ws);
            int grownAfterMusic = ws.Grown;
            double[] busRef = ws.SfxBus;
            int noiseCreated = ws.Noise.Created;
            int compared = 0;
            foreach (string name in SfxRecipes.Names)
            {
                var call = new SfxCall(name, 0, 0, "common");
                RenderedClip fresh = AudioFactory.RenderSfx(call, Rarities, 11, TestRate);
                RenderedClip pooled = AudioFactory.RenderSfx(call, Rarities, 11, TestRate, ws);
                Assert.AreEqual(fresh.Samples.Length, pooled.Samples.Length, name + " 길이");
                Assert.AreEqual(fresh.Fingerprint, pooled.Fingerprint, name + " 되쓰기 뒤에도 같은 샘플");
                compared++;
            }
            Assert.Greater(compared, 20);
            Assert.AreEqual(0, ws.Noise.Rented, "렌더가 끝나면 노이즈 버퍼는 전부 풀로 돌아간다");
            Assert.AreSame(busRef, ws.SfxBus, "음악(가장 긴 잡)이 잡은 버스를 효과음이 그대로 되쓴다");
            Assert.AreEqual(grownAfterMusic, ws.Grown, "효과음 24종을 굽는 동안 버스·FFT 배열을 새로 잡지 않는다");
            // 음악도 같은 작업 공간으로 두 번 — 같은 지문 · 노이즈 버퍼(하이햇 · 같은 길이)는 두 번째부터 새로 안 만든다
            foreach (var kv in Table.Modes)
            {
                RenderedClip fresh = AudioFactory.RenderMusic(Table, kv.Key, 21, TestRate, steps);
                RenderedClip pooled = AudioFactory.RenderMusic(Table, kv.Key, 21, TestRate, steps, ws);
                Assert.AreEqual(fresh.Fingerprint, pooled.Fingerprint, kv.Key + " 음악 되쓰기 지문");
            }
            int createdBeforeRepeat = ws.Noise.Created;
            AudioFactory.RenderMusic(Table, "normal", 21, TestRate, steps, ws);
            Assert.AreEqual(createdBeforeRepeat, ws.Noise.Created, "같은 음악을 다시 구우면 노이즈 버퍼를 하나도 새로 만들지 않는다");
            Assert.Greater(noiseCreated, 0, "첫 음악은 노이즈 버퍼를 만들었다(자가 도는지)");
            // 루프 접기 갈래도 같은 지문
            RenderedClip loopA = AudioFactory.RenderMusic(Table, "normal", 3, 8000);
            RenderedClip loopB = AudioFactory.RenderMusic(Table, "normal", 3, 8000, 0, ws);
            Assert.AreEqual(loopA.Fingerprint, loopB.Fingerprint, "루프 접기도 되쓰기 뒤 같은 샘플");
            // 되쓰기 갈래의 관리 할당은 «결과 float[] + 점수 객체» 뿐이어야 한다 — 새 배열 갈래의 절반 아래(버스 5 × double 이 빠진다)
            long a0 = AllocatedNow(); if (a0 >= 0)
            {
                var call = new SfxCall("gacha", 0, 0, "common");
                long f0 = AllocatedNow(); AudioFactory.RenderSfx(call, Rarities, 11, TestRate); long fresh = AllocatedNow() - f0;
                long p0 = AllocatedNow(); AudioFactory.RenderSfx(call, Rarities, 11, TestRate, ws); long pooled = AllocatedNow() - p0;
                Assert.Less(pooled, fresh / 2, "되쓰기 갈래 관리 할당 " + pooled + "B < 새 배열 갈래 " + fresh + "B 의 절반");
            }
        }

        /// <summary>이 스레드가 지금까지 할당한 관리 바이트 — 런타임이 못 주면 -1(그 단언은 건너뛴다).</summary>
        static long AllocatedNow()
        {
            try { long b = GC.GetAllocatedBytesForCurrentThread(); return b > 0 ? b : -1; }
            catch (Exception) { return -1; }
        }

        [Test]
        public void 자동화_램프_규약()
        {
            var p = new AudioParam(1);
            p.SetValueAtTime(0.0001, 1.0);
            p.ExponentialRampToValueAtTime(0.3, 1.1);
            p.ExponentialRampToValueAtTime(0.0001, 1.5);
            Assert.AreEqual(1, p.ValueAt(0.5), 1e-12, "첫 이벤트 전 = 기본값");
            Assert.AreEqual(0.0001, p.ValueAt(1.0), 1e-12);
            Assert.AreEqual(0.0001 * Math.Pow(3000, 0.5), p.ValueAt(1.05), 1e-9, "지수 램프 중간");
            Assert.AreEqual(0.3, p.ValueAt(1.1), 1e-12, "램프 끝 = 목표");
            Assert.AreEqual(0.0001, p.ValueAt(2.0), 1e-12, "마지막 뒤 = 마지막 목표");
            var q = new AudioParam(350);
            q.Assign(2600);
            Assert.IsTrue(q.IsConstant);
            Assert.AreEqual(2600, q.ValueAt(-5), 1e-12, "`.value =` 는 시각이 없다");
            var l = new AudioParam(1);
            l.SetValueAtTime(0.001, 0);
            l.LinearRampToValueAtTime(0.5, 0.01);
            Assert.AreEqual(0.2505, l.ValueAt(0.005), 1e-12, "선형 램프 중간");
            Assert.IsFalse(l.IsConstant);
        }

        [Test]
        public void DSP_조각()
        {
            var lp = new Dsp.Biquad();
            lp.Set(FilterType.Lowpass, 20000, 1, 8000);
            Assert.AreEqual(0.7, lp.Process(0.7), 1e-12, "나이퀴스트 이상 lowpass = 통과");
            var hp = new Dsp.Biquad();
            hp.Set(FilterType.Highpass, 20000, 1, 8000);
            Assert.AreEqual(0, hp.Process(0.7), 1e-12, "나이퀴스트 이상 highpass = 무음");
            var bp = new Dsp.Biquad();
            bp.Set(FilterType.Bandpass, 1000, 1.4, 44100);
            double e = 0;
            for (int i = 0; i < 4410; i++) { double v = bp.Process(Math.Sin(2 * Math.PI * 1000 * i / 44100.0)); if (i > 2205) e += v * v; }
            Assert.Greater(Math.Sqrt(e / 2205), 0.5, "밴드패스 중심 주파수는 거의 그대로");
            double[] re = { 1, 0, 0, 0, 0, 0, 0, 0 }, im = new double[8];
            Dsp.Fft(re, im, false);
            for (int i = 0; i < 8; i++) { Assert.AreEqual(1, re[i], 1e-12); Assert.AreEqual(0, im[i], 1e-12); }
            Dsp.Fft(re, im, true);
            Assert.AreEqual(1, re[0], 1e-12); Assert.AreEqual(0, re[3], 1e-12);
            var k = new Dsp.ConvolverKernel(new[] { 0.5f, 0.25f });
            double[] y = Dsp.Convolve(new double[] { 1, 0, 0, 2 }, k, 6);
            Assert.AreEqual(0.5, y[0], 1e-9); Assert.AreEqual(0.25, y[1], 1e-9); Assert.AreEqual(0, y[2], 1e-9); Assert.AreEqual(1, y[3], 1e-9); Assert.AreEqual(0.5, y[4], 1e-9); Assert.AreEqual(0, y[5], 1e-9);
            Assert.AreEqual(0, Dsp.CompressorGainDb(-20, -12, 18, 5), 1e-12, "문턱 아래");
            Assert.AreEqual(-(1 - 1 / 5.0) * 12 * 12 / (2 * 18.0), Dsp.CompressorGainDb(0, -12, 18, 5), 1e-12, "0 dBFS 는 니 안(−12~+6)");
            Assert.AreEqual(-(1 - 1 / 5.0) * (10 + 12 - 9), Dsp.CompressorGainDb(10, -12, 18, 5), 1e-12, "니 위 = ratio 직선");
            Assert.Less(Dsp.CompressorGainDb(-6, -12, 18, 5), 0, "니 안은 조금 눌린다");
            float[] ir = Dsp.MakeIR(8000, 1.4, 2.6, Rng.Mulberry(SynthRenderer.IrSeed));
            Assert.AreEqual(11200, ir.Length);
            Assert.AreEqual(0, ir[ir.Length - 1], 1e-3, "IR 꼬리는 0 으로");
            var buf = new float[8000];
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)(1.5 * Math.Sin(2 * Math.PI * 100 * i / 8000.0));
            Dsp.Compress(buf, 8000, -12, 18, 5, 0.004, 0.22);
            float pk = 0; foreach (float s in buf) pk = Math.Max(pk, Math.Abs(s));
            Assert.LessOrEqual(pk, 1f, "리미터 뒤 클램프");
        }
    }
}
