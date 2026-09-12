using System;
using System.Collections.Generic;
using System.IO;

namespace Forge.Core.Data
{
    /// <summary>
    /// 게임 데이터 뿌리(T3). `Assets/StreamingAssets/data/*.json`(T2 가 정본 `web/js/balance-data.js`·`gamedata.js`·`mobs-*.js` 에서 뽑는다)을
    /// 문자열로 받아 강타입 표로 세운다. **어느 수치도 코드 상수로 두지 않는다** — 표에 없는 것은 여기에도 없다.
    /// 파일 읽기는 호출자 몫(안드로이드의 StreamingAssets 는 File 로 못 읽는다) — Game 쪽이 문자열을 넘긴다. 데스크톱·테스트는 <see cref="LoadDirectory"/>.
    /// </summary>
    public sealed class GameData
    {
        public const string BalanceFile = "balance.json";
        public const string GameDataFile = "gamedata.json";
        public const string PetsFile = "mobs-pets.json";
        public const string MountsFile = "mobs-mounts.json";
        public const string EnemiesFile = "mobs-enemies.json";
        public const string SkillFxFile = "mobs-skillfx.json";
        public const string PropsFile = "mobs-props.json";
        public static readonly string[] Files = { BalanceFile, GameDataFile, PetsFile, MountsFile, EnemiesFile, SkillFxFile, PropsFile };

        public BalanceTable Balance;
        public GameDefs Defs;
        public MobTable Pets, Mounts, Enemies, SkillFx;
        public PropsTable Props;

        public static GameData Load(string balanceJson, string gamedataJson, string petsJson, string mountsJson, string enemiesJson, string skillfxJson, string propsJson)
        {
            var g = new GameData();
            g.Balance = BalanceTable.From(MiniJson.ParseObject(balanceJson));
            g.Defs = GameDefs.From(MiniJson.ParseObject(gamedataJson));
            g.Pets = MobTable.From(MiniJson.ParseObject(petsJson));
            g.Mounts = MobTable.From(MiniJson.ParseObject(mountsJson));
            g.Enemies = MobTable.From(MiniJson.ParseObject(enemiesJson));
            g.SkillFx = MobTable.From(MiniJson.ParseObject(skillfxJson));
            g.Props = PropsTable.From(MiniJson.ParseObject(propsJson));
            return g;
        }

        /// <summary>파일 이름(<see cref="Files"/>) → 내용 문자열을 주는 함수로 일곱 파일을 읽는다.</summary>
        public static GameData Load(Func<string, string> read)
        {
            if (read == null) throw new ArgumentNullException("read");
            return Load(read(BalanceFile), read(GameDataFile), read(PetsFile), read(MountsFile), read(EnemiesFile), read(SkillFxFile), read(PropsFile));
        }

        /// <summary>폴더에서 직접(에디터·데스크톱·dotnet 테스트).</summary>
        public static GameData LoadDirectory(string dir)
        {
            return Load(name => File.ReadAllText(Path.Combine(dir, name)));
        }
    }

    // ===================== balance.json =====================

    /// <summary>`balance-data.js` 표 열 개 — 대장간 · 펫 · 스킬 · 탈것으로 나눠 든다.</summary>
    public sealed class BalanceTable
    {
        public ForgeTable Forge;
        public PetTable Pets;
        public SkillTable Skills;
        public MountTable Mounts;
        public JsonObject Raw;

        public static BalanceTable From(JsonObject o)
        {
            return new BalanceTable
            {
                Forge = ForgeTable.From(o),
                Pets = PetTable.From(o),
                Skills = SkillTable.From(o),
                Mounts = MountTable.From(o),
                Raw = o
            };
        }
    }

    /// <summary>대장간: `forgeProbabilities`(레벨 → 시대 → %) · `forgeUpgrades`(레벨 → 비용·초).</summary>
    public sealed class ForgeTable
    {
        /// <summary>레벨("1"~"35") → 시대 → 확률(%). 행 합은 99.95~100.05 로 어긋난 것이 있다(추출원 반올림 · 원작 `weightedPick` 이 합으로 정규화 — T2 완료 기록 ⓒ).</summary>
        public OrderedMap<OrderedMap<double>> Probabilities;
        /// <summary>레벨("2"~"35") → 그 레벨로 올리는 비용·시간.</summary>
        public OrderedMap<ForgeUpgrade> Upgrades;

