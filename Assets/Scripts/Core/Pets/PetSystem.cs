using System;
using System.Collections.Generic;
using Forge.Core.Data;

namespace Forge.Core.Pets
{
    /// <summary>소환 한 번의 결과 — 원작 `Pets.summon` 반환 `{results, requested, summoned, clamped}` + 효과음용 최고 등급.</summary>
    public sealed class SummonResult
    {
        public sealed class Item { public string Rarity; public bool Extra; }
        public List<Item> Results = new List<Item>();
        /// <summary>요청 배수(원작은 인자를 그대로 돌려준다 · 소수여도).</summary>
        public double Requested;
        /// <summary>유료로 나간 알 수(보너스 제외).</summary>
        public int Summoned;
        /// <summary>보관함 여유 부족으로 요청보다 적게 나갔는가 — UI 안내 토스트용.</summary>
        public bool Clamped;
        /// <summary>`SFX.gacha(bestRarity)` 에 줄 등급.</summary>
        public string BestRarity;
    }

    /// <summary>부화 한 건의 결과 — 원작 `Pets.tick` 이 토스트·퀘스트로 알리던 것.</summary>
    public sealed class HatchResult
    {
        public string Rarity;
        /// <summary>부화한 종(보관함이 찼어도 종은 이미 굴렸다).</summary>
        public string Name;
        /// <summary>새로 들어온 개체(보관함이 찼으면 null).</summary>
        public Pet Pet;
        /// <summary>같은 이름이 이미 있었는가(«하나 더 획득» 토스트).</summary>
        public bool Already;
        /// <summary>자동 출전됐는가.</summary>
        public bool AutoActivated;
        /// <summary>보유 상한에 걸려 알로 되돌렸는가.</summary>
        public bool ReturnedAsEgg;
        /// <summary>보유 상한 + 알 보관함도 가득 → 사라졌는가.</summary>
        public bool Dropped;
    }

    public enum MergeOutcome
    {
        /// <summary>합성됨 — 다음 등급 알 하나.</summary>
        Merged,
        /// <summary>여분이 3 미만이거나 최상위 등급.</summary>
        NotEnough,
        /// <summary>알 보관함이 가득 차 합성할 수 없다(원작 토스트).</summary>
        EggCapFull
    }

    /// <summary>
    /// 원작 `web/js/pets.js` 의 `Pets` 를 그대로 옮긴 순수 C# — 알 드랍(`eggDropRates`) · 소환(`skillRatesData` 재사용) ·
    /// 부화(`baseHatchingTimes` × 기술트리) · 젬 스킵 · 부화장 슬롯 · 경험치 흡수 · 합성(여분 3 → 상위 알) · 출전 3마리 · 전투 기여.
    /// 수치는 <see cref="GameData"/>(T2 JSON)와 <see cref="PetRules"/>(원작 코드 상수)에서만 온다. 난수는 <see cref="Rng"/> 로 받아
    /// 원작과 **같은 순서로 같은 횟수** 소비한다(EditMode 가 `pet_vectors.json` 과 대조). UI·퀘스트·저장은 호출자가 결과로 한다.
    /// </summary>
    public sealed class PetSystem
    {
        readonly GameData _data;
        readonly PetRules _rules;
        readonly IPetHost _host;
        readonly Rng _rng;

        public PetState State;

        public PetSystem(GameData data, PetRules rules, IPetHost host, Rng rng, PetState state = null)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (host == null) throw new ArgumentNullException("host");
            if (rng == null) throw new ArgumentNullException("rng");
            _data = data; _rules = rules ?? PetRules.Original(); _host = host; _rng = rng;
            State = state ?? new PetState();
        }

        public PetRules Rules { get { return _rules; } }
        string[] Rarities { get { return _data.Defs.Rarities; } }
        int RarityIndex(string rarity) { return Array.IndexOf(Rarities, rarity); }
        PetTable Table { get { return _data.Balance.Pets; } }

        // ===== 부화장 슬롯 =====

