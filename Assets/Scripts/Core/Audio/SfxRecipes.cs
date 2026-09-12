using System;
using System.Collections.Generic;
using System.Globalization;

namespace Forge.Core.Audio
{
    /// <summary>효과음 호출 한 건 — 이름 + 원작 인자(불/정수/실수는 A·B · 등급 문자열은 Rarity). <see cref="Key"/> 가 클립 캐시 키다.</summary>
    public struct SfxCall
    {
        public string Name;
        public double A;
        public double B;
        public string Rarity;

        public SfxCall(string name, double a = 0, double b = 0, string rarity = null) { Name = name; A = a; B = b; Rarity = rarity; }

        public string Key
        {
            get
            {
                return Name + ":" + A.ToString("R", CultureInfo.InvariantCulture) + ":" + B.ToString("R", CultureInfo.InvariantCulture) + ":" + (Rarity ?? "");
            }
        }

        public override string ToString() { return Key; }
    }

    /// <summary>
    /// 원작 `sfx.js` 222~424줄의 게임 효과음 24종을 **함수 단위로** 옮긴 것(전부 레이어드: 트랜지언트 + 바디 + 무게 + 테일). 숫자는 정본 줄 그대로 —
    /// 이 값들은 표가 아니라 각 효과음 식의 인자라 `sfx.json` 으로 못 뽑아 이 파일 한 곳에 둔다(ROUTINE T30 절 · 결정 기록). 인자 규약(tier 0~5 ·
    /// i 발 번호 · rarity 문자열 · ageIdx)은 원작 호출부(scene3d.js · scene3d-skillfx.js · ui.js)와 같다.
    /// </summary>
    public static class SfxRecipes
    {
        /// <summary>정본 게임 효과음 이름 24종(원작 정의 순서). 합성 프리미티브(tone·noiseBurst·thump·click·ring·sparkle)는 <see cref="SfxSynth"/>.</summary>
        public static readonly string[] Names =
        {
            "hit", "bossSiren", "anvilHit", "stormRumble", "stormCrackle", "stormStrike", "slashArc", "arrowShot",
            "mawRoar", "mawBite", "healDescend", "auraRise", "voidTear", "voidPierce", "voidSnap", "equipToss", "equipSnap", "equipDrop",
            "craft", "craftReveal", "levelUp", "gacha", "summonCharge", "summonReveal",
        };

        /// <summary>`RARITIES.indexOf` 가 필요한 효과음(summonReveal)이 쓰는 등급 순서 — gamedata.json `RARITIES`.</summary>
        public static bool HighRarity(string rarity) { return rarity == "legendary" || rarity == "ultimate" || rarity == "mythic"; }

        /// <summary>이름·인자로 레시피를 고른다(원작 `SFX[name](...args)`). 모르는 이름은 예외 — 호출부 오타를 조용히 삼키지 않는다.</summary>
        public static void Build(SfxSynth s, SfxCall c, IReadOnlyList<string> rarities)
        {
            switch (c.Name)
            {
                case "hit": Hit(s, c.A != 0); break;
                case "bossSiren": BossSiren(s); break;
                case "anvilHit": AnvilHit(s, c.A != 0); break;
                case "stormRumble": StormRumble(s, c.A); break;
                case "stormCrackle": StormCrackle(s); break;
                case "stormStrike": StormStrike(s, (int)c.A); break;
                case "slashArc": SlashArc(s, (int)c.A, (int)c.B); break;
                case "arrowShot": ArrowShot(s, (int)c.A, (int)c.B); break;
                case "mawRoar": MawRoar(s, (int)c.A); break;
                case "mawBite": MawBite(s, (int)c.A); break;
                case "healDescend": HealDescend(s, (int)c.A); break;
                case "auraRise": AuraRise(s, (int)c.A); break;
                case "voidTear": VoidTear(s, (int)c.A); break;
                case "voidPierce": VoidPierce(s, (int)c.A); break;
                case "voidSnap": VoidSnap(s); break;
                case "equipToss": EquipToss(s); break;
                case "equipSnap": EquipSnap(s); break;
                case "equipDrop": EquipDrop(s); break;
                case "craft": Craft(s); break;
                case "craftReveal": CraftReveal(s, (int)c.A); break;
                case "levelUp": LevelUp(s); break;
                case "gacha": Gacha(s, c.Rarity); break;
                case "summonCharge": SummonCharge(s, c.Rarity); break;
                case "summonReveal": SummonReveal(s, c.Rarity, rarities); break;
                default: throw new ArgumentException("모르는 효과음: " + c.Name);
            }
        }

