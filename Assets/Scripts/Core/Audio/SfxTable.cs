using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Audio
{
    /// <summary>
    /// `sfx.json`(T2 추출기가 정본 `web/js/sfx.js` 의 최상위 const 를 뽑은 것) — 음악 모드 4종(`MUSIC_MODES`)과 스텝 상수.
    /// 효과음 레시피·합성 프리미티브는 표가 아니라 함수라 <see cref="SfxRecipes"/> 가 함수 단위로 옮긴다(T30).
    /// </summary>
    public sealed class SfxTable
    {
        public const string File = "sfx.json";

        /// <summary>MUSIC_MODES — 키 순서 = 원작 순서(normal · boss · dungeon · shop).</summary>
        public OrderedMap<MusicMode> Modes;
        /// <summary>MUSIC_STEPS_PER_BAR(16분음표 해상도).</summary>
        public int StepsPerBar;
        /// <summary>MUSIC_BARS_PER_CHORD.</summary>
        public int BarsPerChord;
        /// <summary>MUSIC_LOOP_STEPS = 4 × BARS_PER_CHORD × STEPS_PER_BAR.</summary>
        public int LoopSteps;

        /// <summary>코드 한 개가 차지하는 스텝 수(원작 `_musicScheduleStep` 의 chordSteps).</summary>
        public int ChordSteps { get { return BarsPerChord * StepsPerBar; } }

        public MusicMode Mode(string name) { return Modes.Get(name, null); }
        public bool HasMode(string name) { return name != null && Modes.Has(name); }

        public static SfxTable Parse(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            JsonObject o = MiniJson.ParseObject(json);
            var t = new SfxTable();
            t.StepsPerBar = J.Int(J.Require(o, "MUSIC_STEPS_PER_BAR"));
            t.BarsPerChord = J.Int(J.Require(o, "MUSIC_BARS_PER_CHORD"));
            t.LoopSteps = J.Int(J.Require(o, "MUSIC_LOOP_STEPS"));
            t.Modes = new OrderedMap<MusicMode>();
            JsonObject modes = J.Obj(J.Require(o, "MUSIC_MODES"));
            if (modes == null) throw new FormatException("sfx.json: MUSIC_MODES 가 객체가 아니다");
            foreach (var kv in modes) t.Modes.Add(kv.Key, MusicMode.From(kv.Key, J.Obj(kv.Value), t.StepsPerBar * 2));
            if (t.LoopSteps != 4 * t.BarsPerChord * t.StepsPerBar) throw new FormatException("sfx.json: MUSIC_LOOP_STEPS ≠ 4 × BARS_PER_CHORD × STEPS_PER_BAR");
            return t;
        }
    }

    /// <summary>코드 진행 한 칸 — 근음(bass) · 패드 화음(pad) · 아르페지오 코드 톤(arp). 전부 MIDI 번호.</summary>
    public sealed class MusicChord
    {
        public int Bass;
        public int[] Pad;
        public int[] Arp;
    }

    /// <summary>원작 `cfg.kick` — false / true / 'soft'.</summary>
    public enum KickKind { None, Hard, Soft }

    /// <summary>MUSIC_MODES 의 모드 하나(원작 주석: bpm · 코드 진행 · 멜로디 A/B · 층별 패턴 플래그).</summary>
    public sealed class MusicMode
    {
        public string Name;
        public double Bpm;
        public double Swing;
        public MusicChord[] Prog;
        /// <summary>2마디(32스텝) 모티프 — 인덱스 = 스텝 · 0 = 쉼(원작 `if (mel)`).</summary>
        public int[] Mel;
        public int[] MelB;
        public int[] BassSteps;
        public int ArpEvery;
        public int ArpOff;
        public string PadType;
        public double PadLp;
        public int HatEvery;
        public int HatOff;
        public KickKind Kick;
        public bool Shaker;

        /// <summary>16분음표 길이(초) — 원작 `_applyModeTiming`: 60 / bpm / 4.</summary>
        public double StepDur { get { return 60.0 / Bpm / 4; } }

        public bool HasBassStep(int barStep)
        {
            for (int i = 0; i < BassSteps.Length; i++) if (BassSteps[i] == barStep) return true;
            return false;
        }

        public static MusicMode From(string name, JsonObject o, int motifSteps)
        {
            if (o == null) throw new FormatException("sfx.json: MUSIC_MODES." + name + " 이 객체가 아니다");
            var m = new MusicMode();
            m.Name = name;
            m.Bpm = J.Num(J.Require(o, "bpm"));
            m.Swing = J.Num(J.Require(o, "swing"));
            m.Prog = J.List(J.Require(o, "prog"), v =>
            {
                JsonObject c = J.Obj(v);
                return new MusicChord { Bass = J.Int(J.Require(c, "bass")), Pad = J.IntArr(J.Require(c, "pad")), Arp = J.IntArr(J.Require(c, "arp")) };
            }).ToArray();
            m.Mel = Motif(J.Obj(J.Require(o, "mel")), motifSteps, name + ".mel");
            m.MelB = Motif(J.Obj(J.Require(o, "melB")), motifSteps, name + ".melB");
            m.BassSteps = J.IntArr(J.Require(o, "bassSteps"));
            m.ArpEvery = J.Int(J.Require(o, "arpEvery"));
            m.ArpOff = J.Int(o.Has("arpOff") ? o["arpOff"] : null);
            m.PadType = J.Str(J.Require(o, "padType"));
            m.PadLp = J.Num(J.Require(o, "padLp"));
            m.HatEvery = J.Int(J.Require(o, "hatEvery"));
            m.HatOff = J.Int(J.Require(o, "hatOff"));
            object kick = J.Require(o, "kick");
            m.Kick = kick is bool ? ((bool)kick ? KickKind.Hard : KickKind.None) : (J.Str(kick) == "soft" ? KickKind.Soft : KickKind.None);
            m.Shaker = J.Bool(J.Require(o, "shaker"));
            if (m.Bpm <= 0 || m.Prog.Length == 0) throw new FormatException("sfx.json: MUSIC_MODES." + name + " 의 bpm/prog 가 비었다");
            return m;
        }

        static int[] Motif(JsonObject o, int steps, string where)
        {
            var arr = new int[steps];
            if (o == null) throw new FormatException("sfx.json: " + where + " 가 객체가 아니다");
            foreach (var kv in o)
            {
                int idx;
                if (!int.TryParse(kv.Key, out idx) || idx < 0 || idx >= steps) throw new FormatException("sfx.json: " + where + " 의 스텝 키 " + kv.Key + " 이 0~" + (steps - 1) + " 밖");
                arr[idx] = J.Int(kv.Value);
            }
            return arr;
        }
    }
}
