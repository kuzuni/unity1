using System;
using System.Collections.Generic;
using Forge.Core.Data;
using Forge.Core.Pets;

namespace Forge.Core.Forging
{
    /// <summary>
    /// 대장간 엔진(T14) — 원작 `web/js/forge.js` 의 뽑기·업그레이드·젬 스킵·자동 제련 판정을 순수 C# 으로 옮긴 것.
    /// 표는 <see cref="GameData"/>(balance.json · gamedata.json) · 상태는 <see cref="ForgeState"/>+<see cref="Wallet"/> · 난수는 <see cref="Rng"/>(시드 고정 대조) ·
    /// 시각은 `now()`(ms · 원작 `U.now()`) · 기술트리 배율은 <see cref="IForgeMods"/>. 화면·소리·저장은 이벤트로 알린다 — 여기서는 규칙만 계산한다.
    /// 장착·판매·능력치는 T15(`Core/Gear`).
    /// </summary>
    public sealed class ForgeEngine
    {
        readonly GameData _data;
        readonly ForgeState _s;
        readonly Wallet _w;
        readonly Rng _rng;
        readonly Func<double> _now;
        readonly IForgeMods _mods;

        /// <summary>반복 퀘스트 카운터(원작 `Quests.bump(key, n)`): craft · coinSpend · upgradeStart · gearUpgrade.</summary>
        public event Action<string, double> QuestBump;
        /// <summary>업그레이드가 끝나 레벨이 올랐다(원작 `tickUpgrade` 의 SFX·토스트·시트 갱신 자리).</summary>
        public event Action<int> LevelReached;
        /// <summary>원작이 `saveGame()` 을 부르던 자리(업그레이드 시작 · 완료).</summary>
        public event Action SaveRequested;
        /// <summary>제작이 1개 이상 일어났다(원작 `SFX.craft()`).</summary>
        public event Action Crafted;

        public ForgeEngine(GameData data, ForgeState state, Wallet wallet, Rng rng, Func<double> now, IForgeMods mods = null)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (state == null) throw new ArgumentNullException("state");
            if (wallet == null) throw new ArgumentNullException("wallet");
            if (rng == null) throw new ArgumentNullException("rng");
            if (now == null) throw new ArgumentNullException("now");
            _data = data; _s = state; _w = wallet; _rng = rng; _now = now; _mods = mods ?? ForgeMods.None;
        }

        public ForgeState State { get { return _s; } }
        public Wallet Wallet { get { return _w; } }
        ForgeTable Table { get { return _data.Balance.Forge; } }
        GameDefs Defs { get { return _data.Defs; } }

        // ===== 시대별 뽑기 레벨(원작 «원본 포지마스터 방식») =====

        /// <summary>레벨 캡 = 100 + 기술트리 «장비 레벨업» 보너스.</summary>
        public double MaxItemLevel() { return ForgeRules.RollBaseCap + _mods.GearMaxLevelBonus; }

        /// <summary>구세이브 보정 + 신규 시대 초기화 — 수가 아니거나 유한하지 않거나 1 미만이면 1.</summary>
        public void EnsureRollLevels()
        {
            if (_s.RollLevel == null) _s.RollLevel = new Dictionary<string, double>();
            foreach (string age in Defs.Ages)
            {
                double v;
                if (!_s.RollLevel.TryGetValue(age, out v) || double.IsNaN(v) || double.IsInfinity(v) || v < 1) _s.RollLevel[age] = 1;
            }
        }

        /// <summary>그 시대의 현재 뽑기 레벨 — [1, 캡] 으로 잘라서(기술트리 캡이 내려가는 경우 대비).</summary>
        public double RollLevelOf(string age)
        {
            EnsureRollLevels();
            return Clamp(_s.RollLevel[age], 1, MaxItemLevel());
        }