        public int MaxLevel { get { return Probabilities.Count; } }
        public OrderedMap<double> ProbabilitiesAt(int level) { return Probabilities[Key(level)]; }
        public bool HasUpgrade(int toLevel) { return Upgrades.Has(Key(toLevel)); }
        public ForgeUpgrade Upgrade(int toLevel) { return Upgrades[Key(toLevel)]; }
        internal static string Key(int n) { return n.ToString(System.Globalization.CultureInfo.InvariantCulture); }

        public static ForgeTable From(JsonObject o)
        {
            return new ForgeTable
            {
                Probabilities = J.Map(J.Require(o, "forgeProbabilities"), J.NumMap),
                Upgrades = J.Map(J.Require(o, "forgeUpgrades"), v => { var u = J.Obj(v); return new ForgeUpgrade { Cost = J.Num(u["cost"]), Time = J.Num(u["time"]) }; })
            };
        }
    }

    public sealed class ForgeUpgrade
    {
        public double Cost;
        /// <summary>초.</summary>
        public double Time;
    }

    /// <summary>펫: `eggDropRates`(스테이지 "c-s" → 등급 → 비율) · `baseHatchingTimes`(등급 → 분) · `petStats`(등급 → 종 목록).</summary>
    public sealed class PetTable
    {
        public OrderedMap<OrderedMap<double>> EggDropRates;
        public OrderedMap<double> BaseHatchingTimes;
        public OrderedMap<List<PetStat>> Stats;

        public OrderedMap<double> EggDropAt(int chapter, int stage) { return EggDropRates[ForgeTable.Key(chapter) + "-" + ForgeTable.Key(stage)]; }
        public bool HasEggDrop(int chapter, int stage) { return EggDropRates.Has(ForgeTable.Key(chapter) + "-" + ForgeTable.Key(stage)); }

        /// <summary>종 이름으로 스탯 행(등급 포함). 없으면 null.</summary>
        public PetStat Find(string name)
        {
            foreach (var kv in Stats)
                for (int i = 0; i < kv.Value.Count; i++) if (kv.Value[i].Name == name) return kv.Value[i];
            return null;
        }

        public int SpeciesCount { get { int n = 0; foreach (var kv in Stats) n += kv.Value.Count; return n; } }

        public static PetTable From(JsonObject o)
        {
            var t = new PetTable
            {
                EggDropRates = J.Map(J.Require(o, "eggDropRates"), J.NumMap),
                BaseHatchingTimes = J.NumMap(J.Require(o, "baseHatchingTimes")),
                Stats = new OrderedMap<List<PetStat>>()
            };
            foreach (var kv in J.Obj(J.Require(o, "petStats")))
            {
                string rarity = kv.Key;
                t.Stats.Add(rarity, J.List(kv.Value, v => { var s = J.Obj(v); return new PetStat { Rarity = rarity, Name = J.Str(s["name"]), Damage = J.Num(s["damage"]), Health = J.Num(s["health"]) }; }));
            }
            return t;
        }
    }

    public sealed class PetStat
    {
        public string Rarity;
        public string Name;
        public double Damage;
        public double Health;
    }

    /// <summary>스킬: `skillRatesData`(소환 레벨 "1"~"100" → 등급 6칸 가중치 · 순서 = gamedata RARITIES).</summary>
    public sealed class SkillTable
    {
        public OrderedMap<double[]> Rates;
        public int MaxLevel { get { return Rates.Count; } }
        public double[] RatesAt(int summonLevel) { return Rates[ForgeTable.Key(summonLevel)]; }

        public static SkillTable From(JsonObject o)
        {
            return new SkillTable { Rates = J.Map(J.Require(o, "skillRatesData"), J.NumArr) };
        }
    }

    /// <summary>탈것: `mountSummonRates`(레벨 → needed·등급 비율) · `mountBoosts` · `mountNames`(등급 → 종 이름) · `WINDERS_PER_SUMMON`.</summary>
    public sealed class MountTable
    {
        public OrderedMap<MountSummonRow> SummonRates;
        public OrderedMap<double> Boosts;
        public OrderedMap<string[]> Names;
        public double WindersPerSummon;

