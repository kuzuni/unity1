using System;
using System.Collections.Generic;
using Forge.Core.Battle;
using Forge.Core.Data;
using Forge.Core.Forging;
using Forge.Core.Pets;

namespace Forge.Core.Gear
{
    /// <summary>`autoResolve` 반환 `{equipped, gained}` — 자동 장착은 원작에서 삭제됐으므로 `Equipped` 는 항상 false(모양만 유지 · 호출부가 읽는다).</summary>
    public struct AutoResolveResult
    {
        public bool Equipped;
        public double Gained;
    }

    /// <summary>
    /// 원작 `forge.js` 의 장착·판매·능력치 절(T15 · 결정 20 의 T14/T15 경계): `itemValue` · `itemPower` · `isMatchingGear` · `sellPrice` · `equip` · `sell` · `autoResolve` ·
    /// `allSubsBag` · `heroStats`. 상태는 <see cref="GearState"/>(S.equipment) · 지갑은 T14 <see cref="Wallet"/>(S.coins) · 바깥은 <see cref="IGearHost"/>.
    /// 정본 실행 벡터(`tools/gear_vectors.js` → `Tests/EditMode/Vectors/t15-gear.json`)와 EditMode 가 대조한다.
    /// </summary>
    public sealed class GearSystem
    {
        readonly GameData _data;
        readonly GearState _s;
        readonly Wallet _w;
        readonly IGearHost _h;

        /// <summary>원작 `console.error('Forge.sell: 판매가가 유한하지 않아 …')` — 지급을 건너뛴 반쪽 항목.</summary>
        public event Action<ForgeItem, double> SellRefused;

        public GearSystem(GameData data, GearState state, Wallet wallet, IGearHost host)
        {
            if (data == null) throw new ArgumentNullException("data");
            _data = data; _s = state ?? new GearState(); _w = wallet ?? new Wallet(); _h = host ?? new GearHost();
        }

        public GameDefs Defs { get { return _data.Defs; } }
        public GearState State { get { return _s; } }
        public Wallet Wallet { get { return _w; } }
        public IGearHost Host { get { return _h; } }

        public Big ItemValue(ForgeItem item) { return GearRules.ItemValue(item, _h.StarMult); }
        public Big ItemPower(ForgeItem item) { return GearRules.ItemPower(item, _h.StarMult); }
        public bool IsMatchingGear(ForgeItem a, ForgeItem b) { return GearRules.IsMatchingGear(a, b); }
        public double SellPrice(ForgeItem item) { return GearRules.SellPrice(Defs, item, _h.SellPriceMult); }

        /// <summary>`Forge.equip(item)` — 그 부위에 끼우고 페이퍼돌 갱신(교체 연출) · 영웅 재계산 · 퀘스트 `equipGear`. 반환: 이전 장비(보관·판매 없음 · 호출부는 참조만).</summary>
        public ForgeItem Equip(ForgeItem item)
        {
            if (item == null) throw new ArgumentNullException("item");
            ForgeItem prev = _s.Get(item.Slot);
            _s.Set(item.Slot, item);
            _h.RefreshHeroEquip(true);
            _h.RecalcHero();
            _h.QuestBump("equipGear", 1);
            return prev;
        }

        /// <summary>`Forge.sell(item)` — 판매가가 유한하지 않으면(반쪽 항목) 지급을 건너뛰고 0. 코인 가산 · 퀘스트 `sellGear`.</summary>
        public double Sell(ForgeItem item)
        {
            double price = SellPrice(item);
            if (double.IsNaN(price) || double.IsInfinity(price))
            {
                if (SellRefused != null) SellRefused(item, price);
                return 0;
            }
            _w.Coins += price;
            _h.QuestBump("sellGear", 1);
            return price;
        }

        /// <summary>`Forge.autoResolve(item)` — 자동 경로의 장비는 **무조건 판매**(자동 장착은 삭제됐다 · 되살리지 말 것).</summary>
        public AutoResolveResult AutoResolve(ForgeItem item)
        {
            return new AutoResolveResult { Equipped = false, Gained = Sell(item) };
        }

        /// <summary>`Forge.allSubsBag()` — 장비 8부위(SLOTS 순서 · subs 없는 반쪽은 건너뜀) + 출전 펫 + 장착 탈것 서브스탯 합.</summary>
        public SubsBag AllSubsBag()
        {
            var gearSubs = new List<Substat>();
            string[] slots = Defs.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                ForgeItem it = _s.Get(slots[i]);
                if (it != null && it.Subs != null) gearSubs.AddRange(it.Subs);
            }
            Big a, h;
            var petSubs = new List<Substat>();
            _h.PetBonus(out a, out h, petSubs);
            var mountSubs = new List<Substat>();
            _h.MountBonus(out a, out h, mountSubs);
            return SubsBag.Sum(Defs, gearSubs, petSubs, mountSubs);
        }

        /// <summary>`Forge.heroStats()` — 장비 + 펫 + 탈것 + 스킬 패시브 + 서브스탯 + 버프(고정 가산 · % 배율 뒤). atk·hp 는 Big.</summary>
        public HeroStats HeroStats(out SubsBag bag)
        {
            Big atk = Big.Of(GearRules.BaseAtk), hp = Big.Of(GearRules.BaseHp);
            Big gearAtk = Big.Zero, gearHp = Big.Zero;
            string[] slots = Defs.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                ForgeItem it = _s.Get(slots[i]);
                if (it == null) continue;
                Big v = ItemValue(it);
                if (it.Main == "atk") gearAtk = gearAtk.Add(v); else gearHp = gearHp.Add(v);
            }
            Big pa, ph, ma, mh, sa, sh;
            _h.PetBonus(out pa, out ph, null);
            _h.MountBonus(out ma, out mh, null);
            _h.SkillPassive(out sa, out sh);
            bag = AllSubsBag();
            Big buffAtkFlat = _h.BuffAtkFlat;

            atk = atk.Add(gearAtk.Mul(Big.Of(_h.GearAtkMult))).Add(pa).Add(ma).Add(sa);
            hp = hp.Add(gearHp.Mul(Big.Of(_h.GearHpMult))).Add(ph).Add(mh).Add(sh);
            return new HeroStats
            {
                Atk = atk.Mul(Big.Of(1 + bag.DmgPct / 100)).Add(buffAtkFlat),
                Hp = hp.Mul(Big.Of(1 + bag.HpPct / 100)),
                CritCh = Math.Min(GearRules.CritCap, GearRules.CritBase + bag.CritCh),
                CritDmg = GearRules.CritDmgBase + bag.CritDmg,
                AttacksPerSec = GearRules.AttacksPerSecBase * (1 + bag.AtkSpd / 100),
                DblAtk = Math.Min(GearRules.DblAtkCap, bag.DblAtk),
                Block = Math.Min(GearRules.BlockCap, bag.Block),
                HpRegen = bag.HpRegen,
                Lifesteal = bag.Lifesteal,
                MeleeDmg = bag.MeleeDmg,
                RangedDmg = bag.RangedDmg,
                SkillDmg = bag.SkillDmg,
                SkillCd = Math.Min(GearRules.SkillCdCap, bag.SkillCd),
            };
        }

        public HeroStats HeroStats() { SubsBag bag; return HeroStats(out bag); }
    }
}