        /// <summary>그 시대 장비를 1개 뽑은 뒤의 랜덤워크: +1(70%) / 동일(20%) / −1(10%) · [1, 캡].</summary>
        public double AdvanceRollLevel(string age)
        {
            EnsureRollLevels();
            double roll = _rng.Random() * 100;
            double delta = roll < ForgeRules.RollUpPct ? 1
                : roll < ForgeRules.RollUpPct + ForgeRules.RollSamePct ? 0
                : -1;
            _s.RollLevel[age] = Clamp(_s.RollLevel[age] + delta, 1, MaxItemLevel());
            return _s.RollLevel[age];
        }

        /// <summary>라인 승천 시 — 모든 시대의 뽑기 레벨을 1 로.</summary>
        public void ResetRollLevels()
        {
            _s.RollLevel = new Dictionary<string, double>();
            EnsureRollLevels();
        }

        // ===== 확률 =====

        /// <summary>등급 가중치(대장간 레벨에 따라 고등급 상승).</summary>
        public OrderedMap<double> RarityWeights(int fl) { return ForgeRules.RarityWeights(fl); }

        /// <summary>시대별 확률표 행(지정 레벨 · 표에 없으면 1레벨 행).</summary>
        public OrderedMap<double> AgeProbsAt(int level)
        {
            string key = ForgeTable.Key(level);
            return Table.Probabilities.Has(key) ? Table.Probabilities[key] : Table.Probabilities[ForgeTable.Key(1)];
        }

        /// <summary>그 시대에 나올 수 있는 무기 id 목록(미정의 시대는 중세로 폴백) — 원작 `weaponsOfAge`.</summary>
        public string[] WeaponsOfAge(string age)
        {
            return Defs.AgeWeapons.Has(age) ? Defs.AgeWeapons[age] : Defs.AgeWeapons["medieval"];
        }

        /// <summary>시대·부위별 장신구 이름(표에 없으면 시대 무관 기본 이름 · 그것도 없으면 부위 한글명 하나) — 원작 `accNames`.</summary>
        public string[] AccNames(string age, string slot)
        {
            OrderedMap<string[]> byAge = Defs.AccNamesByAge.Has(age) ? Defs.AccNamesByAge[age] : null;
            if (byAge != null && byAge.Has(slot)) return byAge[slot];
            if (Defs.AccNames.Has(slot)) return Defs.AccNames[slot];
            return new[] { Defs.SlotKr[slot] };
        }

        /// <summary>부위별 외형 변형 개수(무기 = 그 시대 무기 수 · 투구/갑옷 = 시대별 이름 수 · 장신구 = 이름 수 · 없으면 1).</summary>
        public int VariantCount(string age, string slot)
        {
            if (slot == "weapon") return WeaponsOfAge(age).Length;
            if (slot == "helmet" || slot == "armor")
            {
                OrderedMap<string[]> cat = Defs.ItemNames.Has(age) ? Defs.ItemNames[age] : null;
                int n = cat != null && cat.Has(slot) ? cat[slot].Length : 0;
                return n > 0 ? n : 1;
            }
            int m = AccNames(age, slot).Length;
            return m > 0 ? m : 1;
        }

        /// <summary>특정 시대·부위의 개별 아이템(등급 무관) 1개가 나올 확률(%) — 추첨을 그대로 역산.</summary>
        public double ItemDropChance(string age, string slot)
        {
            double ageP = AgeProbsAt(_s.ForgeLevel).Get(age, 0) / 100;
            double slotP = 1.0 / Defs.Slots.Length;
            double variantP = 1.0 / VariantCount(age, slot);
            return ageP * slotP * variantP * 100;
        }

        // ===== 뽑기 =====