        /// <summary>미리 구워 두는 기본 변형 — 원작 호출부가 실제로 넘기는 인자 꼴(tier 0~5 · i 0~4 · 등급 6 · 시대 0~9)의 대표.</summary>
        public static List<SfxCall> DefaultCalls(IReadOnlyList<string> rarities)
        {
            var l = new List<SfxCall>();
            l.Add(new SfxCall("hit", 0)); l.Add(new SfxCall("hit", 1));
            l.Add(new SfxCall("bossSiren"));
            l.Add(new SfxCall("anvilHit", 0)); l.Add(new SfxCall("anvilHit", 1));
            l.Add(new SfxCall("stormRumble", 0)); l.Add(new SfxCall("stormRumble", 0.5));
            l.Add(new SfxCall("stormCrackle"));
            for (int i = 0; i <= 4; i++) l.Add(new SfxCall("stormStrike", i));
            for (int i = 0; i <= 4; i++) l.Add(new SfxCall("slashArc", i, 0));
            for (int i = 0; i <= 4; i++) l.Add(new SfxCall("arrowShot", i, 0));
            foreach (string n in new[] { "mawRoar", "mawBite", "healDescend", "auraRise", "voidTear", "voidPierce" }) l.Add(new SfxCall(n, 0));
            l.Add(new SfxCall("voidSnap"));
            l.Add(new SfxCall("equipToss")); l.Add(new SfxCall("equipSnap")); l.Add(new SfxCall("equipDrop"));
            l.Add(new SfxCall("craft"));
            for (int a = 0; a < 10; a++) l.Add(new SfxCall("craftReveal", a));
            l.Add(new SfxCall("levelUp"));
            for (int r = 0; r < rarities.Count; r++)
            {
                l.Add(new SfxCall("gacha", 0, 0, rarities[r]));
                l.Add(new SfxCall("summonCharge", 0, 0, rarities[r]));
                l.Add(new SfxCall("summonReveal", 0, 0, rarities[r]));
            }
            return l;
        }

        // ---- 게임 효과음 (원작 순서 그대로) ----

        public static void Hit(SfxSynth s, bool crit)
        {
            s.Click(crit ? 4200 : 3200, crit ? 0.3 : 0.2);
            s.NoiseBurst(crit ? 0.12 : 0.08, FilterType.Lowpass, crit ? 2600 : 1800, crit ? 700 : 500, 0, crit ? 0.4 : 0.28);
            s.Thump(crit ? 200 : 170, 65, crit ? 0.11 : 0.08, crit ? 0.4 : 0.28, 0, 0.05);
            if (crit)
            {
                s.Ring(2600, 0.16, 0.1, 0.004, 0.25);
                s.Tone(1200, 0.12, Wave.Triangle, 0.16, 0, 0.04, 1800);
            }
        }

        /// <summary>보스 경고 사이렌 — 화면 연출(#boss-warning)과 같은 .42초 박자로 상승/하강 스윕 3회 · 저역 럼블 · 1.55초 서브베이스 임팩트. ⚠ 타이밍 상수를 바꾸지 말 것.</summary>
        public static void BossSiren(SfxSynth s)
        {
            for (int i = 0; i < 3; i++)
            {
                double d = i * 0.42;
                s.Tone(330, 0.2, Wave.Sawtooth, 0.15, d, 0, 880, 0.012, 0.15);
                s.Tone(880, 0.2, Wave.Sawtooth, 0.15, d + 0.2, 0, 330, 0.012, 0.15);
                s.Tone(110, 0.34, Wave.Square, 0.05, d);
            }
            s.NoiseBurst(1.5, FilterType.Lowpass, 130, 0, 0, 0.11);
            s.Thump(72, 34, 0.5, 0.42, 1.55);
            s.NoiseBurst(0.42, FilterType.Lowpass, 700, 0, 0, 0.32, 1.55, 0.3);
        }