        public MountSummonRow SummonAt(int level) { return SummonRates[ForgeTable.Key(level)]; }
        public int MaxLevel { get { return SummonRates.Count; } }

        public static MountTable From(JsonObject o)
        {
            return new MountTable
            {
                SummonRates = J.Map(J.Require(o, "mountSummonRates"), MountSummonRow.From),
                Boosts = J.NumMap(J.Require(o, "mountBoosts")),
                Names = J.StrArrMap(J.Require(o, "mountNames")),
                WindersPerSummon = J.Num(J.Require(o, "WINDERS_PER_SUMMON"))
            };
        }
    }

    /// <summary>`{needed, common, rare, …}` — needed 는 수 또는 "MAX"(마지막 레벨). Rates 는 needed 를 뺀 등급 → 비율(순서 보존).</summary>
    public sealed class MountSummonRow
    {
        public double Needed;
        public bool NeededIsMax;
        public OrderedMap<double> Rates = new OrderedMap<double>();

        public static MountSummonRow From(object v)
        {
            var o = J.Obj(v);
            var r = new MountSummonRow();
            object needed = o["needed"];
            if (needed is string) { r.NeededIsMax = string.Equals((string)needed, "MAX", StringComparison.Ordinal); r.Needed = 0; }
            else r.Needed = J.Num(needed);
            foreach (var kv in o) if (kv.Key != "needed") r.Rates.Add(kv.Key, J.Num(kv.Value));
            return r;
        }
    }

    // ===================== gamedata.json =====================

    /// <summary>`gamedata.js` 최상위 상수 — 자주 쓰는 것은 강타입 · 나머지는 <see cref="Raw"/>(원문 이름 그대로 키).</summary>
    public sealed class GameDefs
    {
        public string[] Ages;
        public OrderedMap<string> AgeKr, AgeIcon;
        public OrderedMap<int> AgeColors;

        public string[] Rarities;
        public OrderedMap<string> RarityKr, RarityCss;
        public OrderedMap<int> RarityHex;
        public OrderedMap<double> RarityMult;

        public string[] Slots;
        public OrderedMap<string> SlotKr, SlotMain;
        /// <summary>시대 → 부위(helmet/armor) → 이름 목록.</summary>
        public OrderedMap<OrderedMap<string[]>> ItemNames;
        public OrderedMap<string[]> HelmetStyles, ArmorStyles, AgeWeapons;
        /// <summary>시대 → 장신구 부위 → 이름 목록.</summary>
        public OrderedMap<OrderedMap<string[]>> AccNamesByAge;
        public OrderedMap<string[]> AccNames;
        public List<NameSubstance> NameSubstances;
        public OrderedMap<WeaponType> WeaponTypes;

        public double SubstatMin;
        public List<SubstatDef> Substats;

        public List<SkillDef> SkillDefs;
        public OrderedMap<double> SkillBaseDmg, SkillBaseHeal, SkillBaseBuffAtk;
        public OrderedMap<AtkHp> SkillBasePassive;
        public OrderedMap<string> SkillIcons;

        public OrderedMap<string> PetIcons, PetKr;
        public OrderedMap<int> PetColors;
        public OrderedMap<OrderedMap<double>> PetMotion;
        public OrderedMap<string> MountKr, MountIcons;

        public List<Unlock> Unlocks;
        public List<ChapterTheme> ChapterThemes;

        public JsonObject Raw;

        public SkillDef Skill(string id)
        {
            for (int i = 0; i < SkillDefs.Count; i++) if (SkillDefs[i].Id == id) return SkillDefs[i];
            return null;
        }

        public SubstatDef Substat(string key)
        {
            for (int i = 0; i < Substats.Count; i++) if (Substats[i].Key == key) return Substats[i];
            return null;
        }