        /// <summary>아이템 롤: 시대(확률표) + 등급 + 레벨(랜덤워크) + 부위 + 서브스탯 + 이름. 난수 소비 순서가 원작과 같다.</summary>
        public ForgeItem RollItem()
        {
            OrderedMap<double> probs = AgeProbsAt(_s.ForgeLevel);
            string age = _rng.WeightedPick(probs);
            int ageIdx = Array.IndexOf(Defs.Ages, age);

            string rarity = _rng.WeightedPick(ForgeRules.RarityWeights(_s.ForgeLevel));

            double level = RollLevelOf(age);
            AdvanceRollLevel(age);

            string slot = _rng.Choice(Defs.Slots);
            double lvMult = Math.Pow(ForgeRules.LevelStep, level - 1);
            double rMult = Defs.RarityMult[rarity];
            string main = Defs.SlotMain[slot];
            double value = Math.Floor((main == "atk" ? ForgeRules.TierBaseAtkAt(ageIdx) : ForgeRules.TierBaseHpAt(ageIdx)) * lvMult * rMult);

            int numSubs = _rng.RandInt(1, Math.Min(4, Array.IndexOf(Defs.Rarities, rarity) + 1));
            List<Substat> subs = SubstatRoll.Roll(Defs, _rng, numSubs);

            string wtype = null, name;
            int nameIdx;
            if (slot == "weapon")
            {
                string[] pool = WeaponsOfAge(age);
                nameIdx = _rng.RandInt(0, pool.Length - 1);
                wtype = pool[nameIdx];
                name = Defs.WeaponTypes[wtype].Kr;
            }
            else
            {
                OrderedMap<string[]> cat = Defs.ItemNames.Has(age) ? Defs.ItemNames[age] : null;
                if (cat != null && cat.Has(slot))
                {
                    string[] names = cat[slot];
                    nameIdx = _rng.RandInt(0, names.Length - 1);
                    name = names[nameIdx];
                }
                else
                {
                    string[] accs = AccNames(age, slot);
                    nameIdx = _rng.RandInt(0, accs.Length - 1);
                    name = accs[nameIdx];
                }
            }

            return new ForgeItem
            {
                Name = name, Slot = slot, Age = age, AgeIdx = ageIdx, Rarity = rarity, Level = level, Main = main, Value = value,
                Subs = subs, WType = wtype, NameIdx = nameIdx, Stars = _s.AscendCount
            };
        }

        /// <summary>제작 count 회 — 망치 1개씩(무료 제련 확률만큼 건너뜀) · 망치가 없으면 거기서 끝. 퀘스트 «장비 제작» 을 개수만큼.</summary>
        public List<ForgeItem> Craft(int count)
        {
            var results = new List<ForgeItem>();
            for (int i = 0; i < count; i++)
            {
                if (_w.Hammers < 1) break;
                if (!_rng.Chance(_mods.FreeForgeChance)) _w.Hammers -= 1;
                _w.TotalCrafts++;
                results.Add(RollItem());
            }
            if (results.Count > 0 && Crafted != null) Crafted();
            Bump("craft", results.Count);
            return results;
        }

        // ===== 자동 제련 설정(UI-SPEC 21~24) =====

        /// <summary>설정을 돌려준다 — 없으면 기본값 · 옛 `continueOnTarget` 은 지우고 stopOnTarget=false(참/거짓 모두 «계속»).</summary>
        public AutoForgeConfig AutoForgeConfig()
        {
            if (_s.AutoForge == null) _s.AutoForge = new AutoForgeConfig();
            var cfg = _s.AutoForge;
            if (cfg.KeepAges == null) cfg.KeepAges = new List<string>();
            if (cfg.FilterSubs == null) cfg.FilterSubs = new List<string>();
            if (cfg.LegacyContinueOnTarget.HasValue) { cfg.LegacyContinueOnTarget = null; cfg.StopOnTarget = false; }
            return cfg;
        }

        /// <summary>유지 시대나 옵션 필터를 하나라도 켜 뒀는가 = «목표 장비» 가 정의돼 있는가.</summary>
        public bool HasAutoTarget()
        {
            var cfg = AutoForgeConfig();
            return cfg.KeepAges.Count > 0 || (cfg.FilterOn && cfg.FilterSubs.Count > 0);
        }