        /// <summary>모루 타격 — 클릭 + 금속 클랭 2겹 + 스파크 스윕 + 바닥 무게. 마지막 타격(strong)은 낮고 길게.</summary>
        public static void AnvilHit(SfxSynth s, bool strong)
        {
            s.Click(6000, strong ? 0.3 : 0.22);
            s.Tone(strong ? 1650 : 1950, strong ? 0.2 : 0.12, Wave.Square, strong ? 0.2 : 0.14, 0, 0.03, strong ? 620 : 900);
            s.Tone(strong ? 520 : 660, strong ? 0.26 : 0.16, Wave.Triangle, strong ? 0.16 : 0.1, 0, 0.03, strong ? 300 : 440, 0.012, 0.2);
            s.NoiseBurst(strong ? 0.11 : 0.07, FilterType.Lowpass, 6200, 2400, 0, strong ? 0.22 : 0.14);
            s.Thump(150, 55, strong ? 0.13 : 0.09, strong ? 0.3 : 0.2, 0, 0.04);
        }

        /// <summary>먹구름 낙뢰 3박자 ① 구름이 모이는 저역 우르릉. dur 0 = 원작 기본 0.35.</summary>
        public static void StormRumble(SfxSynth s, double dur)
        {
            double d = dur != 0 ? dur : 0.35;
            s.NoiseBurst(Math.Max(0.2, d), FilterType.Lowpass, 260, 90, 0, 0.1, 0, 0.3);
            s.Tone(46, Math.Max(0.25, d), Wave.Sine, 0.16, 0, 0.05, 0, 0.012, 0.35);
        }

        /// <summary>② 충전 지지직.</summary>
        public static void StormCrackle(SfxSynth s)
        {
            s.NoiseBurst(0.07, FilterType.Highpass, 4200, 0, 0, 0.1);
            s.Tone(2400, 0.05, Wave.Square, 0.05, 0, 0.08, 3600);
        }

        /// <summary>③ 낙뢰 — 트랜지언트가 전부. i = 몇 번째(0부터) · 뒤로 갈수록 살짝 낮고 굵게.</summary>
        public static void StormStrike(SfxSynth s, int i)
        {
            double k = 1 - Math.Min(4, i) * 0.06;
            s.Click(7000 * k, 0.3);
            s.NoiseBurst(0.16, FilterType.Lowpass, 7000 * k, 900, 0, 0.34, 0, 0.22);
            s.Thump(180 * k, 44, 0.2, 0.34, 0, 0.07);
            s.Tone(1500 * k, 0.1, Wave.Square, 0.1, 0, 0.06, 400);
        }

        /// <summary>참격 — 스윕 노이즈(가르기) + 짧은 금속 링(날). 벨 때마다 음이 올라간다.</summary>
        public static void SlashArc(SfxSynth s, int i, int tier)
        {
            double k = 1 + Math.Min(4, i) * 0.09;
            s.Click(6200 * k, 0.2);
            s.NoiseBurst(0.1, FilterType.Bandpass, 3200 * k, 1100, 1.4, 0.2);
            s.Ring(2600 * k, 0.11, 0.07 + tier * 0.008, 0.006, 0.18);
        }

        /// <summary>화살 발사 — 시위 퍽 + 고역 스윕. 발마다 다른 고정 지터(난수 없이 균등).</summary>
        public static void ArrowShot(SfxSynth s, int i, int tier)
        {
            double k = 1 + ((i * 37) % 11 - 5) * 0.02;
            s.Thump(300 * k, 120, 0.05, 0.16, 0, 0.04);
            s.NoiseBurst(0.07, FilterType.Highpass, 2600 * k, 0, 0, 0.13);
            s.Tone(1800 * k, 0.06, Wave.Triangle, 0.06 + tier * 0.006, 0, 0.05, 900);
        }

        /// <summary>거대 아가리 포효 — 저역이 전부.</summary>
        public static void MawRoar(SfxSynth s, int tier)
        {
            double g = 0.2 + tier * 0.02;
            s.Tone(120, 0.42, Wave.Sawtooth, g, 0, 0.04, 52, 0.012, 0.4);
            s.Tone(180, 0.36, Wave.Triangle, g * 0.6, 0, 0.05, 78, 0.012, 0.35);
            s.NoiseBurst(0.4, FilterType.Lowpass, 700, 180, 0, 0.16, 0, 0.3);
        }