        public static GameDefs From(JsonObject o)
        {
            var d = new GameDefs { Raw = o };
            d.Ages = J.StrArr(J.Require(o, "AGES"));
            d.AgeKr = J.StrMap(o["AGE_KR"]);
            d.AgeIcon = J.StrMap(o["AGE_ICON"]);
            d.AgeColors = J.IntMap(o["AGE_COLORS"]);

            d.Rarities = J.StrArr(J.Require(o, "RARITIES"));
            d.RarityKr = J.StrMap(o["RARITY_KR"]);
            d.RarityCss = J.StrMap(o["RARITY_CSS"]);
            d.RarityHex = J.IntMap(o["RARITY_HEX"]);
            d.RarityMult = J.NumMap(o["RARITY_MULT"]);

            d.Slots = J.StrArr(J.Require(o, "SLOTS"));
            d.SlotKr = J.StrMap(o["SLOT_KR"]);
            d.SlotMain = J.StrMap(o["SLOT_MAIN"]);
            d.ItemNames = J.Map(o["ITEM_NAMES"], J.StrArrMap);
            d.HelmetStyles = J.StrArrMap(o["HELMET_STYLES"]);
            d.ArmorStyles = J.StrArrMap(o["ARMOR_STYLES"]);
            d.AgeWeapons = J.StrArrMap(o["AGE_WEAPONS"]);
            d.AccNamesByAge = J.Map(o["ACC_NAMES_BY_AGE"], J.StrArrMap);
            d.AccNames = J.StrArrMap(o["ACC_NAMES"]);
            d.NameSubstances = J.List(o["NAME_SUBSTANCES"], v => { var a = J.Arr(v); return new NameSubstance { Key = J.Str(a[0]), Words = J.StrArr(a[1]) }; });
            d.WeaponTypes = J.Map(o["WEAPON_TYPES"], v =>
            {
                var w = J.Obj(v);
                return new WeaponType
                {
                    Kr = J.Str(w["kr"]), Kind = J.Str(w["kind"]), Impact = J.Num(w["impact"]), Motion = J.Str(w["motion"]),
                    RestX = J.Num(w["restX"]), Shape = J.Str(w["shape"]), Mat = J.Str(w["mat"]), Raw = w
                };
            });

            d.SubstatMin = J.Num(J.Require(o, "SUBSTAT_MIN"));
            d.Substats = J.List(J.Require(o, "SUBSTATS"), v => { var a = J.Arr(v); return new SubstatDef { Key = J.Str(a[0]), Label = J.Str(a[1]), Max = J.Num(a[2]) }; });

            d.SkillDefs = J.List(J.Require(o, "SKILL_DEFS"), v =>
            {
                var s = J.Obj(v);
                return new SkillDef
                {
                    Id = J.Str(s["id"]), Name = J.Str(s["name"]), Rarity = J.Str(s["rarity"]), Type = J.Str(s["type"]),
                    Mult = J.Num(s["mult"]), Cd = J.Num(s["cd"]), Fx = J.Str(s["fx"]), Color = J.Str(s["color"]),
                    ImpactAt = J.NumOrNull(s["impactAt"]), Dur = J.NumOrNull(s["dur"]), Raw = s
                };
            });
            d.SkillBaseDmg = J.NumMap(o["SKILL_BASE_DMG"]);
            d.SkillBaseHeal = J.NumMap(o["SKILL_BASE_HEAL"]);
            d.SkillBaseBuffAtk = J.NumMap(o["SKILL_BASE_BUFF_ATK"]);
            d.SkillBasePassive = J.Map(o["SKILL_BASE_PASSIVE"], v => { var p = J.Obj(v); return new AtkHp { Atk = J.Num(p["atk"]), Hp = J.Num(p["hp"]) }; });
            d.SkillIcons = J.StrMap(o["SKILL_ICONS"]);

            d.PetIcons = J.StrMap(o["PET_ICONS"]);
            d.PetKr = J.StrMap(o["PET_KR"]);
            d.PetColors = J.IntMap(o["PET_COLORS"]);
            d.PetMotion = J.Map(o["PET_MOTION"], J.NumMap);
            d.MountKr = J.StrMap(o["MOUNT_KR"]);
            d.MountIcons = J.StrMap(o["MOUNT_ICONS"]);

            d.Unlocks = J.List(o["UNLOCKS"], v => { var u = J.Obj(v); return new Unlock { Stage = J.Str(u["stage"]), Key = J.Str(u["key"]), Name = J.Str(u["name"]) }; });
            d.ChapterThemes = J.List(J.Require(o, "CHAPTER_THEMES"), v =>
            {
                var t = J.Obj(v);
                return new ChapterTheme { Sky = J.Int(t["sky"]), Fog = J.Int(t["fog"]), Ground = J.Int(t["ground"]), Biome = J.Str(t["biome"]), Celestial = J.Str(t["celestial"]), Raw = t };
            });
            return d;
        }
    }