        /// <summary>
        /// 유지 시대·옵션 필터를 통과하는 아이템 = **비교 팝업 후보**(탈락 = 즉시 판매). 목표 미설정이면 무엇도 후보가 아니다.
        /// 통과의 뜻은 «플레이어에게 물어본다» 지 «입혀 준다» 가 아니다 — 자동 장착은 원작에서 삭제됐다.
        /// </summary>
        public bool PassesAutoFilter(ForgeItem item)
        {
            var cfg = AutoForgeConfig();
            if (!HasAutoTarget()) return false;
            if (cfg.KeepAges.Count > 0 && !cfg.KeepAges.Contains(item.Age)) return false;
            if (cfg.FilterOn && cfg.FilterSubs.Count > 0)
            {
                bool any = false;
                var subs = item.Subs ?? new List<Substat>();
                for (int i = 0; i < subs.Count; i++) if (cfg.FilterSubs.Contains(subs[i].Key)) { any = true; break; }
                if (!any) return false;
            }
            return true;
        }

        // ===== 업그레이드(비용·시간 표 · 실시간 타이머) =====

        /// <summary>다음 레벨의 비용·시간 — 만렙이면 null.</summary>
        public ForgeUpgrade UpgradeInfo()
        {
            int next = _s.ForgeLevel + 1;
            if (next > ForgeRules.MaxLevel) return null;
            return Table.HasUpgrade(next) ? Table.Upgrade(next) : null;
        }

        public double UpgradeCost(ForgeUpgrade info) { return ForgeRules.UpgradeCost(info, _mods.ForgeCostMult); }
        public double UpgradeTime(ForgeUpgrade info) { return ForgeRules.UpgradeTime(info, _mods.ForgeTimeMult); }

        public bool CanStartUpgrade()
        {
            ForgeUpgrade info = UpgradeInfo();
            return info != null && !_s.UpgradeEndsAt.HasValue && _w.Coins >= UpgradeCost(info);
        }

        /// <summary>코인을 빼고 타이머를 건다(망치는 제작 전용). 퀘스트 coinSpend·upgradeStart · 저장 요청.</summary>
        public bool StartUpgrade()
        {
            ForgeUpgrade info = UpgradeInfo();
            if (!CanStartUpgrade()) return false;
            double spent = UpgradeCost(info);
            _w.Coins -= spent;
            Bump("coinSpend", spent);
            Bump("upgradeStart", 1);
            _s.UpgradeEndsAt = _now() + UpgradeTime(info) * 1000;
            if (SaveRequested != null) SaveRequested();
            return true;
        }

        /// <summary>남은 시간 10분당 젬 1(올림) · 진행 중이 아니면 0.</summary>
        public double GemSkipCost()
        {
            if (!_s.UpgradeEndsAt.HasValue) return 0;
            return ForgeRules.GemSkipCost(_s.UpgradeEndsAt.Value - _now());
        }

        /// <summary>젬으로 즉시 완료 — 젬이 모자라거나 진행 중이 아니면 false.</summary>
        public bool GemSkip()
        {
            double cost = GemSkipCost();
            if (_w.Gems < cost || !_s.UpgradeEndsAt.HasValue) return false;
            _w.Gems -= cost;
            _s.UpgradeEndsAt = _now() - 1;
            TickUpgrade();
            return true;
        }

        /// <summary>타이머가 끝났으면 레벨 +1(만렙 상한) · 퀘스트 gearUpgrade · LevelReached · 저장 요청. 올랐으면 true.</summary>
        public bool TickUpgrade()
        {
            if (_s.UpgradeEndsAt.HasValue && _now() >= _s.UpgradeEndsAt.Value)
            {
                _s.ForgeLevel = Math.Min(ForgeRules.MaxLevel, _s.ForgeLevel + 1);
                _s.UpgradeEndsAt = null;
                Bump("gearUpgrade", 1);
                if (LevelReached != null) LevelReached(_s.ForgeLevel);
                if (SaveRequested != null) SaveRequested();
                return true;
            }
            return false;
        }

        void Bump(string key, double n) { if (QuestBump != null) QuestBump(key, n); }
        static double Clamp(double v, double a, double b) { return Math.Min(b, Math.Max(a, v)); }
    }
}