        /// <summary>무는 순간 — 이빨 딱 + 살점 뜯는 저역.</summary>
        public static void MawBite(SfxSynth s, int tier)
        {
            s.Click(5200, 0.3);
            s.NoiseBurst(0.11, FilterType.Lowpass, 2600, 500, 0, 0.3);
            s.Thump(150, 40, 0.22, 0.36 + tier * 0.01, 0, 0.05);
        }

        /// <summary>회복 강림 — 내려오는 소리.</summary>
        public static void HealDescend(SfxSynth s, int tier)
        {
            double g = 0.1 + tier * 0.008;
            s.Tone(1400, 0.34, Wave.Sine, g, 0, 0.02, 620, 0.012, 0.4);
            s.Tone(1050, 0.4, Wave.Triangle, g * 0.8, 0, 0.02, 520, 0.012, 0.45);
            s.Sparkle(1500, 4, 0.05, 0.1);
        }

        /// <summary>버프 서클 — 올라가는 소리.</summary>
        public static void AuraRise(SfxSynth s, int tier)
        {
            double g = 0.1 + tier * 0.008;
            s.Tone(320, 0.42, Wave.Triangle, g, 0, 0.03, 780, 0.012, 0.35);
            s.Tone(480, 0.36, Wave.Sine, g * 0.7, 0.05, 0.03, 1170, 0.012, 0.4);
            s.NoiseBurst(0.3, FilterType.Bandpass, 500, 1800, 1.2, 0.06);
        }

        /// <summary>공허의 창 개열 — 공기가 빨려 들어가는 역방향 스웰 · 저역 디튠 2겹(3.5Hz 맥놀이).</summary>
        public static void VoidTear(SfxSynth s, int tier)
        {
            double g = 0.1 + tier * 0.008;
            s.NoiseBurst(0.3, FilterType.Bandpass, 3600, 420, 2.2, 0.13, 0, 0.4);
            s.Tone(96, 0.34, Wave.Sawtooth, g, 0, 0.03, 62, 0.012, 0.45);
            s.Tone(99.5, 0.34, Wave.Sawtooth, g * 0.7, 0, 0.03, 64, 0.012, 0.45);
        }

        /// <summary>관통 — 클릭 → 밴드 노이즈 → 서브 드롭 · 링.</summary>
        public static void VoidPierce(SfxSynth s, int tier)
        {
            int t = tier;
            s.Click(5200, 0.26);
            s.NoiseBurst(0.13, FilterType.Bandpass, 2600, 380, 1.6, 0.3, 0, 0.25);
            s.Thump(150, 34, 0.3, 0.32 + t * 0.01, 0, 0.05);
            s.Ring(1180, 0.16, 0.07 + t * 0.006, 0.01, 0.3);
        }

        /// <summary>폐쇄 — 균열이 탁 다물린다.</summary>
        public static void VoidSnap(SfxSynth s)
        {
            s.Click(8200, 0.2);
            s.NoiseBurst(0.06, FilterType.Highpass, 3000, 0, 0, 0.16);
            s.Thump(240, 70, 0.09, 0.2, 0, 0.04);
        }

        /// <summary>장비 교체 3박자 ① 던짐(0ms) — 중역 노이즈 스윕.</summary>
        public static void EquipToss(SfxSynth s)
        {
            s.NoiseBurst(0.15, FilterType.Bandpass, 1700, 560, 1.1, 0.11);
        }

        /// <summary>② 딸깍(130ms) — 고역 트랜지언트 + 짧은 금속 링.</summary>
        public static void EquipSnap(SfxSynth s)
        {
            s.Click(5400, 0.26);
            s.Thump(440, 190, 0.05, 0.2, 0, 0.03);
            s.Ring(3050, 0.09, 0.07, 0.008, 0.14);
        }

        /// <summary>③ 착지(558ms) — 저역 텀프.</summary>
        public static void EquipDrop(SfxSynth s)
        {
            s.Thump(132, 50, 0.14, 0.24, 0, 0.06);
            s.NoiseBurst(0.08, FilterType.Lowpass, 950, 300, 0, 0.13);
        }