    public sealed class NameSubstance { public string Key; public string[] Words; }

    /// <summary>`WEAPON_TYPES` 한 종: kr · kind(melee/ranged…) · impact(타격 시각 비) · motion · restX · shape · mat.</summary>
    public sealed class WeaponType
    {
        public string Kr, Kind, Motion, Shape, Mat;
        public double Impact, RestX;
        public JsonObject Raw;
    }

    /// <summary>`SUBSTATS` 한 줄 `[key, label, max]` — 값 범위는 SUBSTAT_MIN~max(등급은 개수만 정한다).</summary>
    public sealed class SubstatDef { public string Key, Label; public double Max; }

    /// <summary>`SKILL_DEFS` 한 종. impactAt(타격 시각)·dur(버프 지속)은 종류에 따라 없다.</summary>
    public sealed class SkillDef
    {
        public string Id, Name, Rarity, Type, Fx, Color;
        public double Mult, Cd;
        public double? ImpactAt, Dur;
        public JsonObject Raw;
    }

    public sealed class AtkHp { public double Atk, Hp; }
    public sealed class Unlock { public string Stage, Key, Name; }

    /// <summary>`CHAPTER_THEMES` 한 줄 — 색 0xRRGGBB · biome · celestial(밤 테마의 달 등 · 없으면 null).</summary>
    public sealed class ChapterTheme
    {
        public int Sky, Fog, Ground;
        public string Biome, Celestial;
        public JsonObject Raw;
    }

    // ===================== mobs-props.json =====================

    /// <summary>
    /// 소품은 정본에서 생성 함수(`Props.pine(s,o)` …)라 표가 아니다 — T2 는 생성기 이름(`kinds`)과 xorshift32 시드 표본(`samples`)을 낸다(결정 6).
    /// T9 가 생성기를 C# 으로 옮기고 같은 시드로 이 표본과 대조한다.
    /// </summary>
    public sealed class PropsTable
    {
        public string[] Kinds;
        public uint Seed;
        public string Rng;
        /// <summary>호출식("pine(1,{})") → 표본.</summary>
        public OrderedMap<PropSample> Samples;
        public JsonObject Raw;

        public static PropsTable From(JsonObject o)
        {
            var meta = J.Obj(o["meta"]);
            return new PropsTable
            {
                Kinds = J.StrArr(J.Require(o, "kinds")),
                Seed = meta != null ? J.UInt(meta["seed"]) : 0u,
                Rng = meta != null ? J.Str(meta["rng"]) : null,
                Samples = J.Map(J.Require(o, "samples"), v => PropSample.From(J.Obj(v))),
                Raw = o
            };
        }
    }

    /// <summary>표본 하나: kind · seed · u(칸 크기) · parts(재질 m · 칸 수 n · 칸 목록 v = [x,y,z] 또는 [x,y,z,c]).</summary>
    public sealed class PropSample
    {
        public string Kind;
        public uint Seed;
        public double U;
        public double? Sway;
        public List<PropPart> Parts;
        public JsonObject Raw;

        public static PropSample From(JsonObject o)
        {
            return new PropSample
            {
                Kind = J.Str(o["kind"]), Seed = J.UInt(o["seed"]), U = J.Num(o["u"]), Sway = J.NumOrNull(o["sway"]),
                Parts = J.List(o["parts"], v => { var p = J.Obj(v); return new PropPart { M = J.Str(p["m"]), N = J.Int(p["n"]), V = J.List(p["v"], J.IntArr) }; }),
                Raw = o
            };
        }
    }

    public sealed class PropPart
    {
        public string M;
        public int N;
        /// <summary>칸 목록 — 원소는 [x,y,z] 또는 [x,y,z,c](c 없으면 재질 색).</summary>
        public List<int[]> V;
    }
}
