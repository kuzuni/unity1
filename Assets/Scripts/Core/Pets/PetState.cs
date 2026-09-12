using System.Collections.Generic;

namespace Forge.Core.Pets
{
    /// <summary>미부화 알 — 원작 `S.eggs` 의 한 칸 `{rarity}`. 합성·흡수는 **개체 동일성**(참조)으로 찾는다(원작 `indexOf`).</summary>
    public sealed class Egg
    {
        public string Rarity;
        public Egg() { }
        public Egg(string rarity) { Rarity = rarity; }
    }

    /// <summary>부화 중인 칸 — 원작 `S.hatching` 의 `{rarity, endsAt}`. `EndsAt` 은 절대시각(ms · `U.now()` 기준).</summary>
    public sealed class HatchSlot
    {
        public string Rarity;
        public double EndsAt;
        public HatchSlot() { }
        public HatchSlot(string rarity, double endsAt) { Rarity = rarity; EndsAt = endsAt; }
    }

    /// <summary>서브스탯 한 줄 — 원작 `U.rollSubs` 가 만드는 `{key, label, value}`(값은 소수 한 자리 · 쿨감도 양수 저장).</summary>
    public sealed class Substat
    {
        public string Key;
        public string Label;
        public double Value;
        public Substat() { }
        public Substat(string key, string label, double value) { Key = key; Label = label; Value = value; }
    }

    /// <summary>
    /// 펫 개체 — 원작 `S.pets[i]` 의 `{name, rarity, level, dupes, xp, stars, subs}`.
    /// 같은 종이 여러 개체로 들어온다(원작 2026-08-19 지시). `Dupes` 는 구세이브의 겹침 숫자(합성이 먼저 소진한다).
    /// </summary>
    public sealed class Pet
    {
        public string Name;
        public string Rarity;
        public int Level = 1;
        public int Dupes;
        public double Xp;
        public int Stars;
        public List<Substat> Subs = new List<Substat>();
    }

    /// <summary>
    /// 펫 계통이 쥐는 세이브 상태 — 원작 `S` 의 펫 칸(`eggs`·`hatching`·`pets`·`activePets`·`petSummonCount`·`hatchSlotBonus`)만.
    /// 젬·알 화폐는 다른 계통과 나눠 쓰므로 <see cref="IPetHost"/> 가 든다. 세이브 스키마(T13)는 이것을 그대로 직렬화한다.
    /// </summary>
    public sealed class PetState
    {
        public List<Egg> Eggs = new List<Egg>();
        public List<HatchSlot> Hatching = new List<HatchSlot>();
        public List<Pet> Pets = new List<Pet>();
        /// <summary>출전 중인 <see cref="Pets"/> 인덱스(순서 = 출전 순서 · 대형 자리).</summary>
        public List<int> ActivePets = new List<int>();
        /// <summary>누적 펫 소환 수 → 소환 레벨.</summary>
        public int PetSummonCount;
        /// <summary>부화장 슬롯 젬 구매 수.</summary>
        public int HatchSlotBonus;
    }
}