        /// <summary>현재 부화장 슬롯 수 = min(상한, 기본 + 젬 구매분).</summary>
        public int MaxHatchSlots() { return Math.Min(_rules.MaxHatchSlotsCap, _rules.BaseHatchSlots + State.HatchSlotBonus); }
        /// <summary>다음 슬롯 값 = 단가 × (1 + 구매분).</summary>
        public double SlotCost() { return _rules.SlotGemCost * (1 + State.HatchSlotBonus); }
        public bool CanBuySlot() { return MaxHatchSlots() < _rules.MaxHatchSlotsCap; }
        public bool BuySlot()
        {
            if (!CanBuySlot()) return false;
            double cost = SlotCost();
            if (_host.Gems < cost) return false;
            _host.Gems -= cost;
            State.HatchSlotBonus += 1;
            return true;
        }

        // ===== 알 드랍 · 보관 =====

        /// <summary>스테이지 키("c-s")에 해당하는 알 등급 롤 — 없는 키는 `10-10` 표로(원작 폴백).</summary>
        public string RollEggRarity(string stageKey)
        {
            OrderedMap<double> rates;
            if (!Table.EggDropRates.TryGet(stageKey, out rates)) rates = Table.EggDropRates["10-10"];
            return _rng.WeightedPick(rates);
        }
        public string RollEggRarity(int chapter, int stage) { return RollEggRarity(ForgeTable.Key(chapter) + "-" + ForgeTable.Key(stage)); }

        public bool AddEgg(string rarity)
        {
            if (State.Eggs.Count >= _rules.EggCap) return false;
            State.Eggs.Add(new Egg(rarity));
            return true;
        }

        public int EggSpace() { return Math.Max(0, _rules.EggCap - State.Eggs.Count); }

        // ===== 소환 =====

        /// <summary>소환 레벨 = min(표 끝, floor(누적/5) + 1).</summary>
        public int SummonLevel() { return Math.Min(_data.Balance.Skills.MaxLevel, State.PetSummonCount / 5 + 1); }

        /// <summary>등급 확률(원작은 스킬 소환 곡선 `skillRatesData` 를 재사용) — 키 순서 = RARITIES(가중치 추첨 순서가 곧 규칙이다).</summary>
        public OrderedMap<double> Rates(int level = 0)
        {
            int max = _data.Balance.Skills.MaxLevel;
            int l = level != 0 ? level : SummonLevel();
            l = Math.Min(max, Math.Max(1, l));
            double[] r = _data.Balance.Skills.RatesAt(l);
            var m = new OrderedMap<double>();
            for (int i = 0; i < Rarities.Length; i++) m.Add(Rarities[i], i < r.Length ? r[i] : 0);
            return m;
        }