        public static void Craft(SfxSynth s)
        {
            s.Click(2600, 0.14);
            s.Thump(260, 180, 0.06, 0.16, 0, 0.05);
            s.Tone(660, 0.09, Wave.Square, 0.16, 0, 0.03);
            s.Tone(880, 0.12, Wave.Square, 0.16, 0.06, 0.03, 0, 0.012, 0.12);
        }

        /// <summary>제작 결과 리빌 — 시대가 높을수록 음이 높고, 중세(idx 4) 이후는 5도 + 반짝임.</summary>
        public static void CraftReveal(SfxSynth s, int ageIdx)
        {
            int idx = Math.Max(0, ageIdx);
            double b = 560 * Math.Pow(1.07, idx);
            s.Click(3400, 0.12);
            s.Tone(b, 0.13, Wave.Triangle, 0.18, 0, 0.02, b * 1.5, 0.012, 0.2);
            if (idx >= 4)
            {
                s.Tone(b * 1.5, 0.2, Wave.Sine, 0.1, 0.05, 0, b * 2, 0.012, 0.3);
                s.Sparkle(b * 2, 3, 0.07, 0.08);
            }
        }

        public static void LevelUp(SfxSynth s)
        {
            double[] notes = { 523.25, 659.25, 783.99, 1046.5 };
            for (int i = 0; i < notes.Length; i++)
            {
                double f = notes[i];
                bool last = i == 3;
                s.Tone(f, last ? 0.3 : 0.16, Wave.Triangle, 0.26, i * 0.07, 0.01, 0, 0.012, last ? 0.35 : 0.15);
                s.Tone(f * 2, last ? 0.24 : 0.1, Wave.Sine, 0.08, i * 0.07 + 0.01, 0, 0, 0.012, 0.3);
            }
            s.Sparkle(2093, 4, 0.06, 0.24);
        }

        public static void Gacha(SfxSynth s, string rarity)
        {
            bool hi = HighRarity(rarity);
            s.NoiseBurst(0.22, FilterType.Bandpass, 600, hi ? 3400 : 2200, 1.6, 0.14);
            s.Tone(440, 0.18, Wave.Sine, 0.22, 0, 0.02, hi ? 1320 : 880, 0.012, 0.2);
            s.Tone(660, 0.16, Wave.Triangle, 0.1, 0.08, 0.03, 0, 0.012, 0.25);
            if (hi) s.Sparkle(1760, 4, 0.07, 0.14);
        }

        /// <summary>소환 결과 팝업 '빛 모임' — 상승 스윕 + 노이즈 라이저 + 서브 스웰(UI.SR_CHARGE_MS 240ms 정점의 절반값).</summary>
        public static void SummonCharge(SfxSynth s, string rarity)
        {
            bool hi = HighRarity(rarity);
            s.Tone(220, hi ? 0.21 : 0.15, Wave.Triangle, 0.16, 0, 0, hi ? 1760 : 880, 0.012, 0.2);
            s.NoiseBurst(hi ? 0.2 : 0.13, FilterType.Bandpass, 500, hi ? 5200 : 3000, 2, 0.09);
            s.Thump(55, 110, hi ? 0.2 : 0.14, 0.14);
        }

        /// <summary>아이콘 하나가 팝하는 순간 — 등급이 올라갈수록 음이 높아지고, 전설(idx 3) 이상은 5도 + 반짝임.</summary>
        public static void SummonReveal(SfxSynth s, string rarity, IReadOnlyList<string> rarities)
        {
            int idx = 0;
            if (rarities != null) for (int i = 0; i < rarities.Count; i++) if (rarities[i] == rarity) { idx = i; break; }
            idx = Math.Max(0, idx);
            double b = 520 * Math.Pow(1.09, idx);
            s.Click(3600, 0.1);
            s.Tone(b, 0.12, Wave.Sine, 0.16, 0, 0.03, b * 1.5, 0.012, 0.15);
            if (idx >= 3)
            {
                s.Tone(b * 1.5, 0.22, Wave.Triangle, 0.11, 0.04, 0, b * 2, 0.012, 0.25);
                s.Sparkle(b * 2.2, 3, 0.07, 0.06);
            }
        }
    }
}