        /// <summary>실제로 소환될 개수 = min(max(1, floor(count) || 1), 보관함 여유).</summary>
        public int SummonCount(double count = 1)
        {
            double f = Math.Floor(count);
            int c = double.IsNaN(f) || f == 0 ? 1 : (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, f));
            return Math.Min(Math.Max(1, c), EggSpace());
        }
        public double SummonCost(double count = 1) { return _rules.SummonEggCost * SummonCount(count); }
        public bool CanSummon(double count = 1)
        {
            int n = SummonCount(count);
            return n >= 1 && _host.EggCurrency >= _rules.SummonEggCost * n;
        }

        /// <summary>count번 연속 소환 — 비용·여유를 선결제로 확인 · 기술트리 보너스 알은 유료분 자리를 예약하고 남을 때만. 못 하면 null.</summary>
        public SummonResult Summon(double count = 1)
        {
            int n = SummonCount(count);
            if (n < 1 || _host.EggCurrency < _rules.SummonEggCost * n) return null;
            _host.EggCurrency -= _rules.SummonEggCost * n;
            var res = new SummonResult { Requested = count };
            for (int i = 0; i < n; i++)
            {
                State.PetSummonCount += 1;
                string rarity = _rng.WeightedPick(Rates());
                if (!AddEgg(rarity)) { _host.EggCurrency += _rules.SummonEggCost * (n - i); break; }
                res.Results.Add(new SummonResult.Item { Rarity = rarity });
                // 원작 `U.chance(...)` 는 확률이 0 이어도 난수를 하나 먹는다 — 순서를 지킨다.
                if (_rng.Chance(_host.ExtraEggChance()) && EggSpace() > (n - 1 - i))
                {
                    string extra = _rng.WeightedPick(Rates());
                    if (AddEgg(extra)) res.Results.Add(new SummonResult.Item { Rarity = extra, Extra = true });
                }
            }
            string best = res.Results.Count > 0 ? res.Results[0].Rarity : null;
            for (int i = 1; i < res.Results.Count; i++)
                if (RarityIndex(res.Results[i].Rarity) > RarityIndex(best)) best = res.Results[i].Rarity;
            res.BestRarity = best;
            res.Summoned = 0;
            for (int i = 0; i < res.Results.Count; i++) if (!res.Results[i].Extra) res.Summoned++;
            res.Clamped = n < count;
            return res;
        }

        // ===== 부화 =====

        /// <summary>부화 시간(초) = 표(분) × 60 × 기술트리 가속.</summary>
        public double HatchTimeSec(string rarity) { return Table.BaseHatchingTimes[rarity] * 60 * _host.HatchSpeedMult(); }

        public bool StartHatch(int eggIdx)
        {
            if (State.Hatching.Count >= MaxHatchSlots()) return false;
            if (eggIdx < 0 || eggIdx >= State.Eggs.Count) return false;
            Egg egg = State.Eggs[eggIdx];
            State.Eggs.RemoveAt(eggIdx);
            State.Hatching.Add(new HatchSlot(egg.Rarity, _host.Now() + HatchTimeSec(egg.Rarity) * 1000));
            return true;
        }

        /// <summary>젬 스킵 값 = ceil(남은 분 / 10).</summary>
        public double GemSkipCost(HatchSlot h)
        {
            double remainMin = Math.Max(0, (h.EndsAt - _host.Now()) / 60000);
            return Math.Ceiling(remainMin / 10);
        }

        /// <summary>젬으로 즉시 부화 — 성공하면 곧바로 <see cref="Tick"/> 한 결과(부화 목록) · 실패(없는 칸 · 젬 부족)는 null.</summary>
        public List<HatchResult> GemSkip(int idx)
        {
            if (idx < 0 || idx >= State.Hatching.Count) return null;
            HatchSlot h = State.Hatching[idx];
            double cost = GemSkipCost(h);
            if (_host.Gems < cost) return null;
            _host.Gems -= cost;
            h.EndsAt = _host.Now() - 1;
            return Tick();
        }

        /// <summary>부화 완료 처리 — 뒤에서 앞으로 · 종은 등급 풀에서 균등 · 보유 상한이면 알로 되돌린다 · 자동 출전은 출전 상한 안에서.</summary>
        public List<HatchResult> Tick()
        {
            var results = new List<HatchResult>();
            double now = _host.Now();
            for (int i = State.Hatching.Count - 1; i >= 0; i--)
            {
                HatchSlot h = State.Hatching[i];
                if (!(now >= h.EndsAt)) continue;
                State.Hatching.RemoveAt(i);
                PetStat def = _rng.Choice(Table.Stats[h.Rarity]);
                var r = new HatchResult { Rarity = h.Rarity, Name = def.Name };
                if (State.Pets.Count >= _rules.InvCap)
                {
                    r.ReturnedAsEgg = AddEgg(h.Rarity);
                    r.Dropped = !r.ReturnedAsEgg;
                }
                else
                {
                    bool already = false;
                    for (int k = 0; k < State.Pets.Count; k++) if (State.Pets[k].Name == def.Name) { already = true; break; }
                    var p = new Pet { Name = def.Name, Rarity = h.Rarity, Level = 1, Dupes = 0, Xp = 0, Stars = _host.PetAscendCount(), Subs = RollSubs() };
                    State.Pets.Add(p);
                    r.Pet = p; r.Already = already;
                    if (State.ActivePets.Count < Math.Min(_rules.AutoActive, _rules.MaxActive))
                    {
                        State.ActivePets.Add(State.Pets.Count - 1);
                        r.AutoActivated = true;
                    }
                }
                results.Add(r);
            }
            return results;
        }

        // ===== 스탯 =====

        /// <summary>레벨 배율 — 장비와 같은 커브(`Forge.levelMult`).</summary>
        public double LevelMult(Pet p) { return _host.LevelMult(p.Level); }

        /// <summary>등급 기준치 = 같은 등급 장비 8부위 합 / POWER_DIV.</summary>
        public AtkHp BaseStat(string rarity)
        {
            return new AtkHp { Atk = _host.GearSumAtkAt(rarity) / _rules.PowerDiv, Hp = _host.GearSumHpAt(rarity) / _rules.PowerDiv };
        }

        /// <summary>옵션 2개 — 장비와 같은 풀에서.</summary>
        public List<Substat> RollSubs() { return SubstatRoll.Roll(_data.Defs, _rng, 2); }

        /// <summary>출전 시 1마리 기여 = 등급 기준치 × 레벨 배율 × 승천 배율 × 기술트리(곱하는 순서도 원작과 같다).</summary>
        public void PetPower(Pet p, out Big atk, out Big hp)
        {
            AtkHp b = BaseStat(p.Rarity);
            Big mult = _host.StarMult(p.Stars).Mul(Big.Of(LevelMult(p)));
            atk = mult.Mul(Big.Of(b.Atk * _host.PetDmgMult()));
            hp = mult.Mul(Big.Of(b.Hp * _host.PetHpMult()));
        }

        /// <summary>출전 중인 모든 펫의 합산 — 고정 공격력·체력(Big) + 서브스탯 원본 목록.</summary>
        public void ActiveBonus(out Big atk, out Big hp, List<Substat> subs)
        {
            atk = Big.Zero; hp = Big.Zero;
            for (int k = 0; k < State.ActivePets.Count; k++)
            {
                int idx = State.ActivePets[k];
                if (idx < 0 || idx >= State.Pets.Count) continue;
                Pet p = State.Pets[idx];
                Big a, h;
                PetPower(p, out a, out h);
                atk = atk.Add(a); hp = hp.Add(h);
                if (subs != null && p.Subs != null) subs.AddRange(p.Subs);
            }
        }

        // ===== 경험치 =====

        /// <summary>레벨업 필요 경험치 = floor(80 × level^1.6).</summary>
        public double XpNeeded(int level) { return Math.Floor(80 * Math.Pow(level, 1.6)); }
        /// <summary>재료 경험치 = 30 × 3^등급 인덱스.</summary>
        public double XpValue(string rarity) { return 30 * Math.Pow(3, RarityIndex(rarity)); }

        public void AddXp(int idx, double amount)
        {
            if (idx < 0 || idx >= State.Pets.Count) return;
            Pet p = State.Pets[idx];
            p.Xp += amount;
            while (p.Level < _rules.MaxLevel && p.Xp >= XpNeeded(p.Level))
            {
                p.Xp -= XpNeeded(p.Level);
                p.Level++;
            }
            if (p.Level >= _rules.MaxLevel) p.Xp = 0;
        }

        /// <summary>재료(펫·알 · 참조로 찾는다)를 흡수해 대상에 경험치로 — 재료는 사라지고 출전 인덱스는 당긴다. 대상이 목록에 없으면 false(재료는 이미 소모 · 원작과 같다).</summary>
        public bool AbsorbMaterials(Pet target, IList<Pet> materialPets, IList<Egg> materialEggs)
        {
            if (target == null) return false;
            double totalXp = 0;
            if (materialPets != null)
                for (int k = 0; k < materialPets.Count; k++)
                {
                    Pet p = materialPets[k];
                    if (ReferenceEquals(p, target)) continue;
                    totalXp += XpValue(p.Rarity) * LevelMult(p);
                    int idx = IndexOfRef(State.Pets, p);
                    if (idx < 0) continue;
                    RemovePetAt(idx);
                }
            if (materialEggs != null)
                for (int k = 0; k < materialEggs.Count; k++)
                {
                    Egg e = materialEggs[k];
                    totalXp += XpValue(e.Rarity);
                    int idx = IndexOfRef(State.Eggs, e);
                    if (idx >= 0) State.Eggs.RemoveAt(idx);
                }
            int targetIdx = IndexOfRef(State.Pets, target);
            if (targetIdx < 0) return false;
            AddXp(targetIdx, totalXp);
            return true;
        }

        // ===== 출전 =====

        public bool CanActivate(int petIdx)
        {
            if (petIdx < 0 || petIdx >= State.Pets.Count) return false;
            return State.ActivePets.IndexOf(petIdx) >= 0 || State.ActivePets.Count < _rules.MaxActive;
        }

        /// <summary>출전/해제 토글 — 출전은 MAX_ACTIVE 까지 · 해제는 언제나. 상한과 «없는 펫» 은 둘 다 false(호출부가 <see cref="CanActivate"/> 로 먼저 묻는다).</summary>
        public bool ToggleActive(int petIdx)
        {
            if (petIdx < 0 || petIdx >= State.Pets.Count) return false;
            int pos = State.ActivePets.IndexOf(petIdx);
            if (pos >= 0) State.ActivePets.RemoveAt(pos);
            else if (State.ActivePets.Count >= _rules.MaxActive) return false;
            else State.ActivePets.Add(petIdx);
            return true;
        }

        // ===== 합성 =====

        public bool CanMerge(string rarity)
        {
            int ri = RarityIndex(rarity);
            if (ri >= Rarities.Length - 1) return false;
            return DupesOfRarity(rarity) >= 3;
        }

        /// <summary>소모해도 되는 여분 = 같은 등급 · 레벨 1 · 별 0 · 출전 중 아님.</summary>
        public List<int> SpareIdxsOfRarity(string rarity)
        {
            var outIdx = new List<int>();
            for (int i = 0; i < State.Pets.Count; i++)
            {
                Pet p = State.Pets[i];
                if (p.Rarity != rarity) continue;
                if (Math.Max(p.Level, 1) > 1 || p.Stars > 0) continue;
                if (State.ActivePets.IndexOf(i) >= 0) continue;
                outIdx.Add(i);
            }
            return outIdx;
        }

        /// <summary>구세이브 `dupes` 숫자 + 여분 개체 수.</summary>
        public int DupesOfRarity(string rarity)
        {
            int legacy = 0;
            for (int i = 0; i < State.Pets.Count; i++) if (State.Pets[i].Rarity == rarity) legacy += Math.Max(0, State.Pets[i].Dupes);
            return legacy + SpareIdxsOfRarity(rarity).Count;
        }

        /// <summary>같은 등급 여분 3개 → 상위 등급 알. `dupes` 숫자부터 소진하고 그다음 여분 개체를 **뒤에서부터** 지운다.</summary>
        public MergeOutcome Merge(string rarity, out string nextRarity)
        {
            nextRarity = null;
            if (!CanMerge(rarity)) return MergeOutcome.NotEnough;
            if (State.Eggs.Count >= _rules.EggCap) return MergeOutcome.EggCapFull;
            int need = 3;
            for (int i = 0; i < State.Pets.Count && need > 0; i++)
            {
                Pet p = State.Pets[i];
                if (p.Rarity != rarity || !(p.Dupes > 0)) continue;
                int use = Math.Min(p.Dupes, need);
                p.Dupes -= use; need -= use;
            }
            if (need > 0)
            {
                List<int> spare = SpareIdxsOfRarity(rarity);
                int from = Math.Max(0, spare.Count - need);
                for (int k = spare.Count - 1; k >= from; k--)
                {
                    RemovePetAt(spare[k]);
                    need--;
                }
            }
            nextRarity = Rarities[RarityIndex(rarity) + 1];
            AddEgg(nextRarity);
            return MergeOutcome.Merged;
        }

        // ===== 내부 =====

        /// <summary>개체를 지우고 출전 인덱스를 당긴다(원작 `S.activePets.filter(a => a !== idx).map(a => a > idx ? a − 1 : a)`).</summary>
        void RemovePetAt(int idx)
        {
            State.Pets.RemoveAt(idx);
            var next = new List<int>(State.ActivePets.Count);
            for (int k = 0; k < State.ActivePets.Count; k++)
            {
                int a = State.ActivePets[k];
                if (a == idx) continue;
                next.Add(a > idx ? a - 1 : a);
            }
            State.ActivePets = next;
        }

        static int IndexOfRef<T>(List<T> list, T item) where T : class
        {
            for (int i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], item)) return i;
            return -1;
        }
    }
}
